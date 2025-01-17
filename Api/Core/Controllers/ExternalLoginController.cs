using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Models;
using Api.Core.Services;
using Api.Modules.Tenants.Interfaces;
using GeeksCoreLibrary.Core.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Client.WebIntegration;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;
using OpenIddictConstants = OpenIddict.Abstractions.OpenIddictConstants;

namespace Api.Core.Controllers;

[Controller]
public class ExternalLoginController : Controller
{
    private readonly ILogger<ExternalLoginController> logger;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly GclSettings gclSettings;
    private readonly IUsersService usersService;

    public ExternalLoginController(ILogger<ExternalLoginController> logger,
        IHttpContextAccessor httpContextAccessor, IOptions<GclSettings> gclSettings, IUsersService usersService)
    {
        this.logger = logger;
        this.httpContextAccessor = httpContextAccessor;
        this.usersService = usersService;
        this.gclSettings = gclSettings.Value;
    }

    [AllowAnonymous]
    [HttpGet("/signin/google")]
    public IActionResult GoogleSignin()
    {
        var context = httpContextAccessor.HttpContext;

        context.Request.Query.TryGetValue("subDomain", out var subDomain);
        context.Request.Query.TryGetValue("redirect_uri", out var redirectUri);
        context.Request.Query.TryGetValue("state", out var state);
        context.Request.Query.TryGetValue("client_id", out var clientId);
        context.Request.Query.TryGetValue("isTestEnvironment", out var isTestEnvironment);
        
        var authenticationProperties = new AuthenticationProperties
        {
            RedirectUri = $"/google/callback?response_type=code&client_id={clientId}&redirect_uri={redirectUri}&state={state}&subDomain={subDomain}&isTestEnvironment={isTestEnvironment}",
            Parameters =
            {
                { "scope", "openid email profile" },
                { "prompt" , "select_account"}
            }
        };

        return Challenge(authenticationProperties, OpenIddictClientWebIntegrationConstants.Providers.Google);
    }
    
    [AllowAnonymous]
    [HttpGet("/google/callback")]
    public async Task<IActionResult> GoogleCallbackAsync()
    {
        var authenticateResult = await HttpContext.AuthenticateAsync(OpenIddictClientWebIntegrationConstants.Providers.Google);

        if (!authenticateResult.Succeeded || authenticateResult.Principal == null)
        {
            return Unauthorized("Google login failed.");
        }

        // Extract Google user claims
        var googleClaims = authenticateResult.Principal.Claims.ToList();
        var externalId = authenticateResult.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = googleClaims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        var name = googleClaims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
        var subDomain = "wiserdemo";
        
        if (String.IsNullOrEmpty(email))
        {
            return BadRequest("Email is required.");
        }
        
        // Add sub domain to http context, to that the ClientDatabaseConnection can use it to get the correct connection string.
        httpContextAccessor.HttpContext?.Items.Add(HttpContextConstants.SubDomainKey, subDomain);

        // Match or create a local user record
        var loginResult = await usersService.LoginExternalAsync(email);
        
        // Create a ClaimsPrincipal based on the Google-provided claims
        var identity = new ClaimsIdentity("ExternalLogin");
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Subject, loginResult.ModelObject.Id.ToString()));
        identity.AddClaim(new Claim("external_id", externalId));
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Name, name));
        identity.AddClaim(new Claim("auth_source", "Google"));

        var principal = new ClaimsPrincipal(identity);
        
        // Generate an authorization code using OpenIddict
        var properties = new AuthenticationProperties
        {
            RedirectUri = authenticateResult.Properties!.RedirectUri
        };
        return SignIn(principal, properties, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
}
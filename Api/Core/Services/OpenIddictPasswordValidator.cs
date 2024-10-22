using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Models;
using Api.Modules.Tenants.Interfaces;
using Api.Modules.Tenants.Models;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;

namespace Api.Core.Services;

public class OpenIddictPasswordValidator : IOpenIddictServerHandler<OpenIddictServerEvents.HandleTokenRequestContext>
{
    private readonly ILogger<OpenIddictPasswordValidator> logger;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly GclSettings gclSettings;
    private readonly IUsersService usersService;

    public OpenIddictPasswordValidator(ILogger<OpenIddictPasswordValidator> logger,
        IHttpContextAccessor httpContextAccessor, IOptions<GclSettings> gclSettings, IUsersService usersService)
    {
        this.logger = logger;
        this.httpContextAccessor = httpContextAccessor;
        this.usersService = usersService;
        this.gclSettings = gclSettings.Value;
    }

    /// <inheritdoc />
    public async ValueTask HandleAsync(OpenIddictServerEvents.HandleTokenRequestContext context)
    {
        var subDomain = context.Transaction.Request?[HttpContextConstants.SubDomainKey]?.Value?.ToString();
        logger.LogInformation(
            $"User '{context.Request.Username}' is trying to authenticate for sub domain '{subDomain}'.");
        if (String.IsNullOrWhiteSpace(subDomain))
        {
            context.Reject("subdomain_missing", "No sub domain given");
            return;
        }

        // Extra parameters that get added to response
        var parameters = new Dictionary<string, OpenIddictParameter>();
        
        // Add sub domain to http context, to that the ClientDatabaseConnection can use it to get the correct connection string.
        httpContextAccessor.HttpContext?.Items.Add(HttpContextConstants.SubDomainKey, subDomain);
        
        ulong adminAccountId = 0;
        var adminAccountName = "";
        var selectedUser = context.Transaction.Request?[HttpContextConstants.SelectedUserKey]?.Value?.ToString();
        var isTestEnvironment =
            context.Transaction.Request?[HttpContextConstants.IsTestEnvironmentKey]?.Value?.ToString();
        var totpPin = context.Transaction.Request?[HttpContextConstants.TotpPinKey]?.Value?.ToString();
        var totpBackupCode = context.Transaction.Request?[HttpContextConstants.TotpBackupCodeKey]?.Value?.ToString();
        if (String.IsNullOrWhiteSpace(isTestEnvironment))
        {
            isTestEnvironment = "false";
        }

        var isWiserFrontEndLoginEncrypted = context.Transaction.Request?[HttpContextConstants.IsWiserFrontEndLoginKey]
            ?.Value?.ToString();
        var isWiserFrontEndLogin = false;
        if (!String.IsNullOrWhiteSpace(isWiserFrontEndLoginEncrypted))
        {
            isWiserFrontEndLoginEncrypted = WebUtility.HtmlDecode(isWiserFrontEndLoginEncrypted);
            isWiserFrontEndLogin = isWiserFrontEndLoginEncrypted
                .DecryptWithAesWithSalt(gclSettings.DefaultEncryptionKey, true, 10, true)
                .Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        // First try to login as a regular user.
        var loginResult = await usersService.LoginTenantAsync(context.Request.Username, context.Request.Password,
            subDomain: subDomain,
            generateAuthenticationTokenForCookie: true, totpPin: totpPin, totpBackupCode: totpBackupCode);

        // If the regular user login failed, try to login as an admin account.
        var totpSuccessAdmin = false;
        if (loginResult.StatusCode != HttpStatusCode.OK)
        {
            var adminAccountLoginResult =
                await usersService.LoginAdminAccountAsync(context.Request.Username, context.Request.Password,
                    totpPin: totpPin);
            if (adminAccountLoginResult.StatusCode != HttpStatusCode.OK)
            {
                context.Reject("invalid_client", loginResult.ErrorMessage);
                return;
            }

            adminAccountId = adminAccountLoginResult.ModelObject.Id;
            adminAccountName = adminAccountLoginResult.ModelObject.Name;
            if (adminAccountLoginResult.ModelObject.TotpAuthentication.Enabled && String.IsNullOrWhiteSpace(totpPin) &&
                String.IsNullOrWhiteSpace(totpBackupCode))
            {
                context.Reject("totp_code_required", loginResult.ErrorMessage);
                return;
            }

            totpSuccessAdmin = adminAccountLoginResult.ModelObject.TotpAuthentication.Enabled &&
                               !String.IsNullOrWhiteSpace(totpPin);
            if (String.IsNullOrWhiteSpace(selectedUser))
            {
                // Admin account has not selected a user, so return a list of users.
                var usersList = await usersService.GetAsync();
                if (usersList.ModelObject.Count == 1)
                {
                    // If there is only one user, immediately login as that user.
                    selectedUser = usersList.ModelObject.Single().Fields["username"].ToString();
                }
                else
                {
                    context.Reject("select_user");
                    return;
                }
            }

            loginResult = await usersService.LoginTenantAsync(selectedUser, null,
                adminAccountLoginResult.ModelObject.EncryptedId, subDomain, true);

        }
        else
        {
            parameters.Add("adminlogin", new OpenIddictParameter(false));
        }

         // If we still haven't been able to login, return a login error.
         if (loginResult.StatusCode != HttpStatusCode.OK)
         {
             context.Reject("invalid_client", loginResult.ErrorMessage);
             return;
         }
         
         parameters.Add("name", loginResult.ModelObject.Name);
         parameters.Add("role", loginResult.ModelObject.Role);
             
         if (!String.IsNullOrWhiteSpace(loginResult.ModelObject.CookieValue))
         {
             parameters.Add("cookieValue", new OpenIddictParameter(loginResult.ModelObject.CookieValue));
         }
         
         var claims = CreateClaimsList(loginResult.ModelObject, subDomain, adminAccountId, adminAccountName, isTestEnvironment, isWiserFrontEndLogin);
        
         var identity = new ClaimsIdentity(claims, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

         identity.SetAuthorizationId(loginResult.ModelObject.Id.ToString());
         
         var principal = new ClaimsPrincipal(identity);
         principal.SetScopes("profile", "email", "api.read", "api.write");

         principal.SetDestinations((c) => [OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken]);
         
         context.SignIn(principal, parameters);
    }

    private static IEnumerable<Claim> CreateClaimsList(AdminAccountModel adminAccount, string subDomain, string isTestEnvironment, bool isWiserFrontEndLogin)
    {
        return new List<Claim>
        {
            new(ClaimTypes.GivenName, adminAccount.Name),
            new(ClaimTypes.Name, adminAccount.Login),
            new(ClaimTypes.Email, adminAccount.Login),
            new(ClaimTypes.Role, IdentityConstants.AdminAccountRole),
            new(ClaimTypes.GroupSid, subDomain),
            new(HttpContextConstants.IsTestEnvironmentKey, isTestEnvironment),
            new(HttpContextConstants.IsWiserFrontEndLoginKey, isWiserFrontEndLogin.ToString())
        };
    }

    private static IEnumerable<Claim> CreateClaimsList(UserModel user, string subDomain, ulong adminAccountId, string adminAccountName, string isTestEnvironment, bool isWiserFrontEndLogin)
    {
        var claimsIdentity = new List<Claim>
        {
            new(OpenIddictConstants.Claims.GivenName, user.Name),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(OpenIddictConstants.Claims.Subject , user.Id.ToString()),
            new(ClaimTypes.Role, user.Role),
            new(ClaimTypes.GroupSid, subDomain),
            new(ClaimTypes.Sid, adminAccountId.ToString()),
            new(IdentityConstants.AdminAccountName, adminAccountName ?? ""),
            new(HttpContextConstants.IsTestEnvironmentKey, isTestEnvironment),
            new(HttpContextConstants.IsWiserFrontEndLoginKey, isWiserFrontEndLogin.ToString())
        };

        if (!String.IsNullOrWhiteSpace(user.EmailAddress))
        {
            claimsIdentity.Add(new Claim(ClaimTypes.Email, user.EmailAddress));
        }

        if (!String.IsNullOrWhiteSpace(user.CookieValue))
        {
            claimsIdentity.Add(new Claim(IdentityConstants.TokenIdentifierKey, user.CookieValue));
        }

        if (adminAccountId > 0)
        {
            claimsIdentity.Add(new Claim(ClaimTypes.Role, IdentityConstants.AdminAccountRole));
        }

        return claimsIdentity;
    }
}
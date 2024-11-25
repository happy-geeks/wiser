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
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;

namespace Api.Core.Services;

public class OpenIddictTokenRequestHandler : IOpenIddictServerHandler<OpenIddictServerEvents.HandleTokenRequestContext>
{
    private readonly ILogger<OpenIddictTokenRequestHandler> logger;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly GclSettings gclSettings;
    private readonly IUsersService usersService;

    public OpenIddictTokenRequestHandler(ILogger<OpenIddictTokenRequestHandler> logger,
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
        switch (context.Request.GrantType)
        {
            case OpenIddictConstants.GrantTypes.Password:
                await HandlePasswordAsync(context);
                break;
            case OpenIddictConstants.GrantTypes.RefreshToken:
                await HandleRefreshAsync(context);
                break;
        }
    }

    private ValueTask HandleRefreshAsync(OpenIddictServerEvents.HandleTokenRequestContext context)
    {
        // Create a new ClaimsPrincipal for the refreshed token.
        var identity = context?.Principal?.Identity as ClaimsIdentity;

        if (identity is not { IsAuthenticated: true })
        {
            context!.Reject(OpenIddictConstants.Errors.InvalidClient, "no identity given");
            return ValueTask.CompletedTask;
        }
            
        // Sign in with the refreshed principal.
        context.SignIn(new ClaimsPrincipal(identity));

        return ValueTask.CompletedTask;
    }
    
    private async ValueTask HandlePasswordAsync(OpenIddictServerEvents.HandleTokenRequestContext context)
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

        var isWiserFrontEndLoginEncrypted = context.Transaction.Request?[HttpContextConstants.IsWiserFrontEndLoginKey]?.Value?.ToString();
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
                context.Reject(OpenIddictConstants.Errors.InvalidClient, loginResult.ErrorMessage);
                return;
            }

            adminAccountId = adminAccountLoginResult.ModelObject.Id;
            adminAccountName = adminAccountLoginResult.ModelObject.Name;
            if (adminAccountLoginResult.ModelObject.TotpAuthentication.Enabled && String.IsNullOrWhiteSpace(totpPin) &&
                String.IsNullOrWhiteSpace(totpBackupCode))
            {
                AddLoginResultParameters(parameters, adminAccountLoginResult.ModelObject, false);
                
                var adminIdentity = CreateIdentity(loginResult.ModelObject, subDomain, adminAccountId, adminAccountName, isTestEnvironment, isWiserFrontEndLogin);
                var adminListPrincipal = new ClaimsPrincipal(adminIdentity);

                Signin(context, adminListPrincipal, parameters);
                return;
            }

            totpSuccessAdmin = adminAccountLoginResult.ModelObject.TotpAuthentication.Enabled &&
                               !String.IsNullOrWhiteSpace(totpPin);
            if (String.IsNullOrWhiteSpace(selectedUser))
            {
                var usersList = await usersService.GetAsync();
                if (usersList.ModelObject.Count == 1)
                {
                    // If there is only one user, immediately login as that user.
                    selectedUser = usersList.ModelObject.Single().Fields["username"].ToString();
                }
                else
                {
                    // Create a token with only the api.users_list scope which can be used to get the user list
                    var adminIdentity = CreateIdentity(loginResult.ModelObject, subDomain, adminAccountId, adminAccountName, isTestEnvironment, isWiserFrontEndLogin);
                    var adminListPrincipal = new ClaimsPrincipal(adminIdentity);
                    parameters.Add("showUsersList", true);
                    adminListPrincipal.SetScopes("api.users_list");

                    Signin(context, adminListPrincipal, parameters);
                    return;
                }
            }
            
            loginResult = await usersService.LoginTenantAsync(selectedUser, null,
                adminAccountLoginResult.ModelObject.EncryptedId, subDomain, true);
        }

         // If we still haven't been able to login, return a login error.
         if (loginResult.StatusCode != HttpStatusCode.OK)
         {
             context.Reject(OpenIddictConstants.Errors.InvalidClient, loginResult.ErrorMessage);
             return;
         }
         
         var totpSuccess = totpSuccessAdmin || loginResult.ModelObject.TotpAuthentication.Enabled && (!String.IsNullOrWhiteSpace(totpPin) || !String.IsNullOrWhiteSpace(totpBackupCode));
         AddLoginResultParameters(parameters, loginResult.ModelObject, totpSuccess, adminAccountId != 0);
         
         var identity = CreateIdentity(loginResult.ModelObject, subDomain, adminAccountId, adminAccountName, isTestEnvironment, isWiserFrontEndLogin);
         
         var principal = new ClaimsPrincipal(identity);

         // The access_token will be without scopes and thus unusable for accessing any of the endpoints
         // if the third party authentication still needs to be completed.
         if (!loginResult.ModelObject.TotpAuthentication.Enabled || totpSuccess)
         {
             principal.SetScopes(
                 OpenIddictConstants.Scopes.OpenId,
                 OpenIddictConstants.Scopes.Email,
                 OpenIddictConstants.Scopes.Profile,
                 OpenIddictConstants.Scopes.OfflineAccess,  // Required for refresh tokens
                 "api.read", 
                 "api.write"
             );
         }

         Signin(context, principal, parameters);
    }
    
    /// <summary>
    /// Fill the list of parameters that will be passed to the frontend beside the access token
    /// </summary>
    private void AddLoginResultParameters(Dictionary<string, OpenIddictParameter> parameters, AdminAccountModel user, bool totpSuccess)
    {
        parameters.Add("name", user.Name);
        parameters.Add("SkipRefreshTokenGeneration", !totpSuccess);
        parameters.Add("totpEnabled", user.TotpAuthentication.Enabled);
        parameters.Add("totpQrImageUrl", user.TotpAuthentication.QrImageUrl);
        parameters.Add("totpFirstTime", user.TotpAuthentication.RequiresSetup);
        parameters.Add("totpSuccess", totpSuccess);
        parameters.Add("adminlogin", true);
    }

    /// <summary>
    /// Fill the list of parameters that will be passed to the frontend beside the access token
    /// </summary>
    private void AddLoginResultParameters(Dictionary<string, OpenIddictParameter> parameters, UserModel user, bool totpSuccess, bool isAdminLogin)
    {
        parameters.Add("name", user.Name);
        parameters.Add("role", user.Role);
        parameters.Add("totpEnabled", user.TotpAuthentication.Enabled);
        parameters.Add("totpQrImageUrl", user.TotpAuthentication.QrImageUrl);
        parameters.Add("totpFirstTime", !isAdminLogin&& user.TotpAuthentication.RequiresSetup);
        parameters.Add("SkipRefreshTokenGeneration", !totpSuccess);
        parameters.Add("totpSuccess", totpSuccess);
        parameters.Add("adminlogin", isAdminLogin);

        if (user.RequirePasswordChange.HasValue)
        {
            parameters.Add("requirePasswordChange", user.RequirePasswordChange.Value);
        }
        
        if (!String.IsNullOrWhiteSpace(user.CookieValue))
        {
            parameters.Add("cookieValue", user.CookieValue);
        }
    }

    private void Signin(OpenIddictServerEvents.HandleTokenRequestContext context, ClaimsPrincipal principal, Dictionary<string, OpenIddictParameter> parameters)
    {
        principal.SetDestinations((c) => [OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken]);
        context.SignIn(principal, parameters);
    }

    /// <summary>
    /// Create an ClaimsIdentity with a defined claims collection
    /// </summary>
    /// <returns></returns>
    private ClaimsIdentity CreateIdentity(UserModel user, string subDomain, ulong adminAccountId, string adminAccountName, string isTestEnvironment, bool isWiserFrontEndLogin)
    {
        var claims = CreateClaimsList(user, subDomain, adminAccountId, adminAccountName, isTestEnvironment, isWiserFrontEndLogin);
        
        var identity = new ClaimsIdentity(claims, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        return identity;
    }

    /// <summary>
    /// Create the list of claims to be added to the token
    /// </summary>
    /// <returns><see cref="IEnumerable{Claim}"/> of <see cref="Claim"/></returns>
    private static IEnumerable<Claim> CreateClaimsList(UserModel user, string subDomain, ulong adminAccountId, string adminAccountName, string isTestEnvironment, bool isWiserFrontEndLogin)
    {
        var claimsIdentity = new List<Claim>
        {
            new(ClaimTypes.GroupSid, subDomain),
            new(ClaimTypes.Sid, adminAccountId.ToString()),
            new(IdentityConstants.AdminAccountName, adminAccountName ?? ""),
            new(HttpContextConstants.IsTestEnvironmentKey, isTestEnvironment),
            new(HttpContextConstants.IsWiserFrontEndLoginKey, isWiserFrontEndLogin.ToString()),
            new("idp", "wiser")
        };

        if (user is not null)
        {
            claimsIdentity.Add(new Claim(OpenIddictConstants.Claims.GivenName, user.Name));
            claimsIdentity.Add(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
            claimsIdentity.Add(new Claim(ClaimTypes.Name, user.Username));
            claimsIdentity.Add(new Claim(OpenIddictConstants.Claims.Subject , user.Id.ToString()));
            claimsIdentity.Add(new Claim(ClaimTypes.Role, user.Role));
            
            if (!String.IsNullOrWhiteSpace(user.EmailAddress))
            {
                claimsIdentity.Add(new Claim(ClaimTypes.Email, user.EmailAddress));
            }

            if (!String.IsNullOrWhiteSpace(user.CookieValue))
            {
                claimsIdentity.Add(new Claim(IdentityConstants.TokenIdentifierKey, user.CookieValue));
            }
        }
        else
        {
            claimsIdentity.Add(new Claim(OpenIddictConstants.Claims.Subject , "0"));
        }

        if (adminAccountId > 0)
        {
            claimsIdentity.Add(new Claim(ClaimTypes.Role, IdentityConstants.AdminAccountRole));
        }

        return claimsIdentity;
    }
}
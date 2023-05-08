using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Models;
using Api.Modules.Customers.Interfaces;
using Api.Modules.Customers.Models;
using GeeksCoreLibrary.Core.Extensions;
using IdentityModel;
using IdentityServer4.Models;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Api.Core.Services
{
    /// <summary>
    /// Service for validating OAUTH2 grants via the database of the customer.
    /// </summary>
    public class WiserGrantValidator : IResourceOwnerPasswordValidator
    {
        private readonly ILogger<WiserGrantValidator> logger;
        private readonly IUsersService usersService;
        private readonly IHttpContextAccessor httpContextAccessor;

        /// <summary>
        /// Creates a new instance of <see cref="WiserGrantValidator"/>.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="usersService"></param>
        /// <param name="httpContextAccessor"></param>
        public WiserGrantValidator(ILogger<WiserGrantValidator> logger, IUsersService usersService, IHttpContextAccessor httpContextAccessor)
        {
            this.logger = logger;
            this.usersService = usersService;
            this.httpContextAccessor = httpContextAccessor;
        }

        /// <inheritdoc />
        public async Task ValidateAsync(ResourceOwnerPasswordValidationContext context)
        {
            var subDomain = context.Request.Raw[HttpContextConstants.SubDomainKey];
            logger.LogInformation($"User '{context.UserName}' is trying to authenticate for sub domain '{subDomain}'.");
            if (String.IsNullOrWhiteSpace(subDomain))
            {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidClient, "No sub domain given");
                return;
            }

            // Add sub domain to http context, to that the ClientDatabaseConnection can use it to get the correct connection string.
            httpContextAccessor.HttpContext?.Items.Add(HttpContextConstants.SubDomainKey, subDomain);

            Dictionary<string, object> customResponse;
            ulong adminAccountId = 0;
            var adminAccountName = "";
            var selectedUser = context.Request.Raw[HttpContextConstants.SelectedUserKey];
            var isTestEnvironment = context.Request.Raw[HttpContextConstants.IsTestEnvironmentKey];
            var totpPin = context.Request.Raw[HttpContextConstants.TotpPinKey];
            var totpBackupCode = context.Request.Raw[HttpContextConstants.TotpBackupCodeKey];
            if (String.IsNullOrWhiteSpace(isTestEnvironment))
            {
                isTestEnvironment = "false";
            }

            // First try to login as a regular user.
            var loginResult = await usersService.LoginCustomerAsync(context.UserName, context.Password, subDomain: subDomain, generateAuthenticationTokenForCookie: true, totpPin: totpPin, totpBackupCode: totpBackupCode);

            // If the regular user login failed, try to login as an admin account.
            var totpSuccessAdmin = false;
            if (loginResult.StatusCode != HttpStatusCode.OK)
            {
                var adminAccountLoginResult = await usersService.LoginAdminAccountAsync(context.UserName, context.Password, totpPin: totpPin);
                if (adminAccountLoginResult.StatusCode != HttpStatusCode.OK)
                {
                    context.Result = new GrantValidationResult(TokenRequestErrors.InvalidClient, loginResult.ErrorMessage);
                    return;
                }

                adminAccountId = adminAccountLoginResult.ModelObject.Id;
                adminAccountName = adminAccountLoginResult.ModelObject.Name;
                if (adminAccountLoginResult.ModelObject.TotpAuthentication.Enabled && String.IsNullOrWhiteSpace(totpPin) && String.IsNullOrWhiteSpace(totpBackupCode))
                {
                    // 2FA is enabled for admin account, but admin hasn't entered a PIN yet, so don't load the users list yet.
                    customResponse = new Dictionary<string, object>
                    {
                        { "adminLogin", true },
                        { "name", adminAccountLoginResult.ModelObject.Name },
                        { "SkipRefreshTokenGeneration", true },
                        { "totpEnabled", adminAccountLoginResult.ModelObject.TotpAuthentication.Enabled },
                        { "totpQrImageUrl", adminAccountLoginResult.ModelObject.TotpAuthentication.QrImageUrl },
                        { "totpSuccess", false },
                        // Set access token and refresh token to null to make sure people can't somehow login anyway, when they haven't entered their 2FA PIN yet.
                        { "access_token", null },
                        { "refresh_token", null }
                    };
                    context.Result = new GrantValidationResult(adminAccountLoginResult.ModelObject.Id.ToString(), OidcConstants.AuthenticationMethods.Password, CreateClaimsList(adminAccountLoginResult.ModelObject, subDomain, isTestEnvironment), customResponse: customResponse);
                    return;
                }

                totpSuccessAdmin = adminAccountLoginResult.ModelObject.TotpAuthentication.Enabled && !String.IsNullOrWhiteSpace(totpPin);
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
                        // Everything is ok, create identity.
                        customResponse = new Dictionary<string, object>
                        {
                            { "adminLogin", true },
                            { "name", adminAccountLoginResult.ModelObject.Name },
                            { "users", JsonConvert.SerializeObject(usersList.ModelObject) },
                            { "SkipRefreshTokenGeneration", true },
                            { "totpEnabled", adminAccountLoginResult.ModelObject.TotpAuthentication.Enabled },
                            { "totpSuccess", totpSuccessAdmin },
                            // Set access token and refresh token to null to make sure people can't somehow login anyway, when they haven't selected a user yet.
                            { "access_token", null },
                            { "refresh_token", null }
                        };

                        context.Result = new GrantValidationResult(adminAccountLoginResult.ModelObject.Id.ToString(), OidcConstants.AuthenticationMethods.Password, CreateClaimsList(adminAccountLoginResult.ModelObject, subDomain, isTestEnvironment), customResponse: customResponse);
                        return;
                    }
                }

                loginResult = await usersService.LoginCustomerAsync(selectedUser, null, adminAccountLoginResult.ModelObject.EncryptedId, subDomain, true);
            }

            // If we still haven't been able to login, return a login error.
            if (loginResult.StatusCode != HttpStatusCode.OK)
            {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidClient, loginResult.ErrorMessage);
                return;
            }

            // Everything is ok, create identity.
            var totpSuccess = totpSuccessAdmin || loginResult.ModelObject.TotpAuthentication.Enabled && (!String.IsNullOrWhiteSpace(totpPin) || !String.IsNullOrWhiteSpace(totpBackupCode));
            customResponse = new Dictionary<string, object>
            {
                { "adminLogin", adminAccountId > 0 },
                { "name", loginResult.ModelObject.Name },
                { "role", loginResult.ModelObject.Role },
                { "lastLoginIpAddress", loginResult.ModelObject.LastLoginIpAddress ?? "" },
                { "lastLoginDate", (loginResult.ModelObject.LastLoginDate ?? DateTime.Now).ToString("dd-MM-yyyy HH:mm:ss") },
                { "oldStyleUserId", loginResult.ModelObject.Id.ToString().EncryptWithAesWithSalt() },
                { "cookieValue", loginResult.ModelObject.CookieValue },
                { "encryptedLoginLogId", loginResult.ModelObject.EncryptedLoginLogId },
                { "totpEnabled", loginResult.ModelObject.TotpAuthentication.Enabled },
                { "totpQrImageUrl", loginResult.ModelObject.TotpAuthentication.QrImageUrl },
                { "totpSuccess", totpSuccess },
                { "totpFirstTime", adminAccountId == 0 && loginResult.ModelObject.TotpAuthentication.RequiresSetup }
            };

            // Set access token and refresh token to null to make sure people can't somehow login anyway, when they haven't entered their 2FA PIN yet.
            if (loginResult.ModelObject.TotpAuthentication.Enabled && !totpSuccess)
            {
                customResponse.Add("access_token", null);
                customResponse.Add("refresh_token", null);
            }

            if (loginResult.ModelObject.RequirePasswordChange.HasValue)
            {
                customResponse.Add("requirePasswordChange", loginResult.ModelObject.RequirePasswordChange.Value);
            }

            if (adminAccountId > 0)
            {
                customResponse.Add("adminAccountId", adminAccountId);
                customResponse.Add("adminAccountName", adminAccountName);
            }

            context.Result = new GrantValidationResult(loginResult.ModelObject.Id.ToString(), OidcConstants.AuthenticationMethods.Password, CreateClaimsList(loginResult.ModelObject, subDomain, adminAccountId, adminAccountName, isTestEnvironment), customResponse: customResponse);
        }

        private static IEnumerable<Claim> CreateClaimsList(AdminAccountModel adminAccount, string subDomain, string isTestEnvironment)
        {
            return new List<Claim>
            {
                new(ClaimTypes.GivenName, adminAccount.Name),
                new(ClaimTypes.Name, adminAccount.Login),
                new(ClaimTypes.Email, adminAccount.Login),
                new(ClaimTypes.Role, IdentityConstants.AdminAccountRole),
                new(ClaimTypes.GroupSid, subDomain),
                new(HttpContextConstants.IsTestEnvironmentKey, isTestEnvironment)
            };
        }

        private static IEnumerable<Claim> CreateClaimsList(UserModel user, string subDomain, ulong adminAccountId, string adminAccountName, string isTestEnvironment)
        {
            var claimsIdentity = new List<Claim>
            {
                new(ClaimTypes.GivenName, user.Name),
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Role, user.Role),
                new(ClaimTypes.GroupSid, subDomain),
                new(ClaimTypes.Sid, adminAccountId.ToString()),
                new(IdentityConstants.AdminAccountName, adminAccountName ?? ""),
                new(HttpContextConstants.IsTestEnvironmentKey, isTestEnvironment)
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
}
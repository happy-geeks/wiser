﻿using System;
using System.Collections.Generic;
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
            var adminAccountId = 0;
            string adminAccountName = null;
            var selectedUser = context.Request.Raw[HttpContextConstants.SelectedUserKey];
            var isTestEnvironment = context.Request.Raw[HttpContextConstants.IsTestEnvironmentKey];
            if (String.IsNullOrWhiteSpace(isTestEnvironment))
            {
                isTestEnvironment = "false";
            }
            
            // First try to login as a regular user.
            var loginResult = await usersService.LoginCustomerAsync(context.UserName, context.Password, subDomain: subDomain, generateAuthenticationTokenForCookie: true);

            // If the regular user login failed, try to login as an admin account.
            if (loginResult.StatusCode != HttpStatusCode.OK)
            {
                var adminAccountLoginResult = await usersService.LoginAdminAccountAsync(context.UserName, context.Password);
                if (adminAccountLoginResult.StatusCode != HttpStatusCode.OK)
                {
                    context.Result = new GrantValidationResult(TokenRequestErrors.InvalidClient, adminAccountLoginResult.ErrorMessage);
                    return;
                }

                adminAccountId = adminAccountLoginResult.ModelObject.Id;
                adminAccountName = adminAccountLoginResult.ModelObject.Name;
                if (String.IsNullOrWhiteSpace(selectedUser))
                {
                    // Admin account has not selected a user, so return a list of users.
                    var usersList = await usersService.GetAsync();

                    // Everything is ok, create identity.
                    customResponse = new Dictionary<string, object>
                    {
                        { "adminLogin", true },
                        { "name", adminAccountLoginResult.ModelObject.Name },
                        { "users", JsonConvert.SerializeObject(usersList.ModelObject) },
                        { "SkipRefreshTokenGeneration", true }
                    };
                    context.Result = new GrantValidationResult(adminAccountLoginResult.ModelObject.Id.ToString(), OidcConstants.AuthenticationMethods.Password, CreateClaimsList(adminAccountLoginResult.ModelObject, subDomain, isTestEnvironment), customResponse: customResponse);
                    return;
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
            customResponse = new Dictionary<string, object>
            {
                {"name", loginResult.ModelObject.Name},
                {"role", loginResult.ModelObject.Role},
                {"lastLoginIpAddress", loginResult.ModelObject.LastLoginIpAddress ?? ""},
                {"lastLoginDate", (loginResult.ModelObject.LastLoginDate ?? DateTime.Now).ToString("dd-MM-yyyy HH:mm:ss")},
                {"oldStyleUserId", loginResult.ModelObject.Id.ToString().EncryptWithAesWithSalt()},
                {"cookieValue", loginResult.ModelObject.CookieValue},
                {"requirePasswordChange", loginResult.ModelObject.RequirePasswordChange }
            };
            
            if (adminAccountId > 0)
            {
                customResponse.Add("adminAccountId", adminAccountId.ToString());
                customResponse.Add("adminAccountName", adminAccountName);
            }
            
            context.Result = new GrantValidationResult(loginResult.ModelObject.Id.ToString(), OidcConstants.AuthenticationMethods.Password, CreateClaimsList(loginResult.ModelObject, subDomain, adminAccountId, isTestEnvironment), customResponse: customResponse);
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

        private static IEnumerable<Claim> CreateClaimsList(UserModel user, string subDomain, int adminAccountId, string isTestEnvironment)
        {
            var claimsIdentity = new List<Claim>
            {
                new(ClaimTypes.GivenName, user.Name),
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Role, user.Role),
                new(ClaimTypes.GroupSid, subDomain),
                new(ClaimTypes.Sid, adminAccountId.ToString()),
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

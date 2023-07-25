using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Models;
using Api.Modules.Tenants.Interfaces;
using Api.Modules.Tenants.Models;
using GeeksCoreLibrary.Core.Models;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.Events;
using IdentityServer4.Services;
using IdentityServer4.Test;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace Api.Modules.Tenants.Controllers
{
    /// <summary>
    /// A controller for getting data about users that can authenticate with Wiser.
    /// </summary>
    [Route("api/v3/[controller]")]
    [ApiController]
    [Authorize]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    public class UsersController : ControllerBase
    {
        private readonly IUsersService usersService;
        private readonly IIdentityServerInteractionService interaction;
        private readonly IEventService events;

        /// <summary>
        /// Creates a new instance of UsersController.
        /// </summary>
        public UsersController(IUsersService usersService, IIdentityServerInteractionService interaction, IEventService events)
        {
            this.usersService = usersService;
            this.interaction = interaction;
            this.events = events;
        }

        /// <summary>
        /// Method for getting a list of all users for the authenticated tenant.
        /// </summary>
        /// <returns>A <see cref="List{T}"/> of <see cref="WiserItemModel"/>, but only with names and IDs.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(List<WiserItemModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Get(bool includeAdminUsers = false)
        {
            return (await usersService.GetAsync(includeAdminUsers)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Gets the data of the authenticated user.
        /// </summary>
        /// <returns>A UserModel with the data of the authenticated user.</returns>
        [HttpGet]
        [Route("self")]
        [ProducesResponseType(typeof(UserModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUserData()
        {
            return (await usersService.GetUserDataAsync((ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Sends a new password to a user.
        /// </summary>
        /// <param name="resetPasswordRequestModel">The information for the account to reset the password.</param>
        /// <returns>Always returns true, unless an exception occurred.</returns>
        [HttpPut]
        [Route("reset-password")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequestModel resetPasswordRequestModel)
        {
            HttpContext.Items[HttpContextConstants.SubDomainKey] = resetPasswordRequestModel.SubDomain;
            return (await usersService.ResetPasswordAsync(resetPasswordRequestModel, (ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Changes the password of a user.
        /// </summary>
        /// <param name="passwords">The old and new passwords of the user.</param>
        [HttpPut]
        [Route("password")]
        [ProducesResponseType(typeof(UserModel), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdatePassword(ChangePasswordModel passwords)
        {
            return (await usersService.ChangePasswordAsync((ClaimsIdentity)User.Identity, passwords)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Gets settings for the authenticated user for a specific group of settings.
        /// </summary>
        /// <param name="groupName">The group that the settings belong to.</param>
        /// <param name="key">The unique key for the settings.</param>
        /// <returns>The saved JSON object serialized as a string, or null if no setting for the given group and key were found.</returns>
        [HttpGet]
        [Route("settings/{groupName}/{key}")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSettingsAsync(string groupName, string key)
        {
            return (await usersService.GetSettingsAsync((ClaimsIdentity) User.Identity, groupName, key)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Saves settings for the authenticated user that belong to a specific group.
        /// </summary>
        /// <param name="groupName">The group that the settings belong to.</param>
        /// <param name="key">The unique key for the grid settings. This should be unique for each grid in Wiser, so that no 2 grids use the same settings.</param>
        /// <param name="settings">A JSON object with the settings to save.</param>
        /// <returns>A boolean whether the save action was successful.</returns>
        [HttpPost]
        [Route("settings/{groupName}/{key}")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<IActionResult> SaveSettingsAsync(string groupName, string key, JToken settings)
        {
            return (await usersService.SaveSettingsAsync((ClaimsIdentity) User.Identity, groupName, key, settings)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Gets settings for a grid for the authenticated user, so that users can keep their state of all grids in Wiser.
        /// </summary>
        /// <param name="key">The unique key for the grid settings. This should be unique for each grid in Wiser, so that no 2 grids use the same settings.</param>
        [HttpGet]
        [Route("grid-settings/{key}")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetGridSettingsAsync(string key)
        {
            return (await usersService.GetGridSettingsAsync((ClaimsIdentity)User.Identity, key)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Saves settings for a grid for the authenticated user, so that the next time the grid is loaded, the user keeps those settings.
        /// </summary>
        /// <param name="key">The unique key for the grid settings. This should be unique for each grid in Wiser, so that no 2 grids use the same settings.</param>
        /// <param name="settings">A JSON object with the settings to save.</param>
        [HttpPost]
        [Route("grid-settings/{key}")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<IActionResult> SaveGridSettingsAsync(string key, JToken settings)
        {
            return (await usersService.SaveGridSettingsAsync((ClaimsIdentity)User.Identity, key, settings)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Gets the pinned modules for the authenticated user, so that users can keep their state of the pinned modules in Wiser.
        /// </summary>
        [HttpGet]
        [Route("pinned-modules")]
        [ProducesResponseType(typeof(List<int>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPinnedModulesAsync()
        {
            return (await usersService.GetPinnedModulesAsync((ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Save the list of pinned modules to the user details, so that next time the user will see the same pinned modules.
        /// </summary>
        /// <param name="moduleIds">The list of module IDs that the user has pinned.</param>
        [HttpPost]
        [Route("pinned-modules")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> SavePinnedModulesAsync(List<int> moduleIds)
        {
            return (await usersService.SavePinnedModulesAsync((ClaimsIdentity)User.Identity, moduleIds)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Save the list of modules that should be automatically started when the user logs in, to the user details.
        /// </summary>
        /// <param name="moduleIds">The list of module IDs that the user has set as auto load.</param>
        [HttpPost]
        [Route("auto-load-modules")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> SaveAutoLoadModulesAsync(List<int> moduleIds)
        {
            return (await usersService.SaveAutoLoadModulesAsync((ClaimsIdentity)User.Identity, moduleIds)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Updates the time the current user has been active in Wiser.
        /// </summary>
        /// <param name="encryptedLoginLogId">The encrypted ID of the log table.</param>
        [HttpPut]
        [Route("update-active-time")]
        [ProducesResponseType(typeof(TimeSpan), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateUserActiveTimeAsync([FromQuery]string encryptedLoginLogId)
        {
            return (await usersService.UpdateUserTimeActiveAsync((ClaimsIdentity)User.Identity, encryptedLoginLogId)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Updates the time the current user has been active in Wiser.
        /// </summary>
        /// <param name="encryptedLoginLogId">The encrypted ID of the log table.</param>
        [HttpPut]
        [Route("reset-time-active-changed")]
        [ProducesResponseType(typeof(TimeSpan), StatusCodes.Status200OK)]
        public async Task<IActionResult> ResetTimeActiveChangedAsync([FromQuery]string encryptedLoginLogId)
        {
            return (await usersService.UpdateUserTimeActiveAsync((ClaimsIdentity)User.Identity, encryptedLoginLogId)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Gets all available roles for users.
        /// </summary>
        /// <param name="includePermissions">Optional: Whether to include all permissions that each role has. Default is <see langword="false"/>.</param>
        /// <returns>A list of <see cref="RoleModel"/> with all available roles that users can have.</returns>
        [HttpGet]
        [Route("roles")]
        [ProducesResponseType(typeof(List<RoleModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRolesAsync(bool includePermissions = false)
        {
            return (await usersService.GetRolesAsync(includePermissions)).GetHttpResponseMessage();
        }

        /// <summary>
        /// (Re)generate backup codes for TOTP (2FA) authentication.
        /// This will delete any remaining backup codes from the user and generate new ones.
        /// They will be hashed before they're saved in the database and can therefor only be shown to the user once!
        /// </summary>
        /// <returns>A list with the new backup codes.</returns>
        [HttpPost]
        [Route("totp-backup-codes")]
        [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GenerateTotpBackupCodesAsync()
        {
            return (await usersService.GenerateTotpBackupCodesAsync((ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Will attempt to retrieve the saved layout data for the authenticated user.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("dashboard-settings")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDashboardSettingsAsync()
        {
            return (await usersService.GetDashboardSettingsAsync((ClaimsIdentity) User.Identity)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Will attempt to save the given layout data to the authenticated user.
        /// </summary>
        /// <param name="layoutData">A JSON object containing the layout data to save.</param>
        /// <returns></returns>
        [HttpPost]
        [Route("dashboard-settings")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<IActionResult> SaveDashboardSettingsAsync([FromBody] JToken layoutData)
        {
            return (await usersService.SaveDashboardSettingsAsync((ClaimsIdentity) User.Identity, layoutData)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Endpoint for external login providers such as Google.
        /// This uses IdentityServer4 to handle the authentication.
        /// </summary>
        /// <param name="provider"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("external-login")]
        [AllowAnonymous]
        public IActionResult ExternalLogin(string provider)
        {
            var properties = new AuthenticationProperties
            {
                //RedirectUri = "https://localhost:44377", // The URL where the user will be redirected after authentication
                RedirectUri = Url.Action(nameof(Callback)),
                AllowRefresh = true,
                IsPersistent = true
            };

            return Challenge(properties, provider);
        }
/*
        private (TestUser user, string provider, string providerUserId, IEnumerable<Claim> claims) FindUserFromExternalProvider(AuthenticateResult result)
        {
            var externalUser = result.Principal;

            // try to determine the unique id of the external user (issued by the provider)
            // the most common claim type for that are the sub claim and the NameIdentifier
            // depending on the external provider, some other claim type might be used
            var userIdClaim = externalUser.FindFirst(JwtClaimTypes.Subject) ??
                              externalUser.FindFirst(ClaimTypes.NameIdentifier) ??
                              throw new Exception("Unknown userid");

            // remove the user id claim so we don't include it as an extra claim if/when we provision the user
            var claims = externalUser.Claims.ToList();
            claims.Remove(userIdClaim);

            var provider = result.Properties.Items[".AuthScheme"];
            var providerUserId = userIdClaim.Value;

            // find external user
            //var user = _users.FindByExternalProvider(provider, providerUserId);
            var user = new TestUser()
            {
                SubjectId = "1",
                Username = "Test",
                Password = "Test",
                IsActive = true,
                Claims = new List<Claim>()
                {
                    new(ClaimTypes.GivenName, "Test"),
                    new(ClaimTypes.Name, "Test"),
                    new(ClaimTypes.Role, "Test"),
                    new(ClaimTypes.GroupSid, "main"),
                    new(ClaimTypes.Sid, "Test"),
                    new(IdentityConstants.AdminAccountName, "Test"),
                    new(HttpContextConstants.IsTestEnvironmentKey, "true")
                }
            };

            return (user, provider, providerUserId, claims);
        }

        // if the external login is OIDC-based, there are certain things we need to preserve to make logout work
        // this will be different for WS-Fed, SAML2p or other protocols
        private void ProcessLoginCallback(AuthenticateResult externalResult, List<Claim> localClaims, AuthenticationProperties localSignInProps)
        {
            // if the external system sent a session id claim, copy it over
            // so we can use it for single sign-out
            var sid = externalResult.Principal.Claims.FirstOrDefault(x => x.Type == JwtClaimTypes.SessionId);
            if (sid != null)
            {
                localClaims.Add(new Claim(JwtClaimTypes.SessionId, sid.Value));
            }

            // if the external provider issued an id_token, we'll keep it for signout
            var idToken = externalResult.Properties.GetTokenValue("id_token");
            if (idToken != null)
            {
                localSignInProps.StoreTokens(new[] { new AuthenticationToken { Name = "id_token", Value = idToken } });
            }
        }

        private TestUser AutoProvisionUser(string provider, string providerUserId, IEnumerable<Claim> claims)
        {
            //var user = _users.AutoProvisionUser(provider, providerUserId, claims.ToList());
            var user = new TestUser()
            {
                SubjectId = "1",
                Username = "Test",
                Password = "Test",
                IsActive = true,
                Claims = new List<Claim>()
                {
                    new(ClaimTypes.GivenName, "Test"),
                    new(ClaimTypes.Name, "Test"),
                    new(ClaimTypes.Role, "Test"),
                    new(ClaimTypes.GroupSid, "main"),
                    new(ClaimTypes.Sid, "Test"),
                    new(IdentityConstants.AdminAccountName, "Test"),
                    new(HttpContextConstants.IsTestEnvironmentKey, "true")
                }
            };
            return user;
        }
*/
        [HttpGet]
        [Route("external-login-callback")]
        [AllowAnonymous]
        public async Task<IActionResult> Callback()
        {
            // read external identity from the temporary cookie
            var result = await HttpContext.AuthenticateAsync(IdentityServerConstants.ExternalCookieAuthenticationScheme);
            if (result?.Succeeded != true)
            {
                throw new Exception("External authentication error");
            }

            var externalUser = result.Principal;

            // lookup our user and external provider info
            // try to determine the unique id of the external user (issued by the provider)
            // the most common claim type for that are the sub claim and the NameIdentifier
            // depending on the external provider, some other claim type might be used
            var userIdClaim = externalUser.FindFirst(JwtClaimTypes.Subject) ??
                              externalUser.FindFirst(ClaimTypes.NameIdentifier) ??
                              throw new Exception("Unknown userid");

            var provider = result.Properties.Items[".AuthScheme"];
            var providerUserId = userIdClaim.Value;

            // find external user
            /*var user = _users.FindByExternalProvider(provider, providerUserId);
            if (user == null)
            {
                // this might be where you might initiate a custom workflow for user registration
                // in this sample we don't show how that would be done, as our sample implementation
                // simply auto-provisions new external user
                //
                // remove the user id claim so we don't include it as an extra claim if/when we provision the user
                var claims = externalUser.Claims.ToList();
                claims.Remove(userIdClaim);
                user = _users.AutoProvisionUser(provider, providerUserId, claims.ToList());
            }*/
            var user = new TestUser()
            {
                SubjectId = "1",
                Username = "Test",
                Password = "Test",
                IsActive = true,
                Claims = new List<Claim>()
                {
                    new(ClaimTypes.GivenName, "Test"),
                    new(ClaimTypes.Name, "Test"),
                    new(ClaimTypes.Role, "Test"),
                    new(ClaimTypes.GroupSid, "main"),
                    new(ClaimTypes.Sid, "Test"),
                    new(IdentityConstants.AdminAccountName, "Test"),
                    new(HttpContextConstants.IsTestEnvironmentKey, "true"),
                    new(JwtClaimTypes.Subject, "1")
                }
            };

            // this allows us to collect any additional claims or properties
            // for the specific protocols used and store them in the local auth cookie.
            // this is typically used to store data needed for signout from those protocols.
            var additionalLocalClaims = new List<Claim>
            {
                new(JwtClaimTypes.Subject, providerUserId)
            };
            var localSignInProps = new AuthenticationProperties();
            CaptureExternalLoginContext(result, additionalLocalClaims, localSignInProps);

            // issue authentication cookie for user
            var isuser = new IdentityServerUser(user.SubjectId)
            {
                DisplayName = user.Username,
                IdentityProvider = provider,
                AdditionalClaims = additionalLocalClaims
            };

            await HttpContext.SignInAsync(isuser, localSignInProps);

            // delete temporary cookie used during external authentication
            await HttpContext.SignOutAsync(IdentityServerConstants.ExternalCookieAuthenticationScheme);

            // retrieve return URL
            if (!result.Properties.Items.TryGetValue("returnUrl", out var returnUrl))
            {
                returnUrl = "https://localhost:44377/";
            }

            // check if external login is in the context of an OIDC request
            var context = await interaction.GetAuthorizationContextAsync(returnUrl);

            await events.RaiseAsync(new UserLoginSuccessEvent(provider, providerUserId, user.SubjectId, user.Username, true, context?.Client.ClientId));

            /*if (context != null)
            {
                if (context.IsNativeClient())
                {
                    // The client is native, so this change in how to
                    // return the response is for better UX for the end user.
                    return this.LoadingPage(returnUrl);
                }
            }*/

            return Redirect(returnUrl);

            // read external identity from the temporary cookie
            /*var result = await HttpContext.AuthenticateAsync(IdentityServerConstants.ExternalCookieAuthenticationScheme);
            if (result?.Succeeded != true)
            {
                throw new Exception("External authentication error");
            }


            // lookup our user and external provider info
            var (user, provider, providerUserId, claims) = FindUserFromExternalProvider(result);
            if (user == null)
            {
                // this might be where you might initiate a custom workflow for user registration
                // in this sample we don't show how that would be done, as our sample implementation
                // simply auto-provisions new external user
                user = AutoProvisionUser(provider, providerUserId, claims);
            }

            // this allows us to collect any additional claims or properties
            // for the specific protocols used and store them in the local auth cookie.
            // this is typically used to store data needed for signout from those protocols.
            var additionalLocalClaims = new List<Claim>();
            var localSignInProps = new AuthenticationProperties();
            ProcessLoginCallback(result, additionalLocalClaims, localSignInProps);

            // issue authentication cookie for user
            var isuser = new IdentityServerUser(user.SubjectId)
            {
                DisplayName = user.Username,
                IdentityProvider = provider,
                AdditionalClaims = additionalLocalClaims
            };

            //await HttpContext.SignInAsync(isuser, localSignInProps);

            // delete temporary cookie used during external authentication
            //await HttpContext.SignOutAsync(IdentityServerConstants.ExternalCookieAuthenticationScheme);

            // retrieve return URL
            var returnUrl = result.Properties.Items["returnUrl"] ?? "~/";

            // check if external login is in the context of an OIDC request
            var context = await interaction.GetAuthorizationContextAsync(returnUrl);
            await events.RaiseAsync(new UserLoginSuccessEvent(provider, providerUserId, user.SubjectId, user.Username, true, context?.Client.ClientId));


            return Redirect(returnUrl);*/
        }

        // if the external login is OIDC-based, there are certain things we need to preserve to make logout work
        // this will be different for WS-Fed, SAML2p or other protocols
        private void CaptureExternalLoginContext(AuthenticateResult externalResult, List<Claim> localClaims, AuthenticationProperties localSignInProps)
        {
            // if the external system sent a session id claim, copy it over
            // so we can use it for single sign-out
            var sid = externalResult.Principal.Claims.FirstOrDefault(x => x.Type == JwtClaimTypes.SessionId);
            if (sid != null)
            {
                localClaims.Add(new Claim(JwtClaimTypes.SessionId, sid.Value));
            }

            // if the external provider issued an id_token, we'll keep it for signout
            var idToken = externalResult.Properties.GetTokenValue("id_token");
            if (idToken != null)
            {
                localSignInProps.StoreTokens(new[] { new AuthenticationToken { Name = "id_token", Value = idToken } });
            }
        }
    }
}
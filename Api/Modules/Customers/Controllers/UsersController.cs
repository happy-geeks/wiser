using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Models;
using Api.Modules.Customers.Interfaces;
using Api.Modules.Customers.Models;
using GeeksCoreLibrary.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace Api.Modules.Customers.Controllers
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

        /// <summary>
        /// Creates a new instance of UsersController.
        /// </summary>
        public UsersController(IUsersService usersService)
        {
            this.usersService = usersService;
        }

        /// <summary>
        /// Method for getting a list of all users for the authenticated customer.
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
    }
}

using System.Collections.Generic;
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
    [Route("api/v3/[controller]"), ApiController, Authorize]
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
        /// Method for getting a list of all customers and their users. Only available for admin accounts.
        /// </summary>
        /// <returns>A <see cref="List{T}"/> of <see cref="WiserItemModel"/>, but only with names and IDs.</returns>
        [HttpGet, ProducesResponseType(typeof(List<WiserItemModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Get()
        {
            return (await usersService.GetAsync()).GetHttpResponseMessage();
        }

        /// <summary>
        /// Gets a list of encrypted template names, that can be used in Wiser 2.0+.
        /// </summary>
        /// <returns>A dictionary where the key is the plain text template name and the value is the encrypted template name.</returns>
        [HttpGet, Route("self"), ProducesResponseType(typeof(UserModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUserData()
        {
            return (await usersService.GetUserDataAsync((ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Sends a new password to a user.
        /// </summary>
        /// <param name="resetPasswordRequestModel">The information for the account to reset the password.</param>
        /// <returns>Always returns true, unless an exception occurred.</returns>
        [HttpPut, Route("reset-password"), AllowAnonymous]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequestModel resetPasswordRequestModel)
        {
            HttpContext.Items[HttpContextConstants.SubDomainKey] = resetPasswordRequestModel.SubDomain;
            return (await usersService.ResetPasswordAsync(resetPasswordRequestModel, (ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Changes the password of a user.
        /// </summary>
        /// <param name="passwords">The old and new passwords of the user.</param>
        [HttpPut, Route("password")]
        public async Task<IActionResult> UpdatePassword(ChangePasswordModel passwords)
        {
            return (await usersService.ChangePasswordAsync((ClaimsIdentity)User.Identity, passwords)).GetHttpResponseMessage();
        }
        
        /// <summary>
        /// Gets settings for a grid for the authenticated user, so that users can keep their state of all grids in Wiser.
        /// </summary>
        /// <param name="key">The unique key for the grid settings. This should be unique for each grid in Wiser, so that no 2 grids use the same settings.</param>
        [HttpGet, Route("grid-settings/{key}")]
        public async Task<IActionResult> GetGridSettingsAsync(string key)
        {
            return (await usersService.GetGridSettingsAsync((ClaimsIdentity)User.Identity, key)).GetHttpResponseMessage();
        }
        
        /// <summary>
        /// Gets settings for a grid for the authenticated user, so that users can keep their state of all grids in Wiser.
        /// </summary>
        /// <param name="key">The unique key for the grid settings. This should be unique for each grid in Wiser, so that no 2 grids use the same settings.</param>
        /// <param name="settings">A JSON object with the settings to save.</param>
        [HttpPost, Route("grid-settings/{key}")]
        public async Task<IActionResult> SaveGridSettingsAsync(string key, JToken settings)
        {
            return (await usersService.SaveGridSettingsAsync((ClaimsIdentity)User.Identity, key, settings)).GetHttpResponseMessage();
        }
        
        /// <summary>
        /// Gets the pinned modules for the authenticated user, so that users can keep their state of the pinned modules in Wiser.
        /// </summary>
        [HttpGet, Route("pinned-modules")]
        public async Task<IActionResult> GetPinnedModulesAsync()
        {
            return (await usersService.GetPinnedModulesAsync((ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
        }
        
        /// <summary>
        /// Save the list of pinned modules to the user details, so that next time the user will see the same pinned modules.
        /// </summary>
        /// <param name="moduleIds">The list of module IDs that the user has pinned.</param>
        [HttpPost, Route("pinned-modules")]
        public async Task<IActionResult> SavePinnedModulesAsync(List<int> moduleIds)
        {
            return (await usersService.SavePinnedModulesAsync((ClaimsIdentity)User.Identity, moduleIds)).GetHttpResponseMessage();
        }
        
        /// <summary>
        /// Save the list of modules that should be automatically started when the user logs in, to the user details.
        /// </summary>
        /// <param name="moduleIds">The list of module IDs that the user has set as auto load.</param>
        [HttpPost, Route("auto-load-modules")]
        public async Task<IActionResult> SaveAutoLoadModulesAsync(List<int> moduleIds)
        {
            return (await usersService.SaveAutoLoadModulesAsync((ClaimsIdentity)User.Identity, moduleIds)).GetHttpResponseMessage();
        }
    }
}

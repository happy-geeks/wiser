using System.Collections.Generic;
using System.Net.Mime;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Modules.LinkSettings.Interfaces;
using GeeksCoreLibrary.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.LinkSettings.Controllers
{
    /// <summary>
    /// Controller for all CRUD functions for link type settings.
    /// </summary>
    [Route("api/v3/link-settings")]
    [ApiController]
    [Authorize]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    public class LinkSettingsController : ControllerBase
    {
        private readonly ILinkSettingsService linkSettingsService;

        /// <summary>
        /// Creates a new instance of <see cref="LinkSettingsController"/>.
        /// </summary>
        /// <param name="linkSettingsService"></param>
        public LinkSettingsController(ILinkSettingsService linkSettingsService)
        {
            this.linkSettingsService = linkSettingsService;
        }

        /// <summary>
        /// Get all link settings.
        /// </summary>
        /// <returns>A List of <see cref="LinkSettingsModel"/> with all settings.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(List<LinkSettingsModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllAsync([FromQuery]int branchId = 0)
        {
            return (await linkSettingsService.GetAllAsync((ClaimsIdentity)User.Identity, branchId)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Get link settings based on ID.
        /// </summary>
        /// <param name="id">The ID of the settings from wiser_link.</param>
        /// <returns>A <see cref="LinkSettingsModel"/> with all settings.</returns>
        [HttpGet]
        [Route("{id:int}")]
        [ProducesResponseType(typeof(LinkSettingsModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAsync(int id)
        {
            return (await linkSettingsService.GetAsync((ClaimsIdentity)User.Identity, id)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Creates new link settings.
        /// </summary>
        /// <param name="linkSettings">The link settings to create.</param>
        /// <returns>The newly created link settings.</returns>
        [HttpPost]
        [ProducesResponseType(typeof(LinkSettingsModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create(LinkSettingsModel linkSettings)
        {
            return (await linkSettingsService.CreateAsync((ClaimsIdentity)User.Identity, linkSettings)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Updates existing link settings.
        /// </summary>
        /// <param name="id">The ID of the link settings to update.</param>
        /// <param name="linkSettings">The new data to save.</param>
        [HttpPut]
        [Route("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Update(int id, LinkSettingsModel linkSettings)
        {
            return (await linkSettingsService.UpdateAsync((ClaimsIdentity)User.Identity, id, linkSettings)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Deletes link settings.
        /// </summary>
        /// <param name="id">The ID of the link settings to delete.</param>
        [HttpDelete]
        [Route("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Delete(int id)
        {
            return (await linkSettingsService.DeleteAsync((ClaimsIdentity)User.Identity, id)).GetHttpResponseMessage();
        }
    }
}
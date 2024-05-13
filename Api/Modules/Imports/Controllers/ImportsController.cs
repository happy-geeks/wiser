using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Api.Modules.EntityProperties.Models;
using Api.Modules.Imports.Interfaces;
using Api.Modules.Imports.Models;

namespace Api.Modules.Imports.Controllers
{
    /// <summary>
    /// A controller for doing things for the import module of Wiser.
    /// </summary>
    [Route("api/v3/[controller]")]
    [ApiController]
    [Authorize]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    public class ImportsController : ControllerBase
    {
        private readonly IImportsService importsService;

        /// <summary>
        /// Creates a new instance of <see cref="ImportsController"/>.
        /// </summary>
        public ImportsController(IImportsService importsService)
        {
            this.importsService = importsService;
        }

        /// <summary>
        /// Prepare an import to be imported by the WTS.
        /// </summary>
        /// <param name="importRequest">The information needed for the import.</param>
        /// <returns>A ImportResultModel containing the result of the import.</returns>
        [HttpPost]
        [Route("prepare")]
        [ProducesResponseType(typeof(List<ImportResultModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PrepareImportAsync(ImportRequestModel importRequest)
        {
            return (await importsService.PrepareImportAsync((ClaimsIdentity)User.Identity, importRequest)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Prepare the items to delete by finding all item ids matching the criteria of the DeleteItemsRequestModel.
        /// </summary>
        /// <param name="deleteItemsRequest">The criteria for the items to delete.</param>
        /// <returns>Returns a DeleteItemsConfirmModel containing all the item ids to delete.</returns>
        [HttpPost]
        [Route("delete-items/prepare")]
        [ProducesResponseType(typeof(List<DeleteItemsConfirmModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> PrepareDeleteItemsAsync(DeleteItemsRequestModel deleteItemsRequest)
        {
            return (await importsService.PrepareDeleteItemsAsync((ClaimsIdentity)User.Identity, deleteItemsRequest)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Delete the items corresponding with the provided item ids.
        /// </summary>
        /// <param name="deleteItemsConfirm">The <see cref="DeleteItemsConfirmModel"/> containing the item ids to delete the items from.</param>
        /// <returns>Returns true on success.</returns>
        [HttpPost]
        [Route("delete-items/confirm")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteItemsAsync(DeleteItemsConfirmModel deleteItemsConfirm)
        {
            return (await importsService.DeleteItemsAsync((ClaimsIdentity)User.Identity, deleteItemsConfirm)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Prepare the links to delete by finding all item link ids matching the criteria of the <see cref="DeleteLinksRequestModel"/>.
        /// </summary>
        /// <param name="deleteLinksRequest">The criteria for the item links to delete.</param>
        /// <returns>Returns a collection of all the link item ids to delete.</returns>
        [HttpPost]
        [Route("delete-links/prepare")]
        [ProducesResponseType(typeof(List<DeleteLinksConfirmModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> PrepareDeleteLinksAsync(DeleteLinksRequestModel deleteLinksRequest)
        {
            return (await importsService.PrepareDeleteLinksAsync((ClaimsIdentity)User.Identity, deleteLinksRequest)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Delete the links corresponding to the provided information.
        /// </summary>
        /// <param name="deleteLinksConfirms">A collection of <see cref="DeleteLinksConfirmModel"/>s containing the information about the links to delete.</param>
        /// <returns>Returns true on success.</returns>
        [HttpPost]
        [Route("delete-links/confirm")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteLinksAsync(List<DeleteLinksConfirmModel> deleteLinksConfirms)
        {
            return (await importsService.DeleteLinksAsync((ClaimsIdentity)User.Identity, deleteLinksConfirms)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Retrieves all properties of an entity that can be used in the import module and returns them as <see cref="EntityPropertyModel"/> objects.
        /// </summary>
        /// <param name="entityName">The name of the property whose properties will be retrieved.</param>
        /// <param name="linkType">Optional link type, in case the properties should be retrieved by link type instead of entity name.</param>
        /// <returns>A collection of <see cref="EntityPropertyModel"/> objects.</returns>
        [HttpGet]
        [Route("entity-properties")]
        [ProducesResponseType(typeof(IEnumerable<EntityPropertyModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetEntityPropertiesAsync(string entityName = null, int linkType = 0)
        {
            return (await importsService.GetEntityPropertiesAsync((ClaimsIdentity)User.Identity, entityName, linkType)).GetHttpResponseMessage();
        }
    }
}
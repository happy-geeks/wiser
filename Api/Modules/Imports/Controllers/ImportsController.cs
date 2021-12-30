using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Api.Modules.Imports.Interfaces;
using Api.Modules.Imports.Models;

namespace Api.Modules.Imports.Controllers
{
    [Route("api/v3/[controller]"), ApiController, Authorize]
    public class ImportsController : ControllerBase
    {
        private readonly IImportsService importsService;

        public ImportsController(IImportsService importsService)
        {
            this.importsService = importsService;
        }

        //TODO Verify comment
        /// <summary>
        /// Prepare an import to be imported by the AIS.
        /// </summary>
        /// <param name="importRequest">The information needed for the import.</param>
        /// <returns></returns>
        [HttpPost, Route("prepare"), ProducesResponseType(typeof(List<ImportResultModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> PrepareImport(ImportRequestModel importRequest)
        {
            return (await importsService.PrepareImportAsync((ClaimsIdentity)User.Identity, importRequest)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Prepare the items to delete by finding all item ids matching the criteria of the <see cref="DeleteItemsRequestModel"/>.
        /// </summary>
        /// <param name="deleteItemsRequest">The criteria for the items to delete.</param>
        /// <returns>Returns a <see cref="DeleteItemsConfirmModel"/> containing all the item ids to delete.</returns>
        [HttpPost, Route("delete-items/prepare"), ProducesResponseType(typeof(List<DeleteItemsConfirmModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> PrepareDeleteItems(DeleteItemsRequestModel deleteItemsRequest)
        {
            return (await importsService.PrepareDeleteItems((ClaimsIdentity)User.Identity, deleteItemsRequest)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Delete the items corresponding with the provided item ids.
        /// </summary>
        /// <param name="deleteItemsConfirm">The <see cref="DeleteItemsConfirmModel"/> containing the item ids to delete the items from.</param>
        /// <returns>Returns true on success.</returns>
        [HttpPost, Route("delete-items/confirm"), ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteItems(DeleteItemsConfirmModel deleteItemsConfirm)
        {
            return (await importsService.DeleteItems((ClaimsIdentity)User.Identity, deleteItemsConfirm)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Prepare the links to delete by finding all item link ids matching the criteria of the <see cref="DeleteLinksRequestModel"/>.
        /// </summary>
        /// <param name="deleteLinksRequest">The criteria for the item links to delete.</param>
        /// <returns>Returns a collection of all the link item ids to delete.</returns>
        [HttpPost, Route("delete-links/prepare"), ProducesResponseType(typeof(List<DeleteLinksConfirmModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> PrepareDeleteLinks(DeleteLinksRequestModel deleteLinksRequest)
        {
            return (await importsService.PrepareDeleteLinks((ClaimsIdentity)User.Identity, deleteLinksRequest)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Delete the links corresponding to the provided information.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="deleteLinksConfirms">A collection of <see cref="DeleteLinksConfirmModel"/>s containing the information about the links to delete.</param>
        /// <returns>Returns true on success.</returns>
        [HttpPost, Route("delete-links/confirm"), ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteLinks(List<DeleteLinksConfirmModel> deleteLinksConfirms)
        {
            return (await importsService.DeleteLinks((ClaimsIdentity)User.Identity, deleteLinksConfirms)).GetHttpResponseMessage();
        }
    }
}

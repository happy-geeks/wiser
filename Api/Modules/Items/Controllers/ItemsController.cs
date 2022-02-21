using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Models;
using Api.Modules.Customers.Models;
using Api.Modules.EntityTypes.Models;
using Api.Modules.Files.Interfaces;
using Api.Modules.Files.Models;
using Api.Modules.Grids.Enums;
using Api.Modules.Grids.Interfaces;
using Api.Modules.Grids.Models;
using Api.Modules.Items.Interfaces;
using Api.Modules.Items.Models;
using Api.Modules.Kendo.Models;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Api.Modules.Items.Controllers
{
    // TODO: Add documentation.
    /// <summary>
    /// Controller for all operations that have something to do with Wiser items.
    /// </summary>
    [Route("api/v3/[controller]"), ApiController, Authorize]
    public class ItemsController : ControllerBase
    {
        private readonly IItemsService itemsService;
        private readonly IGridsService gridsService;
        private readonly IFilesService filesService;
        private readonly GclSettings gclSettings;

        /// <summary>
        /// Initializes a new instance of <see cref="ItemsController"/>.
        /// </summary>
        public ItemsController(IItemsService itemsService, IGridsService gridsService, IFilesService filesService, IOptions<GclSettings> gclSettings)
        {
            this.itemsService = itemsService;
            this.gridsService = gridsService;
            this.filesService = filesService;
            this.gclSettings = gclSettings.Value;
        }
        
        [HttpGet, ProducesResponseType(typeof(PagedResults<FlatItemModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetItemsAsync([FromQuery]PagedRequest pagedRequest = null, [FromQuery]WiserItemModel filters = null)
        {
            return (await itemsService.GetItemsAsync((ClaimsIdentity)User.Identity, pagedRequest, filters)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Get the HTML and javascript for single Wiser item, to show the item in Wiser.
        /// </summary>
        /// <param name="encryptedId">The encrypted ID of the item to get.</param>
        /// <param name="propertyIdSuffix">Optional: The suffix of every field on the item. This is used to give each field a unique ID, when multiple items are opened at the same time. Default value is <see langword="null"/>.</param>
        /// <param name="itemLinkId">Optional: The id of the item link from wiser_itemlink. This should be used when opening an item via a sub-entities-grid, to show link fields. Default value is 0.</param>
        /// <param name="entityType">Optional: The entity type of the item. Default value is <see langword="null"/>.</param>
        /// <returns>A <see cref="ItemHtmlAndScriptModel"/> with the HTML and javascript needed to load this item in Wiser.</returns>
        [HttpGet, Route("{encryptedId}"), ProducesResponseType(typeof(ItemHtmlAndScriptModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetItemAsync(string encryptedId, [FromQuery]string propertyIdSuffix = null, [FromQuery]ulong itemLinkId = 0, [FromQuery]string entityType = null)
        {
            return (await itemsService.GetItemHtmlAsync(encryptedId, (ClaimsIdentity)User.Identity, propertyIdSuffix, itemLinkId, entityType)).GetHttpResponseMessage();
        }

        [HttpGet, Route("{encryptedId}/meta"), ProducesResponseType(typeof(ItemMetaDataModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetItemMetDataAsync(string encryptedId, [FromQuery]string entityType = null)
        {
            return (await itemsService.GetItemMetaDataAsync(encryptedId, (ClaimsIdentity)User.Identity, entityType)).GetHttpResponseMessage();
        }

        [HttpGet, Route("{itemId}/block"), ProducesResponseType(typeof(WiserItemModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetHtmlBlockAsync(ulong itemId, [FromQuery]string entityType = null)
        {
            return (await itemsService.GetHtmlForWiser2EntityAsync(itemId, (ClaimsIdentity)User.Identity, entityType)).GetHttpResponseMessage();
        }

        [HttpPost, ProducesResponseType(typeof(CreateItemResultModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> PostAsync(WiserItemModel item, [FromQuery]string parentId = null, [FromQuery]int linkType = 1)
        {
            return (await itemsService.CreateAsync(item, (ClaimsIdentity)User.Identity, parentId, linkType)).GetHttpResponseMessage();
        }

        [HttpPut, Route("{encryptedId}"), ProducesResponseType(typeof(WiserItemModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> PutAsync(string encryptedId, WiserItemModel item)
        {
            return (await itemsService.UpdateAsync(encryptedId, item, (ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
        }

        [HttpPost, Route("{encryptedId}/duplicate/{encryptedParentId}"), ProducesResponseType(typeof(WiserItemDuplicationResultModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> DuplicateAsync(string encryptedId, string encryptedParentId, [FromQuery]string entityType = null, [FromQuery]string parentEntityType = null)
        {
            return (await itemsService.DuplicateItemAsync(encryptedId, encryptedParentId, (ClaimsIdentity)User.Identity, entityType, parentEntityType)).GetHttpResponseMessage();
        }

        [HttpPost, Route("{encryptedId}/copy-to-environment/{newEnvironments:int}"), ProducesResponseType(typeof(WiserItemModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> CopyToEnvironmentAsync(string encryptedId, Environments newEnvironments)
        {
            return (await itemsService.CopyToEnvironmentAsync(encryptedId, newEnvironments, (ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
        }

        [HttpDelete, Route("{encryptedId}"), ProducesResponseType(typeof(WiserItemModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteAsync(string encryptedId, [FromQuery]bool undelete = false, [FromQuery]string entityType = null)
        {
            return (await itemsService.DeleteAsync(encryptedId, (ClaimsIdentity)User.Identity, undelete, entityType)).GetHttpResponseMessage();
        }

        [HttpPost, Route("{encryptedId}/workflow"), ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<IActionResult> WorkflowAsync(string encryptedId, WiserItemModel item, [FromQuery] bool isNewItem = false)
        {
            return (await itemsService.ExecuteWorkflowAsync(encryptedId, isNewItem, (ClaimsIdentity)User.Identity, item)).GetHttpResponseMessage();
        }

        [HttpPost, Route("{encryptedId}/action-button/{propertyId:int}"), ProducesResponseType(typeof(ActionButtonResultModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> ActionButton(string encryptedId, int propertyId, [FromBody] Dictionary<string, object> extraParameters, [FromQuery] string queryId = null, [FromQuery] ulong itemLinkId = 0)
        {
            return (await itemsService.ExecuteCustomQueryAsync(encryptedId, propertyId, extraParameters, queryId, (ClaimsIdentity)User.Identity, itemLinkId)).GetHttpResponseMessage();
        }

        [HttpPost, Route("{encryptedId}/upload")]
        public async Task<IActionResult> Upload(string encryptedId, [FromQuery]string propertyName, [FromQuery]string title = "", [FromQuery]ulong itemLinkId = 0, [FromQuery]bool useTinyPng = false)
        {
            var form = await Request.ReadFormAsync();

            var result = await filesService.UploadAsync(encryptedId, propertyName, title, form.Files, (ClaimsIdentity)User.Identity, itemLinkId, useTinyPng);
            return result.GetHttpResponseMessage();
        }

        [HttpPost, Route("{encryptedId}/files/url")]
        public async Task<IActionResult> AddFileUrl(string encryptedId, [FromBody]FileModel file, [FromQuery]string propertyName, [FromQuery]ulong itemLinkId = 0)
        {
            var result = await filesService.AddFileUrl(encryptedId, propertyName, file, (ClaimsIdentity)User.Identity, itemLinkId);
            return result.GetHttpResponseMessage();
        }
        
        [HttpGet, Route("{itemId}/files/{fileId:int}/{filename}"), AllowAnonymous]
        public async Task<IActionResult> GetFileAsync(string itemId, int fileId, string fileName, [FromQuery] CustomerInformationModel customerInformation, [FromQuery]ulong itemLinkId = 0)
        {
            // Create a ClaimsIdentity based on query parameters instead the Identity from the bearer token due to being called from an image source where no headers can be set.
            var userId = String.IsNullOrWhiteSpace(customerInformation.encryptedUserId) ? 0 : Int32.Parse(customerInformation.encryptedUserId.Replace(" ", "+").DecryptWithAesWithSalt(gclSettings.DefaultEncryptionKey, true));
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.GroupSid, customerInformation.subDomain ?? "")
            };
            var dummyClaimsIdentity = new ClaimsIdentity(claims);
            //Set the sub domain for the database connection.
            HttpContext.Items[HttpContextConstants.SubDomainKey] = customerInformation.subDomain;

            var imageResult = await filesService.GetFileAsync(itemId, fileId, dummyClaimsIdentity, itemLinkId);
            var result = imageResult.GetHttpResponseMessage();
            if (imageResult.StatusCode != HttpStatusCode.OK)
            {
                return result;
            }
            
            if (!String.IsNullOrWhiteSpace(imageResult.ModelObject.Url))
            {
                imageResult.StatusCode = HttpStatusCode.Found;
                return Redirect(imageResult.ModelObject.Url);
            }

            return File(imageResult.ModelObject.Data, imageResult.ModelObject.ContentType);
        }
        
        [HttpDelete, Route("{encryptedItemId}/files/{fileId:int}")]
        public async Task<IActionResult> DeleteFileAsync(string encryptedItemId, int fileId, [FromQuery]ulong itemLinkId = 0)
        {
            return (await filesService.DeleteFileAsync(encryptedItemId, fileId, (ClaimsIdentity)User.Identity, itemLinkId)).GetHttpResponseMessage();
        }
        
        [HttpPut, Route("{encryptedItemId}/files/{fileId:int}/rename/{newName}")]
        public async Task<IActionResult> RenameFileAsync(string encryptedItemId, int fileId, string newName, [FromQuery]ulong itemLinkId = 0)
        {
            return (await filesService.RenameFileAsync(encryptedItemId, fileId, newName, (ClaimsIdentity)User.Identity, itemLinkId)).GetHttpResponseMessage();
        }

        [HttpPost, Route("{encryptedId}/entity-grids/{entityType}"), ProducesResponseType(typeof(GridSettingsAndDataModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> EntityGridAsync(string encryptedId, string entityType, GridReadOptionsModel options, [FromQuery]int linkTypeNumber = 0, [FromQuery]int moduleId = 0, [FromQuery]EntityGridModes mode = EntityGridModes.Normal, [FromQuery]int propertyId = 0, [FromQuery]string queryId = null, [FromQuery]string countQueryId = null, [FromQuery]string fieldGroupName = null, [FromQuery]bool currentItemIsSourceId = false)
        {
            return (await gridsService.GetEntityGridDataAsync(encryptedId, entityType, linkTypeNumber, moduleId, mode, options, propertyId, queryId, countQueryId, fieldGroupName, currentItemIsSourceId, (ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
        }
        
        [HttpGet, Route("{encryptedId}/grids/{propertyId:int}"), ProducesResponseType(typeof(GridSettingsAndDataModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetGridDataAsync(string encryptedId, int propertyId, [FromQuery]string queryId = null, [FromQuery]string countQueryId = null)
        {
            return (await gridsService.GetDataAsync(propertyId, encryptedId, new GridReadOptionsModel(), queryId, countQueryId, (ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
        }
        
        [HttpPost, Route("{encryptedId}/grids-with-filters/{propertyId:int}"), ProducesResponseType(typeof(GridSettingsAndDataModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetGridDataWithFiltersAsync(string encryptedId, int propertyId, GridReadOptionsModel options, [FromQuery]string queryId = null, [FromQuery]string countQueryId = null)
        {
            return (await gridsService.GetDataAsync(propertyId, encryptedId, options, queryId, countQueryId, (ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
        }
        
        [HttpPost, Route("{encryptedId}/grids/{propertyId:int}"), ProducesResponseType(typeof(List<Dictionary<string, object>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> InsertGridDataAsync(string encryptedId, int propertyId, Dictionary<string, object> data)
        {
            return (await gridsService.InsertDataAsync(propertyId, encryptedId, data, (ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
        }
        
        [HttpPut, Route("{encryptedId}/grids/{propertyId:int}"), ProducesResponseType(typeof(List<Dictionary<string, object>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateGridDataAsync(string encryptedId, int propertyId, Dictionary<string, object> data)
        {
            return (await gridsService.UpdateDataAsync(propertyId, encryptedId, data, (ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
        }
        
        [HttpDelete, Route("{encryptedId}/grids/{propertyId:int}"), ProducesResponseType(typeof(List<Dictionary<string, object>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteGridDataAsync(string encryptedId, int propertyId, Dictionary<string, object> data)
        {
            return (await gridsService.DeleteDataAsync(propertyId, encryptedId, data, (ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
        }
        
        [HttpGet, Route("{itemId:long}/entity-types"), ProducesResponseType(typeof(List<EntityTypeModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPossibleEntityTypesAsync(ulong itemId)
        {
            return (await itemsService.GetPossibleEntityTypesAsync(itemId, (ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
        }

        [HttpGet, Route("tree-view"), ProducesResponseType(typeof(List<TreeViewItemModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetItemsForTreeViewAsync([FromQuery] int moduleId, [FromQuery] string encryptedItemId = null, [FromQuery] string entityType = null, [FromQuery] string orderBy = null, [FromQuery] string checkId = null)
        {
            return (await itemsService.GetItemsForTreeViewAsync(moduleId, (ClaimsIdentity)User.Identity, entityType, encryptedItemId, orderBy, checkId)).GetHttpResponseMessage();
        }

        [HttpPut, Route("{encryptedSourceId}/move/{encryptedDestinationId}"), ProducesResponseType(typeof(List<TreeViewItemModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> MoveItemAsync(string encryptedSourceId, string encryptedDestinationId, [FromBody]MoveItemRequestModel data)
        {
            return (await itemsService.MoveItemAsync((ClaimsIdentity)User.Identity, encryptedSourceId, encryptedDestinationId, data.Position, data.EncryptedSourceParentId, data.EncryptedDestinationParentId, data.SourceEntityType, data.DestinationEntityType, data.ModuleId)).GetHttpResponseMessage();
        }

        [HttpPost, Route("add-links")]
        public async Task<IActionResult> AddMultipleLinksAsync([FromBody]AddOrRemoveLinksRequestModel data)
        {
            return (await itemsService.AddMultipleLinksAsync((ClaimsIdentity)User.Identity, data.EncryptedSourceIds, data.EncryptedDestinationIds, data.LinkType, data.SourceEntityType)).GetHttpResponseMessage();
        }

        [HttpDelete, Route("remove-links")]
        public async Task<IActionResult> RemoveMultipleLinksAsync([FromBody]AddOrRemoveLinksRequestModel data)
        {
            return (await itemsService.RemoveMultipleLinksAsync((ClaimsIdentity)User.Identity, data.EncryptedSourceIds, data.EncryptedDestinationIds, data.LinkType, data.SourceEntityType)).GetHttpResponseMessage();
        }
        
        [HttpGet, Route("{id:long}/encrypt"), ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetEncryptedIdAsync(ulong id)
        {
            return (await itemsService.GetEncryptedIdAsync(id, (ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
        }
    }
}
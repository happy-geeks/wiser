using System.Collections.Generic;
using System.Net.Mime;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Models;
using Api.Modules.EntityTypes.Models;
using Api.Modules.Files.Interfaces;
using Api.Modules.Grids.Enums;
using Api.Modules.Grids.Interfaces;
using Api.Modules.Grids.Models;
using Api.Modules.Items.Interfaces;
using Api.Modules.Items.Models;
using Api.Modules.Kendo.Models;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Api.Modules.Items.Controllers
{
    /// <summary>
    /// Controller for all operations that have something to do with Wiser items.
    /// </summary>
    [Route("api/v3/[controller]")]
    [ApiController]
    [Authorize]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
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

        /// <summary>
        /// Gets all items from Wiser. It's possible to use filters here.
        /// The results will be returned in multiple pages, with a max of 500 items per page.
        /// </summary>
        /// <param name="pagedRequest">Optional: Which page to get and how many items per page to get.</param>
        /// <param name="filters">Optional: Add filters if you only want specific results.</param>
        /// <returns>A PagedResults with information about the total amount of items, page number etc. The results property contains the actual results, of type FlatItemModel.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResults<FlatItemModel>), StatusCodes.Status200OK)]
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
        /// <param name="linkType">Optional: The type number of the link, if this item also contains fields on a link.</param>
        /// <returns>A <see cref="ItemHtmlAndScriptModel"/> with the HTML and javascript needed to load this item in Wiser.</returns>
        [HttpGet]
        [Route("{encryptedId}")]
        [ProducesResponseType(typeof(ItemHtmlAndScriptModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetItemAsync(string encryptedId, [FromQuery]string propertyIdSuffix = null, [FromQuery]ulong itemLinkId = 0, [FromQuery]string entityType = null, [FromQuery]int linkType = 0)
        {
            return (await itemsService.GetItemHtmlAsync(encryptedId, (ClaimsIdentity)User.Identity, propertyIdSuffix, itemLinkId, entityType, linkType)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Gets all details of an item.
        /// </summary>
        /// <param name="encryptedId">The encrypted ID of the item.</param>
        /// <param name="entityType">Optional: The entity type of the item to retrieve. This is needed when the item is saved in a different table than wiser_item. We can only look up the name of that table if we know the entity type beforehand.</param>
        /// <returns></returns>
        [HttpGet]
        [Route("{encryptedId}/details")]
        [ProducesResponseType(typeof(WiserItemModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetItemDetailsAsync(string encryptedId, [FromQuery]string entityType = null)
        {
            return (await itemsService.GetItemDetailsAsync(encryptedId, (ClaimsIdentity)User.Identity, entityType)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Retrieve an item that the given item is linked to, or and item that is linked to the given item.
        /// </summary>
        /// <param name="encryptedId">The encrypted ID of the main item.</param>
        /// <param name="entityType">Optional: The entity type of the item to retrieve. This is needed when the item is saved in a different table than wiser_item. We can only look up the name of that table if we know the entity type beforehand.</param>
        /// <param name="itemIdEntityType">Optional: You can enter the entity type of the given itemId here, if you want to get items from a dedicated table and those items can have multiple different entity types. This only works if all those items exist in the same table. Default is null.</param>
        /// <param name="linkType">Optional: The type number of the link.</param>
        /// <param name="reversed">Optional: Whether to retrieve an item that that given item is linked to (<see langword="true"/>), or an item that is linked to the given item (<see langword="false"/>).</param>
        /// <returns></returns>
        [HttpGet]
        [Route("{encryptedId}/linked/details")]
        [ProducesResponseType(typeof(IEnumerable<WiserItemModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetLinkedItemDetailsAsync(string encryptedId, [FromQuery]string entityType = null, [FromQuery]string itemIdEntityType = null, [FromQuery]int linkType = 0, [FromQuery]bool reversed = false)
        {
            return (await itemsService.GetLinkedItemDetailsAsync(encryptedId, (ClaimsIdentity)User.Identity, entityType, itemIdEntityType, linkType, reversed)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Get the meta data of an item. This is data such as the title, entity type, last change date etc.
        /// </summary>
        /// <param name="encryptedId">The encrypted ID of the item.</param>
        /// <param name="entityType">Optional: The entity type of the item to duplicate. This is needed when the item is saved in a different table than wiser_item. We can only look up the name of that table if we know the entity type beforehand.</param>
        /// <returns>The item meta data.</returns>
        [HttpGet]
        [Route("{encryptedId}/meta")]
        [ProducesResponseType(typeof(ItemMetaDataModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetItemMetDataAsync(string encryptedId, [FromQuery]string entityType = null)
        {
            return (await itemsService.GetItemMetaDataAsync(encryptedId, (ClaimsIdentity)User.Identity, entityType)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Get the HTML for a specific Wiser item.
        /// In Wiser you can declare a default query and HTML template for entity types, to render a single item of that type on a website.
        /// This method will return that rendered HTML.
        /// </summary>
        /// <param name="itemId">The ID of the item to render to HTML.</param>
        /// <param name="entityType">Optional: The entity type of the item to duplicate. This is needed when the item is saved in a different table than wiser_item. We can only look up the name of that table if we know the entity type beforehand.</param>
        /// <returns>The rendered HTML for the item.</returns>
        [HttpGet]
        [Route("{itemId}/block")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetHtmlBlockAsync(ulong itemId, [FromQuery]string entityType = null)
        {
            return (await itemsService.GetHtmlForWiserEntityAsync(itemId, (ClaimsIdentity)User.Identity, entityType)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Create a new item.
        /// </summary>
        /// <param name="item">The item to create.</param>
        /// <param name="parentId">Optional: The encrypted ID of the parent to create this item under.</param>
        /// <param name="linkType">Optional: The link type of the link to the parent.</param>
        /// <returns>A CreateItemResultModel with information about the newly created item.</returns>
        [HttpPost]
        [ProducesResponseType(typeof(CreateItemResultModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> PostAsync(WiserItemModel item, [FromQuery]string parentId = null, [FromQuery]int linkType = 1)
        {
            return (await itemsService.CreateAsync(item, (ClaimsIdentity)User.Identity, parentId, linkType)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Updates an item.
        /// </summary>
        /// <param name="encryptedId">The encrypted ID of the item to update.</param>
        /// <param name="item">The new data for the item.</param>
        /// <returns>The updated item.</returns>
        [HttpPut]
        [Route("{encryptedId}")]
        [ProducesResponseType(typeof(WiserItemModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> PutAsync(string encryptedId, WiserItemModel item)
        {
            return (await itemsService.UpdateAsync(encryptedId, item, (ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Creates a duplicate copy of an existing item.
        /// </summary>
        /// <param name="encryptedId">The encrypted ID of the item to duplicate.</param>
        /// <param name="encryptedParentId">The encrypted ID of the parent of the item to duplicate. The copy will be placed under the same parent.</param>
        /// <param name="entityType">Optional: The entity type of the item to duplicate. This is needed when the item is saved in a different table than wiser_item. We can only look up the name of that table if we know the entity type beforehand.</param>
        /// <param name="parentEntityType">Optional: The entity type of the parent of the item to duplicate. This is needed when the parent item is saved in a different table than wiser_item. We can only look up the name of that table if we know the entity type beforehand.</param>
        /// <returns>A WiserItemDuplicationResultModel containing the result of the duplication.</returns>
        [HttpPost]
        [Route("{encryptedId}/duplicate/{encryptedParentId}")]
        [ProducesResponseType(typeof(WiserItemDuplicationResultModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DuplicateAsync(string encryptedId, string encryptedParentId, [FromQuery]string entityType = null, [FromQuery]string parentEntityType = null)
        {
            return (await itemsService.DuplicateItemAsync(encryptedId, encryptedParentId, (ClaimsIdentity)User.Identity, entityType, parentEntityType)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Copy an item one or more other environments, so that you can have multiple different versions of an item for different environments.
        /// </summary>
        /// <param name="encryptedId">The encrypted ID of the item.</param>
        /// <param name="newEnvironments">The environment(s) to copy the item to.</param>
        /// <returns>The copied item.</returns>
        [HttpPost]
        [Route("{encryptedId}/copy-to-environment/{newEnvironments:int}")]
        [ProducesResponseType(typeof(WiserItemModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        // ReSharper disable once RouteTemplates.ParameterTypeAndConstraintsMismatch
        public async Task<IActionResult> CopyToEnvironmentAsync(string encryptedId, Environments newEnvironments)
        {
            return (await itemsService.CopyToEnvironmentAsync(encryptedId, newEnvironments, (ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Change the environments that an item should be visible in.
        /// </summary>
        /// <param name="encryptedId">The encrypted ID of the item.</param>
        /// <param name="entityType">The entity type of the item.</param>
        /// <param name="newEnvironments">The environment(s) to make the item visible in. Use Environments.Hidden (0) to hide an item completely.</param>
        [HttpPatch]
        [Route("{encryptedId}/environment/{newEnvironments:int}")]
        [ProducesResponseType(typeof(WiserItemModel), StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        // ReSharper disable once RouteTemplates.ParameterTypeAndConstraintsMismatch
        public async Task<IActionResult> ChangeEnvironmentAsync(string encryptedId, Environments newEnvironments, [FromQuery]string entityType)
        {
            return (await itemsService.ChangeEnvironmentAsync(encryptedId, entityType, newEnvironments, (ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Delete or undelete an item. Deleting an item will move it to an archive table, so it's never completely deleted by this method.
        /// Undeleting an item moves it from the archive table back to the actual table.
        /// </summary>
        /// <param name="encryptedId">The encrypted ID of the item to delete.</param>
        /// <param name="undelete">Optional: Whether to undelete the item instead of deleting it.</param>
        /// <param name="entityType">Optional: The entity type of the item. This is needed if the item is saved in a different table than wiser_item.</param>
        [HttpDelete]
        [Route("{encryptedId}")]
        [ProducesResponseType(typeof(WiserItemModel), StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteAsync(string encryptedId, [FromQuery]bool undelete = false, [FromQuery]string entityType = null)
        {
            return (await itemsService.DeleteAsync(encryptedId, (ClaimsIdentity)User.Identity, undelete, entityType)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Executes the workflow for an item. In wiser_entity you can set queries that need to be executed after an item has been created or updated.
        /// This method will execute these queries, based on what is being done with the item.
        /// </summary>
        /// <param name="encryptedId">The encrypted ID of the item that was recently created or updated.</param>
        /// <param name="isNewItem">Set to true if the item was just created, or false if it was updated.</param>
        /// <param name="item">Optional: The data of the item to execute the workflow for.</param>
        /// <returns>A boolean indicating whether or not anything was done. If there was no workflow setup, false will be returned, otherwise true.</returns>
        [HttpPost]
        [Route("{encryptedId}/workflow")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> WorkflowAsync(string encryptedId, WiserItemModel item, [FromQuery] bool isNewItem = false)
        {
            return (await itemsService.ExecuteWorkflowAsync(encryptedId, isNewItem, (ClaimsIdentity)User.Identity, item)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Executes a custom query and return the results.
        /// This will call GetCustomQueryAsync and use that query.
        /// This will replace the details from an item in the query before executing it.
        /// </summary>
        /// <param name="encryptedId">The encrypted ID of the item to execute the query for.</param>
        /// <param name="propertyId">The ID of the property from wiser_entityproperty. Set to 0 if you want to use a query ID.</param>
        /// <param name="extraParameters">Any extra parameters to use in the query.</param>
        /// <param name="queryId">The encrypted ID of the query from wiser_query. Encrypt the value "0" if you want to use a property ID.</param>
        /// <param name="itemLinkId">Optional: If the item is linked to something else and you need to know that in the query, enter the ID of that link from wiser_itemlink here.</param>
        /// <returns>The results of the query.</returns>
        [HttpPost]
        [Route("{encryptedId}/action-button/{propertyId:int}")]
        [ProducesResponseType(typeof(ActionButtonResultModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ActionButton(string encryptedId, int propertyId, [FromBody] Dictionary<string, object> extraParameters, [FromQuery] string queryId = null, [FromQuery] ulong itemLinkId = 0)
        {
            return (await itemsService.ExecuteCustomQueryAsync(encryptedId, propertyId, extraParameters, queryId, (ClaimsIdentity)User.Identity, itemLinkId)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Get the data for a sub-entities-grid.
        /// </summary>
        /// <param name="encryptedId">The encrypted ID of the currently opened item that contains the sub-entities-grid.</param>
        /// <param name="entityType">The entity type of the currently opened item that contains the sub-entities-grid.</param>
        /// <param name="linkTypeNumber">The link type number to use for getting linked items.</param>
        /// <param name="moduleId">The module ID of the items to get.</param>
        /// <param name="mode">The mode that the sub-entities-grid is in.</param>
        /// <param name="options">The options for the grid.</param>
        /// <param name="propertyId">The ID of the corresponding row in wiser_entityproperty.</param>
        /// <param name="queryId">Optional: The encrypted ID of the query to execute for getting the data.</param>
        /// <param name="countQueryId">Optional: The encrypted ID of the query to execute for counting the total amount of items.</param>
        /// <param name="fieldGroupName">Optional: The field group name, when getting all fields of a group.</param>
        /// <param name="currentItemIsSourceId">Optional: Whether the opened item (that contains the sub-entities-grid) is the source instead of the destination.</param>
        /// <returns>The data of the grid, as a <see cref="GridSettingsAndDataModel"/>.</returns>
        [HttpPost]
        [Route("{encryptedId}/entity-grids/{entityType}")]
        [ProducesResponseType(typeof(GridSettingsAndDataModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> EntityGridAsync(string encryptedId, string entityType, GridReadOptionsModel options, [FromQuery]int linkTypeNumber = 0, [FromQuery]int moduleId = 0, [FromQuery]EntityGridModes mode = EntityGridModes.Normal, [FromQuery]int propertyId = 0, [FromQuery]string queryId = null, [FromQuery]string countQueryId = null, [FromQuery]string fieldGroupName = null, [FromQuery]bool currentItemIsSourceId = false)
        {
            return (await gridsService.GetEntityGridDataAsync(encryptedId, entityType, linkTypeNumber, moduleId, mode, options, propertyId, queryId, countQueryId, fieldGroupName, currentItemIsSourceId, (ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Get the data for a grid.
        /// </summary>
        /// <param name="propertyId">The ID of the corresponding row in wiser_entityproperty.</param>
        /// <param name="encryptedId">The encrypted ID of the currently opened item that contains the sub-entities-grid.</param>
        /// <param name="queryId">Optional: The encrypted ID of the query to execute for getting the data.</param>
        /// <param name="countQueryId">Optional: The encrypted ID of the query to execute for counting the total amount of items.</param>
        /// <returns>The data of the grid, as a <see cref="GridSettingsAndDataModel"/>.</returns>
        [HttpGet]
        [Route("{encryptedId}/grids/{propertyId:int}")]
        [ProducesResponseType(typeof(GridSettingsAndDataModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetGridDataAsync(string encryptedId, int propertyId, [FromQuery]string queryId = null, [FromQuery]string countQueryId = null)
        {
            return (await gridsService.GetDataAsync(propertyId, encryptedId, new GridReadOptionsModel(), queryId, countQueryId, (ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Get the data for a grid with filters.
        /// </summary>
        /// <param name="propertyId">The ID of the corresponding row in wiser_entityproperty.</param>
        /// <param name="encryptedId">The encrypted ID of the currently opened item that contains the sub-entities-grid.</param>
        /// <param name="options">The options for the grid.</param>
        /// <param name="queryId">Optional: The encrypted ID of the query to execute for getting the data.</param>
        /// <param name="countQueryId">Optional: The encrypted ID of the query to execute for counting the total amount of items.</param>
        /// <returns>The data of the grid, as a <see cref="GridSettingsAndDataModel"/>.</returns>
        [HttpPost]
        [Route("{encryptedId}/grids-with-filters/{propertyId:int}")]
        [ProducesResponseType(typeof(GridSettingsAndDataModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetGridDataWithFiltersAsync(string encryptedId, int propertyId, GridReadOptionsModel options, [FromQuery]string queryId = null, [FromQuery]string countQueryId = null)
        {
            return (await gridsService.GetDataAsync(propertyId, encryptedId, options, queryId, countQueryId, (ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Insert a new row in a grid.
        /// </summary>
        /// <param name="propertyId">The ID of the corresponding row in wiser_entityproperty.</param>
        /// <param name="encryptedId">The encrypted ID of the currently opened item that contains the sub-entities-grid.</param>
        /// <param name="data">The data for the new row.</param>
        /// <returns>The newly added data.</returns>
        [HttpPost]
        [Route("{encryptedId}/grids/{propertyId:int}")]
        [ProducesResponseType(typeof(List<Dictionary<string, object>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> InsertGridDataAsync(string encryptedId, int propertyId, Dictionary<string, object> data)
        {
            return (await gridsService.InsertDataAsync(propertyId, encryptedId, data, (ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Update a row in a grid.
        /// </summary>
        /// <param name="propertyId">The ID of the corresponding row in wiser_entityproperty.</param>
        /// <param name="encryptedId">The encrypted ID of the currently opened item that contains the sub-entities-grid.</param>
        /// <param name="data">The new data for the row.</param>
        [HttpPut]
        [Route("{encryptedId}/grids/{propertyId:int}")]
        [ProducesResponseType(typeof(List<Dictionary<string, object>>), StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateGridDataAsync(string encryptedId, int propertyId, Dictionary<string, object> data)
        {
            return (await gridsService.UpdateDataAsync(propertyId, encryptedId, data, (ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Delete a row in a grid.
        /// </summary>
        /// <param name="propertyId">The ID of the corresponding row in wiser_entityproperty.</param>
        /// <param name="encryptedId">The encrypted ID of the currently opened item that contains the sub-entities-grid.</param>
        /// <param name="data">The new data for the row.</param>
        [HttpDelete]
        [Route("{encryptedId}/grids/{propertyId:int}")]
        [ProducesResponseType(typeof(List<Dictionary<string, object>>), StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DeleteGridDataAsync(string encryptedId, int propertyId, Dictionary<string, object> data)
        {
            return (await gridsService.DeleteDataAsync(propertyId, encryptedId, data, (ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Gets all entity types from an item ID.
        /// Wiser can have multiple wiser_item tables, with a prefix for certain entity types. This means an ID can exists multiple times.
        /// This method will get the different entity types with the given ID.
        /// </summary>
        /// <param name="itemId">The ID of the item to render to HTML.</param>
        /// <returns>A list of all entity types that contain an item with this ID.</returns>
        [HttpGet]
        [Route("{itemId:long}/entity-types")]
        [ProducesResponseType(typeof(List<EntityTypeModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPossibleEntityTypesAsync(ulong itemId)
        {
            return (await itemsService.GetPossibleEntityTypesAsync(itemId, (ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Get all items for a tree view for a specific parent.
        /// This method does not work with dedicated tables for entity types or link types, because we can't know beforehand what entity types and link types a tree view will contain, so we have no way to know which dedicated tables to use.
        /// </summary>
        /// <param name="moduleId">The ID of the module.</param>
        /// <param name="entityType">Optional: The entity type of the item to duplicate. This is needed when the item is saved in a different table than wiser_item. We can only look up the name of that table if we know the entity type beforehand.</param>
        /// <param name="encryptedItemId">Optional: The encrypted ID of the parent to fix the ordering for. If no value has been given, the root will be used as parent.</param>
        /// <param name="orderBy">Optional: Enter the value "item_title" to order by title, or nothing to order by order number.</param>
        /// <param name="checkId">Optional: This is meant for item-linker fields. This is the encrypted ID for the item that should currently be checked.</param>
        /// <param name="linkType">Optional: The type number of the link. This is used in combination with "checkId"; So that items will only be marked as checked if they have the given link ID.</param>
        /// <returns>A list of <see cref="TreeViewItemModel"/>.</returns>
        [HttpGet]
        [Route("tree-view")]
        [ProducesResponseType(typeof(List<TreeViewItemModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetItemsForTreeViewAsync([FromQuery] int moduleId, [FromQuery] string encryptedItemId = null, [FromQuery] string entityType = null, [FromQuery] string orderBy = null, [FromQuery] string checkId = null, [FromQuery] int linkType = 0)
        {
            return (await itemsService.GetItemsForTreeViewAsync(moduleId, (ClaimsIdentity)User.Identity, entityType, encryptedItemId, orderBy, checkId, linkType)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Move an item to a different position in the tree view.
        /// </summary>
        /// <param name="encryptedSourceId">The encrypted ID of the item that is being moved.</param>
        /// <param name="encryptedDestinationId">The encrypted ID of the item that it's being moved towards.</param>
        /// <param name="data">The data needed to know where the item should be moved to.</param>
        [HttpPut]
        [Route("{encryptedSourceId}/move/{encryptedDestinationId}")]
        [ProducesResponseType(typeof(List<TreeViewItemModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> MoveItemAsync(string encryptedSourceId, string encryptedDestinationId, [FromBody]MoveItemRequestModel data)
        {
            return (await itemsService.MoveItemAsync((ClaimsIdentity)User.Identity, encryptedSourceId, encryptedDestinationId, data.Position, data.EncryptedSourceParentId, data.EncryptedDestinationParentId, data.SourceEntityType, data.DestinationEntityType, data.ModuleId)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Link one or more items to one or more other items.
        /// </summary>
        /// <param name="data">The data needed to add new links.</param>
        [HttpPost]
        [Route("add-links")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> AddMultipleLinksAsync([FromBody]AddOrRemoveLinksRequestModel data)
        {
            return (await itemsService.AddMultipleLinksAsync((ClaimsIdentity)User.Identity, data.EncryptedSourceIds, data.EncryptedDestinationIds, data.LinkType, data.SourceEntityType)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Removed one or more links between items.
        /// </summary>
        /// <param name="data">The data for the links to remove.</param>
        [HttpDelete]
        [Route("remove-links")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> RemoveMultipleLinksAsync([FromBody]AddOrRemoveLinksRequestModel data)
        {
            return (await itemsService.RemoveMultipleLinksAsync((ClaimsIdentity)User.Identity, data.EncryptedSourceIds, data.EncryptedDestinationIds, data.LinkType, data.SourceEntityType)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Encrypt an ID of an item.
        /// </summary>
        /// <param name="id">The ID to encrypt.</param>
        /// <returns>The encrypted ID.</returns>
        [HttpGet]
        [Route("{id:long}/encrypt")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetEncryptedIdAsync(ulong id)
        {
            return (await itemsService.GetEncryptedIdAsync(id, (ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Translate all fields of an item into one or more other languages, using the Google Translation API.
        /// This will only translate fields that don't have a value yet for the destination language
        /// </summary>
        /// <param name="encryptedId">The encrypted ID of the item to translate.</param>
        /// <param name="settings">The settings for translating.</param>
        [HttpPut]
        [Route("{encryptedId}/translate")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetEncryptedIdAsync(string encryptedId, TranslateItemRequestModel settings)
        {
            return (await itemsService.TranslateAllFieldsAsync((ClaimsIdentity)User.Identity, encryptedId, settings)).GetHttpResponseMessage();
        }
    }
}
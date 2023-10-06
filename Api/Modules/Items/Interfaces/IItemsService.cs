using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Models;
using Api.Core.Services;
using Api.Modules.EntityTypes.Models;
using Api.Modules.Items.Models;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Models;

namespace Api.Modules.Items.Interfaces
{
    /// <summary>
    /// A service for CRUD and other operations for items from Wiser.
    /// </summary>
    public interface IItemsService
    {
        /// <summary>
        /// Gets all items from Wiser. It's possible to use filters here.
        /// The results will be returned in multiple pages, with a max of 500 items per page.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="pagedRequest">Optional: Which page to get and how many items per page to get.</param>
        /// <param name="filters">Optional: Add filters if you only want specific results.</param>
        /// <returns>A PagedResults with information about the total amount of items, page number etc. The results property contains the actual results, of type FlatItemModel.</returns>
        Task<ServiceResult<PagedResults<FlatItemModel>>> GetItemsAsync(ClaimsIdentity identity, PagedRequest pagedRequest = null, WiserItemModel filters = null);

        /// <summary>
        /// Creates a duplicate copy of an existing item.
        /// </summary>
        /// <param name="encryptedId">The encrypted ID of the item to duplicate.</param>
        /// <param name="encryptedParentId">The encrypted ID of the parent of the item to duplicate. The copy will be placed under the same parent.</param>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="entityType">Optional: The entity type of the item to duplicate. This is needed when the item is saved in a different table than wiser_item. We can only look up the name of that table if we know the entity type beforehand.</param>
        /// <param name="parentEntityType">Optional: The entity type of the parent of the item to duplicate. This is needed when the parent item is saved in a different table than wiser_item. We can only look up the name of that table if we know the entity type beforehand.</param>
        /// <returns>A WiserItemDuplicationResultModel containing the result of the duplication.</returns>
        Task<ServiceResult<WiserItemDuplicationResultModel>> DuplicateItemAsync(string encryptedId, string encryptedParentId, ClaimsIdentity identity, string entityType = null, string parentEntityType = null);

        /// <summary>
        /// Copy an item one or more other environments, so that you can have multiple different versions of an item for different environments.
        /// </summary>
        /// <param name="encryptedId">The encrypted ID of the item.</param>
        /// <param name="newEnvironments">The environment(s) to copy the item to.</param>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <returns>The copied item.</returns>
        Task<ServiceResult<WiserItemModel>> CopyToEnvironmentAsync(string encryptedId, Environments newEnvironments, ClaimsIdentity identity);

        /// <summary>
        /// Change the environments that an item should be visible in.
        /// </summary>
        /// <param name="encryptedId">The encrypted ID of the item.</param>
        /// <param name="entityType">The entity type of the item.</param>
        /// <param name="newEnvironments">The environment(s) to make the item visible in. Use Environments.Hidden (0) to hide an item completely.</param>
        /// <param name="identity">The identity of the authenticated user.</param>
        Task<ServiceResult<bool>> ChangeEnvironmentAsync(string encryptedId, string entityType, Environments newEnvironments, ClaimsIdentity identity);

        /// <summary>
        /// Create a new item.
        /// </summary>
        /// <param name="item">The item to create.</param>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="encryptedParentId">Optional: The encrypted ID of the parent to create this item under.</param>
        /// <param name="linkType">Optional: The link type of the link to the parent.</param>
        /// <returns>A CreateItemResultModel with information about the newly created item.</returns>
        Task<ServiceResult<CreateItemResultModel>> CreateAsync(WiserItemModel item, ClaimsIdentity identity, string encryptedParentId = null, int linkType = 1);

        /// <summary>
        /// Updates an item.
        /// </summary>
        /// <param name="encryptedId">The encrypted ID of the item to update.</param>
        /// <param name="item">The new data for the item.</param>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <returns>The updated item.</returns>
        Task<ServiceResult<WiserItemModel>> UpdateAsync(string encryptedId, WiserItemModel item, ClaimsIdentity identity);

        /// <summary>
        /// Delete or undelete an item. Deleting an item will move it to an archive table, so it's never completely deleted by this method.
        /// Undeleting an item moves it from the archive table back to the actual table.
        /// </summary>
        /// <param name="encryptedId">The encrypted ID of the item to delete.</param>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="undelete">Optional: Whether to undelete the item instead of deleting it.</param>
        /// <param name="entityType">Optional: The entity type of the item to duplicate. This is needed when the item is saved in a different table than wiser_item. We can only look up the name of that table if we know the entity type beforehand.</param>
        Task<ServiceResult<bool>> DeleteAsync(string encryptedId, ClaimsIdentity identity, bool undelete = false, string entityType = null);

        /// <summary>
        /// Executes the workflow for an item. In wiser_entity you can set queries that need to be executed after an item has been created or updated.
        /// This method will execute these queries, based on what is being done with the item.
        /// </summary>
        /// <param name="encryptedId">The encrypted ID of the item that was recently created or updated.</param>
        /// <param name="isNewItem">Set to true if the item was just created, or false if it was updated.</param>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="item">Optional: The data of the item to execute the workflow for.</param>
        Task<ServiceResult<bool>> ExecuteWorkflowAsync(string encryptedId, bool isNewItem, ClaimsIdentity identity, WiserItemModel item = null);

        /// <summary>
        /// Get a query based on either a property ID or a query ID.
        /// If you enter a property ID, then the method will first check if there is an action_query (in wiser_entityproperty), if there isn't, it will return the data_query.
        /// If you enter a query ID, then the query with that ID from wiser_query will be returned.
        /// </summary>
        /// <param name="propertyId">The ID of the property from wiser_entityproperty. Set to 0 if you want to use a query ID.</param>
        /// <param name="queryId">The ID of the query from wiser_query. Set to 0 if you want to use a property ID.</param>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <returns>The query.</returns>
        Task<ServiceResult<string>> GetCustomQueryAsync(int propertyId, int queryId, ClaimsIdentity identity);

        /// <summary>
        /// Executes a custom query and return the results.
        /// This will call GetCustomQueryAsync and use that query.
        /// This will replace the details from an item in the query before executing it.
        /// </summary>
        /// <param name="encryptedId">The encrypted ID of the item to execute the query for.</param>
        /// <param name="propertyId">The ID of the property from wiser_entityproperty. Set to 0 if you want to use a query ID.</param>
        /// <param name="extraParameters">Any extra parameters to use in the query.</param>
        /// <param name="encryptedQueryId">The encrypted ID of the query from wiser_query. Encrypt the value "0" if you want to use a property ID.</param>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="itemLinkId">Optional: If the item is linked to something else and you need to know that in the query, enter the ID of that link from wiser_itemlink here.</param>
        /// <returns>The results of the query.</returns>
        Task<ServiceResult<ActionButtonResultModel>> ExecuteCustomQueryAsync(string encryptedId, int propertyId, Dictionary<string, object> extraParameters, string encryptedQueryId, ClaimsIdentity identity, ulong itemLinkId = 0);

        /// <summary>
        /// Get the HTML and javascript for single Wiser item, to show the item in Wiser.
        /// </summary>
        /// <param name="encryptedId">The encrypted ID of the item to get.</param>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="propertyIdSuffix">Optional: The suffix of every field on the item. This is used to give each field a unique ID, when multiple items are opened at the same time. Default value is <see langword="null"/>.</param>
        /// <param name="itemLinkId">Optional: The id of the item link from wiser_itemlink. This should be used when opening an item via a sub-entities-grid, to show link fields. Default value is 0.</param>
        /// <param name="entityType">Optional: The entity type of the item. This is needed when the item is saved in a different table than wiser_item. We can only look up the name of that table if we know the entity type beforehand.</param>
        /// <param name="linkType">Optional: The type number of the link, if this item also contains fields on a link.</param>
        /// <returns>A <see cref="ItemHtmlAndScriptModel"/> with the HTML and javascript needed to load this item in Wiser. This is needed when the link is saved in a different table than wiser_itemlink. We can only look up the name of that table if we know the link type beforehand.</returns>
        Task<ServiceResult<ItemHtmlAndScriptModel>> GetItemHtmlAsync(string encryptedId, ClaimsIdentity identity, string propertyIdSuffix = null, ulong itemLinkId = 0, string entityType = null, int linkType = 0);

        /// <summary>
        /// Gets a single item by its encrypted item ID.
        /// </summary>
        /// <param name="encryptedId">The encrypted ID of the item to get.</param>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="entityType">Optional: The entity type of the item to retrieve. This is needed when the item is saved in a different table than wiser_item. We can only look up the name of that table if we know the entity type beforehand.</param>
        /// <returns></returns>
        Task<ServiceResult<WiserItemModel>> GetItemDetailsAsync(string encryptedId, ClaimsIdentity identity, string entityType = null);

        /// <summary>
        /// Returns all items linked to a given item, or all items the given item is linked to.
        /// </summary>
        /// <param name="encryptedId">The encrypted ID of the item to get.</param>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="entityType">Optional: The entity type of the item to retrieve. This is needed when the item is saved in a different table than wiser_item. We can only look up the name of that table if we know the entity type beforehand.</param>
        /// <param name="itemIdEntityType">Optional: You can enter the entity type of the given itemId here, if you want to get items from a dedicated table and those items can have multiple different entity types. This only works if all those items exist in the same table. Default is null.</param>
        /// <param name="linkType">Optional: The type number of the link.</param>
        /// <param name="reversed">Optional: Whether to retrieve an item that is linked to this item (<see langword="true"/>), or an item that this item is linked to (<see langword="false"/>).</param>
        /// <returns></returns>
        Task<ServiceResult<List<WiserItemModel>>> GetLinkedItemDetailsAsync(string encryptedId, ClaimsIdentity identity, string entityType = null, string itemIdEntityType = null, int linkType = 0, bool reversed = false);

        /// <summary>
        /// Get the meta data of an item. This is data such as the title, entity type, last change date etc.
        /// </summary>
        /// <param name="encryptedId">The encrypted ID of the item.</param>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="entityType">Optional: The entity type of the item to duplicate. This is needed when the item is saved in a different table than wiser_item. We can only look up the name of that table if we know the entity type beforehand.</param>
        /// <returns>The item meta data.</returns>
        Task<ServiceResult<ItemMetaDataModel>> GetItemMetaDataAsync(string encryptedId, ClaimsIdentity identity, string entityType = null);

        /// <summary>
        /// Gets the query for a property of a Wiser item.
        /// </summary>
        /// <param name="propertyId">The ID of the property.</param>
        /// <param name="queryColumnName">The column name of the query to get (data_query, update_query etc).</param>
        /// <param name="alsoGetOptions">Whether to also get the options of the property.</param>
        /// <param name="itemId">Optional: If this query needs to be executed for a specific item, you can enter that ID here. If the query then contains the value "{itemId}", then that value will be replaced with this ID.</param>
        /// <typeparam name="T">The return type of the method you call this from.</typeparam>
        /// <returns>The query, any errors and the options (if alsoGetOptions is set to <see langword="true"/>).</returns>
        Task<(string Query, ServiceResult<T> ErrorResult, string RawOptions)> GetPropertyQueryAsync<T>(int propertyId, string queryColumnName, bool alsoGetOptions, ulong? itemId = null);

        /// <summary>
        /// Encrypt an ID of an item.
        /// </summary>
        /// <param name="itemId">The ID to encrypt.</param>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <returns>The encrypted ID.</returns>
        Task<ServiceResult<string>> GetEncryptedIdAsync(ulong itemId, ClaimsIdentity identity);

        /// <summary>
        /// Get the HTML for a specific Wiser item.
        /// In Wiser you can declare a default query and HTML template for entity types, to render a single item of that type on a website.
        /// This method will return that rendered HTML.
        /// </summary>
        /// <param name="itemId">The ID of the item to render to HTML.</param>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="entityType">Optional: The entity type of the item to duplicate. This is needed when the item is saved in a different table than wiser_item. We can only look up the name of that table if we know the entity type beforehand.</param>
        /// <returns>The rendered HTML for the item.</returns>
        Task<ServiceResult<string>> GetHtmlForWiserEntityAsync(ulong itemId, ClaimsIdentity identity, string entityType = null);

        /// <summary>
        /// Gets all entity types from an item ID.
        /// Wiser can have multiple wiser_item tables, with a prefix for certain entity types. This means an ID can exists multiple times.
        /// This method will get the different entity types with the given ID.
        /// </summary>
        /// <param name="itemId">The ID of the item to render to HTML.</param>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <returns>A list of all entity types that contain an item with this ID.</returns>
        Task<ServiceResult<List<EntityTypeModel>>> GetPossibleEntityTypesAsync(ulong itemId, ClaimsIdentity identity);

        /// <summary>
        /// Fixes the ordering of a tree view. This makes sure that all children of the given parent have ascending ordering numbers and no duplicate numbers.
        /// </summary>
        /// <param name="moduleId">The ID of the module.</param>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="encryptedParentId">Optional: The encrypted ID of the parent to fix the ordering for. If no value has been given, the root will be used as parent.</param>
        /// <param name="linkType">Optional: The link type number. Default value is 1.</param>
        Task<ServiceResult<bool>> FixTreeViewOrderingAsync(int moduleId, ClaimsIdentity identity, string encryptedParentId = null, int linkType = 1);

        /// <summary>
        /// Get all items for a tree view for a specific parent.
        /// </summary>
        /// <param name="moduleId">The ID of the module.</param>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="entityType">Optional: The entity type of the item to duplicate. This is needed when the item is saved in a different table than wiser_item. We can only look up the name of that table if we know the entity type beforehand.</param>
        /// <param name="encryptedParentId">Optional: The encrypted ID of the parent to fix the ordering for. If no value has been given, the root will be used as parent.</param>
        /// <param name="orderBy">Optional: Enter the value "item_title" to order by title, or nothing to order by order number.</param>
        /// <param name="encryptedCheckId">Optional: This is meant for item-linker fields. This is the encrypted ID for the item that should currently be checked.</param>
        /// <param name="linkType">Optional: The type number of the link. This is used in combination with "checkId"; So that items will only be marked as checked if they have the given link ID.</param>
        /// <returns>A list of <see cref="TreeViewItemModel"/>.</returns>
        Task<ServiceResult<List<TreeViewItemModel>>> GetItemsForTreeViewAsync(int moduleId, ClaimsIdentity identity, string entityType = null, string encryptedParentId = null, string orderBy = null, string encryptedCheckId = null, int linkType = 0);

        /// <summary>
        /// Move an item to a different position in the tree view.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="encryptedSourceId">The encrypted ID of the item that is being moved.</param>
        /// <param name="encryptedDestinationId">The encrypted ID of the item that it's being moved towards.</param>
        /// <param name="position">Shows where the source will be dropped. One of the values over, before, or after.</param>
        /// <param name="encryptedSourceParentId">The encrypted ID of the original parent of the item that is being moved.</param>
        /// <param name="encryptedDestinationParentId">The encrypted ID of the new parent that the item is being moved to.</param>
        /// <param name="sourceEntityType">The entity type of the item that is being moved.</param>
        /// <param name="destinationEntityType">The entity type of the item that it's being moved towards.</param>
        /// <param name="moduleId">The ID of the module.</param>
        Task<ServiceResult<bool>> MoveItemAsync(ClaimsIdentity identity, string encryptedSourceId, string encryptedDestinationId, string position, string encryptedSourceParentId, string encryptedDestinationParentId, string sourceEntityType, string destinationEntityType, int moduleId);

        /// <summary>
        /// Link one or more items to one or more other items.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="encryptedSourceIds">The encrypted IDs of the items that are being linked.</param>
        /// <param name="encryptedDestinationIds">The encrypted IDs of the destination items.</param>
        /// <param name="linkType">The link type to use for all of the links.</param>
        /// <param name="sourceEntityType">Optional: The entity type of the items that are being linked. This is needed when the item is saved in a different table than wiser_item. We can only look up the name of that table if we know the entity type beforehand.</param>
        Task<ServiceResult<bool>> AddMultipleLinksAsync(ClaimsIdentity identity, List<string> encryptedSourceIds, List<string> encryptedDestinationIds, int linkType, string sourceEntityType = null);

        /// <summary>
        /// Removed one or more links between items.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="encryptedSourceIds">The encrypted IDs of the source items of the links to remove.</param>
        /// <param name="encryptedDestinationIds">The encrypted IDs of the destination items.</param>
        /// <param name="linkType">The link type to use for all of the links.</param>
        /// <param name="sourceEntityType">Optional: The entity type of the source items. This is needed when the item is saved in a different table than wiser_item. We can only look up the name of that table if we know the entity type beforehand.</param>
        Task<ServiceResult<bool>> RemoveMultipleLinksAsync(ClaimsIdentity identity, List<string> encryptedSourceIds, List<string> encryptedDestinationIds, int linkType, string sourceEntityType = null);

        /// <summary>
        /// Translate all fields of an item into one or more other languages, using the Google Translation API.
        /// This will only translate fields that don't have a value yet for the destination language
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="encryptedId">The encrypted ID of the item to translate.</param>
        /// <param name="settings">The settings for translating.</param>
        Task<ServiceResult<bool>> TranslateAllFieldsAsync(ClaimsIdentity identity, string encryptedId, TranslateItemRequestModel settings);
    }
}
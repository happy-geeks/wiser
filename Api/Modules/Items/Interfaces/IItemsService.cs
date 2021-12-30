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
    // TODO: Add documentation.
    public interface IItemsService
    {
        Task<ServiceResult<PagedResults<FlatItemModel>>> GetItemsAsync(ClaimsIdentity identity, PagedRequest pagedRequest = null, WiserItemModel filters = null);

        Task<ServiceResult<WiserItemDuplicationResultModel>> DuplicateItemAsync(string encryptedId, string encryptedParentId, ClaimsIdentity identity, string entityType = null, string parentEntityType = null);
        
        Task<ServiceResult<WiserItemModel>> CopyToEnvironmentAsync(string encryptedId, Environments newEnvironments, ClaimsIdentity identity);
        
        Task<ServiceResult<CreateItemResultModel>> CreateAsync(WiserItemModel item, ClaimsIdentity identity, string encryptedParentId = null, int linkType = 1);
        
        Task<ServiceResult<WiserItemModel>> UpdateAsync(string encryptedId, WiserItemModel item, ClaimsIdentity identity);

        Task<ServiceResult<bool>> DeleteAsync(string encryptedId, ClaimsIdentity identity, bool undelete = false, string entityType = null);

        Task<ServiceResult<bool>> ExecuteWorkflowAsync(string encryptedId, bool isNewItem, ClaimsIdentity identity, WiserItemModel item = null);

        Task<ServiceResult<string>> GetCustomQueryAsync(int propertyId, int queryId, ClaimsIdentity identity);

        Task<ServiceResult<ActionButtonResultModel>> ExecuteCustomQueryAsync(string encryptedId, int propertyId, Dictionary<string, object> extraParameters, string encryptedQueryId, ClaimsIdentity identity, ulong itemLinkId = 0);
        
        /// <summary>
        /// Get the HTML and javascript for single Wiser item, to show the item in Wiser.
        /// </summary>
        /// <param name="encryptedId">The encrypted ID of the item to get.</param>
        /// <param name="propertyIdSuffix">Optional: The suffix of every field on the item. This is used to give each field a unique ID, when multiple items are opened at the same time. Default value is <see langword="null"/>.</param>
        /// <param name="itemLinkId">Optional: The id of the item link from wiser_itemlink. This should be used when opening an item via a sub-entities-grid, to show link fields. Default value is 0.</param>
        /// <param name="entityType">Optional: The entity type of the item. Default value is <see langword="null"/>.</param>
        /// <returns>A <see cref="ItemHtmlAndScriptModel"/> with the HTML and javascript needed to load this item in Wiser.</returns>
        Task<ServiceResult<ItemHtmlAndScriptModel>> GetItemHtmlAsync(string encryptedId, ClaimsIdentity identity, string propertyIdSuffix = null, ulong itemLinkId = 0, string entityType = null);

        Task<ServiceResult<ItemMetaDataModel>> GetItemMetaDataAsync(string encryptedId, ClaimsIdentity identity, string entityType = null);

        Task<(string Query, ServiceResult<T> ErrorResult, string RawOptions)> GetPropertyQueryAsync<T>(int propertyId, string queryColumnName, bool alsoGetOptions, ulong? itemId = null);
        
        Task<ServiceResult<string>> GetEncryptedIdAsync(ulong itemId, ClaimsIdentity identity);

        Task<ServiceResult<string>> GetHtmlForWiser2EntityAsync(ulong itemId, ClaimsIdentity dummyClaimsIdentity, string entityType = null);

        Task<ServiceResult<List<EntityTypeModel>>> GetPossibleEntityTypesAsync(ulong itemId, ClaimsIdentity identity);

        Task<ServiceResult<bool>> FixTreeViewOrderingAsync(int moduleId, ClaimsIdentity identity, string entityType = null, string encryptedParentId = null, string orderBy = null, string encryptedCheckId = null);

        Task<ServiceResult<List<TreeViewItemModel>>> GetItemsForTreeViewAsync(int moduleId, ClaimsIdentity identity, string entityType = null, string encryptedParentId = null, string orderBy = null, string encryptedCheckId = null);

        Task<ServiceResult<bool>> MoveItemAsync(ClaimsIdentity identity, string encryptedSourceId, string encryptedDestinationId, string position, string encryptedSourceParentId, string encryptedDestinationParentId, string sourceEntityType, string destinationEntityType, int moduleId);

        Task<ServiceResult<bool>> AddMultipleLinksAsync(ClaimsIdentity identity, List<string> encryptedSourceIds, List<string> encryptedDestinationIds, int linkType, string sourceEntityType = null);

        Task<ServiceResult<bool>> RemoveMultipleLinksAsync(ClaimsIdentity identity, List<string> encryptedSourceIds, List<string> encryptedDestinationIds, int linkType, string sourceEntityType = null);
    }
}

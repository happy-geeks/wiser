using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.EntityTypes.Models;
using GeeksCoreLibrary.Core.Models;

namespace Api.Modules.EntityTypes.Interfaces
{
    /// <summary>
    /// Service for getting settings for entity types.
    /// </summary>
    public interface IEntityTypesService
    {
        /// <summary>
        /// Gets all available entity types.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="onlyEntityTypesWithDisplayName">Optional: Set to <see langword="false"/> to get all entity types, or <see langword="true"/> to get only entity types that have a display name. Default value is <see langword="true"/>.</param>
        /// <param name="includeCount">Optional: Whether to count how many items of each entity type exist in the database.</param>
        /// <param name="skipEntitiesWithoutItems">Optional: Whether to skip entities that have no items. Only works when includeCount is set to <see langword="true" />.</param>
        /// <param name="moduleId">Optional: If you only want entity types from a specific module, enter the ID of that module here.</param>
        /// <param name="branchId">Optional: If getting entity types for a branch, enter the ID of that branch here.</param>
        /// <returns>The list of entity types.</returns>
        Task<ServiceResult<List<EntityTypeModel>>> GetAsync(ClaimsIdentity identity, bool onlyEntityTypesWithDisplayName = true, bool includeCount = false, bool skipEntitiesWithoutItems = false, int moduleId = 0, int branchId = 0);

        /// <summary>
        /// Gets the settings for an entity type. These settings will be cached for 1 hour.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="entityType">The name of the entity type.</param>
        /// <param name="moduleId">Optional: The ID of the module, in case the entity type has different settings for different modules.</param>
        /// <returns>A <see cref="EntitySettingsModel"/> containing all settings of the entity type.</returns>
        Task<ServiceResult<EntitySettingsModel>> GetAsync(ClaimsIdentity identity, string entityType, int moduleId = 0);

        /// <summary>
        /// Gets the settings for an entity type. These settings will be cached for 1 hour.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="id">The ID of the entity type.</param>
        /// <returns>A <see cref="EntitySettingsModel"/> containing all settings of the entity type.</returns>
        Task<ServiceResult<EntitySettingsModel>> GetAsync(ClaimsIdentity identity, int id);

        /// <summary>
        /// Gets all available entity types, based on module id and parent id.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="moduleId">The ID of the module.</param>
        /// <param name="parentId">Optional: The ID of the parent. Set to 0 or skip to use the root.</param>
        /// <returns>A list of available entity names.</returns>
        Task<ServiceResult<List<EntityTypeModel>>> GetAvailableEntityTypesAsync(ClaimsIdentity identity, int moduleId, string parentId = null);

        /// <summary>
        /// Creates a new entity type.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="name">The name of the new entity type.</param>
        /// <param name="moduleId">The module ID the new entity type is linked to.</param>
        /// <returns>The ID of the new entity type.</returns>
        Task<ServiceResult<long>> CreateAsync(ClaimsIdentity identity, string name, int moduleId = 0);

        /// <summary>
        /// Creates a new entity type.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="id">The ID of the entity type.</param>
        /// <param name="settings">The settings to save.</param>
        Task<ServiceResult<bool>> UpdateAsync(ClaimsIdentity identity, int id, EntitySettingsModel settings);

        /// <summary>
        /// Deletes an entity type. This will only delete the entity type itself, not any items that use this type.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="id">The ID of the entity type.</param>
        Task<ServiceResult<bool>> DeleteAsync(ClaimsIdentity identity, int id);

        /// <summary>
        /// Gets the ID of an API connection (from wiser_api_connection) for a specific entity and action.
        /// </summary>
        /// <param name="entityType">The name of the entity type to get the API connection ID for.</param>
        /// <param name="actionType">The action type, this can be "after_insert", "after_update", "before_update" or "before_delete".</param>
        /// <returns></returns>
        Task<ServiceResult<int>> GetApiConnectionIdAsync(string entityType, string actionType);
    }
}
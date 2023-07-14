using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.EntityProperties.Enums;
using Api.Modules.EntityProperties.Models;

namespace Api.Modules.EntityProperties.Interfaces
{
    /// <summary>
    /// Interface for all CRUD operations for entity properties (from the table wiser_entityproperty).
    /// </summary>
    public interface IEntityPropertiesService
    {
        /// <summary>
        /// Get all entity properties.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <returns>A List of <see cref="EntityPropertyModel"/> with all settings.</returns>
        Task<ServiceResult<List<EntityPropertyModel>>> GetAsync(ClaimsIdentity identity);

        /// <summary>
        /// Get entity property based on ID.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="id">The ID of the settings from wiser_entityproperty.</param>
        /// <returns>An <see cref="EntityPropertyModel"/> with all settings.</returns>
        Task<ServiceResult<EntityPropertyModel>> GetAsync(ClaimsIdentity identity, int id);

        /// <summary>
        /// Get all entity properties of a specific entity.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="entityType">The name of the entity.</param>
        /// <param name="onlyEntityTypesWithDisplayName">Only get properties with a display name.</param>
        /// <param name="onlyEntityTypesWithPropertyName">Only get properties with a property name.</param>
        /// <param name="addIdProperty">Add a property for the id.</param>
        /// <param name="orderByName">Optional: Whether to order by name (true) or by order number (false). Default value is true.</param>
        /// <returns>A <see cref="List{EntityPropertyModel}"/> with all properties of a specific entity.</returns>
        Task<ServiceResult<List<EntityPropertyModel>>> GetPropertiesOfEntityAsync(ClaimsIdentity identity, string entityType, bool onlyEntityTypesWithDisplayName = true, bool onlyEntityTypesWithPropertyName = true, bool addIdProperty = false, bool orderByName = true);

        /// <summary>
        /// Get all entity properties of a specific entity, grouped by tab name.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="entityName">The name of the entity.</param>
        /// <returns>A <see cref="List{EntityPropertyTabModel}"/> with all tabs of a specific entity. Each tab contains all fields of that tab.</returns>
        Task<ServiceResult<List<EntityPropertyTabModel>>> GetPropertiesOfEntityGroupedByTabAsync(ClaimsIdentity identity, string entityName);

        /// <summary>
        /// Creates a new entity property.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="entityProperty">The entity property to create.</param>
        /// <returns>The newly created link settings.</returns>
        Task<ServiceResult<EntityPropertyModel>> CreateAsync(ClaimsIdentity identity, EntityPropertyModel entityProperty);

        /// <summary>
        /// Updates an existing entity property.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="id">The ID of the entity property to update.</param>
        /// <param name="entityProperty">The new data to save.</param>
        Task<ServiceResult<bool>> UpdateAsync(ClaimsIdentity identity, int id, EntityPropertyModel entityProperty);

        /// <summary>
        /// Duplicates an entity property.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="id">The ID of the entity property to duplicate.</param>
        /// <param name="newName">The name for the new entity property.</param>
        Task<ServiceResult<int>> DuplicateAsync(ClaimsIdentity identity, int id, string newName);

        /// <summary>
        /// Deletes an entity property.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="id">The ID of the entity property to delete.</param>
        Task<ServiceResult<bool>> DeleteAsync(ClaimsIdentity identity, int id);

        /// <summary>
        /// Copies an entity property to all other available languages that don't have this property yet.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="id">The ID of the entity property to copy.</param>
        /// <param name="tabOption">The tab to add the new fields too.</param>
        Task<ServiceResult<bool>> CopyToAllAvailableLanguagesAsync(ClaimsIdentity identity, int id, CopyToOtherLanguagesTabOptions tabOption);

        /// <summary>
        /// Fixes the ordering of all fields for a specific entity type or link type, so that all fields have consecutive order numbers.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="entityType">Optional: The entity type to fix the ordering for. Leave empty if you want to do it for link fields instead of entity fields.</param>
        /// <param name="linkType">Optional: The link type to fix the ordering for. Leave empty if you want to do it for entity fields instead of link fields.</param>
        Task<ServiceResult<bool>> FixOrderingAsync(ClaimsIdentity identity, string entityType = null, int linkType = 0);

        /// <summary>
        /// Gets all unique values for a specific property of a specific entity type.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="entityType">The entity type that the property belongs to.</param>
        /// <param name="propertyName">The name (key) of the property.</param>
        /// <param name="languageCode">Optional: Enter a language code here if you only want values of a specific language.</param>
        /// <param name="maxResults">The maximum amount of results to return, default is 500.</param>
        /// <returns>A list with unique values of the property.</returns>
        Task<ServiceResult<List<string>>> GetUniquePropertyValuesAsync(ClaimsIdentity identity, string entityType, string propertyName, string languageCode = null, int maxResults = 500);

        /// <summary>
        /// Move an entity property to a new position.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="id">The ID of the entity property</param>
        /// <param name="data">Data required to do the move.</param>
        Task<ServiceResult<bool>> MovePropertyAsync(ClaimsIdentity identity, int id, MoveEntityPropertyRequestModel data);

        /// <summary>
        /// Move an entity tab with all it's properties to a new position.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="data">Data required to do the move.</param>
        Task<ServiceResult<bool>> MoveTabAsync(ClaimsIdentity identity, MoveEntityTabRequestModel data);
    }
}
using Api.Core.Services;
using Api.Modules.EntityProperties.Models;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Modules.EntityProperties.Enums;

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
        /// <param name="entityName">The name of the entity.</param>
        /// <param name="onlyEntityTypesWithDisplayName">Only get properties with a display name.</param>
        /// <param name="onlyEntityTypesWithPropertyName">Only get properties with a property name.</param>
        /// <param name="addIdProperty">Add a property for the id.</param>
        /// <returns>A <see cref="List{EntityPropertyModel}"/> with all properties of a specific entity.</returns>
        Task<ServiceResult<List<EntityPropertyModel>>> GetPropertiesOfEntityAsync(ClaimsIdentity identity, string entityName, bool onlyEntityTypesWithDisplayName = true, bool onlyEntityTypesWithPropertyName = true, bool addIdProperty = false);

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
    }
}

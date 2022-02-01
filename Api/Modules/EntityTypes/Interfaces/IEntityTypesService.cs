using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.EntityTypes.Models;

namespace Api.Modules.EntityTypes.Interfaces
{
    public interface IEntityTypesService
    {
        /// <summary>
        /// Gets all available entity types.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="onlyEntityTypesWithDisplayName">Optional: Set to <see langword="false"/> to get all entity types, or <see langword="true"/> to get only entity types that have a display name. Default value is <see langword="true"/>.</param>
        /// <returns></returns>
        Task<ServiceResult<List<EntityTypeModel>>> GetAsync(ClaimsIdentity identity, bool onlyEntityTypesWithDisplayName = true);

        /// <summary>
        /// Gets entity type by id
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.<</param>
        /// <param name="id">The technical id of the entity type</param>
        /// <param name="moduleId">Module id of the entity type</param>
        /// <returns></returns>
        Task<ServiceResult<EntityTypeModel>> GetAsync(ClaimsIdentity identity, string id, int moduleId);

        /// <summary>
        /// Gets all available entity types, based on module id and parent id.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="moduleId">The ID of the module.</param>
        /// <param name="parentId">Optional: The ID of the parent. Set to 0 or skip to use the root.</param>
        /// <returns>A list of available entity names.</returns>
        Task<ServiceResult<List<string>>> GetAvailableEntityTypesAsync(ClaimsIdentity identity, int moduleId, string parentId = null);

        /// <summary>
        /// Update entity type
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="id">The id of the entity</param>
        /// <param name="entityTypeModel">The entity with new values</param>
        /// <returns>Boolean if update was successful or not</returns>
        Task<ServiceResult<bool>> UpdateAsync(ClaimsIdentity identity, string id, EntityTypeModel entityTypeModel);
    }
}

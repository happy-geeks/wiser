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
        /// <returns></returns>
        Task<ServiceResult<List<EntityTypeModel>>> GetAsync(ClaimsIdentity identity, bool onlyEntityTypesWithDisplayName = true);

        /// <summary>
        /// Gets the settings for an entity type. These settings will be cached for 1 hour.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="entityType">The name of the entity type.</param>
        /// <param name="moduleId">Optional: The ID of the module, in case the entity type has different settings for different modules.</param>
        /// <returns>A <see cref="EntitySettingsModel"/> containing all settings of the entity type.</returns>
        Task<ServiceResult<EntitySettingsModel>> GetAsync(ClaimsIdentity identity, string entityType, int moduleId = 0);

        /// <summary>
        /// Gets all available entity types, based on module id and parent id.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="moduleId">The ID of the module.</param>
        /// <param name="parentId">Optional: The ID of the parent. Set to 0 or skip to use the root.</param>
        /// <returns>A list of available entity names.</returns>
        Task<ServiceResult<List<EntityTypeModel>>> GetAvailableEntityTypesAsync(ClaimsIdentity identity, int moduleId, string parentId = null);
    }
}

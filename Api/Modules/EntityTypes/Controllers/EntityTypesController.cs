using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Modules.EntityTypes.Interfaces;
using Api.Modules.EntityTypes.Models;
using GeeksCoreLibrary.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Api.Modules.EntityTypes.Controllers
{
    /// <summary>
    /// Controller for doing things with Wiser entity types.
    /// An item in Wiser can have different entity types. This entity type decides what kind of item it is (order, basket, product, customer etc) and which fields will be available when opening the item in Wiser.
    /// </summary>
    [Route("api/v3/entity-types"), ApiController, Authorize]
    public class EntityTypesController : ControllerBase
    {
        private readonly IEntityTypesService entityTypesService;
        
        /// <summary>
        /// Creates a new instance of <see cref="EntityTypesController"/>.
        /// </summary>
        /// <param name="entityTypesService"></param>
        public EntityTypesController(IEntityTypesService entityTypesService)
        {
            this.entityTypesService = entityTypesService;
        }

        /// <summary>
        /// Gets all available entity types.
        /// </summary>
        /// <param name="onlyEntityTypesWithDisplayName">Optional: Set to <see langword="false"/> to get all entity types, or <see langword="true"/> to get only entity types that have a display name.</param>
        /// <returns>A list of all available entity types.</returns>
        [HttpGet, ProducesResponseType(typeof(List<EntityTypeModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAsync(bool onlyEntityTypesWithDisplayName = false)
        {
            return (await entityTypesService.GetAsync((ClaimsIdentity)User.Identity, onlyEntityTypesWithDisplayName)).GetHttpResponseMessage();
        }
        
        /// <summary>
        /// Gets the settings for an entity type. These settings will be cached for 1 hour.
        /// </summary>
        /// <param name="entityType">The name of the entity type.</param>
        /// <param name="moduleId">Optional: The ID of the module, in case the entity type has different settings for different modules.</param>
        /// <returns>A <see cref="EntitySettingsModel"/> containing all settings of the entity type.</returns>
        [HttpGet, Route("{entityType}"), ProducesResponseType(typeof(EntitySettingsModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAsync(string entityType, int moduleId = 0)
        {
            return (await entityTypesService.GetAsync((ClaimsIdentity)User.Identity, entityType, moduleId)).GetHttpResponseMessage();
        }
        
        /// <summary>
        /// Gets all available entity types, based on module id and parent id.
        /// </summary>
        /// <param name="moduleId">The ID of the module.</param>
        /// <param name="parentId">Optional: The ID of the parent. Set to 0 or skip to use the root.</param>
        /// <returns>A list of available entity names.</returns>
        [HttpGet, Route("{moduleId:int}"), ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAvailableEntityTypesAsync(int moduleId, string parentId = null)
        {
            return (await entityTypesService.GetAvailableEntityTypesAsync((ClaimsIdentity)User.Identity, moduleId, parentId)).GetHttpResponseMessage();
        }
    }
}

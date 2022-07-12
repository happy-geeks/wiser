using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Net.Mime;
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
    [Route("api/v3/entity-types")]
    [ApiController]
    [Authorize]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
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
        /// <param name="onlyEntityTypesWithDisplayName">Optional: Set to false to get all entity types, or true to get only entity types that have a display name.</param>
        /// <param name="includeCount">Optional: Whether to count how many items of each entity type exist in the database.</param>
        /// <param name="skipEntitiesWithoutItems">Optional: Whether to skip entities that have no items. Only works when includeCount is set to true.</param>
        /// <returns>A list of all available entity types.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(List<EntityTypeModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAsync(bool onlyEntityTypesWithDisplayName = false, bool includeCount = false, bool skipEntitiesWithoutItems = false)
        {
            return (await entityTypesService.GetAsync((ClaimsIdentity)User.Identity, onlyEntityTypesWithDisplayName, includeCount, skipEntitiesWithoutItems)).GetHttpResponseMessage();
        }
        
        /// <summary>
        /// Gets the settings for an entity type.
        /// </summary>
        /// <param name="entityType">The name of the entity type.</param>
        /// <param name="moduleId">Optional: The ID of the module, in case the entity type has different settings for different modules.</param>
        /// <returns>A <see cref="EntitySettingsModel"/> containing all settings of the entity type.</returns>
        [HttpGet]
        [Route("{entityType}")]
        [ProducesResponseType(typeof(EntitySettingsModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAsync(string entityType, int moduleId = 0)
        {
            return (await entityTypesService.GetAsync((ClaimsIdentity)User.Identity, entityType, moduleId)).GetHttpResponseMessage();
        }
        
        /// <summary>
        /// Gets the settings for an entity type.
        /// </summary>
        /// <param name="id">The id the entity type.</param>
        /// <returns>A <see cref="EntitySettingsModel"/> containing all settings of the entity type.</returns>
        [HttpGet]
        [Route("id/{id:int}")]
        [ProducesResponseType(typeof(EntitySettingsModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetViaIdAsync(int id)
        {
            return (await entityTypesService.GetAsync((ClaimsIdentity)User.Identity, id)).GetHttpResponseMessage();
        }
        
        /// <summary>
        /// Gets all available entity types, based on module id and parent id.
        /// </summary>
        /// <param name="moduleId">The ID of the module.</param>
        /// <param name="parentId">Optional: The ID of the parent. Set to 0 or skip to use the root.</param>
        /// <returns>A list of available entity names.</returns>
        [HttpGet]
        [Route("{moduleId:int}")]
        [ProducesResponseType(typeof(List<EntityTypeModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAvailableEntityTypesAsync(int moduleId, string parentId = null)
        {
            return (await entityTypesService.GetAvailableEntityTypesAsync((ClaimsIdentity)User.Identity, moduleId, parentId)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Creates a new entity type.
        /// </summary>
        /// <param name="name">The name of the new entity type.</param>
        /// <param name="moduleId">The module ID the new entity type is connected to.</param>
        /// <returns>The ID of the new entity type.</returns>
        [HttpPost]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateAsync([FromQuery]string name, [FromQuery]int moduleId = 0)
        {
            return (await entityTypesService.CreateAsync((ClaimsIdentity)User.Identity, name, moduleId)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Creates a new entity type.
        /// </summary>
        /// <param name="id">The ID of the entity type.</param>
        /// <param name="settings">The settings to save.</param>
        [HttpPut]
        [Route("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateAsync(int id, EntitySettingsModel settings)
        {
            return (await entityTypesService.UpdateAsync((ClaimsIdentity)User.Identity, id, settings)).GetHttpResponseMessage();
        }
    }
}

using System.Collections.Generic;
using System.Net.Mime;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Modules.EntityTypes.Interfaces;
using Api.Modules.EntityTypes.Models;
using GeeksCoreLibrary.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.EntityTypes.Controllers
{
    /// <summary>
    /// Controller for doing things with Wiser entity types.
    /// An item in Wiser can have different entity types. This entity type decides what kind of item it is (order, basket, product, tenant etc) and which fields will be available when opening the item in Wiser.
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
        /// <param name="moduleId">Optional: If you only want entity types from a specific module, enter the ID of that module here.</param>
        /// <param name="branchId">Optional: If getting entity types for a branch, enter the ID of that branch here.</param>
        /// <returns>A list of all available entity types.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(List<EntityTypeModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAsync(bool onlyEntityTypesWithDisplayName = false, bool includeCount = false, bool skipEntitiesWithoutItems = false, int moduleId = 0, int branchId = 0)
        {
            return (await entityTypesService.GetAsync((ClaimsIdentity)User.Identity, onlyEntityTypesWithDisplayName, includeCount, skipEntitiesWithoutItems, moduleId, branchId)).GetHttpResponseMessage();
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
        /// <param name="parentEntityType">Optional: The entityType of the parent. Defaults to the root.</param>
        /// <returns>A list of available entity names.</returns>
        [HttpGet]
        [Route("{moduleId:int}")]
        [ProducesResponseType(typeof(List<EntityTypeModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAvailableEntityTypesAsync(int moduleId, string parentId = null, string parentEntityType = "")
        {
            return (await entityTypesService.GetAvailableEntityTypesAsync((ClaimsIdentity)User.Identity, moduleId, parentId, parentEntityType)).GetHttpResponseMessage();
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
        /// Updates an existing entity type.
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

        /// <summary>
        /// Deletes an entity type. This will only delete the entity type itself, not any items that use this type.
        /// </summary>
        /// <param name="id">The ID of the entity type.</param>
        [HttpDelete]
        [Route("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DeleteAsync(int id)
        {
            return (await entityTypesService.DeleteAsync((ClaimsIdentity)User.Identity, id)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Gets the ID of an API connection (from wiser_api_connection) for a specific entity and action.
        /// </summary>
        /// <param name="entityType">The name of the entity type to get the API connection ID for.</param>
        /// <param name="actionType">The action type, this can be "after_insert", "after_update", "before_update" or "before_delete".</param>
        [HttpGet]
        [Route("{entityType}/api-connection/{actionType}")]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetApiConnectionIdAsync(string entityType, string actionType)
        {
            return (await entityTypesService.GetApiConnectionIdAsync(entityType, actionType)).GetHttpResponseMessage();
        }
    }
}
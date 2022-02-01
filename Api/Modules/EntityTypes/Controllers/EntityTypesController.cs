using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Modules.EntityTypes.Interfaces;
using Api.Modules.EntityTypes.Models;
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
        public async Task<IActionResult> Get(bool onlyEntityTypesWithDisplayName = false)
        {
            return (await entityTypesService.GetAsync((ClaimsIdentity)User.Identity, onlyEntityTypesWithDisplayName)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Gets entity type by id and moduleId.
        /// </summary>
        /// <returns>A list of all available entity types.</returns>
        [HttpGet, Route("{id}/{moduleId:int}"), ProducesResponseType(typeof(EntityTypeModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> Get(string id, int moduleId)
        {
            return (await entityTypesService.GetAsync((ClaimsIdentity)User.Identity, id, moduleId)).GetHttpResponseMessage();
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

        /// <summary>
        /// Updates existing entity type
        /// </summary>
        /// <param name="id">The technical ID of the entity type</param>
        /// <param name="entityTypeModel">The new entity data to save.</param>
        [HttpPut, Route("{id}")]
        public async Task<IActionResult> Update(string id, EntityTypeModel entityTypeModel)
        {
            return (await entityTypesService.UpdateAsync((ClaimsIdentity)User.Identity, id, entityTypeModel)).GetHttpResponseMessage();
        }
    }
}

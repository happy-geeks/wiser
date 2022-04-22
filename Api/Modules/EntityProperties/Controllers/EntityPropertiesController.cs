using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Api.Modules.EntityProperties.Models;
using Api.Modules.EntityProperties.Interfaces;

namespace Api.Modules.EntityProperties.Controllers
{
    /// <summary>
    /// Controller for all CRUD functions for entity properties.
    /// </summary>
    [Route("api/v3/entity-properties")]
    [ApiController]
    [Authorize]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    public class EntityPropertiesController : ControllerBase
    {
        private readonly IEntityPropertiesService entityPropertiesService;

        /// <summary>
        /// Creates a new instance of <see cref="EntityPropertiesController"/>.
        /// </summary>
        /// <param name="entityPropertiesService"></param>
        public EntityPropertiesController(IEntityPropertiesService entityPropertiesService)
        {
            this.entityPropertiesService = entityPropertiesService;
        }

        /// <summary>
        /// Get all entity properties. 
        /// </summary>
        /// <returns>A List of <see cref="EntityPropertyModel"/> with all settings.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(List<EntityPropertyModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Get()
        {
            return (await entityPropertiesService.GetAsync((ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Get entity property based on ID. 
        /// </summary>
        /// <param name="id">The ID from wiser_entityproperty.</param>
        /// <returns>A <see cref="EntityPropertyModel"/> with all settings.</returns>
        [HttpGet]
        [Route("{id:int}")]
        [ProducesResponseType(typeof(EntityPropertyModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Get(int id)
        {
            return (await entityPropertiesService.GetAsync((ClaimsIdentity)User.Identity, id)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Get all entity properties of a specific entity.
        /// </summary>
        /// <param name="entityName">The name of the entity.</param>
        /// <param name="onlyEntityTypesWithDisplayName">Only get properties with a display name.</param>
        /// <param name="onlyEntityTypesWithPropertyName">Only get properties with a property name.</param>
        /// <param name="addIdProperty">Add a property for the id.</param>
        /// <returns>A <see cref="List{EntityPropertyModel}"/> with all properties of a specific entity.</returns>
        [HttpGet]
        [Route("{entityName}")]
        [ProducesResponseType(typeof(List<EntityPropertyModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPropertiesOfEntity(string entityName, [FromQuery] bool onlyEntityTypesWithDisplayName, [FromQuery] bool onlyEntityTypesWithPropertyName, [FromQuery] bool addIdProperty = false)
        {
            return (await entityPropertiesService.GetPropertiesOfEntityAsync((ClaimsIdentity)User.Identity, entityName, onlyEntityTypesWithDisplayName, onlyEntityTypesWithPropertyName, addIdProperty)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Creates a new entity property.
        /// </summary>
        /// <param name="entityProperty">The entity property to create.</param>
        /// <returns>The newly created entity property.</returns>
        [HttpPost]
        [ProducesResponseType(typeof(EntityPropertyModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create(EntityPropertyModel entityProperty)
        {
            return (await entityPropertiesService.CreateAsync((ClaimsIdentity)User.Identity, entityProperty)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Updates an existing entity property.
        /// </summary>
        /// <param name="id">The ID from of the entity property.</param>
        /// <param name="entityProperty">The new data to save.</param>
        [HttpPut]
        [Route("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Update(int id, EntityPropertyModel entityProperty)
        {
            return (await entityPropertiesService.UpdateAsync((ClaimsIdentity)User.Identity, id, entityProperty)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Deletes an entity property.
        /// </summary>
        /// <param name="id">The ID of the entity property to delete.</param>
        [HttpDelete]
        [Route("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Delete(int id)
        {
            return (await entityPropertiesService.DeleteAsync((ClaimsIdentity)User.Identity, id)).GetHttpResponseMessage();
        }
    }
}

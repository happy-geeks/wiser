using System.Collections.Generic;
using System.Net.Mime;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Modules.EntityProperties.Enums;
using Api.Modules.EntityProperties.Interfaces;
using Api.Modules.EntityProperties.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
        public async Task<IActionResult> GetAsync()
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
        public async Task<IActionResult> GetAsync(int id)
        {
            return (await entityPropertiesService.GetAsync((ClaimsIdentity)User.Identity, id)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Get all entity properties of a specific entity.
        /// </summary>
        /// <param name="entityType">The name of the entity.</param>
        /// <param name="onlyEntityTypesWithDisplayName">Only get properties with a display name.</param>
        /// <param name="onlyEntityTypesWithPropertyName">Only get properties with a property name.</param>
        /// <param name="addIdProperty">Add a property for the id.</param>
        /// <param name="orderByName">Optional: Whether to order by name (true) or by order number (false). Default value is true.</param>
        /// <returns>A <see cref="List{EntityPropertyModel}"/> with all properties of a specific entity.</returns>
        [HttpGet]
        [Route("{entityType}")]
        [ProducesResponseType(typeof(List<EntityPropertyModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPropertiesOfEntityAsync(string entityType, [FromQuery] bool onlyEntityTypesWithDisplayName, [FromQuery] bool onlyEntityTypesWithPropertyName, [FromQuery] bool addIdProperty = false, [FromQuery] bool orderByName = true)
        {
            return (await entityPropertiesService.GetPropertiesOfEntityAsync((ClaimsIdentity)User.Identity, entityType, onlyEntityTypesWithDisplayName, onlyEntityTypesWithPropertyName, addIdProperty, orderByName)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Get all entity properties of a specific entity, grouped by tab name.
        /// </summary>
        /// <param name="entityName">The name of the entity.</param>
        /// <returns>A <see cref="List{EntityPropertyTabModel}"/> with all tabs of a specific entity. Each tab contains all fields of that tab.</returns>
        [HttpGet]
        [Route("{entityName}/grouped-by-tab")]
        [ProducesResponseType(typeof(List<EntityPropertyModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPropertiesOfEntityGroupedByTabAsync(string entityName)
        {
            return (await entityPropertiesService.GetPropertiesOfEntityGroupedByTabAsync((ClaimsIdentity)User.Identity, entityName)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Gets all unique values for a specific property of a specific entity type.
        /// </summary>
        /// <param name="entityType">The entity type that the property belongs to.</param>
        /// <param name="propertyName">The name (key) of the property.</param>
        /// <param name="languageCode">Optional: Enter a language code here if you only want values of a specific language.</param>
        /// <param name="maxResults">The maximum amount of results to return, default is 500.</param>
        /// <returns>A list with unique values of the property.</returns>
        [HttpGet]
        [Route("{entityType}/unique-values/{propertyName}")]
        [ProducesResponseType(typeof(List<EntityPropertyModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUniquePropertyValuesAsync(string entityType, string propertyName, string languageCode = null, int maxResults = 500)
        {
            return (await entityPropertiesService.GetUniquePropertyValuesAsync((ClaimsIdentity)User.Identity, entityType, propertyName, languageCode, maxResults)).GetHttpResponseMessage();
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
        public async Task<IActionResult> CreateAsync(EntityPropertyModel entityProperty)
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
        public async Task<IActionResult> UpdateAsync(int id, EntityPropertyModel entityProperty)
        {
            return (await entityPropertiesService.UpdateAsync((ClaimsIdentity)User.Identity, id, entityProperty)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Duplicates an entity property.
        /// </summary>
        /// <param name="id">The ID of the entity property to duplicate.</param>
        /// <param name="newName">The name for the new entity property.</param>
        [HttpPost]
        [Route("{id:int}/duplicate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> DuplicateAsync(int id, [FromBody]string newName)
        {
            return (await entityPropertiesService.DuplicateAsync((ClaimsIdentity)User.Identity, id, newName)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Deletes an entity property.
        /// </summary>
        /// <param name="id">The ID of the entity property to delete.</param>
        [HttpDelete]
        [Route("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DeleteAsync(int id)
        {
            return (await entityPropertiesService.DeleteAsync((ClaimsIdentity)User.Identity, id)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Copies an entity property to all other available languages that don't have this property yet.
        /// </summary>
        /// <param name="id">The ID of the entity property to copy.</param>
        /// <param name="tabOption">The tab to add the new fields too.</param>
        [HttpPost]
        [Route("{id:int}/copy-to-other-languages")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> CopyToAllAvailableLanguagesAsync(int id, [FromQuery]CopyToOtherLanguagesTabOptions tabOption)
        {
            return (await entityPropertiesService.CopyToAllAvailableLanguagesAsync((ClaimsIdentity)User.Identity, id, tabOption)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Move an entity property to a new position.
        /// </summary>
        /// <param name="id">The ID of the entity property</param>
        /// <param name="data">Data required to do the move.</param>
        [HttpPut]
        [Route("{id}/move")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> MovePropertyAsync(int id, MoveEntityPropertyRequestModel data)
        {
            return (await entityPropertiesService.MovePropertyAsync((ClaimsIdentity)User.Identity, id, data)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Move an entity tab with all it's properties to a new position.
        /// </summary>
        /// <param name="data">Data required to do the move.</param>
        [HttpPut]
        [Route("move-tab")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> MoveTabAsync(MoveEntityTabRequestModel data)
        {
            return (await entityPropertiesService.MoveTabAsync((ClaimsIdentity)User.Identity, data)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Fixes the ordering of all fields for a specific entity type, so that all fields have consecutive order numbers.
        /// </summary>
        /// <param name="entityType">The entity type to fix the ordering for.</param>
        [HttpPut]
        [Route("{entityType}/fix-ordering")]
        [ProducesResponseType(typeof(List<EntityPropertyModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> FixOrderingAsync(string entityType)
        {
            return (await entityPropertiesService.FixOrderingAsync((ClaimsIdentity)User.Identity, entityType)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Fixes the ordering of all fields for a specific link type, so that all fields have consecutive order numbers.
        /// </summary>
        /// <param name="linkType">The link type to fix the ordering for.</param>
        [HttpPut]
        [Route("{linkType:int}/fix-ordering")]
        [ProducesResponseType(typeof(List<EntityPropertyModel>), StatusCodes.Status204NoContent)]
        public async Task<IActionResult> FixOrderingAsync(int linkType)
        {
            return (await entityPropertiesService.FixOrderingAsync((ClaimsIdentity)User.Identity, linkType: linkType)).GetHttpResponseMessage();
        }
    }
}
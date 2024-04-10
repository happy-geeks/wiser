using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Api.Modules.StyledOutput.Models;
using Api.Modules.Queries.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Api.Modules.Queries.Controllers
{
    /// <summary>
    /// Controller for all CRUD functions for wiser query.
    /// </summary>
    [Route("api/v3/[controller]")]
    [ApiController]
    [Authorize]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    public class StyledOutputController : ControllerBase
    {
        private readonly IStyledOutputService styledOutputService;

        /// <summary>
        /// Creates a new instance of <see cref="StyledOutputController"/>.
        /// </summary>
        public StyledOutputController(IStyledOutputService styledOutputService)
        {
            this.styledOutputService = styledOutputService;
        }
        
        /// <summary>
        /// Find a styled output in the wiser_styled_output table and returns the result
        /// </summary>
        /// <param name="id">The ID from wiser_styled_output.</param>
        /// <param name="parameters">The parameters to set before executing the styled output.</param>
        /// <param name="stripNewlinesAndTabs">replaces \r\n \n and \t when encountered in the format.</param>
        /// <param name="page">the page number used in pagination-supported styled outputs.</param>
        /// <param name="resultsPerPage"> the amount of results per page, will be capped at 500 </param>
        /// <returns>The results of the styled output request .</returns>
        [HttpPost]
        [Route("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetStyledOutputResultJson(int id, [FromBody] List<KeyValuePair<string, object>> parameters, bool stripNewlinesAndTabs = false,[FromQuery] int resultsPerPage = 500, [FromQuery] int page = 0)
        {
            return (await styledOutputService.GetStyledOutputResultJsonAsync((ClaimsIdentity) User.Identity, id, parameters, stripNewlinesAndTabs, resultsPerPage, page)).GetHttpResponseMessage();
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="moduleId">The ID of the module.</param>
        /// <param name="entityType">Optional: The entity type of the item to duplicate. This is needed when the item is saved in a different table than wiser_item. We can only look up the name of that table if we know the entity type beforehand.</param>
        /// <param name="encryptedItemId">Optional: The encrypted ID of the parent to fix the ordering for. If no value has been given, the root will be used as parent.</param>
        /// <param name="orderBy">Optional: Enter the value "item_title" to order by title, or nothing to order by order number.</param>
        /// <param name="checkId">Optional: This is meant for item-linker fields. This is the encrypted ID for the item that should currently be checked.</param>
        /// <param name="linkType">Optional: The type number of the link. This is used in combination with "checkId"; So that items will only be marked as checked if they have the given link ID.</param>
        /// <returns>A list of <see cref="TreeViewItemModel"/>.</returns>
        [HttpGet]
        [Route("tree-view")]
        [ProducesResponseType(typeof(List<StyledOutputTreeViewItemModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        //public async Task<IActionResult> GetItemsForTreeViewAsync([FromQuery] int moduleId, [FromQuery] string encryptedItemId = null, [FromQuery] string entityType = null, [FromQuery] string orderBy = null, [FromQuery] string checkId = null, [FromQuery] int linkType = 0)
        public async Task<IActionResult> GetItemsForTreeViewAsync([FromQuery] string CustomId = null, [FromQuery] int styleSheetId = 0)
        {
            return (await styledOutputService.GetStyledOutputsForTreeViewAsync( (ClaimsIdentity)User.Identity, CustomId, styleSheetId)).GetHttpResponseMessage();;//.GetItemsForTreeViewAsync(moduleId, (ClaimsIdentity)User.Identity, entityType, encryptedItemId, orderBy, checkId, linkType)).GetHttpResponseMessage();
        }
    }
}

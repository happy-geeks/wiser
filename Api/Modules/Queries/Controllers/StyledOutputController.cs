using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
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
        /// <returns>The results of the styled output request .</returns>
        [HttpPost]
        [Route("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetStyledOutputResultJson(int id, [FromBody] List<KeyValuePair<string, object>> parameters, bool stripNewlinesAndTabs = false)
        {
            return (await styledOutputService.GetStyledOutputResultJsonAsync((ClaimsIdentity) User.Identity, id, parameters, stripNewlinesAndTabs)).GetHttpResponseMessage();
        }
    }
}

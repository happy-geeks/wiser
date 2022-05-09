using System.Net.Mime;
using System.Threading.Tasks;
using Api.Modules.Babel.Interfaces;
using Api.Modules.Babel.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Babel.Controllers
{
    /// <summary>
    /// A controller for Babel actions, such as converting javascript to work with older browsers.
    /// </summary>
    [Route("api/v3/[controller]")]
    [ApiController]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    public class BabelController : ControllerBase
    {
        private readonly IBabelService babelService;

        /// <summary>
        /// Creates a new instance of <see cref="BabelController"/>.
        /// </summary>
        public BabelController(IBabelService babelService)
        {
            this.babelService = babelService;
        }

        /// <summary>
        /// Converts javascript, using Babel, to make it work in older browsers. This will convert ES6 code to ES5 and will add polyfills for functions that old browsers don't have built in support for.
        /// </summary>
        /// <param name="options">The content to convert and any additional options.</param>
        [HttpPost]
        [ProducesResponseType(typeof(ResponseModel), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseModel), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> ConvertJavascriptAsync(RequestModel options)
        {
            return (await babelService.ConvertJavascriptAsync(options)).GetHttpResponseMessage();
        }
    }
}

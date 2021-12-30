using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Api.Modules.ContentBuilder.Interfaces;
using Api.Modules.ContentBuilder.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Api.Modules.ContentBuilder.Controllers
{
    /// <summary>
    /// A controller for the content builder, to get HTML, snippets etc.
    /// </summary>
    [Route("api/v3/content-builder"), ApiController, Authorize]
    public class ContentBuilderController : ControllerBase
    {
        private readonly IContentBuilderService contentBuilderService;

        /// <summary>
        /// Creates a new instance of <see cref="ContentBuilderController"/>.
        /// </summary>
        public ContentBuilderController(IContentBuilderService contentBuilderService)
        {
            this.contentBuilderService = contentBuilderService;
        }
        
        /// <summary>
        /// Gets all snippets for the content builder. Snippets are pieces of HTML that the user can add in the Content Builder.
        /// </summary>
        /// <returns>A list with zero or more <see cref="ContentBuilderSnippetModel"/>.</returns>
        [HttpGet, Route("snippets"), ProducesResponseType(typeof(List<ContentBuilderSnippetModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSnippetsAsync(bool isTest = false)
        {
            return (await contentBuilderService.GetSnippetsAsync((ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
        }
        
        /// <summary>
        /// Gets the HTML of an item, for the Content Builder.
        /// </summary>
        /// <param name="itemId">The ID of the Wiser item that contains the HTML to get.</param>
        /// <param name="languageCode">Optional: The language code for the HTML, in case of a multi language website.</param>
        /// <param name="propertyName">Optional: The name of the property in the Wiser item that contains the HTML. Default value is "html".</param>
        /// <returns>The HTML as a string.</returns>
        [HttpGet, Route("html"), ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetHtmlAsync(ulong itemId, string languageCode = "", string propertyName = "html")
        {
            return (await contentBuilderService.GetHtmlAsync((ClaimsIdentity)User.Identity, itemId, languageCode, propertyName)).GetHttpResponseMessage("text/html");
        }
    }
}

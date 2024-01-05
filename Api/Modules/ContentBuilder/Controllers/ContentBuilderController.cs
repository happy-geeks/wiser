using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Api.Modules.ContentBuilder.Interfaces;
using Api.Modules.ContentBuilder.Models;
using Api.Modules.Tenants.Models;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Api.Modules.ContentBuilder.Controllers
{
    /// <summary>
    /// A controller for the content builder, to get HTML, snippets etc.
    /// </summary>
    [Route("api/v3/content-builder")]
    [ApiController]
    [Authorize]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    public class ContentBuilderController : ControllerBase
    {
        private readonly IContentBuilderService contentBuilderService;
        private readonly GclSettings gclSettings;

        /// <summary>
        /// Creates a new instance of <see cref="ContentBuilderController"/>.
        /// </summary>
        public ContentBuilderController(IContentBuilderService contentBuilderService, IOptions<GclSettings> gclSettings)
        {
            this.contentBuilderService = contentBuilderService;
            this.gclSettings = gclSettings.Value;
        }
        
        /// <summary>
        /// Gets all snippets for the content builder. Snippets are pieces of HTML that the user can add in the Content Builder.
        /// </summary>
        /// <returns>A list with zero or more <see cref="ContentBuilderSnippetModel"/>.</returns>
        [HttpGet]
        [Route("snippets")]
        [ProducesResponseType(typeof(List<ContentBuilderSnippetModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSnippetsAsync()
        {
            return (await contentBuilderService.GetSnippetsAsync((ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
        }
        
        /// <summary>
        /// Gets all templates for the content box. Templates are pieces of HTML that the user can add in the Content Box.
        /// </summary>
        /// <returns>A list with zero or more <see cref="ContentBuilderSnippetModel"/>.</returns>
        [HttpGet]
        [Route("templates")]
        [ProducesResponseType(typeof(List<ContentBuilderSnippetModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTemplatesAsync()
        {
            return (await contentBuilderService.GetTemplatesAsync((ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
        }
        
        /// <summary>
        /// Gets all categories for templates for the content box. Templates are pieces of HTML that the user can add in the Content Box.
        /// </summary>
        /// <returns>A list with zero or more <see cref="ContentBuilderSnippetModel"/>.</returns>
        [HttpGet]
        [Route("template-categories")]
        [ProducesResponseType(typeof(List<ContentBuilderSnippetModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTemplateCategoriesAsync()
        {
            return (await contentBuilderService.GetTemplateCategoriesAsync((ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
        }
        
        /// <summary>
        /// Gets the HTML of an item, for the Content Builder.
        /// </summary>
        /// <param name="itemId">The ID of the Wiser item that contains the HTML to get.</param>
        /// <param name="languageCode">Optional: The language code for the HTML, in case of a multi language website.</param>
        /// <param name="propertyName">Optional: The name of the property in the Wiser item that contains the HTML. Default value is "html".</param>
        /// <returns>The HTML as a string.</returns>
        [HttpGet]
        [Route("html")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetHtmlAsync(ulong itemId, string languageCode = null, string propertyName = "html")
        {
            return (await contentBuilderService.GetHtmlAsync((ClaimsIdentity)User.Identity, itemId, languageCode, propertyName)).GetHttpResponseMessage(MediaTypeNames.Text.Html);
        }

        /// <summary>
        /// Gets the framework to use for the content builder.
        /// </summary>
        /// <returns>The name of the framework to use.</returns>
        [HttpGet]
        [Route("framework")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetFrameworkAsync()
        {
            return (await contentBuilderService.GetFrameworkAsync()).GetHttpResponseMessage();
        }

        /// <summary>
        /// Gets the template javascript file for the ContentBox so that the tenant's templates can be used in the ContentBox.
        /// </summary>
        /// <returns>A string with the contents of the javascript file.</returns>
        [HttpGet]
        [Route("template.js")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK, "text/javascript")]
        [AllowAnonymous]
        public async Task<IActionResult> GetTemplateJavascriptFileAsync([FromQuery] TenantInformationModel tenantInformation)
        {
            // Create a ClaimsIdentity based on query parameters instead the Identity from the bearer token due to being called from an image source where no headers can be set.
            var userId = String.IsNullOrWhiteSpace(tenantInformation.encryptedUserId) ? 0 : Int32.Parse(tenantInformation.encryptedUserId.Replace(" ", "+").DecryptWithAesWithSalt(gclSettings.DefaultEncryptionKey, true));
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.GroupSid, tenantInformation.subDomain ?? "")
            };
            var dummyClaimsIdentity = new ClaimsIdentity(claims);
            //Set the sub domain for the database connection.
            HttpContext.Items[HttpContextConstants.SubDomainKey] = tenantInformation.subDomain;
            
            return (await contentBuilderService.GetTemplateJavascriptFileAsync(dummyClaimsIdentity)).GetHttpResponseMessage("text/javascript");
        }
    }
}

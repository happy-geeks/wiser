using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Models;
using Api.Modules.Customers.Models;
using Api.Modules.Templates.Interfaces;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace Api.Modules.Templates.Controllers
{
    /// <summary>
    /// Controller for getting or doing things with templates from the templates module in Wiser.
    /// </summary>
    [Route("api/v3/[controller]"), ApiController, Authorize]
    public class TemplatesController : ControllerBase
    {
        private readonly ITemplatesService templatesService;
        private readonly GclSettings gclSettings;

        /// <summary>
        /// Creates a new instance of TemplatesController.
        /// </summary>
        public TemplatesController(ITemplatesService templatesService, IOptions<GclSettings> gclSettings)
        {
            this.templatesService = templatesService;
            this.gclSettings = gclSettings.Value;
        }
        
        /// <summary>
        /// Gets the CSS that should be used for HTML editors, so that their content will look more like how it would look on the customer's website.
        /// </summary>
        /// <returns>A string that contains the CSS that should be loaded in the HTML editor.</returns>
        [HttpGet, Route("css-for-html-editors"), ProducesResponseType(typeof(Dictionary<string, string>), StatusCodes.Status200OK), AllowAnonymous]
        public async Task<IActionResult> GetCssForHtmlEditorsAsync([FromQuery] CustomerInformationModel customerInformation)
        {
            // Create a ClaimsIdentity based on query parameters instead the Identity from the bearer token due to being called from an image source where no headers can be set.
            var userId = String.IsNullOrWhiteSpace(customerInformation.encryptedUserId) ? 0 : Int32.Parse(customerInformation.encryptedUserId.Replace(" ", "+").DecryptWithAesWithSalt(gclSettings.DefaultEncryptionKey, true));
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.GroupSid, customerInformation.subDomain ?? "")
            };
            var dummyClaimsIdentity = new ClaimsIdentity(claims);
            //Set the sub domain for the database connection.
            HttpContext.Items[HttpContextConstants.SubDomainKey] = customerInformation.subDomain;

            return (await templatesService.GetCssForHtmlEditorsAsync(dummyClaimsIdentity)).GetHttpResponseMessage("text/css");
        }
        
        /// <summary>
        /// Gets a query from the wiser database and executes it in the customer database.
        /// </summary>
        /// <param name="templateName">The encrypted name of the wiser template.</param>
        [HttpGet, HttpPost, Route("get-and-execute-query/{templateName}"), ProducesResponseType(typeof(JToken), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAndExecuteQueryAsync(string templateName)
        {
            IFormCollection requestPostData = null;
            if (Request.HasFormContentType)
            {
                requestPostData = await Request.ReadFormAsync();
            }

            return (await templatesService.GetAndExecuteQueryAsync((ClaimsIdentity)User.Identity, templateName, requestPostData)).GetHttpResponseMessage();
        }
    }
}

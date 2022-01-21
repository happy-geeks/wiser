using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Models;
using Api.Modules.Customers.Models;
using Api.Modules.Templates.Interfaces;
using Api.Modules.Templates.Models.DynamicContent;
using Api.Modules.Templates.Models.Other;
using Api.Modules.Templates.Models.Preview;
using Api.Modules.Templates.Models.Template;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
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
        private readonly IPreviewService previewService;
        private readonly GclSettings gclSettings;

        /// <summary>
        /// Creates a new instance of TemplatesController.
        /// </summary>
        public TemplatesController(ITemplatesService templatesService, IOptions<GclSettings> gclSettings, IPreviewService previewService)
        {
            this.templatesService = templatesService;
            this.previewService = previewService;
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
        
        /// <summary>
        /// Retrieve the tree view section underlying the parentId. Transforms the tree view section into a list of TemplateTreeViewModels.
        /// </summary>
        /// <param name="parentId">The id of the template whose child nodes are to be retrieved.</param>
        /// <returns>A List of TemplateTreeViewModels containing the id, names and types of the templates included in the requested section.</returns>
        [HttpGet, Route("tree-view/{parentId:int}"), ProducesResponseType(typeof(List<TemplateTreeViewModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> TreeViewAsync(int parentId = 0)
        {
            return (await templatesService.GetTreeViewSectionAsync(parentId)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Load The development tab.
        /// </summary>
        [HttpGet, Route("tabs/development"), ProducesResponseType(typeof(DevelopmentTemplateModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> DevelopmentTabAsync(int templateId)
        {
            return (await templatesService.GetDevelopmentTabDataAsync(templateId)).GetHttpResponseMessage();
        }
        
        /// <summary>
        /// Retrieve the history of the template. This will include changes made to dynamic content between the releases of templates and the publishes to different environments from this template. This data is collected and combined in a TemnplateHistoryOverviewModel
        /// </summary>
        /// <param name="templateId">The id of the template to retrieve the history from.</param>
        /// <returns>A TemplateHistoryOverviewModel containing a list of templatehistorymodels and a list of publishlogmodels. The model contains base info and a list of changes made within the version and its sub components (e.g. dynamic content, publishes).</returns>
        [HttpGet, Route("tabs/history"), ProducesResponseType(typeof(DevelopmentTemplateModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> HistoryTabAsync(int templateId)
        {
            return (await templatesService.GetTemplateHistoryAsync(templateId)).GetHttpResponseMessage();
        }
        
        /// <summary>
        /// Retrieve al the preview profiles for an item.
        /// </summary>
        /// <param name="templateId">the id of the item to retrieve the preview items of.</param>
        /// <returns>A list of PreviewProfileModel containing the profiles that are available for the given template</returns>
        [HttpGet, Route("tabs/preview"), ProducesResponseType(typeof(DevelopmentTemplateModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> PreviewTabAsync(int templateId)
        {
            return (await previewService.GetPreviewProfiles(templateId)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Retrieve the latest version of the template. 
        /// </summary>
        /// <param name="templateId">The id of the template to retrieve.</param>
        /// <returns>A template model containing the data of the templateversion.</returns>
        [HttpGet, Route("{templateId:int}/settings"), ProducesResponseType(typeof(DevelopmentTemplateModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTemplateSettingsAsync(int templateId)
        {
            return (await templatesService.GetTemplateSettingsAsync(templateId)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Retrieve the published environments of a template, this includes a list of all version options.
        /// </summary>
        /// <param name="templateId">The id of the template from which to retrieve the published environments.</param>
        /// <returns>A published environment model including the versions numbers of the Live, accept and test environment and a list of all possible versions.</returns>
        [HttpGet, Route("{templateId:int}/published-environments"), ProducesResponseType(typeof(PublishedEnvironmentModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPublishedEnvironments(int templateId)
        {
            if (templateId <= 0)
            {
                throw new ArgumentException("The Id cannot be zero.");
            }
            return (await templatesService.GetTemplateEnvironmentsAsync(templateId)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Retrieve the templates that are linked to the given templates.
        /// </summary>
        /// <param name="templateId">The id of the template of which to get the linked templates.</param>
        /// <returns>A Linked Templates model containing lists of linked templates separated into lists of certain types (e.g. scss, javascript).</returns>
        [HttpGet]
        public async Task<LinkedTemplatesModel> GetLinkedTemplates(int templateId)
        {
            if (templateId <= 0)
            {
                throw new ArgumentException("The Id cannot be zero.");
            }
            return await templatesService.GetLinkedTemplatesAsync(templateId);
        }

        /// <summary>
        /// Retrieve the dynamic content that is linked to the given template.
        /// </summary>
        /// <param name="templateId">The id of the template of which the linked dynamic content should be retrieved.</param>
        /// <returns>List of dynamic content overview models. This is a condensed version of dynamic content data for creating a overview of linked content.</returns>
        [HttpGet]
        public async Task<List<DynamicContentOverviewModel>> GetLinkedDynamicContent(int templateId)
        {
            if (templateId <= 0)
            {
                throw new ArgumentException("The Id cannot be zero.");
            }
            var resultOverview = await templatesService.GetLinkedDynamicContentAsync(templateId);

            var resultList = await historyService.GetPublishedEnvoirementsOfOverviewModels(resultOverview);
            return resultList;
        }

        /// <summary>
        /// Publish a template to a new environment. If moved forward the lower environments will also be moved.
        /// </summary>
        /// <param name="templateId">The id of the template to publish.</param>
        /// <param name="version">The version of the template to publish.</param>
        /// <param name="environment">The environment to push the template version to. This will be converted to a PublishedEnvironmentEnum.</param>
        /// <returns>The number of affected rows.</returns>
        [HttpGet]
        public async Task<IActionResult> PublishToEnvironment(int templateId, int version, string environment)
        {
            if (templateId <= 0)
            {
                throw new ArgumentException("The Id is invalid");
            }
            if (version <= 0)
            {
                throw new ArgumentException("The version is invalid");
            }

            var currentPublished = await templatesService.GetTemplateEnvironmentsAsync(templateId);
            return currentPublished.StatusCode != HttpStatusCode.OK 
                ? currentPublished.GetHttpResponseMessage() 
                : (await templatesService.PublishEnvironmentOfTemplateAsync(templateId, version, environment, currentPublished.ModelObject)).GetHttpResponseMessage();
        }
        
        /// <summary>
        /// Save the template data. This includes the editor value linked templates and advanced settings.
        /// </summary>
        /// <param name="templateData">A JSON string containing a templatedatamodel with the new data to save.</param>
        /// <param name="scssLinks"></param>
        /// <param name="jsLinks"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<int> SaveTemplate(string templateData, List<int> scssLinks, List<int> jsLinks)
        {
            if (String.IsNullOrEmpty(templateData))
            {
                throw new ArgumentException("TemplateData cannot be empty.");
            }
            return await templatesService.SaveTemplateVersionAsync(JsonConvert.DeserializeObject<TemplateDataModel>(templateData), scssLinks, jsLinks);
        }
        
        /// <summary>
        /// Save a preview profile bound to the current template.
        /// </summary>
        /// <param name="profile">A Json that meets the standards of a PreviewProfileModel</param>
        /// <param name="templateId">The id of the template that is bound to the profile</param>
        /// <returns>An int confirming the affected rows of the query.</returns>
        [HttpPost]
        public async Task<int> SaveNewPreviewProfile(PreviewProfileModel profile, int templateId)
        {
            if (templateId <= 0)
            {
                throw new ArgumentException("The Id cannot be zero.");
            }
            if (profile.id <= 0)
            {
                throw new ArgumentException("The profileId is invalid");
            }
            return await previewService.SaveNewPreviewProfile(profile, templateId);
        }

        /// <summary>
        /// Edit an existing profile. The existing profile with the given Id in the profile will be overwritten.
        /// </summary>
        /// <param name="profile">A Json that meets the standards of a PreviewProfileModel</param>
        /// <param name="templateId">The id of the template that is bound to the profile</param>
        /// <returns>An int confirming the affected rows of the query.</returns>
        [HttpPost]
        public async Task<int> EditPreviewProfile(PreviewProfileModel profile, int templateId)
        {
            if (templateId <= 0)
            {
                throw new ArgumentException("The Id cannot be zero.");
            }
            if (profile.id <= 0)
            {
                throw new ArgumentException("The profileId is invalid");
            }
            return await previewService.EditPreviewProfile(profile, templateId);
        }

        /// <summary>
        /// Delete a profile from the database. 
        /// </summary>
        /// <param name="templateId">The id of the template bound to the profile. This is added a an extra security for the deletion.</param>
        /// <param name="profileId">The id of the profile that is to be deleted</param>
        /// <returns>An int confirming the affected rows of the query.</returns>
        [HttpPost]
        public async Task<int> DeletePreviewProfiles(int templateId, int profileId)
        {
            if (templateId <= 0)
            {
                throw new ArgumentException("The Id cannot be zero.");
            }
            if (profileId <= 0)
            {
                throw new ArgumentException("The profileId is invalid");
            }
            return await previewService.RemovePreviewProfile(templateId, profileId);
        }

        /// <summary>
        /// This can be used to retrieve a section of the Treeview for the customer's templates. When no templateId (or a template id of 0) is supplied the root will be retrieved.
        /// </summary>
        /// <param name="templateId">The id of the folder/template which childElements need to be loaded. When this is left empty or has a value of 0 the root will be retrieved.</param>
        /// <returns>A list of treeviewtemplates containing the section underlaying the given templateId.</returns>
        [HttpGet]
        public async Task<List<TemplateTreeViewModel>> GetTreeviewSection(int templateId)
        {
            if (templateId < 0)
            {
                throw new ArgumentException("The Id is invalid");
            }
            return await templatesService.GetTreeViewSectionAsync(templateId);
        }

        [HttpGet]
        public async Task<List<SearchResultModel>> GetSearchResults(SearchSettingsModel searchSettings)
        {
            return await templatesService.GetSearchResultsAsync(searchSettings);
        }
    }
}

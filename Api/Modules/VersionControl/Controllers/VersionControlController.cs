using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Modules.Grids.Models;
using Api.Modules.Templates.Interfaces;
using Api.Modules.Templates.Models.Other;
using Api.Modules.Templates.Models.Template;
using Api.Modules.VersionControl.Interfaces;
using Api.Modules.VersionControl.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Api.Modules.VersionControl.Controllers
{
    /// <summary>
    /// Controller for getting or doing things with templates and dynamic content from the version control module in Wiser.
    /// </summary>
    [Route("api/v3/version-control")]
    [ApiController]
    [Authorize]
    public class VersionControlController : Controller
    {
        private readonly IVersionControlService versionControlService;
        private readonly ITemplatesService templatesService;
        private readonly ITemplateCommitsService templateCommitsService;
        private readonly IDynamicContentServiceVersionControl dynamicContentService;
        private readonly ICommitService commitService;

        /// <summary>
        /// Creates a new instance of <see cref="VersionControlController"/>.
        /// </summary>
        public VersionControlController(IVersionControlService versionControlService, ITemplatesService templatesService, ITemplateCommitsService templateCommitsService, IDynamicContentServiceVersionControl dynamicContentService, ICommitService commitService)
        {
            this.versionControlService = versionControlService;
            this.templatesService = templatesService;
            this.templateCommitsService = templateCommitsService;
            this.dynamicContentService = dynamicContentService;
            this.commitService = commitService;
        }
        
        /// <summary>
        /// Get all templates that have uncommitted changes.
        /// </summary>
        [HttpGet]
        [Route("templates-to-commit")]
        [ProducesResponseType(typeof(List<TemplateCommitModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTemplatesToCommitAsync(CreateCommitModel commitModel)
        {
            return (await commitService.GetTemplatesToCommitAsync()).GetHttpResponseMessage();
        }
        

        /// <summary>
        /// Creates new commit and adds it to the database
        /// </summary>
        /// <param name="commitModel"></param>
        /// <returns></returns>
        [HttpPut]
        [ProducesResponseType(typeof(CreateCommitModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateNewCommit(CreateCommitModel commitModel)
        {
            return (await commitService.CreateCommitAsync(commitModel.Description, (ClaimsIdentity) User.Identity)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Adds new row to the database that connects the template to the commit
        /// </summary>
        /// <param name="templateCommitModel"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("template-commit")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateNewTemplateCommit(TemplateCommitModel templateCommitModel)
        {
            return (await templateCommitsService.CreateNewTemplateCommitAsync(templateCommitModel)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Updates the environment of the template
        /// </summary>
        /// <param name="templateId"></param>
        /// <param name="publishNumber"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("{templateId:int}/{publishNumber:int}")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdatePublishEnvironmentTemplate(int templateId, int publishNumber)
        {
            return (await templateCommitsService.UpdatePublishEnvironmentTemplateAsync(templateId, publishNumber)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Updates the published environment of the Template
        /// </summary>
        /// <param name="templateId">The ID of the template</param>
        /// <param name="environment">Name of the environment to publish to.</param>
        /// <param name="version">The version of the template.</param>
        /// <returns></returns>
        [HttpPost]
        [Route("{templateId:int}/publish/{environment}/{version:int}")]
        [ProducesResponseType(typeof(LinkedTemplatesModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> PublishToEnvironmentAsync(int templateId, string environment, int version)
        {

            var currentPublished = await templatesService.GetTemplateEnvironmentsAsync(templateId);
            return currentPublished.StatusCode != HttpStatusCode.OK
                ? currentPublished.GetHttpResponseMessage()
                : (await templatesService.PublishToEnvironmentAsync((ClaimsIdentity) User.Identity, templateId, version, environment, currentPublished.ModelObject)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Gets the templates that have a lower version than the given one
        /// </summary>
        /// <param name="templateId">The ID of the template</param>
        /// <param name="version"> The version of the template</param>
        /// <returns>Returns a Dictionary with all the templates that have a lower version than the given one</returns>
        [HttpGet]
        [Route("{templateId:int}/{version:int}")]
        [ProducesResponseType(typeof(Dictionary<int, int>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTemplatesWithLowerVersion(int templateId, int version)
        {
            return (await templateCommitsService.GetTemplatesWithLowerVersionAsync(templateId, version)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Gets the current published environments of the templates.
        /// </summary>
        /// <param name="templateId">The id of the template</param>
        /// <param name="version">The version of the template</param>
        /// <returns></returns>
        [HttpGet]
        [Route("current-published-environments/{templateId:int}/{version:int}")]
        [ProducesResponseType(typeof(TemplateEnvironments), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCurrentPublishedEnvironments(int templateId, int version)
        {
            return (await templateCommitsService.GetCurrentPublishedEnvironmentAsync(templateId, version)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Gets all the templates of the given commit
        /// </summary>
        /// <param name="commitId">The ID of the Commit</param>
        /// <returns>Returns all the templates of the commit in a list</returns>
        [HttpGet]
        [Route("templates-of-commit/{commitId:int}")]
        [ProducesResponseType(typeof(List<TemplateEnvironments>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTemplatesFromCommit(int commitId)
        {
            return (await versionControlService.GetTemplatesFromCommitAsync(commitId)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Gets all the dynamic content of the given commit
        /// </summary>
        /// <param name="commitId"></param>
        /// <returns>Returns all the dynamic content of the commit in a list</returns>
        [HttpGet]
        [Route("dynamic_content-of-commit/{commitId:int}")]
        [ProducesResponseType(typeof(List<TemplateEnvironments>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDynamicContentFromCommit(int commitId)
        {
            return (await versionControlService.GetDynamicContentFromCommitAsync(commitId)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Gets the dynamic content with the given ID and Version.
        /// </summary>
        /// <param name="contentId">The ID of the dynamic content.</param>
        /// <param name="version">The version of the dynamic content</param>
        /// <returns></returns>
        [HttpGet]
        [Route("dynamic-Content/{contentId:int}/{version:int}")]
        [ProducesResponseType(typeof(DynamicContentModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDynamicContent(int contentId, int version)
        {
            return (await dynamicContentService.GetDynamicContentAsync(contentId, version)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Adds new row to the database that connects the dynamic content to the commit
        /// </summary>
        /// <param name="dynamicContentCommitModel"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("dynamic-content-commit")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateDynamicContentCommit(DynamicContentCommitModel dynamicContentCommitModel)
        {

            return (await dynamicContentService.CreateNewDynamicContentCommitAsync(dynamicContentCommitModel)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Publishes the dynamic content to the given environment
        /// </summary>
        /// <param name="dynamicContentId">The ID of the dynamic content</param>
        /// <param name="environment">The environment to publish to</param>
        /// <param name="version">The version of the dynamic content</param>
        /// <returns></returns>
        [HttpPost]
        [Route("{dynamicContentId:int}/publish-dynamic-content/{environment}/{version:int}")]
        [ProducesResponseType(typeof(PublishedEnvironmentModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPublishedEnvironmentsDynamicContentAsync(int dynamicContentId, string environment, int version)
        {
            var currentPublished = await dynamicContentService.GetDynamicContentEnvironmentsAsync(dynamicContentId);
            return currentPublished.StatusCode != HttpStatusCode.OK
                ? currentPublished.GetHttpResponseMessage()
                : (await dynamicContentService.PublishDynamicContentToEnvironmentAsync((ClaimsIdentity) User.Identity, dynamicContentId, version, environment, currentPublished.ModelObject)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Gets all the dynamic content that have a lower version than the given one.
        /// </summary>
        /// <param name="contentId">The ID of the dynamic content</param>
        /// <param name="version">The version of the dynamic content</param>
        /// <returns>Returns all the dynamic with a lower version in a Dictionary</returns>
        [HttpGet]
        [Route("dynamic-content/lower-versions/{contentId:int}/{version:int}")]
        [ProducesResponseType(typeof(Dictionary<int, int>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDynamicContentWithLowerVersion(int contentId, int version)
        {
            return (await dynamicContentService.GetDynamicContentWithLowerVersionAsync(contentId, version)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Gets the dynamic content that are part of the given template
        /// </summary>
        /// <param name="templateId">the ID of the template</param>
        /// <returns>Returns all the dynamic content that are linked to the given template in a list of dynamic content</returns>
        [HttpGet]
        [Route("dynamic-content-in-template/{templateId:int}")]
        [ProducesResponseType(typeof(List<DynamicContentModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDynamicContentInTemplate(int templateId)
        {
            return (await versionControlService.GetDynamicContentInTemplateAsync(templateId)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Completes the commit
        /// </summary>
        /// <param name="commitId">The id of the commit</param>
        /// <param name="commitCompleted">The bool to swap the commit to complete</param>
        /// <returns></returns>
        [HttpPut]
        [Route("{commitId:int}/complete-commit/{commitCompleted:bool}")]
        [ProducesResponseType(typeof(GridSettingsAndDataModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CompleteCommit(int commitId, bool commitCompleted)
        {
            return (await commitService.CompleteCommit(commitId, commitCompleted)).GetHttpResponseMessage();
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Modules.Grids.Interfaces;
using Api.Modules.Grids.Models;
using Api.Modules.Templates.Interfaces;
using Api.Modules.Templates.Models.Other;
using Api.Modules.Templates.Models.Template;
using Api.Modules.VersionControl.Interfaces;
using Api.Modules.VersionControl.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Api.Modules.Kendo.Models;

namespace Api.Modules.VersionControl.Controllers
{
    /// <summary>
    /// Controller for getting or doing things with templates and dynamic content from the version control module in Wiser.
    /// </summary>
    [Route("api/v3/[controller]"), ApiController, Authorize]
    public class VersionControlController : Controller
    {
        private readonly IVersionControlService _versionControlService;
        private readonly ITemplatesService templatesService;
        private readonly IGridsService gridsService;
        private readonly ITemplateContainerService versionControlTemplateService;
        private readonly IDynamicContentServiceVersionControl dynamicContentService;
        private readonly ICommitService commitService;

        /// <summary>
        /// Creates a new instance of <see cref="VersionControlController"/>.
        /// </summary>
        public VersionControlController(IVersionControlService versionControlService, ITemplatesService templatesService, IGridsService gridsService, ITemplateContainerService templateService, IDynamicContentServiceVersionControl dynamicContentService, ICommitService commitService)
        {
            this._versionControlService = versionControlService;
            this.templatesService = templatesService;
            this.gridsService = gridsService;
            this.versionControlTemplateService = templateService;
            this.dynamicContentService = dynamicContentService;
            this.commitService = commitService;
        }

        /// <summary>
        /// Creates new commit and adds it to the database
        /// </summary>
        /// <param name="commitModel"></param>
        /// <returns></returns>
        [HttpPut, ProducesResponseType(typeof(CreateCommitModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateNewCommit(CreateCommitModel commitModel)
        {
            return (await commitService.CreateCommitAsync(commitModel.Description, (ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Creates new commit in the database
        /// </summary>
        /// <param name="templateId"></param>
        /// <param name="commitItemModel"></param>
        /// <returns></returns>
        [HttpPut, Route("{templateId:int}"), ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateNewCommitItem(int templateId, CommitItemModel commitItemModel)
        {
            return (await commitService.CreateCommitItemAsync(templateId, commitItemModel))
                .GetHttpResponseMessage();
        }

        /// <summary>
        /// Gets data from a commit
        /// </summary>
        /// <returns></returns>
        [HttpGet, Route("Commit")]
        [ProducesResponseType(typeof(CreateCommitModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCommit()
        {
            return (await commitService.GetCommitAsync()).GetHttpResponseMessage();
        }

        /// <summary>
        /// Adds new row to the database that connects the template to the commit
        /// </summary>
        /// <param name="templateCommitModel"></param>
        /// <returns></returns>
        [HttpPut, Route("template-commit"), ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateNewTemplateCommit(TemplateCommitModel templateCommitModel)
        {

            return (await versionControlTemplateService.CreateNewTemplateCommitAsync(templateCommitModel)).GetHttpResponseMessage();
        }

       
        /// <summary>
        /// Updates the environment of the template
        /// </summary>
        /// <param name="templateId"></param>
        /// <param name="publishNumber"></param>
        /// <returns></returns>
        [HttpPut, Route("{templateId:int}/{publishNumber:int}"), ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdatePublishEnvironmentTemplate(int templateId, int publishNumber)
        {
            return (await versionControlTemplateService.UpdatePublishEnvironmentTemplateAsync(templateId,publishNumber)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Updates the published environment of the Template
        /// </summary>
        /// <param name="templateId">The ID of the template</param>
        /// <param name="environment">Name of the enviornment to publish to.</param>
        /// <param name="version">The version of the template.</param>
        /// <returns></returns>
        [HttpPost, Route("{templateId:int}/publish/{environment}/{version:int}"), ProducesResponseType(typeof(LinkedTemplatesModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> PublishToEnvironmentAsync(int templateId, string environment, int version)
        {  

            var currentPublished = await templatesService.GetTemplateEnvironmentsAsync(templateId);
            return currentPublished.StatusCode != HttpStatusCode.OK
                ? currentPublished.GetHttpResponseMessage()
                : (await templatesService.PublishToEnvironmentAsync((ClaimsIdentity)User.Identity, templateId, version, environment, currentPublished.ModelObject)).GetHttpResponseMessage();
        }


        /// <summary>
        /// Gets the data and settings for a module with grid view mode enabled.
        /// </summary>
        /// <param name="id">The ID of the module.</param>
        /// <param name="gridData">The data for the Kendo UI grid.</param>
        [HttpPost, Route("{id:int}/overview-grid"), ProducesResponseType(typeof(GridSettingsAndDataModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> OverviewGridAsync(int id, ModuleGridDataSettings gridData)
        {
            var result = (await gridsService.GetGridDataAsync(id,gridData,(ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
            return result;
            //(id,gridData, (ClaimsIdentity)User.Identity),gridDivId).
        }


        /// <summary>
        /// Gets the templates that have a lower version than the given one
        /// </summary>
        /// <param name="templateId">The ID of the template</param>
        /// <param name="version"> The version of the template</param>
        /// <returns>Returns a Dictionary with all the templates that have a lower version than the given one</returns>
        [HttpGet, Route("{templateId:int}/{version:int}")]
        [ProducesResponseType(typeof(Dictionary<int, int>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTemplatesWithLowerVersion(int templateId, int version)
        {
            return (await versionControlTemplateService.GetTemplatesWithLowerVersionAsync(templateId, version)).GetHttpResponseMessage();
        }


        /// <summary>
        /// Gets the current publisehd environments of the templates
        /// </summary>
        /// <param name="templateId">The id of the template</param>
        /// <param name="version">The version of the template</param>
        /// <returns></returns>
        [HttpGet, Route("current-published-enviornments/{templateId:int}/{version:int}")]
        [ProducesResponseType(typeof(TemplateEnvironments), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCurrentPublishedEnvironments(int templateId,int version)
        {
            return (await versionControlTemplateService.GetCurrentPublishedEnvironmentAsync(templateId, version)).GetHttpResponseMessage();
        }


        /// <summary>
        /// Gets all the templates of the given commit
        /// </summary>
        /// <param name="commitId">The ID of the Commit</param>
        /// <returns>Returns all the templates of the commit in a list</returns>
        [HttpGet, Route("templates-of-commit/{commitId:int}")]
        [ProducesResponseType(typeof(List<TemplateEnvironments>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTemplatesfromCommit(int commitId)
        {
            return (await _versionControlService.GetTemplatesFromCommitAsync(commitId)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Gets all the dynamic content of the given commit
        /// </summary>
        /// <param name="commitId"></param>
        /// <returns>Returns all the dynamic content of the commit in a list</returns>
        [HttpGet, Route("dynamic_content-of-commit/{commitId:int}")]
        [ProducesResponseType(typeof(List<TemplateEnvironments>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDynamicContentfromCommit(int commitId)
        {
            return (await _versionControlService.GetDynamicContentFromCommitAsync(commitId)).GetHttpResponseMessage();
        }


        /// <summary>
        /// Gets the dynamic content with the given ID and Version.
        /// </summary>
        /// <param name="contentId">The ID of the dynamic content.</param>
        /// <param name="version">The version of the dynamic content</param>
        /// <returns></returns>
        [HttpGet, Route("dynamic-Content/{contentId:int}/{version:int}")]
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
        [HttpPut, Route("dynamic-content-commit"), ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
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
        [HttpPost, Route("{dynamicContentId:int}/publish-dynamic-content/{environment}/{version:int}"), ProducesResponseType(typeof(PublishedEnvironmentModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPublishedEnvironmentsDynamicContentAsync(int dynamicContentId, string environment, int version)
        {
            var currentPublished = await dynamicContentService.GetDynamicContentEnvironmentsAsync(dynamicContentId);
            return currentPublished.StatusCode != HttpStatusCode.OK
                ? currentPublished.GetHttpResponseMessage()
                : (await dynamicContentService.PublishDynamicContentToEnvironmentAsync((ClaimsIdentity)User.Identity, dynamicContentId, version, environment, currentPublished.ModelObject)).GetHttpResponseMessage();
        }

        

        /// <summary>
        /// Gets all the dynamic content that have a lower version than the given one.
        /// </summary>
        /// <param name="contentId">The ID of the dynamic content</param>
        /// <param name="version">The version of the dynamic content</param>
        /// <returns>Returns all the dynamic with a lower version in a Dictionary</returns>
        [HttpGet, Route("dynamic-content/lower-versions/{contentId:int}/{version:int}")]
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
        [HttpGet, Route("dynamic-content-in-template/{templateId:int}")]
        [ProducesResponseType(typeof(List<DynamicContentModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDynamicContentInTemplate(int templateId)
        {
            return (await _versionControlService.GetDynamicContentInTemplateAsync(templateId)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Gets all the settings of the grids from the given module
        /// </summary>
        /// <param name="moduleId">The ID of the module</param>
        /// <returns>Returns all the settings of the grids in a list.</returns>
        [HttpGet, Route("module-gird-settings/{moduleId:int}")]
        [ProducesResponseType(typeof(List<ModuleGridSettings>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetModuleGridSettings(int moduleId)
        {
            return (await _versionControlService.GetModuleGridSettingsAsync(moduleId)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Gets the overview and data of the grid diV ID
        /// </summary>
        /// <param name="gridDivId">The id of the grid</param>
        /// <param name="options">The options of the grid</param>
        /// <returns>Returns the grid settings and grid data</returns>
        [HttpPost]
        [Route("{gridDivId}/overview-grid")]
        [ProducesResponseType(typeof(GridSettingsAndDataModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> OverviewGridVersionControlAsync(string gridDivId, GridReadOptionsModel options)
        {
            return (await gridsService.GetOverviewGridVersionControlDataAsync(gridDivId, options, (ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
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

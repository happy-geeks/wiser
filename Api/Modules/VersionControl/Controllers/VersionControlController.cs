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
    /// Controller for getting or doing things with templates from the version control module in Wiser.
    /// </summary>
    [Route("api/v3/[controller]"), ApiController, Authorize]
    public class VersionControlController : Controller
    {
        private readonly IVersionControlService _versionControlService;
        private readonly ITemplatesService templatesService;
        private readonly IGridsService gridsService;


            private readonly ITemplateContainerService versionControlTemplateService;


        public VersionControlController(IVersionControlService versionControlService, ITemplatesService templatesService, IGridsService gridsService, ITemplateContainerService templateService)
        {
            this._versionControlService = versionControlService;
            this.templatesService = templatesService;
            this.gridsService = gridsService;
            this.versionControlTemplateService = templateService;
        }

        /*[HttpPost, Route("{templateId:int}"), ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<IActionResult> SaveAsync(int templateId, VersionControlModel templateData)
        {
            templateData.TemplateId = templateId;
            return (await versionControlService.SaveTemplateVersionAsync((ClaimsIdentity)User.Identity, templateData)).GetHttpResponseMessage();
        }*/

        /// <summary>
        /// 
        /// </summary>
        /// <param name="templateId"></param>
        /// <param name="commitModel"></param>
        /// <returns></returns>
        [HttpPut, ProducesResponseType(typeof(CreateCommitModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateNewCommit(CreateCommitModel commitModel)
        {
            return (await _versionControlService.CreateCommit(commitModel)).GetHttpResponseMessage();
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
            return (await _versionControlService.CreateCommitItem(templateId, commitItemModel))
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
            return (await _versionControlService.GetCommit()).GetHttpResponseMessage();
        }

        /// <summary>
        /// Creates new template and commit row in database
        /// </summary>
        /// <param name="templateCommitModel"></param>
        /// <returns></returns>
        [HttpPut, Route("template-commit"), ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateNewTemplateCommit(TemplateCommitModel templateCommitModel)
        {

            return (await _versionControlService.CreateNewTemplateCommit(templateCommitModel)).GetHttpResponseMessage();
        }

        [HttpPut, Route("update-template"), ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateTemplate(TemplateCommitModel templateCommitModel)
        {

            return (await _versionControlService.UpdateTemplateCommit(templateCommitModel)).GetHttpResponseMessage();
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
            return (await _versionControlService.UpdatePublishEnvironmentTemplate(templateId,publishNumber)).GetHttpResponseMessage();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="templateId"></param>
        /// <param name="environment"></param>
        /// <param name="version"></param>
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
        public async Task<IActionResult> OverviewGridAsync(int id, ModuleGridDataSettings gridData, string gridDivId)
        {
            var result = (await gridsService.GetGridDataAsync(id,gridData,(ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
            return result;
            //(id,gridData, (ClaimsIdentity)User.Identity),gridDivId).
        }

        [HttpGet, Route("PublishedTemplateVersion")]
        [ProducesResponseType(typeof(Dictionary<int,int>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPublishedTemplateIdAndVersion()
        {
            return (await _versionControlService.GetPublishedTemplateIdAndVersion()).GetHttpResponseMessage();
        }
        
        [HttpGet, Route("{templateId:int}/{version:int}")]
        [ProducesResponseType(typeof(Dictionary<int, int>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTemplatesWithLowerVersion(int templateId, int version)
        {
            return (await versionControlTemplateService.GetTemplatesWithLowerVersion(templateId, version)).GetHttpResponseMessage();
        }

        [HttpGet, Route("current-published-enviornments/{templateId:int}/{version:int}")]
        [ProducesResponseType(typeof(VersionControlModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCurrentPublishedEnvironments(int templateId,int version)
        {
            return (await _versionControlService.GetCurrentPublishedEnvironment(templateId, version)).GetHttpResponseMessage();
        }

        [HttpGet, Route("templates-of-commit/{commitId:int}")]
        [ProducesResponseType(typeof(List<VersionControlModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTemplatesfromCommit(int commitId)
        {
            return (await _versionControlService.GetTemplatesFromCommit(commitId)).GetHttpResponseMessage();
        }

        [HttpGet, Route("dynamic_content-of-commit/{commitId:int}")]
        [ProducesResponseType(typeof(List<VersionControlModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDynamicContentfromCommit(int commitId)
        {
            return (await _versionControlService.GetDynamicContentfromCommit(commitId)).GetHttpResponseMessage();
        }


        //DYNAMIC CONTENT
        [HttpGet, Route("dynamic-Content/{contentId:int}/{version:int}")]
        [ProducesResponseType(typeof(DynamicContentModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDynamicContent(int contentId, int version)
        {
            return (await _versionControlService.GetDynamicContent(contentId, version)).GetHttpResponseMessage();
        }

        [HttpPut, Route("dynamic-content-commit"), ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateDynamicContentCommit(DynamicContentCommitModel dynamicContentCommitModel)
        {

            return (await _versionControlService.CreateNewDynamicContentCommit(dynamicContentCommitModel)).GetHttpResponseMessage();
        }

        [HttpPost, Route("{dynamicContentId:int}/publishDyamicContent/{environment}/{version:int}"), ProducesResponseType(typeof(PublishedEnvironmentModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPublishedEnvironmentsDynamicContentAsync(int dynamicContentId, string environment, int version)
        {
            var currentPublished = await _versionControlService.GetDynamicContentEnvironmentsAsync(dynamicContentId);
            return currentPublished.StatusCode != HttpStatusCode.OK
                ? currentPublished.GetHttpResponseMessage()
                : (await _versionControlService.PublishDynamicContentToEnvironmentAsync((ClaimsIdentity)User.Identity, dynamicContentId, version, environment, currentPublished.ModelObject)).GetHttpResponseMessage();
        }

        
        [HttpGet, Route("dynamic-content/lower-versions/{contentId:int}/{version:int}")]
        [ProducesResponseType(typeof(Dictionary<int, int>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDynamicContentWithLowerVersion(int contentId, int version)
        {
            return (await _versionControlService.GetDynamicContentWithLowerVersion(contentId, version)).GetHttpResponseMessage();
        }

        [HttpGet, Route("dynamic-content-in-template/{templateId:int}")]
        [ProducesResponseType(typeof(List<DynamicContentModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDynamicContentInTemplate(int templateId)
        {
            return (await _versionControlService.GetDynamicContentInTemplate(templateId)).GetHttpResponseMessage();
        }

        [HttpGet, Route("module-gird-settings/{moduleId:int}")]
        [ProducesResponseType(typeof(List<ModuleGridSettings>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetModuleGridSettings(int moduleId)
        {
            return (await _versionControlService.GetModuleGridSettings(moduleId)).GetHttpResponseMessage();
        }


        /*[HttpPost, Route("{id:int}/overview-grid"), ProducesResponseType(typeof(GridSettingsAndDataModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> OverviewGridAsync(int id, GridReadOptionsModel options)
        {

            var result = (await gridsService.GetOverviewGridDataAsync(id, options, (ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
            return result;
        }*/

    }
}

using System.Collections.Generic;
using System.Net;
using System.Net.Mime;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Modules.Templates.Interfaces;
using Api.Modules.Templates.Models.DynamicContent;
using Api.Modules.Templates.Models.History;
using Api.Modules.Templates.Models.Template;
using GeeksCoreLibrary.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Templates.Controllers
{
    /// <summary>
    /// Controller for getting or doing things with templates from the templates module in Wiser.
    /// </summary>
    [Route("api/v3/dynamic-content")]
    [ApiController]
    [Authorize]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    public class DynamicContentController : Controller
    {
        private readonly IDynamicContentService dynamicContentService;
        private readonly IHistoryService historyService;
        private readonly ITemplatesService templatesService;

        /// <summary>
        /// Creates a new instance of <see cref="DynamicContentController"/>.
        /// </summary>
        public DynamicContentController(IDynamicContentService dynamicContentService, IHistoryService historyService, ITemplatesService templatesService)
        {
            this.dynamicContentService = dynamicContentService;
            this.historyService = historyService;
            this.templatesService = templatesService;
        }

        /// <summary>
        /// Gets the meta data (name, component mode etc) for a component.
        /// </summary>
        /// <param name="id">The ID of the dynamic component.</param>
        /// <param name="includeSettings">Optional: Whether or not to include the settings that are saved with the component. Default value is <see langword="true" />.</param>
        [HttpGet]
        [Route("{id:int}")]
        [ProducesResponseType(typeof(DynamicContentOverviewModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAsync(int id, bool includeSettings = true)
        {
            return (await dynamicContentService.GetMetaDataAsync(id, includeSettings)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Retrieve all component modes of a dynamic component.
        /// </summary>
        /// <param name="name">The name of the dynamic component.</param>
        /// <returns>A list containing the id and name for each component mode.</returns>
        [HttpGet]
        [Route("{name}/component-modes")]
        [ProducesResponseType(typeof(List<ComponentModeModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult GetComponentModes(string name)
        {
            return dynamicContentService.GetComponentModes(name).GetHttpResponseMessage();
        }

        /// <summary>
        /// Get the history of the current component.
        /// </summary>
        /// <param name="contentId">The component of the history.</param>
        /// <returns>History PartialView containing the retrieved history of the component</returns>
        [HttpGet]
        [Route("{contentId:int}/history")]
        [ProducesResponseType(typeof(List<HistoryVersionModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetHistoryOfComponentAsync(int contentId)
        {
            return (await historyService.GetChangesInComponentAsync(contentId)).GetHttpResponseMessage();
        }

        /// <summary>
        ///  POST endpoint for saving the settings of a component.
        /// </summary>
        /// <param name="id">The id of the content to save</param>
        /// <param name="data">The data to save</param>
        /// <returns>The ID of the saved component.</returns>
        [HttpPost]
        [Route("{id:int}")]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        public async Task<IActionResult> SaveSettingsAsync(int id, DynamicContentOverviewModel data)
        {
            return (await dynamicContentService.SaveAsync((ClaimsIdentity)User.Identity, id, data.Component, data.ComponentModeId ?? 0, data.Title, data.Data)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Undo changes that have been made.
        /// </summary>
        /// <param name="contentId">The ID of the component.</param>
        /// <param name="changes">A json string of changes that can be converted to a List of RevertHistoryModels</param>
        /// <returns>The ID of the component that was reverted.</returns>
        [HttpPost]
        [Route("{contentId:int}/undo-changes")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> RevertChangesAsync(int contentId, List<RevertHistoryModel> changes)
        {
            return (await historyService.RevertChangesAsync((ClaimsIdentity)User.Identity, contentId, changes)).GetHttpResponseMessage();
        }
        
        /// <summary>
        /// Links a dynamic content to a template.
        /// </summary>
        /// <param name="contentId">The ID of the dynamic content.</param>
        /// <param name="templateId">The ID of the template.</param>
        [HttpPut]
        [Route("{contentId:int}/link/{templateId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> AddLinkToTemplateAsync(int contentId, int templateId)
        {
            return (await dynamicContentService.AddLinkToTemplateAsync((ClaimsIdentity)User.Identity, contentId, templateId)).GetHttpResponseMessage();
        }
        
        /// <summary>
        /// Generates a preview for a dynamic component.
        /// </summary>
        /// <param name="componentId">The ID of the component.</param>
        /// <param name="requestModel">The template settings, they don't have to be saved yet.</param>
        /// <returns>The HTML of the component as it would look on the website.</returns>
        [HttpPost]
        [Route("{componentId:int}/html-preview")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [Produces(MediaTypeNames.Text.Html)]
        public async Task<IActionResult> GenerateHtmlForComponentAsync(int componentId, GenerateTemplatePreviewRequestModel requestModel)
        {
            return (await templatesService.GeneratePreviewAsync((ClaimsIdentity)User.Identity, componentId, requestModel)).GetHttpResponseMessage(MediaTypeNames.Text.Html);
        }

        /// <summary>
        /// Publish a dynamic component to a new environment. If moved forward the lower environments will also be moved.
        /// </summary>
        /// <param name="contentId">The id of the dynamic component to publish.</param>
        /// <param name="version">The version of the dynamic component to publish.</param>
        /// <param name="environment">The environment to push the dynamic component version to. This will be converted to a PublishedEnvironmentEnum.</param>
        [HttpPost]
        [Route("{contentId:int}/publish/{environment}/{version:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> PublishToEnvironmentAsync(int contentId, Environments environment, int version)
        {
            var currentPublished = await dynamicContentService.GetEnvironmentsAsync(contentId);
            return currentPublished.StatusCode != HttpStatusCode.OK 
                ? currentPublished.GetHttpResponseMessage() 
                : (await dynamicContentService.PublishToEnvironmentAsync((ClaimsIdentity)User.Identity, contentId, version, environment, currentPublished.ModelObject)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Duplicate a dynamic component (only the latest version).
        /// </summary>
        /// <param name="id">The id of the component.</param>
        /// <param name="templateId">The id of the template to link the new component to.</param>
        [HttpPost]
        [Route("{id:int}/duplicate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> DuplicateAsync(int id, [FromQuery]int templateId)
        {
            return (await dynamicContentService.DuplicateAsync((ClaimsIdentity)User.Identity, id, templateId)).GetHttpResponseMessage();
        }
        
        /// <summary>
        /// Deletes a dynamic content component
        /// </summary>
        /// <param name="contentId">The id of the dynamic content</param>
        /// <returns></returns>
        [HttpDelete]
        [Route("{contentId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteAsync(int contentId)
        {
            return (await dynamicContentService.DeleteAsync(contentId)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Gets all dynamic content that can be linked to the given template.
        /// </summary>
        /// <param name="templateId">The ID of the template.</param>
        /// <returns>A list of dynamic components from other templates.</returns>
        [HttpGet]
        [Route("linkable")]
        [ProducesResponseType(typeof(List<DynamicContentOverviewModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLinkableDynamicContentAsync([FromQuery]int templateId)
        {
            return (await dynamicContentService.GetLinkableDynamicContentAsync(templateId)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Deploy one or more dynamic contents to a branch.
        /// </summary>
        /// <param name="contentId">The ID of the dynamic component to deploy.</param>
        /// <param name="branchId">The ID of the branch to deploy the template to.</param>
        [HttpPost]
        [Route("{contentId:int}/deploy-to-branch/{branchId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DeployToBranchAsync(int contentId, int branchId)
        {
            return (await dynamicContentService.DeployToBranchAsync((ClaimsIdentity) User.Identity, new List<int> { contentId }, branchId)).GetHttpResponseMessage();
        }
    }
}

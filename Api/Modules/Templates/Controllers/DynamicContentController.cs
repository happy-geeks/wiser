using System.Collections.Generic;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Modules.Templates.Interfaces;
using Api.Modules.Templates.Models.DynamicContent;
using Api.Modules.Templates.Models.History;
using Api.Modules.Templates.Models.Template;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Templates.Controllers
{
    /// <summary>
    /// Controller for getting or doing things with templates from the templates module in Wiser.
    /// </summary>
    [Route("api/v3/dynamic-content"), ApiController, Authorize]
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
        
        [HttpGet, Route("{id:int}")]
        [ProducesResponseType(typeof(DynamicContentOverviewModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Get(int id, bool includeSettings = true)
        {
            return (await dynamicContentService.GetMetaDataAsync(id, includeSettings)).GetHttpResponseMessage();
        }
        
        [HttpGet, Route("{name}/component-modes")]
        [ProducesResponseType(typeof(List<ComponentModeModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetComponentModes(string name)
        {
            return dynamicContentService.GetComponentModes(name).GetHttpResponseMessage();
        }

        /// <summary>
        /// Get the history of the current component.
        /// </summary>
        /// <param name="contentId">The component of the history.</param>
        /// <returns>History PartialView containing the retrieved history of the component</returns>
        [HttpGet, Route("{contentId:int}/history")]
        [ProducesResponseType(typeof(List<ComponentModeModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetHistoryOfComponent(int contentId)
        {
            return (await historyService.GetChangesInComponent(contentId)).GetHttpResponseMessage();
        }

        /// <summary>
        ///  POST endpoint for saving the settings of a component.
        /// </summary>
        /// <param name="contentId">The id of the content to save</param>
        /// <param name="data">The data to save</param>
        /// <returns>The ID of the saved component.</returns>
        [HttpPost, Route("{contentId:int}")]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        public async Task<IActionResult> SaveSettings(int contentId, DynamicContentOverviewModel data)
        {
            return (await dynamicContentService.SaveNewSettingsAsync((ClaimsIdentity)User.Identity, contentId, data.Component, data.ComponentModeId.Value, data.Title, data.Data)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Undo changes that have been made.
        /// </summary>
        /// <param name="contentId">The ID of the component.</param>
        /// <param name="changes">A json string of changes that can be converted to a List of RevertHistoryModels</param>
        /// <returns>An int representing the affected rows as confirmation</returns>
        [HttpPost, Route("{contentId:int}/undo-changes")]
        [ProducesResponseType(typeof(List<ComponentModeModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UndoChanges(int contentId, List<RevertHistoryModel> changes)
        {
            return (await historyService.RevertChangesAsync((ClaimsIdentity)User.Identity, contentId, changes)).GetHttpResponseMessage();
        }
        
        /// <summary>
        /// Links a dynamic content to a template.
        /// </summary>
        /// <param name="contentId">The ID of the dynamic content.</param>
        /// <param name="templateId">The ID of the template.</param>
        [HttpPut, Route("{contentId:int}/link/{templateId:int}")]
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
        [HttpPost, Route("{componentId:int}/html-preview"), ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public async Task<IActionResult> GenerateHtmlForComponentAsync(int componentId, GenerateTemplatePreviewRequestModel requestModel)
        {
            return (await templatesService.GeneratePreviewAsync((ClaimsIdentity)User.Identity, componentId, requestModel)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Publish a dynamic component to a new environment. If moved forward the lower environments will also be moved.
        /// </summary>
        /// <param name="contentId">The id of the dynamic component to publish.</param>
        /// <param name="version">The version of the dynamic component to publish.</param>
        /// <param name="environment">The environment to push the dynamic component version to. This will be converted to a PublishedEnvironmentEnum.</param>
        /// <returns>The number of affected rows.</returns>
        [HttpPost, Route("{contentId:int}/publish/{environment}/{version:int}"), ProducesResponseType(typeof(LinkedTemplatesModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> PublishToEnvironmentAsync(int contentId, string environment, int version)
        {
            var currentPublished = await dynamicContentService.GetEnvironmentsAsync(contentId);
            return currentPublished.StatusCode != HttpStatusCode.OK 
                ? currentPublished.GetHttpResponseMessage() 
                : (await dynamicContentService.PublishToEnvironmentAsync((ClaimsIdentity)User.Identity, contentId, version, environment, currentPublished.ModelObject)).GetHttpResponseMessage();
        }
    }
}

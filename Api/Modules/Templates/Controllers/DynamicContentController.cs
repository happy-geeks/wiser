using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Modules.Templates.Helpers;
using Api.Modules.Templates.Interfaces;
using Api.Modules.Templates.Models.DynamicContent;
using Api.Modules.Templates.Models.History;
using GeeksCoreLibrary.Components.Account;
using GeeksCoreLibrary.Core.Cms.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

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

        /// <summary>
        /// Creates a new instance of <see cref="DynamicContentController"/>.
        /// </summary>
        public DynamicContentController(IDynamicContentService dynamicContentService, IHistoryService historyService)
        {
            this.dynamicContentService = dynamicContentService;
            this.historyService = historyService;
        }
        
        [HttpGet, Route("{id:int}")]
        [ProducesResponseType(typeof(DynamicContentOverviewModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Get(int id)
        {
            return (await dynamicContentService.GetMetaDataAsync(id)).GetHttpResponseMessage();
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
        /// <returns>A list of saved settings as confirmation.</returns>
        [HttpPost, Route("{contentId:int}")]
        [ProducesResponseType(typeof(List<ComponentModeModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SaveSettings(int contentId, DynamicContentOverviewModel data)
        {
            if (contentId == 0)
            {
                throw new ArgumentException("The Id cannot be zero.");
            }
            if (data.ComponentModeId is null or 0)
            {
                throw new ArgumentException("The ComponentModeId cannot be zero.");
            }
            
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
            return (await historyService.RevertChanges((ClaimsIdentity)User.Identity, contentId, changes)).GetHttpResponseMessage();
        }
    }
}

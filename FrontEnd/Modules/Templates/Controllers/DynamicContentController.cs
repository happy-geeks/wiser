using System.Collections.Generic;
using Api.Modules.Templates.Helpers;
using Api.Modules.Templates.Models.DynamicContent;
using Api.Modules.Templates.Models.History;
using FrontEnd.Core.Interfaces;
using FrontEnd.Modules.Templates.Interfaces;
using FrontEnd.Modules.Templates.Models;
using Microsoft.AspNetCore.Mvc;

namespace FrontEnd.Modules.Templates.Controllers
{
    [Area("Templates"), Route("Modules/DynamicContent")]
    public class DynamicContentController : Controller
    {
        private readonly IBaseService baseService;
        private readonly IFrontEndDynamicContentService dynamicContentService;

        public DynamicContentController(IBaseService baseService, IFrontEndDynamicContentService dynamicContentService)
        {
            this.baseService = baseService;
            this.dynamicContentService = dynamicContentService;
        }

        /// <summary>
        /// The main page on starting the application. TODO: open with the current component as original
        /// </summary>
        /// <returns>
        /// View containing the attributes of the component to be displayed as fields.
        /// </returns>
        [HttpGet, Route("{id:int}")]
        public IActionResult Index(int id, int templateId)
        {
            var viewModel = baseService.CreateBaseViewModel<DynamicContentViewModel>();
            viewModel.ContentId = id;
            viewModel.TemplateId = templateId;
            viewModel.Components = ReflectionHelper.GetComponents();
            return View(viewModel);
        }

        /// <summary>
        /// Get The tabwindow containing the component properties.
        /// </summary>
        /// <param name="component">The component which properties should be loaded into the tab window.</param>
        /// <param name="data">The data of the component.</param>
        /// <returns>The Partial view containing the HTML of the tab window from dynamic content.</returns>
        [HttpPost, Route("{component}/DynamicContentTabPane")]
        public IActionResult DynamicContentTabPane(string component, [FromBody]DynamicContentOverviewModel data)
        {
            var componentType = ReflectionHelper.GetComponentTypeByName(component);
            var viewModel = dynamicContentService.GenerateDynamicContentInformationViewModel(componentType, data.Data, data.ComponentMode);

            // ReSharper disable once Mvc.PartialViewNotResolved
            return PartialView("Partials/DynamicContentTabPane", viewModel);
        }

        [HttpPost, Route("History")]
        public IActionResult History([FromBody]List<HistoryVersionModel> viewData)
        {
            var viewModel = new DynamicContentHistoryPaneViewModel { History = new List<HistoryVersionViewModel>() };

            foreach (var history in viewData)
            {
                viewModel.History.Add(new HistoryVersionViewModel
                {
                    Changes = history.Changes,
                    Component = history.Component,
                    ChangedBy = history.ChangedBy,
                    Version = history.Version,
                    ChangedOn = history.ChangedOn,
                    RawVersionString = history.RawVersionString,
                    ComponentMode = history.ComponentMode,
                    ChangedFields = dynamicContentService.GenerateChangesListForHistory(history.Changes)
                });
            }

            // ReSharper disable once Mvc.PartialViewNotResolved
            return PartialView("Partials/DynamicContentHistoryPane", viewModel);
        }

        [HttpPost, Route("HistoryRow")]
        public IActionResult HistoryRow([FromBody]List<HistoryVersionModel> viewData)
        {
            var viewModel = new DynamicContentHistoryPaneViewModel { History = new List<HistoryVersionViewModel>() };

            foreach (var history in viewData)
            {
                viewModel.History.Add(new HistoryVersionViewModel
                {
                    Changes = history.Changes,
                    Component = history.Component,
                    ChangedBy = history.ChangedBy,
                    Version = history.Version,
                    ChangedOn = history.ChangedOn,
                    RawVersionString = history.RawVersionString,
                    ComponentMode = history.ComponentMode,
                    ChangedFields = dynamicContentService.GenerateChangesListForHistory(history.Changes)
                });
            }

            // ReSharper disable once Mvc.PartialViewNotResolved
            return PartialView("Partials/DynamicContentHistoryRows", viewModel);
        }

        [HttpPost, Route("PublishedEnvironments")]
        public IActionResult PublishedEnvironments([FromBody]DynamicContentOverviewModel tabViewData)
        {
            // ReSharper disable once Mvc.PartialViewNotResolved
            return PartialView("Partials/PublishedEnvironments", tabViewData);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Api.Modules.Templates.Helpers;
using Api.Modules.Templates.Models.DynamicContent;
using Api.Modules.Templates.Models.History;
using FrontEnd.Core.Interfaces;
using FrontEnd.Modules.Templates.Interfaces;
using FrontEnd.Modules.Templates.Models;
using GeeksCoreLibrary.Core.Cms;
using GeeksCoreLibrary.Core.Cms.Attributes;
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
            viewModel.Components = GetComponents();
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
        
        /// <summary>
        /// Get all possible components. These components should are retrieved from the assembly and should have the basetype CmsComponent&lt;CmsSettings, Enum&gt;
        /// </summary>
        /// <returns>Dictionary of typeinfos and object attributes of all the components found in the GCL.</returns>
        private Dictionary<TypeInfo, CmsObjectAttribute> GetComponents()
        {
            var componentType = typeof(CmsComponent<CmsSettings, Enum>);
            var assembly = componentType.Assembly;

            var typeInfoList = assembly.DefinedTypes.Where(
                type => type.BaseType != null
                        && type.BaseType.IsGenericType
                        && componentType.IsGenericType
                        && type.BaseType.GetGenericTypeDefinition() == componentType.GetGenericTypeDefinition()
            ).OrderBy(type => type.Name).ToList();

            var resultDictionary = new Dictionary<TypeInfo, CmsObjectAttribute>();

            foreach (var typeInfo in typeInfoList)
            {
                resultDictionary.Add(typeInfo, typeInfo.GetCustomAttribute<CmsObjectAttribute>());
            }
            return resultDictionary;
        }
    }
}

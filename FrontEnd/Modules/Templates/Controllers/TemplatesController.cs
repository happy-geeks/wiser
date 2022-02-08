using System;
using System.Collections.Generic;
using Api.Modules.Templates.Models.History;
using Api.Modules.Templates.Models.Preview;
using Api.Modules.Templates.Models.Template;
using FrontEnd.Core.Interfaces;
using FrontEnd.Modules.Base.Models;
using FrontEnd.Modules.Templates.Models;
using GeeksCoreLibrary.Modules.Templates.Enums;
using Microsoft.AspNetCore.Mvc;

namespace FrontEnd.Modules.Templates.Controllers
{
    [Area("Templates"), Route("Modules/Templates")]
    public class TemplatesController : Controller
    {
        private readonly IBaseService baseService;

        public TemplatesController(IBaseService baseService)
        {
            this.baseService = baseService;
        }

        public IActionResult Index()
        {
            var viewModel = baseService.CreateBaseViewModel<BaseModuleViewModel>();
            return View(viewModel);
        }

        [HttpPost, Route("DevelopmentTab")]
        public IActionResult DevelopmentTab([FromBody]DevelopmentTabViewModel tabViewData)
        {
            tabViewData.EditorType = tabViewData.TemplateSettings.Type switch
            {
                TemplateTypes.Unknown => "text",
                TemplateTypes.Html => "text/html",
                TemplateTypes.Css => "text/css",
                TemplateTypes.Scss => "text/x-scss",
                TemplateTypes.Js => "text/javascript",
                TemplateTypes.Query => "text/x-mysql",
                TemplateTypes.Normal => "text",
                TemplateTypes.Xml => "application/xml",
                _ => throw new ArgumentOutOfRangeException(nameof(tabViewData.TemplateSettings.Type), tabViewData.TemplateSettings.Type.ToString())
            };

            tabViewData.SettingsPartial = tabViewData.TemplateSettings.Type switch
            {
                TemplateTypes.Html => "HtmlSettings",
                TemplateTypes.Css => "CssSettings",
                TemplateTypes.Scss => "ScssSettings",
                TemplateTypes.Js => "JavascriptSettings",
                TemplateTypes.Query => "QuerySettings",
                _ => null
            };

            return PartialView("Tabs/DevelopmentTab", tabViewData);
        }

        [HttpPost, Route("HistoryTab")]
        public IActionResult HistoryTab([FromBody]TemplateHistoryOverviewModel tabViewData)
        {
            return PartialView("Tabs/HistoryTab", tabViewData);
        }

        [HttpPost, Route("PreviewTab")]
        public IActionResult PreviewTab([FromBody]List<PreviewProfileModel> tabViewData)
        {
            return PartialView("Tabs/PreviewTab", tabViewData);
        }

        [HttpPost, Route("PublishedEnvironments")]
        public IActionResult PublishedEnvironments([FromBody]TemplateSettingsModel tabViewData)
        {
            return PartialView("Partials/PublishedEnvironments", tabViewData);
        }
    }
}

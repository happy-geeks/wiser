using System.Collections.Generic;
using Api.Modules.Templates.Models.History;
using Api.Modules.Templates.Models.Preview;
using Api.Modules.Templates.Models.Template;
using FrontEnd.Core.Interfaces;
using FrontEnd.Modules.Base.Models;
using FrontEnd.Modules.Templates.Models;
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

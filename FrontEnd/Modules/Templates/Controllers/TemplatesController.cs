using System.Collections.Generic;
using Api.Modules.Templates.Models.Template;
using FrontEnd.Core.Interfaces;
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
            var viewModel = baseService.CreateBaseViewModel<TemplateOverviewViewModel>();
            viewModel.TreeView = new List<TemplateTreeViewModel>();
            return View(viewModel);
        }
    }
}

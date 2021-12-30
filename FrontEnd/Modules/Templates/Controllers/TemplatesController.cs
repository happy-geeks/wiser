using FrontEnd.Core.Interfaces;
using FrontEnd.Modules.Base.Models;
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
            return View(baseService.CreateBaseViewModel<BaseModuleViewModel>());
        }
    }
}

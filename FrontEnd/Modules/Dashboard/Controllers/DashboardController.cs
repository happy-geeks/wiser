using FrontEnd.Core.Interfaces;
using FrontEnd.Modules.Base.Models;
using Microsoft.AspNetCore.Mvc;

namespace FrontEnd.Modules.Dashboard.Controllers
{
    [Area("Dashboard"), Route("Modules/Dashboard")]
    public class DashboardController : Controller
    {
        private readonly IBaseService baseService;

        public DashboardController(IBaseService baseService)
        {
            this.baseService = baseService;
        }

        public IActionResult Index()
        {
            var viewModel = baseService.CreateBaseViewModel<BaseModuleViewModel>();

            return View(viewModel);
        }
    }
}

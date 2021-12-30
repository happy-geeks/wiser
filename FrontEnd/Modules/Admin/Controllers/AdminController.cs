using FrontEnd.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FrontEnd.Modules.Admin.Controllers
{
    [Area("Admin"), Route("Modules/Admin")]
    public class AdminController : Controller
    {
        private readonly IBaseService baseService;

        public AdminController(IBaseService baseService)
        {
            this.baseService = baseService;
        }
        
        public IActionResult Index()
        {
            return View(baseService.CreateBaseViewModel());
        }
    }
}

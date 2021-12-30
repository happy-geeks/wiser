using Microsoft.AspNetCore.Mvc;
using FrontEnd.Core.Interfaces;
using FrontEnd.Modules.Base.Models;

namespace FrontEnd.Modules.Search.Controllers
{
    [Area("Search"), Route("Modules/Search")]
    public class SearchController : Controller
    {
        private readonly IBaseService baseService;

        public SearchController(IBaseService baseService)
        {
            this.baseService = baseService;
        }
        
        public IActionResult Index()
        {
            return View(baseService.CreateBaseViewModel<BaseModuleViewModel>());
        }
    }
}

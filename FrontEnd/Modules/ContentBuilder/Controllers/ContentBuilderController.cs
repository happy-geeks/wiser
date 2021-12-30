using FrontEnd.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FrontEnd.Modules.ContentBuilder.Controllers
{
    [Area("ContentBuilder"), Route("Modules/ContentBuilder")]
    public class ContentBuilderController : Controller
    {
        private readonly IBaseService baseService;

        public ContentBuilderController(IBaseService baseService)
        {
            this.baseService = baseService;
        }
        
        public IActionResult Index()
        {
            return View(baseService.CreateBaseViewModel());
        }
    }
}

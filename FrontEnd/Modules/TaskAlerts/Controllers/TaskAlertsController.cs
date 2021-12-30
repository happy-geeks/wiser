using FrontEnd.Core.Interfaces;
using FrontEnd.Modules.Base.Models;
using Microsoft.AspNetCore.Mvc;

namespace FrontEnd.Modules.TaskAlerts.Controllers
{
    [Area("TaskAlerts"), Route("Modules/TaskAlerts")]
    public class TaskAlertsController : Controller
    {
        private readonly IBaseService baseService;

        public TaskAlertsController(IBaseService baseService)
        {
            this.baseService = baseService;
        }
        
        public IActionResult Index()
        {
            return View(baseService.CreateBaseViewModel<BaseModuleViewModel>());
        }
        
        [Route("History")]
        public IActionResult History()
        {
            return View(baseService.CreateBaseViewModel<BaseModuleViewModel>());
        }
    }
}

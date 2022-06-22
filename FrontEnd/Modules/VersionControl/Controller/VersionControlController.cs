using System;
using FrontEnd.Core.Interfaces;
using FrontEnd.Modules.Base.Models;
using FrontEnd.Modules.VersionControl.Models;
using Microsoft.AspNetCore.Mvc;

namespace FrontEnd.Modules.VersionControl.Controller
{
    [Area("VersionControl"), Route("Modules/VersionControl")]
    public class VersionControlController : Microsoft.AspNetCore.Mvc.Controller
    {
        private readonly IBaseService baseService;

        public VersionControlController(IBaseService baseService)
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

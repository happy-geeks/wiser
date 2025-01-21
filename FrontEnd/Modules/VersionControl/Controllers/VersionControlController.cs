using FrontEnd.Core.Interfaces;
using FrontEnd.Modules.Base.Models;
using Microsoft.AspNetCore.Mvc;

namespace FrontEnd.Modules.VersionControl.Controllers;

[Area("VersionControl"), Route("Modules/VersionControl")]
public class VersionControlController(IBaseService baseService) : Controller
{
    public IActionResult Index()
    {
        var viewModel = baseService.CreateBaseViewModel<BaseModuleViewModel>();
        return View(viewModel);
    }
}
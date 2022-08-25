using FrontEnd.Core.Interfaces;
using FrontEnd.Modules.Base.Models;
using Microsoft.AspNetCore.Mvc;

namespace FrontEnd.Modules.VersionControl.Controllers;

[Area("VersionControl"), Route("Modules/VersionControl")]
public class VersionControlController : Controller
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
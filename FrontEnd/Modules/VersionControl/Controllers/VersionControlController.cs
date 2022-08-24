using FrontEnd.Core.Interfaces;
using FrontEnd.Modules.Base.Models;
using Microsoft.AspNetCore.Mvc;

namespace FrontEnd.Modules.VersionControl.Controllers;

[Area("VersionControl"), Route("Modules/VersionControl")]
public class VersionControlController : Controller
{
    private readonly IBaseService _baseService;

    public VersionControlController(IBaseService baseService)
    {
        _baseService = baseService;
    }

    public IActionResult Index()
    {
        var viewModel = _baseService.CreateBaseViewModel<BaseModuleViewModel>();
        return View(viewModel);
    }
}
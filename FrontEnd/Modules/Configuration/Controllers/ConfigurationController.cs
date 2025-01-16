using FrontEnd.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FrontEnd.Modules.Configuration.Controllers;

[Area("Configuration"), Route("Modules/Configuration")]
public class ConfigurationController(IBaseService baseService) : Controller
{
    public IActionResult Index()
    {
        var viewModel = baseService.CreateBaseViewModel();

        return View(viewModel);
    }
}
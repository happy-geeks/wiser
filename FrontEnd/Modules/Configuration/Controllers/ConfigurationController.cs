using FrontEnd.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FrontEnd.Modules.Configuration.Controllers;

[Area("Configuration"), Route("Modules/Configuration")]
public class ConfigurationController : Controller
{
    private readonly IBaseService baseService;

    public ConfigurationController(IBaseService baseService)
    {
        this.baseService = baseService;
    }
        
    public IActionResult Index()
    {
        var viewModel = baseService.CreateBaseViewModel();

        return View(viewModel);
    }
}
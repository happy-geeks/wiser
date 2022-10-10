using FrontEnd.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FrontEnd.Modules.Communication.Controllers;

[Area("Communication"), Route("Modules/Communication")]
public class CommunicationController : Controller
{
    private readonly IBaseService baseService;

    public CommunicationController(IBaseService baseService)
    {
        this.baseService = baseService;
    }
        
    public IActionResult Index()
    {
        var viewModel = baseService.CreateBaseViewModel();

        return View(viewModel);
    }
}
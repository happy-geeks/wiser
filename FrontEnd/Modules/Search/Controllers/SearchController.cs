using FrontEnd.Core.Interfaces;
using FrontEnd.Modules.Base.Models;
using Microsoft.AspNetCore.Mvc;

namespace FrontEnd.Modules.Search.Controllers;

[Area("Search"), Route("Modules/Search")]
public class SearchController(IBaseService baseService) : Controller
{
    public IActionResult Index()
    {
        return View(baseService.CreateBaseViewModel<BaseModuleViewModel>());
    }
}
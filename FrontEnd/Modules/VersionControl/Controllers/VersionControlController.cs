using Microsoft.AspNetCore.Mvc;

namespace FrontEnd.Modules.VersionControl.Controllers;

public class VersionControlController : Controller
{
    // GET
    public IActionResult Index()
    {
        return View();
    }
}
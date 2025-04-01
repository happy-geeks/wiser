using System;
using System.IO;
using System.Linq;
using FrontEnd.Core.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace FrontEnd.Core.Controllers;

public class HomeController(IBaseService baseService, IWebHostEnvironment webHostEnvironment) : Controller
{
    public IActionResult Index()
    {
        var viewModel = baseService.CreateBaseViewModel();

        var partnerStylesDirectory = new DirectoryInfo(Path.Combine(webHostEnvironment.ContentRootPath, "Core/Css/partner"));
        if (partnerStylesDirectory.Exists)
        {
            viewModel.LoadPartnerStyle = partnerStylesDirectory.GetFiles("*.css").Any(f => Path.GetFileNameWithoutExtension(f.Name).Equals(viewModel.SubDomain, StringComparison.OrdinalIgnoreCase));
        }

        return View(viewModel);
    }
}
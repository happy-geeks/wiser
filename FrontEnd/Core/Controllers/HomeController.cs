using System;
using System.IO;
using System.Linq;
using FrontEnd.Core.Interfaces;
using FrontEnd.Core.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FrontEnd.Core.Controllers
{
    public class HomeController : Controller
    {
        private readonly IBaseService baseService;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly FrontEndSettings frontEndSettings;

        public HomeController(IOptions<FrontEndSettings> frontEndSettings, IBaseService baseService, IWebHostEnvironment webHostEnvironment)
        {
            this.baseService = baseService;
            this.webHostEnvironment = webHostEnvironment;
            this.frontEndSettings = frontEndSettings.Value;
        }

        public IActionResult Index()
        {
            var viewModel = baseService.CreateBaseViewModel();
            
            var partnerStylesDirectory = new DirectoryInfo(Path.Combine(webHostEnvironment.ContentRootPath, @"Core/Css/partner"));
            if (partnerStylesDirectory.Exists)
            {
                viewModel.LoadPartnerStyle = partnerStylesDirectory.GetFiles("*.css").Any(f => Path.GetFileNameWithoutExtension(f.Name).Equals(viewModel.SubDomain, StringComparison.OrdinalIgnoreCase));
            }
            
            return View(viewModel);
        }
    }
}

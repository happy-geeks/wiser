using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FrontEnd.Core.Interfaces;
using FrontEnd.Core.Models;
using GeeksCoreLibrary.Core.Extensions;
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
        private readonly IHomeService homeService;

        public HomeController(IOptions<FrontEndSettings> frontEndSettings, IBaseService baseService, IWebHostEnvironment webHostEnvironment, IHomeService homeService)
        {
            this.baseService = baseService;
            this.webHostEnvironment = webHostEnvironment;
            this.frontEndSettings = frontEndSettings.Value;
            this.homeService = homeService;
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

        [HttpGet]
        [Route("login")]
        public async Task<IActionResult> LoginAsync(string token)
        {
            //await homeService.Login();
            
            return View("Login");
        }
    }
}

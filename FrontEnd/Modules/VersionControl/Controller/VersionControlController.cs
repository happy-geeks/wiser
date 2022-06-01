using System;
using FrontEnd.Core.Interfaces;
using FrontEnd.Modules.VersionControl.Models;
using Microsoft.AspNetCore.Mvc;

namespace FrontEnd.Modules.VersionControl.Controller
{
    [Area("VersionControl"), Route("Modules/VersionControl")]
    public class VersionControlController : Microsoft.AspNetCore.Mvc.Controller
    {
        private readonly IBaseService baseService;

        public VersionControlController(IBaseService baseService)
        {
            this.baseService = baseService;
        }

        public IActionResult Index([FromQuery]VersionControlViewModel viewModel)
        {
            viewModel ??= new VersionControlViewModel();
            var defaultModel = baseService.CreateBaseViewModel();

            viewModel.Settings = defaultModel.Settings;
            viewModel.WiserVersion = defaultModel.WiserVersion;
            viewModel.SubDomain = defaultModel.SubDomain;
            viewModel.IsTestEnvironment = defaultModel.IsTestEnvironment;
            viewModel.Wiser1BaseUrl = defaultModel.Wiser1BaseUrl;
            viewModel.ApiAuthenticationUrl = defaultModel.ApiAuthenticationUrl;
            viewModel.ApiRoot = defaultModel.ApiRoot;
            viewModel.LoadPartnerStyle = defaultModel.LoadPartnerStyle;

            if (!String.IsNullOrWhiteSpace(viewModel.SaveButtonText))
            {
                viewModel.SaveButtonText = "Opslaan";
            }

            if (viewModel.IframeMode)
            {
                viewModel.BodyCssClass = "iframe";
            }


           
            if (viewModel.ModuleId == 0)
            {
                viewModel.ModuleId = 6001;
            }
            Console.WriteLine("Test");
            return View(viewModel);
        }
    }
}

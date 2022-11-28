using FrontEnd.Core.Interfaces;
using FrontEnd.Modules.ContentBuilder.Models;
using Microsoft.AspNetCore.Mvc;

namespace FrontEnd.Modules.ContentBuilder.Controllers
{
    [Area("ContentBuilder"), Route("Modules/ContentBuilder")]
    public class ContentBuilderController : Controller
    {
        private readonly IBaseService baseService;

        public ContentBuilderController(IBaseService baseService)
        {
            this.baseService = baseService;
        }
        
        public IActionResult Index([FromQuery]ContentBuilderViewModel viewModel)
        {
            viewModel ??= new ContentBuilderViewModel();
            var defaultModel = baseService.CreateBaseViewModel();

            viewModel.Settings = defaultModel.Settings;
            viewModel.WiserVersion = defaultModel.WiserVersion;
            viewModel.SubDomain = defaultModel.SubDomain;
            viewModel.IsTestEnvironment = defaultModel.IsTestEnvironment;
            viewModel.Wiser1BaseUrl = defaultModel.Wiser1BaseUrl;
            viewModel.ApiAuthenticationUrl = defaultModel.ApiAuthenticationUrl;
            viewModel.ApiRoot = defaultModel.ApiRoot;
            viewModel.LoadPartnerStyle = defaultModel.LoadPartnerStyle;
            
            return View(viewModel);
        }
    }
}

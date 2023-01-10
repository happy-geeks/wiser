using FrontEnd.Core.Interfaces;
using FrontEnd.Modules.ContentBox.Models;
using Microsoft.AspNetCore.Mvc;

namespace FrontEnd.Modules.ContentBox.Controllers;

[Area("ContentBox"), Route("Modules/ContentBox")]
public class ContentBoxController : Controller
{
    private readonly IBaseService baseService;

    public ContentBoxController(IBaseService baseService)
    {
        this.baseService = baseService;
    }
        
    public IActionResult Index([FromQuery]ContentBoxViewModel viewModel)
    {
        viewModel ??= new ContentBoxViewModel();
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
using FrontEnd.Core.Interfaces;
using FrontEnd.Modules.FileManager.Models;
using Microsoft.AspNetCore.Mvc;

namespace FrontEnd.Modules.FileManager.Controllers;

[Area("FileManager"), Route("Modules/FileManager")]
public class FileManagerController : Controller
{
    private readonly IBaseService baseService;

    public FileManagerController(IBaseService baseService)
    {
        this.baseService = baseService;
    }
        
    public IActionResult Index([FromQuery]FileManagerViewModel viewModel)
    {
        viewModel ??= new FileManagerViewModel();
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
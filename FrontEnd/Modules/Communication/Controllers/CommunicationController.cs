using FrontEnd.Core.Interfaces;
using FrontEnd.Modules.Communication.Models;
using Microsoft.AspNetCore.Mvc;

namespace FrontEnd.Modules.Communication.Controllers;

[Area("Communication"), Route("Modules/Communication")]
public class CommunicationController(IBaseService baseService) : Controller
{
    public IActionResult Index()
    {
        var viewModel = baseService.CreateBaseViewModel();

        return View(viewModel);
    }

    [Route("Settings")]
    public IActionResult Settings([FromQuery]CommunicationSettingsViewModel viewModel)
    {
        viewModel ??= new CommunicationSettingsViewModel();
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
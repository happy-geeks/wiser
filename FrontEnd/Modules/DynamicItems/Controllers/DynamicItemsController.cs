using System;
using FrontEnd.Core.Interfaces;
using FrontEnd.Modules.DynamicItems.Models;
using Microsoft.AspNetCore.Mvc;

namespace FrontEnd.Modules.DynamicItems.Controllers;

[Area("DynamicItems"), Route("Modules/DynamicItems")]
public class DynamicItemsController(IBaseService baseService) : Controller
{
    public IActionResult Index([FromQuery]DynamicItemsViewModel viewModel)
    {
        viewModel ??= new DynamicItemsViewModel();
        var defaultModel = baseService.CreateBaseViewModel();

        viewModel.Settings = defaultModel.Settings;
        viewModel.WiserVersion = defaultModel.WiserVersion;
        viewModel.SubDomain = defaultModel.SubDomain;
        viewModel.IsTestEnvironment = defaultModel.IsTestEnvironment;
        viewModel.Wiser1BaseUrl = defaultModel.Wiser1BaseUrl;
        viewModel.ApiAuthenticationUrl = defaultModel.ApiAuthenticationUrl;
        viewModel.ApiRoot = defaultModel.ApiRoot;
        viewModel.ApiRootV4 = defaultModel.ApiRootV4;
        viewModel.LoadPartnerStyle = defaultModel.LoadPartnerStyle;

        if (!String.IsNullOrWhiteSpace(viewModel.SaveButtonText))
        {
            viewModel.SaveButtonText = "Opslaan";
        }

        if (viewModel.IframeMode)
        {
            viewModel.BodyCssClass = "iframe";
        }

        return View(viewModel);
    }
}
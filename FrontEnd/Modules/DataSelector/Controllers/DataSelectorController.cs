﻿using System;
using FrontEnd.Core.Interfaces;
using FrontEnd.Modules.DataSelector.Models;
using Microsoft.AspNetCore.Mvc;

namespace FrontEnd.Modules.DataSelector.Controllers;

[Area("DataSelector"), Route("Modules/DataSelector")]
public class DataSelectorController(IBaseService baseService) : Controller
{
    public IActionResult Index(bool embedded = false, string embedoptions = null, bool exportMode = false)
    {
        var viewModel = baseService.CreateBaseViewModel<DataSelectorViewModel>();
        if (embedded)
        {
            viewModel.BodyCssClass = "embedded";
            viewModel.EmbedOptions.AddRange((embedoptions ?? "").Split([','], StringSplitOptions.RemoveEmptyEntries));
        }
        viewModel.ExportMode = exportMode;

        return View(viewModel);
    }
}
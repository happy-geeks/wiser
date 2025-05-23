﻿using FrontEnd.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FrontEnd.Modules.Admin.Controllers;

[Area("Admin"), Route("Modules/Admin")]
public class AdminController(IBaseService baseService) : Controller
{
    public IActionResult Index()
    {
        return View(baseService.CreateBaseViewModel());
    }
}
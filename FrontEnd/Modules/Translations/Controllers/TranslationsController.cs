﻿using Microsoft.AspNetCore.Mvc;
using FrontEnd.Core.Interfaces;
using FrontEnd.Modules.Base.Models;

namespace FrontEnd.Modules.Translations.Controllers
{
    [Area("Translations"), Route("Modules/Translations")]
    public class TranslationsController : Controller
    {
        private readonly IBaseService baseService;

        public TranslationsController(IBaseService baseService)
        {
            this.baseService = baseService;
        }
        
        public IActionResult Index()
        {
            return View(baseService.CreateBaseViewModel<BaseModuleViewModel>());
        }
    }
}

using System.Collections.Generic;
using Api.Modules.Translations.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Api.Modules.Translations.Controllers;

/// <summary>
/// Controller which receives instructions from Javascript to transmit translations for a module. 
/// </summary>

[Route("api/v3/translations")]
[ApiController]
public class TranslationsController : Controller
{
    private readonly ITranslationsService translationsService;

    /// <summary>
    /// Creates a new instance of TranslationsController.
    /// </summary>
    public TranslationsController(ITranslationsService translationsService)
    {
        this.translationsService = translationsService;
    }
    
    /// <summary>
    /// Gets a resource file as json. Automatically selects the resource file of the current culture
    /// </summary>
    /// <param name="pathToResourceFileDirectory">Path to the resource file</param>
    /// <param name="cultureAndCountry">Two letter culture dash two letter country, e.g: en-GB or en-US</param>
    /// <returns>Json of the resource file</returns>
    [HttpGet]
    [Route("get-translations-for-module")]
    public IActionResult GetCurrentCultureResourceAsJson(string pathToResourceFileDirectory, string cultureAndCountry)
    {
        Dictionary<string, string> resourceFileAsDict = translationsService.GetCurrentCultureResourceAsDict(pathToResourceFileDirectory, cultureAndCountry);
        if (resourceFileAsDict != null)
        {
            return Json(JsonConvert.SerializeObject(resourceFileAsDict));
        }
        else
        {
            // No proper translation is found, so return empty json (cannot return null)
            return Json(new EmptyResult());
        }
    }
}
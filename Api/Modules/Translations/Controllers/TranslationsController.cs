using Api.Modules.Translations.Interfaces;
using Api.Modules.Translations.Services;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;

namespace Api.Modules.Translations.Controllers;

/// <summary>
/// Controller which receives instructions from Javascript to transmit translations for a module. 
/// </summary>

[Route("api/v3/translation")]
[ApiController]
[Authorize] 
public class TranslationsController : Controller
{
    private readonly ITranslationsService translationsService;

    /// <summary>
    /// Creates a new instance of TranslationController.
    /// </summary>
    public TranslationsController(ITranslationsService translationsService)
    {
        this.translationsService = translationsService;
    }

    /// <summary>
    /// Retrieve a translation of a string for the user's currently set culture.
    /// </summary>
    /// <param name="translationKey"></param>
    /// <returns>Localized string. Returns the key if no localization is found</returns>
    [HttpGet]
    public string GetTranslation(string translationKey)
    {
        return translationsService.GetStringTranslation(translationKey);
    }

    /// <summary>
    /// Retrieves a translation of an input string containing HTML without translating the HTML.
    /// Used for dynamic content.
    /// </summary>
    /// <param name="htmlTranslationKey"></param>
    /// <returns>Localized string. Returns the key if no localization is found</returns>
    [HttpGet]
    public string GetHtmlTranslation(string htmlTranslationKey)
    {
        return translationsService.GetHtmlTranslation(htmlTranslationKey);
    }
}
using System.Collections.Generic;
using System.Net.Mime;
using System.Threading.Tasks;
using Api.Modules.Languages.Interfaces;
using GeeksCoreLibrary.Modules.Languages.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Languages.Controllers;

/// <summary>
/// Controller for all operations that have something to do with Wiser languages.
/// </summary>
[Route("api/v3/[controller]")]
[ApiController]
[Authorize]
[Consumes(MediaTypeNames.Application.Json)]
[Produces(MediaTypeNames.Application.Json)]
public class LanguagesController
{
    private readonly ILanguagesService languagesService;

    /// <summary>
    /// Creates a new instance of <see cref="LanguagesController"/>.
    /// </summary>
    public LanguagesController(ILanguagesService languagesService)
    {
        this.languagesService = languagesService;
    }

    /// <summary>
    /// Gets all languages that are configured in Wiser.
    /// </summary>
    /// <returns>A list of <see cref="LanguageModel">LanguageModel</see> with all configured languages.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<LanguageModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsync()
    {
        return (await languagesService.GetAllLanguagesAsync()).GetHttpResponseMessage();
    }
    
    /// <summary>
    /// Gets all values from the translations module.
    /// </summary>
    /// <returns>A dictionary where the key is the translation key and the value is the translation of the default language.</returns>
    [HttpGet]
    [Route("translations")]
    [ProducesResponseType(typeof(List<LanguageModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTranslationsAsync()
    {
        return (await languagesService.GetAllTranslationsAsync()).GetHttpResponseMessage();
    }
}
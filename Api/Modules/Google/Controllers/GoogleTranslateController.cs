using System.Collections.Generic;
using System.Net.Mime;
using System.Threading.Tasks;
using Api.Modules.ContentBuilder.Models;
using Api.Modules.Google.Interfaces;
using Google.Cloud.Translation.V2;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Google.Controllers;

/// <summary>
/// Controller for translating texts via the Google Translate API.
/// </summary>
[Route("api/v3/google/translate")]
[ApiController]
[Authorize]
[Consumes(MediaTypeNames.Application.Json)]
[Produces(MediaTypeNames.Application.Json)]
public class GoogleTranslateController : Controller
{
    private readonly IGoogleTranslateService googleTranslateService;

    /// <summary>
    /// Creates a new instance of <see cref="GoogleTranslateController"/>.
    /// </summary>
    public GoogleTranslateController(IGoogleTranslateService googleTranslateService)
    {
        this.googleTranslateService = googleTranslateService;
    }
        
    /// <summary>
    /// Gets all snippets for the content builder. Snippets are pieces of HTML that the user can add in the Content Builder.
    /// </summary>
    /// <returns>A list with zero or more <see cref="ContentBuilderSnippetModel"/>.</returns>
    [HttpPost]
    [Route("text")]
    [ProducesResponseType(typeof(TranslationResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSnippetsAsync([FromBody]string text, [FromQuery]string targetLanguageCode, [FromQuery]string sourceLanguageCode = null)
    {
        return (await googleTranslateService.TranslateTextAsync(new List<string> { text }, targetLanguageCode, sourceLanguageCode)).GetHttpResponseMessage();
    }
}
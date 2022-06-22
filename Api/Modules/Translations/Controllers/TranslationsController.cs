using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;
using Api.Modules.Translations.Interfaces;
using System.Threading.Tasks;
using Api.Modules.Translations.Services;

namespace Api.Modules.Translations.Controllers
{
    /// <summary>
    /// Controller for all CRUD functions for translations.
    /// </summary>
    [Route("api/v3/[controller]")]
    [ApiController]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    public class TranslationsController : Controller
    {
        private readonly ITranslationsService translationsSerice = new TranslationsService();

        /// <summary>
        /// Returns translations for specified location and language in json format
        /// </summary>
        /// <param name="location">The location of the json file including the file name(without the file extension).</param>
        /// <param name="cultureCode">The two digit code that specifies the language of the translations.</param>
        /// <returns>A json string with all translations.</returns>
        [HttpGet]
        public async Task<IActionResult> GetTranslations(string location, string cultureCode)
        {
            return Content(translationsSerice.GetTranslations(location, cultureCode), "application/json");
        }
    }
}

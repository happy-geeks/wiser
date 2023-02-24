using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Core.Models;
using Api.Core.Services;
using GeeksCoreLibrary.Modules.Languages.Models;

namespace Api.Modules.Languages.Interfaces;

/// <summary>
/// A service for getting languages and translations from Wiser.
/// </summary>
public interface ILanguagesService
{
    /// <summary>
    /// Gets all languages that are configured in Wiser.
    /// </summary>
    /// <returns>A list of <see cref="LanguageModel">LanguageModel</see> with all configured languages.</returns>
    Task<ServiceResult<List<LanguageModel>>> GetAllLanguagesAsync();

    /// <summary>
    /// Gets all values from the translations module.
    /// </summary>
    /// <returns>A dictionary where the key is the translation key and the value is the translation of the default language.</returns>
    Task<ServiceResult<List<SimpleKeyValueModel>>> GetAllTranslationsAsync();
}
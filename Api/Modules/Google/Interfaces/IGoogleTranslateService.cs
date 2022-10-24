using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Core.Services;
using Google.Cloud.Translation.V2;

namespace Api.Modules.Google.Interfaces;

/// <summary>
/// Service for translating texts via the Google Translate API.
/// </summary>
public interface IGoogleTranslateService
{
    /// <summary>
    /// Translates plain text into something else, using the Google Translate API.
    /// </summary>
    /// <param name="textItems">The text strings to translate. Must not be null or contain null elements.</param>
    /// <param name="targetLanguageCode">The code for the language to translate into. Must not be null.</param>
    /// <param name="sourceLanguageCode">The code for the language to translate from. May be null, in which case the server will detect the source language.</param>
    /// <returns>A list of translations. This will be the same size as <paramref name="textItems"/>, in the same order.</returns>
    Task<ServiceResult<IList<TranslationResult>>> TranslateTextAsync(IEnumerable<string> textItems, string targetLanguageCode, string sourceLanguageCode = null);

    /// <summary>
    /// Translates HTML into something else, using the Google Translate API.
    /// </summary>
    /// <param name="htmlItems">The HTML strings to translate. Must not be null or contain null elements.</param>
    /// <param name="targetLanguageCode">The code for the language to translate into. Must not be null.</param>
    /// <param name="sourceLanguageCode">The code for the language to translate from. May be null, in which case the server will detect the source language.</param>
    /// <returns>A list of translations. This will be the same size as <paramref name="htmlItems"/>, in the same order.</returns>
    Task<ServiceResult<IList<TranslationResult>>> TranslateHtmlAsync(IEnumerable<string> htmlItems, string targetLanguageCode, string sourceLanguageCode = null);
}
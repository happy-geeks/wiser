using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.Google.Exceptions;
using Api.Modules.Google.Interfaces;
using Api.Modules.Google.Models;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Helpers;
using Google.Cloud.Translation.V2;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Api.Modules.Google.Services;

/// <inheritdoc cref="IGoogleTranslateService" />
public class GoogleTranslateService : IGoogleTranslateService, IScopedService
{
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly ILogger<GoogleTranslateService> logger;
    private readonly GoogleSettings googleSettings;

    /// <summary>
    /// Creates a new instance of <see cref="GoogleTranslateService"/>.
    /// </summary>
    public GoogleTranslateService(IOptions<GoogleSettings> googleSettings, IHttpContextAccessor httpContextAccessor, ILogger<GoogleTranslateService> logger)
    {
        this.httpContextAccessor = httpContextAccessor;
        this.logger = logger;
        this.googleSettings = googleSettings.Value;
    }

    /// <inheritdoc />
    public async Task<ServiceResult<IList<TranslationResult>>> TranslateTextAsync(IEnumerable<string> textItems, string targetLanguageCode, string sourceLanguageCode = null)
    {
        try
        {
            var client = CreateTranslationClient();
            var result = await client.TranslateTextAsync(textItems, targetLanguageCode, sourceLanguageCode);
            return new ServiceResult<IList<TranslationResult>>(result);
        }
        catch (InvalidApiKeyException invalidApiKeyException)
        {
            logger.LogWarning(invalidApiKeyException, "User tried to translate, but there is no API key configured in Wiser.");
            return new ServiceResult<IList<TranslationResult>>
            {
                ErrorMessage = "De vertaalmodule is niet beschikbaar omdat er geen API Key voor de Google Translate API is ingesteld. Neem a.u.b. contact op met ons.",
                StatusCode = HttpStatusCode.Conflict
            };
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult<IList<TranslationResult>>> TranslateHtmlAsync(IEnumerable<string> htmlItems, string targetLanguageCode, string sourceLanguageCode = null)
    {
        try
        {
            var client = CreateTranslationClient();
            var result = await client.TranslateHtmlAsync(htmlItems, targetLanguageCode, sourceLanguageCode);
            return new ServiceResult<IList<TranslationResult>>(result);
        }
        catch (InvalidApiKeyException invalidApiKeyException)
        {
            logger.LogWarning(invalidApiKeyException, "User tried to translate, but there is no API key configured in Wiser.");
            return new ServiceResult<IList<TranslationResult>>
            {
                ErrorMessage = "De vertaalmodule is niet beschikbaar omdat er geen API Key voor de Google Translate API is ingesteld. Neem a.u.b. contact op met ons.",
                StatusCode = HttpStatusCode.Conflict
            };
        }
    }

    private TranslationClient CreateTranslationClient()
    {
        if (String.IsNullOrWhiteSpace(googleSettings.TranslationsApiKey))
        {
            throw new InvalidApiKeyException($"No API key found in app settings for Google Translations API.");
        }

        var client = TranslationClient.CreateFromApiKey(googleSettings.TranslationsApiKey);

        //client.Service.HttpClient.DefaultRequestHeaders.Referrer = HttpContextHelpers.GetOriginalRequestUri(httpContextAccessor.HttpContext);
        client.Service.HttpClient.DefaultRequestHeaders.Referrer = new Uri("https://wiserdemo.wiser3.nl");
        return client;
    }
}
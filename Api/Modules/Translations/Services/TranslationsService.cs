using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Resources;
using Api.Core.Services;
using Api.Modules.Translations.Controllers;
using Api.Modules.Translations.Interfaces;
using DocumentFormat.OpenXml.ExtendedProperties;
using DocumentFormat.OpenXml.Vml.Office;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using IdentityServer4.Models;
using Microsoft.Extensions.Localization;
using Microsoft.AspNetCore.Mvc.Localization;
using NUglify.Helpers;

namespace Api.Modules.Translations.Services;


/// <summary>
/// Service for translation services, for example, retrieving the translation of a module.
/// </summary>
public class TranslationsService : ITranslationsService
{
    private readonly IStringLocalizer<TranslationsService> localizer;
    private readonly IHtmlLocalizer<TranslationsService> htmlLocalizer;

    /// <summary>
    /// Creates a new instance of TranslationService
    /// </summary>
    public TranslationsService(IStringLocalizer<TranslationsService> localizer, 
                                IHtmlLocalizer<TranslationsService> htmlLocalizer)
    {
        this.localizer = localizer;
        this.htmlLocalizer = htmlLocalizer;
    }

    /// <inheritdoc />
    public string GetStringTranslation(string translationKey)
    {
        return localizer[translationKey];
    }

    /// <inheritdoc />
    public string GetHtmlTranslation(string translationKey)
    {
        return htmlLocalizer[translationKey].Value;
    }

    /// <inheritdoc />
    public Dictionary<string, string> GetCurrentCultureResourceAsList(string pathToResourceFileDirectory)
    {
        Dictionary<string, string> resourceDictToReturn = new Dictionary<string, string>(); 
        // Create a resourcemanager which will help fetch a current-culture resource file in the given directory.
        // Second parameter retrieves the current assembly (BackEnd)
        ResourceManager resourceManager =
            new ResourceManager(pathToResourceFileDirectory, typeof(TranslationsService).Assembly);
        
        // Get the resource file from the given directory based on the current culture.
        // Do not create a new one if the requested culture doesn't exist,
        // but do try to find a parent culture (e.g. en-GB parent is en)
        ResourceSet resourceSet = resourceManager.GetResourceSet(CultureInfo.CurrentUICulture, 
                                                                    false, true);
        if (resourceSet != null)
        {
            foreach (DictionaryEntry entry in resourceSet)
            {
                string resourceKey = entry.Key.ToString();
                string translationValue = "";
                // Value should, but might not be, a string- So check if it's null first, then convert to string
                // and check if empty.
                if (entry.Value != null && entry.Value.ToString() != null)
                {
                    translationValue = entry.Value.ToString();
                }
                else
                {
                    // If the key does not have a translation, instead the key should be the translation/default value.
                    translationValue = resourceKey;
                }
                translationValue = entry.Key.ToString();
                resourceDictToReturn.Add(resourceKey,translationValue);
            }
        }
        return resourceDictToReturn;
    }
}
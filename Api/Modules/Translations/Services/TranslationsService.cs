using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Resources;
using Api.Modules.Translations.Interfaces;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using Microsoft.Extensions.Localization;

namespace Api.Modules.Translations.Services;


/// <summary>
/// Service for translation services, for example, retrieving the translation of a module.
/// </summary>
public class TranslationsService : ITranslationsService, IScopedService
{
    private readonly IStringLocalizer<TranslationsService> localizer;

    /// <summary>
    /// Creates a new instance of TranslationService
    /// </summary>
    public TranslationsService(IStringLocalizer<TranslationsService> localizer)
    {
        this.localizer = localizer;
    }

    /// <inheritdoc />
    public Dictionary<string, string> GetCurrentCultureResourceAsDict(string pathToResourceFileDirectory, string cultureAndCountry)
    {
        var resourceFileAsDict = new Dictionary<string, string>(); 
        // Create a resourcemanager which will help fetch a current-culture resource file in the given directory.
        // Second parameter retrieves the current assembly (BackEnd)
        var resourceManager = new ResourceManager(pathToResourceFileDirectory, System.Reflection.Assembly.GetExecutingAssembly());
        
        // Get the resource file from the given directory based on the current culture.
        // Do not create a new one if the requested culture doesn't exist,
        // but do try to find a parent culture (e.g. en-GB parent is en)
        ResourceSet resourceSet;
        try
        {
            var apiDefinedCulture = new CultureInfo(cultureAndCountry);
            resourceSet = resourceManager.GetResourceSet(apiDefinedCulture, true, true);
        }
        catch (MissingManifestResourceException e)
        {
            // Can't find any appropriate resource file for the culture or its parents, so return the default (english) instead.
            Console.Error.Write("Could not find the resource file for the given culture. Returning English instead.");
            try
            {
                var apiDefinedCulture = new CultureInfo("en");
                resourceSet = resourceManager.GetResourceSet(apiDefinedCulture, true, true);
            }
            catch (MissingManifestResourceException eM)
            {
                // If this is reached, that means the english version of the requested resource file doesn't exist.
                // So, that probably means the resource file itself doesn't exist. Throw an error instead.
                Console.Error.Write("Could not find the resource file in English. Check if the resource directory exists.");
                throw new MissingManifestResourceException();
            }
        }
        
        if (resourceSet != null)
        {
            foreach (DictionaryEntry entry in resourceSet)
            {
                var resourceKey = entry.Key.ToString();
                var translationValue = "";
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
                resourceFileAsDict.Add(resourceKey,translationValue);
            }
        }
        return resourceFileAsDict;
    }
}
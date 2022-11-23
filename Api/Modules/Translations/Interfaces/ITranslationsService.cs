using System;
using System.Collections.Generic;

namespace Api.Modules.Translations.Interfaces;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;

public interface ITranslationsService
{
    /// <summary>
    /// Retrieve a translation of a string for the user's currently set culture.
    /// </summary>
    /// <param name="translationKey"></param>
    /// <returns>Localized string. Returns the key if no localization is found</returns>
    string GetStringTranslation(string translationKey);
    
    /// <summary>
    /// Retrieves a translation of an input string containing HTML without translating the HTML.
    /// Used for dynamic content.
    /// </summary>
    /// <param name="translationKey"></param>
    /// <returns>Localized string. Returns the key if no localization is found</returns>
    string GetHtmlTranslation(string translationKey);

    /// <summary>
    /// Retrieves a resource file from a directory, selects it according to the current
    /// culture, and returns it as a list of strings.
    /// </summary>
    /// <param name="pathToResourceFileDirectory">Path to the location of the resource file, excluding the resource file.</param>
    /// <returns>List of strings</returns>
    Dictionary<string, string> GetCurrentCultureResourceAsList(string pathToResourceFileDirectory);


}
using System;
using System.Collections.Generic;

namespace Api.Modules.Translations.Interfaces;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;

public interface ITranslationsService
{
    /// <summary>
    /// Retrieves a resource file from a directory, selects it according to the current
    /// culture, and returns it as a dict of strings.
    /// </summary>
    /// <param name="pathToResourceFileDirectory">Path to the location of the resource file, excluding the resource file.
    /// Should be located somewhere in Api/Modules/Translations/Resources</param>
    /// <param name="cultureAndCountry">Two letter culture dash two letter country, e.g: en-GB or en-US</param>
    /// <returns>Dict of strings</returns>
    ServiceResult<Dictionary<string,string>> GetCurrentCultureResourceAsDict(string pathToResourceFileDirectory, string cultureAndCountry);


}
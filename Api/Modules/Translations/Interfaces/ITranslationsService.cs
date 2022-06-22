namespace Api.Modules.Translations.Interfaces
{
    public interface ITranslationsService
    {
        /// <summary>
        /// Returns translations for specified location and language in string format
        /// </summary>
        /// <param name="location">The location of the specified file + the file name(without file extension).</param>
        /// <param name="cultureCode">Two digit code specifiying the language.</param>
        /// <returns>String with the contents of the translation file</returns>
        string GetTranslations(string location, string cultureCode);
    }
}

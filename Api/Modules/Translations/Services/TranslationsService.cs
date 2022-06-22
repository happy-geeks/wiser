using Api.Modules.Translations.Interfaces;
using System;
using System.IO;

namespace Api.Modules.Translations.Services
{
    public class TranslationsService : ITranslationsService
    {
        /// <inheritdoc />
        public string GetTranslations(string location, string cultureCode)
        {
            string jsonString = "";

            try
            {
                jsonString = File.ReadAllText($"Modules\\Translations\\Resources\\{location}.{cultureCode}.json");
            }
            catch (Exception _)
            {
                Console.WriteLine(_);
            }
   
            return jsonString;
        }
    }
}

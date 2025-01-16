using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Api.Core.Models;
using Api.Core.Services;
using Api.Modules.Languages.Interfaces;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Languages.Models;
using GeeksCoreLibrary.Modules.Objects.Interfaces;

namespace Api.Modules.Languages.Services;

/// <inheritdoc cref="ILanguagesService" />
public class LanguagesService : ILanguagesService, IScopedService
{
    private readonly GeeksCoreLibrary.Modules.Languages.Interfaces.ILanguagesService gclLanguagesService;
    private readonly IDatabaseConnection databaseConnection;
    private readonly IObjectsService objectsService;

    /// <summary>
    /// Creates a new instance of <see cref="LanguagesService"/>.
    /// </summary>
    public LanguagesService(GeeksCoreLibrary.Modules.Languages.Interfaces.ILanguagesService gclLanguagesService, IDatabaseConnection databaseConnection, IObjectsService objectsService)
    {
        this.gclLanguagesService = gclLanguagesService;
        this.databaseConnection = databaseConnection;
        this.objectsService = objectsService;
    }

    /// <inheritdoc />
    public async Task<ServiceResult<List<LanguageModel>>> GetAllLanguagesAsync()
    {
        var results = await gclLanguagesService.GetAllLanguagesAsync();
        return new ServiceResult<List<LanguageModel>>(results);
    }

    /// <inheritdoc />
    public async Task<ServiceResult<List<SimpleKeyValueModel>>> GetAllTranslationsAsync()
    {
        var defaultLanguage = await gclLanguagesService.GetLanguageCodeAsync();
        var translationsItemId = await objectsService.FindSystemObjectByDomainNameAsync("W2LANGUAGES_TranslationsItemId");

        var query = $"""
                     SELECT 
                     	`key`,
                     	CONCAT_WS('', value, long_value) AS value
                     FROM {WiserTableNames.WiserItemDetail}
                     WHERE item_id = ?itemId
                     AND language_code = ?languageCode
                     GROUP BY `key`
                     ORDER BY `key` ASC
                     """;
        
        databaseConnection.AddParameter("itemId", translationsItemId);
        databaseConnection.AddParameter("languageCode", defaultLanguage);
        var dataTable = await databaseConnection.GetAsync(query);

        var results = dataTable.Rows.Cast<DataRow>()
            .Select(dataRow => new SimpleKeyValueModel { Key = dataRow["key"], Value = dataRow["value"] });

        return new ServiceResult<List<SimpleKeyValueModel>>(results.ToList());
    }
}
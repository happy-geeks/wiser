using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Api.Core.Services;
using Api.Modules.Products.Enums;
using Api.Modules.Products.Interfaces;
using Api.Modules.Products.Models;
using Api.Modules.Queries.Interfaces;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using NUglify.Helpers;

namespace Api.Modules.Products.Services;

/// <summary>
/// Service for handling the product api related calls
/// </summary>
public class ProductsService(
    IDatabaseConnection clientDatabaseConnection,
    IWiserItemsService wiserItemsService,
    IQueriesService queriesService,
    IStyledOutputService styledOutputService,
    IDatabaseHelpersService databaseHelpersService,
    ILogger<ProductsService> logger,
    HttpContextAccessor httpContextAccessor)
    : IProductsService, IScopedService
{
    private const string ProductApiPropertyTabName = "Product Api";

    /// <inheritdoc />
    public async Task<ServiceResult<JToken>> GetProductAsync(ClaimsIdentity identity, ulong wiserId)
    {
        // First ensure we have our tables up to date.
        await databaseHelpersService.CheckAndUpdateTablesAsync([WiserTableNames.WiserProductsApi]);

        // Setup up query and run it.
        await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
        clientDatabaseConnection.ClearParameters();
        clientDatabaseConnection.AddParameter("id", wiserId);

        var selectQuery = $"SELECT `output` FROM {WiserTableNames.WiserProductsApi} item WHERE item.wiser_id = ?id  ORDER BY `version` DESC LIMIT 1";

        var dataTable = await clientDatabaseConnection.GetAsync(selectQuery);

        // Check if we have a result, if not we inform the user its not there yet, they should generate it first.(don't lazy load/gen it).
        if (dataTable.Rows.Count == 0)
        {
            var errorMsg = $"Wiser product api Output for product with id '{wiserId}' does not exist( did you not generate it yet?).";
            logger.LogError(errorMsg);
            return new ServiceResult<JToken>
            {
                StatusCode = HttpStatusCode.NotFound,
                ErrorMessage = errorMsg
            };
        }

        try
        {
            return new ServiceResult<JToken>
            {
                StatusCode = HttpStatusCode.OK,
                ModelObject = JToken.Parse(dataTable.Rows[0].Field<string>("output"))
            };
        }
        catch
        {
            var errorMsg = $"Wiser product api Output for product with id '{wiserId}' does not have valid Json content.";
            logger.LogError(errorMsg);
            return new ServiceResult<JToken>
            {
                StatusCode = HttpStatusCode.NotFound,
                ErrorMessage = errorMsg
            };
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult<JToken>> GetAllProductsAsync(ClaimsIdentity identity, DateTime? date,  int page = 0)
    {
        // First ensure we have our tables up to date.
        await databaseHelpersService.CheckAndUpdateTablesAsync([WiserTableNames.WiserProductsApi]);

        // Clean up and adjust date string.

        // Check if a date was entered, if not take today.
        var dateWasProvidedByUser = date != null;

        if (!dateWasProvidedByUser)
        {
            date = DateTime.Now.Date;
        }

        var productsSinceWithTime = date.Value.AddMinutes(1);

        // Setup up query and run it.
        await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
        clientDatabaseConnection.ClearParameters();

        // Grab the select query from the product api settings.
        var productsQuery = await GetGlobalSettingAsync(ProductsServiceConstants.PropertySelectProductsKey);

        if (productsQuery == null)
        {
            var errorMsg = $"Wiser product api cannot find the query to select the product ids.";
            logger.LogError(errorMsg);
            return new ServiceResult<JToken>
            {
                StatusCode = HttpStatusCode.BadRequest,
                ErrorMessage = errorMsg
            };
        }

        const int resultsPerPage = 500;

        clientDatabaseConnection.AddParameter("productChangesSince", productsSinceWithTime.ToString("yyyy-MM-dd"));
        clientDatabaseConnection.AddParameter("pageOffset", page * resultsPerPage);
        clientDatabaseConnection.AddParameter("resultsPerPage", resultsPerPage);

        // Make a temp table and store the call the select query into it, use this for a hard join with the product api table.
        var tempTableQuery = $"""
DROP temporary table IF EXISTS wiser_temp_{WiserTableNames.WiserProductsApi}_ids;
CREATE TEMPORARY TABLE wiser_temp_{WiserTableNames.WiserProductsApi}_ids ( wiser_id INT );
INSERT INTO wiser_temp_{WiserTableNames.WiserProductsApi}_ids (wiser_id)
{productsQuery}
""";
        await clientDatabaseConnection.ExecuteAsync(tempTableQuery);

        // Now get the actual product api data,
        // Debug note: when testing it is best to remove the hard join (tempids) with the temp table, this will give you all products.
        // Alternatively you can also copy the previous query and run it along with this one to test with the temp table in place.
        var selectQuery = $"""
SELECT wp.wiser_id, wp.output
FROM {WiserTableNames.WiserProductsApi} wp
JOIN wiser_temp_{WiserTableNames.WiserProductsApi}_ids tempids ON tempids.wiser_id = wp.wiser_id
JOIN (
    SELECT  wiser_id, MAX(version) AS max_version
    FROM {WiserTableNames.WiserProductsApi}
    GROUP BY wiser_id
) latest
ON wp.wiser_id = latest.wiser_id 
AND wp.version = latest.max_version
WHERE added_on > ?productChangesSince
LIMIT ?pageOffset , ?resultsPerPage
""";

        var dataTable = await clientDatabaseConnection.GetAsync(selectQuery);

        // Generate the urls we need to return.
        var pageUriBuilder = HttpContextHelpers.GetOriginalRequestUriBuilder(httpContextAccessor?.HttpContext);

        var prevPageQueryString = HttpUtility.ParseQueryString(pageUriBuilder.Query);
        var nextPageQueryString = HttpUtility.ParseQueryString(pageUriBuilder.Query);

        if (dateWasProvidedByUser)
        {
            prevPageQueryString["date"]  = date?.ToString("yyyy-MM-dd");
            nextPageQueryString["date"] = date?.ToString("yyyy-MM-dd");
        }

        prevPageQueryString["page"] = (page - 1).ToString();
        nextPageQueryString["page"] = (page + 1).ToString();

        pageUriBuilder.Query = prevPageQueryString?.ToString() ?? "";
        var prevPageUrl = pageUriBuilder.ToString();

        pageUriBuilder.Query = nextPageQueryString?.ToString() ?? "";
        var nextPageUrl = pageUriBuilder.ToString();

        // Generate the json object for the result.
        var jsonObject = new JObject
        {
            ["count"] = dataTable.Rows.Count,
            ["previous"] = page > 0 ? prevPageUrl : "",
            ["next"] = dataTable.Rows.Count < resultsPerPage ? "" : nextPageUrl,
            ["results"] = null
        };

        var jsonArray = new JArray();

        foreach (DataRow product in dataTable.Rows)
        {
            jsonArray.Add(JToken.Parse(product.Field<string>("output")));
        }

        jsonObject["results"] = jsonArray;

        return new ServiceResult<JToken>
        {
            StatusCode = HttpStatusCode.OK,
            ModelObject = jsonObject
        };
    }

    /// <inheritdoc />
    public async Task<ServiceResult<JToken>> RefreshProductsAllAsync(ClaimsIdentity identity, bool ignoreCoolDown = false)
    {
        // First ensure we have our tables up to date.
        await databaseHelpersService.CheckAndUpdateTablesAsync([WiserTableNames.WiserProductsApi]);

        // Check if the properties for the product api are set up, if not we need to create them.
        // We do this here because we only want these properties present when the product api is used.
        // (To prevent extra tabs for costumers that dont use this feature.)
        await EnsureProductApiPropertiesAsync();

        var productsQuery = await GetGlobalSettingAsync(ProductsServiceConstants.PropertySelectProductsKey);
        if (productsQuery == null)
        {
            var errorMsg = $"Wiser product api cannot find the productsQuery for the product api.";
            logger.LogError(errorMsg);
            throw new KeyNotFoundException(errorMsg);
        }

        var dataTable = await clientDatabaseConnection.GetAsync(productsQuery);
        var ids = dataTable.Rows.Cast<DataRow>().Select(product => product.Field<ulong>("id")).ToList();

        return await RefreshProductsAsync(identity, ids, ignoreCoolDown);
    }

    /// <inheritdoc />
    public async Task<ServiceResult<JToken>> RefreshProductsAsync(ClaimsIdentity identity, ICollection<ulong> wiserIds, bool ignoreCoolDown = false)
    {
        // First ensure we have our tables up to date.
        await databaseHelpersService.CheckAndUpdateTablesAsync([WiserTableNames.WiserProductsApi]);

        // Check if the properties for the product api are set up, if not we need to create them.
        // We do this here because we only want these properties present when the product api is used.
        // (To prevent extra tabs for costumers that dont use this feature.)
        await EnsureProductApiPropertiesAsync();

        // Check if for the requested ids there is a product version of the setting available in wiser_itemdetail (if not create it).
        await EnsureProductApiPropertyDetailsAsync(wiserIds);

        var productEntityType = await GetGlobalSettingAsync(ProductsServiceConstants.PropertyEntityName);
        if (productEntityType == null)
        {
            var errorMsg = $"Wiser product api cannot find the entity type for the product api.";
            logger.LogError(errorMsg);
            throw new KeyNotFoundException(errorMsg);
        }

        var coolDown = await GetGlobalSettingAsync(ProductsServiceConstants.PropertyMinimalRefreshCoolDown);
        if (coolDown == null)
        {
            var errorMsg = $"Wiser product api cannot find the cool down setting for the product api.";
            logger.LogError(errorMsg);
            throw new KeyNotFoundException(errorMsg);
        }

        var coolDownString = ignoreCoolDown
            ? ""
            : $"AND apis.refresh_date < DATE_SUB(NOW(), INTERVAL {coolDown} MINUTE) OR apis.refresh_date IS NULL";

        var getApiDataQuery = $"""
SELECT 
item.id AS `wiser_id`,
apis.hash AS `old_hash`,
apis.version AS `version`,
apis.id AS `api_entry_id`,
DatasourceType.`value` AS `{ProductsServiceConstants.ProductPropertyDatasourceType}`,
DatasourceStatic.`value` AS `{ProductsServiceConstants.ProductPropertyStatic}`,
DatasourceQueryId.`value` AS `{ProductsServiceConstants.ProductPropertyQueryId}`,
DatasourceStyledId.`value` AS `{ProductsServiceConstants.PropertyStyledOutputId}`,
item.`published_environment` AS `published_environment`

FROM {WiserTableNames.WiserItem} `item`
LEFT JOIN  {WiserTableNames.WiserItemDetail} `DatasourceType` ON `DatasourceType`.key = '{ProductsServiceConstants.ProductPropertyDatasourceType}' AND `DatasourceType`.item_id = item.id
LEFT JOIN  {WiserTableNames.WiserItemDetail} `DatasourceStatic` ON `DatasourceStatic`.key = '{ProductsServiceConstants.ProductPropertyStatic}' AND `DatasourceStatic`.item_id = item.id
LEFT JOIN  {WiserTableNames.WiserItemDetail} `DatasourceQueryId` ON `DatasourceQueryId`.key = '{ProductsServiceConstants.ProductPropertyQueryId}' AND `DatasourceQueryId`.item_id = item.id
LEFT JOIN  {WiserTableNames.WiserItemDetail} `DatasourceStyledId` ON `DatasourceStyledId`.key = '{ProductsServiceConstants.PropertyStyledOutputId}' AND `DatasourceStyledId`.item_id = item.id
LEFT JOIN (
    SELECT  wiser_id, MAX(version) AS max_version
    FROM {WiserTableNames.WiserProductsApi}
    GROUP BY wiser_id
) latest_version ON latest_version.wiser_id = item.id
LEFT JOIN {WiserTableNames.WiserProductsApi} apis ON apis.wiser_id = item.id AND apis.version = latest_version.max_version 
 
WHERE item.entity_type = '{productEntityType}' 
  {coolDownString}
  AND item.id IN ({string.Join(",", wiserIds)})
LIMIT 256
""";
        await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
        var dataTable = await clientDatabaseConnection.GetAsync(getApiDataQuery);

        var generatedData = new List<ProductApiModel>();

        var currentDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var noUpdates = new List<ulong>();

        // Depending on the type, run the parsing and store the result in the product api table.
        foreach (DataRow row in dataTable.Rows)
        {
            var wiserId = row.Field<ulong>("wiser_id");
            var publishedEnvironment = row.Field<int>("published_environment");
            var datasourceType = int.Parse(row.Field<string>(ProductsServiceConstants.ProductPropertyDatasourceType) ?? "0");
            var staticText = row.Field<string>(ProductsServiceConstants.ProductPropertyStatic);
            var queryId = int.Parse(row.Field<string>(ProductsServiceConstants.ProductPropertyQueryId) ?? "0");
            var styledOutputId = int.Parse(row.Field<string>(ProductsServiceConstants.PropertyStyledOutputId ) ?? "0");
            var oldHash = row.Field<string>("old_hash");
            var version = row.IsNull("version") ? 0 : row.Field<int>("version");
            var apiEntryId = row.IsNull("api_entry_id") ? 0 : row.Field<ulong>("api_entry_id");
            var content = string.Empty;

            var parameters = new Dictionary<string,object>();
            parameters.Add("itemId", wiserId);

            // Check if we have a valid output, if not we skip this one.
            switch ((ProductApiPropertyDataSources)datasourceType)
            {
                case ProductApiPropertyDataSources.StaticSource:
                    // Static text.
                    content = staticText;
                    break;

                case ProductApiPropertyDataSources.Query:
                    // Query.
                    try
                    {
                        var queryResult = await queriesService.GetQueryResultAsJsonAsync(identity, queryId, false, parameters.ToList());

                        if (queryResult.StatusCode == HttpStatusCode.OK)
                        {
                            content = queryResult.ModelObject.ToString();
                        }
                        else
                        {
                            logger.LogWarning($"issue creating product api with query for wiseritem '{wiserId}' running query '{queryId}' - {queryResult.StatusCode}");
                            content = null;
                        }
                    }
                    catch (Exception exception)
                    {
                        logger.LogWarning(exception, $"issue creating product api with query for wiseritem '{wiserId}' running query '{queryId}'");
                        content = null;
                    }
                    break;

                case ProductApiPropertyDataSources.StyledOutput:
                    // Styled output.
                    try
                    {
                        var styledOutputResult = await styledOutputService.GetStyledOutputResultJsonAsync(identity, styledOutputId, parameters.ToList(),false,500,0);
                        if (styledOutputResult.StatusCode == HttpStatusCode.OK)
                        {
                            content = styledOutputResult.ModelObject.ToString();
                        }
                        else
                        {
                            logger.LogWarning($"issue creating product api with styledoutput for wiseritem '{wiserId}' running styledoutput '{styledOutputId}' - {styledOutputResult.StatusCode}");
                            content = null;
                        }
                    }
                    catch (Exception exception)
                    {
                        logger.LogWarning(exception, $"issue creating product api with styled out for wiseritem '{wiserId}' running styledoutput id '{styledOutputId}'");
                        content = null;
                    }
                    break;
            }

            // Make a hash and do a quick compare.
            if (!content.IsNullOrWhiteSpace())
            {
                var newHash = content.ToSha512Simple();
                if (newHash != oldHash)
                {
                    ProductApiModel productApiModel = new ProductApiModel
                    {
                        WiserId = wiserId,
                        Version = version + 1,
                        Output = content,
                        Hash = newHash,
                        Removed = publishedEnvironment <= 0,
                        AddedBy = "productApiRefresh",
                        AddedOn = currentDateTime,
                        RefreshDate = currentDateTime
                    };
                    // We have a new hash, we need to update the product api table.
                    generatedData.Add(productApiModel);
                }
                else
                {
                    noUpdates.Add(apiEntryId);
                }
            }
        }

        // If we have new date lets make new versions.
        if (generatedData.Count > 0)
        {
            await AddProductApiEntriesAsync(identity,generatedData);
        }

        // If we have entries that have not changed but were checked, update their refresh dates.
        if (noUpdates.Count > 0)
        {
            await UpdateProductApiEntriesAsync(identity,noUpdates,currentDateTime);
        }

        return new ServiceResult<JToken>
        {
            StatusCode = HttpStatusCode.OK
        };
    }

    /// <summary>
    /// Helper function to update the last check date on the given product api entries with the given timestamp.
    /// </summary>
    /// <param name="identity">The identity of the user performing this command.</param>
    /// <param name="apiEntryIds">The id of the wiser product api entries we are trying to update.</param>
    /// <param name="currentDateTime">The id of the wiser product we are trying to read.</param>
    private async Task UpdateProductApiEntriesAsync(ClaimsIdentity identity, ICollection<ulong> apiEntryIds, string currentDateTime)
    {
        var query = $"""
UPDATE `wiser_products_api` SET `refresh_date` = {currentDateTime}
WHERE `wiser_id` IN ( {string.Join(",", apiEntryIds)} )
""";

        await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
        clientDatabaseConnection.ClearParameters();

        try
        {
            await clientDatabaseConnection.ExecuteAsync(query);
        }
        catch (Exception e)
        {
            var errorMsg = $"Wiser product api encountered a problem adding new entries to the table.";
            if (errorMsg != null)
            {
                logger.LogError(e, errorMsg);
                throw new Exception(errorMsg);
            }
        }
    }

    /// <summary>
    /// This function checks and if needed creates the product api properties in the database.
    /// </summary>
    private async Task EnsureProductApiPropertiesAsync()
    {
        await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
        clientDatabaseConnection.ClearParameters();

        var productEntityType = await GetGlobalSettingAsync(ProductsServiceConstants.PropertyEntityName);
        if (productEntityType == null)
        {
            var errorMsg = $"Wiser product api cannot find the entity type for the product api.";
            logger.LogError(errorMsg);
            throw new KeyNotFoundException(errorMsg);
        }

        var selectQuery = $"""
SELECT id FROM {WiserTableNames.WiserEntityProperty} property 
WHERE property.tab_name = '{ProductApiPropertyTabName}' AND property.entity_name = '{productEntityType}'
""";

        var dataTable = await clientDatabaseConnection.GetAsync(selectQuery);

        if (dataTable.Rows.Count == 0)
        {
            // Select query for the display element.
            // (Use verbatim strings because have both escaped and evaluated values.)
            var selectQueryForResultDisplay = $@"
SELECT
output AS `jsonResult`
FROM {WiserTableNames.WiserProductsApi} apis
WHERE wiser_id = '{{itemId}}'
ORDER BY version DESC
LIMIT 1;
";

            // We are missing the properties, we need to create them.
            // (Use verbatim strings because have both escaped and evaluated values.)
            var createPropertyQuery = $@"
INSERT INTO `wiser_entityproperty` (`label_style`, `label_width`, `module_id`, `entity_name`, `tab_name`, `group_name`, `inputtype`, `display_name`, `property_name`,`ordering`, `default_value`, `width`, `height`, `options`, `explanation`,`data_query`) 
VALUES ('normal', 0, 620, '{productEntityType}', '{ProductApiPropertyTabName}', '', 'empty', 'results', 'results', 510, '<pre>{{jsonResult}}</pre>', 100, 0, '{{}}', '.', ""{selectQueryForResultDisplay}"")
ON DUPLICATE KEY UPDATE id=id;

INSERT INTO `wiser_entityproperty` (`label_style`, `label_width`, `module_id`, `entity_name`, `tab_name`, `group_name`, `inputtype`, `display_name`, `property_name`,`ordering`, `default_value`, `width`, `height`, `options`, `explanation`) 
VALUES ('normal', 0, 620, '{productEntityType}', '{ProductApiPropertyTabName}', '', 'combobox', 'DataSourceType', '{ProductsServiceConstants.ProductPropertyDatasourceType}', 501, '0', 100, 0, '{{ \""useDropDownList\"": true, \""dataSource\"": [ {{ \""name\"": \""Static\"", \""id\"": \""0\"" }}, {{ \""name\"": \""Query\"", \""id\"": \""1\"" }}, {{ \""name\"": \""StyledOutput\"", \""id\"": \""2\"" }} ] }}', 'Het datasource type, dit bepaald welke output ge-called word per product')
ON DUPLICATE KEY UPDATE id=id;

INSERT INTO `wiser_entityproperty` (`label_style`, `label_width`, `module_id`, `entity_name`, `tab_name`, `group_name`, `inputtype`, `display_name`, `property_name`,`ordering`, `default_value`, `width`, `height`, `options`, `explanation`, `depends_on_field`, `depends_on_operator`, `depends_on_value`) 
VALUES ('normal', 0, 620, '{productEntityType}', '{ProductApiPropertyTabName}', '', 'numeric-input', 'query id', '{ProductsServiceConstants.ProductPropertyQueryId}', 502, '0', 100, 0, '{{""decimals"":0 , ""format"": ""#""}}', 'De id van de query die gebruikt moet worden.', '{ProductsServiceConstants.ProductPropertyDatasourceType}', '=', '1')
ON DUPLICATE KEY UPDATE id=id;

INSERT INTO `wiser_entityproperty` (`label_style`, `label_width`, `module_id`, `entity_name`, `tab_name`, `group_name`, `inputtype`, `display_name`, `property_name`,`ordering`, `default_value`, `width`, `height`, `options`, `explanation`, `depends_on_field`, `depends_on_operator`, `depends_on_value`) 
VALUES ('normal', 0, 620, '{productEntityType}', '{ProductApiPropertyTabName}', '', 'numeric-input', 'styledoutput id', '{ProductsServiceConstants.ProductPropertyStyledOutputId}', 502, '0', 100, 0, '{{""decimals"":0 , ""format"": ""#""}}', 'De id van de query die gebruikt moet worden.', '{ProductsServiceConstants.ProductPropertyDatasourceType}', '=', '2')
ON DUPLICATE KEY UPDATE id=id;

INSERT INTO `wiser_entityproperty` (`label_style`, `label_width`, `module_id`, `entity_name`, `tab_name`, `group_name`, `inputtype`, `display_name`, `property_name`,`ordering`, `default_value`, `width`, `height`, `options`, `explanation`, `depends_on_field`, `depends_on_operator`, `depends_on_value`) 
VALUES ('normal', 0, 620, '{productEntityType}', '{ProductApiPropertyTabName}', '', 'textbox', 'static text', '{ProductsServiceConstants.ProductPropertyStatic}', 502, '{{ ""name"": ""notset""}}', 100, 100, '', 'De static text die gebruikt moet worden..', '{ProductsServiceConstants.ProductPropertyDatasourceType}', '=', '0')
ON DUPLICATE KEY UPDATE id=id;
";
            await clientDatabaseConnection.ExecuteAsync(createPropertyQuery);
        }
    }

    /// <summary>
    /// A function that checks if the for a list of given products the product api properties are set up, if not it creates them.
    /// </summary>
    /// <param name="wiserIds">A list of ids of the wiser products.</param>
    /// <exception cref="KeyNotFoundException"></exception>
    private async Task EnsureProductApiPropertyDetailsAsync(ICollection<ulong> wiserIds)
    {
        await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
        clientDatabaseConnection.ClearParameters();

        var productEntityType = await GetGlobalSettingAsync(ProductsServiceConstants.PropertyEntityName);
        if (productEntityType == null)
        {
            var errorMsg = $"Wiser product api cannot find the entity type for the product api.";
            logger.LogError(errorMsg);
            throw new KeyNotFoundException(errorMsg);
        }

        // Let's see if there are any products without details ( we assume that if datasource_type is there all details are saved).
        var findMissingQuery = $"""
SELECT item.id AS wiser_id
FROM {WiserTableNames.WiserItem} `item`
LEFT JOIN {WiserTableNames.WiserItemDetail} `detail` ON `detail`.key = '{ProductsServiceConstants.ProductPropertyDatasourceType}' AND `detail`.item_id = item.id
WHERE detail.`id` IS NULL AND item.entity_type = '{productEntityType}' AND item.id IN ({string.Join(",", wiserIds)})
""";
        var missingDataTable = await clientDatabaseConnection.GetAsync(findMissingQuery);

        if (missingDataTable.Rows.Count > 0)
        {
            var globalSettingDatasourceType = await GetGlobalSettingAsync(ProductsServiceConstants.PropertyDatasourceType);
            var globalSettingsQueryId = await GetGlobalSettingAsync(ProductsServiceConstants.PropertyQueryId);
            var globalSettingsStyledOutputId = await GetGlobalSettingAsync(ProductsServiceConstants.PropertyStyledOutputId);
            var globalSettingsStaticText = await GetGlobalSettingAsync(ProductsServiceConstants.PropertyStatic);

            // If we have some missing details, we need to create them.
            foreach (DataRow row in missingDataTable.Rows)
            {
                var wiserId = row.Field<ulong>("wiser_id");

                await SaveProductApiSettingsForProductAsync(wiserId, productEntityType,
                    globalSettingDatasourceType, globalSettingsQueryId, globalSettingsStyledOutputId,
                    globalSettingsStaticText);
            }
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult<JToken>> RefreshProductAsync(ClaimsIdentity identity, ulong wiserId, bool ignoreCoolDown = false)
    {
        return await RefreshProductsAsync(identity, [wiserId],ignoreCoolDown);
    }

    /// <inheritdoc />
    public async Task<ServiceResult<JToken>> SetDefaultSettingsOnAllProductsAsync(ClaimsIdentity identity)
    {
        await EnsureProductApiPropertiesAsync();

        await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
        clientDatabaseConnection.ClearParameters();

        var productEntityType = await GetGlobalSettingAsync(ProductsServiceConstants.PropertyEntityName);
        if (productEntityType == null)
        {
            var errorMsg = $"Wiser product api cannot find the entity type for the product api.";
            logger.LogError(errorMsg);
            throw new KeyNotFoundException(errorMsg);
        }

        // Fetch the ids of all product api's based on if the datasourcetype is found.
        var getIds = $"""
SELECT item_id
FROM wiser_itemdetail
WHERE `key` = '{ProductsServiceConstants.ProductPropertyDatasourceType}'
""";

        var products = await clientDatabaseConnection.GetAsync(getIds);

        if (products.Rows.Count > 0)
        {
            var globalSettingDatasourceType = await GetGlobalSettingAsync(ProductsServiceConstants.PropertyDatasourceType);
            var globalSettingsQueryId = await GetGlobalSettingAsync(ProductsServiceConstants.PropertyQueryId);
            var globalSettingsStyledOutputId = await GetGlobalSettingAsync(ProductsServiceConstants.PropertyStyledOutputId);
            var globalSettingsStaticText = await GetGlobalSettingAsync(ProductsServiceConstants.PropertyStatic);

            // If we have some missing details, we need to create them.
            foreach (DataRow row in products.Rows)
            {
                var wiserId = row.Field<ulong>("item_id");

                await SaveProductApiSettingsForProductAsync(wiserId, productEntityType,
                    globalSettingDatasourceType, globalSettingsQueryId, globalSettingsStyledOutputId,
                    globalSettingsStaticText);
            }
        }

        return new ServiceResult<JToken>
        {
            StatusCode = HttpStatusCode.OK
        };
    }

    private async Task<string> GetGlobalSettingAsync(string settingName)
    {
        await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
        clientDatabaseConnection.ClearParameters();
        clientDatabaseConnection.AddParameter("settingName", settingName);

        var selectQuery = $"""
SELECT `value` FROM {WiserTableNames.WiserItemDetail} detail 
JOIN {WiserTableNames.WiserItem} item ON item.id = detail.item_id AND item.entity_type = '{ProductsServiceConstants.SettingsEntityName}' 
WHERE detail.key = '{settingName}' LIMIT 1
""";

        var dataTable = await clientDatabaseConnection.GetAsync(selectQuery);

        return dataTable.Rows.Count == 0 ? null : dataTable.Rows[0].Field<string>("value");
    }

    /// <summary>
    /// Saves the default settings for a given product.
    /// </summary>
    private async Task SaveProductApiSettingsForProductAsync(ulong wiserId, string productEntityType,
        string globalSettingDatasourceType,string globalSettingsQueryId, string globalSettingsStyledOutputId, string globalSettingsStaticText)
    {
        await wiserItemsService.SaveItemDetailAsync(
            new WiserItemDetailModel
            {
                Key = ProductsServiceConstants.ProductPropertyDatasourceType,
                Value = globalSettingDatasourceType,
                GroupName = "general"
            }, wiserId, entityType: productEntityType, saveHistory: false);
        await wiserItemsService.SaveItemDetailAsync(
            new WiserItemDetailModel
            {
                Key = ProductsServiceConstants.ProductPropertyQueryId,
                Value = globalSettingsQueryId,
                GroupName = "general"
            }, wiserId, entityType: productEntityType, saveHistory: false);
        await wiserItemsService.SaveItemDetailAsync(
            new WiserItemDetailModel
            {
                Key = ProductsServiceConstants.ProductPropertyStyledOutputId,
                Value = globalSettingsStyledOutputId,
                GroupName = "general"
            }, wiserId, entityType: productEntityType, saveHistory: false);
        await wiserItemsService.SaveItemDetailAsync(
            new WiserItemDetailModel
            {
                Key = ProductsServiceConstants.ProductPropertyStatic,
                Value = globalSettingsStaticText, GroupName = "general"
            }, wiserId, entityType: productEntityType, saveHistory: false);
    }

    /// <summary>
    /// Function that makes inserts the product api entries into the database.
    /// </summary>
    /// <param name="identity">The identity of the user performing this command.</param>
    /// <param name="items">A list of api models that need to be added to the dababase.</param>
    private Task AddProductApiEntriesAsync(ClaimsIdentity identity, ICollection<ProductApiModel> items)
    {
        var insertQuery = new StringBuilder();

        foreach (var item in items)
        {
            insertQuery.Append
            ($"""
INSERT INTO `{WiserTableNames.WiserProductsApi}` (`wiser_id`, `version`, `output`, `added_by`, `hash`,`refresh_date`,`added_on`) 
VALUES ('{item.WiserId}', '{item.Version}', '{item.Output}', '{item.AddedBy}', '{item.Hash}', '{item.RefreshDate}', '{item.AddedOn}');
""");
        }

        try
        {
            return clientDatabaseConnection.ExecuteAsync(insertQuery.ToString());
        }
        catch (Exception e)
        {
            var errorMsg = $"Wiser product api encountered a problem adding new entries to the table.";
            logger.LogError(e,errorMsg);
            throw new Exception(errorMsg);
        }
    }
}
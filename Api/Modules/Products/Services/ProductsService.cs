using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.Products.Interfaces;
using Api.Modules.Queries.Interfaces;
using Api.Modules.Queries.Models;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using NUglify.Helpers;

namespace Api.Modules.Products.Services;

/// <summary>
/// Service for handling the product api related calls 
/// </summary>
public class ProductsService : IProductsService, IScopedService
{
    private readonly IDatabaseConnection clientDatabaseConnection;
    private readonly IWiserItemsService wiserItemsService;
    private readonly IQueriesService queriesService;
    private readonly IStyledOutputService styledOutputService;
    private readonly IDatabaseHelpersService databaseHelpersService;
    private readonly ILogger<ProductsService> logger;

    private const string ProductApiPropertyTabName = "Product Api";
    private const string ProductApiPropertyGroupName = "Product-Api";
    private const string ProductApiPropertyDatasource = "wiser_product_api_datasource";

    enum ProductApiPropertyDatasourceId
    {
        StaticSource,
        Query,
        StyledOutput
    };
    
    private const string ProductApiProductPropertyDatasourceType = "wiser_product_api_datasource_type";
    private const string ProductApiProductPropertyStatic = "wiser_product_api_static";
    private const string ProductApiProductPropertyQueryId = "wiser_product_api_query_id";
    private const string ProductApiProductPropertyStyledOutputId = "wiser_product_api_styledoutput_id";

    private const string ProductsApiPropertySelectProductsKey = "product_ids_query";
    private const string ProductsApiPropertyEntityName = "product_entity_name";
    private const string ProductsApiPropertyDatasourceType = "datasource_type";
    private const string ProductsApiPropertyStatic = "datasource_static";
    private const string ProductsApiPropertyQueryId = "datasource_query";
    private const string ProductsApiPropertyStyledOutputId = "datasource_styledoutput";
    private const string ProductsApiPropertyMinimalRefreshCoolDown = "minimal_refresh_cooldown";

    private const string ProductsApiSettingsEntityName = "ProductsApiSettings";

    /// <summary>
    /// Creates a new instance of <see cref="ProductsService"/>.
    /// </summary>
    public ProductsService(
        IDatabaseConnection clientDatabaseConnection,
        IWiserItemsService wiserItemsService,
        IQueriesService queriesService,
        IStyledOutputService styledOutputService,
        IDatabaseHelpersService databaseHelpersService,
        ILogger<ProductsService> logger
    )
    {
        this.clientDatabaseConnection = clientDatabaseConnection;
        this.wiserItemsService = wiserItemsService;
        this.queriesService = queriesService;
        this.styledOutputService = styledOutputService;
        this.databaseHelpersService = databaseHelpersService;
        this.logger = logger;
    }


    /// <inheritdoc />
    public async Task<ServiceResult<JToken>> GetProduct(ClaimsIdentity identity, ulong wiserId)
    {
        // First ensure we have our tables up to date.
        await databaseHelpersService.CheckAndUpdateTablesAsync([WiserTableNames.WiserProductsApi]);

        // Setup up query and run it.
        await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
        clientDatabaseConnection.ClearParameters();
        clientDatabaseConnection.AddParameter("id", wiserId);

        var selectQuery =
            $"SELECT `output` FROM {WiserTableNames.WiserProductsApi} item WHERE item.wiser_id = ?id  ORDER BY `version` DESC LIMIT 1";

        var dataTable = await clientDatabaseConnection.GetAsync(selectQuery);

        // Check if we have a result, if not we inform the user its not there yet, they should generate it first.(don't lazy load/gen it).
        if (dataTable.Rows.Count == 0)
        {
            var errorMsg =
                $"Wiser product api Output for product with id '{wiserId}' does not exist( did you not generate it yet?).";
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
            var errorMsg =
                $"Wiser product api Output for product with id '{wiserId}' does not have valid Json content.";
            logger.LogError(errorMsg);
            return new ServiceResult<JToken>
            {
                StatusCode = HttpStatusCode.NotFound,
                ErrorMessage = errorMsg
            };
        }
    }
    
    /// <inheritdoc />
    public async Task<ServiceResult<JToken>> GetAllProducts(ClaimsIdentity identity, string callingUrl, string date,
        int page = 0)
    {
        // First ensure we have our tables up to date.
        await databaseHelpersService.CheckAndUpdateTablesAsync([WiserTableNames.WiserProductsApi]);

        // Clean up and adjust date string.
        DateTime productsSinceWithTime;

        // Check if a date was entered, if not take today.
        var dateWasProvidedByUser = !string.IsNullOrEmpty(date);

        if (!dateWasProvidedByUser)
        {
            date = DateTime.Now.ToString("yyyy-MM-dd");
        }

        if (!DateTime.TryParse(date, out productsSinceWithTime))
        {
            var errorMsg = $"Wiser product api Output for product with date '{date}' is not a valid date.";
            logger.LogError(errorMsg);
            return new ServiceResult<JToken>
            {
                StatusCode = HttpStatusCode.BadRequest,
                ErrorMessage = errorMsg
            };
        }

        var productsSince = productsSinceWithTime.Date.AddMinutes(1);

        // Setup up query and run it.
        await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
        clientDatabaseConnection.ClearParameters();

        // Grab the select query from the product api settings.
        var productsQuery = await GetGlobalSettingAsync(ProductsApiPropertySelectProductsKey);

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

        clientDatabaseConnection.AddParameter("productChangesSince", productsSince.ToString("yyyy-MM-dd"));
        clientDatabaseConnection.AddParameter("pageOffset", page * resultsPerPage);
        clientDatabaseConnection.AddParameter("resultsPerPage", resultsPerPage);

        // Make a temp table and store the call the select query into it, use this for a hard join with the product api table.
        string tempTableQuery = $@"
DROP temporary table IF EXISTS wiser_temp_{WiserTableNames.WiserProductsApi}_ids;
CREATE TEMPORARY TABLE wiser_temp_{WiserTableNames.WiserProductsApi}_ids ( wiser_id INT );
INSERT INTO wiser_temp_{WiserTableNames.WiserProductsApi}_ids (wiser_id)
{productsQuery}
";
        await clientDatabaseConnection.ExecuteAsync(tempTableQuery);

        // Now get the actual product api data,
        // Debug note: when testing it is best to remove the hard join (tempids) with the temp table, this will give you all products.
        // Alternatively you can also copy the previous query and run it along with this one to test with the temp table in place.
        string selectQuery = $@"
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
LIMIT ?pageOffset , ?resultsPerPage";

        var dataTable = await clientDatabaseConnection.GetAsync(selectQuery);

        // Generate the json object for the result.
        var prevUrl = dateWasProvidedByUser
            ? $"{callingUrl}?date={date}&page={page - 1}"
            : $"{callingUrl}?page={page - 1}";
        var nextUrl = dateWasProvidedByUser
            ? $"{callingUrl}?date={date}&page={page + 1}"
            : $"{callingUrl}?page={page + 1}";

        var jsonObject = new JObject
        {
            ["count"] = dataTable.Rows.Count,
            ["previous"] = page > 0 ? prevUrl : "",
            ["next"] = dataTable.Rows.Count < resultsPerPage ? "" : nextUrl,
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
    public async Task<ServiceResult<JToken>> RefreshProductsAlAsync(ClaimsIdentity identity, bool ignoreCoolDown = false)
    {
        // First ensure we have our tables up to date.
        await databaseHelpersService.CheckAndUpdateTablesAsync([WiserTableNames.WiserProductsApi]);

        // Check if the properties for the product api are set up, if not we need to create them.
        // We do this here because we only want these properties present when the product api is used.
        // (To prevent extra tabs for costumers that dont use this feature.)
        await EnsureProductApiPropertiesAsync();

        var productsQuery = await GetGlobalSettingAsync(ProductsApiPropertySelectProductsKey);
        if (productsQuery == null)
        {
            var errorMsg = $"Wiser product api cannot find the productsQuery for the product api.";
            logger.LogError(errorMsg);
            throw new KeyNotFoundException(errorMsg);
        }

        var dataTable = await clientDatabaseConnection.GetAsync(productsQuery);
        var ids = new List<ulong>();
        foreach (DataRow product in dataTable.Rows) ids.Add(product.Field<ulong>("id"));
        
        return await RefreshProductsAsync(identity, ids, ignoreCoolDown);
    }
    
    /// <inheritdoc />
    public async Task<ServiceResult<JToken>> RefreshProductsAsync(ClaimsIdentity identity, ICollection<ulong> wiserIds,
        bool ignoreCoolDown = false)
    {
        // First ensure we have our tables up to date.
        await databaseHelpersService.CheckAndUpdateTablesAsync([WiserTableNames.WiserProductsApi]);

        // Check if the properties for the product api are set up, if not we need to create them.
        // We do this here because we only want these properties present when the product api is used.
        // (To prevent extra tabs for costumers that dont use this feature.)
        await EnsureProductApiPropertiesAsync();

        // Check if for the requested ids there is a product version of the setting available in wiser_itemdetail (if not create it).
        await EnsureProductApiPropertyDetailsAsync(wiserIds);

        var productEntityType = await GetGlobalSettingAsync(ProductsApiPropertyEntityName);
        if (productEntityType == null)
        {
            var errorMsg = $"Wiser product api cannot find the entity type for the product api.";
            logger.LogError(errorMsg);
            throw new KeyNotFoundException(errorMsg);
        }

        var coolDown = await GetGlobalSettingAsync(ProductsApiPropertyMinimalRefreshCoolDown);
        if (coolDown == null)
        {
            var errorMsg = $"Wiser product api cannot find the cool down setting for the product api.";
            logger.LogError(errorMsg);
            throw new KeyNotFoundException(errorMsg);
        }

        var coolDownString =
            ignoreCoolDown ? "" : $"AND apis.refresh_date < DATE_SUB(NOW(), INTERVAL {coolDown} MINUTE) OR apis.refresh_date IS NULL";

        var getApiDataQuery = $@"
SELECT 
item.id AS `wiser_id`,
apis.hash AS `old_hash`,
apis.version AS `version`,
apis.id AS `api_entry_id`,
DatasourceType.`value` AS `{ProductApiProductPropertyDatasourceType}`,
DatasourceStatic.`value` AS `{ProductApiProductPropertyStatic}`,
DatasourceQueryId.`value` AS `{ProductApiProductPropertyQueryId}`,
DatasourceStyledId.`value` AS `{ProductsApiPropertyStyledOutputId}`,
item.`published_environment` AS `published_environment`

FROM {WiserTableNames.WiserItem} `item`
LEFT JOIN  {WiserTableNames.WiserItemDetail} `DatasourceType` ON `DatasourceType`.key = '{ProductApiProductPropertyDatasourceType}' AND `DatasourceType`.item_id = item.id
LEFT JOIN  {WiserTableNames.WiserItemDetail} `DatasourceStatic` ON `DatasourceStatic`.key = '{ProductApiProductPropertyStatic}' AND `DatasourceStatic`.item_id = item.id
LEFT JOIN  {WiserTableNames.WiserItemDetail} `DatasourceQueryId` ON `DatasourceQueryId`.key = '{ProductApiProductPropertyQueryId}' AND `DatasourceQueryId`.item_id = item.id
LEFT JOIN  {WiserTableNames.WiserItemDetail} `DatasourceStyledId` ON `DatasourceStyledId`.key = '{ProductsApiPropertyStyledOutputId}' AND `DatasourceStyledId`.item_id = item.id
LEFT JOIN (
    SELECT  wiser_id, MAX(version) AS max_version
    FROM {WiserTableNames.WiserProductsApi}
    GROUP BY wiser_id
) latest_version ON latest_version.wiser_id = item.id
LEFT JOIN {WiserTableNames.WiserProductsApi} apis ON apis.wiser_id = item.id AND apis.version = latest_version.max_version 
 
WHERE item.entity_type = '{productEntityType}' 
  {coolDownString}
  AND item.id IN ({string.Join(",", wiserIds)})
LIMIT 256";
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
            var datasourceType = int.Parse(row.Field<string>(ProductApiProductPropertyDatasourceType) ?? "0");
            var staticText = row.Field<string>(ProductApiProductPropertyStatic);
            var queryId = int.Parse(row.Field<string>(ProductApiProductPropertyQueryId) ?? "0");
            var styledOutputId = int.Parse(row.Field<string>(ProductsApiPropertyStyledOutputId ) ?? "0");
            var oldHash = row.Field<string>("old_hash");
            var version = row.IsNull("version") ? 0 : row.Field<int>("version");
            var apiEntryId = row.IsNull("api_entry_id") ? 0 : row.Field<ulong>("api_entry_id");
            var content = string.Empty;
          
            List<KeyValuePair<string, object>> parameters = new List<KeyValuePair<string, object>>();
            parameters.Add(new KeyValuePair<string, object>("itemId,", wiserId));

            // Check if we have a valid output, if not we skip this one.
            switch ((ProductApiPropertyDatasourceId)datasourceType)
            {
                case ProductApiPropertyDatasourceId.StaticSource:
                    // Static text.
                    content = staticText;
                    break;
                
                case ProductApiPropertyDatasourceId.Query:
                    // Query.
                    try
                    {
                        var queryResult = await queriesService.GetQueryResultAsJsonAsync(identity, queryId, false, parameters);

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
                
                case ProductApiPropertyDatasourceId.StyledOutput:
                    // Styled output.
                    try
                    {
                        var styledOutputResult = await styledOutputService.GetStyledOutputResultJsonAsync(identity, styledOutputId, parameters,false,500,0);
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
                var newHash = ComputeMd5Hash(content);
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
            await UpdateProductApiEntries(identity,noUpdates,currentDateTime);
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
    private async Task UpdateProductApiEntries(ClaimsIdentity identity, ICollection<ulong> apiEntryIds, string currentDateTime)
    {
        var query = $@"
        UPDATE `wiser_products_api` SET `refresh_date` = {currentDateTime}
        WHERE `wiser_id` IN ( {string.Join(",", apiEntryIds)} )";
        
        await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
        clientDatabaseConnection.ClearParameters();
        
        try
        {
            await clientDatabaseConnection.ExecuteAsync(query);
        }
        catch (Exception e)
        {
            var errorMsg = $"Wiser product api encountered a problem adding new entries to the table.";
            logger.LogError(errorMsg);
            throw new Exception(errorMsg);
        }
    }

    /// <summary>
    /// This function checks and if needed creates the product api properties in the database.
    /// </summary>
    private async Task EnsureProductApiPropertiesAsync()
    {
        await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
        clientDatabaseConnection.ClearParameters();

        var productEntityType = await GetGlobalSettingAsync(ProductsApiPropertyEntityName);
        if (productEntityType == null)
        {
            var errorMsg = $"Wiser product api cannot find the entity type for the product api.";
            logger.LogError(errorMsg);
            throw new KeyNotFoundException(errorMsg);
        }

        var selectQuery = $@"
SELECT id FROM {WiserTableNames.WiserEntityProperty} property 
WHERE property.tab_name = '{ProductApiPropertyTabName}' AND property.entity_name = '{productEntityType}'";

        var dataTable = await clientDatabaseConnection.GetAsync(selectQuery);

        if (dataTable.Rows.Count == 0)
        {
            // Select query for the display element.
            var selectQueryForResultDisplay = $@"
SELECT
output AS `jsonResult`
FROM {WiserTableNames.WiserProductsApi} apis
WHERE wiser_id = '{{itemId}}'
ORDER BY version DESC
LIMIT 1;
";

            // We are missing the properties, we need to create them.
            var createPropertyQuery = $@"
INSERT INTO `wiser_entityproperty` (`label_style`, `label_width`, `module_id`, `entity_name`, `tab_name`, `group_name`, `inputtype`, `display_name`, `property_name`,`ordering`, `default_value`, `width`, `height`, `options`, `explanation`,`data_query`) 
VALUES ('normal', 0, 620, '{productEntityType}', '{ProductApiPropertyTabName}', 'general', 'empty', 'results', 'results', 510, '<pre>{{jsonResult}}</pre>', 100, 0, '{{}}', '.', ""{selectQueryForResultDisplay}"")
ON DUPLICATE KEY UPDATE id=id;

INSERT INTO `wiser_entityproperty` (`label_style`, `label_width`, `module_id`, `entity_name`, `tab_name`, `group_name`, `inputtype`, `display_name`, `property_name`,`ordering`, `default_value`, `width`, `height`, `options`, `explanation`) 
VALUES ('normal', 0, 620, '{productEntityType}', '{ProductApiPropertyTabName}', 'general', 'combobox', 'DataSourceType', '{ProductApiProductPropertyDatasourceType}', 501, '0', 100, 0, '{{ \""useDropDownList\"": true, \""dataSource\"": [ {{ \""name\"": \""Static\"", \""id\"": \""0\"" }}, {{ \""name\"": \""Query\"", \""id\"": \""1\"" }}, {{ \""name\"": \""StyledOutput\"", \""id\"": \""2\"" }} ] }}', 'Het datasource type, dit bepaald welke output ge-called word per product')
ON DUPLICATE KEY UPDATE id=id;

INSERT INTO `wiser_entityproperty` (`label_style`, `label_width`, `module_id`, `entity_name`, `tab_name`, `group_name`, `inputtype`, `display_name`, `property_name`,`ordering`, `default_value`, `width`, `height`, `options`, `explanation`, `depends_on_field`, `depends_on_operator`, `depends_on_value`) 
VALUES ('normal', 0, 620, '{productEntityType}', '{ProductApiPropertyTabName}', 'general', 'numeric-input', 'query id', '{ProductApiProductPropertyQueryId}', 502, '0', 100, 0, '{{""decimals"":0 , ""format"": ""#""}}', 'De id van de query die gebruikt moet worden.', '{ProductApiProductPropertyDatasourceType}', '=', '1')
ON DUPLICATE KEY UPDATE id=id;

INSERT INTO `wiser_entityproperty` (`label_style`, `label_width`, `module_id`, `entity_name`, `tab_name`, `group_name`, `inputtype`, `display_name`, `property_name`,`ordering`, `default_value`, `width`, `height`, `options`, `explanation`, `depends_on_field`, `depends_on_operator`, `depends_on_value`) 
VALUES ('normal', 0, 620, '{productEntityType}', '{ProductApiPropertyTabName}', 'general', 'numeric-input', 'styledoutput id', '{ProductApiProductPropertyStyledOutputId}', 502, '0', 100, 0, '{{""decimals"":0 , ""format"": ""#""}}', 'De id van de query die gebruikt moet worden.', '{ProductApiProductPropertyDatasourceType}', '=', '2')
ON DUPLICATE KEY UPDATE id=id;

INSERT INTO `wiser_entityproperty` (`label_style`, `label_width`, `module_id`, `entity_name`, `tab_name`, `group_name`, `inputtype`, `display_name`, `property_name`,`ordering`, `default_value`, `width`, `height`, `options`, `explanation`, `depends_on_field`, `depends_on_operator`, `depends_on_value`) 
VALUES ('normal', 0, 620, '{productEntityType}', '{ProductApiPropertyTabName}', 'general', 'textbox', 'static text', '{ProductApiProductPropertyStatic}', 502, '{{ ""name"": ""notset""}}', 100, 100, '', 'De static text die gebruikt moet worden..', '{ProductApiProductPropertyDatasourceType}', '=', '0')
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

        var productEntityType = await GetGlobalSettingAsync(ProductsApiPropertyEntityName);
        if (productEntityType == null)
        {
            var errorMsg = $"Wiser product api cannot find the entity type for the product api.";
            logger.LogError(errorMsg);
            throw new KeyNotFoundException(errorMsg);
        }

        // Let's see if there are any products without details ( we assume that if datasource_type is there all details are saved).
        var findMissingQuery = $@"
SELECT item.id AS wiser_id
FROM {WiserTableNames.WiserItem} `item`
LEFT JOIN {WiserTableNames.WiserItemDetail} `detail` ON `detail`.key = '{ProductApiProductPropertyDatasourceType}' AND `detail`.item_id = item.id
WHERE detail.`id` IS NULL AND item.entity_type = '{productEntityType}' AND item.id IN ({string.Join(",", wiserIds)})
";
        var missingDataTable = await clientDatabaseConnection.GetAsync(findMissingQuery);

        if (missingDataTable.Rows.Count > 0)
        {
            var globalSettingDatasourceType = await GetGlobalSettingAsync(ProductsApiPropertyDatasourceType);
            var globalSettingsQueryId = await GetGlobalSettingAsync(ProductsApiPropertyQueryId);
            var globalSettingsStyledOutputId = await GetGlobalSettingAsync(ProductsApiPropertyStyledOutputId);
            var globalSettingsStaticText = await GetGlobalSettingAsync(ProductsApiPropertyStatic);

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
    public async Task<ServiceResult<JToken>> OverwriteApiProductSettingsForAllProductAsync(ClaimsIdentity identity)
    {
        await EnsureProductApiPropertiesAsync();
        
        await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
        clientDatabaseConnection.ClearParameters();

        var productEntityType = await GetGlobalSettingAsync(ProductsApiPropertyEntityName);
        if (productEntityType == null)
        {
            var errorMsg = $"Wiser product api cannot find the entity type for the product api.";
            logger.LogError(errorMsg);
            throw new KeyNotFoundException(errorMsg);
        }

        // Fetch the ids of all product api's based on if the datasourcetype is found.
        var getIds = $@"
SELECT item_id
FROM wiser_itemdetail
WHERE `key` = '{ProductApiProductPropertyDatasourceType}'";
        
        var products = await clientDatabaseConnection.GetAsync(getIds);

        if (products.Rows.Count > 0)
        {
            var globalSettingDatasourceType = await GetGlobalSettingAsync(ProductsApiPropertyDatasourceType);
            var globalSettingsQueryId = await GetGlobalSettingAsync(ProductsApiPropertyQueryId);
            var globalSettingsStyledOutputId = await GetGlobalSettingAsync(ProductsApiPropertyStyledOutputId);
            var globalSettingsStaticText = await GetGlobalSettingAsync(ProductsApiPropertyStatic);

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

        var selectQuery = $@"
SELECT `value` FROM {WiserTableNames.WiserItemDetail} detail 
JOIN {WiserTableNames.WiserItem} item ON item.id = detail.item_id AND item.entity_type = '{ProductsApiSettingsEntityName}' 
WHERE detail.key = '{settingName}' LIMIT 1";

        var dataTable = await clientDatabaseConnection.GetAsync(selectQuery);

        return dataTable.Rows.Count == 0 ? null : dataTable.Rows[0].Field<string>("value");
    }
    
    // Helper function to generate the hash for the content.
    // Using md5 because its faster, never use this function for hashing something related to security,
    // This is for quick comparison only!
    private static string ComputeMd5Hash(string input)
    {
        using (MD5 md5 = MD5.Create())
        {
            byte[] bytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(bytes);

            // Convert hash to hexadecimal string
            StringBuilder sb = new StringBuilder();
            foreach (byte b in hashBytes)
                sb.Append(b.ToString("x2"));

            return sb.ToString();
        }
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
                Key = ProductApiProductPropertyDatasourceType, Value = globalSettingDatasourceType,
                GroupName = "general"
            }, wiserId, entityType: productEntityType, saveHistory: false);
        await wiserItemsService.SaveItemDetailAsync(
            new WiserItemDetailModel
            {
                Key = ProductApiProductPropertyQueryId, Value = globalSettingsQueryId, GroupName = "general"
            }, wiserId, entityType: productEntityType, saveHistory: false);
        await wiserItemsService.SaveItemDetailAsync(
            new WiserItemDetailModel
            {
                Key = ProductApiProductPropertyStyledOutputId, Value = globalSettingsStyledOutputId,
                GroupName = "general"
            }, wiserId, entityType: productEntityType, saveHistory: false);
        await wiserItemsService.SaveItemDetailAsync(
            new WiserItemDetailModel
            {
                Key = ProductApiProductPropertyStatic, Value = globalSettingsStaticText, GroupName = "general"
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
            ($@"
INSERT INTO `{WiserTableNames.WiserProductsApi}` (`wiser_id`, `version`, `output`, `added_by`, `hash`,`refresh_date`,`added_on`) 
VALUES ('{item.WiserId}', '{item.Version}', '{item.Output}', '{item.AddedBy}', '{item.Hash}', '{item.RefreshDate}', '{item.AddedOn}'); ");
        }
        
        try
        {
            return clientDatabaseConnection.ExecuteAsync(insertQuery.ToString());
        }
        catch (Exception e)
        {
            var errorMsg = $"Wiser product api encountered a problem adding new entries to the table.";
            logger.LogError(errorMsg);
            throw new Exception(errorMsg);
        }
    }
}
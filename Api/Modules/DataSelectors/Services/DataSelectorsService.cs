using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Api.Core.Helpers;
using Api.Core.Services;
using Api.Modules.Tenants.Interfaces;
using Api.Modules.DataSelectors.Interfaces;
using Api.Modules.DataSelectors.Models;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Exports.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using GclDataSelectors = GeeksCoreLibrary.Modules.DataSelector.Interfaces;
 
namespace Api.Modules.DataSelectors.Services
{
    /// <summary>
    /// Service for the data selector in Wiser.
    /// </summary>
    public class DataSelectorsService : IDataSelectorsService, IScopedService
    {
        private readonly IWiserTenantsService wiserTenantsService;
        private readonly IDatabaseConnection clientDatabaseConnection;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly GclDataSelectors.IDataSelectorsService gclDataSelectorsService;
        private readonly IExcelService excelService;
        private readonly IDatabaseHelpersService databaseHelpersService;
        private readonly IWiserItemsService wiserItemsService;
        private readonly ICsvService csvService;

        private const string DataSelectorTemplateEntityType = "dataselector-template";

        /// <summary>
        /// Creates a new instance of <see cref="DataSelectorsService"/>
        /// </summary>
        public DataSelectorsService(IWiserTenantsService wiserTenantsService, IDatabaseConnection clientDatabaseConnection, IHttpContextAccessor httpContextAccessor, GclDataSelectors.IDataSelectorsService gclDataSelectorsService, IExcelService excelService, IDatabaseHelpersService databaseHelpersService, IWiserItemsService wiserItemsService, ICsvService csvService)
        {
            this.wiserTenantsService = wiserTenantsService;
            this.clientDatabaseConnection = clientDatabaseConnection;
            this.httpContextAccessor = httpContextAccessor;
            this.gclDataSelectorsService = gclDataSelectorsService;
            this.excelService = excelService;
            this.databaseHelpersService = databaseHelpersService;
            this.wiserItemsService = wiserItemsService;
            this.csvService = csvService;
        }

        /// <inheritdoc />
        public async Task<ServiceResult<List<DataSelectorEntityPropertyModel>>> GetEntityProperties(string entityName, bool forExportMode, ClaimsIdentity identity)
        {
            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();

            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("entityName", entityName);
            clientDatabaseConnection.AddParameter("forExportMode", forExportMode);

            var dataTable = await clientDatabaseConnection.GetAsync(@"
                SELECT
                    CONCAT(
                        property.`value`,
                        IF(
                            ?forExportMode = 1 AND property.languageCode <> '',
                            CONCAT('_', property.languageCode),
                            ''
                        )
                    ) AS `value`,
                    property.entityName,
                    CONCAT_WS(' - ', NULLIF(property.tabName, ''), property.displayName) AS displayName,
                    property.`value` AS propertyName,
                    IF(?forExportMode = TRUE, property.languageCode, '') AS languageCode
                FROM (
                    # ID.
                    SELECT 'id' AS `value`, ?entityName AS entityName, 'ID' AS displayName, '' AS languageCode, NULL AS tabName, 0 AS dynamicField, '0' AS sort

                    UNION

                    # Encrypted ID.
                    SELECT 'idencrypted' AS `value`, ?entityName AS entityName, 'Versleuteld ID' AS displayName, '' AS languageCode, NULL AS tabName, 0 AS dynamicField, '1' AS sort

                    UNION

                    # Unique ID.
                    SELECT 'unique_uuid' AS `value`, ?entityName AS entityName, 'Uniek ID' AS displayName, '' AS languageCode, NULL AS tabName, 0 AS dynamicField, '2' AS sort

                    UNION

                    # Title.
                    SELECT 'itemtitle' AS `value`, ?entityName AS entityName, 'Item titel' AS displayName, '' AS languageCode, NULL AS tabName, 0 AS dynamicField, '3' AS sort

                    UNION

                    # Changed on.
                    SELECT 'changed_on' AS `value`, ?entityName AS entityName, 'Gewijzigd op' AS displayName, '' AS languageCode, NULL AS tabName, 0 AS dynamicField, '4' AS sort

                    UNION

                    # Changed by.
                    SELECT 'changed_by' AS `value`, ?entityName AS entityName, 'Gewijzigd door' AS displayName, '' AS languageCode, NULL AS tabName, 0 AS dynamicField, '5' AS sort

                    UNION

                    # Entity properties.
                    (SELECT
                        IF(property_name = '', CreateJsonSafeProperty(display_name), property_name) AS `value`,
                        entity_name AS entityName,
                        CONCAT(
                            IF(
                                # Check if there are more than one properties with the same property name.
                                COUNT(*) > 1,
                                # If True; Use the property name with the character capitalizted to create the display name.
                                CONCAT(UPPER(SUBSTR(property_name, 1, 1)), SUBSTR(property_name, 2)),
                                # If False; Use the property's own display name.
                                IF(
                                    ?forExportMode = FALSE AND language_code <> '',
                                    CONCAT(' (', language_code, ')'),
                                    display_name
                                )
                            ),
                            IF(
                                ?forExportMode = TRUE AND language_code <> '',
                                CONCAT(' (', language_code, ')'),
                                ''
                            )
                        ) AS displayName,
                        language_code AS languageCode,
                        tab_name AS tabName,
                        1 AS dynamicField,
                        IF(COUNT(*) > 1, CONCAT(UPPER(SUBSTR(property_name, 1, 1)), SUBSTR(property_name, 2)), display_name) AS sort
                    FROM wiser_entityproperty
                    WHERE
                        entity_name = ?entityName
                        # Some entities should be ignored due to their input types.
                        AND inputtype NOT IN (
                            'action-button',
                            'auto-increment',
                            'button',
                            'chart',
                            'data-selector',
                            'empty',
                            'file-upload',
                            'grid',
                            'image-upload',
                            'item-linker',
                            'linked-item',
                            'querybuilder',
                            'scheduler',
                            'sub-entities-grid',
                            'timeline'
                        )
                    GROUP BY `value`, IF(?forExportMode = TRUE, language_code, NULL))

                    UNION

                    # SEO variants of the entity properties.
                    (SELECT
                        CONCAT(IF(property_name = '', CreateJsonSafeProperty(display_name), property_name), '_SEO') AS `value`,
                        entity_name AS entityName,
                        CONCAT(
                            IF(
                                # Check if there are more than one properties with the same property name.
                                COUNT(*) > 1,
                                # If True; Use the property name with the character capitalizted to create the display name.
                                CONCAT(UPPER(SUBSTR(property_name, 1, 1)), SUBSTR(property_name, 2)),
                                # If False; Use the property's own display name.
                                display_name
                            ),
                            IF(
                                language_code <> '',
                                CONCAT(' (', language_code, ')'),
                                ''
                            ),
                            ' (SEO)'
                        ) AS displayName,
                        language_code AS languageCode,
                        tab_name AS tabName,
                        1 AS dynamicField,
                        CONCAT(IF(COUNT(*) > 1, CONCAT(UPPER(SUBSTR(property_name, 1, 1)), SUBSTR(property_name, 2)), display_name), ' (SEO)') AS sort
                    FROM wiser_entityproperty
                    WHERE
                        entity_name = ?entityName
                        # Some entities should be ignored due to their input types.
                        AND inputtype NOT IN (
                            'action-button',
                            'auto-increment',
                            'button',
                            'chart',
                            'data-selector',
                            'empty',
                            'file-upload',
                            'grid',
                            'image-upload',
                            'item-linker',
                            'linked-item',
                            'querybuilder',
                            'scheduler',
                            'sub-entities-grid',
                            'timeline'
                        )
                        AND also_save_seo_value = 1
                    GROUP BY `value`, IF(?forExportMode = TRUE, language_code, NULL))
                ) AS property
                # Static fields first, then order by the 'sort' value.
                ORDER BY property.dynamicField, property.sort");

            var results = new List<DataSelectorEntityPropertyModel>();
            if (dataTable.Rows.Count == 0)
            {
                return new ServiceResult<List<DataSelectorEntityPropertyModel>>(results);
            }

            results.AddRange(dataTable.Rows.Cast<DataRow>().Select(dataRow => new DataSelectorEntityPropertyModel
            {
                UniqueId = dataRow.Field<string>("value"),
                PropertyName = dataRow.Field<string>("propertyName"),
                LanguageCode = dataRow.Field<string>("languageCode"),
                EntityName = dataRow.Field<string>("entityName"),
                DisplayName = dataRow.Field<string>("displayName")
            }));

            return new ServiceResult<List<DataSelectorEntityPropertyModel>>(results);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<List<DataSelectorModel>>> GetAsync(ClaimsIdentity identity, bool forExportModule = false, bool forRendering = false, bool forCommunicationModule = false, bool forBranches = false)
        {
            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            await databaseHelpersService.CheckAndUpdateTablesAsync(new List<string> { WiserTableNames.WiserDataSelector });

            var whereClauses = new List<string> { "removed = 0" };
            if (forExportModule)
            {
                whereClauses.Add("show_in_export_module = 1");
            }
            if (forCommunicationModule)
            {
                whereClauses.Add("show_in_communication_module = 1");
            }
            if (forRendering)
            {
                whereClauses.Add("available_for_rendering = 1");
            }

            if (forBranches)
            {
                whereClauses.Add("available_for_branches = 1");
            }

            var dataTable = await clientDatabaseConnection.GetAsync($@"SELECT id, name
FROM {WiserTableNames.WiserDataSelector}
WHERE {String.Join(" AND ", whereClauses)}
ORDER BY name ASC");

            var results = new List<DataSelectorModel>();
            if (dataTable.Rows.Count == 0)
            {
                return new ServiceResult<List<DataSelectorModel>>(results);
            }

            foreach (DataRow dataRow in dataTable.Rows)
            {
                results.Add(new DataSelectorModel
                {
                    Id = dataRow.Field<int>("id"),
                    EncryptedId = await wiserTenantsService.EncryptValue(dataRow.Field<int>("id").ToString(), identity),
                    Name = dataRow.Field<string>("name")
                });
            }

            return new ServiceResult<List<DataSelectorModel>>(results);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<int>> SaveAsync(ClaimsIdentity identity, DataSelectorModel data)
        {
            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            await databaseHelpersService.CheckAndUpdateTablesAsync(new List<string> { WiserTableNames.WiserDataSelector });

            clientDatabaseConnection.AddParameter("name", data.Name);
            clientDatabaseConnection.AddParameter("availableForRendering", data.AvailableForRendering);
            clientDatabaseConnection.AddParameter("defaultTemplate", data.DefaultTemplate);
            clientDatabaseConnection.AddParameter("requestJson", data.RequestJson);
            clientDatabaseConnection.AddParameter("savedJson", data.SavedJson);
            clientDatabaseConnection.AddParameter("showInExportModule", data.ShowInExportModule);
            clientDatabaseConnection.AddParameter("showInCommunicationModule", data.ShowInCommunicationModule);
            clientDatabaseConnection.AddParameter("showInDashboard", data.ShowInDashboard);
            clientDatabaseConnection.AddParameter("availableForBranches", data.AvailableForBranches);

            int result;
            var dataTable = await clientDatabaseConnection.GetAsync($@"SELECT id FROM `{WiserTableNames.WiserDataSelector}` WHERE name = ?name");
            if (dataTable.Rows.Count == 0)
            {
                result = (int)await clientDatabaseConnection.InsertRecordAsync($@"INSERT INTO {WiserTableNames.WiserDataSelector} (name, request_json, saved_json, show_in_export_module, available_for_rendering, default_template, show_in_communication_module, show_in_dashboard, available_for_branches)
                                                                                    VALUES (?name, ?requestJson, ?savedJson, ?showInExportModule, ?availableForRendering, ?defaultTemplate, ?showInCommunicationModule, ?showInDashboard, ?availableForBranches)");
            }
            else
            {
                result = dataTable.Rows[0].Field<int>("id");
                await clientDatabaseConnection.ExecuteAsync($@"UPDATE `{WiserTableNames.WiserDataSelector}` SET request_json = ?requestJson, saved_json = ?savedJson, show_in_export_module = ?showInExportModule, available_for_rendering = ?availableForRendering, default_template = ?defaultTemplate, show_in_communication_module = ?showInCommunicationModule, show_in_dashboard = ?showInDashboard, available_for_branches = ?availableForBranches WHERE name = ?name");
            }

            clientDatabaseConnection.AddParameter("id", result);

            // Set "show_in_dashboard" to 0 for all other data selector if this data selector has "show_in_dashboard" enabled.
            if (data.ShowInDashboard)
            {
                await clientDatabaseConnection.ExecuteAsync($"UPDATE `{WiserTableNames.WiserDataSelector}` SET show_in_dashboard = 0 WHERE id <> ?id");
            }

            // Add the permissions for the roles that have been marked. Will only add new ones to preserve limited permissions.
            var query = $@"INSERT IGNORE INTO `{WiserTableNames.WiserPermission}` (role_id, data_selector_id, permissions)
VALUES(?roleId, ?id, 15)";

            foreach (var role in data.AllowedRoles.Split(","))
            {
                clientDatabaseConnection.AddParameter("roleId", role);
                await clientDatabaseConnection.ExecuteAsync(query);
            }

            // Delete permissions for the roles that are missing in the allowed roles.
            clientDatabaseConnection.AddParameter("roles_with_permissions", data.AllowedRoles);
            query = $"DELETE FROM `{WiserTableNames.WiserPermission}` WHERE data_selector_id = ?id AND data_selector_id != 0 AND NOT FIND_IN_SET(role_id, ?roles_with_permissions)";
            await clientDatabaseConnection.ExecuteAsync(query);

            return new ServiceResult<int>(result);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<DataSelectorSignatureResultModel>> GenerateSignatureAsync(SortedList<string, string> values, ClaimsIdentity identity)
        {
            var tenant = await wiserTenantsService.GetSingleAsync(identity);
            var encryptionKey = tenant.ModelObject.EncryptionKey;
            var dateString = DateTime.Now.ToString("yyyyMMddHHmmss");

            var stringToHash = new StringBuilder();
            values["trace"] = "false";
            values["datetime"] = dateString;

            stringToHash.Append(String.Join("", values.Select(value => value.Key + value.Value)));
            stringToHash.Append("secret");
            stringToHash.Append(encryptionKey);

            var result = new DataSelectorSignatureResultModel
            {
                Signature = stringToHash.ToString().ToSha512ForPasswords(),
                ExtraQueryString = $"datetime={dateString}&trace=false"
            };

            return new ServiceResult<DataSelectorSignatureResultModel>(result);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<JArray>> GetResultsAsync(WiserDataSelectorRequestModel data, ClaimsIdentity identity)
        {
            var (jsonResult, statusCode, error) = await GetJsonResponseAsync(data, identity);
            if (statusCode != HttpStatusCode.OK)
            {
                return new ServiceResult<JArray>
                {
                    StatusCode = statusCode,
                    ErrorMessage = error
                };
            }

            return new ServiceResult<JArray>(jsonResult);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<string>> GetQueryAsync(WiserDataSelectorRequestModel data, ClaimsIdentity identity)
        {
            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                throw new Exception("HttpContext.Current is null, can't proceed.");
            }

            // Set the encryption key for the JCL internally. The JCL can't know which key to use otherwise.
            var tenant = (await wiserTenantsService.GetSingleAsync(identity)).ModelObject;
            GclSettings.Current.ExpiringEncryptionKey = tenant.EncryptionKey;

            var queryId = 0;
            var dataSelectorId = 0;
            if (data != null && !Int32.TryParse(data.QueryId, out queryId))
            {
                queryId = await wiserTenantsService.DecryptValue<int>(data.QueryId, identity);
            }

            if (data != null && !Int32.TryParse(data.EncryptedDataSelectorId, out dataSelectorId))
            {
                dataSelectorId = await wiserTenantsService.DecryptValue<int>(data.EncryptedDataSelectorId, identity);
                data.DataSelectorId = dataSelectorId;
            }

            if (data == null || (queryId == 0 && data.Settings == null && dataSelectorId == 0 && String.IsNullOrWhiteSpace(data.ContainsPath) && String.IsNullOrWhiteSpace(data.EntityTypes)))
            {
                return new ServiceResult<string>
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessage = "No data selector, path AND entity type found! Please make sure you supply either a valid JSON object or an ID of a valid selector, or a path + entity type."
                };
            }

            if (data.Settings != null)
            {
                // Never try to get the query with "insecure" set to true. The GCL will not allow it.
                data.Settings.Insecure = false;
            }

            var (itemsRequest, statusCode, error) = await gclDataSelectorsService.InitializeItemsRequestAsync(data, true);
            if (statusCode != HttpStatusCode.OK)
            {
                return new ServiceResult<string>
                {
                    StatusCode = statusCode,
                    ErrorMessage = error
                };
            }

            var query = await gclDataSelectorsService.GetQueryAsync(itemsRequest);
            return new ServiceResult<string>(query);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<byte[]>> ToExcelAsync(WiserDataSelectorRequestModel data, ClaimsIdentity identity)
        {
            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                throw new Exception("HttpContext.Current is null, can't proceed.");
            }

            // Set the encryption key for the JCL internally. The JCL can't know which key to use otherwise.
            var tenant = (await wiserTenantsService.GetSingleAsync(identity)).ModelObject;
            GclSettings.Current.ExpiringEncryptionKey = tenant.EncryptionKey;

            var (jsonResult, statusCode, error) = await GetJsonResponseAsync(data, identity);
            if (statusCode != HttpStatusCode.OK)
            {
                return new ServiceResult<byte[]>
                {
                    StatusCode = statusCode,
                    ErrorMessage = error
                };
            }

            var excelFile = excelService.JsonArrayToExcel(jsonResult);
            return new ServiceResult<byte[]>(excelFile);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<string>> ToHtmlAsync(WiserDataSelectorRequestModel data, ClaimsIdentity identity)
        {
            if (data == null)
            {
                return new ServiceResult<string>
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessage = "No data received"
                };
            }

            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                throw new Exception("HttpContext.Current is null, can't proceed.");
            }

            // Set the encryption key for the JCL internally. The JCL can't know which key to use otherwise.
            var tenant = (await wiserTenantsService.GetSingleAsync(identity)).ModelObject;
            GclSettings.Current.ExpiringEncryptionKey = tenant.EncryptionKey;

            // This is for backwards compatibility, a lot of queries in wiser_query contain {itemId_decrypt_withdate}, but the GCL expects something like {itemId:decrypt(true)}.
            // To not have to change all queries for all our tenants, we made this workaround so that old queries still work. The GCL will replace everything from httpContext.Items automatically.
            if (!String.IsNullOrWhiteSpace(httpContext.Request.Query["itemId"]))
            {
                httpContext.Items["itemId_decrypt_withdate"] = await wiserTenantsService.DecryptValue<ulong>(httpContext.Request.Query["itemId"], identity);
            }

            var (result, statusCode, error) = await gclDataSelectorsService.ToHtmlAsync(data);
            if (statusCode != HttpStatusCode.OK)
            {
                return new ServiceResult<string>
                {
                    StatusCode = statusCode,
                    ErrorMessage = error
                };
            }

            return new ServiceResult<string>(result);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<FileContentResult>> ToPdfAsync(WiserDataSelectorRequestModel data, ClaimsIdentity identity)
        {
            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                throw new Exception("HttpContext.Current is null, can't proceed.");
            }

            // Set the encryption key for the GCL internally. The GCL can't know which key to use otherwise.
            var tenant = (await wiserTenantsService.GetSingleAsync(identity)).ModelObject;
            GclSettings.Current.ExpiringEncryptionKey = tenant.EncryptionKey;

            var (result, statusCode, error) = await gclDataSelectorsService.ToPdfAsync(data);
            if (statusCode != HttpStatusCode.OK)
            {
                return new ServiceResult<FileContentResult>
                {
                    StatusCode = statusCode,
                    ErrorMessage = error
                };
            }

            return new ServiceResult<FileContentResult>(result);
        }
        
        /// <inheritdoc />
        public async Task<ServiceResult<byte[]>> ToCsvAsync(WiserDataSelectorRequestModel data, ClaimsIdentity identity, char separator)
        {
            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                throw new Exception("HttpContext.Current is null, can't proceed.");
            }

            // Set the encryption key for the GCL internally. The GCL can't know which key to use otherwise.
            var tenant = (await wiserTenantsService.GetSingleAsync(identity)).ModelObject;
            GclSettings.Current.ExpiringEncryptionKey = tenant.EncryptionKey;

            var (jsonResult, statusCode, error) = await GetJsonResponseAsync(data, identity);
            if (statusCode != HttpStatusCode.OK)
            {
                return new ServiceResult<byte[]>
                {
                    StatusCode = statusCode,
                    ErrorMessage = error
                };
            }
            
            var csvBody = csvService.JsonArrayToCsv(jsonResult);
            var buffer = Encoding.UTF8.GetBytes(csvBody);
            
            return new ServiceResult<byte[]>(buffer);
        }

        /// <inheritdoc />
        public IActionResult CreateFileResult(WiserDataSelectorRequestModel data, ServiceResult<byte[]> result, string defaultFileName, string extension, string contentType)
        {
            if (String.IsNullOrWhiteSpace(data.FileName))
            {
                data.FileName = defaultFileName;
            }
            data.FileName = Path.ChangeExtension(data.FileName, extension);

            var fileResult = new FileContentResult(result.ModelObject, contentType)
            {
                FileDownloadName = data.FileName
            };

            return fileResult;
        }

        /// <inheritdoc />
        public IActionResult SetFileName(WiserDataSelectorRequestModel data, ServiceResult<FileContentResult> result, string defaultFileName, string extension)
        {
            if (String.IsNullOrWhiteSpace(data.FileName))
            {
                data.FileName = defaultFileName;
            }
            data.FileName = Path.ChangeExtension(data.FileName, extension);

            result.ModelObject.FileDownloadName = data.FileName;
            return result.ModelObject;
        }

        /// <inheritdoc />
        public async Task<ServiceResult<List<WiserItemModel>>> GetTemplatesAsync(ClaimsIdentity identity)
        {
            var tablePrefix = await wiserItemsService.GetTablePrefixForEntityAsync(DataSelectorTemplateEntityType);
            clientDatabaseConnection.AddParameter("entityType", DataSelectorTemplateEntityType);
            var dataTable = await clientDatabaseConnection.GetAsync($@"SELECT id, title 
                                                                        FROM {tablePrefix}{WiserTableNames.WiserItem} 
                                                                        WHERE entity_type = ?entityType
                                                                        ORDER BY title ASC");
            if (dataTable.Rows.Count == 0)
            {
                return new ServiceResult<List<WiserItemModel>>(new List<WiserItemModel>());
            }

            var results = dataTable.Rows.Cast<DataRow>().Select(dataRow => new WiserItemModel
            {
                Id = dataRow.Field<ulong>("id"),
                Title = dataRow.Field<string>("title")
            }).ToList();

            return new ServiceResult<List<WiserItemModel>>(results);
        }

        private async Task<(JArray Result, HttpStatusCode StatusCode, string Error)> GetJsonResponseAsync(WiserDataSelectorRequestModel data, ClaimsIdentity identity)
        {
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                throw new Exception("HttpContext.Current is null, can't proceed.");
            }

            // Set the encryption key for the JCL internally. The JCL can't know which key to use otherwise.
            var tenant = (await wiserTenantsService.GetSingleAsync(identity)).ModelObject;
            GclSettings.Current.ExpiringEncryptionKey = tenant.EncryptionKey;

            var queryId = 0;
            var dataSelectorId = 0;
            if (!String.IsNullOrWhiteSpace(data?.QueryId) && !Int32.TryParse(data.QueryId, out queryId))
            {
                queryId = await wiserTenantsService.DecryptValue<int>(data.QueryId, identity);
            }

            if (!String.IsNullOrWhiteSpace(data?.EncryptedDataSelectorId) && !Int32.TryParse(data.EncryptedDataSelectorId, out dataSelectorId))
            {
                dataSelectorId = await wiserTenantsService.DecryptValue<int>(data.EncryptedDataSelectorId, identity);
                data.DataSelectorId = dataSelectorId;
            }

            if (data == null || (queryId == 0 && data.Settings == null && dataSelectorId == 0 && String.IsNullOrWhiteSpace(data.ContainsPath) && String.IsNullOrWhiteSpace(data.EntityTypes)))
            {
                return (null, HttpStatusCode.BadRequest, "No data selector, path AND entity type found! Please make sure you supply either a valid JSON object or an ID of a valid selector, or a path + entity type.");
            }

            if (data.Settings != null)
            {
                // Never try to get the result with "insecure" set to true. The GCL will not allow it.
                data.Settings.Insecure = false;
            }

            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();

            return await gclDataSelectorsService.GetJsonResponseAsync(data, true);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<JToken>> GetDataSelectorResultAsJsonAsync(ClaimsIdentity identity, int id, bool asKeyValuePair, List<KeyValuePair<string, object>> parameters, bool skipPermissionsCheck = false)
        {
            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("id", id);
            var query = $"SELECT id FROM {WiserTableNames.WiserDataSelector} WHERE id = ?id";
            var dataTable = await clientDatabaseConnection.GetAsync(query);
            if (dataTable.Rows.Count == 0)
            {
                return new ServiceResult<JToken>
                {
                    StatusCode = HttpStatusCode.NotFound,
                    ErrorMessage = $"Data selector with ID '{id}' does not exist."
                };
            }

            if (!skipPermissionsCheck && (await wiserItemsService.GetUserDataSelectorPermissionsAsync(id, IdentityHelpers.GetWiserUserId(identity)) & AccessRights.Read) == AccessRights.Nothing)
            {
                return new ServiceResult<JToken>
                {
                    StatusCode = HttpStatusCode.Unauthorized,
                    ErrorMessage = $"Wiser user '{IdentityHelpers.GetUserName(identity)}' has no permission to execute this data selector."
                };
            }

            var dataSelectorSettings = new WiserDataSelectorRequestModel
            {
                EncryptedDataSelectorId = await wiserTenantsService.EncryptValue(id, identity),
                ExtraData = parameters
            };

            var response = await GetJsonResponseAsync(dataSelectorSettings, identity);

            // If the data selector was not successful return the status it had given.
            if (response.StatusCode != HttpStatusCode.OK)
            {
                return new ServiceResult<JToken>
                {
                    StatusCode = response.StatusCode,
                    ErrorMessage = response.Error
                };
            }

            // If object does not need to be packed as key value pair return as is.
            if (!asKeyValuePair)
            {
                return new ServiceResult<JToken>(response.Result);
            }

            // Combine object to key value pair.
            var combinedResult = new JObject();

            foreach (var item in response.Result)
            {
                combinedResult.Add(item["key"].ToString(), item["value"]);
            }

            return new ServiceResult<JToken>(combinedResult);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<string>> CheckDashboardConflictAsync(int id)
        {
            clientDatabaseConnection.AddParameter("id", id);
            var getDataSelectorResult = await clientDatabaseConnection.GetAsync($"SELECT `name` FROM {WiserTableNames.WiserDataSelector} WHERE id <> ?id AND show_in_dashboard = 1 LIMIT 1");
            return new ServiceResult<string>(getDataSelectorResult.Rows.Count == 0 ? null : getDataSelectorResult.Rows[0].Field<string>("name"));
        }

        /// <inheritdoc />
        public async Task<ServiceResult<int>> ExistsAsync(string name)
        {
            clientDatabaseConnection.AddParameter("name", name);
            var getDataSelectorResult = await clientDatabaseConnection.GetAsync($"SELECT id FROM {WiserTableNames.WiserDataSelector} WHERE name = ?name LIMIT 1");
            return new ServiceResult<int>(getDataSelectorResult.Rows.Count == 0 ? 0 : getDataSelectorResult.Rows[0].Field<int>("id"));
        }
    }
}
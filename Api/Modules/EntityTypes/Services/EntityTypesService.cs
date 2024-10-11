using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Helpers;
using Api.Core.Services;
using Api.Modules.EntityTypes.Models;
using Api.Modules.Tenants.Interfaces;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;
using IEntityTypesService = Api.Modules.EntityTypes.Interfaces.IEntityTypesService;
using IBranchesService = Api.Modules.Branches.Interfaces.IBranchesService;

namespace Api.Modules.EntityTypes.Services
{
    /// <inheritdoc cref="IEntityTypesService" />
    public class EntityTypesService : IEntityTypesService, IScopedService
    {
        private readonly IWiserTenantsService wiserTenantsService;
        private readonly IDatabaseConnection clientDatabaseConnection;
        private readonly IWiserItemsService wiserItemsService;
        private readonly IDatabaseHelpersService databaseHelpersService;
        private readonly IServiceProvider serviceProvider;
        private readonly IBranchesService branchesService;

        /// <summary>
        /// Creates a new instance of <see cref="EntityTypesService"/>.
        /// </summary>
        public EntityTypesService(IWiserTenantsService wiserTenantsService, IDatabaseConnection clientDatabaseConnection, IWiserItemsService wiserItemsService, IDatabaseHelpersService databaseHelpersService, IServiceProvider serviceProvider, IBranchesService branchesService)
        {
            this.wiserTenantsService = wiserTenantsService;
            this.clientDatabaseConnection = clientDatabaseConnection;
            this.wiserItemsService = wiserItemsService;
            this.databaseHelpersService = databaseHelpersService;
            this.serviceProvider = serviceProvider;
            this.branchesService = branchesService;
        }

        /// <inheritdoc />
        public async Task<ServiceResult<List<EntityTypeModel>>> GetAsync(ClaimsIdentity identity, bool onlyEntityTypesWithDisplayName = true, bool includeCount = false, bool skipEntitiesWithoutItems = false, int moduleId = 0, int branchId = 0)
        {
            using var scope = serviceProvider.CreateScope();
            var databaseConnectionResult = await branchesService.GetBranchDatabaseConnectionAsync(scope, identity, branchId);
            if (databaseConnectionResult.StatusCode != HttpStatusCode.OK)
            {
                return new ServiceResult<List<EntityTypeModel>>
                {
                    ErrorMessage = databaseConnectionResult.ErrorMessage,
                    StatusCode = databaseConnectionResult.StatusCode
                };
            }

            var databaseConnectionToUse = databaseConnectionResult.ModelObject;
            var databaseHelpersServiceToUse = this.databaseHelpersService;

            if (branchId > 0)
            {
                databaseHelpersServiceToUse = scope.ServiceProvider.GetRequiredService<IDatabaseHelpersService>();
            }

            var result = new List<EntityTypeModel>();
            var query = $@"SELECT 
	entity.name, 
	IF(entity.friendly_name IS NULL OR entity.friendly_name = '', entity.name, entity.friendly_name) AS displayName,
	entity.module_id,
    module.`name` AS moduleName,
    entity.dedicated_table_prefix
FROM {WiserTableNames.WiserEntity} AS entity
LEFT JOIN {WiserTableNames.WiserModule} AS module ON module.id = entity.module_id
WHERE entity.`name` <> ''
{(onlyEntityTypesWithDisplayName ? "AND entity.friendly_name IS NOT NULL AND entity.friendly_name <> ''" : "")}
{(moduleId <= 0 ? "" : "AND entity.module_id = ?moduleId")}
GROUP BY entity.`name`
ORDER BY CONCAT(IF(entity.friendly_name IS NULL OR entity.friendly_name = '', entity.name, entity.friendly_name), IF(module.`name` IS NULL, '', CONCAT(' (', module.`name`, ')'))) ASC";

            databaseConnectionToUse.AddParameter("moduleId", moduleId);
            await databaseConnectionToUse.EnsureOpenConnectionForReadingAsync();
            var dataTable = await databaseConnectionToUse.GetAsync(query);
            if (dataTable.Rows.Count == 0)
            {
                return new ServiceResult<List<EntityTypeModel>>(result);
            }

            // First add all entity types to the results list.
            result.AddRange(dataTable.Rows.Cast<DataRow>()
                            .Select(dataRow => new EntityTypeModel
                            {
                                DisplayName = dataRow.Field<string>("displayName"),
                                Id = dataRow.Field<string>("name"),
                                ModuleId = dataRow.Field<int>("module_id"),
                                ModuleName = dataRow.Field<string>("moduleName") ?? "",
                                DedicatedTablePrefix = wiserItemsService.GetTablePrefixForEntity(new EntitySettingsModel {DedicatedTablePrefix = dataRow.Field<string>("dedicated_table_prefix")})
                            }));

            // Then count the amount of items per entity type.
            if (includeCount)
            {
                // Group all entities by table prefix, so that we can build one query per wiser_item table.
                foreach (var group in result.GroupBy(entity => entity.DedicatedTablePrefix))
                {
                    var tableName = $"{group.Key}{WiserTableNames.WiserItem}";
                    if (!await databaseHelpersServiceToUse.TableExistsAsync(tableName))
                    {
                        continue;
                    }

                    //  Count the amount of entity types per type per table.
                    query = $"""
                             SELECT entity_type, COUNT(*) AS totalItems 
                             FROM {tableName}
                             WHERE entity_type IN ({String.Join(", ", group.Select(entity => entity.Id.ToMySqlSafeValue(true)))})
                             GROUP BY entity_type
                             """;

                    var countDataTable = await databaseConnectionToUse.GetAsync(query);
                    foreach (DataRow dataRow in countDataTable.Rows)
                    {
                        var entityType = dataRow.Field<String>("entity_type");
                        group.Single(entity => String.Equals(entity.Id, entityType, StringComparison.OrdinalIgnoreCase)).TotalItems = Convert.ToInt32(dataRow["totalItems"]);
                    }
                }
            }

            if (includeCount && skipEntitiesWithoutItems)
            {
                result = result.Where(entity => entity.TotalItems is > 0).ToList();
            }

            return new ServiceResult<List<EntityTypeModel>>(result);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<EntitySettingsModel>> GetAsync(ClaimsIdentity identity, string entityType, int moduleId = 0)
        {
            var result = await wiserItemsService.GetEntityTypeSettingsAsync(entityType, moduleId);
            if (String.IsNullOrEmpty(result?.EntityType))
            {
                return new ServiceResult<EntitySettingsModel>
                {
                    StatusCode = HttpStatusCode.NotFound
                };
            }

            return new ServiceResult<EntitySettingsModel>(result);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<EntitySettingsModel>> GetAsync(ClaimsIdentity identity, int id)
        {
            if (id <= 0)
            {
                return new ServiceResult<EntitySettingsModel>
                {
                    ErrorMessage = "Parameter 'id' should be greater than 0.",
                    StatusCode = HttpStatusCode.BadRequest
                };
            }

            clientDatabaseConnection.AddParameter("id", id);
            var dataTable = await clientDatabaseConnection.GetAsync($"SELECT * FROM {WiserTableNames.WiserEntity} WHERE id = ?id");
            if (dataTable.Rows.Count == 0)
            {
                return new ServiceResult<EntitySettingsModel>
                {
                    ErrorMessage = $"Entity with id '{id}' not found.",
                    StatusCode = HttpStatusCode.NotFound
                };
            }

            var dataRow = dataTable.Rows[0];
            var defaultOrdering = dataRow.Field<string>("default_ordering") switch
            {
                "link_ordering" => EntityOrderingTypes.LinkOrdering,
                "item_title" => EntityOrderingTypes.ItemTitle,
                _ => throw new ArgumentOutOfRangeException("default_ordering", dataRow.Field<string>("default_ordering"))
            };
            var deleteAction = dataRow.Field<string>("delete_action") switch
            {
                "archive" => EntityDeletionTypes.Archive,
                "permanent" => EntityDeletionTypes.Permanent,
                "hide" => EntityDeletionTypes.Hide,
                "disallow" => EntityDeletionTypes.Disallow,
                _ => throw new ArgumentOutOfRangeException("delete_action", dataRow.Field<string>("delete_action"))
            };

            var acceptedChildTypes = new List<string>();
            var acceptedChildTypesValue = dataRow.Field<string>("accepted_childtypes");
            if (!String.IsNullOrWhiteSpace(acceptedChildTypesValue))
            {
                acceptedChildTypes.AddRange(acceptedChildTypesValue.Split(','));
            }

            var result = new EntitySettingsModel
            {
                Id = id,
                EntityType = dataRow.Field<string>("name"),
                ModuleId = dataRow.Field<int>("module_id"),
                AcceptedChildTypes = acceptedChildTypes,
                Icon = dataRow.Field<string>("icon"),
                IconAdd = dataRow.Field<string>("icon_add"),
                ShowInTreeView = Convert.ToBoolean(dataRow["show_in_tree_view"]),
                QueryAfterInsert = dataRow.Field<string>("query_after_insert"),
                QueryAfterUpdate = dataRow.Field<string>("query_after_update"),
                QueryBeforeUpdate = dataRow.Field<string>("query_before_update"),
                QueryBeforeDelete = dataRow.Field<string>("query_before_delete"),
                Color = dataRow.Field<string>("color"),
                ShowInSearch = Convert.ToBoolean(dataRow["show_in_search"]),
                ShowOverviewTab = Convert.ToBoolean(dataRow["show_overview_tab"]),
                SaveTitleAsSeo = Convert.ToBoolean(dataRow["save_title_as_seo"]),
                ApiAfterInsert = dataRow.Field<int?>("api_after_insert"),
                ApiAfterUpdate = dataRow.Field<int?>("api_after_update"),
                ApiBeforeUpdate = dataRow.Field<int?>("api_before_update"),
                ApiBeforeDelete = dataRow.Field<int?>("api_before_delete"),
                ShowTitleField = Convert.ToBoolean(dataRow["show_title_field"]),
                DisplayName = dataRow.Field<string>("friendly_name"),
                SaveHistory = Convert.ToBoolean(dataRow["save_history"]),
                DefaultOrdering = defaultOrdering,
                TemplateQuery = dataRow.Field<string>("template_query"),
                TemplateHtml = dataRow.Field<string>("template_html"),
                EnableMultipleEnvironments = Convert.ToBoolean(dataRow["enable_multiple_environments"]),
                IconExpanded = dataRow.Field<string>("icon_expanded"),
                DedicatedTablePrefix = dataRow.Field<string>("dedicated_table_prefix"),
                DeleteAction = deleteAction,
                ShowInDashboard = Convert.ToBoolean(dataRow["show_in_dashboard"])
            };

            return new ServiceResult<EntitySettingsModel>(result);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<List<EntityTypeModel>>> GetAvailableEntityTypesAsync(ClaimsIdentity identity, int moduleId, string parentId = null)
        {
            ulong actualParentId;
            if (String.IsNullOrWhiteSpace(parentId))
            {
                actualParentId = 0;
            }
            else if (!UInt64.TryParse(parentId, out actualParentId))
            {
                actualParentId = await wiserTenantsService.DecryptValue<ulong>(parentId, identity);
            }

            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("moduleId", moduleId);
            clientDatabaseConnection.AddParameter("parentId", actualParentId);

            var result = new List<EntityTypeModel>();
            var query = $@"SELECT 
                                childEntity.name, 
                                IF(childEntity.friendly_name IS NULL OR childEntity.friendly_name = '', childEntity.name, childEntity.friendly_name) AS displayName
                            FROM {WiserTableNames.WiserEntity} AS entity
                            LEFT JOIN {WiserTableNames.WiserItem} AS item ON item.entity_type = entity.name AND item.moduleid = entity.module_id
                            JOIN {WiserTableNames.WiserEntity} AS childEntity ON childEntity.module_id = ?moduleId AND childEntity.name <> '' AND FIND_IN_SET(childEntity.name, entity.accepted_childtypes)
                            WHERE entity.module_id = ?moduleId
                            AND ((?parentId = 0 AND entity.name = '') OR (?parentId > 0 AND item.id = ?parentId))
                            GROUP BY childEntity.name
                            ORDER BY childEntity.name";

            var dataTable = await clientDatabaseConnection.GetAsync(query);
            if (dataTable.Rows.Count == 0)
            {
                return new ServiceResult<List<EntityTypeModel>>(result);
            }

            result.AddRange(dataTable.Rows.Cast<DataRow>().Select(dataRow => new EntityTypeModel
            {
                Id = dataRow.Field<string>("name"),
                DisplayName = dataRow.Field<string>("displayName")
            }));

            return new ServiceResult<List<EntityTypeModel>>(result);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<long>> CreateAsync(ClaimsIdentity identity, string name, int moduleId = 0)
        {
            try
            {
                // Empty string is allowed, so a root entity can be created for a module.
                clientDatabaseConnection.AddParameter("name", name ?? String.Empty);
                clientDatabaseConnection.AddParameter("moduleId", moduleId);
                var result = await clientDatabaseConnection.InsertRecordAsync($"INSERT INTO {WiserTableNames.WiserEntity} (name, module_id) VALUES (?name, ?moduleId)");
                return new ServiceResult<long>(result);
            }
            catch (MySqlException mySqlException)
            {
                if (mySqlException.Number == (int)MySqlErrorCode.DuplicateKeyEntry)
                {
                    return new ServiceResult<long>
                    {
                        StatusCode = HttpStatusCode.Conflict,
                        ErrorMessage = $"An entry already exists with {nameof(name)} = '{name}', {nameof(moduleId)} = '{moduleId}'"
                    };
                }

                throw;
            }
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> UpdateAsync(ClaimsIdentity identity, int id, EntitySettingsModel settings)
        {
            if (id <= 0)
            {
                return new ServiceResult<bool>
                {
                    ErrorMessage = "Parameter 'id' should be greater than 0.",
                    StatusCode = HttpStatusCode.BadRequest
                };
            }

            // First try to create the tables, if we have a dedicated table prefix.
            var tablePrefix = settings.DedicatedTablePrefix;
            if (!String.IsNullOrWhiteSpace(tablePrefix))
            {
                if (!tablePrefix.EndsWith("_"))
                {
                    tablePrefix += "_";
                }

                if (!await databaseHelpersService.TableExistsAsync($"{tablePrefix}{WiserTableNames.WiserItem}"))
                {
                    await clientDatabaseConnection.ExecuteAsync($@"CREATE TABLE `{tablePrefix}{WiserTableNames.WiserItem}` LIKE `{WiserTableNames.WiserItem}`;
CREATE TABLE `{tablePrefix}{WiserTableNames.WiserItem}{WiserTableNames.ArchiveSuffix}` LIKE `{WiserTableNames.WiserItem}{WiserTableNames.ArchiveSuffix}`;
CREATE TABLE `{tablePrefix}{WiserTableNames.WiserItemDetail}` LIKE `{WiserTableNames.WiserItemDetail}`;
CREATE TABLE `{tablePrefix}{WiserTableNames.WiserItemDetail}{WiserTableNames.ArchiveSuffix}` LIKE `{WiserTableNames.WiserItemDetail}{WiserTableNames.ArchiveSuffix}`;
CREATE TABLE `{tablePrefix}{WiserTableNames.WiserItemFile}` LIKE `{WiserTableNames.WiserItemFile}`;
CREATE TABLE `{tablePrefix}{WiserTableNames.WiserItemFile}{WiserTableNames.ArchiveSuffix}` LIKE `{WiserTableNames.WiserItemFile}{WiserTableNames.ArchiveSuffix}`;");

                    var createTriggersQuery = await ResourceHelpers.ReadTextResourceFromAssemblyAsync("Api.Core.Queries.WiserInstallation.CreateDedicatedItemTablesTriggers.sql");
                    createTriggersQuery = createTriggersQuery.Replace("{tablePrefix}", tablePrefix);
                    await clientDatabaseConnection.ExecuteAsync(createTriggersQuery);
                }
            }

            // Then update the entity in wiser_entity. If the creation of tables somehow fails, this code will not be executed, that is on purpose.
            var defaultOrdering = settings.DefaultOrdering switch
            {
                EntityOrderingTypes.ItemTitle => "item_title",
                EntityOrderingTypes.LinkOrdering => "link_ordering",
                _ => throw new ArgumentOutOfRangeException(nameof(settings.DefaultOrdering), settings.DefaultOrdering.ToString())
            };

            var deleteAction = settings.DeleteAction switch
            {
                EntityDeletionTypes.Archive => "archive",
                EntityDeletionTypes.Permanent => "permanent",
                EntityDeletionTypes.Hide => "hide",
                EntityDeletionTypes.Disallow => "disallow",
                _ => throw new ArgumentOutOfRangeException(nameof(settings.DeleteAction), settings.DeleteAction.ToString())
            };

            // Check if the name has been changed.
            clientDatabaseConnection.AddParameter("id", id);
            var query = $"SELECT name FROM {WiserTableNames.WiserEntity} WHERE id = ?id";
            var dataTable = await clientDatabaseConnection.GetAsync(query);
            if (dataTable.Rows.Count == 0)
            {
                return new ServiceResult<bool>
                {
                    StatusCode = HttpStatusCode.NotFound,
                    ErrorMessage = $"Entity with ID '{id}' does not exist"
                };
            }

            var previousName = dataTable.Rows[0].Field<string>("name");
            if (!String.Equals(previousName, settings.EntityType, StringComparison.OrdinalIgnoreCase))
            {
                // Update the name in wiser_entityproperty and wiser_entity.
                clientDatabaseConnection.AddParameter("previousName", previousName);
                clientDatabaseConnection.AddParameter("newName", settings.EntityType);
                query = $@"UPDATE {WiserTableNames.WiserEntityProperty} 
SET options = JSON_PRETTY(JSON_REPLACE(options, '$.entityType', ?newName))
WHERE JSON_VALID(options) AND JSON_EXTRACT(options, '$.entityType') = ?previousName;

UPDATE {WiserTableNames.WiserEntityProperty}
SET entity_name = ?newName
WHERE entity_name = ?previousName;";
                await clientDatabaseConnection.ExecuteAsync(query);

                // This query might seem a bit strange, but it's to make sure we only replace the complete entity name, in case there exists entity names that contain the given name.
                // For example, if we were to replace 'basket' with something else, we don't want to also replace 'basketline'.
                // This is done by first adding a comma at the start and the end, so that we can then simply replace ",x," with ",y," and then trim the commas again.
                query = $@"UPDATE {WiserTableNames.WiserEntity}
SET accepted_childtypes = TRIM(BOTH ',' FROM REPLACE(CONCAT(',', accepted_childtypes, ','), CONCAT(',', ?previousName, ','), CONCAT(',', ?newName, ',')))
WHERE FIND_IN_SET(?previousName, accepted_childtypes)";
                await clientDatabaseConnection.ExecuteAsync(query);
            }

            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("name", settings.EntityType);
            clientDatabaseConnection.AddParameter("module_id", settings.ModuleId);
            clientDatabaseConnection.AddParameter("accepted_childtypes", String.Join(",", settings.AcceptedChildTypes));
            clientDatabaseConnection.AddParameter("icon", settings.Icon);
            clientDatabaseConnection.AddParameter("icon_add", settings.IconAdd);
            clientDatabaseConnection.AddParameter("show_in_tree_view", settings.ShowInTreeView);
            clientDatabaseConnection.AddParameter("query_after_insert", settings.QueryAfterInsert);
            clientDatabaseConnection.AddParameter("query_after_update", settings.QueryAfterUpdate);
            clientDatabaseConnection.AddParameter("query_before_update", settings.QueryBeforeUpdate);
            clientDatabaseConnection.AddParameter("query_before_delete", settings.QueryBeforeDelete);
            clientDatabaseConnection.AddParameter("color", settings.Color);
            clientDatabaseConnection.AddParameter("show_in_search", settings.ShowInSearch);
            clientDatabaseConnection.AddParameter("show_overview_tab", settings.ShowOverviewTab);
            clientDatabaseConnection.AddParameter("save_title_as_seo", settings.SaveTitleAsSeo);
            clientDatabaseConnection.AddParameter("api_after_insert", settings.ApiAfterInsert);
            clientDatabaseConnection.AddParameter("api_after_update", settings.ApiAfterUpdate);
            clientDatabaseConnection.AddParameter("api_before_update", settings.ApiBeforeUpdate);
            clientDatabaseConnection.AddParameter("api_before_delete", settings.ApiBeforeDelete);
            clientDatabaseConnection.AddParameter("show_title_field", settings.ShowTitleField);
            clientDatabaseConnection.AddParameter("friendly_name", settings.DisplayName);
            clientDatabaseConnection.AddParameter("save_history", settings.SaveHistory);
            clientDatabaseConnection.AddParameter("default_ordering", defaultOrdering);
            clientDatabaseConnection.AddParameter("template_query", settings.TemplateQuery);
            clientDatabaseConnection.AddParameter("template_html", settings.TemplateHtml);
            clientDatabaseConnection.AddParameter("enable_multiple_environments", settings.EnableMultipleEnvironments);
            clientDatabaseConnection.AddParameter("icon_expanded", settings.IconExpanded);
            clientDatabaseConnection.AddParameter("dedicated_table_prefix", settings.DedicatedTablePrefix);
            clientDatabaseConnection.AddParameter("delete_action", deleteAction);
            clientDatabaseConnection.AddParameter("show_in_dashboard", settings.ShowInDashboard);
            await clientDatabaseConnection.InsertOrUpdateRecordBasedOnParametersAsync(WiserTableNames.WiserEntity, id);

            return new ServiceResult<bool>(true)
            {
                StatusCode = HttpStatusCode.NoContent
            };
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> DeleteAsync(ClaimsIdentity identity, int id)
        {
            if (id <= 0)
            {
                throw new ArgumentException("Id must be greater than 0.", nameof(id));
            }

            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            clientDatabaseConnection.AddParameter("id", id);

            // Delete all properties/fields of the entity and then the entity itself.
            var query = $@"DELETE property.* 
FROM {WiserTableNames.WiserEntity} AS entity
JOIN {WiserTableNames.WiserEntityProperty} AS property ON property.entity_name = entity.name AND property.module_id IN (0, entity.module_id)
WHERE entity.id = ?id;
DELETE FROM {WiserTableNames.WiserEntity} WHERE id = ?id;";
            await clientDatabaseConnection.ExecuteAsync(query);

            return new ServiceResult<bool>(true)
            {
                StatusCode = HttpStatusCode.NoContent
            };
        }

        /// <inheritdoc />
        public async Task<ServiceResult<int>> GetApiConnectionIdAsync(string entityType, string actionType)
        {
            if (String.IsNullOrWhiteSpace(entityType))
            {
                throw new ArgumentNullException(nameof(entityType), "The parameter 'entityType' needs to have a value.");
            }
            if (String.IsNullOrWhiteSpace(actionType))
            {
                throw new ArgumentNullException(nameof(actionType), "The parameter 'actionType' needs to have a value.");
            }

            var columnName = actionType.ToLowerInvariant() switch
            {
                "after_insert" => "api_after_insert",
                "after_update" => "api_after_update",
                "before_update" => "api_before_update",
                "before_delete" => "api_before_delete",
                _ => throw new ArgumentOutOfRangeException(nameof(actionType), actionType, $"Invalid value for {nameof(actionType)}")
            };

            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            clientDatabaseConnection.AddParameter("entityType", entityType);

            var query = $@"SELECT {columnName} FROM {WiserTableNames.WiserEntity} WHERE name = ?entityType ORDER BY {columnName} DESC LIMIT 1";
            var dataTable = await clientDatabaseConnection.GetAsync(query);
            return new ServiceResult<int>(dataTable.Rows.Count == 0 ? 0 : dataTable.Rows[0].Field<int?>(columnName) ?? 0);
        }
    }
}
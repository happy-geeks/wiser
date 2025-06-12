using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Mime;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Helpers;
using Api.Core.Interfaces;
using Api.Core.Models;
using Api.Modules.EntityTypes.Models;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Enums;
using GeeksCoreLibrary.Modules.Databases.Helpers;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Databases.Models;
using GeeksCoreLibrary.Modules.Databases.Services;
using LazyCache;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using Constants = GeeksCoreLibrary.Modules.Databases.Models.Constants;
using IEntityTypesService = Api.Modules.EntityTypes.Interfaces.IEntityTypesService;
using IGclEntityTypesService = GeeksCoreLibrary.Core.Interfaces.IEntityTypesService;

namespace Api.Core.Services;

/// <inheritdoc cref="IWiserDatabaseHelpersService" />
public class WiserDatabaseHelpersService(
    IDatabaseConnection clientDatabaseConnection,
    IDatabaseHelpersService databaseHelpersService,
    IServiceProvider serviceProvider,
    IEntityTypesService entityTypesService,
    ILinkTypesService linkTypesService,
    IAppCache cache,
    IGclEntityTypesService gclEntityTypesService)
    : IWiserDatabaseHelpersService, IScopedService
{
    // Constants for custom migrations.
    private const string TriggersName = "wiser_triggers";
    private const string RemoveVirtualColumnsName = "wiser_remove_virtual_columns";
    private const string AddBranchSettingsModuleName = "wiser_add_branch_settings_module";
    private const string UpdateFileSecuritySettingsName = "wiser_update_file_security_settings";
    private const string AddPaymentMethodFilterProperties = "wiser_add_paymentmethod_filter_properties";
    private const string AddProductsApiSettingsName = "wiser_add_products_api_settings";

    /// <summary>
    /// The list of tables that need to be updated first, because others depend on them.
    /// </summary>
    private static readonly List<string> FirstPriorityTables =
    [
        WiserTableNames.WiserEntity,
        WiserTableNames.WiserEntityProperty,
        WiserTableNames.WiserLink
    ];

    /// <summary>
    /// The list of tables that should be updated after the first priority tables.
    /// </summary>
    private static readonly List<string> SecondPriorityTables =
    [
        WiserTableNames.WiserItem,
        WiserTableNames.WiserItemDetail,
        WiserTableNames.WiserModule,
        WiserTableNames.WiserItemFile,
        WiserTableNames.WiserItemLink,
        WiserTableNames.WiserItemLinkDetail,
        WiserTableNames.WiserDataSelector,
        WiserTableNames.WiserTemplate,
        WiserTableNames.WiserTemplateExternalFiles,
        WiserTableNames.WiserDynamicContent,
        WiserTableNames.WiserTemplateDynamicContent,
        WiserTableNames.WiserTemplatePublishLog,
        WiserTableNames.WiserPreviewProfiles,
        WiserTableNames.WiserDynamicContentPublishLog,
        WiserTableNames.WiserQuery,
        WiserTableNames.WiserPermission,
        WiserTableNames.WiserUserRoles,
        WiserTableNames.WiserCommunication,
        WiserTableNames.WiserStyledOutput,
        WiserTableNames.WiserParentUpdates,
        WiserTableNames.WiserHistory,
        Constants.DatabaseConnectionLogTableName
    ];

    /// <summary>
    /// The list of custom migrations that can be triggered automatically when a user logs in.
    /// </summary>
    private static readonly List<string> AutomaticCustomMigrations =
    [
        TriggersName,
        RemoveVirtualColumnsName,
        AddBranchSettingsModuleName,
        AddPaymentMethodFilterProperties,
        AddProductsApiSettingsName
    ];

    /// <summary>
    /// The list of custom migrations that need to be triggered manually by a user.
    /// </summary>
    private static readonly List<string> ManualCustomMigrations =
    [
        UpdateFileSecuritySettingsName
    ];

    /// <summary>
    /// The list of custom migration definitions.
    /// </summary>
    private static readonly List<WiserTableDefinitionModel> CustomMigrationDefinitions =
    [
        new() {Name = TriggersName, LastUpdate = new DateTime(2025, 6, 4)},
        new() {Name = RemoveVirtualColumnsName, LastUpdate = new DateTime(2024, 9, 12)},
        new() {Name = AddBranchSettingsModuleName, LastUpdate = new DateTime(2024, 11, 18)},
        new() {Name = UpdateFileSecuritySettingsName, LastUpdate = new DateTime(2025, 1, 12)},
        new() {Name = AddProductsApiSettingsName, LastUpdate = new DateTime(2025, 4, 15)},
    ];

    /// <inheritdoc />
    public async Task DoAutomaticDatabaseMigrationsForTenantAsync(ClaimsIdentity identity)
    {
        await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
        clientDatabaseConnection.SetCommandTimeout(600);

        // Get information that we need for (some of) the migrations.
        var migrationsList = await databaseHelpersService.GetMigrationsStatusAsync(clientDatabaseConnection.ConnectedDatabase);
        var linkTypes = await linkTypesService.GetAllLinkTypeSettingsAsync() ?? [];
        var dedicatedLinkTablePrefixes = linkTypes.Select(linkTypesService.GetTablePrefixForLink).Distinct().ToList();
        var dedicatedEntityTablePrefixes = await gclEntityTypesService.GetDedicatedTablePrefixesAsync() ?? [];

        if (!dedicatedLinkTablePrefixes.Contains(String.Empty))
        {
            dedicatedLinkTablePrefixes.Add(String.Empty);
        }
        if (!dedicatedEntityTablePrefixes.Contains(String.Empty))
        {
            dedicatedEntityTablePrefixes.Add(String.Empty);
        }

        // If the table changes don't contain wiser_itemdetail, it means this is an older database.
        // We can assume that this database already has the table, otherwise nothing would work.
        // We add that table to the list of last table updates, so that the GCL doesn't try to add any indexes that might be missing,
        // because that takes a very long time for old databases with lots of data.
        if (!migrationsList.ContainsKey(WiserTableNames.WiserItemDetail))
        {
            await clientDatabaseConnection.ExecuteAsync($"INSERT IGNORE INTO {WiserTableNames.WiserTableChanges} (name, last_update) VALUES ('{WiserTableNames.WiserItemDetail}', '{DateTime.Now:yyyy-MM-dd HH:mm:ss}')");
            migrationsList.TryAdd(WiserTableNames.WiserItemDetail, DateTime.Now);
        }

        // Same for wiser_itemlink.
        if (!migrationsList.ContainsKey(WiserTableNames.WiserItemLink))
        {
            await clientDatabaseConnection.ExecuteAsync($"INSERT IGNORE INTO {WiserTableNames.WiserTableChanges} (name, last_update) VALUES ('{WiserTableNames.WiserItemLink}', '{DateTime.Now:yyyy-MM-dd HH:mm:ss}')");
            migrationsList.TryAdd(WiserTableNames.WiserItemLink, DateTime.Now);
        }

        // These tables need to be updated first, because others depend on them.
        await databaseHelpersService.CheckAndUpdateTablesAsync(FirstPriorityTables);

        // Do the rest of the tables.
        await databaseHelpersService.CheckAndUpdateTablesAsync(SecondPriorityTables);

        // Execute custom migrations.
        foreach (var migration in AutomaticCustomMigrations)
        {
            switch (migration)
            {
                case TriggersName:
                    await UpdateTriggersAsync(migrationsList, dedicatedEntityTablePrefixes, dedicatedLinkTablePrefixes);
                    break;
                case RemoveVirtualColumnsName:
                    await RemoveVirtualColumnsAsync(migrationsList, dedicatedEntityTablePrefixes, dedicatedLinkTablePrefixes);
                    break;
                case AddBranchSettingsModuleName:
                    await AddBranchSettingsModuleAsync(migrationsList);
                    break;
                case AddPaymentMethodFilterProperties:
                    await AddPaymentMethodFilterPropertiesAsync(migrationsList);
                    break;
                case AddProductsApiSettingsName:
                    await AddProductsApiSettingsAsync(migrationsList);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(migration), migration, "Unknown migration name.");
            }
        }

        // Delete the cache, to make sure that the migrations list gets updated correctlt.
        cache.Remove($"CachedDatabaseHelpersService_GetLastTableUpdates_{clientDatabaseConnection.ConnectedDatabase}_");
    }

    /// <inheritdoc />
    public async Task DoManualDatabaseMigrationsForTenantAsync(ClaimsIdentity identity, List<string> migrationNames)
    {
        await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
        clientDatabaseConnection.SetCommandTimeout(600);

        // Get information that we need for (some of) the migrations.
        var migrationsList = await databaseHelpersService.GetMigrationsStatusAsync(clientDatabaseConnection.ConnectedDatabase);
        var linkTypes = await linkTypesService.GetAllLinkTypeSettingsAsync() ?? [];
        var entityTypes = (await entityTypesService.GetAsync(identity, false))?.ModelObject ?? [];

        foreach (var migration in migrationNames)
        {
            if (!ManualCustomMigrations.Contains(migration))
            {
                throw new ArgumentException($"Unknown migration name: {migration}");
            }

            switch (migration)
            {
                case UpdateFileSecuritySettingsName:
                    await UpdateFileSecuritySettingsAsync(migrationsList, linkTypes, entityTypes);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(migration), migration, "Unknown migration name.");
            }
        }

        // Delete the cache, to make sure that the migrations list gets updated correctlt.
        cache.Remove($"CachedDatabaseHelpersService_GetLastTableUpdates_{clientDatabaseConnection.ConnectedDatabase}_");
    }

    /// <inheritdoc />
    public async Task<ServiceResult<List<DatabaseMigrationInformationModel>>> GetMigrationsAsync(ClaimsIdentity identity, bool manualMigrationsOnly, bool includeAlreadyExecutedMigrations)
    {
        await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();

        // Get the migration status from the tenant database.
        var migrationsList = await databaseHelpersService.GetMigrationsStatusAsync(clientDatabaseConnection.ConnectedDatabase);

        // Create lists with all migrations that can be done.
        var allManualMigrations = ManualCustomMigrations;
        List<string> allAutomaticMigrations =
        [
            ..AutomaticCustomMigrations,
            ..FirstPriorityTables,
            ..SecondPriorityTables
        ];

        List<string> allMigrations = [..allManualMigrations];
        if (!manualMigrationsOnly)
        {
            allMigrations.AddRange(allAutomaticMigrations);
        }

        // Generate the list to return.
        var migrations = new List<DatabaseMigrationInformationModel>();
        foreach (var migration in allMigrations)
        {
            // A migration can be a custom migration or a table update, so check in both lists.
            var migrationDefinition = WiserTableDefinitions.TablesToUpdate.SingleOrDefault(table => table.Name == migration) ?? CustomMigrationDefinitions.SingleOrDefault(definition => definition.Name == migration);
            if (migrationDefinition == null)
            {
                // Throw an exception if the migration is not found, because that means the developer forgot to add it to the list.
                throw new InvalidOperationException($"Migration definition not found for migration: {migration}");
            }

            // Skip migrations that were not requested.
            if (migrationsList.TryGetValue(migration, out var lastUpdate) && lastUpdate >= migrationDefinition.LastUpdate && !includeAlreadyExecutedMigrations)
            {
                continue;
            }

            // Add the migration to the list and create a display name and description for it.
            migrations.Add(new DatabaseMigrationInformationModel
            {
                Id = migration,
                Name = migration switch
                {
                    TriggersName => "Update triggers for all Wiser tables",
                    RemoveVirtualColumnsName => "Remove virtual columns on all wiser item detail tables",
                    AddBranchSettingsModuleName => "Add the branch settings module",
                    UpdateFileSecuritySettingsName => "Update file security settings",
                    AddProductsApiSettingsName => "Add the settings for the products api",
                    _ when FirstPriorityTables.Contains(migration) || SecondPriorityTables.Contains(migration) => $"Table: {migrationDefinition.Name}",
                    _ => throw new ArgumentOutOfRangeException(nameof(migration), migration, "Unknown migration name.")
                },
                Description = migration switch
                {
                    TriggersName => "Update the triggers for all Wiser tables. We use these triggers to keep track of changes in data and to update related data in other tables.",
                    RemoveVirtualColumnsName => "Remove virtual columns on all wiser item detail tables. These columns were an experiment that did not work out (caused too many problems), so we want to delete them again.",
                    AddBranchSettingsModuleName => "Add the branch settings module. This is a new module that all tenants should have. This module is for configuring the default settings of branches and automatic merges of those branches.",
                    UpdateFileSecuritySettingsName => "Update file security settings. This migration will update the security settings for wiser item files. They used to be disabled by default, this will enable them by default. This is not fully backwards compatible, so this needs to be coordinated and tested properly before executing this migration.",
                    AddProductsApiSettingsName => "Add the settings for the products api. This is a new module that all tenants should have. This module is for configuring the default settings of the products api.",
                    _ when FirstPriorityTables.Contains(migration) || SecondPriorityTables.Contains(migration) => $"Update the table definition of '{migrationDefinition.Name}' to add any new or updated columns or indexes.",
                    _ => throw new ArgumentOutOfRangeException(nameof(migration), migration, "Unknown migration name.")
                },
                IsCustomMigration = AutomaticCustomMigrations.Contains(migration) || ManualCustomMigrations.Contains(migration),
                LastRunOn = lastUpdate,
                LastUpdateOn = migrationDefinition.LastUpdate,
                RequiresManualTrigger = allManualMigrations.Contains(migration)
            });
        }

        return new ServiceResult<List<DatabaseMigrationInformationModel>>(migrations);
    }

    /// <inheritdoc />
    public async Task DoDatabaseMigrationsForMainDatabaseAsync()
    {
        using var scope = serviceProvider.CreateScope();

        // Get the MySqlDatabaseConnection instead of ClientDatabaseConnection, because we want to create these tables in the main tenant.
        var originalConnection = scope.ServiceProvider.GetRequiredService<MySqlDatabaseConnection>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<MySqlDatabaseHelpersService>>();
        var mainDatabaseHelpersService = new MySqlDatabaseHelpersService(originalConnection, logger);

        var tablesToUpdate = new List<string>
        {
            ApiTableNames.ApiRequestLogs
        };

        // Copy the original GCL log table, we want the same columns for our log table, with some extras.
        var logTable = WiserTableDefinitions.TablesToUpdate.Single(table => table.Name == WiserTableNames.GclRequestLog);
        logTable.Name = ApiTableNames.ApiRequestLogs;
        logTable.Indexes.ForEach(index => index.TableName = ApiTableNames.ApiRequestLogs);
        logTable.Columns.Add(new ColumnSettingsModel("sub_domain", MySqlDbType.VarChar, 255, notNull: true, defaultValue: ""));
        logTable.Columns.Add(new ColumnSettingsModel("is_from_wiser_front_end", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0"));
        logTable.Indexes.Add(new IndexSettingsModel(ApiTableNames.ApiRequestLogs, "idx_sub_domain", IndexTypes.Normal, ["sub_domain", "is_from_wiser_front_end"]));
        mainDatabaseHelpersService.ExtraWiserTableDefinitions = [logTable];

        await mainDatabaseHelpersService.CheckAndUpdateTablesAsync(tablesToUpdate);

        // Delete the cache, to make sure that the migrations list gets updated correctlt.
        cache.Remove($"CachedDatabaseHelpersService_GetLastTableUpdates_{originalConnection.ConnectedDatabase}_");
    }

    /// <summary>
    /// Remove virtual columns from Wiser tables, if they stil exist.
    /// This was an experiment in the past, but we decided not to use them due to some problems with them.
    /// </summary>
    /// <param name="migrationsList">The list of all migrations that have been done on the database and the dates and times when they have been done.</param>
    /// <param name="dedicatedEntityTablePrefixes">The list with all unique table prefixes of entity types in the current tenant.</param>
    /// <param name="dedicatedLinkTablePrefixes">The list of all unique table prefixes of link types in the current tenant.</param>
    private async Task RemoveVirtualColumnsAsync(Dictionary<string, DateTime> migrationsList, List<string> dedicatedEntityTablePrefixes, List<string> dedicatedLinkTablePrefixes)
    {
        if (migrationsList.TryGetValue(RemoveVirtualColumnsName, out var value) && value >= CustomMigrationDefinitions.Single(definition => definition.Name == RemoveVirtualColumnsName).LastUpdate)
        {
            return;
        }

        // Remove virtual columns from wiser_itemdetail and wiser_itemdetail_archive tables.
        foreach (var tablePrefix in dedicatedEntityTablePrefixes)
        {
            var tableName = $"{tablePrefix}{WiserTableNames.WiserItemDetail}";
            if (await databaseHelpersService.TableExistsAsync(tableName))
            {
                if (await databaseHelpersService.ColumnExistsAsync(tableName, "value_as_int"))
                {
                    await databaseHelpersService.DropColumnAsync(tableName, "value_as_int");
                }

                if (await databaseHelpersService.ColumnExistsAsync(tableName, "value_as_decimal"))
                {
                    await databaseHelpersService.DropColumnAsync(tableName, "value_as_decimal");
                }
            }

            var archiveTableName = $"{tablePrefix}{WiserTableNames.WiserItemDetail}{WiserTableNames.ArchiveSuffix}";
            if (!await databaseHelpersService.TableExistsAsync(archiveTableName))
            {
                continue;
            }

            if (await databaseHelpersService.ColumnExistsAsync(archiveTableName, "value_as_int"))
            {
                await databaseHelpersService.DropColumnAsync(archiveTableName, "value_as_int");
            }

            if (await databaseHelpersService.ColumnExistsAsync(archiveTableName, "value_as_decimal"))
            {
                await databaseHelpersService.DropColumnAsync(archiveTableName, "value_as_decimal");
            }
        }

        // Remove virtual columns from wiser_itemlinkdetail and wiser_itemlinkdetail_archive tables.
        foreach (var tablePrefix in dedicatedLinkTablePrefixes)
        {
            var tableName = $"{tablePrefix}{WiserTableNames.WiserItemLinkDetail}";
            if (await databaseHelpersService.TableExistsAsync(tableName))
            {
                if (await databaseHelpersService.ColumnExistsAsync(tableName, "value_as_int"))
                {
                    await databaseHelpersService.DropColumnAsync(tableName, "value_as_int");
                }

                if (await databaseHelpersService.ColumnExistsAsync(tableName, "value_as_decimal"))
                {
                    await databaseHelpersService.DropColumnAsync(tableName, "value_as_decimal");
                }
            }

            var archiveTableName = $"{tablePrefix}{WiserTableNames.WiserItemLinkDetail}{WiserTableNames.ArchiveSuffix}";
            if (!await databaseHelpersService.TableExistsAsync(archiveTableName))
            {
                continue;
            }

            if (await databaseHelpersService.ColumnExistsAsync(archiveTableName, "value_as_int"))
            {
                await databaseHelpersService.DropColumnAsync(archiveTableName, "value_as_int");
            }

            if (await databaseHelpersService.ColumnExistsAsync(archiveTableName, "value_as_decimal"))
            {
                await databaseHelpersService.DropColumnAsync(archiveTableName, "value_as_decimal");
            }
        }

        // Update wiser_table_changes.
        clientDatabaseConnection.AddParameter("tableName", RemoveVirtualColumnsName);
        clientDatabaseConnection.AddParameter("lastUpdate", DateTime.Now);
        await clientDatabaseConnection.ExecuteAsync($"""
                                                     INSERT INTO {WiserTableNames.WiserTableChanges} (name, last_update) 
                                                     VALUES (?tableName, ?lastUpdate) 
                                                     ON DUPLICATE KEY UPDATE last_update = VALUES(last_update)
                                                     """);
    }

    /// <summary>
    /// Make sure that all triggers for Wiser tables are up-to-date.
    /// </summary>
    /// <param name="migrationsList">The list of all migrations that have been done on the database and the dates and times when they have been done.</param>
    /// <param name="dedicatedEntityTablePrefixes">The list with all unique table prefixes of entity types in the current tenant.</param>
    /// <param name="dedicatedLinkTablePrefixes">The list of all unique table prefixes of link types in the current tenant.</param>
    private async Task UpdateTriggersAsync(Dictionary<string, DateTime> migrationsList, List<string> dedicatedEntityTablePrefixes, List<string> dedicatedLinkTablePrefixes)
    {
        if (migrationsList.TryGetValue(TriggersName, out var value) && value >= CustomMigrationDefinitions.Single(definition => definition.Name == TriggersName).LastUpdate)
        {
            return;
        }

        // Normal table trigger.
        var createTriggersQuery = await ResourceHelpers.ReadTextResourceFromAssemblyAsync("Api.Core.Queries.WiserInstallation.CreateTriggers.sql");
        await clientDatabaseConnection.ExecuteAsync(createTriggersQuery);

        // Dedicated table trigger.
        var createDedicatedTriggersQuery = await ResourceHelpers.ReadTextResourceFromAssemblyAsync("Api.Core.Queries.WiserInstallation.CreateDedicatedItemTablesTriggers.sql");

        var queries = dedicatedEntityTablePrefixes
            .Where(tablePrefix => !String.IsNullOrWhiteSpace(tablePrefix))
            .Select(tablePrefix => createDedicatedTriggersQuery.Replace("{tablePrefix}", tablePrefix));
        foreach (var query in queries)
        {
            await clientDatabaseConnection.ExecuteAsync(query);
        }

        // Dedicated link table trigger.
        var createDedicatedLinkTriggersQuery = await ResourceHelpers.ReadTextResourceFromAssemblyAsync("Api.Core.Queries.WiserInstallation.CreateDedicatedLinkTableTriggers.sql");

        queries = dedicatedLinkTablePrefixes
            .Where(tablePrefix => !String.IsNullOrWhiteSpace(tablePrefix))
            .Select(tablePrefix => createDedicatedLinkTriggersQuery.Replace("{LinkType}", tablePrefix.Trim('_')));
        foreach (var query in queries)
        {
            await clientDatabaseConnection.ExecuteAsync(query);
        }

        // Update wiser_table_changes.
        clientDatabaseConnection.AddParameter("tableName", TriggersName);
        clientDatabaseConnection.AddParameter("lastUpdate", DateTime.Now);
        await clientDatabaseConnection.ExecuteAsync($"""
                                                     INSERT INTO {WiserTableNames.WiserTableChanges} (name, last_update) 
                                                     VALUES (?tableName, ?lastUpdate) 
                                                     ON DUPLICATE KEY UPDATE last_update = VALUES(last_update)
                                                     """);
    }

    /// <summary>
    /// Add the module for branch settings to tenants that don't have it yet.
    /// </summary>
    /// <param name="migrationsList">The list of all migrations that have been done on the database and the dates and times when they have been done.</param>
    private async Task AddBranchSettingsModuleAsync(Dictionary<string, DateTime> migrationsList)
    {
        if (migrationsList.TryGetValue(AddBranchSettingsModuleName, out var value) && value >= CustomMigrationDefinitions.Single(definition => definition.Name == AddBranchSettingsModuleName).LastUpdate)
        {
            return;
        }

        var addBranchSettingsModuleQuery = await ResourceHelpers.ReadTextResourceFromAssemblyAsync("Api.Core.Queries.WiserInstallation.BranchSettingsModule.sql");
        await clientDatabaseConnection.ExecuteAsync(addBranchSettingsModuleQuery);

        // Update wiser_table_changes.
        clientDatabaseConnection.AddParameter("tableName", AddBranchSettingsModuleName);
        clientDatabaseConnection.AddParameter("lastUpdate", DateTime.Now);
        await clientDatabaseConnection.ExecuteAsync($"""
                                                     INSERT INTO {WiserTableNames.WiserTableChanges} (name, last_update) 
                                                     VALUES (?tableName, ?lastUpdate) 
                                                     ON DUPLICATE KEY UPDATE last_update = VALUES(last_update)
                                                     """);
    }

    /// <summary>
    /// Add the settings for the products api.
    /// </summary>
    /// <param name="migrationsList">The list of all migrations that have been done on the database and the dates and times when they have been done.</param>
    private async Task AddProductsApiSettingsAsync(Dictionary<string, DateTime> migrationsList)
    {
        if (migrationsList.TryGetValue(AddProductsApiSettingsName, out var value) && value >= CustomMigrationDefinitions.Single(definition => definition.Name == AddProductsApiSettingsName).LastUpdate)
        {
            return;
        }

        var addProductsApiSettingsQuery = await ResourceHelpers.ReadTextResourceFromAssemblyAsync("Api.Core.Queries.WiserInstallation.ProductsApiSettings.sql");
        await clientDatabaseConnection.ExecuteAsync(addProductsApiSettingsQuery);

        // Update wiser_table_changes.
        clientDatabaseConnection.AddParameter("tableName", AddProductsApiSettingsName);
        clientDatabaseConnection.AddParameter("lastUpdate", DateTime.Now);
        await clientDatabaseConnection.ExecuteAsync($"""
                                                     INSERT INTO {WiserTableNames.WiserTableChanges} (name, last_update) 
                                                     VALUES (?tableName, ?lastUpdate) 
                                                     ON DUPLICATE KEY UPDATE last_update = VALUES(last_update)
                                                     """);
    }


    /// <summary>
    /// Add properties used for filters
    /// </summary>
    /// <param name="migrationsList">The list of all migrations that have been done on the database and the dates and times when they have been done.</param>
    private async Task AddPaymentMethodFilterPropertiesAsync(Dictionary<string, DateTime> migrationsList)
    {
        if (migrationsList.TryGetValue(AddPaymentMethodFilterProperties, out var value) && value >= new DateTime(2025, 4, 2))
        {
            return;
        }

        // Check whether there is a WiserPaymentMethod to update
        var paymentMethodEntityTable = await clientDatabaseConnection.GetAsync($"""
                                                                 SELECT id 
                                                                 FROM {WiserTableNames.WiserEntity}
                                                                 WHERE `name` = 'WiserPaymentMethod';
                                                                 """);
        if (paymentMethodEntityTable.Rows.Count > 0)
        {
            await clientDatabaseConnection.ExecuteAsync($"""
                 INSERT IGNORE INTO {WiserTableNames.WiserEntityProperty} (`module_id`, `entity_name`, `link_type`, `tab_name`, `group_name`, `inputtype`, `display_name`, `property_name`, `explanation`, `ordering`, `regex_validation`, `mandatory`, `readonly`, `default_value`, `css`, `width`, `height`, `options`, `data_query`, `action_query`, `search_query`, `search_count_query`, `grid_delete_query`, `grid_insert_query`, `grid_update_query`, `depends_on_field`, `depends_on_operator`, `depends_on_value`, `language_code`, `custom_script`, `also_save_seo_value`, `depends_on_action`, `save_on_change`, `extended_explanation`, `label_style`, `label_width`, `enable_aggregation`, `aggregate_options`, `access_key`, `visibility_path_regex`)VALUES (0,  'WiserPaymentMethod', 0,  '', '', 'input', 'Taalcodes', 'paymentmethodlanguagecodes', 'Puntkomma (;) gescheiden taalcodes waarmee bepaald kan worden wanneer de betaalmethode zichtbaar is. Als dit leeg is dan is het zichtbaar bij elke taalcode.', 11, '', 0, 0, '', NULL, 0, 0, '', '', '', '', '', '', '', '', '', NULL, '', '', '', 0, NULL, 0, 0, 'normal', '0', 0, '', '', '');
                 INSERT IGNORE INTO {WiserTableNames.WiserEntityProperty} (`module_id`, `entity_name`, `link_type`, `tab_name`, `group_name`, `inputtype`, `display_name`, `property_name`, `explanation`, `ordering`, `regex_validation`, `mandatory`, `readonly`, `default_value`, `css`, `width`, `height`, `options`, `data_query`, `action_query`, `search_query`, `search_count_query`, `grid_delete_query`, `grid_insert_query`, `grid_update_query`, `depends_on_field`, `depends_on_operator`, `depends_on_value`, `language_code`, `custom_script`, `also_save_seo_value`, `depends_on_action`, `save_on_change`, `extended_explanation`, `label_style`, `label_width`, `enable_aggregation`, `aggregate_options`, `access_key`, `visibility_path_regex`) VALUES (0, 'WiserPaymentMethod', 0, '', '', 'input', 'Url Regex', 'paymentmethodurlregex', 'Regex waarmee bepaald kan worden of de betaalmethode op het url moet worden getoond. Dit gebruikt het seo pad en de query parameters. Als het leeg is dan is het zichtbaar op elk URL', 10, '', 0, 0, '', NULL, 0, 0, '', '', '', '', '', '', '', '', '', NULL, '', '', '', 0, NULL, 0, 0, 'normal', '0', 0, '', '', '');
                 """);
        }

        // Update wiser_table_changes.
        clientDatabaseConnection.AddParameter("tableName", AddPaymentMethodFilterProperties);
        clientDatabaseConnection.AddParameter("lastUpdate", DateTime.Now);
        await clientDatabaseConnection.ExecuteAsync($"""
                                                     INSERT INTO {WiserTableNames.WiserTableChanges} (name, last_update) 
                                                     VALUES (?tableName, ?lastUpdate) 
                                                     ON DUPLICATE KEY UPDATE last_update = VALUES(last_update)
                                                     """);
    }

    /// <summary>
    /// Update security settings for wiser item files. They used to be disabled by default, this will enable them by default.
    /// </summary>
    /// <param name="migrationsList">The list of all migrations that have been done on the database and the dates and times when they have been done.</param>
    /// <param name="entityTypes">The list with all entity types in the current tenant.</param>
    /// <param name="linkTypes">The list of all link types in the current tenant.</param>
    private async Task UpdateFileSecuritySettingsAsync(Dictionary<string, DateTime> migrationsList, List<LinkSettingsModel> linkTypes, List<EntityTypeModel> entityTypes)
    {
        if (migrationsList.TryGetValue(UpdateFileSecuritySettingsName, out var value) && value >= CustomMigrationDefinitions.Single(definition => definition.Name == UpdateFileSecuritySettingsName).LastUpdate)
        {
            return;
        }

        var tablesToUpdate = new List<string> {WiserTableNames.WiserItemFile, $"{WiserTableNames.WiserItemFile}{WiserTableNames.ArchiveSuffix}"};

        // Add all item file tables that use dedicated tables for link types.
        tablesToUpdate.AddRange(linkTypes.Where(linkType => linkType.UseDedicatedTable).Select(linkType => $"{linkTypesService.GetTablePrefixForLink(linkType)}{WiserTableNames.WiserItemFile}"));
        tablesToUpdate.AddRange(linkTypes.Where(linkType => linkType.UseDedicatedTable).Select(linkType => $"{linkTypesService.GetTablePrefixForLink(linkType)}{WiserTableNames.WiserItemFile}{WiserTableNames.ArchiveSuffix}"));

        // Add all item file tables that use dedicated tables for entity types.
        tablesToUpdate.AddRange(entityTypes.Where(entityType => !String.IsNullOrWhiteSpace(entityType.DedicatedTablePrefix)).Select(entityType => $"{entityType.DedicatedTablePrefix}{WiserTableNames.WiserItemFile}"));
        tablesToUpdate.AddRange(entityTypes.Where(entityType => !String.IsNullOrWhiteSpace(entityType.DedicatedTablePrefix)).Select(entityType => $"{entityType.DedicatedTablePrefix}{WiserTableNames.WiserItemFile}{WiserTableNames.ArchiveSuffix}"));

        // Update the protected column to have a default value of 1.
        var updateDefaultProtectionValueQuery = String.Join(Environment.NewLine, tablesToUpdate.Select(table => $"ALTER TABLE `{table}` MODIFY COLUMN `protected` tinyint NOT NULL DEFAULT 1;"));
        await clientDatabaseConnection.ExecuteAsync(updateDefaultProtectionValueQuery);

        const string getUnprotectedFilesQuery = $$"""
                                                   SELECT `id`
                                                   FROM `{0}`
                                                   WHERE `protected` = 0 
                                                   AND `content_type` NOT LIKE 'image/%'
                                                   AND `content_type` NOT LIKE 'video/%'
                                                   AND `content_type` NOT LIKE 'application/font-%'
                                                   AND `content_type` NOT LIKE 'font/%'
                                                   AND `content_type` NOT IN ('{{MediaTypeNames.Text.Html}}', 'application/vnd.ms-fontobject')
                                                   LIMIT 200
                                                   """;

        const string enableFileProtectionQuery = """
                                                UPDATE `{0}`
                                                SET `protected` = 1
                                                WHERE `id` IN ({1})
                                                """;

        foreach (var tableName in tablesToUpdate)
        {
            var dataTable = await clientDatabaseConnection.GetAsync(String.Format(getUnprotectedFilesQuery, tableName));
            var ids = dataTable.Rows.Cast<DataRow>().Select(row => Convert.ToUInt64(row["id"])).ToList();

            // Update items in batches of 200 to not overtax the database (which happened during tests) and to not run into timeouts.
            while (ids.Count > 0)
            {
                // Update the current 200 files.
                await clientDatabaseConnection.ExecuteAsync(String.Format(enableFileProtectionQuery, tableName, String.Join(",", ids)));

                // Get the next 200 file IDs.
                dataTable = await clientDatabaseConnection.GetAsync(String.Format(getUnprotectedFilesQuery, tableName));
                ids = dataTable.Rows.Cast<DataRow>().Select(row => Convert.ToUInt64(row["id"])).ToList();

                // Wait a little bit to give the database some time to process our changes.
                await Task.Delay(100);
            }
        }

        // Update wiser_table_changes.
        clientDatabaseConnection.AddParameter("tableName", UpdateFileSecuritySettingsName);
        clientDatabaseConnection.AddParameter("lastUpdate", DateTime.Now);
        await clientDatabaseConnection.ExecuteAsync($"""
                                                     INSERT INTO {WiserTableNames.WiserTableChanges} (name, last_update) 
                                                     VALUES (?tableName, ?lastUpdate) 
                                                     ON DUPLICATE KEY UPDATE last_update = VALUES(last_update)
                                                     """);
    }
}
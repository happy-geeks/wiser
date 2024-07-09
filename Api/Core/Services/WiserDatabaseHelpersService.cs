using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Api.Core.Helpers;
using Api.Core.Interfaces;
using Api.Core.Models;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Enums;
using GeeksCoreLibrary.Modules.Databases.Helpers;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Databases.Models;
using GeeksCoreLibrary.Modules.Databases.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace Api.Core.Services;

/// <inheritdoc cref="IWiserDatabaseHelpersService" />
public class WiserDatabaseHelpersService : IWiserDatabaseHelpersService, IScopedService
{
    private readonly IDatabaseConnection clientDatabaseConnection;
    private readonly IDatabaseHelpersService databaseHelpersService;
    private readonly IServiceProvider serviceProvider;

    /// <summary>
    /// Creates a new instance of <see cref="WiserDatabaseHelpersService"/>.
    /// </summary>
    public WiserDatabaseHelpersService(IDatabaseConnection clientDatabaseConnection, IDatabaseHelpersService databaseHelpersService, IServiceProvider serviceProvider)
    {
        this.clientDatabaseConnection = clientDatabaseConnection;
        this.databaseHelpersService = databaseHelpersService;
        this.serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public async Task DoDatabaseMigrationsForTenantAsync()
    {
        await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();

        var lastTableUpdates = await databaseHelpersService.GetLastTableUpdatesAsync(clientDatabaseConnection.ConnectedDatabase);

        // If the table changes don't contain wiser_itemdetail, it means this is an older database.
        // We can assume that this database already has the able, otherwise nothing would work.
        // We add that table to the list of last table updates, so that the GCL doesn't try to add any indexes that might be missing,
        // because that takes a very long time for old databases with lots of data.
        if (!lastTableUpdates.ContainsKey(WiserTableNames.WiserItemDetail))
        {
            await clientDatabaseConnection.ExecuteAsync($"INSERT IGNORE INTO {WiserTableNames.WiserTableChanges} (name, last_update) VALUES ('{WiserTableNames.WiserItemDetail}', '{DateTime.Now:yyyy-MM-dd HH:mm:ss}')");
            lastTableUpdates.TryAdd(WiserTableNames.WiserItemDetail, DateTime.Now);
        }

        // Make sure that Wiser tables are up-to-date.
        const string TriggersName = "wiser_triggers";
        await databaseHelpersService.CheckAndUpdateTablesAsync(new List<string>
        {
            WiserTableNames.WiserEntity,
            WiserTableNames.WiserEntityProperty,
            WiserTableNames.WiserLink
        });
        await databaseHelpersService.CheckAndUpdateTablesAsync(new List<string>
        {
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
            WiserTableNames.WiserCommunication,
            WiserTableNames.WiserStyledOutput,
            WiserTableNames.WiserParentUpdates,
            GeeksCoreLibrary.Modules.Databases.Models.Constants.DatabaseConnectionLogTableName
        });

        // Make sure that all triggers for Wiser tables are up-to-date.
        if (!lastTableUpdates.ContainsKey(TriggersName) || lastTableUpdates[TriggersName] < new DateTime(2024, 6, 10))
        {
            var createTriggersQuery = await ResourceHelpers.ReadTextResourceFromAssemblyAsync("Api.Core.Queries.WiserInstallation.CreateTriggers.sql");
            await clientDatabaseConnection.ExecuteAsync(createTriggersQuery);

            // Update wiser_table_changes.
            clientDatabaseConnection.AddParameter("tableName", TriggersName);
            clientDatabaseConnection.AddParameter("lastUpdate", DateTime.Now);
            await clientDatabaseConnection.ExecuteAsync($@"INSERT INTO {WiserTableNames.WiserTableChanges} (name, last_update) 
VALUES (?tableName, ?lastUpdate) 
ON DUPLICATE KEY UPDATE last_update = VALUES(last_update)");
            clientDatabaseConnection.ClearParameters();
        }
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
        logTable.Indexes.Add(new IndexSettingsModel(ApiTableNames.ApiRequestLogs, "idx_sub_domain", IndexTypes.Normal, new List<string> { "sub_domain", "is_from_wiser_front_end" }));
        mainDatabaseHelpersService.ExtraWiserTableDefinitions = new List<WiserTableDefinitionModel> { logTable };

        await mainDatabaseHelpersService.CheckAndUpdateTablesAsync(tablesToUpdate);
    }
}
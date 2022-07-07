using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.Dashboard.Enums;
using Api.Modules.Dashboard.Interfaces;
using Api.Modules.Dashboard.Models;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;

namespace Api.Modules.Dashboard.Services;

/// <inheritdoc cref="IDashboardService" />
public class DashboardService : IDashboardService, IScopedService
{
    private readonly IDatabaseConnection clientDatabaseConnection;

    /// <summary>
    /// Creates a new instance of <see cref="DashboardService"/>.
    /// </summary>
    public DashboardService(IDatabaseConnection clientDatabaseConnection)
    {
        this.clientDatabaseConnection = clientDatabaseConnection;
    }

    /// <inheritdoc />
    public async Task<ServiceResult<DashboardDataModel>> GetDataAsync(ClaimsIdentity identity, DateTime? periodFrom = null, DateTime? periodTo = null, ItemsDataPeriodFilterTypes itemsDataPeriodFilterType = ItemsDataPeriodFilterTypes.All, int branchId = 0, bool forceRefresh = false)
    {
        var result = new DashboardDataModel();
        
        await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();

        if (false && !periodFrom.HasValue && !periodTo.HasValue)
        {
            // Use cached data.
        }
        else
        {
            // Use fresh data.
            result.EntityUsage = await GetEntityUsage(periodFrom, periodTo, itemsDataPeriodFilterType);
        }

        return new ServiceResult<DashboardDataModel>
        {
            ModelObject = result,
            StatusCode = HttpStatusCode.OK
        };
    }

    private async Task<List<EntityUsageModel>> GetEntityUsage(DateTime? periodFrom = null, DateTime? periodTo = null, ItemsDataPeriodFilterTypes itemsDataPeriodFilterType = ItemsDataPeriodFilterTypes.All)
    {
        // Get entity information first.
        var entities = new List<EntityUsageModel>();
        // Due to the table prefix functionality we're forced to retrieve this information one entity at a time.
        var tablePrefixInformation = await clientDatabaseConnection.GetAsync("SELECT DISTINCT dedicated_table_prefix FROM wiser_entity");
        foreach (var dataRow in tablePrefixInformation.Rows.Cast<DataRow>())
        {
            var prefix = dataRow.Field<string>("dedicated_table_prefix");
            if (!String.IsNullOrWhiteSpace(prefix) && !prefix.EndsWith("_"))
            {
                prefix += "_";
            }

            // Create the WHERE part of the query. This is for the period filter.
            var whereParts = new List<string>();
            if (itemsDataPeriodFilterType != ItemsDataPeriodFilterTypes.All && (periodFrom.HasValue || periodTo.HasValue))
            {
                var columnName = itemsDataPeriodFilterType switch
                {
                    ItemsDataPeriodFilterTypes.NewlyCreated => "added_on",
                    ItemsDataPeriodFilterTypes.Changed => "changed_on",
                    _ => ""
                };

                if (!String.IsNullOrWhiteSpace(columnName))
                {
                    if (periodFrom.HasValue)
                    {
                        whereParts.Add($"`{columnName}` >= '{periodFrom.Value:yyyy-MM-dd} 00:00:00'");
                    }
                    if (periodTo.HasValue)
                    {
                        whereParts.Add($"`{columnName}` <= '{periodTo.Value:yyyy-MM-dd} 23:59:59'");
                    }
                }
            }

            var wherePart = whereParts.Count > 0 ? $" WHERE {String.Join(" AND ", whereParts)}" : String.Empty;

            var usageData = await clientDatabaseConnection.GetAsync($"SELECT entity_type, COUNT(*) AS cnt FROM `{prefix}{WiserTableNames.WiserItem}`{wherePart} GROUP BY entity_type");
            entities.AddRange(usageData.Rows.Cast<DataRow>().Select(entityDataRow => new EntityUsageModel
            {
                Name = entityDataRow.Field<string>("entity_type"),
                AmountOfItems = Convert.ToInt32(entityDataRow["cnt"])
            }));

            // Also retrieved archived item count.
            var archiveUsageData = await clientDatabaseConnection.GetAsync($"SELECT entity_type, COUNT(*) AS cnt FROM `{prefix}{WiserTableNames.WiserItem}{WiserTableNames.ArchiveSuffix}`{wherePart} GROUP BY entity_type");
            foreach (var entityDataRow in archiveUsageData.Rows.Cast<DataRow>())
            {
                var name = entityDataRow.Field<string>("entity_type");
                var entity = entities.FirstOrDefault(e => e.Name.Equals(name));
                if (entity == null)
                {
                    continue;
                }

                entity.AmountOfArchivedItems = Convert.ToInt32(entityDataRow["cnt"]);
            }
        }

        // Only the top 8 should be used, so order by the amount of items descending, and take the last 8.
        return entities.OrderByDescending(e => e.AmountOfItems).Take(8).ToList();
    }

    private async Task EnsureTableAsync()
    {
        await clientDatabaseConnection.EnsureOpenConnectionForWritingAsync();
        await clientDatabaseConnection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS `wiser_dashboard_data` (
              `id` bigint NOT NULL AUTO_INCREMENT,
              `last_update` datetime NOT NULL,
              `entity_usage` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL COMMENT 'JSON object with entity usage',
              `user_login_count_top10` int NOT NULL DEFAULT 0,
              `user_login_count_other` int NOT NULL DEFAULT 0,
              `user_login_time_top10` time NOT NULL DEFAULT '00:00:00',
              `user_login_time_other` time NOT NULL DEFAULT '00:00:00',
              PRIMARY KEY (`id`) USING BTREE
            )");
    }

    private async Task RefreshTableData()
    {
        await clientDatabaseConnection.EnsureOpenConnectionForWritingAsync();
        await clientDatabaseConnection.GetAsync($@"");
    }
}
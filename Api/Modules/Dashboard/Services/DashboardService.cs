using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.Branches.Interfaces;
using Api.Modules.Dashboard.Enums;
using Api.Modules.Dashboard.Interfaces;
using Api.Modules.Dashboard.Models;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Enums;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Databases.Models;
using MySql.Data.MySqlClient;

namespace Api.Modules.Dashboard.Services;

/// <inheritdoc cref="IDashboardService" />
public class DashboardService : IDashboardService, IScopedService
{
    private readonly IDatabaseConnection clientDatabaseConnection;
    private readonly IDatabaseHelpersService databaseHelpersService;
    private readonly IBranchesService branchesService;

    /// <summary>
    /// Creates a new instance of <see cref="DashboardService"/>.
    /// </summary>
    public DashboardService(IDatabaseConnection clientDatabaseConnection, IDatabaseHelpersService databaseHelpersService, IBranchesService branchesService)
    {
        this.clientDatabaseConnection = clientDatabaseConnection;
        this.databaseHelpersService = databaseHelpersService;
        this.branchesService = branchesService;
    }

    /// <inheritdoc />
    public async Task<ServiceResult<DashboardDataModel>> GetDataAsync(ClaimsIdentity identity, DateTime? periodFrom = null, DateTime? periodTo = null, int branchId = 0, bool forceRefresh = false)
    {
        var result = new DashboardDataModel();

        var branchesData = await branchesService.GetAsync(identity);
        string databaseName = null;

        switch (branchId)
        {
            case -1:
            {
                // Get data from all branches.
                var sources = new List<DashboardDataModel>();
                foreach (var tempBranchId in branchesData.ModelObject.Select(b => b.Id))
                {
                    // Recursively call this function with the branch ID.
                    sources.Add((await GetDataAsync(identity, periodFrom, periodTo, tempBranchId, forceRefresh)).ModelObject);
                }

                result = CombineResults(sources);

                // Limit the item counts to the highest 8.
                foreach (var key in result.Items.Keys)
                {
                    result.Items[key] = result.Items[key].OrderByDescending(i => i.AmountOfItems).Take(8).ToList();
                }

                return new ServiceResult<DashboardDataModel>
                {
                    ModelObject = result,
                    StatusCode = HttpStatusCode.OK
                };
            }
            case > 0:
            {
                // If a branch ID is set, then try to get the database name for that branch so data can be retrieved from that branch.
                var branch = branchesData.ModelObject.SingleOrDefault(b => b.Id == branchId);
                if (branch != null)
                {
                    databaseName = branch.Database.DatabaseName;
                }

                break;
            }
        }

        await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();

        if (!periodFrom.HasValue && !periodTo.HasValue)
        {
            await EnsureTableAsync(databaseName);

            var queryDatabasePart = !String.IsNullOrWhiteSpace(databaseName) ? $"`{databaseName}`." : String.Empty;

            // Check if table data is new enough.
            var refreshData = forceRefresh;
            if (!forceRefresh)
            {
                // If a refresh isn't forced, check if the data exists or if it has expired.
                var status = await clientDatabaseConnection.GetAsync($"SELECT last_update FROM {queryDatabasePart}wiser_dashboard LIMIT 1");
                refreshData = status.Rows.Count == 0 || status.Rows[0].Field<DateTime>("last_update") < DateTime.Now.AddDays(-1);
            }

            if (refreshData)
            {
                await RefreshTableDataAsync(databaseName);
            }

            var wiserStatsData = await clientDatabaseConnection.GetAsync($"SELECT items_data, user_login_count_top10, user_login_count_other, user_login_time_top10, user_login_time_other FROM {queryDatabasePart}wiser_dashboard");
            if (wiserStatsData.Rows.Count == 0)
            {
                // This should not be possible, so throw an error here if this happens.
                throw new Exception("Table data is missing!");
            }

            // Data is stored as a JSON string.
            var rawItemsData = wiserStatsData.Rows[0].Field<string>("items_data");
            var itemsData = !String.IsNullOrWhiteSpace(rawItemsData)
                ? Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, List<ItemsCountModel>>>(rawItemsData)
                : new Dictionary<string, List<ItemsCountModel>>();

            result.Items = itemsData ?? new Dictionary<string, List<ItemsCountModel>>();
            result.UserLoginCountTop10 = wiserStatsData.Rows[0].Field<int>("user_login_count_top10");
            result.UserLoginCountOther = wiserStatsData.Rows[0].Field<int>("user_login_count_other");
            result.UserLoginTimeTop10 = wiserStatsData.Rows[0].Field<TimeSpan>("user_login_time_top10");
            result.UserLoginTimeOther = wiserStatsData.Rows[0].Field<TimeSpan>("user_login_time_other");
        }
        else
        {
            // Use fresh data.
            result.Items = await GetItemsCountAsync(periodFrom, periodTo, databaseName);
        }

        // Limit the item counts to the highest 8.
        foreach (var key in result.Items.Keys)
        {
            result.Items[key] = result.Items[key].OrderByDescending(i => i.AmountOfItems).Take(8).ToList();
        }

        return new ServiceResult<DashboardDataModel>
        {
            ModelObject = result,
            StatusCode = HttpStatusCode.OK
        };
    }

    /// <summary>
    /// Retrieves the item counts for the different filters. Optionally filtered on period, and optionally from a different database (for branches).
    /// </summary>
    /// <param name="periodFrom">The starting date when items were created or changed.</param>
    /// <param name="periodTo">The ending date when items were created or changed.</param>
    /// <param name="databaseName">The database name of a branch. Set to null or empty string to use current branch.</param>
    /// <returns></returns>
    private async Task<Dictionary<string, List<ItemsCountModel>>> GetItemsCountAsync(DateTime? periodFrom = null, DateTime? periodTo = null, string databaseName = null)
    {
        var result = new Dictionary<string, List<ItemsCountModel>>(3)
        {
            { ItemsDataPeriodFilterTypes.All.ToString("G"), await GetItemsCountWithFilterAsync(ItemsDataPeriodFilterTypes.All, periodFrom, periodTo, databaseName) },
            { ItemsDataPeriodFilterTypes.NewlyCreated.ToString("G"), await GetItemsCountWithFilterAsync(ItemsDataPeriodFilterTypes.NewlyCreated, periodFrom, periodTo, databaseName) },
            { ItemsDataPeriodFilterTypes.Changed.ToString("G"), await GetItemsCountWithFilterAsync(ItemsDataPeriodFilterTypes.Changed, periodFrom, periodTo, databaseName) }
        };

        return result;
    }

    /// <summary>
    /// Adds a list of item counts to a dictionary.
    /// </summary>
    /// <param name="result"></param>
    /// <param name="collectionName"></param>
    /// <param name="itemsCountList"></param>
    private static void AddItemCountsToResult(IReadOnlyDictionary<string, List<ItemsCountModel>> result, string collectionName, List<ItemsCountModel> itemsCountList)
    {
        foreach (var itemsCount in itemsCountList)
        {
            var currentItemsCount = result[collectionName].SingleOrDefault(i => i.Name == itemsCount.Name);
            if (currentItemsCount == null)
            {
                result[collectionName].Add(itemsCount);
            }
            else
            {
                currentItemsCount.AmountOfItems += itemsCount.AmountOfItems;
                currentItemsCount.AmountOfArchivedItems += itemsCount.AmountOfArchivedItems;
            }
        }
    }

    private async Task<List<ItemsCountModel>> GetItemsCountWithFilterAsync(ItemsDataPeriodFilterTypes itemsDataPeriodFilterType, DateTime? periodFrom = null, DateTime? periodTo = null, string databaseName = null)
    {
        var databasePart = !String.IsNullOrWhiteSpace(databaseName) ? $"`{databaseName}`." : String.Empty;

        // Get entity information first.
        var entities = new List<ItemsCountModel>();

        // Due to the table prefix functionality we're forced to retrieve this information one entity at a time.
        var tablePrefixInformation = await clientDatabaseConnection.GetAsync($"SELECT DISTINCT dedicated_table_prefix FROM {databasePart}`{WiserTableNames.WiserEntity}`");
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

            var itemsCountData = await clientDatabaseConnection.GetAsync($"SELECT entity_type, COUNT(*) AS cnt FROM {databasePart}`{prefix}{WiserTableNames.WiserItem}`{wherePart} GROUP BY entity_type");
            entities.AddRange(itemsCountData.Rows.Cast<DataRow>().Select(entityDataRow => new ItemsCountModel
            {
                Name = entityDataRow.Field<string>("entity_type"),
                AmountOfItems = Convert.ToInt32(entityDataRow["cnt"])
            }));

            // Also retrieved archived item count.
            var archiveUsageData = await clientDatabaseConnection.GetAsync($"SELECT entity_type, COUNT(*) AS cnt FROM {databasePart}`{prefix}{WiserTableNames.WiserItem}{WiserTableNames.ArchiveSuffix}`{wherePart} GROUP BY entity_type");
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

        return entities;
    }

    /// <summary>
    /// Takes multiple instances of <see cref="DashboardDataModel"/> objects, and combines the results into one.
    /// </summary>
    /// <param name="sources"></param>
    /// <returns></returns>
    private static DashboardDataModel CombineResults(IEnumerable<DashboardDataModel> sources)
    {
        var result = new DashboardDataModel
        {
            // Combine items data first.
            Items = new Dictionary<string, List<ItemsCountModel>>(3)
        };

        var collectionAll = ItemsDataPeriodFilterTypes.All.ToString("G");
        var collectionNewlyCreated = ItemsDataPeriodFilterTypes.NewlyCreated.ToString("G");
        var collectionChanged = ItemsDataPeriodFilterTypes.Changed.ToString("G");
        foreach (var source in sources)
        {
            AddItemCountsToResult(result.Items, collectionAll, source.Items[collectionAll]);
            AddItemCountsToResult(result.Items, collectionNewlyCreated, source.Items[collectionNewlyCreated]);
            AddItemCountsToResult(result.Items, collectionChanged, source.Items[collectionChanged]);

            result.UserLoginCountTop10 += source.UserLoginCountTop10;
            result.UserLoginCountOther += source.UserLoginCountOther;
            result.UserLoginTimeTop10 = result.UserLoginTimeTop10.Add(source.UserLoginTimeTop10);
            result.UserLoginTimeOther = result.UserLoginTimeOther.Add(source.UserLoginTimeOther);
        }

        return result;
    }

    /// <summary>
    /// Makes sure the wiser_dashboard table exists.
    /// TODO: Creation of the tables should be moved to the GCL. But this should only be done once the table definitions are final.
    /// </summary>
    private async Task EnsureTableAsync(string databaseName = null)
    {
        var tableChanges = await databaseHelpersService.GetLastTableUpdatesAsync(databaseName);

        var tableDefinition = new WiserTableDefinitionModel
        {
            Name = "wiser_dashboard", // TODO: Use WiserTableNames
            LastUpdate = new DateTime(2022, 7, 7),
            Columns = new List<ColumnSettingsModel>
            {
                new("id", MySqlDbType.Int32, notNull: true, isPrimaryKey: true, autoIncrement: true),
                new("last_update", MySqlDbType.DateTime, notNull: true),
                new("items_data", MySqlDbType.MediumText),
                new("user_login_count_top10", MySqlDbType.Int32, notNull: true, defaultValue: "0"),
                new("user_login_count_other", MySqlDbType.Int32, notNull: true, defaultValue: "0"),
                new("user_login_time_top10", MySqlDbType.Time, notNull: true, defaultValue: "00:00:00"),
                new("user_login_time_other", MySqlDbType.Time, notNull: true, defaultValue: "00:00:00")
            }
        };

        if (!tableChanges.ContainsKey(tableDefinition.Name.ToLowerInvariant()) || tableChanges[tableDefinition.Name.ToLowerInvariant()] < tableDefinition.LastUpdate)
        {
            await databaseHelpersService.CreateOrUpdateTableAsync(tableDefinition.Name, tableDefinition.Columns, databaseName: databaseName);
        }

        tableDefinition = new WiserTableDefinitionModel
        {
            Name = "wiser_login_log", // TODO: Use WiserTableNames
            LastUpdate = new DateTime(2022, 7, 7),
            Columns = new List<ColumnSettingsModel>
            {
                new("id", MySqlDbType.UInt64, notNull: true, isPrimaryKey: true, autoIncrement: true),
                new("user_id", MySqlDbType.UInt64, notNull: true),
                new("time_active", MySqlDbType.Time, notNull: true, defaultValue: "00:00:00"),
                new("added_on", MySqlDbType.DateTime, notNull: true)
            },
            Indexes = new List<IndexSettingsModel>
            {   // TODO: Use WiserTableNames
                new("wiser_login_log", "idx_added_on", IndexTypes.Normal, new List<string> { "added_on" }),
                new("wiser_login_log", "idx_user_Id", IndexTypes.Normal, new List<string> { "user_id" })
            }
        };

        if (!tableChanges.ContainsKey(tableDefinition.Name.ToLowerInvariant()) || tableChanges[tableDefinition.Name.ToLowerInvariant()] < tableDefinition.LastUpdate)
        {
            await databaseHelpersService.CreateOrUpdateTableAsync(tableDefinition.Name, tableDefinition.Columns, databaseName: databaseName);
            await databaseHelpersService.CreateOrUpdateIndexesAsync(tableDefinition.Indexes, databaseName);
        }
    }

    /// <summary>
    /// Refreshes the base Wiser statistics data. This is without a period, and will be remembered for a day.
    /// </summary>
    /// <param name="databaseName"></param>
    private async Task RefreshTableDataAsync(string databaseName = null)
    {
        var databasePart = !String.IsNullOrWhiteSpace(databaseName) ? $"`{databaseName}`." : String.Empty;

        // Retrieve items data.
        var itemsData = await GetItemsCountAsync(databaseName: databaseName);

        // Limit the item counts to the highest 8.
        foreach (var key in itemsData.Keys)
        {
            itemsData[key] = itemsData[key].OrderByDescending(i => i.AmountOfItems).Take(8).ToList();
        }

        // TODO: add user login count and user login times.

        clientDatabaseConnection.ClearParameters();
        clientDatabaseConnection.AddParameter("last_update", DateTime.Now);
        clientDatabaseConnection.AddParameter("items_data", Newtonsoft.Json.JsonConvert.SerializeObject(itemsData));
        await clientDatabaseConnection.ExecuteAsync($"INSERT INTO {databasePart}wiser_dashboard (last_update, items_data) VALUES (?last_update, ?items_data)");
    }
}
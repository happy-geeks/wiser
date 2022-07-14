using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
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

                // 0 is main branch. It's not included in the branchesData, so add it manually.
                var branchIds = new List<int> { 0 };
                branchIds.AddRange(branchesData.ModelObject.Select(b => b.Id));

                foreach (var tempBranchId in branchIds)
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

        if (!String.IsNullOrWhiteSpace(databaseName))
        {
            if (!await databaseHelpersService.DatabaseExistsAsync(databaseName))
            {
                return new ServiceResult<DashboardDataModel>
                {
                    ModelObject = null,
                    StatusCode = HttpStatusCode.NotFound
                };
            }
        }

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

            var userLoginTimeTop10 = wiserStatsData.Rows[0].Field<TimeSpan>("user_login_time_top10");
            var userLoginTimeOther = wiserStatsData.Rows[0].Field<TimeSpan>("user_login_time_other");

            result.Items = itemsData ?? new Dictionary<string, List<ItemsCountModel>>();
            result.UserLoginCountTop10 = wiserStatsData.Rows[0].Field<int>("user_login_count_top10");
            result.UserLoginCountOther = wiserStatsData.Rows[0].Field<int>("user_login_count_other");
            result.UserLoginTimeTop10 = (int) Math.Round(userLoginTimeTop10.TotalSeconds);
            result.UserLoginTimeOther = (int) Math.Round(userLoginTimeOther.TotalSeconds);
        }
        else
        {
            // Use fresh data.
            result.Items = await GetItemsCountAsync(periodFrom, periodTo, databaseName);

            var userData = await GetUserDataAsync(periodFrom, periodTo, databaseName);
            result.UserLoginCountTop10 = userData.UserLoginCountTop10;
            result.UserLoginCountOther = userData.UserLoginCountOther;
            result.UserLoginTimeTop10 = (int) Math.Round(userData.UserLoginTimeTop10.TotalSeconds);
            result.UserLoginTimeOther = (int) Math.Round(userData.UserLoginTimeOther.TotalSeconds);
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
    private static void AddItemCountsToResult(Dictionary<string, List<ItemsCountModel>> result, string collectionName, List<ItemsCountModel> itemsCountList)
    {
        if (!result.ContainsKey(collectionName))
        {
            result.Add(collectionName, new List<ItemsCountModel>());
        }

        foreach (var itemsCount in itemsCountList)
        {
            var currentItemsCount = result[collectionName].SingleOrDefault(i => i.EntityName == itemsCount.EntityName);
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

    /// <summary>
    /// Retrieves the amount of items, optionally filtered on a period.
    /// </summary>
    /// <param name="itemsDataPeriodFilterType">The filter type. Only works if <paramref name="periodFrom"/> and <paramref name="periodTo"/> are set.</param>
    /// <param name="periodFrom">From which moment the items should be counted.</param>
    /// <param name="periodTo">To which moment the items should be counted.</param>
    /// <param name="databaseName">The name of a branch database that should be looked in. Can be empty to use current branch.</param>
    /// <returns>A list of all items and the amount of those items that exist.</returns>
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
                        clientDatabaseConnection.AddParameter("periodFrom", periodFrom.Value);
                        whereParts.Add($"`{columnName}` >= ?periodFrom");
                    }

                    if (periodTo.HasValue)
                    {
                        clientDatabaseConnection.AddParameter("periodTo", periodTo.Value.AddDays(1).AddSeconds(-1));
                        whereParts.Add($"`{columnName}` <= ?periodTo");
                    }
                }
            }

            var wherePart = whereParts.Count > 0 ? $" WHERE {String.Join(" AND ", whereParts)}" : String.Empty;

            var itemsTableName = $"{prefix}{WiserTableNames.WiserItem}";
            if (await databaseHelpersService.TableExistsAsync(itemsTableName, databaseName))
            {
                var itemsCountData = await clientDatabaseConnection.GetAsync($"SELECT entity_type, COUNT(*) AS cnt FROM {databasePart}`{itemsTableName}`{wherePart} GROUP BY entity_type");
                entities.AddRange(itemsCountData.Rows.Cast<DataRow>().Select(entityDataRow => new ItemsCountModel
                {
                    EntityName = entityDataRow.Field<string>("entity_type"),
                    AmountOfItems = Convert.ToInt32(entityDataRow["cnt"])
                }));
            }

            // Also retrieved archived item count.
            var itemsArchiveTableName = $"{prefix}{WiserTableNames.WiserItem}{WiserTableNames.ArchiveSuffix}";
            if (await databaseHelpersService.TableExistsAsync(itemsArchiveTableName, databaseName))
            {
                // Only the entity types of the regular count need to be counted, so create a filter to be used in the WHERE part.
                var entityTypesForFilter = entities.Select(e => e.EntityName).ToArray();
                var entityTypeFilter = new StringBuilder();
                entityTypeFilter.Append("entity_type IN (");
                for (var i = 0; i < entityTypesForFilter.Length; i++)
                {
                    clientDatabaseConnection.AddParameter($"entity_type{i}", entityTypesForFilter[i]);
                    entityTypeFilter.Append($"?entity_type{i}");
                    if (i < entityTypesForFilter.Length - 1)
                    {
                        entityTypeFilter.Append(", ");
                    }
                }
                entityTypeFilter.Append(")");
                whereParts.Add(entityTypeFilter.ToString());
                
                // Update the wherePart string.
                wherePart = whereParts.Count > 0 ? $" WHERE {String.Join(" AND ", whereParts)}" : String.Empty;

                var archiveUsageData = await clientDatabaseConnection.GetAsync($"SELECT entity_type, COUNT(*) AS cnt FROM {databasePart}`{itemsArchiveTableName}`{wherePart} GROUP BY entity_type");
                foreach (var entityDataRow in archiveUsageData.Rows.Cast<DataRow>())
                {
                    var name = entityDataRow.Field<string>("entity_type");
                    var entity = entities.FirstOrDefault(e => e.EntityName.Equals(name));
                    if (entity == null)
                    {
                        continue;
                    }

                    entity.AmountOfArchivedItems = Convert.ToInt32(entityDataRow["cnt"]);
                }
            }
        }

        return entities;
    }

    private async Task<(int UserLoginCountTop10, int UserLoginCountOther, TimeSpan UserLoginTimeTop10, TimeSpan UserLoginTimeOther)> GetUserDataAsync(DateTime? periodFrom = null, DateTime? periodTo = null, string databaseName = null)
    {
        var databasePart = !String.IsNullOrWhiteSpace(databaseName) ? $"`{databaseName}`." : String.Empty;

        await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();

        // Create the WHERE part of the query. This is for the period filter.
        var whereParts = new List<string>();
        if (periodFrom.HasValue || periodTo.HasValue)
        {
            if (periodFrom.HasValue)
            {
                clientDatabaseConnection.AddParameter("periodFrom", periodFrom.Value);
                whereParts.Add($"`added_on` >= ?periodFrom");
            }

            if (periodTo.HasValue)
            {
                clientDatabaseConnection.AddParameter("periodTo", periodTo.Value.AddDays(1).AddSeconds(-1));
                whereParts.Add($"`added_on` <= ?periodTo");
            }
        }

        var wherePart = whereParts.Count > 0 ? $" WHERE {String.Join(" AND ", whereParts)}" : String.Empty;

        // Retrieve user login count data.
        var userLoginCountData = await clientDatabaseConnection.GetAsync($@"
            SELECT user_id, COUNT(*) AS login_count
            FROM {databasePart}wiser_login_log
            {wherePart}
            GROUP BY user_id
            ORDER BY login_count DESC");

        // Turn data rows into a list of counts.
        var loginCounts = userLoginCountData.Rows.Cast<DataRow>().Select(dr => Convert.ToInt32(dr["login_count"])).ToList();
        var loginCountTop10 = loginCounts.Take(10).Sum();
        var loginCountOther = loginCounts.Count > 10 ? loginCounts.Skip(10).Sum() : 0;

        // Retrieve user login time active data.
        var userLoginTimeData = await clientDatabaseConnection.GetAsync($@"
            SELECT user_id, SEC_TO_TIME(SUM(TIME_TO_SEC(time_active))) AS time_active
            FROM {databasePart}wiser_login_log
            {wherePart}
            GROUP BY user_id
            ORDER BY TIME_TO_SEC(time_active) DESC");

        // Initialize two TimeSpans.
        var timeActiveTop10 = TimeSpan.Zero;
        var timeActiveOther = TimeSpan.Zero;

        // Turn the data rows into a list of TimeSpans.
        var timeSpans = userLoginTimeData.Rows.Cast<DataRow>().Select(dr => dr.Field<TimeSpan>("time_active")).ToList();
        timeActiveTop10 = timeSpans.Take(10).Aggregate(timeActiveTop10, (current, timeSpan) => current.Add(timeSpan));
        timeActiveOther = timeSpans.Count > 10 ? timeSpans.Skip(10).Aggregate(timeActiveOther, (current, timeSpan) => current.Add(timeSpan)) : TimeSpan.Zero;

        return (loginCountTop10, loginCountOther, timeActiveTop10, timeActiveOther);
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
            // A source can be null if a branch database doesn't exist.
            if (source == null)
            {
                continue;
            }

            if (source.Items.ContainsKey(collectionAll))
            {
                AddItemCountsToResult(result.Items, collectionAll, source.Items[collectionAll]);
            }

            if (source.Items.ContainsKey(collectionNewlyCreated))
            {
                AddItemCountsToResult(result.Items, collectionNewlyCreated, source.Items[collectionNewlyCreated]);
            }

            if (source.Items.ContainsKey(collectionChanged))
            {
                AddItemCountsToResult(result.Items, collectionChanged, source.Items[collectionChanged]);
            }

            result.UserLoginCountTop10 += source.UserLoginCountTop10;
            result.UserLoginCountOther += source.UserLoginCountOther;
            result.UserLoginTimeTop10 = result.UserLoginTimeTop10 += source.UserLoginTimeTop10;
            result.UserLoginTimeOther = result.UserLoginTimeOther += source.UserLoginTimeOther;
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
                new("added_on", MySqlDbType.DateTime, notNull: true),
                new("time_active_changed_on", MySqlDbType.DateTime, notNull: true, comment: "The last time the time_active field was updated.")
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

        // Retrieve user data.
        var userData = await GetUserDataAsync(databaseName: databaseName);

        // Clear old data first.
        await clientDatabaseConnection.ExecuteAsync($"TRUNCATE TABLE {databasePart}wiser_dashboard");

        clientDatabaseConnection.ClearParameters();
        clientDatabaseConnection.AddParameter("last_update", DateTime.Now);
        clientDatabaseConnection.AddParameter("items_data", Newtonsoft.Json.JsonConvert.SerializeObject(itemsData));
        clientDatabaseConnection.AddParameter("user_login_count_top10", userData.UserLoginCountTop10);
        clientDatabaseConnection.AddParameter("user_login_count_other", userData.UserLoginCountOther);
        clientDatabaseConnection.AddParameter("user_login_time_top10", userData.UserLoginTimeTop10);
        clientDatabaseConnection.AddParameter("user_login_time_other", userData.UserLoginTimeOther);
        await clientDatabaseConnection.ExecuteAsync($"INSERT INTO {databasePart}wiser_dashboard (last_update, items_data, user_login_count_top10, user_login_count_other, user_login_time_top10, user_login_time_other) VALUES (?last_update, ?items_data, ?user_login_count_top10, ?user_login_count_other, ?user_login_time_top10, ?user_login_time_other)");
    }
}
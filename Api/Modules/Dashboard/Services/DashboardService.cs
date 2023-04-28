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
using Api.Modules.DataSelectors.Interfaces;
using Api.Modules.EntityTypes.Models;
using Api.Modules.TaskAlerts.Interfaces;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Databases.Models;
using GeeksCoreLibrary.Modules.WiserDashboard.Models;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;

namespace Api.Modules.Dashboard.Services;

/// <inheritdoc cref="IDashboardService" />
public class DashboardService : IDashboardService, IScopedService
{
    private readonly IDatabaseConnection clientDatabaseConnection;
    private readonly IDatabaseHelpersService databaseHelpersService;
    private readonly IBranchesService branchesService;
    private readonly ITaskAlertsService taskAlertsService;
    private readonly IDataSelectorsService dataSelectorsService;

    /// <summary>
    /// Creates a new instance of <see cref="DashboardService"/>.
    /// </summary>
    public DashboardService(IDatabaseConnection clientDatabaseConnection, IDatabaseHelpersService databaseHelpersService, IBranchesService branchesService, ITaskAlertsService taskAlertsService, IDataSelectorsService dataSelectorsService)
    {
        this.clientDatabaseConnection = clientDatabaseConnection;
        this.databaseHelpersService = databaseHelpersService;
        this.branchesService = branchesService;
        this.taskAlertsService = taskAlertsService;
        this.dataSelectorsService = dataSelectorsService;
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

                // Add open task alerts (not branch-specific, always current branch).
                result.OpenTaskAlerts = await GetOpenTaskAlertsAsync(identity);

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

        // Make sure the tables exist.
        await KeepTablesUpToDateAsync(databaseName);
        await CheckAndUpdateTablesAsync(databaseName);
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

        // If both the "periodFrom" parameter and "periodTo" parameter have no value, then the cached data should be used.
        if (!periodFrom.HasValue && !periodTo.HasValue)
        {
            var queryDatabasePart = !String.IsNullOrWhiteSpace(databaseName) ? $"`{databaseName}`." : String.Empty;

            // Check if table data is new enough.
            var refreshData = forceRefresh;
            if (!forceRefresh)
            {
                // If a refresh isn't forced, check if the data exists or if it has expired.
                var status = await clientDatabaseConnection.GetAsync($"SELECT last_update FROM {queryDatabasePart}`{WiserTableNames.WiserDashboard}` LIMIT 1");
                refreshData = status.Rows.Count == 0 || status.Rows[0].Field<DateTime>("last_update") < DateTime.Now.AddDays(-1);
            }

            if (refreshData)
            {
                await RefreshTableDataAsync(databaseName);
            }

            var wiserStatsData = await clientDatabaseConnection.GetAsync($"SELECT items_data, entities_data, user_login_count_top10, user_login_count_other, user_login_active_top10, user_login_active_other FROM {queryDatabasePart}`{WiserTableNames.WiserDashboard}`");
            if (wiserStatsData.Rows.Count == 0)
            {
                // This should not be possible, so throw an error here if this happens.
                throw new Exception("Table data is missing!");
            }

            // Data is stored as a JSON string.
            var rawItemsData = wiserStatsData.Rows[0].Field<string>("items_data");
            var itemsData = !String.IsNullOrWhiteSpace(rawItemsData)
                ? Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, List<ItemsCountModel>>>(rawItemsData)
                : new Dictionary<string, List<ItemsCountModel>>(0);

            var rawEntitiesData = wiserStatsData.Rows[0].Field<string>("entities_data");
            var entitiesData = !String.IsNullOrWhiteSpace(rawEntitiesData)
                ? Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, List<EntityTypeModel>>>(rawEntitiesData)
                : new Dictionary<string, List<EntityTypeModel>>(0);

            var userLoginActiveTop10 = wiserStatsData.Rows[0].Field<long>("user_login_active_top10");
            var userLoginActiveOther = wiserStatsData.Rows[0].Field<long>("user_login_active_other");

            result.Items = itemsData ?? new Dictionary<string, List<ItemsCountModel>>(0);
            result.Entities = entitiesData ?? new Dictionary<string, List<EntityTypeModel>>(0);
            result.UserLoginCountTop10 = wiserStatsData.Rows[0].Field<int>("user_login_count_top10");
            result.UserLoginCountOther = wiserStatsData.Rows[0].Field<int>("user_login_count_other");
            result.UserLoginActiveTop10 = userLoginActiveTop10;
            result.UserLoginActiveOther = userLoginActiveOther;
        }
        else
        {
            // Use fresh data.
            result.Items = await GetItemsCountAsync(periodFrom, periodTo, databaseName);
            result.Entities = await GetEntitiesDataAsync(databaseName);

            var userData = await GetUserDataAsync(periodFrom, periodTo, databaseName);
            result.UserLoginCountTop10 = userData.UserLoginCountTop10;
            result.UserLoginCountOther = userData.UserLoginCountOther;
            result.UserLoginActiveTop10 = userData.UserLoginActiveTop10;
            result.UserLoginActiveOther = userData.UserLoginActiveOther;
        }

        // Limit the item counts to the highest 8.
        foreach (var key in result.Items.Keys)
        {
            result.Items[key] = result.Items[key].OrderByDescending(i => i.AmountOfItems).Take(8).ToList();
        }

        // Add open task alerts (not branch-specific, always current branch).
        result.OpenTaskAlerts = await GetOpenTaskAlertsAsync(identity);

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
            { PeriodFilterTypes.All.ToString("G"), await GetItemsCountWithFilterAsync(PeriodFilterTypes.All, periodFrom, periodTo, databaseName) },
            { PeriodFilterTypes.NewlyCreated.ToString("G"), await GetItemsCountWithFilterAsync(PeriodFilterTypes.NewlyCreated, periodFrom, periodTo, databaseName) },
            { PeriodFilterTypes.Changed.ToString("G"), await GetItemsCountWithFilterAsync(PeriodFilterTypes.Changed, periodFrom, periodTo, databaseName) }
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
    /// <param name="periodFilterType">The filter type. Only works if <paramref name="periodFrom"/> and <paramref name="periodTo"/> are set.</param>
    /// <param name="periodFrom">The minimum age of the data.</param>
    /// <param name="periodTo">The maximum age of the data.</param>
    /// <param name="databaseName">The name of a branch database that should be used. Can be empty to use current branch.</param>
    /// <returns>A list of all items and the amount of those items that exist.</returns>
    private async Task<List<ItemsCountModel>> GetItemsCountWithFilterAsync(PeriodFilterTypes periodFilterType, DateTime? periodFrom = null, DateTime? periodTo = null, string databaseName = null)
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
            if (periodFilterType != PeriodFilterTypes.All && (periodFrom.HasValue || periodTo.HasValue))
            {
                var columnName = periodFilterType switch
                {
                    PeriodFilterTypes.NewlyCreated => "added_on",
                    PeriodFilterTypes.Changed => "changed_on",
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

    /// <summary>
    /// Retriever user login data. This is how many times users have logged in and how long they have stayed logged in.
    /// This is not per user, but rather a difference between the top 10 users and the remaining users.
    /// </summary>
    /// <param name="periodFrom">The minimum age of the data.</param>
    /// <param name="periodTo">The maximum age of the data.</param>
    /// <param name="databaseName">The name of a branch database that should be used. Can be empty to use current branch.</param>
    /// <returns>A <see cref="ValueTuple"/> containing the top 10 login counts and times, and the remaining login counts and times.</returns>
    private async Task<(int UserLoginCountTop10, int UserLoginCountOther, long UserLoginActiveTop10, long UserLoginActiveOther)> GetUserDataAsync(DateTime? periodFrom = null, DateTime? periodTo = null, string databaseName = null)
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
        var query = $@"SELECT user_id, COUNT(*) AS login_count
FROM {databasePart}{WiserTableNames.WiserLoginLog}
{wherePart}
GROUP BY user_id
ORDER BY login_count DESC";
        var userLoginCountData = await clientDatabaseConnection.GetAsync(query);

        // Turn data rows into a list of counts.
        var loginCounts = userLoginCountData.Rows.Cast<DataRow>().Select(dr => Convert.ToInt32(dr["login_count"])).ToList();
        var loginCountTop10 = loginCounts.Take(10).Sum();
        var loginCountOther = loginCounts.Count > 10 ? loginCounts.Skip(10).Sum() : 0;

        // Retrieve user login time active data.
        query = $@"SELECT user_id, SUM(time_active_in_seconds) AS time_active
FROM {databasePart}{WiserTableNames.WiserLoginLog}
{wherePart}
GROUP BY user_id
ORDER BY time_active DESC";
        var userLoginTimeData = await clientDatabaseConnection.GetAsync(query);

        // Turn the data rows into a list of TimeSpans.
        var timeSpans = userLoginTimeData.Rows.Cast<DataRow>().Select(dataRow => Convert.ToInt64(dataRow["time_active"])).ToList();
        var timeActiveTop10 = timeSpans.Take(10).Sum();
        var timeActiveOther = timeSpans.Count > 10 ? timeSpans.Skip(10).Sum() : 0;

        return (loginCountTop10, loginCountOther, timeActiveTop10, timeActiveOther);
    }

    /// <summary>
    /// Retrieves the data about the entities that should be shown in the dashboard.
    /// </summary>
    /// <param name="databaseName">The name of a branch database that should be used. Can be empty to use current branch.</param>
    /// <returns>A <see cref="List{T}"/> containing <see cref="EntityTypeModel"/> objects.</returns>
    private async Task<Dictionary<string, List<EntityTypeModel>>> GetEntitiesDataAsync(string databaseName = null)
    {
        var collectionAll = PeriodFilterTypes.All.ToString("G");
        var collectionNewlyCreated = PeriodFilterTypes.NewlyCreated.ToString("G");
        var result = new Dictionary<string, List<EntityTypeModel>>(2)
        {
            { collectionAll, new List<EntityTypeModel>(3) },
            { collectionNewlyCreated, new List<EntityTypeModel>(3) }
        };

        // First retrieve the entities that have "show_in_dashboard" set to 1. Limit it to 3.
        await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();

        // The database portion which will be placed in front of the table names of the FROM and JOIN statements.
        var queryDatabasePart = !String.IsNullOrWhiteSpace(databaseName) ? $"`{databaseName}`." : String.Empty;
        var entitiesData = await clientDatabaseConnection.GetAsync($@"
            SELECT entity.`name`, entity.dedicated_table_prefix, module.id AS module_id, module.icon
            FROM {queryDatabasePart}{WiserTableNames.WiserEntity} AS entity
            JOIN {queryDatabasePart}{WiserTableNames.WiserModule} AS module ON module.id = entity.module_id
            WHERE entity.show_in_dashboard = 1
            LIMIT 3");

        foreach (var dataRow in entitiesData.Rows.Cast<DataRow>())
        {
            var tablePrefix = dataRow.Field<string>("dedicated_table_prefix") ?? String.Empty;
            if (!String.IsNullOrWhiteSpace(tablePrefix) && !tablePrefix.EndsWith("_"))
            {
                tablePrefix += "_";
            }

            var entityName = dataRow.Field<string>("name");

            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("entity_type", entityName);

            // Retrieve total count of items with this entity type.
            var entityCountData = await clientDatabaseConnection.GetAsync($@"
                SELECT COUNT(*)
                FROM {queryDatabasePart}{tablePrefix}{WiserTableNames.WiserItem}
                WHERE entity_type = ?entity_type");

            var entityCount = Convert.ToInt32(entityCountData.Rows[0][0]);

            // Now retrieve the count for entities that are new. New is considered to be a month old at most.
            var newEntityCountData = await clientDatabaseConnection.GetAsync($@"
                SELECT COUNT(*)
                FROM {queryDatabasePart}{tablePrefix}{WiserTableNames.WiserItem}
                WHERE entity_type = ?entity_type AND added_on BETWEEN DATE_SUB(NOW(), INTERVAL 1 MONTH) AND NOW()");

            var newEntityCount = Convert.ToInt32(newEntityCountData.Rows[0][0]);

            result[collectionAll].Add(new EntityTypeModel
            {
                DisplayName = entityName,
                ModuleId = dataRow.Field<int>("module_id"),
                ModuleIcon = dataRow.Field<string>("icon"),
                TotalItems = entityCount
            });
            result[collectionNewlyCreated].Add(new EntityTypeModel
            {
                DisplayName = entityName,
                ModuleId = dataRow.Field<int>("module_id"),
                ModuleIcon = dataRow.Field<string>("icon"),
                TotalItems = newEntityCount
            });
        }

        result[collectionAll] = result[collectionAll].OrderByDescending(e => e.TotalItems).ToList();
        result[collectionNewlyCreated] = result[collectionNewlyCreated].OrderByDescending(e => e.TotalItems).ToList();

        return result;
    }

    /// <summary>
    /// Retrieves all open task alerts for all users and tallies them per user.
    /// </summary>
    private async Task<Dictionary<string, int>> GetOpenTaskAlertsAsync(ClaimsIdentity identity, string databaseName = null)
    {
        var result = new Dictionary<string, int>();
        var openTaskAlerts = await taskAlertsService.GetAsync(identity, true, databaseName);

        foreach (var item in openTaskAlerts.ModelObject)
        {
            // Initialize result for a user if it's not added yet.
            if (!result.ContainsKey(item.UserName))
            {
                result[item.UserName] = 0;
            }

            // Simply add one for the current user.
            result[item.UserName]++;
        }

        return result;
    }

    /// <summary>
    /// Takes multiple instances of <see cref="DashboardDataModel"/> objects and combines them into one.
    /// </summary>
    /// <param name="sources">A list of <see cref="DashboardDataModel"/> objects to combine into one.</param>
    /// <returns>The combined <see cref="DashboardDataModel"/> object.</returns>
    private static DashboardDataModel CombineResults(IEnumerable<DashboardDataModel> sources)
    {
        var result = new DashboardDataModel
        {
            Items = new Dictionary<string, List<ItemsCountModel>>(3),
            Entities = new Dictionary<string, List<EntityTypeModel>>(2),
            OpenTaskAlerts = new Dictionary<string, int>()
        };

        var collectionAll = PeriodFilterTypes.All.ToString("G");
        var collectionNewlyCreated = PeriodFilterTypes.NewlyCreated.ToString("G");
        var collectionChanged = PeriodFilterTypes.Changed.ToString("G");
        foreach (var source in sources)
        {
            // A source can be null if a branch database doesn't exist.
            if (source == null)
            {
                continue;
            }

            // Combine items data.
            if (source.Items.TryGetValue(collectionAll, out var item))
            {
                AddItemCountsToResult(result.Items, collectionAll, item);
            }

            if (source.Items.TryGetValue(collectionNewlyCreated, out item))
            {
                AddItemCountsToResult(result.Items, collectionNewlyCreated, item);
            }

            if (source.Items.TryGetValue(collectionChanged, out item))
            {
                AddItemCountsToResult(result.Items, collectionChanged, item);
            }

            // Combine entities data.
            foreach (var sourceEntityList in source.Entities)
            {
                foreach (var sourceEntity in sourceEntityList.Value)
                {
                    // The sourceEntityList.Key will be either 'All' or 'NewlyCreated'.
                    var entity = result.Entities[sourceEntityList.Key].FirstOrDefault(e => e.DisplayName == sourceEntity.DisplayName);
                    if (entity == null)
                    {
                        result.Entities[sourceEntityList.Key].Add(sourceEntity);
                    }
                    else
                    {
                        entity.TotalItems += sourceEntity.TotalItems;
                    }

                    // No more than 3 entities should be in the list.
                    if (result.Entities[sourceEntityList.Key].Count == 3)
                    {
                        break;
                    }
                }
            }

            // Combine user data.
            result.UserLoginCountTop10 += source.UserLoginCountTop10;
            result.UserLoginCountOther += source.UserLoginCountOther;
            result.UserLoginActiveTop10 += source.UserLoginActiveTop10;
            result.UserLoginActiveOther += source.UserLoginActiveOther;

            // Combine open task alerts data.
            foreach (var (key, value) in source.OpenTaskAlerts)
            {
                result.OpenTaskAlerts.TryAdd(key, 0);
                result.OpenTaskAlerts[key] += value;
            }
        }

        return result;
    }

    /// <summary>
    /// Makes sure the necessary table exist and that the necessary tables have the necessary columns.
    /// </summary>
    /// <param name="databaseName">The name of the database that is currently being worked in (for the branches functionality).</param>
    private async Task CheckAndUpdateTablesAsync(string databaseName = null)
    {
        await databaseHelpersService.CheckAndUpdateTablesAsync(new List<string>
        {
            // Ensure entities table has the "show_in_dashboard" column.
            WiserTableNames.WiserEntity,
            // Ensure data selector table has the "show_in_dashboard" column.
            WiserTableNames.WiserDataSelector,
            // Main dashboard table.
            WiserTableNames.WiserDashboard,
            // For the user login data.
            WiserTableNames.WiserLoginLog
        }, databaseName);
    }

    /// <summary>
    /// Refreshes the base Wiser statistics data. This is without a period, and will be remembered for a day.
    /// </summary>
    /// <param name="databaseName">The name of the database that is currently being worked in (for the branches functionality).</param>
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

        // Retrieve entities data.
        var entitiesData = await GetEntitiesDataAsync(databaseName: databaseName);

        // Retrieve user data.
        var userData = await GetUserDataAsync(databaseName: databaseName);

        // Clear old data first.
        await clientDatabaseConnection.ExecuteAsync($"TRUNCATE TABLE {databasePart}`{WiserTableNames.WiserDashboard}`");

        clientDatabaseConnection.ClearParameters();
        clientDatabaseConnection.AddParameter("last_update", DateTime.Now);
        clientDatabaseConnection.AddParameter("items_data", Newtonsoft.Json.JsonConvert.SerializeObject(itemsData));
        clientDatabaseConnection.AddParameter("entities_data", Newtonsoft.Json.JsonConvert.SerializeObject(entitiesData));
        clientDatabaseConnection.AddParameter("user_login_count_top10", userData.UserLoginCountTop10);
        clientDatabaseConnection.AddParameter("user_login_count_other", userData.UserLoginCountOther);
        clientDatabaseConnection.AddParameter("user_login_active_top10", userData.UserLoginActiveTop10);
        clientDatabaseConnection.AddParameter("user_login_active_other", userData.UserLoginActiveOther);
        await clientDatabaseConnection.ExecuteAsync($"INSERT INTO {databasePart}`{WiserTableNames.WiserDashboard}` (last_update, items_data, entities_data, user_login_count_top10, user_login_count_other, user_login_active_top10, user_login_active_other) VALUES (?last_update, ?items_data, ?entities_data, ?user_login_count_top10, ?user_login_count_other, ?user_login_active_top10, ?user_login_active_other)");
    }

    /// <inheritdoc />
    public async Task<ServiceResult<List<Service>>> GetWtsServicesAsync(ClaimsIdentity identity)
    {
        await databaseHelpersService.CheckAndUpdateTablesAsync(new List<string>()
        {
            WiserTableNames.WtsLogs,
            WiserTableNames.WtsServices
        });

        var services = new List<Service>();

        var dataTable = await clientDatabaseConnection.GetAsync($"SELECT * FROM {WiserTableNames.WtsServices}");

        if (dataTable.Rows.Count == 0)
        {
            return new ServiceResult<List<Service>>
            {
                ModelObject = services,
                StatusCode = HttpStatusCode.OK
            };
        }

        foreach (DataRow row in dataTable.Rows)
        {
            var service = new Service()
            {
                Id = row.Field<int>("id"),
                Configuration = row.Field<string>("configuration"),
                TimeId = row.Field<int>("time_id"),
                Action = row.Field<string>("action"),
                Scheme = row.Field<string>("scheme"),
                LastRun = row.Field<DateTime?>("last_run"),
                NextRun = row.Field<DateTime?>("next_run"),
                RunTime = row.Field<double>("run_time"),
                State = row.Field<string>("state"),
                Paused = row.Field<bool>("paused"),
                ExtraRun = row.Field<bool>("extra_run"),
                TemplateId = row["template_id"] == DBNull.Value ? -1 : row.Field<int>("template_id")
            };

            services.Add(service);
        }

        return new ServiceResult<List<Service>>
        {
            ModelObject = services,
            StatusCode = HttpStatusCode.OK
        };
    }

    /// <inheritdoc />
    public async Task<ServiceResult<List<ServiceLog>>> GetWtsServiceLogsAsync(ClaimsIdentity identity, int id)
    {
        var logs = new List<ServiceLog>();

        clientDatabaseConnection.AddParameter("serviceId", id);
        var datatable = await clientDatabaseConnection.GetAsync($@"SELECT log.*
                                                                        FROM {WiserTableNames.WtsServices} AS service
                                                                        JOIN {WiserTableNames.WtsLogs} AS log ON log.configuration = service.configuration AND log.time_id = service.time_id
                                                                        WHERE service.id = ?serviceId
                                                                        ORDER BY log.added_on DESC");

        foreach (DataRow row in datatable.Rows)
        {
            var log = new ServiceLog()
            {
                Id = row.Field<int>("id"),
                Level = row.Field<string>("level"),
                Scope = row.Field<string>("scope"),
                Source = row.Field<string>("source"),
                Configuration = row.Field<string>("configuration"),
                TimeId = row.Field<int>("time_id"),
                Order = row.Field<int>("order"),
                AddedOn = row.Field<DateTime>("added_on"),
                Message = row.Field<string>("message"),
                IsTest = row.Field<bool>("is_test")
            };

            logs.Add(log);
        }

        return new ServiceResult<List<ServiceLog>>()
        {
            ModelObject = logs,
            StatusCode = HttpStatusCode.OK
        };
    }

    /// <inheritdoc />
    public async Task<ServiceResult<ServicePauseStates>> SetWtsServicePauseStateAsync(ClaimsIdentity identity, int id, bool state)
    {
        clientDatabaseConnection.AddParameter("serviceId", id);
        clientDatabaseConnection.AddParameter("pauseState", state);

        var dataTable = await clientDatabaseConnection.GetAsync($"SELECT state FROM {WiserTableNames.WtsServices} WHERE id = ?serviceId");

        if (dataTable.Rows.Count == 0)
        {
            return new ServiceResult<ServicePauseStates>()
            {
                StatusCode = HttpStatusCode.NotFound,
                ErrorMessage = $"There is no service with ID '{id}' and can therefore not be paused."
            };
        }

        // If the service is currently running the pause will be activated after the run completed.
        var currentState = dataTable.Rows[0].Field<string>("state");
        var serviceIsCurrentlyRunning = currentState.Equals("running", StringComparison.InvariantCultureIgnoreCase);
        var serviceIsNotActive = currentState.Equals("stopped", StringComparison.InvariantCultureIgnoreCase);

        await clientDatabaseConnection.ExecuteAsync($@"UPDATE {WiserTableNames.WtsServices}
SET paused = ?pauseState
{(state && !serviceIsCurrentlyRunning && !serviceIsNotActive ? ", state = 'paused'" : !state &&!serviceIsCurrentlyRunning && !serviceIsNotActive ? ", state = 'active'" : "")}
WHERE id = ?serviceId");

        return new ServiceResult<ServicePauseStates>()
        {
            StatusCode = HttpStatusCode.OK,
            ModelObject = !state ? ServicePauseStates.Unpaused : serviceIsCurrentlyRunning ? ServicePauseStates.WillPauseAfterRunFinished : ServicePauseStates.Paused
        };
    }

    /// <inheritdoc />
    public async Task<ServiceResult<ServiceExtraRunStates>> SetWtsServiceExtraRunStateAsync(ClaimsIdentity identity, int id, bool state)
    {
        clientDatabaseConnection.AddParameter("serviceId", id);
        clientDatabaseConnection.AddParameter("extraRunState", state);

        var dataTable = await clientDatabaseConnection.GetAsync($"SELECT state FROM {WiserTableNames.WtsServices} WHERE id = ?serviceId");

        if (dataTable.Rows.Count == 0)
        {
            return new ServiceResult<ServiceExtraRunStates>()
            {
                StatusCode = HttpStatusCode.NotFound,
                ErrorMessage = $"There is no service with ID '{id}' and can therefore not be marked for an extra run."
            };
        }

        var currentState = dataTable.Rows[0].Field<string>("state");

        // If the service is currently running the extra run state can't change.
        if (currentState.Equals("running", StringComparison.InvariantCultureIgnoreCase))
        {
            return new ServiceResult<ServiceExtraRunStates>()
            {
                StatusCode = HttpStatusCode.OK,
                ModelObject = ServiceExtraRunStates.ServiceRunning
            };
        }

        // If the service is stopped no WTS instance is able to perform the extra run.
        if (currentState.Equals("stopped", StringComparison.InvariantCultureIgnoreCase))
        {
            return new ServiceResult<ServiceExtraRunStates>()
            {
                StatusCode = HttpStatusCode.OK,
                ModelObject = ServiceExtraRunStates.WtsOffline
            };
        }

        await clientDatabaseConnection.ExecuteAsync($@"UPDATE {WiserTableNames.WtsServices}
SET extra_run = ?extraRunState
WHERE id = ?serviceId");

        return new ServiceResult<ServiceExtraRunStates>()
        {
            StatusCode = HttpStatusCode.OK,
            ModelObject = state ? ServiceExtraRunStates.Marked : ServiceExtraRunStates.Unmarked
        };
    }

    /// <inheritdoc />
    public async Task<ServiceResult<JToken>> GetDataSelectorResultAsync(ClaimsIdentity identity)
    {
        // First get the ID of the data selector whose result needs to be returned.
        var dataSelectorData = await clientDatabaseConnection.GetAsync($"SELECT id FROM `{WiserTableNames.WiserDataSelector}` WHERE show_in_dashboard = 1 LIMIT 1");

        if (dataSelectorData.Rows.Count == 0)
        {
            return new ServiceResult<JToken>(null);
        }

        // Get the ID from the result and validate it.
        var dataSelectorId = Convert.ToInt32(dataSelectorData.Rows[0]["id"]);
        if (dataSelectorId <= 0)
        {
            return new ServiceResult<JToken>(null);
        }

        // Simply return the data selector service's result, as it's exactly the same type.
        return await dataSelectorsService.GetDataSelectorResultAsJsonAsync(identity, dataSelectorId, false, null, true);
    }

    /// <summary>
    /// Checks if the MySQL tables for the dashboard is up-to-date.
    /// </summary>
    private async Task KeepTablesUpToDateAsync(string databaseName)
    {
        var lastTableUpdates = await databaseHelpersService.GetLastTableUpdatesAsync(databaseName);

        // Check if the dashboard table needs to be updated.
        if (!await databaseHelpersService.TableExistsAsync(WiserTableNames.WiserDashboard) || lastTableUpdates.TryGetValue(WiserTableNames.WiserDashboard, out var value) && value >= new DateTime(2023, 2, 23))
        {
            return;
        }

        // Add columns.
        var column = new ColumnSettingsModel("user_login_active_top10", MySqlDbType.Int64, notNull: true, defaultValue: "0");
        await databaseHelpersService.AddColumnToTableAsync(WiserTableNames.WiserDashboard, column, false, databaseName);
        column = new ColumnSettingsModel("user_login_active_other", MySqlDbType.Int64, notNull: true, defaultValue: "0");
        await databaseHelpersService.AddColumnToTableAsync(WiserTableNames.WiserDashboard, column, false, databaseName);

        // Convert and drop the "user_login_time_top10" column if it still exists.
        if (await databaseHelpersService.ColumnExistsAsync(WiserTableNames.WiserDashboard, "user_login_time_top10", databaseName))
        {
            await ConvertTimeSpanToSecondsAsync(WiserTableNames.WiserDashboard, databaseName, "user_login_time_top10", "user_login_active_top10");
            await databaseHelpersService.DropColumnAsync(WiserTableNames.WiserDashboard, "user_login_time_top10", databaseName);
        }

        // Convert and drop the "user_login_time_other" column if it still exists.
        if (await databaseHelpersService.ColumnExistsAsync(WiserTableNames.WiserDashboard, "user_login_time_other", databaseName))
        {
            await ConvertTimeSpanToSecondsAsync(WiserTableNames.WiserDashboard, databaseName, "user_login_time_other", "user_login_active_other");
            await databaseHelpersService.DropColumnAsync(WiserTableNames.WiserDashboard, "user_login_time_other", databaseName);
        }

        clientDatabaseConnection.ClearParameters();
        clientDatabaseConnection.AddParameter("tableName", WiserTableNames.WiserDashboard);
        clientDatabaseConnection.AddParameter("lastUpdate", DateTime.Now);
        var lastUpdateData = await clientDatabaseConnection.GetAsync($"SELECT `name` FROM `{WiserTableNames.WiserTableChanges}` WHERE `name` = ?tableName");
        var queryDatabasePart = !String.IsNullOrWhiteSpace(databaseName) ? $"`{databaseName}`." : String.Empty;
        if (lastUpdateData.Rows.Count == 0)
        {
            await clientDatabaseConnection.ExecuteAsync($"INSERT INTO {queryDatabasePart}`{WiserTableNames.WiserTableChanges}` (`name`, last_update) VALUES (?tableName, ?lastUpdate)");
        }
        else
        {
            await clientDatabaseConnection.ExecuteAsync($"UPDATE {queryDatabasePart}`{WiserTableNames.WiserTableChanges}` SET last_update = ?lastUpdate WHERE `name` = ?tableName LIMIT 1");
        }
    }

    private async Task ConvertTimeSpanToSecondsAsync(string tableName, string databaseName, string oldColumnName, string newColumnName)
    {
        var queryDatabasePart = !String.IsNullOrWhiteSpace(databaseName) ? $"`{databaseName}`." : String.Empty;

        var convertDataTable = await clientDatabaseConnection.GetAsync($"SELECT id, `{oldColumnName}` FROM {queryDatabasePart}`{tableName}`");
        foreach (var dataRow in convertDataTable.Rows.Cast<DataRow>())
        {
            var seconds = Convert.ToInt32(Math.Floor(dataRow.Field<TimeSpan>(oldColumnName).TotalSeconds));

            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("tableId", Convert.ToUInt64(dataRow["id"]));
            clientDatabaseConnection.AddParameter("seconds", seconds);
            await clientDatabaseConnection.ExecuteAsync($"UPDATE `{tableName}` SET `{newColumnName}` = ?seconds WHERE id = ?tableId");
        }
    }
}
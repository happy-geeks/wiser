using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Api.Core.Extensions;
using Api.Core.Helpers;
using Api.Core.Interfaces;
using Api.Core.Models;
using Api.Core.Services;
using Api.Modules.Grids.Enums;
using Api.Modules.Grids.Interfaces;
using Api.Modules.Grids.Models;
using Api.Modules.Items.Interfaces;
using Api.Modules.Kendo.Models;
using Api.Modules.Modules.Models;
using Api.Modules.Tenants.Interfaces;
using Api.Modules.Tenants.Models;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Core.Services;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.GclReplacements.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Api.Modules.Grids.Services
{
    /// <summary>
    /// Service for (Kendo) grid functionality.
    /// </summary>
    public class GridsService : IGridsService, IScopedService
    {
        private readonly IItemsService itemsService;
        private readonly IWiserTenantsService wiserTenantsService;
        private readonly IDatabaseConnection clientDatabaseConnection;
        private readonly IWiserItemsService wiserItemsService;
        private readonly ILogger<GridsService> logger;
        private readonly IStringReplacementsService stringReplacementsService;
        private readonly IApiReplacementsService apiReplacementsService;
        private readonly ApiSettings apiSettings;

        private static readonly List<string> ItemColumns = new()
        {
            "id",
            "unique_uuid",
            "uniqueUuid",
            "entity_type",
            "entityType",
            "moduleid",
            "published_environment",
            "publishedEnvironment",
            "readonly",
            "removed",
            "title",
            "added_on",
            "addedOn",
            "added_by",
            "addedBy",
            "changed_on",
            "changedOn",
            "changed_by",
            "changedBy"
        };

        /// <summary>
        /// Creates a new instance of GridsService.
        /// </summary>
        public GridsService(IItemsService itemsService, IWiserTenantsService wiserTenantsService, IDatabaseConnection clientDatabaseConnection, IWiserItemsService wiserItemsService, ILogger<GridsService> logger, IStringReplacementsService stringReplacementsService, IApiReplacementsService apiReplacementsService, IOptions<ApiSettings> apiSettings)
        {
            this.itemsService = itemsService;
            this.wiserTenantsService = wiserTenantsService;
            this.clientDatabaseConnection = clientDatabaseConnection;
            this.wiserItemsService = wiserItemsService;
            this.logger = logger;
            this.stringReplacementsService = stringReplacementsService;
            this.apiReplacementsService = apiReplacementsService;
            this.apiSettings = apiSettings.Value;
        }

        /// <inheritdoc />
        public async Task<ServiceResult<GridSettingsAndDataModel>> GetEntityGridDataAsync(string encryptedId,
            string entityType,
            int linkTypeNumber,
            int moduleId,
            EntityGridModes mode,
            GridReadOptionsModel options,
            int propertyId,
            string encryptedQueryId,
            string encryptedCountQueryId,
            string fieldGroupName,
            bool currentItemIsSourceId,
            ClaimsIdentity identity)
        {
            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            clientDatabaseConnection.SetCommandTimeout(600);
            var tenant = await wiserTenantsService.GetSingleAsync(identity);
            var encryptionKey = tenant.ModelObject.EncryptionKey;
            var itemId = wiserTenantsService.DecryptValue<ulong>(encryptedId, tenant.ModelObject);
            var userId = IdentityHelpers.GetWiserUserId(identity);
            var fieldsInformation = new List<(string Field, string InputType, Dictionary<string, object> Options)>();

            var forceAddColumns = false;
            var selectQuery = "";
            var countQuery = "";
            var gridOptionsValue = "";
            var extraJavascript = new StringBuilder();

            // Get entity type settings, so see whether we can have different versions of a single item for different environments or if the items have their own dedicated table.
            var versionJoinClause = "";
            var subQueryVersionJoinClause = "";
            var versionWhereClause = "";
            var tablePrefix = "";
            var linkTablePrefix = "";
            if (!String.IsNullOrWhiteSpace(entityType) && !String.Equals(entityType, "all", StringComparison.OrdinalIgnoreCase) && !String.Equals(entityType, "undefined", StringComparison.OrdinalIgnoreCase))
            {
                var entityTypeSettings = await wiserItemsService.GetEntityTypeSettingsAsync(entityType);
                tablePrefix = wiserItemsService.GetTablePrefixForEntity(entityTypeSettings);

                if (entityTypeSettings.EnableMultipleEnvironments)
                {
                    versionJoinClause = $@"# Only get the latest version of an item.
                                        LEFT JOIN {tablePrefix}{WiserTableNames.WiserItem}{{0}} AS otherVersion ON otherVersion.original_item_id > 0 AND i.original_item_id > 0 AND otherVersion.original_item_id = i.original_item_id AND (otherVersion.changed_on > i.changed_on OR (otherVersion.changed_on = i.changed_on AND otherVersion.id > i.id))";
                    subQueryVersionJoinClause = $@"# Only get the latest version of an item.
                                        LEFT JOIN {tablePrefix}{WiserTableNames.WiserItem}{{0}} AS otherVersion ON otherVersion.original_item_id > 0 AND i.originalItemId > 0 AND otherVersion.original_item_id = i.originalItemId AND (otherVersion.changed_on > i.changedOn OR (otherVersion.changed_on = i.changedOn AND otherVersion.id > i.id))";
                    versionWhereClause = "AND otherVersion.id IS NULL";
                }
            }

            // Get the link type settings, to find out whether to use the wiser_itemlink table, or the columns "parent_item_id" from the wiser_item table.
            var useItemParentId = false;
            if (linkTypeNumber > 0)
            {
                var linkTypeSettings = await wiserItemsService.GetLinkTypeSettingsAsync(linkTypeNumber, currentItemIsSourceId ? null : entityType, currentItemIsSourceId ? entityType : null);
                useItemParentId = linkTypeSettings.UseItemParentId;
                linkTablePrefix = wiserItemsService.GetTablePrefixForLink(linkTypeSettings);
            }

            // Find out if there are custom queries for the grid.
            var columnsToSelect = "options";
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("id", propertyId);
            if (mode == EntityGridModes.LinkOverview)
            {
                columnsToSelect += ", search_query, search_count_query";
            }

            DataTable dataTable;
            var gridOptionsDataTable = await clientDatabaseConnection.GetAsync($"SELECT {columnsToSelect} FROM {WiserTableNames.WiserEntityProperty} WHERE id = ?id");

            if (gridOptionsDataTable.Rows.Count > 0)
            {
                if (gridOptionsDataTable.Columns.Contains("search_query"))
                {
                    selectQuery = gridOptionsDataTable.Rows[0].Field<string>("search_query");
                }

                if (gridOptionsDataTable.Columns.Contains("search_count_query"))
                {
                    countQuery = gridOptionsDataTable.Rows[0].Field<string>("search_count_query");

                    // If the count query is empty and the select query contains a limit, build a count query based on the select query without the limit and sort.
                    if (String.IsNullOrWhiteSpace(countQuery) && !String.IsNullOrWhiteSpace(selectQuery) && selectQuery.Contains("{limit}", StringComparison.OrdinalIgnoreCase))
                    {
                        countQuery = $@"SELECT COUNT(*) FROM (
                                        {selectQuery.Replace("{limit}", "", StringComparison.OrdinalIgnoreCase).Replace("{sort}", "", StringComparison.OrdinalIgnoreCase).Trim(';')}
                                    ) AS x";
                    }
                }

                gridOptionsValue = gridOptionsDataTable.Rows[0].Field<string>("options");
            }

            var results = new GridSettingsAndDataModel();
            var fieldMappings = new List<FieldMapModel>();
            if (mode != EntityGridModes.LinkOverview)
            {
                results = await GridSettingsAndDataModelFromFieldOptionsAsync(propertyId, gridOptionsValue, itemId);
            }
            else if (!String.IsNullOrWhiteSpace(gridOptionsValue))
            {
                try
                {
                    var mainGridOptions = JsonConvert.DeserializeObject<GridSettingsAndDataModel>(gridOptionsValue.Replace("{itemId}", itemId.ToString(), StringComparison.OrdinalIgnoreCase));
                    if (mainGridOptions?.SearchGridSettings?.GridViewSettings != null)
                    {
                        results = mainGridOptions.SearchGridSettings.GridViewSettings;
                    }

                    if (mainGridOptions?.SearchGridSettings?.FieldMappings != null)
                    {
                        fieldMappings = mainGridOptions.SearchGridSettings.FieldMappings;
                    }
                }
                catch (Exception exception)
                {
                    logger.LogError($"An error occurred while deserializing the options for a LinkOverview grid. PropertyId: {propertyId}, itemId: {itemId}, options: {gridOptionsValue}, error: {exception}");
                }
            }

            results.PageSize = options != null && options.PageSize > 0 ? options.PageSize : 100;

            var hasPredefinedSchema = false;

            switch (mode)
            {
                // For link overview mode it's possible to enter custom queries, use those if entered.
                case EntityGridModes.LinkOverview when !String.IsNullOrWhiteSpace(selectQuery):
                {
                    forceAddColumns = !results.Columns.Any();

                    if (results.Columns.All(c => c.Selectable == false))
                    {
                        results.Columns.Insert(0, new GridColumn {Selectable = true, Width = "30px"});
                    }

                    clientDatabaseConnection.ClearParameters();
                    clientDatabaseConnection.AddParameter("itemId", itemId);
                    clientDatabaseConnection.AddParameter("userId", userId);

                    selectQuery = selectQuery.Replace("'{itemId}'", "?itemId", StringComparison.OrdinalIgnoreCase);
                    countQuery = countQuery?.Replace("'{itemId}'", "?itemId", StringComparison.OrdinalIgnoreCase);
                    selectQuery = selectQuery.Replace("{itemId}", "?itemId", StringComparison.OrdinalIgnoreCase);
                    countQuery = countQuery?.Replace("{itemId}", "?itemId", StringComparison.OrdinalIgnoreCase);
                    (selectQuery, countQuery) = BuildGridQueries(options, selectQuery, countQuery, identity, "", fieldMappings: fieldMappings, tablePrefix: tablePrefix);

                    // Get the count, but only if this is not the first load.
                    if (options?.FirstLoad ?? true)
                    {
                        var countDataTable = await clientDatabaseConnection.GetAsync(countQuery);
                        results.TotalResults = Convert.ToInt32(countDataTable.Rows[0][0]);
                    }

                    // Get the data.
                    dataTable = await clientDatabaseConnection.GetAsync(selectQuery);
                    break;
                }
                case EntityGridModes.ChangeHistory:
                {
                    // Data for the change history of an item.
                    results.Columns.Add(new GridColumn {Field = "changedon", Title = "Datum", Format = "{0:dd MMMM yyyy - HH:mm:ss}"});
                    results.Columns.Add(new GridColumn {Field = "field", Title = "Veldnaam"});
                    results.Columns.Add(new GridColumn {Field = "action", Title = "Actie type"});
                    results.Columns.Add(new GridColumn {Field = "oldvalue", Title = "Oude waarde"});
                    results.Columns.Add(new GridColumn {Field = "newvalue", Title = "Nieuwe waarde"});
                    results.Columns.Add(new GridColumn {Field = "changedby", Title = "Gewijzigd door"});

                    hasPredefinedSchema = true;
                    results.SchemaModel.Fields.Add("id", new FieldModel {Type = "number"});
                    results.SchemaModel.Fields.Add("changedon", new FieldModel {Type = "date"});
                    results.SchemaModel.Fields.Add("tablename", new FieldModel {Type = "string"});
                    results.SchemaModel.Fields.Add("itemid", new FieldModel {Type = "number"});
                    results.SchemaModel.Fields.Add("action", new FieldModel {Type = "string"});
                    results.SchemaModel.Fields.Add("changedby", new FieldModel {Type = "string"});
                    results.SchemaModel.Fields.Add("field", new FieldModel {Type = "string"});
                    results.SchemaModel.Fields.Add("oldvalue", new FieldModel {Type = "string"});
                    results.SchemaModel.Fields.Add("newvalue", new FieldModel {Type = "string"});

                    clientDatabaseConnection.ClearParameters();
                    clientDatabaseConnection.AddParameter("itemId", itemId);
                    clientDatabaseConnection.AddParameter("itemTableName", $"{tablePrefix}{WiserTableNames.WiserItem}");
                    clientDatabaseConnection.AddParameter("detailTableName", $"{tablePrefix}{WiserTableNames.WiserItemDetail}");
                    clientDatabaseConnection.AddParameter("itemArchiveTableName", $"{tablePrefix}{WiserTableNames.WiserItem}{WiserTableNames.ArchiveSuffix}");
                    clientDatabaseConnection.AddParameter("detailArchiveTableName", $"{tablePrefix}{WiserTableNames.WiserItemDetail}{WiserTableNames.ArchiveSuffix}");

                    countQuery = $$"""
                                   SELECT COUNT(*) AS `count`
                                   FROM {{WiserTableNames.WiserHistory}}
                                   WHERE item_id = ?itemid
                                   AND tablename IN (?itemTableName, ?detailTableName, ?itemArchiveTableName, ?detailArchiveTableName)
                                   [if({changedby_has_filter}=1)]AND changed_by {changedby_filter}[endif]
                                   [if({changedon_has_filter}=1)]AND DATE(changed_on) {changedon_filter}[endif]
                                   [if({field_has_filter}=1)]AND field {field_filter}[endif]
                                   [if({action_has_filter}=1)]AND `action` {action_filter}[endif]
                                   [if({oldvalue_has_filter}=1)]AND oldvalue {oldvalue_filter}[endif]
                                   [if({newvalue_has_filter}=1)]AND newvalue {newvalue_filter}[endif]
                                   """;

                    selectQuery = $$"""
                                    SELECT 
                                    	id AS id,
                                    	changed_on AS changedon,
                                    	tablename AS tablename,
                                    	item_id AS itemid,
                                    	action AS action,
                                    	changed_by AS changedby,
                                    	field AS field,
                                    	oldvalue AS oldvalue,
                                    	newvalue AS newvalue
                                    FROM {{WiserTableNames.WiserHistory}}
                                    
                                    WHERE item_id = ?itemid
                                    AND tablename IN (?itemTableName, ?detailTableName, ?itemArchiveTableName, ?detailArchiveTableName)
                                    [if({changedby_has_filter}=1)]AND changed_by {changedby_filter}[endif]
                                    [if({changedon_has_filter}=1)]AND DATE(changed_on) {changedon_filter}[endif]
                                    [if({field_has_filter}=1)]AND field {field_filter}[endif]
                                    [if({action_has_filter}=1)]AND `action` {action_filter}[endif]
                                    [if({oldvalue_has_filter}=1)]AND oldvalue {oldvalue_filter}[endif]
                                    [if({newvalue_has_filter}=1)]AND newvalue {newvalue_filter}[endif]
                                    GROUP BY id
                                    ORDER BY changedon DESC, id DESC
                                    {limit}
                                    """;

                    (selectQuery, countQuery) = BuildGridQueries(options, selectQuery, countQuery, identity, "ORDER BY current.changed_on DESC, current.id DESC", tablePrefix: tablePrefix);

                    // Get the count, but only if this is not the first load.
                    if (options?.FirstLoad ?? true)
                    {
                        var countDataTable = await clientDatabaseConnection.GetAsync(countQuery);
                        results.TotalResults = Convert.ToInt32(countDataTable.Rows[0][0]);
                    }

                    // Get the data.
                    dataTable = await clientDatabaseConnection.GetAsync(selectQuery);
                    break;
                }
                case EntityGridModes.TaskHistory:
                {
                    results.Columns.Add(new GridColumn {Field = "duedate", Title = "Due-date", Format = "{0:dd MMMM yyyy}"});
                    results.Columns.Add(new GridColumn {Field = "sender", Title = "Verzonden door"});
                    results.Columns.Add(new GridColumn {Field = "receiver", Title = "Verzonden aan"});
                    results.Columns.Add(new GridColumn {Field = "content", Title = "Tekst", Width = "600px"});
                    results.Columns.Add(new GridColumn {Field = "checkeddate", Title = "Afgevinkt op", Format = "{0:dd MMMM yyyy - HH:mm:ss}"});

                    countQuery = $@"SELECT COUNT(*)
                                    FROM {WiserTableNames.WiserItem} i

                                    JOIN {WiserTableNames.WiserItemDetail} AS dueDate ON dueDate.item_id = i.id AND dueDate.`key` = 'agendering_date' [if({{due_date}}!)]AND dueDate.`value` IS NOT NULL AND dueDate.`value` <> '' AND DATE(dueDate.`value`) {{due_date_filter}}[endif] 
                                    [if({{checked_date_has_filter}}!1)]LEFT [endif]JOIN {WiserTableNames.WiserItemDetail} checkedDate ON checkedDate.item_id = i.id AND checkedDate.`key` = 'checkedon' [if({{checked_date_has_filter}}=1)]AND checkedDate.`value` IS NOT NULL AND checkedDate.`value` <> '' AND DATE(checkedDate.`value`) {{checked_date_filter}}[endif] 
                                    [if({{sender_has_filter}}!1)]LEFT [endif]JOIN {WiserTableNames.WiserItemDetail} sender ON sender.item_id = i.id AND sender.`key` = 'placed_by_id' [if({{sender_has_filter}}=1)]AND sender.`value` {{sender_filter}}[endif] 
                                    JOIN {WiserTableNames.WiserItemDetail} receiver ON receiver.item_id = i.id AND receiver.`key` = 'userid' [if({{receiver_has_filter}}=1)]AND receiver.`value` {{receiver_filter}}[endif] 
                                    JOIN {WiserTableNames.WiserItemDetail} content ON content.item_id = i.id AND content.`key` = 'content' [if({{content_has_filter}}=1)]AND content.`value` {{content_filter}}[endif] 

                                    WHERE i.entity_type = 'agendering'";

                    selectQuery = $@"SELECT
	                                    i.id,
	                                    i.id AS encryptedId_encrypt_withdate,
	                                    STR_TO_DATE(dueDate.`value`, '%Y-%m-%d %H:%i:%s') AS dueDate,
	                                    STR_TO_DATE(checkedDate.`value`, '%Y-%m-%d %H:%i:%s') AS checkedDate,
	                                    IFNULL(sender.`value`, '') AS sender,
	                                    receiver.`value` AS receiver,
	                                    content.`value` AS content
                                    FROM {WiserTableNames.WiserItem} i

                                    JOIN {WiserTableNames.WiserItemDetail} dueDate ON dueDate.item_id = i.id AND dueDate.`key` = 'agendering_date' [if({{dueDate}}!)]AND dueDate.`value` <> ''AND DATE(dueDate.`value`) {{dueDate_filter}}[endif] 
                                    [if({{checkedDate}}=)]LEFT [endif]JOIN {WiserTableNames.WiserItemDetail} checkedDate ON checkedDate.item_id = i.id AND checkedDate.`key` = 'checkedon' [if({{checkedDate}}!)]AND dueDate.`value` IS NOT NULL AND checkedDate.`value` IS NOT NULL AND checkedDate.`value` <> '' AND DATE(checkedDate.`value`) {{checkedDate_filter}}[endif] 
                                    [if({{sender}}=)]LEFT [endif]JOIN {WiserTableNames.WiserItemDetail} sender ON sender.item_id = i.id AND sender.`key` = 'placed_by_id' [if({{sender}}!)]AND sender.`value` {{sender_filter}}[endif] 
                                    JOIN {WiserTableNames.WiserItemDetail} receiver ON receiver.item_id = i.id AND receiver.`key` = 'userid' [if({{receiver}}!)]AND receiver.`value` {{receiver_filter}}[endif] 
                                    JOIN {WiserTableNames.WiserItemDetail} content ON content.item_id = i.id AND content.`key` = 'content' [if({{content}}!)]AND content.`value` {{content_filter}}[endif] 

                                    WHERE i.entity_type = 'agendering'
                                    {{sort}}
                                    {{limit}}";

                    (selectQuery, countQuery) = BuildGridQueries(options, selectQuery, countQuery, identity, "ORDER BY dueDate DESC", tablePrefix: tablePrefix);

                    // Get the count, but only if this is not the first load.
                    if (options?.FirstLoad ?? true)
                    {
                        var countDataTable = await clientDatabaseConnection.GetAsync(countQuery);
                        results.TotalResults = Convert.ToInt32(countDataTable.Rows[0][0]);
                    }

                    // Get the data.
                    dataTable = await clientDatabaseConnection.GetAsync(selectQuery);
                    break;
                }
                case EntityGridModes.CustomQuery:
                {
                    var queryId = String.IsNullOrWhiteSpace(encryptedQueryId) ? 0 : Int32.Parse(encryptedQueryId.Replace(" ", "+").DecryptWithAesWithSalt(encryptionKey, true));
                    var countQueryId = String.IsNullOrWhiteSpace(encryptedCountQueryId) ? 0 : Int32.Parse(encryptedCountQueryId.Replace(" ", "+").DecryptWithAesWithSalt(encryptionKey, true));

                    var customQueryResult = await itemsService.GetCustomQueryAsync(propertyId, queryId, identity);
                    var countQueryResult = new ServiceResult<string>();
                    if (countQueryId > 0)
                    {
                        countQueryResult = await itemsService.GetCustomQueryAsync(propertyId, countQueryId, identity);
                    }

                    if (customQueryResult.StatusCode != HttpStatusCode.OK)
                    {
                        return new ServiceResult<GridSettingsAndDataModel>
                        {
                            StatusCode = customQueryResult.StatusCode,
                            ErrorMessage = customQueryResult.ErrorMessage
                        };
                    }

                    clientDatabaseConnection.ClearParameters();
                    clientDatabaseConnection.AddParameter("itemId", itemId);

                    selectQuery = customQueryResult.ModelObject;
                    if (customQueryResult.StatusCode == HttpStatusCode.OK)
                    {
                        countQuery = countQueryResult.ModelObject;
                    }

                    // If the count query is empty and the select query contains a limit, build a count query based on the select query without the limit and sort.
                    if (String.IsNullOrWhiteSpace(countQuery) && !String.IsNullOrWhiteSpace(selectQuery) && selectQuery.Contains("{limit}", StringComparison.OrdinalIgnoreCase))
                    {
                        countQuery = $@"SELECT COUNT(*) FROM (
                                            {selectQuery.Replace("{limit}", "", StringComparison.OrdinalIgnoreCase).Replace("{sort}", "", StringComparison.OrdinalIgnoreCase).Trim(';')}
                                        ) AS x";
                    }

                    selectQuery = selectQuery.Replace("'{itemId}'", "?itemId", StringComparison.OrdinalIgnoreCase);
                    countQuery = countQuery?.Replace("'{itemId}'", "?itemId", StringComparison.OrdinalIgnoreCase);
                    selectQuery = selectQuery.Replace("{itemId}", "?itemId", StringComparison.OrdinalIgnoreCase);
                    countQuery = countQuery?.Replace("{itemId}", "?itemId", StringComparison.OrdinalIgnoreCase);
                    (selectQuery, countQuery) = BuildGridQueries(options, selectQuery, countQuery, identity, "", tablePrefix: tablePrefix);

                    // Get the count, but only if this is not the first load.
                    if (!String.IsNullOrWhiteSpace(countQuery) && (options?.FirstLoad ?? true))
                    {
                        var countDataTable = await clientDatabaseConnection.GetAsync(countQuery);
                        if (countDataTable.Rows.Count == 0)
                        {
                            countQuery = null;
                        }
                        else
                        {
                            results.TotalResults = Convert.ToInt32(countDataTable.Rows[0][0]);
                        }
                    }

                    // Get the actual data for the grid.
                    dataTable = await clientDatabaseConnection.GetAsync(selectQuery);

                    // If we have no count query, just count the total rows of the select query.
                    if (String.IsNullOrWhiteSpace(countQuery))
                    {
                        results.TotalResults = dataTable.Rows.Count;
                    }

                    break;
                }
                case EntityGridModes.SearchModule:
                {
                    results.Columns.Add(new GridColumn {Field = "icon", Title = "&nbsp;", Filterable = false, Width = "70px", Template = "<div class='grid-icon #:icon# icon-bg-#:color#'></div>"});
                    results.Columns.Add(new GridColumn {Field = "title", Title = "Titel", Template = "<strong>#: title #</strong><br><small>#: entitytype #</small>"});
                    results.Columns.Add(new GridColumn {Field = "addedon", Title = "Aangemaakt op", Format = "{0:dd MMMM yyyy}"});
                    results.Columns.Add(new GridColumn {Field = "addedby", Title = "Aangemaakt door"});
                    results.Columns.Add(new GridColumn {Field = "moreinfo", Title = "Overige info"});

                    if (options?.Filter?.Filters == null || !options.Filter.Filters.Any())
                    {
                        return new ServiceResult<GridSettingsAndDataModel>
                        {
                            ErrorMessage = "Search grid needs to have at least one value to search for.",
                            StatusCode = HttpStatusCode.BadRequest
                        };
                    }

                    var genericFilter = options.Filter.Filters.FirstOrDefault(f => f.Field.Equals("search", StringComparison.OrdinalIgnoreCase));
                    var removedFilter = options.Filter.Filters.FirstOrDefault(f => f.Field.Equals("removed", StringComparison.OrdinalIgnoreCase));

                    if (genericFilter == null)
                    {
                        // Filtering on specific field(s).
                        countQuery = $@"SELECT COUNT(*)
                                        FROM (
                                            SELECT DISTINCT i.id
                                            FROM {tablePrefix}{WiserTableNames.WiserItem} i
                                            JOIN {WiserTableNames.WiserEntity} e ON e.name = i.entity_type AND e.show_in_search = 1

                                            {String.Format(versionJoinClause, "")}

                                            # Check permissions. Default permissions are everything enabled, so if the user has no role or the role has no permissions on this item, they are allowed everything.
	                                        LEFT JOIN {WiserTableNames.WiserUserRoles} user_role ON user_role.user_id = ?userId
	                                        LEFT JOIN {WiserTableNames.WiserPermission} permission ON permission.role_id = user_role.role_id AND permission.item_id = i.id

                                            {{filters}}
                                            WHERE [if({{title}}!)]i.title {{title_filter}}[else]TRUE[endif] 
                                            [if({{uniqueUuid}}!)]AND i.unique_uuid {{uniqueUuid_filter}}[endif]
                                            [if({{added_by}}!)]AND i.added_by {{added_by_filter}}[endif]
                                            [if({{changed_by}}!)]AND i.changed_by {{changed_by_filter}}[endif]
                                            [if({{id}}!)]AND i.id {{id_filter}}[endif]
                                            {versionWhereClause}
                                            AND (?entityType = '' OR i.entity_type = ?entityType)
                                            AND (permission.id IS NULL OR (permission.permissions & 1) > 0)";

                        selectQuery = $@"SELECT 
	                                        i.id,
	                                        i.id AS encryptedId_encrypt_withdate,
                                            i.title,
	                                        i.entity_type AS entityType,
	                                        i.moduleId,
	                                        i.added_on AS addedOn,
	                                        i.added_by AS addedBy,
	                                        e.icon,
	                                        e.color,
	                                        '' AS more_info
                                        FROM {tablePrefix}{WiserTableNames.WiserItem} i
                                        JOIN {WiserTableNames.WiserEntity} e ON e.name = i.entity_type AND e.show_in_search = 1

                                        {String.Format(versionJoinClause, "")}

                                        # Check permissions. Default permissions are everything enabled, so if the user has no role or the role has no permissions on this item, they are allowed everything.
	                                    LEFT JOIN {WiserTableNames.WiserUserRoles} user_role ON user_role.user_id = ?userId
	                                    LEFT JOIN {WiserTableNames.WiserPermission} permission ON permission.role_id = user_role.role_id AND permission.item_id = i.id

                                        {{filters}}
                                        WHERE [if({{title}}!)]i.title {{title_filter}}[else]TRUE[endif] 
                                        [if({{uniqueUuid}}!)]AND i.unique_uuid {{uniqueUuid_filter}}[endif]
                                        [if({{addedBy}}!)]AND i.added_by {{addedBy_filter}}[endif]
                                        [if({{changedBy}}!)]AND i.changed_by {{changedBy_filter}}[endif]
                                        [if({{id}}!)]AND i.id {{id_filter}}[endif]
                                        {versionWhereClause}
                                        AND (?entityType = '' OR i.entity_type = ?entityType)
                                        AND (permission.id IS NULL OR (permission.permissions & 1) > 0)

                                        GROUP BY i.id";

                        if (removedFilter == null || removedFilter.Value == "1")
                        {
                            countQuery += $@"
                                            UNION
                                            SELECT DISTINCT i.id
                                            FROM {tablePrefix}{WiserTableNames.WiserItem}{WiserTableNames.ArchiveSuffix} i
                                            JOIN {WiserTableNames.WiserEntity} e ON e.name = i.entity_type AND e.show_in_search = 1

                                            {String.Format(versionJoinClause, WiserTableNames.ArchiveSuffix)}

                                            # Check permissions. Default permissions are everything enabled, so if the user has no role or the role has no permissions on this item, they are allowed everything.
	                                        LEFT JOIN {WiserTableNames.WiserUserRoles} user_role ON user_role.user_id = ?userId
	                                        LEFT JOIN {WiserTableNames.WiserPermission} permission ON permission.role_id = user_role.role_id AND permission.item_id = i.id

                                            {{filters}}
                                            WHERE [if({{title}}!)]i.title {{title_filter}}[else]TRUE[endif] 
                                            [if({{uniqueUuid}}!)]AND i.unique_uuid {{uniqueUuid_filter}}[endif]
                                            [if({{addedBy}}!)]AND i.added_by {{addedBy_filter}}[endif]
                                            [if({{changedBy}}!)]AND i.changed_by {{changedBy_filter}}[endif]
                                            [if({{id}}!)]AND i.id {{id_filter}}[endif]
                                            {versionWhereClause}
                                            AND (?entityType = '' OR i.entity_type = ?entityType)
                                            AND (permission.id IS NULL OR (permission.permissions & 1) > 0)";

                            selectQuery += $@"
                                            UNION ALL
                                            SELECT 
	                                            i.id,
	                                            i.id AS encryptedId_encrypt_withdate,
                                                i.title,
	                                            i.entity_type AS entityType,
	                                            i.moduleId,
	                                            i.added_on AS addedOn,
	                                            i.added_by AS addedBy,
	                                            e.icon,
	                                            e.color,
	                                            'Dit item is verwijderd' AS more_info
                                            FROM {tablePrefix}{WiserTableNames.WiserItem}{WiserTableNames.ArchiveSuffix} i
                                            JOIN {WiserTableNames.WiserEntity} e ON e.name = i.entity_type AND e.show_in_search = 1

                                            {String.Format(versionJoinClause, WiserTableNames.ArchiveSuffix)}

                                            # Check permissions. Default permissions are everything enabled, so if the user has no role or the role has no permissions on this item, they are allowed everything.
	                                        LEFT JOIN {WiserTableNames.WiserUserRoles} user_role ON user_role.user_id = ?userId
	                                        LEFT JOIN {WiserTableNames.WiserPermission} permission ON permission.role_id = user_role.role_id AND permission.item_id = i.id

                                            {{filters}}
                                            WHERE [if({{title}}!)]i.title {{title_filter}}[else]TRUE[endif] 
                                            [if({{uniqueUuid}}!)]AND i.unique_uuid {{uniqueUuid_filter}}[endif]
                                            [if({{addedBy}}!)]AND i.added_by {{addedBy_filter}}[endif]
                                            [if({{changedBy}}!)]AND i.changed_by {{changedBy_filter}}[endif]
                                            [if({{id}}!)]AND i.id {{id_filter}}[endif]
                                            {versionWhereClause}
                                            AND (?entityType = '' OR i.entity_type = ?entityType)
                                            AND (permission.id IS NULL OR (permission.permissions & 1) > 0)

                                            GROUP BY i.id";
                        }

                        countQuery += ") AS x";
                    }
                    else
                    {
                        // Generic searching on everything.
                        countQuery = $@"SELECT COUNT(*) 
                                        FROM (
                                            SELECT i.id
                                            FROM {tablePrefix}{WiserTableNames.WiserItem} i
                                            JOIN {WiserTableNames.WiserEntity} e ON e.name = i.entity_type AND e.show_in_search = 1

                                            {String.Format(versionJoinClause, "")}

                                            # Check permissions. Default permissions are everything enabled, so if the user has no role or the role has no permissions on this item, they are allowed everything.
	                                        LEFT JOIN {WiserTableNames.WiserUserRoles} user_role ON user_role.user_id = ?userId
	                                        LEFT JOIN {WiserTableNames.WiserPermission} permission ON permission.role_id = user_role.role_id AND permission.item_id = i.id

                                            WHERE i.title LIKE CONCAT(?search, '%')
                                            {versionWhereClause}
                                            AND (?entityType = '' OR i.entity_type = ?entityType)
                                            AND (permission.id IS NULL OR (permission.permissions & 1) > 0)

                                            UNION

                                            SELECT i.id
                                            FROM {tablePrefix}{WiserTableNames.WiserItemDetail} id
                                            JOIN {tablePrefix}{WiserTableNames.WiserItem} i ON i.id = id.item_id AND (?entityType = '' OR i.entity_type = ?entityType)
                                            JOIN {WiserTableNames.WiserEntity} e ON e.name = i.entity_type AND e.show_in_search = 1

                                            {String.Format(versionJoinClause, "")}

                                            # Check permissions. Default permissions are everything enabled, so if the user has no role or the role has no permissions on this item, they are allowed everything.
	                                        LEFT JOIN {WiserTableNames.WiserUserRoles} user_role ON user_role.user_id = ?userId
	                                        LEFT JOIN {WiserTableNames.WiserPermission} permission ON permission.role_id = user_role.role_id AND permission.item_id = i.id

                                            WHERE id.`value` LIKE CONCAT(?search, '%')
                                            {versionWhereClause}
                                            AND (permission.id IS NULL OR (permission.permissions & 1) > 0)
                                            GROUP BY i.id";

                        selectQuery = $@"SELECT 
	                                        i.id,
	                                        i.id AS encryptedId_encrypt_withdate,
                                            i.title,
	                                        i.entity_type AS entityType,
	                                        i.moduleId,
	                                        i.added_on AS addedOn,
	                                        i.added_by AS addedBy,
	                                        e.icon,
	                                        e.color,
	                                        '' AS moreInfo
                                        FROM {tablePrefix}{WiserTableNames.WiserItem} i
                                        JOIN {WiserTableNames.WiserEntity} e ON e.name = i.entity_type AND e.show_in_search = 1

                                        {String.Format(versionJoinClause, "")}

                                        # Check permissions. Default permissions are everything enabled, so if the user has no role or the role has no permissions on this item, they are allowed everything.
	                                    LEFT JOIN {WiserTableNames.WiserUserRoles} user_role ON user_role.user_id = ?userId
	                                    LEFT JOIN {WiserTableNames.WiserPermission} permission ON permission.role_id = user_role.role_id AND permission.item_id = i.id

                                        WHERE i.title LIKE CONCAT(?search, '%')
                                        {versionWhereClause}
                                        AND (?entityType = '' OR i.entity_type = ?entityType)
                                        AND (permission.id IS NULL OR (permission.permissions & 1) > 0)

                                        UNION

                                        SELECT
	                                        i.id,
	                                        i.id AS encryptedId_encrypt_withdate,
                                            i.title,
	                                        i.entity_type AS entityType,
	                                        i.moduleId,
	                                        i.added_on AS addedOn,
	                                        i.added_by AS addedby,
	                                        e.icon,
	                                        e.color,
	                                        '' AS moreInfo
                                        FROM {tablePrefix}{WiserTableNames.WiserItemDetail} id
                                        JOIN {tablePrefix}{WiserTableNames.WiserItem} i ON i.id = id.item_id AND (?entityType = '' OR i.entity_type = ?entityType)
                                        JOIN {WiserTableNames.WiserEntity} e ON e.name = i.entity_type AND e.show_in_search = 1

                                        {String.Format(versionJoinClause, "")}

                                        # Check permissions. Default permissions are everything enabled, so if the user has no role or the role has no permissions on this item, they are allowed everything.
	                                    LEFT JOIN {WiserTableNames.WiserUserRoles} user_role ON user_role.user_id = ?userId
	                                    LEFT JOIN {WiserTableNames.WiserPermission} permission ON permission.role_id = user_role.role_id AND permission.item_id = i.id

                                        WHERE id.`value` LIKE CONCAT(?search, '%')
                                        {versionWhereClause}
                                        AND (permission.id IS NULL OR (permission.permissions & 1) > 0)
                                        GROUP BY i.id";

                        if (removedFilter == null || removedFilter.Value == "1")
                        {
                            countQuery += $@"
                                                UNION

                                                SELECT i.id
                                                FROM {tablePrefix}{WiserTableNames.WiserItem}{WiserTableNames.ArchiveSuffix} i
                                                JOIN {WiserTableNames.WiserEntity} e ON e.name = i.entity_type AND e.show_in_search = 1

                                                {String.Format(versionJoinClause, WiserTableNames.ArchiveSuffix)}

                                                # Check permissions. Default permissions are everything enabled, so if the user has no role or the role has no permissions on this item, they are allowed everything.
	                                            LEFT JOIN {WiserTableNames.WiserUserRoles} user_role ON user_role.user_id = ?userId
	                                            LEFT JOIN {WiserTableNames.WiserPermission} permission ON permission.role_id = user_role.role_id AND permission.item_id = i.id

                                                WHERE i.title LIKE CONCAT(?search, '%')
                                                {versionWhereClause}
                                                AND (?entityType = '' OR i.entity_type = ?entityType)
                                                AND (permission.id IS NULL OR (permission.permissions & 1) > 0)

                                                UNION

                                                SELECT i.id
                                                FROM {tablePrefix}{WiserTableNames.WiserItemDetail}{WiserTableNames.ArchiveSuffix} id
                                                JOIN {tablePrefix}{WiserTableNames.WiserItem}{WiserTableNames.ArchiveSuffix} i ON i.id = id.item_id AND (?entityType = '' OR i.entity_type = ?entityType)
                                                JOIN {WiserTableNames.WiserEntity} e ON e.name = i.entity_type AND e.show_in_search = 1

                                                {String.Format(versionJoinClause, WiserTableNames.ArchiveSuffix)}

                                                # Check permissions. Default permissions are everything enabled, so if the user has no role or the role has no permissions on this item, they are allowed everything.
	                                            LEFT JOIN {WiserTableNames.WiserUserRoles} user_role ON user_role.user_id = ?userId
	                                            LEFT JOIN {WiserTableNames.WiserPermission} permission ON permission.role_id = user_role.role_id AND permission.item_id = i.id

                                                WHERE id.`value` LIKE CONCAT(?search, '%')
                                                {versionWhereClause}
                                                AND (permission.id IS NULL OR (permission.permissions & 1) > 0)
                                                GROUP BY i.id";

                            selectQuery = $@"
                                            UNION

                                            SELECT 
	                                            i.id,
	                                            i.id AS encryptedId_encrypt_withdate,
                                                i.title,
	                                            i.entity_type AS entityType,
	                                            i.moduleId,
	                                            i.added_on AS addedOn,
	                                            i.added_by AS addedBy,
	                                            e.icon,
	                                            e.color,
	                                            'Dit item is verwijderd' AS moreInfo
                                            FROM {tablePrefix}{WiserTableNames.WiserItem}{WiserTableNames.ArchiveSuffix} i
                                            JOIN {WiserTableNames.WiserEntity} e ON e.name = i.entity_type AND e.show_in_search = 1

                                            {String.Format(versionJoinClause, WiserTableNames.ArchiveSuffix)}

                                            # Check permissions. Default permissions are everything enabled, so if the user has no role or the role has no permissions on this item, they are allowed everything.
	                                        LEFT JOIN {WiserTableNames.WiserUserRoles} user_role ON user_role.user_id = ?userId
	                                        LEFT JOIN {WiserTableNames.WiserPermission} permission ON permission.role_id = user_role.role_id AND permission.item_id = i.id

                                            WHERE i.title LIKE CONCAT(?search, '%')
                                            {versionWhereClause}
                                            AND (?entityType = '' OR i.entity_type = ?entityType)
                                            AND (permission.id IS NULL OR (permission.permissions & 1) > 0)

                                            UNION

                                            SELECT
	                                            i.id,
	                                            i.id AS encryptedId_encrypt_withdate,
                                                i.title,
	                                            i.entity_type AS entityType,
	                                            i.moduleId,
	                                            i.added_on AS addedOn,
	                                            i.added_by AS addedBy,
	                                            e.icon,
	                                            e.color,
	                                            '' AS moreInfo
                                            FROM {tablePrefix}{WiserTableNames.WiserItemDetail}{WiserTableNames.ArchiveSuffix} id
                                            JOIN {tablePrefix}{WiserTableNames.WiserItem}{WiserTableNames.ArchiveSuffix} i ON i.id = id.item_id AND (?entityType = '' OR i.entity_type = ?entityType)
                                            JOIN {WiserTableNames.WiserEntity} e ON e.name = i.entity_type AND e.show_in_search = 1

                                            {String.Format(versionJoinClause, WiserTableNames.ArchiveSuffix)}

                                            # Check permissions. Default permissions are everything enabled, so if the user has no role or the role has no permissions on this item, they are allowed everything.
	                                        LEFT JOIN {WiserTableNames.WiserUserRoles} user_role ON user_role.user_id = ?userId
	                                        LEFT JOIN {WiserTableNames.WiserPermission} permission ON permission.role_id = user_role.role_id AND permission.item_id = i.id

                                            WHERE id.`value` LIKE CONCAT(?search, '%')
                                            {versionWhereClause}
                                            AND (permission.id IS NULL OR (permission.permissions & 1) > 0)
                                            GROUP BY i.id";
                        }

                        countQuery += ") AS x";

                        clientDatabaseConnection.AddParameter("search", genericFilter.Value);
                    }

                    clientDatabaseConnection.AddParameter("userId", userId);
                    (selectQuery, countQuery) = BuildGridQueries(options, selectQuery, countQuery, identity, "ORDER BY due_date DESC", tablePrefix: tablePrefix);

                    // Get the count, but only if this is not the first load.
                    clientDatabaseConnection.AddParameter("entityType", entityType.Equals("all", StringComparison.OrdinalIgnoreCase) ? "" : entityType);
                    if (options?.FirstLoad ?? true)
                    {
                        var countDataTable = await clientDatabaseConnection.GetAsync(countQuery);
                        results.TotalResults = Convert.ToInt32(countDataTable.Rows[0][0]);
                    }

                    // Get the data.
                    dataTable = await clientDatabaseConnection.GetAsync(selectQuery);
                    break;
                }
                case EntityGridModes.ItemDetailsGroup when String.IsNullOrWhiteSpace(fieldGroupName):
                    return new ServiceResult<GridSettingsAndDataModel>
                    {
                        StatusCode = HttpStatusCode.BadRequest,
                        ErrorMessage = "FieldGroupName is required for mode 6 (ItemDetailsGroup)"
                    };
                case EntityGridModes.ItemDetailsGroup:
                {
                    results.Columns.Add(new GridColumn {Field = "key", Title = "Naam"});
                    results.Columns.Add(new GridColumn {Field = "value", Title = "Waarde"});
                    if (String.IsNullOrWhiteSpace(results.LanguageCode))
                    {
                        results.Columns.Add(new GridColumn {Field = "languagecode", Title = "Taalcode", Width = "100px"});
                    }

                    results.Columns.Add(new GridColumn {Field = "command", Title = "&nbsp;", Width = "120px", Command = new List<string> {"destroy"}});

                    results.SchemaModel.Fields.Add("id", new FieldModel {Editable = false, Type = "number", Nullable = false});
                    results.SchemaModel.Fields.Add("key", new FieldModel {Editable = true, Type = "string", Nullable = false});
                    results.SchemaModel.Fields.Add("value", new FieldModel {Editable = true, Type = "string", Nullable = true});
                    results.SchemaModel.Fields.Add("languagecode", new FieldModel {Editable = true, Type = "string", Nullable = true});

                    hasPredefinedSchema = true;

                    clientDatabaseConnection.ClearParameters();
                    clientDatabaseConnection.AddParameter("itemId", itemId);
                    clientDatabaseConnection.AddParameter("groupName", fieldGroupName);
                    if (!String.IsNullOrWhiteSpace(results.LanguageCode))
                    {
                        options ??= new GridReadOptionsModel();
                        options.Filter ??= new GridFilterModel();
                        options.Filter.Filters ??= new List<GridFilterModel>();
                        options.Filter.Filters.RemoveAll(filter => filter.Field == "languageCode");
                        options.Filter.Filters.Add(new GridFilterModel
                        {
                            Field = "languageCode",
                            Value = results.LanguageCode,
                            Operator = "eq"
                        });
                    }

                    countQuery = $@"SELECT COUNT(*)
                                FROM {tablePrefix}{WiserTableNames.WiserItemDetail}
                                WHERE item_id = ?itemId
                                AND groupname = ?groupName
                                [if({{key_has_filter}}!)]AND `key` {{key_filter}}[endif]
                                [if({{value_has_filter}}!)]AND `value` {{value_filter}}[endif]
                                [if({{languageCode_has_filter}}!)]AND `language_code` {{languageCode_filter}}[endif]";

                    selectQuery = $@"SELECT
                                    id,
                                    language_code AS languageCode,
                                    `key`,
	                                CONCAT_WS('', `value`, long_value) AS `value`
                                FROM {tablePrefix}{WiserTableNames.WiserItemDetail}
                                WHERE item_id = ?itemId
                                AND groupname = ?groupName
                                [if({{key_has_filter}}!)]AND `key` {{key_filter}}[endif]
                                [if({{value_has_filter}}!)]AND `value` {{value_filter}}[endif]
                                [if({{languageCode_has_filter}}!)]AND `language_code` {{languageCode_filter}}[endif]
                                {{sort}}
                                {{limit}}";

                    (selectQuery, countQuery) = BuildGridQueries(options, selectQuery, countQuery, identity, "ORDER BY language_code ASC, `key` ASC", tablePrefix: tablePrefix);

                    // Get the count, but only if this is not the first load.
                    if (options?.FirstLoad ?? true)
                    {
                        var countDataTable = await clientDatabaseConnection.GetAsync(countQuery);
                        results.TotalResults = Convert.ToInt32(countDataTable.Rows[0][0]);
                    }

                    // Get the data.
                    dataTable = await clientDatabaseConnection.GetAsync(selectQuery);
                    break;
                }
                default:
                {
                    // Normal grid data.
                    var hasColumnsFromOptions = results.Columns.Any();
                    if (!hasColumnsFromOptions)
                    {
                        var filterable = new Dictionary<string, object> {{"extra", true}};
                        results.Columns.Add(new GridColumn {Selectable = true, Width = "55px"});
                        results.Columns.Add(new GridColumn {Field = "id", Title = "ID", Width = "80px", Filterable = filterable});
                        results.Columns.Add(new GridColumn {Field = "linkid", Title = "Koppel-ID", Width = "55px", Filterable = filterable});
                        results.Columns.Add(new GridColumn {Field = "entitytype", Title = "Type", Width = "100px", Filterable = filterable, Template = "#: window.dynamicItems.getEntityTypeFriendlyName(entitytype) #"});
                        results.Columns.Add(new GridColumn {Field = "publishedenvironment", Title = "Gepubliceerde omgeving", Width = "50px", Template = "<ins title='#: publishedenvironment #' class='icon-#: publishedenvironment #'></ins>"});
                        results.Columns.Add(new GridColumn {Field = "title", Title = "Naam", Filterable = filterable});
                    }

                    hasPredefinedSchema = true;
                    results.SchemaModel.Fields.Add("id", new FieldModel {Editable = false, Type = "number", Nullable = false});
                    results.SchemaModel.Fields.Add("encryptedid", new FieldModel {Editable = false, Type = "string", Nullable = false});
                    results.SchemaModel.Fields.Add("uniqueuuid", new FieldModel {Editable = false, Type = "string", Nullable = false});
                    results.SchemaModel.Fields.Add("entitytype", new FieldModel {Type = "string"});
                    results.SchemaModel.Fields.Add("publishedenvironment", new FieldModel {Type = "number"});
                    results.SchemaModel.Fields.Add("title", new FieldModel {Type = "string"});
                    results.SchemaModel.Fields.Add("linktypenumber", new FieldModel {Type = "number"});
                    results.SchemaModel.Fields.Add("linkid", new FieldModel {Type = "number", Editable = false});
                    results.SchemaModel.Fields.Add("addedon", new FieldModel {Type = "date", Editable = false});
                    results.SchemaModel.Fields.Add("addedby", new FieldModel {Type = "string", Editable = false});
                    results.SchemaModel.Fields.Add("changedon", new FieldModel {Type = "date", Editable = false});
                    results.SchemaModel.Fields.Add("changedby", new FieldModel {Type = "string", Editable = false});
                    results.SchemaModel.Fields.Add(WiserItemsService.LinkOrderingFieldName, new FieldModel {Type = "number", Nullable = false});

                    await itemsService.FixTreeViewOrderingAsync(moduleId, identity, encryptedId, linkTypeNumber);

                    var columnsQuery = $@"SELECT  
	                                        IF(p.property_name IS NULL OR p.property_name = '', p.display_name, p.property_name) AS field,
                                            p.display_name AS title,
                                            p.overview_width AS width,
                                            p.inputtype,
                                            p.options,
                                            p.readonly,
                                            p.data_query,
                                            p.depends_on_field,
                                            p.depends_on_action,
                                            p.link_type > 0 AS isLinkProperty,
                                            p.regex_validation,
                                            p.mandatory
                                        FROM {WiserTableNames.WiserEntityProperty} p 
                                        WHERE (p.entity_name = ?entityType OR (p.link_type > 0 AND p.link_type = ?linkTypeNumber))
                                        AND p.visible_in_overview = 1
                                        GROUP BY IF(p.property_name IS NULL OR p.property_name = '', p.display_name, p.property_name)
                                        ORDER BY p.ordering";
                    clientDatabaseConnection.ClearParameters();
                    clientDatabaseConnection.AddParameter("entityType", entityType);
                    clientDatabaseConnection.AddParameter("linkTypeNumber", linkTypeNumber);
                    var columnsDataTable = await clientDatabaseConnection.GetAsync(columnsQuery);
                    var reservedWordsArray = new[] { "abstract","arguments","await","boolean","break","byte","case","catch","char","class","const","continue","debugger","default","delete","do","double","else","enum","eval","export","extends","false","final","finally","float","for","function","goto","if","implements","import","in","instanceof","int","interface","let","long","native","new","null","package","private","protected","public","return","short","static","super","switch","synchronized","this","throw","throws","transient","true","try","typeof","var","void","volatile","while","with","yield" };

                    if (columnsDataTable.Rows.Count > 0)
                    {
                        foreach (DataRow dataRow in columnsDataTable.Rows)
                        {
                            var fieldName = dataRow.Field<string>("field").ToLowerInvariant().MakeJsonPropertyName();
                            if (reservedWordsArray.Contains(fieldName))
                            {
                                throw new Exception( $"{fieldName}(variable: fieldName) is a reserved Javascript keyword");
                            }

                            var field = new FieldModel
                            {
                                Editable = !Convert.ToBoolean(dataRow["readonly"]),
                                Validation = new ValidationSettingsModel
                                {
                                    Required = Convert.ToBoolean(dataRow["mandatory"])
                                }
                            };

                            var regexValidation = dataRow.Field<string>("regex_validation");
                            if (!String.IsNullOrWhiteSpace(regexValidation))
                            {
                                field.Validation.Pattern = regexValidation;
                            }

                            var column = new GridColumn
                            {
                                Field = fieldName,
                                Width = dataRow.IsNull("width") ? "" : $"{dataRow["width"]}px",
                                Title = dataRow.Field<string>("title"),
                                DataQuery = dataRow.Field<string>("data_query"),
                                IsLinkProperty = Convert.ToBoolean(dataRow["isLinkProperty"])
                            };

                            var inputType = dataRow.Field<string>("inputtype");
                            var fieldOptionsValue = dataRow.Field<string>("options");
                            var fieldOptions = new Dictionary<string, object>();

                            if (!String.IsNullOrWhiteSpace(fieldOptionsValue))
                            {
                                fieldOptions = JsonConvert.DeserializeObject<Dictionary<string, object>>(fieldOptionsValue);
                            }

                            fieldsInformation.Add((dataRow.Field<string>("field"), inputType, fieldOptions));

                            var addField = true;
                            switch (inputType)
                            {
                                case "checkbox":
                                    field.Type = "boolean";
                                    column.Editor = "booleanEditor";
                                    column.Template = $" # if ({fieldName} == true) {{ # Ja #}} else {{ # Nee # }} #";
                                    break;
                                case "numeric-input":
                                    field.Type = "number";
                                    break;
                                case "date-time picker":
                                    field.Type = "date";

                                    if (!fieldOptions.ContainsKey("type"))
                                    {
                                        column.Format = "{0:dd-MM-yyyy HH:mm:ss}";
                                    }
                                    else
                                    {
                                        switch (fieldOptions["type"])
                                        {
                                            case "date":
                                                column.Format = "{0:dd-MM-yyyy}";
                                                break;
                                            case "time":
                                                column.Editor = "timeEditor";
                                                column.Format = "{0:HH:mm:ss}";
                                                break;
                                            default:
                                                column.Editor = "dateTimeEditor";
                                                column.Format = "{0:dd-MM-yyyy HH:mm:ss}";
                                                break;
                                        }
                                    }

                                    break;
                                case "combobox":
                                    column.Template = $"#: ({fieldName}_input || {fieldName} || '') #";
                                    results.SchemaModel.Fields.Add($"{fieldName}_input", new FieldModel {Editable = false});

                                    if (!String.IsNullOrEmpty(dataRow.Field<string>("depends_on_field")) && dataRow.Field<string>("depends_on_action") == "refresh")
                                    {
                                        field.Editable = false;
                                        break;
                                    }

                                    var dataSource = column.DataItems ?? new List<DataSourceItemModel>();
                                    var fieldOptionsDataSource = fieldOptions.FirstOrDefault(x => x.Key.Equals("dataSource", StringComparison.OrdinalIgnoreCase));
                                    if (fieldOptionsDataSource.Value != null)
                                    {
                                        foreach (var value in (JArray) fieldOptionsDataSource.Value)
                                        {
                                            if (value is JObject valueAsObject)
                                            {
                                                dataSource.Add(new DataSourceItemModel
                                                {
                                                    Value = valueAsObject.Value<string>("id"),
                                                    Text = valueAsObject.Value<string>("name")
                                                });
                                            }
                                        }
                                    }
                                    else if (!String.IsNullOrWhiteSpace(column.DataQuery) && !column.DataQuery.Contains("{"))
                                    {
                                        var comboBoxDataTable = await clientDatabaseConnection.GetAsync(column.DataQuery);
                                        if (comboBoxDataTable.Rows.Count > 0)
                                        {
                                            foreach (DataRow dataSourceRow in comboBoxDataTable.Rows)
                                            {
                                                var text = dataSourceRow[comboBoxDataTable.Columns.Count > 1 ? 1 : 0]?.ToString();
                                                dataSource.Add(new DataSourceItemModel
                                                {
                                                    Value = dataSourceRow[0],
                                                    Text = text
                                                });
                                            }
                                        }
                                    }
                                    else if (fieldOptions.ContainsKey("entityType"))
                                    {
                                        var entityTypeForComboBoxData = fieldOptions["entityType"];
                                        var query = $"SELECT id, title FROM {WiserTableNames.WiserItem} WHERE entity_type = ?entityTypeForComboBoxData ORDER BY title ASC";
                                        clientDatabaseConnection.AddParameter("entityTypeForComboBoxData", entityTypeForComboBoxData);
                                        var entityTypeDataTable = await clientDatabaseConnection.GetAsync(query);
                                        if (entityTypeDataTable.Rows.Count > 0)
                                        {
                                            foreach (DataRow dataSourceRow in entityTypeDataTable.Rows)
                                            {
                                                var text = dataSourceRow[entityTypeDataTable.Columns.Count > 1 ? 1 : 0]?.ToString();
                                                dataSource.Add(new DataSourceItemModel
                                                {
                                                    Value = dataSourceRow[0],
                                                    Text = text
                                                });
                                            }
                                        }
                                    }

                                    column.Values = dataSource;
                                    break;
                                case "item-linker":
                                case "gpslocation":
                                case "action-button":
                                case "button":
                                case "imagecoords":
                                case "querybuilder":
                                case "file-upload":
                                case "sub-entities-grid":
                                    addField = false;
                                    break;
                            }

                            if (!addField)
                            {
                                continue;
                            }

                            if (!hasColumnsFromOptions)
                            {
                                results.Columns.Add(column);
                            }

                            results.SchemaModel.Fields.Add(fieldName, field);
                        }
                    }

                    if (!hasColumnsFromOptions)
                    {
                        results.Columns.Add(new GridColumn {Field = "addedon", Title = "Toegevoegd op", Width = "150px", Format = "{0:dd-MM-yyyy HH:mm:ss}", Hidden = true});
                        results.Columns.Add(new GridColumn {Field = "addedby", Title = "Toegevoegd door", Width = "100px", Hidden = true});
                        results.Columns.Add(new GridColumn {Field = "changedon", Title = "Gewijzigd op", Width = "150px", Format = "{0:dd-MM-yyyy HH:mm:ss}", Hidden = true});
                        results.Columns.Add(new GridColumn {Field = "changedby", Title = "Gewijzigd door", Width = "100px", Hidden = true});
                    }

                    // Build the queries.
                    clientDatabaseConnection.AddParameter("userId", userId);
                    clientDatabaseConnection.AddParameter("itemId", itemId);
                    clientDatabaseConnection.AddParameter("linkTypeNumber", linkTypeNumber);
                    clientDatabaseConnection.AddParameter("moduleId", moduleId);

                    switch (mode)
                    {
                        case EntityGridModes.LinkOverview:
                        {
                            countQuery = $@"SELECT SUM(x.count)
                                            FROM (
                                                # Count all items where the original_item_id is not 0 and only count one version of each item.
                                                # This is done in 2 queries, because that is a lot faster that counting everything in a single query.
                                                SELECT COUNT(DISTINCT i.id) AS count
                                                FROM {tablePrefix}{WiserTableNames.WiserItem} i

                                                {{filters}}

                                                {String.Format(versionJoinClause, "")}

                                                # Check permissions. Default permissions are everything enabled, so if the user has no role or the role has no permissions on this item, they are allowed everything.
	                                            LEFT JOIN {WiserTableNames.WiserUserRoles} user_role ON user_role.user_id = ?userId
	                                            LEFT JOIN {WiserTableNames.WiserPermission} permission ON permission.role_id = user_role.role_id AND permission.item_id = i.id

                                                WHERE i.entity_type = ?entityType
                                                AND i.published_environment >= 0
                                                AND i.original_item_id > 0
                                                {versionWhereClause}
                                                AND (permission.id IS NULL OR (permission.permissions & 1) > 0)
                                                AND i.id <> ?itemId
                                                [if({{hasWhere}}=1)]AND ({{where}})[endif]

                                                UNION ALL

                                                # Count all items with original_item_id = 0, which means those items have no other versions.
                                                SELECT COUNT(DISTINCT i.id) AS count
                                                FROM {tablePrefix}{WiserTableNames.WiserItem} i

                                                {{filters}}

                                                # Check permissions. Default permissions are everything enabled, so if the user has no role or the role has no permissions on this item, they are allowed everything.
	                                            LEFT JOIN {WiserTableNames.WiserUserRoles} user_role ON user_role.user_id = ?userId
	                                            LEFT JOIN {WiserTableNames.WiserPermission} permission ON permission.role_id = user_role.role_id AND permission.item_id = i.id

                                                WHERE i.entity_type = ?entityType
                                                AND i.published_environment >= 0
                                                AND i.original_item_id = 0
                                                AND (permission.id IS NULL OR (permission.permissions & 1) > 0)
                                                AND i.id <> ?itemId
                                                [if({{hasWhere}}=1)]AND ({{where}})[endif]
                                            ) AS x";

                            selectQuery = $@"SELECT
	                                            GROUP_CONCAT(CONCAT(id.`key`, '=', IFNULL(idt.`value`, id.`value`), '') SEPARATOR '~~~') AS `fields`,
                                                i.*
                                            FROM (
                                                # Sub query so that we can first limit the items, then get all fields of those remaining items and group by item.
                                                # If we don't do this, MySQL will first get all items to group them, then it will add the limit, which is a lot slower.
                                                SELECT 
	                                                i.id,
	                                                i.id AS encryptedId_encrypt_withdate,
                                                    i.original_item_id AS originalItemId,
	                                                i.title,
                                                    CASE i.published_environment
    	                                                WHEN 0 THEN 'onzichtbaar'
                                                        WHEN 1 THEN 'dev'
                                                        WHEN 2 THEN 'test'
                                                        WHEN 3 THEN 'acceptatie'
                                                        WHEN 4 THEN 'live'
                                                    END AS publishedEnvironment,
                                                    i.entity_type AS entityType,
                                                    i.added_on AS addedOn,
                                                    i.added_by AS addedBy,
                                                    i.changed_on AS changedOn,
                                                    i.changed_by AS changedBy,
                                                    i.parent_item_id AS parentItemId
                                                FROM {tablePrefix}{WiserTableNames.WiserItem} i

                                                {{filters}}
                                                {{joinsForSorting}}

                                                # Check permissions. Default permissions are everything enabled, so if the user has no role or the role has no permissions on this item, they are allowed everything.
	                                            LEFT JOIN {WiserTableNames.WiserUserRoles} user_role ON user_role.user_id = ?userId
	                                            LEFT JOIN {WiserTableNames.WiserPermission} permission ON permission.role_id = user_role.role_id AND permission.item_id = i.id
                                                
                                                WHERE i.entity_type = ?entityType
                                                AND i.published_environment >= 0
                                                AND (permission.id IS NULL OR (permission.permissions & 1) > 0)
                                                AND i.id <> ?itemId
                                                [if({{hasWhere}}=1)]AND ({{where}})[endif]

                                                GROUP BY IF(i.original_item_id > 0, i.original_item_id, i.id)
                                                {{sort}}
                                                {{limit}}
                                            ) AS i

                                            {String.Format(subQueryVersionJoinClause, "")}

                                            LEFT JOIN {WiserTableNames.WiserEntityProperty} p ON p.entity_name = i.entityType AND p.visible_in_overview = 1
                                            LEFT JOIN {tablePrefix}{WiserTableNames.WiserItemDetail} id ON id.item_id = i.id AND ((p.property_name IS NOT NULL AND p.property_name <> '' AND id.`key` = p.property_name) OR ((p.property_name IS NULL OR p.property_name = '') AND id.`key` = p.display_name))
                                            LEFT JOIN {tablePrefix}{WiserTableNames.WiserItemDetail} idt ON idt.item_id = i.id AND ((p.property_name IS NOT NULL AND p.property_name <> '' AND idt.`key` = CONCAT(p.property_name, '_input')) OR ((p.property_name IS NULL OR p.property_name = '') AND idt.`key` = CONCAT(p.display_name, '_input')))

                                            # We need to sort here again, otherwise the outer query will return a different ordering than the sub query.
                                            # But we also need to sort in the sub query, otherwise the limit will return the wrong set of rows.
                                            {{joinsForSorting}}

                                            WHERE TRUE
                                            {versionWhereClause}

                                            GROUP BY i.id
                                            {{sort}}";
                            break;
                        }
                        default:
                        {
                            if (useItemParentId)
                            {
                                countQuery = $@"SELECT COUNT(DISTINCT i.id)
                                                FROM {tablePrefix}{WiserTableNames.WiserItem} i
                                                {{filters}}

                                                {versionJoinClause}

                                                # Check permissions. Default permissions are everything enabled, so if the user has no role or the role has no permissions on this item, they are allowed everything.
	                                            LEFT JOIN {WiserTableNames.WiserUserRoles} user_role ON user_role.user_id = ?userId
	                                            LEFT JOIN {WiserTableNames.WiserPermission} permission ON permission.role_id = user_role.role_id AND permission.item_id = i.id

                                                WHERE i.parent_item_id = ?itemId
                                                {(String.IsNullOrEmpty(entityType) ? "" : "AND FIND_IN_SET(i.entity_type, ?entityType)")}
                                                {(moduleId <= 0 ? "" : "AND i.moduleid = ?moduleId")}
                                                {versionWhereClause}
                                                AND (permission.id IS NULL OR (permission.permissions & 1) > 0)
                                                [if({{hasWhere}}=1)]AND ({{where}})[endif]";

                                selectQuery = $@"SELECT
	                                                i.id,
	                                                i.id AS encryptedId_encrypt_withdate,
                                                    i.unique_uuid AS uniqueUuid,
                                                    CASE i.published_environment
    	                                                WHEN 0 THEN 'onzichtbaar'
                                                        WHEN 1 THEN 'dev'
                                                        WHEN 2 THEN 'test'
                                                        WHEN 3 THEN 'acceptatie'
                                                        WHEN 4 THEN 'live'
                                                    END AS publishedEnvironment,
                                                    i.title,
                                                    i.entity_type AS entityType,
	                                                GROUP_CONCAT(CONCAT(id.`key`, '=', id.`value`, '') SEPARATOR '~~~') AS fields,
                                                    ?linkTypeNumber AS linkTypeNumber,
                                                    0 AS linkId,
                                                    i.added_on AS addedOn,
                                                    i.added_by AS addedBy,
                                                    i.changed_on AS changedOn,
                                                    i.changed_by AS changedBy,
                                                    i.ordering AS `{WiserItemsService.LinkOrderingFieldName}`
                                                FROM {tablePrefix}{WiserTableNames.WiserItem} i

                                                {{filters}}
                                                {{joinsForSorting}}

                                                {versionJoinClause}

                                                # Check permissions. Default permissions are everything enabled, so if the user has no role or the role has no permissions on this item, they are allowed everything.
	                                            LEFT JOIN {WiserTableNames.WiserUserRoles} user_role ON user_role.user_id = ?userId
	                                            LEFT JOIN {WiserTableNames.WiserPermission} permission ON permission.role_id = user_role.role_id AND permission.item_id = i.id

                                                LEFT JOIN {WiserTableNames.WiserEntityProperty} p ON p.entity_name = i.entity_type AND p.visible_in_overview = 1
                                                LEFT JOIN {tablePrefix}{WiserTableNames.WiserItemDetail} id ON id.item_id = i.id AND ((p.property_name IS NOT NULL AND p.property_name <> '' AND id.`key` IN(p.property_name, CONCAT(p.property_name, '_input'))) OR ((p.property_name IS NULL OR p.property_name = '') AND id.`key` IN(p.display_name, CONCAT(p.property_name, '_input'))))

                                                WHERE i.parent_item_id = ?itemId
                                                {(String.IsNullOrEmpty(entityType) ? "" : "AND FIND_IN_SET(i.entity_type, ?entityType)")}
                                                {(moduleId <= 0 ? "" : "AND i.moduleid = ?moduleId")}
                                                {versionWhereClause}
                                                AND (permission.id IS NULL OR (permission.permissions & 1) > 0)
                                                [if({{hasWhere}}=1)]AND ({{where}})[endif]
                                                GROUP BY i.id
                                                {{sort}}
                                                {{limit}}";
                            }
                            else
                            {
                                countQuery = $@"SELECT COUNT(DISTINCT i.id)
                                                FROM {linkTablePrefix}{WiserTableNames.WiserItemLink} il
                                                JOIN {tablePrefix}{WiserTableNames.WiserItem} i ON i.id = il.{(currentItemIsSourceId ? "destination_item_id" : "item_id")} {(String.IsNullOrEmpty(entityType) ? "" : "AND FIND_IN_SET(i.entity_type, ?entityType)")} {(moduleId <= 0 ? "" : "AND i.moduleid = ?moduleId")}
                                                {{filters}}

                                                {String.Format(versionJoinClause, "")}

                                                # Check permissions. Default permissions are everything enabled, so if the user has no role or the role has no permissions on this item, they are allowed everything.
	                                            LEFT JOIN {WiserTableNames.WiserUserRoles} user_role ON user_role.user_id = ?userId
	                                            LEFT JOIN {WiserTableNames.WiserPermission} permission ON permission.role_id = user_role.role_id AND permission.item_id = i.id

                                                WHERE il.{(currentItemIsSourceId ? "item_id" : "destination_item_id")} = ?itemId
                                                {versionWhereClause}
                                                AND (permission.id IS NULL OR (permission.permissions & 1) > 0)
                                                {(linkTypeNumber <= 0 ? "" : "AND il.type = ?linkTypeNumber")}
                                                [if({{hasWhere}}=1)]AND ({{where}})[endif]";

                                selectQuery = $@"SELECT
	                                                i.id,
	                                                i.id AS encryptedId_encrypt_withdate,
                                                    i.unique_uuid AS uniqueUuid,
                                                    CASE i.published_environment
    	                                                WHEN 0 THEN 'onzichtbaar'
                                                        WHEN 1 THEN 'dev'
                                                        WHEN 2 THEN 'test'
                                                        WHEN 3 THEN 'acceptatie'
                                                        WHEN 4 THEN 'live'
                                                    END AS publishedEnvironment,
                                                    i.title,
                                                    i.entity_type AS entityType,
	                                                GROUP_CONCAT(CONCAT(id.`key`, '=', id.`value`, '') SEPARATOR '~~~') AS fields,
                                                    il.type AS linkTypeNumber,
                                                    il.id AS linkId,
                                                    i.added_on AS addedOn,
                                                    i.added_by AS addedBy,
                                                    i.changed_on AS changedOn,
                                                    i.changed_by AS changedBy,
                                                    il.ordering AS `{WiserItemsService.LinkOrderingFieldName}`
                                                FROM {linkTablePrefix}{WiserTableNames.WiserItemLink} il
                                                JOIN {tablePrefix}{WiserTableNames.WiserItem} i ON i.id = il.{(currentItemIsSourceId ? "destination_item_id" : "item_id")} {(String.IsNullOrEmpty(entityType) ? "" : "AND FIND_IN_SET(i.entity_type, ?entityType)")} {(moduleId <= 0 ? "" : "AND i.moduleid = ?moduleId")}

                                                {{filters}}
                                                {{joinsForSorting}}

                                                {String.Format(versionJoinClause, "")}

                                                # Check permissions. Default permissions are everything enabled, so if the user has no role or the role has no permissions on this item, they are allowed everything.
	                                            LEFT JOIN {WiserTableNames.WiserUserRoles} user_role ON user_role.user_id = ?userId
	                                            LEFT JOIN {WiserTableNames.WiserPermission} permission ON permission.role_id = user_role.role_id AND permission.item_id = i.id

                                                LEFT JOIN {WiserTableNames.WiserEntityProperty} p ON p.entity_name = i.entity_type AND p.visible_in_overview = 1
                                                LEFT JOIN {tablePrefix}{WiserTableNames.WiserItemDetail} id ON id.item_id = il.item_id AND ((p.property_name IS NOT NULL AND p.property_name <> '' AND id.`key` IN(p.property_name, CONCAT(p.property_name, '_input'))) OR ((p.property_name IS NULL OR p.property_name = '') AND id.`key` IN(p.display_name, CONCAT(p.property_name, '_input'))))

                                                WHERE il.{(currentItemIsSourceId ? "item_id" : "destination_item_id")} = ?itemId
                                                {versionWhereClause}
                                                AND (permission.id IS NULL OR (permission.permissions & 1) > 0)
                                                {(linkTypeNumber <= 0 ? "" : "AND il.type = ?linkTypeNumber")}
                                                [if({{hasWhere}}=1)]AND ({{where}})[endif]
                                                GROUP BY i.id

                                                UNION

                                                SELECT
	                                                i.id,
	                                                i.id AS encryptedId_encrypt_withdate,
                                                    i.unique_uuid AS uniqueUuid,
                                                    CASE i.published_environment
    	                                                WHEN 0 THEN 'onzichtbaar'
                                                        WHEN 1 THEN 'dev'
                                                        WHEN 2 THEN 'test'
                                                        WHEN 3 THEN 'acceptatie'
                                                        WHEN 4 THEN 'live'
                                                    END AS publishedEnvironment,
                                                    i.title,
                                                    i.entity_type AS entityType,
	                                                GROUP_CONCAT(CONCAT(id.`key`, '=', id.`value`, '') SEPARATOR '~~~') AS fields,
                                                    il.type AS linkTypeNumber,
                                                    il.id AS linkId,
                                                    i.added_on AS addedOn,
                                                    i.added_by AS addedBy,
                                                    i.changed_on AS changedOn,
                                                    i.changed_by AS changedBy,
                                                    il.ordering AS `{WiserItemsService.LinkOrderingFieldName}`
                                                FROM {linkTablePrefix}{WiserTableNames.WiserItemLink} il
                                                JOIN {tablePrefix}{WiserTableNames.WiserItem} i ON i.id = il.{(currentItemIsSourceId ? "destination_item_id" : "item_id")} {(String.IsNullOrEmpty(entityType) ? "" : "AND FIND_IN_SET(i.entity_type, ?entityType)")} {(moduleId <= 0 ? "" : "AND i.moduleid = ?moduleId")}

                                                {{filters}}
                                                {{joinsForSorting}}

                                                {String.Format(versionJoinClause, "")}

                                                # Check permissions. Default permissions are everything enabled, so if the user has no role or the role has no permissions on this item, they are allowed everything.
	                                            LEFT JOIN {WiserTableNames.WiserUserRoles} user_role ON user_role.user_id = ?userId
	                                            LEFT JOIN {WiserTableNames.WiserPermission} permission ON permission.role_id = user_role.role_id AND permission.item_id = i.id

                                                JOIN {WiserTableNames.WiserEntityProperty} p ON p.link_type = il.type AND p.visible_in_overview = 1
                                                JOIN {linkTablePrefix}{WiserTableNames.WiserItemLinkDetail} id ON id.itemlink_id = il.id AND ((p.property_name IS NOT NULL AND p.property_name <> '' AND id.`key` IN(p.property_name, CONCAT(p.property_name, '_input'))) OR ((p.property_name IS NULL OR p.property_name = '') AND id.`key` IN(p.display_name, CONCAT(p.property_name, '_input'))))

                                                WHERE il.{(currentItemIsSourceId ? "item_id" : "destination_item_id")} = ?itemId
                                                {versionWhereClause}
                                                AND (permission.id IS NULL OR (permission.permissions & 1) > 0)
                                                {(linkTypeNumber <= 0 ? "" : "AND il.type = ?linkTypeNumber")}
                                                [if({{hasWhere}}=1)]AND ({{where}})[endif]

                                                GROUP BY i.id

                                                {{sort}}
                                                {{limit}}";
                            }

                            break;
                        }
                    }

                    var defaultSorting = mode == EntityGridModes.LinkOverview ? "ORDER BY i.title ASC" : $"ORDER BY {WiserItemsService.LinkOrderingFieldName} ASC, title ASC";
                    (selectQuery, countQuery) = BuildGridQueries(options, selectQuery, countQuery, identity, defaultSorting, tablePrefix: tablePrefix);

                    // Get the count, but only if this is not the first load.
                    if (options?.FirstLoad ?? true)
                    {
                        var countDataTable = await clientDatabaseConnection.GetAsync(countQuery);
                        results.TotalResults = Convert.ToInt32(countDataTable.Rows[0][0]);
                    }

                    // Get the actual data for the grid.
                    dataTable = await clientDatabaseConnection.GetAsync(selectQuery);
                    break;
                }
            }

            if (!hasPredefinedSchema)
            {
                BuildGridSchema(dataTable, results, !forceAddColumns && results.Columns != null && results.Columns.Any());
            }

            if (dataTable == null || dataTable.Rows.Count == 0)
            {
                return new ServiceResult<GridSettingsAndDataModel>(results);
            }

            object HandleFieldValue(string fieldName, object value)
            {
                var field = fieldsInformation.FirstOrDefault(f => f.Field.Equals(fieldName, StringComparison.OrdinalIgnoreCase));

                if (field.InputType != null)
                {
                    switch (field.InputType.ToLowerInvariant())
                    {
                        case "secure-input":
                            if (String.IsNullOrWhiteSpace(value?.ToString()))
                            {
                                break;
                            }

                            var securityMethod = "JCL_SHA512";

                            if (field.Options.ContainsKey(WiserItemsService.SecurityMethodKey))
                            {
                                securityMethod = field.Options[WiserItemsService.SecurityMethodKey]?.ToString()?.ToUpperInvariant();
                            }

                            var securityKey = "";
                            if (securityMethod.InList("JCL_AES", "AES"))
                            {
                                if (field.Options.ContainsKey(WiserItemsService.SecurityKeyKey))
                                {
                                    securityKey = field.Options[WiserItemsService.SecurityKeyKey]?.ToString();
                                }

                                if (String.IsNullOrEmpty(securityKey))
                                {
                                    securityKey = encryptionKey;
                                }
                            }

                            switch (securityMethod)
                            {
                                case "JCL_AES":
                                    try
                                    {
                                        value = value.ToString().DecryptWithAesWithSalt(securityKey);
                                    }
                                    catch (Exception exception)
                                    {
                                        logger.LogError($"Error while trying to decrypt value '{value}' for field '{field.Field}': {exception}");
                                    }

                                    break;
                                case "AES":
                                    try
                                    {
                                        value = value.ToString().DecryptWithAes(securityKey);
                                    }
                                    catch (Exception exception)
                                    {
                                        logger.LogError($"Error while trying to decrypt value '{value}' for field '{field.Field}': {exception}");
                                    }

                                    break;
                            }

                            break;
                    }
                }

                return value;
            }

            if (!dataTable.Columns.Contains("fields"))
            {
                // Build the results dictionary.
                foreach (DataRow dataRow in dataTable.Rows)
                {
                    var rowData = new Dictionary<string, object>();
                    results.Data.Add(rowData);

                    foreach (DataColumn dataColumn in dataTable.Columns)
                    {
                        var columnName = dataColumn.ColumnName.ToLowerInvariant().Replace("_encrypt_withdate", "").Replace("_encrypt", "").Replace("_hide", "").MakeJsonPropertyName();

                        if (dataColumn.ColumnName.Contains("_encrypt", StringComparison.OrdinalIgnoreCase)
                            || dataColumn.ColumnName.Contains("_encrypt_hide", StringComparison.OrdinalIgnoreCase)
                            || dataColumn.ColumnName.Contains("_encrypt_withdate", StringComparison.OrdinalIgnoreCase)
                            || dataColumn.ColumnName.Contains("_encrypt_withdate_hide", StringComparison.OrdinalIgnoreCase))
                        {
                            var value = dataRow.IsNull(dataColumn.ColumnName) ? "" : dataRow[dataColumn.ColumnName].ToString();
                            rowData[columnName] = wiserTenantsService.EncryptValue(value, tenant.ModelObject);
                        }
                        else if (dataColumn.ColumnName.Contains("_decrypt", StringComparison.OrdinalIgnoreCase)
                                 || dataColumn.ColumnName.Contains("_decrypt_hide", StringComparison.OrdinalIgnoreCase)
                                 || dataColumn.ColumnName.Contains("_decrypt_withdate", StringComparison.OrdinalIgnoreCase)
                                 || dataColumn.ColumnName.Contains("_decrypt_withdate_hide", StringComparison.OrdinalIgnoreCase))
                        {
                            var value = dataRow.IsNull(dataColumn.ColumnName) ? "" : dataRow[dataColumn.ColumnName].ToString();
                            rowData[columnName] = wiserTenantsService.DecryptValue<string>(value, tenant.ModelObject);
                        }
                        else if (dataColumn.ColumnName.Contains("_normalencrypt", StringComparison.OrdinalIgnoreCase)
                                 || dataColumn.ColumnName.Contains("_normalencrypt_hide", StringComparison.OrdinalIgnoreCase))
                        {
                            var value = dataRow.IsNull(dataColumn.ColumnName) ? "" : dataRow[dataColumn.ColumnName].ToString();
                            rowData[columnName] = Uri.EscapeDataString(value.EncryptWithAes());
                        }
                        else if (dataColumn.ColumnName.Contains("_normaldecrypt", StringComparison.OrdinalIgnoreCase)
                                 || dataColumn.ColumnName.Contains("_normaldecrypt_hide", StringComparison.OrdinalIgnoreCase))
                        {
                            var value = dataRow.IsNull(dataColumn.ColumnName) ? "" : dataRow[dataColumn.ColumnName].ToString();
                            rowData[columnName] = Uri.UnescapeDataString(value).DecryptWithAes();
                        }
                        else
                        {
                            var value = dataRow.IsNull(dataColumn.ColumnName) ? "" : dataRow[dataColumn.ColumnName];
                            rowData[columnName] = HandleFieldValue(dataColumn.ColumnName, value);
                        }
                    }
                }
            }
            else
            {
                // Build the results dictionary.
                foreach (DataRow dataRow in dataTable.Rows)
                {
                    var id = Convert.ToUInt64(dataRow["id"]);
                    var rowData = results.Data.FirstOrDefault(r => (ulong) r["id"] == id);
                    if (rowData == null)
                    {
                        rowData = new Dictionary<string, object>();
                        results.Data.Add(rowData);
                    }

                    foreach (DataColumn dataColumn in dataTable.Columns)
                    {
                        var columnName = dataColumn.ColumnName.ToLowerInvariant().Replace("_encrypt_withdate", "").Replace("_encrypt", "").Replace("_hide", "").MakeJsonPropertyName();

                        if (columnName.Equals("fields", StringComparison.OrdinalIgnoreCase))
                        {
                            var fields = dataRow.Field<string>("fields");
                            if (String.IsNullOrWhiteSpace(fields))
                            {
                                continue;
                            }

                            var fieldsList = fields.Split(new[] {"~~~"}, StringSplitOptions.RemoveEmptyEntries).Select(f =>
                            {
                                var value = f.Split(new[] {'='}, 2);
                                return new Tuple<string, string>(value.Length > 0 ? value[0] : "", value.Length > 1 ? value[1] : "");
                            });

                            foreach (var (propertyName, propertyValue) in fieldsList)
                            {
                                var name = propertyName?.ToLowerInvariant().MakeJsonPropertyName();
                                if (String.IsNullOrWhiteSpace(name) || rowData.ContainsKey(name))
                                {
                                    continue;
                                }

                                var field = results.SchemaModel.Fields.FirstOrDefault(f => f.Key == name).Value;
                                if (field == null)
                                {
                                    rowData.Add(name, propertyValue);
                                }
                                else
                                {
                                    switch (field.Type)
                                    {
                                        case "boolean":
                                            rowData.Add(name, propertyValue.Equals("true", StringComparison.OrdinalIgnoreCase) || propertyValue.Equals("1", StringComparison.Ordinal));
                                            break;
                                        case "number":
                                            if (UInt64.TryParse(propertyValue, out var longValue))
                                            {
                                                rowData.Add(name, longValue);
                                            }
                                            else if (Decimal.TryParse(propertyValue.Replace(",", "."), NumberStyles.Any, new CultureInfo("en-US"), out var decimalValue))
                                            {
                                                rowData.Add(name, decimalValue);
                                            }
                                            else
                                            {
                                                rowData.Add(name, propertyValue);
                                            }

                                            break;
                                        default:
                                            rowData.Add(name, HandleFieldValue(name, propertyValue));
                                            break;
                                    }
                                }
                            }

                        }
                        else if (dataColumn.ColumnName.Contains("_encrypt", StringComparison.OrdinalIgnoreCase)
                                 || dataColumn.ColumnName.Contains("_encrypt_hide", StringComparison.OrdinalIgnoreCase)
                                 || dataColumn.ColumnName.Contains("_encrypt_withdate", StringComparison.OrdinalIgnoreCase)
                                 || dataColumn.ColumnName.Contains("_encrypt_withdate_hide", StringComparison.OrdinalIgnoreCase))
                        {
                            var value = dataRow.IsNull(dataColumn.ColumnName) ? "" : dataRow[dataColumn.ColumnName].ToString();
                            rowData[columnName] = wiserTenantsService.EncryptValue(value, tenant.ModelObject);
                        }
                        else if (dataColumn.ColumnName.Contains("_decrypt", StringComparison.OrdinalIgnoreCase)
                                 || dataColumn.ColumnName.Contains("_decrypt_hide", StringComparison.OrdinalIgnoreCase)
                                 || dataColumn.ColumnName.Contains("_decrypt_withdate", StringComparison.OrdinalIgnoreCase)
                                 || dataColumn.ColumnName.Contains("_decrypt_withdate_hide", StringComparison.OrdinalIgnoreCase))
                        {
                            var value = dataRow.IsNull(dataColumn.ColumnName) ? "" : dataRow[dataColumn.ColumnName].ToString();
                            rowData[columnName] = wiserTenantsService.DecryptValue<string>(value, tenant.ModelObject);
                        }
                        else if (dataColumn.ColumnName.Contains("_normalencrypt", StringComparison.OrdinalIgnoreCase)
                                 || dataColumn.ColumnName.Contains("_normalencrypt_hide", StringComparison.OrdinalIgnoreCase))
                        {
                            var value = dataRow.IsNull(dataColumn.ColumnName) ? "" : dataRow[dataColumn.ColumnName].ToString();
                            rowData[columnName] = Uri.EscapeDataString(value.EncryptWithAes());
                        }
                        else if (dataColumn.ColumnName.Contains("_normaldecrypt", StringComparison.OrdinalIgnoreCase)
                                 || dataColumn.ColumnName.Contains("_normaldecrypt_hide", StringComparison.OrdinalIgnoreCase))
                        {
                            var value = dataRow.IsNull(dataColumn.ColumnName) ? "" : dataRow[dataColumn.ColumnName].ToString();
                            rowData[columnName] = Uri.UnescapeDataString(value).DecryptWithAes();
                        }
                        else
                        {
                            var value = dataRow.IsNull(dataColumn.ColumnName) ? "" : dataRow[dataColumn.ColumnName];
                            rowData[columnName] = HandleFieldValue(dataColumn.ColumnName, value);
                        }
                    }
                }
            }


            if (extraJavascript.Length > 0)
            {
                results.ExtraJavascript = extraJavascript.ToString();
            }

            return new ServiceResult<GridSettingsAndDataModel>(results);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<GridSettingsAndDataModel>> GetOverviewGridDataAsync(int moduleId, GridReadOptionsModel options, ClaimsIdentity identity, bool isForExport = false)
        {
            if (moduleId <= 0)
            {
                throw new ArgumentNullException(nameof(moduleId));
            }

            if (isForExport)
            {
                // Timeout of 4 hours for exports.
                clientDatabaseConnection.SetCommandTimeout(apiSettings.SqlCommandTimeoutForExportsAndLongQueries);
            }

            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            var tenant = await wiserTenantsService.GetSingleAsync(identity);

            // Get module settings.
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("id", moduleId);
            var query = $"SELECT custom_query, count_query, options FROM {WiserTableNames.WiserModule} WHERE id = ?id";
            var dataTable = await clientDatabaseConnection.GetAsync(query);

            if (dataTable.Rows.Count == 0)
            {
                return new ServiceResult<GridSettingsAndDataModel>
                {
                    StatusCode = HttpStatusCode.NotFound,
                    ErrorMessage = $"No grid data available for module {moduleId}"
                };
            }

            // Build the schema model for the Kendo UI grid.
            var firstRow = dataTable.Rows[0];
            var moduleSettings = firstRow.Field<string>("options");
            var moduleSettingsModel = new GridViewSettingsModel();
            var results = new GridSettingsAndDataModel();
            if (!String.IsNullOrWhiteSpace(moduleSettings))
            {
                moduleSettingsModel = JsonConvert.DeserializeObject<GridViewSettingsModel>(moduleSettings);
                results = moduleSettingsModel!.GridViewSettings;
            }

            if (results.PageSize <= 0)
            {
                results.PageSize = 100;
            }

            // Build the queries.
            var selectQuery = firstRow.Field<string>("custom_query");
            if (String.IsNullOrWhiteSpace(selectQuery))
            {
                return new ServiceResult<GridSettingsAndDataModel>(results);
            }

            var countQuery = firstRow.Field<string>("count_query");

            // If the count query is empty and the select query contains a limit, build a count query based on the select query without the limit and sort.
            if (String.IsNullOrWhiteSpace(countQuery) && !String.IsNullOrWhiteSpace(selectQuery) && selectQuery.Contains("{limit}", StringComparison.OrdinalIgnoreCase))
            {
                countQuery = $@"SELECT COUNT(*) FROM (
                                    {selectQuery.Replace("{limit}", "", StringComparison.OrdinalIgnoreCase).Replace("{sort}", "", StringComparison.OrdinalIgnoreCase).Trim(';')}
                                ) AS x";
            }

            var userId = IdentityHelpers.GetWiserUserId(identity);
            clientDatabaseConnection.AddParameter("userId", userId);
            (selectQuery, countQuery) = BuildGridQueries(options, selectQuery, countQuery, identity, null, moduleSettingsModel.FieldMappings);

            // Get the count, but only if this is not the first load.
            if (!results.ClientSidePaging && !String.IsNullOrWhiteSpace(countQuery) && (options?.FirstLoad ?? true))
            {
                var countDataTable = await clientDatabaseConnection.GetAsync(countQuery);
                if (countDataTable.Rows.Count > 0 && !countDataTable.Rows[0].IsNull(0))
                {
                    results.TotalResults = Convert.ToInt32(countDataTable.Rows[0][0]);
                }
            }

            // Get the actual data for the grid.
            dataTable = await clientDatabaseConnection.GetAsync(selectQuery);

            BuildGridSchema(dataTable, results, results.Columns != null && results.Columns.Any());
            FillGridData(dataTable, results, identity, IdentityHelpers.IsTestEnvironment(identity), tenant.ModelObject);

            if (results.ClientSidePaging || String.IsNullOrWhiteSpace(countQuery))
            {
                results.TotalResults = dataTable.Rows.Count;
            }

            return new ServiceResult<GridSettingsAndDataModel>(results);
        }

        private (string selectQuery, string countQuery) BuildGridQueries(GridReadOptionsModel options,
            string selectQuery,
            string countQuery,
            ClaimsIdentity identity,
            string defaultSort = null,
            List<FieldMapModel> fieldMappings = null,
            string tablePrefix = null)
        {
            tablePrefix ??= "";

            // Note that this function contains ' ?? ""' after every Replace. This is because that function returns null if the input string is an empty string, which would cause lots of problems in the rest of the code.
            fieldMappings ??= new List<FieldMapModel>();

            selectQuery = apiReplacementsService.DoIdentityReplacements(selectQuery ?? "", identity, true);
            countQuery = apiReplacementsService.DoIdentityReplacements(countQuery ?? "", identity, true);

            if (options == null)
            {
                selectQuery = selectQuery.Replace("{limit}", "", StringComparison.OrdinalIgnoreCase).Replace("{sort}", defaultSort, StringComparison.OrdinalIgnoreCase).Replace("{filters}", "", StringComparison.OrdinalIgnoreCase) ?? "";
            }
            else
            {
                var whereClause = new List<(string, List<string>)>();

                int AddFiltersToQuery(int counter, GridFilterModel filter, StringBuilder filtersQuery, (string, List<string>) subWhereClause)
                {
                    if (String.IsNullOrEmpty(filter.Field))
                    {
                        var newSubWhereClause = (filter.Logic, new List<string>());
                        whereClause.Add(newSubWhereClause);
                        return filter.Filters?.Aggregate(counter, (current, subFilter) => AddFiltersToQuery(current, subFilter, filtersQuery, newSubWhereClause)) ?? counter;
                    }

                    if (String.IsNullOrWhiteSpace(filter.Operator))
                    {
                        filter.Operator = "startswith";
                    }

                    counter++;

                    // Old way of filtering:
                    selectQuery = selectQuery.Replace($"{{{filter.Field.ToMySqlSafeValue(false)}_{filter.Operator.ToMySqlSafeValue(false)}}}", filter.Value.ToMySqlSafeValue(false), StringComparison.OrdinalIgnoreCase) ?? "";
                    countQuery = countQuery.Replace($"{{{filter.Field.ToMySqlSafeValue(false)}_{filter.Operator.ToMySqlSafeValue(false)}}}", filter.Value.ToMySqlSafeValue(false), StringComparison.OrdinalIgnoreCase) ?? "";

                    selectQuery = selectQuery.Replace($"{{{filter.Field.ToMySqlSafeValue(false)}}}", filter.Value.ToMySqlSafeValue(false), StringComparison.OrdinalIgnoreCase) ?? "";
                    countQuery = countQuery.Replace($"{{{filter.Field.ToMySqlSafeValue(false)}}}", filter.Value.ToMySqlSafeValue(false), StringComparison.OrdinalIgnoreCase) ?? "";

                    // New way of filtering.
                    string @operator;
                    var filterValue = filter.Value;
                    object value = filter.Value;
                    // Column names are used as variables in KendoUI, but dashes don't work for that, so we replace them with a double underscore.
                    var fieldName = filter.Field.UnmakeJsonPropertyName();
                    var parameterName = $"value{counter}";
                    var parameterInWhere = $"?{parameterName}";
                    var addParameter = true;
                    var valueSelector = $"`idv{counter}`.`value`";
                    var longValueSelector = $"`idv{counter}`.`long_value`";
                    var itemTableAlias = "i";

                    var fieldMap = fieldMappings.FirstOrDefault(f => f.Field.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
                    if (!String.IsNullOrWhiteSpace(fieldMap?.Property))
                    {
                        fieldName = fieldMap.Property;
                    }

                    if (!String.IsNullOrWhiteSpace(fieldMap?.ItemTableAlias))
                    {
                        itemTableAlias = fieldMap.ItemTableAlias;
                    }

                    fieldName = fieldName.ToMySqlSafeValue(false);
                    var whereColumnSelector = $"`{itemTableAlias}`.`{fieldName}`";

                    if ("true".Equals(filterValue, StringComparison.OrdinalIgnoreCase))
                    {
                        @operator = ">";
                        value = 0;
                    }
                    else if ("false".Equals(filterValue, StringComparison.OrdinalIgnoreCase))
                    {
                        @operator = "<=";
                        value = 0;
                    }
                    else
                    {
                        if (((fieldMap != null && fieldMap.AddToWhereInsteadOfJoin) || ItemColumns.Any(c => c.Equals(fieldName, StringComparison.OrdinalIgnoreCase))) && Decimal.TryParse(filter.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedDecimal))
                        {
                            value = parsedDecimal;
                        }
                        else if (DateTime.TryParse(filter.Value, out var parsedDate))
                        {
                            value = parsedDate;
                            parameterInWhere = $"DATE({parameterInWhere})";
                            valueSelector = $"DATE({valueSelector})";
                            longValueSelector = $"DATE({longValueSelector})";
                            whereColumnSelector = $"DATE({whereColumnSelector})";
                        }

                        switch (filter.Operator.ToLowerInvariant())
                        {
                            case "eq":
                            {
                                @operator = "=";
                                break;
                            }

                            case "neq":
                            {
                                @operator = "<>";
                                break;
                            }

                            case "startswith":
                            {
                                @operator = "LIKE";
                                parameterInWhere = $"CONCAT({parameterInWhere}, '%')";
                                break;
                            }

                            case "contains":
                            {
                                @operator = "LIKE";
                                parameterInWhere = $"CONCAT('%', {parameterInWhere}, '%')";
                                break;
                            }

                            case "doesnotcontain":
                            {
                                @operator = "NOT LIKE";
                                parameterInWhere = $"CONCAT('%', {parameterInWhere}, '%')";
                                break;
                            }

                            case "endswith":
                            {
                                @operator = "LIKE";
                                parameterInWhere = $"CONCAT('%', {parameterInWhere})";
                                break;
                            }

                            case "isnull":
                            {
                                @operator = "IS NULL";
                                value = "";
                                parameterInWhere = "";
                                addParameter = false;
                                break;
                            }

                            case "isnotnull":
                            {
                                @operator = "IS NOT NULL";
                                value = "";
                                parameterInWhere = "";
                                addParameter = false;
                                break;
                            }

                            case "isempty":
                            {
                                @operator = "=";
                                value = "";
                                break;
                            }

                            case "isnotempty":
                            {
                                @operator = "<>";
                                value = "";
                                break;
                            }

                            case "lt":
                            {
                                @operator = "<";
                                break;
                            }

                            case "gt":
                            {
                                @operator = ">";
                                break;
                            }

                            case "lte":
                            {
                                @operator = "<=";
                                break;
                            }

                            case "gte":
                            {
                                @operator = ">=";
                                break;
                            }

                            default:
                            {
                                @operator = "=";
                                break;
                            }
                        }
                    }

                    if (addParameter)
                    {
                        clientDatabaseConnection.AddParameter(parameterName, value);
                    }

                    if (fieldMap == null || !fieldMap.Ignore)
                    {
                        if ((fieldMap != null && fieldMap.AddToWhereInsteadOfJoin) || ItemColumns.Any(c => c.Equals(fieldName, StringComparison.OrdinalIgnoreCase)))
                        {
                            subWhereClause.Item2.Add($"{whereColumnSelector} {@operator} {parameterInWhere}");
                        }
                        else
                        {
                            filtersQuery.AppendLine($"JOIN {tablePrefix}{WiserTableNames.WiserItemDetail} AS `idv{counter}` ON `idv{counter}`.item_id = `{itemTableAlias}`.id AND `idv{counter}`.`key` = '{fieldName}' AND ({valueSelector} {@operator} {parameterInWhere} OR {longValueSelector} {@operator} {parameterInWhere})");
                        }
                    }

                    selectQuery = selectQuery.Replace($"{{{fieldName}_filter}}", $" {@operator} {parameterInWhere}", StringComparison.OrdinalIgnoreCase) ?? "";
                    countQuery = countQuery.Replace($"{{{fieldName}_filter}}", $" {@operator} {parameterInWhere}", StringComparison.OrdinalIgnoreCase) ?? "";
                    selectQuery = selectQuery.Replace($"{{{fieldName}_has_filter}}", "1", StringComparison.OrdinalIgnoreCase) ?? "";
                    countQuery = countQuery.Replace($"{{{fieldName}_has_filter}}", "1", StringComparison.OrdinalIgnoreCase) ?? "";
                    return counter;
                }


                if (options.Take <= 0)
                {
                    selectQuery = selectQuery.Replace("{limit}", "", StringComparison.OrdinalIgnoreCase) ?? "";
                }
                else
                {
                    clientDatabaseConnection.AddParameter("skip", options.Skip);
                    clientDatabaseConnection.AddParameter("take", options.Take);
                    selectQuery = selectQuery.Replace("{limit}", "LIMIT ?skip, ?take", StringComparison.OrdinalIgnoreCase) ?? "";
                }

                if (options.Sort == null || !options.Sort.Any())
                {
                    selectQuery = selectQuery.Replace("{sort}", defaultSort ?? "", StringComparison.OrdinalIgnoreCase) ?? "";
                }
                else
                {
                    // The {joinsForSorting} variable is added to default queries that return all their item details dynamically.
                    // In those cases, we can't order normally like we do in other cases. We need to add joins to specific item details, so that we can order based on those values.
                    var shouldCreateJoinsForSorting = selectQuery.Contains("{joinsForSorting}", StringComparison.OrdinalIgnoreCase);
                    if (shouldCreateJoinsForSorting)
                    {
                        var joinsForSorting = new StringBuilder();
                        foreach (var sort in options.Sort.Where(s => !ItemColumns.Any(x => x.Equals(s.Field, StringComparison.OrdinalIgnoreCase))))
                        {
                            // Column names are used as variables in KendoUI, but dashes don't work for that, so we replace them with a double underscore.
                            var fieldName = sort.Field.UnmakeJsonPropertyName();
                            var itemTableAlias = "i";

                            var fieldMap = fieldMappings.FirstOrDefault(f => f.Field.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
                            if (!String.IsNullOrWhiteSpace(fieldMap?.Property))
                            {
                                fieldName = fieldMap.Property;
                            }

                            if (!String.IsNullOrWhiteSpace(fieldMap?.ItemTableAlias))
                            {
                                itemTableAlias = fieldMap.ItemTableAlias;
                            }

                            fieldName = fieldName.ToMySqlSafeValue(false);

                            joinsForSorting.AppendLine($"LEFT JOIN {tablePrefix}{WiserTableNames.WiserItemDetail} AS `{fieldName}` ON `{fieldName}`.item_id = `{itemTableAlias}`.id AND `{fieldName}`.`key` = '{fieldName}'");
                        }

                        selectQuery = selectQuery.Replace("{joinsForSorting}", joinsForSorting.ToString(), StringComparison.OrdinalIgnoreCase);
                    }

                    selectQuery = selectQuery.Replace("{sort}", $"ORDER BY {String.Join(", ", options.Sort.Select(s => $"{(!shouldCreateJoinsForSorting || ItemColumns.Any(x => x.Equals(s.Field, StringComparison.OrdinalIgnoreCase)) ? $"`{s.Field.UnmakeJsonPropertyName().ToMySqlSafeValue(false)}`" : $"CONCAT_WS('', `{s.Field.UnmakeJsonPropertyName().ToMySqlSafeValue(false)}`.`value`, `{s.Field.UnmakeJsonPropertyName().ToMySqlSafeValue(false)}`.`long_value`)")} {s.Dir.ToMySqlSafeValue(false).ToUpperInvariant()}"))}", StringComparison.OrdinalIgnoreCase) ?? "";
                }

                if (options.Filter?.Filters == null || !options.Filter.Filters.Any())
                {
                    selectQuery = selectQuery.Replace("{filters}", "", StringComparison.OrdinalIgnoreCase) ?? "";
                    countQuery = countQuery.Replace("{filters}", "", StringComparison.OrdinalIgnoreCase) ?? "";

                    selectQuery = selectQuery.Replace("{hasWhere}", "0", StringComparison.OrdinalIgnoreCase) ?? "";
                    countQuery = countQuery.Replace("{hasWhere}", "0", StringComparison.OrdinalIgnoreCase) ?? "";

                    selectQuery = selectQuery.Replace("{where}", "TRUE", StringComparison.OrdinalIgnoreCase) ?? "";
                    countQuery = countQuery.Replace("{where}", "TRUE", StringComparison.OrdinalIgnoreCase) ?? "";
                }
                else
                {
                    var filtersQuery = new StringBuilder();
                    var counter = 0;
                    var logic = options.Filter.Logic.ToMySqlSafeValue(false);

                    selectQuery = selectQuery.Replace("{logic}", logic, StringComparison.OrdinalIgnoreCase) ?? "";
                    countQuery = countQuery.Replace("{logic}", logic, StringComparison.OrdinalIgnoreCase) ?? "";

                    var mainWhereClause = (logic, new List<string>());
                    whereClause.Add(mainWhereClause);
                    foreach (var filter in options.Filter.Filters)
                    {
                        counter = AddFiltersToQuery(counter, filter, filtersQuery, mainWhereClause);
                    }

                    selectQuery = selectQuery.Replace("{filters}", filtersQuery.ToString(), StringComparison.OrdinalIgnoreCase) ?? "";
                    countQuery = countQuery.Replace("{filters}", filtersQuery.ToString(), StringComparison.OrdinalIgnoreCase) ?? "";

                    selectQuery = selectQuery.Replace("{hasWhere}", whereClause.Any() ? "1" : "0", StringComparison.OrdinalIgnoreCase) ?? "";
                    countQuery = countQuery.Replace("{hasWhere}", whereClause.Any() ? "1" : "0", StringComparison.OrdinalIgnoreCase) ?? "";

                    if (!whereClause.Any(x => x.Item2.Any()))
                    {
                        selectQuery = selectQuery.Replace("{where}", "TRUE", StringComparison.OrdinalIgnoreCase) ?? "";
                        countQuery = countQuery.Replace("{where}", "TRUE", StringComparison.OrdinalIgnoreCase) ?? "";
                    }
                    else
                    {
                        var value = String.Join($" {logic} ", whereClause.Select(x => !x.Item2.Any() ? "" : $"({String.Join($" {x.Item1} ", x.Item2)})").Where(x => !String.IsNullOrWhiteSpace(x)));
                        selectQuery = selectQuery.Replace("{where}", value, StringComparison.OrdinalIgnoreCase) ?? "";
                        countQuery = countQuery.Replace("{where}", value, StringComparison.OrdinalIgnoreCase) ?? "";
                    }
                }

                if (options.ExtraValuesForQuery != null)
                {
                    foreach (var pair in options.ExtraValuesForQuery)
                    {
                        selectQuery = selectQuery.Replace($"{{{pair.Key}}}", pair.Value.ToMySqlSafeValue(false), StringComparison.OrdinalIgnoreCase) ?? "";
                        countQuery = countQuery.Replace($"{{{pair.Key}}}", pair.Value.ToMySqlSafeValue(false), StringComparison.OrdinalIgnoreCase) ?? "";
                    }
                }
            }

            // Remove any left over variables that we can't use and handle [if] statements.
            var regex = new Regex("{[^}]*}", RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(200));
            selectQuery = regex.Replace(selectQuery, "");
            countQuery = regex.Replace(countQuery, "");

            // Handle [if] statements in the query.
            selectQuery = stringReplacementsService.EvaluateTemplate(regex.Replace(selectQuery, ""));
            countQuery = stringReplacementsService.EvaluateTemplate(regex.Replace(countQuery, ""));

            return (selectQuery, countQuery);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<GridSettingsAndDataModel>> GetDataAsync(int propertyId,
            string encryptedId,
            GridReadOptionsModel options,
            string encryptedQueryId,
            string encryptedCountQueryId,
            ClaimsIdentity identity)
        {
            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            clientDatabaseConnection.SetCommandTimeout(600);
            var tenant = await wiserTenantsService.GetSingleAsync(identity);
            var queryId = String.IsNullOrWhiteSpace(encryptedQueryId) ? 0 : wiserTenantsService.DecryptValue<int>(encryptedQueryId, tenant.ModelObject);
            var countQueryId = String.IsNullOrWhiteSpace(encryptedCountQueryId) ? 0 : wiserTenantsService.DecryptValue<int>(encryptedCountQueryId, tenant.ModelObject);
            var itemId = String.IsNullOrWhiteSpace(encryptedId) ? 0 : wiserTenantsService.DecryptValue<ulong>(encryptedId, tenant.ModelObject);
            var hasPredefinedColumns = false;
            var results = new GridSettingsAndDataModel();
            var extraJavascript = new StringBuilder();
            string selectQuery;
            var countQuery = "";

            if (queryId > 0)
            {
                var customQueryResult = await itemsService.GetCustomQueryAsync(propertyId, queryId, identity);
                var countQueryResult = await itemsService.GetCustomQueryAsync(propertyId, countQueryId, identity);
                if (customQueryResult.StatusCode != HttpStatusCode.OK)
                {
                    return new ServiceResult<GridSettingsAndDataModel>
                    {
                        StatusCode = customQueryResult.StatusCode,
                        ErrorMessage = customQueryResult.ErrorMessage
                    };
                }

                selectQuery = customQueryResult.ModelObject;
                if (customQueryResult.StatusCode == HttpStatusCode.OK)
                {
                    countQuery = countQueryResult.ModelObject;
                }
            }
            else
            {
                var (query, errorResult, gridConfiguration) = await itemsService.GetPropertyQueryAsync<GridSettingsAndDataModel>(propertyId, "data_query", true, itemId);
                selectQuery = query;

                if (errorResult != null)
                {
                    return errorResult;
                }

                // Deserialize the options of the grid into our model.
                results = await GridSettingsAndDataModelFromFieldOptionsAsync(propertyId, gridConfiguration, itemId);
                hasPredefinedColumns = results.Columns.Any();
            }

            // If the count query is empty and the select query contains a limit, build a count query based on the select query without the limit and sort.
            if (String.IsNullOrWhiteSpace(countQuery) && !String.IsNullOrWhiteSpace(selectQuery) && selectQuery.Contains("{limit}", StringComparison.OrdinalIgnoreCase))
            {
                countQuery = $@"SELECT COUNT(*) FROM (
                                    {selectQuery.Replace("{limit}", "", StringComparison.OrdinalIgnoreCase).Replace("{sort}", "", StringComparison.OrdinalIgnoreCase).Trim(';')}
                                ) AS x";
            }

            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("itemId", itemId);
            (selectQuery, countQuery) = BuildGridQueries(options, selectQuery, countQuery, identity, "");

            // Get the count, but only if this is not the first load.
            if ((options?.FirstLoad ?? true) && !String.IsNullOrWhiteSpace(countQuery))
            {
                var countDataTable = await clientDatabaseConnection.GetAsync(countQuery);
                results.TotalResults = Convert.ToInt32(countDataTable.Rows[0][0]);
            }

            // Get the actual data for the grid.
            var dataTable = await clientDatabaseConnection.GetAsync(selectQuery);

            if ((options?.FirstLoad ?? true) && String.IsNullOrWhiteSpace(countQuery))
            {
                results.TotalResults = dataTable.Rows.Count;
            }

            // Build the schema model for the Kendo UI grid.
            BuildGridSchema(dataTable, results, hasPredefinedColumns);

            if (extraJavascript.Length > 0)
            {
                results.ExtraJavascript = extraJavascript.ToString();
            }

            // Return the data.
            if (dataTable.Rows.Count == 0)
            {
                return new ServiceResult<GridSettingsAndDataModel>(results);
            }

            FillGridData(dataTable, results, identity, IdentityHelpers.IsTestEnvironment(identity), tenant.ModelObject);

            return new ServiceResult<GridSettingsAndDataModel>(results);
        }

        private async Task<GridSettingsAndDataModel> GridSettingsAndDataModelFromFieldOptionsAsync(int propertyId, string rawOptions, ulong itemId)
        {
            if (String.IsNullOrWhiteSpace(rawOptions))
            {
                return new GridSettingsAndDataModel();
            }

            GridSettingsAndDataModel results = null;
            try
            {
                results = JsonConvert.DeserializeObject<GridSettingsAndDataModel>(rawOptions.Replace("{itemId}", itemId.ToString(), StringComparison.OrdinalIgnoreCase));

                foreach (var column in results.Columns.Where(c => !String.IsNullOrWhiteSpace(c.Editor)))
                {
                    var kendoFieldType = column.Editor;
                    if (!kendoFieldType.Equals("kendoDropDownList", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var dataSource = column.DataItems ?? new List<DataSourceItemModel>();
                    if (!String.IsNullOrWhiteSpace(column.DataQuery))
                    {
                        clientDatabaseConnection.ClearParameters();
                        var dataTable = await clientDatabaseConnection.GetAsync(column.DataQuery);
                        if (dataTable.Rows.Count > 0)
                        {
                            foreach (DataRow dataRow in dataTable.Rows)
                            {
                                dataSource.Add(new DataSourceItemModel
                                {
                                    Value = dataRow[0],
                                    Text = dataRow[1]?.ToString()
                                });
                            }
                        }
                    }

                    column.Values = dataSource;
                }
            }
            catch (Exception exception)
            {
                logger.LogError($"An error occurred while deserializing the options for a grid. PropertyId: {propertyId}, itemId: {itemId}, options: {rawOptions}, error: {exception}");
            }

            return results;
        }

        private void FillGridData(DataTable dataTable, GridSettingsAndDataModel results, ClaimsIdentity identity, bool isTest, TenantModel tenant)
        {
            var encryptionKey = tenant.EncryptionKey;
            foreach (DataRow dataRow in dataTable.Rows)
            {
                var rowData = new Dictionary<string, object>();
                results.Data.Add(rowData);

                foreach (DataColumn dataColumn in dataTable.Columns)
                {
                    var columnName = dataColumn.ColumnName.ToLowerInvariant().Replace("_encrypt_withdate", "").Replace("_encrypt", "").Replace("_decrypt", "").Replace("_decrypt_withdate", "").Replace("_normalencrypt", "").Replace("_normaldecrypt", "").Replace("_hide", "").MakeJsonPropertyName();
                    var value = dataRow.IsNull(dataColumn.ColumnName) ? "" : dataRow[dataColumn.ColumnName].ToString();
                    try
                    {
                        if (dataColumn.ColumnName.Contains("_encrypt", StringComparison.OrdinalIgnoreCase))
                        {
                            rowData[columnName] = wiserTenantsService.EncryptValue(value, tenant);
                        }
                        else if (dataColumn.ColumnName.Contains("_normalencrypt", StringComparison.OrdinalIgnoreCase))
                        {
                            rowData[columnName] = Uri.EscapeDataString(value.EncryptWithAes());
                        }
                        else if (dataColumn.ColumnName.Contains("_normaldecrypt", StringComparison.OrdinalIgnoreCase))
                        {
                            rowData[columnName] = value.DecryptWithAes(encryptionKey);
                        }
                        else if (dataColumn.ColumnName.Contains("_decrypt", StringComparison.OrdinalIgnoreCase))
                        {
                            rowData[columnName] = value.DecryptWithAesWithSalt(encryptionKey);
                        }
                        else
                        {
                            rowData.Add(columnName, dataRow[dataColumn.ColumnName]);
                        }
                    }
                    catch (Exception ex)
                    {
                        rowData[columnName] = $"[Decrypt Error]";
                        logger.LogWarning($"[Decrypt Error] {ex.Message}", ex);
                    }
                }
            }
        }

        private static void BuildGridSchema(DataTable dataTable, GridSettingsAndDataModel results, bool hasPredefinedColumns)
        {
            foreach (DataColumn dataColumn in dataTable.Columns)
            {
                string kendoColumnType;
                switch (Type.GetTypeCode(dataColumn.DataType))
                {
                    case TypeCode.Boolean:
                        kendoColumnType = "boolean";
                        break;
                    case TypeCode.Byte:
                    case TypeCode.SByte:
                    case TypeCode.Int16:
                    case TypeCode.Int32:
                    case TypeCode.Int64:
                    case TypeCode.Double:
                    case TypeCode.Decimal:
                    case TypeCode.UInt16:
                    case TypeCode.UInt32:
                    case TypeCode.UInt64:
                        kendoColumnType = "number";
                        break;
                    case TypeCode.DateTime:
                        kendoColumnType = "date";
                        break;
                    default:
                        kendoColumnType = null;
                        break;
                }

                var fieldName = dataColumn.ColumnName.ToLowerInvariant().Replace("_withdate", "").Replace("_encrypt", "").Replace("_hide", "").MakeJsonPropertyName();
                if (fieldName.Equals("fields", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Encrypted values are always strings.
                if (dataColumn.ColumnName.Contains("_encrypt", StringComparison.OrdinalIgnoreCase))
                {
                    kendoColumnType = null;
                }

                results.SchemaModel.Fields.Add(fieldName,
                    new FieldModel
                    {
                        Editable = !dataColumn.ColumnName.Equals("id", StringComparison.OrdinalIgnoreCase),
                        Nullable = true,
                        Type = kendoColumnType
                    });

                if (!hasPredefinedColumns && !dataColumn.ColumnName.EndsWith("_hide", StringComparison.OrdinalIgnoreCase))
                {
                    results.Columns.Add(new GridColumn
                    {
                        Field = fieldName,
                        Title = dataColumn.ColumnName
                    });
                }
            }
        }

        /// <inheritdoc />
        public async Task<ServiceResult<Dictionary<string, object>>> InsertDataAsync(int propertyId, string encryptedId, Dictionary<string, object> data, ClaimsIdentity identity)
        {
            try
            {
                await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
                var itemId = await wiserTenantsService.DecryptValue<ulong>(encryptedId, identity);
                var (insertQuery, errorResult, _) = await itemsService.GetPropertyQueryAsync<Dictionary<string, object>>(propertyId, "grid_insert_query", false, itemId);
                if (errorResult != null)
                {
                    return errorResult;
                }

                insertQuery = DoReplacementsForGridMutationQuery(identity, encryptedId, data,  insertQuery, itemId);

                // TODO: Make ID field configurable?
                data["id"] = await clientDatabaseConnection.InsertRecordAsync(insertQuery, false);

                return new ServiceResult<Dictionary<string, object>>(data);
            }
            catch (MySqlException mySqlException)
            {
                if (mySqlException.Number != 1062)
                {
                    throw;
                }

                return new ServiceResult<Dictionary<string, object>>
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessage = "Dit item bestaat al en kan niet nogmaals toegevoegd worden."
                };
            }
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> UpdateDataAsync(int propertyId, string encryptedId, Dictionary<string, object> data, ClaimsIdentity identity)
        {
            try
            {
                await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
                var itemId = await wiserTenantsService.DecryptValue<ulong>(encryptedId, identity);
                var (modifyQuery, errorResult, _) = await itemsService.GetPropertyQueryAsync<bool>(propertyId, "grid_update_query", false, itemId);
                if (errorResult != null)
                {
                    return errorResult;
                }

                modifyQuery = DoReplacementsForGridMutationQuery(identity, encryptedId, data, modifyQuery, itemId);

                await clientDatabaseConnection.ExecuteAsync(modifyQuery);

                return new ServiceResult<bool>
                {
                    StatusCode = HttpStatusCode.NoContent
                };
            }
            catch (MySqlException mySqlException)
            {
                if (mySqlException.Number != 1062)
                {
                    throw;
                }

                return new ServiceResult<bool>
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessage = "Dit item bestaat al en kan niet nogmaals toegevoegd worden."
                };
            }
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> DeleteDataAsync(int propertyId, string encryptedId, Dictionary<string, object> data, ClaimsIdentity identity)
        {
            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            var itemId = await wiserTenantsService.DecryptValue<ulong>(encryptedId, identity);
            var (deleteQuery, errorResult, _) = await itemsService.GetPropertyQueryAsync<bool>(propertyId, "grid_delete_query", false, itemId);
            if (errorResult != null)
            {
                return errorResult;
            }

            deleteQuery = DoReplacementsForGridMutationQuery(identity, encryptedId, data, deleteQuery, itemId);

            await clientDatabaseConnection.ExecuteAsync(deleteQuery);

            return new ServiceResult<bool>
            {
                StatusCode = HttpStatusCode.NoContent
            };
        }

        private string DoReplacementsForGridMutationQuery(ClaimsIdentity identity, string encryptedId, Dictionary<string, object> data, string query, ulong itemId)
        {
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("itemId", itemId);
            var counter = 0;
            foreach (var key in data.Keys)
            {
                clientDatabaseConnection.AddParameter(key.UnmakeJsonPropertyName(), data[key]);
                query = query.Replace($"{{propertyKey{counter}}}", key.UnmakeJsonPropertyName(), StringComparison.OrdinalIgnoreCase);
                query = query.Replace($"{{propertyValue{counter}}}", data[key] == null ? "" : data[key].ToString().ToMySqlSafeValue(false), StringComparison.OrdinalIgnoreCase);
                counter++;
            }

            var userId = IdentityHelpers.GetWiserUserId(identity);
            query = query.Replace("{userId}", userId.ToString(), StringComparison.OrdinalIgnoreCase);
            query = query.Replace("{username}", IdentityHelpers.GetUserName(identity, true) ?? "", StringComparison.OrdinalIgnoreCase);
            query = query.Replace("{userEmailAddress}", IdentityHelpers.GetEmailAddress(identity) ?? "", StringComparison.OrdinalIgnoreCase);
            query = query.Replace("{userType}", IdentityHelpers.GetRoles(identity) ?? "", StringComparison.OrdinalIgnoreCase);
            query = query.Replace("{encryptedId}", encryptedId, StringComparison.OrdinalIgnoreCase);
            query = query.Replace("{itemId}", itemId.ToString(), StringComparison.OrdinalIgnoreCase);
            return query;
        }
    }
}
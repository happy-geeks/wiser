using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Api.Modules.Kendo.Enums;
using Api.Modules.Templates.Helpers;
using Api.Modules.Templates.Interfaces.DataLayer;
using Api.Modules.Templates.Models.DynamicContent;
using Api.Modules.Templates.Models.Other;
using Api.Modules.Templates.Models.Template;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Enums;
using GeeksCoreLibrary.Modules.Templates.Models;
using Constants = Api.Modules.Templates.Models.Other.Constants;

namespace Api.Modules.Templates.Services.DataLayer
{
    /// <inheritdoc cref="ITemplateDataService" />
    public class TemplateDataService : ITemplateDataService, IScopedService
    {
        private readonly IDatabaseConnection clientDatabaseConnection;
        private readonly IDatabaseHelpersService databaseHelpersService;

        /// <summary>
        /// Creates a new instance of <see cref="TemplateDataService"/>.
        /// </summary>
        public TemplateDataService(IDatabaseConnection clientDatabaseConnection, IDatabaseHelpersService databaseHelpersService)
        {
            this.clientDatabaseConnection = clientDatabaseConnection;
            this.databaseHelpersService = databaseHelpersService;
        }

        /// <inheritdoc />
        public async Task<TemplateSettingsModel> GetMetaDataAsync(int templateId)
        {
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("templateId", templateId);
            var dataTable = await clientDatabaseConnection.GetAsync($@"SELECT 
    template.parent_id,
    template.template_type,
    template.template_name,
    template.version,
    template.added_on, 
    template.added_by, 
    template.changed_on, 
    template.changed_by, 
    template.ordering
FROM {WiserTableNames.WiserTemplate} AS template 
LEFT JOIN {WiserTableNames.WiserTemplate} AS otherVersion ON otherVersion.template_id = template.template_id AND otherVersion.version > template.version
WHERE template.template_id = ?templateId
AND template.removed = 0
AND otherVersion.id IS NULL
LIMIT 1");

            return dataTable.Rows.Count == 0 ? new TemplateSettingsModel() : new TemplateSettingsModel
            {
                TemplateId = templateId,
                ParentId = dataTable.Rows[0].Field<int?>("parent_id"),
                Type = dataTable.Rows[0].Field<TemplateTypes>("template_type"),
                Name = dataTable.Rows[0].Field<string>("template_name"),
                Version = dataTable.Rows[0].Field<int>("version"),
                AddedOn = dataTable.Rows[0].Field<DateTime>("added_on"),
                AddedBy = dataTable.Rows[0].Field<string>("added_by"),
                ChangedOn = dataTable.Rows[0].Field<DateTime>("changed_on"),
                ChangedBy = dataTable.Rows[0].Field<string>("changed_by"),
                Ordering = dataTable.Rows[0].Field<int>("ordering")
            };
        }

        /// <inheritdoc />
        public async Task<TemplateSettingsModel> GetDataAsync(int templateId, Environments? environment = null, int? version = null)
        {
            clientDatabaseConnection.ClearParameters();

            string publishedVersionWhere;
            var publishedVersionJoin = "";
            if (version.HasValue)
            {
                clientDatabaseConnection.AddParameter("version", version.Value);
                publishedVersionWhere = "AND template.version = ?version";
            }
            else
            {
                publishedVersionWhere = environment switch
                {
                    null => "AND otherVersion.id IS NULL",
                    Environments.Hidden => "AND template.published_environment = 0",
                    _ => $"AND (template.published_environment & {(int) environment}) = {(int) environment}"
                };

                if (environment == null)
                {
                    publishedVersionJoin = $"LEFT JOIN {WiserTableNames.WiserTemplate} AS otherVersion ON otherVersion.template_id = template.template_id AND otherVersion.version > template.version";
                }
            }

            clientDatabaseConnection.AddParameter("templateId", templateId);
            var dataTable = await clientDatabaseConnection.GetAsync($@"SELECT 
    template.template_id, 
    template.parent_id, 
    template.template_type, 
    template.template_name, 
    template.template_data, 
    template.version,
    template.added_on, 
    template.added_by,
    template.changed_on, 
    template.changed_by,
    template.cache_per_url,
    template.cache_per_querystring,
    template.cache_per_hostname,
    template.cache_per_user,
    template.cache_using_regex,
    template.cache_minutes, 
    template.cache_location, 
    template.cache_regex,
    template.login_required, 
    template.login_role, 
    template.login_redirect_url,
    template.linked_templates, 
    template.ordering,
    template.insert_mode,
    template.load_always,
    template.disable_minifier,
    template.url_regex,
    template.external_files,
    IF(COUNT(externalFiles.external_file) = 0, NULL, JSON_ARRAYAGG(JSON_OBJECT('uri', externalFiles.external_file, 'hash', externalFiles.hash, 'ordering', externalFiles.ordering))) AS external_files_json,
    template.grouping_create_object_instead_of_array,
    template.grouping_prefix,
    template.grouping_key,
    template.grouping_key_column_name,
    template.grouping_value_column_name,
    template.is_scss_include_template,
    template.use_in_wiser_html_editors,
    template.pre_load_query,
    template.return_not_found_when_pre_load_query_has_no_data,
    template.routine_type,
    template.routine_parameters,
    template.routine_return_type,
    template.trigger_timing,
    template.trigger_event,
    template.trigger_table_name,
    template.is_default_header,
    template.is_default_footer,
    template.default_header_footer_regex,
    template.is_partial,
    template.widget_content,
    template.widget_location
FROM {WiserTableNames.WiserTemplate} AS template
{publishedVersionJoin}
LEFT JOIN {WiserTableNames.WiserTemplateExternalFiles} AS externalFiles ON externalFiles.template_id = template.id
WHERE template.template_id = ?templateId
{publishedVersionWhere}
AND template.removed = 0
GROUP BY template.id
ORDER BY template.version DESC 
LIMIT 1");

            if (dataTable.Rows.Count == 0)
            {
                return new TemplateSettingsModel
                {
                    TemplateId = templateId
                };
            }

            return TemplateHelpers.DataRowToTemplateSettingsModel(dataTable.Rows[0]);
        }

        /// <inheritdoc />
        public async Task<Dictionary<int, int>> GetPublishedEnvironmentsAsync(int templateId, string branchDatabaseName = null)
        {
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("templateId", templateId);
            var versionList = new Dictionary<int, int>();

            var databaseNamePrefix = String.IsNullOrWhiteSpace(branchDatabaseName) ? "" : $"`{branchDatabaseName}`.";
            var dataTable = await clientDatabaseConnection.GetAsync($"SELECT version, published_environment FROM {databaseNamePrefix}{WiserTableNames.WiserTemplate} WHERE template_id = ?templateId AND removed = 0");

            foreach (DataRow row in dataTable.Rows)
            {
                versionList.Add(row.Field<int>("version"), Convert.ToInt32(row["published_environment"]));
            }

            return versionList;
        }

        /// <inheritdoc />
        public async Task<(int Id, int Version, Environments Environment, bool Removed)> GetLatestVersionAsync(int templateId, string branchDatabaseName = null)
        {
            var databaseNamePrefix = String.IsNullOrWhiteSpace(branchDatabaseName) ? "" : $"`{branchDatabaseName}`.";

            // Get the ID and published environment of the latest version of the template.
            var query = $@"SELECT 
    template.id,
    template.version,
    template.published_environment,
    template.removed
FROM {databaseNamePrefix}{WiserTableNames.WiserTemplate} AS template
LEFT JOIN {databaseNamePrefix}{WiserTableNames.WiserTemplate} AS otherVersion ON otherVersion.template_id = template.template_id AND otherVersion.version > template.version
WHERE template.template_id = ?templateId
AND otherVersion.id IS NULL";
            clientDatabaseConnection.AddParameter("templateId", templateId);
            var dataTable = await clientDatabaseConnection.GetAsync(query,  skipCache: true, useWritingConnectionIfAvailable: true);
            if (dataTable.Rows.Count == 0)
            {
                throw new Exception($"Template with ID {templateId} not found.");
            }

            var id = Convert.ToInt32(dataTable.Rows[0]["id"]);
            var version = Convert.ToInt32(dataTable.Rows[0]["version"]);
            var publishedEnvironment = (Environments)Convert.ToInt32(dataTable.Rows[0]["published_environment"]);
            var removed = Convert.ToBoolean(dataTable.Rows[0]["removed"]);
            return (id, version, publishedEnvironment, removed);
        }

        /// <inheritdoc />
        public async Task<int> UpdatePublishedEnvironmentAsync(int templateId, int version, Environments environment, PublishLogModel publishLog, string username, string branchDatabaseName = null)
        {
            switch (environment)
            {
                case Environments.Test:
                    environment |= Environments.Development;
                    break;
                case Environments.Acceptance:
                    environment |= Environments.Test | Environments.Development;
                    break;
                case Environments.Live:
                    environment |= Environments.Acceptance | Environments.Test | Environments.Development;
                    break;
            }

            clientDatabaseConnection.AddParameter("templateId", templateId);
            clientDatabaseConnection.AddParameter("version", version);
            clientDatabaseConnection.AddParameter("environment", (int)environment);

            var databaseNamePrefix = String.IsNullOrWhiteSpace(branchDatabaseName) ? "" : $"`{branchDatabaseName}`.";

            // Add the bit of the selected environment to the selected version.
            var query = $@"UPDATE {databaseNamePrefix}{WiserTableNames.WiserTemplate}
SET published_environment = published_environment | ?environment
WHERE template_id = ?templateId
AND version = ?version
AND (published_environment & ?environment) != ?environment";
            var affectedRows = await clientDatabaseConnection.ExecuteAsync(query);

            // Query to remove the selected environment from all other versions, the ~ operator flips all the bits (1s become 0s and 0s become 1s).
            // This way we can safely turn off just the specific bits without having to check to see if the bit is set.
            query = $@"UPDATE {databaseNamePrefix}{WiserTableNames.WiserTemplate}
SET published_environment = published_environment & ~?environment
WHERE template_id = ?templateId
AND version != ?version
AND (published_environment & ?environment) > 0";

            affectedRows += await clientDatabaseConnection.ExecuteAsync(query);

            if (affectedRows == 0)
            {
                return affectedRows;
            }

            // If any rows have been updated, it means this version wasn't deployed to the selected environment yet and we should add it to the history.
            clientDatabaseConnection.AddParameter("oldlive", publishLog.OldLive);
            clientDatabaseConnection.AddParameter("oldaccept", publishLog.OldAccept);
            clientDatabaseConnection.AddParameter("oldtest", publishLog.OldTest);
            clientDatabaseConnection.AddParameter("newlive", publishLog.NewLive);
            clientDatabaseConnection.AddParameter("newaccept", publishLog.NewAccept);
            clientDatabaseConnection.AddParameter("newtest", publishLog.NewTest);
            clientDatabaseConnection.AddParameter("now", DateTime.Now);
            clientDatabaseConnection.AddParameter("username", username);

            query = $@"INSERT INTO {databaseNamePrefix}{WiserTableNames.WiserTemplatePublishLog}
(
    template_id,
    old_live,
    old_accept,
    old_test,
    new_live,
    new_accept,
    new_test,
    changed_on,
    changed_by
) 
VALUES
(
    ?templateid,
    ?oldlive,
    ?oldaccept,
    ?oldtest,
    ?newlive,
    ?newaccept,
    ?newtest,
    ?now,
    ?username
)";
            await clientDatabaseConnection.ExecuteAsync(query);

            return affectedRows;
        }

        /// <inheritdoc />
        public async Task<List<LinkedTemplateModel>> GetLinkedTemplatesAsync(int templateId)
        {
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("templateId", templateId);
            var dataTable = await clientDatabaseConnection.GetAsync($"SELECT linked_templates FROM {WiserTableNames.WiserTemplate} WHERE template_id = ?templateId AND removed = 0 ORDER BY version DESC LIMIT 1");
            if (dataTable.Rows.Count == 0)
            {
                return new List<LinkedTemplateModel>();
            }

            var linkedTemplateValue = dataTable.Rows[0].Field<string>("linked_templates");
            if (String.IsNullOrWhiteSpace(linkedTemplateValue))
            {
                return new List<LinkedTemplateModel>();
            }

            // To make sure we don't get bad values in the query below, we split and convert to int.
            var linkedTemplateIds = linkedTemplateValue.Split(",").Select(Int32.Parse);

            dataTable = await clientDatabaseConnection.GetAsync($@"SELECT
    template.template_id,
    template.template_name,
    template.template_type,
	CONCAT_WS(' >> ', parent5.template_name, parent4.template_name, parent3.template_name, parent2.template_name, parent1.template_name) AS path
FROM {WiserTableNames.WiserTemplate} AS template
LEFT JOIN {WiserTableNames.WiserTemplate} AS otherVersion ON otherVersion.template_id = template.template_id AND otherVersion.version > template.version
LEFT JOIN {WiserTableNames.WiserTemplate} AS parent1 ON parent1.template_id = template.parent_id AND parent1.version = (SELECT MAX(version) FROM {WiserTableNames.WiserTemplate} WHERE template_id = template.parent_id)
LEFT JOIN {WiserTableNames.WiserTemplate} AS parent2 ON parent2.template_id = parent1.parent_id AND parent2.version = (SELECT MAX(version) FROM {WiserTableNames.WiserTemplate} WHERE template_id = parent1.parent_id)
LEFT JOIN {WiserTableNames.WiserTemplate} AS parent3 ON parent3.template_id = parent2.parent_id AND parent3.version = (SELECT MAX(version) FROM {WiserTableNames.WiserTemplate} WHERE template_id = parent2.parent_id)
LEFT JOIN {WiserTableNames.WiserTemplate} AS parent4 ON parent4.template_id = parent3.parent_id AND parent4.version = (SELECT MAX(version) FROM {WiserTableNames.WiserTemplate} WHERE template_id = parent3.parent_id)
LEFT JOIN {WiserTableNames.WiserTemplate} AS parent5 ON parent5.template_id = parent4.parent_id AND parent5.version = (SELECT MAX(version) FROM {WiserTableNames.WiserTemplate} WHERE template_id = parent4.parent_id)
WHERE template.template_id IN ({String.Join(",", linkedTemplateIds)})
AND template.removed = 0
AND otherVersion.id IS NULL
ORDER BY template.template_type ASC, template.template_name ASC");

            if (dataTable.Rows.Count == 0)
            {
                return new List<LinkedTemplateModel>();
            }

            return (dataTable.Rows.Cast<DataRow>().Select(row => new LinkedTemplateModel
            {
                TemplateId = row.Field<int>("template_id"),
                TemplateName = row.Field<string>("template_name"),
                LinkType = row.Field<TemplateTypes>("template_type"),
                Path = String.IsNullOrWhiteSpace(row.Field<string>("path")) ? "ROOT" : row.Field<string>("path")
            })).ToList();
        }

        /// <inheritdoc />
        public async Task<List<LinkedTemplateModel>> GetTemplatesAvailableForLinkingAsync(int templateId)
        {
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("templateId", templateId);
            var dataTable = await clientDatabaseConnection.GetAsync($@"SELECT 
	template.template_id,
	template.template_name,
	template.template_type,
	CONCAT_WS(' >> ', parent5.template_name, parent4.template_name, parent3.template_name, parent2.template_name, parent1.template_name) AS path
FROM {WiserTableNames.WiserTemplate} AS template
LEFT JOIN {WiserTableNames.WiserTemplate} AS otherVersion ON otherVersion.template_id = template.template_id AND otherVersion.version > template.version
LEFT JOIN {WiserTableNames.WiserTemplate} AS parent1 ON parent1.template_id = template.parent_id AND parent1.version = (SELECT MAX(version) FROM {WiserTableNames.WiserTemplate} WHERE template_id = template.parent_id)
LEFT JOIN {WiserTableNames.WiserTemplate} AS parent2 ON parent2.template_id = parent1.parent_id AND parent2.version = (SELECT MAX(version) FROM {WiserTableNames.WiserTemplate} WHERE template_id = parent1.parent_id)
LEFT JOIN {WiserTableNames.WiserTemplate} AS parent3 ON parent3.template_id = parent2.parent_id AND parent3.version = (SELECT MAX(version) FROM {WiserTableNames.WiserTemplate} WHERE template_id = parent2.parent_id)
LEFT JOIN {WiserTableNames.WiserTemplate} AS parent4 ON parent4.template_id = parent3.parent_id AND parent4.version = (SELECT MAX(version) FROM {WiserTableNames.WiserTemplate} WHERE template_id = parent3.parent_id)
LEFT JOIN {WiserTableNames.WiserTemplate} AS parent5 ON parent5.template_id = parent4.parent_id AND parent5.version = (SELECT MAX(version) FROM {WiserTableNames.WiserTemplate} WHERE template_id = parent4.parent_id)
WHERE template.template_type IN ({(int)TemplateTypes.Css}, {(int)TemplateTypes.Scss}, {(int)TemplateTypes.Js})
AND template.removed = 0
AND otherVersion.id IS NULL
ORDER BY template.template_type ASC, template.template_name ASC");

            if (dataTable.Rows.Count == 0)
            {
                return new List<LinkedTemplateModel>();
            }

            return (dataTable.Rows.Cast<DataRow>().Select(row => new LinkedTemplateModel
            {
                TemplateId = row.Field<int>("template_id"),
                TemplateName = row.Field<string>("template_name"),
                LinkType = row.Field<TemplateTypes>("template_type"),
                Path = String.IsNullOrWhiteSpace(row.Field<string>("path")) ? "ROOT" : row.Field<string>("path")
            })).ToList();
        }

        /// <inheritdoc />
        public async Task<List<LinkedDynamicContentDao>> GetLinkedDynamicContentAsync(int templateId)
        {
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("templateId", templateId);
            var dataTable = await clientDatabaseConnection.GetAsync($@"SELECT 
    wdc.content_id, 
    wdc.component, 
    wdc.component_mode, 
    GROUP_CONCAT(DISTINCT otherdc.`usages`) AS `usages`,
    wdc.added_on,
    wdc.added_by,
    wdc.changed_on,
    wdc.changed_by,
    wdc.`title`
FROM {WiserTableNames.WiserTemplateDynamicContent} tdclink 
JOIN {WiserTableNames.WiserDynamicContent} wdc ON wdc.content_id = tdclink.content_id AND wdc.removed = 0
LEFT JOIN (
	SELECT dcusages.content_id, tt.template_name AS `usages` 
	FROM {WiserTableNames.WiserTemplateDynamicContent} dcusages 
	JOIN {WiserTableNames.WiserTemplate} tt ON tt.template_id=dcusages.destination_template_id AND tt.removed = 0
) AS otherdc ON otherdc.content_id=tdclink.content_id
WHERE tdclink.destination_template_id = ?templateid
AND wdc.version = (SELECT MAX(dc.version) FROM {WiserTableNames.WiserDynamicContent} dc WHERE dc.content_id = wdc.content_id)
GROUP BY wdc.content_id");
            var resultList = new List<LinkedDynamicContentDao>();

            foreach (DataRow row in dataTable.Rows)
            {
                var resultDao = new LinkedDynamicContentDao();

                resultDao.Id = row.Field<int>("content_id");
                resultDao.Component = row.Field<string>("component");
                resultDao.ComponentMode = row.Field<string>("component_mode");
                resultDao.Usages = row.Field<string>("usages");
                resultDao.AddedOn = row.Field<DateTime>("added_on");
                resultDao.AddedBy = row.Field<string>("added_by");
                resultDao.ChangedOn = row.Field<DateTime>("changed_on");
                resultDao.ChangedBy = row.Field<string>("changed_by");
                resultDao.Title = row.Field<string>("title");

                resultList.Add(resultDao);
            }

            return resultList;
        }

        /// <inheritdoc />
        public async Task SaveAsync(TemplateSettingsModel templateSettings, string templateLinks, string username)
        {
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("templateId", templateSettings.TemplateId);

            // Get the ID and published environment of the latest version of the template.
            var latestVersion = await GetLatestVersionAsync(templateSettings.TemplateId);

            // If the latest version is published to live, create a new version, because we never want to edit the version that is published to live directly.
            if ((latestVersion.Environment & Environments.Live) == Environments.Live)
            {
                latestVersion.Id = await CreateNewVersionAsync(templateSettings.TemplateId);
            }

            clientDatabaseConnection.AddParameter("id", latestVersion.Id);
            clientDatabaseConnection.AddParameter("name", templateSettings.Name);
            clientDatabaseConnection.AddParameter("editorValue", templateSettings.EditorValue);
            clientDatabaseConnection.AddParameter("minifiedValue", templateSettings.MinifiedValue);
            clientDatabaseConnection.AddParameter("cachePerUrl", templateSettings.CachePerUrl);
            clientDatabaseConnection.AddParameter("cachePerQueryString", templateSettings.CachePerQueryString);
            clientDatabaseConnection.AddParameter("cachePerHostName", templateSettings.CachePerHostName);
            clientDatabaseConnection.AddParameter("cachePerUser", templateSettings.CachePerUser);
            clientDatabaseConnection.AddParameter("cacheUsingRegex", templateSettings.CacheUsingRegex);
            clientDatabaseConnection.AddParameter("cacheMinutes", templateSettings.CacheMinutes);
            clientDatabaseConnection.AddParameter("cacheLocation", templateSettings.CacheLocation);
            clientDatabaseConnection.AddParameter("cacheRegex", templateSettings.CacheRegex);
            clientDatabaseConnection.AddParameter("loginRequired", templateSettings.LoginRequired);
            clientDatabaseConnection.AddParameter("loginRole", templateSettings.LoginRoles == null ? "" : String.Join(",", templateSettings.LoginRoles.OrderBy(x => x)));
            clientDatabaseConnection.AddParameter("loginRedirectUrl", templateSettings.LoginRedirectUrl);
            clientDatabaseConnection.AddParameter("now", DateTime.Now);
            clientDatabaseConnection.AddParameter("username", username);
            clientDatabaseConnection.AddParameter("insertMode", (int)templateSettings.InsertMode);
            clientDatabaseConnection.AddParameter("loadAlways", templateSettings.LoadAlways);
            clientDatabaseConnection.AddParameter("disableMinifier", templateSettings.DisableMinifier);
            clientDatabaseConnection.AddParameter("urlRegex", templateSettings.UrlRegex);
            clientDatabaseConnection.AddParameter("groupingCreateObjectInsteadOfArray", templateSettings.GroupingCreateObjectInsteadOfArray);
            clientDatabaseConnection.AddParameter("groupingPrefix", templateSettings.GroupingPrefix);
            clientDatabaseConnection.AddParameter("groupingKey", templateSettings.GroupingKey);
            clientDatabaseConnection.AddParameter("groupingKeyColumnName", templateSettings.GroupingKeyColumnName);
            clientDatabaseConnection.AddParameter("groupingValueColumnName", templateSettings.GroupingValueColumnName);
            clientDatabaseConnection.AddParameter("isScssIncludeTemplate", templateSettings.IsScssIncludeTemplate);
            clientDatabaseConnection.AddParameter("useInWiserHtmlEditors", templateSettings.UseInWiserHtmlEditors);
            clientDatabaseConnection.AddParameter("templateLinks", templateLinks);
            clientDatabaseConnection.AddParameter("preLoadQuery", templateSettings.PreLoadQuery);
            clientDatabaseConnection.AddParameter("returnNotFoundWhenPreLoadQueryHasNoData", templateSettings.ReturnNotFoundWhenPreLoadQueryHasNoData);
            clientDatabaseConnection.AddParameter("routineType", (int)templateSettings.RoutineType);
            clientDatabaseConnection.AddParameter("routineParameters", templateSettings.RoutineParameters);
            clientDatabaseConnection.AddParameter("routineReturnType", templateSettings.RoutineReturnType);
            clientDatabaseConnection.AddParameter("triggerTiming", (int)templateSettings.TriggerTiming);
            clientDatabaseConnection.AddParameter("triggerEvent", (int)templateSettings.TriggerEvent);
            clientDatabaseConnection.AddParameter("triggerTableName", templateSettings.TriggerTableName);
            clientDatabaseConnection.AddParameter("isDefaultHeader", templateSettings.IsDefaultHeader);
            clientDatabaseConnection.AddParameter("isDefaultFooter", templateSettings.IsDefaultFooter);
            clientDatabaseConnection.AddParameter("defaultHeaderFooterRegex", templateSettings.DefaultHeaderFooterRegex);
            clientDatabaseConnection.AddParameter("isPartial", templateSettings.IsPartial);
            clientDatabaseConnection.AddParameter("widgetContent", templateSettings.WidgetContent);
            clientDatabaseConnection.AddParameter("widgetLocation", (int)templateSettings.WidgetLocation);

            // Save the template itself.
            var query = $@"UPDATE {WiserTableNames.WiserTemplate}
SET template_name = ?name,
    template_data = ?editorValue,
    template_data_minified = ?minifiedValue,
    changed_on = ?now,
    changed_by = ?username,
    cache_per_url = ?cachePerUrl,
    cache_per_querystring = ?cachePerQueryString,
    cache_per_hostname = ?cachePerHostName,
    cache_per_user = ?cachePerUser,
    cache_using_regex = ?cacheUsingRegex,
    cache_minutes = ?cacheMinutes,
    cache_location = ?cacheLocation,
    cache_regex = ?cacheRegex,
    login_required = ?loginRequired,
    login_role = ?loginRole,
    login_redirect_url = ?loginRedirectUrl,
    linked_templates = ?templateLinks,
    insert_mode = ?insertMode,
    load_always = ?loadAlways,
    disable_minifier = ?disableMinifier,
    url_regex = ?urlRegex,
    grouping_create_object_instead_of_array = ?groupingCreateObjectInsteadOfArray,
    grouping_prefix = ?groupingPrefix,
    grouping_key = ?groupingKey,
    grouping_key_column_name = ?groupingKeyColumnName,
    grouping_value_column_name = ?groupingValueColumnName,
    is_scss_include_template = ?isScssIncludeTemplate,
    use_in_wiser_html_editors = ?useInWiserHtmlEditors,
    pre_load_query = ?preLoadQuery,
    return_not_found_when_pre_load_query_has_no_data = ?returnNotFoundWhenPreLoadQueryHasNoData,
    routine_type = ?routineType,
    routine_parameters = ?routineParameters,
    routine_return_type = ?routineReturnType,
    trigger_timing = ?triggerTiming,
    trigger_event = ?triggerEvent,
    trigger_table_name = ?triggerTableName,
    is_default_header = ?isDefaultHeader,
    is_default_footer = ?isDefaultFooter,
    default_header_footer_regex = ?defaultHeaderFooterRegex,
    is_partial = ?isPartial,
    widget_content = ?widgetContent,
    widget_location = ?widgetLocation,
    is_dirty = TRUE,
    # Set the external_files column empty, because we have a new table for this now. This value will be moved to that table in code below.
    external_files = ''
WHERE id = ?id";
            await clientDatabaseConnection.ExecuteAsync(query);

            // Find out which external files are already saved in the database, so we don't need to re-download them every time someone saves the template.
            query = $"SELECT id, external_file, hash, ordering FROM {WiserTableNames.WiserTemplateExternalFiles} WHERE template_id = ?id";
            var externalFilesDataTable = await clientDatabaseConnection.GetAsync(query);
            var externalFilesToDelete = new List<int>();
            var externalFilesInDatabase = new List<PageResourceModel>();
            foreach (DataRow dataRow in externalFilesDataTable.Rows)
            {
                var data = new PageResourceModel
                {
                    Id = dataRow.Field<int>("id"),
                    Uri = new Uri(dataRow.Field<string>("external_file"), UriKind.RelativeOrAbsolute),
                    Hash = dataRow.Field<string>("hash"),
                    Ordering = dataRow.Field<int>("ordering")
                };

                if (!templateSettings.ExternalFiles.Any(y => String.Equals(y.Uri.ToString(), data.Uri.ToString(), StringComparison.OrdinalIgnoreCase)))
                {
                    externalFilesToDelete.Add(data.Id);
                }

                externalFilesInDatabase.Add(data);
            }

            // Delete any external files that are not in the new list.
            if (externalFilesToDelete.Any())
            {
                query = $"DELETE FROM {WiserTableNames.WiserTemplateExternalFiles} WHERE id IN ({String.Join(",", externalFilesToDelete)})";
                await clientDatabaseConnection.ExecuteAsync(query);
            }

            // Save the new list of external templates and generate hashes for them if we can.
            using var client = new HttpClient();

            var orderedExternalFiles = templateSettings.ExternalFiles.OrderBy(x => x.Ordering).ToList();
            for (var i = 0; i < orderedExternalFiles.Count; i++)
            {
                var externalFile = orderedExternalFiles[i];
                var dataForDatabase = externalFilesInDatabase.FirstOrDefault(x => String.Equals(x.Uri.ToString(), externalFile.Uri.ToString(), StringComparison.OrdinalIgnoreCase)) ?? externalFile;

                if (dataForDatabase.Uri.IsAbsoluteUri && String.IsNullOrWhiteSpace(dataForDatabase.Hash))
                {
                    var contents = await client.GetStringAsync(externalFile.Uri);
                    dataForDatabase.Hash = $"sha512-{StringHelpers.HashValue(contents, new HashSettingsModel {Algorithm = HashAlgorithms.SHA512, Representation = HashRepresentations.Base64})}";
                }

                dataForDatabase.Ordering = externalFile.Ordering;
                clientDatabaseConnection.AddParameter("externalFileId", dataForDatabase.Id);
                clientDatabaseConnection.AddParameter("externalFile", dataForDatabase.Uri.ToString());
                clientDatabaseConnection.AddParameter("hash", dataForDatabase.Hash ?? "");
                clientDatabaseConnection.AddParameter("ordering", i);

                query = dataForDatabase.Id > 0
                    ? $"UPDATE {WiserTableNames.WiserTemplateExternalFiles} SET hash = ?hash, ordering = ?ordering WHERE id = ?externalFileId"
                    : $"INSERT INTO {WiserTableNames.WiserTemplateExternalFiles} (template_id, external_file, hash, ordering) VALUES (?id, ?externalFile, ?hash, ?ordering)";

                await clientDatabaseConnection.ExecuteAsync(query);
            }
        }

        /// <inheritdoc />
        public async Task<int> CreateNewVersionAsync(int templateId)
        {
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("templateId", templateId);
            clientDatabaseConnection.AddParameter("now", DateTime.Now);

            // Copy the template itself to a new version.
            var query = $@"INSERT INTO {WiserTableNames.WiserTemplate} (
    parent_id,
    template_name,
    template_data,
    template_data_minified,
    template_type,
    version,
    template_id,
    added_on,
    added_by,
    changed_on,
    changed_by,
    published_environment,
    cache_minutes,
    login_required,
    login_role,
    login_redirect_url,
    linked_templates,
    ordering,
    insert_mode,
    load_always,
    disable_minifier,
    url_regex,
    external_files,
    grouping_create_object_instead_of_array,
    grouping_prefix,
    grouping_key,
    grouping_key_column_name,
    grouping_value_column_name,
    removed,
    is_scss_include_template,
    use_in_wiser_html_editors,
    pre_load_query,
    cache_location,
    return_not_found_when_pre_load_query_has_no_data,
    cache_regex,
    routine_type,
    routine_parameters,
    routine_return_type,
    is_default_header,
    is_default_footer,
    default_header_footer_regex,
    trigger_timing,
    trigger_event,
    trigger_table_name,
    is_partial,
    widget_content,
    widget_location,
    cache_per_url,
    cache_per_querystring,
    cache_per_hostname,
    cache_per_user,
    cache_using_regex,
    is_dirty
)
SELECT
    template.parent_id,
    template.template_name,
    template.template_data,
    template.template_data_minified,
    template.template_type,
    template.version + 1 AS version,
    template.template_id,
    ?now AS added_on,
    'Wiser' AS added_by,
    ?now AS changed_on,
    'Wiser' AS changed_by,
    0 AS published_environment,
    template.cache_minutes,
    template.login_required,
    template.login_role,
    template.login_redirect_url,
    template.linked_templates,
    template.ordering,
    template.insert_mode,
    template.load_always,
    template.disable_minifier,
    template.url_regex,
    template.external_files,
    template.grouping_create_object_instead_of_array,
    template.grouping_prefix,
    template.grouping_key,
    template.grouping_key_column_name,
    template.grouping_value_column_name,
    template.removed,
    template.is_scss_include_template,
    template.use_in_wiser_html_editors,
    template.pre_load_query,
    template.cache_location,
    template.return_not_found_when_pre_load_query_has_no_data,
    template.cache_regex,
    template.routine_type,
    template.routine_parameters,
    template.routine_return_type,
    template.is_default_header,
    template.is_default_footer,
    template.default_header_footer_regex,
    template.trigger_timing,
    template.trigger_event,
    template.trigger_table_name,
    template.is_partial,
    template.widget_content,
    template.widget_location,
    template.cache_per_url,
    template.cache_per_querystring,
    template.cache_per_hostname,
    template.cache_per_user,
    template.cache_using_regex,
    FALSE AS is_dirty
FROM {WiserTableNames.WiserTemplate} AS template
LEFT JOIN {WiserTableNames.WiserTemplate} AS otherVersion ON otherVersion.template_id = template.template_id AND otherVersion.version > template.version
WHERE template.template_id = ?templateId
AND otherVersion.id IS NULL;";
            var newId = await clientDatabaseConnection.InsertRecordAsync(query);

            // Copy the external files to the new version.
            clientDatabaseConnection.AddParameter("newId", newId);
            query = @$"INSERT INTO {WiserTableNames.WiserTemplateExternalFiles} (template_id, external_file, hash, ordering)
SELECT 
    template.id, 
    file.external_file,
    file.hash,
    file.ordering 
FROM {WiserTableNames.WiserTemplate} AS template
JOIN {WiserTableNames.WiserTemplate} AS otherVersion ON otherVersion.template_id = template.template_id AND otherVersion.version = template.version - 1
JOIN {WiserTableNames.WiserTemplateExternalFiles} AS file ON file.template_id = otherVersion.id
WHERE template.id = ?newId;";
            await clientDatabaseConnection.InsertRecordAsync(query);

            return Convert.ToInt32(newId);
        }

        /// <inheritdoc />
        public async Task<List<TemplateTreeViewDao>> GetTreeViewSectionAsync(int parentId)
        {
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("parentId", parentId);

            string rootTemplateName = null;
            var query = $@"SELECT template.template_name
FROM {WiserTableNames.WiserTemplate} AS template
LEFT JOIN {WiserTableNames.WiserTemplate} AS otherVersion ON otherVersion.template_id = template.template_id AND otherVersion.version > template.version
WHERE
    template.template_id = ?parentId
    AND template.parent_id IS NULL
    AND template.template_type = {(int)TemplateTypes.Directory}
    AND template.removed = 0
    AND otherVersion.id IS NULL
GROUP BY template.template_id
ORDER BY template.ordering ASC";

            var parentDataTable = await clientDatabaseConnection.GetAsync(query);
            if (parentDataTable.Rows.Count > 0)
            {
                // Is a root element, check the name to see if additional data should be retrieved, like views or triggers.
                rootTemplateName = parentDataTable.Rows[0].Field<string>("template_name");
            }

            query = $@"SELECT
    template.id,
    template.template_name,
    template.template_type,
    template.template_id,
    template.parent_id,
    COUNT(child.id) > 0 AS has_children
FROM {WiserTableNames.WiserTemplate} AS template
LEFT JOIN {WiserTableNames.WiserTemplate} AS otherVersion ON otherVersion.template_id = template.template_id AND otherVersion.version > template.version
LEFT JOIN {WiserTableNames.WiserTemplate} AS child ON child.parent_id = template.template_id
WHERE template.parent_id {(parentId == 0 ? $"IS NULL AND template.template_type = {(int)TemplateTypes.Directory}" : "= ?parentId")}
AND template.removed = 0
AND otherVersion.id IS NULL
GROUP BY template.template_id
ORDER BY template.ordering ASC";

            var dataTable = await clientDatabaseConnection.GetAsync(query);

            var treeViewItems = dataTable.Rows.Cast<DataRow>()
                .Select(row => new TemplateTreeViewDao
                {
                    HasChildren = Convert.ToBoolean(row["has_children"]),
                    ParentId = row.Field<int?>("parent_id"),
                    TemplateId = row.Field<int>("template_id"),
                    TemplateName = row.Field<string>("template_name"),
                    TemplateType = row.Field<TemplateTypes>("template_type")
                }).ToList();

            // Check if additional items should be retrieved when retrieving from a root item.
            // These can be views, routines, or triggers.
            if (!String.IsNullOrWhiteSpace(rootTemplateName))
            {
                var existingItems = treeViewItems.Select(treeViewItem => treeViewItem.TemplateName).ToList();

                switch (rootTemplateName.ToUpperInvariant())
                {
                    case "ROUTINES":
                        treeViewItems.AddRange(await GetRoutinesAsTreeViewItemsAsync(parentId, existingItems));
                        break;
                    case "VIEWS":
                        treeViewItems.AddRange(await GetViewsAsTreeViewItemsAsync(parentId, existingItems));
                        break;
                    case "TRIGGERS":
                        treeViewItems.AddRange(await GetTriggersAsTreeViewItemsAsync(parentId, existingItems));
                        break;
                }
            }

            return treeViewItems;
        }

        /// <inheritdoc />
        public async Task<List<SearchResultModel>> SearchAsync(string searchValue, string encryptionKey)
        {
            var searchForId = false;
            if (searchValue.StartsWith("#"))
            {
                searchForId = true;
                searchValue = searchValue[1..];
            }

            var whereClauseForTemplates = searchForId ? "template.template_id = ?searchValue" : "(template.template_name LIKE CONCAT('%', ?searchValue, '%') OR template.template_data LIKE CONCAT('%', ?searchValue, '%'))";
            var whereClauseForDynamicContent = searchForId ? "component.content_id = ?searchValue" : "(component.title LIKE CONCAT('%', ?searchValue, '%') OR component.settings LIKE CONCAT('%', ?searchValue, '%') OR component.component_mode LIKE CONCAT('%', ?searchValue, '%'))";
            var query = $@"SELECT 
	template.template_id,
	template.template_type,
	template.template_name,
	parent1.template_id AS parent1Id,
	parent1.template_name AS parent1Name,
	parent2.template_id AS parent2Id,
	parent2.template_name AS parent2Name,
	parent3.template_id AS parent3Id,
	parent3.template_name AS parent3Name,
	parent4.template_id AS parent4Id,
	parent4.template_name AS parent4Name,
	parent5.template_id AS parent5Id,
	parent5.template_name AS parent5Name,
	parent6.template_id AS parent6Id,
	parent6.template_name AS parent6Name,
	parent7.template_id AS parent7Id,
	parent7.template_name AS parent7Name,
	parent8.template_id AS parent8Id,
	parent8.template_name AS parent8Name
FROM (
	# The actual searching is done in an inner query and the parents are retrieved in the outer query, 
	# so that we only get the parents of the actual results instead of everything in the database.
	SELECT 
		template.template_id,
		template.template_type,
		template.template_name,
		template.parent_id
	FROM {WiserTableNames.WiserTemplate} AS template
	LEFT JOIN {WiserTableNames.WiserTemplate} AS otherVersion ON otherVersion.template_id = template.template_id AND otherVersion.version > template.version
	WHERE {whereClauseForTemplates}
	AND template.removed = 0
	AND otherVersion.id IS NULL
	
	UNION
	
	SELECT 
		template.template_id,
		template.template_type,
		template.template_name,
		template.parent_id
	FROM (
		# The actual searching is done in an inner query and the parents are retrieved in the outer query, 
		# so that we only get the parents of the actual results instead of everything in the database.
		SELECT component.content_id
		FROM {WiserTableNames.WiserDynamicContent} AS component
		LEFT JOIN {WiserTableNames.WiserDynamicContent} AS otherVersion ON otherVersion.content_id = component.content_id AND otherVersion.version > component.version
		WHERE {whereClauseForDynamicContent}
		AND component.removed = 0
		AND otherVersion.id IS NULL
	) AS component
	JOIN {WiserTableNames.WiserTemplateDynamicContent} AS link ON link.content_id = component.content_id
	JOIN {WiserTableNames.WiserTemplate} AS template ON template.template_id = link.destination_template_id
	LEFT JOIN {WiserTableNames.WiserTemplate} AS otherVersionTemplate ON otherVersionTemplate.template_id = template.template_id AND otherVersionTemplate.version > template.version
	WHERE otherVersionTemplate.id IS NULL
	GROUP BY template.template_id
) AS template
LEFT JOIN {WiserTableNames.WiserTemplate} AS parent1 ON parent1.template_id = template.parent_id AND parent1.version = (SELECT MAX(version) FROM {WiserTableNames.WiserTemplate} WHERE template_id = template.parent_id)
LEFT JOIN {WiserTableNames.WiserTemplate} AS parent2 ON parent2.template_id = parent1.parent_id AND parent2.version = (SELECT MAX(version) FROM {WiserTableNames.WiserTemplate} WHERE template_id = parent1.parent_id)
LEFT JOIN {WiserTableNames.WiserTemplate} AS parent3 ON parent3.template_id = parent2.parent_id AND parent3.version = (SELECT MAX(version) FROM {WiserTableNames.WiserTemplate} WHERE template_id = parent2.parent_id)
LEFT JOIN {WiserTableNames.WiserTemplate} AS parent4 ON parent4.template_id = parent3.parent_id AND parent4.version = (SELECT MAX(version) FROM {WiserTableNames.WiserTemplate} WHERE template_id = parent3.parent_id)
LEFT JOIN {WiserTableNames.WiserTemplate} AS parent5 ON parent5.template_id = parent4.parent_id AND parent5.version = (SELECT MAX(version) FROM {WiserTableNames.WiserTemplate} WHERE template_id = parent4.parent_id)
LEFT JOIN {WiserTableNames.WiserTemplate} AS parent6 ON parent6.template_id = parent5.parent_id AND parent6.version = (SELECT MAX(version) FROM {WiserTableNames.WiserTemplate} WHERE template_id = parent5.parent_id)
LEFT JOIN {WiserTableNames.WiserTemplate} AS parent7 ON parent7.template_id = parent6.parent_id AND parent7.version = (SELECT MAX(version) FROM {WiserTableNames.WiserTemplate} WHERE template_id = parent6.parent_id)
LEFT JOIN {WiserTableNames.WiserTemplate} AS parent8 ON parent8.template_id = parent7.parent_id AND parent8.version = (SELECT MAX(version) FROM {WiserTableNames.WiserTemplate} WHERE template_id = parent7.parent_id)";

            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("searchValue", searchValue);
            var dataTable = await clientDatabaseConnection.GetAsync(query);

            var allItems = new List<SearchResultModel>();

            foreach (DataRow dataRow in dataTable.Rows)
            {
                var templateId = dataRow.Field<int>("template_id");
                if (allItems.Any(i => i.TemplateId == templateId))
                {
                    continue;
                }

                var result = new SearchResultModel
                {
                    TemplateId = templateId,
                    TemplateName = dataRow.Field<string>("template_name"),
                    Type = dataRow.Field<TemplateTypes>("template_type"),
                    IsFolder = dataRow.Field<TemplateTypes>("template_type") == TemplateTypes.Directory
                };

                var previousParent = result;
                var parentCounter = 1;
                while (!dataRow.IsNull($"parent{parentCounter}Id"))
                {
                    var id = dataRow.Field<int>($"parent{parentCounter}Id");
                    previousParent.ParentId = id;
                    var newParent = allItems.SingleOrDefault(i => i.TemplateId == id);
                    if (newParent == null)
                    {
                        newParent = new SearchResultModel
                        {
                            TemplateId = id,
                            TemplateName = dataRow.Field<string>($"parent{parentCounter}Name"),
                            Type = TemplateTypes.Directory,
                            IsFolder = true
                        };

                        allItems.Add(newParent);
                    }

                    previousParent = newParent;
                    parentCounter++;
                }

                allItems.Add(result);
            }

            void AddChildren(List<SearchResultModel> currentLevel)
            {
                foreach (var result in currentLevel)
                {
                    result.ChildNodes = allItems.Where(i => i.ParentId == result.TemplateId).Cast<TemplateTreeViewModel>().ToList();
                    AddChildren(result.ChildNodes.Cast<SearchResultModel>().ToList());
                }
            }

            var results = allItems.Where(i => i.ParentId == 0).ToList();
            AddChildren(results);

            // If an ID was searched return the results.
            if (searchForId)
            {
                return results;
            }

            // Load all XML templates to check the search value in encrypted templates.
            query = $@"SELECT template.template_id, template.template_data
FROM {WiserTableNames.WiserTemplate} AS template
LEFT JOIN {WiserTableNames.WiserTemplate} AS otherVersion ON otherVersion.template_id = template.template_id AND otherVersion.version > template.version
WHERE template.template_type = {(int)TemplateTypes.Xml}
AND template.removed = 0
AND otherVersion.id IS NULL";

            dataTable = await clientDatabaseConnection.GetAsync(query);

            // Local function to add unique results to the all items collection.
            void AddEncryptedTemplates(List<SearchResultModel> currentLevel)
            {
                foreach (var result in currentLevel)
                {
                    if (!allItems.Any(i => i.TemplateId == result.TemplateId))
                    {
                        allItems.Add(result);
                    }

                    AddEncryptedTemplates(result.ChildNodes.Cast<SearchResultModel>().ToList());
                }
            }

            var encryptedTemplatesAdded = false;

            foreach (DataRow dataRow in dataTable.Rows)
            {
                var templateId = dataRow.Field<int>("template_id");
                var templateData = dataRow.Field<string>("template_data");

                // Non encrypted templates are already checked by thr query and can be skipped.
                if (allItems.Any(i => i.TemplateId == templateId) || String.IsNullOrWhiteSpace(templateData) || templateData.StartsWith("<") || templateData.DecryptWithAes(encryptionKey, useSlowerButMoreSecureMethod: true).IndexOf(searchValue, StringComparison.OrdinalIgnoreCase) == -1)
                {
                    continue;
                }

                var searchResults = await SearchAsync($"#{templateId}", encryptionKey);
                AddEncryptedTemplates(searchResults);
                encryptedTemplatesAdded = true;
            }


            if (!encryptedTemplatesAdded)
            {
                return results;
            }

            // Reset the result with the added values from the encrypted templates.
            results = allItems.Where(i => i.ParentId == 0).ToList();
            AddChildren(results);

            return results;
        }

        /// <inheritdoc/>
        public async Task<int> CreateAsync(string name, int? parent, TemplateTypes type, string username, string editorValue = null, int? ordering  = null)
        {
            ordering ??= await GetHighestOrderNumberOfChildrenAsync(parent) + 1;
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("name", name);
            clientDatabaseConnection.AddParameter("parent", parent);
            clientDatabaseConnection.AddParameter("type", type);
            clientDatabaseConnection.AddParameter("now", DateTime.Now);
            clientDatabaseConnection.AddParameter("username", username);
            clientDatabaseConnection.AddParameter("ordering", ordering);
            clientDatabaseConnection.AddParameter("editorValue", editorValue);

            var dataTable = await clientDatabaseConnection.GetAsync(@$"SET @id = (SELECT MAX(template_id)+1 FROM {WiserTableNames.WiserTemplate});
INSERT INTO {WiserTableNames.WiserTemplate} (parent_id, template_name, template_type, version, template_id, added_on, added_by, changed_on, changed_by, published_environment, ordering, template_data, cache_minutes)
VALUES (?parent, ?name, ?type, 1, @id, ?now, ?username, ?now, ?username, 1, ?ordering, ?editorValue, -1);
SELECT @id;");

            return Convert.ToInt32(dataTable.Rows[0]["@id"]);
        }

        /// <inheritdoc />
        public async Task FixTreeViewOrderingAsync(int parentId)
        {
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("parentId", parentId);

            var query = $@"SET @ordering = 0;
UPDATE {WiserTableNames.WiserTemplate} AS template
JOIN (
	SELECT
		x.template_id,
		@ordering := @ordering + 1 AS newOrdering
	FROM (
		SELECT template.template_id
		FROM {WiserTableNames.WiserTemplate} AS template
		LEFT JOIN {WiserTableNames.WiserTemplate} AS otherVersion ON otherVersion.template_id = template.template_id AND otherVersion.version > template.version
		WHERE template.parent_id = ?parentId
        AND template.removed = 0
		AND otherVersion.id IS NULL
		ORDER BY template.ordering ASC, template.template_type DESC, template.template_name ASC
	) AS x
) AS ordering ON ordering.template_id = template.template_id
SET template.ordering = ordering.newOrdering";
            await clientDatabaseConnection.ExecuteAsync(query);
        }

        /// <inheritdoc />
        public async Task<TemplateSettingsModel> GetParentAsync(int templateId)
        {
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("id", templateId);

            var query = $@"SELECT parent.template_id, parent.template_name
FROM {WiserTableNames.WiserTemplate} AS template
LEFT JOIN {WiserTableNames.WiserTemplate} AS otherVersion ON otherVersion.template_id = template.template_id AND otherVersion.version > template.version
JOIN {WiserTableNames.WiserTemplate} AS parent ON parent.template_id = template.parent_id AND parent.removed = 0
WHERE template.template_id = ?id
AND template.removed = 0
AND otherVersion.id IS NULL
ORDER BY parent.version DESC
LIMIT 1";
            var dataTable = await clientDatabaseConnection.GetAsync(query);

            if (dataTable.Rows.Count == 0)
            {
                return null;
            }

            var id = dataTable.Rows[0].Field<int?>("template_id");
            if (!id.HasValue || id.Value == 0)
            {
                return null;
            }

            return new TemplateSettingsModel
            {
                TemplateId = id.Value,
                Name = dataTable.Rows[0].Field<string>("template_name")
            };
        }

        /// <inheritdoc />
        public async Task<int> GetOrderingAsync(int templateId)
        {
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("id", templateId);

            var query = $@"SELECT ordering
FROM {WiserTableNames.WiserTemplate}
WHERE template_id = ?id
AND removed = 0
ORDER BY version DESC
LIMIT 1";
            var dataTable = await clientDatabaseConnection.GetAsync(query);
            return dataTable.Rows.Count == 0 ? 0 : dataTable.Rows[0].Field<int>("ordering");
        }

        /// <inheritdoc />
        public async Task<int> GetHighestOrderNumberOfChildrenAsync(int? templateId)
        {
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("id", templateId);

            var query = $@"SELECT IFNULL(MAX(template.ordering), 0) AS ordering
FROM {WiserTableNames.WiserTemplate} AS template
LEFT JOIN {WiserTableNames.WiserTemplate} AS otherVersion ON otherVersion.template_id = template.template_id AND otherVersion.version > template.version
WHERE template.parent_id = ?id
AND template.removed = 0
AND otherVersion.id IS NULL";
            var dataTable = await clientDatabaseConnection.GetAsync(query);
            return dataTable.Rows.Count == 0 ? 0 : Convert.ToInt32(dataTable.Rows[0]["ordering"]);
        }

        /// <inheritdoc />
        public async Task MoveAsync(int sourceId, int destinationId, int sourceParentId, int destinationParentId, int oldOrderNumber, int newOrderNumber, TreeViewDropPositions dropPosition, string username)
        {
            try
            {
                await clientDatabaseConnection.BeginTransactionAsync();

                string query;
                clientDatabaseConnection.ClearParameters();
                clientDatabaseConnection.AddParameter("sourceId", sourceId);
                clientDatabaseConnection.AddParameter("destinationId", destinationId);
                clientDatabaseConnection.AddParameter("sourceParentId", sourceParentId);
                clientDatabaseConnection.AddParameter("destinationParentId", destinationParentId);
                clientDatabaseConnection.AddParameter("oldOrderNumber", oldOrderNumber);
                clientDatabaseConnection.AddParameter("newOrderNumber", newOrderNumber);
                clientDatabaseConnection.AddParameter("now", DateTime.Now);
                clientDatabaseConnection.AddParameter("username", username);

                // When drop position is before or after, move other items with the same parent 1 position lower (except when placing an item on top of a directory, then it should be added last).
                if (dropPosition != TreeViewDropPositions.Over)
                {
                    query = $@"UPDATE {WiserTableNames.WiserTemplate}
SET ordering = ordering + 1
WHERE parent_id = ?destinationParentId
AND ordering >= ?newOrderNumber
AND template_id <> ?sourceId
AND removed = 0";

                    await clientDatabaseConnection.ExecuteAsync(query);
                }

                // Move the template to it's new position.
                query = $@"UPDATE {WiserTableNames.WiserTemplate} 
SET parent_id = ?destinationParentId, ordering = ?newOrderNumber, changed_on = ?now, changed_by = ?username
WHERE template_id = ?sourceId
AND parent_id = ?sourceParentId
AND removed = 0";
                await clientDatabaseConnection.ExecuteAsync(query);

                // Fill gap in old parent directory (move items one place higher).
                query = $@"UPDATE {WiserTableNames.WiserTemplate}
SET ordering = ordering - 1
WHERE parent_id = ?sourceParentId
AND ordering > ?oldOrderNumber
AND removed = 0";
                await clientDatabaseConnection.ExecuteAsync(query);

                await clientDatabaseConnection.CommitTransactionAsync();
            }
            catch
            {
                await clientDatabaseConnection.RollbackTransactionAsync();
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<StringBuilder> GetScssIncludesForScssTemplateAsync(int templateId)
        {
            // First we need to find the root ID.
            // Some tenants have multiple websites in the same Wiser instance and can therefor have multiple SCSS root directories.
            // We need to find the root directory for the given template, so that we don't include SCSS from a different website.
            string name;
            var scssRootId = templateId;

            do
            {
                var parent = await GetParentAsync(scssRootId);
                if (parent == null)
                {
                    break;
                }

                name = parent.Name;
                scssRootId = parent.TemplateId;
            } while (scssRootId > 0 && !String.IsNullOrWhiteSpace(name) && !name.Equals("SCSS", StringComparison.OrdinalIgnoreCase));

            var result = new StringBuilder();

            clientDatabaseConnection.AddParameter("rootId", scssRootId);

            var query = $@"SELECT template.template_data
FROM {WiserTableNames.WiserTemplate} AS template
LEFT JOIN {WiserTableNames.WiserTemplate} AS otherVersion ON otherVersion.template_id = template.template_id AND otherVersion.version > template.version
LEFT JOIN {WiserTableNames.WiserTemplate} AS parent1 ON parent1.template_id = template.parent_id AND parent1.version = (SELECT MAX(version) FROM {WiserTableNames.WiserTemplate} WHERE template_id = template.parent_id)
LEFT JOIN {WiserTableNames.WiserTemplate} AS parent2 ON parent2.template_id = parent1.parent_id AND parent2.version = (SELECT MAX(version) FROM {WiserTableNames.WiserTemplate} WHERE template_id = parent1.parent_id)
LEFT JOIN {WiserTableNames.WiserTemplate} AS parent3 ON parent3.template_id = parent2.parent_id AND parent3.version = (SELECT MAX(version) FROM {WiserTableNames.WiserTemplate} WHERE template_id = parent2.parent_id)
LEFT JOIN {WiserTableNames.WiserTemplate} AS parent4 ON parent4.template_id = parent3.parent_id AND parent4.version = (SELECT MAX(version) FROM {WiserTableNames.WiserTemplate} WHERE template_id = parent3.parent_id)
LEFT JOIN {WiserTableNames.WiserTemplate} AS parent5 ON parent5.template_id = parent4.parent_id AND parent5.version = (SELECT MAX(version) FROM {WiserTableNames.WiserTemplate} WHERE template_id = parent4.parent_id)
LEFT JOIN {WiserTableNames.WiserTemplate} AS parent6 ON parent6.template_id = parent5.parent_id AND parent6.version = (SELECT MAX(version) FROM {WiserTableNames.WiserTemplate} WHERE template_id = parent5.parent_id)
LEFT JOIN {WiserTableNames.WiserTemplate} AS parent7 ON parent7.template_id = parent6.parent_id AND parent7.version = (SELECT MAX(version) FROM {WiserTableNames.WiserTemplate} WHERE template_id = parent6.parent_id)
LEFT JOIN {WiserTableNames.WiserTemplate} AS parent8 ON parent8.template_id = parent7.parent_id AND parent8.version = (SELECT MAX(version) FROM {WiserTableNames.WiserTemplate} WHERE template_id = parent7.parent_id)
WHERE template.template_type = {(int) TemplateTypes.Scss}
AND template.is_scss_include_template = 1
AND template.removed = 0
AND otherVersion.id IS NULL
AND template.template_data IS NOT NULL
AND (?rootId = 0 OR ?rootId IN (parent8.template_id, parent7.template_id, parent6.template_id, parent5.template_id, parent4.template_id, parent3.template_id, parent2.template_id, parent1.template_id))
ORDER BY parent8.ordering, parent7.ordering, parent6.ordering, parent5.ordering, parent4.ordering, parent3.ordering, parent2.ordering, parent1.ordering, template.ordering";
            var dataTable = await clientDatabaseConnection.GetAsync(query);

            foreach (DataRow dataRow in dataTable.Rows)
            {
                result.AppendLine(dataRow.Field<string>("template_data"));
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<List<TemplateSettingsModel>> GetScssTemplatesThatAreNotIncludesAsync(int templateId)
        {
            // First we need to find the root ID.
            // Some tenants have multiple websites in the same Wiser instance and can therefor have multiple SCSS root directories.
            // We need to find the root directory for the given template, so that we don't include SCSS from a different website.
            string name;
            var scssRootId = templateId;

            do
            {
                var parent = await GetParentAsync(scssRootId);
                if (parent == null)
                {
                    break;
                }

                name = parent.Name;
                scssRootId = parent.TemplateId;
            } while (scssRootId > 0 && !String.IsNullOrWhiteSpace(name) && !name.Equals("SCSS", StringComparison.OrdinalIgnoreCase));

            clientDatabaseConnection.AddParameter("rootId", scssRootId);

            var query = $@"SELECT
    template.template_id, 
    template.parent_id, 
    template.template_type, 
    template.template_name, 
    template.template_data, 
    template.version,
    template.added_on, 
    template.added_by,   
    template.changed_on, 
    template.changed_by,   
    template.cache_per_url,   
    template.cache_per_querystring,   
    template.cache_per_hostname,
    template.cache_per_user,
    template.cache_using_regex,  
    template.cache_minutes, 
    template.cache_location, 
    template.cache_regex,
    template.login_required, 
    template.login_role, 
    template.login_redirect_url, 
    template.linked_templates, 
    template.ordering,
    template.insert_mode,
    template.load_always,
    template.disable_minifier,
    template.url_regex,
    template.external_files,
    IF(COUNT(externalFiles.external_file) = 0, NULL, JSON_ARRAYAGG(JSON_OBJECT('uri', externalFiles.external_file, 'hash', externalFiles.hash, 'ordering', externalFiles.ordering))) AS external_files_json,
    template.grouping_create_object_instead_of_array,
    template.grouping_prefix,
    template.grouping_key,
    template.grouping_key_column_name,
    template.grouping_value_column_name,
    template.is_scss_include_template,
    template.use_in_wiser_html_editors,
    template.pre_load_query,
    template.return_not_found_when_pre_load_query_has_no_data,
    template.widget_content,
    template.widget_location
FROM {WiserTableNames.WiserTemplate} AS template
LEFT JOIN {WiserTableNames.WiserTemplate} AS otherVersion ON otherVersion.template_id = template.template_id AND otherVersion.version > template.version
LEFT JOIN {WiserTableNames.WiserTemplate} AS parent1 ON parent1.template_id = template.parent_id AND parent1.version = (SELECT MAX(version) FROM {WiserTableNames.WiserTemplate} WHERE template_id = template.parent_id)
LEFT JOIN {WiserTableNames.WiserTemplate} AS parent2 ON parent2.template_id = parent1.parent_id AND parent2.version = (SELECT MAX(version) FROM {WiserTableNames.WiserTemplate} WHERE template_id = parent1.parent_id)
LEFT JOIN {WiserTableNames.WiserTemplate} AS parent3 ON parent3.template_id = parent2.parent_id AND parent3.version = (SELECT MAX(version) FROM {WiserTableNames.WiserTemplate} WHERE template_id = parent2.parent_id)
LEFT JOIN {WiserTableNames.WiserTemplate} AS parent4 ON parent4.template_id = parent3.parent_id AND parent4.version = (SELECT MAX(version) FROM {WiserTableNames.WiserTemplate} WHERE template_id = parent3.parent_id)
LEFT JOIN {WiserTableNames.WiserTemplate} AS parent5 ON parent5.template_id = parent4.parent_id AND parent5.version = (SELECT MAX(version) FROM {WiserTableNames.WiserTemplate} WHERE template_id = parent4.parent_id)
LEFT JOIN {WiserTableNames.WiserTemplate} AS parent6 ON parent6.template_id = parent5.parent_id AND parent6.version = (SELECT MAX(version) FROM {WiserTableNames.WiserTemplate} WHERE template_id = parent5.parent_id)
LEFT JOIN {WiserTableNames.WiserTemplate} AS parent7 ON parent7.template_id = parent6.parent_id AND parent7.version = (SELECT MAX(version) FROM {WiserTableNames.WiserTemplate} WHERE template_id = parent6.parent_id)
LEFT JOIN {WiserTableNames.WiserTemplate} AS parent8 ON parent8.template_id = parent7.parent_id AND parent8.version = (SELECT MAX(version) FROM {WiserTableNames.WiserTemplate} WHERE template_id = parent7.parent_id)
LEFT JOIN {WiserTableNames.WiserTemplateExternalFiles} AS externalFiles ON externalFiles.template_id = template.id
WHERE template.template_type = {(int) TemplateTypes.Scss}
AND template.is_scss_include_template = 0
AND template.removed = 0
AND otherVersion.id IS NULL
AND template.template_data IS NOT NULL
AND (?rootId = 0 OR ?rootId IN (parent8.template_id, parent7.template_id, parent6.template_id, parent5.template_id, parent4.template_id, parent3.template_id, parent2.template_id, parent1.template_id))
ORDER BY parent8.ordering, parent7.ordering, parent6.ordering, parent5.ordering, parent4.ordering, parent3.ordering, parent2.ordering, parent1.ordering, template.ordering";
            var dataTable = await clientDatabaseConnection.GetAsync(query);

            return dataTable.Rows.Cast<DataRow>().Select(TemplateHelpers.DataRowToTemplateSettingsModel).ToList();
        }

        /// <inheritdoc />
        public async Task<bool> DeleteAsync(int templateId, string username, bool alsoDeleteChildren = true)
        {
            clientDatabaseConnection.AddParameter("templateId", templateId);
            clientDatabaseConnection.AddParameter("username", username);
            clientDatabaseConnection.AddParameter("now", DateTime.Now);

            // First get some information that we need.
            var query = $@"SELECT
    parent_id,
    template_name,
    template_type,
    ordering,
    version,
    removed
FROM {WiserTableNames.WiserTemplate}
WHERE template_id = ?templateId
ORDER BY version DESC
LIMIT 1";
            var dataTable = await clientDatabaseConnection.GetAsync(query);
            if (dataTable.Rows.Count == 0 || Convert.ToBoolean(dataTable.Rows[0]["removed"]))
            {
                // Don't do anything if the template doesn't exist or is already deleted.
                return false;
            }

            var templateName = dataTable.Rows[0].Field<string>("template_name");
            var templateType = (TemplateTypes)dataTable.Rows[0].Field<int>("template_type");

            // Add a new row/version of this template where all settings/data are empty and removed is set to 1.
            clientDatabaseConnection.AddParameter("parentId", dataTable.Rows[0].Field<int>("parent_id"));
            clientDatabaseConnection.AddParameter("name", templateName);
            clientDatabaseConnection.AddParameter("type", dataTable.Rows[0].Field<int>("template_type"));
            clientDatabaseConnection.AddParameter("ordering", dataTable.Rows[0].Field<int>("ordering"));
            clientDatabaseConnection.AddParameter("version", dataTable.Rows[0].Field<int>("version") + 1);

            query = $@"INSERT INTO {WiserTableNames.WiserTemplate} (template_id, parent_id, template_name, template_type, ordering, version, removed, added_on, added_by, changed_on, changed_by, is_dirty)
VALUES (?templateId, ?parentId, ?name, ?type, ?ordering, ?version, 1, ?now, ?username, ?now, ?username, TRUE)";
            await clientDatabaseConnection.ExecuteAsync(query);

            if (alsoDeleteChildren)
            {
                // Delete all children of the template by also adding new versions with removed = 1 for them.
                query = $@"INSERT INTO {WiserTableNames.WiserTemplate} (template_id, parent_id, template_name, template_type, ordering, version, removed, added_on, added_by, changed_on, changed_by, is_dirty)
SELECT 
    template.template_id,
    template.parent_id,
    template.template_name,
    template.template_type,
    template.ordering,
    template.version + 1,
    1,
    ?now,
    ?username,
    ?now,
    ?username,
    TRUE
FROM {WiserTableNames.WiserTemplate} AS template
LEFT JOIN {WiserTableNames.WiserTemplate} AS otherVersion ON otherVersion.template_id = template.template_id AND otherVersion.version > template.version
WHERE template.parent_id = ?templateId
AND template.removed = 0
AND otherVersion.id IS NULL";
                await clientDatabaseConnection.ExecuteAsync(query);
            }

            // Also delete the view, routine, or trigger that this template was managing.
            switch (templateType)
            {
                case TemplateTypes.View:
                    await clientDatabaseConnection.ExecuteAsync($"DROP VIEW IF EXISTS `{templateName}`;");
                    break;
                case TemplateTypes.Routine:
                    await clientDatabaseConnection.ExecuteAsync($"DROP FUNCTION IF EXISTS `{templateName}`; DROP PROCEDURE IF EXISTS `{templateName}`;");
                    break;
                case TemplateTypes.Trigger:
                    await clientDatabaseConnection.ExecuteAsync($"DROP TRIGGER IF EXISTS `{templateName}`;");
                    break;
            }

            return true;
        }

        /// <inheritdoc />
        public void DecryptEditorValueIfEncrypted(string encryptionKey, TemplateSettingsModel rawTemplateModel)
        {
            if (rawTemplateModel.Type == TemplateTypes.Xml && !String.IsNullOrWhiteSpace(rawTemplateModel.EditorValue) && !rawTemplateModel.EditorValue.Trim().StartsWith("<"))
            {
                rawTemplateModel.EditorValue = rawTemplateModel.EditorValue.DecryptWithAes(encryptionKey, useSlowerButMoreSecureMethod: true);
            }
        }

        /// <inheritdoc />
        public async Task DeployToBranchAsync(List<int> templateIds, string branchDatabaseName)
        {
            var temporaryTableName = $"temp_templates_{Guid.NewGuid():N}";
            // Branches always exist within the same database cluster, so we don't need to make a new connection for it.
            var query = $@"CREATE TABLE `{branchDatabaseName}`.`{temporaryTableName}` LIKE {WiserTableNames.WiserTemplate};
INSERT INTO `{branchDatabaseName}`.`{temporaryTableName}`
SELECT template.*
FROM {WiserTableNames.WiserTemplate} AS template
LEFT JOIN {WiserTableNames.WiserTemplate} AS otherVersion ON otherVersion.template_id = template.template_id AND otherVersion.version > template.version
WHERE template.template_id IN ({String.Join(", ", templateIds)})
AND otherVersion.id IS NULL;

UPDATE `{branchDatabaseName}`.{WiserTableNames.WiserTemplate} AS template
JOIN `{branchDatabaseName}`.`{temporaryTableName}` AS temp ON temp.template_id = template.template_id AND temp.version = template.version
SET template.parent_id = temp.parent_id,
    template.template_name = temp.template_name,
    template.template_data = temp.template_data,
    template.template_data_minified = temp.template_data_minified,
    template.template_type = temp.template_type,
    template.version = temp.version,
    template.template_id = temp.template_id,
    template.added_on = temp.added_on,
    template.added_by = temp.added_by,
    template.changed_on = temp.changed_on,
    template.changed_by = temp.changed_by,
    template.published_environment = temp.published_environment,
    template.cache_minutes = temp.cache_minutes,
    template.login_required = temp.login_required,
    template.login_role = temp.login_role,
    template.login_redirect_url = temp.login_redirect_url,
    template.linked_templates = temp.linked_templates,
    template.ordering = temp.ordering,
    template.insert_mode = temp.insert_mode,
    template.load_always = temp.load_always,
    template.disable_minifier = temp.disable_minifier,
    template.url_regex = temp.url_regex,
    template.external_files = temp.external_files,
    template.grouping_create_object_instead_of_array = temp.grouping_create_object_instead_of_array,
    template.grouping_prefix = temp.grouping_prefix,
    template.grouping_key = temp.grouping_key,
    template.grouping_key_column_name = temp.grouping_key_column_name,
    template.grouping_value_column_name = temp.grouping_value_column_name,
    template.removed = temp.removed,
    template.is_scss_include_template = temp.is_scss_include_template,
    template.use_in_wiser_html_editors = temp.use_in_wiser_html_editors,
    template.pre_load_query = temp.pre_load_query,
    template.cache_location = temp.cache_location,
    template.return_not_found_when_pre_load_query_has_no_data = temp.return_not_found_when_pre_load_query_has_no_data,
    template.cache_regex = temp.cache_regex,
    template.routine_type = temp.routine_type,
    template.routine_parameters = temp.routine_parameters,
    template.routine_return_type = temp.routine_return_type,
    template.is_default_header = temp.is_default_header,
    template.is_default_footer = temp.is_default_footer,
    template.default_header_footer_regex = temp.default_header_footer_regex,
    template.trigger_timing = temp.trigger_timing,
    template.trigger_event = temp.trigger_event,
    template.trigger_table_name = temp.trigger_table_name,
    template.is_partial = temp.is_partial,
    template.widget_content = temp.widget_content,
    template.widget_location = temp.widget_location,
    template.cache_per_url = temp.cache_per_url,
    template.cache_per_querystring = temp.cache_per_querystring,
    template.cache_per_hostname = temp.cache_per_hostname,
    template.cache_per_user = temp.cache_per_user,
    template.cache_using_regex = temp.cache_using_regex,
    template.is_dirty = temp.is_dirty;

INSERT INTO `{branchDatabaseName}`.{WiserTableNames.WiserTemplate} (
	parent_id,
    template_name,
    template_data,
    template_data_minified,
    template_type,
    version,
    template_id,
    added_on,
    added_by,
    changed_on,
    changed_by,
    published_environment,
    cache_minutes,
    login_required,
    login_role,
    login_redirect_url,
    linked_templates,
    ordering,
    insert_mode,
    load_always,
    disable_minifier,
    url_regex,
    external_files,
    grouping_create_object_instead_of_array,
    grouping_prefix,
    grouping_key,
    grouping_key_column_name,
    grouping_value_column_name,
    removed,
    is_scss_include_template,
    use_in_wiser_html_editors,
    pre_load_query,
    cache_location,
    return_not_found_when_pre_load_query_has_no_data,
    cache_regex,
    routine_type,
    routine_parameters,
    routine_return_type,
    is_default_header,
    is_default_footer,
    default_header_footer_regex,
    trigger_timing,
    trigger_event,
    trigger_table_name,
    is_partial,
    widget_content,
    widget_location,
    cache_per_url,
    cache_per_querystring,
    cache_per_hostname,
    cache_per_user,
    cache_using_regex,
    is_dirty
) 
SELECT 
	parent_id,
    template_name,
    template_data,
    template_data_minified,
    template_type,
    version,
    template_id,
    added_on,
    added_by,
    changed_on,
    changed_by,
    published_environment,
    cache_minutes,
    login_required,
    login_role,
    login_redirect_url,
    linked_templates,
    ordering,
    insert_mode,
    load_always,
    disable_minifier,
    url_regex,
    external_files,
    grouping_create_object_instead_of_array,
    grouping_prefix,
    grouping_key,
    grouping_key_column_name,
    grouping_value_column_name,
    removed,
    is_scss_include_template,
    use_in_wiser_html_editors,
    pre_load_query,
    cache_location,
    return_not_found_when_pre_load_query_has_no_data,
    cache_regex,
    routine_type,
    routine_parameters,
    routine_return_type,
    is_default_header,
    is_default_footer,
    default_header_footer_regex,
    trigger_timing,
    trigger_event,
    trigger_table_name,
    is_partial,
    widget_content,
    widget_location,
    cache_per_url,
    cache_per_querystring,
    cache_per_hostname,
    cache_per_user,
    cache_using_regex,
    is_dirty
FROM `{branchDatabaseName}`.`{temporaryTableName}` AS temp
WHERE NOT EXISTS (
    SELECT 1
    FROM `{branchDatabaseName}`.{WiserTableNames.WiserTemplate} AS template
    WHERE template.template_id = temp.template_id
    AND template.version = temp.version
);

DELETE FROM `{branchDatabaseName}`.{WiserTableNames.WiserTemplateExternalFiles} WHERE template_id IN (SELECT id FROM `{branchDatabaseName}`.`{temporaryTableName}`);
INSERT INTO `{branchDatabaseName}`.{WiserTableNames.WiserTemplateExternalFiles} (template_id, external_file, hash, ordering)
SELECT 
    branchTemplate.id, 
    file.external_file,
    file.hash,
    file.ordering 
FROM `{branchDatabaseName}`.`{temporaryTableName}` AS template
JOIN {WiserTableNames.WiserTemplateExternalFiles} AS file ON file.template_id = template.id
JOIN `{branchDatabaseName}`.{WiserTableNames.WiserTemplate} AS branchTemplate ON branchTemplate.template_id = template.template_id AND branchTemplate.version = template.version;

DROP TABLE IF EXISTS `{branchDatabaseName}`.`{temporaryTableName}`";
            await clientDatabaseConnection.ExecuteAsync(query);
        }

        /// <inheritdoc />
        public async Task KeepTablesUpToDateAsync()
        {
            var lastTableUpdates = await databaseHelpersService.GetLastTableUpdatesAsync(clientDatabaseConnection.ConnectedDatabase);

            // Check if the templates table needs to be updated.
            if ((lastTableUpdates.TryGetValue(Constants.SetIsDirtyToTemplates, out var value) && value >= new DateTime(2023, 7, 4)))
            {
                return;
            }

            var query = $@"UPDATE {WiserTableNames.WiserTemplate} AS template
LEFT JOIN {WiserTableNames.WiserTemplate} AS otherVersion ON otherVersion.template_id = template.template_id AND otherVersion.version > template.version
LEFT JOIN {WiserTableNames.WiserCommitTemplate} AS templateCommit ON templateCommit.template_id = template.template_id AND templateCommit.version = template.version
SET template.is_dirty = TRUE
WHERE template.template_type != {(int)TemplateTypes.Directory} 
AND template.removed = 0
AND template.is_dirty = FALSE
AND otherVersion.id IS NULL
AND templateCommit.id IS NULL";
            await clientDatabaseConnection.ExecuteAsync(query);

            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("tableName", Constants.SetIsDirtyToTemplates);
            clientDatabaseConnection.AddParameter("lastUpdate", DateTime.Now);
            var lastUpdateData = await clientDatabaseConnection.GetAsync($"SELECT NULL FROM `{WiserTableNames.WiserTableChanges}` WHERE `name` = ?tableName");
            if (lastUpdateData.Rows.Count == 0)
            {
                await clientDatabaseConnection.ExecuteAsync($"INSERT INTO `{WiserTableNames.WiserTableChanges}` (`name`, last_update) VALUES (?tableName, ?lastUpdate)");
            }
            else
            {
                await clientDatabaseConnection.ExecuteAsync($"UPDATE `{WiserTableNames.WiserTableChanges}` SET last_update = ?lastUpdate WHERE `name` = ?tableName LIMIT 1");
            }
        }

        /// <summary>
        /// Retrieves database views that are not managed via the templates module yet, and returns them as <see cref="TemplateTreeViewDao"/> items.
        /// </summary>
        /// <param name="parentId">The template ID of the root that normally contains the views.</param>
        /// <param name="exclusions">The names of views that should be excluded.</param>
        /// <returns>A <see cref="List{T}"/> of <see cref="TemplateTreeViewDao"/> items.</returns>
        private async Task<List<TemplateTreeViewDao>> GetViewsAsTreeViewItemsAsync(int parentId, IReadOnlyList<string> exclusions = null)
        {
            var tableNamesForStatement = new List<string>();
            var excludeStatement = String.Empty;
            if (exclusions is { Count: > 0 })
            {
                for (var i = 0; i < exclusions.Count; i++)
                {
                    clientDatabaseConnection.AddParameter($"tableName{i}", exclusions[i]);
                    tableNamesForStatement.Add($"?tableName{i}");
                }

                excludeStatement = $"AND TABLE_NAME NOT IN ({String.Join(",", tableNamesForStatement)})";
            }

            var query = $@"SELECT TABLE_NAME
FROM information_schema.VIEWS
WHERE TABLE_SCHEMA = DATABASE() {excludeStatement}";

            var dataTable = await clientDatabaseConnection.GetAsync(query);
            return dataTable.Rows.Cast<DataRow>()
                .Select(row => new TemplateTreeViewDao
                {
                    HasChildren = false,
                    ParentId = parentId,
                    TemplateName = row.Field<string>("TABLE_NAME"),
                    TemplateType = TemplateTypes.View,
                    IsVirtualItem = true
                }).ToList();
        }

        /// <summary>
        /// Retrieves database routines that are not managed via the templates module yet, and returns them as <see cref="TemplateTreeViewDao"/> items.
        /// </summary>
        /// <param name="parentId">The template ID of the root that normally contains the routines.</param>
        /// <param name="exclusions">The names of routines that should be excluded.</param>
        /// <returns>A <see cref="List{T}"/> of <see cref="TemplateTreeViewDao"/> items.</returns>
        private async Task<List<TemplateTreeViewDao>> GetRoutinesAsTreeViewItemsAsync(int parentId, IReadOnlyList<string> exclusions = null)
        {
            var routineNamesForStatement = new List<string>();
            var excludeStatement = String.Empty;
            if (exclusions is { Count: > 0 })
            {
                for (var i = 0; i < exclusions.Count; i++)
                {
                    clientDatabaseConnection.AddParameter($"routineName{i}", exclusions[i]);
                    routineNamesForStatement.Add($"?routineName{i}");
                }

                excludeStatement = $"AND ROUTINE_NAME NOT IN ({String.Join(",", routineNamesForStatement)})";
            }

            var query = $@"SELECT ROUTINE_NAME
FROM information_schema.ROUTINES
WHERE ROUTINE_SCHEMA = DATABASE() {excludeStatement}";

            var dataTable = await clientDatabaseConnection.GetAsync(query);
            return dataTable.Rows.Cast<DataRow>()
                .Select(row => new TemplateTreeViewDao
                {
                    HasChildren = false,
                    ParentId = parentId,
                    TemplateName = row.Field<string>("ROUTINE_NAME"),
                    TemplateType = TemplateTypes.Routine,
                    IsVirtualItem = true
                }).ToList();
        }

        /// <summary>
        /// Retrieves database triggers that are not managed via the templates module yet, and returns them as <see cref="TemplateTreeViewDao"/> items.
        /// </summary>
        /// <param name="parentId">The template ID of the root that normally contains the triggers.</param>
        /// <param name="exclusions">The names of triggers that should be excluded.</param>
        /// <returns>A <see cref="List{T}"/> of <see cref="TemplateTreeViewDao"/> items.</returns>
        private async Task<List<TemplateTreeViewDao>> GetTriggersAsTreeViewItemsAsync(int parentId, IReadOnlyList<string> exclusions = null)
        {
            var triggerNamesForStatement = new List<string>();
            var excludeStatement = String.Empty;
            if (exclusions is { Count: > 0 })
            {
                for (var i = 0; i < exclusions.Count; i++)
                {
                    clientDatabaseConnection.AddParameter($"triggerName{i}", exclusions[i]);
                    triggerNamesForStatement.Add($"?triggerName{i}");
                }

                excludeStatement = $"AND TRIGGER_NAME NOT IN ({String.Join(",", triggerNamesForStatement)})";
            }

            var query = $@"SELECT TRIGGER_NAME
FROM information_schema.TRIGGERS
WHERE TRIGGER_SCHEMA = DATABASE() {excludeStatement}";

            var dataTable = await clientDatabaseConnection.GetAsync(query);
            return dataTable.Rows.Cast<DataRow>()
                .Select(row => new TemplateTreeViewDao
                {
                    HasChildren = false,
                    ParentId = parentId,
                    TemplateName = row.Field<string>("TRIGGER_NAME"),
                    TemplateType = TemplateTypes.Trigger,
                    IsVirtualItem = true
                }).ToList();
        }
    }
}
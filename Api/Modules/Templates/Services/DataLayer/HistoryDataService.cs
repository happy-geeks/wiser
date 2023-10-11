using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Api.Modules.Templates.Interfaces.DataLayer;
using Api.Modules.Templates.Models.History;
using Api.Modules.Templates.Models.Template;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Enums;

namespace Api.Modules.Templates.Services.DataLayer
{
    /// <inheritdoc cref="IHistoryDataService" />
    public class HistoryDataService : IHistoryDataService, IScopedService
    {
        private readonly IDatabaseConnection connection;

        /// <summary>
        /// Creates a new instance of <see cref="HistoryDataService"/>.
        /// </summary>
        public HistoryDataService(IDatabaseConnection connection)
        {
            this.connection = connection;
        }

        /// <inheritdoc />
        public async Task<List<HistoryVersionModel>> GetDynamicContentHistoryAsync(int contentId, int page, int itemsPerPage)
        {
            connection.ClearParameters();
            connection.AddParameter("contentId", contentId);
            connection.AddParameter("limit", (page - 1) * itemsPerPage);
            connection.AddParameter("offset", itemsPerPage);

            var dataTable = await connection.GetAsync($@"SELECT 
    version,
    changed_on,
    changed_by,
    component,
    component_mode,
    settings 
FROM {WiserTableNames.WiserDynamicContent} 
WHERE content_id = ?contentId 
ORDER BY version DESC
LIMIT ?limit, ?offset");

            var resultDict = new List<HistoryVersionModel>();
            foreach (DataRow row in dataTable.Rows)
            {
                resultDict.Add(new HistoryVersionModel
                {
                    Version = row.Field<int>("version"),
                    ChangedOn = row.Field<DateTime>("changed_on"),
                    ChangedBy = row.Field<string>("changed_by"),
                    Component = row.Field<string>("component"),
                    ComponentMode = row.Field<string>("component_mode"),
                    RawVersionString = row.Field<string>("settings")
                });
            }
            return resultDict;
        }

        /// <inheritdoc />
        public async Task<Dictionary<int, int>> GetPublishedEnvironmentsFromDynamicContentAsync(int contentId)
        {
            var versionList = new Dictionary<int, int>();

            connection.AddParameter("id", contentId);
            var dataTable = await connection.GetAsync(@$"SELECT 
    version,
    published_environment 
FROM {WiserTableNames.WiserDynamicContent}
WHERE content_id = ?id");

            foreach (DataRow row in dataTable.Rows)
            {
                versionList.Add(row.Field<int>("version"), row.Field<sbyte>("published_environment"));
            }

            return versionList;
        }

        /// <inheritdoc />
        public async Task<List<TemplateSettingsModel>> GetTemplateHistoryAsync(int templateId, int page, int itemsPerPage)
        {
            connection.ClearParameters();
            connection.AddParameter("templateId", templateId);
            connection.AddParameter("limit", (page - 1) * itemsPerPage);
            connection.AddParameter("offset", itemsPerPage);;

            var dataTable = await connection.GetAsync($@"SELECT 
    template.template_id, 
    template.parent_id, 
    template.template_name, 
    template.template_type, 
    template.template_data, 
    template.version, 
    template.changed_on, 
    template.changed_by, 
    template.cache_per_url,
    template.cache_per_querystring,
    template.cache_per_hostname,
    template.cache_using_regex,
    template.cache_minutes, 
    template.cache_location, 
    template.cache_regex, 
    template.login_required, 
    template.login_role, 
    template.ordering, 
    GROUP_CONCAT(CONCAT_WS(';', linkedTemplates.template_id, linkedTemplates.template_name, linkedTemplates.template_type)) AS linkedTemplates,
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
LEFT JOIN (SELECT linkedTemplate.template_id, template_name, template_type FROM {WiserTableNames.WiserTemplate} linkedTemplate WHERE linkedTemplate.removed = 0 GROUP BY template_id) AS linkedTemplates ON FIND_IN_SET(linkedTemplates.template_id, template.linked_templates)
WHERE template.template_id = ?templateId
AND template.removed = 0
GROUP BY template.version
ORDER BY version DESC
LIMIT ?limit, ?offset");

            var resultList = new List<TemplateSettingsModel>();

            foreach (DataRow row in dataTable.Rows)
            {
                var templateData = new TemplateSettingsModel
                {
                    TemplateId = row.Field<int>("template_id"),
                    ParentId = row.Field<int?>("parent_id"),
                    Type = row.Field<TemplateTypes>("template_type"),
                    Name = row.Field<string>("template_name"),
                    EditorValue = row.Field<string>("template_data"),
                    Version = row.Field<int>("version"),
                    ChangedOn = row.Field<DateTime>("changed_on"),
                    ChangedBy = row.Field<string>("changed_by"),
                    CachePerUrl = dataTable.Rows[0].Field<bool>("cache_per_url"),
                    CachePerQueryString = dataTable.Rows[0].Field<bool>("cache_per_querystring"),
                    CacheUsingRegex = dataTable.Rows[0].Field<bool>("cache_using_regex"),
                    CachePerHostName = dataTable.Rows[0].Field<bool>("cache_per_hostname"),
                    CacheMinutes = row.Field<int>("cache_minutes"),
                    CacheLocation= (TemplateCachingLocations)row.Field<int>("cache_location"),
                    LoginRequired = Convert.ToBoolean(row["login_required"]),
                    Ordering = row.Field<int>("ordering"),
                    LinkedTemplates = new LinkedTemplatesModel
                    {
                        RawLinkList = row.Field<string>("linkedTemplates")
                    },
                    InsertMode = row.Field<ResourceInsertModes>("insert_mode"),
                    LoadAlways = Convert.ToBoolean(row["load_always"]),
                    DisableMinifier = Convert.ToBoolean(row["disable_minifier"]),
                    UrlRegex = row.Field<string>("url_regex"),
                    ExternalFiles = row.Field<string>("external_files")?.Split(new [] {';', ',' }, StringSplitOptions.RemoveEmptyEntries)?.ToList() ?? new List<string>(),
                    GroupingCreateObjectInsteadOfArray = Convert.ToBoolean(row["grouping_create_object_instead_of_array"]),
                    GroupingPrefix = row.Field<string>("grouping_prefix"),
                    GroupingKey = row.Field<string>("grouping_key"),
                    GroupingKeyColumnName = row.Field<string>("grouping_key_column_name"),
                    GroupingValueColumnName = row.Field<string>("grouping_value_column_name"),
                    IsScssIncludeTemplate = Convert.ToBoolean(row["is_scss_include_template"]),
                    UseInWiserHtmlEditors = Convert.ToBoolean(row["use_in_wiser_html_editors"]),
                    PreLoadQuery = row.Field<string>("pre_load_query"),
                    ReturnNotFoundWhenPreLoadQueryHasNoData = Convert.ToBoolean(row["return_not_found_when_pre_load_query_has_no_data"]),
                    RoutineType = (RoutineTypes)row.Field<int>("routine_type"),
                    RoutineParameters = row.Field<string>("routine_parameters"),
                    RoutineReturnType = row.Field<string>("routine_return_type"),
                    TriggerTiming = (TriggerTimings)row.Field<int>("trigger_timing"),
                    TriggerEvent = (TriggerEvents)row.Field<int>("trigger_event"),
                    TriggerTableName = row.Field<string>("trigger_table_name"),
                    IsDefaultHeader = Convert.ToBoolean(row["is_default_header"]),
                    IsDefaultFooter = Convert.ToBoolean(row["is_default_footer"]),
                    DefaultHeaderFooterRegex = row.Field<string>("default_header_footer_regex"),
                    IsPartial = Convert.ToBoolean(row["is_partial"]),
                    WidgetContent = row.Field<string>("widget_content"),
                    WidgetLocation = (PageWidgetLocations) Convert.ToInt32(row["widget_location"])
                };

                var loginRolesString = row.Field<string>("login_role");
                if (!String.IsNullOrWhiteSpace(loginRolesString))
                {
                    templateData.LoginRoles = loginRolesString.Split(",").Select(Int32.Parse).ToList();
                }

                resultList.Add(templateData);
            }

            return resultList;
        }

        /// <inheritdoc />
        public async Task<List<PublishHistoryModel>> GetPublishHistoryFromTemplateAsync(int templateId, int page, int itemsPerPage)
        {
            connection.ClearParameters();
            connection.AddParameter("templateId", templateId);
            connection.AddParameter("limit", (page - 1) * itemsPerPage);
            connection.AddParameter("offset", itemsPerPage);

            var dataTable = await connection.GetAsync($@"SELECT MIN(changed_on), MAX(changed_on)
INTO @minDate, @maxDate
FROM (
    SELECT template.id, template.changed_on FROM {WiserTableNames.WiserTemplate} AS template
    WHERE template.template_id = ?templateId
    AND template.removed = 0
    ORDER BY template.version DESC
    LIMIT ?limit, ?offset
) AS t;
										
SELECT 
    template_id,
    old_live,
    old_accept,
    old_test,
    new_live,
    new_accept,
    new_test,
    changed_on,
    changed_by
FROM {WiserTableNames.WiserTemplatePublishLog} 
WHERE template_id = ?templateId 
AND changed_on BETWEEN @minDate AND @maxDate
ORDER BY changed_on DESC");

            var resultList = new List<PublishHistoryModel>();

            foreach (DataRow row in dataTable.Rows)
            {
                var publishHistory = new PublishHistoryModel
                {
                    TemplateId = row.Field<int>("template_id"),
                    ChangedOn = row.Field<DateTime>("changed_on"),
                    ChangedBy = row.Field<string>("changed_by"),
                    PublishLog = new PublishLogModel(
                        row.Field<int>("template_id"),
                        row.Field<int>("old_live"),
                        row.Field<int>("old_accept"),
                        row.Field<int>("old_test"),
                        row.Field<int>("new_live"),
                        row.Field<int>("new_accept"),
                        row.Field<int>("new_test")
                    )
                };

                resultList.Add(publishHistory);
            }

            return resultList;
        }
    }
}
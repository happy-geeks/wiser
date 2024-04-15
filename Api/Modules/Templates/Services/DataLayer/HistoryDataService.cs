﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Api.Modules.Templates.Helpers;
using Api.Modules.Templates.Interfaces.DataLayer;
using Api.Modules.Templates.Models.History;
using Api.Modules.Templates.Models.Template;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;

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
    settings,
    title
FROM {WiserTableNames.WiserDynamicContent} 
WHERE content_id = ?contentId 
ORDER BY version DESC
LIMIT ?limit, ?offset");

            var resultDict = new List<HistoryVersionModel>();
            foreach (DataRow row in dataTable.Rows)
            {
                resultDict.Add(new HistoryVersionModel
                {
                    Name = row.Field<string>("title"),
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
    template.ordering, 
    GROUP_CONCAT(CONCAT_WS(';', linkedTemplates.template_id, linkedTemplates.template_name, linkedTemplates.template_type)) AS linked_templates,
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
LEFT JOIN {WiserTableNames.WiserTemplateExternalFiles} AS externalFiles ON externalFiles.template_id = template.id
LEFT JOIN (SELECT linkedTemplate.template_id, template_name, template_type FROM {WiserTableNames.WiserTemplate} linkedTemplate WHERE linkedTemplate.removed = 0 GROUP BY template_id) AS linkedTemplates ON FIND_IN_SET(linkedTemplates.template_id, template.linked_templates)
WHERE template.template_id = ?templateId
AND template.removed = 0
GROUP BY template.version
ORDER BY version DESC
LIMIT ?limit, ?offset");

            return dataTable.Rows.Cast<DataRow>().Select(TemplateHelpers.DataRowToTemplateSettingsModel).ToList();
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
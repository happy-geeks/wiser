﻿using System;
using System.Collections.Generic;
using System.Data;
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
        public async Task<List<HistoryVersionModel>> GetDynamicContentHistoryAsync(int contentId)
        {
            connection.ClearParameters();
            connection.AddParameter("contentId", contentId);
            var dataTable = await connection.GetAsync($"SELECT wdc.version, wdc.changed_on, wdc.changed_by, wdc.component, wdc.component_mode, wdc.settings FROM {WiserTableNames.WiserDynamicContent} wdc WHERE content_id = ?contentId ORDER BY wdc.version DESC");

            var resultDict = new List<HistoryVersionModel>();
            foreach (DataRow row in dataTable.Rows)
            {
                resultDict.Add(new HistoryVersionModel(row.Field<int>("version"), row.Field<DateTime>("changed_on"), row.Field<string>("changed_by"), row.Field<string>("component"), row.Field<string>("component_mode"), row.Field<string>("settings")));
            }
            return resultDict;
        }

        /// <inheritdoc />
        public async Task<Dictionary<int, int>> GetPublishedEnvironmentsFromDynamicContentAsync(int templateId)
        {
            var versionList = new Dictionary<int, int>();

            connection.AddParameter("id", templateId);
            var dataTable = await connection.GetAsync($"SELECT wdc.version, wdc.published_environment FROM `{WiserTableNames.WiserDynamicContent}` wdc WHERE wdc.content_id = ?id");

            foreach (DataRow row in dataTable.Rows)
            {
                versionList.Add(row.Field<int>("version"), row.Field<sbyte>("published_environment"));
            }

            return versionList;
        }

        /// <inheritdoc />
        public async Task<List<TemplateSettingsModel>> GetTemplateHistoryAsync(int templateId)
        {
            connection.ClearParameters();
            connection.AddParameter("templateId", templateId);
            var dataTable = await connection.GetAsync($@"SELECT 
                                                                template.template_id, 
                                                                template.parent_id, 
                                                                template.template_name, 
                                                                template.template_type, 
                                                                template.template_data, 
                                                                template.version, 
                                                                template.changed_on, 
                                                                template.changed_by, 
                                                                template.use_cache,
                                                                template.cache_minutes, 
                                                                template.handle_request, 
                                                                template.handle_session, 
                                                                template.handle_objects, 
                                                                template.handle_standards, 
                                                                template.handle_translations, 
                                                                template.handle_dynamic_content, 
                                                                template.handle_logic_blocks, 
                                                                template.handle_mutators, 
                                                                template.login_required, 
                                                                template.login_user_type, 
                                                                template.login_session_prefix, 
                                                                template.login_role, 
                                                                template.ordering, 
                                                                GROUP_CONCAT(CONCAT_WS(';', linkedTemplates.template_id, linkedTemplates.template_name, linkedTemplates.template_type)) AS linkedTemplates 
                                                            FROM {WiserTableNames.WiserTemplate} AS template 
				                                            LEFT JOIN (SELECT linkedTemplate.template_id, template_name, template_type FROM {WiserTableNames.WiserTemplate} linkedTemplate GROUP BY template_id) AS linkedTemplates ON FIND_IN_SET(linkedTemplates.template_id, template.linked_templates)
                                                            WHERE template.template_id = ?templateId
				                                            GROUP BY template.version
                                                            ORDER BY version DESC");

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
                    UseCache = row.Field<int>("use_cache"),
                    CacheMinutes = row.Field<int>("cache_minutes"),
                    HandleRequests = Convert.ToBoolean(row["handle_request"]),
                    HandleSession = Convert.ToBoolean(row["handle_session"]),
                    HandleStandards = Convert.ToBoolean(row["handle_standards"]),
                    HandleObjects = Convert.ToBoolean(row["handle_objects"]),
                    HandleTranslations = Convert.ToBoolean(row["handle_translations"]),
                    HandleDynamicContent = Convert.ToBoolean(row["handle_dynamic_content"]),
                    HandleLogicBlocks = Convert.ToBoolean(row["handle_logic_blocks"]),
                    HandleMutators = Convert.ToBoolean(row["handle_mutators"]),
                    LoginRequired = Convert.ToBoolean(row["login_required"]),
                    LoginUserType = row.Field<string>("login_user_type"),
                    LoginSessionPrefix = row.Field<string>("login_session_prefix"),
                    LoginRole = row.Field<string>("login_role"),
                    Ordering = row.Field<int>("ordering"),
                    LinkedTemplates = new LinkedTemplatesModel
                    {
                        RawLinkList = row.Field<string>("linkedTemplates")
                    }
                };

                resultList.Add(templateData);
            }

            return resultList;
        }

        /// <inheritdoc />
        public async Task<List<PublishHistoryModel>> GetPublishHistoryFromTemplateAsync(int templateId)
        {
            connection.ClearParameters();
            connection.AddParameter("templateid", templateId);

            var dataTable = await connection.GetAsync($@"SELECT publink.template_id, publink.old_live, publink.old_accept, publink.old_test, publink.new_live, publink.new_accept, publink.new_test, publink.changed_on, publink.changed_by
                    FROM {WiserTableNames.WiserTemplatePublishLog} publink WHERE publink.template_id=?templateid ORDER BY publink.changed_on DESC"
            );

            var resultList = new List<PublishHistoryModel>();

            foreach (DataRow row in dataTable.Rows)
            {
                var publishHistory = new PublishHistoryModel
                {
                    Templateid = (int)row.Field<long>("template_id"),
                    ChangedOn = row.Field<DateTime>("changed_on"),
                    ChangedBy = row.Field<string>("changed_by"),
                    PublishLog = new PublishLogModel(
                        row.Field<long>("template_id"),
                        row.Field<long>("old_live"),
                        row.Field<long>("old_accept"),
                        row.Field<long>("old_test"),
                        row.Field<long>("new_live"),
                        row.Field<long>("new_accept"),
                        row.Field<long>("new_test")
                    )
                };

                resultList.Add(publishHistory);
            }

            return resultList;
        }
    }
}

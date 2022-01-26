using System;
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
        public async Task<List<HistoryVersionModel>> GetDynamicContentHistory(int contentId)
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
        public async Task<Dictionary<int, int>> GetPublishedEnvironmentsFromDynamicContent(int templateId)
        {
            var versionList = new Dictionary<int, int>();

            connection.AddParameter("id", templateId);
            var dataTable = await connection.GetAsync($"SELECT wdc.version, wdc.published_environment FROM `{WiserTableNames.WiserDynamicContent}` wdc WHERE wdc.content_id = ?id");

            foreach (DataRow row in dataTable.Rows)
            {
                versionList.Add(row.Field<int>("version"), row.Field<SByte>("published_environment"));
            }

            return versionList;
        }
        
        /// <inheritdoc />
        public async Task<List<TemplateSettingsModel>> GetTemplateHistory(int templateId)
        {
            connection.ClearParameters();
            connection.AddParameter("templateid", templateId);
            var dataTable = await connection.GetAsync($@"SELECT wtt.template_id, wtt.parent_id, wtt.template_name, wtt.template_type, wtt.template_data, wtt.version, wtt.changed_on, wtt.changed_by, wtt.usecache, 
                wtt.cacheminutes, wtt.handlerequest, wtt.handlesession, wtt.handleobjects, wtt.handlestandards, wtt.handletranslations, wtt.handledynamiccontent, wtt.handlelogicblocks, wtt.handlemutators, 
                wtt.loginrequired, wtt.loginusertype, wtt.loginsessionprefix, wtt.loginrole, GROUP_CONCAT(CONCAT_WS(';',linkedtemplates.template_id, linkedtemplates.template_name, linkedtemplates.template_type)) AS linkedtemplates 
                FROM {WiserTableNames.WiserTemplate} wtt 
				LEFT JOIN (SELECT linkedTemplate.template_id, template_name, template_type FROM {WiserTableNames.WiserTemplate} linkedTemplate GROUP BY template_id) AS linkedtemplates 
				ON FIND_IN_SET(linkedtemplates.template_id ,wtt.linkedtemplates)
                WHERE wtt.template_id = ?templateid
				GROUP BY wtt.version
                ORDER BY version DESC"
            );

            var resultList = new List<TemplateSettingsModel>();

            foreach(DataRow row in dataTable.Rows)
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
                    UseCache = row.Field<int>("usecache"),
                    CacheMinutes = row.Field<int>("cacheminutes"),
                    HandleRequests = Convert.ToBoolean(row.Field<sbyte>("handlerequest")),
                    HandleSession = Convert.ToBoolean(row.Field<sbyte>("handlesession")),
                    HandleStandards = Convert.ToBoolean(row.Field<sbyte>("handlestandards")),
                    HandleObjects = Convert.ToBoolean(row.Field<sbyte>("handleobjects")),
                    HandleTranslations = Convert.ToBoolean(row.Field<sbyte>("handletranslations")),
                    HandleDynamicContent = Convert.ToBoolean(row.Field<sbyte>("handledynamiccontent")),
                    HandleLogicBlocks = Convert.ToBoolean(row.Field<sbyte>("handlelogicblocks")),
                    HandleMutators = Convert.ToBoolean(row.Field<sbyte>("handlemutators")),
                    LoginRequired = Convert.ToBoolean(row.Field<sbyte>("loginrequired")),
                    LoginUserType = row.Field<string>("loginusertype"),
                    LoginSessionPrefix = row.Field<string>("loginsessionprefix"),
                    LoginRole = row.Field<string>("loginrole"),
                    LinkedTemplates = new LinkedTemplatesModel
                    {
                        RawLinkList = row.Field<string>("linkedtemplates")
                    }
                };

                resultList.Add(templateData);
            }

            return resultList;
        }
        
        /// <inheritdoc />
        public async Task<List<PublishHistoryModel>> GetPublishHistoryFromTemplate(int templateId) {
            connection.ClearParameters();
            connection.AddParameter("templateid", templateId);

            var dataTable = await connection.GetAsync($@"SELECT publink.template_id, publink.old_live, publink.old_accept, publink.old_test, publink.new_live, publink.new_accept, publink.new_test, publink.changed_on, publink.changed_by
                    FROM {WiserTableNames.WiserTemplatePublishLog} publink WHERE publink.template_id=?templateid ORDER BY publink.changed_on DESC"
            );

            var resultList = new List<PublishHistoryModel>();

            foreach(DataRow row in dataTable.Rows)
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

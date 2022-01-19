using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Api.Modules.Templates.Interfaces.DataLayer;
using Api.Modules.Templates.Models.History;
using Api.Modules.Templates.Models.Template;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Modules.Databases.Interfaces;

namespace Api.Modules.Templates.Services.DataLayer
{
    public class HistoryDataService : IHistoryDataService, IScopedService
    {
        private readonly IDatabaseConnection connection;

        public HistoryDataService(IDatabaseConnection connection)
        {
            this.connection = connection;
        }

        /// <summary>
        /// Returns the components history as a dictionary.
        /// </summary>
        /// <returns></returns>
        public async Task<List<HistoryVersionModel>> GetDynamicContentHistory(int templateId)
        {
            connection.ClearParameters();
            connection.AddParameter("templateid", templateId);
            var dataTable = await connection.GetAsync("SELECT wdc.version, wdc.changed_on, wdc.changed_by, wdc.component, wdc.component_mode, wdc.filledvariables FROM wiser_dynamiccontent_test wdc WHERE templateid = ?templateid ORDER BY wdc.version DESC");

            var resultDict = new List<HistoryVersionModel>();
            foreach (DataRow row in dataTable.Rows)
            {
                resultDict.Add(new HistoryVersionModel(row.Field<int>("version"), row.Field<DateTime>("changed_on"), row.Field<string>("changed_by"), row.Field<string>("component"), row.Field<string>("component_mode"), row.Field<string>("filledvariables")));
            }
            return resultDict;
        }

        /// <summary>
        /// Get a list of versions and their published environments form a dynamic content.
        /// </summary>
        /// <param name="templateId">The id of the dynamic content.</param>
        /// <returns>List of version numbers and their published environment.</returns>
        public async Task<Dictionary<int, int>> GetPublishedEnvoirementsFromDynamicContent(int templateId)
        {
            var versionList = new Dictionary<int, int>();

            var dataTable = await connection.GetAsync("SELECT wdc.version, wdc.published_envoirement FROM `wiser_dynamiccontent_test` wdc WHERE wdc.templateid = " + templateId);

            foreach (DataRow row in dataTable.Rows)
            {
                versionList.Add(row.Field<int>("version"), row.Field<SByte>("published_envoirement"));
            }

            return versionList;
        }

        /// <summary>
        /// Get the history of a template. This will retrieve all versions of the template which can be compared for changes.
        /// </summary>
        /// <param name="templateId">The id of the template which history should be retrieved.</param>
        /// <returns>A list of templatedatamodels forming the history of the template. The list is ordered by version number (DESC).</returns>
        public async Task<List<TemplateDataModel>> GetTemplateHistory(int templateId)
        {
            connection.ClearParameters();
            connection.AddParameter("templateid", templateId);
            var dataTable = await connection.GetAsync(@"SELECT wtt.template_id, wtt.template_name, wtt.template_data, wtt.version, wtt.changed_on, wtt.changed_by, wtt.usecache, 
                wtt.cacheminutes, wtt.handlerequest, wtt.handlesession, wtt.handleobjects, wtt.handlestandards, wtt.handletranslations, wtt.handledynamiccontent, wtt.handlelogicblocks, wtt.handlemutators, 
                wtt.loginrequired, wtt.loginusertype, wtt.loginsessionprefix, wtt.loginrole, GROUP_CONCAT(CONCAT_WS(';',linkedtemplates.template_id, linkedtemplates.template_name, linkedtemplates.template_type)) AS linkedtemplates 
                FROM wiser_template_test wtt 
				LEFT JOIN (SELECT linkedTemplate.template_id, template_name, template_type FROM wiser_template_test linkedTemplate GROUP BY template_id) AS linkedtemplates 
				ON FIND_IN_SET(linkedtemplates.template_id ,wtt.linkedtemplates)
                WHERE wtt.template_id = ?templateid
				GROUP BY wtt.version
                ORDER BY version DESC"
            );

            var resultList = new List<TemplateDataModel>();

            foreach(DataRow row in dataTable.Rows)
            {
                var templateData = new TemplateDataModel();

                templateData.templateid = row.Field<int>("template_id");
                templateData.name = row.Field<string>("template_name");
                templateData.editorValue = row.Field<string>("template_data");
                templateData.version = row.Field<int>("version");
                templateData.changed_on = row.Field<DateTime>("changed_on");
                templateData.changed_by = row.Field<string>("changed_by");

                templateData.useCache = row.Field<int>("usecache");
                templateData.cacheMinutes = row.Field<int>("cacheminutes");
                templateData.handleRequests = Convert.ToBoolean(row.Field<sbyte>("handlerequest"));
                templateData.handleSession = Convert.ToBoolean(row.Field<sbyte>("handlesession"));
                templateData.handleStandards = Convert.ToBoolean(row.Field<sbyte>("handlestandards"));
                templateData.handleObjects = Convert.ToBoolean(row.Field<sbyte>("handleobjects"));
                templateData.handleTranslations = Convert.ToBoolean(row.Field<sbyte>("handletranslations"));
                templateData.handleDynamicContent = Convert.ToBoolean(row.Field<sbyte>("handledynamiccontent"));
                templateData.handleLogicBlocks = Convert.ToBoolean(row.Field<sbyte>("handlelogicblocks"));
                templateData.handleMutators = Convert.ToBoolean(row.Field<sbyte>("handlemutators"));
                templateData.loginRequired = Convert.ToBoolean(row.Field<sbyte>("loginrequired"));
                templateData.loginUserType = row.Field<string>("loginusertype");
                templateData.loginSessionPrefix = row.Field<string>("loginsessionprefix");
                templateData.loginRole = row.Field<string>("loginrole");

                var linkedTemplates = new LinkedTemplatesModel();
                linkedTemplates.rawLinkList = row.Field<string>("linkedtemplates");
                templateData.linkedTemplates = linkedTemplates;

                resultList.Add(templateData);
            }

            return resultList;
        }

        /// <summary>
        /// Get the history of a template from the publish log table. The list will be ordered on date desc.
        /// </summary>
        /// <param name="templateId">The Id of the template whose history to retrieve</param>
        /// <returns>A list of publishmodels containing the values of the change from the publish log datatable.</returns>
        public async Task<List<PublishHistoryModel>> GetPublishHistoryFromTemplate(int templateId) {
            connection.ClearParameters();
            connection.AddParameter("templateid", templateId);

            var dataTable = await connection.GetAsync(@"SELECT publink.template_id, publink.old_live, publink.old_accept, publink.old_test, publink.new_live, publink.new_accept, publink.new_test, publink.changed_on, publink.changed_by
                    FROM wiser_template_publish_log_test publink WHERE publink.template_id=?templateid ORDER BY publink.changed_on DESC"
            );

            var resultList = new List<PublishHistoryModel>();

            foreach(DataRow row in dataTable.Rows)
            {
                var publishHistory = new PublishHistoryModel();
                publishHistory.templateid = (int)row.Field<Int64>("template_id");
                publishHistory.changed_on = row.Field<DateTime>("changed_on");
                publishHistory.changed_by = row.Field<string>("changed_by");
                publishHistory.publishLog = new PublishLogModel(
                        row.Field<Int64>("template_id"), 
                        row.Field<Int64>("old_live"), 
                        row.Field<Int64>("old_accept"), 
                        row.Field<Int64>("old_test"), 
                        row.Field<Int64>("new_live"), 
                        row.Field<Int64>("new_accept"), 
                        row.Field<Int64>("new_test")
                    );

                resultList.Add(publishHistory);
            }

            return resultList;
        }

    }
}

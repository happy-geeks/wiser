using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using Api.Modules.Templates.Enums;
using Api.Modules.Templates.Interfaces.DataLayer;
using Api.Modules.Templates.Models.DynamicContent;
using Api.Modules.Templates.Models.Other;
using Api.Modules.Templates.Models.Template;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Modules.Databases.Interfaces;

namespace Api.Modules.Templates.Services.DataLayer
{
    public class TemplateDataService : ITemplateDataService, IScopedService
    {
        private readonly IDatabaseConnection connection;
        
        public TemplateDataService(IDatabaseConnection connection)
        {
            this.connection = connection;
        }

        /// <summary>
        /// Get the template data of a template.
        /// </summary>
        /// <param name="templateId">The id of the template to retrieve the data from.</param>
        /// <returns>A templatedatamodel containing the current template data of the template with the given id.</returns>
        public async Task<TemplateDataModel> GetTemplateData(int templateId)
        {
            connection.ClearParameters();
            connection.AddParameter("templateid", templateId);
            var dataTable = await connection.GetAsync(@"SELECT wtt.template_id, wtt.template_name, wtt.template_data, wtt.version, wtt.changed_on, wtt.changed_by, wtt.usecache, 
                wtt.cacheminutes, wtt.handlerequest, wtt.handlesession, wtt.handleobjects, wtt.handlestandards, wtt.handletranslations, wtt.handledynamiccontent, wtt.handlelogicblocks, wtt.handlemutators, 
                wtt.loginrequired, wtt.loginusertype, wtt.loginsessionprefix, wtt.loginrole , GROUP_CONCAT(CONCAT_WS(';',linkedtemplates.template_id, linkedtemplates.template_name, linkedtemplates.template_type)) AS linkedtemplates
                FROM wiser_template_test wtt 
				LEFT JOIN (SELECT linkedTemplate.template_id, template_name, template_type FROM wiser_template_test linkedTemplate GROUP BY template_id) AS linkedtemplates 
				ON FIND_IN_SET(linkedtemplates.template_id ,wtt.linkedtemplates)
                WHERE wtt.template_id = 1 AND wtt.version = (SELECT MAX(version) FROM wiser_template_test WHERE template_id = wtt.template_id)
				GROUP BY wtt.template_id
                ORDER BY wtt.version DESC 
                LIMIT 1"
            );

            var templateData = new TemplateDataModel();

            if (dataTable.Rows.Count == 1) {
                templateData.templateid = dataTable.Rows[0].Field<int>("template_id");
                templateData.name = dataTable.Rows[0].Field<string>("template_name");
                templateData.editorValue = dataTable.Rows[0].Field<string>("template_data");
                templateData.version = dataTable.Rows[0].Field<int>("version");
                templateData.changed_on = dataTable.Rows[0].Field<DateTime>("changed_on");
                templateData.changed_by = dataTable.Rows[0].Field<string>("changed_by");

                templateData.useCache = dataTable.Rows[0].Field<int>("usecache");
                templateData.cacheMinutes = dataTable.Rows[0].Field<int>("cacheminutes");
                templateData.handleRequests = Convert.ToBoolean(dataTable.Rows[0].Field<sbyte>("handlerequest"));
                templateData.handleSession = Convert.ToBoolean(dataTable.Rows[0].Field<sbyte>("handlesession"));
                templateData.handleStandards = Convert.ToBoolean(dataTable.Rows[0].Field<sbyte>("handlestandards"));
                templateData.handleObjects = Convert.ToBoolean(dataTable.Rows[0].Field<sbyte>("handleobjects"));
                templateData.handleTranslations = Convert.ToBoolean(dataTable.Rows[0].Field<sbyte>("handletranslations"));
                templateData.handleDynamicContent = Convert.ToBoolean(dataTable.Rows[0].Field<sbyte>("handledynamiccontent"));
                templateData.handleLogicBlocks = Convert.ToBoolean(dataTable.Rows[0].Field<sbyte>("handlelogicblocks"));
                templateData.handleMutators = Convert.ToBoolean(dataTable.Rows[0].Field<sbyte>("handlemutators"));
                templateData.loginRequired = Convert.ToBoolean(dataTable.Rows[0].Field<sbyte>("loginrequired"));
                templateData.loginUserType = dataTable.Rows[0].Field<string>("loginusertype");
                templateData.loginSessionPrefix = dataTable.Rows[0].Field<string>("loginsessionprefix");
                templateData.loginRole = dataTable.Rows[0].Field<string>("loginrole");
            }

            return templateData;
        }

        /// <summary>
        /// Get published environments from a template.
        /// </summary>
        /// <param name="templateId">The id of the template which environment should be retrieved.</param>
        /// <returns>A list of all version and their published environment.</returns>
        public async Task<Dictionary<int, int>> GetPublishedEnvironmentsFromTemplate (int templateId)
        {
            connection.ClearParameters();
            connection.AddParameter("templateid", templateId);
            var versionList = new Dictionary<int, int>();

            var dataTable = await connection.GetAsync("SELECT wtt.version, wtt.published_envoirement FROM `wiser_template_test` wtt WHERE wtt.template_id = ?templateid");

            foreach (DataRow row in  dataTable.Rows)
            {
                versionList.Add(row.Field<int>("version"), row.Field<SByte>("published_envoirement"));
            }

            return versionList;
        }

        /// <summary>
        /// Publish the template to an environment. This method will execute the publishmodel instructions it recieves, logic for publishing linked environments should be handled in the servicelayer.
        /// </summary>
        /// <param name="templateId">The id of the template of which the enviroment should be published.</param>
        /// <param name="publishModel">A publish model containing the versions that should be altered and their respective values to be altered with.</param>
        /// <returns>An int confirming the rows altered by the query.</returns>
        public async Task<int> PublishEnvironmentOfTemplate(int templateId, Dictionary<int,int> publishModel, PublishLogModel publishLog)
        {
            connection.ClearParameters();
            connection.AddParameter("templateid", templateId);

            var baseQueryPart = @"UPDATE wiser_template_test wtt 
                SET wtt.published_envoirement = case wtt.version";

            var dynamicQueryPart = "";
            var dynamicWherePart = " AND wtt.version IN (";
            foreach(var versionChange in publishModel)
            {
                dynamicQueryPart += " WHEN " + versionChange.Key + " THEN wtt.published_envoirement+" + versionChange.Value;
                dynamicWherePart += versionChange.Key+",";
            }
            dynamicWherePart = dynamicWherePart.Substring(0, dynamicWherePart.Length-1) + ")";
            var endQueryPart = @" end
                WHERE wtt.template_id = ?templateid";

            var query = baseQueryPart + dynamicQueryPart + endQueryPart + dynamicWherePart;

            connection.AddParameter("oldlive", publishLog.oldLive);
            connection.AddParameter("oldaccept", publishLog.oldAccept);
            connection.AddParameter("oldtest", publishLog.oldTest);
            connection.AddParameter("newlive", publishLog.newLive);
            connection.AddParameter("newaccept", publishLog.newAccept);
            connection.AddParameter("newtest", publishLog.newTest);

            var logQuery = @"INSERT INTO wiser_template_publish_log_test (template_id, old_live, old_accept, old_test, new_live, new_accept, new_test, changed_on, changed_by) 
            VALUES(
                ?templateid,
                ?oldlive,
                ?oldaccept,
                ?oldtest,
                ?newlive,
                ?newaccept,
                ?newtest,
                NOW(),
                'InsertTest'
            )";

            return await connection.ExecuteAsync(query + ";" + logQuery);
        }

        /// <summary>
        /// Get the templates linked to the current template and their relation to the current template.
        /// </summary>
        /// <param name="templateId">The id of the template which linked templates should be retrieved.</param>
        /// <returns>Return a list of linked templates in the form of linkedtemplatemodels.</returns>
        public async Task<List<LinkedTemplateModel>> GetLinkedTemplates(int templateId)
        {
            connection.ClearParameters();
            connection.AddParameter("templateid", templateId);
            var dataTable = await connection.GetAsync($@"SELECT wtt.template_name, ?templateid AS template_id, wtt.template_id AS destination_template_id, wtt.template_type AS type, 'test' AS type_name
                    FROM wiser_template_test wtt
					WHERE wtt.template_id !=?templateid AND FIND_IN_SET(wtt.template_id, (SELECT tlink.linkedtemplates FROM `wiser_template_test` tlink WHERE tlink.template_id = ?templateid AND tlink.version = (SELECT MAX(version) FROM wiser_template_test WHERE template_id = tlink.template_id))) GROUP BY wtt.template_id ORDER BY wtt.template_name");

            var linkList = new List<LinkedTemplateModel>();

            foreach(DataRow row in dataTable.Rows)
            {
                var linkedTemplate = new LinkedTemplateModel();
                linkedTemplate.templateId = row.Field<int>("destination_template_id");
                linkedTemplate.templateName = row.Field<string>("template_name");
                linkedTemplate.linkType = row.Field<LinkedTemplatesEnum>("type");
                linkedTemplate.linkName = row.Field<string>("type_name");

                linkedTemplate.parentId = 0;
                linkedTemplate.parentName = "TODO";

                linkList.Add(linkedTemplate);
            }

            return linkList;
        }

        /// <summary>
        /// Get templates that can be linked to the current template but aren't linked yet.
        /// </summary>
        /// <param name="templateId">The id of the template for which the linkoptions should be retrieved.</param>
        /// <returns>A list of possible links in the form of linkedtemplatemodels.</returns>
        public async Task<List<LinkedTemplateModel>> GetLinkOptionsForTemplate(int templateId)
        {
            connection.ClearParameters();
            connection.AddParameter("templateid", templateId);
            var dataTable = await connection.GetAsync($@"SELECT wtt.template_name, wtt.template_id, wtt.template_type 
                    FROM wiser_template_test wtt
					WHERE wtt.template_id !=?templateid AND FIND_IN_SET(wtt.template_id, (SELECT tlink.linkedtemplates FROM `wiser_template_test` tlink WHERE tlink.template_id = ?templateid AND tlink.version = (SELECT MAX(version) FROM wiser_template_test WHERE template_id = tlink.template_id))) = 0 ORDER BY wtt.template_name"
            );

            var linkList = new List<LinkedTemplateModel>();

            foreach (DataRow row in dataTable.Rows)
            {
                var linkedTemplate = new LinkedTemplateModel();
                linkedTemplate.templateId = row.Field<int>("template_id");
                linkedTemplate.templateName = row.Field<string>("template_name");
                linkedTemplate.linkType = row.Field<LinkedTemplatesEnum>("template_type");

                linkedTemplate.parentId = 0;
                linkedTemplate.parentName = "TODO";

                linkList.Add(linkedTemplate);
            }

            return linkList;
        }

        /// <summary>
        /// Get dynamic content that is linked to the current template.
        /// </summary>
        /// <param name="templateId">The id of the template of which the linked dynamic content is to be retrieved.</param>
        /// <returns>A list of dynamic content data for all the dynamic content linked to the current template.</returns>
        public async Task<List<LinkedDynamicContentDAO>> GetLinkedDynamicContent (int templateId)
        {
            connection.ClearParameters();
            connection.AddParameter("templateid", templateId);
            var dataTable = await connection.GetAsync($@"SELECT 
                wdc.templateid, 
                wdc.component, 
                wdc.component_mode, 
                GROUP_CONCAT(DISTINCT otherdc.`usages`) AS `usages`,
                wdc.changed_on,
                wdc.changed_by,
                wdc.`title`
                FROM `wiser_template_dynamiccontent_test` tdclink 
                LEFT JOIN wiser_dynamiccontent_test wdc ON wdc.templateid = tdclink.destination_template_id 
                LEFT JOIN (
	                SELECT dcusages.destination_template_id, tt.template_name AS `usages` 
	                FROM wiser_template_dynamiccontent_test dcusages 
	                INNER JOIN wiser_template_test tt ON tt.template_id=dcusages.template_id 
                ) AS otherdc ON otherdc.destination_template_id=tdclink.destination_template_id
                WHERE tdclink.template_id = ?templateid
                AND wdc.version = (SELECT MAX(dc.version) FROM wiser_dynamiccontent_test dc WHERE dc.templateid = wdc.templateid)
                GROUP BY wdc.templateid");
            var resultList = new List<LinkedDynamicContentDAO>();

            foreach (DataRow row in dataTable.Rows) {
                var resultDao = new LinkedDynamicContentDAO();

                resultDao.id = row.Field<int>("templateid");
                resultDao.component = row.Field<string>("component");
                resultDao.component_mode = row.Field<string>("component_mode");
                resultDao.usages = row.Field<string>("usages");
                resultDao.changed_on = row.Field<DateTime>("changed_on");
                resultDao.changed_by = row.Field<string>("changed_by");
                resultDao.title = row.Field<string>("title");

                resultList.Add(resultDao);
            }

            return resultList;
        }

        /// <summary>
        /// Saves the template data as a new version of the template.
        /// </summary>
        /// <param name="templateData">A templatedatamodel containing the new data to save as a new template version.</param>
        /// <returns>An int confirming the affected rows of the query.</returns>
        public Task<int> SaveTemplateVersion(TemplateDataModel templateData, List<int> sccsLinks, List<int>jsLinks)
        {
            connection.ClearParameters();
            connection.AddParameter("templateid", templateData.templateid);
            connection.AddParameter("name", templateData.name);
            connection.AddParameter("editorValue", templateData.editorValue);

            connection.AddParameter("useCache", templateData.useCache);
            connection.AddParameter("cacheMinutes", templateData.cacheMinutes);
            connection.AddParameter("handleRequests", templateData.handleRequests);
            connection.AddParameter("handleSession", templateData.handleSession);
            connection.AddParameter("handleObjects", templateData.handleObjects);
            connection.AddParameter("handleStandards", templateData.handleStandards);
            connection.AddParameter("handleTranslations", templateData.handleTranslations);
            connection.AddParameter("handleDynamicContent", templateData.handleDynamicContent);
            connection.AddParameter("handleLogicBlocks", templateData.handleLogicBlocks);
            connection.AddParameter("handleMutators", templateData.handleMutators);
            connection.AddParameter("loginRequired", templateData.loginRequired);
            connection.AddParameter("loginUserType", templateData.loginUserType);
            connection.AddParameter("loginSessionPrefix", templateData.loginSessionPrefix);
            connection.AddParameter("loginRole", templateData.loginRole);

            var mergeList = new List<int>();
            mergeList.AddRange(sccsLinks);
            mergeList.AddRange(jsLinks);
            var linkedTemplates = String.Join(",", mergeList);
            connection.AddParameter("templatelinks", linkedTemplates);

            return connection.ExecuteAsync(@"
                SET @VersionNumber = (SELECT MAX(version)+1 FROM `wiser_template_test` WHERE template_id = ?templateid GROUP BY template_id);
                INSERT INTO wiser_template_test (
                    template_name, 
                    template_data, 
                    template_type, 
                    `version`, 
                    template_id, 
                    changed_on, 
                    changed_by, 
                    `usecache`, 
                    cacheminutes, 
                    handlerequest, 
                    handlesession, 
                    handleobjects, 
                    handlestandards,
                    handletranslations,
                    handledynamiccontent,
                    handlelogicblocks,
                    handlemutators,
                    loginrequired,
                    loginusertype,
                    loginsessionprefix,
                    loginrole,
                    linkedtemplates
                ) 
                VALUES (
                    ?name,
                    ?editorValue,
                    5052,
                    @VersionNumber,
                    ?templateid,
                    NOW(),
                    'InsertTest',
                    ?useCache,
                    ?cacheMinutes,
                    ?handleRequests,
                    ?handleSession,
                    ?handleObjects,
                    ?handleStandards,
                    ?handleTranslations,
                    ?handleDynamicContent,
                    ?handleLogicBlocks,
                    ?handleMutators,
                    ?loginRequired,
                    ?loginUserType,
                    ?loginSessionPrefix,
                    ?loginRole,
                    ?templatelinks
                )
            ");
        }

        /// <summary>
        /// Saves the linked templates for a template. This will add new links and remove old links.
        /// </summary>
        /// <param name="templateId">The id of the template who's links to save.</param>
        /// <param name="linksToAdd">The list of template ids to add as a link</param>
        /// <param name="linksToRemove">The list of template ids to remove as a link</param>
        /// <returns></returns>
        public async Task<int> SaveLinkedTemplates(int templateId, List<int> linksToAdd, List<int> linksToRemove) {
            connection.ClearParameters();
            connection.AddParameter("templateid", templateId);

            if (linksToAdd.Count > 0) {
                var addQueryBase = @"INSERT INTO wiser_templatelink_test (template_id, destination_template_id, ordering, type, type_name, added_on)";


                var dynamicQuery = @"VALUES ";
                foreach (var link in linksToAdd)
                {
                    dynamicQuery += "(?templateid, "+link+ ", 1, (SELECT template_type FROM wiser_template_test WHERE template_id="+link+" ORDER BY version DESC LIMIT 1), 'new type', NOW()),";
                }
                dynamicQuery = dynamicQuery.Substring(0, dynamicQuery.Length - 1);
                var addQuery = addQueryBase + dynamicQuery;

                await connection.ExecuteAsync(addQuery);
            }

            if (linksToRemove.Count > 0)
            {
                var removeQueryBase = "DELETE FROM wiser_templatelink_test WHERE template_id=?templateid";

                var removeQueryList = " AND destination_template_id IN (";
                foreach(var link in linksToRemove)
                {
                    removeQueryList += link + ",";
                }
                removeQueryList = removeQueryList.Substring(0, removeQueryList.Length - 1) + ")";

                var removeQuery = removeQueryBase + removeQueryList;

                await connection.ExecuteAsync(removeQuery);
            }

            return 1;
        }

        /// <summary>
        /// Retreives a section of the treeview around the given id. In case the id is 0 the root section of the tree will be retrieved.
        /// </summary>
        /// <param name="parentId">The id of the parent element of the treesection that needs to be retrieved</param>
        /// <returns>A list of templatetreeview items that are children of the given id.</returns>
        public async Task<List<TemplateTreeViewDAO>> GetTreeViewSection(int parentId)
        {
            connection.ClearParameters();
            connection.AddParameter("parentid", parentId);
            var dataTable = new DataTable();
            // Retrieves the Root
            if (parentId == 0)
            {
                dataTable = await connection.GetAsync(@"
                SELECT wtt.id, wtt.template_name, wtt.template_type, wtt.template_id, wtt.parent_id FROM `wiser_template_test` wtt 
                LEFT JOIN wiser_template_test parentTemp ON parentTemp.template_id = wtt.parent_id
                WHERE wtt.parent_id IS NULL AND wtt.template_type = 1");
            }
            //Retrieve section under parentId
            else
            {
                dataTable = await connection.GetAsync(@"
                SELECT wtt.id, wtt.template_name, wtt.template_type, wtt.template_id, wtt.parent_id, IF((EXISTS (SELECT id FROM wiser_template_test WHERE parent_id=wtt.template_id)), 1,0) AS has_children FROM `wiser_template_test` wtt 
                LEFT JOIN wiser_template_test parentTemp ON parentTemp.template_id = wtt.parent_id
                WHERE parentTemp.template_id = ?parentid
                GROUP BY wtt.template_id
                ORDER BY wtt.template_type, wtt.template_name");
            }

            var treeviewSection = new List<TemplateTreeViewDAO>();

            foreach (DataRow row in dataTable.Rows)
            {
                var hasChildren = false;
                if(parentId != 0 && row.Field<int>("has_children")>0) {
                    hasChildren = true;
                }

                    var treeviewNode = new TemplateTreeViewDAO(
                        row.Field<int>("template_id"),
                        row.Field<string>("template_name"),
                        row.Field<int>("template_type"),
                        row.Field<int?>("parent_id"),
                        hasChildren
                    );
                treeviewSection.Add(treeviewNode);
            }

            return treeviewSection;
        }

        public async Task<List<SearchResultModel>> GetSearchResults(SearchSettingsModel searchSettings)
        {
            connection.ClearParameters();
            connection.AddParameter("needle", searchSettings.needle);
            var dataTable = await connection.GetAsync(BuildSearchQuery(searchSettings));

            var searchResults = new List<SearchResultModel>();

            foreach(DataRow row in dataTable.Rows)
            {
                var result = new SearchResultModel
                {
                    id = row.Field<int>("id"),
                    name = row.Field<string>("name"),
                    type = row.Field<string>("type"),
                    parent = row.Field<string>("parent")
                };
                searchResults.Add(result);
            }

            return searchResults;
        }

        private string BuildSearchQuery(SearchSettingsModel searchSettings)
        {
            var searchQuery = new StringBuilder();
            if (!searchSettings.IsTemplateSearchDisabled())
            {
                searchQuery.Append("SELECT t.template_id AS id, 'template' AS type, t.template_name AS name, (SELECT par.template_name FROM wiser_template_test par WHERE par.template_id=t.parent_id) AS parent FROM wiser_template_test t WHERE ");

                switch (searchSettings.searchEnvironment)
                {
                    case Environments.Development:
                        searchQuery.Append("t.version = (SELECT MAX(tt.version) FROM wiser_template_test tt WHERE tt.template_id = t.template_id)");
                        break;
                    case Environments.Test:
                        connection.AddParameter("environment", Environments.Test);
                        searchQuery.Append("t.published_envoirement & ?environment");
                        break;
                    case Environments.Acceptance:
                        connection.AddParameter("environment", Environments.Acceptance);
                        searchQuery.Append("t.published_envoirement & ?environment");
                        break;
                    case Environments.Live:
                        connection.AddParameter("environment", Environments.Live);
                        searchQuery.Append("t.published_envoirement & ?environment");
                        break;
                    default:
                        searchQuery.Append("t.version = (SELECT MAX(tt.version) FROM wiser_template_test tt WHERE tt.template_id = t.template_id)");
                        break;
                }

                searchQuery.Append("AND (");

                if (searchSettings.searchTemplateId) {
                    searchQuery.Append("t.template_id = ?needle OR "); 
                }
                if (searchSettings.searchTemplateType)
                {
                    searchQuery.Append("t.template_type LIKE CONCAT('%',?needle,'%') OR ");
                }
                if (searchSettings.searchTemplateName)
                {
                    searchQuery.Append("t.template_name LIKE CONCAT('%',?needle,'%') OR ");
                }
                if (searchSettings.searchTemplateData)
                {
                    searchQuery.Append("t.template_data LIKE CONCAT('%',?needle,'%') OR ");
                }
                if (searchSettings.searchTemplateParent)
                {
                    searchQuery.Append("t.template_name LIKE CONCAT('%',?needle,'%') OR ");
                }
                if (searchSettings.searchTemplateLinkedTemplates)
                {
                    searchQuery.Append("t.linkedtemplates LIKE CONCAT('%',?needle,'%') OR ");
                }

                //Remove last 'OR' from query
                searchQuery.Remove(searchQuery.Length - 3, 3);

                searchQuery.Append(")");

                searchQuery.Append(" UNION ALL ");
            }
            if (!searchSettings.IsDynamicContentSearchDisabled())
            {
                searchQuery.Append("SELECT wdc.templateid AS id, 'dynamiccontent' AS type, wdc.title AS name, (SELECT GROUP_CONCAT(tdc.template_id) FROM wiser_template_dynamiccontent_test tdc WHERE tdc.destination_template_id=wdc.templateid) AS parent FROM wiser_dynamiccontent_test wdc WHERE ");

                switch (searchSettings.searchEnvironment)
                {
                    case Environments.Development:
                        searchQuery.Append("wdc.version = (SELECT MAX(dc.version) FROM wiser_dynamiccontent_test dc WHERE dc.templateid = wdc.templateid)");
                        break;
                    case Environments.Test:
                        connection.AddParameter("environment", Environments.Test);
                        searchQuery.Append("wdc.published_envoirement & ?environment");
                        break;
                    case Environments.Acceptance:
                        connection.AddParameter("environment", Environments.Acceptance);
                        searchQuery.Append("wdc.published_envoirement & ?environment");
                        break;
                    case Environments.Live:
                        connection.AddParameter("environment", Environments.Live);
                        searchQuery.Append("wdc.published_envoirement & ?environment");
                        break;
                    default:
                        searchQuery.Append("wdc.version = (SELECT MAX(dc.version) FROM wiser_dynamiccontent_test dc WHERE dc.templateid = wdc.templateid)");
                        break;
                }

                searchQuery.Append("AND (");

                if (searchSettings.searchDynamicContentId)
                {
                    searchQuery.Append("wdc.templateid = ?needle OR ");
                }
                if (searchSettings.searchDynamicContentFilledVariables)
                {
                    searchQuery.Append("wdc.filledvariables LIKE CONCAT('%',?needle,'%') OR ");
                }
                if (searchSettings.searchDynamicContentComponentName)
                {
                    searchQuery.Append("wdc.component LIKE CONCAT('%',?needle,'%') OR ");
                }
                if (searchSettings.searchDynamicContentComponentMode)
                {
                    searchQuery.Append("wdc.component_mode LIKE CONCAT('%',?needle,'%') OR ");
                }

                //Remove last 'OR' from query
                searchQuery.Remove(searchQuery.Length-3, 3);

                searchQuery.Append(")");
            }

            return searchQuery.ToString();
        }
    }
}

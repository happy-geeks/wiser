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
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Enums;

namespace Api.Modules.Templates.Services.DataLayer
{
    /// <inheritdoc cref="ITemplateDataService" />
    public class TemplateDataService : ITemplateDataService, IScopedService
    {
        private readonly IDatabaseConnection connection;

        /// <summary>
        /// Creates a new instance of <see cref="TemplateDataService"/>.
        /// </summary>
        public TemplateDataService(IDatabaseConnection connection)
        {
            this.connection = connection;
        }

        /// <inheritdoc />
        public async Task<TemplateSettingsModel> GetTemplateMetaData(int templateId)
        {
            connection.ClearParameters();
            connection.AddParameter("templateId", templateId);
            var dataTable = await connection.GetAsync($@"SELECT 
                                                                template.parent_id,
                                                                template.template_type,
                                                                template.template_name, 
                                                                template.version, 
                                                                template.changed_on, 
                                                                template.changed_by
                                                            FROM {WiserTableNames.WiserTemplate} AS template 
                                                            LEFT JOIN {WiserTableNames.WiserTemplate} AS otherVersion ON otherVersion.template_id = template.template_id AND otherVersion.version > template.version
                                                            WHERE template.template_id = ?templateId
                                                            AND otherVersion.id IS NULL
                                                            LIMIT 1");
            
            return dataTable.Rows.Count == 0 ? new TemplateSettingsModel() : new TemplateSettingsModel
            {
                TemplateId = templateId,
                ParentId = dataTable.Rows[0].Field<int?>("parent_id"),
                Type = dataTable.Rows[0].Field<TemplateTypes>("template_type"),
                Name = dataTable.Rows[0].Field<string>("template_name"),
                Version = dataTable.Rows[0].Field<int>("version"),
                ChangedOn = dataTable.Rows[0].Field<DateTime>("changed_on"),
                ChangedBy = dataTable.Rows[0].Field<string>("changed_by")
            };
        }

        /// <inheritdoc />
        public async Task<TemplateSettingsModel> GetTemplateData(int templateId)
        {
            connection.ClearParameters();
            connection.AddParameter("templateId", templateId);
            var dataTable = await connection.GetAsync($@"SELECT wtt.template_id, wtt.parent_id, wtt.template_type, wtt.template_name, wtt.template_data, wtt.version, wtt.changed_on, wtt.changed_by, wtt.usecache, 
                wtt.cacheminutes, wtt.handlerequest, wtt.handlesession, wtt.handleobjects, wtt.handlestandards, wtt.handletranslations, wtt.handledynamiccontent, wtt.handlelogicblocks, wtt.handlemutators, 
                wtt.loginrequired, wtt.loginusertype, wtt.loginsessionprefix, wtt.loginrole , GROUP_CONCAT(CONCAT_WS(';',linkedtemplates.template_id, linkedtemplates.template_name, linkedtemplates.template_type)) AS linkedtemplates
                FROM {WiserTableNames.WiserTemplate} wtt 
				LEFT JOIN (SELECT linkedTemplate.template_id, template_name, template_type FROM {WiserTableNames.WiserTemplate} linkedTemplate GROUP BY template_id) AS linkedtemplates 
				ON FIND_IN_SET(linkedtemplates.template_id ,wtt.linkedtemplates)
                WHERE wtt.template_id = ?templateId AND wtt.version = (SELECT MAX(version) FROM {WiserTableNames.WiserTemplate} WHERE template_id = wtt.template_id)
				GROUP BY wtt.template_id
                ORDER BY wtt.version DESC 
                LIMIT 1"
            );

            var templateData = new TemplateSettingsModel();

            if (dataTable.Rows.Count == 1)
            {
                templateData.TemplateId = dataTable.Rows[0].Field<int>("template_id");
                templateData.ParentId = dataTable.Rows[0].Field<int?>("parent_id");
                templateData.Type = dataTable.Rows[0].Field<TemplateTypes>("template_type");
                templateData.Name = dataTable.Rows[0].Field<string>("template_name");
                templateData.EditorValue = dataTable.Rows[0].Field<string>("template_data");
                templateData.Version = dataTable.Rows[0].Field<int>("version");
                templateData.ChangedOn = dataTable.Rows[0].Field<DateTime>("changed_on");
                templateData.ChangedBy = dataTable.Rows[0].Field<string>("changed_by");

                templateData.UseCache = dataTable.Rows[0].Field<int>("usecache");
                templateData.CacheMinutes = dataTable.Rows[0].Field<int>("cacheminutes");
                templateData.HandleRequests = Convert.ToBoolean(dataTable.Rows[0].Field<sbyte>("handlerequest"));
                templateData.HandleSession = Convert.ToBoolean(dataTable.Rows[0].Field<sbyte>("handlesession"));
                templateData.HandleStandards = Convert.ToBoolean(dataTable.Rows[0].Field<sbyte>("handlestandards"));
                templateData.HandleObjects = Convert.ToBoolean(dataTable.Rows[0].Field<sbyte>("handleobjects"));
                templateData.HandleTranslations = Convert.ToBoolean(dataTable.Rows[0].Field<sbyte>("handletranslations"));
                templateData.HandleDynamicContent = Convert.ToBoolean(dataTable.Rows[0].Field<sbyte>("handledynamiccontent"));
                templateData.HandleLogicBlocks = Convert.ToBoolean(dataTable.Rows[0].Field<sbyte>("handlelogicblocks"));
                templateData.HandleMutators = Convert.ToBoolean(dataTable.Rows[0].Field<sbyte>("handlemutators"));
                templateData.LoginRequired = Convert.ToBoolean(dataTable.Rows[0].Field<sbyte>("loginrequired"));
                templateData.LoginUserType = dataTable.Rows[0].Field<string>("loginusertype");
                templateData.LoginSessionPrefix = dataTable.Rows[0].Field<string>("loginsessionprefix");
                templateData.LoginRole = dataTable.Rows[0].Field<string>("loginrole");
            }

            return templateData;
        }

        /// <inheritdoc />
        public async Task<Dictionary<int, int>> GetPublishedEnvironmentsFromTemplate(int templateId)
        {
            connection.ClearParameters();
            connection.AddParameter("templateid", templateId);
            var versionList = new Dictionary<int, int>();

            var dataTable = await connection.GetAsync($"SELECT wtt.version, wtt.published_environment FROM {WiserTableNames.WiserTemplate} wtt WHERE wtt.template_id = ?templateid");

            foreach (DataRow row in dataTable.Rows)
            {
                versionList.Add(row.Field<int>("version"), row.Field<SByte>("published_environment"));
            }

            return versionList;
        }

        /// <inheritdoc />
        public async Task<int> PublishEnvironmentOfTemplate(int templateId, Dictionary<int, int> publishModel, PublishLogModel publishLog, string username)
        {
            connection.ClearParameters();
            connection.AddParameter("templateid", templateId);

            var baseQueryPart = $@"UPDATE {WiserTableNames.WiserTemplate} wtt 
                SET wtt.published_environment = case wtt.version";

            var dynamicQueryPart = "";
            var dynamicWherePart = " AND wtt.version IN (";
            foreach (var versionChange in publishModel)
            {
                dynamicQueryPart += " WHEN " + versionChange.Key + " THEN wtt.published_environment+" + versionChange.Value;
                dynamicWherePart += versionChange.Key + ",";
            }
            dynamicWherePart = dynamicWherePart.Substring(0, dynamicWherePart.Length - 1) + ")";
            var endQueryPart = @" end
                WHERE wtt.template_id = ?templateid";

            var query = baseQueryPart + dynamicQueryPart + endQueryPart + dynamicWherePart;

            connection.AddParameter("oldlive", publishLog.OldLive);
            connection.AddParameter("oldaccept", publishLog.OldAccept);
            connection.AddParameter("oldtest", publishLog.OldTest);
            connection.AddParameter("newlive", publishLog.NewLive);
            connection.AddParameter("newaccept", publishLog.NewAccept);
            connection.AddParameter("newtest", publishLog.NewTest);
            connection.AddParameter("now", DateTime.Now);
            connection.AddParameter("username", username);

            var logQuery = $@"INSERT INTO {WiserTableNames.WiserTemplatePublishLog} (template_id, old_live, old_accept, old_test, new_live, new_accept, new_test, changed_on, changed_by) 
            VALUES(
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

            return await connection.ExecuteAsync(query + ";" + logQuery);
        }

        /// <inheritdoc />
        public async Task<List<LinkedTemplateModel>> GetLinkedTemplates(int templateId)
        {
            connection.ClearParameters();
            connection.AddParameter("templateid", templateId);
            var dataTable = await connection.GetAsync($@"SELECT wtt.template_name, ?templateid AS template_id, wtt.template_id AS destination_template_id, wtt.template_type AS type
                    FROM {WiserTableNames.WiserTemplate} wtt
					WHERE wtt.template_id !=?templateid AND FIND_IN_SET(wtt.template_id, (SELECT tlink.linkedtemplates FROM {WiserTableNames.WiserTemplate} tlink WHERE tlink.template_id = ?templateid AND tlink.version = (SELECT MAX(version) FROM {WiserTableNames.WiserTemplate} WHERE template_id = tlink.template_id))) GROUP BY wtt.template_id ORDER BY wtt.template_name");

            var linkList = new List<LinkedTemplateModel>();

            foreach (DataRow row in dataTable.Rows)
            {
                var linkedTemplate = new LinkedTemplateModel();
                linkedTemplate.TemplateId = row.Field<int>("destination_template_id");
                linkedTemplate.TemplateName = row.Field<string>("template_name");
                linkedTemplate.LinkType = row.Field<TemplateTypes>("type");

                linkList.Add(linkedTemplate);
            }

            return linkList;
        }

        /// <inheritdoc />
        public async Task<List<LinkedTemplateModel>> GetLinkOptionsForTemplate(int templateId)
        {
            connection.ClearParameters();
            connection.AddParameter("templateid", templateId);
            var dataTable = await connection.GetAsync($@"SELECT wtt.template_name, wtt.template_id, wtt.template_type 
                    FROM {WiserTableNames.WiserTemplate} wtt
					WHERE wtt.template_id !=?templateid AND FIND_IN_SET(wtt.template_id, (SELECT tlink.linkedtemplates FROM {WiserTableNames.WiserTemplate} tlink WHERE tlink.template_id = ?templateid AND tlink.version = (SELECT MAX(version) FROM {WiserTableNames.WiserTemplate} WHERE template_id = tlink.template_id))) = 0 ORDER BY wtt.template_name"
            );

            var linkList = new List<LinkedTemplateModel>();

            foreach (DataRow row in dataTable.Rows)
            {
                var linkedTemplate = new LinkedTemplateModel();
                linkedTemplate.TemplateId = row.Field<int>("template_id");
                linkedTemplate.TemplateName = row.Field<string>("template_name");
                linkedTemplate.LinkType = row.Field<TemplateTypes>("template_type");

                linkList.Add(linkedTemplate);
            }

            return linkList;
        }

        /// <inheritdoc />
        public async Task<List<LinkedDynamicContentDao>> GetLinkedDynamicContent(int templateId)
        {
            connection.ClearParameters();
            connection.AddParameter("templateid", templateId);
            var dataTable = await connection.GetAsync($@"SELECT 
                wdc.content_id, 
                wdc.component, 
                wdc.component_mode, 
                GROUP_CONCAT(DISTINCT otherdc.`usages`) AS `usages`,
                wdc.changed_on,
                wdc.changed_by,
                wdc.`title`
                FROM {WiserTableNames.WiserTemplateDynamicContent} tdclink 
                LEFT JOIN {WiserTableNames.WiserDynamicContent} wdc ON wdc.content_id = tdclink.content_id 
                LEFT JOIN (
	                SELECT dcusages.content_id, tt.template_name AS `usages` 
	                FROM {WiserTableNames.WiserTemplateDynamicContent} dcusages 
	                INNER JOIN {WiserTableNames.WiserTemplate} tt ON tt.template_id=dcusages.destination_template_id 
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
                resultDao.ChangedOn = row.Field<DateTime>("changed_on");
                resultDao.ChangedBy = row.Field<string>("changed_by");
                resultDao.Title = row.Field<string>("title");

                resultList.Add(resultDao);
            }

            return resultList;
        }

        /// <inheritdoc />
        public Task<int> SaveTemplateVersion(TemplateSettingsModel templateSettings, List<int> sccsLinks, List<int> jsLinks, string username)
        {
            connection.ClearParameters();
            connection.AddParameter("templateId", templateSettings.TemplateId);
            connection.AddParameter("parentId", templateSettings.ParentId);
            connection.AddParameter("name", templateSettings.Name);
            connection.AddParameter("editorValue", templateSettings.EditorValue);
            connection.AddParameter("type", templateSettings.Type);
            connection.AddParameter("useCache", templateSettings.UseCache);
            connection.AddParameter("cacheMinutes", templateSettings.CacheMinutes);
            connection.AddParameter("handleRequests", templateSettings.HandleRequests);
            connection.AddParameter("handleSession", templateSettings.HandleSession);
            connection.AddParameter("handleObjects", templateSettings.HandleObjects);
            connection.AddParameter("handleStandards", templateSettings.HandleStandards);
            connection.AddParameter("handleTranslations", templateSettings.HandleTranslations);
            connection.AddParameter("handleDynamicContent", templateSettings.HandleDynamicContent);
            connection.AddParameter("handleLogicBlocks", templateSettings.HandleLogicBlocks);
            connection.AddParameter("handleMutators", templateSettings.HandleMutators);
            connection.AddParameter("loginRequired", templateSettings.LoginRequired);
            connection.AddParameter("loginUserType", templateSettings.LoginUserType);
            connection.AddParameter("loginSessionPrefix", templateSettings.LoginSessionPrefix);
            connection.AddParameter("loginRole", templateSettings.LoginRole);
            connection.AddParameter("now", DateTime.Now);
            connection.AddParameter("username", username);

            var mergeList = new List<int>();
            mergeList.AddRange(sccsLinks);
            mergeList.AddRange(jsLinks);
            var linkedTemplates = String.Join(",", mergeList);
            connection.AddParameter("templatelinks", linkedTemplates);

            return connection.ExecuteAsync($@"
                SET @VersionNumber = (SELECT MAX(version)+1 FROM {WiserTableNames.WiserTemplate} WHERE template_id = ?templateid GROUP BY template_id);
                INSERT INTO {WiserTableNames.WiserTemplate} (
                    template_name, 
                    template_data, 
                    template_type, 
                    `version`, 
                    template_id, 
                    parent_id,
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
                    ?type,
                    @VersionNumber,
                    ?templateId,
                    ?parentId,
                    ?now,
                    ?username,
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

        /// <inheritdoc />
        public async Task<int> SaveLinkedTemplates(int templateId, List<int> linksToAdd, List<int> linksToRemove)
        {
            connection.ClearParameters();
            connection.AddParameter("templateid", templateId);

            if (linksToAdd.Count > 0)
            {
                var addQueryBase = $"INSERT INTO {WiserTableNames.WiserTemplateLink} (template_id, destination_template_id, ordering, type, type_name, added_on)";


                var dynamicQuery = @"VALUES ";
                foreach (var link in linksToAdd)
                {
                    dynamicQuery += $"(?templateid, {link}, 1, (SELECT template_type FROM {WiserTableNames.WiserTemplate} WHERE template_id={link} ORDER BY version DESC LIMIT 1), 'new type', NOW()),";
                }
                dynamicQuery = dynamicQuery.Substring(0, dynamicQuery.Length - 1);
                var addQuery = addQueryBase + dynamicQuery;

                await connection.ExecuteAsync(addQuery);
            }

            if (linksToRemove.Count > 0)
            {
                var removeQueryBase = $"DELETE FROM {WiserTableNames.WiserTemplateLink} WHERE template_id=?templateid";

                var removeQueryList = " AND destination_template_id IN (";
                foreach (var link in linksToRemove)
                {
                    removeQueryList += link + ",";
                }
                removeQueryList = removeQueryList.Substring(0, removeQueryList.Length - 1) + ")";

                var removeQuery = removeQueryBase + removeQueryList;

                await connection.ExecuteAsync(removeQuery);
            }

            return 1;
        }

        /// <inheritdoc />
        public async Task<List<TemplateTreeViewDao>> GetTreeViewSection(int parentId)
        {
            connection.ClearParameters();
            connection.AddParameter("parentid", parentId);
            DataTable dataTable;
            // Retrieves the Root
            if (parentId == 0)
            {
                dataTable = await connection.GetAsync($@"
                SELECT wtt.id, wtt.template_name, wtt.template_type, wtt.template_id, wtt.parent_id FROM {WiserTableNames.WiserTemplate} wtt 
                LEFT JOIN {WiserTableNames.WiserTemplate} parentTemp ON parentTemp.template_id = wtt.parent_id
                WHERE wtt.parent_id IS NULL AND wtt.template_type = 7");
            }
            //Retrieve section under parentId
            else
            {
                dataTable = await connection.GetAsync($@"
                SELECT wtt.id, wtt.template_name, wtt.template_type, wtt.template_id, wtt.parent_id, IF((EXISTS (SELECT id FROM {WiserTableNames.WiserTemplate} WHERE parent_id=wtt.template_id)), 1,0) AS has_children FROM {WiserTableNames.WiserTemplate} wtt 
                LEFT JOIN {WiserTableNames.WiserTemplate} parentTemp ON parentTemp.template_id = wtt.parent_id
                WHERE parentTemp.template_id = ?parentid
                GROUP BY wtt.template_id
                ORDER BY wtt.template_type DESC, wtt.template_name");
            }

            var treeviewSection = new List<TemplateTreeViewDao>();

            foreach (DataRow row in dataTable.Rows)
            {
                bool hasChildren = parentId != 0 && row.Field<int>("has_children") > 0;

                var treeviewNode = new TemplateTreeViewDao
                {
                    HasChildren = hasChildren,
                    ParentId = row.Field<int?>("parent_id"),
                    TemplateId = row.Field<int>("template_id"),
                    TemplateName = row.Field<string>("template_name"),
                    TemplateType = row.Field<TemplateTypes>("template_type")
                };

                treeviewSection.Add(treeviewNode);
            }

            return treeviewSection;
        }

        /// <inheritdoc />
        public async Task<List<SearchResultModel>> GetSearchResults(SearchSettingsModel searchSettings)
        {
            connection.ClearParameters();
            connection.AddParameter("needle", searchSettings.Needle);
            var dataTable = await connection.GetAsync(BuildSearchQuery(searchSettings));

            var searchResults = new List<SearchResultModel>();

            foreach (DataRow row in dataTable.Rows)
            {
                var result = new SearchResultModel
                {
                    Id = row.Field<int>("id"),
                    Name = row.Field<string>("name"),
                    Type = row.Field<string>("type"),
                    Parent = row.Field<string>("parent")
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
                searchQuery.Append($"SELECT t.template_id AS id, 'template' AS type, t.template_name AS name, (SELECT par.template_name FROM {WiserTableNames.WiserTemplate} par WHERE par.template_id=t.parent_id) AS parent FROM {WiserTableNames.WiserTemplate} t WHERE ");

                switch (searchSettings.SearchEnvironment)
                {
                    case Environments.Development:
                        searchQuery.Append($"t.version = (SELECT MAX(tt.version) FROM {WiserTableNames.WiserTemplate} tt WHERE tt.template_id = t.template_id)");
                        break;
                    case Environments.Test:
                        connection.AddParameter("environment", Environments.Test);
                        searchQuery.Append("t.published_environment & ?environment");
                        break;
                    case Environments.Acceptance:
                        connection.AddParameter("environment", Environments.Acceptance);
                        searchQuery.Append("t.published_environment & ?environment");
                        break;
                    case Environments.Live:
                        connection.AddParameter("environment", Environments.Live);
                        searchQuery.Append("t.published_environment & ?environment");
                        break;
                    default:
                        searchQuery.Append($"t.version = (SELECT MAX(tt.version) FROM {WiserTableNames.WiserTemplate} tt WHERE tt.template_id = t.template_id)");
                        break;
                }

                searchQuery.Append("AND (");

                if (searchSettings.SearchTemplateId)
                {
                    searchQuery.Append("t.template_id = ?needle OR ");
                }
                if (searchSettings.SearchTemplateType)
                {
                    searchQuery.Append("t.template_type LIKE CONCAT('%',?needle,'%') OR ");
                }
                if (searchSettings.SearchTemplateName)
                {
                    searchQuery.Append("t.template_name LIKE CONCAT('%',?needle,'%') OR ");
                }
                if (searchSettings.SearchTemplateData)
                {
                    searchQuery.Append("t.template_data LIKE CONCAT('%',?needle,'%') OR ");
                }
                if (searchSettings.SearchTemplateParent)
                {
                    searchQuery.Append("t.template_name LIKE CONCAT('%',?needle,'%') OR ");
                }
                if (searchSettings.SearchTemplateLinkedTemplates)
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
                searchQuery.Append($"SELECT wdc.content_id AS id, 'dynamiccontent' AS type, wdc.title AS name, (SELECT GROUP_CONCAT(tdc.template_id) FROM {WiserTableNames.WiserTemplateDynamicContent} tdc WHERE tdc.destination_template_id=wdc.content_id) AS parent FROM {WiserTableNames.WiserDynamicContent} wdc WHERE ");

                switch (searchSettings.SearchEnvironment)
                {
                    case Environments.Development:
                        searchQuery.Append($"wdc.version = (SELECT MAX(dc.version) FROM {WiserTableNames.WiserDynamicContent} dc WHERE dc.content_id = wdc.content_id)");
                        break;
                    case Environments.Test:
                        connection.AddParameter("environment", Environments.Test);
                        searchQuery.Append("wdc.published_environment & ?environment");
                        break;
                    case Environments.Acceptance:
                        connection.AddParameter("environment", Environments.Acceptance);
                        searchQuery.Append("wdc.published_environment & ?environment");
                        break;
                    case Environments.Live:
                        connection.AddParameter("environment", Environments.Live);
                        searchQuery.Append("wdc.published_environment & ?environment");
                        break;
                    default:
                        searchQuery.Append($"wdc.version = (SELECT MAX(dc.version) FROM {WiserTableNames.WiserDynamicContent} dc WHERE dc.content_id = wdc.content_id)");
                        break;
                }

                searchQuery.Append("AND (");

                if (searchSettings.SearchDynamicContentId)
                {
                    searchQuery.Append("wdc.content_id = ?needle OR ");
                }
                if (searchSettings.SearchDynamicContentSettings)
                {
                    searchQuery.Append("wdc.settings LIKE CONCAT('%',?needle,'%') OR ");
                }
                if (searchSettings.SearchDynamicContentComponentName)
                {
                    searchQuery.Append("wdc.component LIKE CONCAT('%',?needle,'%') OR ");
                }
                if (searchSettings.SearchDynamicContentComponentMode)
                {
                    searchQuery.Append("wdc.component_mode LIKE CONCAT('%',?needle,'%') OR ");
                }

                //Remove last 'OR' from query
                searchQuery.Remove(searchQuery.Length - 3, 3);

                searchQuery.Append(")");
            }

            return searchQuery.ToString();
        }
    }
}

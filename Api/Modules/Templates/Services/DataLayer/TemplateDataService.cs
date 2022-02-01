using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public async Task<TemplateSettingsModel> GetMetaDataAsync(int templateId)
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
        public async Task<TemplateSettingsModel> GetDataAsync(int templateId)
        {
            connection.ClearParameters();
            connection.AddParameter("templateId", templateId);
            var dataTable = await connection.GetAsync($@"SELECT 
                                                                template.template_id, 
                                                                template.parent_id, 
                                                                template.template_type, 
                                                                template.template_name, 
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
                                                                template.linked_templates
                                                            FROM {WiserTableNames.WiserTemplate} AS template 
                                                            WHERE template.template_id = ?templateId
                                                            ORDER BY template.version DESC 
                                                            LIMIT 1");

            if (dataTable.Rows.Count == 0)
            {
                return null;
            }

            var templateData = new TemplateSettingsModel
            {
                TemplateId = dataTable.Rows[0].Field<int>("template_id"),
                ParentId = dataTable.Rows[0].Field<int?>("parent_id"),
                Type = dataTable.Rows[0].Field<TemplateTypes>("template_type"),
                Name = dataTable.Rows[0].Field<string>("template_name"),
                EditorValue = dataTable.Rows[0].Field<string>("template_data"),
                Version = dataTable.Rows[0].Field<int>("version"),
                ChangedOn = dataTable.Rows[0].Field<DateTime>("changed_on"),
                ChangedBy = dataTable.Rows[0].Field<string>("changed_by"),
                UseCache = dataTable.Rows[0].Field<int>("use_cache"),
                CacheMinutes = dataTable.Rows[0].Field<int>("cache_minutes"),
                HandleRequests = Convert.ToBoolean(dataTable.Rows[0].Field<sbyte>("handle_request")),
                HandleSession = Convert.ToBoolean(dataTable.Rows[0].Field<sbyte>("handle_session")),
                HandleStandards = Convert.ToBoolean(dataTable.Rows[0].Field<sbyte>("handle_standards")),
                HandleObjects = Convert.ToBoolean(dataTable.Rows[0].Field<sbyte>("handle_objects")),
                HandleTranslations = Convert.ToBoolean(dataTable.Rows[0].Field<sbyte>("handle_translations")),
                HandleDynamicContent = Convert.ToBoolean(dataTable.Rows[0].Field<sbyte>("handle_dynamic_content")),
                HandleLogicBlocks = Convert.ToBoolean(dataTable.Rows[0].Field<sbyte>("handle_logic_blocks")),
                HandleMutators = Convert.ToBoolean(dataTable.Rows[0].Field<sbyte>("handle_mutators")),
                LoginRequired = Convert.ToBoolean(dataTable.Rows[0].Field<sbyte>("login_required")),
                LoginUserType = dataTable.Rows[0].Field<string>("login_user_type"),
                LoginSessionPrefix = dataTable.Rows[0].Field<string>("login_session_prefix"),
                LoginRole = dataTable.Rows[0].Field<string>("login_role")
            };

            return templateData;
        }

        /// <inheritdoc />
        public async Task<Dictionary<int, int>> GetPublishedEnvironmentsAsync(int templateId)
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
        public async Task<int> UpdatePublishedEnvironmentAsync(int templateId, Dictionary<int, int> publishModel, PublishLogModel publishLog, string username)
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
        public async Task<List<LinkedTemplateModel>> GetLinkedTemplatesAsync(int templateId)
        {
            connection.ClearParameters();
            connection.AddParameter("templateId", templateId);
            var dataTable = await connection.GetAsync($"SELECT linked_templates FROM {WiserTableNames.WiserTemplate} WHERE template_id = ?templateId ORDER BY version DESC LIMIT 1");
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

            dataTable = await connection.GetAsync($@"SELECT
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
            connection.ClearParameters();
            connection.AddParameter("templateId", templateId);
            var dataTable = await connection.GetAsync($@"SELECT 
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
                                                            WHERE template.template_type IN (2, 3, 4)
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
        public Task<int> SaveAsync(TemplateSettingsModel templateSettings, List<int> sccsLinks, List<int> jsLinks, string username)
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
            connection.AddParameter("templateLinks", String.Join(",", mergeList));

            return connection.ExecuteAsync($@"
                SET @VersionNumber = (SELECT MAX(version)+1 FROM {WiserTableNames.WiserTemplate} WHERE template_id = ?templateId GROUP BY template_id);
                INSERT INTO {WiserTableNames.WiserTemplate} (
                    template_name, 
                    template_data, 
                    template_type, 
                    `version`, 
                    template_id, 
                    parent_id,
                    changed_on, 
                    changed_by, 
                    use_cache,
                    cache_minutes, 
                    handle_request, 
                    handle_session, 
                    handle_objects, 
                    handle_standards,
                    handle_translations,
                    handle_dynamic_content,
                    handle_logic_blocks,
                    handle_mutators,
                    login_required,
                    login_user_type,
                    login_session_prefix,
                    login_role,
                    linked_templates
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
                    ?templateLinks
                )");
        }

        /// <inheritdoc />
        public async Task<int> UpdateLinkedTemplatesAsync(int templateId, List<int> linksToAdd, List<int> linksToRemove)
        {
            connection.ClearParameters();
            connection.AddParameter("templateId", templateId);
            connection.AddParameter("now", DateTime.Now);

            if (linksToAdd.Count > 0)
            {
                var addQueryBase = $"INSERT INTO {WiserTableNames.WiserTemplateLink} (template_id, destination_template_id, ordering, type, type_name, added_on)";


                var dynamicQuery = @"VALUES ";
                foreach (var link in linksToAdd)
                {
                    dynamicQuery += $"(?templateid, {link}, 1, (SELECT template_type FROM {WiserTableNames.WiserTemplate} WHERE template_id={link} ORDER BY version DESC LIMIT 1), 'new type', ?now),";
                }
                dynamicQuery = dynamicQuery.Substring(0, dynamicQuery.Length - 1);
                var addQuery = addQueryBase + dynamicQuery;

                await connection.ExecuteAsync(addQuery);
            }

            if (linksToRemove.Count > 0)
            {
                var removeQueryBase = $"DELETE FROM {WiserTableNames.WiserTemplateLink} WHERE template_id = ?templateId";

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
        public async Task<List<TemplateTreeViewDao>> GetTreeViewSectionAsync(int parentId)
        {
            connection.ClearParameters();
            connection.AddParameter("parentId", parentId);

            // TODO: Order by ordering column (which doesn't exist yet).
            var query = $@"SELECT
	                        template.id,
	                        template.template_name,
	                        template.template_type,
	                        template.template_id,
	                        template.parent_id,
	                        COUNT(child.id) > 0 AS has_children
                        FROM {WiserTableNames.WiserTemplate} AS template
                        LEFT JOIN {WiserTableNames.WiserTemplate} AS otherVersion ON otherVersion.template_id = template.template_id AND otherVersion.version > template.version
                        LEFT JOIN {WiserTableNames.WiserTemplate} AS child ON child.parent_id = template.template_id
                        WHERE template.parent_id {(parentId == 0 ? "IS NULL AND template.template_type = 7" : "= ?parentId")}
                        AND otherVersion.id IS NULL
                        GROUP BY template.template_id
                        ORDER BY template.template_type DESC, template.template_name ASC";

            var dataTable = await connection.GetAsync(query);

            return (dataTable.Rows.Cast<DataRow>()
            .Select(row => new TemplateTreeViewDao
            {
                HasChildren = Convert.ToBoolean(row["has_children"]),
                ParentId = row.Field<int?>("parent_id"),
                TemplateId = row.Field<int>("template_id"),
                TemplateName = row.Field<string>("template_name"),
                TemplateType = row.Field<TemplateTypes>("template_type")
            })).ToList();
        }

        /// <inheritdoc />
        public async Task<List<SearchResultModel>> SearchAsync(SearchSettingsModel searchSettings)
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

        /// <inheritdoc/>
        public async Task<int> CreateAsync(string name, int parent, TemplateTypes type, string username)
        {
            connection.ClearParameters();
            connection.AddParameter("name", name);
            connection.AddParameter("parent", parent);
            connection.AddParameter("type", type);
            connection.AddParameter("now", DateTime.Now);
            connection.AddParameter("username", username);

            var dataTable = await connection.GetAsync(@$"SET @id = (SELECT MAX(template_id)+1 FROM {WiserTableNames.WiserTemplate});
                                                            INSERT INTO {WiserTableNames.WiserTemplate} (parent_id, template_name, template_type, version, template_id, changed_on, changed_by, published_environment)
                                                            VALUES (?parent, ?name, ?type, 1, @id, ?now, ?username, 0);
                                                            SELECT @id;");

            return Convert.ToInt32(dataTable.Rows[0]["@id"]);
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
                    searchQuery.Append("t.linked_templates LIKE CONCAT('%',?needle,'%') OR ");
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

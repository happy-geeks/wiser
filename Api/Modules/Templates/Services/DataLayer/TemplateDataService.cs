using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Api.Modules.Kendo.Enums;
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
        private readonly IDatabaseConnection clientDatabaseConnection;

        /// <summary>
        /// Creates a new instance of <see cref="TemplateDataService"/>.
        /// </summary>
        public TemplateDataService(IDatabaseConnection clientDatabaseConnection)
        {
            this.clientDatabaseConnection = clientDatabaseConnection;
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
                ChangedOn = dataTable.Rows[0].Field<DateTime>("changed_on"),
                ChangedBy = dataTable.Rows[0].Field<string>("changed_by"),
                Ordering = dataTable.Rows[0].Field<int>("ordering")
            };
        }

        /// <inheritdoc />
        public async Task<TemplateSettingsModel> GetDataAsync(int templateId)
        {
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("templateId", templateId);
            var dataTable = await clientDatabaseConnection.GetAsync($@"SELECT 
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
                                                                template.cache_location, 
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
                                                                template.linked_templates, 
                                                                template.ordering,
                                                                template.insert_mode,
                                                                template.load_always,
                                                                template.url_regex,
                                                                template.external_files,
                                                                template.grouping_create_object_instead_of_array,
                                                                template.grouping_prefix,
                                                                template.grouping_key,
                                                                template.grouping_key_column_name,
                                                                template.grouping_value_column_name,
                                                                template.is_scss_include_template,
                                                                template.use_in_wiser_html_editors,
                                                                template.pre_load_query
                                                            FROM {WiserTableNames.WiserTemplate} AS template 
                                                            WHERE template.template_id = ?templateId
                                                            AND template.removed = 0
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
                UseCache = (TemplateCachingModes)dataTable.Rows[0].Field<int>("use_cache"),
                CacheMinutes = dataTable.Rows[0].Field<int>("cache_minutes"),
                CacheLocation = (TemplateCachingLocations)dataTable.Rows[0].Field<int>("cache_location"),
                HandleRequests = Convert.ToBoolean(dataTable.Rows[0]["handle_request"]),
                HandleSession = Convert.ToBoolean(dataTable.Rows[0]["handle_session"]),
                HandleStandards = Convert.ToBoolean(dataTable.Rows[0]["handle_standards"]),
                HandleObjects = Convert.ToBoolean(dataTable.Rows[0]["handle_objects"]),
                HandleTranslations = Convert.ToBoolean(dataTable.Rows[0]["handle_translations"]),
                HandleDynamicContent = Convert.ToBoolean(dataTable.Rows[0]["handle_dynamic_content"]),
                HandleLogicBlocks = Convert.ToBoolean(dataTable.Rows[0]["handle_logic_blocks"]),
                HandleMutators = Convert.ToBoolean(dataTable.Rows[0]["handle_mutators"]),
                LoginRequired = Convert.ToBoolean(dataTable.Rows[0]["login_required"]),
                LoginUserType = dataTable.Rows[0].Field<string>("login_user_type"),
                LoginSessionPrefix = dataTable.Rows[0].Field<string>("login_session_prefix"),
                LoginRole = dataTable.Rows[0].Field<string>("login_role"),
                Ordering = dataTable.Rows[0].Field<int>("ordering"),
                InsertMode = dataTable.Rows[0].Field<ResourceInsertModes>("insert_mode"),
                LoadAlways = Convert.ToBoolean(dataTable.Rows[0]["load_always"]),
                UrlRegex = dataTable.Rows[0].Field<string>("url_regex"),
                ExternalFiles = dataTable.Rows[0].Field<string>("external_files")?.Split(new [] {';', ',' }, StringSplitOptions.RemoveEmptyEntries)?.ToList() ?? new List<string>(),
                GroupingCreateObjectInsteadOfArray = Convert.ToBoolean(dataTable.Rows[0]["grouping_create_object_instead_of_array"]),
                GroupingPrefix = dataTable.Rows[0].Field<string>("grouping_prefix"),
                GroupingKey = dataTable.Rows[0].Field<string>("grouping_key"),
                GroupingKeyColumnName = dataTable.Rows[0].Field<string>("grouping_key_column_name"),
                GroupingValueColumnName = dataTable.Rows[0].Field<string>("grouping_value_column_name"),
                IsScssIncludeTemplate = Convert.ToBoolean(dataTable.Rows[0]["is_scss_include_template"]),
                UseInWiserHtmlEditors = Convert.ToBoolean(dataTable.Rows[0]["use_in_wiser_html_editors"]),
                LinkedTemplates = new LinkedTemplatesModel
                {
                    RawLinkList = dataTable.Rows[0].Field<string>("linked_templates")
                },
                PreLoadQuery = dataTable.Rows[0].Field<string>("pre_load_query")
            };

            return templateData;
        }

        /// <inheritdoc />
        public async Task<Dictionary<int, int>> GetPublishedEnvironmentsAsync(int templateId)
        {
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("templateId", templateId);
            var versionList = new Dictionary<int, int>();

            var dataTable = await clientDatabaseConnection.GetAsync($"SELECT version, published_environment FROM {WiserTableNames.WiserTemplate} WHERE template_id = ?templateId AND removed = 0");

            foreach (DataRow row in dataTable.Rows)
            {
                versionList.Add(row.Field<int>("version"), row.Field<SByte>("published_environment"));
            }

            return versionList;
        }

        /// <inheritdoc />
        public async Task<int> UpdatePublishedEnvironmentAsync(int templateId, Dictionary<int, int> publishModel, PublishLogModel publishLog, string username)
        {
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("templateId", templateId);

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

            clientDatabaseConnection.AddParameter("oldlive", publishLog.OldLive);
            clientDatabaseConnection.AddParameter("oldaccept", publishLog.OldAccept);
            clientDatabaseConnection.AddParameter("oldtest", publishLog.OldTest);
            clientDatabaseConnection.AddParameter("newlive", publishLog.NewLive);
            clientDatabaseConnection.AddParameter("newaccept", publishLog.NewAccept);
            clientDatabaseConnection.AddParameter("newtest", publishLog.NewTest);
            clientDatabaseConnection.AddParameter("now", DateTime.Now);
            clientDatabaseConnection.AddParameter("username", username);

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

            return await clientDatabaseConnection.ExecuteAsync(query + ";" + logQuery);
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
                                                                        WHERE template.template_type IN (2, 3, 4)
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
                wdc.changed_on,
                wdc.changed_by,
                wdc.`title`
                FROM {WiserTableNames.WiserTemplateDynamicContent} tdclink 
                LEFT JOIN {WiserTableNames.WiserDynamicContent} wdc ON wdc.content_id = tdclink.content_id 
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
                resultDao.ChangedOn = row.Field<DateTime>("changed_on");
                resultDao.ChangedBy = row.Field<string>("changed_by");
                resultDao.Title = row.Field<string>("title");

                resultList.Add(resultDao);
            }

            return resultList;
        }

        /// <inheritdoc />
        public async Task<int> SaveAsync(TemplateSettingsModel templateSettings, string templateLinks, string username)
        {
            var ordering = await GetOrderingAsync(templateSettings.TemplateId);
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("templateId", templateSettings.TemplateId);
            clientDatabaseConnection.AddParameter("parentId", templateSettings.ParentId);
            clientDatabaseConnection.AddParameter("name", templateSettings.Name);
            clientDatabaseConnection.AddParameter("editorValue", templateSettings.EditorValue);
            clientDatabaseConnection.AddParameter("minifiedValue", templateSettings.MinifiedValue);
            clientDatabaseConnection.AddParameter("type", templateSettings.Type);
            clientDatabaseConnection.AddParameter("useCache", (int)templateSettings.UseCache);
            clientDatabaseConnection.AddParameter("cacheMinutes", templateSettings.CacheMinutes);
            clientDatabaseConnection.AddParameter("cacheLocation", templateSettings.CacheLocation);
            clientDatabaseConnection.AddParameter("handleRequests", templateSettings.HandleRequests);
            clientDatabaseConnection.AddParameter("handleSession", templateSettings.HandleSession);
            clientDatabaseConnection.AddParameter("handleObjects", templateSettings.HandleObjects);
            clientDatabaseConnection.AddParameter("handleStandards", templateSettings.HandleStandards);
            clientDatabaseConnection.AddParameter("handleTranslations", templateSettings.HandleTranslations);
            clientDatabaseConnection.AddParameter("handleDynamicContent", templateSettings.HandleDynamicContent);
            clientDatabaseConnection.AddParameter("handleLogicBlocks", templateSettings.HandleLogicBlocks);
            clientDatabaseConnection.AddParameter("handleMutators", templateSettings.HandleMutators);
            clientDatabaseConnection.AddParameter("loginRequired", templateSettings.LoginRequired);
            clientDatabaseConnection.AddParameter("loginUserType", templateSettings.LoginUserType);
            clientDatabaseConnection.AddParameter("loginSessionPrefix", templateSettings.LoginSessionPrefix);
            clientDatabaseConnection.AddParameter("loginRole", templateSettings.LoginRole);
            clientDatabaseConnection.AddParameter("now", DateTime.Now);
            clientDatabaseConnection.AddParameter("username", username);
            clientDatabaseConnection.AddParameter("ordering", ordering);
            clientDatabaseConnection.AddParameter("insertMode", (int)templateSettings.InsertMode);
            clientDatabaseConnection.AddParameter("loadAlways", templateSettings.LoadAlways);
            clientDatabaseConnection.AddParameter("urlRegex", templateSettings.UrlRegex);
            clientDatabaseConnection.AddParameter("externalFiles", String.Join(";", templateSettings.ExternalFiles));
            clientDatabaseConnection.AddParameter("groupingCreateObjectInsteadOfArray", templateSettings.GroupingCreateObjectInsteadOfArray);
            clientDatabaseConnection.AddParameter("groupingPrefix", templateSettings.GroupingPrefix);
            clientDatabaseConnection.AddParameter("groupingKey", templateSettings.GroupingKey);
            clientDatabaseConnection.AddParameter("groupingKeyColumnName", templateSettings.GroupingKeyColumnName);
            clientDatabaseConnection.AddParameter("groupingValueColumnName", templateSettings.GroupingValueColumnName);
            clientDatabaseConnection.AddParameter("isScssIncludeTemplate", templateSettings.IsScssIncludeTemplate);
            clientDatabaseConnection.AddParameter("useInWiserHtmlEditors", templateSettings.UseInWiserHtmlEditors);
            clientDatabaseConnection.AddParameter("templateLinks", templateLinks);
            clientDatabaseConnection.AddParameter("preLoadQuery", templateSettings.PreLoadQuery);

            return await clientDatabaseConnection.ExecuteAsync($@"
                SET @VersionNumber = (SELECT MAX(version)+1 FROM {WiserTableNames.WiserTemplate} WHERE template_id = ?templateId GROUP BY template_id);
                INSERT INTO {WiserTableNames.WiserTemplate} (
                    template_name, 
                    template_data, 
                    template_data_minified,
                    template_type, 
                    `version`, 
                    template_id, 
                    parent_id,
                    changed_on, 
                    changed_by, 
                    use_cache,
                    cache_minutes, 
                    cache_location,
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
                    linked_templates,
                    ordering,
                    insert_mode,
                    load_always,
                    url_regex,
                    external_files,
                    grouping_create_object_instead_of_array,
                    grouping_prefix,
                    grouping_key,
                    grouping_key_column_name,
                    grouping_value_column_name,
                    is_scss_include_template,
                    use_in_wiser_html_editors,
                    pre_load_query
                ) 
                VALUES (
                    ?name,
                    ?editorValue,
                    ?minifiedValue,
                    ?type,
                    @VersionNumber,
                    ?templateId,
                    ?parentId,
                    ?now,
                    ?username,
                    ?useCache,
                    ?cacheMinutes,
                    ?cacheLocation,
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
                    ?templateLinks,
                    ?ordering,
                    ?insertMode,
                    ?loadAlways,
                    ?urlRegex,
                    ?externalFiles,
                    ?groupingCreateObjectInsteadOfArray,
                    ?groupingPrefix,
                    ?groupingKey,
                    ?groupingKeyColumnName,
                    ?groupingValueColumnName,
                    ?isScssIncludeTemplate,
                    ?useInWiserHtmlEditors,
                    ?preLoadQuery
                )");
        }

        /// <inheritdoc />
        public async Task<List<TemplateTreeViewDao>> GetTreeViewSectionAsync(int parentId)
        {
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("parentId", parentId);

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
                        AND template.removed = 0
                        AND otherVersion.id IS NULL
                        GROUP BY template.template_id
                        ORDER BY template.ordering ASC";

            var dataTable = await clientDatabaseConnection.GetAsync(query);

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
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("needle", searchSettings.Needle);
            var dataTable = await clientDatabaseConnection.GetAsync(BuildSearchQuery(searchSettings));

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
            var ordering = await GetHighestOrderNumberOfChildrenAsync(parent) + 1;
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("name", name);
            clientDatabaseConnection.AddParameter("parent", parent);
            clientDatabaseConnection.AddParameter("type", type);
            clientDatabaseConnection.AddParameter("now", DateTime.Now);
            clientDatabaseConnection.AddParameter("username", username);
            clientDatabaseConnection.AddParameter("ordering", ordering);

            var dataTable = await clientDatabaseConnection.GetAsync(@$"SET @id = (SELECT MAX(template_id)+1 FROM {WiserTableNames.WiserTemplate});
                                                            INSERT INTO {WiserTableNames.WiserTemplate} (parent_id, template_name, template_type, version, template_id, changed_on, changed_by, published_environment, ordering)
                                                            VALUES (?parent, ?name, ?type, 1, @id, ?now, ?username, 1, ?ordering);
                                                            SELECT @id;");

            return Convert.ToInt32(dataTable.Rows[0]["@id"]);
        }

        /// <inheritdoc />
        public async Task FixTreeViewOrderingAsync(int parentId)
        {
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("parentId", parentId);
            await clientDatabaseConnection.ExecuteAsync($@"SET @ordering = 0;
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
                                                SET template.ordering = ordering.newOrdering");
        }

        /// <inheritdoc />
        public async Task<TemplateSettingsModel> GetParentAsync(int templateId)
        {
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("id", templateId);
            var dataTable = await clientDatabaseConnection.GetAsync($@"SELECT parent.template_id, parent.template_name
                                                            FROM {WiserTableNames.WiserTemplate} AS template
                                                            LEFT JOIN {WiserTableNames.WiserTemplate} AS otherVersion ON otherVersion.template_id = template.template_id AND otherVersion.version > template.version
                                                            JOIN {WiserTableNames.WiserTemplate} AS parent ON parent.template_id = template.parent_id AND parent.removed = 0
                                                            WHERE template.template_id = ?id
                                                            AND template.removed = 0
                                                            AND otherVersion.id IS NULL
                                                            ORDER BY parent.version DESC
                                                            LIMIT 1");

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
            var dataTable = await clientDatabaseConnection.GetAsync($@"SELECT ordering
                                                            FROM {WiserTableNames.WiserTemplate}
                                                            WHERE template_id = ?id
                                                            AND removed = 0
                                                            ORDER BY version DESC
                                                            LIMIT 1");
            return dataTable.Rows.Count == 0 ? 0 : dataTable.Rows[0].Field<int>("ordering");
        }

        /// <inheritdoc />
        public async Task<int> GetHighestOrderNumberOfChildrenAsync(int templateId)
        {
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("id", templateId);
            var dataTable = await clientDatabaseConnection.GetAsync($@"SELECT IFNULL(MAX(template.ordering), 0) AS ordering
                                                                    FROM {WiserTableNames.WiserTemplate} AS template
                                                                    LEFT JOIN {WiserTableNames.WiserTemplate} AS otherVersion ON otherVersion.template_id = template.template_id AND otherVersion.version > template.version
                                                                    WHERE template.parent_id = ?id
                                                                    AND template.removed = 0
                                                                    AND otherVersion.id IS NULL");
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
            // Some customers have multiple websites in the same Wiser instance and can therefor have multiple SCSS root directories.
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
            var dataTable = await clientDatabaseConnection.GetAsync($@"SELECT template.template_data
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
                                                                        WHERE template.template_type = {(int)TemplateTypes.Scss}
                                                                        AND template.is_scss_include_template = 1
                                                                        AND template.removed = 0
                                                                        AND otherVersion.id IS NULL
                                                                        AND template.template_data IS NOT NULL
                                                                        AND (?rootId = 0 OR ?rootId IN (parent8.template_id, parent7.template_id, parent6.template_id, parent5.template_id, parent4.template_id, parent3.template_id, parent2.template_id, parent1.template_id))
                                                                        ORDER BY parent8.ordering, parent7.ordering, parent6.ordering, parent5.ordering, parent4.ordering, parent3.ordering, parent2.ordering, parent1.ordering, template.ordering");

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
            // Some customers have multiple websites in the same Wiser instance and can therefor have multiple SCSS root directories.
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

            var result = new List<TemplateSettingsModel>();

            clientDatabaseConnection.AddParameter("rootId", scssRootId);
            var dataTable = await clientDatabaseConnection.GetAsync($@"SELECT
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
                                                                            template.cache_location, 
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
                                                                            template.linked_templates, 
                                                                            template.ordering,
                                                                            template.insert_mode,
                                                                            template.load_always,
                                                                            template.url_regex,
                                                                            template.external_files,
                                                                            template.grouping_create_object_instead_of_array,
                                                                            template.grouping_prefix,
                                                                            template.grouping_key,
                                                                            template.grouping_key_column_name,
                                                                            template.grouping_value_column_name,
                                                                            template.is_scss_include_template,
                                                                            template.use_in_wiser_html_editors,
                                                                            template.pre_load_query
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
                                                                        WHERE template.template_type = {(int)TemplateTypes.Scss}
                                                                        AND template.is_scss_include_template = 0
                                                                        AND template.removed = 0
                                                                        AND otherVersion.id IS NULL
                                                                        AND template.template_data IS NOT NULL
                                                                        AND (?rootId = 0 OR ?rootId IN (parent8.template_id, parent7.template_id, parent6.template_id, parent5.template_id, parent4.template_id, parent3.template_id, parent2.template_id, parent1.template_id))
                                                                        ORDER BY parent8.ordering, parent7.ordering, parent6.ordering, parent5.ordering, parent4.ordering, parent3.ordering, parent2.ordering, parent1.ordering, template.ordering");

            foreach (DataRow dataRow in dataTable.Rows)
            {
                result.Add(new TemplateSettingsModel
                {
                    TemplateId = dataRow.Field<int>("template_id"),
                    ParentId = dataRow.Field<int?>("parent_id"),
                    Type = dataRow.Field<TemplateTypes>("template_type"),
                    Name = dataRow.Field<string>("template_name"),
                    EditorValue = dataRow.Field<string>("template_data"),
                    Version = dataRow.Field<int>("version"),
                    ChangedOn = dataRow.Field<DateTime>("changed_on"),
                    ChangedBy = dataRow.Field<string>("changed_by"),
                    UseCache = (TemplateCachingModes)dataRow.Field<int>("use_cache"),
                    CacheMinutes = dataRow.Field<int>("cache_minutes"),
                    CacheLocation = (TemplateCachingLocations)dataRow.Field<int>("cache_location"),
                    HandleRequests = Convert.ToBoolean(dataRow["handle_request"]),
                    HandleSession = Convert.ToBoolean(dataRow["handle_session"]),
                    HandleStandards = Convert.ToBoolean(dataRow["handle_standards"]),
                    HandleObjects = Convert.ToBoolean(dataRow["handle_objects"]),
                    HandleTranslations = Convert.ToBoolean(dataRow["handle_translations"]),
                    HandleDynamicContent = Convert.ToBoolean(dataRow["handle_dynamic_content"]),
                    HandleLogicBlocks = Convert.ToBoolean(dataRow["handle_logic_blocks"]),
                    HandleMutators = Convert.ToBoolean(dataRow["handle_mutators"]),
                    LoginRequired = Convert.ToBoolean(dataRow["login_required"]),
                    LoginUserType = dataRow.Field<string>("login_user_type"),
                    LoginSessionPrefix = dataRow.Field<string>("login_session_prefix"),
                    LoginRole = dataRow.Field<string>("login_role"),
                    Ordering = dataRow.Field<int>("ordering"),
                    InsertMode = dataRow.Field<ResourceInsertModes>("insert_mode"),
                    LoadAlways = Convert.ToBoolean(dataRow["load_always"]),
                    UrlRegex = dataRow.Field<string>("url_regex"),
                    ExternalFiles = dataRow.Field<string>("external_files")?.Split(new [] {';', ',' }, StringSplitOptions.RemoveEmptyEntries)?.ToList() ?? new List<string>(),
                    GroupingCreateObjectInsteadOfArray = Convert.ToBoolean(dataRow["grouping_create_object_instead_of_array"]),
                    GroupingPrefix = dataRow.Field<string>("grouping_prefix"),
                    GroupingKey = dataRow.Field<string>("grouping_key"),
                    GroupingKeyColumnName = dataRow.Field<string>("grouping_key_column_name"),
                    GroupingValueColumnName = dataRow.Field<string>("grouping_value_column_name"),
                    IsScssIncludeTemplate = Convert.ToBoolean(dataRow["is_scss_include_template"]),
                    UseInWiserHtmlEditors = Convert.ToBoolean(dataRow["use_in_wiser_html_editors"]),
                    LinkedTemplates = new LinkedTemplatesModel
                    {
                        RawLinkList = dataRow.Field<string>("linked_templates")
                    },
                    PreLoadQuery = dataRow.Field<string>("pre_load_query")
                });
            }

            return result;
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

            // Add a new row/version of this template where all settings/data are empty and removed is set to 1.
            clientDatabaseConnection.AddParameter("parentId", dataTable.Rows[0].Field<int>("parent_id"));
            clientDatabaseConnection.AddParameter("name", dataTable.Rows[0].Field<string>("template_name"));
            clientDatabaseConnection.AddParameter("type", dataTable.Rows[0].Field<int>("template_type"));
            clientDatabaseConnection.AddParameter("ordering", dataTable.Rows[0].Field<int>("ordering"));
            clientDatabaseConnection.AddParameter("version", dataTable.Rows[0].Field<int>("version") + 1);

            query = $@"INSERT INTO {WiserTableNames.WiserTemplate} (template_id, parent_id, template_name, template_type, ordering, version, removed, changed_on, changed_by)
                    VALUES (?templateId, ?parentId, ?name, ?type, ?ordering, ?version, 1, ?now, ?username)";
            await clientDatabaseConnection.ExecuteAsync(query);

            if (!alsoDeleteChildren)
            {
                return true;
            }

            // Delete all children of the template by also adding new versions with removed = 1 for them.
            query = $@"INSERT INTO {WiserTableNames.WiserTemplate} (template_id, parent_id, template_name, template_type, ordering, version, removed, changed_on, changed_by)
                    SELECT 
                        template.template_id,
	                    template.parent_id,
	                    template.template_name,
	                    template.template_type,
	                    template.ordering,
	                    template.version + 1,
                        1,
                        ?now,
                        ?username
                    FROM wiser_template AS template
                    LEFT JOIN wiser_template AS otherVersion ON otherVersion.template_id = template.template_id AND otherVersion.version > template.version
                    WHERE template.parent_id = ?templateId
                    AND template.removed = 0
                    AND otherVersion.id IS NULL";
            await clientDatabaseConnection.ExecuteAsync(query);

            return true;
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
                        clientDatabaseConnection.AddParameter("environment", Environments.Test);
                        searchQuery.Append("t.published_environment & ?environment");
                        break;
                    case Environments.Acceptance:
                        clientDatabaseConnection.AddParameter("environment", Environments.Acceptance);
                        searchQuery.Append("t.published_environment & ?environment");
                        break;
                    case Environments.Live:
                        clientDatabaseConnection.AddParameter("environment", Environments.Live);
                        searchQuery.Append("t.published_environment & ?environment");
                        break;
                    default:
                        searchQuery.Append($"t.version = (SELECT MAX(tt.version) FROM {WiserTableNames.WiserTemplate} tt WHERE tt.template_id = t.template_id)");
                        break;
                }

                searchQuery.Append("AND t.removed = 0 AND (");

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
                        clientDatabaseConnection.AddParameter("environment", Environments.Test);
                        searchQuery.Append("wdc.published_environment & ?environment");
                        break;
                    case Environments.Acceptance:
                        clientDatabaseConnection.AddParameter("environment", Environments.Acceptance);
                        searchQuery.Append("wdc.published_environment & ?environment");
                        break;
                    case Environments.Live:
                        clientDatabaseConnection.AddParameter("environment", Environments.Live);
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

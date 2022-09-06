using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Api.Modules.Templates.Interfaces.DataLayer;
using Api.Modules.Templates.Models.DynamicContent;
using Api.Modules.Templates.Models.Template;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using Newtonsoft.Json;

namespace Api.Modules.Templates.Services.DataLayer
{
    /// <inheritdoc cref="IDynamicContentDataService" />
    public class DynamicContentDataService : IDynamicContentDataService, IScopedService
    {
        private readonly IDatabaseConnection connection;

        /// <summary>
        /// Creates a new instance of <see cref="DynamicContentDataService"/>.
        /// </summary>
        public DynamicContentDataService(IDatabaseConnection connection)
        {
            this.connection = connection;
        }

        /// <inheritdoc />
        public async Task<KeyValuePair<string, Dictionary<string, object>>> GetVersionDataAsync(int version, int contentId)
        {
            connection.ClearParameters();
            connection.AddParameter("version", version);
            connection.AddParameter("contentId", contentId);
            var dataTable = await connection.GetAsync($"SELECT wdc.settings, wdc.`title` FROM {WiserTableNames.WiserDynamicContent} wdc WHERE wdc.content_id = ?contentId AND wdc.version = ?version AND removed = 0 ORDER BY wdc.version DESC LIMIT 1");

            var settings = dataTable.Rows[0].Field<string>("settings") ?? "{}";
            var title = dataTable.Rows[0].Field<string>("title");

            return new KeyValuePair<string, Dictionary<string, object>>(title, JsonConvert.DeserializeObject<Dictionary<string, object>>(settings));
        }
        
        /// <inheritdoc />
        public async Task<KeyValuePair<string, Dictionary<string, object>>> GetComponentDataAsync(int contentId)
        {
            var rawData = await GetDatabaseData(contentId);

            return new KeyValuePair<string, Dictionary<string, object>>(rawData.Key, JsonConvert.DeserializeObject<Dictionary<string, object>>(rawData.Value));
        }
        
        /// <inheritdoc />
        public async Task<int> SaveSettingsStringAsync(int contentId, string component, string componentMode, string title, Dictionary<string, object> settings, string username)
        {
            if (contentId <= 0)
            {
                var dataTable = await connection.GetAsync($"SELECT IFNULL(MAX(content_id), 0) AS max_content_id FROM {WiserTableNames.WiserDynamicContent}");
                contentId = (dataTable.Rows.Count == 0 ? 0 : Convert.ToInt32(dataTable.Rows[0]["max_content_id"])) + 1;
            }

            connection.ClearParameters();
            connection.AddParameter("settings", JsonConvert.SerializeObject(settings));
            connection.AddParameter("component", component);
            connection.AddParameter("componentMode", componentMode);
            connection.AddParameter("contentId", contentId);
            connection.AddParameter("title", title);
            connection.AddParameter("now", DateTime.Now);
            connection.AddParameter("username", username);

            await connection.InsertRecordAsync($@"
            SET @VersionNumber = IFNULL((SELECT MAX(version)+1 FROM {WiserTableNames.WiserDynamicContent} WHERE content_id = ?contentId GROUP BY content_id), 1);

            INSERT INTO {WiserTableNames.WiserDynamicContent} (`version`, `changed_on`, `changed_by`, `settings`, `content_id`, `component`, `component_mode`, `title`) 
            VALUES (@VersionNumber, ?now, ?username, ?settings, ?contentId, ?component, ?componentMode, ?title)");

            return contentId;
        }
        
        /// <inheritdoc />
        public async Task<List<string>> GetComponentAndModeFromContentIdAsync(int contentId)
        {
            connection.ClearParameters();
            connection.AddParameter("id", contentId);
            var dataTable = await connection.GetAsync($"SELECT wdc.component, wdc.component_mode FROM {WiserTableNames.WiserDynamicContent} wdc WHERE id = ?id LIMIT 1");

            var results = new List<string>
            {
                dataTable.Rows[0].Field<string>("component"),
                dataTable.Rows[0].Field<string>("component_mode")
            };

            return results;
        }

        /// <inheritdoc />
        public async Task<DynamicContentOverviewModel> GetMetaDataAsync(int contentId)
        {
            connection.ClearParameters();
            connection.AddParameter("contentId", contentId);
            var dataTable = await connection.GetAsync($@"SELECT
                                                            component.id,
                                                            component.component,
                                                            component.component_mode,
                                                            component.version,
                                                            component.title,
                                                            component.changed_on,
                                                            component.changed_by
                                                        FROM {WiserTableNames.WiserDynamicContent} AS component
                                                        WHERE component.content_id = ?contentId
                                                        AND component.removed = 0
                                                        ORDER BY component.version DESC
                                                        LIMIT 1");

            return dataTable.Rows.Count == 0 ? null : new DynamicContentOverviewModel
            {
                Id = dataTable.Rows[0].Field<int>("id"),
                Component = dataTable.Rows[0].Field<string>("component"),
                ComponentMode = dataTable.Rows[0].Field<string>("component_mode"),
                LatestVersion = dataTable.Rows[0].Field<int>("version"),
                Title = dataTable.Rows[0].Field<string>("title"),
                ChangedOn = dataTable.Rows[0].Field<DateTime>("changed_on"),
                ChangedBy = dataTable.Rows[0].Field<string>("changed_by")
            };
        }
        
        /// <inheritdoc />
        public async Task AddLinkToTemplateAsync(int contentId, int templateId, string username)
        {
            connection.ClearParameters();
            connection.AddParameter("contentId", contentId);
            connection.AddParameter("templateId", templateId);
            connection.AddParameter("now", DateTime.Now);
            connection.AddParameter("username", username);
            await connection.ExecuteAsync($@"INSERT IGNORE INTO {WiserTableNames.WiserTemplateDynamicContent} (content_id, destination_template_id, added_on, added_by)
                                                VALUES (?contentId, ?templateId, ?now, ?username)");
        }

        /// <inheritdoc />
        public async Task<Dictionary<int, int>> GetPublishedEnvironmentsAsync(int contentId)
        {
            connection.ClearParameters();
            connection.AddParameter("contentId", contentId);
            var versionList = new Dictionary<int, int>();

            var dataTable = await connection.GetAsync($"SELECT version, published_environment FROM {WiserTableNames.WiserDynamicContent} WHERE content_id = ?contentId AND removed = 0");

            foreach (DataRow row in dataTable.Rows)
            {
                versionList.Add(row.Field<int>("version"), row.Field<SByte>("published_environment"));
            }

            return versionList;
        }

        /// <inheritdoc />
        public async Task<int> UpdatePublishedEnvironmentAsync(int contentId, Dictionary<int, int> publishModel, PublishLogModel publishLog, string username)
        {
            connection.ClearParameters();
            connection.AddParameter("contentId", contentId);

            var baseQueryPart = $@"UPDATE {WiserTableNames.WiserDynamicContent} SET published_environment = CASE version";

            var dynamicQueryPart = "";
            var dynamicWherePart = " AND version IN (";
            foreach (var versionChange in publishModel)
            {
                dynamicQueryPart += $" WHEN {versionChange.Key} THEN published_environment + {versionChange.Value}";
                dynamicWherePart += $"{versionChange.Key},";
            }
            dynamicWherePart = $"{dynamicWherePart[..^1]})";
            var endQueryPart = @" END
                WHERE content_id = ?contentId";

            var query = baseQueryPart + dynamicQueryPart + endQueryPart + dynamicWherePart;

            connection.AddParameter("oldlive", publishLog.OldLive);
            connection.AddParameter("oldaccept", publishLog.OldAccept);
            connection.AddParameter("oldtest", publishLog.OldTest);
            connection.AddParameter("newlive", publishLog.NewLive);
            connection.AddParameter("newaccept", publishLog.NewAccept);
            connection.AddParameter("newtest", publishLog.NewTest);
            connection.AddParameter("now", DateTime.Now);
            connection.AddParameter("username", username);

            var logQuery = $@"INSERT INTO {WiserTableNames.WiserDynamicContentPublishLog} (content_id, old_live, old_accept, old_test, new_live, new_accept, new_test, changed_on, changed_by) 
            VALUES(
                ?contentId,
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
        public async Task DuplicateAsync(int contentId, int newTemplateId, string username)
        {
            var dataTable = await connection.GetAsync($"SELECT IFNULL(MAX(content_id), 0) AS max_content_id FROM {WiserTableNames.WiserDynamicContent}");
            var newContentId = (dataTable.Rows.Count == 0 ? 0 : Convert.ToInt32(dataTable.Rows[0]["max_content_id"])) + 1;
            
            var query = $@"INSERT INTO {WiserTableNames.WiserDynamicContent}
(
    content_id,
    settings,
    component,
    component_mode,
    version,
    title,
    changed_on,
    changed_by,
    published_environment
)
SELECT 
    ?newContentId,
    content.settings,
    content.component,
    content.component_mode,
    1,
    CONCAT(content.title, ' - Duplicate'),
    ?now,
    ?username,
    0
FROM {WiserTableNames.WiserDynamicContent} AS content
LEFT JOIN {WiserTableNames.WiserDynamicContent} AS otherVersion ON otherVersion.content_id = content.content_id AND otherVersion.version > content.version
WHERE content.content_id = ?contentId
AND content.removed = 0
AND otherVersion.id IS NULL;

INSERT INTO {WiserTableNames.WiserTemplateDynamicContent} (content_id, destination_template_id, added_on, added_by)
VALUES (?newContentId, ?templateId, ?now, ?username);";
            
            connection.AddParameter("contentId", contentId);
            connection.AddParameter("newContentId", newContentId);
            connection.AddParameter("now", DateTime.Now);
            connection.AddParameter("username", username);
            connection.AddParameter("templateId", newTemplateId);
            await connection.ExecuteAsync(query);
        }

        private async Task<KeyValuePair<string, string>> GetDatabaseData(int contentId)
        {
            connection.ClearParameters();
            connection.AddParameter("contentId", contentId);
            var dataTable = await connection.GetAsync($@"SELECT settings, title, removed FROM {WiserTableNames.WiserDynamicContent} WHERE content_id = ?contentId ORDER BY version DESC LIMIT 1");
            if (Convert.ToBoolean(dataTable.Rows[0]["removed"]))
            {
                return new KeyValuePair<string, string>(null, null);
            }

            var settings = dataTable.Rows[0].Field<string>("settings");
            var title = dataTable.Rows[0].Field<string>("title");

            return new KeyValuePair<string, string>(title, settings);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Api.Core.Models;
using Api.Modules.Templates.Interfaces.DataLayer;
using Api.Modules.Templates.Models.DynamicContent;
using Api.Modules.Templates.Models.Template;
using Api.Modules.Tenants.Helpers;
using Api.Modules.Tenants.Models;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Databases.Services;
using GeeksCoreLibrary.Modules.Templates.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Constants = Api.Modules.Templates.Models.Other.Constants;

namespace Api.Modules.Templates.Services.DataLayer
{
    /// <inheritdoc cref="IDynamicContentDataService" />
    public class DynamicContentDataService : IDynamicContentDataService, IScopedService
    {
        private readonly IDatabaseConnection clientDatabaseConnection;
        private readonly IDatabaseHelpersService databaseHelpersService;
        private readonly IServiceProvider serviceProvider;
        private readonly ApiSettings apiSettings;

        /// <summary>
        /// Creates a new instance of <see cref="DynamicContentDataService"/>.
        /// </summary>
        public DynamicContentDataService(IDatabaseConnection clientDatabaseConnection, IDatabaseHelpersService databaseHelpersService, IServiceProvider serviceProvider, IOptions<ApiSettings> apiSettings)
        {
            this.clientDatabaseConnection = clientDatabaseConnection;
            this.databaseHelpersService = databaseHelpersService;
            this.serviceProvider = serviceProvider;
            this.apiSettings = apiSettings.Value;
        }

        /// <inheritdoc />
        public async Task<List<DynamicContentOverviewModel>> GetLinkableDynamicContentAsync(int templateId)
        {
            clientDatabaseConnection.AddParameter("templateId", templateId);
            var query = $@"SELECT
	component.content_id,
	component.title,
	component.version,
    template.template_id,
	CONCAT_WS(' >> ', parent5.template_name, parent4.template_name, parent3.template_name, parent2.template_name, parent1.template_name, IFNULL(template.template_name, 'Geen')) AS templatePath
FROM {WiserTableNames.WiserDynamicContent} AS component
LEFT JOIN {WiserTableNames.WiserDynamicContent} AS otherVersion ON otherVersion.content_id = component.content_id AND otherVersion.version > component.version
LEFT JOIN {WiserTableNames.WiserTemplateDynamicContent} AS linkToTemplate ON linkToTemplate.content_id = component.content_id
LEFT JOIN {WiserTableNames.WiserTemplate} AS template ON template.template_id = linkToTemplate.destination_template_id AND template.template_type = {(int)TemplateTypes.Html} AND template.version = (SELECT MAX(version) FROM {WiserTableNames.WiserTemplate} WHERE template_id = linkToTemplate.destination_template_id)
LEFT JOIN {WiserTableNames.WiserTemplate} AS parent1 ON parent1.template_id = template.parent_id AND parent1.version = (SELECT MAX(version) FROM {WiserTableNames.WiserTemplate} WHERE template_id = template.parent_id)
LEFT JOIN {WiserTableNames.WiserTemplate} AS parent2 ON parent2.template_id = parent1.parent_id AND parent2.version = (SELECT MAX(version) FROM {WiserTableNames.WiserTemplate} WHERE template_id = parent1.parent_id)
LEFT JOIN {WiserTableNames.WiserTemplate} AS parent3 ON parent3.template_id = parent2.parent_id AND parent3.version = (SELECT MAX(version) FROM {WiserTableNames.WiserTemplate} WHERE template_id = parent2.parent_id)
LEFT JOIN {WiserTableNames.WiserTemplate} AS parent4 ON parent4.template_id = parent3.parent_id AND parent4.version = (SELECT MAX(version) FROM {WiserTableNames.WiserTemplate} WHERE template_id = parent3.parent_id)
LEFT JOIN {WiserTableNames.WiserTemplate} AS parent5 ON parent5.template_id = parent4.parent_id AND parent5.version = (SELECT MAX(version) FROM {WiserTableNames.WiserTemplate} WHERE template_id = parent4.parent_id)
WHERE otherVersion.id IS NULL
AND (linkToTemplate.id IS NULL OR linkToTemplate.destination_template_id <> ?templateId)
ORDER BY templatePath ASC, component.title ASC";

            var dataTable = await clientDatabaseConnection.GetAsync(query);
            var results = dataTable.Rows.Cast<DataRow>().Select(dataRow => new DynamicContentOverviewModel
            {
                Id = dataRow.Field<int>("content_id"),
                Title = dataRow.Field<string>("title"),
                LatestVersion = dataRow.Field<int>("version"),
                TemplateId = dataRow.Field<int?>("template_id"),
                TemplatePath = dataRow.Field<string>("templatePath")
            });

            return results.ToList();
        }

        /// <inheritdoc />
        public async Task<KeyValuePair<string, Dictionary<string, object>>> GetVersionDataAsync(int version, int contentId)
        {
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("version", version);
            clientDatabaseConnection.AddParameter("contentId", contentId);
            var dataTable = await clientDatabaseConnection.GetAsync($"SELECT wdc.settings, wdc.`title` FROM {WiserTableNames.WiserDynamicContent} wdc WHERE wdc.content_id = ?contentId AND wdc.version = ?version AND removed = 0 ORDER BY wdc.version DESC LIMIT 1");

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
        public async Task<(int Id, int Version, Environments Environment, bool Removed)> GetLatestVersionAsync(int contentId)
        {
            // Get the ID and published environment of the latest version of the component.
            var query = $@"SELECT 
    component.id,
    component.version,
    component.published_environment,
    component.removed
FROM {WiserTableNames.WiserDynamicContent} AS component
LEFT JOIN {WiserTableNames.WiserDynamicContent} AS otherVersion ON otherVersion.content_id = component.content_id AND otherVersion.version > component.version
WHERE component.content_id = ?contentId
AND otherVersion.id IS NULL";
            clientDatabaseConnection.AddParameter("contentId", contentId);
            var dataTable = await clientDatabaseConnection.GetAsync(query);
            if (dataTable.Rows.Count == 0)
            {
                throw new Exception($"Template with ID {contentId} not found.");
            }

            var id = Convert.ToInt32(dataTable.Rows[0]["id"]);
            var version = Convert.ToInt32(dataTable.Rows[0]["version"]);
            var publishedEnvironment = (Environments)Convert.ToInt32(dataTable.Rows[0]["published_environment"]);
            var removed = Convert.ToBoolean(dataTable.Rows[0]["removed"]);
            return (id, version, publishedEnvironment, removed);
        }

        /// <inheritdoc />
        public async Task<int> SaveAsync(int contentId, string component, string componentMode, string title, Dictionary<string, object> settings, string username)
        {
            string query;
            if (contentId <= 0)
            {
                var dataTable = await clientDatabaseConnection.GetAsync($"SELECT IFNULL(MAX(content_id), 0) AS max_content_id FROM {WiserTableNames.WiserDynamicContent}");
                contentId = (dataTable.Rows.Count == 0 ? 0 : Convert.ToInt32(dataTable.Rows[0]["max_content_id"])) + 1;
                clientDatabaseConnection.AddParameter("contentId", contentId);

                query = $@"INSERT INTO {WiserTableNames.WiserDynamicContent} (`version`, `changed_on`, `changed_by`, `settings`, `content_id`, `component`, `component_mode`, `title`, `is_dirty`) 
VALUES (1, ?now, ?username, ?settings, ?contentId, ?component, ?componentMode, ?title, TRUE)";
            }
            else
            {
                clientDatabaseConnection.AddParameter("contentId", contentId);
                // Get the ID and published environment of the latest version of the component.
                var latestVersion = await GetLatestVersionAsync(contentId);

                // If the latest version is published to live, create a new version, because we never want to edit the version that is published to live directly.
                if ((latestVersion.Environment & Environments.Live) == Environments.Live)
                {
                    latestVersion.Id = await CreateNewVersionAsync(contentId);
                }

                clientDatabaseConnection.AddParameter("id", latestVersion.Id);
                query = $@"UPDATE {WiserTableNames.WiserDynamicContent} 
SET `changed_on` = ?now, 
    `changed_by` = ?username, 
    `settings` = ?settings, 
    `component` = ?component, 
    `component_mode` = ?componentMode, 
    `title` = ?title,
    `is_dirty` = TRUE
WHERE id = ?id";
            }

            clientDatabaseConnection.AddParameter("settings", JsonConvert.SerializeObject(settings));
            clientDatabaseConnection.AddParameter("component", component);
            clientDatabaseConnection.AddParameter("componentMode", componentMode);
            clientDatabaseConnection.AddParameter("title", title);
            clientDatabaseConnection.AddParameter("now", DateTime.Now);
            clientDatabaseConnection.AddParameter("username", username);

            await clientDatabaseConnection.ExecuteAsync(query);
            return contentId;
        }

        /// <inheritdoc />
        public async Task<int> CreateNewVersionAsync(int contentId)
        {
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("contentId", contentId);
            clientDatabaseConnection.AddParameter("now", DateTime.Now);

            var query = $@"INSERT INTO {WiserTableNames.WiserDynamicContent} (`version`, `changed_on`, `changed_by`, `settings`, `content_id`, `component`, `component_mode`, `title`, `is_dirty`) 
SELECT
    component.version + 1 AS version,
    ?now AS changed_on,
    'Wiser' AS changed_by,
    component.settings,
    component.content_id,
    component.component,
    component.component_mode,
    component.title,
    FALSE AS is_dirty
FROM {WiserTableNames.WiserDynamicContent} AS component
LEFT JOIN {WiserTableNames.WiserDynamicContent} AS otherVersion ON otherVersion.content_id = component.content_id AND otherVersion.version > component.version
WHERE component.content_id = ?contentId
AND otherVersion.id IS NULL";

            var newId = await clientDatabaseConnection.InsertRecordAsync(query);
            return Convert.ToInt32(newId);
        }

        /// <inheritdoc />
        public async Task<List<string>> GetComponentAndModeFromContentIdAsync(int contentId)
        {
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("id", contentId);
            var dataTable = await clientDatabaseConnection.GetAsync($"SELECT wdc.component, wdc.component_mode FROM {WiserTableNames.WiserDynamicContent} wdc WHERE id = ?id LIMIT 1");

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
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("contentId", contentId);
            var dataTable = await clientDatabaseConnection.GetAsync($@"SELECT
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
                Id = contentId,
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
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("contentId", contentId);
            clientDatabaseConnection.AddParameter("templateId", templateId);
            clientDatabaseConnection.AddParameter("now", DateTime.Now);
            clientDatabaseConnection.AddParameter("username", username);
            await clientDatabaseConnection.ExecuteAsync($@"INSERT IGNORE INTO {WiserTableNames.WiserTemplateDynamicContent} (content_id, destination_template_id, added_on, added_by)
                                                VALUES (?contentId, ?templateId, ?now, ?username)");
        }

        /// <inheritdoc />
        public async Task<Dictionary<int, int>> GetPublishedEnvironmentsAsync(int contentId, TenantModel branch = null)
        {
            var query = $"SELECT version, published_environment FROM {WiserTableNames.WiserDynamicContent} WHERE content_id = ?contentId AND removed = 0";
            DataTable dataTable;
            if (branch != null)
            {
                // Create new connection for branch database.
                var connectionStringBuilder = TenantHelpers.GetConnectionString(branch.Database, apiSettings.DatabasePasswordEncryptionKey);
                using var scope = serviceProvider.CreateScope();
                var branchDatabaseConnection = scope.ServiceProvider.GetRequiredService<MySqlDatabaseConnection>();
                await branchDatabaseConnection.ChangeConnectionStringsAsync(connectionStringBuilder.ConnectionString);

                branchDatabaseConnection.ClearParameters();
                branchDatabaseConnection.AddParameter("contentId", contentId);
                dataTable = await branchDatabaseConnection.GetAsync(query);
            }
            else
            {
                clientDatabaseConnection.ClearParameters();
                clientDatabaseConnection.AddParameter("contentId", contentId);
                dataTable = await clientDatabaseConnection.GetAsync(query);
            }

            var versionList = new Dictionary<int, int>();
            foreach (DataRow row in dataTable.Rows)
            {
                versionList.Add(row.Field<int>("version"), Convert.ToInt32(row["published_environment"]));
            }

            return versionList;
        }

        /// <inheritdoc />
        public async Task<int> UpdatePublishedEnvironmentAsync(int contentId, int version, Environments environment, PublishLogModel publishLog, string username, TenantModel branch = null)
        {
            using var scope = serviceProvider.CreateScope();
            var connection = clientDatabaseConnection;
            if (branch != null)
            {
                // Create new connection for branch database.
                var connectionStringBuilder = TenantHelpers.GetConnectionString(branch.Database, apiSettings.DatabasePasswordEncryptionKey);
                connection = scope.ServiceProvider.GetRequiredService<MySqlDatabaseConnection>();
                await connection.ChangeConnectionStringsAsync(connectionStringBuilder.ConnectionString);
            }

            switch (environment)
            {
                case Environments.Test:
                    environment |= Environments.Development;
                    break;
                case Environments.Acceptance:
                    environment |= Environments.Test | Environments.Development;
                    break;
                case Environments.Live:
                    environment |= Environments.Acceptance | Environments.Test | Environments.Development;
                    break;
            }

            connection.AddParameter("contentId", contentId);
            connection.AddParameter("version", version);
            connection.AddParameter("environment", (int)environment);

            // Add the bit of the selected environment to the selected version.
            var query = $"""
                         UPDATE {WiserTableNames.WiserDynamicContent}
                         SET published_environment = published_environment | ?environment
                         WHERE content_id = ?contentId
                         AND version = ?version
                         """;
            var affectedRows = await connection.ExecuteAsync(query);

            // Query to remove the selected environment from all other versions, the ~ operator flips all the bits (1s become 0s and 0s become 1s).
            // This way we can safely turn off just the specific bits without having to check to see if the bit is set.
            query = $"""
                     UPDATE {WiserTableNames.WiserDynamicContent}
                     SET published_environment = published_environment & ~?environment
                     WHERE content_id = ?contentId
                     AND version != ?version
                     """;

            affectedRows += await connection.ExecuteAsync(query);

            if (affectedRows == 0)
            {
                return affectedRows;
            }

            connection.AddParameter("oldlive", publishLog.OldLive);
            connection.AddParameter("oldaccept", publishLog.OldAccept);
            connection.AddParameter("oldtest", publishLog.OldTest);
            connection.AddParameter("newlive", publishLog.NewLive);
            connection.AddParameter("newaccept", publishLog.NewAccept);
            connection.AddParameter("newtest", publishLog.NewTest);
            connection.AddParameter("now", DateTime.Now);
            connection.AddParameter("username", username);

            query = $"""
                     INSERT INTO {WiserTableNames.WiserDynamicContentPublishLog}
                     (
                         content_id,
                         old_live,
                         old_accept,
                         old_test,
                         new_live,
                         new_accept,
                         new_test,
                         changed_on,
                         changed_by
                     ) 
                     VALUES
                     (
                         ?contentId,
                         ?oldlive,
                         ?oldaccept,
                         ?oldtest,
                         ?newlive,
                         ?newaccept,
                         ?newtest,
                         ?now,
                         ?username
                     )
                     """;

            return await connection.ExecuteAsync(query);
        }

        /// <inheritdoc />
        public async Task DuplicateAsync(int contentId, int newTemplateId, string username)
        {
            var dataTable = await clientDatabaseConnection.GetAsync($"SELECT IFNULL(MAX(content_id), 0) AS max_content_id FROM {WiserTableNames.WiserDynamicContent}");
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

            clientDatabaseConnection.AddParameter("contentId", contentId);
            clientDatabaseConnection.AddParameter("newContentId", newContentId);
            clientDatabaseConnection.AddParameter("now", DateTime.Now);
            clientDatabaseConnection.AddParameter("username", username);
            clientDatabaseConnection.AddParameter("templateId", newTemplateId);
            await clientDatabaseConnection.ExecuteAsync(query);
        }

        /// <inheritdoc />
        public async Task DeleteAsync(string username, int contentId)
        {
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("contentId", contentId);

            var query = $@"SELECT component, component_mode, title, version, removed
FROM {WiserTableNames.WiserDynamicContent}
WHERE content_id = ?contentId
ORDER BY version DESC
LIMIT 1";

            var dataTable = await clientDatabaseConnection.GetAsync(query);
            if (dataTable.Rows.Count == 0 || Convert.ToBoolean(dataTable.Rows[0]["removed"]))
            {
                return;
            }

            clientDatabaseConnection.AddParameter("component", dataTable.Rows[0].Field<string>("component"));
            clientDatabaseConnection.AddParameter("componentMode", dataTable.Rows[0].Field<string>("component_mode"));
            clientDatabaseConnection.AddParameter("title", dataTable.Rows[0].Field<string>("title"));
            clientDatabaseConnection.AddParameter("version", dataTable.Rows[0].Field<int>("version") + 1);
            clientDatabaseConnection.AddParameter("username", username);
            clientDatabaseConnection.AddParameter("now", DateTime.Now);

            query = $@"INSERT INTO {WiserTableNames.WiserDynamicContent} (content_id, component, component_mode, title, version, changed_on, changed_by, removed, is_dirty)
VALUES(?contentId, ?component, ?componentMode, ?title, ?version, ?now, ?username, TRUE, TRUE)";

            await clientDatabaseConnection.ExecuteAsync(query);
        }

        /// <inheritdoc />
        public async Task DeployToBranchAsync(List<int> dynamicContentIds, TenantModel branch)
        {
            // First get the components that need to be deployed.
            var query = $"""
                         SELECT content.*
                         FROM {WiserTableNames.WiserDynamicContent} AS content
                         LEFT JOIN {WiserTableNames.WiserDynamicContent} AS otherVersion ON otherVersion.content_id = content.content_id AND otherVersion.version > content.version
                         WHERE content.content_id IN ({String.Join(", ", dynamicContentIds)})
                         AND otherVersion.id IS NULL
                         """;
            var templateData = await clientDatabaseConnection.GetAsync(query);

            // Create new connection for branch database.
            var connectionStringBuilder = TenantHelpers.GetConnectionString(branch.Database, apiSettings.DatabasePasswordEncryptionKey);
            using var scope = serviceProvider.CreateScope();
            var branchDatabaseConnection = scope.ServiceProvider.GetRequiredService<MySqlDatabaseConnection>();
            await branchDatabaseConnection.ChangeConnectionStringsAsync(connectionStringBuilder.ConnectionString);

            // Create temporary table for the templates.
            var temporaryTableName = $"temp_dynamic_content_{Guid.NewGuid():N}";
            query = $"CREATE TABLE `{temporaryTableName}` LIKE {WiserTableNames.WiserDynamicContent};";
            await branchDatabaseConnection.ExecuteAsync(query);

            // Insert the templates into the temporary table.
            await branchDatabaseConnection.BulkInsertAsync(templateData, temporaryTableName);

            // Update the templates in wiser_dynamic_content and then drop the temporary table.
            query = $"""
                     UPDATE {WiserTableNames.WiserDynamicContent} AS content
                     JOIN `{temporaryTableName}` AS temp ON temp.content_id = content.content_id AND temp.version = content.version
                     SET content.settings = temp.settings, content.component = temp.component, content.component_mode = temp.component_mode, content.title = temp.title, content.changed_on = temp.changed_on, content.changed_by = temp.changed_by, content.removed = temp.removed, content.is_dirty = temp.is_dirty;

                     INSERT INTO {WiserTableNames.WiserDynamicContent} (
                     	content_id,
                     	settings,
                     	component,
                     	component_mode,
                     	version,
                     	title,
                     	changed_on,
                     	changed_by,
                     	published_environment,
                     	removed,
                     	is_dirty,
                     	added_on,
                     	added_by
                     ) 
                     SELECT 
                     	content_id,
                     	settings,
                     	component,
                     	component_mode,
                     	version,
                     	title,
                     	changed_on,
                     	changed_by,
                     	published_environment,
                     	removed,
                     	is_dirty,
                        added_on,
                        added_by
                     FROM `{temporaryTableName}` AS temp
                     WHERE NOT EXISTS (
                         SELECT 1
                         FROM {WiserTableNames.WiserDynamicContent} AS content
                         WHERE content.content_id = temp.content_id
                         AND content.version = temp.version
                     );

                     DROP TABLE IF EXISTS `{temporaryTableName}`;
                     """;
            await branchDatabaseConnection.ExecuteAsync(query);

            // Also copy all links of dynamic content to template to the branch.
            query = $"""
                     SELECT *
                     FROM {WiserTableNames.WiserTemplateDynamicContent}
                     WHERE content_id IN ({String.Join(", ", dynamicContentIds)});
                     """;
            var templateLinks = await clientDatabaseConnection.GetAsync(query);
            await branchDatabaseConnection.BulkInsertAsync(templateLinks, WiserTableNames.WiserTemplateDynamicContent, useInsertIgnore: true);
        }

        /// <inheritdoc />
        public async Task KeepTablesUpToDateAsync()
        {
            var lastTableUpdates = await databaseHelpersService.GetLastTableUpdatesAsync(clientDatabaseConnection.ConnectedDatabase);

            // Check if the components table needs to be updated.
            if ((lastTableUpdates.TryGetValue(Constants.SetIsDirtyToComponents, out var value) && value >= new DateTime(2023, 7, 4)))
            {
                return;
            }

            var query = $@"UPDATE {WiserTableNames.WiserDynamicContent} AS component
LEFT JOIN {WiserTableNames.WiserDynamicContent} AS otherVersion ON otherVersion.content_id = component.content_id AND otherVersion.version > component.version
LEFT JOIN {WiserTableNames.WiserCommitDynamicContent} AS componentCommit ON componentCommit.dynamic_content_id = component.content_id AND componentCommit.version = component.version
SET component.is_dirty = TRUE
WHERE component.removed = 0
AND component.is_dirty = FALSE
AND otherVersion.id IS NULL
AND componentCommit.id IS NULL";
            await clientDatabaseConnection.ExecuteAsync(query);

            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("tableName", Constants.SetIsDirtyToComponents);
            clientDatabaseConnection.AddParameter("lastUpdate", DateTime.Now);
            var lastUpdateData = await clientDatabaseConnection.GetAsync($"SELECT NULL FROM `{WiserTableNames.WiserTableChanges}` WHERE `name` = ?tableName");
            if (lastUpdateData.Rows.Count == 0)
            {
                await clientDatabaseConnection.ExecuteAsync($"INSERT INTO `{WiserTableNames.WiserTableChanges}` (`name`, last_update) VALUES (?tableName, ?lastUpdate)");
            }
            else
            {
                await clientDatabaseConnection.ExecuteAsync($"UPDATE `{WiserTableNames.WiserTableChanges}` SET last_update = ?lastUpdate WHERE `name` = ?tableName LIMIT 1");
            }
        }

        private async Task<KeyValuePair<string, string>> GetDatabaseData(int contentId)
        {
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("contentId", contentId);
            var dataTable = await clientDatabaseConnection.GetAsync($"SELECT settings, title, removed FROM {WiserTableNames.WiserDynamicContent} WHERE content_id = ?contentId ORDER BY version DESC LIMIT 1");
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
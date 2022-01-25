using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Api.Modules.Templates.Interfaces.DataLayer;
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
        public async Task<KeyValuePair<string, Dictionary<string, object>>> GetVersionData(int version, int contentId)
        {
            connection.ClearParameters();
            connection.AddParameter("version", version);
            connection.AddParameter("contentId", contentId);
            var dataTable = await connection.GetAsync($"SELECT wdc.settings, wdc.`title` FROM {WiserTableNames.WiserDynamicContent} wdc WHERE wdc.content_id = ?contentId AND wdc.version = ?version ORDER BY wdc.version DESC LIMIT 1");

            var settings = dataTable.Rows[0].Field<string>("settings") ?? "{}";
            var title = dataTable.Rows[0].Field<string>("title");

            return new KeyValuePair<string, Dictionary<string, object>>(title, JsonConvert.DeserializeObject<Dictionary<string, object>>(settings));
        }
        
        /// <inheritdoc />
        public async Task<KeyValuePair<string, Dictionary<string, object>>> GetTemplateData(int contentId)
        {
            var rawData = await GetDatabaseData(contentId);

            return new KeyValuePair<string, Dictionary<string, object>>(rawData.Key, JsonConvert.DeserializeObject<Dictionary<string, object>>(rawData.Value));
        }
        
        /// <inheritdoc />
        private async Task<KeyValuePair<string, string>> GetDatabaseData(int contentId)
        {
            connection.ClearParameters();
            connection.AddParameter("contentId", contentId);
            var dataTable = await connection.GetAsync($@"SELECT wdc.settings, wdc.`title` FROM {WiserTableNames.WiserDynamicContent} wdc WHERE content_id = ?contentId ORDER BY wdc.version DESC LIMIT 1");

            var settings = dataTable.Rows[0].Field<string>("settings");
            var title = dataTable.Rows[0].Field<string>("title");

            return new KeyValuePair<string, string>(title, settings);
        }
        
        /// <inheritdoc />
        public async Task<int> SaveSettingsString(int contentId, string component, string componentMode, string title, Dictionary<string, object> settings, string username)
        {
            connection.ClearParameters();
            connection.AddParameter("settings", JsonConvert.SerializeObject(settings));
            connection.AddParameter("component", component);
            connection.AddParameter("componentMode", componentMode);
            connection.AddParameter("contentId", contentId);
            connection.AddParameter("title", title);
            connection.AddParameter("now", DateTime.Now);
            connection.AddParameter("username", username);

            return await connection.ExecuteAsync($@"
            SET @VersionNumber = (SELECT MAX(version)+1 FROM `{WiserTableNames.WiserDynamicContent}` WHERE content_id = ?contentId GROUP BY content_id);

            INSERT INTO {WiserTableNames.WiserDynamicContent} (`version`, `changed_on`, `changed_by`, `settings`, `content_id`, `component`, `component_mode`, `title`) 
            VALUES (@VersionNumber, ?now, ?username, ?settings, ?contentId, ?component, ?componentMode, ?title)");
        }
        
        /// <inheritdoc />
        public async Task<List<string>> GetComponentAndModeFromContentId(int contentId)
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
    }
}

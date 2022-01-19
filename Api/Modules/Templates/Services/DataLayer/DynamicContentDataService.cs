using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Api.Modules.Templates.Interfaces.DataLayer;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using Newtonsoft.Json;

namespace Api.Modules.Templates.Services.DataLayer
{
    public class DynamicContentDataService : IDynamicContentDataService, IScopedService
    {
        private readonly IDatabaseConnection connection;

        public DynamicContentDataService(IDatabaseConnection connection)
        {
            this.connection = connection;
        }

        /// <summary>
        /// Retrieve the variable data of a set version. This can be used for retrieving past versions or the current version if the version number is known.
        /// </summary>
        /// <param name="version">The version number to distinguish the values by</param>
        /// <returns>Dictionary of propertynames and their values in the given version.</returns>
        public async Task<KeyValuePair<string, Dictionary<string, object>>> GetVersionData (int version, int templateId)
        {
            connection.ClearParameters();
            connection.AddParameter("version", version);
            connection.AddParameter("templateid", templateId);
            var dataTable = await connection.GetAsync("SELECT wdc.filledvariables, wdc.`title` FROM wiser_dynamiccontent_test wdc WHERE wdc.templateid = 1 AND wdc.version = ?version ORDER BY wdc.version DESC LIMIT 1");
            
            var filledvariables = dataTable.Rows[0].Field<string>("filledvariables");
            var title = dataTable.Rows[0].Field<string>("title");

            return new KeyValuePair<string, Dictionary<string, object>>(title, JsonConvert.DeserializeObject<Dictionary<string, object>>(filledvariables));
        }

        

        /// <summary>
        /// Get the type data from the database associated with the given type.
        /// </summary>
        /// <param name="type">The type of which the data is requested.</param>
        /// <returns>Dictionary containing the properties and their values.</returns>
        public async Task<KeyValuePair<string, Dictionary<string, object>>> GetTemplateData(int templateId)
        {
            var rawData = await GetDatabaseData(templateId);

            return new KeyValuePair<string, Dictionary<string, object>>(rawData.Key, JsonConvert.DeserializeObject<Dictionary<string, object>>(rawData.Value));
        }

        /// <summary>
        /// Retrieve the accountComponentData from the database.
        /// </summary>
        /// <returns>String with filledvariables from the database. Within the string are keyvaluepairs of propertynames and their values.</returns>
        private async Task<KeyValuePair<string, string>> GetDatabaseData(int templateId)
        {
            connection.ClearParameters();
            connection.AddParameter("templateid", templateId);
            var dataTable = await connection.GetAsync($@"SELECT wdc.filledvariables, wdc.`title` FROM wiser_dynamiccontent_test wdc WHERE templateid = ?templateid ORDER BY wdc.version DESC LIMIT 1");

            var filledvariables = dataTable.Rows[0].Field<string>("filledvariables");
            var title = dataTable.Rows[0].Field<string>("title");

            return new KeyValuePair<string, string>(title, filledvariables);
        }

        /// <summary>
        /// Save the given variables and their values as a new version in the database.
        /// </summary>
        /// <param name="settings">A dictionary of propertynames and their values.</param>
        /// <returns>An int indicating the result of the executed query.</returns>
        public async Task<int> SaveSettingsString(int templateid, string component, string componentMode, string templateName, Dictionary<string, object> settings)
        {
            connection.ClearParameters();
            connection.AddParameter("filledvariables", JsonConvert.SerializeObject(settings));
            connection.AddParameter("component", component);
            connection.AddParameter("componentMode", componentMode);
            connection.AddParameter("templateid", templateid);
            connection.AddParameter("templateName", templateName);

            return await connection.ExecuteAsync($@"
            SET @VersionNumber = (SELECT MAX(version)+1 FROM `wiser_dynamiccontent_test` WHERE templateid = ?templateid GROUP BY templateid);

            INSERT INTO wiser_dynamiccontent_test (`version`, `changed_on`, `changed_by`, `filledvariables`, `templateid`, `moduleid`, `component`, `component_mode`, `title`) 
            VALUES (
	            @VersionNumber,
	            NOW(),
	            'InsertTest',
                ?filledvariables,
                ?templateid,
                42,
                ?component,
                ?componentMode,
                ?templateName
                )
            ");
        }

        /// <summary>
        /// Retrieve the component and componentMode from the dynamic content table.
        /// </summary>
        /// <param name="contentId">The id of the dynamic content whose component and componentMode to retrieve</param>
        /// <returns>A list of strings containing the component and componentMode</returns>
        public async Task<List<string>> GetComponentAndModeFromContentId (int contentId)
        {
            connection.ClearParameters();
            var dataTable = await connection.GetAsync("SELECT wdc.component, wdc.component_mode FROM wiser_dynamiccontent_test wdc WHERE id = "+contentId+" LIMIT 1");
            
            var results = new List<string>();
            results.Add(dataTable.Rows[0].Field<string>("component"));
            results.Add(dataTable.Rows[0].Field<string>("component_mode"));

            return results;
        }
    }
}

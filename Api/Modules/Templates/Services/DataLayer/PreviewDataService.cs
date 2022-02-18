using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Api.Modules.Templates.Interfaces.DataLayer;
using Api.Modules.Templates.Models.Preview;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;

namespace Api.Modules.Templates.Services.DataLayer
{
    /// <inheritdoc cref="IPreviewDataService" />
    public class PreviewDataService : IPreviewDataService, IScopedService
    {
        private readonly IDatabaseConnection connection;

        /// <summary>
        /// Creates a new instance of <see cref="PreviewDataService"/>.
        /// </summary>
        public PreviewDataService(IDatabaseConnection connection)
        {
            this.connection = connection;
        }

        /// <inheritdoc />
        public async Task<List<PreviewProfileDao>> GetAsync(int templateId)
        {
            connection.ClearParameters();
            connection.AddParameter("templateId", templateId);

            var dataTable = await connection.GetAsync($"SELECT id, name, url, variables FROM {WiserTableNames.WiserPreviewProfiles} WHERE template_id = ?templateId ORDER BY name");
            var resultList = new List<PreviewProfileDao>();
            foreach (DataRow row in dataTable.Rows)
            {
                resultList.Add(new PreviewProfileDao(
                        row.Field<int>("id"),
                        row.Field<string>("name"),
                        row.Field<string>("url"),
                        row.Field<string>("variables")
                    )
                );
            }

            return resultList;
        }

        /// <inheritdoc />
        public async Task<int> CreateAsync(PreviewProfileDao profile, int templateId)
        {
            connection.ClearParameters();
            connection.AddParameter("templateId", templateId);
            connection.AddParameter("name", profile.Name);
            connection.AddParameter("url", profile.Url);
            connection.AddParameter("variables", profile.RawVariables);

            return (int)await connection.InsertRecordAsync($"INSERT INTO {WiserTableNames.WiserPreviewProfiles} (name, template_id, url, variables) VALUES (?name, ?templateId, ?url, ?variables)");
        }

        /// <inheritdoc />
        public async Task<int> UpdateAsync(PreviewProfileDao profile, int templateId)
        {
            connection.ClearParameters();
            connection.AddParameter("templateId", templateId);
            connection.AddParameter("profileId", profile.Id);
            connection.AddParameter("name", profile.Name);
            connection.AddParameter("url", profile.Url);
            connection.AddParameter("variables", profile.RawVariables);

            return await connection.ExecuteAsync($"UPDATE {WiserTableNames.WiserPreviewProfiles} SET name = ?name, url = ?url, variables = ?variables WHERE template_id = ?templateId AND id = ?profileId");
        }

        /// <inheritdoc />
        public async Task<int> DeleteAsync(int templateId, int profileId)
        {
            connection.ClearParameters();
            connection.AddParameter("templateId", templateId);
            connection.AddParameter("profileId", profileId);

            return await connection.ExecuteAsync($"DELETE FROM {WiserTableNames.WiserPreviewProfiles} WHERE template_id = ?templateId AND id = ?profileId");
        }
    }
}
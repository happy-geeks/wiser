using System;
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
        public async Task<List<PreviewProfileDao>> Get(int templateId)
        {
            connection.ClearParameters();
            connection.AddParameter("templateid", templateId);

            var dataTable = await connection.GetAsync($"SELECT ppt.id, ppt.template_name, ppt.url, ppt.previewvariables FROM {WiserTableNames.WiserPreviewProfiles} ppt WHERE ppt.template_id = ?templateid ORDER BY ppt.ordering");
            var resultList = new List<PreviewProfileDao>();
            foreach (DataRow row in dataTable.Rows)
            {
                resultList.Add(new PreviewProfileDao(
                        row.Field<long>("id"),
                        row.Field<string>("template_name"),
                        row.Field<string>("url"),
                        row.Field<string>("previewvariables")
                    )
                );
            }

            return resultList;
        }

        /// <inheritdoc />
        public async Task<int> Create(PreviewProfileDao profile, int templateId)
        {
            connection.ClearParameters();
            connection.AddParameter("templateid", templateId);
            connection.AddParameter("name", profile.GetName());
            connection.AddParameter("url", profile.GetUrl());
            connection.AddParameter("variables", profile.GetRawVariables());

            return (int)await connection.InsertRecordAsync($"INSERT INTO {WiserTableNames.WiserPreviewProfiles}(template_name, template_id, url, previewvariables, ordering) VALUES(?name, ?templateid, ?url, ?variables, 1)");
        }

        /// <inheritdoc />
        public async Task<int> Update(PreviewProfileDao profile, int templateId)
        {
            connection.ClearParameters();
            connection.AddParameter("templateid", templateId);
            connection.AddParameter("profileid", profile.GetId());
            connection.AddParameter("name", profile.GetName());
            connection.AddParameter("url", profile.GetUrl());
            connection.AddParameter("variables", profile.GetRawVariables());

            return await connection.ExecuteAsync($"UPDATE {WiserTableNames.WiserPreviewProfiles} SET template_name=IF(?name IS NULL OR ?name='', template_name, ?name), url=?url, previewvariables=?variables, ordering=1 WHERE template_id=?templateid AND id=?profileid");
        }

        /// <inheritdoc />
        public async Task<int> Delete(int templateId, int profileId)
        {
            connection.ClearParameters();
            connection.AddParameter("templateid", templateId);
            connection.AddParameter("profileid", profileId);

            return await connection.ExecuteAsync($"DELETE FROM {WiserTableNames.WiserPreviewProfiles} ppt WHERE ppt.template_id = ?templateid AND ppt.id = ?profileid");
        }
    }
}
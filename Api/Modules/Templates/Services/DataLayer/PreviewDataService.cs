using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Api.Modules.Templates.Interfaces.DataLayer;
using Api.Modules.Templates.Models.Preview;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Modules.Databases.Interfaces;

namespace Api.Modules.Templates.Services.DataLayer
{
    public class PreviewDataService : IPreviewDataService, IScopedService
    {
        private readonly IDatabaseConnection connection;

        public PreviewDataService(IDatabaseConnection connection)
        {
            this.connection = connection;
        }

        /// <summary>
        /// Edit an existing preview profile. This will only alter the name if a name is given in the param.
        /// </summary>
        /// <param name="profile">A previewprofile containing the id, name and settings of the preview profile</param>
        /// <param name="templateId"></param>
        /// <returns>A int representing the rows affected.</returns>
        public async Task<int> EditPreviewProfile(PreviewProfileDAO profile, int templateId)
        {
            connection.ClearParameters();
            connection.AddParameter("templateid", templateId);
            connection.AddParameter("profileid", profile.GetId());
            connection.AddParameter("name", profile.GetName());
            connection.AddParameter("url", profile.GetUrl());
            connection.AddParameter("variables", profile.GetRawVariables());

            return await connection.ExecuteAsync("UPDATE wiser_previewprofiles_test SET template_name=IF(?name IS NULL OR ?name='', template_name, ?name), url=?url, previewvariables=?variables, ordering=1 WHERE template_id=?templateid AND id=?profileid");
        }

        /// <summary>
        /// Retrieve all preview profiles that are available at a given template.
        /// </summary>
        /// <param name="templateId">The id of the template</param>
        /// <returns>A list of preview profile models containing the id, name and settings for a preview profile.</returns>
        public async Task<List<PreviewProfileDAO>> GetPreviewProfiles(int templateId)
        {
            connection.ClearParameters();
            connection.AddParameter("templateid", templateId);

            var dataTable = await connection.GetAsync("SELECT ppt.id, ppt.template_name, ppt.url, ppt.previewvariables FROM wiser_previewprofiles_test ppt WHERE ppt.template_id = ?templateid ORDER BY ppt.ordering");
            var resultList = new List<PreviewProfileDAO>();
            foreach(DataRow row in dataTable.Rows)
            {
                resultList.Add(new PreviewProfileDAO(
                        row.Field<Int64>("id"),
                        row.Field<string>("template_name"),
                        row.Field<string>("url"),
                        row.Field<string>("previewvariables")
                    )
                ); 
            }
            return resultList;
        }

        /// <summary>
        /// Deletes a preview profile with the given id.
        /// </summary>
        /// <param name="templateId"></param>
        /// <param name="profileId">The id of the preview profile</param>
        /// <returns>An int representing the rows affected.</returns>
        public async Task<int> RemovePreviewProfile(int templateId, int profileId)
        {
            connection.ClearParameters();
            connection.AddParameter("templateid", templateId);
            connection.AddParameter("profileid", profileId);

            return await connection.ExecuteAsync("DELETE FROM wiser_previewprofiles_test ppt WHERE ppt.template_id = ?templateid AND ppt.id = ?profileid");
        }

        /// <summary>
        /// Create a new preview profile that matches the params. This wil always set a new name.
        /// </summary>
        /// <param name="profile">A previewprofilemodel containing the name and settings of the new profile</param>
        /// <param name="templateId"></param>
        /// <returns>An int representing the rows affected.</returns>
        public async Task<int> SaveNewPreviewProfile(PreviewProfileDAO profile, int templateId)
        {
            connection.ClearParameters();
            connection.AddParameter("templateid", templateId);
            connection.AddParameter("name", profile.GetName());
            connection.AddParameter("url", profile.GetUrl());
            connection.AddParameter("variables", profile.GetRawVariables());

            return await connection.ExecuteAsync("INSERT INTO wiser_previewprofiles_test(template_name, template_id, url, previewvariables, ordering) VALUES(?name, ?templateid, ?url, ?variables, 1)");
        }
    }
}

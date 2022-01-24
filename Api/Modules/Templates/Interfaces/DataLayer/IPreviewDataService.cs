using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Modules.Templates.Models.Preview;

namespace Api.Modules.Templates.Interfaces.DataLayer
{
    /// <summary>
    /// Data service for doing CRUD operations in database for preview profiles for the templates module.
    /// </summary>
    public interface IPreviewDataService
    {
        /// <summary>
        /// Retrieve all preview profiles that are available at a given template.
        /// </summary>
        /// <param name="templateId">The id of the template</param>
        /// <returns>A list of preview profile models containing the id, name and settings for a preview profile.</returns>
        public Task<List<PreviewProfileDao>> Get(int templateId);

        /// <summary>
        /// Create a new preview profile that matches the params. This wil always set a new name.
        /// </summary>
        /// <param name="profile">A previewprofilemodel containing the name and settings of the new profile</param>
        /// <param name="templateId"></param>
        /// <returns>The ID of the new profile.</returns>
        public Task<int> Create(PreviewProfileDao profile, int templateId);

        /// <summary>
        /// Edit an existing preview profile. This will only alter the name if a name is given in the param.
        /// </summary>
        /// <param name="profile">A previewprofile containing the id, name and settings of the preview profile</param>
        /// <param name="templateId"></param>
        /// <returns>A int representing the rows affected.</returns>
        public Task<int> Update(PreviewProfileDao profile, int templateId);

        /// <summary>
        /// Deletes a preview profile with the given id.
        /// </summary>
        /// <param name="templateId"></param>
        /// <param name="profileId">The id of the preview profile</param>
        /// <returns>An int representing the rows affected.</returns>
        public Task<int> Delete(int templateId, int profileId);
    }
}

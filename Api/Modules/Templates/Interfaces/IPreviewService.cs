using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.Templates.Models.Preview;

namespace Api.Modules.Templates.Interfaces
{
    /// <summary>
    /// Service for CRUD operations for preview profiles for the templates module.
    /// </summary>
    public interface IPreviewService
    {
        /// <summary>
        /// Retrieve al the preview profiles for an item.
        /// </summary>
        /// <param name="templateId">the id of the item to retrieve the preview items of.</param>
        /// <returns>A list of PreviewProfileModel containing the profiles that are available for the given template</returns>
        public Task<ServiceResult<List<PreviewProfileModel>>> GetAsync(int templateId);
        
        /// <summary>
        /// Creates a new instance of a preview profile with the given data.
        /// </summary>
        /// <param name="profile">A PreviewProfileModel containing the data of the profile to create</param>
        /// <param name="templateId"></param>
        public Task<ServiceResult<PreviewProfileModel>> CreateAsync(PreviewProfileModel profile, int templateId);

        /// <summary>
        /// Alter an existing preview profile. The preview profiles name can be empty to keep the old name.
        /// </summary>
        /// <param name="profile">A PreviewProfileModel containing the data of the profile to save</param>
        /// <param name="templateId"></param>
        public Task<ServiceResult<bool>> UpdateAsync(PreviewProfileModel profile, int templateId);
        
        /// <summary>
        /// Delete a preview profile.
        /// </summary>
        /// <param name="templateId"></param>
        /// <param name="profileId">The id of the preview profile that is to be deleted</param>
        public Task<ServiceResult<bool>> DeleteAsync(int templateId, int profileId);
    }
}

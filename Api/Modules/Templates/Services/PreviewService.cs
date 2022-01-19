using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Modules.Templates.Helpers;
using Api.Modules.Templates.Interfaces;
using Api.Modules.Templates.Interfaces.DataLayer;
using Api.Modules.Templates.Models.Preview;

namespace Api.Modules.Templates.Services
{
    public class PreviewService : IPreviewService
    {
        private readonly IPreviewDataService previewDataService;

        public PreviewService(IPreviewDataService previewDataService)
        {
            this.previewDataService = previewDataService;
        }

        /// <summary>
        /// Retrieve al the preview profiles for an item.
        /// </summary>
        /// <param name="templateId">the id of the item to retrieve the preview items of.</param>
        /// <returns>A list of PreviewProfileModel containing the profiles that are available for the given template</returns>
        public async Task<List<PreviewProfileModel>> GetPreviewProfiles(int templateId)
        {
            var dataList = await previewDataService.GetPreviewProfiles(templateId);
            var modelList = new List<PreviewProfileModel>();

            var helper = new PreviewProfileHelper();

            foreach(var previewDAO in dataList)
            {
                modelList.Add(helper.ConvertPreviewProfileDAOToModel(previewDAO));
            }

            return modelList;
        }

        /// <summary>
        /// Delete a preview profile.
        /// </summary>
        /// <param name="templateId"></param>
        /// <param name="profileId">The id of the previewprofile that is to be deleted</param>
        /// <returns>An int confirming the rows affected</returns>
        public async Task<int> RemovePreviewProfile(int templateId, int profileId)
        {
            return await previewDataService.RemovePreviewProfile(templateId, profileId);
        }

        /// <summary>
        /// Creates a new instance of a previewprofile with the given data
        /// </summary>
        /// <param name="profile">A PreviewProfileModel containing the data of the profile to create</param>
        /// <param name="templateId"></param>
        /// <returns>An int confirming the rows affected</returns>
        public async Task<int> SaveNewPreviewProfile(PreviewProfileModel profile, int templateId)
        {
            var helper = new PreviewProfileHelper();

            return await previewDataService.SaveNewPreviewProfile(helper.ConvertPreviewProfileModelToDAO(profile), templateId);
        }

        /// <summary>
        /// Alter an existing preview profile. The preview profiles name can be empty to keep the old name.
        /// </summary>
        /// <param name="profile">A PreviewProfileModel containing the data of the profile to save</param>
        /// <param name="templateId"></param>
        /// <returns>An int confirming the rows affected</returns>
        public async Task<int> EditPreviewProfile(PreviewProfileModel profile, int templateId)
        {
            var helper = new PreviewProfileHelper();

            return await previewDataService.EditPreviewProfile(helper.ConvertPreviewProfileModelToDAO(profile), templateId);
        }
    }
}

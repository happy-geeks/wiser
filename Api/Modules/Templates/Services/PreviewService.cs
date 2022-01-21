using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.Templates.Helpers;
using Api.Modules.Templates.Interfaces;
using Api.Modules.Templates.Interfaces.DataLayer;
using Api.Modules.Templates.Models.Preview;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;

namespace Api.Modules.Templates.Services
{
    /// <inheritdoc cref="IPreviewService" />
    public class PreviewService : IPreviewService, IScopedService
    {
        private readonly IPreviewDataService previewDataService;

        /// <summary>
        /// Creates a new instance of <see cref="PreviewService"/>.
        /// </summary>
        public PreviewService(IPreviewDataService previewDataService)
        {
            this.previewDataService = previewDataService;
        }

        /// <inheritdoc />
        public async Task<ServiceResult<List<PreviewProfileModel>>> GetPreviewProfiles(int templateId)
        {
            var dataList = await previewDataService.GetPreviewProfiles(templateId);
            var modelList = new List<PreviewProfileModel>();
            
            foreach (var previewDao in dataList)
            {
                modelList.Add(PreviewProfileHelper.ConvertPreviewProfileDAOToModel(previewDao));
            }

            return new ServiceResult<List<PreviewProfileModel>>(modelList);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<PreviewProfileModel>> CreatePreviewProfile(PreviewProfileModel profile, int templateId)
        {
            profile.id = await previewDataService.CreatePreviewProfile(PreviewProfileHelper.ConvertPreviewProfileModelToDAO(profile), templateId);
            return new ServiceResult<PreviewProfileModel>(profile);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> EditPreviewProfile(PreviewProfileModel profile, int templateId)
        {
            await previewDataService.EditPreviewProfile(PreviewProfileHelper.ConvertPreviewProfileModelToDAO(profile), templateId);
            return new ServiceResult<bool>(true);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> RemovePreviewProfile(int templateId, int profileId)
        {
            await previewDataService.RemovePreviewProfile(templateId, profileId);
            return new ServiceResult<bool>(true);
        }
    }
}

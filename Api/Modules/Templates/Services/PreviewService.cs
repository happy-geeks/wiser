using System;
using System.Collections.Generic;
using System.Net;
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
        public async Task<ServiceResult<List<PreviewProfileModel>>> Get(int templateId)
        {
            var dataList = await previewDataService.Get(templateId);
            var modelList = new List<PreviewProfileModel>();
            
            foreach (var previewDao in dataList)
            {
                modelList.Add(PreviewProfileHelper.ConvertPreviewProfileDAOToModel(previewDao));
            }

            return new ServiceResult<List<PreviewProfileModel>>(modelList);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<PreviewProfileModel>> Create(PreviewProfileModel profile, int templateId)
        {
            if (templateId <= 0)
            {
                return new ServiceResult<PreviewProfileModel>
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessage = "The Id cannot be zero."
                };
            }

            profile.Id = await previewDataService.Create(PreviewProfileHelper.ConvertPreviewProfileModelToDAO(profile), templateId);
            return new ServiceResult<PreviewProfileModel>(profile);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> Update(PreviewProfileModel profile, int templateId)
        {
            if (templateId <= 0)
            {
                return new ServiceResult<bool>
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessage = "The Id cannot be zero."
                };
            }

            if (profile.Id <= 0)
            {
                return new ServiceResult<bool>
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessage = "The profileId is invalid"
                };
            }
            
            await previewDataService.Update(PreviewProfileHelper.ConvertPreviewProfileModelToDAO(profile), templateId);
            return new ServiceResult<bool>(true)
            {
                StatusCode = HttpStatusCode.NoContent
            };
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> Delete(int templateId, int profileId)
        {
            if (templateId <= 0)
            {
                return new ServiceResult<bool>
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessage = "The Id cannot be zero."
                };
            }
            if (profileId <= 0)
            {
                return new ServiceResult<bool>
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessage = "The profileId is invalid"
                };
            }

            await previewDataService.Delete(templateId, profileId);
            return new ServiceResult<bool>(true)
            {
                StatusCode = HttpStatusCode.NoContent
            };
        }
    }
}

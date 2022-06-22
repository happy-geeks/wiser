using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Helpers;
using Api.Core.Services;
using Api.Modules.Templates.Helpers;
using Api.Modules.Templates.Models.Other;
using Api.Modules.VersionControl.Interfaces;
using Api.Modules.VersionControl.Interfaces.DataLayer;
using Api.Modules.VersionControl.Models;

namespace Api.Modules.VersionControl.Service
{
    /// <inheritdoc cref="IDynamicContentServiceVersionControl" />
    public class DynamicContentServiceVersionControl : IDynamicContentServiceVersionControl
    {
        private readonly IDynamicContentDataServiceVersionControl dynamicContentDataService;
        /// <summary>
        /// Creates a new instance of <see cref="DynamicContentServiceVersionControl"/>.
        /// </summary>
        public DynamicContentServiceVersionControl(IDynamicContentDataServiceVersionControl dynamicContentDataService)
        {
            this.dynamicContentDataService = dynamicContentDataService;
        }

        /// <inheritdoc />
        public async Task<ServiceResult<DynamicContentModel>> GetDynamicContentAsync(int contentId, int version)
        {
            var result = await dynamicContentDataService.GetDynamicContentAsync(contentId, version);

            return new ServiceResult<DynamicContentModel>(result);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> CreateNewDynamicContentCommitAsync(
            DynamicContentCommitModel dynamicContentCommitModel)
        {
            var result = await dynamicContentDataService.CreateNewDynamicContentCommitAsync(dynamicContentCommitModel);

            return new ServiceResult<bool>(result);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<PublishedEnvironmentModel>> GetDynamicContentEnvironmentsAsync(
            int dynamicContentId)
        {

            if (dynamicContentId <= 0)
            {
                throw new ArgumentException("The Id cannot be zero.");
            }

            var versionsAndPublished =
                await dynamicContentDataService.GetDynamicContentEnvironmentsAsync(dynamicContentId);

            return new ServiceResult<PublishedEnvironmentModel>(
                PublishedEnvironmentHelper.CreatePublishedEnvironmentsFromVersionDictionary(versionsAndPublished));
        }

        /// <inheritdoc />
        public async Task<ServiceResult<int>> PublishDynamicContentToEnvironmentAsync(ClaimsIdentity identity,
            int dynamicContentId, int version, string environment, PublishedEnvironmentModel currentPublished)
        {
            if (dynamicContentId <= 0)
            {
                throw new ArgumentException("The Id is invalid");
            }

            if (version <= 0)
            {
                throw new ArgumentException("The version is invalid");
            }

            var newPublished =
                PublishedEnvironmentHelper.CalculateEnvironmentsToPublish(currentPublished, version, environment);

            var publishLog =
                PublishedEnvironmentHelper.GeneratePublishLog(dynamicContentId, currentPublished, newPublished);

            return new ServiceResult<int>(
                await dynamicContentDataService.UpdateDynamicContentPublishedEnvironmentAsync(dynamicContentId,
                    newPublished, publishLog, IdentityHelpers.GetUserName(identity)));
        }

        /// <inheritdoc />
        public async Task<ServiceResult<Dictionary<int, int>>> GetDynamicContentWithLowerVersionAsync(int contentId,
            int version)
        {
            var result = await dynamicContentDataService.GetDynamicContentWithLowerVersionAsync(contentId, version);

            return new ServiceResult<Dictionary<int, int>>(result);
        }
    }
}

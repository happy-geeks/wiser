using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.VersionControl.Interfaces;
using Api.Modules.VersionControl.Interfaces.DataLayer;
using Api.Modules.VersionControl.Models;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;

namespace Api.Modules.VersionControl.Services
{
    /// <inheritdoc cref="IVersionControlService" />
    public class VersionControlService : IVersionControlService, IScopedService
    {
        private readonly IVersionControlDataService versionControlDataService;
        
        /// <summary>
        /// Creates a new instance of <see cref="VersionControlService"/>.
        /// </summary>
        public VersionControlService(IVersionControlDataService versionControlDataService)
        {
            this.versionControlDataService = versionControlDataService;
        }

        /// <inheritdoc />
        public async Task<ServiceResult<Dictionary<int, int>>> GetPublishedTemplateIdAndVersionAsync()
        {
            var result = await versionControlDataService.GetPublishedTemplateIdAndVersionAsync();

            return new ServiceResult<Dictionary<int,int>>(result);
        }


        /// <inheritdoc />
        public async Task<ServiceResult<bool>> CreatePublishLogAsync(int templateId, int version)
        {
            var result = await versionControlDataService.CreatePublishLog(templateId, version);

            return new ServiceResult<bool>(result);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<List<TemplateCommitModel>>> GetTemplatesFromCommitAsync(int commitId)
        {
            var result = await versionControlDataService.GetTemplatesFromCommitAsync(commitId);
            return new ServiceResult<List<TemplateCommitModel>>(result);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<List<DynamicContentCommitModel>>> GetDynamicContentFromCommitAsync(int commitId)
        {
            var result = await versionControlDataService.GetDynamicContentFromCommitAsync(commitId);
            return new ServiceResult<List<DynamicContentCommitModel>>(result);
        }
    }
}

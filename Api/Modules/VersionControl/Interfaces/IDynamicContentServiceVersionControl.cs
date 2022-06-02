using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.Templates.Models.Other;
using Api.Modules.VersionControl.Models;

namespace Api.Modules.VersionControl.Interfaces
{
    public interface IDynamicContentServiceVersionControl
    {

        Task<ServiceResult<DynamicContentModel>> GetDynamicContent(int contentId, int version);

        Task<ServiceResult<bool>> CreateNewDynamicContentCommit(DynamicContentCommitModel dynamicContentCommitModel);

        Task<ServiceResult<PublishedEnvironmentModel>> GetDynamicContentEnvironmentsAsync(int dynamicContentId);

        Task<ServiceResult<int>> PublishDynamicContentToEnvironmentAsync(ClaimsIdentity identity, int dynamicContentId, int version, string environment, PublishedEnvironmentModel currentPublished);

        Task<ServiceResult<Dictionary<int, int>>> GetDynamicContentWithLowerVersion(int templateId, int version);

    }
}

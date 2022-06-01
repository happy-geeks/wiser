using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.Grids.Models;
using Api.Modules.Templates.Models.Other;
using Api.Modules.VersionControl.Models;

namespace Api.Modules.VersionControl.Interfaces
{
    /// <summary>
    /// 
    /// </summary>
    public interface IVersionControlService
    {
        Task<ServiceResult<CreateCommitModel>> CreateCommit(CreateCommitModel commitModel);

        Task<ServiceResult<bool>> CreateCommitItem(int templateId, CommitItemModel commitItemModel);

        Task<ServiceResult<CreateCommitModel>> GetCommit();
        Task<ServiceResult<Dictionary<int,int>>> GetPublishedTemplateIdAndVersion();
        //Task<ServiceResult<Dictionary<int,int>>> GetTemplatesWithLowerVersion(int templateId, int version);

    
        Task<ServiceResult<bool>> CreateNewTemplateCommit(TemplateCommitModel templateCommitModel);
        

        Task<ServiceResult<bool>> UpdatePublishEnvironmentTemplate(int templateId, int publishNumber);
        Task<ServiceResult<bool>> UpdateTemplateCommit(TemplateCommitModel templateCommitModel);

        Task<ServiceResult<VersionControlModel>> GetCurrentPublishedEnvironment(int templateId, int version);

        Task<ServiceResult<bool>> CreatePublishLog(int templateId, int version);

        Task<ServiceResult<List<TemplateCommitModel>>> GetTemplatesFromCommit(int commitId);
        Task<ServiceResult<List<DynamicContentCommitModel>>> GetDynamicContentfromCommit(int commitId);

        Task<ServiceResult<List<ModuleGridSettings>>> GetModuleGridSettings(int moduleId);

        //DYNAMIC CONTENT
        Task<ServiceResult<DynamicContentModel>> GetDynamicContent(int contentId, int version);

        Task<ServiceResult<bool>> CreateNewDynamicContentCommit(DynamicContentCommitModel dynamicContentCommitModel);

        Task<ServiceResult<PublishedEnvironmentModel>> GetDynamicContentEnvironmentsAsync(int dynamicContentId);

        Task<ServiceResult<int>> PublishDynamicContentToEnvironmentAsync(ClaimsIdentity identity, int dynamicContentId, int version, string environment, PublishedEnvironmentModel currentPublished);

        Task<ServiceResult<Dictionary<int, int>>> GetDynamicContentWithLowerVersion(int templateId, int version);

        Task<ServiceResult<List<DynamicContentModel>>> GetDynamicContentInTemplate(int templateId);

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.Grids.Models;
using Api.Modules.Templates.Models.Other;
using Api.Modules.Templates.Models.Template;
using Api.Modules.VersionControl.Models;

namespace Api.Modules.VersionControl.Interfaces
{
    public interface IVersionControlDataService
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="templateId"></param>
        /// <param name="commitModel"></param>
        /// <returns></returns>
        Task<CreateCommitModel> CreateCommit(CreateCommitModel commitModel);

        Task<bool> CreateCommitItem(int templateId, CommitItemModel commitItemModel);

        Task<CreateCommitModel> GetCommit();
        Task<Dictionary<int,int>> GetPublishedTemplateIdAndVersion();
        //Task<Dictionary<int, int>> GetTemplatesWithLowerVersion(int templateId, int version);

       

        Task<bool> CreateNewTemplateCommit(TemplateCommitModel templateCommitModel);
        Task<bool> UpdateTemplateCommit(TemplateCommitModel templateCommitModel);

        Task<bool> UpdatePublishEnvironmentTemplate(int templateId, int publishNumber);

        Task<bool> CreatePublishLog(int templateId, int version);

        Task<VersionControlModel> GetCurrentPublishedEnvironment(int templateId, int version);

        Task<List<TemplateCommitModel>> GetTemplatesFromCommit(int commitId);
        Task<List<DynamicContentCommitModel>> GetDynamicContentfromCommit(int commitId);
        Task<List<ModuleGridSettings>> GetModuleGridSettings(int moduleId);

        //DynamicContent
        Task<DynamicContentModel> GetDynamicContent(int contentId, int version);
        Task<bool> CreateNewDynamicContentCommit(DynamicContentCommitModel dynamicContentCommitModel);

        Task<Dictionary<int, int>> GetDynamicContentEnvironmentsAsync(int dynamicContentId);

        Task<int> UpdateDynamicContentPublishedEnvironmentAsync(int dynamicContentId, Dictionary<int, int> publishModel, PublishLogModel publishLog, string username);
        
        Task<Dictionary<int, int>> GetDynamicContentWithLowerVersion(int templateId, int version);

        Task<List<DynamicContentModel>> GetDynamicContentInTemplate(int templateId);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.VersionControl.Models;

namespace Api.Modules.VersionControl.Interfaces
{
    public interface ITemplateContainerService
    {
        Task<ServiceResult<Dictionary<int, int>>> GetTemplatesWithLowerVersion(int templateId, int version);

        Task<ServiceResult<bool>> CreateNewTemplateCommit(TemplateCommitModel templateCommitModel);


        Task<ServiceResult<bool>> UpdatePublishEnvironmentTemplate(int templateId, int publishNumber);
        Task<ServiceResult<bool>> UpdateTemplateCommit(TemplateCommitModel templateCommitModel);

        Task<ServiceResult<VersionControlModel>> GetCurrentPublishedEnvironment(int templateId, int version);
    }
}

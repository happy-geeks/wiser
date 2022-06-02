using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Api.Modules.VersionControl.Models;

namespace Api.Modules.VersionControl.Interfaces.DataLayer
{
    public interface ITemplateContainerDataService
    {
        Task<Dictionary<int, int>> GetTemplatesWithLowerVersion(int templateId, int version);

        Task<bool> CreateNewTemplateCommit(TemplateCommitModel templateCommitModel);
        Task<bool> UpdateTemplateCommit(TemplateCommitModel templateCommitModel);

        Task<bool> UpdatePublishEnvironmentTemplate(int templateId, int publishNumber);

        Task<VersionControlModel> GetCurrentPublishedEnvironment(int templateId, int version);
    }
}

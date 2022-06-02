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

        Task<ServiceResult<Dictionary<int,int>>> GetPublishedTemplateIdAndVersion();

        Task<ServiceResult<bool>> CreatePublishLog(int templateId, int version);
        Task<ServiceResult<List<TemplateCommitModel>>> GetTemplatesFromCommit(int commitId);

        Task<ServiceResult<List<DynamicContentCommitModel>>> GetDynamicContentfromCommit(int commitId);

        Task<ServiceResult<List<ModuleGridSettings>>> GetModuleGridSettings(int moduleId);

        Task<ServiceResult<List<DynamicContentModel>>> GetDynamicContentInTemplate(int templateId);

    }
}

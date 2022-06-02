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
   
        Task<Dictionary<int,int>> GetPublishedTemplateIdAndVersion();
        //Task<Dictionary<int, int>> GetTemplatesWithLowerVersion(int templateId, int version);

       

        

        Task<bool> CreatePublishLog(int templateId, int version);

        

        Task<List<TemplateCommitModel>> GetTemplatesFromCommit(int commitId);
        Task<List<DynamicContentCommitModel>> GetDynamicContentfromCommit(int commitId);
        Task<List<ModuleGridSettings>> GetModuleGridSettings(int moduleId);

        //DynamicContent


        Task<List<DynamicContentModel>> GetDynamicContentInTemplate(int templateId);
    }
}

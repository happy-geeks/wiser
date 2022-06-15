using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Helpers;
using Api.Core.Services;
using Api.Modules.Grids.Models;
using Api.Modules.Templates.Helpers;
using Api.Modules.Templates.Models.Other;
using Api.Modules.VersionControl.Interfaces;
using Api.Modules.VersionControl.Models;
using BuckarooSdk.Services.CreditCards.BanContact.Push;

namespace Api.Modules.VersionControl.Service
{
    public class VersionControlService : IVersionControlService
    {

        private readonly IVersionControlDataService versionControlDataService;

        public VersionControlService(IVersionControlDataService versionControlDataService)
        {
            this.versionControlDataService = versionControlDataService;
        }


        /// <inheritdoc />
        public async Task<ServiceResult<Dictionary<int, int>>> GetPublishedTemplateIdAndVersion()
        {
            var result = await versionControlDataService.GetPublishedTemplateIdAndVersion();

            return new ServiceResult<Dictionary<int,int>>(result);
        }


        /// <inheritdoc />
        public async Task<ServiceResult<bool>> CreatePublishLog(int templateId, int version)
        {
            var result = await versionControlDataService.CreatePublishLog(templateId, version);

            return new ServiceResult<bool>(result);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<List<TemplateCommitModel>>> GetTemplatesFromCommit(int commitId)
        {
            var result = await versionControlDataService.GetTemplatesFromCommit(commitId);
            return new ServiceResult<List<TemplateCommitModel>>(result);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<List<DynamicContentCommitModel>>> GetDynamicContentfromCommit(int commitId)
        {
            var result = await versionControlDataService.GetDynamicContentfromCommit(commitId);
            return new ServiceResult<List<DynamicContentCommitModel>>(result);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<List<ModuleGridSettings>>> GetModuleGridSettings(int moduleId)
        {
            var result = await versionControlDataService.GetModuleGridSettings(moduleId);
            return new ServiceResult<List<ModuleGridSettings>>(result);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<List<DynamicContentModel>>> GetDynamicContentInTemplate(int templateId)
        {
            var result = await versionControlDataService.GetDynamicContentInTemplate(templateId);

            return new ServiceResult<List<DynamicContentModel>>(result);
        }
    }
}

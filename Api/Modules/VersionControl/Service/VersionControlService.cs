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

        public async Task<ServiceResult<CreateCommitModel>> CreateCommit(CreateCommitModel commitModel)
        {
            if (String.IsNullOrWhiteSpace(commitModel.Description))
            {
                throw new ArgumentException("No commit message!");
            }


            var result = await versionControlDataService.CreateCommit(commitModel);

            return new ServiceResult<CreateCommitModel>(result);
        }

        public async Task<ServiceResult<bool>> CreateCommitItem(int templateId, CommitItemModel commitItemModel)
        {

            var result = await versionControlDataService.CreateCommitItem(templateId, commitItemModel);

            return new ServiceResult<bool>(result);
        }

        public async Task<ServiceResult<CreateCommitModel>> GetCommit()
        {
            var result = await versionControlDataService.GetCommit();

            return new ServiceResult<CreateCommitModel>(result);
        }

        public async Task<ServiceResult<Dictionary<int, int>>> GetPublishedTemplateIdAndVersion()
        {
            var result = await versionControlDataService.GetPublishedTemplateIdAndVersion();

            return new ServiceResult<Dictionary<int,int>>(result);
        }

        

        /*public async Task<ServiceResult<Dictionary<int, int>>> GetTemplatesWithLowerVersion(int templateId, int version)
        {
            var result = await versionControlDataService.GetTemplatesWithLowerVersion(templateId,version);

            return new ServiceResult<Dictionary<int, int>>(result);
        }*/

        public async Task<ServiceResult<bool>> CreateNewTemplateCommit(TemplateCommitModel templateCommitModel)
        {

            /*
            if (!templateCommitModel.IsTest && !templateCommitModel.IsLive && !templateCommitModel.IsAcceptance)
            {
                throw new ArgumentException("Need environment");
            }

            */

           

            bool isTest = false;
            bool isAcceptatie = false;
            bool isLive = false;

            if (templateCommitModel.Enviornment == "live")
            {
                isTest = true;
                isAcceptatie = true;
                isLive = true;
            }else if (templateCommitModel.Enviornment == "accept")
            {
                isAcceptatie = true;
                isTest = true;
            }else if (templateCommitModel.Enviornment == "test")
            {
                isTest = true;
            }

            templateCommitModel.IsTest = isTest;
            templateCommitModel.IsAcceptance = isAcceptatie;
            templateCommitModel.IsLive = isLive;


          
            var result = await versionControlDataService.CreateNewTemplateCommit(templateCommitModel);

            return new ServiceResult<bool>(result);
        }

        public async Task<ServiceResult<bool>> UpdatePublishEnvironmentTemplate(int templateId, int publishNumber)
        {
            var result = await versionControlDataService.UpdatePublishEnvironmentTemplate(templateId, publishNumber);

            return new ServiceResult<bool>(result);
        }

        public async Task<ServiceResult<bool>> UpdateTemplateCommit(TemplateCommitModel templateCommitModel)
        {
            bool isTest = false;
            bool isAcceptatie = false;
            bool isLive = false;

            if (templateCommitModel.Enviornment == "live")
            {
                isTest = true;
                isAcceptatie = true;
                isLive = true;
            }
            else if (templateCommitModel.Enviornment == "accept")
            {
                isAcceptatie = true;
                isTest = true;
            }
            else if (templateCommitModel.Enviornment == "test")
            {
                isTest = true;
            }

            templateCommitModel.IsTest = isTest;
            templateCommitModel.IsAcceptance = isAcceptatie;
            templateCommitModel.IsLive = isLive;

            var result = await versionControlDataService.UpdateTemplateCommit(templateCommitModel);

            return new ServiceResult<bool>(result);
        }

        public async Task<ServiceResult<VersionControlModel>> GetCurrentPublishedEnvironment(int templateId,
            int version)
        {
            var result = await versionControlDataService.GetCurrentPublishedEnvironment(templateId, version);

            return new ServiceResult<VersionControlModel>(result);
        }

        public async Task<ServiceResult<bool>> CreatePublishLog(int templateId, int version)
        {
            var result = await versionControlDataService.CreatePublishLog(templateId, version);

            return new ServiceResult<bool>(result);
        }

        public async Task<ServiceResult<List<TemplateCommitModel>>> GetTemplatesFromCommit(int commitId)
        {
            var result = await versionControlDataService.GetTemplatesFromCommit(commitId);
            return new ServiceResult<List<TemplateCommitModel>>(result);
        }

        public async Task<ServiceResult<List<DynamicContentCommitModel>>> GetDynamicContentfromCommit(int commitId)
        {
            var result = await versionControlDataService.GetDynamicContentfromCommit(commitId);
            return new ServiceResult<List<DynamicContentCommitModel>>(result);
        }

        public async Task<ServiceResult<List<ModuleGridSettings>>> GetModuleGridSettings(int moduleId)
        {
            var result = await versionControlDataService.GetModuleGridSettings(moduleId);
            return new ServiceResult<List<ModuleGridSettings>>(result);
        }


        public async Task<ServiceResult<DynamicContentModel>> GetDynamicContent(int contentId, int version)
        {
            var result = await versionControlDataService.GetDynamicContent(contentId, version);

            return new ServiceResult<DynamicContentModel>(result);
        }

        public async Task<ServiceResult<bool>> CreateNewDynamicContentCommit(DynamicContentCommitModel dynamicContentCommitModel)
        {
            var result = await versionControlDataService.CreateNewDynamicContentCommit(dynamicContentCommitModel);

            return new ServiceResult<bool>(result);
        }

        public async Task<ServiceResult<PublishedEnvironmentModel>> GetDynamicContentEnvironmentsAsync(int dynamicContentId)
        {

            if (dynamicContentId <= 0)
            {
                throw new ArgumentException("The Id cannot be zero.");
            }

            var versionsAndPublished = await versionControlDataService.GetDynamicContentEnvironmentsAsync(dynamicContentId);

         

            return new ServiceResult<PublishedEnvironmentModel>(PublishedEnvironmentHelper.CreatePublishedEnvironmentsFromVersionDictionary(versionsAndPublished));
        }

        public async Task<ServiceResult<int>> PublishDynamicContentToEnvironmentAsync(ClaimsIdentity identity, int dynamicContentId, int version, string environment, PublishedEnvironmentModel currentPublished)
        {
            if (dynamicContentId <= 0)
            {
                throw new ArgumentException("The Id is invalid");
            }

            if (version <= 0)
            {
                throw new ArgumentException("The version is invalid");
            }

            

            var newPublished = PublishedEnvironmentHelper.CalculateEnvironmentsToPublish(currentPublished, version, environment);

            var publishLog = PublishedEnvironmentHelper.GeneratePublishLog(dynamicContentId, currentPublished, newPublished);

            return new ServiceResult<int>(await versionControlDataService.UpdateDynamicContentPublishedEnvironmentAsync(dynamicContentId, newPublished, publishLog, IdentityHelpers.GetUserName(identity)));
        }

        public async Task<ServiceResult<Dictionary<int, int>>> GetDynamicContentWithLowerVersion(int contentId, int version)
        {
            var result = await versionControlDataService.GetDynamicContentWithLowerVersion(contentId, version);

            return new ServiceResult<Dictionary<int, int>>(result);
        }

        public async Task<ServiceResult<List<DynamicContentModel>>> GetDynamicContentInTemplate(int templateId)
        {
            var result = await versionControlDataService.GetDynamicContentInTemplate(templateId);

            return new ServiceResult<List<DynamicContentModel>>(result);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.VersionControl.Interfaces.DataLayer;
using Api.Modules.VersionControl.Interfaces;
using Api.Modules.VersionControl.Models;

namespace Api.Modules.VersionControl.Service
{
    public class TemplateContainerService : ITemplateContainerService
    {

        private readonly ITemplateContainerDataService templateDataService;

        public TemplateContainerService(ITemplateContainerDataService templateDataService)
        {
            this.templateDataService = templateDataService;
        }

        /// <inheritdoc />
        public async Task<ServiceResult<Dictionary<int, int>>> GetTemplatesWithLowerVersion(int templateId, int version)
        {
            var result = await templateDataService.GetTemplatesWithLowerVersion(templateId, version);

            return new ServiceResult<Dictionary<int, int>>(result);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> CreateNewTemplateCommit(TemplateCommitModel templateCommitModel)
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



            var result = await templateDataService.CreateNewTemplateCommit(templateCommitModel);

            return new ServiceResult<bool>(result);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> UpdatePublishEnvironmentTemplate(int templateId, int publishNumber)
        {
            var result = await templateDataService.UpdatePublishEnvironmentTemplate(templateId, publishNumber);

            return new ServiceResult<bool>(result);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<TemplateEnvironments>> GetCurrentPublishedEnvironment(int templateId,
            int version)
        {
            var result = await templateDataService.GetCurrentPublishedEnvironment(templateId, version);

            return new ServiceResult<TemplateEnvironments>(result);
        }

        /// <inheritdoc />
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

            var result = await templateDataService.UpdateTemplateCommit(templateCommitModel);

            return new ServiceResult<bool>(result);
        }

    }
}

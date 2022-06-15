using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.VersionControl.Interfaces.DataLayer;
using Api.Modules.VersionControl.Interfaces;
using Api.Modules.VersionControl.Models;
using GeeksCoreLibrary.Core.Enums;

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
        public async Task<ServiceResult<Dictionary<int, int>>> GetTemplatesWithLowerVersionAsync(int templateId, int version)
        {
            var result = await templateDataService.GetTemplatesWithLowerVersionAsync(templateId, version);

            return new ServiceResult<Dictionary<int, int>>(result);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> CreateNewTemplateCommitAsync(TemplateCommitModel templateCommitModel)
        {

            bool isTest = false;
            bool isAcceptatie = false;
            bool isLive = false;

            if (templateCommitModel.Enviornment == Environments.Live)
            {
                isTest = true;
                isAcceptatie = true;
                isLive = true;
            }
            else if (templateCommitModel.Enviornment == Environments.Acceptance)
            {
                isAcceptatie = true;
                isTest = true;
            }
            else if (templateCommitModel.Enviornment == Environments.Test)
            {
                isTest = true;
            }

            templateCommitModel.IsTest = isTest;
            templateCommitModel.IsAcceptance = isAcceptatie;
            templateCommitModel.IsLive = isLive;

            var result = await templateDataService.CreateNewTemplateCommitAsync(templateCommitModel);

            return new ServiceResult<bool>(result);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> UpdatePublishEnvironmentTemplateAsync(int templateId, int publishNumber)
        {
            var result = await templateDataService.UpdatePublishEnvironmentTemplateAsync(templateId, publishNumber);

            return new ServiceResult<bool>(result);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<TemplateEnvironments>> GetCurrentPublishedEnvironmentAsync(int templateId,
            int version)
        {
            var result = await templateDataService.GetCurrentPublishedEnvironmentAsync(templateId, version);

            return new ServiceResult<TemplateEnvironments>(result);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> UpdateTemplateCommitAsync(TemplateCommitModel templateCommitModel)
        {
            bool isTest = false;
            bool isAcceptatie = false;
            bool isLive = false;

            if (templateCommitModel.Enviornment == Environments.Live)
            {
                isTest = true;
                isAcceptatie = true;
                isLive = true;
            }
            else if (templateCommitModel.Enviornment == Environments.Acceptance)
            {
                isAcceptatie = true;
                isTest = true;
            }
            else if (templateCommitModel.Enviornment == Environments.Test)
            {
                isTest = true;
            }

            templateCommitModel.IsTest = isTest;
            templateCommitModel.IsAcceptance = isAcceptatie;
            templateCommitModel.IsLive = isLive;

            var result = await templateDataService.UpdateTemplateCommitAsync(templateCommitModel);

            return new ServiceResult<bool>(result);
        }

    }
}

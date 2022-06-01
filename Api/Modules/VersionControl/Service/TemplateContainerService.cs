using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.VersionControl.Interfaces.DataLayer;
using Api.Modules.VersionControl.Interfaces;

namespace Api.Modules.VersionControl.Service
{
    public class TemplateContainerService : ITemplateContainerService
    {

        private readonly ITemplateContainerDataService templateDataService;

        public TemplateContainerService(ITemplateContainerDataService templateDataService)
        {
            this.templateDataService = templateDataService;
        }

        public async Task<ServiceResult<Dictionary<int, int>>> GetTemplatesWithLowerVersion(int templateId, int version)
        {
            var result = await templateDataService.GetTemplatesWithLowerVersion(templateId, version);

            return new ServiceResult<Dictionary<int, int>>(result);
        }
    }
}

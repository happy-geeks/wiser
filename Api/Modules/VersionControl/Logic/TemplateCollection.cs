using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.VersionControl.Interfaces.DataLayer;

namespace Api.Modules.VersionControl.Logic
{
    public class TemplateCollection
    {
        // : ITemplateContainerDataService
        private List<Template> templates { get; set; }

        private ITemplateContainerDataService templateDataService;

        public TemplateCollection(ITemplateContainerDataService templateDataService)
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

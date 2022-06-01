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
    }
}

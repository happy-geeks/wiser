using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Modules.VersionControl.Interfaces.DataLayer
{
    public interface ITemplateContainerDataService
    {
        Task<Dictionary<int, int>> GetTemplatesWithLowerVersion(int templateId, int version);
    }
}

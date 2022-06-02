using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Modules.Templates.Models.Template;
using Api.Modules.VersionControl.Models;

namespace Api.Modules.VersionControl.Interfaces.DataLayer
{
    public interface IDynamicContentDataServiceVersionControl
    {
        Task<DynamicContentModel> GetDynamicContent(int contentId, int version);
        Task<bool> CreateNewDynamicContentCommit(DynamicContentCommitModel dynamicContentCommitModel);

        Task<Dictionary<int, int>> GetDynamicContentEnvironmentsAsync(int dynamicContentId);

        Task<int> UpdateDynamicContentPublishedEnvironmentAsync(int dynamicContentId, Dictionary<int, int> publishModel, PublishLogModel publishLog, string username);

        Task<Dictionary<int, int>> GetDynamicContentWithLowerVersion(int templateId, int version);
    }
}

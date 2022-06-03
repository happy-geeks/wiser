using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Modules.Templates.Models.Template;
using Api.Modules.VersionControl.Models;

namespace Api.Modules.VersionControl.Interfaces.DataLayer
{
    public interface IDynamicContentDataServiceVersionControl
    {
        /// <summary>
        /// Gets a dynamic content item with the id and version.
        /// </summary>
        /// <param name="contentId">The ID of the dynamic content item</param>
        /// <param name="version">The version of the dynamic content item</param>
        /// <returns>A model with te data of the dynamic content item</returns>
        Task<DynamicContentModel> GetDynamicContent(int contentId, int version);

        /// <summary>
        /// Creates new dynamic content item that is linked to the commit
        /// </summary>
        /// <param name="dynamicContentCommitModel">Contains the data of the dynamic content and commit that will be used to add it to the database</param>
        /// <returns></returns>
        Task<bool> CreateNewDynamicContentCommit(DynamicContentCommitModel dynamicContentCommitModel);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dynamicContentId"></param>
        /// <returns></returns>
        Task<Dictionary<int, int>> GetDynamicContentEnvironmentsAsync(int dynamicContentId);

        Task<int> UpdateDynamicContentPublishedEnvironmentAsync(int dynamicContentId, Dictionary<int, int> publishModel, PublishLogModel publishLog, string username);

        Task<Dictionary<int, int>> GetDynamicContentWithLowerVersion(int templateId, int version);
    }
}

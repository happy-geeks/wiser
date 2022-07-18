using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Modules.Templates.Models.Template;
using Api.Modules.VersionControl.Models;

namespace Api.Modules.VersionControl.Interfaces.DataLayer
{
    /// <summary>
    /// Data service for handeling data related to the dynamic content items in the version control model.
    /// </summary>
    public interface IDynamicContentDataServiceVersionControl
    {
        /// <summary>
        /// Gets a dynamic content item with the id and version.
        /// </summary>
        /// <param name="contentId">The ID of the dynamic content item.</param>
        /// <param name="version">The version of the dynamic content item.</param>
        /// <returns>A model with te data of the dynamic content item.</returns>
        Task<DynamicContentModel> GetDynamicContentAsync(int contentId, int version);

        /// <summary>
        /// Creates new dynamic content item that is linked to the commit.
        /// </summary>
        /// <param name="dynamicContentCommitModel">Contains the data of the dynamic content and commit that will be used to add it to the database.</param>
        /// <returns></returns>
        Task CreateNewDynamicContentCommitAsync(DynamicContentCommitModel dynamicContentCommitModel);

        /// <summary>
        /// Gets all the versions of a specific dynamic content item with the published environments.
        /// </summary>
        /// <param name="dynamicContentId">The id of the dynamic content item.</param>
        /// <returns>Returns a dictionary with the version of the dyanmic content and the published environment.</returns>
        Task<Dictionary<int, int>> GetDynamicContentEnvironmentsAsync(int dynamicContentId);

        /// <summary>
        /// Updates the published envirionment of the dynamic content.
        /// </summary>
        /// <param name="dynamicContentId">The id of the dynamic content item to change the published environment from.</param>
        /// <param name="publishModel">The model with what you want to publish.</param>
        /// <param name="publishLog">The data of the publish log.</param>
        /// <param name="username">The username of the person that made the change.</param>
        /// <returns></returns>
        Task<int> UpdateDynamicContentPublishedEnvironmentAsync(int dynamicContentId, Dictionary<int, int> publishModel, PublishLogModel publishLog, string username);

        /// <summary>
        /// Gets the dynamic content items that are of a lower version.
        /// </summary>
        /// <param name="dynamicContent">The id of the dynamic conent id.</param>
        /// <param name="version">The version of the dynamic content.</param>
        /// <returns>Returns a dictonary with the dynamic content items that have a lower version.</returns>
        Task<Dictionary<int, int>> GetDynamicContentWithLowerVersionAsync(int dynamicContent, int version);
    }
}

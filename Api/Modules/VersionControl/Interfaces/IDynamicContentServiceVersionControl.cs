using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.Templates.Models.Other;
using Api.Modules.VersionControl.Models;

namespace Api.Modules.VersionControl.Interfaces
{

    /// <summary>
    /// A service for handeling data related to the dynamic content items in the version control model.
    /// </summary>
    public interface IDynamicContentServiceVersionControl
    {
        /// <summary>
        /// Gets a dynamic content item with the id and version.
        /// </summary>
        /// <param name="contentId">The ID of the dynamic content item.</param>
        /// <param name="version">The version of the dynamic content item.</param>
        /// <returns>A model with te data of the dynamic content item.</returns>
        Task<ServiceResult<DynamicContentModel>> GetDynamicContentAsync(int contentId, int version);

        /// <summary>
        /// Creates new dynamic content item that is linked to the commit.
        /// </summary>
        /// <param name="dynamicContentCommitModel">Contains the data of the dynamic content and commit that will be used to add it to the database.</param>
        /// <returns></returns>
        Task<ServiceResult<bool>> CreateNewDynamicContentCommitAsync(DynamicContentCommitModel dynamicContentCommitModel);

        /// <summary>
        /// Gets all the versions of a specific dynamic content item with the published environments.
        /// </summary>
        /// <param name="dynamicContentId">The id of the dynamic content item.</param>
        /// <returns>Returns a dictionary with the version of the dyanmic content and the published environment.</returns>
        Task<ServiceResult<PublishedEnvironmentModel>> GetDynamicContentEnvironmentsAsync(int dynamicContentId);

        /// <summary>
        /// Publishes the dynamic content item to the given environment.
        /// </summary>
        /// <param name="identity">The identity of the person that made the change.</param>
        /// <param name="dynamicContentId">The id of the dynamic content.</param>
        /// <param name="version">The ersion of the dynamic content.</param>
        /// <param name="environment">The environment to publish to.</param>
        /// <param name="currentPublished">A model with the currently published dynamic content items.</param>
        /// <returns></returns>
        Task<ServiceResult<int>> PublishDynamicContentToEnvironmentAsync(ClaimsIdentity identity, int dynamicContentId, int version, string environment, PublishedEnvironmentModel currentPublished);

        /// <summary>
        /// Gets the dynamic content items that are of a lower version.
        /// </summary>
        /// <param name="dynamicContent">The id of the dynamic conent id.</param>
        /// <param name="version">The version of the dynamic content.</param>
        /// <returns>Returns a dictonary with the dynamic content items that have a lower version.</returns>
        Task<ServiceResult<Dictionary<int, int>>> GetDynamicContentWithLowerVersionAsync(int dynamicContent, int version);

    }
}

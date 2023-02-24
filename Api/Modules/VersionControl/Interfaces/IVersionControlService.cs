using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.VersionControl.Models;

namespace Api.Modules.VersionControl.Interfaces
{
    /// <summary>
    /// A service for handeling general data of the version control model.
    /// </summary>
    public interface IVersionControlService
    {
        /// <summary>
        /// Gets all the published templates.
        /// </summary>
        /// <returns>Returns a dictionary with all the templates that are currently published. The first value will be the template id and the second one is the version.</returns>
        Task<ServiceResult<Dictionary<int,int>>> GetPublishedTemplateIdAndVersionAsync();

        /// <summary>
        /// Creates a log with the changes made to the template.
        /// </summary>
        /// <param name="templateId">The id of the template.</param>
        /// <param name="version">The version of the template.</param>
        /// <returns></returns>
        Task<ServiceResult<bool>> CreatePublishLogAsync(int templateId, int version);

        /// <summary>
        /// Gets all the templates from a specific commit.
        /// </summary>
        /// <param name="commitId">The id of the commit.</param>
        /// <returns>Returns a list with templates.</returns>
        Task<ServiceResult<List<TemplateCommitModel>>> GetTemplatesFromCommitAsync(int commitId);

        /// <summary>
        /// Gets the dynamic content items from the given commit.
        /// </summary>
        /// <param name="commitId">The id of the commit.</param>
        /// <returns>Returns a list of dynamic contnet items.</returns>
        Task<ServiceResult<List<DynamicContentCommitModel>>> GetDynamicContentFromCommitAsync(int commitId);

        /// <summary>
        /// Deploy one or more commits to a branch.
        /// </summary>
        /// <param name="identity">The <see cref="ClaimsIdentity"/> of the authenticated user.</param>
        /// <param name="commitIds">The IDs of the commits to deploy.</param>
        /// <param name="branchId">The ID of the branch to deploy to.</param>
        /// <param name="useTransaction">Optional: Whether to use a database transaction. Set to <see langword="false"/> when you call this via a method that already started it's own transaction. Default value is <see langword="true"/>.</param>
        Task<ServiceResult<bool>> DeployToBranchAsync(ClaimsIdentity identity, List<int> commitIds, int branchId, bool useTransaction = true);
    }
}

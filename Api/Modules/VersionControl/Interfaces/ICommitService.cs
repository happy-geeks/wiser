using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.VersionControl.Models;
using GeeksCoreLibrary.Core.Enums;

namespace Api.Modules.VersionControl.Interfaces;

/// <summary>
/// A service for handling data related to the commit items in the version control model.
/// </summary>
public interface ICommitService
{
    /// <summary>
    /// Creates new commit item in the database.
    /// </summary>
    /// <param name="data">The data of the commit</param>
    /// <param name="identity">The authenticated user data.</param>
    /// <returns>Returns a model of the commit.</returns>
    Task<ServiceResult<CommitModel>> CreateAndOrDeployCommitAsync(CommitModel data, ClaimsIdentity identity);

    /// <summary>
    /// Deploy a commit to an environment. This will only mark the commit as deployed, it will not actually deploy the commit.
    /// </summary>
    /// <param name="id">The ID of the commit to deploy.</param>
    /// <param name="environment">The environment to deploy to. The commit will always also be deployed to lower environments, so if you deploy to acceptance for example, it will also be deployed to development and test, if it wasn't already.</param>
    /// <param name="identity">The authenticated user data.</param>
    Task<ServiceResult<bool>> LogDeploymentOfCommitAsync(int id, Environments environment, ClaimsIdentity identity);

    /// <summary>
    /// Get all templates that have uncommitted changes.
    /// </summary>
    Task<ServiceResult<List<TemplateCommitModel>>> GetTemplatesToCommitAsync();

    /// <summary>
    /// Get all dynamic content that have uncommitted changes.
    /// </summary>
    /// <returns>A list of <see cref="DynamicContentCommitModel"/>.</returns>
    Task<ServiceResult<List<DynamicContentCommitModel>>> GetDynamicContentsToCommitAsync();

    /// <summary>
    /// Get the history of commits. You must set at least one of the parameters to true.
    /// </summary>
    /// <param name="includeCompleted">Whether to include completed commits.</param>
    /// <param name="includeIncompleted">Whether to include commits that haven't been completed yet.</param>
    /// <returns>A list of <see cref="CommitModel"/>.</returns>
    Task<ServiceResult<List<CommitModel>>> GetCommitHistoryAsync(bool includeCompleted, bool includeIncompleted);
}
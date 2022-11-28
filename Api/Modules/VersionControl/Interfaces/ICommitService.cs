using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.VersionControl.Models;

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
    /// Completes the commit.
    /// </summary>
    /// <param name="commitId">The id of the commit.</param>
    /// <param name="commitCompleted">The bool that will set the commit to completed.</param>
    Task<ServiceResult<bool>> CompleteCommitAsync(int commitId, bool commitCompleted);

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
    /// Get all commits that haven't been completed yet,
    /// </summary>
    /// <returns>A list of <see cref="CommitModel"/>.</returns>
    Task<ServiceResult<List<CommitModel>>> GetNotCompletedCommitsAsync();
}
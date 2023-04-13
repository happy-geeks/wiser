using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Modules.VersionControl.Models;
using GeeksCoreLibrary.Core.Enums;

namespace Api.Modules.VersionControl.Interfaces.DataLayer;

/// <summary>
/// Data service for handling data related to the commit items in the version control model.
/// </summary>
public interface ICommitDataService
{
    /// <summary>
    /// Gets a single commit.
    /// </summary>
    /// <param name="id">The ID of the commit.</param>
    /// <returns>A <see cref="CommitModel"/> with the result.</returns>
    Task<CommitModel> GetCommitAsync(int id);

    /// <summary>
    /// Creates new commit item in the database.
    /// </summary>
    /// <param name="data">The data for the commit.</param>
    /// <returns>Returns a model of the commit.</returns>
    Task<CommitModel> CreateCommitAsync(CommitModel data);

    /// <summary>
    /// Deploy a commit to an environment. This will only mark the commit as deployed, it will not actually deploy the commit.
    /// </summary>
    /// <param name="id">The ID of the commit to deploy.</param>
    /// <param name="environment">The environment to deploy to. The commit will always also be deployed to lower environments, so if you deploy to acceptance for example, it will also be deployed to development and test, if it wasn't already.</param>
    /// <param name="username">The name of the user that deployed the commit.</param>
    Task LogDeploymentOfCommitAsync(int id, Environments environment, string username);

    /// <summary>
    /// Get all templates that have uncommitted changes.
    /// </summary>
    Task<List<TemplateCommitModel>> GetTemplatesToCommitAsync();

    /// <summary>
    /// Get all dynamic contents that have uncommitted changes.
    /// </summary>
    Task<List<DynamicContentCommitModel>> GetDynamicContentsToCommitAsync();

    /// <summary>
    /// Get the history of commits. You must set at least one of the parameters to true.
    /// </summary>
    /// <param name="includeCompleted">Whether to include completed commits.</param>
    /// <param name="includeIncompleted">Whether to include commits that haven't been completed yet.</param>
    /// <returns>A list of <see cref="CommitModel"/>.</returns>
    Task<List<CommitModel>> GetCommitHistoryAsync(bool includeCompleted, bool includeIncompleted);
}
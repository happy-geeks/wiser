using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Modules.VersionControl.Models;

namespace Api.Modules.VersionControl.Interfaces.DataLayer
{
    /// <summary>
    /// Data service for handeling data related to the commit items in the version control model.
    /// </summary>
    public interface ICommitDataService
    {
        /// <summary>
        /// Creates new commit item in the database.
        /// </summary>
        /// <param name="commitMessage"></param>
        /// <param name="username"></param>
        /// <returns>Returns a model of the commit.</returns>
        Task<CreateCommitModel> CreateCommitAsync(string commitMessage, string username);

        /// <summary>
        /// Completes the commmit
        /// </summary>
        /// <param name="commitId">The id of the commit</param>
        /// <param name="commitCompleted">Bool that sets the commit to completed</param>
        Task CompleteCommitAsync(int commitId, bool commitCompleted);

        /// <summary>
        /// Get all templates that have uncommitted changes.
        /// </summary>
        Task<List<TemplateCommitModel>> GetTemplatesToCommitAsync();
    }
}

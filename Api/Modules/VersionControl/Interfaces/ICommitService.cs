using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.VersionControl.Models;
using System.Security.Claims;

namespace Api.Modules.VersionControl.Interfaces
{
    /// <summary>
    /// A service for handeling data related to the commit items in the version control model.
    /// </summary>
    public interface ICommitService
    {
        /// <summary>
        /// Creates new commit item in the database.
        /// </summary>
        /// <returns>Returns a model of the commit.</returns>
        Task<ServiceResult<CreateCommitModel>> CreateCommitAsync(string commitMessage, ClaimsIdentity identity);

        /// <summary>
        /// Completes the commit
        /// </summary>
        /// <param name="commitId">The id of the commit</param>
        /// <param name="commitCompleted">The bool that will set the commit to completed</param>
        /// <returns></returns>
        Task<ServiceResult<bool>> CompleteCommit(int commitId, bool commitCompleted);

        /// <summary>
        /// Get all templates that have uncommitted changes.
        /// </summary>
        Task<ServiceResult<List<TemplateCommitModel>>> GetTemplatesToCommitAsync();
    }
}

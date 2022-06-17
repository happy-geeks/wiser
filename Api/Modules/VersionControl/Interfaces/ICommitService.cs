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
        /// <param name="commitModel"></param>
        /// <returns>Returns a model of the commit.</returns>
        Task<ServiceResult<CreateCommitModel>> CreateCommitAsync(string commitMessage, ClaimsIdentity identity);


        /// <summary>
        /// Creates new commit item in the database.
        /// </summary>
        /// <param name="templateId">The ID of the template.</param>
        /// <param name="commitItemModel">The data from the commit.</param>
        /// <returns></returns>
        Task<ServiceResult<bool>> CreateCommitItemAsync(int templateId, CommitItemModel commitItemModel);

        /// <summary>
        /// Gets the most recently added commit.
        /// </summary>
        /// <returns>Returns a model of the commit.</returns>
        Task<ServiceResult<CreateCommitModel>> GetCommitAsync();
    }
}

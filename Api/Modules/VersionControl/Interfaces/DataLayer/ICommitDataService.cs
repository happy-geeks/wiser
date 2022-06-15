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
        /// <param name="commitModel"></param>
        /// <returns>Returns a model of the commit.</returns>
        Task<CreateCommitModel> CreateCommit(CreateCommitModel commitModel);

        /// <summary>
        /// Creates new commit item in the database.
        /// </summary>
        /// <param name="templateId">The ID of the template.</param>
        /// <param name="commitItemModel">The data from the commit.</param>
        /// <returns></returns>
        Task<bool> CreateCommitItem(int templateId, CommitItemModel commitItemModel);

        /// <summary>
        /// Gets the most recently added commit.
        /// </summary>
        /// <returns>Returns a model of the commit.</returns>
        Task<CreateCommitModel> GetCommit();
    }
}

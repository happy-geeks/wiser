using System.Threading.Tasks;
using Api.Modules.VersionControl.Models;

namespace Api.Modules.VersionControl.Interfaces.DataLayer
{
    public interface ICommitDataService
    {
        /// <summary>
        /// Creates new commit item in the database
        /// </summary>
        /// <param name="commitModel"></param>
        /// <returns></returns>
        Task<CreateCommitModel> CreateCommit(CreateCommitModel commitModel);

        /// <summary>
        /// Creates new commit item in the database.
        /// </summary>
        /// <param name="templateId">The ID of the template.</param>
        /// <param name="commitItemModel">The data from the commit.</param>
        /// <returns></returns>
        Task<bool> CreateCommitItem(int templateId, CommitItemModel commitItemModel);

        /// <summary>
        /// Gets the most recently added commit
        /// </summary>
        /// <returns></returns>
        Task<CreateCommitModel> GetCommit();
    }
}

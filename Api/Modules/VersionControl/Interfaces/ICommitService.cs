using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.VersionControl.Models;

namespace Api.Modules.VersionControl.Interfaces
{
    public interface ICommitService
    {
        /// <summary>
        /// Creates new Commit in the database
        /// </summary>
        /// <param name="commitModel">The data of the commit</param>
        /// <returns></returns>
        Task<ServiceResult<CreateCommitModel>> CreateCommit(CreateCommitModel commitModel);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="templateId"></param>
        /// <param name="commitItemModel"></param>
        /// <returns></returns>
        Task<ServiceResult<bool>> CreateCommitItem(int templateId, CommitItemModel commitItemModel);

        Task<ServiceResult<CreateCommitModel>> GetCommit();
    }
}

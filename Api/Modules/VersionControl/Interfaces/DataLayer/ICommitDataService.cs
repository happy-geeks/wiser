using System.Threading.Tasks;
using Api.Modules.VersionControl.Models;

namespace Api.Modules.VersionControl.Interfaces.DataLayer
{
    public interface ICommitDataService
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="templateId"></param>
        /// <param name="commitModel"></param>
        /// <returns></returns>
        Task<CreateCommitModel> CreateCommit(CreateCommitModel commitModel);

        Task<bool> CreateCommitItem(int templateId, CommitItemModel commitItemModel);

        Task<CreateCommitModel> GetCommit();
    }
}

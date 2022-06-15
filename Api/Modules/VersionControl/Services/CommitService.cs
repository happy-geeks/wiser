using System;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.VersionControl.Interfaces;
using Api.Modules.VersionControl.Interfaces.DataLayer;
using Api.Modules.VersionControl.Models;

namespace Api.Modules.VersionControl.Service
{
    public class CommitService : ICommitService
    {
        private readonly ICommitDataService commitDataService;

        public CommitService(ICommitDataService commitDataService)
        {
            this.commitDataService = commitDataService;
        }

        /// <inheritdoc />
        public async Task<ServiceResult<CreateCommitModel>> CreateCommit(CreateCommitModel commitModel)
        {
            if (String.IsNullOrWhiteSpace(commitModel.Description))
            {
                throw new ArgumentException("No commit message!");
            }


            var result = await commitDataService.CreateCommit(commitModel);

            return new ServiceResult<CreateCommitModel>(result);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> CreateCommitItem(int templateId, CommitItemModel commitItemModel)
        {

            var result = await commitDataService.CreateCommitItem(templateId, commitItemModel);

            return new ServiceResult<bool>(result);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<CreateCommitModel>> GetCommit()
        {
            var result = await commitDataService.GetCommit();

            return new ServiceResult<CreateCommitModel>(result);
        }
    }
}

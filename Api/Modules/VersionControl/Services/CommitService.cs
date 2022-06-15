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
        public async Task<ServiceResult<CreateCommitModel>> CreateCommitAsync(CreateCommitModel commitModel)
        {
            if (String.IsNullOrWhiteSpace(commitModel.Description))
            {
                throw new ArgumentException("No commit message!");
            }

            var result = await commitDataService.CreateCommitAsync(commitModel);

            return new ServiceResult<CreateCommitModel>(result);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> CreateCommitItemAsync(int templateId, CommitItemModel commitItemModel)
        {

            var result = await commitDataService.CreateCommitItemAsync(templateId, commitItemModel);

            return new ServiceResult<bool>(result);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<CreateCommitModel>> GetCommitAsync()
        {
            var result = await commitDataService.GetCommitAsync();

            return new ServiceResult<CreateCommitModel>(result);
        }
    }
}

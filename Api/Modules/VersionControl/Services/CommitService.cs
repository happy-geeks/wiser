using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Helpers;
using Api.Core.Services;
using Api.Modules.VersionControl.Interfaces;
using Api.Modules.VersionControl.Interfaces.DataLayer;
using Api.Modules.VersionControl.Models;

namespace Api.Modules.VersionControl.Services
{
    /// <inheritdoc cref="ICommitService" />
    public class CommitService : ICommitService
    {
        private readonly ICommitDataService commitDataService;
        /// <summary>
        /// Creates a new instance of <see cref="CommitService"/>.
        /// </summary>
        public CommitService(ICommitDataService commitDataService)
        {
            this.commitDataService = commitDataService;
        }


        /// <inheritdoc />
        public async Task<ServiceResult<CreateCommitModel>> CreateCommitAsync(string commitMessage, ClaimsIdentity identity)
        {
            if (String.IsNullOrWhiteSpace(commitMessage))
            {
                throw new ArgumentException("No commit message!");
            }

            var result = await commitDataService.CreateCommitAsync(commitMessage, IdentityHelpers.GetUserName(identity));

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

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> CompleteCommit(int commitId, bool commitCompleted)
        {
            var result = await commitDataService.CompleteCommit(commitId, commitCompleted);

            return new ServiceResult<bool>(result);
        }

    }
}

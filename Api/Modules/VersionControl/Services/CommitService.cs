using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.VersionControl.Interfaces;
using Api.Modules.VersionControl.Interfaces.DataLayer;
using Api.Modules.VersionControl.Models;

namespace Api.Modules.VersionControl.Services;

/// <inheritdoc cref="ICommitService" />
public class CommitService : ICommitService
{
    private readonly ICommitDataService commitDataService;
    
    /// <summary>
    ///     Creates a new instance of <see cref="CommitService"/>.
    /// </summary>
    /// <param name="commitDataService"></param>
    public CommitService(ICommitDataService commitDataService)
    {
        this.commitDataService = commitDataService;
    }
    
    public Task<ServiceResult<CreateCommitModel>> CreateCommitAsync(string commitMessage, ClaimsIdentity identity)
    {
        throw new System.NotImplementedException();
    }

    public Task<ServiceResult<bool>> CompleteCommit(int commitId, bool commitCompleted)
    {
        throw new System.NotImplementedException();
    }

    public async Task<ServiceResult<List<TemplateCommitModel>>> GetTemplatesToCommitAsync()
    {
        var results = await commitDataService.GetTemplatesToCommitAsync();

        return new ServiceResult<List<TemplateCommitModel>>(results);
    }
}
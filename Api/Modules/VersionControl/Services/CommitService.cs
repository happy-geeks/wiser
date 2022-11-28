using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Helpers;
using Api.Core.Services;
using Api.Modules.Branches.Interfaces;
using Api.Modules.Templates.Interfaces;
using Api.Modules.VersionControl.Interfaces;
using Api.Modules.VersionControl.Interfaces.DataLayer;
using Api.Modules.VersionControl.Models;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using Microsoft.Extensions.Logging;

namespace Api.Modules.VersionControl.Services;

/// <inheritdoc cref="ICommitService" />
public class CommitService : ICommitService, IScopedService
{
    private readonly ICommitDataService commitDataService;
    private readonly ITemplatesService templatesService;
    private readonly IDynamicContentService dynamicContentService;
    private readonly IDatabaseConnection databaseConnection;
    private readonly IDatabaseHelpersService databaseHelpersService;
    private readonly IBranchesService branchesService;
    private readonly IVersionControlService versionControlService;
    private readonly ILogger<CommitService> logger;

    /// <summary>
    /// Creates a new instance of <see cref="CommitService"/>.
    /// </summary>
    public CommitService(ICommitDataService commitDataService, ITemplatesService templatesService, IDynamicContentService dynamicContentService, IDatabaseConnection databaseConnection, IDatabaseHelpersService databaseHelpersService, IBranchesService branchesService, IVersionControlService versionControlService, ILogger<CommitService> logger)
    {
        this.commitDataService = commitDataService;
        this.templatesService = templatesService;
        this.dynamicContentService = dynamicContentService;
        this.databaseConnection = databaseConnection;
        this.databaseHelpersService = databaseHelpersService;
        this.branchesService = branchesService;
        this.versionControlService = versionControlService;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task<ServiceResult<CommitModel>> CreateAndOrDeployCommitAsync(CommitModel data, ClaimsIdentity identity)
    {
        if (data.Id == 0 && String.IsNullOrWhiteSpace(data.Description))
        {
            return new ServiceResult<CommitModel>
            {
                StatusCode = HttpStatusCode.BadRequest,
                ErrorMessage = "Please enter a description"
            };
        }

        if (data.Id == 0 && (data.Templates == null || !data.Templates.Any()) && (data.DynamicContents == null || !data.DynamicContents.Any()))
        {
            return new ServiceResult<CommitModel>
            {
                StatusCode = HttpStatusCode.BadRequest,
                ErrorMessage = "Please select at least one template or dynamic content to commit"
            };
        }

        await databaseConnection.BeginTransactionAsync();

        try
        {
            databaseConnection.ClearParameters();

            data.AddedBy = IdentityHelpers.GetUserName(identity, true);
            data.AddedOn = DateTime.Now;

            var result = data.Id == 0 ? await commitDataService.CreateCommitAsync(data) : await commitDataService.GetCommitAsync(data.Id);
            if (result == null)
            {
                return new ServiceResult<CommitModel>
                {
                    StatusCode = HttpStatusCode.NotFound,
                    ErrorMessage = $"Commit with ID '{data.Id}' not found."
                };
            }

            if (result.Templates != null)
            {
                foreach (var template in result.Templates)
                {
                    var currentPublished = await templatesService.GetTemplateEnvironmentsAsync(template.TemplateId);
                    if (currentPublished.StatusCode != HttpStatusCode.OK)
                    {
                        throw new Exception($"Could not get environments of template '{template.TemplateId}'. Error was: {currentPublished.ErrorMessage}");
                    }

                    await templatesService.PublishToEnvironmentAsync(identity, template.TemplateId, template.Version, data.Environment, currentPublished.ModelObject);
                }
            }

            if (result.DynamicContents != null)
            {
                foreach (var dynamicContent in result.DynamicContents)
                {
                    var currentPublished = await dynamicContentService.GetEnvironmentsAsync(dynamicContent.DynamicContentId);
                    if (currentPublished.StatusCode != HttpStatusCode.OK)
                    {
                        throw new Exception($"Could not get environments of dynamic content '{dynamicContent.DynamicContentId}'. Error was: {currentPublished.ErrorMessage}");
                    }

                    await dynamicContentService.PublishToEnvironmentAsync(identity, dynamicContent.DynamicContentId, dynamicContent.Version, data.Environment, currentPublished.ModelObject);
                }
            }

            if (data.Environment == Environments.Live)
            {
                // If this commit went to live, then always deploy the commit to all branches.
                var allBranches = await branchesService.GetAsync(identity);
                if (allBranches.ModelObject != null)
                {
                    foreach (var branch in allBranches.ModelObject)
                    {
                        try
                        {
                            await versionControlService.DeployToBranchAsync(identity, new List<int> {result.Id}, branch.Id, false);
                        }
                        catch (Exception exception)
                        {
                            // Don't return errors to user, because branches will sometimes be half deleted by developers and we don't want to prevent someone from committing if that's the case.
                            logger.LogWarning(exception, $"Error while trying to deploy commit '{result.Id}' to branch '{branch.Id}'.");
                        }
                    }
                }

                // Mark the commit as completed if it's committed to live.
                await CompleteCommitAsync(result.Id, true);
            }

            await databaseConnection.CommitTransactionAsync();

            return new ServiceResult<CommitModel>(result);
        }
        catch
        {
            await databaseConnection.RollbackTransactionAsync();
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult<bool>> CompleteCommitAsync(int commitId, bool commitCompleted)
    {
        await commitDataService.CompleteCommitAsync(commitId, commitCompleted);
        return new ServiceResult<bool>(true);
    }

    /// <inheritdoc />
    public async Task<ServiceResult<List<TemplateCommitModel>>> GetTemplatesToCommitAsync()
    {
        await databaseHelpersService.CheckAndUpdateTablesAsync(new List<string>
        {
            WiserTableNames.WiserCommit,
            WiserTableNames.WiserCommitTemplate,
            WiserTableNames.WiserCommitDynamicContent
        });
        
        var results = await commitDataService.GetTemplatesToCommitAsync();

        return new ServiceResult<List<TemplateCommitModel>>(results);
    }

    /// <inheritdoc />
    public async Task<ServiceResult<List<DynamicContentCommitModel>>> GetDynamicContentsToCommitAsync()
    {
        await databaseHelpersService.CheckAndUpdateTablesAsync(new List<string>
        {
            WiserTableNames.WiserCommit,
            WiserTableNames.WiserCommitTemplate,
            WiserTableNames.WiserCommitDynamicContent
        });

        var results = await commitDataService.GetDynamicContentsToCommitAsync();

        return new ServiceResult<List<DynamicContentCommitModel>>(results);
    }

    /// <inheritdoc />
    public async Task<ServiceResult<List<CommitModel>>> GetNotCompletedCommitsAsync()
    {
        await databaseHelpersService.CheckAndUpdateTablesAsync(new List<string>
        {
            WiserTableNames.WiserCommit,
            WiserTableNames.WiserCommitTemplate,
            WiserTableNames.WiserCommitDynamicContent
        });
        
        var results = await commitDataService.GetNotCompletedCommitsAsync();

        return new ServiceResult<List<CommitModel>>(results);
    }
}
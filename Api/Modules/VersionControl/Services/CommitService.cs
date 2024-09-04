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
using Api.Modules.Templates.Interfaces.DataLayer;
using Api.Modules.Templates.Models.Other;
using Api.Modules.VersionControl.Enums;
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
    private readonly IReviewService reviewService;
    private readonly ITemplateDataService templateDataService;
    private readonly IDynamicContentDataService dynamicContentDataService;

    /// <summary>
    /// Creates a new instance of <see cref="CommitService"/>.
    /// </summary>
    public CommitService(ICommitDataService commitDataService, ITemplatesService templatesService,
        IDynamicContentService dynamicContentService, IDatabaseConnection databaseConnection,
        IDatabaseHelpersService databaseHelpersService, IBranchesService branchesService,
        IVersionControlService versionControlService, ILogger<CommitService> logger,
        IReviewService reviewService, ITemplateDataService templateDataService,
        IDynamicContentDataService dynamicContentDataService)
    {
        this.commitDataService = commitDataService;
        this.templatesService = templatesService;
        this.dynamicContentService = dynamicContentService;
        this.databaseConnection = databaseConnection;
        this.databaseHelpersService = databaseHelpersService;
        this.branchesService = branchesService;
        this.versionControlService = versionControlService;
        this.logger = logger;
        this.reviewService = reviewService;
        this.templateDataService = templateDataService;
        this.dynamicContentDataService = dynamicContentDataService;
    }

    /// <inheritdoc />
    public async Task<ServiceResult<CommitModel>> CreateAndOrDeployCommitAsync(CommitModel data, ClaimsIdentity identity)
    {
        var isNewCommit = data.Id == 0;
        if (isNewCommit && String.IsNullOrWhiteSpace(data.Description))
        {
            return new ServiceResult<CommitModel>
            {
                StatusCode = HttpStatusCode.BadRequest,
                ErrorMessage = "Please enter a description"
            };
        }

        if (isNewCommit && (data.Templates == null || !data.Templates.Any()) && (data.DynamicContents == null || !data.DynamicContents.Any()))
        {
            return new ServiceResult<CommitModel>
            {
                StatusCode = HttpStatusCode.BadRequest,
                ErrorMessage = "Please select at least one template or dynamic content to commit"
            };
        }

        // Make sure the tables are up-to-date.
        await databaseHelpersService.CheckAndUpdateTablesAsync(new List<string>
        {
            WiserTableNames.WiserCommit,
            WiserTableNames.WiserCommitTemplate,
            WiserTableNames.WiserCommitDynamicContent,
            WiserTableNames.WiserCommitReviews,
            WiserTableNames.WiserCommitReviewRequests,
            WiserTableNames.WiserCommitReviewComments
        });

        // Don't allow commits to live if there are any pending reviews.
        if (data.Environment == Environments.Live && data.ReviewRequestedUsers != null && data.ReviewRequestedUsers.Any())
        {
            return new ServiceResult<CommitModel>
            {
                StatusCode = HttpStatusCode.BadRequest,
                ErrorMessage = "You cannot commit to live if you have requested reviews."
            };
        }

        await databaseConnection.BeginTransactionAsync();

        try
        {
            databaseConnection.ClearParameters();

            data.AddedBy = IdentityHelpers.GetUserName(identity, true);
            data.AddedOn = DateTime.Now;

            var result = isNewCommit ? await commitDataService.CreateCommitAsync(data) : await commitDataService.GetCommitAsync(data.Id);
            if (result == null)
            {
                return new ServiceResult<CommitModel>
                {
                    StatusCode = HttpStatusCode.NotFound,
                    ErrorMessage = $"Commit with ID '{data.Id}' not found."
                };
            }

            // Create a review request.
            if (data.ReviewRequestedUsers != null && data.ReviewRequestedUsers.Any())
            {
                await reviewService.RequestReviewForCommitAsync(identity, result.Id, data.ReviewRequestedUsers);
            }
            else if (!isNewCommit && data.Environment == Environments.Live && result.Review?.Status is ReviewStatuses.Pending or ReviewStatuses.RequestChanges)
            {
                return new ServiceResult<CommitModel>
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessage = "You cannot commit to live until the changes have been approved by the requested code reviewer(s)."
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
                    
                    if (!AllowedToPublishToTargetedEnvironment(template.Version, data.Environment, currentPublished.ModelObject))
                    {
                        continue;
                    }
                    
                    await templatesService.PublishToEnvironmentAsync(identity, template.TemplateId, template.Version, data.Environment, currentPublished.ModelObject);

                    // Create a new version of the template, so that any changes made after this will be done in the new version instead of the published one.
                    await templatesService.CreateNewVersionAsync(template.TemplateId, template.Version);
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

                    if (!AllowedToPublishToTargetedEnvironment(dynamicContent.Version, data.Environment, currentPublished.ModelObject))
                    {
                        continue;
                    }
                    
                    await dynamicContentService.PublishToEnvironmentAsync(identity, dynamicContent.DynamicContentId, dynamicContent.Version, data.Environment, currentPublished.ModelObject);

                    // Create a new version of the component, so that any changes made after this will be done in the new version instead of the published one.
                    await dynamicContentService.CreateNewVersionAsync(dynamicContent.DynamicContentId, dynamicContent.Version);
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
            }

            // Log the deployment of the commit, so that we can see in the history when it was deployed to which environment.
            await LogDeploymentOfCommitAsync(result.Id, data.Environment, identity);

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
    public async Task<ServiceResult<bool>> DeployCommitsAsync(DeployCommitsRequestModel data, ClaimsIdentity identity)
    {
        var commits = new List<CommitModel>();

        foreach (var commitId in data.CommitIds)
        {
            var commit = await commitDataService.GetCommitAsync(commitId);
            if (commit == null)
            {
                return new ServiceResult<bool>
                {
                    StatusCode = HttpStatusCode.NotFound,
                    ErrorMessage = $"Commit with ID '{commitId}' not found."
                };
            }

            commits.Add(commit);
        }

        // Don't allow commits to live if there are any pending reviews.
        if (data.Environment == Environments.Live && commits.Any(commit => commit.Review?.Status is ReviewStatuses.Pending or ReviewStatuses.RequestChanges))
        {
            return new ServiceResult<bool>
            {
                StatusCode = HttpStatusCode.BadRequest,
                ErrorMessage = "You cannot commit to live until the changes have been approved by the requested code reviewer(s)."
            };
        }

        // Get all unique templates from all commits and the highest version of each template.
        var templates = commits.SelectMany(x => x.Templates).GroupBy(x => x.TemplateId).Select(x => new { TemplateId = x.Key, Version = x.Max(y => y.Version) }).ToList();

        // Get all unique dynamic contents from all commits and the highest version of each dynamic content.
        var dynamicContents = commits.SelectMany(x => x.DynamicContents).GroupBy(x => x.DynamicContentId).Select(x => new { DynamicContentId = x.Key, Version = x.Max(y => y.Version) }).ToList();

        // Publish all templates.
        foreach (var template in templates)
        {
            var currentPublished = await templatesService.GetTemplateEnvironmentsAsync(template.TemplateId);
            if (currentPublished.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"Could not get environments of template '{template.TemplateId}'. Error was: {currentPublished.ErrorMessage}");
            }

            if (!AllowedToPublishToTargetedEnvironment(template.Version, data.Environment, currentPublished.ModelObject))
            {
                continue;
            }
            
            await templatesService.PublishToEnvironmentAsync(identity, template.TemplateId, template.Version, data.Environment, currentPublished.ModelObject);

            // Create a new version of the template, so that any changes made after this will be done in the new version instead of the published one.
            await templatesService.CreateNewVersionAsync(template.TemplateId, template.Version);
        }

        // Publish all dynamic contents.
        foreach (var dynamicContent in dynamicContents)
        {
            var currentPublished = await dynamicContentService.GetEnvironmentsAsync(dynamicContent.DynamicContentId);
            if (currentPublished.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"Could not get environments of dynamic content '{dynamicContent.DynamicContentId}'. Error was: {currentPublished.ErrorMessage}");
            }

            if (!AllowedToPublishToTargetedEnvironment(dynamicContent.Version, data.Environment, currentPublished.ModelObject))
            {
                continue;
            }
            
            await dynamicContentService.PublishToEnvironmentAsync(identity, dynamicContent.DynamicContentId, dynamicContent.Version, data.Environment, currentPublished.ModelObject);

            // Create a new version of the component, so that any changes made after this will be done in the new version instead of the published one.
            await dynamicContentService.CreateNewVersionAsync(dynamicContent.DynamicContentId, dynamicContent.Version);
        }

        // Log the deployment of the commits, so that we can see in the history when it was deployed to which environment.
        foreach (var commit in commits)
        {
            await LogDeploymentOfCommitAsync(commit.Id, data.Environment, identity);
        }

        return new ServiceResult<bool>(true)
        {
            StatusCode = HttpStatusCode.NoContent
        };
    }

    /// <inheritdoc />
    public async Task<ServiceResult<bool>> LogDeploymentOfCommitAsync(int id, Environments environment, ClaimsIdentity identity)
    {
        await databaseHelpersService.CheckAndUpdateTablesAsync(new List<string>
        {
            WiserTableNames.WiserCommit
        });

        await commitDataService.LogDeploymentOfCommitAsync(id, environment, IdentityHelpers.GetUserName(identity, true));
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

        // Do any table updates that might be needed.
        await templateDataService.KeepTablesUpToDateAsync();

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

        // Do any table updates that might be needed.
        await dynamicContentDataService.KeepTablesUpToDateAsync();

        var results = await commitDataService.GetDynamicContentsToCommitAsync();

        return new ServiceResult<List<DynamicContentCommitModel>>(results);
    }

    /// <inheritdoc />
    public async Task<ServiceResult<List<CommitModel>>> GetCommitHistoryAsync(bool includeCompleted, bool includeIncompleted)
    {
        await databaseHelpersService.CheckAndUpdateTablesAsync(new List<string>
        {
            WiserTableNames.WiserCommit,
            WiserTableNames.WiserCommitTemplate,
            WiserTableNames.WiserCommitDynamicContent
        });

        var results = await commitDataService.GetCommitHistoryAsync(includeCompleted, includeIncompleted);

        return new ServiceResult<List<CommitModel>>(results);
    }

    /// <summary>
    /// Checks if the version being published is higher than the current version of the targeted environment.
    /// This prevents commits containing older version to override a newer version.
    /// </summary>
    /// <param name="version">The version that will be published to the environment.</param>
    /// <param name="targetEnvironment">The targeted environment to publish to.</param>
    /// <param name="currentPublished">The current published information.</param>
    /// <returns>Returns true if the version is allowed to be published to the targeted environment, otherwise returns false.</returns>
    private static bool AllowedToPublishToTargetedEnvironment(int version, Environments targetEnvironment, PublishedEnvironmentModel currentPublished)
    {
        switch (targetEnvironment)
        {
            case Environments.Development:
                if (version == currentPublished.VersionList.Last())
                {
                    return true;
                }
                break;
            case Environments.Test:
                if (version > currentPublished.TestVersion)
                {
                    return true;
                }
                break;
            case Environments.Acceptance:
                if (version > currentPublished.AcceptVersion)
                {
                    return true;
                }
                break;
            case Environments.Live:
                if (version > currentPublished.LiveVersion)
                {
                    return true;
                }
                break;
        }

        return false;
    }
}
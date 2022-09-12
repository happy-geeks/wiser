using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Helpers;
using Api.Core.Services;
using Api.Modules.Templates.Interfaces;
using Api.Modules.VersionControl.Interfaces;
using Api.Modules.VersionControl.Interfaces.DataLayer;
using Api.Modules.VersionControl.Models;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Modules.Databases.Interfaces;

namespace Api.Modules.VersionControl.Services;

/// <inheritdoc cref="ICommitService" />
public class CommitService : ICommitService, IScopedService
{
    private readonly ICommitDataService commitDataService;
    private readonly ITemplatesService templatesService;
    private readonly IDynamicContentService dynamicContentService;
    private readonly IDatabaseConnection databaseConnection;

    /// <summary>
    /// Creates a new instance of <see cref="CommitService"/>.
    /// </summary>
    public CommitService(ICommitDataService commitDataService, ITemplatesService templatesService, IDynamicContentService dynamicContentService, IDatabaseConnection databaseConnection)
    {
        this.commitDataService = commitDataService;
        this.templatesService = templatesService;
        this.dynamicContentService = dynamicContentService;
        this.databaseConnection = databaseConnection;
    }

    /// <inheritdoc />
    public async Task<ServiceResult<CreateCommitModel>> CreateCommitAsync(CreateCommitModel data, ClaimsIdentity identity)
    {
        if (String.IsNullOrWhiteSpace(data.Description))
        {
            return new ServiceResult<CreateCommitModel>
            {
                StatusCode = HttpStatusCode.BadRequest,
                ErrorMessage = "Please enter a descrption"
            };
        }

        if ((data.Templates == null || !data.Templates.Any()) && (data.DynamicContents == null || !data.DynamicContents.Any()))
        {
            return new ServiceResult<CreateCommitModel>
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

            var result = await commitDataService.CreateCommitAsync(data);

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

            await databaseConnection.CommitTransactionAsync();

            return new ServiceResult<CreateCommitModel>(result);
        }
        catch
        {
            await databaseConnection.RollbackTransactionAsync();
            throw;
        }
    }

    /// <inheritdoc />
    public Task<ServiceResult<bool>> CompleteCommit(int commitId, bool commitCompleted)
    {
        throw new System.NotImplementedException();
    }

    /// <inheritdoc />
    public async Task<ServiceResult<List<TemplateCommitModel>>> GetTemplatesToCommitAsync()
    {
        var results = await commitDataService.GetTemplatesToCommitAsync();

        return new ServiceResult<List<TemplateCommitModel>>(results);
    }

    /// <inheritdoc />
    public async Task<ServiceResult<List<DynamicContentCommitModel>>> GetDynamicContentsToCommitAsync()
    {
        var results = await commitDataService.GetDynamicContentsToCommitAsync();

        return new ServiceResult<List<DynamicContentCommitModel>>(results);
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.Branches.Interfaces;
using Api.Modules.Tenants.Interfaces;
using Api.Modules.Templates.Interfaces;
using Api.Modules.VersionControl.Interfaces;
using Api.Modules.VersionControl.Interfaces.DataLayer;
using Api.Modules.VersionControl.Models;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Modules.Databases.Interfaces;

namespace Api.Modules.VersionControl.Services
{
    /// <inheritdoc cref="IVersionControlService" />
    public class VersionControlService : IVersionControlService, IScopedService
    {
        private readonly IVersionControlDataService versionControlDataService;
        private readonly IBranchesService branchesService;
        private readonly IWiserTenantsService wiserTenantsService;
        private readonly ITemplatesService templatesService;
        private readonly IDynamicContentService dynamicContentService;
        private readonly IDatabaseConnection databaseConnection;

        /// <summary>
        /// Creates a new instance of <see cref="VersionControlService"/>.
        /// </summary>
        public VersionControlService(IVersionControlDataService versionControlDataService, IBranchesService branchesService, IWiserTenantsService wiserTenantsService, ITemplatesService templatesService, IDynamicContentService dynamicContentService, IDatabaseConnection databaseConnection)
        {
            this.versionControlDataService = versionControlDataService;
            this.branchesService = branchesService;
            this.wiserTenantsService = wiserTenantsService;
            this.templatesService = templatesService;
            this.dynamicContentService = dynamicContentService;
            this.databaseConnection = databaseConnection;
        }

        /// <inheritdoc />
        public async Task<ServiceResult<Dictionary<int, int>>> GetPublishedTemplateIdAndVersionAsync()
        {
            var result = await versionControlDataService.GetPublishedTemplateIdAndVersionAsync();

            return new ServiceResult<Dictionary<int,int>>(result);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> CreatePublishLogAsync(int templateId, int version)
        {
            var result = await versionControlDataService.CreatePublishLog(templateId, version);

            return new ServiceResult<bool>(result);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<List<TemplateCommitModel>>> GetTemplatesFromCommitAsync(int commitId)
        {
            var result = await versionControlDataService.GetTemplatesFromCommitAsync(commitId);
            return new ServiceResult<List<TemplateCommitModel>>(result);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<List<DynamicContentCommitModel>>> GetDynamicContentFromCommitAsync(int commitId)
        {
            var result = await versionControlDataService.GetDynamicContentFromCommitAsync(commitId);
            return new ServiceResult<List<DynamicContentCommitModel>>(result);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> DeployToBranchAsync(ClaimsIdentity identity, List<int> commitIds, int branchId, bool useTransaction = true)
        {
            // The user must be logged in the main branch, otherwise they can't use this functionality.
            if (!(await branchesService.IsMainBranchAsync(identity)).ModelObject)
            {
                return new ServiceResult<bool>
                {
                    ModelObject = false,
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessage = "The current branch is not the main branch. This functionality can only be used from the main branch." 
                };
            }
            
            // Check if the branch exists.
            var branchToDeploy = (await wiserTenantsService.GetSingleAsync(branchId, true)).ModelObject;
            if (branchToDeploy == null)
            {
                return new ServiceResult<bool>
                {
                    ModelObject = false,
                    StatusCode = HttpStatusCode.NotFound,
                    ErrorMessage = $"Branch with ID {branchId} does not exist" 
                };
            }

            // Make sure the user did not try to enter an ID for a branch that they don't own.
            if (!(await branchesService.CanAccessBranchAsync(identity, branchToDeploy)).ModelObject)
            {
                return new ServiceResult<bool>
                {
                    ModelObject = false,
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessage = $"You don't have permissions to access a branch with ID {branchId}" 
                };
            }

            if (useTransaction) await databaseConnection.BeginTransactionAsync();
            
            try
            {
                foreach (var commitId in commitIds)
                {
                    // Get all templates and dynamic content that are part of this commit.
                    var templates = await versionControlDataService.GetTemplatesFromCommitAsync(commitId);
                    var dynamicContents = await versionControlDataService.GetDynamicContentFromCommitAsync(commitId);

                    // Deploy the templates that are part of the commit to the branch.
                    if (templates.Any())
                    {
                        var templateResult = await templatesService.DeployToBranchAsync(identity, templates.Select(template => template.TemplateId).ToList(), branchId);

                        if (templateResult.StatusCode != HttpStatusCode.NoContent)
                        {
                            return new ServiceResult<bool>
                            {
                                ModelObject = false,
                                ErrorMessage = templateResult.ErrorMessage,
                                StatusCode = templateResult.StatusCode
                            };
                        }
                    }

                    // Deploy the dynamic contents that are part of the commit to the branch.
                    if (dynamicContents.Any())
                    {
                        var dynamicContentResult = await dynamicContentService.DeployToBranchAsync(identity, dynamicContents.Select(dynamicContent => dynamicContent.DynamicContentId).ToList(), branchId);

                        if (dynamicContentResult.StatusCode != HttpStatusCode.NoContent)
                        {
                            return new ServiceResult<bool>
                            {
                                ModelObject = false,
                                ErrorMessage = dynamicContentResult.ErrorMessage,
                                StatusCode = dynamicContentResult.StatusCode
                            };
                        }
                    }
                }

                if (useTransaction) await databaseConnection.CommitTransactionAsync();
            }
            catch (Exception)
            {
                if (useTransaction) await databaseConnection.RollbackTransactionAsync();
                throw;
            }

            return new ServiceResult<bool>(true)
            {
                StatusCode = HttpStatusCode.NoContent
            };
        }
    }
}

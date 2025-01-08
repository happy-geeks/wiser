using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Helpers;
using Api.Core.Services;
using Api.Modules.Branches.Interfaces;
using Api.Modules.Templates.Helpers;
using Api.Modules.Templates.Interfaces;
using Api.Modules.Templates.Interfaces.DataLayer;
using Api.Modules.Templates.Models.DynamicContent;
using Api.Modules.Templates.Models.Other;
using Api.Modules.Tenants.Interfaces;
using Api.Modules.Tenants.Models;
using GeeksCoreLibrary.Components.Account;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Enums;
using MySqlConnector;

namespace Api.Modules.Templates.Services
{
    /// <inheritdoc cref="IDynamicContentService" />
    public class DynamicContentService : IDynamicContentService, IScopedService
    {
        private readonly IDynamicContentDataService dataService;
        private readonly IHistoryService historyService;
        private readonly IBranchesService branchesService;
        private readonly IWiserTenantsService wiserTenantsService;

        /// <summary>
        /// Creates a new instance of <see cref="DynamicContentService"/>.
        /// </summary>
        public DynamicContentService(IDynamicContentDataService dataService, IHistoryService historyService, IBranchesService branchesService, IWiserTenantsService wiserTenantsService)
        {
            this.dataService = dataService;
            this.historyService = historyService;
            this.branchesService = branchesService;
            this.wiserTenantsService = wiserTenantsService;
        }

        /// <inheritdoc />
        public Dictionary<int, string> GetComponentModes(Type component)
        {
            if (component?.BaseType == null)
            {
                return new Dictionary<int, string>();
            }

            var info = component.BaseType.GetTypeInfo();
            var settingsType = info.GetGenericArguments().FirstOrDefault();
            var componentModeProperty = settingsType?.GetProperty("ComponentMode");
            if (componentModeProperty == null || !componentModeProperty.PropertyType.IsEnum)
            {
                return new Dictionary<int, string>();
            }

            var enumFields = componentModeProperty.PropertyType.GetFields();
            var returnDict = new Dictionary<int, string>();

            foreach (var enumField in enumFields)
            {
                if (enumField.Name.Equals("value__")) continue;
                returnDict.Add((int)enumField.GetRawConstantValue(), enumField.Name);
            }

            return returnDict;
        }

        /// <inheritdoc />
        public ServiceResult<List<ComponentModeModel>> GetComponentModes(string name)
        {
            if (String.IsNullOrWhiteSpace(name))
            {
                return new ServiceResult<List<ComponentModeModel>>
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessage = "Name cannot be empty"
                };
            }

            var type = ReflectionHelper.GetComponentTypeByName(name);
            if (type == null)
            {
                return new ServiceResult<List<ComponentModeModel>>
                {
                    StatusCode = HttpStatusCode.NotFound,
                    ErrorMessage = $"Component with type '{name}' not found."
                };
            }

            var componentModes = GetComponentModes(type);
            var results = componentModes.Select(componentMode => new ComponentModeModel { Id = componentMode.Key, Name = componentMode.Value }).ToList();

            return new ServiceResult<List<ComponentModeModel>>(results);
        }

        /// <inheritdoc />
        public List<PropertyInfo> GetPropertiesOfType(Type CmsSettingsType)
        {
            var resultlist = new List<PropertyInfo>();
            var allProperties = CmsSettingsType.GetProperties();

            foreach (var property in allProperties)
            {
                resultlist.Add(property);
            }

            return resultlist;
        }

        /// <inheritdoc />
        public async Task<ServiceResult<Dictionary<string, object>>> GetComponentDataAsync(int contentId)
        {
            var data = (await dataService.GetComponentDataAsync(contentId)).Value;

            if (data == null)
            {
                return new ServiceResult<Dictionary<string, object>>
                {
                    StatusCode = HttpStatusCode.NotFound
                };
            }

            return new ServiceResult<Dictionary<string, object>>(data);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<int>> SaveAsync(ClaimsIdentity identity, int contentId, string component, int componentMode, string title, Dictionary<string, object> settings)
        {
            var componentType = ReflectionHelper.GetComponentTypeByName(component);
            var modes = GetComponentModes(componentType);
            modes.TryGetValue(componentMode, out var componentModeName);

            // Remove default values so that they won't be saved in the database.
            var assembly = Assembly.GetAssembly(componentType);
            var fullTypeName = $"{componentType.Namespace}.Models.{componentType.Name}{componentModeName}SettingsModel";
            var type = assembly?.GetType(fullTypeName);
            var defaultValueProperties = type?.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (defaultValueProperties != null)
            {
                var settingsToRemove = new List<string>();
                foreach (var setting in settings)
                {
                    var defaultValueProperty = defaultValueProperties.FirstOrDefault(p => p.Name == setting.Key);
                    var defaultValueAttribute = defaultValueProperty?.GetCustomAttribute<DefaultValueAttribute>();
                    if (defaultValueAttribute == null)
                    {
                        continue;
                    }

                    var itemValue = setting.Value == null ? "" : setting.Value.ToString()!.Replace("\r", "").Replace("\n", "");
                    var defaultValue = defaultValueAttribute.Value == null ? "" : defaultValueAttribute.Value.ToString()!.Replace("\r", "").Replace("\n", "");
                    if (itemValue != defaultValue)
                    {
                        continue;
                    }

                    settingsToRemove.Add(setting.Key);
                }

                foreach (var propertyName in settingsToRemove)
                {
                    settings.Remove(propertyName);
                }
            }

            var id = await dataService.SaveAsync(contentId, component, componentModeName, title, settings, IdentityHelpers.GetUserName(identity, true));
            return new ServiceResult<int>(id);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<int>> CreateNewVersionAsync(int contentId, int versionBeingDeployed = 0)
        {
            // ReSharper disable once InvertIf
            if (versionBeingDeployed > 0)
            {
                var latestVersion = await dataService.GetLatestVersionAsync(contentId);
                if (versionBeingDeployed != latestVersion.Version || latestVersion.Removed)
                {
                    return new ServiceResult<int>(0);
                }
            }

            return new ServiceResult<int>(await dataService.CreateNewVersionAsync(contentId));
        }

        /// <inheritdoc />
        public async Task<ServiceResult<DynamicContentOverviewModel>> GetMetaDataAsync(int contentId, bool includeSettings = true)
        {
            if (contentId <= 0)
            {
                return new ServiceResult<DynamicContentOverviewModel>(new DynamicContentOverviewModel
                {
                    Data = new Dictionary<string, object>(),
                    Component = nameof(Account),
                    ComponentMode = nameof(Account.ComponentModes.LoginSingleStep)
                });
            }

            var result = await dataService.GetMetaDataAsync(contentId);
            if (result == null)
            {
                return new ServiceResult<DynamicContentOverviewModel>
                {
                    StatusCode = HttpStatusCode.NotFound
                };
            }

            result.Versions = await historyService.GetHistoryVersionsOfDynamicContent(contentId);

            if (includeSettings)
            {
                result.Data = (await dataService.GetComponentDataAsync(contentId)).Value;
            }

            return new ServiceResult<DynamicContentOverviewModel>
            {
                StatusCode = HttpStatusCode.OK,
                ModelObject = result
            };
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> AddLinkToTemplateAsync(ClaimsIdentity identity, int contentId, int templateId)
        {
            await dataService.AddLinkToTemplateAsync(contentId, templateId, IdentityHelpers.GetUserName(identity, true));
            return new ServiceResult<bool>(true)
            {
                StatusCode = HttpStatusCode.NoContent
            };
        }

        /// <inheritdoc />
        public async Task<ServiceResult<PublishedEnvironmentModel>> GetEnvironmentsAsync(int contentId, TenantModel branch = null)
        {
            if (contentId <= 0)
            {
                throw new ArgumentException("The Id cannot be zero.");
            }

            var versionsAndPublished = await dataService.GetPublishedEnvironmentsAsync(contentId, branch);

            return new ServiceResult<PublishedEnvironmentModel>(PublishedEnvironmentHelper.CreatePublishedEnvironmentsFromVersionDictionary(versionsAndPublished));
        }

        /// <inheritdoc />
        public async Task<ServiceResult<int>> PublishToEnvironmentAsync(ClaimsIdentity identity, int contentId, int version, Environments environment, PublishedEnvironmentModel currentPublished, TenantModel branch = null)
        {
            if (contentId <= 0)
            {
                throw new ArgumentException("The Id is invalid");
            }

            if (version <= 0)
            {
                throw new ArgumentException("The version is invalid");
            }

            // Create a new version of the dynamic content, so that any changes made after this will be done in the new version instead of the published one.
            // Does not apply if the dynamic content was published to live within a branch.
            if (branch == null)
            {
                await CreateNewVersionAsync(contentId, version);
            }

            var newPublished = PublishedEnvironmentHelper.CalculateEnvironmentsToPublish(currentPublished, version, environment);

            var publishLog = PublishedEnvironmentHelper.GeneratePublishLog(contentId, currentPublished, newPublished);
            await dataService.UpdatePublishedEnvironmentAsync(contentId, version, environment, publishLog, IdentityHelpers.GetUserName(identity, true), branch);
            return new ServiceResult<int>
            {
                StatusCode = HttpStatusCode.NoContent
            };
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> DuplicateAsync(ClaimsIdentity identity, int contentId, int newTemplateId)
        {
            await dataService.DuplicateAsync(contentId, newTemplateId, IdentityHelpers.GetUserName(identity, true));
            return new ServiceResult<bool>(true);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> DeleteAsync(ClaimsIdentity identity, int contentId)
        {
            if (contentId <= 0)
            {
                throw new ArgumentException("The Id is invalid");
            }
            await dataService.DeleteAsync(IdentityHelpers.GetUserName(identity, true), contentId);
            return new ServiceResult<bool>(true);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<List<DynamicContentOverviewModel>>> GetLinkableDynamicContentAsync(int templateId)
        {
            if (templateId <= 0)
            {
                throw new ArgumentException("The Id cannot be zero.");
            }

            var results = await dataService.GetLinkableDynamicContentAsync(templateId);
            return new ServiceResult<List<DynamicContentOverviewModel>>(results);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> DeployToBranchAsync(ClaimsIdentity identity, List<int> dynamicContentIds, int branchId)
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

            // Now we can deploy the template to the branch.
            try
            {
                await dataService.DeployToBranchAsync(dynamicContentIds, branchToDeploy);
            }
            catch (MySqlException mySqlException)
            {
                switch (mySqlException.ErrorCode)
                {
                    case MySqlErrorCode.DuplicateKeyEntry:
                        // We ignore duplicate key errors, because it's possible that a dynamic content already exists in a branch, but it wasn't deployed to the correct environment.
                        // So we ignore this error, so we can still deploy that dynamic content to production in the branch, see the next bit of code after this try/catch.
                        break;
                    case MySqlErrorCode.WrongValueCountOnRow:
                        return new ServiceResult<bool>
                        {
                            StatusCode = HttpStatusCode.Conflict,
                            ErrorMessage = "The tables for the template module are not up-to-date in the selected branch. Please open the template module in that branch once, so that the tables will be automatically updated."
                        };
                    default:
                        throw;
                }
            }

            // Publish all templates to live environment in the branch, because a branch will never actually be used on a live environment
            // and then we can make sure that the deployed templates will be up to date on whichever website environment uses that branch.
            foreach (var dynamicContentId in dynamicContentIds)
            {
                var contentData = await dataService.GetMetaDataAsync(dynamicContentId);
                var currentPublished = (await GetEnvironmentsAsync(dynamicContentId, branchToDeploy)).ModelObject;

                await PublishToEnvironmentAsync(identity, dynamicContentId, contentData.LatestVersion ?? 1, Environments.Live, currentPublished, branchToDeploy);
            }

            return new ServiceResult<bool>(true)
            {
                StatusCode = HttpStatusCode.NoContent
            };
        }
    }
}
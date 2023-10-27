using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.Kendo.Enums;
using Api.Modules.Templates.Interfaces;
using Api.Modules.Templates.Models;
using Api.Modules.Templates.Models.DynamicContent;
using Api.Modules.Templates.Models.History;
using Api.Modules.Templates.Models.Measurements;
using Api.Modules.Templates.Models.Other;
using Api.Modules.Templates.Models.Template;
using Api.Modules.Templates.Models.Template.WtsModels;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Enums;
using GeeksCoreLibrary.Modules.Templates.Models;
using LazyCache;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace Api.Modules.Templates.Services
{
    /// <inheritdoc cref="ITemplatesService" />
    public class CachedTemplatesService : ITemplatesService
    {
        private readonly IAppCache cache;
        private readonly ITemplatesService templatesService;
        private readonly ICacheService cacheService;
        private readonly IDatabaseConnection databaseConnection;
        private readonly GclSettings gclSettings;

        /// <summary>
        /// Creates a new instance of <see cref="CachedTemplatesService"/>.
        /// </summary>
        public CachedTemplatesService(IAppCache cache, ITemplatesService templatesService, IOptions<GclSettings> gclSettings, ICacheService cacheService, IDatabaseConnection databaseConnection)
        {
            this.cache = cache;
            this.templatesService = templatesService;
            this.cacheService = cacheService;
            this.databaseConnection = databaseConnection;
            this.gclSettings = gclSettings.Value;
        }

        /// <inheritdoc />
        public ServiceResult<Template> Get(int templateId = 0, string templateName = null, string rootName = "")
        {
            return templatesService.Get(templateId, templateName, rootName);
        }

        /// <inheritdoc />
        public Task<ServiceResult<QueryTemplate>> GetQueryAsync(int templateId = 0, string templateName = null)
        {
            return templatesService.GetQueryAsync(templateId, templateName);
        }

        /// <inheritdoc />
        public Task<ServiceResult<JToken>> GetAndExecuteQueryAsync(ClaimsIdentity identity, string templateName, IFormCollection requestPostData = null)
        {
            return templatesService.GetAndExecuteQueryAsync(identity, templateName, requestPostData);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<string>> GetCssForHtmlEditorsAsync(ClaimsIdentity identity)
        {
            await databaseConnection.EnsureOpenConnectionForReadingAsync();
            return await cache.GetOrAddAsync($"css_for_html_editors_{databaseConnection.GetDatabaseNameForCaching()}",
                async cacheEntry =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultTemplateCacheDuration;
                    return await templatesService.GetCssForHtmlEditorsAsync(identity);
                }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Templates));
        }

        /// <inheritdoc />
        public async Task<ServiceResult<TemplateEntityModel>> GetTemplateByNameAsync(string templateName, bool wiserTemplate = false)
        {
            return await templatesService.GetTemplateByNameAsync(templateName, wiserTemplate);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<TemplateSettingsModel>> GetTemplateMetaDataAsync(int templateId)
        {
            return await templatesService.GetTemplateMetaDataAsync(templateId);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<TemplateSettingsModel>> GetTemplateSettingsAsync(ClaimsIdentity identity, int templateId, Environments? environment = null)
        {
            return await templatesService.GetTemplateSettingsAsync(identity, templateId, environment);
        }
        
        /// <inheritdoc />
        public async Task<ServiceResult<TemplateWtsConfigurationModel>> GetTemplateWtsConfigurationAsync(ClaimsIdentity identity, int templateId, Environments? environment = null)
        {
            return await templatesService.GetTemplateWtsConfigurationAsync(identity, templateId, environment);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<PublishedEnvironmentModel>> GetTemplateEnvironmentsAsync(int templateId, string branchDatabaseName)
        {
            return await templatesService.GetTemplateEnvironmentsAsync(templateId, branchDatabaseName);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<LinkedTemplatesModel>> GetLinkedTemplatesAsync(int templateId)
        {
            return await templatesService.GetLinkedTemplatesAsync(templateId);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<List<DynamicContentOverviewModel>>> GetLinkedDynamicContentAsync(int templateId)
        {
            return await templatesService.GetLinkedDynamicContentAsync(templateId);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<int>> PublishToEnvironmentAsync(ClaimsIdentity identity, int templateId, int version, Environments environment, PublishedEnvironmentModel currentPublished, string branchDatabaseName)
        {
            return await templatesService.PublishToEnvironmentAsync(identity, templateId, version, environment, currentPublished, branchDatabaseName);
        }
        
        /// <inheritdoc />
        public async Task<ServiceResult<bool>> SaveAsync(ClaimsIdentity identity, int templateId, TemplateWtsConfigurationModel configuration)
        {
            return await templatesService.SaveAsync(identity, templateId, configuration);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> SaveAsync(ClaimsIdentity identity, TemplateSettingsModel template, bool skipCompilation = false)
        {
            return await templatesService.SaveAsync(identity, template, skipCompilation);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<int>> CreateNewVersionAsync(int templateId, int versionBeingDeployed = 0)
        {
            return await templatesService.CreateNewVersionAsync(templateId, versionBeingDeployed);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<List<TemplateTreeViewModel>>> GetTreeViewSectionAsync(ClaimsIdentity identity, int parentId)
        {
            return await templatesService.GetTreeViewSectionAsync(identity, parentId);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<List<SearchResultModel>>> SearchAsync(ClaimsIdentity identity, string searchValue)
        {
            return await templatesService.SearchAsync(identity, searchValue);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<TemplateHistoryOverviewModel>> GetTemplateHistoryAsync(ClaimsIdentity identity, int templateId, int pageNumber, int itemsPerPage)
        {
            return await templatesService.GetTemplateHistoryAsync(identity, templateId, pageNumber, itemsPerPage);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<TemplateTreeViewModel>> CreateAsync(ClaimsIdentity identity, string name, int parent, TemplateTypes type, string editorValue = "")
        {
            return await templatesService.CreateAsync(identity, name, parent, type, editorValue);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> RenameAsync(ClaimsIdentity identity, int id, string newName)
        {
            return await templatesService.RenameAsync(identity, id, newName);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> MoveAsync(ClaimsIdentity identity, int sourceId, int destinationId, TreeViewDropPositions dropPosition)
        {
            return await templatesService.MoveAsync(identity, sourceId, destinationId, dropPosition);
        }

        /// <inheritdoc />
        public Task<ServiceResult<List<TemplateTreeViewModel>>> GetEntireTreeViewStructureAsync(ClaimsIdentity identity, int parentId, string startFrom, Environments? environment = null)
        {
            return templatesService.GetEntireTreeViewStructureAsync(identity, parentId, startFrom, environment);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> DeleteAsync(ClaimsIdentity identity, int templateId, bool alsoDeleteChildren = true)
        {
            return await templatesService.DeleteAsync(identity, templateId, alsoDeleteChildren);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<string>> CheckDefaultHeaderConflict(int templateId, string regexString)
        {
            return await templatesService.CheckDefaultHeaderConflict(templateId, regexString);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<string>> CheckDefaultFooterConflict(int templateId, string regexString)
        {
            return await templatesService.CheckDefaultFooterConflict(templateId, regexString);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<TemplateSettingsModel>> GetVirtualTemplateAsync(string objectName, TemplateTypes templateType)
        {
            return await templatesService.GetVirtualTemplateAsync(objectName, templateType);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<IList<string>>> GetTableNamesForTriggerTemplatesAsync()
        {
            // No need to cache this, because the GCL already caches the result.
            return await templatesService.GetTableNamesForTriggerTemplatesAsync();
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> DeployToBranchAsync(ClaimsIdentity identity, List<int> templateIds, int branchId)
        {
            return await templatesService.DeployToBranchAsync(identity, templateIds, branchId);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<MeasurementSettings>> GetMeasurementSettingsAsync(int templateId = 0, int componentId = 0)
        {
            return await templatesService.GetMeasurementSettingsAsync(templateId, componentId);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> SaveMeasurementSettingsAsync(MeasurementSettings settings, int templateId = 0, int componentId = 0)
        {
            return await templatesService.SaveMeasurementSettingsAsync(settings, templateId, componentId);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<List<RenderLogModel>>> GetRenderLogsAsync(int templateId, int version = 0, string urlRegex = null, Environments? environment = null, ulong userId = 0, string languageCode = null, int pageSize = 500, int pageNumber = 1, bool getDailyAverage = false, DateTime? start = null, DateTime? end = null)
        {
            return await templatesService.GetRenderLogsAsync(templateId, version, urlRegex, environment, userId, languageCode, pageSize, pageNumber, getDailyAverage, start, end);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> ConvertLegacyTemplatesToNewTemplatesAsync(ClaimsIdentity identity)
        {
            return await templatesService.ConvertLegacyTemplatesToNewTemplatesAsync(identity);
        }
    }
}
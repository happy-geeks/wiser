using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.Kendo.Enums;
using Api.Modules.Templates.Interfaces;
using Api.Modules.Templates.Models;
using Api.Modules.Templates.Models.DynamicContent;
using Api.Modules.Templates.Models.History;
using Api.Modules.Templates.Models.Other;
using Api.Modules.Templates.Models.Template;
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
            return await cache.GetOrAdd($"css_for_html_editors_{databaseConnection.GetDatabaseNameForCaching()}",
                async cacheEntry =>
                {
                    cacheEntry.SlidingExpiration = gclSettings.DefaultTemplateCacheDuration;
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
        public async Task<ServiceResult<TemplateSettingsModel>> GetTemplateSettingsAsync(int templateId)
        {
            return await templatesService.GetTemplateSettingsAsync(templateId);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<PublishedEnvironmentModel>> GetTemplateEnvironmentsAsync(int templateId)
        {
            return await templatesService.GetTemplateEnvironmentsAsync(templateId);
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
        public async Task<ServiceResult<int>> PublishToEnvironmentAsync(ClaimsIdentity identity, int templateId, int version, string environment, PublishedEnvironmentModel currentPublished)
        {
            return await templatesService.PublishToEnvironmentAsync(identity, templateId, version, environment, currentPublished);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> SaveTemplateVersionAsync(ClaimsIdentity identity, TemplateSettingsModel template, bool skipCompilation = false)
        {
            return await templatesService.SaveTemplateVersionAsync(identity, template, skipCompilation);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<List<TemplateTreeViewModel>>> GetTreeViewSectionAsync(int parentId)
        {
            return await templatesService.GetTreeViewSectionAsync(parentId);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<List<SearchResultModel>>> SearchAsync(SearchSettingsModel searchSettings)
        {
            return await templatesService.SearchAsync(searchSettings);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<TemplateHistoryOverviewModel>> GetTemplateHistoryAsync(int templateId)
        {
            return await templatesService.GetTemplateHistoryAsync(templateId);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<TemplateTreeViewModel>> CreateAsync(ClaimsIdentity identity, string name, int parent, TemplateTypes type, string editorValue="")
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
        public Task<ServiceResult<List<TemplateTreeViewModel>>> GetEntireTreeViewStructureAsync(int parentId, string startFrom)
        {
            return templatesService.GetEntireTreeViewStructureAsync(parentId, startFrom);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> DeleteAsync(ClaimsIdentity identity, int templateId, bool alsoDeleteChildren = true)
        {
            return await templatesService.DeleteAsync(identity, templateId, alsoDeleteChildren);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<string>> GeneratePreviewAsync(ClaimsIdentity identity, int componentId, GenerateTemplatePreviewRequestModel requestModel)
        {
            return await templatesService.GeneratePreviewAsync(identity, componentId, requestModel);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<string>> GeneratePreviewAsync(ClaimsIdentity identity, GenerateTemplatePreviewRequestModel requestModel)
        {
            return await templatesService.GeneratePreviewAsync(identity, requestModel);
        }
    }
}

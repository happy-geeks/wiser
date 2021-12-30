using System.Collections.Specialized;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.Templates.Interfaces;
using Api.Modules.Templates.Models;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Models;
using LazyCache;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace Api.Modules.Templates.Services
{
    public class CachedTemplatesService : ITemplatesService
    {
        private readonly IAppCache cache;
        private readonly ITemplatesService templatesService;
        private readonly ICacheService cacheService;
        private readonly IDatabaseConnection databaseConnection;
        private readonly GclSettings gclSettings;

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
        public async Task<ServiceResult<TemplateModel>> GetTemplateByName(string templateName, bool wiserTemplate = false)
        {
            return await templatesService.GetTemplateByName(templateName, wiserTemplate);
        }
    }
}

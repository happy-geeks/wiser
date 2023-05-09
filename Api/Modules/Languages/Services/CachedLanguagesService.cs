using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Core.Models;
using Api.Core.Services;
using Api.Modules.Languages.Interfaces;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Languages.Models;
using LazyCache;
using Microsoft.Extensions.Options;

namespace Api.Modules.Languages.Services;

/// <inheritdoc />
public class CachedLanguagesService : ILanguagesService
{
    private readonly IAppCache cache;
    private readonly ICacheService cacheService;
    private readonly ApiSettings apiSettings;
    private readonly ILanguagesService languagesService;
    private readonly IDatabaseConnection databaseConnection;

    /// <summary>
    /// Creates a new instance of <see cref="CachedLanguagesService"/>.
    /// </summary>
    public CachedLanguagesService(IAppCache cache, IOptions<ApiSettings> apiSettings, ICacheService cacheService, ILanguagesService languagesService, IDatabaseConnection databaseConnection)
    {
        this.cache = cache;
        this.cacheService = cacheService;
        this.apiSettings = apiSettings.Value;
        this.languagesService = languagesService;
        this.databaseConnection = databaseConnection;
    }

    /// <inheritdoc />
    public async Task<ServiceResult<List<LanguageModel>>> GetAllLanguagesAsync()
    {
        return await languagesService.GetAllLanguagesAsync();
    }

    /// <inheritdoc />
    public async Task<ServiceResult<List<SimpleKeyValueModel>>> GetAllTranslationsAsync()
    {
        await databaseConnection.EnsureOpenConnectionForReadingAsync();
        return await cache.GetOrAddAsync($"translations_{databaseConnection.GetDatabaseNameForCaching()}",
            async cacheEntry =>
            {
                cacheEntry.AbsoluteExpirationRelativeToNow = apiSettings.DefaultUsersCacheDuration;
                return await languagesService.GetAllTranslationsAsync();
            }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.WiserItems));
    }
}
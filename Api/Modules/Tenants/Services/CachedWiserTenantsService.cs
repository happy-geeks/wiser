using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Helpers;
using Api.Core.Models;
using Api.Core.Services;
using Api.Modules.Tenants.Enums;
using Api.Modules.Tenants.Interfaces;
using Api.Modules.Tenants.Models;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Interfaces;
using LazyCache;
using Microsoft.Extensions.Options;

namespace Api.Modules.Tenants.Services
{
    /// <summary>
    /// Service for operations related to Wiser tenants.
    /// </summary>
    public class CachedWiserTenantsService : IWiserTenantsService, IScopedService
    {
        #region Private fields

        private readonly IAppCache cache;
        private readonly ApiSettings apiSettings;
        private readonly ICacheService cacheService;
        private readonly IWiserTenantsService wiserTenantsService;

        #endregion

        /// <summary>
        /// Creates a new instance of WiserTenantsService.
        /// </summary>
        public CachedWiserTenantsService(IAppCache cache, IOptions<ApiSettings> apiSettings, ICacheService cacheService, IWiserTenantsService wiserTenantsService)
        {
            this.cache = cache;
            this.apiSettings = apiSettings.Value;
            this.cacheService = cacheService;
            this.wiserTenantsService = wiserTenantsService;
        }

        /// <inheritdoc />
        public async Task<ServiceResult<TenantModel>> GetSingleAsync(ClaimsIdentity identity, bool includeDatabaseInformation = false)
        {
            var subDomain = IdentityHelpers.GetSubDomain(identity);
            return await cache.GetOrAddAsync($"customer_{subDomain}_{includeDatabaseInformation}",
                async cacheEntry =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = apiSettings.DefaultUsersCacheDuration;
                    return await wiserTenantsService.GetSingleAsync(identity, includeDatabaseInformation);
                });
        }

        /// <inheritdoc />
        public async Task<ServiceResult<TenantModel>> GetSingleAsync(int id, bool includeDatabaseInformation = false)
        {
            return await cache.GetOrAddAsync($"customer_{id}_{includeDatabaseInformation}",
                async cacheEntry =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = apiSettings.DefaultUsersCacheDuration;
                    return await wiserTenantsService.GetSingleAsync(id, includeDatabaseInformation);
                });
        }

        /// <inheritdoc />
        public async Task<ServiceResult<string>> GetEncryptionKey(ClaimsIdentity identity, bool forceLiveKey = false)
        {
            var subDomain = IdentityHelpers.GetSubDomain(identity);
            var userId = IdentityHelpers.GetWiserUserId(identity);
            return await cache.GetOrAddAsync($"encryption_key_{subDomain}_{(!forceLiveKey && IdentityHelpers.IsTestEnvironment(identity) ? "test" : "live")}",
                async cacheEntry =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = apiSettings.DefaultUsersCacheDuration;
                    return await wiserTenantsService.GetEncryptionKey(identity, forceLiveKey);
                });
        }

        #region Wiser users data/settings

        /// <inheritdoc />
        public async Task<T> DecryptValue<T>(string encryptedValue, ClaimsIdentity identity)
        {
            return await wiserTenantsService.DecryptValue<T>(encryptedValue, identity);
        }

        /// <inheritdoc />
        public T DecryptValue<T>(string encryptedValue, TenantModel tenant)
        {
            return wiserTenantsService.DecryptValue<T>(encryptedValue, tenant);
        }

        /// <inheritdoc />
        public async Task<string> EncryptValue(object valueToEncrypt, ClaimsIdentity identity)
        {
            return await wiserTenantsService.EncryptValue(valueToEncrypt, identity);
        }

        /// <inheritdoc />
        public string EncryptValue(object valueToEncrypt, TenantModel tenant)
        {
            return wiserTenantsService.EncryptValue(valueToEncrypt, tenant);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<TenantExistsResults>> TenantExistsAsync(string name, string subDomain)
        {
            return await wiserTenantsService.TenantExistsAsync(name, subDomain);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<TenantModel>> CreateTenantAsync(TenantModel tenant, bool isWebShop = false, bool isConfigurator = false, bool isMultiLanguage = false)
        {
            return await wiserTenantsService.CreateTenantAsync(tenant, isWebShop, isConfigurator, isMultiLanguage);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<string>> GetTitleAsync(string subDomain)
        {
            return await cache.GetOrAddAsync($"customer_title_{subDomain}",
                async cacheEntry =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = apiSettings.DefaultUsersCacheDuration;
                    return await wiserTenantsService.GetTitleAsync(subDomain);
                }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.WiserItems));
        }

        /// <inheritdoc />
        public bool IsMainDatabase(ClaimsIdentity identity)
        {
            return IsMainDatabase(IdentityHelpers.GetSubDomain(identity));
        }

        /// <inheritdoc />
        public bool IsMainDatabase(string subDomain)
        {
            return cache.GetOrAdd($"is_main_database_{subDomain}",
                cacheEntry =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = apiSettings.DefaultUsersCacheDuration;
                    return wiserTenantsService.IsMainDatabase(subDomain);
                }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.WiserItems));
        }

        /// <inheritdoc />
        public async Task CreateOrUpdateTenantAsync(TenantModel tenant)
        {
            await wiserTenantsService.CreateOrUpdateTenantAsync(tenant);
        }

        /// <inheritdoc />
        public string GenerateConnectionStringFromTenant(TenantModel tenant, bool passwordIsEncrypted = true)
        {
            return wiserTenantsService.GenerateConnectionStringFromTenant(tenant, passwordIsEncrypted);
        }

        #endregion
    }
}
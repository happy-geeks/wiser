using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Helpers;
using Api.Core.Models;
using Api.Core.Services;
using Api.Modules.Customers.Enums;
using Api.Modules.Customers.Interfaces;
using Api.Modules.Customers.Models;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Interfaces;
using LazyCache;
using Microsoft.Extensions.Options;

namespace Api.Modules.Customers.Services
{
    /// <summary>
    /// Service for operations related to Wiser customers.
    /// </summary>
    public class CachedWiserCustomersService : IWiserCustomersService, IScopedService
    {
        #region Private fields

        private readonly IAppCache cache;
        private readonly ApiSettings apiSettings;
        private readonly ICacheService cacheService;
        private readonly IWiserCustomersService wiserCustomersService;

        #endregion

        /// <summary>
        /// Creates a new instance of WiserCustomersService.
        /// </summary>
        public CachedWiserCustomersService(IAppCache cache, IOptions<ApiSettings> apiSettings, ICacheService cacheService, IWiserCustomersService wiserCustomersService)
        {
            this.cache = cache;
            this.apiSettings = apiSettings.Value;
            this.cacheService = cacheService;
            this.wiserCustomersService = wiserCustomersService;
        }

        /// <inheritdoc />
        public async Task<ServiceResult<CustomerModel>> GetSingleAsync(ClaimsIdentity identity, bool includeDatabaseInformation = false)
        {
            var subDomain = IdentityHelpers.GetSubDomain(identity);
            return await cache.GetOrAddAsync($"customer_{subDomain}_{includeDatabaseInformation}",
                async cacheEntry =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = apiSettings.DefaultUsersCacheDuration;
                    return await wiserCustomersService.GetSingleAsync(identity, includeDatabaseInformation);
                });
        }

        /// <inheritdoc />
        public async Task<ServiceResult<CustomerModel>> GetSingleAsync(int id, bool includeDatabaseInformation = false)
        {
            return await cache.GetOrAddAsync($"customer_{id}_{includeDatabaseInformation}",
                async cacheEntry =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = apiSettings.DefaultUsersCacheDuration;
                    return await wiserCustomersService.GetSingleAsync(id, includeDatabaseInformation);
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
                    return await wiserCustomersService.GetEncryptionKey(identity, forceLiveKey);
                });
        }

        #region Wiser users data/settings

        /// <inheritdoc />
        public async Task<T> DecryptValue<T>(string encryptedValue, ClaimsIdentity identity)
        {
            return await wiserCustomersService.DecryptValue<T>(encryptedValue, identity);
        }

        /// <inheritdoc />
        public T DecryptValue<T>(string encryptedValue, CustomerModel customer)
        {
            return wiserCustomersService.DecryptValue<T>(encryptedValue, customer);
        }

        /// <inheritdoc />
        public async Task<string> EncryptValue(object valueToEncrypt, ClaimsIdentity identity)
        {
            return await wiserCustomersService.EncryptValue(valueToEncrypt, identity);
        }

        /// <inheritdoc />
        public string EncryptValue(object valueToEncrypt, CustomerModel customer)
        {
            return wiserCustomersService.EncryptValue(valueToEncrypt, customer);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<CustomerExistsResults>> CustomerExistsAsync(string name, string subDomain)
        {
            return await wiserCustomersService.CustomerExistsAsync(name, subDomain);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<CustomerModel>> CreateCustomerAsync(CustomerModel customer, bool isWebShop = false, bool isConfigurator = false, bool isMultiLanguage = false)
        {
            return await wiserCustomersService.CreateCustomerAsync(customer, isWebShop, isConfigurator, isMultiLanguage);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<string>> GetTitleAsync(string subDomain)
        {
            return await cache.GetOrAddAsync($"customer_title_{subDomain}",
                async cacheEntry =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = apiSettings.DefaultUsersCacheDuration;
                    return await wiserCustomersService.GetTitleAsync(subDomain);
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
                    return wiserCustomersService.IsMainDatabase(subDomain);
                }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.WiserItems));
        }

        /// <inheritdoc />
        public async Task CreateOrUpdateCustomerAsync(CustomerModel customer)
        {
            await wiserCustomersService.CreateOrUpdateCustomerAsync(customer);
        }

        /// <inheritdoc />
        public string GenerateConnectionStringFromCustomer(CustomerModel customer, bool passwordIsEncrypted = true)
        {
            return wiserCustomersService.GenerateConnectionStringFromCustomer(customer, passwordIsEncrypted);
        }

        #endregion
    }
}
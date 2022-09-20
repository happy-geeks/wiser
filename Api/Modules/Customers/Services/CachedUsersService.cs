using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Models;
using Api.Core.Services;
using Api.Modules.Customers.Interfaces;
using Api.Modules.Customers.Models;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using LazyCache;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace Api.Modules.Customers.Services
{
    /// <inheritdoc />
    public class CachedUsersService : IUsersService
    {
        private readonly IAppCache cache;
        private readonly ICacheService cacheService;
        private readonly IUsersService usersService;
        private readonly IDatabaseConnection databaseConnection;
        private readonly ApiSettings apiSettings;

        /// <summary>
        /// Creates a new instance of CachedUsersService.
        /// </summary>
        public CachedUsersService(IAppCache cache, IOptions<ApiSettings> apiSettings, ICacheService cacheService, IUsersService usersService, IDatabaseConnection databaseConnection)
        {
            this.cache = cache;
            this.cacheService = cacheService;
            this.usersService = usersService;
            this.databaseConnection = databaseConnection;
            this.apiSettings = apiSettings.Value;
        }

        /// <inheritdoc />
        public async Task<ServiceResult<List<WiserItemModel>>> GetAsync()
        {
            await databaseConnection.EnsureOpenConnectionForReadingAsync();
            return await cache.GetOrAdd($"users_{databaseConnection.GetDatabaseNameForCaching()}",
                async cacheEntry =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = apiSettings.DefaultUsersCacheDuration;
                    return await usersService.GetAsync();
                }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.WiserItems));
        }

        /// <inheritdoc />
        public Task<ServiceResult<AdminAccountModel>> LoginAdminAccountAsync(string username, string password, string ipAddress = null)
        {
            return usersService.LoginAdminAccountAsync(username, password, ipAddress);
        }

        /// <inheritdoc />
        public Task<ServiceResult<UserModel>> LoginCustomerAsync(string username, string password, string encryptedAdminAccountId = null, string subDomain = null, bool generateAuthenticationTokenForCookie = false, string ipAddress = null, ClaimsIdentity identity = null)
        {
            return usersService.LoginCustomerAsync(username, password, encryptedAdminAccountId, subDomain, generateAuthenticationTokenForCookie, ipAddress, identity);
        }

        /// <inheritdoc />
        public Task<ServiceResult<bool>> ResetPasswordAsync(ResetPasswordRequestModel resetPasswordRequestModel, ClaimsIdentity identity)
        {
            return usersService.ResetPasswordAsync(resetPasswordRequestModel, identity);
        }

        /// <inheritdoc />
        public Task<ServiceResult<ValidateCookieModel>> ValidateLoginCookieAsync(string cookieValue, string subDomain = null, string ipAddress = null, string sessionId = null, string encryptedAdminAccountId = null, ClaimsIdentity identity = null)
        {
            return usersService.ValidateLoginCookieAsync(cookieValue, subDomain, ipAddress, sessionId, encryptedAdminAccountId, identity);
        }

        /// <inheritdoc />
        public Task<ServiceResult<UserModel>> ChangePasswordAsync(ClaimsIdentity identity, ChangePasswordModel passwords)
        {
            return usersService.ChangePasswordAsync(identity, passwords);
        }

        /// <inheritdoc />
        public Task<ServiceResult<UserModel>> ChangeEmailAddressAsync(ulong userId, string subDomain, string newEmailAddress, ClaimsIdentity identity)
        {
            return usersService.ChangeEmailAddressAsync(userId, subDomain, newEmailAddress, identity);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<UserModel>> GetUserDataAsync(ClaimsIdentity identity)
        {
            return await usersService.GetUserDataAsync(this, identity);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<UserModel>> GetUserDataAsync(IUsersService service, ClaimsIdentity identity)
        {
            return await usersService.GetUserDataAsync(service, identity);
        }

        /// <inheritdoc />
        public Task<ServiceResult<string>> GetGridSettingsAsync(ClaimsIdentity identity, string uniqueKey)
        {
            return usersService.GetGridSettingsAsync(identity, uniqueKey);
        }

        /// <inheritdoc />
        public Task<ServiceResult<List<int>>> GetPinnedModulesAsync(ClaimsIdentity identity)
        {
            return usersService.GetPinnedModulesAsync(identity);
        }

        /// <inheritdoc />
        public Task<ServiceResult<List<int>>> GetAutoLoadModulesAsync(ClaimsIdentity identity)
        {
            return usersService.GetAutoLoadModulesAsync(identity);
        }

        /// <inheritdoc />
        public Task<ServiceResult<bool>> SaveGridSettingsAsync(ClaimsIdentity identity, string uniqueKey, JToken settings)
        {
            return usersService.SaveGridSettingsAsync(identity, uniqueKey, settings);
        }

        /// <inheritdoc />
        public Task<ServiceResult<bool>> SavePinnedModulesAsync(ClaimsIdentity identity, List<int> moduleIds)
        {
            return usersService.SavePinnedModulesAsync(identity, moduleIds);
        }

        /// <inheritdoc />
        public Task<ServiceResult<bool>> SaveAutoLoadModulesAsync(ClaimsIdentity identity, List<int> moduleIds)
        {
            return usersService.SaveAutoLoadModulesAsync(identity, moduleIds);
        }

        /// <inheritdoc />
        public async Task<string> GetUserEmailAddressAsync(ulong id)
        {
            await databaseConnection.EnsureOpenConnectionForReadingAsync();
            return await cache.GetOrAdd($"users_{databaseConnection.GetDatabaseNameForCaching()}_{id}_email",
                async cacheEntry =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = apiSettings.DefaultUsersCacheDuration;
                    return await usersService.GetUserEmailAddressAsync(id);
                }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.WiserItems));
        }

        /// <inheritdoc />
        public async Task<UserModel> GetWiserSettingsForUserAsync(string encryptionKey)
        {
            await databaseConnection.EnsureOpenConnectionForReadingAsync();
            return await cache.GetOrAdd($"user_data_wiser_settings_{databaseConnection.GetDatabaseNameForCaching()}",
                async cacheEntry =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = apiSettings.DefaultUsersCacheDuration;
                    return await usersService.GetWiserSettingsForUserAsync(encryptionKey);
                }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Objects));
        }

        /// <inheritdoc />
        public Task<string> GenerateAndSaveNewRefreshTokenAsync(string cookieSelector, string subDomain, string ticket)
        {
            return usersService.GenerateAndSaveNewRefreshTokenAsync(cookieSelector, subDomain, ticket);
        }

        /// <inheritdoc />
        public Task<string> UseRefreshTokenAsync(string subDomain, string refreshToken)
        {
            return usersService.UseRefreshTokenAsync(subDomain, refreshToken);
        }
    }
}

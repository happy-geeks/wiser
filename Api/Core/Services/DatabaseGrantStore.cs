using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Core.Interfaces;
using Api.Core.Models;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Api.Core.Services
{
    /// <summary>
    /// A store for saving grants from IdentityServer4 into the database.
    /// </summary>
    public class DatabaseGrantStore : IPersistedGrantStore, IScopedService
    {
        private readonly ILogger logger;
        private readonly IDatabaseGrantsService databaseGrantsService;
        private readonly IHttpContextAccessor httpContextAccessor;

        /// <summary>
        /// Creates a new instance of DatabaseGrantStore.
        /// </summary>
        public DatabaseGrantStore(ILogger<DatabaseGrantStore> logger, IDatabaseGrantsService databaseGrantsService, IHttpContextAccessor httpContextAccessor)
        {
            this.logger = logger;
            this.databaseGrantsService = databaseGrantsService;
            this.httpContextAccessor = httpContextAccessor;
        }

        /// <inheritdoc />
        public async Task StoreAsync(PersistedGrant grant)
        {
            try
            {
                var result = await databaseGrantsService.GetAsync(grant.Key);
                if (result == null)
                {
                    logger.LogDebug($"{grant.Key} not found in database");
                    await databaseGrantsService.CreateAsync(grant);
                }
                else
                {
                    logger.LogDebug($"{grant.Key} found in database");
                    await databaseGrantsService.UpdateAsync(grant.Key, grant);
                }
            }
            catch (Exception exception)
            {
                logger.LogError(exception, $"Exception occurred while inserting or updating '{grant.Key}' persisted grant in database");
            }
        }

        /// <inheritdoc />
        public async Task<PersistedGrant> GetAsync(string key)
        {
            try
            {
                if (httpContextAccessor.HttpContext != null && httpContextAccessor.HttpContext.Request.HasFormContentType)
                {
                    var subDomain = httpContextAccessor.HttpContext.Request.Form[HttpContextConstants.SubDomainKey].ToString();
                    httpContextAccessor.HttpContext.Items[HttpContextConstants.SubDomainKey] = subDomain;
                }

                return await databaseGrantsService.GetAsync(key);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, $"Exception occurred while getting persisted grant '{key}' from database");
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<PersistedGrant>> GetAllAsync(PersistedGrantFilter filter)
        {
            try
            {
                return await databaseGrantsService.GetAllAsync(filter);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Exception occurred while getting persisted grants from database");
                return null;
            }
        }

        /// <inheritdoc />
        public async Task RemoveAsync(string key)
        {
            try
            {
                await databaseGrantsService.DeleteAsync(key);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, $"Exception occurred while deleting persisted grant '{key}' from database");
            }
        }

        /// <inheritdoc />
        public async Task RemoveAllAsync(PersistedGrantFilter filter)
        {
            try
            {
                await databaseGrantsService.DeleteAllAsync(filter);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Exception occurred while deleting persisted grants from database");
            }
        }
    }
}

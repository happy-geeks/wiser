using System.Collections.Generic;
using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Stores;

namespace Api.Core.Interfaces
{
    /// <summary>
    /// Service for storing grants from IdentityServer4 in the database.
    /// </summary>
    public interface IDatabaseGrantsService
    {
        /// <summary>
        /// Gets a single grant from database, based on the unique key.
        /// </summary>
        /// <param name="key">The unique key of the grant.</param>
        /// <returns>The PersistedGrant.</returns>
        Task<PersistedGrant> GetAsync(string key);

        /// <summary>
        /// Gets all grants with filters.
        /// </summary>
        /// <param name="filter">The filters.</param>
        /// <returns>An IEnumerable with the results.</returns>
        Task<IEnumerable<PersistedGrant>> GetAllAsync(PersistedGrantFilter filter);

        /// <summary>
        /// Adds a new grant to the database.
        /// </summary>
        /// <param name="grant">The PersistedGrant to save.</param>
        Task CreateAsync(PersistedGrant grant);

        /// <summary>
        /// Adds a new grant to the database.
        /// </summary>
        /// <param name="key">The unique key of the grant.</param>
        /// <param name="grant">The PersistedGrant to save.</param>
        Task UpdateAsync(string key, PersistedGrant grant);

        /// <summary>
        /// Deletes a grant from the database.
        /// </summary>
        /// <param name="key">The unique key of the grant.</param>
        Task DeleteAsync(string key);

        /// <summary>
        /// Deletes multiple grants from the database, based on given filters.
        /// </summary>
        /// <param name="filter">The filters.</param>
        Task DeleteAllAsync(PersistedGrantFilter filter);
    }
}

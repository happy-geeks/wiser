using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.Customers.Models;

namespace Api.Modules.Branches.Interfaces
{
    /// <summary>
    /// A service for doing things with Wiser branches. Branches in Wiser are copies of their original database where they can make changes in before putting them in production.
    /// </summary>
    public interface IBranchesService
    {
        /// <summary>
        /// Creates a new environment for the authenticated customer.
        /// This will create a new database schema on the same server/cluster and then fill it with part of the data from the original database.
        /// </summary>
        /// <param name="identity">The <see cref="ClaimsIdentity">ClaimsIdentity</see> of the authenticated user.</param>
        /// <param name="name">The name of the environment</param>
        Task<ServiceResult<CustomerModel>> CreateAsync(ClaimsIdentity identity, string name);

        /// <summary>
        /// Gets the environments for the authenticated user.
        /// </summary>
        /// <param name="identity">The <see cref="ClaimsIdentity">ClaimsIdentity</see> of the authenticated user.</param>
        /// <returns>A list of <see cref="CustomerModel"/>.</returns>
        Task<ServiceResult<List<CustomerModel>>> GetAsync(ClaimsIdentity identity);

        /// <summary>
        /// Gets whether the current branch is the main branch.
        /// </summary>
        /// <param name="identity">The <see cref="ClaimsIdentity">ClaimsIdentity</see> of the authenticated user.</param>
        /// <returns>A boolean indicating whether the current branch is the main branch.</returns>
        Task<ServiceResult<bool>> IsMainBranchAsync(ClaimsIdentity identity);

        /// <summary>
        /// Synchronise all changes done to wiser items, from a specific environment, to the production environment.
        /// This will look in wiser_history for what has been changed, copy those changes to production and then clear the history.
        /// </summary>
        /// <param name="identity">The <see cref="ClaimsIdentity">ClaimsIdentity</see> of the authenticated user.</param>
        /// <param name="id">The ID of the environment to copy the changes from.</param>
        Task<ServiceResult<SynchroniseChangesToProductionResultModel>> MergeAsync(ClaimsIdentity identity, int id);
    }
}
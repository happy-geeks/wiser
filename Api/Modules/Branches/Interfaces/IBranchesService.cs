using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.Branches.Models;
using Api.Modules.Customers.Models;
using GeeksCoreLibrary.Modules.Branches.Models;

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
        /// <param name="settings">The settings for the new environment.</param>
        Task<ServiceResult<CustomerModel>> CreateAsync(ClaimsIdentity identity, CreateBranchSettingsModel settings);

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
        /// Gets whether the current branch is the main branch.
        /// </summary>
        /// <param name="branch">The <see cref="CustomerModel">CustomerModel</see> of the branch to check.</param>
        /// <returns>A boolean indicating whether the current branch is the main branch.</returns>
        ServiceResult<bool> IsMainBranch(CustomerModel branch);

        /// <summary>
        /// Get the changes of a branch.
        /// </summary>
        /// <param name="identity">The <see cref="ClaimsIdentity">ClaimsIdentity</see> of the authenticated user.</param>
        /// <param name="id">The ID of the branch to get the changes of.</param>
        /// <returns>A list of changes per entity type / Wiser setting type.</returns>
        Task<ServiceResult<ChangesAvailableForMergingModel>> GetChangesAsync(ClaimsIdentity identity, int id);

        /// <summary>
        /// Synchronise all changes done to wiser items, from a specific environment, to the production environment.
        /// This will look in wiser_history for what has been changed, copy those changes to production and then clear the history.
        /// </summary>
        /// <param name="identity">The <see cref="ClaimsIdentity">ClaimsIdentity</see> of the authenticated user.</param>
        /// <param name="settings">The settings of what exactly to merge.</param>
        Task<ServiceResult<MergeBranchResultModel>> MergeAsync(ClaimsIdentity identity, MergeBranchSettingsModel settings);

        /// <summary>
        /// Gets whether the current user can access a certain branch.
        /// This will check if the given branch has the same parent ID as the branch that the user is currently authenticated for.
        /// </summary>
        /// <param name="identity">The <see cref="ClaimsIdentity">ClaimsIdentity</see> of the authenticated user.</param>
        /// <param name="branchId">The ID of the branch to check.</param>
        /// <returns>A boolean indicating whether the user is allowed access to the given branch.</returns>
        Task<ServiceResult<bool>> CanAccessBranchAsync(ClaimsIdentity identity, int branchId);

        /// <summary>
        /// Gets whether the current user can access a certain branch.
        /// This will check if the given branch has the same parent ID as the branch that the user is currently authenticated for.
        /// </summary>
        /// <param name="identity">The <see cref="ClaimsIdentity">ClaimsIdentity</see> of the authenticated user.</param>
        /// <param name="branch">The <see cref="CustomerModel">CustomerModel</see> of the branch to check.</param>
        /// <returns>A boolean indicating whether the user is allowed access to the given branch.</returns>
        Task<ServiceResult<bool>> CanAccessBranchAsync(ClaimsIdentity identity, CustomerModel branch);

        /// <summary>
        /// Marks a branch to be deleted by the WTS.
        /// </summary>
        /// <param name="identity">The <see cref="ClaimsIdentity">ClaimsIdentity</see> of the authenticated user.</param>
        /// <param name="id">The ID of the branch that should be deleted.</param>
        Task<ServiceResult<bool>> DeleteAsync(ClaimsIdentity identity, int id);
    }
}
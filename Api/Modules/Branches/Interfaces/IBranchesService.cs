using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.Branches.Models;
using Api.Modules.Tenants.Models;
using GeeksCoreLibrary.Modules.Branches.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Modules.Branches.Interfaces
{
    /// <summary>
    /// A service for doing things with Wiser branches. Branches in Wiser are copies of their original database where they can make changes in before putting them in production.
    /// </summary>
    public interface IBranchesService
    {
        /// <summary>
        /// Creates a new environment for the authenticated tenant.
        /// This will create a new database schema on the same server/cluster and then fill it with part of the data from the original database.
        /// </summary>
        /// <param name="identity">The <see cref="ClaimsIdentity">ClaimsIdentity</see> of the authenticated user.</param>
        /// <param name="settings">The settings for the new environment.</param>
        Task<ServiceResult<TenantModel>> CreateAsync(ClaimsIdentity identity, CreateBranchSettingsModel settings);

        /// <summary>
        /// Gets the environments for the authenticated user.
        /// </summary>
        /// <param name="identity">The <see cref="ClaimsIdentity">ClaimsIdentity</see> of the authenticated user.</param>
        /// <returns>A list of <see cref="TenantModel"/>.</returns>
        Task<ServiceResult<List<TenantModel>>> GetAsync(ClaimsIdentity identity);

        /// <summary>
        /// Gets whether the current branch is the main branch.
        /// </summary>
        /// <param name="identity">The <see cref="ClaimsIdentity">ClaimsIdentity</see> of the authenticated user.</param>
        /// <returns>A boolean indicating whether the current branch is the main branch.</returns>
        Task<ServiceResult<bool>> IsMainBranchAsync(ClaimsIdentity identity);

        /// <summary>
        /// Gets whether the current branch is the main branch.
        /// </summary>
        /// <param name="branch">The <see cref="TenantModel">TenantModel</see> of the branch to check.</param>
        /// <returns>A boolean indicating whether the current branch is the main branch.</returns>
        ServiceResult<bool> IsMainBranch(TenantModel branch);

        /// <summary>
        /// Get the changes of a branch.
        /// </summary>
        /// <param name="identity">The <see cref="ClaimsIdentity">ClaimsIdentity</see> of the authenticated user.</param>
        /// <param name="id">The ID of the branch to get the changes of.</param>
        /// <param name="entityTypes">A list of entity types to count the changes for.</param>
        /// <returns>A list of changes per entity type / Wiser setting type.</returns>
        Task<ServiceResult<ChangesAvailableForMergingModel>> GetChangesAsync(ClaimsIdentity identity, int id, List<string> entityTypes);

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
        /// <param name="branch">The <see cref="TenantModel">TenantModel</see> of the branch to check.</param>
        /// <returns>A boolean indicating whether the user is allowed access to the given branch.</returns>
        Task<ServiceResult<bool>> CanAccessBranchAsync(ClaimsIdentity identity, TenantModel branch);

        /// <summary>
        /// Marks a branch to be deleted by the WTS.
        /// </summary>
        /// <param name="identity">The <see cref="ClaimsIdentity">ClaimsIdentity</see> of the authenticated user.</param>
        /// <param name="id">The ID of the branch that should be deleted.</param>
        Task<ServiceResult<bool>> DeleteAsync(ClaimsIdentity identity, int id);

        /// <summary>
        /// Get the ID of a branch that is mapped to the main branch.
        /// If <paramref name="idIsFromBranch"/> equals <see langword="false"/> then the ID is from the main branch and the ID of the branch will be returned.
        /// </summary>
        /// <param name="id">The ID to get the mapped value for.</param>
        /// <param name="idIsFromBranch">Optional: If the ID being given is from the branch.</param>
        /// <returns></returns>
        Task<ulong?> GetMappedIdAsync(ulong id, bool idIsFromBranch = true);

        /// <summary>
        /// Generates a new ID for the specified table. This will get the highest number from both databases and add 1 to that number.
        /// This is to make sure that the new ID can be created in both databases to match.
        /// If a null is supplied for a branchDatabase we only find the new id on the main database.
        /// </summary>
        /// <param name="tableName">The name of the table.</param>
        /// <param name="mainDatabaseConnection">The connection to the main database.</param>
        /// <param name="branchDatabase">The connection to the branch database.</param>
        /// <returns>The new ID that should be used for the item in both databases.</returns>
        Task<ulong> GenerateNewIdAsync(string tableName, IDatabaseConnection mainDatabaseConnection, IDatabaseConnection branchDatabase = null);

        /// <summary>
        /// Get a <see cref="IDatabaseConnection"/> for a branch. If the branch ID is 0 then the main database connection will be returned.
        /// </summary>
        /// <param name="scope">The scope in which to create the database connection.</param>
        /// <param name="identity">The <see cref="ClaimsIdentity">ClaimsIdentity</see> of the authenticated user.</param>
        /// <param name="branchId">The ID of the branch.</param>
        /// <returns>The database connection either to the branch.</returns>
        Task<ServiceResult<IDatabaseConnection>> GetBranchDatabaseConnectionAsync(IServiceScope scope, ClaimsIdentity identity, int branchId);
    }
}
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.Queries.Models;

namespace Api.Modules.Queries.Interfaces
{
    //TODO Verify comments
    /// <summary>
    /// Service for getting queries for the Wiser modules.
    /// </summary>
    public interface IQueriesService
    {
        /// <summary>
        /// Gets the queries that can be used for an export in the export module.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <returns></returns>
        Task<ServiceResult<List<QueryModel>>> GetForExportModuleAsync(ClaimsIdentity identity);
        
        /// <summary>
        /// Gets the queries that can be used in the admin module.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <returns></returns>
        Task<ServiceResult<List<QueryModel>>> GetAsync(ClaimsIdentity identity);

        /// <summary>
        /// Gets the queries that can be used in the admin module.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="id">The ID from wiser_query.</param>
        /// <returns></returns>
        Task<ServiceResult<QueryModel>> GetAsync(ClaimsIdentity identity, int id);

        /// <summary>
        /// Create query that can be used in the admin module.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="queryModel">The query settings to create.</param>
        /// <returns></returns>
        Task<ServiceResult<QueryModel>> CreateAsync(ClaimsIdentity identity, QueryModel queryModel);

        /// <summary>
        /// Update query that can be used in the admin module.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="queryModel">TThe query settings to save.</param>
        /// <returns></returns>
        Task<ServiceResult<bool>> UpdateAsync(ClaimsIdentity identity, QueryModel queryModel);

        /// <summary>
        /// Update query that can be used in the admin module.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="id">The ID from wiser_query.</param>
        /// <returns></returns>
        Task<ServiceResult<bool>> DeleteAsync(ClaimsIdentity identity, int id);

    }
}

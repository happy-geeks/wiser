using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.Queries.Models;
using Newtonsoft.Json.Linq;

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
        /// <param name="description">The description of the new query.</param>
        /// <returns></returns>
        Task<ServiceResult<QueryModel>> CreateAsync(ClaimsIdentity identity, string description);

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

        /// <summary>
        /// Execute a query and return the results in JSON format.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="id">The ID from wiser_query.</param>
        /// <param name="parameters">The parameters to set before executing the query.</param>
        /// <returns></returns>
        Task<ServiceResult<JArray>> GetQueryResultAsJsonAsync(ClaimsIdentity identity, int id, List<KeyValuePair<string, object>> parameters);
    }
}

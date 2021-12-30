using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.Queries.Models;

namespace Api.Modules.Queries.Interfaces
{
    //TODO Verify comments
    /// <summary>
    /// Service for getting queries for the Wiser export module.
    /// </summary>
    public interface IQueriesService
    {
        /// <summary>
        /// Gets the queries that can be used for an export in the export module.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <returns></returns>
        Task<ServiceResult<List<QueryModel>>> GetForExportModuleAsync(ClaimsIdentity identity);
    }
}

using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.TaskAlerts.Models;

namespace Api.Modules.TaskAlerts.Interfaces
{
    /// <summary>
    /// Service for getting task alerts.
    /// </summary>
    public interface ITaskAlertsService
    {
        /// <summary>
        /// Gets all task alerts for the user.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="getAllUsers">Whether the task alerts for all users should be retrieved instead of the only the current user.</param>
        /// <param name="branchDatabaseName">The name of a branch database to use. Set to null or empty to use current branch.</param>
        Task<ServiceResult<List<TaskAlertModel>>> GetAsync(ClaimsIdentity identity, bool getAllUsers = false, string branchDatabaseName = null);
    }
}

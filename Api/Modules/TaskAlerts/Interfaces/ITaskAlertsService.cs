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
        Task<ServiceResult<List<TaskAlertModel>>> GetAsync(ClaimsIdentity identity);
    }
}

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Modules.TaskAlerts.Interfaces;
using Api.Modules.TaskAlerts.Models;
using Microsoft.AspNetCore.Authorization;

namespace Api.Modules.TaskAlerts.Controllers
{
    /// <summary>
    /// Controller for getting task alerts.
    /// </summary>
    [Route("api/v3/task-alerts"), ApiController, Authorize]
    public class TaskAlertsController : ControllerBase
    {
        private readonly ITaskAlertsService taskAlertsService;

        /// <summary>
        /// Creates a new instance of <see cref="TaskAlertsController"/>.
        /// </summary>
        public TaskAlertsController(ITaskAlertsService taskAlertsService)
        {
            this.taskAlertsService = taskAlertsService;
        }
        
        /// <summary>
        /// Gets all task alerts for the user.
        /// </summary>
        [HttpGet, ProducesResponseType(typeof(List<TaskAlertModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetForExportModuleAsync()
        {
            return (await taskAlertsService.GetAsync((ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
        }
    }
}

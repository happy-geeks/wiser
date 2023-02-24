using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Modules.Dashboard.Enums;
using Api.Modules.Dashboard.Interfaces;
using Api.Modules.Dashboard.Models;
using GeeksCoreLibrary.Modules.WiserDashboard.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace Api.Modules.Dashboard.Controllers;

/// <summary>
/// A controller for the content builder, to get HTML, snippets etc.
/// </summary>
[Route("api/v3/dashboard")]
[ApiController]
[Authorize]
[Consumes(MediaTypeNames.Application.Json)]
[Produces(MediaTypeNames.Application.Json)]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService dashboardService;

    /// <summary>
    /// Creates a new instance of <see cref="DashboardController"/>.
    /// </summary>
    public DashboardController(IDashboardService dashboardService)
    {
        this.dashboardService = dashboardService;
    }

    /// <summary>
    /// Retrieve dashboard data.
    /// </summary>
    /// <param name="periodFrom">The minimum <see cref="DateTime"/> of the data.</param>
    /// <param name="periodTo">The maximum <see cref="DateTime"/> of the data.</param>
    /// <param name="branchId">Which branch should be used. A value of 0 means current branch, and -1 means all branches.</param>
    /// <param name="forceRefresh">Whether the data should be refreshed instead of cached data being used.</param>
    /// <returns>A <see cref="DashboardDataModel"/> object.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(DashboardDataModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get([FromQuery]DateTime? periodFrom = null, [FromQuery]DateTime? periodTo = null, [FromQuery]int branchId = 0, [FromQuery]bool forceRefresh = false)
    {
        return (await dashboardService.GetDataAsync((ClaimsIdentity)User.Identity, periodFrom, periodTo, branchId, forceRefresh)).GetHttpResponseMessage();
    }

    /// <summary>
    /// Gets the result of the data selector that has the "show in dashboard" option enabled, or null if no data
    /// selector has that option enabled.
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [Route("dataselector")]
    [ProducesResponseType(typeof(JToken), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(JToken), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDataSelectorResultAsync()
    {
        return (await dashboardService.GetDataSelectorResultAsync((ClaimsIdentity) User.Identity)).GetHttpResponseMessage();
    }

    /// <summary>
    /// Get the services from the WTS.
    /// </summary>
    /// <returns>A <see cref="Service"/> object containing various information about the usage of Wiser.</returns>
    [HttpGet]
    [Route("services")]
    [ProducesResponseType(typeof(List<Service>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWtsServicesAsync()
    {
        return (await dashboardService.GetWtsServicesAsync((ClaimsIdentity) User.Identity)).GetHttpResponseMessage();
    }

    /// <summary>
    /// Pause or unpause a service from the WTS.
    /// </summary>
    /// <param name="id">The ID of the service to change the state of.</param>
    /// <param name="state">The state to set. True to pause, false to unpause.</param>
    /// <returns></returns>
    [HttpPut]
    [Route("services/{id:int}/pause/{state:bool}")]
    [ProducesResponseType(typeof(ServicePauseStates), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServicePauseStates), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PauseServiceAsync(int id, bool state)
    {
        return (await dashboardService.SetWtsServicePauseStateAsync((ClaimsIdentity) User.Identity, id, state)).GetHttpResponseMessage();
    }
    
    /// <summary>
    /// Mark or unmark a service from the WTS to perform an extra run.
    /// </summary>
    /// <param name="id">The ID of the service to change the state of.</param>
    /// <param name="state">The state to set. True to mark, false to unmark.</param>
    /// <returns></returns>
    [HttpPut]
    [Route("services/{id:int}/extra-run/{state:bool}")]
    [ProducesResponseType(typeof(ServiceExtraRunStates), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceExtraRunStates), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExtraRunServiceAsync(int id, bool state)
    {
        return (await dashboardService.SetWtsServiceExtraRunStateAsync((ClaimsIdentity) User.Identity, id, state)).GetHttpResponseMessage();
    }

    /// <summary>
    /// Get the logs from a specific service.
    /// </summary>
    /// <param name="id">The ID of the service.</param>
    /// <returns></returns>
    [HttpGet]
    [Route("services/{id:int}/logs")]
    [ProducesResponseType(typeof(List<ServiceLog>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWtsServiceLogsAsync(int id)
    {
        return (await dashboardService.GetWtsServiceLogsAsync((ClaimsIdentity) User.Identity, id)).GetHttpResponseMessage();
    }
}
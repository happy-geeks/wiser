using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Modules.Dashboard.Interfaces;
using Api.Modules.Dashboard.Models;
using GeeksCoreLibrary.Modules.WiserDashboard.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
    /// Get the services from the AIS.
    /// </summary>
    /// <returns>A <see cref="Service"/> object containing various information about the usage of Wiser.</returns>
    [HttpGet, Route("services")]
    [ProducesResponseType(typeof(List<Service>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAisServicesAsync()
    {
        return (await dashboardService.GetAisServicesAsync((ClaimsIdentity) User.Identity)).GetHttpResponseMessage();
    }

    /// <summary>
    /// Pauses a service asynchronous
    /// </summary>
    /// <param name="id">The ID of the service that will be paused</param>
    /// <param name="state">The state of the service that will be paused</param>
    /// <returns></returns>
    [HttpPut, Route("services/{id:int}/pause/{state:bool}")]
    public async Task<IActionResult> PauseServiceAsync(int id, bool state)
    {
        return (await dashboardService.SetAisServicePauseStateAsync((ClaimsIdentity) User.Identity, id, state)).GetHttpResponseMessage();
    }

    /// <summary>
    /// Get the logs from a specific service.
    /// </summary>
    /// <param name="id">The ID of the service.</param>
    /// <returns></returns>
    [HttpGet, Route("services/{id:int}/logs")]
    public async Task<IActionResult> GetAisServiceLogsAsync(int id)
    {
        return (await dashboardService.GetAisServiceLogsAsync((ClaimsIdentity) User.Identity, id)).GetHttpResponseMessage();
    }
}
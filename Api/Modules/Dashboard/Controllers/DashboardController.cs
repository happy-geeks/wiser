using System;
using System.Net.Mime;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Modules.Dashboard.Enums;
using Api.Modules.Dashboard.Interfaces;
using Api.Modules.Dashboard.Models;
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
    /// <param name="periodFrom"></param>
    /// <param name="periodTo"></param>
    /// <param name="branchId"></param>
    /// <returns></returns>
    [HttpGet]
    [ProducesResponseType(typeof(DashboardDataModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get([FromQuery]DateTime? periodFrom = null, [FromQuery]DateTime? periodTo = null, [FromQuery]int branchId = 0)
    {
        return (await dashboardService.GetDataAsync((ClaimsIdentity)User.Identity, periodFrom, periodTo, branchId)).GetHttpResponseMessage();
    }
}
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.Dashboard.Enums;
using Api.Modules.Dashboard.Models;
using GeeksCoreLibrary.Modules.WiserDashboard.Models;

namespace Api.Modules.Dashboard.Interfaces;

/// <summary>
/// A service for the dashboard, to get usage data, etc.
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Retrieves the data for the dashboard. The default data will be cached for a day (in the database), but can optionally be forced to be refreshed.
    /// If <paramref name="periodFrom"/> and/or <paramref name="periodTo"/> is set, the data will always be retrieved on-demand.
    /// </summary>
    /// <param name="identity">The identity of the authenticated user.</param>
    /// <param name="periodFrom"></param>
    /// <param name="periodTo"></param>
    /// <param name="branchId"></param>
    /// <param name="forceRefresh">Whether to force a refresh of the data.</param>
    /// <returns>A <see cref="DashboardDataModel"/> object containing various information about the usage of Wiser.</returns>
    Task<ServiceResult<DashboardDataModel>> GetDataAsync(ClaimsIdentity identity, DateTime? periodFrom = null, DateTime? periodTo = null, int branchId = 0, bool forceRefresh = false);

    /// <summary>
    /// Retrieves the latest state of the services managed by the WTS.
    /// </summary>
    /// <param name="identity">The identity of the authenticated user.</param>
    /// <returns>A collection of <see cref="Service"/> objects.</returns>
    Task<ServiceResult<List<Service>>> GetWtsServicesAsync(ClaimsIdentity identity);

    /// <summary>
    /// Retrieves the logs from the given service.
    /// </summary>
    /// <param name="identity">The identity of the authenticated user.</param>
    /// <param name="id">The ID of the service to get the logs from.</param>
    /// <returns>A collection of logs.</returns>
    Task<ServiceResult<List<ServiceLog>>> GetWtsServiceLogsAsync(ClaimsIdentity identity, int id);

    /// <summary>
    /// Set the pause state of a service.
    /// </summary>
    /// <param name="identity">The identity of the authenticated user.</param>
    /// <param name="id">The ID of the service to set the pause state of.</param>
    /// <param name="state">The pause state.</param>
    /// <returns>Returns the pause state based on the given action.</returns>
    Task<ServiceResult<ServicePauseStates>> SetWtsServicePauseStateAsync(ClaimsIdentity identity, int id, bool state);
}
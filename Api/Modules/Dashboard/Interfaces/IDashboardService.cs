using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.Dashboard.Enums;
using Api.Modules.Dashboard.Models;

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
    /// <param name="itemsDataPeriodFilterType"></param>
    /// <param name="branchId"></param>
    /// <param name="forceRefresh">Whether to force a refresh of the data.</param>
    /// <returns>A <see cref="DashboardDataModel"/> object containing various information about the usage of Wiser.</returns>
    Task<ServiceResult<DashboardDataModel>> GetDataAsync(ClaimsIdentity identity, DateTime? periodFrom = null, DateTime? periodTo = null, ItemsDataPeriodFilterTypes itemsDataPeriodFilterType = ItemsDataPeriodFilterTypes.All, int branchId = 0, bool forceRefresh = false);
}
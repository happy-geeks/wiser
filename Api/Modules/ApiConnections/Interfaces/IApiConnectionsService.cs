using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.ApiConnections.Models;

namespace Api.Modules.ApiConnections.Interfaces;

/// <summary>
/// A service for communication with external APIs and for getting/saving settings for it.
/// </summary>
public interface IApiConnectionsService
{
    /// <summary>
    /// Gets the settings for all external API connections.
    /// </summary>
    /// <param name="identity">The authenticated user.</param>
    /// <returns>A list of <see cref="ApiConnectionModel"/>s with the settings.</returns>
    Task<ServiceResult<List<ApiConnectionModel>>> GetSettingsAsync(ClaimsIdentity identity);
    
    /// <summary>
    /// Gets the settings for communicating with an external API.
    /// </summary>
    /// <param name="identity">The authenticated user.</param>
    /// <param name="id">The ID of the API settings.</param>
    /// <returns>An <see cref="ApiConnectionModel">ApiConnectionModel</see> with the settings.</returns>
    Task<ServiceResult<ApiConnectionModel>> GetSettingsAsync(ClaimsIdentity identity, int id);
}
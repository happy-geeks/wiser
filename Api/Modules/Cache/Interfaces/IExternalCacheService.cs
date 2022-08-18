using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.Cache.Models;

namespace Api.Modules.Cache.Interfaces;

/// <summary>
/// Service for manipulating the cache of an GCL website.
/// </summary>
public interface IExternalCacheService
{
    /// <summary>
    /// Clears the cache of a GCL website. You can chose which cache area(s) to clear.
    /// </summary>
    /// <param name="settings">The settings for clearing the cache.</param>
    public Task<ServiceResult<bool>> ClearCacheAsync(ClearCacheSettingsModel settings);
}
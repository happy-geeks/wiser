using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.Modules.Models;

namespace Api.Modules.Modules.Interfaces
{
    /// <summary>
    /// Service for getting information / settings for Wiser 2.0+ modules.
    /// </summary>
    public interface IModulesService
    {
        /// <summary>
        /// Gets the list of modules that the authenticated user has access to.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <returns></returns>
        Task<ServiceResult<SortedList<string, List<ModuleAccessRightsModel>>>> GetAsync(ClaimsIdentity identity);

        /// <summary>
        /// Gets the settings for a single module.
        /// </summary>
        /// <param name="id">The ID of the module.</param>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <returns></returns>
        Task<ServiceResult<ModuleSettingsModel>> GetSettingsAsync(int id, ClaimsIdentity identity);

        /// <summary>
        /// Exports a module to Excel. Only works for grid view modules.
        /// </summary>
        /// <param name="id">The ID of the module.</param>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <returns></returns>
        Task<ServiceResult<byte[]>> ExportAsync(int id, ClaimsIdentity identity);
    }
}
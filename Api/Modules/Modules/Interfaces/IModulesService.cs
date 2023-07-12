using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.Modules.Models;

namespace Api.Modules.Modules.Interfaces
{
    /// <summary>
    /// Service for getting information / settings for Wiser modules.
    /// </summary>
    public interface IModulesService
    {
        /// <summary>
        /// Gets the list of modules that the authenticated user has access to.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <returns></returns>
        Task<ServiceResult<Dictionary<string, List<ModuleAccessRightsModel>>>> GetAsync(ClaimsIdentity identity);

        /// <summary>
        /// Gets the settings for all modules.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <returns></returns>
        Task<ServiceResult<List<ModuleSettingsModel>>> GetSettingsAsync(ClaimsIdentity identity);

        /// <summary>
        /// Gets the settings for a single module.
        /// </summary>
        /// <param name="id">The ID of the module.</param>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="encryptValues">Optional: Whether to encrypt values in the JSON settings, such as queryId. Default is true. Set to false when getting the settings for the admin module.</param>
        /// <returns></returns>
        Task<ServiceResult<ModuleSettingsModel>> GetSettingsAsync(int id, ClaimsIdentity identity, bool encryptValues = true);

        /// <summary>
        /// Creates a new module
        /// </summary>
        /// <param name="name">The name of the module</param>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <returns>The id of new module</returns>
        Task<ServiceResult<int>> CreateAsync(string name, ClaimsIdentity identity);

        /// <summary>
        /// Update the settings for a single module.
        /// </summary>
        /// <param name="id">The ID of the module.</param>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="moduleSettingsModel">Module setting</param>
        /// <returns></returns>
        Task<ServiceResult<bool>> UpdateSettingsAsync(int id, ClaimsIdentity identity, ModuleSettingsModel moduleSettingsModel);

        /// <summary>
        /// Exports a module to Excel. Only works for grid view modules.
        /// </summary>
        /// <param name="id">The ID of the module.</param>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <returns></returns>
        Task<ServiceResult<byte[]>> ExportAsync(int id, ClaimsIdentity identity);

        /// <summary>
        /// Gets a list of all currently used module groups.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <returns>A list with all group names.</returns>
        Task<ServiceResult<List<string>>> GetModuleGroupsAsync(ClaimsIdentity identity);

        /// <summary>
        /// Deletes a module.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="id">The ID of the module.</param>
        Task<ServiceResult<bool>> DeleteAsync(ClaimsIdentity identity, int id);
    }
}
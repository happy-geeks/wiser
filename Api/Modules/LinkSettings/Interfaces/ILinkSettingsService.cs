using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using GeeksCoreLibrary.Core.Models;

namespace Api.Modules.LinkSettings.Interfaces
{
    /// <summary>
    /// Interface for all CRUD operations for link settings (from the table wiser_link).
    /// </summary>
    public interface ILinkSettingsService
    {
        /// <summary>
        /// Get all link settings.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="branchId">Optional: If getting entity types for a branch, enter the ID of that branch here.</param>
        /// <returns>A List of <see cref="LinkSettingsModel"/> with all settings.</returns>
        Task<ServiceResult<List<LinkSettingsModel>>> GetAllAsync(ClaimsIdentity identity, int branchId = 0);

        /// <summary>
        /// Get link settings based on ID.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="id">The ID of the settings from wiser_link.</param>
        /// <returns>A <see cref="LinkSettingsModel"/> with all settings.</returns>
        Task<ServiceResult<LinkSettingsModel>> GetAsync(ClaimsIdentity identity, int id);

        /// <summary>
        /// Creates new link settings.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="linkSettings">The link settings to create.</param>
        /// <returns>The newly created link settings.</returns>
        Task<ServiceResult<LinkSettingsModel>> CreateAsync(ClaimsIdentity identity, LinkSettingsModel linkSettings);

        /// <summary>
        /// Updates existing link settings.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="id">The ID of the link settings to update.</param>
        /// <param name="linkSettings">The new data to save.</param>
        Task<ServiceResult<bool>> UpdateAsync(ClaimsIdentity identity, int id, LinkSettingsModel linkSettings);

        /// <summary>
        /// Deletes link settings.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="id">The ID of the link settings to delete.</param>
        Task<ServiceResult<bool>> DeleteAsync(ClaimsIdentity identity, int id);
    }
}
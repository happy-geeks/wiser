using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using GeeksCoreLibrary.Modules.Communication.Enums;
using GeeksCoreLibrary.Modules.Communication.Models;

namespace Api.Modules.Communication.Interfaces;

/// <summary>
/// A service for loading and saving communication settings in the communication module.
/// </summary>
public interface ICommunicationsService
{
    /// <summary>
    /// Get the settings of a specific row from wiser_communication.
    /// </summary>
    /// <param name="id">The ID of the communication settings to get.</param>
    /// <param name="nameOnly">Optional: Whether to only get the name (and ID) or everything.</param>
    /// <returns>A <see cref="CommunicationSettingsModel"/> with the settings, or <see langword="null"/> if it doesn't exist.</returns>
    Task<ServiceResult<CommunicationSettingsModel>> GetSettingsAsync(int id, bool nameOnly = false);

    /// <summary>
    /// Get the settings of all communications of a specific type (such as SMS or e-mail).
    /// </summary>
    /// <param name="type">Optional: The <see cref="CommunicationTypes"/> to get the settings for. Leave null to get everything.</param>
    /// <param name="namesOnly">Optional: Whether to only get the names (and IDs) or everything.</param>
    /// <returns>A list of <see cref="CommunicationSettingsModel"/>.</returns>
    Task<ServiceResult<List<CommunicationSettingsModel>>> GetSettingsAsync(CommunicationTypes? type = null, bool namesOnly = false);

    /// <summary>
    /// Create new settings or updates existing settings (based on <see cref="CommunicationSettingsModel.Id"/>).
    /// </summary>
    /// <param name="identity">The <see cref="ClaimsIdentity"/> of the authenticated user.</param>
    /// <param name="settings">The <see cref="CommunicationSettingsModel"/> to create or update.</param>
    Task<ServiceResult<CommunicationSettingsModel>> SaveSettingsAsync(ClaimsIdentity identity, CommunicationSettingsModel settings);

    /// <summary>
    /// Deletes a row of communication settings.
    /// </summary>
    /// <param name="identity">The <see cref="ClaimsIdentity"/> of the authenticated user.</param>
    /// <param name="id">The ID of the settings to delete.</param>
    Task<ServiceResult<bool>> DeleteSettingsAsync(ClaimsIdentity identity, int id);

    /// <summary>
    /// Send an e-mail to someone.
    /// </summary>
    /// <param name="identity">The <see cref="ClaimsIdentity"/> of the authenticated user.</param>
    /// <param name="communication">The <see cref="SingleCommunicationModel"/> with information for sending the e-mail.</param>
    Task<ServiceResult<bool>> SendEmailAsync(ClaimsIdentity identity, SingleCommunicationModel communication);
}
using System.Threading.Tasks;
using Api.Core.Services;
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
    /// <returns>A <see cref="CommunicationSettingsModel"/> with the settings, or <see langword="null"/> if it doesn't exist.</returns>
    Task<ServiceResult<CommunicationSettingsModel>> GetSettingsAsync(int id);

    /// <summary>
    /// Create new settings or updates existing settings (based on <see cref="CommunicationSettingsModel.Id"/>).
    /// </summary>
    /// <param name="settings">The <see cref="CommunicationSettingsModel"/> to create or update.</param>
    Task<ServiceResult<CommunicationSettingsModel>> SaveSettingsAsync(CommunicationSettingsModel settings);

    /// <summary>
    /// Deletes a row of communication settings.
    /// </summary>
    /// <param name="id">The ID of the settings to delete.</param>
    Task<ServiceResult<bool>> DeleteSettingsAsync(int id);
}
using FrontEnd.Core.Models;

namespace FrontEnd.Modules.Communication.Models;

public class CommunicationSettingsViewModel : BaseViewModel
{
    /// <summary>
    /// Gets or sets the ID for the communication settings. Should be 0 when creating new settings.
    /// </summary>
    public int SettingsId { get; set; }
    
    /// <summary>
    /// Gets or sets the name for the settings. Only used when creating new settings.
    /// </summary>
    public string SettingsName { get; set; }
}
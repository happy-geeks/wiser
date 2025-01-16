namespace Api.Modules.CloudFlare.Models;

/// <summary>
/// The model for CloudFlare Settings.
/// These settings are used to connect to CloudFlare to upload images.
/// Will be filled with SystemObjectValues
/// </summary>
public class CloudFlareSettingsModel
{
    /// <summary>
    /// Gets or sets the Authorization Key
    /// </summary>
    public string AuthorizationKey { get; set; }

    /// <summary>
    /// Gets or sets the Authorization Email
    /// </summary>
    public string AuthorizationEmail { get; set; }

    /// <summary>
    /// Gets or sets the AccountId
    /// </summary>
    public string AccountId { get; set; }
}
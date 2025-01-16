namespace Api.Modules.DigitalOcean.Models;

/// <summary>
/// Settings for connecting with the Digital Ocean API.
/// </summary>
public class DigitalOceanSettings
{
    /// <summary>
    /// Gets or sets the client ID.
    /// </summary>
    public string ClientId { get; set; }

    /// <summary>
    /// Gets or sets the client secret.
    /// </summary>
    public string ClientSecret { get; set; }
}
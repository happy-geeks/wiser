namespace Api.Core.Models;

/// <summary>
/// A model for settings for Google authentication.
/// </summary>
public class GoogleAuthenticationSettings
{
    /// <summary>
    /// Gets or sets the client ID from Google.
    /// </summary>
    public string ClientId { get; set; }

    /// <summary>
    /// Gets or sets the client secret from Google.
    /// </summary>
    public string ClientSecret { get; set; }
}
namespace Api.Modules.Customers.Models;

/// <summary>
/// Model for external login request.
/// </summary>
public class ExternalLoginRequestModel
{
    /// <summary>
    /// Gets or sets the provider (Google, Facebook etc) used for external login.
    /// </summary>
    public string Provider { get; set; }

    /// <summary>
    /// Gets or sets the authentication token that was provided by the external login provider.
    /// </summary>
    public string Token { get; set; }
}
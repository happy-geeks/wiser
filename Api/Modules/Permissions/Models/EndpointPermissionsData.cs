namespace Api.Modules.Permissions.Models;

/// <summary>
/// Model containing extra data needed for endpoint permissions.
/// </summary>
public class EndpointPermissionsData : PermissionData
{
    /// <summary>
    /// Gets or sets the URL for the endpoint.
    /// </summary>
    public string EndpointUrl { get; set; }

    /// <summary>
    /// Gets or sets the HTTP method for the endpoint.
    /// </summary>
    public string EndpointHttpMethod { get; set; }
}
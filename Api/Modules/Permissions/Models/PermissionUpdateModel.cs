using Api.Modules.Permissions.Enums;
using GeeksCoreLibrary.Core.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Api.Modules.Permissions.Models;

/// <summary>
/// Model used to update the permissions for a given subject and role
/// </summary>
public class PermissionUpdateModel
{
    /// <summary>
    /// Gets or sets the ID of the permission.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the subject the permission is set for.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public PermissionSubjects Subject { get; set; }

    /// <summary>
    /// Gets or sets the id of the item the permission is set for
    /// </summary>
    public int SubjectId { get; set; }

    /// <summary>
    /// Gets or sets the access rights the role has for the subject
    /// </summary>
    public AccessRights Permission { get; set; }

    /// <summary>
    /// Gets or sets the id of the role the permission is set for
    /// </summary>
    public int RoleId { get; set; }

    /// <summary>
    /// Gets or sets the URL of the endpoint, if this is a permission for an endpoint on an API of website.
    /// </summary>
    public string EndpointUrl { get; set; }

    /// <summary>
    /// Gets or sets the HTTP method of the endpoint, if this is a permission for an endpoint on an API of website.
    /// </summary>
    public string EndpointHttpMethod { get; set; }
}
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
    public AccessRights PermissionCode { get; set; }

    /// <summary>
    /// Gets or sets the id of the role the permission is set for
    /// </summary>
    public int RoleId { get; set; }
}
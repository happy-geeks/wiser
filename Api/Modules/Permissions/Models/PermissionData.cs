namespace Api.Modules.Permissions.Models;

/// <summary>
/// Model containing permission data for a role
/// </summary>
public class PermissionData
{
    /// <summary>
    /// Gets or sets the ID of the permission.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the id of the role the permission is set for
    /// </summary>
    public int RoleId { get; set; }

    /// <summary>
    /// Gets or sets the name of the role the permission is set for
    /// </summary>
    public string RoleName { get; set; }

    /// <summary>
    /// Gets or sets the access rights the role has for the subject
    /// </summary>
    public int Permission { get; set; }

    /// <summary>
    /// Gets or sets the id of the object the permission is set for
    /// </summary>
    public int ObjectId { get; set; }

    /// <summary>
    /// Gets or sets the name of the object the permission is set for
    /// </summary>
    public string ObjectName { get; set; }
}
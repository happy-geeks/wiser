using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Api.Modules.Permissions.Enums;

/// <summary>
/// Enumeration of subjects to set permissions for
/// </summary>
public enum PermissionSubject
{
    /// <summary>
    /// This value means something went wrong when setting the value
    /// Added in case deserialization of the enum goes wrong
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Represents the permission options for wiser modules
    /// </summary>
    Modules = 1,
    /// <summary>
    /// Represents the permission options for wiser queries
    /// </summary>
    Queries = 2
}
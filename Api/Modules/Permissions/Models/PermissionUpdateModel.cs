using Api.Modules.Permissions.Enums;
using GeeksCoreLibrary.Core.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Api.Modules.Permissions.Models;

public class PermissionUpdateModel
{
    [JsonConverter(typeof(StringEnumConverter))]
    public PermissionSubject Subject { get; set; }

    public int SubjectId { get; set; }

    public AccessRights PermissionCode { get; set; }

    public int RoleId { get; set; }
}
using GeeksCoreLibrary.Core.Enums;
using Newtonsoft.Json;

namespace Api.Modules.Permissions.Models;

public abstract class BasePermissionData
{
    public int RoleId { get; set; }

    public string RoleName { get; set; }
    
    public int Permission { get; set; }
}
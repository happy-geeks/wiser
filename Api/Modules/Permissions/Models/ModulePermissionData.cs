namespace Api.Modules.Permissions.Models;

public class ModulePermissionData : BasePermissionData
{
    public int ModuleId { get; set; }
    public string ModuleName { get; set; }
}
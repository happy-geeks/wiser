namespace Api.Modules.Permissions.Models;

public class QueryPermissionData : BasePermissionData
{
    public int QueryId { get; set; }

    public string QueryName { get; set; }
}
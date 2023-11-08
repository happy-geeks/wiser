using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.Permissions.Enums;
using Api.Modules.Permissions.Interfaces;
using Api.Modules.Permissions.Models;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Modules.Databases.Interfaces;

namespace Api.Modules.Permissions.Services;

/// <inheritdoc cref="Api.Modules.Permissions.Interfaces.IPermissionsService" />
public class PermissionsService : IPermissionsService, IScopedService
{
    private readonly IDatabaseConnection databaseConnection;
    
    /// <summary>
    /// Creates a new instance of PermissionsService.
    /// </summary>
    /// <param name="databaseConnection"></param>
    public PermissionsService(IDatabaseConnection databaseConnection)
    {
        this.databaseConnection = databaseConnection;
    }

    /// <inheritdoc />
    public async Task<ServiceResult<bool>> SetPermissionAsync(PermissionUpdateModel permissionUpdateModel)
    {
        var subjectIdColumn = permissionUpdateModel.Subject switch
        {
            PermissionSubject.Modules => "module_id",
            PermissionSubject.Queries => "query_id",
            _ => throw new NotImplementedException($"Used {nameof(PermissionSubject)} value has not yet been implemented")
        };
        
        var query = $@"INSERT INTO `wiser_permission` (
     `role_id`,
     `entity_name`,
     `item_id`,
     `entity_property_id`,
     `permissions`,
      `{subjectIdColumn}`
 ) 
 VALUES (
     ?roleId, 
     '',
     0,
     0,
     ?permissionCode,
     ?subjectId
 )
ON DUPLICATE KEY UPDATE permissions = ?permissionCode;";
        
        databaseConnection.ClearParameters();
        databaseConnection.AddParameter("roleId", permissionUpdateModel.RoleId);
        databaseConnection.AddParameter("permissionCode", (int)permissionUpdateModel.PermissionCode);
        databaseConnection.AddParameter("subjectId",permissionUpdateModel.SubjectId);

        var result = await databaseConnection.ExecuteAsync(query);

        return new ServiceResult<bool>(result > 0);
    }

    /// <inheritdoc />
    public async Task<ServiceResult<IList<BasePermissionData>>> GetPermissionsAsync(int roleId, PermissionSubject subject)
    {
        var result = subject switch
        {
            PermissionSubject.Modules => await GetModulePermissionsAsync(roleId),
            PermissionSubject.Queries => await GetQueryPermissionsAsync(roleId),
            _ => throw new NotImplementedException($"Used {nameof(PermissionSubject)} value has not yet been implemented")
        };
        return new ServiceResult<IList<BasePermissionData>>(result);
    }

    private async Task<IList<BasePermissionData>> GetQueryPermissionsAsync(int roleId)
    {
        string query = @"SELECT
	role.id AS `roleId`,
	role.role_name AS `roleName`,
	`query`.id AS `queryId`,
	IFNULL(`query`.description, CONCAT('QueryID: ',`query`.id)) AS `queryName`,
	IFNULL(permission.permissions, 0) AS `permission`
FROM wiser_query AS `query`
JOIN wiser_roles AS role ON role.id = ?roleId
LEFT JOIN wiser_permission AS permission ON role.id = permission.role_id AND permission.query_id = `query`.id
ORDER BY queryName ASC";
        
        databaseConnection.ClearParameters();
        databaseConnection.AddParameter("roleId", roleId);

        var data = await databaseConnection.GetReaderAsync(query);

        var list = new List<BasePermissionData>();
        while (await data.ReadAsync())
        {
            list.Add(new QueryPermissionData()
            {
                RoleId = data.GetInt32(0),
                RoleName = data.GetString(1),
                QueryId = data.GetInt32(2),
                QueryName = data.GetString(3),
                Permission = data.GetInt32(4)
            });
        }
        return list;
    }
    
    private async Task<IList<BasePermissionData>> GetModulePermissionsAsync(int roleId)
    {
        string query = @"SELECT
	role.id AS `roleId`,
	role.role_name AS `roleName`,
	module.id AS `moduleId`,
	IFNULL(module.name, CONCAT('ModuleID: ',module.id)) AS `moduleName`,
	IFNULL(permission.permissions, 0) AS `permission`
FROM wiser_module AS module
JOIN wiser_roles AS role ON role.id = ?roleId
LEFT JOIN wiser_permission AS permission ON role.id = permission.role_id AND permission.module_id = module.id
ORDER BY moduleName ASC
";
        
        databaseConnection.ClearParameters();
        databaseConnection.AddParameter("roleId", roleId);

        var data = await databaseConnection.GetReaderAsync(query);

        var list = new List<BasePermissionData>();
        while (await data.ReadAsync())
        {
            list.Add(new ModulePermissionData()
            {
                RoleId = data.GetInt32(0),
                RoleName = data.GetString(1),
                ModuleId = data.GetInt32(2),
                ModuleName = data.GetString(3),
                Permission = data.GetInt32(4)
            });
        }
        return list;
    }
}
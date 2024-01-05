using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.Permissions.Enums;
using Api.Modules.Permissions.Interfaces;
using Api.Modules.Permissions.Models;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Models;
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
            PermissionSubjects.Modules => "module_id",
            PermissionSubjects.Queries => "query_id",
            _ => throw new ArgumentOutOfRangeException($"Used {nameof(PermissionSubjects)} value has not yet been implemented")
        };

        var query = $@"INSERT INTO `{WiserTableNames.WiserPermission}` (
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
    public async Task<ServiceResult<IList<PermissionData>>> GetPermissionsAsync(int roleId, PermissionSubjects subject)
    {
        var result = subject switch
        {
            PermissionSubjects.Modules => await GetModulePermissionsAsync(roleId),
            PermissionSubjects.Queries => await GetQueryPermissionsAsync(roleId),
            _ => throw new ArgumentOutOfRangeException($"Used {nameof(PermissionSubjects)} value has not yet been implemented")
        };
        return new ServiceResult<IList<PermissionData>>(result);
    }

    private Task<IList<PermissionData>> GetQueryPermissionsAsync(int roleId)
    {
        var query = $@"SELECT
	role.id AS `roleId`,
	role.role_name AS `roleName`,
	`query`.id AS `queryId`,
	IFNULL(`query`.description, CONCAT('QueryID: ',`query`.id)) AS `queryName`,
	IFNULL(permission.permissions, 0) AS `permission`
FROM {WiserTableNames.WiserQuery} AS `query`
JOIN {WiserTableNames.WiserRoles} AS role ON role.id = ?roleId
LEFT JOIN wiser_permission AS permission ON role.id = permission.role_id AND permission.query_id = `query`.id
ORDER BY queryName ASC";
        databaseConnection.ClearParameters();
        databaseConnection.AddParameter("roleId", roleId);

        return GetPermissionsDataAsync(query);
    }

    private Task<IList<PermissionData>> GetModulePermissionsAsync(int roleId)
    {
        var query = $@"SELECT
	role.id AS `roleId`,
	role.role_name AS `roleName`,
	module.id AS `moduleId`,
	IFNULL(module.name, CONCAT('ModuleID: ',module.id)) AS `moduleName`,
	IFNULL(permission.permissions, 0) AS `permission`
FROM {WiserTableNames.WiserModule} AS module
JOIN {WiserTableNames.WiserRoles} AS role ON role.id = ?roleId
LEFT JOIN wiser_permission AS permission ON role.id = permission.role_id AND permission.module_id = module.id
ORDER BY moduleName ASC
";
        databaseConnection.ClearParameters();
        databaseConnection.AddParameter("roleId", roleId);

        return GetPermissionsDataAsync(query);
    }


    private async Task<IList<PermissionData>> GetPermissionsDataAsync(string query)
    {
        var data = await databaseConnection.GetReaderAsync(query);

        var list = new List<PermissionData>();
        while (await data.ReadAsync())
        {
            list.Add(new PermissionData
            {
                RoleId = data.GetInt32(0),
                RoleName = data.GetString(1),
                ObjectId = data.GetInt32(2),
                ObjectName = data.GetString(3),
                Permission = data.GetInt32(4)
            });
        }
        return list;
    }
}
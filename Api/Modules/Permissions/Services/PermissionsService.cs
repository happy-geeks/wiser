using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
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
    public async Task<ServiceResult<bool>> SetAsync(PermissionUpdateModel permissionUpdateModel)
    {
        var subjectIdColumn = permissionUpdateModel.Subject switch
        {
            PermissionSubjects.Modules => "module_id",
            PermissionSubjects.Queries => "query_id",
            PermissionSubjects.Endpoints => "endpoint_url",
            _ => throw new ArgumentOutOfRangeException($"Used {nameof(PermissionSubjects)} value has not yet been implemented")
        };

        var query = $"""
                     INSERT INTO `{WiserTableNames.WiserPermission}` (
                          `role_id`,
                          `entity_name`,
                          `item_id`,
                          `entity_property_id`,
                          `permissions`,
                          `{subjectIdColumn}`
                          {(permissionUpdateModel.Subject == PermissionSubjects.Endpoints ? ", endpoint_http_method" : "")}
                      ) 
                      VALUES (
                          ?roleId, 
                          '',
                          0,
                          0,
                          ?permissionCode,
                          ?subjectId
                          {(permissionUpdateModel.Subject == PermissionSubjects.Endpoints ? ", ?endpointHttpMethod" : "")}
                      )
                     ON DUPLICATE KEY UPDATE permissions = ?permissionCode;
                     """;

        if (permissionUpdateModel.Id > 0)
        {
            query = $"""
                     UPDATE `{WiserTableNames.WiserPermission}`
                     SET permissions = ?permissionCode,
                     `{subjectIdColumn}` = ?subjectId
                     {(permissionUpdateModel.Subject == PermissionSubjects.Endpoints ? ", endpoint_http_method = ?endpointHttpMethod" : "")}
                     WHERE id = ?id;
                     """;
        }

        databaseConnection.ClearParameters();
        databaseConnection.AddParameter("id", permissionUpdateModel.Id);
        databaseConnection.AddParameter("roleId", permissionUpdateModel.RoleId);
        databaseConnection.AddParameter("permissionCode", (int)permissionUpdateModel.Permission);

        if (permissionUpdateModel.Subject == PermissionSubjects.Endpoints)
        {
            databaseConnection.AddParameter("subjectId", permissionUpdateModel.EndpointUrl);
            databaseConnection.AddParameter("endpointHttpMethod", permissionUpdateModel.EndpointHttpMethod);
        }
        else
        {
            databaseConnection.AddParameter("subjectId", permissionUpdateModel.SubjectId);
        }

        var result = await databaseConnection.ExecuteAsync(query);

        return new ServiceResult<bool>(result > 0);
    }

    /// <inheritdoc />
    public async Task<ServiceResult<bool>> DeleteAsync(int id)
    {
        var query = $"DELETE FROM {WiserTableNames.WiserPermission} WHERE id = ?id";
        databaseConnection.AddParameter("id", id);
        var affectedRows = await databaseConnection.ExecuteAsync(query);
        return new ServiceResult<bool>(affectedRows > 0);
    }

    /// <inheritdoc />
    public async Task<ServiceResult<IList<PermissionData>>> GetAsync(int roleId, PermissionSubjects subject)
    {
        var result = subject switch
        {
            PermissionSubjects.Modules => await GetModulePermissionsAsync(roleId),
            PermissionSubjects.Queries => await GetQueryPermissionsAsync(roleId),
            PermissionSubjects.Endpoints => await GetEndpointPermissionsAsync(roleId),
            _ => throw new ArgumentOutOfRangeException($"Used {nameof(PermissionSubjects)} value has not yet been implemented")
        };

        return new ServiceResult<IList<PermissionData>>(result);
    }

    private async Task<IList<PermissionData>> GetQueryPermissionsAsync(int roleId)
    {
        var query = $@"SELECT
	permission.id,
	role.id AS `roleId`,
	role.role_name AS `roleName`,
	`query`.id AS `objectId`,
	IFNULL(`query`.description, CONCAT('QueryID: ',`query`.id)) AS `objectName`,
	IFNULL(permission.permissions, 0) AS `permission`
FROM {WiserTableNames.WiserQuery} AS `query`
JOIN {WiserTableNames.WiserRoles} AS role ON role.id = ?roleId
LEFT JOIN {WiserTableNames.WiserPermission} AS permission ON role.id = permission.role_id AND permission.query_id = `query`.id
ORDER BY objectName ASC";
        databaseConnection.ClearParameters();
        databaseConnection.AddParameter("roleId", roleId);

        return await GetPermissionsDataAsync<PermissionData>(query).ToListAsync();
    }

    private async Task<IList<PermissionData>> GetModulePermissionsAsync(int roleId)
    {
        var query = $@"SELECT
	permission.id,
	role.id AS `roleId`,
	role.role_name AS `roleName`,
	module.id AS `objectId`,
	IFNULL(module.name, CONCAT('ModuleID: ',module.id)) AS `objectName`,
	IFNULL(permission.permissions, 0) AS `permission`
FROM {WiserTableNames.WiserModule} AS module
JOIN {WiserTableNames.WiserRoles} AS role ON role.id = ?roleId
LEFT JOIN {WiserTableNames.WiserPermission} AS permission ON role.id = permission.role_id AND permission.module_id = module.id
ORDER BY objectName ASC";
        databaseConnection.ClearParameters();
        databaseConnection.AddParameter("roleId", roleId);

        return await GetPermissionsDataAsync<PermissionData>(query).ToListAsync();
    }

    private async Task<IList<PermissionData>> GetEndpointPermissionsAsync(int roleId)
    {
        var query = $@"SELECT
	permission.id,
	role.id AS `roleId`,
	role.role_name AS `roleName`,
    permission.endpoint_url AS `endpointUrl`,
    permission.endpoint_http_method AS `endpointHttpMethod`,
	IFNULL(permission.permissions, 0) AS `permission`
FROM {WiserTableNames.WiserPermission} AS permission
JOIN {WiserTableNames.WiserRoles} AS role ON role.id = permission.role_id
WHERE permission.role_id = ?roleId
AND permission.endpoint_url != ''
ORDER BY endpointUrl ASC";
        databaseConnection.ClearParameters();
        databaseConnection.AddParameter("roleId", roleId);

        var data = await databaseConnection.GetAsync(query);
        var result = new List<PermissionData>();
        foreach (DataRow dataRow in data.Rows)
        {
             var permissionData = DataRowToPermissionData<EndpointPermissionsData>(dataRow);
             permissionData.EndpointUrl = dataRow.Field<string>("endpointUrl");
             permissionData.EndpointHttpMethod = dataRow.Field<string>("endpointHttpMethod");
             result.Add(permissionData);
        }

        return result;
    }

    private async IAsyncEnumerable<T> GetPermissionsDataAsync<T>(string query) where T : PermissionData, new()
    {
        var data = await databaseConnection.GetAsync(query);
        foreach (DataRow dataRow in data.Rows)
        {
            yield return DataRowToPermissionData<T>(dataRow);
        }
    }

    private static T DataRowToPermissionData<T>(DataRow dataRow) where T : PermissionData, new()
    {
        var permissionData = new T
        {
            RoleId = Convert.ToInt32(dataRow["roleId"]),
            RoleName = dataRow.Field<string>("roleName"),
            Permission = Convert.ToInt32(dataRow["permission"])
        };

		if (!dataRow.IsNull("id"))
		{
			permissionData.Id = Convert.ToInt32(dataRow["id"]);
		}

        if (!dataRow.Table.Columns.Contains("objectId"))
        {
            return permissionData;
        }

        permissionData.ObjectId = Convert.ToInt32(dataRow["objectId"]);
        permissionData.ObjectName = dataRow.Field<string>("objectName");

        return permissionData;
    }
}
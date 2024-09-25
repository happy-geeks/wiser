using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.Permissions.Enums;
using Api.Modules.Permissions.Models;

namespace Api.Modules.Permissions.Interfaces;

/// <summary>
/// Service for management of permissions.
/// </summary>
public interface IPermissionsService
{
    /// <summary>
    /// Set role permissions for the given subject.
    /// </summary>
    /// <param name="permissionUpdateModel">Model containing the data used to set the permissions.</param>
    /// <returns>Boolean whether any permission were set.</returns>>
    Task<ServiceResult<bool>> SetAsync(PermissionUpdateModel permissionUpdateModel);

    /// <summary>
    /// Delete a permission by id.
    /// </summary>
    /// <param name="id">The ID of the permission.</param>
    /// <returns>Boolean whether the delete action was successful.</returns>
    Task<ServiceResult<bool>> DeleteAsync(int id);

    /// <summary>
    /// Get role permissions for the given subject.
    /// </summary>
    /// <param name="roleId">The role id of the role for which the data should be retrieved</param>
    /// <param name="subject">The subject for which the data should be retrieved. For possible values see <see cref="PermissionSubjects"/>.</param>
    /// <returns>List of permission data</returns>
    Task<ServiceResult<IList<PermissionData>>> GetAsync(int roleId, PermissionSubjects subject);
}
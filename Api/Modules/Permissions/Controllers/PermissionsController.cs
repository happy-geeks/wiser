using System.Collections.Generic;
using System.Net.Mime;
using System.Threading.Tasks;
using Api.Modules.Permissions.Enums;
using Api.Modules.Permissions.Interfaces;
using Api.Modules.Permissions.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Permission.Controllers;

/// <summary>
/// Controller for management of permissions
/// </summary>
[Route("api/v3/[controller]")]
[ApiController]
[Authorize]
[Consumes(MediaTypeNames.Application.Json)]
[Produces(MediaTypeNames.Application.Json)]
public class PermissionsController : ControllerBase
{
    private IPermissionsService permissionsService;
    
    /// <summary>
    /// Creates a new instance of PermissionController.
    /// </summary>
    public PermissionsController(IPermissionsService permissionsService)
    {
        this.permissionsService = permissionsService;
    }
    
    /// <summary>
    /// Set role permissions
    /// </summary>
    /// <param name="permissionUpdateModel">Model containing the data used to set the permissions</param>
    [HttpPost]
    [Route("set-permission")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SetPermissionAsync([FromBody] PermissionUpdateModel permissionUpdateModel)
    {
        return (await permissionsService.SetPermissionAsync(permissionUpdateModel)).GetHttpResponseMessage();
    }
    
    /// <summary>
    /// Get role permissions
    /// </summary>
    /// <param name="roleId">The role id of the role for which the data should be retrieved</param>
    /// <param name="subject">The subject for which the data should be retrieved. For possible values see <see cref="PermissionSubject"/>.</param>
    [HttpGet]
    [Route("get-permissions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(List<BasePermissionData>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPermissionsAsync(int roleId, PermissionSubject subject)
    {
        return (await permissionsService.GetPermissionsAsync(roleId, subject)).GetHttpResponseMessage();
    }
}
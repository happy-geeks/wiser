using System.Collections.Generic;
using System.Net.Mime;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Modules.ApiConnections.Interfaces;
using Api.Modules.ApiConnections.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.ApiConnections.Controllers;

/// <summary>
/// Controller for CRUD operations for Wiser API connections.
/// </summary>
[Route("api/v3/api-connections")]
[ApiController]
[Authorize]
[Consumes(MediaTypeNames.Application.Json)]
[Produces(MediaTypeNames.Application.Json)]
public class ApiConnectionsController : Controller
{
    private readonly IApiConnectionsService apiConnectionsService;

    /// <summary>
    /// Creates a new instance of <see cref="ApiConnectionsController"/>.
    /// </summary>
    public ApiConnectionsController(IApiConnectionsService apiConnectionsService)
    {
        this.apiConnectionsService = apiConnectionsService;
    }

    /// <summary>
    /// Gets the settings for all external API connections.
    /// </summary>
    /// <returns>A list of <see cref="ApiConnectionModel"/>s with the settings.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<ApiConnectionModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSettingsAsync()
    {
        return (await apiConnectionsService.GetSettingsAsync((ClaimsIdentity) User.Identity)).GetHttpResponseMessage();
    }

    /// <summary>
    /// Gets the settings for communicating with an external API.
    /// </summary>
    /// <param name="id">The ID of the API settings.</param>
    /// <returns>An <see cref="ApiConnectionModel">ApiConnectionModel</see> with the settings.</returns>
    [HttpGet]
    [Route("{id:int}")]
    [ProducesResponseType(typeof(ApiConnectionModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSettingsAsync(int id)
    {
        return (await apiConnectionsService.GetSettingsAsync((ClaimsIdentity) User.Identity, id)).GetHttpResponseMessage();
    }
}
using System.Net.Mime;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Core.Controllers;

/// <summary>
/// Controller for doing generic database operations, such as migrations.
/// </summary>
[Route("api/v3/database")]
[ApiController]
[Authorize]
[Consumes(MediaTypeNames.Application.Json)]
[Produces(MediaTypeNames.Application.Json)]
public class WiserDatabaseController : Controller
{
    private readonly IWiserDatabaseHelpersService wiserDatabaseHelpersService;

    /// <summary>
    /// Creates a new instance of <see cref="WiserDatabaseController"/>.
    /// </summary>
    public WiserDatabaseController(IWiserDatabaseHelpersService wiserDatabaseHelpersService)
    {
        this.wiserDatabaseHelpersService = wiserDatabaseHelpersService;
    }

    /// <summary>
    /// Do database migrations for the tenant database, of the currently authenticated user, to keep all tables and data up-to-date.
    /// </summary>
    [HttpPut("tenant-migrations")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DoDatabaseMigrationsForTenantAsync()
    {
        await wiserDatabaseHelpersService.DoDatabaseMigrationsForTenantAsync((ClaimsIdentity)User.Identity);
        return NoContent();
    }
}
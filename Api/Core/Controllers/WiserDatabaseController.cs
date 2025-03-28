using System.Collections.Generic;
using System.Net.Mime;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Interfaces;
using Api.Core.Models;
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
    /// Get a list of database migrations that are available for the tenant database, of the currently authenticated user.
    /// </summary>
    /// <param name="manualMigrationsOnly">Whether to only return migrations that need to be triggered manually.</param>
    /// <param name="includeAlreadyExecutedMigrations">Whether to include migrations that have already been completed.</param>
    /// <returns>A list of migrations that need to be manually triggered and that haven't been triggered yet.</returns>
    [HttpGet("tenant-migrations")]
    [ProducesResponseType(typeof(List<DatabaseMigrationInformationModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMigrationsAsync(bool manualMigrationsOnly, bool includeAlreadyExecutedMigrations)
    {
        var result = await wiserDatabaseHelpersService.GetMigrationsAsync((ClaimsIdentity)User.Identity, manualMigrationsOnly, includeAlreadyExecutedMigrations);
        return result.GetHttpResponseMessage();
    }

    /// <summary>
    /// Do database migrations for the tenant database, of the currently authenticated user, to keep all tables and data up-to-date.
    /// This will do migrations that can (and should) be done automatically, without user interaction.
    /// </summary>
    [HttpPut("tenant-migrations")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DoAutomaticDatabaseMigrationsForTenantAsync()
    {
        await wiserDatabaseHelpersService.DoAutomaticDatabaseMigrationsForTenantAsync((ClaimsIdentity)User.Identity);
        return NoContent();
    }

    /// <summary>
    /// Do database migrations that require user interaction for the tenant database, of the currently authenticated user.
    /// This is for doing migrations that are not safe to do automatically, and require a user to decide when to do them.
    /// </summary>
    /// <param name="migrationNames">The migrations that should be executed.</param>
    [HttpPut("manual-tenant-migrations")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DoManualDatabaseMigrationsForTenantAsync(List<string> migrationNames)
    {
        await wiserDatabaseHelpersService.DoManualDatabaseMigrationsForTenantAsync((ClaimsIdentity)User.Identity, migrationNames);
        return NoContent();
    }
}
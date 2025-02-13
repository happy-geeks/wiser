using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Models;
using Api.Core.Services;

namespace Api.Core.Interfaces;

/// <summary>
/// A service that provides helper methods for the Wiser database.
/// </summary>
public interface IWiserDatabaseHelpersService
{
    /// <summary>
    /// Do database migrations for the tenant database, of the currently authenticated user, to keep all tables and data up-to-date.
    /// This will do migrations that can (and should) be done automatically, without user interaction.
    /// </summary>
    /// <param name="identity">The identity of the authenticated user.</param>
    Task DoAutomaticDatabaseMigrationsForTenantAsync(ClaimsIdentity identity);

    /// <summary>
    /// Do database migrations that require user interaction for the tenant database, of the currently authenticated user.
    /// This is for doing migrations that are not safe to do automatically, and require a user to decide when to do them.
    /// </summary>
    /// <param name="identity">The identity of the authenticated user.</param>
    /// <param name="migrationNames">The migrations that should be executed.</param>
    Task DoManualDatabaseMigrationsForTenantAsync(ClaimsIdentity identity, List<string> migrationNames);

    /// <summary>
    /// Get a list of database migrations that are available for the tenant database, of the currently authenticated user.
    /// </summary>
    /// <param name="identity">The identity of the authenticated user.</param>
    /// <param name="manualMigrationsOnly">Whether to only return migrations that need to be triggered manually.</param>
    /// <param name="includeAlreadyExecutedMigrations">Whether to include migrations that have already been completed.</param>
    /// <returns>A list of migrations that need to be manually triggered and that haven't been triggered yet.</returns>
    Task<ServiceResult<List<DatabaseMigrationInformationModel>>> GetMigrationsAsync(ClaimsIdentity identity, bool manualMigrationsOnly, bool includeAlreadyExecutedMigrations);

    /// <summary>
    /// Do database migrations for the main database, to keep all tables and data up-to-date.
    /// </summary>
    Task DoDatabaseMigrationsForMainDatabaseAsync();
}
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

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
    Task DoAutomaticDatabaseMigrationsForTenantAsync(ClaimsIdentity identity);

    /// <summary>
    /// Do database migrations that require user interaction for the tenant database, of the currently authenticated user.
    /// This is for doing migrations that are not safe to do automatically, and require a user to decide when to do them.
    /// </summary>
    Task DoManualDatabaseMigrationsForTenantAsync(ClaimsIdentity identity, List<string> migrationNames);

    /// <summary>
    /// Do database migrations for the main database, to keep all tables and data up-to-date.
    /// </summary>
    Task DoDatabaseMigrationsForMainDatabaseAsync();
}
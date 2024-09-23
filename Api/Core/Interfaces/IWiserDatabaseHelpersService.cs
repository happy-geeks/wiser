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
    /// </summary>
    Task DoDatabaseMigrationsForTenantAsync(ClaimsIdentity identity);

    /// <summary>
    /// Do database migrations for the main database, to keep all tables and data up-to-date.
    /// </summary>
    Task DoDatabaseMigrationsForMainDatabaseAsync();
}
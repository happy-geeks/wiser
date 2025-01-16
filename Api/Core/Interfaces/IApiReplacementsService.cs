using System.Security.Claims;

namespace Api.Core.Interfaces;

/// <summary>
/// Service for doing specific Wiser API replacements on templates/queries.
/// </summary>
public interface IApiReplacementsService
{
    /// <summary>
    /// Replace values from a ClaimsIdentity.
    /// </summary>
    /// <param name="input">The input string to do the replacements on.</param>
    /// <param name="identity">The ClaimsIdentity.</param>
    /// <param name="forQuery">Optional: Set to <see langword="true"/> to make all replaced values safe against SQL injection.</param>
    /// <returns>The string with the replaced values.</returns>
    string DoIdentityReplacements(string input, ClaimsIdentity identity, bool forQuery = false);
}
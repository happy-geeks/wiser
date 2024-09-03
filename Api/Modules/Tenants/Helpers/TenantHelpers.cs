using Api.Modules.Tenants.Models;
using GeeksCoreLibrary.Core.Extensions;
using MySqlConnector;

namespace Api.Modules.Tenants.Helpers;

/// <summary>
/// Helper methods for tenant operations.
/// </summary>
public static class TenantHelpers
{
    /// <summary>
    /// Get a connection string from the given connection information.
    /// </summary>
    /// <param name="connectionInformation">The connection information.</param>
    /// <param name="databasePasswordEncryptionKey">The encryption key that is needed to decrypt the password of the connection information.</param>
    /// <returns>A <see cref="MySqlConnectionStringBuilder"/>.</returns>
    public static MySqlConnectionStringBuilder GetConnectionString(ConnectionInformationModel connectionInformation, string databasePasswordEncryptionKey)
    {
        return new MySqlConnectionStringBuilder
        {
            IgnoreCommandTransaction = true,
            ConvertZeroDateTime = true,
            AllowUserVariables = true,
            CharacterSet = "utf8mb4",
            Server = connectionInformation.Host,
            Port = (uint)connectionInformation.PortNumber,
            UserID = connectionInformation.Username,
            Password = connectionInformation.Password.DecryptWithAesWithSalt(databasePasswordEncryptionKey),
            Database = connectionInformation.DatabaseName
        };
    }
}
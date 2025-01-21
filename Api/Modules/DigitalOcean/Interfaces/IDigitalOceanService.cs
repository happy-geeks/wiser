using System.Threading.Tasks;
using Api.Modules.DigitalOcean.Models;

namespace Api.Modules.DigitalOcean.Interfaces;

/// <summary>
/// An interface for the Digital Ocean Service
/// </summary>
public interface IDigitalOceanService
{
    /// <summary>
    /// Redirect to the Digital Ocean authorization page.
    /// </summary>
    string AuthorizationRedirect();
        
    /// <summary>
    /// Processes a callback from Digital Ocean's OAUTH2 authentication.
    /// </summary>
    /// <param name="code">The authentication code from Digital Ocean.</param>
    Task<string> ProcessCallbackAsync(string code);
       
    /// <summary>
    /// Gets information about a database cluster.
    /// </summary>
    Task<GetDatabasesResponseModel> DatabaseListAsync(string accessToken);
        
    /// <summary>
    /// Create a new database in a cluster.
    /// </summary>
    Task<CreateDatabaseApiResponseModel> CreateDatabaseAsync(string databaseCluster, string database, string user, string accessToken);
        
    /// <summary>
    /// Restricts a MySQL User in the database
    /// </summary>
    Task<bool> RestrictMysqlUserToDbAsync(CreateDatabaseApiResponseModel databaseInfo, string accessToken);
}
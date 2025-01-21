using System.Collections.Generic;

namespace Api.Modules.DigitalOcean.Models;

/// <summary>
/// Model for response of the get database endpoint of the Digital Ocean API.
/// </summary>
public class GetDatabasesResponseModel
{
    /// <summary>
    /// Gets or sets the list of databases.
    /// </summary>
    public List<DatabaseApiModel> Databases { get; set; }
}
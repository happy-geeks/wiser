using System.Collections.Generic;

namespace Api.Modules.DigitalOcean.Models;

/// <summary>
/// A model that contains the response of database users from the Digital Ocean API.
/// </summary>
public class PostDatabaseUserResponseModel
{
    /// <summary>
    /// List of users that have access to the Digital Ocean database
    /// </summary>
    public List<UserApiModel> Users { get; set; } = [];
}
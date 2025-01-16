namespace Api.Modules.DigitalOcean.Models;

/// <summary>
/// Model class for a user to the Digital Ocean database
/// </summary>
public class UserApiModel
{
    /// <summary>
    /// Username of the user in the Digital Ocean database
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// Role of the user in the Digital Ocean database
    /// </summary>
    public string Role { get; set; }
    /// <summary>
    /// Password of the user in the Digital Ocean database
    /// </summary>
    public string Password { get; set; }
}
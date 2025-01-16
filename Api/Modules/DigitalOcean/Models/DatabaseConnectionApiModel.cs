namespace Api.Modules.DigitalOcean.Models;

/// <summary>
/// This class describes an object that makes a connection to the database via an API
/// </summary>
public class DatabaseConnectionApiModel
{
    /// <summary>
    /// Protocol that will be used
    /// </summary>
    public string Protocol { get; set; }

    /// <summary>
    /// The URI that will be used
    /// </summary>
    public string Uri { get; set; }

    /// <summary>
    /// Name of the database
    /// </summary>
    public string Database { get; set; }

    /// <summary>
    /// Hostname of the database
    /// </summary>
    public string Host { get; set; }

    /// <summary>
    /// Port of the database
    /// </summary>
    public string Port { get; set; }

    /// <summary>
    /// Username of the user that will be logged in
    /// </summary>
    public string User { get; set; }

    /// <summary>
    /// Password of the user that will be logged in
    /// </summary>
    public string Password { get; set; }

    /// <summary>
    /// SSL that can be used for logging in
    /// </summary>
    public string Ssl { get; set; }
}
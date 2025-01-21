namespace Api.Modules.DigitalOcean.Models;

/// <summary>
/// This class describes an object with data about making a new database
/// </summary>
public class CreateDatabaseRequestModel
{
    /// <summary>
    /// The cluster of the database
    /// </summary>
    public string DatabaseCluster { get; set; }
        
    /// <summary>
    /// The name of the database
    /// </summary>
    public string Database { get; set; }
        
    /// <summary>
    /// The user of the database
    /// </summary>
    public string User { get; set; }
}
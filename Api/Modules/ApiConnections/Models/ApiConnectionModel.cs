using Newtonsoft.Json.Linq;

namespace Api.Modules.ApiConnections.Models;

/// <summary>
/// A model with settings for communication with an external API.
/// </summary>
public class ApiConnectionModel
{
    /// <summary>
    /// Gets or sets the ID of the connection.
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the connection.
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// Gets or sets the options for the external API.
    /// </summary>
    public JToken Options { get; set; }
    
    /// <summary>
    /// Gets or sets the data/options that are needed for authenticating with the external API.
    /// </summary>
    public JToken AuthenticationData { get; set; }
}
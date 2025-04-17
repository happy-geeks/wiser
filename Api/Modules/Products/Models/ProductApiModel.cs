namespace Api.Modules.Queries.Models;

/// <summary>
/// A model for a styledoutput within Wiser.
/// </summary>
public class ProductApiModel
{
    /// <summary>
    /// Gets or sets the id.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the wiser id related to this product.
    /// </summary>
    public ulong WiserId { get; set; }

    /// <summary>
    /// Gets or sets the version number related to this product.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Gets or sets the output aka the content of the 
    /// </summary>
    public string Output { get; set; }

    /// <summary>
    /// Gets or sets the value to see if the product was removed or not.
    /// </summary>
    public bool Removed { get; set; }

    /// <summary>
    /// Gets or sets the added on date for this entry.
    /// </summary>
    public string AddedOn { get; set; }

    /// <summary>
    /// Gets or sets whom/what added this entry when it was created.
    /// </summary>
    public string AddedBy { get; set; }
    
    /// <summary>
    /// Gets or sets whom/what added this entry when it was created.
    /// </summary>
    public string RefreshDate { get; set; }
    
    /// <summary>
    /// Gets or Sets the option field, this is a JSON style option string field that gets parsed for every run
    /// </summary>
    public string Hash { get; set; }
}
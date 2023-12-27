namespace Api.Modules.Pdfs.Models;

/// <summary>
/// A model for defining the pdf merge functionality
/// </summary>
public class MergePdfModel
{
    /// <summary>
    /// Gets or sets the entity type of items to search for.
    /// </summary>
    public string EntityType { get; set; }

    /// <summary>
    /// Gets or sets the encrypted item ids 
    /// </summary>
    public string EncrypedItemIdsList { get; set; }

    /// <summary>
    /// Gets or sets the property name of the files to retreive
    /// </summary>
    public string PropertyName { get; set; }
}
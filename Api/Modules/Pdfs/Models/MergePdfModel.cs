using Api.Modules.Files.Models;

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
    public string[] EncryptedItemIdsList { get; set; }

    /// <summary>
    /// Gets or sets the property name of the files to retreive, this can be a comma-separated list
    /// </summary>
    public string[] PropertyNames { get; set; }
    
    /// <summary>
    /// Gets or sets the selection options used for the merge
    /// With this you can determine which of the file gets used if an item has multiple
    /// </summary>
    public SelectionOptions FileSelection { get; set; } = SelectionOptions.None;
}
using System.ComponentModel.DataAnnotations;

namespace Api.Modules.Imports.Models;

/// <summary>
/// A model for a delete items request to the Wiser import/export module.
/// </summary>
public class DeleteItemsRequestModel
{
    /// <summary>
    /// Gets or sets the path for the uploaded file.
    /// </summary>
    [Required]
    public string FilePath { get; set; }

    /// <summary>
    /// Gets or sets the name of the entity of the items to delete.
    /// </summary>
    [Required]
    public string EntityName { get; set; }

    /// <summary>
    /// gets or sets the name of the property to match the contents of the file with.
    /// </summary>
    [Required]
    public string PropertyName { get; set; }
}
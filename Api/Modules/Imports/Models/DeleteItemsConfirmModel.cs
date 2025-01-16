using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Api.Modules.Imports.Models;

/// <summary>
/// A model for the confirmation of the items to delete by import.
/// </summary>
public class DeleteItemsConfirmModel
{
    /// <summary>
    /// Gets or sets the entity type of the items to delete.
    /// </summary>
    [Required]
    public string EntityType { get; set; }

    /// <summary>
    /// Gets or sets the IDs of the items to delete.
    /// </summary>
    [Required]
    public List<ulong> Ids { get; set; }
}
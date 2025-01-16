using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Api.Core.Enums;

namespace Api.Modules.Imports.Models;

/// <summary>
/// A model for a delete links request to the Wiser import/export module.
/// </summary>
public class DeleteLinksRequestModel
{
    /// <summary>
    /// Gets or sets the path for the uploaded file.
    /// </summary>
    [Required]
    public string FilePath { get; set; }

    /// <summary>
    /// Gets or sets the type of link delete based on the number of columns of the uploaded file.
    /// </summary>
    [Required]
    public DeleteLinksTypes DeleteLinksType { get; set; }

    /// <summary>
    /// Gets or sets the ID of the link to wherein the links need to be deleted. Only used for <see cref="DeleteLinksTypes.Single"/>.
    /// </summary>
    public int LinkId { get; set; }

    /// <summary>
    /// Gets or sets the settings of the delete links. Only used for <see cref="DeleteLinksTypes.Multiple"/>.
    /// </summary>
    public List<Dictionary<string, object>> DeleteSettings { get; set; }
}
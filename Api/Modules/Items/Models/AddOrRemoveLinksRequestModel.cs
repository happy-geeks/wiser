using System.Collections.Generic;

namespace Api.Modules.Items.Models;

/// <summary>
/// A model for a request to add or more a link between items.
/// </summary>
public class AddOrRemoveLinksRequestModel
{
    /// <summary>
    /// Deprecated: Gets or sets a list of encrypted item IDs, of all items that are being added or removed.
    /// </summary>
    public List<string> EncryptedSourceIds { get; set; }

    /// <summary>
    /// Deprecated: Gets or sets a list of encrypted item IDs of the destinations of where to move the item(s) should be to or removed from.
    /// </summary>
    public List<string> EncryptedDestinationIds { get; set; }

    /// <summary>
    /// Gets or sets a list of item IDs, of all items that are being added or removed.
    /// </summary>
    public List<ulong> SourceIds { get; set; }

    /// <summary>
    /// Gets or sets a list of item IDs of the destinations of where to move the item(s) should be to or removed from.
    /// </summary>
    public List<ulong> DestinationIds { get; set; }

    /// <summary>
    /// Gets or sets the link type number.
    /// </summary>
    public int LinkType { get; set; }

    /// <summary>
    /// Gets or sets the entity type of the source item(s).
    /// </summary>
    public string SourceEntityType { get; set; }
}
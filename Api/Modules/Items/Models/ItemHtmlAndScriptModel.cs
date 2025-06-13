using System.Collections.Generic;

namespace Api.Modules.Items.Models;

/// <summary>
/// A model for showing an item in Wiser.
/// </summary>
public class ItemHtmlAndScriptModel
{
    /// <summary>
    /// Gets or sets the list of tabs for this item.
    /// </summary>
    public List<ItemTabModel> Tabs { get; set; } = [];

    /// <summary>
    /// Gets or sets whether the user is allowed to read this item.
    /// </summary>
    public bool CanRead { get; set; }

    /// <summary>
    /// Gets or sets whether the user is allowed to change existing this item.
    /// </summary>
    public bool CanWrite { get; set; }

    /// <summary>
    /// Gets or sets whether the user is allowed to create new this item.
    /// </summary>
    public bool CanCreate { get; set; }

    /// <summary>
    /// Gets or sets whether the user is allowed to delete this item.
    /// </summary>
    public bool CanDelete { get; set; }
}
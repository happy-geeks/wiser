namespace Api.Modules.Items.Models;

/// <summary>
/// A model for a result of creating a new Wiser item.
/// </summary>
public class CreateItemResultModel
{
    /// <summary>
    /// Gets or sets the encrypted id of the new item.
    /// </summary>
    public string NewItemId { get; set; }

    /// <summary>
    /// Gets or sets the plain id of the new item.
    /// </summary>
    public ulong NewItemIdPlain { get; set; }

    /// <summary>
    /// Gets or sets the icon that the new item should have in the tree view in Wiser.
    /// </summary>
    public string Icon { get; set; }

    /// <summary>
    /// Gets or sets the link id, of the link from the new item to the given parent.
    /// </summary>
    public long NewLinkId { get; set; }
}
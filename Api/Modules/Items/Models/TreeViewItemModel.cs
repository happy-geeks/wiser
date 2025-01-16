namespace Api.Modules.Items.Models;

/// <summary>
/// A model for returning data for a Wiser item in a tree view.
/// </summary>
public class TreeViewItemModel
{
    /// <summary>
    /// Gets or sets the encrypted ID of the item.
    /// </summary>
    public string EncryptedItemId { get; set; }

    /// <summary>
    /// Gets or sets the plain ID of the item.
    /// </summary>
    public ulong PlainItemId { get; set; }

    /// <summary>
    /// Gets or sets the encrypted original ID of this item.
    /// An item can have different data on production than on test for example. If that happens, the ItemId will contain the Id of the original item.
    /// The first time an item is made, that is considered the original item. For those items, the Id and ItemId will be the same.
    /// </summary>
    public string EncryptedOriginalItemId { get; set; }
        
    /// <summary>
    /// Gets or sets the plain original ID of this item.
    /// An item can have different data on production than on test for example. If that happens, the ItemId will contain the Id of the original item.
    /// The first time an item is made, that is considered the original item. For those items, the Id and ItemId will be the same.
    /// </summary>
    public ulong PlainOriginalItemId { get; set; }

    /// <summary>
    /// Gets or sets the title/name of the item.
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Gets or sets whether this item has children.
    /// </summary>
    public bool HasChildren { get; set; }

    /// <summary>
    /// Gets the CSS class for which icon to show when the item is collapsed in the tree view.
    /// </summary>
    public string SpriteCssClass => CollapsedSpriteCssClass;
        
    /// <summary>
    /// Gets or sets the CSS class for which icon to show when the item is collapsed in the tree view.
    /// </summary>
    public string CollapsedSpriteCssClass { get; set; }
        
    /// <summary>
    /// Gets or sets the CSS class for which icon to show when the item is expanded in the tree view.
    /// </summary>
    public string ExpandedSpriteCssClass { get; set; }

    /// <summary>
    /// Gets or sets the CSS class for the main HTML element of this item in the tree view.
    /// </summary>
    public string NodeCssClass { get; set; }

    /// <summary>
    /// Gets or sets the entity type of this item.
    /// </summary>
    public string EntityType { get; set; }

    /// <summary>
    /// Gets or sets which types of children this item can have.
    /// </summary>
    public string AcceptedChildTypes { get; set; }

    /// <summary>
    /// Gets or sets the encrypted destination/parent ID of the parent.
    /// </summary>
    public string DestinationItemId { get; set; }

    /// <summary>
    /// Gets or sets the original ID of the parent.
    /// An item can have different data on production than on test for example. If that happens, the ItemId will contain the Id of the original item.
    /// The first time an item is made, that is considered the original item. For those items, the Id and ItemId will be the same.
    /// </summary>
    public ulong OriginalParentId { get; set; }

    /// <summary>
    /// Gets or sets whether this item should be checked in the tree view.
    /// Only applicable for tree views in item-linker fields.
    /// </summary>
    public bool Checked { get; set; }
}
namespace Api.Modules.Items.Models;

/// <summary>
/// A class that represents an item in a context menu in Wiser.
/// </summary>
public class ContextMenuItem
{
    /// <summary>
    /// The text to display for the context menu item.
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// The CSS class to use for the sprite of the context menu item.
    /// </summary>
    public string SpriteCssClass { get; set; }

    /// <summary>
    /// The action to perform when the context menu item is clicked.
    /// </summary>
    public string Action { get; set; }

    /// <summary>
    /// The entity type that the context menu item is for.
    /// </summary>
    public string EntityType { get; set; }
}
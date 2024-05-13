namespace Api.Modules.Files.Models;

/// <summary>
/// A model for a file or directory in a tree view.
/// </summary>
public class FileTreeViewModel
{
    /// <summary>
    /// Gets or sets the ID of the file.
    /// </summary>
    public ulong Id { get; set; }

    /// <summary>
    /// Gets or sets the encrypted ID of the file.
    /// </summary>
    public string EncryptedId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the directory that the file is linked to.
    /// </summary>
    public ulong ItemId { get; set; }

    /// <summary>
    /// Gets or sets the encrypted ID of the directory that the file is linked to.
    /// </summary>
    public string EncryptedItemId { get; set; }

    /// <summary>
    /// Gets or sets the name of the file or directory.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the property name of the file.
    /// </summary>
    public string PropertyName { get; set; } = Constants.GlobalFilePropertyName;

    /// <summary>
    /// Gets or sets whether this is a directory.
    /// </summary>
    public bool IsDirectory { get; set; }

    /// <summary>
    /// Gets or sets whether this directory has children (files or other directories).
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
    /// Gets or sets the HTML, if this is an HTML file.
    /// </summary>
    public string Html { get; set; }

    /// <summary>
    /// Gets or sets the content type of the file.
    /// </summary>
    public string ContentType { get; set; }
}
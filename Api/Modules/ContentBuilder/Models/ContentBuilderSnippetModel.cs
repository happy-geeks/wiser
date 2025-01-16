namespace Api.Modules.ContentBuilder.Models;

/// <summary>
/// A model for a Content Builder HTML snippet.
/// </summary>
public class ContentBuilderSnippetModel
{
    /// <summary>
    /// Gets or sets the ID.
    /// </summary>
    public ulong Id { get; set; }

    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the URL to the thumbnail image.
    /// </summary>
    public string Thumbnail { get; set; }

    /// <summary>
    /// Gets or sets the ID of the category.
    /// </summary>
    public ulong CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the name of the category.
    /// </summary>
    public string Category { get; set; }

    /// <summary>
    /// Gets or sets the HTML of the snippet.
    /// </summary>
    public string Html { get; set; }
}
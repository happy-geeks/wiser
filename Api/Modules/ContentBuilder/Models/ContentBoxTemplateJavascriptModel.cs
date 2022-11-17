namespace Api.Modules.ContentBuilder.Models;

public class ContentBoxTemplateJavascriptModel
{
    /// <summary>
    /// Gets or sets the URL to the thumbnail image.
    /// </summary>
    public string Thumbnail { get; set; }

    /// <summary>
    /// Gets or sets the ID of the category.
    /// </summary>
    public string Category { get; set; }
    
    public ulong DesignId { get; set; }

    /// <summary>
    /// Gets or sets the HTML of the snippet.
    /// </summary>
    public string Html { get; set; }

    /// <summary>
    /// Gets or sets the CSS file that is needed for the contents of this template.
    /// </summary>
    public string ContentCss { get; set; } = "";

    /// <summary>
    /// Gets or sets the CSS class that the outer element of the template should get.
    /// </summary>
    public string ContentClass { get; set; } = "";
}
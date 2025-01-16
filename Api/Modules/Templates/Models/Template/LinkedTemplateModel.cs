using GeeksCoreLibrary.Modules.Templates.Enums;

namespace Api.Modules.Templates.Models.Template;

/// <summary>
/// A model to store information about a link between two templates.
/// </summary>
public class LinkedTemplateModel
{
    /// <summary>
    /// Gets or sets the ID of the linked template
    /// </summary>
    public int TemplateId { get; set; }

    /// <summary>
    /// Gets or sets the name of the linked template
    /// </summary>
    public string TemplateName { get; set; }

    /// <summary>
    /// Gets or sets the path of the linked template
    /// </summary>
    public string Path { get; set; }

    /// <summary>
    /// Gets or sets the type of how the template is linked
    /// </summary>
    public TemplateTypes LinkType { get; set; }
}
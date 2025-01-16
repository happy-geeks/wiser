using GeeksCoreLibrary.Modules.Templates.Enums;

namespace Api.Modules.Templates.Models.Template;

/// <summary>
/// Model class used while making a new template.
/// </summary>
public class NewTemplateModel
{
    /// <summary>
    /// Gets or sets the Name of the new template
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the type of the new template
    /// </summary>
    public TemplateTypes Type { get; set; }

    /// <summary>
    /// Gets or sets the optional editorValue of the template, this can be used for importing files.
    /// </summary>
    public string EditorValue { get; set; }
}
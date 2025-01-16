using System.Collections.Generic;

namespace Api.Modules.Templates.Models.Template;

/// <summary>
/// Model class
/// </summary>
public class LinkedTemplatesModel
{
    /// <summary>
    /// Gets or sets a list of linked SCSS templates in a Linked Templates object
    /// </summary>
    public List<LinkedTemplateModel> LinkedScssTemplates { get; set; } = [];

    /// <summary>
    /// Gets or sets a list of linked CSS templates in a Linked Templates object
    /// </summary>
    // ReSharper disable once CollectionNeverUpdated.Global
    public List<LinkedTemplateModel> LinkedCssTemplates { get; set; } = [];

    /// <summary>
    /// Gets or sets a list of linked Javascript templates in a Linked Templates object
    /// </summary>
    public List<LinkedTemplateModel> LinkedJavascript { get; set; } = [];

    /// <summary>
    /// Gets or sets a list of linked Options templates in a Linked Templates object
    /// </summary>
    public List<LinkedTemplateModel> LinkOptionsTemplates { get; set; } = [];

    /// <summary>
    /// Gets or sets a raw link list in a Linked Templates object
    /// </summary>
    public string RawLinkList { get; set; }
}
namespace Api.Modules.Templates.Models;

/// <summary>
/// A model for a template entity.
/// </summary>
public class TemplateEntityModel
{
    /// <summary>
    /// Gets and sets the ID.
    /// </summary>
    public ulong Id { get; set; }
        
    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the subject for the mail.
    /// </summary>
    public string Subject { get; set; }

    /// <summary>
    /// Gets or sets the content.
    /// </summary>
    public string Content { get; set; }
}
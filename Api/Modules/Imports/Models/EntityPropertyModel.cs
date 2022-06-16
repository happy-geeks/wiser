namespace Api.Modules.Imports.Models;

/// <summary>
/// An entity property with the data needed by the import module.
/// </summary>
public class EntityPropertyModel
{
    /// <summary>
    /// The display name of the property.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The property's key. 
    /// </summary>
    public string Value { get; set; }

    /// <summary>
    /// Gets or sets the language code for this property.
    /// </summary>
    public string LanguageCode { get; set; }

    /// <summary>
    /// Gets or sets whether the field represents an image upload.
    /// </summary>
    public bool IsImageField { get; set; }

    /// <summary>
    /// Gets or sets whether multiple images are allowed for this property.
    /// </summary>
    public bool AllowMultipleImages { get; set; }

    /// <summary>
    /// Gets or sets the order of this field, which is used to decide which property will be chosen when multiple names match.
    /// This is a combination of the 'ordering' field and 'id' field of the entity property.
    /// </summary>
    public string PropertyOrder { get; set; }
}
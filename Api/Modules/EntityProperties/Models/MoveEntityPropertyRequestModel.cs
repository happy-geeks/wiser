namespace Api.Modules.EntityProperties.Models;

/// <summary>
/// Model for moving an entity property.
/// </summary>
public class MoveEntityPropertyRequestModel
{
    /// <summary>
    /// Gets or sets the ID of the entity property.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the entity type that the property belongs too, if it belongs to an entity.
    /// </summary>
    public string EntityType { get; set; }

    /// <summary>
    /// Gets or stets the type number of the link this property belongs too, if it belongs to a link and not an entity.
    /// </summary>
    public int LinkType { get; set; }

    /// <summary>
    /// Gets or sets the name of the tab that the property currently belongs too.
    /// </summary>
    public string CurrentTabName { get; set; }

    /// <summary>
    /// Gets or sets the name of the tab that the property should be moved too (can be the same as the current one).
    /// </summary>
    public string NewTabName { get; set; }

    /// <summary>
    /// Gets or sets the current ordering number.
    /// </summary>
    public int CurrentIndex { get; set; }

    /// <summary>
    /// Gets or sets the new ordering number.
    /// </summary>
    public int NewIndex { get; set; }
}
namespace Api.Modules.EntityTypes.Models;

/// <summary>
/// The model for a Wiser entity type.
/// An item in Wiser always has an entity type, this model contains information about such entity types.
/// </summary>
public class EntityTypeModel
{
    /// <summary>
    /// Gets or sets the technical name.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the ID of the module that this entity type belongs to.
    /// </summary>
    public int ModuleId { get; set; }

    /// <summary>
    /// Gets or sets the name of the module that this entity type belongs to.
    /// </summary>
    public string ModuleName { get; set; }

    /// <summary>
    /// Gets or sets the icon name of the module this entity type belongs to.
    /// </summary>
    public string ModuleIcon { get; set; }

    /// <summary>
    /// Gets or sets the prefix for the table that items of this type are stored in.
    /// If empty, they will be stored in wiser_item, otherwise in [DedicatedTablePrefix_wiser_item].
    /// </summary>
    public string DedicatedTablePrefix { get; set; }

    /// <summary>
    /// Gets or sets the total amount of items of this entity type.
    /// </summary>
    public int? TotalItems { get; set; }

    /// <summary>
    /// Gets or sets the link type number from this entity to the parent entity.
    /// Only set when the entity information is requested as a child entity.
    /// </summary>
    public ulong? LinkTypeNumber { get; set; }
}
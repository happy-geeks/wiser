namespace Api.Modules.Dashboard.Models;

/// <summary>
/// This class describes an object that keeps count of the amount of items that an Entity has
/// </summary>
public class ItemsCountModel
{
    /// <summary>
    /// Gets or sets the name of the entity. This will be the friendly name if the entity has one.
    /// </summary>
    public string EntityName { get; set; }

    /// <summary>
    /// Gets or sets the amount of items that have this entity type.
    /// </summary>
    public int AmountOfItems { get; set; }

    /// <summary>
    /// Gets or sets the amount of items in the archive that have this entity type.
    /// </summary>
    public int AmountOfArchivedItems { get; set; }
}
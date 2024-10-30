using System.Collections.Generic;
using Api.Modules.EntityProperties.Enums;

namespace Api.Modules.EntityProperties.Models;

/// <summary>
/// A model for a tab with fields.
/// </summary>
public class EntityPropertyGroupModel
{
    /// <summary>
    /// Gets or sets the ID of the group.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the group.
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the tab the property is shown in.
    /// </summary>
    public string TabName { get; set; }

    /// <summary>
    /// Gets or sets all fields in this group.
    /// </summary>
    public List<EntityPropertyModel> Properties { get; set; } = new();

    /// <summary>
    /// This just indicates that the current item is a group. This makes it easier for our javascript code to check what it is working with.
    /// </summary>
    public EntityPropertyModelTypes Type { get; set; } = EntityPropertyModelTypes.Group;
}
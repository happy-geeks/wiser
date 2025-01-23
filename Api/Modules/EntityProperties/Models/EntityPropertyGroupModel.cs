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
    /// Gets or sets the name of the entity the group belongs to.
    /// </summary>
    public string EntityType { get; set; }

    /// <summary>
    /// Gets the name of the group. Used for treeview in JS, which doesn't use the Name for some reason.
    /// </summary>
    public string DisplayName { get { return Name; } }

    /// <summary>
    /// Gets or sets the name of the tab the property is shown in.
    /// </summary>
    public string TabName { get; set; }

    /// <summary>
    /// Gets or sets the width of the property group in percentages.
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Gets or sets the minimum width width of the property group in pixels.
    /// </summary>
    public int MinimumWidth { get; set; }

    /// <summary>
    /// Gets or sets the stacking orientation of the properties inside the property group. Options are 'Horizontal' and 'Vertical'.
    /// </summary>
    public string Orientation { get; set; }

    /// <summary>
    /// Gets or sets if the property group can be collapsed in the interface.
    /// </summary>
    public bool Collapsible { get; set; }

    /// <summary>
    /// Gets or sets if the property group name is shown in the interface.
    /// </summary>
    public bool ShowName { get; set; }

    /// <summary>
    /// Gets or sets all fields in this group.
    /// </summary>
    public List<EntityPropertyModel> Properties { get; set; } = new();

    /// <summary>
    /// This just indicates that the current item is a group. This makes it easier for our javascript code to check what it is working with.
    /// </summary>
    public EntityPropertyModelTypes Type { get; set; } = EntityPropertyModelTypes.Group;
}
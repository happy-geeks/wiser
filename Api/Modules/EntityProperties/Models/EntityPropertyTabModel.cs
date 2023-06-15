using System.Collections.Generic;

namespace Api.Modules.EntityProperties.Models;

/// <summary>
/// A model for a tab with fields.
/// </summary>
public class EntityPropertyTabModel
{
    /// <summary>
    /// Gets or sets the ID of the tab. This is the same as name at the moment.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the tab.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets all fields in this tab.
    /// </summary>
    public List<EntityPropertyModel> Properties { get; set; } = new();

    /// <summary>
    /// This just indicates that the current item is a tab. This makes it easier for our javascript code to check if an item in the item is a tab or a field.
    /// </summary>
    public bool IsTab { get; set; } = true;
}
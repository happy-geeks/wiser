using System.Collections.Generic;
using Api.Modules.EntityProperties.Enums;

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
    public List<EntityPropertyGroupModel> Properties { get; set; } = [];

    /// <summary>
    /// This just indicates that the current item is a tab. This makes it easier for our javascript code to check what it is working with.
    /// </summary>
    public EntityPropertyModelTypes Type { get; set; } = EntityPropertyModelTypes.Tab;
}
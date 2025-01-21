using System.Collections.Generic;
using Api.Modules.Grids.Models;

namespace Api.Modules.Modules.Models;

/// <summary>
/// A model for settings for a Wiser grid view module.
/// </summary>
public class GridViewSettingsModel
{
    /// <summary>
    /// Gets or sets whether this module has grid view mode enabled.
    /// </summary>
    public bool GridViewMode { get; set; }

    /// <summary>
    /// Gets or sets the field mappings, to use for filters.
    /// </summary>
    public List<FieldMapModel> FieldMappings { get; set; }

    /// <summary>
    /// Gets or sets the grid view settings.
    /// </summary>
    public GridSettingsAndDataModel GridViewSettings { get; set; }
}
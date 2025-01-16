namespace Api.Modules.Kendo.Models;

/// <summary>
/// A model for grid sort settings.
/// </summary>
public class GridSortModel
{
    /// <summary>
    /// Gets or sets the field to sort on.
    /// </summary>
    public string Field { get; set; }

    /// <summary>
    /// Gets or set the direction to sort (asc or desc).
    /// </summary>
    public string Dir { get; set; }
}
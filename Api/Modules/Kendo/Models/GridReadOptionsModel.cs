using System.Collections.Generic;

namespace Api.Modules.Kendo.Models;

/// <summary>
/// A model for grid options.
/// </summary>
public class GridReadOptionsModel
{
    /// <summary>
    /// Gets or sets the page number.
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Gets or sets the page size (amount of items per page).
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Gets or sets how many items to skip.
    /// </summary>
    public int Skip { get; set; }

    /// <summary>
    /// Gets or sets how many items to take.
    /// </summary>
    public int Take { get; set; }

    /// <summary>
    /// Gets or sets sort settings.
    /// </summary>
    public List<GridSortModel> Sort { get; set; }

    /// <summary>
    /// Gets or sets filter settings.
    /// </summary>
    public GridFilterModel Filter { get; set; }

    /// <summary>
    /// Gets or sets any extra values to replace in the data query.
    /// </summary>
    public Dictionary<string, string> ExtraValuesForQuery { get; set; }

    /// <summary>
    /// Gets or sets whether this request is the first load in general, or the first load after changing filters.
    /// </summary>
    public bool FirstLoad { get; set; } = true;
}
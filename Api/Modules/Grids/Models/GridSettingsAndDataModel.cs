using System.Collections.Generic;
using Api.Modules.Kendo.Models;
using Api.Modules.Modules.Models;

namespace Api.Modules.Grids.Models;

/// <summary>
/// A model for grid settings and data.
/// </summary>
public class GridSettingsAndDataModel
{
    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Gets or sets the columns that should be shown in the grid.
    /// </summary>
    public List<GridColumn> Columns { get; set; } = [];

    /// <summary>
    /// Gets or sets the schema model, with information about fields and their types.
    /// </summary>
    public DataSourceSchemaModel SchemaModel { get; set; } = new();

    /// <summary>
    /// Gets or sets the data of the grid.
    /// </summary>
    public List<Dictionary<string, object>> Data { get; set; } = [];

    /// <summary>
    /// Gets or sets the total amount of results.
    /// </summary>
    public int TotalResults { get; set; }

    /// <summary>
    /// Gets or sets any extra javascript that should be executed in Wiser.
    /// </summary>
    public string ExtraJavascript { get; set; }

    /// <summary>
    /// Gets or sets the search grid settings. These are settings for the grid that is openend in a pop-up when the user wants to add an existing item to the grid.
    /// </summary>
    public GridViewSettingsModel SearchGridSettings { get; set; }

    /// <summary>
    /// Gets or sets whether to use client side paging.
    /// </summary>
    public bool ClientSidePaging { get; set; }

    /// <summary>
    /// Gets or sets whether to use client side sorting.
    /// </summary>
    public bool ClientSideSorting { get; set; }

    /// <summary>
    /// Gets or sets whether to use client side filtering.
    /// </summary>
    public bool ClientSideFiltering { get; set; }

    /// <summary>
    /// Gets or sets whether the current item is the source instead of the destination.
    /// </summary>
    public bool CurrentItemIsSourceId { get; set; }

    /// <summary>
    /// Gets or sets the language code for the grid. This is used for grids with field groups, to have a group per language code.
    /// </summary>
    public string LanguageCode { get; set; }
}
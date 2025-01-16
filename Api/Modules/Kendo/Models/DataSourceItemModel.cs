namespace Api.Modules.Kendo.Models;

/// <summary>
/// A model for a Kendo data source item.
/// </summary>
public class DataSourceItemModel
{
    /// <summary>
    /// Gets or sets the value of the item.
    /// </summary>
    public object Value { get; set; }

    /// <summary>
    /// Gets or sets the text (display name) of the item.
    /// </summary>
    public string Text { get; set; }
}
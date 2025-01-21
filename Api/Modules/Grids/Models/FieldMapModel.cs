namespace Api.Modules.Grids.Models;

/// <summary>
/// A model for a field mapping (to map a grid field to a property). This is used for filters.
/// </summary>
public class FieldMapModel
{
    /// <summary>
    /// Gets or sets the name of the field.
    /// </summary>
    public string Field { get; set; }

    /// <summary>
    /// Gets or sets the name of the property.
    /// </summary>
    public string Property { get; set; }

    /// <summary>
    /// Gets or sets whether to ignore this field in the filters.
    /// </summary>
    public bool Ignore { get; set; }

    /// <summary>
    /// Gets or sets the table alias of the wiser_item table that should be used for this field.
    /// </summary>
    public string ItemTableAlias { get; set; } = "i";

    /// <summary>
    /// Gets or sets whether to add this field to the WHERE clause of the query, instead of creating a JOIN for it.
    /// </summary>
    public bool AddToWhereInsteadOfJoin { get; set; }
}
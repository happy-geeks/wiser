namespace Api.Modules.Queries.Models;

/// <summary>
/// A model for a StyledOutputOptionModel within StyledOutput rows.
/// </summary>
public class StyledOutputOptionModel
{
    /// <summary>
    /// Gets or sets the Max result set per Page that gets used when pagination is used.
    /// </summary>
    public int MaxResultsPerPage { get; set; }

    /// <summary>
    /// Gets or sets the log timing option, this adds logging values to the runtime and runcount columns.
    /// </summary>
    public bool LogTiming { get; set; }

    /// <summary>
    /// Gets or sets If we should remove elements that could not be found (including their keys).
    /// </summary>
    public bool RemoveNotFoundElements { get; set; }
}
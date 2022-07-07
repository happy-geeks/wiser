namespace Api.Modules.Dashboard.Enums;

/// <summary>
/// Determines how the data period filter affects which items are retrieved.
/// </summary>
public enum ItemsDataPeriodFilterTypes
{
    /// <summary>
    /// Retrieve all items (ignores period filter).
    /// </summary>
    All,
    /// <summary>
    /// Retrieve all items that were created within the period filter.
    /// </summary>
    NewlyCreated,
    /// <summary>
    /// Retrieve all items that were changed within the period filter.
    /// </summary>
    Changed,
    /// <summary>
    /// TODO: No function yet.
    /// </summary>
    Active,
    /// <summary>
    /// TODO: No function yet.
    /// </summary>
    Archived
}
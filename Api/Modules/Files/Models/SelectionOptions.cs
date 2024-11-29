namespace Api.Modules.Files.Models;

/// <summary>
/// Used to determine which file should be selected if there are multiple options.
/// </summary>
public enum SelectionOptions
{
    /// <summary>
    /// Indicates that it doesn't matter which file gets returned.
    /// </summary>
    None,
    /// <summary>
    /// Gets the oldest file.
    /// </summary>
    Oldest,
    /// <summary>
    /// Gets the newest file.
    /// </summary>
    Newest
}
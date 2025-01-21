namespace Api.Modules.Kendo.Enums;

/// <summary>
/// Drop position for a Kendo tree view component.
/// </summary>
public enum TreeViewDropPositions
{
    /// <summary>
    /// The item was dropped on a directory and needs to be added at the end of that directory.
    /// </summary>
    Over,

    /// <summary>
    /// The item was dropped before another item.
    /// </summary>
    Before,

    /// <summary>
    /// The item was dropped after another item.
    /// </summary>
    After
}
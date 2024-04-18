namespace Api.Modules.Templates.Enums;

/// <summary>
/// The options for log booleans.
/// </summary>
public enum LogBoolean
{
    /// <summary>
    /// Inherit the value from the parent.
    /// </summary>
    inherit,
    /// <summary>
    /// Enable the log, no matter what the parent is set to.
    /// </summary>
    @true,
    /// <summary>
    /// Disable the log, no matter what the parent is set to.
    /// </summary>
    @false
}
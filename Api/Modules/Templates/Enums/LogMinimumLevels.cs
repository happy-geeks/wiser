namespace Api.Modules.Templates.Enums;

/// <summary>
/// All types of minimum log levels. Setting this, will log everything of the selected type and everything that comes after it.
/// </summary>
public enum LogMinimumLevels
{
    /// <summary>
    /// Inherit the log level from the parent.
    /// </summary>
    Inherit,
    /// <summary>
    /// Log everything.
    /// </summary>
    Debug,
    /// <summary>
    /// Log information and higher.
    /// </summary>
    Information,
    /// <summary>
    /// Log warnings and higher.
    /// </summary>
    Warning,
    /// <summary>
    /// Log errors and higher.
    /// </summary>
    Error,
    /// <summary>
    /// Only log critical errors.
    /// </summary>
    Critical
}
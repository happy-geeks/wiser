namespace Api.Modules.Kendo.Models;

/// <summary>
/// A model for Kendo validator settings.
/// </summary>
public class ValidationSettingsModel
{
    /// <summary>
    /// Gets or sets whether the field is required.
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// Gets or sets a regex pattern for validating the value.
    /// </summary>
    public string Pattern { get; set; }

    /// <summary>
    /// Gets or sets the minimum value.
    /// </summary>
    public long Min { get; set; }

    /// <summary>
    /// Gets or sets the maximum value.
    /// </summary>
    public long Max { get; set; }
}
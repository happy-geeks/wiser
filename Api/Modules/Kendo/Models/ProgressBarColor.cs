namespace Api.Modules.Kendo.Models;

/// <summary>
/// A model for settings for a progress bar color.
/// </summary>
public class ProgressBarColor
{
    /// <summary>
    /// Gets or sets the maximum value until when the progress bar should have this color.
    /// </summary>
    public int Max { get; set; }

    /// <summary>
    /// Gets or sets the background color.
    /// </summary>
    public string Background { get; set; }

    /// <summary>
    /// Gets or sets the border color.
    /// </summary>
    public string Border { get; set; }
}
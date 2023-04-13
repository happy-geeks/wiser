using System;
using GeeksCoreLibrary.Core.Enums;

namespace Api.Modules.Templates.Models.Measurements;

/// <summary>
/// A model that contains logging data of a single render of a template or component.
/// </summary>
public class RenderLogModel
{
    /// <summary>
    /// Gets or sets the ID of the template or component.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the version of the template or component.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Gets or sets the URL of the page that the template or component was rendered on.
    /// </summary>
    public Uri Url { get; set; }

    /// <summary>
    /// Gets or sets the environment that the template or component was rendered on.
    /// </summary>
    public Environments Environment { get; set; }

    /// <summary>
    /// Gets or sets the date and time that the rendering was started.
    /// </summary>
    public DateTime Start { get; set; }

    /// <summary>
    /// Gets or sets the date and time that the rendering was finished.
    /// </summary>
    public DateTime? End { get; set; }

    /// <summary>
    /// Gets or sets the date (without time) that the component or template was rendered.
    /// </summary>
    public DateTime Date => Start.Date;

    /// <summary>
    /// Gets or sets the total time it took to render the template or component, in milliseconds.
    /// </summary>
    public ulong TimeTakenInMilliseconds { get; set; }

    /// <summary>
    /// Gets the total time it took to render the template or component, in seconds.
    /// </summary>
    public decimal TimeTakenInSeconds => (decimal)TimeTakenInMilliseconds / 1000;

    /// <summary>
    /// Gets or sets the total time it took to render the template or component.
    /// </summary>
    public TimeSpan TimeTaken => TimeSpan.FromMilliseconds(TimeTakenInMilliseconds);

    /// <summary>
    /// Gets the total time it took to render the template or component, formatted as mm:ss.fff.
    /// </summary>
    public string TimeTakenFormatted => TimeTaken.TotalMilliseconds < 1 ? "< 00:00.001" : TimeTaken.ToString("mm\\:ss\\.fff");

    /// <summary>
    /// Gets or sets the ID of the user that was logged in when this template or component was rendered.
    /// This is 0 if no user was logged in.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public ulong UserId { get; set; }

    /// <summary>
    /// Gets or sets the language code of the language that the user had selected on the website when the template or component was rendered.
    /// </summary>
    public string LanguageCode { get; set; }

    /// <summary>
    /// Gets or sets any error that occurred while rendering the component.
    /// </summary>
    public string Error { get; set; }

    /// <summary>
    /// Gets or sets the name of the template or component.
    /// </summary>
    public string Name { get; set; }
}
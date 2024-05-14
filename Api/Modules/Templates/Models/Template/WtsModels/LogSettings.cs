using Api.Modules.Templates.Attributes;
using Api.Modules.Templates.Enums;

namespace Api.Modules.Templates.Models.Template.WtsModels;

/// <summary>
/// A model for the log settings of different parts of the template.
/// </summary>
public class LogSettings
{
    /// <summary>
    /// The minimum log level logged
    /// </summary>
    [WtsProperty(
        IsVisible = true,
        Title = "Minimaal log level",
        Description = "Het minimale log level dat gelogd wordt.",
        ConfigurationTab = null,
        KendoComponent = KendoComponents.DropDownList,
        IsRequired = true
    )]
    public LogMinimumLevels LogMinimumLevel { get; set; } = LogMinimumLevels.Information;

    /// <summary>
    /// Log messages sent at startup and shutdown. For example, the configuration being started or stopped.
    /// </summary>
    [WtsProperty(
        IsVisible = true,
        Description = "Loggen wanneer de service gestart en gestopt wordt",
        ConfigurationTab = null,
        KendoComponent = KendoComponents.DropDownList,
        IsRequired = true
    )]
    public LogBoolean LogStartAndStop { get; set; }

    /// <summary>
    /// Log messages sent at the beginning and end of the run. For example, the start and stop time of the run scheme or what action is performed.
    /// </summary>
    [WtsProperty(
        IsVisible = true,
        Description = "Loggen wanneer een run-cyclus begint en eindigt",
        ConfigurationTab = null,
        KendoComponent = KendoComponents.DropDownList,
        IsRequired = true
    )]
    public LogBoolean LogRunStartAndStop { get; set; }

    /// <summary>
    /// Log messages sent during the run. For example, the query being executed or the URL of an HTTP API request.
    /// </summary>
    [WtsProperty(
        IsVisible = true,
        Description = "Log de body (bijvoorbeeld de query of de API call die wordt uitgevoerd)",
        ConfigurationTab = null,
        KendoComponent = KendoComponents.DropDownList,
        IsRequired = true
    )]
    public LogBoolean LogRunBody { get; set; }
}
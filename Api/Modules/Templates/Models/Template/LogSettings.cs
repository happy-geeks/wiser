using Api.Modules.Templates.Attributes;
using Api.Modules.Templates.Enums;

namespace Api.Modules.Templates.Models.Template
{
    /// <summary>
    /// A model for the log settings of different parts of the template.
    /// </summary>
    public class LogSettings
    {
        /// <summary>
        /// The minimum log level logged
        /// </summary>
        [WtsAttributes.WtsProperty(
            IsVisible = true,
            Title = "Minimaal log level",
            Description = "",
            ConfigurationTab = null,
            KendoComponent = KendoComponents.DropDownList
        )]
        public LogMinimumLevels LogMinimumLevel {get; set;}
        
        /// <summary>
        /// Log messages sent at startup and shutdown. For example, the configuration being started or stopped.
        /// </summary>
        [WtsAttributes.WtsProperty(
            IsVisible = true,
            Description = "Loggen wanneer de service gestart en gestopt wordt",
            ConfigurationTab = null,
            KendoComponent = KendoComponents.CheckBox
        )]
        public bool LogStartAndStop {get; set;}
        
        /// <summary>
        /// Log messages sent at the beginning and end of the run. For example, the start and stop time of the run scheme or what action is performed.
        /// </summary>
        [WtsAttributes.WtsProperty(
            IsVisible = true,
            Description = "Loggen wanneer een run-cyclus begint en eindigt",
            ConfigurationTab = null,
            KendoComponent = KendoComponents.CheckBox
        )]
        public bool LogRunStartAndStop {get; set;}
        
        /// <summary>
        /// Log messages sent during the run. For example, the query being executed or the URL of an HTTP API request.
        /// </summary>
        [WtsAttributes.WtsProperty(
            IsVisible = true,
            Description = "Log de body (bijvoorbeeld de query of de API call die wordt uitgevoerd)",
            ConfigurationTab = null,
            KendoComponent = KendoComponents.CheckBox
        )]
        public bool LogRunBody {get; set;}
    }
}
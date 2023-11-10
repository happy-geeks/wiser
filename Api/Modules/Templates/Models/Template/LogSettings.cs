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
            isVisible = true,
            Title = "Minimaal log level",
            Description = "",
            KendoTab = KendoTab.Null,
            KendoComponent = KendoComponent.DropDownList
        )]
        public LogMinimumLevels LogMinimumLevel {get; set;}
        
        /// <summary>
        /// Log messages sent at startup and shutdown. For example, the configuration being started or stopped.
        /// </summary>
        [WtsAttributes.WtsProperty(
            isVisible = true,
            Description = "Log bij opstarten en afsluiten",
            KendoTab = KendoTab.Null,
            KendoComponent = KendoComponent.CheckBox
        )]
        public bool LogStartAndStop {get; set;}
        
        /// <summary>
        /// Log messages sent at the beginning and end of the run. For example, the start and stop time of the run scheme or what action is performed.
        /// </summary>
        [WtsAttributes.WtsProperty(
            isVisible = true,
            Description = "Log bij het begin en einde van de run-cyclus",
            KendoTab = KendoTab.Null,
            KendoComponent = KendoComponent.CheckBox
        )]
        public bool LogRunStartAndStop {get; set;}
        
        /// <summary>
        /// Log messages sent during the run. For example, the query being executed or the URL of an HTTP API request.
        /// </summary>
        [WtsAttributes.WtsProperty(
            isVisible = true,
            Description = "Log de body (bijvoorbeeld de query of de API call die wordt uitgevoerd)",
            KendoTab = KendoTab.Null,
            KendoComponent = KendoComponent.CheckBox
        )]
        public bool LogRunBody {get; set;}
    }
}
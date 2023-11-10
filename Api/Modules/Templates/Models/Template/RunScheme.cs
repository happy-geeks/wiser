using System.Xml.Serialization;
using Api.Modules.Templates.Enums;
using JetBrains.Annotations;
using Api.Modules.Templates.Attributes;

namespace Api.Modules.Templates.Models.Template
{
    /// <summary>
    /// A model for the run scheme settings of the template.
    /// </summary>
    public class RunScheme
    {
        /// <summary>
        /// Type of the run scheme
        /// </summary>
        [WtsAttributes.WtsProperty(
            isVisible = true,
            Title = "Type",
            Description = "Het type van de timer",
            KendoTab = KendoTab.Timers,
            KendoComponent = KendoComponent.DropDownList,
            BelongsToForm = "runSchemeForm"
        )]
        public RunSchemeTypes Type { get; set; }
        
        /// <summary>
        /// Unique id of the run scheme.
        /// </summary>
        [WtsAttributes.WtsProperty(
            isVisible = true,
            Title = "TimeId",
            Description = "Het unieke id van de timer",
            KendoTab = KendoTab.Timers,
            KendoComponent = KendoComponent.NumericTextBox,
            BelongsToForm = "runSchemeForm"
        )]
        public int TimeId { get; set; }
        
        /// <summary>
        /// How much time should be between each run.
        /// Format: hours:minutes:seconds
        /// Only if type is continuous.
        /// </summary>
        [XmlElement("Delay", DataType = "string")]
        [CanBeNull]
        [WtsAttributes.WtsProperty(
             isVisible = true,
             Title = "Delay",
             Description = "De tijd tussen elke run. Formaat: uren:minuten:seconden",
             KendoTab = KendoTab.Timers,
             KendoComponent = KendoComponent.TimePicker,
             BelongsToForm = "runSchemeForm"
         )]
        public string Delay { get; set; }
        
        /// <summary>
        /// The time from when the actions associated with this runscheme are started.
        /// </summary>
        [XmlElement("StartTime", DataType = "string")]
        [CanBeNull]
        [WtsAttributes.WtsProperty(
            isVisible = true,
            Title = "Start tijd",
            Description = "De tijd vanaf wanneer de acties van deze timer worden uitgevoerd",
            KendoTab = KendoTab.Timers,
            KendoComponent = KendoComponent.TimePicker,
            BelongsToForm = "runSchemeForm"
        )]
        public string StartTime { get; set; }
        
        /// <summary>
        /// The time at which the actions associated with this runscheme will no longer be executed.
        /// </summary>
        [XmlElement("StopTime", DataType = "string")]
        [CanBeNull]
        [WtsAttributes.WtsProperty(
            isVisible = true,
            Title = "Stop tijd",
            Description = "De tijd tot wanneer de acties van deze timer worden uitgevoerd",
            KendoTab = KendoTab.Timers,
            KendoComponent = KendoComponent.TimePicker,
            BelongsToForm = "runSchemeForm"
        )]
        public string StopTime { get; set; }
        
        /// <summary>
        /// Whether the run scheme should not be executed on specific days.
        /// </summary>
        [CanBeNull]
        [WtsAttributes.WtsProperty(
            isVisible = true,
            Title = "Skip dagen",
            Description = "Of de timer niet moet worden uitgevoerd op bepaalde dagen (Bijvoorbeeld: 1,2,3,4,5,6,7)",
            KendoTab = KendoTab.Timers,
            KendoComponent = KendoComponent.TextBox,
            BelongsToForm = "runSchemeForm"
        )]
        public string SkipDays { get; set; }
        
        /// <summary>
        /// The day of the week on which the run scheme should run.
        /// </summary>
        [WtsAttributes.WtsProperty(
            isVisible = true,
            Title = "Dag van de week",
            Description = "De dag van de week waarop de timer moet worden uitgevoerd (Bijvoorbeeld: 1 = maandag, 2 = dinsdag, etc.)",
            KendoTab = KendoTab.Timers,
            KendoComponent = KendoComponent.NumericTextBox,
            BelongsToForm = "runSchemeForm"
        )]
        public int? DayOfWeek { get; set; }
        
        /// <summary>
        /// The day of the month on which the run scheme should run.
        /// </summary>
        [WtsAttributes.WtsProperty(
            isVisible = true,
            Title = "Dag van de maand",
            Description = "De dag van de maand waarop de timer moet worden uitgevoerd (Bijvoorbeeld: 1 = 1e dag van de maand, 2 = 2e dag van de maand, etc.)",
            KendoTab = KendoTab.Timers,
            KendoComponent = KendoComponent.NumericTextBox,
            BelongsToForm = "runSchemeForm"
        )]
        public int? DayOfMonth { get; set; }
        
        /// <summary>
        /// The time at which the run scheme is to be executed.
        /// Only if type is not continuous.
        /// </summary>
        [XmlElement("Hour", DataType = "string")]
        [CanBeNull]
        [WtsAttributes.WtsProperty(
            isVisible = true,
            Title = "Tijd",
            Description = "De tijd waarop de timer moet worden uitgevoerd (Formaat: uren:minuten:seconden)",
            KendoTab = KendoTab.Timers,
            KendoComponent = KendoComponent.TimePicker,
            BelongsToForm = "runSchemeForm"
        )]
        public string Hour { get; set; }
        
        /// <summary>
        /// Whether to run the run scheme on the weekend.
        /// </summary>
        [WtsAttributes.WtsProperty(
            isVisible = true,
            Description = "Timer niet uitvoeren in het weekend",
            KendoTab = KendoTab.Timers,
            KendoComponent = KendoComponent.CheckBox,
            BelongsToForm = "runSchemeForm"
        )]
        public bool? SkipWeekend { get; set; }
        
        /// <summary>
        /// If the run scheme should be run immediately on start up of the wts.
        /// </summary>
        [WtsAttributes.WtsProperty(
            isVisible = true,
            Description = "Timer uitvoeren bij opstarten van de WTS",
            KendoTab = KendoTab.Timers,
            KendoComponent = KendoComponent.CheckBox,
            BelongsToForm = "runSchemeForm"
        )]
        public bool? RunImmediately { get; set; }
        
        /// <summary>
        /// The settings to be used for logging.
        /// </summary>
        [CanBeNull]
        [WtsAttributes.WtsProperty(
            isVisible = false,
            KendoTab = KendoTab.Timers,
            isFilled = false
        )]
        public LogSettings LogSettings { get; set; }
    }
}
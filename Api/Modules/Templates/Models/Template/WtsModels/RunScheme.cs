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
            IsVisible = true,
            IsRequired = true,
            Title = "Type",
            Description = "Het type van de timer",
            ConfigurationTab = ConfigurationTab.Timers,
            KendoComponent = KendoComponents.DropDownList
        )]
        public RunSchemeTypes Type { get; set; }
        
        /// <summary>
        /// Unique id of the run scheme.
        /// </summary>
        [WtsAttributes.WtsProperty(
            IsVisible = true,
            IsRequired = true,
            Title = "TimeId",
            Description = "Het unieke id van de timer",
            ConfigurationTab = ConfigurationTab.Timers,
            KendoComponent = KendoComponents.NumericTextBox,
            KendoOptions = @"
               {
                  ""format"": ""#"",
                  ""decimals"": 0
                }
            "
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
             IsVisible = true,
             Title = "Wachttijd",
             Description = "De tijd tussen elke run. Formaat: uren:minuten:seconden",
             ConfigurationTab = ConfigurationTab.Timers,
             KendoComponent = KendoComponents.TimePicker,
             DependsOnField = "Type",
             DependsOnValue = new [] {"Continuous"},
             KendoOptions = @"
               {
                  ""dateInput"": ""true"",
                  ""componentType"": ""modern"",
                  ""format"": ""HH:mm:ss""
                }
            "
         )]
        public string Delay { get; set; }
        
        /// <summary>
        /// The time at which the run scheme is to be executed.
        /// Only if type is not continuous.
        /// </summary>
        [XmlElement("Hour", DataType = "string")]
        [CanBeNull]
        [WtsAttributes.WtsProperty(
            IsVisible = true,
            Title = "Tijd",
            Description = "De tijd waarop de timer moet worden uitgevoerd (Formaat: uren:minuten:seconden)",
            ConfigurationTab = ConfigurationTab.Timers,
            KendoComponent = KendoComponents.TimePicker,
            DependsOnField = "Type",
            DependsOnValue = new [] {"Daily", "Weekly", "Monthly"},
            KendoOptions = @"
               {
                  ""dateInput"": ""true"",
                  ""componentType"": ""modern"",
                  ""format"": ""HH:mm:ss""
                }
            "
        )]
        public string Hour { get; set; }
        
        /// <summary>
        /// The time from when the actions associated with this runscheme are started.
        /// </summary>
        [XmlElement("StartTime", DataType = "string")]
        [CanBeNull]
        [WtsAttributes.WtsProperty(
            IsVisible = true,
            Title = "Starttijd",
            Description = "De tijd vanaf wanneer de acties van deze timer worden uitgevoerd",
            ConfigurationTab = ConfigurationTab.Timers,
            KendoComponent = KendoComponents.TimePicker,
            DependsOnField = "Type",
            DependsOnValue = new [] {"Continuous"},
            KendoOptions = @"
               {
                  ""dateInput"": ""true"",
                  ""componentType"": ""modern"",
                  ""format"": ""HH:mm:ss""
                }
            "
        )]
        public string StartTime { get; set; }
        
        /// <summary>
        /// The time at which the actions associated with this runscheme will no longer be executed.
        /// </summary>
        [XmlElement("StopTime", DataType = "string")]
        [CanBeNull]
        [WtsAttributes.WtsProperty(
            IsVisible = true,
            Title = "Stoptijd",
            Description = "De tijd tot wanneer de acties van deze timer worden uitgevoerd",
            ConfigurationTab = ConfigurationTab.Timers,
            KendoComponent = KendoComponents.TimePicker,
            DependsOnField = "Type",
            DependsOnValue = new [] {"Continuous"},
            KendoOptions = @"
               {
                  ""dateInput"": ""true"",
                  ""componentType"": ""modern"",
                  ""format"": ""HH:mm:ss""
                }
            "
        )]
        public string StopTime { get; set; }
        
        /// <summary>
        /// Whether the run scheme should not be executed on specific days.
        /// </summary>
        [CanBeNull]
        [WtsAttributes.WtsProperty(
            IsVisible = true,
            Title = "Skip dagen",
            Description = "Of de timer niet moet worden uitgevoerd op bepaalde dagen (Bijvoorbeeld: 1,2,3,4,5,6,7)",
            ConfigurationTab = ConfigurationTab.Timers,
            KendoComponent = KendoComponents.NumericTextBox,
            KendoOptions = @"
               {
                  ""format"": ""#"",
                  ""decimals"": 0
                }
            "
        )]
        public string SkipDays { get; set; }
        
        /// <summary>
        /// The day of the week on which the run scheme should run.
        /// </summary>
        [WtsAttributes.WtsProperty(
            IsVisible = true,
            Title = "Dag van de week",
            Description = "De dag van de week waarop de timer moet worden uitgevoerd (Bijvoorbeeld: 1 = maandag, 2 = dinsdag, etc.)",
            ConfigurationTab = ConfigurationTab.Timers,
            KendoComponent = KendoComponents.NumericTextBox,
            KendoOptions = @"
               {
                  ""format"": ""#"",
                  ""decimals"": 0
                }
            ",
            DependsOnField = "Type",
            DependsOnValue = new [] {"Weekly"}
        )]
        public int? DayOfWeek { get; set; }
        
        [XmlIgnore]
        public bool DayOfWeekSpecified
        {
            get { return DayOfWeek.HasValue; }
        }
        
        /// <summary>
        /// The day of the month on which the run scheme should run.
        /// </summary>
        [WtsAttributes.WtsProperty(
            IsVisible = true,
            Title = "Dag van de maand",
            Description = "De dag van de maand waarop de timer moet worden uitgevoerd (Bijvoorbeeld: 1 = 1e dag van de maand, 2 = 2e dag van de maand, etc.)",
            ConfigurationTab = ConfigurationTab.Timers,
            KendoComponent = KendoComponents.NumericTextBox,
            KendoOptions = @"
               {
                  ""format"": ""#"",
                  ""decimals"": 0
                }
            ",
            DependsOnField = "Type",
            DependsOnValue = new [] {"Monthly"}
        )]
        public int? DayOfMonth { get; set; }
        
        [XmlIgnore]
        public bool DayOfMonthSpecified
        {
            get { return DayOfMonth.HasValue; }
        }
        
        /// <summary>
        /// Whether to run the run scheme on the weekend.
        /// </summary>
        [WtsAttributes.WtsProperty(
            IsVisible = true,
            Description = "Timer niet uitvoeren in het weekend",
            ConfigurationTab = ConfigurationTab.Timers,
            KendoComponent = KendoComponents.CheckBox
        )]
        public bool? SkipWeekend { get; set; }
        
        [XmlIgnore]
        public bool SkipWeekendSpecified
        {
            get { return SkipWeekend.HasValue; }
        }
        
        /// <summary>
        /// If the run scheme should be run immediately on start up of the wts.
        /// </summary>
        [WtsAttributes.WtsProperty(
            IsVisible = true,
            Description = "Timer uitvoeren bij opstarten van de WTS",
            ConfigurationTab = ConfigurationTab.Timers,
            KendoComponent = KendoComponents.CheckBox
        )]
        public bool? RunImmediately { get; set; }
        
        [XmlIgnore]
        public bool RunImmediatelySpecified
        {
            get { return RunImmediately.HasValue; }
        }
        
        /// <summary>
        /// The settings to be used for logging.
        /// </summary>
        [CanBeNull]
        [WtsAttributes.WtsProperty(
            IsVisible = false,
            ConfigurationTab = ConfigurationTab.Timers
        )]
        public LogSettings LogSettings { get; set; }
    }
}
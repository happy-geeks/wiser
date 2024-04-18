using System.Xml.Serialization;
using Api.Modules.Templates.Attributes;
using Api.Modules.Templates.Enums;
using Api.Modules.Templates.Models.Template.WtsModels;
using JetBrains.Annotations;

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
        [WtsProperty(
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
        [WtsProperty(
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
        [WtsProperty(
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
        [WtsProperty(
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
        [WtsProperty(
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
        [WtsProperty(
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
        [WtsProperty(
            IsVisible = true,
            Title = "Skip dagen",
            Description = "Of de timer niet moet worden uitgevoerd op bepaalde dagen (Bijvoorbeeld: 1,2,3,4,5,6,7)",
            ConfigurationTab = ConfigurationTab.Timers,
            KendoComponent = KendoComponents.TextBox,
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
        /// <comment>
        /// Normally an enum is used for this, but since the values
        /// require a alternative display name, this is not possible.
        /// The form builder uses either an enum
        /// or a datasource given in the kendoOptions
        /// </comment>
        [WtsProperty(
            IsVisible = true,
            Title = "Dag van de week",
            Description = "De dag van de week waarop de timer moet worden uitgevoerd (Bijvoorbeeld: 1 = maandag, 2 = dinsdag, etc.)",
            ConfigurationTab = ConfigurationTab.Timers,
            KendoComponent = KendoComponents.DropDownList,
            KendoOptions = @"
               {
                  ""format"": ""#"",
                  ""decimals"": 0,
                  ""dataSource"": [
                    { ""text"": ""Maandag"", ""value"": 1 },
                    { ""text"": ""Dinsdag"", ""value"": 2 },
                    { ""text"": ""Woensdag"", ""value"": 3 },
                    { ""text"": ""Donderdag"", ""value"": 4 },
                    { ""text"": ""Vrijdag"", ""value"": 5 },
                    { ""text"": ""Zaterdag"", ""value"": 6 },
                    { ""text"": ""Zondag"", ""value"": 7 }
                  ],
                  ""dataTextField"": ""text"",
                  ""dataValueField"": ""value""
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
        [WtsProperty(
            IsVisible = true,
            Title = "Dag van de maand",
            Description = "De dag van de maand waarop de timer wordt uitgevoerd (bijv. 1 = 1e dag). Als de dag niet bestaat, wordt de laatste dag van de maand gebruikt.",
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
        [WtsProperty(
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
        [WtsProperty(
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
        [WtsProperty(
            IsVisible = false,
            ConfigurationTab = ConfigurationTab.Timers
        )]
        public LogSettings LogSettings { get; set; }
    }
}
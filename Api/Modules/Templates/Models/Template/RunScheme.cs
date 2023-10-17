using System.Xml.Serialization;
using Api.Modules.Templates.Enums;
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
        public RunSchemeTypes Type { get; set; }
        
        /// <summary>
        /// Unique id of the run scheme.
        /// </summary>
        public int TimeId { get; set; }
        
        /// <summary>
        /// How much time should be between each run.
        /// Format: hours:minutes:seconds
        /// Only if type is continuous.
        /// </summary>
        [XmlElement("Delay", DataType = "string")]
        [CanBeNull]
        public string Delay { get; set; }
        
        /// <summary>
        /// The time from when the actions associated with this runscheme are started.
        /// </summary>
        [XmlElement("StartTime", DataType = "string")]
        [CanBeNull]
        public string StartTime { get; set; }
        
        /// <summary>
        /// The time at which the actions associated with this runscheme will no longer be executed.
        /// </summary>
        [XmlElement("StopTime", DataType = "string")]
        [CanBeNull]
        public string StopTime { get; set; }
        
        /// <summary>
        /// Whether to run the run scheme on the weekend.
        /// </summary>
        public bool? SkipWeekend { get; set; }
        
        /// <summary>
        /// Whether the run scheme should not be executed on specific days.
        /// </summary>
        [CanBeNull]
        public string SkipDays { get; set; }
        
        /// <summary>
        /// The day of the week on which the run scheme should run.
        /// </summary>
        public int? DayOfWeek { get; set; }
        
        /// <summary>
        /// The day of the month on which the run scheme should run.
        /// </summary>
        public int? DayOfMonth { get; set; }
        
        /// <summary>
        /// The time at which the run scheme is to be executed.
        /// Only if type is not continuous.
        /// </summary>
        [XmlElement("Hour", DataType = "string")]
        [CanBeNull]
        public string Hour { get; set; }
        
        /// <summary>
        /// The settings to be used for logging.
        /// </summary>
        [CanBeNull]
        public LogSettings LogSettings { get; set; }
        
        /// <summary>
        /// If the run scheme should be run immediately on start up of the wts.
        /// </summary>
        public bool? RunImmediately { get; set; }
    }
}
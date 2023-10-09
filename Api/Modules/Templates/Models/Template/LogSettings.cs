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
        public LogMinimumLevels LogMinimumLevel {get; set;}
        
        /// <summary>
        /// Log messages sent at startup and shutdown. For example, the configuration being started or stopped.
        /// </summary>
        public bool LogStartAndStop {get; set;}
        
        /// <summary>
        /// Log messages sent at the beginning and end of the run. For example, the start and stop time of the run scheme or what action is performed.
        /// </summary>
        public bool LogRunStartAndStop {get; set;}
        
        /// <summary>
        /// Log messages sent during the run. For example, the query being executed or the URL of an HTTP API request.
        /// </summary>
        public bool LogRunBody {get; set;}
    }
}
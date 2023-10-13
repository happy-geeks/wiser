using System.Xml.Serialization;

namespace Api.Modules.Templates.Models.Template
{
    /// <summary>
    /// A model for parsed xml of a template.
    /// </summary>
    [XmlRoot("Configuration", Namespace = "")]
    public class TemplateWtsConfigurationModel
    {
        /// <summary>
        /// Gets or sets the service name of the editor value of the template.
        /// </summary>
        [XmlElement("ServiceName")]
        public string ServiceName { get; set; }
        
        /// <summary>
        /// Gets or sets the connection string of the editor value of the template.
        /// </summary>
        [XmlElement("ConnectionString")]
        public string ConnectionString { get; set; }
        
        /// <summary>
        /// Gets or sets the log settings for the configuration (Global if not overwritten)
        /// </summary>
        public LogSettings LogSettings { get; set; }
        
        /// <summary>
        /// All levels of minimum logging
        /// </summary>
        [XmlIgnore]
        public string[] LogMinimumLevels { get; set; }
        
        /// <summary>
        /// All run scheme types
        /// </summary>
        [XmlIgnore]
        public string[] RunSchemeTypes { get; set; }
    }
}
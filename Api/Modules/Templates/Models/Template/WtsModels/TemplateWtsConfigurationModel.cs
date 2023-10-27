using System.Collections.Generic;
using System.Xml.Serialization;

namespace Api.Modules.Templates.Models.Template.WtsModels
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
        /// Gets or sets the run schemes settings for the configuration
        /// </summary>
        public List<RunScheme> RunSchemes { get; set; }
        
        /// <summary>
        /// Gets or sets the queries in the configuration.
        /// </summary>
        [XmlElement("Query")]
        public List<QueryModel> Queries { get; set; }
        
        /// <summary>
        /// Gets or sets the http api's in the configuration.
        /// </summary>
        [XmlElement("HttpApi")]
        public List<HttpApiModel> HttpApis { get; set; }
        
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
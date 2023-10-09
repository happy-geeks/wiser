namespace Api.Modules.Templates.Models.Template
{
    /// <summary>
    /// A model for parsed xml of a template.
    /// </summary>
    public class TemplateParsedXmlModel
    {
        /// <summary>
        /// Gets or sets the ID of the template.
        /// </summary>
        public int TemplateId { get; set; }
        
        /// <summary>
        /// Gets or sets the service name of the editor value of the template.
        /// </summary>
        public string ServiceName { get; set; }
        
        /// <summary>
        /// Gets or sets the connection string of the editor value of the template.
        /// </summary>
        public string ConnectionString { get; set; }
        
        /// <summary>
        /// Gets or sets the log settings for the configuration (Global if not overwritten)
        /// </summary>
        public LogSettings ConfigurationSettings { get; set; }
        
        /// <summary>
        /// All levels of minimum logging
        /// </summary>
        public string[] LogMinimumLevels { get; set; }
        
        /// <summary>
        /// All run scheme types
        /// </summary>
        public string[] RunSchemeTypes { get; set; }
    }
}
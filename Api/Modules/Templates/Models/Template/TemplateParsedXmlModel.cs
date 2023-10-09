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
    }
}
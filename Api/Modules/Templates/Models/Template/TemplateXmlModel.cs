namespace Api.Modules.Templates.Models.Template
{
    /// <summary>
    /// A model for xml of a template.
    /// </summary>
    public class TemplateXmlModel
    {
        /// <summary>
        /// Gets or sets the ID of the template.
        /// </summary>
        public int TemplateId { get; set; }
        
        /// <summary>
        /// Gets or sets the editorValue of the template.
        /// </summary>
        public string EditorValue { get; set; }
    }
}
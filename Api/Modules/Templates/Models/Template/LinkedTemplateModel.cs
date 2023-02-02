using GeeksCoreLibrary.Modules.Templates.Enums;

namespace Api.Modules.Templates.Models.Template
{
#pragma warning disable CS1591
    public class LinkedTemplateModel
#pragma warning restore CS1591
    {
        /// <summary>
        /// Gets or sets the ID of the linked template
        /// </summary>
        public int TemplateId { get; set; }

        /// <summary>
        /// Gets or sets the name of the linked template
        /// </summary>
        public string TemplateName { get; set; }

        /// <summary>
        /// Gets or sets the path of the linked template
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the type of how the template is linked
        /// </summary>
        public TemplateTypes LinkType { get; set; }
    }
}

using GeeksCoreLibrary.Modules.Templates.Enums;

namespace Api.Modules.Templates.Models.Template
{
    public class LinkedTemplateModel
    {
        public int TemplateId { get; set; }
        public string TemplateName { get; set; }

        public TemplateTypes LinkType { get; set; }
    }
}

using Api.Modules.Templates.Enums;

namespace Api.Modules.Templates.Models.Template
{
    public class LinkedTemplateModel
    {
        public int TemplateId { get; set; }
        public string TemplateName { get; set; }
        public int ParentId { get; set; }
        public string ParentName { get; set; }

        public LinkedTemplatesEnum LinkType { get; set; }
        public string LinkName { get; set; }
    }
}

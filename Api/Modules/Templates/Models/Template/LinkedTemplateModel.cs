using Api.Modules.Templates.Enums;

namespace Api.Modules.Templates.Models.Template
{
    public class LinkedTemplateModel
    {
        public int templateId { get; set; }
        public string templateName { get; set; }
        public int parentId { get; set; }
        public string parentName { get; set; }

        public LinkedTemplatesEnum linkType { get; set; }
        public string linkName { get; set; }
    }
}

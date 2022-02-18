using System.Collections.Generic;

namespace Api.Modules.Templates.Models.Template
{
    public class LinkedTemplatesModel
    {
        public List<LinkedTemplateModel> LinkedScssTemplates { get; set; } = new();

        public List<LinkedTemplateModel> LinkedCssTemplates { get; set; } = new();

        public List<LinkedTemplateModel> LinkedJavascript { get; set; } = new();

        public List<LinkedTemplateModel> LinkOptionsTemplates { get; set; } = new();

        public string RawLinkList { get; set; }
    }
}

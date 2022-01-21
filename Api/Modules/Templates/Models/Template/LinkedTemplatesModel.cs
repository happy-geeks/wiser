using System.Collections.Generic;

namespace Api.Modules.Templates.Models.Template
{
    public class LinkedTemplatesModel
    {
        public List<LinkedTemplateModel> LinkedSccsTemplates;
        public List<LinkedTemplateModel> LinkedCssTemplates;

        public List<LinkedTemplateModel> LinkedJavascript;

        public List<LinkedTemplateModel> LinkOptionsTemplates;

        public string RawLinkList;

        public LinkedTemplatesModel ()
        {
            this.LinkedCssTemplates = new List<LinkedTemplateModel>();
            this.LinkedJavascript = new List<LinkedTemplateModel>();
            this.LinkedSccsTemplates = new List<LinkedTemplateModel>();
        }
    }
}

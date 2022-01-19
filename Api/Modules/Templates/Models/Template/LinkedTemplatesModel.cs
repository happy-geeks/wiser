using System.Collections.Generic;

namespace Api.Modules.Templates.Models.Template
{
    public class LinkedTemplatesModel
    {
        public List<LinkedTemplateModel> linkedSCCSTemplates;
        public List<LinkedTemplateModel> linkedCSSTemplates;

        public List<LinkedTemplateModel> linkedJavascript;

        public List<LinkedTemplateModel> linkOptionsTemplates;

        public string rawLinkList;

        public LinkedTemplatesModel ()
        {
            this.linkedCSSTemplates = new List<LinkedTemplateModel>();
            this.linkedJavascript = new List<LinkedTemplateModel>();
            this.linkedSCCSTemplates = new List<LinkedTemplateModel>();
        }
    }
}

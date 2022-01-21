using System;

namespace Api.Modules.Templates.Models.Template
{
    public class TemplateTreeViewModel
    {
        public int TemplateId { get; set; }
        public string TemplateName { get; set; }
        public Boolean IsFolder { get; set; }
        public Boolean HasChildren { get; set; }

        public TemplateTreeViewModel ()
        {

        }

        public TemplateTreeViewModel(int templateId, string templateName, Boolean isFolder, Boolean hasChildren)
        {
            this.TemplateId = templateId;
            this.TemplateName = templateName;
            this.IsFolder = isFolder;
            this.HasChildren = hasChildren;
        }
    }
}

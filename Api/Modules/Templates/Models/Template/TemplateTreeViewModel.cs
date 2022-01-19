using System;

namespace Api.Modules.Templates.Models.Template
{
    public class TemplateTreeViewModel
    {
        public int templateId { get; set; }
        public string templateName { get; set; }
        public Boolean isFolder { get; set; }
        public Boolean hasChildren { get; set; }

        public TemplateTreeViewModel ()
        {

        }

        public TemplateTreeViewModel(int templateId, string templateName, Boolean isFolder, Boolean hasChildren)
        {
            this.templateId = templateId;
            this.templateName = templateName;
            this.isFolder = isFolder;
            this.hasChildren = hasChildren;
        }
    }
}

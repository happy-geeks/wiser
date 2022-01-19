using System.Collections.Generic;

namespace Api.Modules.Templates.Models.Template
{
    public class TemplateTreeViewRootModel
    {
        public int templateId { get; set; }
        public string templateName { get; set; }
        public int templateType { get; set; }
        public List<TemplateTreeViewModel> childNodes { get; set; }

        public TemplateTreeViewRootModel ()
        {

        }

        public TemplateTreeViewRootModel(int templateId, string templateName, int templateType)
        {
            this.templateId = templateId;
            this.templateName = templateName;
            this.templateType = templateType;
        }

        public TemplateTreeViewRootModel (int templateId, string templateName, int templateType, List<TemplateTreeViewModel> childNodes)
        {
            this.templateId = templateId;
            this.templateName = templateName;
            this.templateType = templateType;
            this.childNodes = childNodes;
        }

        public void SetChildNodes(List<TemplateTreeViewModel> treeviewList)
        {
            this.childNodes = treeviewList;
        }
    }
}

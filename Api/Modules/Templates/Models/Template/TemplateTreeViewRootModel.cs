using System.Collections.Generic;

namespace Api.Modules.Templates.Models.Template
{
    public class TemplateTreeViewRootModel
    {
        public int TemplateId { get; set; }
        public string TemplateName { get; set; }
        public int TemplateType { get; set; }
        public List<TemplateTreeViewModel> ChildNodes { get; set; }

        public TemplateTreeViewRootModel ()
        {

        }

        public TemplateTreeViewRootModel(int templateId, string templateName, int templateType)
        {
            this.TemplateId = templateId;
            this.TemplateName = templateName;
            this.TemplateType = templateType;
        }

        public TemplateTreeViewRootModel (int templateId, string templateName, int templateType, List<TemplateTreeViewModel> childNodes)
        {
            this.TemplateId = templateId;
            this.TemplateName = templateName;
            this.TemplateType = templateType;
            this.ChildNodes = childNodes;
        }

        public void SetChildNodes(List<TemplateTreeViewModel> treeviewList)
        {
            this.ChildNodes = treeviewList;
        }
    }
}

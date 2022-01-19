using System;

namespace Api.Modules.Templates.Models.Template
{
    public class TemplateTreeViewDAO
    {
        int templateId { get; set; }
        string templateName { get; set; }
        int templateType { get; set; }
        int? parentId { get; set; }

        Boolean hasChildren { get; set; }

        public TemplateTreeViewDAO(int templateId, string templateName, int templateType, int? parentId, Boolean hasChildren)
        {
            this.templateId = templateId;
            this.templateName = templateName;
            this.templateType = templateType;
            this.parentId = parentId;
            this.hasChildren = hasChildren;
        }

        public int GetTemplateId ()
        {
            return this.templateId;
        }

        public string GetTemplateName ()
        {
            return this.templateName;
        }

        public int GetTemplateType ()
        {
            return this.templateType;
        }

        public int? GetParentId ()
        {
            return this.parentId;
        }

        public Boolean GetHasChildren ()
        {
            return this.hasChildren;
        }
    }
}

using System;

namespace Api.Modules.Templates.Models.Template
{
    public class TemplateTreeViewDao
    {
        int TemplateId { get; set; }
        string TemplateName { get; set; }
        int TemplateType { get; set; }
        int? ParentId { get; set; }

        Boolean HasChildren { get; set; }

        public TemplateTreeViewDao(int templateId, string templateName, int templateType, int? parentId, Boolean hasChildren)
        {
            this.TemplateId = templateId;
            this.TemplateName = templateName;
            this.TemplateType = templateType;
            this.ParentId = parentId;
            this.HasChildren = hasChildren;
        }

        public int GetTemplateId ()
        {
            return this.TemplateId;
        }

        public string GetTemplateName ()
        {
            return this.TemplateName;
        }

        public int GetTemplateType ()
        {
            return this.TemplateType;
        }

        public int? GetParentId ()
        {
            return this.ParentId;
        }

        public Boolean GetHasChildren ()
        {
            return this.HasChildren;
        }
    }
}

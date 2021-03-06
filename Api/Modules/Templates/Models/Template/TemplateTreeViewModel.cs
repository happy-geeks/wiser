using System.Collections.Generic;

namespace Api.Modules.Templates.Models.Template
{
    public class TemplateTreeViewModel
    {
        private bool isFolder;
        public int TemplateId { get; set; }
        public string TemplateName { get; set; }

        public bool IsFolder
        {
            get => isFolder;
            set
            {
                isFolder = value;
                if (!value) return;
                CollapsedSpriteCssClass = "icon-folder-closed";
                ExpandedSpriteCssClass = "icon-folder";
                SpriteCssClass = "icon-folder-closed";
            }
        }

        public bool HasChildren { get; set; }
        public string CollapsedSpriteCssClass { get; set; }
        public string ExpandedSpriteCssClass { get; set; }
        public string SpriteCssClass { get; set; }

        public TemplateSettingsModel TemplateSettings { get; set; }

        public List<TemplateTreeViewModel> ChildNodes { get; set; }

        public TemplateTreeViewModel()
        {

        }

        public TemplateTreeViewModel(int templateId, string templateName, bool isFolder, bool hasChildren)
        {
            this.TemplateId = templateId;
            this.TemplateName = templateName;
            this.IsFolder = isFolder;
            this.HasChildren = hasChildren;
        }
    }
}

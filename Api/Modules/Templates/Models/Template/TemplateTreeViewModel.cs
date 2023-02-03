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
        public int TemplateType { get; set; }

        /// <summary>
        /// Whether the tree view item was not retrieved from the templates data.
        /// The <see cref="TemplateType"/> property should identify where this item was retrieved from instead.
        /// </summary>
        /// <remarks>
        /// This is typically meant for views, routines, and triggers that were retrieved from the database.
        /// </remarks>
        public bool IsVirtualItem { get; set; }
    }
}

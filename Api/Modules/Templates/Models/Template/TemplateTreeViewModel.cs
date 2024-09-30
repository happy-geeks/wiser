using System.Collections.Generic;

namespace Api.Modules.Templates.Models.Template
{
    /// <summary>
    /// Model class that contains all information needed to create a tree view of templates.
    /// </summary>
    public class TemplateTreeViewModel
    {
        private bool isFolder;
        
        /// <summary>
        /// Gets or sets the ID of the Template.
        /// </summary>
        public int TemplateId { get; set; }

        /// <summary>
        /// Gets or sets the name of the Template.
        /// </summary>
        public string TemplateName { get; set; }

        /// <summary>
        /// Gets or sets whether a Template is a folder or not.
        /// </summary>
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

        /// <summary>
        /// Gets or sets if the template has any children or not.
        /// </summary>
        public bool HasChildren { get; set; }
        
        /// <summary>
        /// Gets or sets the CSS class (for example the folder) which is collapsed.
        /// </summary>
        public string CollapsedSpriteCssClass { get; set; }
        
        /// <summary>
        /// Gets or sets the CSS class (for example the folder) which is expanded.
        /// </summary>
        public string ExpandedSpriteCssClass { get; set; }
        
        /// <summary>
        /// Gets or sets the base CSS class (for example the folder).
        /// </summary>
        public string SpriteCssClass { get; set; }

        /// <summary>
        /// Gets or sets the settings for the Template.
        /// </summary>
        public TemplateSettingsModel TemplateSettings { get; set; }

        /// <summary>
        /// Gets or sets a list of all child nodes this template contains.
        /// </summary>
        public List<TemplateTreeViewModel> ChildNodes { get; set; }
        
        /// <summary>
        /// Gets or sets the type of the template.
        /// </summary>
        public int TemplateType { get; set; }

        /// <summary>
        /// Whether the tree view item was not retrieved from the templates data.
        /// The <see cref="TemplateType"/> property should identify where this item was retrieved from instead.
        /// </summary>
        /// <remarks>
        /// This is typically meant for views, routines, and triggers that were retrieved from the database.
        /// </remarks>
        public bool IsVirtualItem { get; set; }
        
        /// <summary>
        /// Whether the item should start as expanded in the treeview.
        /// </summary>
        public bool Expanded { get; set; }
    }
}

using System;
using GeeksCoreLibrary.Modules.Templates.Enums;

namespace Api.Modules.Templates.Models.Template
{
    /// <summary>
    /// Data Access Object with information about templates which are shown in a tree view to be written to the database 
    /// </summary>
    public class TemplateTreeViewDao
    {
        /// <summary>
        /// Gets or sets the ID of the Template
        /// </summary>
        public int TemplateId { get; set; }
        
        /// <summary>
        /// Gets or sets the Name of the Template
        /// </summary>
        public string TemplateName { get; set; }
        
        /// <summary>
        /// Gets or sets the Type of the Template
        /// </summary>
        public TemplateTypes TemplateType { get; set; }
        /// <summary>
        /// Gets or sets the ID of the higher up template in the treeview
        /// This is an optional value as an template can also be the highest element in the tree view.
        /// </summary>
        public int? ParentId { get; set; }
        
        /// <summary>
        /// Gets or sets if the templates has underlying templates aka children
        /// </summary>
        public bool HasChildren { get; set; }

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

using GeeksCoreLibrary.Modules.Templates.Enums;

namespace Api.Modules.Templates.Models.Template
{
    public class TemplateTreeViewDao
    {
        public int TemplateId { get; set; }
        public string TemplateName { get; set; }
        public TemplateTypes TemplateType { get; set; }
        public int? ParentId { get; set; }
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

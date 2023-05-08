using System.Collections.Generic;

namespace Api.Modules.Templates.Models.Template
{
    /// <summary>
    /// Model class which contains information about the highest template in the tree view
    /// </summary>
    public class TemplateTreeViewRootModel
    {
        /// <summary>
        /// Gets or sets the ID of the Template
        /// </summary>
        public int TemplateId { get; set; }
        
        /// <summary>
        /// Gets or sets the name of the Template
        /// </summary>
        public string TemplateName { get; set; }
        
        /// <summary>
        /// Gets or sets the type of the Template
        /// </summary>
        public int TemplateType { get; set; }
        
        /// <summary>
        /// Gets or sets a list of all underlying templates in the tree view
        /// </summary>
        public List<TemplateTreeViewModel> ChildNodes { get; set; }

#pragma warning disable CS1591
        public TemplateTreeViewRootModel ()
#pragma warning restore CS1591
        {

        }

        /// <summary>
        /// Constructor for a template treeview without the childnodes.
        /// </summary>
        /// <param name="templateId">The ID of the Template</param>
        /// <param name="templateName">The name of the Template</param>
        /// <param name="templateType">The type of the Template</param>
        public TemplateTreeViewRootModel(int templateId, string templateName, int templateType)
        {
            this.TemplateId = templateId;
            this.TemplateName = templateName;
            this.TemplateType = templateType;
        }

        /// <summary>
        /// Constructor for a template treeview with the childnodes.
        /// </summary>
        /// <param name="templateId">The ID of the Template</param>
        /// <param name="templateName">The name of the Template</param>
        /// <param name="templateType">The type of the Template</param>
        /// <param name="childNodes">A list of all underlying templates in the tree view</param>
        public TemplateTreeViewRootModel (int templateId, string templateName, int templateType, List<TemplateTreeViewModel> childNodes)
        {
            this.TemplateId = templateId;
            this.TemplateName = templateName;
            this.TemplateType = templateType;
            this.ChildNodes = childNodes;
        }
    }
}

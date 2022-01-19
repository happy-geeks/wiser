using System.Collections.Generic;
using Api.Modules.Templates.Models.Template;
using FrontEnd.Modules.Base.Models;

namespace FrontEnd.Modules.Templates.Models
{
    public class TemplateOverviewViewModel : BaseModuleViewModel
    {
        /// <summary>
        /// Gets or sets the tree view root data.
        /// </summary>
        public List<TemplateTreeViewModel> TreeView { get; set; }
    }
}

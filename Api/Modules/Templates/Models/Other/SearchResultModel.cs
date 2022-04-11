using System.Collections.Generic;
using Api.Modules.Templates.Models.Template;
using GeeksCoreLibrary.Modules.Templates.Enums;

namespace Api.Modules.Templates.Models.Other
{
    /// <summary>
    /// A model for returning template search results.
    /// </summary>
    public class SearchResultModel : TemplateTreeViewModel
    {

        /// <summary>
        /// Gets or sets the template type (query, html, javascript etc).
        /// </summary>
        public TemplateTypes Type { get; set; }
        
        /// <summary>
        /// Gets or sets the parent directory of the template.
        /// </summary>
        public int ParentId { get; set; }
    }
}

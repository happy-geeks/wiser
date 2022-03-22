using System.Collections.Generic;
using GeeksCoreLibrary.Modules.Templates.Enums;

namespace Api.Modules.Templates.Models.Other
{
    /// <summary>
    /// A model for returning template search results.
    /// </summary>
    public class SearchResultModel
    {
        /// <summary>
        /// Get or sets the ID of the template.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the template.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the template type (query, html, javascript etc).
        /// </summary>
        public TemplateTypes Type { get; set; }
        
        /// <summary>
        /// Gets or sets the parent directory of the template.
        /// </summary>
        public int ParentId { get; set; }

        /// <summary>
        /// Gets or sets the found children.
        /// </summary>
        public List<SearchResultModel> Children { get; set; }
    }
}

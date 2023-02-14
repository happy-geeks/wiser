using System.Collections.Generic;

namespace Api.Modules.Templates.Models.Preview
{
    /// <summary>
    /// A model to store information about a preview for a template.
    /// </summary>
    public class PreviewProfileModel
    {
        /// <summary>
        /// Gets or sets the ID of the PreviewProfile object
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// Gets or sets the name of the PreviewProfile object
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Gets or sets the URL of the PreviewProfile object
        /// </summary>
        public string Url { get; set; }
        
        /// <summary>
        /// Gets or sets a list of variables that can be added to a PreviewProfile object
        /// </summary>
        public List<PreviewVariableModel> Variables { get; set; }
    }
}

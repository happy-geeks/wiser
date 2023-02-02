using System.Collections.Generic;

namespace Api.Modules.Templates.Models.Preview
{
#pragma warning disable CS1591
    public class PreviewProfileModel
#pragma warning restore CS1591
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

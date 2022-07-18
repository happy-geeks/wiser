using Api.Modules.Templates.Models.Other;

namespace Api.Modules.VersionControl.Models
{
    /// <summary>
    /// A model that represents a template with all the version that are published.
    /// </summary>
    public class TemplateEnvironments
    {
        /// <summary>
        /// The id of the template
        /// </summary>
        public int TemplateId { get; set; }

        /// <summary>
        /// The published environment model
        /// </summary>
        public PublishedEnvironmentModel PublishedEnvironments { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Api.Modules.Templates.Models.Other;
using Api.Modules.Templates.Models.Template;
using GeeksCoreLibrary.Modules.Templates.Enums;

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
        /// The published enviornement model
        /// </summary>
        public PublishedEnvironmentModel PublishedEnvironments { get; set; }
    }
}

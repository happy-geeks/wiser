using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Api.Modules.Templates.Models.Other;

namespace Api.Modules.VersionControl.Models
{
    /// <summary>
    /// A model of the dynamic content item
    /// </summary>
    public class DynamicContentModel
    {
        /// <summary>
        /// Gets or sets the Id
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// Gets or sets the Version
        /// </summary>
        public int Version { get; set; }
        /// <summary>
        /// Gets or sets the Title
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// Gets or sets the Component
        /// </summary>
        public string Component { get; set; }
        /// <summary>
        /// Gets or sets the Component Mode
        /// </summary>
        public string ComponentMode { get; set; }
        /// <summary>
        /// Gets or sets the Component Mode Id
        /// </summary>
        public int? ComponentModeId { get; set; }
        /// <summary>
        /// Gets or sets the Usages
        /// </summary>
        public List<string> Usages { get; set; }
        /// <summary>
        /// Gets or sets the Renders
        /// </summary>
        public int? Renders { get; set; }
        /// <summary>
        /// Gets or sets the Average render time
        /// </summary>
        public int? AverageRenderTime { get; set; }
        /// <summary>
        /// Gets or sets the Changed on date
        /// </summary>
        public DateTime? ChangedOn { get; set; }
        /// <summary>
        /// Gets or sets the Chaned by
        /// </summary>
        public string ChangedBy { get; set; }
        /// <summary>
        /// Gets or sets the LatestVersion
        /// </summary>
        public int? LatestVersion { get; set; }

    }
}

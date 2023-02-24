using System;
using System.Collections.Generic;
using Api.Modules.Templates.Models.Other;

namespace Api.Modules.Templates.Models.DynamicContent
{
    /// <summary>
    /// Model class for the information in a Dynamic Content Overview
    /// </summary>
    public class DynamicContentOverviewModel
    {
        /// <summary>
        /// Gets or sets the ID of the Dynamic Content Overview
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// Gets or sets the Title of the Dynamic Content Overview
        /// </summary>
        public string Title { get; set; }
        
        /// <summary>
        /// Gets or sets the component of the Dynamic Content Overview
        /// </summary>
        public string Component { get; set; }
        
        /// <summary>
        /// Gets or sets the component mode of the Dynamic Content Overview
        /// </summary>
        public string ComponentMode { get; set; }
        
        /// <summary>
        /// Gets or sets the ID of the component mode of the Dynamic Content Overview
        /// </summary>
        public int? ComponentModeId { get; set; }
        
        /// <summary>
        /// Gets or sets a list of usages of the Dynamic Content Overview
        /// </summary>
        public List<string> Usages { get; set; }
        
        /// <summary>
        /// Gets or sets the amount of Renders of a Dynamic Content Overview
        /// </summary>
        public int? Renders { get; set; }
        
        /// <summary>
        /// Gets or sets the average render time of a Dynamic Content Overview
        /// </summary>
        public int? AverageRenderTime { get; set; }
        
        /// <summary>
        /// Gets or sets the date and time when the Dynamic Content Overview has changed
        /// </summary>
        public DateTime? ChangedOn { get; set; }
        
        /// <summary>
        /// Gets or sets who changed the Dynamic Content Overview
        /// </summary>
        public string ChangedBy { get; set; }
        
        /// <summary>
        /// Gets or sets the version number of the latest version of the Dynamic Content Overview
        /// </summary>
        public int? LatestVersion { get; set; }
        
        /// <summary>
        /// Gets or sets an key/value dictionary of data for the Dynamic Content Overview
        /// </summary>
        public Dictionary<string, object> Data { get; set; }

        /// <summary>
        /// Gets or sets an object which hold which version the Dynamic Content Overview is on. This is on all published environments
        /// </summary>
        public PublishedEnvironmentModel Versions { get; set; }

        /// <summary>
        /// Gets or sets of the ID of the template
        /// </summary>
        public int? TemplateId { get; set; }

        /// <summary>
        /// Gets or sets of the path of the template
        /// </summary>
        public string TemplatePath { get; set; }
    }
}

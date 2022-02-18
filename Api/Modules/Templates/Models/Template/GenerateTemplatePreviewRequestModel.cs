using System;
using System.Collections.Generic;
using Api.Modules.Templates.Models.Preview;

namespace Api.Modules.Templates.Models.Template
{
    /// <summary>
    /// A model for a request to generate a preview of an HTML template.
    /// </summary>
    public class GenerateTemplatePreviewRequestModel
    {
        /// <summary>
        /// Gets or sets the template settings.
        /// </summary>
        public TemplateSettingsModel TemplateSettings { get; set; }

        /// <summary>
        /// Gets or sets the URL to simulate.
        /// </summary>
        public Uri Url { get; set; }

        /// <summary>
        /// Gets or sets any extra variables for POST/session to simulate.
        /// </summary>
        public List<PreviewVariableModel> PreviewVariables { get; set; }

        /// <summary>
        /// Gets or sets any components with settings that have not been saved to database yet.
        /// </summary>
        public List<GeeksCoreLibrary.Modules.Templates.Models.DynamicContent> Components { get; set; }
    }
}

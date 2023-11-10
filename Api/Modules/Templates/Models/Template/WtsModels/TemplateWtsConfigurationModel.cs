using System.Collections.Generic;
using System.Xml.Serialization;
using Api.Modules.Templates.Attributes;

namespace Api.Modules.Templates.Models.Template.WtsModels
{
    /// <summary>
    /// A model for parsed xml of a template.
    /// </summary>
    [XmlRoot("Configuration", Namespace = "")]
    public class TemplateWtsConfigurationModel
    {
        /// <summary>
        /// Gets or sets the service name of the editor value of the template.
        /// </summary>
        [XmlElement("ServiceName"), WtsAttributes.WtsProperty(
            isVisible = true,
            Title = "Naam",
            Description = "De naam van de service",
            KendoTab = KendoTab.Service,
            KendoComponent = KendoComponent.TextBox
        )]
        public string ServiceName { get; set; }
        
        /// <summary>
        /// Gets or sets the connection string of the editor value of the template.
        /// </summary>
        [XmlElement("ConnectionString"), WtsAttributes.WtsProperty(
             isVisible = true,
             Title = "Connectiestring",
             Description = "De connection string van de database",
             KendoTab = KendoTab.Service,
             KendoComponent = KendoComponent.TextBox
         )]
        public string ConnectionString { get; set; }
        
        /// <summary>
        /// Gets or sets the log settings for the configuration (Global if not overwritten)
        /// </summary>
        [WtsAttributes.WtsProperty(
            isVisible = false,
            KendoTab = KendoTab.Service
        )]
        public LogSettings LogSettings { get; set; }
        
        /// <summary>
        /// Gets or sets the run schemes settings for the configuration
        /// </summary>
        [WtsAttributes.WtsProperty(
            isVisible = true,
            Title = "Timers",
            Description = "",
            KendoTab = KendoTab.Timers,
            KendoComponent = KendoComponent.Grid,
            KendoOptions = @"
               {
                  ""selectable"": true,
                  ""editable"": {
                      ""mode"": ""popup""
                  },
                  ""deletable"": true,
                  ""toolbar"": [
                    ""create"",
                    ""edit"",
                    ""delete""
                  ]
               }
            "
            // KendoOptions = @"
            //    {
            //       ""selectable"": true,
            //       ""editable"": {
            //           ""mode"": ""popup""
            //       },
            //       ""deletable"": true,
            //       ""columns"": [
            //         { ""command"": [""edit""], ""title"": ""Edit"", ""width"": ""100px"" },
            //         { ""command"": [""destroy""], ""title"": ""Delete"", ""width"": ""100px"" }
            //       ]
            //    }
            // "
            // KendoOptions = "selectable: true, toolbar: ['create', 'save', 'cancel']"
        )]
        public List<RunScheme> RunSchemes { get; set; }
        
        /// <summary>
        /// Gets or sets the queries in the configuration.
        /// </summary>
        [XmlElement("Query")]
        [WtsAttributes.WtsProperty(
            isVisible = false,
            KendoTab = KendoTab.Actions
        )]
        public List<QueryModel> Queries { get; set; }
        
        /// <summary>
        /// Gets or sets the http api's in the configuration.
        /// </summary>
        [XmlElement("HttpApi")]
        [WtsAttributes.WtsProperty(
            isVisible = false,
            KendoTab = KendoTab.Actions
        )]
        public List<HttpApiModel> HttpApis { get; set; }
        
        /// <summary>
        /// All levels of minimum logging
        /// </summary>
        [XmlIgnore]
        [WtsAttributes.WtsProperty(
            isVisible = false
        )]
        public string[] LogMinimumLevels { get; set; }
        
        /// <summary>
        /// All run scheme types
        /// </summary>
        [XmlIgnore]
        [WtsAttributes.WtsProperty(
            isVisible = false
        )]
        public string[] RunSchemeTypes { get; set; }
    }
}
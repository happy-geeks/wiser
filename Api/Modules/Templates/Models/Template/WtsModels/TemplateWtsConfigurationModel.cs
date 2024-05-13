using System.Collections.Generic;
using System.Xml.Serialization;
using Api.Modules.Templates.Attributes;
using Api.Modules.Templates.Enums;

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
        [XmlElement("ServiceName"), WtsProperty(
            IsVisible = true,
            IsRequired = true,
            Title = "Naam",
            Description = "De naam van de service",
            ConfigurationTab = ConfigurationTab.Service,
            KendoComponent = KendoComponents.TextBox
        )]
        public string ServiceName { get; set; }

        /// <summary>
        /// Gets or sets the connection string of the editor value of the template.
        /// </summary>
        [XmlElement("ConnectionString"), WtsProperty(
             IsVisible = true,
             IsRequired = true,
             Title = "Connectiestring",
             Description = "De connection string van de database",
             ConfigurationTab = ConfigurationTab.Service,
             KendoComponent = KendoComponents.TextBox
        )]
        public string ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the log settings for the configuration (Global if not overwritten)
        /// </summary>
        [WtsProperty(
            IsVisible = false,
            ConfigurationTab = ConfigurationTab.Service
        )]
        public LogSettings LogSettings { get; set; }

        /// <summary>
        /// Gets or sets the run schemes settings for the configuration
        /// </summary>
        [WtsProperty(
            IsVisible = true,
            Title = "Timers",
            Description = "",
            ConfigurationTab = ConfigurationTab.Timers,
            KendoComponent = KendoComponents.Grid,
            AllowEdit = true,
            IdProperty = "TimeId",
            UseDataSource = true,
            KendoOptions = @"
               {
                  ""resizable"": true,
                  ""height"": 280,
                  ""selectable"": true,
                  ""columns"": [
                    {
                        ""field"": ""timeId"",
                        ""title"": ""ID""
                    },
                    {
                        ""field"": ""type"",
                        ""title"": ""Type""
                    }
                  ]
               }
            "
        )]
        public List<RunScheme> RunSchemes { get; set; }

        /// <summary>
        /// Gets or sets the queries in the configuration.
        /// </summary>
        [XmlElement("Query")]
        [WtsProperty(
            IsVisible = false,
            ConfigurationTab = ConfigurationTab.Actions
        )]
        public List<QueryModel> Queries { get; set; }

        /// <summary>
        /// Gets or sets the http api's in the configuration.
        /// </summary>
        [XmlElement("HttpApi")]
        [WtsProperty(
            IsVisible = false,
            ConfigurationTab = ConfigurationTab.Actions
        )]
        public List<HttpApiModel> HttpApis { get; set; }
    }
}
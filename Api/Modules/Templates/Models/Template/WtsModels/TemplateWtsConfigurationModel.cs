using System.Collections.Generic;
using System.Xml.Serialization;
using Api.Modules.Templates.Attributes;
using Api.Modules.Templates.Enums;

namespace Api.Modules.Templates.Models.Template.WtsModels;

/// <summary>
/// A model for parsed XML of a template.
/// </summary>
[XmlRoot("Configuration", Namespace = "")]
public class TemplateWtsConfigurationModel
{
    /// <summary>
    /// Gets or sets the service name for the editor value of the template.
    /// </summary>
    [XmlElement("ServiceName"), WtsProperty(
         IsVisible = true,
         IsRequired = true,
         Title = "Naam",
         Description = "De naam van de service",
         ConfigurationTab = ConfigurationTab.Service,
         DataComponent = DataComponents.KendoTextBox
     )]
    public string ServiceName { get; set; }

    /// <summary>
    /// Gets or sets the connection string for the editor value of the template.
    /// </summary>
    [XmlElement("ConnectionString"), WtsProperty(
         IsVisible = true,
         IsRequired = true,
         Title = "Connectiestring",
         Description = "De connectiestring van de database",
         ConfigurationTab = ConfigurationTab.Service,
         DataComponent = DataComponents.KendoTextBox
     )]
    public string ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the service description for the editor value of the template.
    /// </summary>
    [WtsProperty(
        IsVisible = true,
        IsRequired = false,
        Title = "Notificatie e-mails",
        Description = "Stuurt een e-mail als de service faalt. Meerdere e-mailadressen kunnen worden gescheiden met een puntkomma.",
        ConfigurationTab = ConfigurationTab.Service,
        DataComponent = DataComponents.KendoTextBox
    )]
    public string ServiceFailedNotificationEmails { get; set; }

    /// <summary>
    /// Gets or sets the log settings for the configuration (Global if not overridden).
    /// </summary>
    [WtsProperty(
        IsVisible = false,
        ConfigurationTab = ConfigurationTab.Service
    )]
    public LogSettings LogSettings { get; set; }

    /// <summary>
    /// Gets or sets the run scheme settings for the configuration.
    /// </summary>
    [WtsProperty(
        IsVisible = true,
        Title = "Timers",
        Description = "",
        ConfigurationTab = ConfigurationTab.Timers,
        DataComponent = DataComponents.KendoGrid,
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
        IsVisible = true,
        ConfigurationTab = ConfigurationTab.Queries,
        DataComponent = DataComponents.KendoGrid,
        Title = "Query",
        AllowEdit = true,
        IdProperty = "Actionid",
        UseDataSource = true,
        KendoOptions = @"
               {
                  ""resizable"": true,
                  ""height"": 280,
                  ""selectable"": true,
                  ""columns"": [
                    {
                        ""field"": ""actionId"",
                        ""title"": ""ID""
                    },
                    {
                        ""field"": ""comment"",
                        ""title"": ""Comment""
                    }
                  ]
               }
            "

    )]
    public List<WtsQueryModel> Queries { get; set; }

    private List<HttpApiModel> httpApis;

    /// <summary>
    /// Gets or sets the HTTP APIs in the configuration.
    /// </summary>
    [XmlElement("HttpApi")]
    [WtsProperty(
        IsVisible = true,
        ConfigurationTab = ConfigurationTab.HttpApis,
        DataComponent = DataComponents.KendoGrid,
        Title = "HTTP API's",
        Description = "",
        AllowEdit = true,
        IdProperty = "Actionid",
        UseDataSource = true,
        KendoOptions = @"
           {
              ""height"": 280,
                ""persistSelection"": true,
              ""sortable"": true,
              ""selectable"": true,
              ""columns"": [
                {
                    ""field"": ""actionId"",
                    ""title"": ""ID""
                },
                {
                    ""field"": ""comment"",
                    ""title"": ""Comment""
                }
              ]
           }
        "
    )]
    public List<HttpApiModel> HttpApis
    {
        get => httpApis;
        set => httpApis = value;
    }
}
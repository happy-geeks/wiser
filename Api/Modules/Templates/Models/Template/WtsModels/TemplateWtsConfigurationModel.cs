using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Serialization;
using Api.Modules.Templates.Attributes;
using Api.Modules.Templates.Enums;
using Newtonsoft.Json;

namespace Api.Modules.Templates.Models.Template.WtsModels;
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
        DataComponent = DataComponents.KendoTextBox
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
         DataComponent = DataComponents.KendoTextBox
    )]
    public string ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the log settings for the configuration (Global if not overwritten).
    /// </summary>
    [WtsProperty(
         IsVisible = true,
         IsRequired = false,
         Title = "Notificatie emails",
         Description = "Stuurt een e-mail als de service faalt. Meerdere e-mailadressen kunnen worden gescheiden met een puntkomma.",
         ConfigurationTab = ConfigurationTab.Service,
         DataComponent = DataComponents.KendoTextBox
     )]
    public string ServiceFailedNotificationEmails { get; set; }
    
    [WtsProperty(
        IsVisible = false,
        ConfigurationTab = ConfigurationTab.Service
    )]
    public LogSettings LogSettings { get; set; }

    /// <summary>
    /// Gets or sets the run schemes settings for the configuration.
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
        ConfigurationTab = ConfigurationTab.Actions,
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
                    ""field"": ""actionid"",
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

    /// <summary>
    /// Gets or sets the http api's in the configuration.
    /// </summary>
    /*
     [XmlElement("HttpApi")]
    [WtsProperty(
        IsVisible = false,
        ConfigurationTab = ConfigurationTab.Actions
    )]
    public List<HttpApiModel> HttpApis { get; set; }*/
    
    [XmlAnyElement]
    [JsonIgnore]
    public List<XElement> ChildItemsExtra { get; set; }
    
    /// <summary>
    /// Gets or sets the all items the don't have a dedicated object/class in the current version system, intended to avoid dataloss. becarefull if you choose to edit these manualy
    /// </summary>
    [XmlIgnore, WtsProperty(
        IsVisible = true,
        IsDisabled = true,
        IsRequired = true,
        Title = "extra data",
        Description = "Informatie die niet in het systeem is verwerkt. Voorkomt dataverlies. In een ideale situatie is dit veld leeg.",
        ConfigurationTab = ConfigurationTab.Service,
        DataComponent = DataComponents.KendoTextBox
    )]
    public string ChildItemsExtraString {
        get
        {
            List<string> s = new List<string>();
            if (ChildItemsExtra == null)
            {
                ChildItemsExtra=new List<XElement>();
            }
            ChildItemsExtra.ForEach(x => s.Add(x.ToString()));
            return string.Join(",", s);;
        }
        set
        {
            
            List<string> listBack = value.Split(',').ToList();
            ChildItemsExtra= new List<XElement>();
            if (listBack.Count == 0 || string.IsNullOrWhiteSpace(listBack[0]))
            {
                return;
            }

            listBack.ForEach(si=>ChildItemsExtra.Add(XElement.Parse(si)));
        }
    }
}
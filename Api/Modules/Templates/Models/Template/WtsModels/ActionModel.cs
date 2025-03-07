using System.Xml.Serialization;
using Api.Modules.Templates.Attributes;
using Api.Modules.Templates.Enums;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Models;
using JetBrains.Annotations;

namespace Api.Modules.Templates.Models.Template.WtsModels
{
    public abstract class ActionModel
    {
        [XmlElement("TimeId", DataType = "int")]
        [WtsProperty(
            IsVisible = true,
            IsRequired = false,
            Title = "TijdId",
            Description = "het tijd id wordt gebruikt om de kopellen aan de timer, zorg dat deze werde gelijk is aan het is van de timer",
            ConfigurationTab = ConfigurationTab.Actions,
            KendoComponent = KendoComponents.NumericTextBox,
            KendoOptions = @"
                   {
                      ""format"": ""#"",
                      ""decimals"": 0
                    }
                "
        )]
        public int TimeId { get; set; }
        
        [XmlElement("Order", DataType = "int")]
        [WtsProperty(
            IsVisible = true,
            IsRequired = false,
            Title = "Order",
            Description = "de volgorde waarin de acties moeten worden uitgevoerd",
            ConfigurationTab = ConfigurationTab.Actions,
            KendoComponent = KendoComponents.NumericTextBox,
            KendoOptions = @"
                   {
                      ""format"": ""#"",
                      ""decimals"": 0
                    }
                "
        )]
        public int Order { get; set; }

        [XmlIgnore]
        [WtsProperty(
            IsVisible = true,
            AllowEdit = false,
            IsRequired = false,
            Title = "action",
            Description = "a combination of the timeid and the order",
            ConfigurationTab = ConfigurationTab.Actions,
            KendoComponent = KendoComponents.TextBox
        )]
        public string Actionid => (TimeId + "-" + Order);
        
        private string _resultSetName;
        [WtsProperty(
            IsVisible = true,
            IsRequired = false,
            Title = "ResultSetName",
            Description = "indien de actie een resultaat moet data retourneren, geef hier de naam van de resultaatset op, anders laat dit leeg",
            ConfigurationTab = ConfigurationTab.Actions,
            KendoComponent = KendoComponents.TextBox
        )]
        [CanBeNull]
        public string ResultSetName{
            get => _resultSetName;
            set
            {
                if (value == "") value = null;
                _resultSetName = value;
            } 
        }
        
        private string _useResultSet;
        
        [WtsProperty(
            IsVisible = true,
            IsRequired = false,
            Title = "UseResultSet",
            Description = "als er gebruik moet worden van een eerder defineerde result set, geef hier de naam op, anders laat dit leeg.",
            ConfigurationTab = ConfigurationTab.Actions,
            KendoComponent = KendoComponents.TextBox
        )]
        
        [CanBeNull]
        public string UseResultSet
        {
            get => _useResultSet;
            set
                {
                    if (value == "") value = null;
                    _useResultSet = value;
                } 
        }
        
        [CanBeNull]
        public HashSettingsModel HashSettings { get; set; } = new()
        {
            Algorithm = HashAlgorithms.SHA256,
            Representation = HashRepresentations.Base64
        };
        
        [CanBeNull]
        public string OnlyWithStatusCode { get; set; }
        
        [CanBeNull]
        public string OnlyWithSuccessState { get; set; }
        
        [CanBeNull]
        public LogSettings LogSettings { get; set; }
    }
}
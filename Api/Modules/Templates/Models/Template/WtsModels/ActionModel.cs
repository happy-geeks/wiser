using System;
using System.Drawing;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Api.Modules.Templates.Attributes;
using Api.Modules.Templates.Enums;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Models;
using JetBrains.Annotations;
using Newtonsoft.Json;
using OpenIddict.Client;

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
            Title = "Action",
            Description = "een combinatie tussen de timeid en de order",
            ConfigurationTab = ConfigurationTab.Actions,
            KendoComponent = KendoComponents.TextBox,
            IsDisabled = true
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
        
        [XmlIgnore]
        [WtsProperty(
            IsVisible = true,
            IsRequired = false,
            Title = "hash data",
            Description = "moet data worden gehashed",
            ConfigurationTab = ConfigurationTab.Actions,
            KendoComponent = KendoComponents.CheckBox
        )]
        public bool HashData { get; set; }
        
        [XmlIgnore]
        [WtsProperty(
            IsVisible = true,
            IsRequired = true,
            Title = "Hash algorithm",
            Description = "als hash data is geselecteerd, geef hier de hash algoritme op.",
            ConfigurationTab = ConfigurationTab.Actions,
            KendoComponent = KendoComponents.DropDownList
        )]
        public HashAlgorithms HashAlgorithm { get; set; }
        
        [XmlIgnore]
        [WtsProperty(
            IsVisible = true,
            IsRequired = true,
            Title = "hash representation",
            Description = "als hash data is geselecteerd, geef hier de hash representatie op.",
            ConfigurationTab = ConfigurationTab.Actions,
            KendoComponent = KendoComponents.DropDownList
        )]
        public HashRepresentations HashRepresentation { get; set; }
        
        [CanBeNull]
        [XmlElement("HashSettings")]
        public HashSettingsModel HashSettings {
            get
            {
                Console.WriteLine("HASH? "+HashData);
                if (HashData)
                {
                    return new HashSettingsModel()
                    {
                        Algorithm = HashAlgorithm,
                        Representation = HashRepresentation,
                    };
                }
                return null;
            }
            set
            {
                if (value == null)
                {
                    HashData = false;
                    
                }
                else
                {
                    HashData = true;
                    HashAlgorithm = value.Algorithm;
                    HashRepresentation = value.Representation;
                }
            }
        }

        [XmlIgnore]
        [WtsProperty(
            IsVisible = true,
            IsRequired = true,
            Title = "Only execute when",
            Description = "only execute the result set contain specific values/states. the needed field will depend on where the result set came from, see wts wiki",
            ConfigurationTab = ConfigurationTab.Actions,
            KendoComponent = KendoComponents.DropDownList
        )]
        public OnlyWithTypes OnlyWithTypes { get; set; }
        
        #region OnlyWithStatusCode

        [XmlIgnore]
        [WtsProperty(
            IsVisible = true,
            IsRequired = false,
            Title = "status code source",
            UseDataSource = true,
            Description = "the source of the status code",
            ConfigurationTab = ConfigurationTab.Actions,
            KendoComponent = KendoComponents.TextBox,
            DependsOnField = "OnlyWithTypes",
            DependsOnValue = new [] {"OnlyWithStatusCode"},
            customUiOrder = 0
        )]
        [CanBeNull]
        public string OnlyWithStatusCode_item
        {
            get;
            set;
        }
        [XmlIgnore]
        [CanBeNull]
        [WtsProperty(
            IsVisible = true,
            IsRequired = false,
            Title = "status code",
            Description = "the status code (werkt alleen met resulaten van een http api call)",
            ConfigurationTab = ConfigurationTab.Actions,
            KendoComponent = KendoComponents.NumericTextBox,
            DependsOnField = "OnlyWithTypes",
            DependsOnValue = new [] {"OnlyWithStatusCode"},
            KendoOptions = @"
                   {
                      ""format"": ""#"",
                      ""decimals"": 0
                    }
                "
        )]
        public string OnlyWithStatusCode_code { get; set; }
        [CanBeNull]
        [JsonIgnore]
        public XmlNode OnlyWithStatusCode
        {
        get
        {   
            if (OnlyWithStatusCode_item==null||OnlyWithStatusCode_code==null)return null;
            if (OnlyWithTypes != OnlyWithTypes.OnlyWithStatusCode) return null;
            return new XmlDocument().CreateCDataSection(OnlyWithStatusCode_item +","+ OnlyWithStatusCode_code);
        }
            set
            {
                if (value==null || value.Value== null)return;
                string[] r = value.Value.Split(',');
                OnlyWithStatusCode_item = r[0];
                OnlyWithStatusCode_code = r.Length > 1 ? r[1] : "";
            }
        }
        #endregion

        #region OnlyWithSuccessState

        [XmlIgnore]
        [WtsProperty(
            IsVisible = true,
            IsRequired = false,
            Title = "sucess code source",
            UseDataSource = true,
            Description = "the source van de sucess code",
            ConfigurationTab = ConfigurationTab.Actions,
            KendoComponent = KendoComponents.TextBox,          
            DependsOnField = "OnlyWithTypes",
            DependsOnValue = new [] {"OnlyWithSuccessState"}
        )]
        [CanBeNull]
        public string OnlyWithSuccessState_item
        {
            get;
            set;
        }
        [XmlIgnore]
        [CanBeNull]
        [WtsProperty(
            IsVisible = true,
            IsRequired = false,
            Title = "sucess code",
            Description = "the sucess code (werkt alleen met resulaten van een ImportFile of GenerateFile)",
            ConfigurationTab = ConfigurationTab.Actions,
            KendoComponent = KendoComponents.TextBox,
            DependsOnField = "OnlyWithTypes",
            DependsOnValue = new [] {"OnlyWithSuccessState"}
        )]
        public string OnlyWithSuccessState_state { get; set; }
        [CanBeNull]
        [JsonIgnore]
        public XmlNode OnlyWithSuccessState
        {
            get
            {   
                if (OnlyWithSuccessState_item==null||OnlyWithSuccessState_state==null)return null;
                if (OnlyWithTypes != OnlyWithTypes.OnlyWithSuccessState) return null;
                return new XmlDocument().CreateCDataSection(OnlyWithSuccessState_item +","+ OnlyWithSuccessState_state);
            }
            set
            {
                if (value==null || value.Value== null)return;
                string[] r = value.Value.Split(',');
                OnlyWithSuccessState_item = r[0];
                OnlyWithSuccessState_state = r.Length > 1 ? r[1] : "";
            }
        }

        #endregion

        #region OnlyWithValue

        [XmlIgnore]
        [WtsProperty(
            IsVisible = true,
            IsRequired = false,
            Title = "value source",
            UseDataSource = true,
            Description = "de source van de value",
            ConfigurationTab = ConfigurationTab.Actions,
            KendoComponent = KendoComponents.TextBox,
            DependsOnField = "OnlyWithTypes",
            DependsOnValue = new [] {"OnlyWithValue"}
        )]
        [CanBeNull]
        public string OnlyWithValue_Source
        {
            get;
            set;
        }
        [XmlIgnore]
        [CanBeNull]
        [WtsProperty(
            IsVisible = true,
            IsRequired = false,
            Title = "value",
            Description = "de verwachte value",
            ConfigurationTab = ConfigurationTab.Actions,
            KendoComponent = KendoComponents.TextBox,
            DependsOnField = "OnlyWithTypes",
            DependsOnValue = new [] {"OnlyWithValue"}
        )]
        public string OnlyWithValue_Value { get; set; }
        [CanBeNull]
        [JsonIgnore]
        public XmlNode OnlyWithValue
        {
            get
            {   
                if (OnlyWithValue_Source==null||OnlyWithValue_Value==null)return null;
                if (OnlyWithTypes != OnlyWithTypes.OnlyWithValue) return null;
                return new XmlDocument().CreateCDataSection(OnlyWithValue_Source +","+ OnlyWithValue_Value);
            }
            set
            {
                if (value==null || value.Value== null)return;
                string[] r = value.Value.Split(',');
                OnlyWithValue_Source = r[0];
                OnlyWithValue_Value = r.Length > 1 ? r[1] : "";
            }
        }

        #endregion
        
        
        [CanBeNull]
        public LogSettings LogSettings { get; set; }
        

        [XmlIgnore]
        private string comment { get; set; }
        [WtsProperty(
            IsVisible = true,
            IsRequired = false,
            Title = "Comment",
            Description = "gebruikt voor documentatie, geen effect op de actie zelf.",
            ConfigurationTab = ConfigurationTab.Actions,
            KendoComponent = KendoComponents.TextBox
        )]
        [CanBeNull]
        public string Comment {
            get
            {
                if (string.IsNullOrWhiteSpace(comment)) return null;
                return comment;
            }
            set
            {
                comment = value;
            } }
    }
}
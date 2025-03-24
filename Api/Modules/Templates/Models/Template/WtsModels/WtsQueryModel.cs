using System.Drawing.Printing;
using System.Runtime.InteropServices.JavaScript;
using System.Xml;
using System.Xml.Serialization;
using Api.Modules.Templates.Attributes;
using Api.Modules.Templates.Enums;
using JetBrains.Annotations;
using Newtonsoft.Json;
<<<<<<< HEAD
    
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
=======
using Twilio.Rest.Api.V2010.Account.IncomingPhoneNumber;
>>>>>>> 61cfa54a (progress towards the m.v.p.)

namespace Api.Modules.Templates.Models.Template.WtsModels;
    [XmlType("Query")]
    public class WtsQueryModel : ActionModel
    {
        [XmlIgnore]
        [WtsProperty(
            IsVisible = true,
            IsRequired = true,
            Title = "Query",
            Description = "de query die moet worden uitgevoerd",
            UseDataSource = true,
            ConfigurationTab = ConfigurationTab.Actions,
            KendoComponent = KendoComponents.TextArea
        )]
        public string Query
        {
            get
            {
                if (CDataContent == null) return "";
                return CDataContent.Value;
            }
            set
            {
                CDataContent = new XmlDocument().CreateCDataSection(value);
            }
        }

        private XmlCDataSection cDataContent;
        
        [XmlElement("Query")]
        [JsonIgnore]
        public XmlCDataSection CDataContent
        {
            get
            {
                /*if (cDataContent != null && cDataContent.Attributes == null)
                {
                    cDataContent.Attributes = new XmlAttributeCollection();
                }
                
                if (cDataContent != null && cDataContent.Attributes.GetNamedItem("genre") == null)
                {
                    XmlDocument doc = new XmlDocument();
                    XmlAttribute newAttr = doc.CreateAttribute("genre");
                    newAttr.Value = "novel";

                    cDataContent.Attributes.SetNamedItem(newAttr);
                }*/
                return cDataContent;
            }
            set
            {
                cDataContent=value;
            }
        }
        [XmlIgnore]
        [CanBeNull]
        private string timeout;
        
        [WtsProperty(
            IsVisible = true,
            IsRequired = false,
            Title = "Timeout",
            Description = "how many seconds until timeout? 0 = no timeout",
            ConfigurationTab = ConfigurationTab.Actions,
            KendoComponent = KendoComponents.NumericTextBox,
            KendoOptions = @"
                       {
                          ""format"": ""#"",
                          ""decimals"": 0
                        }
                    "
        )]
        [CanBeNull]
        public string Timeout {
            get
            {
                if( timeout=="0" ) return null;
                return timeout;
            }
            set
            {
                timeout = value;
            }
        }

        private CharacterEncodingModel characterEncoding;

        public CharacterEncodingModel CharacterEncoding
        {
            get
            {
                if ( characterEncoding==null ||(string.IsNullOrWhiteSpace(characterEncoding.CharacterSet) && string.IsNullOrWhiteSpace(characterEncoding.Collation))) return null;
                
                return characterEncoding;
            }
            set
            {
                if (value == null)
                {
                    characterEncoding = new CharacterEncodingModel();
                    return;
                }
                characterEncoding = value;
            }
        }

        [XmlIgnore]     
        private bool? useTransaction { get; set; }

        [WtsProperty(
            IsVisible = true,
            IsRequired = false,
            Title = "Use Transaction",
            Description = "moeten transacties gebruikt worden in deze query?",
            ConfigurationTab = ConfigurationTab.Actions,
            KendoComponent = KendoComponents.CheckBox
        )]
        [XmlIgnore] 
        public bool? UseTransaction {
            get
            {
                if (useTransaction == null||useTransaction.Value==false)
                {
                    return null;
                }
                return useTransaction;
            }
            set
            {
                useTransaction = value;
            } }
        [XmlElement("UseTransaction")]
        [CanBeNull]
        public string UseTransactionString {
            get
            {
                if (useTransaction == null||useTransaction.Value==false)
                {
                    return null;
                }
                return useTransaction.ToString();
            }
            set
            {
                bool.TryParse(value, out bool valid);
                if (!valid)
                {
                    useTransaction = false;
                    return;
                }
                useTransaction = bool.Parse(value);
            } }
        
        //when would this get used?
        /*[XmlIgnore]
        public bool UseTransactionSpecified
        {
            get { return UseTransaction.HasValue; }
        }*/
    }

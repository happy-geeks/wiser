using System.Drawing.Printing;
using System.Runtime.InteropServices.JavaScript;
using System.Xml;
using System.Xml.Serialization;
using Api.Modules.Templates.Attributes;
using Api.Modules.Templates.Enums;
using JetBrains.Annotations;
using Newtonsoft.Json;
  
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Api.Modules.Templates.Models.Template.WtsModels;
    [XmlType("Query")]
    public class WtsQueryModel : ActionModel
    {
        /// <summary>
        /// Gets or sets the query that will be used
        /// </summary>
        
        [XmlIgnore]
        [WtsProperty(
            IsVisible = true,
            IsRequired = true,
            Title = "Query",
            Description = "de query die moet worden uitgevoerd",
            UseDataSource = true,
            ConfigurationTab = ConfigurationTab.Actions,
            DataComponent = DataComponents.TextArea
        )]
        public string Query
        {
            get
            {
                if (CDataContent == null) return "";
                return CDataContent.Value;
            }
            set => CDataContent = new XmlDocument().CreateCDataSection(value);
        }

        /// <summary>
        /// converts the query to a cdata section for loading/storing in the xml file
        /// </summary>
        [XmlElement("Query")]
        [JsonIgnore]
        public XmlCDataSection CDataContent { get; set; }
        
        [XmlIgnore]
        [CanBeNull]
        private string timeout;
        
        /// <summary>
        /// Gets or sets the timeout in seconds. if the interger is 0 it will be null instead)
        /// </summary>
        [WtsProperty(
            IsVisible = true,
            IsRequired = false,
            Title = "Timeout",
            Description = "how many seconds until timeout? 0 = no timeout",
            ConfigurationTab = ConfigurationTab.Actions,
            DataComponent = DataComponents.KendoNumericTextBox,
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
            set => timeout = value;
        }

        private CharacterEncodingModel characterEncoding;
        /// <summary>
        /// Gets or sets the character encoding settings. if the data is incompleet it will be null instead
        /// </summary>
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
            DataComponent = DataComponents.KendoCheckBox
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
            set => useTransaction = value;
        }
        /// <summary>
        /// Gets or sets is tranactions will be used
        /// </summary>
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
    }

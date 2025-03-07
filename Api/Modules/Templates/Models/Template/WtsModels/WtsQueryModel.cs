using System.Xml;
using System.Xml.Serialization;
using Api.Modules.Templates.Attributes;
using Api.Modules.Templates.Enums;
using Newtonsoft.Json;
    
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Api.Modules.Templates.Models.Template.WtsModels;
    [XmlType("Query")]
    public class WtsQueryModel : ActionModel
    {
        [XmlIgnore]
        [WtsProperty(
            IsVisible = true,
            IsRequired = true,
            Title = "test",
            Description = "Het type van de timer",
            UseDataSource = true,
            ConfigurationTab = ConfigurationTab.Actions,
            KendoComponent = KendoComponents.TextBox
        )]
        public string Query
        {
            get
            {
                return CDataContent.Value;
            }
            set
            {
                CDataContent = new XmlDocument().CreateCDataSection(value);
            }
        }
        [XmlElement("Query")]
        [JsonIgnore]
        public XmlCDataSection CDataContent
        {
            get;
            set;
        }
        
        public int? Timeout { get; set; }

        public CharacterEncodingModel CharacterEncoding { get; set; }

        public bool? UseTransaction { get; set; }
        [XmlIgnore]
        public bool UseTransactionSpecified
        {
            get { return UseTransaction.HasValue; }
        }
    }

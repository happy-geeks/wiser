using System.Xml.Serialization;
using Api.Modules.Templates.Attributes;
using Api.Modules.Templates.Enums;

namespace Api.Modules.Templates.Models.Template.WtsModels;

    public class CharacterEncodingModel
    {   
        [XmlIgnore]
        private readonly string defaultCharacterSet = "utf8mb4";
        [XmlIgnore]
        private readonly string defaultCollation = "utf8mb4_general_ci";
        
        [XmlIgnore]
        private string characterSet;
        
        [WtsProperty(
            IsVisible = true,
            IsRequired = false,
            Title = "CharacterSet",
            Description = "welke characterset moet gebruikt worden?",
            ConfigurationTab = ConfigurationTab.Actions,
            KendoComponent = KendoComponents.TextBox
        )]
        [XmlIgnore]
        public string CharacterSet
        {
            get;
            set;
        }
        [XmlElement("CharacterSet")]
        public string CharacterSetXml
        {
            get
            {
                if (string.IsNullOrWhiteSpace(CharacterSet)||CharacterSet==defaultCharacterSet)
                {
                    return null;
                }
                return CharacterSet;
            }
            set
            {
                if (value == null)
                {
                    CharacterSet = defaultCharacterSet;
                }

                CharacterSet = value;
            }
        }
        [XmlIgnore]
        private string collation;
        
        [XmlIgnore]    
        [WtsProperty(
            IsVisible = true,
            IsRequired = false,
            Title = "Collation",
            Description = "welke Collation moet gebruikt worden?",
            ConfigurationTab = ConfigurationTab.Actions,
            KendoComponent = KendoComponents.TextBox
        )]
        public string Collation { get; set; }
        [XmlElement("Collation")]
        public string CollationXml
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Collation)||Collation==defaultCollation)
                {
                    return null;
                }
                return Collation;
            }
            set
            {
                if (value == null)
                {
                    Collation = defaultCollation;
                }

                Collation = value;
            }
        }
    }

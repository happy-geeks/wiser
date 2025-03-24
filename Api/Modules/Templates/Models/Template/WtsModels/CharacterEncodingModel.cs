using System.Xml.Serialization;
using Api.Modules.Templates.Attributes;
using Api.Modules.Templates.Enums;

namespace Api.Modules.Templates.Models.Template.WtsModels;

    public class CharacterEncodingModel
    {   
        //if either of the 2 setting are equal to the default return null to reduce file size
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
            DataComponent = DataComponents.KendoTextBox
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
                if (string.IsNullOrWhiteSpace(CharacterSet))
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
            DataComponent = DataComponents.KendoTextBox
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
                if (string.IsNullOrWhiteSpace(Collation))
                {
                    Collation = defaultCollation;
                }

                Collation = value;
            }
        }
    }
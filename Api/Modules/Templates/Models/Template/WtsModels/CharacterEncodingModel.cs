using System;
using System.Xml.Serialization;
using Api.Modules.Templates.Attributes;
using Api.Modules.Templates.Enums;
using Newtonsoft.Json;

namespace Api.Modules.Templates.Models.Template.WtsModels;

/// <summary>
/// A model representing character encoding settings for a WTS configuration template.
/// </summary>
public class CharacterEncodingModel
{
    //If either of the two settings is equal to the default, return null to reduce file size.
    private const string DefaultCharacterSet = "utf8mb4";
    private const string DefaultCollation = "utf8mb4_general_ci";

    /// <summary>
    /// The character set to be used in an SQL query.
    /// </summary>
    [WtsProperty(
        IsVisible = true,
        IsRequired = false,
        Title = "Karakterset",
        Description = "Welke Karakterset moet gebruikt worden?",
        ConfigurationTab = ConfigurationTab.Queries,
        DataComponent = DataComponents.KendoTextBox
    )]
    [XmlIgnore]
    public string CharacterSet { get; set; }

    /// <summary>
    /// The character set to be used in an SQL query, serialized for XML.
    /// </summary>
    [JsonIgnore]
    [XmlElement("CharacterSet")]
    public string CharacterSetXml
    {
        get
        {
            if (String.IsNullOrWhiteSpace(CharacterSet) || CharacterSet == DefaultCharacterSet)
            {
                return null;
            }

            return CharacterSet;
        }
        set
        {
            if (String.IsNullOrWhiteSpace(CharacterSet))
            {
                CharacterSet = DefaultCharacterSet;
            }

            CharacterSet = value;
        }
    }

    /// <summary>
    /// The collation to be used in an SQL query.
    /// </summary>
    [XmlIgnore]
    [WtsProperty(
        IsVisible = true,
        IsRequired = false,
        Title = "Collation",
        Description = "Welke Collation moet gebruikt worden?",
        ConfigurationTab = ConfigurationTab.Queries,
        DataComponent = DataComponents.KendoTextBox
    )]
    public string Collation { get; set; }

    /// <summary>
    /// The collation to be used in an SQL query, serialized for XML.
    /// </summary>
    [XmlElement("Collation")]
    [JsonIgnore]
    public string CollationXml
    {
        get
        {
            if (String.IsNullOrWhiteSpace(Collation) || Collation == DefaultCollation)
            {
                return null;
            }

            return Collation;
        }
        set
        {
            if (String.IsNullOrWhiteSpace(Collation))
            {
                Collation = DefaultCollation;
            }

            Collation = value;
        }
    }
}
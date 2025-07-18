using System;
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
    /// Gets or sets the query that will be used.
    /// </summary>
    [XmlIgnore]
    [WtsProperty(
        IsVisible = true,
        IsRequired = true,
        Title = "Query",
        Description = "De query die moet worden uitgevoerd.",
        UseDataSource = true,
        ConfigurationTab = ConfigurationTab.Queries,
        DataComponent = DataComponents.TextArea
    )]
    public string Query
    {
        get
        {
            if (CDataContent == null)
            {
                return "";
            }

            return CDataContent.Value;
        }
        set => CDataContent = new XmlDocument().CreateCDataSection(value);
    }

    /// <summary>
    /// Converts the query to a CDATA section for loading/storing in the XML file.
    /// </summary>
    [XmlElement("Query")]
    [JsonIgnore]
    public XmlCDataSection CDataContent { get; set; }

    [XmlIgnore]
    [CanBeNull]
    private int? timeout;

    /// <summary>
    /// Gets or sets the timeout in seconds. If the integer is 0 it will be null instead.
    /// </summary>
    [WtsProperty(
        IsVisible = true,
        IsRequired = false,
        Title = "Time out",
        Description = "Hoeveel seconden tot de time-out? 0 betekent geen time out.",
        ConfigurationTab = ConfigurationTab.Queries,
        DataComponent = DataComponents.KendoNumericTextBox,
        KendoOptions = @"
                   {
                      ""format"": ""#"",
                      ""decimals"": 0
                    }
                "
    )]
    [CanBeNull]
    public int? Timeout {
        get
        {
            if(timeout == 0)
            {
                return null;
            }

            return timeout;
        }
        set => timeout = value;
    }

    private CharacterEncodingModel characterEncoding;

    /// <summary>
    /// Gets or sets the character encoding settings. If the data is incomplete it will return null instead.
    /// </summary>
    public CharacterEncodingModel CharacterEncoding
    {
        get
        {
            if ( characterEncoding==null ||(String.IsNullOrWhiteSpace(characterEncoding.CharacterSet) && String.IsNullOrWhiteSpace(characterEncoding.Collation)))
            {
                return null;
            }

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
        Title = "Gebruik transacties",
        Description = "Selecteer of transacties gebruikt moeten worden in deze query?",
        ConfigurationTab = ConfigurationTab.Queries,
        DataComponent = DataComponents.KendoCheckBox
    )]
    [XmlIgnore]
    public bool? UseTransaction
    {
        get
        {
            if (useTransaction == null || useTransaction.Value == false)
            {
                return null;
            }
            return useTransaction;
        }
        set => useTransaction = value;
    }

    /// <summary>
    /// Gets or sets whether tranactions will be used.
    /// </summary>
    [XmlElement("UseTransaction")]
    [CanBeNull]
    [JsonIgnore]
    public string UseTransactionString
    {
        get
        {
            if (useTransaction == null || useTransaction.Value==false)
            {
                return null;
            }
            return useTransaction.ToString();
        }
        set
        {
            Boolean.TryParse(value, out var valid);
            if (!valid)
            {
                useTransaction = false;
                return;
            }
            useTransaction = Boolean.Parse(value);
        }
    }
}
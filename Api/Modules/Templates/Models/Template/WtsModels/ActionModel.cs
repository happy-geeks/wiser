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

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member.

namespace Api.Modules.Templates.Models.Template.WtsModels;

public abstract class ActionModel
{
    /// <summary>
    /// Gets or sets the time ID, which is used to determine which timer should be connected to this action.
    /// </summary>
    [XmlElement("TimeId", DataType = "int")]
    [WtsProperty(
        IsVisible = true,
        IsRequired = false,
        Title = "Tijd ID",
        Description = "Het tijd ID wordt gebruikt om de koppeling met de timer te maken. Zorg ervoor dat dit ID gelijk is aan dat van de timer.",
        ConfigurationTab = ConfigurationTab.Actions,
        DataComponent = DataComponents.KendoNumericTextBox,
        KendoOptions = @"
               {
                  ""format"": ""#"",
                  ""decimals"": 0
                }
            "
    )]
    public int TimeId { get; set; }
    
    /// <summary>
    /// Gets or sets the order ID. This determines the sequence in which different actions within the same timer are executed.
    /// </summary>
    [XmlElement("Order", DataType = "int")]
    [WtsProperty(
        IsVisible = true,
        IsRequired = false,
        Title = "Volgorde",
        Description = "Geeft de volgorde aan waarin de acties worden uitgevoerd.",
        ConfigurationTab = ConfigurationTab.Actions,
        DataComponent = DataComponents.KendoNumericTextBox,
        KendoOptions = @"
               {
                  ""format"": ""#"",
                  ""decimals"": 0
                }
            "
    )]
    public int Order { get; set; }
    
    /// <summary>
    /// The action id is a combination between the time ID and order. This value has to be unique.
    /// </summary>
    [XmlIgnore]
    [WtsProperty(
        IsVisible = true,
        AllowEdit = false,
        IsRequired = false,
        Title = "Actie ID",
        Description = "Een combinatie van het tijd ID en de order.",
        ConfigurationTab = ConfigurationTab.Actions,
        DataComponent = DataComponents.KendoTextBox,
        IsDisabled = true
    )]
    public string Actionid => ($"{TimeId}-{Order}");
    
    private string resultSetName;
    
    /// <summary>
    /// Gets or sets the name that will be used to store the result set. An empty string represents null.
    /// </summary>
    [WtsProperty(
        IsVisible = true,
        IsRequired = false,
        Title = "Resultaatsetnaam",
        Description = "Als de actie data moet retourneren als resultaat, geef hier de naam van de resultaatset op, laat dit anders leeg.",
        ConfigurationTab = ConfigurationTab.Actions,
        DataComponent = DataComponents.KendoTextBox
    )]
    [CanBeNull]
    public string ResultSetName{
        get => resultSetName;
        set
        {
            if (value == "") value = null;
            resultSetName = value;
        } 
    }
    
    private string useResultSet;
    
    /// <summary>
    /// Gets or sets the name that will be used to retrieve the result set. An empty string represents null.
    /// </summary>
    [WtsProperty(
        IsVisible = true,
        IsRequired = false,
        Title = "Gebruik resultaatset",
        Description = "Als er gebruik moet worden gemaakt van een eerder gedefinieerde resultaatset, geef hier de naam op; laat dit anders leeg.",
        ConfigurationTab = ConfigurationTab.Actions,
        DataComponent = DataComponents.KendoTextBox
    )]
    [CanBeNull]
    public string UseResultSet
    {
        get => useResultSet;
        set
            {
                if (value == "") value = null;
                useResultSet = value;
            } 
    }
    
    /// <summary>
    /// Gets or sets if this data should be hashed.
    /// </summary>
    [XmlIgnore]
    [WtsProperty(
        IsVisible = true,
        IsRequired = false,
        Title = "Hash data",
        Description = "Vink het vakje aan om de data te hashen.",
        ConfigurationTab = ConfigurationTab.Actions,
        DataComponent = DataComponents.KendoCheckBox
    )]
    
    public bool HashData { get; set; }
    
    /// <summary>
    /// Gets or sets the hashing algorithm used. If HashData is false, this is set to null.
    /// </summary>
    [XmlIgnore]
    [WtsProperty(
        IsVisible = true,
        IsRequired = true,
        Title = "Hashalgoritme",
        Description = "Als Hash data is geselecteerd, geef hier het hashalgoritme op.",
        ConfigurationTab = ConfigurationTab.Actions,
        DataComponent = DataComponents.KendoDropDownList
    )]
    public HashAlgorithms HashAlgorithm { get; set; }
    
    /// <summary>
    /// Gets or sets the hash representation used. If HashData is false, this is set to null.
    /// </summary>
    [XmlIgnore]
    [WtsProperty(
        IsVisible = true,
        IsRequired = true,
        Title = "Hashrepresentatie",
        Description = "Als Hash data is geselecteerd, geef hier de hashrepresentatie op.",
        ConfigurationTab = ConfigurationTab.Actions,
        DataComponent = DataComponents.KendoDropDownList
    )]
    public HashRepresentations HashRepresentation { get; set; }
    
    /// <summary>
    ///Gets or sets the hash setting used for XML file generation/loading. If HashData is false, this is set to null.
    /// </summary>
    [CanBeNull]
    [XmlElement("HashSettings")]
    public HashSettingsModel HashSettings
    {
        get
        {
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
        Title = "Alleen uitvoeren als",
        Description = "Voer alleen de resultaatset uit die specifieke waarden/toestanden bevat. Het benodigde veld is afhankelijk van de oorsprong van de resultaatset, zie de WTS wiki voor meet informatie.",
        ConfigurationTab = ConfigurationTab.Actions,
        DataComponent = DataComponents.KendoDropDownList
    )]
    
    public OnlyWithTypes OnlyWithTypes { get; set; }
    
    #region OnlyWithStatusCode

    [XmlIgnore]
    [WtsProperty(
        IsVisible = true,
        IsRequired = false,
        Title = "Statuscode bron",
        UseDataSource = true,
        Description = "De bron van de statuscode.",
        ConfigurationTab = ConfigurationTab.Actions,
        DataComponent = DataComponents.KendoTextBox,
        DependsOnField = "OnlyWithTypes",
        DependsOnValue = new [] {"OnlyWithStatusCode"}
    )]
    [CanBeNull]
    public string OnlyWithStatusCode_item { get; set; }
    
    [XmlIgnore]
    [CanBeNull]
    [WtsProperty(
        IsVisible = true,
        IsRequired = false,
        Title = "Statuscode",
        Description = "De statuscode. (Werkt alleen met resultaten van een HTTP API call.)",
        ConfigurationTab = ConfigurationTab.Actions,
        DataComponent = DataComponents.KendoNumericTextBox,
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
            return new XmlDocument().CreateCDataSection($"{OnlyWithStatusCode_item},{OnlyWithStatusCode_code}");
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
        Title = "Succescode bron",
        UseDataSource = true,
        Description = "De bron van de succescode.",
        ConfigurationTab = ConfigurationTab.Actions,
        DataComponent = DataComponents.KendoTextBox,          
        DependsOnField = "OnlyWithTypes",
        DependsOnValue = new [] {"OnlyWithSuccessState"}
    )]
    [CanBeNull]
    public string OnlyWithSuccessState_item { get; set; }
    
    [XmlIgnore]
    [CanBeNull]
    [WtsProperty(
        IsVisible = true,
        IsRequired = false,
        Title = "Succescode",
        Description = "De succescode. (Werkt alleen met resultaten van een ImportFile of GenerateFile.)",
        ConfigurationTab = ConfigurationTab.Actions,
        DataComponent = DataComponents.KendoTextBox,
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
            return new XmlDocument().CreateCDataSection($"{OnlyWithSuccessState_item},{OnlyWithSuccessState_state}");
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
        Title = "Bron waarde",
        UseDataSource = true,
        Description = "De bron van de waarde.",
        ConfigurationTab = ConfigurationTab.Actions,
        DataComponent = DataComponents.KendoTextBox,
        DependsOnField = "OnlyWithTypes",
        DependsOnValue = new [] {"OnlyWithValue"}
    )]
    [CanBeNull]
    public string OnlyWithValue_Source { get; set; }
    
    [XmlIgnore]
    [CanBeNull]
    [WtsProperty(
        IsVisible = true,
        IsRequired = false,
        Title = "Waarde",
        Description = "De verwachte waarde.",
        ConfigurationTab = ConfigurationTab.Actions,
        DataComponent = DataComponents.KendoTextBox,
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
            return new XmlDocument().CreateCDataSection($"{OnlyWithValue_Source},{OnlyWithValue_Value}");
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
    
    /// <summary>
    /// Gets or sets the comment. Used for documentation purposes.
    /// </summary>
    [WtsProperty(
        IsVisible = true,
        IsRequired = false,
        Title = "Commentaar",
        Description = "Wordt gebruikt voor documentatie, heeft geen effect op de actie zelf.",
        ConfigurationTab = ConfigurationTab.Actions,
        DataComponent = DataComponents.KendoTextBox
    )]
    [CanBeNull]
    public string Comment 
    {
        get
        {
            if (string.IsNullOrWhiteSpace(comment)) return null;
            return comment;
        }
        set => comment = value;
    }
}

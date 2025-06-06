using System;
using System.Xml.Serialization;
using Api.Modules.Templates.Attributes;
using Api.Modules.Templates.Enums;
using JetBrains.Annotations;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Api.Modules.Templates.Models.Template.WtsModels;

public class BodyPartModel
{
    [WtsProperty(
        IsVisible = true,
        IsRequired = true,
        Title = "Body Part Tekst",
        UseDataSource = true,
        Description = "Tekst die wordt toegevoegd aan de body – bodypart.",
        DataComponent = DataComponents.KendoTextBox
    )]
    public string Text { get; set; }
    
    [WtsProperty(
        IsVisible = true,
        IsRequired = false,
        Title = "Gebruik resultaatset bodypart.",
        Description = "Als er gebruik moet worden gemaakt van een eerder gedefinieerde resultaatset, geef hier de naam op; laat dit anders leeg.",
        DataComponent = DataComponents.KendoTextBox
    )]
    public string UseResultSet { get; set; }
    [XmlIgnore]
    private bool? forceIndex { get; set; }
    
    [XmlIgnore]
    [WtsProperty(
        IsVisible = true,
        IsRequired = false,
        Title = "Force Index",
        Description = "Force Index",
        DataComponent = DataComponents.KendoCheckBox
    )]
    public bool? ForceIndex
    {
        get
        {
            if (forceIndex == null || forceIndex.Value == false)
            {
                return null;
            }
            return forceIndex;
        }
        set => forceIndex = value;
    }
    
    [XmlElement("ForceIndex")]
    [CanBeNull]
    public string ForceIndexString 
    {
        get
        {
            if (forceIndex == null || forceIndex.Value==false)
            {
                return null;
            }
            return forceIndex.ToString();
        }
        set
        {
            bool.TryParse(value, out bool valid);
            if (!valid)
            {
                forceIndex = false;
                return;
            }
            forceIndex = bool.Parse(value);
        }
    }
    [XmlIgnore]
    private bool? singleItem { get; set; }

    [XmlIgnore]
    [WtsProperty(
        IsVisible = true,
        IsRequired = false,
        Title = "een item",
        Description = "een item",
        DataComponent = DataComponents.KendoCheckBox
    )]
    public bool? SingleItem
    {
        get
        {
            if (singleItem == null || singleItem.Value == false)
            {
                return null;
            }
            return singleItem;
        }
        set => singleItem = value;
    }
    
    [XmlElement("SingleItem")]
    [CanBeNull]
    public string SingleItemString 
    {
        get
        {
            if (singleItem == null || singleItem.Value==false)
            {
                return null;
            }
            return singleItem.ToString();
        }
        set
        {
            bool.TryParse(value, out bool valid);
            if (!valid)
            {
                singleItem = false;
                return;
            }
            singleItem = bool.Parse(value);
        }
    }
    [XmlIgnore]
    private bool? evaluateLogicSnippets { get; set; }
    
    [XmlIgnore]
    [WtsProperty(
        IsVisible = true,
        IsRequired = false,
        Title = "Evalueer logica snippets.",
        Description = "Evalueer logica snippets.",
        DataComponent = DataComponents.KendoCheckBox
    )]
    public bool? EvaluateLogicSnippets
    {
        get
        {
            if (evaluateLogicSnippets == null || evaluateLogicSnippets.Value == false)
            {
                return null;
            }
            return evaluateLogicSnippets;
        }
        set => evaluateLogicSnippets = value;
    }
    [XmlElement("EvaluateLogicSnippets")]
    [CanBeNull]
    public string EvaluateLogicSnippetsString 
    {
        get
        {
            if (evaluateLogicSnippets == null || evaluateLogicSnippets.Value==false)
            {
                return null;
            }
            return evaluateLogicSnippets.ToString();
        }
        set
        {
            bool.TryParse(value, out bool valid);
            if (!valid)
            {
                evaluateLogicSnippets = false;
                return;
            }
            evaluateLogicSnippets = bool.Parse(value);
        }
    }
    
    [XmlIgnore]
    public string uid { get; set; } = Guid.NewGuid().ToString();
}
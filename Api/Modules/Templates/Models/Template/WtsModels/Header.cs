using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Api.Modules.Templates.Attributes;
using Api.Modules.Templates.Enums;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Api.Modules.Templates.Models.Template.WtsModels;

[XmlType("Header")]
public class Header
{
    [WtsProperty(
        IsVisible = true,
        IsRequired = true,
        Title = "Header naam",
        UseDataSource = true,
        Description = "De naam van de header.",
        DataComponent = DataComponents.KendoTextBox
    )]
    [DefaultValue("no name")]
    public string Name { get; set; }

    [WtsProperty(
        IsVisible = true,
        IsRequired = true,
        Title = "Header waarde",
        UseDataSource = true,
        Description = "De waarde van de header.",
        DataComponent = DataComponents.KendoTextBox
    )]
    public string Value { get; set; }

    [WtsProperty(
        IsVisible = true,
        IsRequired = false,
        Title = "Gebruik resultaatset header",
        Description = "Als er gebruik moet worden gemaakt van een eerder gedefinieerde resultaatset, geef hier de naam op; laat dit anders leeg.",
        DataComponent = DataComponents.KendoTextBox
    )]
    public string UseResultSet { get; set; }
    
    [XmlIgnore]
    public string ID { get; set; } = Guid.NewGuid().ToString();
}
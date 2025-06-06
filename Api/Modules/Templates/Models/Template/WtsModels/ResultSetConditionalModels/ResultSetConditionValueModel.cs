using System.Text.Json.Serialization;
using System.Xml.Serialization;
using Api.Modules.Templates.Attributes;
using Api.Modules.Templates.Enums;
using JetBrains.Annotations;

namespace Api.Modules.Templates.Models.Template.WtsModels;

public class ResultSetConditionValueModel : ResultSetConditionModel
{
    [XmlIgnore]
    [WtsProperty(
        IsVisible = true,
        IsRequired = false,
        Title = "Bron waarde",
        UseDataSource = true,
        Description = "De bron van de waarde.",
        DataComponent = DataComponents.KendoTextBox,
        DependsOnField = "OnlyWithTypes",
        DependsOnValue = new [] {"OnlyWithValue"}
    )]
    [CanBeNull]
    public string Selector_Value
    {
        get => GetSelector();
        set => SetSelector(value);
    }

    [XmlIgnore]
    [CanBeNull]
    [WtsProperty(
        IsVisible = true,
        IsRequired = false,
        Title = "Waarde",
        Description = "De verwachte waarde.",
        DataComponent = DataComponents.KendoTextBox,
        DependsOnField = "OnlyWithTypes",
        DependsOnValue = new [] {"OnlyWithValue"}
    )]
    public string ValueForComparison_Value 
    {
        get => GetValue();
        set => SetValue(value);
    }

    public override OnlyWithTypes type { get; set; } = OnlyWithTypes.OnlyWithValue;
}
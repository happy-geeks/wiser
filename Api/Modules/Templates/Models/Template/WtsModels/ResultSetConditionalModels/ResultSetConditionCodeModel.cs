using System.Text.Json.Serialization;
using System.Xml.Serialization;
using Api.Modules.Templates.Attributes;
using Api.Modules.Templates.Enums;
using JetBrains.Annotations;

namespace Api.Modules.Templates.Models.Template.WtsModels;

public class ResultSetConditionCodeModel: ResultSetConditionModel
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
        DependsOnValue = new [] {"OnlyWithStatusCode"}
    )]
    [CanBeNull]
    public string Selector_Core
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
    public string ValueForComparison_Core     
    {
        get => GetValue();
        set => SetValue(value);
    }

    public override OnlyWithTypes type { get; set; } = OnlyWithTypes.OnlyWithStatusCode;
}
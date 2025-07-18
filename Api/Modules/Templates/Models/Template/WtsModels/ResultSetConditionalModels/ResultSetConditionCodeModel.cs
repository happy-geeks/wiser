using System.Xml.Serialization;
using Api.Modules.Templates.Attributes;
using Api.Modules.Templates.Enums;
using JetBrains.Annotations;

namespace Api.Modules.Templates.Models.Template.WtsModels.ResultSetConditionalModels;

/// <summary>
/// A model representing a condition for a result set based on a specific code.
/// </summary>
public class ResultSetConditionCodeModel : ResultSetConditionModel
{
    /// <summary>
    /// The selector for the condition, which is the source of the value to be compared.
    /// </summary>
    [XmlIgnore]
    [WtsProperty(
        IsVisible = true,
        IsRequired = false,
        Title = "Bron waarde",
        UseDataSource = true,
        Description = "De bron van de waarde.",
        DataComponent = DataComponents.KendoTextBox,
        DependsOnField = "OnlyWithTypes",
        DependsOnValue = ["OnlyWithStatusCode"]
    )]
    [CanBeNull]
    public string SelectorCore
    {
        get => GetSelector();
        set => SetSelector(value);
    }

    /// <summary>
    /// The value to be compared against the expected value.
    /// </summary>
    [XmlIgnore]
    [CanBeNull]
    [WtsProperty(
        IsVisible = true,
        IsRequired = false,
        Title = "Waarde",
        Description = "De verwachte waarde.",
        DataComponent = DataComponents.KendoNumericTextBox,
        DependsOnField = "OnlyWithTypes",
        DependsOnValue = ["OnlyWithStatusCode"],
        KendoOptions = @"
               {
                  ""format"": ""#"",
                  ""decimals"": 0
                }
            "
    )]
    public string ValueForComparisonCore
    {
        get => GetValue();
        set => SetValue(value);
    }

    /// <summary>
    /// The type of condition this model represents, which is specifically for status codes.
    /// </summary>
    public override OnlyWithTypes Type { get; set; } = OnlyWithTypes.OnlyWithStatusCode;
}
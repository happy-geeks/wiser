using System.Xml.Serialization;
using Api.Modules.Templates.Attributes;
using Api.Modules.Templates.Enums;
using JetBrains.Annotations;

namespace Api.Modules.Templates.Models.Template.WtsModels.ResultSetConditionalModels;

/// <summary>
/// A model representing a condition for a result set that checks for a successful status.
/// </summary>
public class ResultSetConditionSuccessStatusModel : ResultSetConditionModel
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
        DependsOnValue = ["OnlyWithSuccessState"]
    )]
    [CanBeNull]
    public string SelectorStatus
    {
        get => GetSelector();
        set => SetSelector(value);
    }

    /// <summary>
    /// The value to be compared against the expected value for a successful status.
    /// </summary>
    [XmlIgnore]
    [CanBeNull]
    [WtsProperty(
        IsVisible = true,
        IsRequired = false,
        Title = "Waarde",
        Description = "De verwachte waarde.",
        DataComponent = DataComponents.KendoTextBox,
        DependsOnField = "OnlyWithTypes",
        DependsOnValue = ["OnlyWithSuccessState"]
    )]
    public string ValueForComparisonStatus
    {
        get => GetValue();
        set => SetValue(value);
    }

    /// <summary>
    /// The type of condition this model represents, which is specifically for successful states.
    /// </summary>
    public override OnlyWithTypes Type { get; set; } = OnlyWithTypes.OnlyWithSuccessState;
}
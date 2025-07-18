using System.Xml.Serialization;
using Api.Modules.Templates.Attributes;
using Api.Modules.Templates.Enums;
using JetBrains.Annotations;

namespace Api.Modules.Templates.Models.Template.WtsModels.ResultSetConditionalModels;

/// <summary>
/// A model representing a condition in a result set that compares a value against a source.
/// </summary>
public class ResultSetConditionValueModel : ResultSetConditionModel
{
    /// <summary>
    /// The source of the value to compare against.
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
        DependsOnValue = ["OnlyWithValue"]
    )]
    [CanBeNull]
    public string SelectorValue
    {
        get => GetSelector();
        set => SetSelector(value);
    }

    /// <summary>
    /// The expected value to compare against the source value.
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
        DependsOnValue = ["OnlyWithValue"]
    )]
    public string ValueForComparisonValue
    {
        get => GetValue();
        set => SetValue(value);
    }

    /// <summary>
    /// The type of condition this model represents.
    /// </summary>
    public override OnlyWithTypes Type { get; set; } = OnlyWithTypes.OnlyWithValue;
}
using System;
using System.Xml;
using Api.Modules.Templates.Enums;

namespace Api.Modules.Templates.Models.Template.WtsModels.ResultSetConditionalModels;

/// <summary>
/// A base class for result set condition models.
/// </summary>
public class ResultSetConditionModel
{
    // The selector and valueForComparision are private to avoid them interfering with saving.
    // Each has a set and get function to compensate for this. Each implementation of the model has a different name for the selector and value for comparison,
    // to avoid confusing the front end by having 2 or more fields with the same name.
    private string Selector { get; set; } = "";
    private string ValueForComparison { get; set; } = "";

    /// <summary>
    /// The type of the condition.
    /// </summary>
    public virtual OnlyWithTypes Type { get; set; } = OnlyWithTypes.None;

    /// <summary>
    /// Set the selector for the condition.
    /// </summary>
    /// <param name="value">The value to set.</param>
    public void SetSelector(string value)
    {
        Selector = value;
    }

    /// <summary>
    /// Get the selector for the condition.
    /// </summary>
    /// <returns></returns>
    public string GetSelector()
    {
        return Selector;
    }

    /// <summary>
    /// Set the value for comparison for the condition.
    /// </summary>
    /// <param name="value">The value for comparison.</param>
    public void SetValue(string value)
    {
        ValueForComparison = value;
    }

    /// <summary>
    /// Get the value for comparison for the condition.
    /// </summary>
    /// <returns></returns>
    public string GetValue()
    {
        return ValueForComparison;
    }

    /// <summary>
    /// Set the properties of the model from an XML node.
    /// </summary>
    /// <param name="value">The <see cref="XmlNode"/> to get the data from.</param>
    /// <returns>The comparison type, or <c>null</c> if the value is empty.</returns>
    public OnlyWithTypes? SetModel(XmlNode value)
    {
        if (String.IsNullOrEmpty(value.Value))
        {
            return null;
        }

        var splitString = value.Value.Split(',');
        Selector = splitString[0];
        ValueForComparison = splitString.Length > 1 ? splitString[1] : "";

        return Type;
    }

    /// <summary>
    /// Create an XML node representing the model.
    /// </summary>
    /// <param name="currentType">The comparison type.</param>
    /// <returns>An <see cref="XmlNode"/> with the data from the model.</returns>
    public XmlNode GetModel(OnlyWithTypes currentType)
    {
        if (String.IsNullOrEmpty(Selector) && String.IsNullOrEmpty(ValueForComparison))
        {
            return null;
        }

        return currentType != Type ? null : new XmlDocument().CreateCDataSection($"{Selector},{ValueForComparison}");
    }

    /// <summary>
    /// Check if the model is empty.
    /// </summary>
    /// <returns>A <see cref="bool"/> to indicate whether the model is empty.</returns>
    public bool IsEmpty()
    {
        return String.IsNullOrEmpty(Selector) && String.IsNullOrEmpty(ValueForComparison);
    }
}
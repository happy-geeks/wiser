namespace Api.Modules.Templates.Enums;

/// <summary>
/// Represents the different types of data components that can be used in a WTS configuration template.
/// </summary>
public enum DataComponents
{
    /// <summary>
    /// A normal multi-line text input field.
    /// TODO: The textArea component type currently only works when its input field receives data from a KendoGrid object.
    /// </summary>
    TextArea,

    /// <summary>
    /// A multi-line text input from the Kendo UI library.
    /// </summary>
    KendoTextBox,

    /// <summary>
    /// A numeric input field from the Kendo UI library.
    /// </summary>
    KendoNumericTextBox,

    /// <summary>
    /// A dropdown list from the Kendo UI library.
    /// </summary>
    KendoDropDownList,

    /// <summary>
    /// A checkbox from the Kendo UI library.
    /// </summary>
    KendoCheckBox,

    /// <summary>
    /// A date time picker from the Kendo UI library.
    /// </summary>
    KendoTimePicker,

    /// <summary>
    /// A grid component from the Kendo UI library.
    /// </summary>
    KendoGrid
}
namespace Api.Modules.Templates.Enums;

public enum DataComponents
{
    //Todo: Currently the text area component will only work if it's data is connected to a grid.
    //This is likely because of text areas storing the value of a string differently then a normal input.
    TextArea,
    KendoTextBox,
    KendoNumericTextBox,
    KendoDropDownList,
    KendoCheckBox,
    KendoTimePicker,
    KendoGrid
}
 
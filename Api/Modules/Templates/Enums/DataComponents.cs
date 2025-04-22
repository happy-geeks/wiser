namespace Api.Modules.Templates.Enums
{
    public enum DataComponents
    {
        //Todo: Currently the text area component will only work if it's data is connected to a grid. This is likly because of text areas storing the value of a string diffrently then a normal input. But i could be wrong
        TextArea,
        KendoTextBox,
        KendoNumericTextBox,
        KendoDropDownList,
        KendoCheckBox,
        KendoTimePicker,
        KendoGrid
    }
}
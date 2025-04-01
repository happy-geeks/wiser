namespace Api.Modules.Templates.Enums
{
    public enum DataComponents
    {
        //Todo: currently the text area component will only work if it's data is connected to a grid. this is likly because of text areas storing the value of a string diffrently then a normal input. but i could be wrong
        TextArea,
        KendoTextBox,
        KendoNumericTextBox,
        KendoDropDownList,
        KendoCheckBox,
        KendoTimePicker,
        KendoGrid
    }
}
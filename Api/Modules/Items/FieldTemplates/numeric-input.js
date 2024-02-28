(() => {
    const options = $.extend({
        culture: "nl-NL"
    }, {options});

    const field = $("#field_{propertyIdWithSuffix}");
    
    const kendoComponent = field.kendoNumericTextBox($.extend({change: window.dynamicItems.fields.onFieldValueChange.bind(window.dynamicItems.fields)}, {options})).data("kendoNumericTextBox");

    if (options.saveOnEnter) {
        field.keypress((event) => {
            window.dynamicItems.fields.onInputFieldKeyUp(event, options);
        });
    }

    {customScript}
})();
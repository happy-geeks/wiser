(() => {
    let options = $.extend({
        culture: "nl-NL"
    }, {options});

    let field = $("#field_{propertyIdWithSuffix}");
    
    let kendoComponent = field.kendoNumericTextBox($.extend({change: window.dynamicItems.fields.onFieldValueChange.bind(window.dynamicItems.fields)}, {options})).data("kendoNumericTextBox");

    if (options.saveOnEnter) {
        field.keypress(function(event) {
            window.dynamicItems.fields.onInputFieldKeyUp(event, options);
        });
    }

    {customScript}
})();
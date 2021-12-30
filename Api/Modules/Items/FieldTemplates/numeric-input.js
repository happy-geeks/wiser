(function() {
    var options = $.extend({
        culture: "nl-NL"
    }, {options});

    var field = $("#field_{propertyIdWithSuffix}");
    var kendoComponent = field.kendoNumericTextBox($.extend({change: window.dynamicItems.fields.onFieldValueChange.bind(window.dynamicItems.fields)}, {options})).data("kendoNumericTextBox");

    if (options.saveOnEnter) {
        field.keypress(function(event) {
            window.dynamicItems.fields.onInputFieldKeyUp(event, options);
        });
    }

    {customScript}
})();
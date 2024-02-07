(function() {
    let kendoComponent = $("#field_{propertyIdWithSuffix}").kendoColorPicker($.extend({
        buttons: false,
        change: window.dynamicItems.fields.onFieldValueChange.bind(window.dynamicItems.fields),
    }, {options})).data("kendoColorPicker");
    
    const readonly = {readonly};
    
    kendoComponent.enable(!readonly);
})();
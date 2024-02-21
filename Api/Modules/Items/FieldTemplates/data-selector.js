(() => {
    const field = $("#field_{propertyIdWithSuffix}");
    
    const options = $.extend({
        click: function(event) {
            window.dynamicItems.fields.onDataSelectorButtonClick(event, {default_value}, {itemId}, {propertyId}, {options}, field); 
        },
        icon: "gear"
    }, {options});

    if (field.text) {
        field.find(".originalText").html(options.text);
    }
    
    const kendoComponent = field.kendoButton(options).data("kendoButton");
    
    {customScript}
})();
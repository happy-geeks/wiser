(function() {
    var field = $("#field_{propertyIdWithSuffix}");
    var options = $.extend({
        click: function(event) {
            window.dynamicItems.fields.onDataSelectorButtonClick(event, {default_value}, {itemId}, {propertyId}, {options}, field); 
        },
        icon: "gear"
    }, {options});

    if (field.text) {
        field.find(".originalText").html(options.text);
    }
    var kendoComponent = field.kendoButton(options).data("kendoButton");
    {customScript}
})();
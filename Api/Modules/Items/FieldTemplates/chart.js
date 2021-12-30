(function() {
    var field = $("#chart{propertyIdWithSuffix}");
    var loader = field.closest(".item").find(".field-loader");
    var optionsFromProperty = {options};

    var options = $.extend(true, {
        title: { text: "{title}" },
        dataSource: {
            transport: {
                read: {
                    method: "POST",
                    url: dynamicItems.settings.wiserApiRoot + "items/" + encodeURIComponent("{itemIdEncrypted}") + "/action-button/{propertyId}?queryId=" + encodeURIComponent(optionsFromProperty.queryId || 0),
                    contentType: "application/json"
                }
            },
        
            schema: {
                data: "other_data"
            }
        },
        dataBound: function(event) {
            loader.removeClass("loading");
        }
    }, optionsFromProperty);
    console.log("chart options", options);

    var kendoComponent = field.kendoChart(options).data("kendoChart");
    console.log("chart", kendoComponent);
	
    {customScript}
})();
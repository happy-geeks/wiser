(function() {
    var field = $("#chart{propertyIdWithSuffix}");
    var loader = field.closest(".item").find(".field-loader");
    var optionsFromProperty = {options};

    var options = $.extend(true, {
        title: { text: "{title}" },
        dataSource: {
            transport: {
                read: (options) => {
                    Wiser2.api({
                        url: dynamicItems.settings.wiserApiRoot + "items/" + encodeURIComponent("{itemIdEncrypted}") + "/action-button/{propertyId}?queryId=" + encodeURIComponent(optionsFromProperty.queryId || 0),
                        dataType: "json",
                        method: "POST",
                        data: options.data
                    }).then((result) => {
                        options.success(result);
                    }).catch((result) => {
                        options.error(result);
                    });
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
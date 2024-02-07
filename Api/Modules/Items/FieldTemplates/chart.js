(function() {
    const field = $("#chart{propertyIdWithSuffix}");
    const loader = field.closest(".item").find(".field-loader");
    const optionsFromProperty = {options};
    const height = parseInt("{height}") || undefined;
    
    let chartArea;
    
    if (height) {
        chartArea = {
            height: height
        }
    }

    let options = $.extend(true, {
        title: { text: "{title}" },
        chartArea: chartArea,
        dataSource: {
            transport: {
                read: (options) => {
                    Wiser.api({
                        url: dynamicItems.settings.wiserApiRoot + "items/" + encodeURIComponent("{itemIdEncrypted}") + "/action-button/{propertyId}?queryId=" + encodeURIComponent(optionsFromProperty.queryId || 0),
                        dataType: "json",
                        method: "POST",
                        contentType: "application/json",
                        data: options.data
                    }).then((result) => {
                        options.success(result);
                    }).catch((result) => {
                        options.error(result);
                    });
                }
            },
        
            schema: {
                data: "otherData"
            }
        },
        dataBound: function(event) {
            loader.removeClass("loading");
        }
    }, optionsFromProperty);

    const kendoComponent = field.kendoChart(options).data("kendoChart");
	
    {customScript}
})();
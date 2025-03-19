(() => {
let field = $("#scheduler{propertyIdWithSuffix}");
let loader = field.closest(".item").find(".field-loader");
let optionsFromProperty = {options};

let options = $.extend(true, {
    views: [
        "day",
        { type: "week", selected: true },
        "month",
        "agenda",
        "timeline"
    ],
    height: 600,
    editable: false,
    dataSource: {
        transport: {
            read: function(transportOptions) {
                let queryResults = null;
                try {
                    queryResults = Wiser.api({
                        method: "POST",
                        url: dynamicItems.settings.wiserApiRoot + "items/" + encodeURIComponent("{itemIdEncrypted}") + "/action-button/{propertyId}?queryId=" + encodeURIComponent(optionsFromProperty.queryId || 0) + "&&itemLinkId={itemLinkId}",
                        contentType: "application/json"
                    });

                    if (!queryResults || !queryResults.otherData) {
                        transportOptions.error(queryResults);
                        return;
                    }

                    for (let dataIndex = 0; dataIndex < queryResults.otherData.length; dataIndex++) {
                        let dataItem = queryResults.otherData[dataIndex];

                        if (typeof dataItem.resources !== "string") {
                            continue;
                        }

                        dataItem.resources = dataItem.resources.split(",");
                    }

                    transportOptions.success(queryResults.otherData)
                }
                
                catch (exception) {
                    transportOptions.error(exception)
                }
            }
        },
        
        schema: {
            model: {
                id: "id",
                fields: {
                    taskId: { from: "id", type: "number" },
                    title: { from: "title", defaultValue: "Geen titel", validation: { required: true } },
                    start: { type: "date", from: "start" },
                    end: { type: "date", from: "end" },
                    resources: { from: "resources" },
                    color: { type: "string", from: "color" },
                    isAllDay: { type: "boolean", from: "all_day" }
                }
            }
        }
    },
    group: {
        resources: ["resources"],
        orientation: "vertical"
    },
    dataBound: function(event) {
        loader.removeClass("loading");
    }
}, optionsFromProperty);

if (optionsFromProperty.resourcesQueryId) {
    options.resources = [
        {
            field: "resources",
            title: "Persoon / ruimte / object",
            dataColorField: "color",
            multiple: true,
            dataSource: {
                schema: {
                    data: "otherData"
                },
                transport: {
                    read: (kendoReadOptions) => {
                        Wiser.api({
                            url: dynamicItems.settings.wiserApiRoot + "items/" + encodeURIComponent("{itemIdEncrypted}") + "/action-button/{propertyId}?queryId=" + encodeURIComponent(optionsFromProperty.resourcesQueryId || 0) + "&itemLinkId={itemLinkId}",
                            contentType: "application/json",
                            dataType: "json",
                            method: "POST",
                            data: kendoReadOptions.data
                        }).then((result) => {
                            kendoReadOptions.success(result);
                        }).catch((result) => {
                            kendoReadOptions.error(result);
                        });
                    }
                }
            }
        }
    ];
}


let kendoComponent = field.kendoScheduler(options).data("kendoScheduler");
	
{customScript}
})();
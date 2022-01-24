(function() {
var field = $("#scheduler{propertyIdWithSuffix}");
var loader = field.closest(".item").find(".field-loader");
var optionsFromProperty = {options};

var options = $.extend(true, {
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
                Wiser2.api({
                    method: "POST",
                    url: dynamicItems.settings.wiserApiRoot + "items/" + encodeURIComponent("{itemIdEncrypted}") + "/action-button/{propertyId}?queryId=" + encodeURIComponent(optionsFromProperty.queryId || 0) + "&&itemLinkId={itemLinkId}",
                    contentType: "application/json"
                }).then(function(queryResults) {
                    if (!queryResults || !queryResults.otherData) {
                        transportOptions.error(queryResults);
                        return;
                    }
                    
                    for (var dataIndex = 0; dataIndex < queryResults.otherData.length; dataIndex++) {
                        var dataItem = queryResults.otherData[dataIndex];
                        
                        if (typeof dataItem.resources !== "string") {
                            continue;
                        }
                        
                        dataItem.resources = dataItem.resources.split(",");
                    }
                    
                    transportOptions.success(queryResults.otherData);
                }).catcg(function(jqXHR, textStatus, errorThrown) {
                    transportOptions.error(jqXHR, textStatus, errorThrown);
                });
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
                        Wiser2.api({
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


var kendoComponent = field.kendoScheduler(options).data("kendoScheduler");
	
{customScript}
})();
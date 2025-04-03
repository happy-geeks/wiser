(function () {
    let field = $("#timeline{propertyIdWithSuffix}");
    let loader = field.closest(".item").find(".field-loader");
    let optionsFromProperty = {options};
    let disableOpeningOfItems = optionsFromProperty.disableOpeningOfItems;
    let html = $("#eventTemplate_{propertyIdWithSuffix}").html();

    if (disableOpeningOfItems) {
        html = html.replace("openDetails", "openDetails hidden");
    }

    let options = $.extend(true, {
        orientation: "horizontal",
        editable: false,
        eventTemplate: kendo.template(html),
        dateFormat: "d MMM, yyyy",
        dataSource: {
            transport: {
                read: async (transportOptions) =>{
                    try {
                        let queryResults = await Wiser.api({
                            method: "POST",
                            url: dynamicItems.settings.wiserApiRoot + "items/" + encodeURIComponent("{itemIdEncrypted}") + "/action-button/{propertyId}?queryId=" + encodeURIComponent(optionsFromProperty.queryId || 0) + "&itemLinkId={itemLinkId}",
                            contentType: "application/json"
                        });

                        if (!queryResults || !queryResults.otherData || queryResults.otherData.length === 0) {
                            transportOptions.error("Geen data");
                            loader.removeClass("loading");
                            field.html("Geen data");
                            field.attr("class", "");
                        } else {
                            transportOptions.success(queryResults.otherData);
                        }
                    }
                    catch(errorThrown) {
                        transportOptions.error(errorThrown);
                    }
                }
            },
            schema: {
                model: {
                    id: "id",
                    fields: {
                        date: {
                            type: "date"
                        }
                    }
                }
            }
        },
        dataBound: (event) => {
            loader.removeClass("loading");

            // Default select last item
            let lastEvent = event.sender.element.find(".k-timeline-track-item:last");
            event.sender.open(lastEvent);

            // Bind action to "open item" buttons
            if (!disableOpeningOfItems) {
                field.on("click", ".openDetails", (event) => {
                    let itemId = $(this).closest(".timelineEvent").data("itemid");
                    window.dynamicItems.windows.loadItemInWindow(false, 0, itemId, options.entityType, "", false, null, options, 0);
                });
            }
        }
    }, optionsFromProperty);

    let kendoComponent = field.kendoTimeline(options).data("kendoTimeline");
    {customScript}
})();
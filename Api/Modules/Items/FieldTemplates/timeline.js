(function () {
    const field = $("#timeline{propertyIdWithSuffix}");
    const loader = field.closest(".item").find(".field-loader");
    const optionsFromProperty = {options};
    const disableOpeningOfItems = optionsFromProperty.disableOpeningOfItems;
    
    let html = $("#eventTemplate_{propertyIdWithSuffix}").html();

    if (disableOpeningOfItems) {
        html = html.replace("openDetails", "openDetails hidden");
    }

    const options = $.extend(true, {
        orientation: "horizontal",
        editable: false,
        eventTemplate: kendo.template(html),
        dateFormat: "d MMM, yyyy",
        dataSource: {
            transport: {
                read: function (transportOptions) {
                    Wiser.api({
                        method: "POST",
                        url: dynamicItems.settings.wiserApiRoot + "items/" + encodeURIComponent("{itemIdEncrypted}") + "/action-button/{propertyId}?queryId=" + encodeURIComponent(optionsFromProperty.queryId || 0) + "&itemLinkId={itemLinkId}",
                        contentType: "application/json"
                    }).then(function (queryResults) {
                        if (!queryResults || !queryResults.otherData || queryResults.otherData.length === 0) {
                            transportOptions.error("Geen data");
                            loader.removeClass("loading");
                            field.html("Geen data");
                            field.attr("class", "");
                            return;
                        }
                        else {
                            transportOptions.success(queryResults.otherData);
                        }
                    }).catch(function (jqXHR, textStatus, errorThrown) {
                        transportOptions.error(jqXHR, textStatus, errorThrown);
                    });
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
        dataBound: function (event) {
            loader.removeClass("loading");

            // Default select last item
            const lastEvent = event.sender.element.find(".k-timeline-track-item:last");
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

    const kendoComponent = field.kendoTimeline(options).data("kendoTimeline");
    {customScript}
})();
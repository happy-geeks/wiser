(function() {
var height = "{height}" || undefined;
var field = $("#field_{propertyIdWithSuffix}");

$.ajax({
    url: window.dynamicItems.settings.wiserApiRoot + "items/{itemId}/grids/{propertyId}?encryptedCustomerId=" + window.dynamicItems.settings.customerId,
    method: "GET",
    contentType: "application/json",
    dataType: "json",
    success: function(result) {
        generateGrid(result);
    }
});

function generateGrid(response) {
    if (response.extra_javascript) {
        jQuery.globalEval(response.extra_javascript);
    }
    
    if (response.columns && response.columns.length) {
        for (var i = 0; i < response.columns.length; i++) {
            if (!response.columns[i].editor) {
                continue;
            }
            
            response.columns[i].editor = window[response.columns[i].editor];
        }
    }
    
    var kendoComponent = field.kendoGrid({
        dataSource: {
			autoSync: true,
            transport: {
                read: function(options) {
                    options.success(response.data);
                },
                create: function(options) {
                    $.ajax({
                        url: window.dynamicItems.settings.wiserApiRoot + "items/{itemId}/grids/{propertyId}?encryptedCustomerId=" + window.dynamicItems.settings.customerId,
                        method: "POST",
                        contentType: "application/json",
                        dataType: "json",
                        data: JSON.stringify(options.data.models),
                        done: function(result) {
                            // notify the data source that the request succeeded
                            options.success(result);
                        },
                        fail: function(result) {
                            // notify the data source that the request failed
                            options.error(result);
                        }
                    });
                },
                update: function(options) {
                    $.ajax({
                        url: window.dynamicItems.settings.wiserApiRoot + "items/{itemId}/grids/{propertyId}?encryptedCustomerId=" + window.dynamicItems.settings.customerId,
                        method: "PUT",
                        contentType: "application/json",
                        dataType: "json",
                        data: JSON.stringify(options.data.models),
                        done: function(result) {
                            // notify the data source that the request succeeded
                            options.success(result);
                        },
                        fail: function(result) {
                            // notify the data source that the request failed
                            options.error(result);
                        }
                    });
                },
                destroy: function(options) {
                    $.ajax({
                        url: window.dynamicItems.settings.wiserApiRoot + "items/{itemId}/grids/{propertyId}?encryptedCustomerId=" + window.dynamicItems.settings.customerId,
                        method: "DELETE",
                        contentType: "application/json",
                        dataType: "json",
                        data: JSON.stringify(options.data.models),
                        done: function(result) {
                            // notify the data source that the request succeeded
                            options.success(result);
                        },
                        fail: function(result) {
                            // notify the data source that the request failed
                            options.error(result);
                        }
                    });
                }
            },
            pageSize: response.pageSize || 10,
            schema: {
                model: response.schema_model
            },
            batch: true
        },
        columns: response.columns,
        pageable: true,
        editable: "incell",
        toolbar: ["create", "excel"],
        height: height,
		excel: {
			fileName: "{title} Export.xlsx",
			//proxyURL: "https://demos.telerik.com/kendo-ui/service/export",
			filterable: true
		},
		edit: function (event) {
            // Note: This code is a fix/workaround for editable grids inside a kendoSortable. Source: https://docs.telerik.com/kendo-ui/controls/interactivity/sortable/how-to/use-sortable-grid
            var input = event.container.find("[data-role=numerictextbox]");
            var widget = input.data("kendoNumericTextBox");
            var model = event.model;

            if (!widget) {
                input = event.container.find("input");
                input.on("keyup", function (event2) {
                    $(this).trigger("change");
                });
            } else {
                widget.bind("spin", function (e) {
                    e.sender.trigger("change");
                });

                input.on("keyup", function (e) {
                    if (e.key === kendo.culture().numberFormat["."]) {
                            // for Kendo UI NumericTextBox only
                            return;
                    }
                    widget.value(input.val());
                    widget.trigger("change");
                });
            }
		}
    }).data("kendoGrid");
    
	dynamicItems.grids.attachSelectionCounter(kendoComponent);
	
    {customScript}
}
})();
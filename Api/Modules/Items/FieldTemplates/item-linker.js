(function() {
var field = $("#fieldSet_{propertyIdWithSuffix}");
var loader = field.closest(".item").find(".grid-loader");
var checkGridElement = field.find("#checkGrid_{propertyIdWithSuffix}");
var checkTreeElement = field.find("#checkTree_{propertyIdWithSuffix}");
var options = {options};
var currentItemId = '{itemIdEncrypted}';
var loadingCount = 0;
var readonly = {readonly};

var startLoader = function() {
    loadingCount++;
    loader.addClass("loading");
};

var stopLoader = function(reloadGridWhenDone) {
    loadingCount--;
    if (loadingCount < 0) {
        loadingCount = 0;
    }
    
    if (loadingCount === 0) {
        loader.removeClass("loading");
    
        if (reloadGridWhenDone) {
            checkGridElement.data("kendoGrid").dataSource.read();
        }
    }
};

field.find(".filterText").keyup(function (event) {
    var filterText = $(this).val();
    console.log("filtering", filterText, event);

    if (filterText !== "") {
        checkTreeElement.find(".k-group > li").hide();
        checkTreeElement.find(".k-in:contains(" + filterText + ")").each(function () {
            $(this).parents("ul, li").each(function () {
                var treeView = checkTreeElement.data("kendoTreeView");
                treeView.expand($(this).parents("li"));
                $(this).show();
            });
        });
        
        checkTreeElement.find(".k-group .k-in:contains(" + filterText + ")").each(function () {
            $(this).parents("ul, li").each(function () {
                $(this).show();
            });
        });
    }
    else {
        checkTreeElement.find(".k-group").find("li").show();
        var nodes = checkTreeElement.find("> .k-group > li");

        $.each(nodes, function (i, val) {
            if (nodes[i].getAttribute("data-expanded") == null) {
                $(nodes[i]).find("li").hide();
            }
        });
    }
});

var showStructure = options.showStructure || !options.entityTypes || options.entityTypes.length !== 1;

checkTreeElement.kendoTreeView({
    dataValueField: !showStructure ? "id" : "encrypted_item_id",
    dataTextField: !showStructure ? "name" : "title",
    checkboxes: readonly !== true,
    check: function(event) {
        if (readonly === true) {
            return;
        }
        
        startLoader();
        
        let sourceItem = event.sender.dataItem(event.node);
        let templateName = sourceItem.checked ? "ADD_LINK" : "REMOVE_LINK";
        
        $.get(window.dynamicItems.settings.serviceRoot + "/" + encodeURIComponent(templateName) + "?source=" + encodeURIComponent(sourceItem.id) + "&destination=" + encodeURIComponent(currentItemId) + "&linkTypeNumber=" + (options.linkTypeNumber || ""),
            function(result) {
                stopLoader(true);
            }
        );
    },

    dataSource: {
        transport: {
            read: {
                url: !showStructure 
                    ? window.dynamicItems.settings.serviceRoot + "GET_ALL_ITEMS_OF_TYPE?moduleid=" + options.moduleId + "&checkId=" + encodeURIComponent(currentItemId) + "&entityType=" + encodeURIComponent((!options.entityTypes ? "" : options.entityTypes.join())) + "&orderBy=" + encodeURIComponent((options.orderBy || ""))
                    : window.dynamicItems.settings.wiserApiRoot + "items/tree-view?moduleId=" + options.moduleId + "&checkId=" + encodeURIComponent(currentItemId) + (!options.entityTypes ? "" : ("&entityType=" + encodeURIComponent(options.entityTypes.join()))) + (options.orderBy ? ("&orderBy=" + encodeURIComponent(options.orderBy)) : ""),
                dataType: "json"
            }
        },
        schema: {
            model: {
                id: !showStructure ? "id" : "encrypted_item_id",
                hasChildren: !showStructure ? "haschilds" : "has_children"
            }
        }
    }
});

$.get(window.dynamicItems.settings.serviceRoot + "/GET_COLUMNS_FOR_LINK_TABLE?linkTypeNumber=" + (options.linkTypeNumber || "") + "&id=" + encodeURIComponent(currentItemId),
    function (customColumns) {
        var model = {
            id: "id",
            fields: {
                id: {
                    type: "number"
                },
                published_environment: {
                    type: "string"
                },
                title: {
                    type: "string"
                },
                entity_type: {
                    type: "string"
                },
                property_: {
                    type: "object"
                }
            }
        };
    
        var columns = [
            {
                field: "id",
                title: "Id",
                width: 55
            },
            {
                field: "title",
                title: "Naam"
            }
        ];
    
        if (customColumns && customColumns.length > 0) {
            for (var i = 0; i < customColumns.length; i++) {
                var column = customColumns[i];
                columns.push(column);
            }
        }
        
        if (!options.hideCommandColumn) {
            let commandColumnWidth = 80;
            var commands = [];
            
            if (!options.disableOpeningOfItems) {
                commands.push({
                    name: "openDetails",
                    iconClass: "k-icon k-i-hyperlink-open",
                    text: "&nbsp;",
                    click: function(event) { window.dynamicItems.grids.onShowDetailsClick(event, grid, options); }
                });
            }
            
            if (!readonly && options.deletionOfItems && options.deletionOfItems.toLowerCase() !== "off") {
                commandColumnWidth += 80;
                
                commands.push({
                    name: "remove",
                    text: "",
                    iconClass: "k-icon k-i-delete",
                    click: function(event) { window.dynamicItems.grids.onDeleteItemClick(event, this, options.deletionOfItems, options); }
                });
            }
            
            columns.push({
                title: "&nbsp;",
                width: commandColumnWidth,
                command: commands
            });
        }
        
        var toolbar = [];
         
        if (!options.toolbar || !options.toolbar.hideExportButton) {
            toolbar.push({name: "excel"});
        }
        
        if (!readonly && (!options.toolbar || !options.toolbar.hideCheckAllButton)) {
            toolbar.push({ 
                name: "checkAll", 
                text: "Alles selecteren", 
                template: "<a class='k-button k-button-icontext' href='\\#' onclick='return window.dynamicItems.grids.onItemLinkerSelectAll(\"\\#checkTree_{propertyIdWithSuffix}\", true)'><span class='k-icon k-i-checkbox-checked'></span>Alles selecteren</a>" 
            });
        }
        
        if (!readonly && (!options.toolbar || !options.toolbar.hideUncheckAllButton)) {
            toolbar.push({ 
                name: "uncheckAll", 
                text: "Alles deselecteren", 
                template: "<a class='k-button k-button-icontext' href='\\#' onclick='return window.dynamicItems.grids.onItemLinkerSelectAll(\"\\#checkTree_{propertyIdWithSuffix}\", false)'><span class='k-icon k-i-checkbox'></span>Alles deselecteren</a>" 
            });
        }
    
        if (checkGridElement.data("kendoGrid")) {
            checkGridElement.data("kendoGrid").destroy();
            checkGridElement.empty();
        }
    
        var grid = checkGridElement.kendoGrid({
            dataSource: {
                transport: {
                    read: function(readOptions) {
                        startLoader();
                        
                        $.get(window.dynamicItems.settings.serviceRoot + "/GET_DATA_FOR_FIELD_TABLE?itemId=" + encodeURIComponent("{itemIdEncrypted}") + "&linkTypeNumber=" + (options.linkTypeNumber || "") + "&moduleId=" + options.moduleId + "&entity_type=" + encodeURIComponent((!options.entityTypes ? "" : options.entityTypes.join())), function(results) {
                            if (!results) {
                                readOptions.success(results);
                                return;
                            }
    
                            for (var i = 0; i < results.length; i++) {
                                var row = results[i];
                                if (!row.property_) {
                                    row.property_ = {};
                                }
                            }
    
                            readOptions.success(results);
                            stopLoader();
                        });
                    },
                    update: function(options) {
                        if (readonly === true) {
                            return;
                        }
                        
                        startLoader();
                        
                        let itemModel = {
                            title: options.data.title,
                            details: []
                        };
                        
                        for (let key in options.data.property_) {
                            itemModel.details.push({
                                key: key,
                                value: options.data.property_[key]
                            });
                        }
                        
                        $.ajax({
                            url: window.dynamicItems.settings.wiserApiRoot + "items/" + encodeURIComponent(options.data.encryptedId),
                            method: "PUT",
                            contentType: "application/json",
                            dataType: "json",
                            data: JSON.stringify(itemModel),
                            done: function(result) {
                                // notify the data source that the request succeeded
                                options.success(options.data);
                                stopLoader();
                            },
                            fail: function(result) {
                                // notify the data source that the request failed
                                options.error(result);
                                stopLoader();
                            }
                        });
                    },
                    destroy: function(destroyOptions) {
                        if (readonly === true) {
                            return;
                        }
                        
                        startLoader();
                        
                        $.get(window.dynamicItems.settings.serviceRoot + "/REMOVE_LINK?source_plain=" + encodeURIComponent(options.data.id) + "&destination=" + encodeURIComponent(currentItemId) + "&linkTypeNumber=" + (options.linkTypeNumber || ""), 
                        function(results) {
                            destroyOptions.success(results);
                            checkTreeElement.data("kendoTreeView").dataSource.read();
                            stopLoader();
                        });
                    }
                },
                pageSize: options.pageSize || 10,
                schema: {
                    model: model
                }
            },
			toolbar: toolbar,
			excel: {
				fileName: "{title} Export.xlsx",
				//proxyURL: "https://demos.telerik.com/kendo-ui/service/export",
				filterable: true
			},
            columns: columns,
            pageable: {
                pageSize: options.pageSize || 10,
                refresh: true
            },
            sortable: true,
            resizable: true,
            editable: readonly === true || options.disableInlineEditing ? false : "incell",
            filterable: {
                extra: false,
                operators: {
                    string: {
                        contains: "Bevat",
                        doesnotcontain: "Bevat niet",
                        eq: "Is gelijk aan",
                        neq: "Is niet gelijk aan",
                        startswith: "Begint met",
                        doesnotstartwith: "Begint niet met",
                        endswith: "Eindigt met",
                        doesnotendwith: "Eindigt niet met"
                    }
                }
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
    
        grid.thead.kendoTooltip({
            filter: "th",
            content: function (event) {
                var target = event.target; // element for which the tooltip is shown
                return $(target).text();
            }
        });

        if (!options.disableOpeningOfItems) {
            checkGridElement.on("dblclick", "tbody tr[data-uid]", function(event) { window.dynamicItems.grids.onShowDetailsClick(event, grid, options); });
        }
    });
})();
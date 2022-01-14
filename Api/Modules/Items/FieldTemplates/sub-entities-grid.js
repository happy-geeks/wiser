(function() {
var field = $("#overviewGrid{propertyIdWithSuffix}");
var loader = field.closest(".item").find(".grid-loader");
var options = {options};
var customQueryGrid = options.customQuery === true;
var kendoComponent;
var isFirstLoad = true;
var height = "{height}" || undefined;
var linkTypeParameter = "";
if (options.linkTypeNumber) {
    linkTypeParameter = "?linkTypeNumber=" + encodeURIComponent(options.linkTypeNumber || "0");
}

var readonly = {readonly};
var rowIndex = null;
var cellIndex = null;
var editCount = 0;
var hideCheckboxColumn = !options.checkboxes || options.checkboxes === "false" || options.checkboxes <= 0;
var usingDataSelector = !!options.dataSelectorId;
options.usingDataSelector = usingDataSelector;
var gridMode = 0;
if (options.fieldGroupName) {
	gridMode = 6;
}

if (customQueryGrid) {
    $.get(window.dynamicItems.settings.wiserApiRoot + "items/{itemIdEncrypted}/grids/{propertyId}" + linkTypeParameter).then(function(customQueryResults) {
        if (customQueryResults.extra_javascript) {
            jQuery.globalEval(customQueryResults.extra_javascript);
        }
        
        if (!hideCheckboxColumn) {
            customQueryResults.columns.splice(0, 0, {
                selectable: true,
                width: "30px"
            });
        }

        if (!options.disableOpeningOfItems) {
            if (customQueryResults.schema_model && customQueryResults.schema_model.fields) {
                // If there is no field for encrypted ID, don't allow the user to open items, they'd just get an error.
                options.disableOpeningOfItems = !(customQueryResults.schema_model.fields.encryptedId || customQueryResults.schema_model.fields.encrypted_id || customQueryResults.schema_model.fields.encryptedid || customQueryResults.schema_model.fields.idencrypted);
            }
        }
        
        if (!options.hideCommandColumn) {
            let commandColumnWidth = 0;
            let commands = [];
            
            if (!options.disableOpeningOfItems) {
                commandColumnWidth += 80;
                
                commands.push({
                    name: "openDetails",
                    iconClass: "k-icon k-i-hyperlink-open",
                    text: "",
                    click: function(event) { window.dynamicItems.grids.onShowDetailsClick(event, kendoComponent, options); }
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
            } else if (!readonly && customQueryGrid && options.hasCustomDeleteQuery) {
                commandColumnWidth += 160;
                
                commands.push("destroy");
            }
            
            if (commands.length > 0) {
                customQueryResults.columns.push({
                    title: "&nbsp;",
                    width: commandColumnWidth,
                    command: commands
                });
            }
        }
        
        generateGrid(customQueryResults.data, customQueryResults.schema_model, customQueryResults.columns);
    });
} else {
    var done = function(gridSettings) {
        if (usingDataSelector) {
            gridSettings = {
                data: gridSettings,
                columns: options.columns
            };
        }
        
        if (gridSettings.extra_javascript) {
            jQuery.globalEval(gridSettings.extra_javascript);
        }
        
        // Add most columns here.
        if (gridSettings.columns && gridSettings.columns.length) {
            for (var i = 0; i < gridSettings.columns.length; i++) {
                var column = gridSettings.columns[i];
                
                switch (column.field || "") {
                    case "":
                        column.hidden = hideCheckboxColumn;
                        break;
                    case "id":
                        column.hidden = options.hideIdColumn || false;
                        break;
                    case "link_id":
                        column.hidden = options.hideLinkIdColumn || false;
                        break;
                    case "entity_type":
                        column.hidden = options.hideTypeColumn || false;
                        break;
                    case "published_environment":
                        column.hidden = options.hideEnvironmentColumn || false;
                        break;
                    case "title":
                        column.hidden = options.hideTitleColumn || false;
                        break;
                    case "added_on":
                        column.hidden = !options.showAddedOnColumn;
                        break;
                    case "added_by":
                        column.hidden = !options.showAddedByColumn;
                        break;
                    case "changed_on":
                        column.hidden = !options.showChangedOnColumn;
                        break;
                    case "changed_by":
                        column.hidden = !options.showChangedByColumn;
                        break;
                }
            }
        }

        if (!options.disableOpeningOfItems) {
            if (gridSettings.schema_model && gridSettings.schema_model.fields) {
                // If there is no field for encrypted ID, don't allow the user to open items, they'd just get an error.
                options.disableOpeningOfItems = !(gridSettings.schema_model.fields.encryptedId || gridSettings.schema_model.fields.encrypted_id || gridSettings.schema_model.fields.encryptedid || gridSettings.schema_model.fields.idencrypted);
            }
        }
        
        // Add command columns seperately, because of the click event that we can't do properly server-side.
        if (!options.hideCommandColumn) {
            let commandColumnWidth = 80;
            let commands = [];
            
            if (!options.disableOpeningOfItems && !options.fieldGroupName) {
                commands.push({
                    name: "openDetails",
                    iconClass: "k-icon k-i-hyperlink-open",
                    text: "",
                    click: function(event) { window.dynamicItems.grids.onShowDetailsClick(event, kendoComponent, options); }
                });
            }
            
            if (!readonly && options.deletionOfItems && options.deletionOfItems.toLowerCase() !== "off" && !options.fieldGroupName) {
                commandColumnWidth += 80;
                
                commands.push({
                    name: "remove",
                    text: "",
                    iconClass: "k-icon k-i-delete",
                    click: function(event) { window.dynamicItems.grids.onDeleteItemClick(event, this, options.deletionOfItems, options); }
                });
            }
            
            if (gridSettings.columns && commands.length > 0) {
                gridSettings.columns.push({
                    title: "&nbsp;",
                    width: commandColumnWidth,
                    command: commands
                });
            }
        }
        
        generateGrid(gridSettings.data, gridSettings.schema_model, gridSettings.columns);
    }
    
    if (usingDataSelector) {
        $.ajax({
            url: window.dynamicItems.settings.getItemsUrl + "?trace=false&encryptedDataSelectorId=" + encodeURIComponent(options.dataSelectorId) + "&itemId=" + encodeURIComponent("{itemIdEncrypted}"),
            contentType: "application/json"
        }).then(done);
    } else {
        $.ajax({
            url: window.dynamicItems.settings.wiserApiRoot + "items/{itemIdEncrypted}/entity-grids/" + encodeURIComponent(options.entityType) + "?propertyId={propertyId}" + linkTypeParameter.replace("?", "&") + "&mode=" + gridMode.toString() + "&fieldGroupName=" + encodeURIComponent(options.fieldGroupName) + "&currentItemIsSourceId=" + (options.currentItemIsSourceId || false).toString(),
            method: "POST",
            contentType: "application/json"
        }).then(done);
    }
}
    
function generateGrid(data, model, columns) {
    var toolbar = [];
    if (!options.toolbar || !options.toolbar.hideExportButton) {
        toolbar.push({
            name: "excel"
        });
    }

    if (!options.toolbar || !options.toolbar.hideFullScreenButton) {
        toolbar.push({
            name: "fullScreen",
            text: "",
            template: `<a class='k-button k-button-icontext full-screen' title='Grid naar fullscreen' href='\\#'><span class='k-icon k-i-wiser-maximize'></span></a>`
        });
    }

    if (window.dynamicItems.grids.onClearAllFiltersClick && (!options.toolbar || !options.toolbar.hideClearFiltersButton)) {
        toolbar.push({
            name: "clearAllFilters",
            text: "",
            template: "<a class='k-button k-button-icontext clear-all-filters' title='Alle filters wissen' href='\\#' onclick='return window.dynamicItems.grids.onClearAllFiltersClick(event)'><span class='k-icon k-i-filter-clear'></span></a>"
        });
    }

    if (!options.toolbar || !options.toolbar.hideCount) {
        toolbar.push({
            name: "count",
            iconClass: "",
            text: "",
            template: '<div class="counterContainer"><span class="counter">0</span> <span class="plural">resultaten</span><span class="singular" style="display: none;">resultaat</span></div>'
        });
    }
    
    if (!readonly && (!options.toolbar || !options.toolbar.hideCreateButton)) {
        toolbar.push(options.fieldGroupName || (customQueryGrid && (!options.entityType || options.hasCustomInsertQuery))
        ? "create" 
        : { 
            name: "add", 
            text: "Nieuw", 
            template: "<a class='k-button k-button-icontext' href='\\#' onclick='return window.dynamicItems.grids.onNewSubEntityClick(\"{itemIdEncrypted}\", {itemId}, \"" + options.entityType + "\", \"\\#overviewGrid{propertyIdWithSuffix}\", " + !options.hideTitleColumn + ", \"" + (options.linkTypeNumber || "") + "\")'><span class='k-icon k-i-file-add'></span>" + options.entityType + " toevoegen</a>" 
        });
    }
    
    if (!readonly && options.entityType && (!options.toolbar || !options.toolbar.hideLinkButton)) {
        toolbar.push({ 
            name: "link", 
            text: "Koppelen", 
            template: "<a class='k-button k-button-icontext' href='\\#' onclick='return window.dynamicItems.grids.onLinkSubEntityClick(\"{itemIdEncrypted}\", {itemId}, \"" + options.entityType + "\", \"\\#overviewGrid{propertyIdWithSuffix}\", \"" + (options.linkTypeNumber || "") + "\", " + (options.hideIdColumn || false) + ", " + (options.hideLinkIdColumn || false) + ", " + (options.hideTypeColumn || false) + ", " + (options.hideEnvironmentColumn || false) + ", " + (options.hideTitleColumn || false) + ", {propertyId}, \"" + JSON.stringify(options).replace(/"/g, '\\"') + "\")'><span class='k-icon k-i-link-horizontal'></span>" + options.entityType + " koppelen</a>" 
        });
    }
    
    if (options.toolbar && options.toolbar.customActions && options.toolbar.customActions.length > 0) {
        for (var i = 0; i < options.toolbar.customActions.length; i++) {
            var customAction = options.toolbar.customActions[i];
            
            toolbar.push({
                name: "customAction" + i.toString(),
                text: customAction.text,
                template: "<a class='k-button k-button-icontext' href='\\#' onclick='return window.dynamicItems.fields.onSubEntitiesGridToolbarActionClick(\"\\#overviewGrid{propertyIdWithSuffix}\", \"{itemIdEncrypted}\", {propertyId}, " + JSON.stringify(customAction) + ", event)' style='" + (kendo.htmlEncode(customAction.style || "")) + "'><span class='k-icon k-i-" + customAction.icon + "'></span>" + customAction.text + "</a>" 
            });
        }
    }
    
    if (columns && columns.length) {
        for (var i = 0; i < columns.length; i++) {
            (function () {
                var column = columns[i];
                var editable = column.editable;
                if (column.field) {
                    column.field = column.field.toLowerCase();
                }
                
                if (typeof column.editable === "boolean") {
                    column.editable = function(event) { return editable; };
                } else if (column.editable) {
                    console.warn("Column '" + (column.field || "") + "' has an invalid value in property 'editable':", column.editable);
                    delete column.editable;
                }
                
                if (!column.editor) {
                    return;
                }
                
                column.editor = window.dynamicItems.grids[columns[i].editor];
            }());
        }
    }

    if (field.data("kendoGrid")) {
        field.data("kendoGrid").destroy();
        field.empty();
    }

    var editable;
    if (readonly === true) {
        editable = false;
    } else if (options.editable) {
        editable = options.editable;
    } else if(options.fieldGroupName) {
        editable = "incell";
    } else {
        editable = {
            destroy: customQueryGrid && (options.hasCustomDeleteQuery || false),
            update: customQueryGrid && (options.hasCustomUpdateQuery || false) && !options.disableInlineEditing,
            mode: "incell"
        };
    }
    
    var dataBindingType;
    var kendoGridOptions = $.extend(true, {
        dataSource: {
            autoSync: true,
            serverFiltering: !!options.serverFiltering,
			sort: customQueryGrid ? undefined : { field: "__ordering", dir: "asc" },
            transport: {
                read: function(transportOptions) {
                    try {
                        loader.addClass("loading");
                        
                        if (isFirstLoad) {
                            transportOptions.success(data);
                            isFirstLoad = false;
                            loader.removeClass("loading");
                            return;
                        }
                        
                        if (customQueryGrid) {
                            $.ajax({
                                url: window.dynamicItems.settings.wiserApiRoot + "items/{itemIdEncrypted}/grids-with-filters/{propertyId}" + linkTypeParameter,
                                method: "POST",
                                contentType: "application/json",
                                data: JSON.stringify(transportOptions.data)
                            }).done(function(customQueryResults) {
                                if (customQueryResults.data) {
                                    for (var i = 0; i < customQueryResults.data.length; i++) {
                                        var row = customQueryResults.data[i];
                                        if (!row.property_) {
                                            row.property_ = {};
                                        }
                                    }
                                }
                                transportOptions.success(customQueryResults.data);
                                loader.removeClass("loading");
                            });
                        } else {
                            $.ajax({
                                url: window.dynamicItems.settings.wiserApiRoot + "items/{itemIdEncrypted}/entity-grids/" + encodeURIComponent(options.entityType) + "?propertyId={propertyId}" + linkTypeParameter.replace("?", "&") + "&mode=" + gridMode.toString() + "&fieldGroupName=" + encodeURIComponent(options.fieldGroupName) + "&currentItemIsSourceId=" + (options.currentItemIsSourceId || false).toString(),
                                method: "POST",
                                contentType: "application/json",
                                data: JSON.stringify(transportOptions.data)
                            }).done(function(gridSettings) {
                                transportOptions.success(gridSettings.data);
                                loader.removeClass("loading");
                            });
                        }
                    } catch(exception) {
                        console.error(exception);
                        loader.removeClass("loading");
                        kendo.alert("Er is iets fout gegaan tijdens het laden van het veld '{title}'. Probeer het a.u.b. nogmaals door de pagina te verversen, of neem contact op met ons.");
                        transportOptions.error(exception);
                    }
                },
                update: function(transportOptions) {
                    try {
                        if (readonly === true) {
                            return;
                        }
                        
                        loader.addClass("loading");
                        
                        if (customQueryGrid) {
                            $.ajax({
                                url: window.dynamicItems.settings.wiserApiRoot + "items/{itemIdEncrypted}/grids/{propertyId}",
                                method: "PUT",
                                contentType: "application/json",
                                dataType: "json",
                                data: JSON.stringify(transportOptions.data)
                            }).done(function(result) {
                                // notify the data source that the request succeeded
                                transportOptions.success(result);
                                loader.removeClass("loading");
                            }).fail(function(jqXHR, textStatus, errorThrown) {
                                console.error("UPDATE FAIL", textStatus, errorThrown, jqXHR);
                                loader.removeClass("loading");
                                // notify the data source that the request failed
                                kendo.alert("Er is iets fout gegaan tijdens het opslaan van het veld '{title}'.<br>" + (errorThrown ? errorThrown : "Probeer het a.u.b. nogmaals, of neem contact op met ons."));
                                // notify the data source that the request failed
                                transportOptions.error(jqXHR);
                            });
                            
                            return;
                        }
                        
                        let itemModel = {
                            title: transportOptions.data.name,
                            details: []
                        };
                        
						var encryptedId = transportOptions.data.encryptedId || transportOptions.data.encrypted_id;
						if (options.fieldGroupName) {
							encryptedId = "{itemIdEncrypted}";
							transportOptions.data.group_name = options.fieldGroupName;
							itemModel.details.push(transportOptions.data);
						} else {
							var nonFieldProperties = ["id", "published_environment", "encrypted_id", "entity_type", "link_id", "link_type", "link_type_number", "encryptedId", "added_on", "added_by", "changed_on", "changed_by"];
							for (var key in transportOptions.data) {
								if (!transportOptions.data.hasOwnProperty(key) || nonFieldProperties.indexOf(key) > -1) {
									continue;
								}
								
								if (key === "name" || key === "title") {
									itemModel.title = transportOptions.data[key];
									continue;
								} else if (key === "__ordering") {
									if (dynamicItems.fieldTemplateFlags.enableSubEntitiesGridsOrdering) {
										itemModel.details.push({ "key": key, "value": transportOptions.data[key], "is_link_property": true, "item_link_id": transportOptions.data.link_id });
									}
									continue;
								}
								
								if (kendoComponent && kendoComponent.columns) {
									for (var i = 0; i < kendoComponent.columns.length; i++) {
										var column = kendoComponent.columns[i];
										if ((column.field + "_input") !== key || !column.values || !column.values.length) {
											continue;
										}
										
										for (var i2 = 0; i2 < column.values.length; i2++) {
											var columnDataItem = column.values[i2];
											if (transportOptions.data[key.replace("_input", "")] !== columnDataItem.value) {
												continue;
											}
											
											transportOptions.data[key] = columnDataItem.text || value;
										}
									}
								}
								
								var isLinkProperty = false;
								if (columns && columns.length) {
									for (var i = 0; i < columns.length; i++) {
										if (columns[i].field !== key) {
											continue;
										}
										
										isLinkProperty = columns[i].is_link_property === true;
										break;
									}
								}
								
								itemModel.details.push({ "key": key, "value": transportOptions.data[key], "is_link_property": isLinkProperty, "item_link_id": transportOptions.data.link_id });
							}
						}
                        
                        $.ajax({
                            url: window.dynamicItems.settings.wiserApiRoot + "items/" + encodeURIComponent(encryptedId),
                            method: "PUT",
                            contentType: "application/json",
                            dataType: "json",
                            data: JSON.stringify(itemModel)
                        }).done(function(result) {
							if (transportOptions.data && transportOptions.data.details) {
								for (var i = 0; i < transportOptions.data.details.length; i++) {
									var currentField = transportOptions.data.details[i];
									if (currentField.key !== "__ordering") {
										continue;
									}
									
									transportOptions.data.__ordering = currentField.value;
								}
							}
							
                            // notify the data source that the request succeeded
                            transportOptions.success(transportOptions.data);
                            if (options.fieldGroupName) {
                                // Reload the grid, so that we have the IDs of all the items.
                                kendoComponent.dataSource.read();
                            }
                            loader.removeClass("loading");
                        }).fail(function(jqXHR, textStatus, errorThrown) {
                            console.error("UPDATE FAIL", textStatus, errorThrown, jqXHR);
                            loader.removeClass("loading");
                            // notify the data source that the request failed
                            kendo.alert("Er is iets fout gegaan tijdens het opslaan van het veld '{title}'.<br>" + (errorThrown ? errorThrown : "Probeer het a.u.b. nogmaals, of neem contact op met ons."));
                            transportOptions.error(jqXHR);
                        });
                    } catch(exception) {
                        console.error(exception);
                        loader.removeClass("loading");
                        kendo.alert("Er is iets fout gegaan tijdens het opslaan van het veld '{title}'. Probeer het a.u.b. nogmaals, of neem contact op met ons.");
                        transportOptions.error(exception);
                    }
                },
                create: function(transportOptions) {
                    try {
                        if (readonly === true) {
                            return;
                        }
						
                        if (options.fieldGroupName) {
							let itemModel = {
								details: []
							};
							var encryptedId = "{itemIdEncrypted}";
							transportOptions.data.group_name = options.fieldGroupName;
							itemModel.details.push(transportOptions.data);
							
							$.ajax({
								url: window.dynamicItems.settings.wiserApiRoot + "items/" + encodeURIComponent(encryptedId),
								method: "PUT",
								contentType: "application/json",
								dataType: "json",
								data: JSON.stringify(itemModel)
							}).done(function(result) {
								if (transportOptions.data && transportOptions.data.details) {
									for (var i = 0; i < transportOptions.data.details.length; i++) {
										var currentField = transportOptions.data.details[i];
										if (currentField.key !== "__ordering") {
											continue;
										}
										
										transportOptions.data.__ordering = currentField.value;
									}
								}
								
								// notify the data source that the request succeeded
								transportOptions.success(transportOptions.data);
								loader.removeClass("loading");
							}).fail(function(jqXHR, textStatus, errorThrown) {
								console.error("UPDATE FAIL", textStatus, errorThrown, jqXHR);
								loader.removeClass("loading");
								// notify the data source that the request failed
								kendo.alert("Er is iets fout gegaan tijdens het opslaan van het veld '{title}'.<br>" + (errorThrown ? errorThrown : "Probeer het a.u.b. nogmaals, of neem contact op met ons."));
								transportOptions.error(jqXHR);
							});
						} else if (customQueryGrid) {
                            loader.addClass("loading");
                            
                            $.ajax({
                                url: window.dynamicItems.settings.wiserApiRoot + "items/{itemIdEncrypted}/grids/{propertyId}",
                                method: "POST",
                                contentType: "application/json",
                                dataType: "json",
                                data: JSON.stringify(transportOptions.data)
                            }).done(function(result) {
                                // notify the data source that the request succeeded
                                transportOptions.success(result);
                                loader.removeClass("loading");
                            }).fail(function(jqXHR, textStatus, errorThrown) {
                                // notify the data source that the request failed
                                transportOptions.error(jqXHR);
                                loader.removeClass("loading");
                                kendo.alert("Er is iets fout gegaan tijdens het aanmaken van een item.<br>" + (errorThrown ? errorThrown : "Probeer het a.u.b. nogmaals, of neem contact op met ons."));
                            });
                        }
                    } catch(exception) {
                        console.error(exception);
                        loader.removeClass("loading");
                        kendo.alert("Er is iets fout gegaan tijdens het opslaan van het veld '{title}'. Probeer het a.u.b. nogmaals, of neem contact op met ons.");
                        transportOptions.error(exception);
                    }
                },
                destroy: function(transportOptions) {
                    try {
                        if (readonly === true) {
                            return;
                        }
                        
                        if (options.fieldGroupName) {
							let itemModel = {
								details: []
							};
							var encryptedId = "{itemIdEncrypted}";
							transportOptions.data.group_name = options.fieldGroupName;
							transportOptions.data.value = null;
							itemModel.details.push(transportOptions.data);
							
							$.ajax({
								url: window.dynamicItems.settings.wiserApiRoot + "items/" + encodeURIComponent(encryptedId),
								method: "PUT",
								contentType: "application/json",
								dataType: "json",
								data: JSON.stringify(itemModel)
							}).done(function(result) {
								if (transportOptions.data && transportOptions.data.details) {
									for (var i = 0; i < transportOptions.data.details.length; i++) {
										var currentField = transportOptions.data.details[i];
										if (currentField.key !== "__ordering") {
											continue;
										}
										
										transportOptions.data.__ordering = currentField.value;
									}
								}
								
								// notify the data source that the request succeeded
								transportOptions.success(transportOptions.data);
								loader.removeClass("loading");
							}).fail(function(jqXHR, textStatus, errorThrown) {
								console.error("UPDATE FAIL", textStatus, errorThrown, jqXHR);
								loader.removeClass("loading");
								// notify the data source that the request failed
								kendo.alert("Er is iets fout gegaan tijdens het opslaan van het veld '{title}'.<br>" + (errorThrown ? errorThrown : "Probeer het a.u.b. nogmaals, of neem contact op met ons."));
								transportOptions.error(jqXHR);
							});
						} else if (customQueryGrid) {
                            loader.addClass("loading");
                            
                            $.ajax({
                                url: window.dynamicItems.settings.wiserApiRoot + "items/{itemIdEncrypted}/grids/{propertyId}",
                                method: "DELETE",
                                contentType: "application/json",
                                dataType: "json",
                                data: JSON.stringify(transportOptions.data),
                                success: function(result) {
                                    // notify the data source that the request succeeded
                                    transportOptions.success(result);
                                    loader.removeClass("loading");
                                },
                                error: function(jqXHR, textStatus, errorThrown) {
                                    // notify the data source that the request failed
                                    transportOptions.error(jqXHR);
                                    loader.removeClass("loading");
                                    kendo.alert("Er is iets fout gegaan tijdens het verwijderen van deze regel.<br>" + (errorThrown ? errorThrown : "Probeer het a.u.b. nogmaals, of neem contact op met ons."));
                                }
                            });
                        }
                    } catch(exception) {
                        console.error(exception);
                        loader.removeClass("loading");
                        kendo.alert("Er is iets fout gegaan tijdens het opslaan van het veld '{title}'. Probeer het a.u.b. nogmaals, of neem contact op met ons.");
                        transportOptions.error(exception);
                    }
                }
            },
            pageSize: options.pageSize || 10,
            schema: {
                model: model
            }
        },
        excel: {
            fileName: "{title} Export.xlsx",
            allPages: true,
            filterable: true
        },
        pageable: {
            pageSize: options.pageSize || 10,
            refresh: true
        },
        sortable: true,
        resizable: true,
        navigatable: true,
        height: height,
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
            },
            messages: {
                isTrue: "<span>Ja</span>",
                isFalse: "<span>Nee</span>"
            }
        },
        filterMenuInit: window.dynamicItems.grids.onFilterMenuInit,
        filterMenuOpen: window.dynamicItems.grids.onFilterMenuOpen,
        columnHide: window.dynamicItems.grids.saveGridViewState.bind(window.dynamicItems.grids, "sub_entities_grid_columns_{propertyId}"),
        columnShow: window.dynamicItems.grids.saveGridViewState.bind(window.dynamicItems.grids, "sub_entities_grid_columns_{propertyId}"),
        excelExport: function(e) {
            loader.removeClass("loading");
        },
        dataBinding: function(e) {
            dataBindingType = e.action;

            // Remember the current selected cell, because the focus will be lost after the data has been bound.
            var current = e.sender.current() || [];
            if (current[0]) {
                cellIndex = current.index();
                rowIndex = current.parent().index();
            }
        },
        dataBound: function(event) {
            // Setup any progress bars.
            event.sender.tbody.find(".progress").each(function(e) {
                var row = $(this).closest("tr");
                var columnIndex = $(this).closest("td").index();
                if (columnIndex < 0 || columnIndex >= columns.length) {
                    console.warn("Found progress bar in column " + columnIndex.toString() + " but couldn't find the corresponding column in grid.options.columns.");
                    return;
                }
                
                var column = columns[columnIndex];
                var model = event.sender.dataItem(row);
                var value = parseInt(model[column.field]) || 0;
                column.progress_bar_settings = column.progress_bar_settings || {};

                var progressBar = $(this).kendoProgressBar({
                    max: column.progress_bar_settings.max_progress || 100,
                    value: value
                }).data("kendoProgressBar");
                
                if (!column.progress_bar_settings.progress_colors || !column.progress_bar_settings.progress_colors.length) {
                    return;
                }
                
                var progressColors = column.progress_bar_settings.progress_colors.sort(function(a, b) {
                    return b.max - a.max;
                });
                                
                for (var i = 0; i < progressColors.length; i++) {
                    var progressColor = progressColors[i];
                    if (value <= progressColor.max) {
                        progressBar.progressWrapper.css({
                            "background-color": progressColor.background,
                            "border-color": progressColor.border
                        });
                    }
                }
            });
            
            // Show the amount of results in the toolbar.
            const totalCount = event.sender.dataSource.total();
            const counterContainer = event.sender.element.find(".k-grid-toolbar .counterContainer");
            counterContainer.find(".counter").html(kendo.toString(totalCount, "n0"));
            counterContainer.find(".plural").toggle(totalCount !== 1);
            counterContainer.find(".singular").toggle(totalCount === 1);
                        
            // If cellIndex is unknown, or the user did not start editting a new cell (which means they just unfocussed the grid), do nothing.
            if (isNaN(cellIndex) ||  editCount < 1) {
                return;
            }
            
            // Re-focus en edit the cell that was previously selected.
            var cellToFocus = event.sender.tbody.children().eq(rowIndex).children().eq(cellIndex);
            event.sender.current(cellToFocus);
            event.sender.editCell(cellToFocus);

            if (dataBindingType === "sync") {
                return;
            }
            rowIndex = cellIndex = null;
            
            // Reset the edit count back to 0.
            editCount = 0;
        },
        edit: function(e) {
            // If the model is dirty, it means there is a change in the current data row.
            // If that is the case while this edit event is called, it means that the user changed a value and then started editting again.
            // Therefor we update the editCount, so that the dataBound event re-focusses this cell so that the user can keep editting.
            if (e.model.dirty) {
                editCount++;
            }
        },
        save: function(e) {
            if (options.refreshGridAfterInlineEdit) {
                e.sender.one("dataBound", function() {
                    e.sender.dataSource.read();
                });
            }
        }
    }, options);

    kendoGridOptions.editable = editable;
    kendoGridOptions.selectable = hideCheckboxColumn ? options.selectable : false;
    kendoGridOptions.toolbar = toolbar.length === 0 ? null : toolbar;
    kendoGridOptions.columns = columns;

    kendoComponent = field.kendoGrid(kendoGridOptions).data("kendoGrid");

    window.dynamicItems.grids.loadGridViewState("sub_entities_grid_columns_{propertyId}", kendoComponent);

    kendoComponent.thead.kendoTooltip({
        filter: "th",
        content: function (event) {
            var target = event.target; // element for which the tooltip is shown
            return $(target).text();
        }
    });

    if (!options.disableOpeningOfItems) {
        field.on("dblclick", "tbody tr[data-uid] td", function(event) { window.dynamicItems.grids.onShowDetailsClick(event, kendoComponent, options); });
    }
    
    kendoComponent.element.find(".k-grid-excel").click(function(event) { loader.addClass("loading"); });

	dynamicItems.grids.attachSelectionCounter(field[0]);
	
	if (!customQueryGrid && dynamicItems.fieldTemplateFlags.enableSubEntitiesGridsOrdering) {
        kendoComponent.table.kendoSortable({
            autoScroll: true,
			hint: function(element) {
				var table = kendoComponent.table.clone(); // Clone the Grid table.
				var wrapperWidth = kendoComponent.wrapper.width(); // Get the Grid width.
				var wrapper = $("<div class='k-grid k-widget'></div>").width(wrapperWidth);
				var hint;

				table.find("thead").remove(); // Remove the Grid header from the hint.
				table.find("tbody").empty(); // Remove the existing rows from the hint.
				table.wrap(wrapper); // Wrap the table
				table.append(element.clone().removeAttr("uid")); // Append the dragged element.

				hint = table.parent(); // Get the wrapper.

				return hint; // Return the hint element.
			},
			cursor: "move",
			placeholder: function(element) {
				return element.clone().addClass("k-state-hover").css("opacity", 0.65);
			},
			container: "#overviewGrid{propertyIdWithSuffix}",
			filter: ">tbody >tr",
			change: function(e) {
				// Kendo starts ordering with 0, but wiser starts with 1.
				var oldIndex = e.oldIndex + 1; // The old position.
				var newIndex = e.newIndex + 1; // The new position.
				var view = kendoComponent.dataSource.view();
				var dataItem = kendoComponent.dataSource.getByUid(e.item.data("uid")); // Retrieve the moved dataItem.

				dataItem.__ordering = newIndex; // Update the order
				dataItem.dirty = true;

				// Shift the order of the records.
				if (oldIndex < newIndex) {
					for (var i = oldIndex + 1; i <= newIndex; i++) {
						view[i-1].__ordering--;
						view[i-1].dirty = true;
					}
				} else {
					for (var i = oldIndex - 1; i >= newIndex; i--) {
						view[i-1].__ordering++;
						view[i-1].dirty = true;
					}
				}

				kendoComponent.dataSource.sync();
			}
		});
	}

    {customScript}
}
})();
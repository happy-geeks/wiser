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
    Wiser.api({ 
        url: `${window.dynamicItems.settings.wiserApiRoot}items/${encodeURIComponent("{itemIdEncrypted}")}/grids/{propertyId}${linkTypeParameter}` 
    }).then(function(customQueryResults) {
        if (customQueryResults.extraJavascript) {
            jQuery.globalEval(customQueryResults.extraJavascript);
        }
        
        if (!hideCheckboxColumn) {
            customQueryResults.columns.splice(0, 0, {
                selectable: true,
                width: "30px"
            });
        }

        if (!options.disableOpeningOfItems) {
            if (customQueryResults.schemaModel && customQueryResults.schemaModel.fields) {
                // If there is no field for encrypted ID, don't allow the user to open items, they'd just get an error.
                options.disableOpeningOfItems = !(customQueryResults.schemaModel.fields.encryptedId || customQueryResults.schemaModel.fields.encrypted_id || customQueryResults.schemaModel.fields.encryptedid || customQueryResults.schemaModel.fields.idencrypted);
            }
        }
        
        if (!options.hideCommandColumn) {
            let commandColumnWidth = 0;
            let commands = [];
            
            if (!options.disableOpeningOfItems) {
                commandColumnWidth += 60;
                
                commands.push({
                    name: "openDetails",
                    iconClass: "k-icon k-i-hyperlink-open",
                    text: "",
                    title: "Item openen",
                    click: function(event) { window.dynamicItems.grids.onShowDetailsClick(event, kendoComponent, options, false); }
                });
                
                if (options.allowOpeningOfItemsInNewTab) {
                    commandColumnWidth += 60;

                    commands.push({
                        name: "openDetailsInNewTab",
                        iconClass: "k-icon k-i-window",
                        text: "",
                        title: "Item openen in nieuwe tab",
                        click: function(event) { window.dynamicItems.grids.onShowDetailsClick(event, kendoComponent, options, true); }
                    });
                }
            }

            if (!readonly && options.deletionOfItems && options.deletionOfItems.toLowerCase() !== "off") {
                commandColumnWidth += 60;
                
                commands.push({
                    name: "remove",
                    text: "",
                    iconClass: "k-icon k-i-delete",
                    click: function(event) { window.dynamicItems.grids.onDeleteItemClick(event, this, options.deletionOfItems, options); }
                });
            } else if (!readonly && customQueryGrid && options.hasCustomDeleteQuery) {
                commandColumnWidth += 120;
                
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
        
        generateGrid(customQueryResults.data, customQueryResults.schemaModel, customQueryResults.columns);
    });
} else {
    var done = function(gridSettings) {
        if (usingDataSelector) {
            gridSettings = {
                data: gridSettings,
                columns: options.columns
            };
        }
        
        if (gridSettings.extraJavascript) {
            jQuery.globalEval(gridSettings.extraJavascript);
        }
        
        // Add most columns here.
        if (gridSettings.columns && gridSettings.columns.length) {
            for (var i = 0; i < gridSettings.columns.length; i++) {
                var column = gridSettings.columns[i];
                
                switch ((column.field || "").toLowerCase()) {
                    case "":
                        column.hidden = hideCheckboxColumn;
                        break;
                    case "id":
                        column.hidden = options.hideIdColumn || false;
                        break;
                    case "link_id":
                    case "linkid":
                        column.hidden = options.hideLinkIdColumn || false;
                        break;
                    case "entity_type":
                    case "entitytype":
                        column.hidden = options.hideTypeColumn || false;
                        break;
                    case "published_environment":
                    case "publishedenvironment":
                        column.hidden = options.hideEnvironmentColumn || false;
                        break;
                    case "title":
                        column.hidden = options.hideTitleColumn || false;
                        break;
                    case "added_on":
                    case "addedon":
                        column.hidden = !options.showAddedOnColumn;
                        break;
                    case "added_by":
                    case "addedby":
                        column.hidden = !options.showAddedByColumn;
                        break;
                    case "changed_on":
                    case "changedon":
                        column.hidden = !options.showChangedOnColumn;
                        break;
                    case "changed_by":
                    case "changedby":
                        column.hidden = !options.showChangedByColumn;
                        break;
                }
            }
        }

        if (!options.disableOpeningOfItems) {
            if (gridSettings.schemaModel && gridSettings.schemaModel.fields) {
                // If there is no field for encrypted ID, don't allow the user to open items, they'd just get an error.
                options.disableOpeningOfItems = !(gridSettings.schemaModel.fields.encryptedId || gridSettings.schemaModel.fields.encrypted_id || gridSettings.schemaModel.fields.encryptedid || gridSettings.schemaModel.fields.idencrypted);
            }
        }
        
        // Add command columns separately, because of the click event that we can't do properly server-side.
        if (!options.hideCommandColumn) {
            let commandColumnWidth = 0;
            let commands = [];
            
            if (!options.disableOpeningOfItems && !options.fieldGroupName) {
                commandColumnWidth += 60;
                commands.push({
                    name: "openDetails",
                    iconClass: "k-icon k-i-hyperlink-open",
                    text: "",
                    click: function(event) { window.dynamicItems.grids.onShowDetailsClick(event, kendoComponent, options, false); }
                });

                if (options.allowOpeningOfItemsInNewTab) {
                    commandColumnWidth += 60;

                    commands.push({
                        name: "openDetailsInNewTab",
                        iconClass: "k-icon k-i-window",
                        text: "",
                        click: function(event) { window.dynamicItems.grids.onShowDetailsClick(event, kendoComponent, options, true); }
                    });
                }
            }
            
            if (!readonly && options.deletionOfItems && options.deletionOfItems.toLowerCase() !== "off" && !options.fieldGroupName) {
                commandColumnWidth += 60;
                
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
        
        generateGrid(gridSettings.data, gridSettings.schemaModel, gridSettings.columns);
    }
    
    if (usingDataSelector) {
        Wiser.api({
            url: `${window.dynamicItems.settings.getItemsUrl}?trace=false&encryptedDataSelectorId=${encodeURIComponent(options.dataSelectorId)}&itemId=${encodeURIComponent("{itemIdEncrypted}")}`,
            contentType: "application/json"
        }).then(done);
    } else {
        Wiser.api({
            url: `${window.dynamicItems.settings.wiserApiRoot}items/${encodeURIComponent("{itemIdEncrypted}")}/entity-grids/${encodeURIComponent(options.entityType || "{entityType}")}?propertyId={propertyId}${linkTypeParameter.replace("?", "&")}&mode=${gridMode.toString()}&fieldGroupName=${encodeURIComponent(options.fieldGroupName || "")}&currentItemIsSourceId=${(options.currentItemIsSourceId || false).toString()}`,
            method: "POST",
            contentType: "application/json"
        }).then(done);
    }
}    
async function generateGrid(data, model, columns) {
    var toolbar = [];
    if (!options.toolbar || !options.toolbar.hideExportButton) {
        toolbar.push({
            name: "excel"
        });
    }

    if (window.dynamicItems.grids.onClearAllFiltersClick && (!options.toolbar || !options.toolbar.hideClearFiltersButton)) {
        toolbar.push({
            name: "clearAllFilters",
            text: "",
            template: "<a class='k-button k-button-icontext clear-all-filters' title='Alle filters wissen' href='\\#' onclick='return window.dynamicItems.grids.onClearAllFiltersClick(event)'><span class='k-icon k-i-filter-clear'></span></a>"
        });
    }

    if (!options.toolbar || !options.toolbar.hideFullScreenButton) {
        toolbar.push({
            name: "fullScreen",
            text: "",
            template: `<a class='k-button k-button-icontext full-screen' title='Grid naar fullscreen' href='\\#' onclick='return window.dynamicItems.grids.onMaximizeGridClick(event)'><span class='k-icon k-i-wiser-maximize'></span></a>`
        });
    }

    if (!options.toolbar || !options.toolbar.hideCount) {
        toolbar.push({
            name: "count",
            iconClass: "",
            text: "",
            template: '<div class="counterContainer"><span class="counter">0</span> <span class="plural">resultaten</span><span class="singular" style="display: none;">resultaat</span></div>'
        });
    } else {
        toolbar.push({
            name: "whitespace",
            iconClass: "",
            text: "",
            template: '<div class="counterContainer"></div>'
        });
    }

    if (!readonly && (!options.toolbar || !options.toolbar.hideCreateButton)) {
        toolbar.push(options.fieldGroupName || (customQueryGrid && (!options.entityType || options.hasCustomInsertQuery))
            ? "create"
            : {
                name: "add",
                text: "Nieuw",
                template: "<a class='k-button k-button-icontext' href='\\#' onclick='return window.dynamicItems.grids.onNewSubEntityClick(\"{itemIdEncrypted}\", \"" + options.entityType + "\", \"\\#overviewGrid{propertyIdWithSuffix}\", " + !options.hideTitleColumn + ", \"" + (options.linkTypeNumber || "") + "\")'><span class='k-icon k-i-file-add'></span>" + window.dynamicItems.getEntityTypeFriendlyName(options.entityType) + " toevoegen</a>"
            });
    }

    if (!readonly && options.entityType && (!options.toolbar || !options.toolbar.hideLinkButton)) {
        toolbar.push({
            name: "link",
            text: "Koppelen",
            template: "<a class='k-button k-button-icontext' href='\\#' onclick='return window.dynamicItems.grids.onLinkSubEntityClick(\"{itemIdEncrypted}\", {itemId}, \"{entityType}\", \"" + options.entityType + "\", \"\\#overviewGrid{propertyIdWithSuffix}\", \"" + (options.linkTypeNumber || "") + "\", " + (options.hideIdColumn || false) + ", " + (options.hideLinkIdColumn || false) + ", " + (options.hideTypeColumn || false) + ", " + (options.hideEnvironmentColumn || false) + ", " + (options.hideTitleColumn || false) + ", {propertyId}, \"" + JSON.stringify(options).replace(/"/g, '\\"') + "\")'><span class='k-icon k-i-link-horizontal'></span>" + window.dynamicItems.getEntityTypeFriendlyName(options.entityType) + " koppelen</a>"
        });
    }

    if (options.toolbar && options.toolbar.customActions && options.toolbar.customActions.length > 0) {
        dynamicItems.grids.addCustomActionsToToolbar("#overviewGrid{propertyIdWithSuffix}", "{itemIdEncrypted}", "{propertyId}", toolbar, options.toolbar.customActions, "{entityType}");
    }

    if (columns && columns.length) {
        for (var i = 0; i < columns.length; i++) {
            (function () {
                var column = columns[i];
                var editable = column.editable;
                if (column.field && customQueryGrid) {
                    column.field = column.field.toLowerCase();
                }

                if (typeof column.editable === "boolean") {
                    column.editable = function (event) {
                        return editable;
                    };
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
    } else if (options.fieldGroupName) {
        editable = "incell";
    } else {
        editable = {
            destroy: customQueryGrid && (options.hasCustomDeleteQuery || false),
            update: customQueryGrid && (options.hasCustomUpdateQuery || false) && !options.disableInlineEditing,
            mode: "incell"
        };
    }

    var dataBindingType;
    let filtersChanged = false;
    var kendoGridOptions = $.extend(true, {
        dataSource: {
            autoSync: true,
            serverFiltering: !!options.serverFiltering,
            sort: customQueryGrid ? undefined : {field: "__ordering", dir: "asc"},
            transport: {
                read: function (transportOptions) {
                    try {
                        loader.addClass("loading");

                        if (isFirstLoad) {
                            transportOptions.success(data);
                            isFirstLoad = false;
                            loader.removeClass("loading");
                            return;
                        }

                        if (customQueryGrid) {
                            Wiser.api({
                                url: `${window.dynamicItems.settings.wiserApiRoot}items/${encodeURIComponent("{itemIdEncrypted}")}/grids-with-filters/{propertyId}${linkTypeParameter}`,
                                method: "POST",
                                contentType: "application/json",
                                data: JSON.stringify(transportOptions.data)
                            }).then(function (customQueryResults) {
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
                            }).catch(function (error) {
                                transportOptions.error(error);
                                loader.removeClass("loading");
                            });
                        } else {
                            Wiser.api({
                                url: `${window.dynamicItems.settings.wiserApiRoot}items/${encodeURIComponent("{itemIdEncrypted}")}/entity-grids/${encodeURIComponent(options.entityType || "{entityType}")}?propertyId={propertyId}${linkTypeParameter.replace("?", "&")}&mode=${gridMode.toString()}&fieldGroupName=${encodeURIComponent(options.fieldGroupName || "")}&currentItemIsSourceId=${(options.currentItemIsSourceId || false).toString()}`,
                                method: "POST",
                                contentType: "application/json",
                                data: JSON.stringify(transportOptions.data)
                            }).then(function (gridSettings) {
                                transportOptions.success(gridSettings.data);
                                loader.removeClass("loading");
                            }).catch(function (error) {
                                transportOptions.error(error);
                                loader.removeClass("loading");
                            });
                        }
                    } catch (exception) {
                        console.error(exception);
                        loader.removeClass("loading");
                        kendo.alert("Er is iets fout gegaan tijdens het laden van het veld '{title}'. Probeer het a.u.b. nogmaals door de pagina te verversen, of neem contact op met ons.");
                        transportOptions.error(exception);
                    }
                },
                update: function (transportOptions) {
                    try {
                        if (readonly === true) {
                            return;
                        }

                        loader.addClass("loading");

                        if (customQueryGrid) {
                            Wiser.api({
                                url: `${window.dynamicItems.settings.wiserApiRoot}items/${encodeURIComponent("{itemIdEncrypted}")}/grids/{propertyId}`,
                                method: "PUT",
                                contentType: "application/json",
                                dataType: "json",
                                data: JSON.stringify(transportOptions.data)
                            }).then(function (result) {
                                // notify the data source that the request succeeded
                                transportOptions.success(result);
                                loader.removeClass("loading");
                            }).catch(function (jqXHR, textStatus, errorThrown) {
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
                            details: [],
                            entityType: "{entityType}"
                        };

                        var encryptedId = transportOptions.data.encryptedId || transportOptions.data.encrypted_id || transportOptions.data.encryptedid;
                        if (options.fieldGroupName) {
                            encryptedId = "{itemIdEncrypted}";
                            transportOptions.data.groupName = options.fieldGroupName;
                            // If we have a predefined language code, then always force that language code, so that the user doesn't have to enter it manually.
                            if (options.languageCode) {
                                transportOptions.data.languageCode = options.languageCode;
                            }
                            itemModel.details.push(transportOptions.data);
                        } else {
                            var nonFieldProperties = [
                                "id",
                                "published_environment",
                                "publishedenvironment",
                                "encrypted_id",
                                "encryptedid",
                                "entity_type",
                                "entitytype",
                                "link_id",
                                "linkid",
                                "link_type",
                                "linktype",
                                "linktypenumber",
                                "added_on",
                                "addedon",
                                "added_by",
                                "addedby",
                                "changed_on",
                                "changedon",
                                "changed_by",
                                "changedby"
                            ];
                            for (var key in transportOptions.data) {
                                if (!transportOptions.data.hasOwnProperty(key) || nonFieldProperties.indexOf(key.toLowerCase()) > -1) {
                                    continue;
                                }

                                if (key === "name" || key === "title") {
                                    itemModel.title = transportOptions.data[key];
                                    continue;
                                } else if (key === "__ordering") {
                                    if (dynamicItems.fieldTemplateFlags.enableSubEntitiesGridsOrdering) {
                                        itemModel.details.push({
                                            "key": key,
                                            "value": transportOptions.data[key],
                                            "isLinkProperty": true,
                                            "itemLinkId": transportOptions.data.linkId || transportOptions.data.linkid || transportOptions.data.link_id,
                                            "linkType": options.linkTypeNumber || 0
                                        });
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

                                        isLinkProperty = columns[i].isLinkProperty === true;
                                        break;
                                    }
                                }

                                itemModel.details.push({
                                    "key": key,
                                    "value": transportOptions.data[key],
                                    "isLinkProperty": isLinkProperty,
                                    "itemLinkId": isLinkProperty ? (transportOptions.data.linkId || transportOptions.data.linkid || transportOptions.data.link_id) : 0,
                                    "linkType": options.linkTypeNumber || 0
                                });
                            }
                        }

                        Wiser.api({
                            url: window.dynamicItems.settings.wiserApiRoot + "items/" + encodeURIComponent(encryptedId),
                            method: "PUT",
                            contentType: "application/json",
                            dataType: "json",
                            data: JSON.stringify(itemModel)
                        }).then(function (result) {
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
                        }).catch(function (jqXHR, textStatus, errorThrown) {
                            console.error("UPDATE FAIL", textStatus, errorThrown, jqXHR);
                            loader.removeClass("loading");
                            // notify the data source that the request failed
                            kendo.alert("Er is iets fout gegaan tijdens het opslaan van het veld '{title}'.<br>" + (errorThrown ? errorThrown : "Probeer het a.u.b. nogmaals, of neem contact op met ons."));
                            transportOptions.error(jqXHR);
                        });
                    } catch (exception) {
                        console.error(exception);
                        loader.removeClass("loading");
                        kendo.alert("Er is iets fout gegaan tijdens het opslaan van het veld '{title}'. Probeer het a.u.b. nogmaals, of neem contact op met ons.");
                        transportOptions.error(exception);
                    }
                },
                create: function (transportOptions) {
                    try {
                        if (readonly === true) {
                            return;
                        }

                        if (options.fieldGroupName) {
                            let itemModel = {
                                details: [],
                                entityType: "{entityType}" 
                            };
                            
                            const encryptedId = "{itemIdEncrypted}";
                            transportOptions.data.groupName = options.fieldGroupName;
                            // If we have a predefined language code, then always force that language code, so that the user doesn't have to enter it manually.
                            if (options.languageCode) {
                                transportOptions.data.languageCode = options.languageCode;
                            }
                            itemModel.details.push(transportOptions.data);

                            Wiser.api({
                                url: window.dynamicItems.settings.wiserApiRoot + "items/" + encodeURIComponent(encryptedId),
                                method: "PUT",
                                contentType: "application/json",
                                dataType: "json",
                                data: JSON.stringify(itemModel)
                            }).then(function (result) {
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
                            }).catch(function (jqXHR, textStatus, errorThrown) {
                                console.error("UPDATE FAIL", textStatus, errorThrown, jqXHR);
                                loader.removeClass("loading");
                                // notify the data source that the request failed
                                kendo.alert("Er is iets fout gegaan tijdens het opslaan van het veld '{title}'.<br>" + (errorThrown ? errorThrown : "Probeer het a.u.b. nogmaals, of neem contact op met ons."));
                                transportOptions.error(jqXHR);
                            });
                        } else if (customQueryGrid) {
                            loader.addClass("loading");

                            Wiser.api({
                                url: `${window.dynamicItems.settings.wiserApiRoot}items/${encodeURIComponent("{itemIdEncrypted}")}/grids/{propertyId}`,
                                method: "POST",
                                contentType: "application/json",
                                dataType: "json",
                                data: JSON.stringify(transportOptions.data)
                            }).then(function (result) {
                                // notify the data source that the request succeeded
                                transportOptions.success(result);
                                loader.removeClass("loading");
                            }).catch(function (jqXHR, textStatus, errorThrown) {
                                // notify the data source that the request failed
                                transportOptions.error(jqXHR);
                                loader.removeClass("loading");
                                kendo.alert("Er is iets fout gegaan tijdens het aanmaken van een item.<br>" + (errorThrown ? errorThrown : "Probeer het a.u.b. nogmaals, of neem contact op met ons."));
                            });
                        }
                    } catch (exception) {
                        console.error(exception);
                        loader.removeClass("loading");
                        kendo.alert("Er is iets fout gegaan tijdens het opslaan van het veld '{title}'. Probeer het a.u.b. nogmaals, of neem contact op met ons.");
                        transportOptions.error(exception);
                    }
                },
                destroy: function (transportOptions) {
                    try {
                        if (readonly === true) {
                            return;
                        }

                        if (options.fieldGroupName) {
                            let itemModel = {
                                details: [],
                                entityType: "{entityType}"
                            };
                            var encryptedId = "{itemIdEncrypted}";
                            transportOptions.data.groupName = options.fieldGroupName;
                            // If we have a predefined language code, then always force that language code, so that the user doesn't have to enter it manually.
                            if (options.languageCode) {
                                transportOptions.data.languageCode = options.languageCode;
                            }
                            transportOptions.data.value = null;
                            transportOptions.data.key = "";
                            itemModel.details.push(transportOptions.data);

                            Wiser.api({
                                url: window.dynamicItems.settings.wiserApiRoot + "items/" + encodeURIComponent(encryptedId),
                                method: "PUT",
                                contentType: "application/json",
                                dataType: "json",
                                data: JSON.stringify(itemModel)
                            }).then(function (result) {
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
                            }).catch(function (jqXHR, textStatus, errorThrown) {
                                console.error("UPDATE FAIL", textStatus, errorThrown, jqXHR);
                                loader.removeClass("loading");
                                // notify the data source that the request failed
                                kendo.alert("Er is iets fout gegaan tijdens het opslaan van het veld '{title}'.<br>" + (errorThrown ? errorThrown : "Probeer het a.u.b. nogmaals, of neem contact op met ons."));
                                transportOptions.error(jqXHR);
                            });
                        } else if (customQueryGrid) {
                            loader.addClass("loading");

                            Wiser.api({
                                url: `${window.dynamicItems.settings.wiserApiRoot}items/${encodeURIComponent("{itemIdEncrypted}")}/grids/{propertyId}`,
                                method: "DELETE",
                                contentType: "application/json",
                                dataType: "json",
                                data: JSON.stringify(transportOptions.data)
                            }).then(function (result) {
                                // notify the data source that the request succeeded
                                transportOptions.success(result);
                                loader.removeClass("loading");
                            }).catch(function (jqXHR, textStatus, errorThrown) {
                                // notify the data source that the request failed
                                transportOptions.error(jqXHR);
                                loader.removeClass("loading");
                                kendo.alert("Er is iets fout gegaan tijdens het verwijderen van deze regel.<br>" + (errorThrown ? errorThrown : "Probeer het a.u.b. nogmaals, of neem contact op met ons."));
                            });
                        }
                    } catch (exception) {
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
        columnHide: (event) => window.dynamicItems.grids.saveGridViewColumnsState("sub_entities_grid_columns_{propertyId}", event.sender),
        columnShow: (event) => window.dynamicItems.grids.saveGridViewColumnsState("sub_entities_grid_columns_{propertyId}", event.sender),
        excelExport: function (e) {
            loader.removeClass("loading");
        },
        dataBinding: function (e) {
            dataBindingType = e.action;

            // Remember the current selected cell, because the focus will be lost after the data has been bound.
            var current = e.sender.current() || [];
            if (current[0]) {
                cellIndex = current.index();
                rowIndex = current.parent().index();
            }
        },
        dataBound: async (event) => {
            // To hide toolbar buttons that require a row to be selected.
            dynamicItems.grids.onGridSelectionChange(event);

            // Save the filters
            if (kendoGridOptions.keepFiltersState !== false && filtersChanged) {
                dynamicItems.grids.saveGridViewFiltersState("sub_entities_grid_filters_{propertyId}", event.sender)
            }

            // Setup any progress bars.
            event.sender.tbody.find(".progress").each(function (e) {
                var row = $(this).closest("tr");
                var columnIndex = $(this).closest("td").index();
                if (columnIndex < 0 || columnIndex >= columns.length) {
                    console.warn("Found progress bar in column " + columnIndex.toString() + " but couldn't find the corresponding column in grid.options.columns.");
                    return;
                }

                var column = columns[columnIndex];
                var model = event.sender.dataItem(row);
                var value = parseInt(model[column.field]) || 0;
                column.progressBarSettings = column.progressBarSettings || {};

                var progressBar = $(this).kendoProgressBar({
                    max: column.progressBarSettings.maxProgress || 100,
                    value: value
                }).data("kendoProgressBar");

                if (!column.progressBarSettings.progressColors || !column.progressBarSettings.progressColors.length) {
                    return;
                }

                var progressColors = column.progressBarSettings.progressColors.sort(function (a, b) {
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
            if (isNaN(cellIndex) || editCount < 1) {
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
        edit: function (e) {
            // If the model is dirty, it means there is a change in the current data row.
            // If that is the case while this edit event is called, it means that the user changed a value and then started editting again.
            // Therefor we update the editCount, so that the dataBound event re-focusses this cell so that the user can keep editting.
            if (e.model.dirty) {
                editCount++;
            }

            // This will remove the min and max attributes from a kendo numeric text box.
            // For some reason, these numeric textboxes often get a min and max of 0, meaning that you can't enter any value other than 0.
            // I was not able to figure out the cause of this, so I made this work around.
            var kendoNumericTextBox = e.container.find("input[data-type=number]").data("kendoNumericTextBox");
            if (kendoNumericTextBox) {
                kendoNumericTextBox.min(null);
                kendoNumericTextBox.max(null);
            }
        },
        save: function (e) {
            if (options.refreshGridAfterInlineEdit) {
                e.sender.one("dataBound", function () {
                    e.sender.dataSource.read();
                });
            }
        },
        filter: (event) => {filtersChanged = true;},
        change: dynamicItems.grids.onGridSelectionChange.bind(dynamicItems.grids)
    }, options);

    kendoGridOptions.editable = editable;
    kendoGridOptions.selectable = hideCheckboxColumn ? options.selectable : false;
    kendoGridOptions.toolbar = toolbar.length === 0 ? null : toolbar;
    kendoGridOptions.columns = columns;

    await window.dynamicItems.grids.loadGridViewColumnsState("sub_entities_grid_columns_{propertyId}", kendoGridOptions);
    if (kendoGridOptions.keepFiltersState !== false) {
        await window.dynamicItems.grids.loadGridViewFiltersState("sub_entities_grid_filters_{propertyId}", kendoGridOptions);
    }

    kendoComponent = field.kendoGrid(kendoGridOptions).data("kendoGrid");

    kendoComponent.thead.kendoTooltip({
        filter: "th",
        content: function (event) {
            var target = event.target; // element for which the tooltip is shown
            return $(target).text();
        }
    });

    if (!options.disableOpeningOfItems) {
        field.on("dblclick", "tbody tr[data-uid] td", function (event) {
            window.dynamicItems.grids.onShowDetailsClick(event, kendoComponent, options, false);
        });
    }

    kendoComponent.element.find(".k-grid-excel").click(function (event) {
        loader.addClass("loading");
    });

    dynamicItems.grids.attachSelectionCounter(field[0]);

    if (!customQueryGrid && dynamicItems.fieldTemplateFlags.enableSubEntitiesGridsOrdering && !options.fieldGroupName) {
        kendoComponent.table.kendoSortable({
            autoScroll: true,
            hint: function (element) {
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
            placeholder: function (element) {
                return element.clone().addClass("k-state-hover").css("opacity", 0.65);
            },
            container: "#overviewGrid{propertyIdWithSuffix}",
            filter: ">tbody >tr",
            change: function (e) {
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
                        view[i - 1].__ordering--;
                        view[i - 1].dirty = true;
                    }
                } else {
                    for (var i = oldIndex - 1; i >= newIndex; i--) {
                        view[i - 1].__ordering++;
                        view[i - 1].dirty = true;
                    }
                }

                kendoComponent.dataSource.sync();
            }
        });
    }

    {customScript}
}
})();
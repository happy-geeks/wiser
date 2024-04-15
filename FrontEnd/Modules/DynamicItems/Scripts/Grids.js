import { Wiser } from "../../Base/Scripts/Utils.js";
import "../../Base/Scripts/Processing.js";

require("@progress/kendo-ui/js/kendo.tooltip.js");
require("@progress/kendo-ui/js/kendo.button.js");
require("@progress/kendo-ui/js/kendo.dialog.js");
require("@progress/kendo-ui/js/kendo.grid.js");
require("@progress/kendo-ui/js/cultures/kendo.culture.nl-NL.js");
require("@progress/kendo-ui/js/messages/kendo.messages.nl-NL.js");

/**
 * Class for any and all functionality for grids.
 */
export class Grids {

    /**
     * Initializes a new instance of the Grids class.
     * @param {DynamicItems} base An instance of the base class (DynamicItems).
     */
    constructor(base) {
        this.base = base;

        this.mainGrid = null;
        this.mainGridFirstLoad = true;
        this.mainGridForceRecount = false;
    }

    /**
     * Do all initializations for the Grids class, such as adding bindings.
     */
    async initialize() {
        if (this.base.settings.gridViewMode && !this.base.settings.iframeMode) {
            this.base.settings.gridViewSettings = this.base.settings.gridViewSettings || {};

            const hideGrid = await this.setupInformationBlock();
            if (!hideGrid) {
                this.setupGridViewMode();
            }
        }
    }

    /**
     * Setup the main information block for when the module has gridViewMode enabled and the informationBlock enabled.
     * @returns {boolean} Whether or not the grid view should be hidden.
     */
    async setupInformationBlock() {
        let hideGrid = false;
        const informationBlockSettings = this.base.settings.gridViewSettings.informationBlock;

        if (!informationBlockSettings || !informationBlockSettings.initialItem) {
            return hideGrid;
        }

        this.base.settings.openGridItemsInBlock = informationBlockSettings.openGridItemsInBlock;
        this.base.settings.showSaveAndCreateNewItemButton = informationBlockSettings.showSaveAndCreateNewItemButton;
        this.base.settings.hideDefaultSaveButton = informationBlockSettings.hideDefaultSaveButton;

        const initialProcess = `loadInformationBlock_${Date.now()}`;

        try {
            window.processing.addProcess(initialProcess);
            const mainContainer = $("#wiser").addClass(`with-information-block information-${informationBlockSettings.position || "bottom"}`);
            const informationBlockContainer = $("#informationBlock").removeClass("hidden").addClass(informationBlockSettings.position || "bottom");
            if (informationBlockSettings.height) {
                informationBlockContainer.css("flex-basis", informationBlockSettings.height);
            }

            if (informationBlockSettings.width) {
                informationBlockContainer.css("width", informationBlockSettings.width);
                if (informationBlockSettings.width.indexOf("%") > -1) {
                    const informationBlockWidth = parseInt(informationBlockSettings.width.replace("%", ""));
                    $("#gridView").css("width", `${(100 - informationBlockWidth)}%`);
                    hideGrid = informationBlockWidth >= 100;
                    if (informationBlockWidth >= 100) {
                        informationBlockContainer.addClass("full");
                    }
                }
            }

            this.informationBlockIframe = $(`<iframe />`).appendTo(informationBlockContainer);
            this.informationBlockIframe[0].onload = (event) => {
                if (event.target.contentDocument.URL === "about:blank") {
                    return;
                }

                window.processing.removeProcess(initialProcess);

                dynamicItems.grids.informationBlockIframe[0].contentDocument.addEventListener("dynamicItems.onSaveButtonClick", () => {
                    if (!this.mainGrid || !this.mainGrid.dataSource) {
                        return;
                    }

                    this.mainGrid.dataSource.read();
                });

                if (this.base.settings.hideDefaultSaveButton) {
                    this.informationBlockIframe[0].contentWindow.$("#saveBottom").addClass("hidden").data("hidden-via-parent", true);
                }

                if (!this.base.settings.showSaveAndCreateNewItemButton) {
                    return;
                }

                this.informationBlockIframe[0].contentWindow.$("#saveAndCreateNewItemButton").removeClass("hidden").data("shown-via-parent", true).kendoButton({
                    click: async (event) => {
                        if (!(await this.informationBlockIframe[0].contentWindow.dynamicItems.onSaveButtonClick(event))) {
                            return false;
                        }

                        const createItemResult = await this.base.createItem(informationBlockSettings.initialItem.entityType, informationBlockSettings.initialItem.newItemParentId, "", null, [], true);
                        if (!createItemResult) {
                            return hideGrid;
                        }

                        const itemId = createItemResult.itemId;
                        this.informationBlockIframe.attr("src", `/Modules/DynamicItems?itemId=${itemId}&moduleId=${this.base.settings.moduleId}&iframe=true&readonly=${!!informationBlockSettings.initialItem.readOnly}&hideFooter=${!!informationBlockSettings.initialItem.hideFooter}&hideHeader=${!!informationBlockSettings.initialItem.hideHeader}`);
                    },
                    icon: "save"
                });
            };

            let itemId = informationBlockSettings.initialItem.itemId;
            if (!itemId) {
                const createItemResult = await this.base.createItem(informationBlockSettings.initialItem.entityType, informationBlockSettings.initialItem.newItemParentId, "", null, [], true);
                if (!createItemResult) {
                    return hideGrid;
                }
                itemId = createItemResult.itemId;
            }

            this.informationBlockIframe.attr("src", `/Modules/DynamicItems?itemId=${itemId}&moduleId=${this.base.settings.moduleId}&iframe=true&readonly=${!!informationBlockSettings.initialItem.readOnly}&hideFooter=${!!informationBlockSettings.initialItem.hideFooter}&hideHeader=${!!informationBlockSettings.initialItem.hideHeader}`);
        } catch (exception) {
            kendo.alert("Er is iets fout gegaan tijdens het laden van de data voor deze module. Sluit a.u.b. de module en probeer het nogmaals, of neem contact op met ons.");
            console.error(exception);
            window.processing.removeProcess(initialProcess);
        }

        return hideGrid;
    }

    /**
     * Setup the main grid for when the module has gridViewMode enabled.
     */
    async setupGridViewMode() {
        const initialProcess = `loadMainGrid_${Date.now()}`;

        try {
            window.processing.addProcess(initialProcess);
            let gridViewSettings = $.extend({}, this.base.settings.gridViewSettings);
            let gridDataResult;
            let previousFilters = gridViewSettings.keepFiltersState === false ? null : await this.loadGridViewState(`main_grid_filters_${this.base.settings.moduleId}`);

            const usingDataSelector = !!gridViewSettings.dataSelectorId;
            if (usingDataSelector) {
                gridDataResult = {
                    columns: gridViewSettings.columns,
                    pageSize: gridViewSettings.pageSize || 100,
                    data: await Wiser.api({
                        url: `${this.base.settings.getItemsUrl}?encryptedDataSelectorId=${encodeURIComponent(gridViewSettings.dataSelectorId)}`,
                        contentType: "application/json"
                    })
                };

                if (gridDataResult.data && gridDataResult.data.length > 0 && (!gridDataResult.columns || !gridDataResult.columns.length)) {
                    gridDataResult.columns = [];
                    let data = gridDataResult.data[0];
                    for (let key in data) {
                        if (!data.hasOwnProperty(key)) {
                            continue;
                        }

                        gridDataResult.columns.push({ title: key, field: key });
                    }
                }
            } else {
                const options = {
                    page: 1,
                    pageSize: gridViewSettings.pageSize || 100,
                    skip: 0,
                    take: gridViewSettings.clientSidePaging ? 0 : (gridViewSettings.pageSize || 100),
                    firstLoad: true
                };

                if (previousFilters) {
                    options.filter = JSON.parse(previousFilters);
                }
                else if (gridViewSettings.dataSource && gridViewSettings.dataSource.filter) {
                    options.filter = gridViewSettings.dataSource.filter;
                    previousFilters = JSON.stringify(options.filter);
                }

                gridDataResult = await Wiser.api({
                    url: `${this.base.settings.wiserApiRoot}modules/${encodeURIComponent(this.base.settings.moduleId)}/overview-grid`,
                    method: "POST",
                    contentType: "application/json",
                    data: JSON.stringify(options)
                });

                if (gridDataResult.extraJavascript) {
                    $.globalEval(gridDataResult.extraJavascript);
                }
            }

            let disableOpeningOfItems = gridViewSettings.disableOpeningOfItems;
            if (!disableOpeningOfItems) {
                if (gridDataResult.schemaModel && gridDataResult.schemaModel.fields) {
                    // If there is no field for encrypted ID, don't allow the user to open items, they'd just get an error.
                    disableOpeningOfItems = !(gridDataResult.schemaModel.fields.encryptedId || gridDataResult.schemaModel.fields.encrypted_id || gridDataResult.schemaModel.fields.encryptedid || gridDataResult.schemaModel.fields.idencrypted);
                }
            }

            if (!gridViewSettings.hideCommandColumn) {
                let commandColumnWidth = 80;
                const commands = [];


                if (!disableOpeningOfItems) {
                    commands.push({
                        name: "openDetails",
                        iconClass: "k-icon k-i-hyperlink-open",
                        text: "",
                        click: (event) => { this.base.grids.onShowDetailsClick(event, this.mainGrid, { customQuery: true, usingDataSelector: usingDataSelector, fromMainGrid: true }, false); }
                    });

                    if (gridViewSettings.allowOpeningOfItemsInNewTab) {
                        commandColumnWidth += 60;

                        commands.push({
                            name: "openDetailsInNewTab",
                            iconClass: "k-icon k-i-window",
                            text: "",
                            click: (event) => { this.base.grids.onShowDetailsClick(event, this.mainGrid, { customQuery: true, usingDataSelector: usingDataSelector, fromMainGrid: true }, true); }
                        });
                    }
                }

                if (gridViewSettings.deleteItemQueryId && (typeof (gridViewSettings.showDeleteButton) === "undefined" || gridViewSettings.showDeleteButton === true)) {
                    commandColumnWidth += 40;

                    const onDeleteClick = async (event) => {
                        const mainItemDetails = this.mainGrid.dataItem($(event.currentTarget).closest("tr")) || {};

                        if (!gridViewSettings || gridViewSettings.showDeleteConformations !== false) {
                            const itemName = mainItemDetails.title || mainItemDetails.name;
                            const deleteConfirmationText = itemName ? `het item '${itemName}'` : "het geselecteerde item";
                            await Wiser.showConfirmDialog(`Weet u zeker dat u ${deleteConfirmationText} wilt verwijderen?`)
                        }

                        await Wiser.api({
                            method: "POST",
                            url: `${this.base.settings.wiserApiRoot}items/${encodeURIComponent(mainItemDetails.encryptedId || mainItemDetails.encrypted_id || mainItemDetails.encryptedid || this.base.settings.zeroEncrypted)}/action-button/0?queryId=${encodeURIComponent(gridViewSettings.deleteItemQueryId)}&itemLinkId=${encodeURIComponent(mainItemDetails.linkId || mainItemDetails.linkId || 0)}`,
                            data: JSON.stringify(mainItemDetails),
                            contentType: "application/json"
                        });

                        this.mainGrid.dataSource.read();
                    };

                    commands.push({
                        name: "remove",
                        iconClass: "k-icon k-i-delete",
                        text: "",
                        click: onDeleteClick.bind(this)
                    });
                }
                else if (gridViewSettings.showDeleteButton === true) {
                    commandColumnWidth += 40;

                    commands.push({
                        name: "remove",
                        text: "",
                        iconClass: "k-icon k-i-delete",
                        click: (event) => { this.base.grids.onDeleteItemClick(event, this.mainGrid, "deleteItem", gridViewSettings); }
                    });
                }

                if (gridDataResult.columns) {
                    gridDataResult.columns.push({
                        title: "&nbsp;",
                        width: commandColumnWidth,
                        command: commands
                    });
                }
            }

            const toolbar = [];

            if (!gridViewSettings.toolbar || !gridViewSettings.toolbar.hideRefreshButton) {
                toolbar.push({
                    name: "refreshCustom",
                    iconClass: "k-icon k-i-refresh",
                    text: "",
                    template: `<a class='k-button k-button-icontext k-grid-refresh' href='\\#' title='Verversen'><span class='k-icon k-i-refresh'></span></a>`
                });
            }

            if (!gridViewSettings.toolbar || !gridViewSettings.toolbar.hideClearFiltersButton) {
                toolbar.push({
                    name: "clearAllFilters",
                    text: "",
                    template: `<a class='k-button k-button-icontext clear-all-filters' title='Alle filters wissen' href='\\#' onclick='return window.dynamicItems.grids.onClearAllFiltersClick(event)'><span class='k-icon k-i-filter-clear'></span></a>`
                });
            }

            if (!gridViewSettings.toolbar || !gridViewSettings.toolbar.hideCount) {
                toolbar.push({
                    name: "count",
                    iconClass: "",
                    text: "",
                    template: `<div class="counterContainer"><span class="counter">0</span> <span class="plural">resultaten</span><span class="singular" style="display: none;">resultaat</span></div>`
                });
            } else {
                toolbar.push({
                    name: "whitespace",
                    iconClass: "",
                    text: "",
                    template: `<div class="counterContainer"></div>`
                });
            }

            if (!gridViewSettings.toolbar || !gridViewSettings.toolbar.hideExportButton) {
                toolbar.push({
                    name: "excel"
                });
            }

            if ((!gridViewSettings.toolbar || !gridViewSettings.toolbar.hideCreateButton) && this.base.settings.permissions.canCreate) {
                toolbar.push({
                    name: "add",
                    text: "Nieuw",
                    template: `<a class='k-button k-button-icontext' href='\\#' onclick='return window.dynamicItems.dialogs.openCreateItemDialog(null, null, null, ${gridViewSettings.skipNameForNewItems})'><span class='k-icon k-i-file-add'></span>Nieuw item toevoegen</a>`
                });
            }

            if (gridViewSettings.toolbar && gridViewSettings.toolbar.customActions && gridViewSettings.toolbar.customActions.length > 0) {
                this.addCustomActionsToToolbar("#gridView", 0, 0, toolbar, gridViewSettings.toolbar.customActions);
            }

            let totalResults = gridDataResult.totalResults;

            // Setup filters. They are turned off by default, but can be turned on with default settings.
            let filterable = false;
            const defaultFilters = {
                extra: false,
                operators: {
                    string: {
                        startswith: "Begint met",
                        eq: "Is gelijk aan",
                        neq: "Is ongelijk aan",
                        contains: "Bevat",
                        doesnotcontain: "Bevat niet",
                        endswith: "Eindigt op",
                        "isnull": "Is leeg",
                        "isnotnull": "Is niet leeg"
                    }
                },
                messages: {
                    isTrue: "<span>Ja</span>",
                    isFalse: "<span>Nee</span>"
                }
            };

            if (gridViewSettings.filterable === true) {
                filterable = defaultFilters;
            } else if (typeof gridViewSettings.filterable === "object") {
                filterable = $.extend(true, {}, defaultFilters, gridViewSettings.filterable);
            } else if (gridViewSettings.clientSideFiltering === true) {
                filterable = defaultFilters;
            }

            // Delete properties that we have already defined, so that they won't be overwritten again by the $.extend below.
            delete gridViewSettings.filterable;
            delete gridViewSettings.toolbar;

            let columns = gridViewSettings.columns || [];
            if (columns) {
                if (gridDataResult.columns && gridDataResult.columns.length > 0) {
                    for (let column of gridDataResult.columns) {
                        const filtered = columns.filter(c => (c.field || "").toLowerCase() === (column.field || "").toLowerCase());
                        if (filtered.length > 0) {
                            continue;
                        }

                        columns.push(column);
                    }
                }

                if (columns.length === 0) {
                    columns = undefined; // So that Kendo auto generated the columns, it won't do that if we give an empty array.
                } else {
                    columns = columns.map(e => {
                        const result = e;
                        if (result.field) {
                            result.field = result.field.toLowerCase();
                        }
                        return result;
                    });
                }
            }

            let filtersChanged = false;
            const finalGridViewSettings = $.extend(true, {
                dataSource: {
                    serverPaging: !usingDataSelector && !gridViewSettings.clientSidePaging,
                    serverSorting: !usingDataSelector && !gridViewSettings.clientSideSorting,
                    serverFiltering: !usingDataSelector && !gridViewSettings.clientSideFiltering,
                    pageSize: gridDataResult.pageSize,
                    transport: {
                        read: async (transportOptions) => {
                            const process = `loadMainGrid_${Date.now()}`;

                            try {
                                if (this.mainGridFirstLoad) {
                                    transportOptions.success(gridDataResult);
                                    this.mainGridFirstLoad = false;
                                    window.processing.removeProcess(initialProcess);
                                    return;
                                }

                                if (!transportOptions.data) {
                                    transportOptions.data = {};
                                }

                                window.processing.addProcess(process);

                                // If we're using the same filters as before, we don't need to count the total amount of results again,
                                // so we tell the API whether this is the case, so that it can skip the execution of the count query, to make scrolling through the grid faster.
                                let currentFilters = null;
                                if (transportOptions.data.filter) {
                                    currentFilters = JSON.stringify(transportOptions.data.filter);
                                }

                                transportOptions.data.firstLoad = this.mainGridForceRecount || currentFilters !== previousFilters;
                                filtersChanged = currentFilters !== previousFilters;
                                transportOptions.data.pageSize = transportOptions.data.page_size || transportOptions.data.pageSize;
                                previousFilters = currentFilters;
                                this.mainGridForceRecount = false;

                                let newGridDataResult;
                                if (usingDataSelector) {
                                    newGridDataResult = {
                                        columns: gridViewSettings.columns,
                                        pageSize: gridViewSettings.pageSize || 100,
                                        data: await Wiser.api({
                                            url: `${this.base.settings.getItemsUrl}?encryptedDataSelectorId=${encodeURIComponent(gridViewSettings.dataSelectorId)}`,
                                            contentType: "application/json"
                                        })
                                    };
                                } else {
                                    newGridDataResult = await Wiser.api({
                                        url: `${this.base.settings.wiserApiRoot}modules/${encodeURIComponent(this.base.settings.moduleId)}/overview-grid`,
                                        method: "POST",
                                        contentType: "application/json",
                                        data: JSON.stringify(transportOptions.data)
                                    });
                                }

                                if (typeof newGridDataResult.totalResults !== "number" || !transportOptions.data.firstLoad) {
                                    newGridDataResult.totalResults = totalResults;
                                } else if (transportOptions.data.firstLoad) {
                                    totalResults = newGridDataResult.totalResults;
                                }

                                transportOptions.success(newGridDataResult);
                            } catch (exception) {
                                console.error(exception);
                                transportOptions.error(exception);
                                kendo.alert("Er is iets fout gegaan tijdens het laden van de data voor deze module. Sluit a.u.b. de module en probeer het nogmaals, of neem contact op met ons.");
                            }

                            window.processing.removeProcess(process);
                        }
                    },
                    schema: {
                        data: "data",
                        total: "totalResults",
                        model: gridDataResult.schemaModel
                    }
                },
                excel: {
                    fileName: "Module Export.xlsx",
                    filterable: true,
                    allPages: true
                },
                columnResize: (event) => this.saveGridViewColumnsState(`main_grid_columns_${this.base.settings.moduleId}`, event.sender),
                columnReorder: (event) => this.saveGridViewColumnsState(`main_grid_columns_${this.base.settings.moduleId}`, event.sender),
                columnHide: (event) => this.saveGridViewColumnsState(`main_grid_columns_${this.base.settings.moduleId}`, event.sender),
                columnShow: (event) => this.saveGridViewColumnsState(`main_grid_columns_${this.base.settings.moduleId}`, event.sender),
                dataBound: async (event) => {
                    const totalCount = event.sender.dataSource.total();
                    const counterContainer = event.sender.element.find(".k-grid-toolbar .counterContainer");
                    counterContainer.find(".counter").html(kendo.toString(totalCount, "n0"));
                    counterContainer.find(".plural").toggle(totalCount !== 1);
                    counterContainer.find(".singular").toggle(totalCount === 1);

                    // To hide toolbar buttons that require a row to be selected.
                    this.onGridSelectionChange(event);

                    if (gridViewSettings.keepFiltersState !== false && filtersChanged) {
                        await this.saveGridViewFiltersState(`main_grid_filters_${this.base.settings.moduleId}`, event.sender);
                    }
                },
                change: this.onGridSelectionChange.bind(this),
                resizable: true,
                sortable: true,
                scrollable: usingDataSelector ? true : {
                    virtual: true
                },
                filterable: filterable,
                filterMenuInit: this.onFilterMenuInit.bind(this),
                filterMenuOpen: this.onFilterMenuOpen.bind(this)
            }, gridViewSettings);

            finalGridViewSettings.selectable = gridViewSettings.selectable || false;
            finalGridViewSettings.toolbar = toolbar.length === 0 ? null : toolbar;
            finalGridViewSettings.columns = columns;

            if (previousFilters) {
                finalGridViewSettings.dataSource.filter = JSON.parse(previousFilters);
            }

            await this.loadGridViewColumnsState(`main_grid_columns_${this.base.settings.moduleId}`, finalGridViewSettings);

            await require("/kendo/messages/kendo.grid.nl-NL.js");

            this.mainGrid = $("#gridView").kendoGrid(finalGridViewSettings).data("kendoGrid");

            if (!disableOpeningOfItems) {
                this.mainGrid.element.on("dblclick", "tbody tr[data-uid] td", (event) => { this.base.grids.onShowDetailsClick(event, this.mainGrid, { customQuery: true, usingDataSelector: usingDataSelector, fromMainGrid: true }); });
            }
            this.mainGrid.element.find(".k-i-refresh").parent().click(this.base.onMainRefreshButtonClick.bind(this.base));
        } catch (exception) {
            kendo.alert("Er is iets fout gegaan tijdens het laden van de data voor deze module. Sluit a.u.b. de module en probeer het nogmaals, of neem contact op met ons.");
            console.error(exception);
            window.processing.removeProcess(initialProcess);
        }
    }

    /**
     * Save the column state of a grid. Depending on the settings, users can hide/show whichever columns in a grid that they want.
     * This method is to save that state so that the user's choices will be remembered.
     * @param key The key/name of the state that is being saved. This should be an unique key for every grid.
     * @param grid The grid to get the state of.
     * @returns {Promise<void>}
     */
    async saveGridViewColumnsState(key, grid) {
        try {
            const dataToSave = kendo.stringify(grid.getOptions().columns);
            await this.saveGridViewState(key, dataToSave);
        } catch (exception) {
            kendo.alert("Er is iets fout gegaan tijdens het opslaan van de instellingen voor dit grid. Probeer het nogmaals, of neem contact op met ons.");
            console.error(exception);
        }
    }

    /**
     * Save the filter state of a grid. Depending on the settings, users can hide/show whichever columns in a grid that they want.
     * This method is to save that state so that the user's choices will be remembered.
     * @param key The key/name of the state that is being saved. This should be an unique key for every grid.
     * @param grid The grid to get the state of.
     * @returns {Promise<void>}
     */
    async saveGridViewFiltersState(key, grid) {
        try {
            const filter = grid.dataSource.filter();
            const dataToSave = !filter ? null : kendo.stringify(filter);
            await this.saveGridViewState(key, dataToSave);
        } catch (exception) {
            kendo.alert("Er is iets fout gegaan tijdens het opslaan van de instellingen voor dit grid. Probeer het nogmaals, of neem contact op met ons.");
            console.error(exception);
        }
    }

    /**
     * Save a certain state of a grid view in session storage and in database.
     * @param key The key/name of the state that is being saved. This should be an unique key for every grid.
     * @param dataToSave The stringified state data to save.
     * @returns {Promise<void>} The promise of the request.
     */
    async saveGridViewState(key, dataToSave) {
        // Add the ID of the logged in user to the key for local storage. Just in case someone logs in as multiple users.
        let localStorageKey = key;
        const userData = await Wiser.getLoggedInUserData(this.base.settings.wiserApiRoot);
        if (userData) {
            localStorageKey += `_${userData.id}`;
        }
        sessionStorage.setItem(localStorageKey, dataToSave);

        return Wiser.api({
            url: `${this.base.settings.wiserApiRoot}users/grid-settings/${encodeURIComponent(key)}`,
            method: "POST",
            contentType: "application/json",
            data: dataToSave
        });
    }

    /**
     * Load a state for a grid. Will return the state as a string, that can be parsed as JSON.
     * @param key The name/key of the state to load.
     * @returns {Promise<string>} The state as a stringified JSON object.
     */
    async loadGridViewState(key) {
        let value;
        let localStorageKey = key;

        // Add the ID of the logged in user to the key for local storage. Just in case someone logs in as multiple users.
        const userData = await Wiser.getLoggedInUserData(this.base.settings.wiserApiRoot);
        if (userData) {
            localStorageKey += `_${userData.id}`;
        }

        value = sessionStorage.getItem(localStorageKey);
        if (!value) {
            value = await Wiser.api({
                url: `${this.base.settings.wiserApiRoot}users/grid-settings/${encodeURIComponent(key)}`,
                method: "GET",
                contentType: "application/json"
            });

            sessionStorage.setItem(localStorageKey, value || "");
        }

        return value;
    }

    /**
     * Load the saved state of columns back into a grid. Depending on the settings, users can hide/show whichever columns in a grid that they want.
     * This method is to save that state so that the user's choices will be remembered.
     * This method should be called BEFORE the grid is being initialized.
     * @param key The name/key of the state to load.
     * @param gridOptions The options object for the grid to load the state into.
     * @returns {Promise<void>}
     */
    async loadGridViewColumnsState(key, gridOptions) {
        const value = await this.loadGridViewState(key);

        if (!value) {
            return;
        }

        const columns = JSON.parse(value);
        if (!gridOptions || !gridOptions.columns || !gridOptions.columns.length) {
            return;
        }

        //Try to retreive and set all saved grid settings
        try {
            for (let savedColumnIndex=0; savedColumnIndex<columns.length; savedColumnIndex++) {
                for (let tableColumnsIndex=0; tableColumnsIndex<gridOptions.columns.length; tableColumnsIndex++) {
                    if (columns[savedColumnIndex].field === gridOptions.columns[tableColumnsIndex].field) {
                        gridOptions.columns[tableColumnsIndex].hidden = columns[savedColumnIndex].hidden;
                        gridOptions.columns[tableColumnsIndex].width = columns[savedColumnIndex].width;
                        if (gridOptions.reorderable ?? false) {
                            //Only re-arrange columns if there is a possibility for the user to arrange them
                            let moveColumn = gridOptions.columns.splice(tableColumnsIndex,1)[0];
                            gridOptions.columns.splice(savedColumnIndex, 0, moveColumn);                            
                        }
                        break;
                    }
                }
            }
        } catch (error) {
            console.error("Reading and setting grid settings failed:", error);
        }
    }

    /**
     * Load the saved state of filters back into a grid.
     * This method is to save that state so that the user's choices will be remembered.
     * @param key The name/key of the state to load.
     * @param gridOptions The options object for the grid to load the state into.
     * @returns {Promise<void>}
     */
    async loadGridViewFiltersState(key, gridOptions) {
        const value = await this.loadGridViewState(key);

        if (!value) {
            return;
        }

        gridOptions.dataSource.filter = JSON.parse(value);
    }

    async initializeItemsGrid(options, field, loader, itemId, height = undefined, propertyId = 0, extraData = null) {
        // TODO: Implement all functionality of all grids (https://app.asana.com/0/12170024697856/1138392544929161), so that we can use this method for everything.

        try {
            itemId = itemId || this.base.settings.zeroEncrypted;
            let customQueryGrid = options.customQuery === true;
            let kendoGrid;
            options.pageSize = options.pageSize || 25;

            const hideCheckboxColumn = !options.checkboxes || options.checkboxes === "false" || options.checkboxes <= 0;
            const gridOptions = {
                page: 1,
                pageSize: options.pageSize,
                skip: 0,
                take: options.pageSize,
                extraValuesForQuery: extraData
            };

            if (customQueryGrid) {
                const customQueryResults = await Wiser.api({
                    url: `${this.base.settings.wiserApiRoot}items/${encodeURIComponent(itemId)}/entity-grids/custom?mode=4&queryId=${options.queryId || this.base.settings.zeroEncrypted}&countQueryId=${options.countQueryId || this.base.settings.zeroEncrypted}`,
                    method: "POST",
                    contentType: "application/json",
                    data: JSON.stringify(gridOptions)
                });

                if (customQueryResults.extraJavascript) {
                    $.globalEval(customQueryResults.extraJavascript);
                }

                if (Wiser.validateArray(options.columns)) {
                    customQueryResults.columns = options.columns;
                }

                if (!hideCheckboxColumn) {
                    customQueryResults.columns.splice(0, 0, {
                        selectable: true,
                        width: "30px",
                        headerTemplate: "&nbsp;"
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
                    const commands = [];

                    if (!options.disableOpeningOfItems) {
                        commandColumnWidth += 60;

                        commands.push({
                            name: "openDetails",
                            iconClass: "k-icon k-i-hyperlink-open",
                            text: "",
                            click: (event) => { this.onShowDetailsClick(event, kendoGrid, options, false); }
                        });

                        if (options.allowOpeningOfItemsInNewTab) {
                            commandColumnWidth += 60;

                            commands.push({
                                name: "openDetailsInNewTab",
                                iconClass: "k-icon k-i-window",
                                text: "",
                                click: (event) => { this.onShowDetailsClick(event, kendoComponent, options, true); }
                            });
                        }
                    }

                    customQueryResults.columns.push({
                        title: "&nbsp;",
                        width: commandColumnWidth,
                        command: commands
                    });
                }

                if (options.allowMultipleRows) {
                    const checkBoxColumns = customQueryResults.columns.filter(c => c.selectable);
                    for (let checkBoxColumn of checkBoxColumns) {
                        delete checkBoxColumn.headerTemplate;
                    }
                }

                kendoGrid = await this.generateGrid(field, loader, options, customQueryGrid, customQueryResults, propertyId, height, itemId, extraData);
            } else {
                const gridSettings = await Wiser.api({
                    url: `${this.base.settings.wiserApiRoot}items/${itemId}/entity-grids/${encodeURIComponent(options.entityType)}?propertyId=${propertyId}&mode=1`,
                    method: "POST",
                    contentType: "application/json",
                    data: JSON.stringify(gridOptions)
                });

                if (gridSettings.extraJavascript) {
                    $.globalEval(gridSettings.extraJavascript);
                }

                // Add most columns here.
                if (gridSettings.columns && gridSettings.columns.length) {
                    for (let i = 0; i < gridSettings.columns.length; i++) {
                        var column = gridSettings.columns[i];

                        switch (column.field || "") {
                            case "":
                                column.hidden = hideCheckboxColumn;
                                if (!options.allowMultipleRows) {
                                    column.headerTemplate = "&nbsp;";
                                }
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
                            case "name":
                                column.hidden = options.hideTitleColumn || false;
                                break;
                        }
                    }
                }

                if (!options.disableOpeningOfItems) {
                    if (gridSettings.schemaModel && gridSettings.schemaModel.fields) {
                        // If there is no field for encrypted ID, don't allow the user to open items, they'd just get an error.
                        options.disableOpeningOfItems = !(gridSettings.schemaModel.fields.encryptedId || gridSettings.schemaModel.fields.encrypted_id || gridSettings.schemaModel.fields.encryptedid || gridSettings.schemModel.fields.idencrypted);
                    }
                }

                // Add command columns separately, because of the click event that we can't do properly server-side.
                if (!options.hideCommandColumn) {
                    let commandColumnWidth = 60;
                    let commands = [];

                    if (!options.disableOpeningOfItems) {
                        commands.push({
                            name: "openDetails",
                            iconClass: "k-icon k-i-hyperlink-open",
                            text: "",
                            click: (event) => { this.onShowDetailsClick(event, kendoGrid, options, false); }
                        });

                        if (options.allowOpeningOfItemsInNewTab) {
                            commandColumnWidth += 60;

                            commands.push({
                                name: "openDetailsInNewTab",
                                iconClass: "k-icon k-i-window",
                                text: "",
                                click: (event) => { this.onShowDetailsClick(event, kendoComponent, options, true); }
                            });
                        }
                    }

                    gridSettings.columns.push({
                        title: "&nbsp;",
                        width: commandColumnWidth,
                        command: commands
                    });
                }

                kendoGrid = await this.generateGrid(field, loader, options, customQueryGrid, gridSettings, propertyId, height, itemId, extraData);
            }

        } catch (exception) {
            console.error(exception);
            kendo.alert("Er is iets fout gegaan met het initialiseren van het overzicht. Probeer het a.u.b. nogmaals of neem contact op met ons.");
        }
    }

    async generateGrid(element, loader, options, customQueryGrid, data, propertyId, height, itemId, extraData) {
        // TODO: Implement all functionality of all grids (https://app.asana.com/0/12170024697856/1138392544929161), so that we can use this method for everything.
        let isFirstLoad = true;

        const columns = data.columns;
        if (columns && columns.length) {
            for (let column of columns) {
                if (column.field) {
                    column.field = column.field.toLowerCase();
                }

                if (!column.editor) {
                    continue;
                }

                column.editor = this[column.editor];
            }
        }

        const toolbar = [];
        if (!options.toolbar || !options.toolbar.hideClearFiltersButton) {
            toolbar.push({
                name: "clearAllFilters",
                text: "",
                template: `<a class='k-button k-button-icontext clear-all-filters' title='Alle filters wissen' href='\\#' onclick='return window.dynamicItems.grids.onClearAllFiltersClick(event)'><span class='k-icon k-i-filter-clear'></span></a>`
            });
        }
        if (!options.toolbar || !options.toolbar.hideFullScreenButton) {
            toolbar.push({
                name: "fullScreen",
                text: "",
                template: `<a class='k-button k-button-icontext full-screen' title='Grid naar fullscreen' href='\\#' onclick='return window.dynamicItems.grids.onMaximizeGridClick(event)'><span class='k-icon k-i-wiser-maximize'></span></a>`
            });
        }
        if (element.data("kendoGrid")) {
            element.data("kendoGrid").destroy();
            element.empty();
        }

        await require("/kendo/messages/kendo.grid.nl-NL.js");

        const kendoGrid = element.kendoGrid({
            dataSource: {
                transport: {
                    read: async (transportOptions) => {
                        try {
                            if (loader) {
                                loader.addClass("loading");
                            }

                            if (isFirstLoad) {
                                transportOptions.success(data);
                                isFirstLoad = false;
                                if (loader) {
                                    loader.removeClass("loading");
                                }
                                return;
                            }

                            if (!transportOptions.data) {
                                transportOptions.data = {};
                            }
                            transportOptions.data.extraValuesForQuery = extraData;
                            transportOptions.data.pageSize = transportOptions.data.pageSize;

                            if (customQueryGrid) {
                                const customQueryResults = await Wiser.api({
                                    url: `${this.base.settings.wiserApiRoot}items/${itemId}/entity-grids/custom?mode=4&queryId=${options.queryId || this.base.settings.zeroEncrypted}&countQueryId=${options.countQueryId || this.base.settings.zeroEncrypted}`,
                                    method: "POST",
                                    contentType: "application/json",
                                    data: JSON.stringify(transportOptions.data)
                                });

                                transportOptions.success(customQueryResults);

                                if (loader) {
                                    loader.removeClass("loading");
                                }
                            } else {
                                const gridSettings = await Wiser.api({
                                    url: `${this.base.settings.wiserApiRoot}items/${itemId}/entity-grids/${encodeURIComponent(options.entityType)}?propertyId=${propertyId}&mode=1`,
                                    method: "POST",
                                    contentType: "application/json",
                                    data: JSON.stringify(transportOptions.data)
                                });

                                transportOptions.success(gridSettings);

                                if (loader) {
                                    loader.removeClass("loading");
                                }
                            }
                        } catch (exception) {
                            console.error(exception);
                            if (loader) {
                                loader.removeClass("loading");
                            }
                            kendo.alert("Er is iets fout gegaan tijdens het laden van het veld '{title}'. Probeer het a.u.b. nogmaals door de pagina te verversen, of neem contact op met ons.");
                            transportOptions.error(exception);
                        }
                    }
                },
                serverPaging: true,
                serverSorting: true,
                serverFiltering: true,
                pageSize: options.pageSize || 10,
                schema: {
                    data: "data",
                    total: "totalResults",
                    model: data.schemaModel
                }
            },
            columns: columns,
            pageable: {
                pageSize: options.pageSize || 10,
                refresh: true
            },
            toolbar: toolbar,
            sortable: true,
            resizable: true,
            editable: false,
            navigatable: true,
            selectable: options.selectable || false,
            height: height,
            filterable: {
                extra: false,
                operators: {
                    string: {
                        startswith: "Begint met",
                        eq: "Is gelijk aan",
                        neq: "Is ongelijk aan",
                        contains: "Bevat",
                        doesnotcontain: "Bevat niet",
                        endswith: "Eindigt op"
                    }
                },
                messages: {
                    isTrue: "<span>Ja</span>",
                    isFalse: "<span>Nee</span>"
                }
            },
            filterMenuInit: this.base.grids.onFilterMenuInit.bind(this),
            filterMenuOpen: this.base.grids.onFilterMenuOpen.bind(this)
        }).data("kendoGrid");

        kendoGrid.thead.kendoTooltip({
            filter: "th",
            content: function (event) {
                const target = event.target; // element for which the tooltip is shown
                return $(target).text();
            }
        });

        if (!options.disableOpeningOfItems) {
            element.on("dblclick", "tbody tr[data-uid] td", (event) => { this.onShowDetailsClick(event, kendoGrid, options); });
        }

        if (!options.allowMultipleRows) {
            kendoGrid.tbody.on("click", ".k-checkbox", (event) => {
                var row = $(event.target).closest("tr");

                if (row.hasClass("k-state-selected")) {
                    setTimeout(() => {
                        kendoGrid.clearSelection();
                    });
                } else {
                    kendoGrid.clearSelection();
                }
            });
        }

        return kendoGrid;
    }

    /**
     * This method adds a counter to a grid.
     * This counter shows on the bottom right of the grid how many rows are selected.
     * @param {any} gridElement The div that contains the grid.
     */
    attachSelectionCounter(gridElement) {
        const onSelectionChange = () => {
            const num = $(gridElement).data("kendoGrid").select().length;
            const numSelectedElement = gridElement.querySelector(".numSelected");
            const newTxt = `${num} item${(num === 1 ? "" : "s")} geselecteerd`;

            if (numSelectedElement) {
                numSelectedElement.innerHTML = newTxt;
                if (num === 0) {
                    $(numSelectedElement).hide();
                } else {
                    $(numSelectedElement).show();
                }
            } else if (num > 0) {
                const pagerInfoElement = gridElement.querySelector(".k-pager-info");
                if (pagerInfoElement) {
                    const newEl = `<span class="k-pager-info k-label numSelected">${newTxt}</span>`;
                    pagerInfoElement.insertAdjacentHTML("afterEnd", newEl);
                }
            }
        };

        $(gridElement).data("kendoGrid").bind("change", onSelectionChange);
    }

    /**
     * Adds all custom buttons in the toolbar for a grid, in the correct groups, based on the given settings.
     * @param gridSelector {any} The selector to find the corresponding grid in the DOM.
     * @param {any} toolbar The toolbar array of the grid.
     * @param {string} encryptedItemId The encrypted item ID of the item that the grid is located on, if applicable.
     * @param {number} propertyId The ID of the property with the sub-entities-grid, if applicable.
     * @param {any} customActions The custom actions from the grid settings.
     * @param entityType {string} The entity type of the item that contains the grid.
     */
    addCustomActionsToToolbar(gridSelector, encryptedItemId, propertyId, toolbar, customActions, entityType) {
        const groups = [];
        const actionsWithoutGroups = [];
        encryptedItemId = encryptedItemId || "";
        propertyId = propertyId || "0";

        for (let i = 0; i < customActions.length; i++) {
            const customAction = customActions[i];
            const className = !customAction.allowNoSelection ? "hidden hide-when-no-selected-rows" : "";

            // Check permissions.
            if (customAction.doesCreate && !this.base.settings.permissions.canCreate) {
                continue;
            }
            if (customAction.doesUpdate && !this.base.settings.permissions.canWrite) {
                continue;
            }
            if (customAction.doesDelete && !this.base.settings.permissions.canDelete) {
                continue;
            }
            
            const conditionAttribute = customAction.condition
                ? `data-condition="${Misc.encodeHtml(customAction.condition)}"`
                : '';
            
            const selector = gridSelector.replace(/#/g, "\\#");
            
            const { condition, ...customActionData } = customAction;
            
            if (customAction.groupName) {
                let group = groups.filter(g => g.name === customAction.groupName)[0];
                if (!group) {
                    group = {
                        name: customAction.groupName,
                        icon: customAction.icon,
                        actions: []
                    };

                    groups.push(group);
                }
                
                group.actions.push(`<a class='k-button k-button-icontext ${className}' href='\\#' ${conditionAttribute} onclick='return window.dynamicItems.fields.onSubEntitiesGridToolbarActionClick("${selector}", "${encryptedItemId}", "${propertyId}", ${JSON.stringify(customActionData)}, event, "${entityType}")' style='${(kendo.htmlEncode(customAction.style || ""))}'><span>${customAction.text}</span></a>`);
            } else {
                actionsWithoutGroups.push({
                    name: `customAction${i.toString()}`,
                    text: customAction.text,
                    template: `<a class='k-button k-button-icontext ${className}' href='\\#' ${conditionAttribute} onclick='return window.dynamicItems.fields.onSubEntitiesGridToolbarActionClick("${selector}", "${encryptedItemId}", "${propertyId}", ${JSON.stringify(customActionData)}, event, "${entityType}")' style='${(kendo.htmlEncode(customAction.style || ""))}'><span class='k-icon k-i-${customAction.icon}'></span>${customAction.text}</a>`
                });
            }
        }

        // Always add the groups first.
        for (let group of groups) {
            toolbar.push({
                name: "buttonGroup",
                text: "",
                template: `<div class='k-button-drop'>
                            <span class='k-button-toggle k-button k-button-icontext'><span class='k-icon k-i-${group.icon}'></span>${group.name}</span>
                            <div>
                                ${group.actions.join("")}
                            </div>
                            </div>`
            });
        }

        // Add actions without groups last.
        toolbar.push(...actionsWithoutGroups);
    }

    /**
     * Event to show the details of an item from a sub entities grid.
     * @param {any} event The event.
     * @param {any} grid The grid that executed the event.
     * @param {any} options The options for the grid.
     * @param openInNewTab {boolean} Whether to open the item in a new tab in Wiser (like opening a new module).
     */
    async onShowDetailsClick(event, grid, options, openInNewTab = false) {
        event.preventDefault();

        const dataItem = grid.dataItem($(event.currentTarget).closest("tr"));
        const tableCell = $(event.currentTarget).closest("td");
        const column = grid.options.columns[tableCell.index()] || {};

        let itemId = dataItem.id || dataItem.itemId || dataItem.itemid || dataItem.item_id;
        let encryptedId = dataItem.encryptedId || dataItem.encrypted_id || dataItem.encryptedid || dataItem.idencrypted;
        const originalEncryptedId = encryptedId;
        let entityType = dataItem.entityType || dataItem.entity_type || dataItem.entitytype;
        let title = dataItem.title;
        const linkId = dataItem.linkId || dataItem.link_id || dataItem.linkid;
        const linkType = dataItem.linkTypeNumber || dataItem.link_type_number || dataItem.linktypenumber || dataItem.linkType || dataItem.link_type || dataItem.linktype;

        if (options.fromMainGrid && this.base.settings.openGridItemsInBlock) {
            this.base.grids.informationBlockIframe.attr("src", `${"/Modules/DynamicItems"}?itemId=${encryptedId}&moduleId=${this.base.settings.moduleId}&iframe=true`);
            return;
        }

        // If this grid uses a custom query, it means that we need to get the data a different way, because the grid can have data from multiple different entity types.
        if (options.customQuery === true) {
            if (!column.field) {
                // If the clicked column has no field property (such as the command column), use the item ID of the main entity type.
                itemId = dataItem[`ID_${options.entityType || entityType}`] || dataItem[`id_${options.entityType || entityType}`] || dataItem[`itemId_${options.entityType || entityType}`] || dataItem[`itemid_${options.entityType || entityType}`] || dataItem[`item_id_${options.entityType || entityType}`] || itemId;
                encryptedId = dataItem[`encryptedId_${options.entityType || entityType}`] || dataItem[`encryptedid_${options.entityType || entityType}`] || dataItem[`encrypted_id_${options.entityType || entityType}`] || dataItem[`idencrypted_${options.entityType || entityType}`] || encryptedId;
            } else if (!options.usingDataSelector) {
                // If the clicked column has a field property, it should contain the entity name. Then we can find the ID column for that same entity.
                const split = Strings.unmakeJsonPropertyName(column.field).split(/_(.+)/).filter(s => s !== "");
                if (split.length < 2 && !entityType) {
                    if (!options.hideCommandColumn && (!this.base.settings.gridViewSettings || !this.base.settings.gridViewSettings.hideCommandColumn)) {
                        console.error(`Could not retrieve entity type from clicked column ('${column.field}')`);
                        kendo.alert("Er is geen entiteittype gevonden voor de aangeklikte kolom. Neem a.u.b. contact op met ons.");
                    }

                    return;
                }

                let idFound = false;
                let encryptedIdFound = false;
                if (split.length >= 2) {
                    entityType = split[split.length - 1];

                    for (const key in dataItem) {
                        if (!dataItem.hasOwnProperty(key)) {
                            continue;
                        }

                        const columnName = Strings.unmakeJsonPropertyName(key);

                        if (!idFound && (columnName.indexOf(`ID_${entityType}`) === 0 || columnName.indexOf(`id_${entityType}`) === 0 || columnName.indexOf(`itemId_${entityType}`) === 0 || columnName.indexOf(`itemid_${entityType}`) === 0 || columnName.indexOf(`item_id_${entityType}`) === 0)) {
                            itemId = dataItem[key];
                            idFound = true;
                        }

                        if (!encryptedIdFound && (columnName.indexOf(`encryptedId_${entityType}`) === 0 || columnName.indexOf(`encryptedid_${entityType}`) === 0 || columnName.indexOf(`encrypted_id_${entityType}`) === 0 || columnName.indexOf(`idencrypted_${entityType}`) === 0)) {
                            encryptedId = dataItem[key];
                            encryptedIdFound = true;
                        }

                        if (encryptedIdFound && idFound) {
                            break;
                        }
                    }
                }
            }

            if (!encryptedId) {
                encryptedId = originalEncryptedId;
            }

            if (!encryptedId) {
                if (!options.hideCommandColumn && (!this.base.settings.gridViewSettings || !this.base.settings.gridViewSettings.hideCommandColumn)) {
                    kendo.alert("Er is geen encrypted ID gevonden. Neem a.u.b. contact op met ons.");
                }
                return;
            }

            if (!title || !itemId || !entityType) {
                const itemDetails = (await this.base.getItemDetails(encryptedId, entityType));
                if (!itemDetails) {
                    kendo.alert("Er is geen item gevonden met het id in de geselecteerde regel. Waarschijnlijk is dit geen geldig ID. Neem a.u.b. contact op met ons.");
                    return;
                }

                title = title || itemDetails.title;
                itemId = itemId || itemDetails.id || itemDetails.itemId;
                entityType = entityType || itemDetails.entityType;
            }
        }

        if (!encryptedId) {
            kendo.alert("Er is geen encrypted ID gevonden. Neem a.u.b. contact op met ons.");
            return;
        }

        if (openInNewTab) {
            if (!window.parent) {
                kendo.alert("Er kan geen parent frame gevonden worden. Waarschijnlijk heeft u deze module in een losse browser tab geopend. Open de module a.u.b. via de normale manier in Wiser.")
                return;
            }

            window.parent.postMessage({
                action: "OpenItem",
                actionData: {
                    moduleId: this.base.settings.moduleId,
                    name: title || `Item #${itemId}`,
                    type: "dynamicItems",
                    itemId: encryptedId,
                    queryString: `?itemId=${encodeURIComponent(encryptedId)}&moduleId=${this.base.settings.moduleId}&iframe=true&entityType=${entityType}`
                }
            });
        }
        else {
            this.base.windows.loadItemInWindow(false, itemId, encryptedId, entityType, title, !options.hideTitleFieldInWindow, grid, options, linkId, null, null, linkType);
        }
    }

    /**
     * Event that gets fired when clicking the link sub entity button in a sub-entities-grid.
     * This will open a window with a grid that contains all entities of a certain type.
     * The user can use checkboxes in that grid to link items.
     * @param {any} encryptedParentId The encrypted item ID of the parent to link the items to.
     * @param {any} plainParentId The plain item ID of the parent to link the items to.
     * @param {any} entityType The entity type of items to show in the search window.
     * @param {any} senderGridSelector A selector to find the sender grid.
     * @param {any} linkTypeNumber The link type number.
     * @param {boolean} hideIdColumn Indicates whether or not to hide the ID column.
     * @param {boolean} hideLinkIdColumn Indicates whether or not to hide the link ID column.
     * @param {boolean} hideTypeColumn Indicates whether or not to hide the type column.
     * @param {boolean} hideEnvironmentColumn Indicates whether or not to hide the environment column.
     * @param {boolean} hideTitleColumn Indicates whether or not to hide the title column.
     * @param {number} propertyId The ID of the current property.
     * @param {any} gridOptions The options of the grid.
     */
    onLinkSubEntityClick(encryptedParentId, plainParentId, entityType, senderGridSelector, linkTypeNumber, hideIdColumn, hideLinkIdColumn, hideTypeColumn, hideEnvironmentColumn, hideTitleColumn, propertyId, gridOptions) {
        linkTypeNumber = linkTypeNumber || "";
        if (typeof gridOptions === "string") {
            gridOptions = JSON.parse(gridOptions);
        }

        this.base.windows.searchItemsWindow.maximize().open();
        this.base.windows.searchItemsWindow.title(`${this.base.getEntityTypeFriendlyName(entityType)} zoeken en koppelen`);
        this.base.windows.initializeSearchItemsGrid(entityType, encryptedParentId, propertyId, gridOptions);
        $.extend(this.base.windows.searchItemsWindowSettings, {
            parentId: encryptedParentId,
            plainParentId: plainParentId,
            senderGrid: $(senderGridSelector).data("kendoGrid"),
            entityType: entityType,
            linkTypeNumber: linkTypeNumber,
            propertyId: propertyId,
            currentItemIsSourceId: gridOptions.currentItemIsSourceId,
            setOrdering: gridOptions.toolbar.linkItemsSetOrdering
        });
        $.extend(this.base.windows.searchGridSettings, {
            hideIdColumn: hideIdColumn,
            hideLinkIdColumn: hideLinkIdColumn,
            hideTypeColumn: hideTypeColumn,
            hideEnvironmentColumn: hideEnvironmentColumn,
            hideTitleColumn: hideTitleColumn,
            propertyId: propertyId,
            enableSelectAllServerSide: gridOptions.searchGridSettings.enableSelectAllServerSide,
            currentItemIsSourceId: gridOptions.currentItemIsSourceId
        });
    }

    /**
     * Event that gets executed when the user clicks the button to create a new sub entity inside a sub-entities-grid.
     * This will create a new item and then open a popup where the user can edit the values with.
     * @param {string} parentId The encrypted ID of the parent to link the item to. This is usually the item that contains the sub-entities-grid.
     * @param {string} entityType The type of entity to create.
     * @param {string} senderGridSelector The CSS selector to find the main element for the sub-entities-grid.
     * @param {boolean} showTitleField Whether or not to show the field where the user can edit the title/name of the new item.
     * @param {number} linkTypeNumber The link type number.
     */
    async onNewSubEntityClick(parentId, entityType, senderGridSelector, showTitleField, linkTypeNumber) {
        linkTypeNumber = linkTypeNumber || "";

        const senderGrid = $(senderGridSelector).data("kendoGrid");
        if (senderGrid) {
            senderGrid.element.siblings(".grid-loader").addClass("loading");
        }

        try {
            // Create the new item.
            const createItemResult = await this.base.createItem(entityType, parentId, "", linkTypeNumber);
            if (createItemResult) {
                await this.base.windows.loadItemInWindow(true, createItemResult.itemIdPlain, createItemResult.itemId, entityType, null, showTitleField, senderGrid, { hideTitleColumn: !showTitleField }, createItemResult.linkId, `Nieuw(e) ${this.base.getEntityTypeFriendlyName(entityType)} aanmaken`);
            }
        } catch (exception) {
            console.error(exception);
            let error = exception;
            if (exception.responseText) {
                error = exception.responseText;
            } else if (exception.statusText) {
                error = exception.statusText;
            }
            kendo.alert(`Er is iets fout gegaan met het aanmaken van het item. Probeer het a.u.b. nogmaals of neem contact op met ons.<br><br>De fout was:<br><pre>${kendo.htmlEncode(error)}</pre>`);
        }

        if (senderGrid) {
            senderGrid.element.siblings(".grid-loader").removeClass("loading");
        }
    }

    /**
     * Event for deleting an item and/or item link.
     * It gets executed when the user clicks the delete button in a sub entities grid.
     * @param {any} event The click event of the delete button.
     * @param {any} senderGrid The sender grid.
     * @param {string} deletionType The deletion type. Possible values: "askUser", "deleteItem" or "deleteLink".
     * @param {any} options The options of the field/property.
     */
    async onDeleteItemClick(event, senderGrid, deletionType, options) {
        deletionType = deletionType || "";
        // prevent page scroll position change
        event.preventDefault();
        // e.target is the DOM element representing the button
        const tr = $(event.target).closest("tr"); // get the current table row (tr)
        // get the data bound to the current table row
        const dataItem = senderGrid.dataItem(tr);
        let encryptedId = dataItem.encryptedId || dataItem.encrypted_id || dataItem.encryptedid;
        let itemId = dataItem.itemId || dataItem.item_id || dataItem.id;
        let selectedItemDetails = {};

        if (!encryptedId) {
            // If the clicked column has no field property (such as the command column), use the item ID of the main entity type.
            const itemId = dataItem[`ID_${options.entityType}`] || dataItem[`id_${options.entityType}`] || dataItem[`itemId_${options.entityType}`] || dataItem[`itemid_${options.entityType}`] || dataItem[`item_id_${options.entityType}`];

            if (!itemId) {
                kendo.alert(`Er is geen encrypted ID gevonden voor dit item. Neem a.u.b. contact op met ons.`);
                return;
            }

            selectedItemDetails = (await this.base.getItemDetails(itemId)) || {};
            encryptedId = selectedItemDetails.encryptedId || selectedItemDetails.encrypted_id || selectedItemDetails.encryptedid;
        }

        const itemTitleForDeleteDialog = dataItem.title || dataItem.name || selectedItemDetails.title || dataItem.id || selectedItemDetails.id;
        let itemDeleteDialogText = itemTitleForDeleteDialog ? `het item '${itemTitleForDeleteDialog}'` : "het geselecteerde item";
        switch (deletionType.toLowerCase()) {
            case "askuser":
                {
                    const dialog = $("#gridDeleteDialog");

                    dialog.kendoDialog({
                        title: "Verwijderen",
                        closable: false,
                        modal: true,
                        content: `<p>Wilt u ${itemDeleteDialogText} in het geheel verwijderen, of alleen de koppeling tussen de 2 items?</p>`,
                        deactivate: (e) => {
                            // Destroy the dialog on deactivation so that it can be re-initialized again next time.
                            // If we don't do this, deleting multiple items in a row will not work properly.
                            e.sender.destroy();
                        },
                        actions: [
                            {
                                text: "Annuleren"
                            },
                            {
                                text: "Gehele item",
                                action: (e) => {
                                    try {
                                        this.base.deleteItem(encryptedId, options.entityType).then(() => {
                                            senderGrid.dataSource.read();
                                        });
                                    } catch (exception) {
                                        console.error(exception);
                                        if (exception.status === 409) {
                                            const message = exception.responseText || "Het is niet meer mogelijk om dit item te verwijderen.";
                                            kendo.alert(message);
                                        } else {
                                            kendo.alert("Er is iets fout gegaan tijdens het verwijderen van dit item. Probeer het a.u.b. nogmaals of neem contact op met ons.");
                                        }
                                    }
                                }
                            },
                            {
                                text: "Alleen koppeling",
                                primary: true,
                                action: (e) => {
                                    const destinationItemId = dataItem.encryptedDestinationItemId || senderGrid.element.closest(".item").data("itemIdEncrypted");
                                    const linkType = dataItem.linkTypeNumber || dataItem.link_type_number || dataItem.linktypenumber || dataItem.linkType || dataItem.link_type || dataItem.linktype;
                                    this.base.removeItemLink(options.currentItemIsSourceId ? destinationItemId : encryptedId, options.currentItemIsSourceId ? encryptedId : destinationItemId, linkType).then(() => {
                                        senderGrid.dataSource.read();
                                    });
                                }
                            }
                        ]
                    }).data("kendoDialog").open();

                    break;
                }
            case "deleteitem":
                {
                    if (!options || options.showDeleteConformations !== false) {
                        await Wiser.showConfirmDialog(`Weet u zeker dat u ${itemDeleteDialogText} wilt verwijderen?`);
                    }

                    try {
                        await this.base.deleteItem(dataItem.encryptedId || dataItem.encrypted_id || dataItem.encryptedid, options.entityType);
                    } catch (exception) {
                        console.error(exception);
                        if (exception.status === 409) {
                            const message = exception.responseText || "Het is niet meer mogelijk om dit item te verwijderen.";
                            kendo.alert(message);
                        } else {
                            kendo.alert("Er is iets fout gegaan tijdens het verwijderen van dit item. Probeer het a.u.b. nogmaals of neem contact op met ons.");
                        }
                    }
                    senderGrid.dataSource.read();
                    break;
                }
            case "deletelink":
                {
                    if (!options || options.showDeleteConformations !== false) {
                        await Wiser.showConfirmDialog(`Weet u zeker dat u de koppeling met ${itemDeleteDialogText} wilt verwijderen? Let op dat alleen de koppeling wordt verwijderd, niet het item zelf.`);
                    }

                    const destinationItemId = dataItem.encryptedDestinationItemId || dataItem.encrypted_destination_item_id || senderGrid.element.closest(".item").data("itemIdEncrypted");
                    const linkType = dataItem.linkTypeNumber || dataItem.link_type_number || dataItem.linktypenumber || dataItem.linkType || dataItem.link_type || dataItem.linktype;
                    await this.base.removeItemLink(options.currentItemIsSourceId ? destinationItemId : encryptedId, options.currentItemIsSourceId ? encryptedId : destinationItemId, linkType);
                    senderGrid.dataSource.read();
                    break;
                }
            default:
                {
                    console.warn(`onGridDeleteItemClick with unsupported deletionType '${deletionType}'`);
                    break;
                }
        }
    }

    onItemLinkerSelectAll(treeViewSelector, checkAll) {
        const treeView = $(treeViewSelector);

        Wiser.showConfirmDialog(`Weet u zeker dat u alles wilt ${checkAll ? "aanvinken" : "uitvinken"}? Indien dit veel items zijn kan dit lang duren.`,
            checkAll ? "Alles aanvinken" : "Alles uitvinken",
            "Annuleren",
            checkAll ? "Alles aanvinken" : "Alles uitvinken").then(() => {
            const allCheckBoxes = treeView.find(".k-checkbox-wrapper input");
            allCheckBoxes.prop("checked", checkAll).trigger("change");
        });
    }

    /**
     * This is for handling the event 'filterMenuInit' in a Kendo grid.
     * It will set the formatting of numeric fields and maybe other things in the future.
     * @param {any} event The kendo event.
     */
    onFilterMenuInit(event) {
        // Set the format of numeric fields, otherwise numbers will be shown like '3.154.079,00' instead of '3154079'.
        event.container.find("[data-role='numerictextbox']").each((index, element) => {
            $(element).data("kendoNumericTextBox").setOptions({
                format: "0"
            });
        });
    }

    /**
     * This is for handling the event 'filterMenuOpen' in a Kendo grid.
     * @param {any} event The kendo event.
     */
    onFilterMenuOpen(event) {
        // Set the focus on the last textbox in the filter menu.
        event.container.find(".k-textbox:visible, .k-input:visible").last().focus();
    }

    /**
     * Event handler for clicking the clear all filters button in a grid.
     * @param {any} event The click event.
     */
    onClearAllFiltersClick(event) {
        event.preventDefault();

        const grid = $(event.target).closest(".k-grid").data("kendoGrid");
        if (!grid) {
            console.error("Grid not found, cannot clear filters.", event, $(event.target).closest(".k-grid"));
            return;
        }

        grid.dataSource.filter({});
        // manually trigger the filter event to save the state because the above call doesn't do so
        grid.trigger("filter", { filter: null, field: null });
    }

    /**
     * Event handler for clicking the maximize button in a grid.
     * @param {any} event The click event.
     */
    onMaximizeGridClick(event) {
        event.preventDefault();

        const grid = $(event.target).closest(".k-grid").data("kendoGrid");
        if (!grid) {
            console.error("Grid not found, cannot maximize it.", event, $(event.target).closest(".k-grid"));
            return;
        }

        const originalParent = grid.wrapper.parent();
        const gridWindow = $("#maximizeSubEntitiesGridWindow").clone(true);
        const titleElement = originalParent.find("h4");

        // Move the grid to the window.
        gridWindow.find(".k-content-frame").append(grid.wrapper);

        gridWindow.kendoWindow({
            width: "100%",
            height: "100%",
            title: titleElement.find("label").text(),
            close: (closeEvent) => {
                // Move the grid back to it's original position when closing the full screen window.
                titleElement.after(grid.wrapper);

                // Destroy the window.
                closeEvent.sender.destroy();
                gridWindow.remove();
            }
        });

        const kendoWindow = gridWindow.data("kendoWindow").center().open();
    }

    /**
     * Event handler for when a user (de)selects one or more rows in a Kendo UI grid.
     * @param {any} event
     */
    async onGridSelectionChange(event) {
        // Check based on given condition to hide.
        const conditionalButtons = event.sender.wrapper.find('.k-button.hide-when-no-selected-rows');
        conditionalButtons.each(async function () {
            const button = $(this);
            const condition = button.data('condition');
            
            // Do not hide buttons by default.
            let shouldHide = false;
            
            // Conditional check.
            if(condition) {
                const decodedCondition = Misc.decodeHtml(condition);
                
                // Gather field data for each selected row in the grid.
                const selectedData = [];
                event.sender.wrapper.find('tr.k-state-selected').each(function() {
                    const row = $(this);
                    const grid = row.closest('.k-grid').data('kendoGrid');
                    const rowData = grid.dataItem(row);
                    selectedData.push(rowData);
                });

                // Evaluate the condition for every selected row in the grid.
                shouldHide = !selectedData.every(function(element, index, array) {
                    const parameterNames = Object.keys(element);
                    const parameterValues = Object.values(element);

                    const func = new Function(...parameterNames, `return ${decodedCondition}`);
                    return func(...parameterValues);
                });
            }
            
            // Show or hide the action button based on the evaluated condition or default value.
            button.toggleClass('hidden', shouldHide || event.sender.select().length === 0);
        });
        
        // Check whether to hide a button group when no buttons are visible in the group.
        for (let buttonGroup of event.sender.wrapper.find(".k-button-drop")) {
            const buttonGroupElement = $(buttonGroup);
            const totalAmountOfButtons = buttonGroupElement.find("a.k-button:not(.hidden)").length;
            buttonGroupElement.toggleClass("hidden", totalAmountOfButtons === 0);
        }
    }

    timeEditor(container, options) {
        $(`<input data-text-field="${options.field}" data-value-field="${options.field}" data-bind="value:${options.field}" data-format="${options.format}"/>`)
            .appendTo(container)
            .kendoTimePicker({});
    }

    dateTimeEditor(container, options) {
        $(`<input data-text-field="${options.field}" data-value-field="${options.field}" data-bind="value:${options.field}" data-format="${options.format}"/>`)
            .appendTo(container)
            .kendoDateTimePicker({});
    }

    booleanEditor(container, options) {
        const guid = kendo.guid();

        $(`<label class="checkbox"><input type="checkbox" id="${guid}" class="textField k-input" name="${options.field}" data-type="boolean" data-bind="checked:${options.field}" /><span></span></label>`)
            .appendTo(container);
    }
}
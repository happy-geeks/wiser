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
     * @param {VersionControl} base An instance of the base class (DynamicItems).
     */
    constructor(base) {
        this.base = base;

        this.templateChangesGrid = null;
    }
    
    async initialize() {
        /*this.base.settings.gridViewOptionParse = this.base.settings.gridViewOptionParse || {};   
        await this.getModuleGridData(6001);*/
        
        // noinspection ES6MissingAwait
        this.setupTemplateChangesGrid();
    }
    
    async setupTemplateChangesGrid() {
        try {
            const gridSettings = {
                dataSource: {
                    transport: {
                        read: async (transportOptions) => {
                            const initialProcess = `GetTemplatesToCommit_${Date.now()}`;
                            window.processing.addProcess(initialProcess);
                            
                            try {
                                const templatesToCommit = await Wiser.api({
                                    url: `${this.base.settings.wiserApiRoot}version-control/templates-to-commit`,
                                    method: "GET",
                                    contentType: "application/json"
                                });
    
                                console.log("templatesToCommit", templatesToCommit);
                                transportOptions.success(templatesToCommit);
                            } catch (exception) {
                                console.error(exception);
                                kendo.alert("Er is iets fout gegaan met het laden van de wijzigingen in templates. Sluit a.u.b. deze module, open deze daarna opnieuw en probeer het vervolgens opnieuw. Of neem contact op als dat niet werkt.");
                                transportOptions.error(exception);
                            }

                            window.processing.removeProcess(initialProcess);
                        }
                    },
                    schema: {
                        model: {
                            id: "id",
                            fields: {
                                changedOn: {
                                    type: "date"
                                }
                            }
                        }
                    }
                },
                columns: [
                    {
                        "selectable": "true",
                        "width": "50px"
                    },
                    {
                        "field": "templateId",
                        "title": "ID",
                        "width": "100px"
                    },
                    {
                        "field": "templateType",
                        "title": "Type",
                        "width": "150px"
                    },
                    {
                        "field": "templateParentName",
                        "title": "Map",
                        "width": "150px"
                    },
                    {
                        "field": "templateName",
                        "title": "Template"
                    },
                    {
                        "field": "version",
                        "title": "Versie",
                        "width": "150px"
                    },
                    {
                        "field": "versionTest",
                        "title": "Versie test",
                        "width": "150px"
                    },
                    {
                        "field": "versionAcceptance",
                        "title": "Versie acceptatie",
                        "width": "150px"
                    },
                    {
                        "field": "versionLive",
                        "title": "Versie live",
                        "width": "150px"
                    },
                    {
                        "field": "changedOn",
                        "format": "{0:F}",
                        "title": "Datum",
                        "width": "300px"
                    },
                    {
                        "field": "changedBy",
                        "title": "Door"
                    }
                ]
            };
            
            this.templateChangesGrid = $("#templateChangesGrid").kendoGrid(gridSettings).data("kendoGrid");
            
        } catch (exception) {
            console.error(exception);
            kendo.alert("Er is iets fout gegaan met het laden van de wijzigingen in templates. Sluit a.u.b. deze module, open deze daarna opnieuw en probeer het vervolgens opnieuw. Of neem contact op als dat niet werkt.");
        }
    }

    async getModuleGridData(moduleId) {
        try {
            const moduleGridSettings = await Wiser.api({
                url: `${this.base.settings.wiserApiRoot}version-control/module-gird-settings/${moduleId}`,
                method: "GET",
                contentType: "application/json",
            });

            for (const [key, value] of Object.entries(moduleGridSettings)) {

                var customQuery = value["customQuery"];
                var countQuery = value["countQuery"];
                var gridOptions = value["gridOptions"];
                var gridDivId = value["gridDivId"];
                var gridReadOptions = value["gridReadOptions"];
        
                await this.setupGridViewMode(customQuery, countQuery, gridOptions, gridDivId, gridReadOptions);
            } 

            this.gridsAreLoaded = true;

        } catch (exception) {
            console.error(exception);
            kendo.alert("Er is iets fout gegaan. Sluit a.u.b. deze module, open deze daarna opnieuw en probeer het vervolgens opnieuw. Of neem contact op als dat niet werkt.");
        } finally {
        }
    }

    async setupGridViewMode(customQuery,countQuery, gridOptions, gridViewId, gridReadOptions) {

        var gridViewOptionParse = JSON.parse(gridOptions);

        const initialProcess = `loadMainGrid_${Date.now()}`;

        try {
            window.processing.addProcess(initialProcess);
           
            const gridViewOptionsSettings = JSON.parse(gridOptions);

            let gridViewSettings = gridViewOptionsSettings.gridViewSettings;
            let gridViewOptionParse = $.extend({}, this.base.settings.gridViewOptionParse);
            let gridDataResult;
            let previousFilters = null;

            const usingDataSelector = !!gridViewOptionParse.dataSelectorId;

            var options;

            if (gridReadOptions == "") {
                options = {
                    page: 1,
                    pageSize: gridViewOptionParse.pageSize || 100,
                    skip: 0,
                    take: gridViewOptionParse.clientSidePaging ? 0 : (gridViewOptionParse.pageSize || 100),
                    firstLoad: true
                }
            } else {
                options = JSON.parse(gridReadOptions);
            }
           

            const gridOptionsData = {
                customQuery: customQuery,
                countQuery: countQuery,
                gridOptions: gridOptions,
                gridReadOptions: options
            }
  

            if (gridViewOptionParse.dataSource && gridViewOptionParse.dataSource.filter) {
                options.filter = gridViewOptionParse.dataSource.filter;
                previousFilters = JSON.stringify(options.filter);
            }
                gridDataResult = await Wiser.api({
                    url: `${this.base.settings.wiserApiRoot}version-control/${encodeURIComponent(this.base.settings.moduleId)}/overview-grid`,
                    method: "POST",
                    contentType: "application/json",
                    data: JSON.stringify(gridOptionsData)
                });


                if (gridDataResult.extraJavascript) {
                    $.globalEval(gridDataResult.extraJavascript);
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
                        click: (event) => { this.base.grids.onShowDetailsClick(event, this.mainGrid, { customQuery: true, usingDataSelector: usingDataSelector, fromMainGrid: true }); }
                    });
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
               
            }

            const toolbar = [];

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
            }

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
                    columns = undefined; 
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
                                if (this.mainGridFirstLoad && this.gridsAreLoaded == false) {
                                    transportOptions.success(gridDataResult);
                                    
                                    window.processing.removeProcess(initialProcess);
                                    return;
                                }
                                //get wich grid you are sorting
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
                                transportOptions.data.pageSize = transportOptions.data.pageSize;
                                previousFilters = currentFilters;
                                this.mainGridForceRecount = false;
                                
                                let newGridDataResult;
                                let newGridDataResult1;
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

                                    var uid = finalGridViewSettings.dataSource.fields[0].uid;
                                    var uidElement = document.getElementById(uid);
                                    var table = uidElement.closest("[data-role='grid']");
                                    var id = table.id;
                               
                                    newGridDataResult1 = await Wiser.api({
                                       
                                        url: `${this.base.settings.wiserApiRoot}version-control/${id}/overview-grid`,
                                        method: "POST",
                                        contentType: "application/json",
                                        data: JSON.stringify(transportOptions.data)
                                    });
                                    
                                }

                                if (typeof newGridDataResult1.totalResults !== "number" || !transportOptions.data.firstLoad) {
                                    newGridDataResult1.totalResults = totalResults;
                                } else if (transportOptions.data.firstLoad) {
                                    totalResults = newGridDataResult1.totalResults;
                                }

                                transportOptions.success(newGridDataResult1);

                                if (gridViewId == "#deploygrid") {

                                    var grid = document.getElementById("deploygrid").getElementsByTagName("tbody").item(0);

                                    for (var i = 0; i < grid.childElementCount; i++) {
                                        var gridRow = grid.childNodes.item(i);
                                        var contentDataRow = gridRow.querySelector('[data-field="content_data"]');
                                        var data = contentDataRow.innerHTML

                                        var editedString = data.replaceAll(";", "<br>");
                                        grid.childNodes.item(i).querySelector('[data-field="content_data"]').innerHTML = editedString;
                                    }

                                }

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
                
                dataBound: (event) => {
                    const totalCount = event.sender.dataSource.total();
                    const counterContainer = event.sender.element.find(".k-grid-toolbar .counterContainer");
                    counterContainer.find(".counter").html(kendo.toString(totalCount, "n0"));
                    counterContainer.find(".plural").toggle(totalCount !== 1);
                    counterContainer.find(".singular").toggle(totalCount === 1);

                    
                },
                
                resizable: true,
                sortable: true,
                scrollable: usingDataSelector ? true : {
                    virtual: true
                },
                filterable: filterable
            }, gridViewSettings);

            finalGridViewSettings.selectable = gridViewSettings.selectable || false;
            finalGridViewSettings.toolbar = toolbar.length === 0 ? null : toolbar;
            finalGridViewSettings.columns = columns;

            this.mainGrid = $(gridViewId).kendoGrid(finalGridViewSettings).data("kendoGrid");
  
            await this.loadGridViewState(`main_grid_columns_${this.base.settings.moduleId}`, this.mainGrid);

            if (!disableOpeningOfItems) {
                this.mainGrid.element.on("dblclick", "tbody tr[data-uid] td", (event) => { this.base.grids.onShowDetailsClick(event, this.mainGrid, { customQuery: true, usingDataSelector: usingDataSelector, fromMainGrid: true }); });
            }

            if (gridViewId == "#deploygrid") {

                var grid = document.getElementById("deploygrid").getElementsByTagName("tbody").item(0);

                for (var i = 0; i < grid.childElementCount; i++) {
                    var gridRow = grid.childNodes.item(i);
                    var contentDataRow = gridRow.querySelector('[data-field="content_data"]');
                    var data = contentDataRow.innerHTML

                    var editedString = data.replaceAll(";", "<br>");
                    grid.childNodes.item(i).querySelector('[data-field="content_data"]').innerHTML = editedString;
                }

            }

        } catch (exception) {
            kendo.alert("Er is iets fout gegaan tijdens het laden van de data voor deze module. Sluit a.u.b. de module en probeer het nogmaals, of neem contact op met ons.");
            console.error(exception);
            window.processing.removeProcess(initialProcess);
        }
    }

    async loadGridViewState(key, grid) {

        let value;

        value = sessionStorage.getItem(key);
        
        if (!value) {
            value = await Wiser.api({
                url: `${this.base.settings.wiserApiRoot}users/grid-settings/${encodeURIComponent(key)}`,
                method: "GET",
                contentType: "application/json"
            });

            sessionStorage.setItem(key, value || "[]");
        }

        if (!value) {
            return;
        }

        const columns = JSON.parse(value);
        const gridOptions = grid.getOptions();

        

        if (!gridOptions || !gridOptions.columns || !gridOptions.columns.length) {
            return;
        }

        for (let column of gridOptions.columns) {
            const savedColumn = columns.filter(c => c.field === column.field);
            if (savedColumn.length === 0) {
                continue;
            }

            column.hidden = savedColumn[0].hidden;
        }
        grid.setOptions(gridOptions);
    }
}

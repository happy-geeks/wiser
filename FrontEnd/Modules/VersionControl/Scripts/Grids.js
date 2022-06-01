import { Wiser2 } from "../../Base/Scripts/Utils.js";
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

        this.mainGrid = null;
        this.mainGridFirstLoad = true;
        this.mainGridForceRecount = false;
        this.gridsAreLoaded = false;

    }

   

    /**
     * Do all initializations for the Grids class, such as adding bindings.
     */
    async initialize() {

      
        console.log(this.base.settings.gridViewOptionParse);
        this.base.settings.gridViewOptionParse = this.base.settings.gridViewOptionParse || {};   

        console.log(this.base.settings.gridViewOptionParse);
        console.log(this.gridsAreLoaded);
        await this.getModuleGridData(6001);
        console.log(this.gridsAreLoaded);

        console.log("IDK2");
       
    }

 

    async getModuleGridData(moduleId) {
        

        try {
            const moduleGridSettings = await Wiser2.api({
                url: `${this.base.settings.wiserApiRoot}VersionControl/module-gird-settings/${moduleId}`,
                method: "GET",
                contentType: "application/json",
            });


            for (const [key, value] of Object.entries(moduleGridSettings)) {

                var customQuery = value["customQuery"];
                var countQuery = value["countQuery"];
                var gridOptions = value["gridOptions"];
                var gridDivId = value["gridDivId"];


                console.log(gridDivId);

                await this.setupGridViewMode(customQuery, countQuery, gridOptions, gridDivId);
            } 



        } catch (exception) {
            console.error(exception);
            kendo.alert("Er is iets fout gegaan. Sluit a.u.b. deze module, open deze daarna opnieuw en probeer het vervolgens opnieuw. Of neem contact op als dat niet werkt.");
        } finally {
            //this.mainGridFirstLoad = false;
        }

   

    }
   
    /*
    generateCommitGrid() {
        var customQuery = "SELECT t.template_id,t.parent_id, t.template_name, t.version, t.changed_on, t.published_environment, test_template.version AS versie_test, acceptatie_template.version AS versie_acceptatie, live_template.version AS versie_live, t.changed_by, m.template_name AS templateparenttype,CASE WHEN test_template.version is null THEN 0 ELSE 1 END AS test,CASE WHEN acceptatie_template.version is null THEN 0 ELSE 1 END AS accept, CASE WHEN live_template.version is null THEN 0 ELSE 1 END AS live           FROM wiser_template t        LEFT JOIN wiser_template m ON t.parent_id = m.template_id        LEFT JOIN wiser_template test_template ON test_template.template_id = t.template_id AND(test_template.published_environment = 2 OR test_template.published_environment = 6 OR test_template.published_environment = 10 OR test_template.published_environment = 14) AND test_template.template_type != 7        LEFT JOIN wiser_template acceptatie_template ON acceptatie_template.template_id = t.template_id AND(acceptatie_template.published_environment = 4 OR acceptatie_template.published_environment = 6 OR acceptatie_template.published_environment = 12 OR acceptatie_template.published_environment = 14) AND acceptatie_template.template_type != 7        LEFT JOIN wiser_template live_template ON live_template.template_id = t.template_id AND(live_template.published_environment = 8 OR live_template.published_environment = 10 OR live_template.published_environment = 12 OR live_template.published_environment = 14) AND live_template.template_type != 7        LEFT JOIN wiser_template test ON test.template_id = t.template_id AND test.version = t.version AND test.published_environment != 0        LEFT JOIN wiser_template x ON x.template_id = t.template_id AND x.version = t.version        WHERE t.version = (SELECT MAX(version) FROM wiser_template x2 WHERE x2.template_id = t.template_id)        AND t.published_environment != 14        AND NOT EXISTS(SELECT * FROM dev_template_live dt WHERE dt.itemid = t.template_id and dt.version = t.version)        AND t.parent_id != ''        ORDER BY t.template_id ASC, version";
        var countQuery = "";
        var gridOptions = '        {            "gridViewMode": true,                "gridViewSettings": {                "selectable": "multiple, row",                    "pageSize": 100,                            "columns": [                { "selectable": "true", "width": "50px"},              { "field": "template_id", "title": "ID" },                                { "field": "templateparenttype", "title": "Type" },                                { "field": "parent_id", "title": "Parent" },                                { "field": "template_name", "title": "Template" },                                { "field": "version", "title": "Versie" },                                { "field": "versie_live", "title": "Versie live" },                                { "field": "versie_acceptatie", "title": "Versie acceptatie" },                                { "field": "versie_test", "title": "Versie test" },                                { "field": "changed_on", "format":"{0:F}", "title": "Datum" },                               { "field": "changed_by", "title": "Door" }                            ]            }        }';
        this.setupGridViewMode(customQuery, countQuery, gridOptions, "#gridView");
    }

    generateDeployGrid() {
        var customQuery = "SELECT result.*, CONCAT( '' , group_concat(usefull_data),'') as commit_data         FROM(            select dev_commit.id, dev_commit.description, dev_commit.addedon, dev_commit.changedby, group_concat(wdc.component separator '\n') AS usefull_data  FROM dev_commit        	INNER JOIN wiser_commit_dynamic_content wcdc ON dev_commit.id = wcdc.commit_id       	INNER JOIN wiser_dynamic_content wdc ON wcdc.dynamic_content_id = wdc.content_id AND wcdc.version = wdc.version AND wdc.published_environment != 0        	group by dev_commit.id                  union all                  select dev_commit.id, dev_commit.description, dev_commit.addedon, dev_commit.changedby, group_concat(wiser_template.template_name separator '\n') AS usefull_data         FROM dev_commit         INNER JOIN dev_template_live ON dev_commit.id = dev_template_live.commitid         INNER JOIN wiser_template ON dev_template_live.itemid = wiser_template.template_id AND dev_template_live.version = wiser_template.version AND wiser_template.published_environment != 0     group by dev_commit.id) AS result        group by result.id";
        var countQuery = "";
        var gridOptions = '{        "gridViewMode": true,            "gridViewSettings": {            "pageSize": 100,                "selectable": "multiple, row",                    "columns": [                       { "selectable": "true", "width": "50px"},  { "field": "id", "title": "ID" },                        { "field": "description", "title": "Commit Message" },                        { "field": "addedon", "format":"{0:F}","title": "Datum" },                        { "field": "", "title": "Live","width":"150px" },                        { "field": "", "title": "Acceptatie","width":"150px" },                        { "field": "", "title": "Test","width":"150px" },                        { "field": "changedby", "title": "Door" },			{ "field": "commit_data", "title": "Templates","width":"300px", "class":"test" }                    ]        }    }';
        this.setupGridViewMode(customQuery, countQuery, gridOptions, "#deploygrid");
      

        var table = document.querySelector("#deploygrid");

        console.log(table);

        const parent1 = document.querySelectorAll('td[data-field]');

        console.log(parent1);
        //convert string to html and back to string?

    }

    //get data column with data-field from the template data
    //convert string to html
    //aply <br /> 
    //convert back to string

    /*
        select dev_commit.id, group_concat(wiser_template.template_data separator "<br/>")
        FROM dev_commit
        INNER JOIN dev_template_live ON dev_commit.id = dev_template_live.commitid
        INNER JOIN wiser_template ON dev_template_live.itemid = wiser_template.template_id AND dev_template_live.version = wiser_template.version
        group by dev_commit.id
     
      
    */
    /*
    generateHistoryGrid() {
        

        var customQuery = "SELECT t.*, m.template_name AS templateparenttype, dtl.addedon ,test_template.version AS test_version, acceptatie_template.version AS accepatatie_version, live_template.version AS live_version        FROM wiser_template t        LEFT JOIN wiser_template m ON t.parent_id = m.template_id        LEFT JOIN dev_template_live dtl ON dtl.itemid = t.template_id AND dtl.version = t.version        LEFT JOIN wiser_template test_template ON test_template.template_id = t.template_id AND(test_template.published_environment = 2 OR test_template.published_environment = 6 OR test_template.published_environment = 10 OR test_template.published_environment = 14) AND test_template.template_type != 7        LEFT JOIN wiser_template acceptatie_template ON acceptatie_template.template_id = t.template_id AND(acceptatie_template.published_environment = 4 OR acceptatie_template.published_environment = 6 OR acceptatie_template.published_environment = 12 OR acceptatie_template.published_environment = 14) AND acceptatie_template.template_type != 7        LEFT JOIN wiser_template live_template ON live_template.template_id = t.template_id AND(live_template.published_environment = 8 OR live_template.published_environment = 10 OR live_template.published_environment = 12 OR live_template.published_environment = 14) AND live_template.template_type != 7        LEFT JOIN wiser_template test ON test.template_id = t.template_id AND test.version = t.version AND test.published_environment != 0        WHERE EXISTS(SELECT * FROM dev_template_live dt WHERE t.template_id = dt.itemid and t.version = dt.version)        AND t.published_environment = 0        ORDER BY dtl.addedon DESC";
        var countQuery = "";
        var gridOptions = '{\r\n          \t"gridViewMode": true,\r\n       \t"checkboxes": true,   \t\r\n       \t"gridViewSettings": {\r\n       \t\t"selectable": "row",\r\n          \t\t"pageSize": 100,\r\n          \t\t"columns": [\r\n     \t\t\t{ "field": "template_id", "title": "ID" },\r\n       \t\t\t{ "field": "templateparenttype", "title": "Type" },\r\n       \t\t\t{ "field": "parent_id", "title": "Parent" },\r\n       \t\t\t{ "field": "template_name", "title": "Template" },\r\n      \t\t\t{ "field": "version", "title": "Versie" },\r\n      \t\t\t{ "field": "test_version", "title": "Versie Test" },\r\n      \t\t\t{ "field": "accepatatie_version", "title": "Versie Accept" },\r\n      \t\t\t{ "field": "live_version", "title": "Versie Live" },\r\n      \t\t\t{ "field": "addedon", "format":"{0:F}","title": "Datum" }\r\n       \t\t]\r\n          \t}\r\n   }';
        this.setupGridViewMode(customQuery, countQuery, gridOptions, "#historyGridId");
    }

    async getStuff() {
        const getStuff = await Wiser2.api({
            url: `${this.base.settings.wiserApiRoot}/VersionControl/PublishedTemplateVersion`,
            method: "GET",
            contentType: "application/json"
        });
        return getStuff;
    }


    generateDynamicContentGrid() {
        var customQuery = "SELECT t.content_id,t.component, t.component_mode, t.version, t.changed_on, t.published_environment, t.changed_by, CASE WHEN test_content.version is null THEN 0 ELSE 1 END AS test,CASE WHEN acceptatie_content.version is null THEN 0 ELSE 1 END AS accept, CASE WHEN live_content.version is null THEN 0 ELSE 1 END AS live                    FROM wiser_dynamic_content t        LEFT JOIN wiser_dynamic_content test_content ON test_content.content_id = t.content_id AND(test_content.published_environment = 2 OR test_content.published_environment = 6 OR test_content.published_environment = 10)        LEFT JOIN wiser_dynamic_content acceptatie_content ON acceptatie_content.content_id = t.content_id AND(acceptatie_content.published_environment = 4 OR acceptatie_content.published_environment = 6 OR acceptatie_content.published_environment = 12)        LEFT JOIN wiser_dynamic_content live_content ON live_content.content_id = t.content_id AND(live_content.published_environment = 8 OR live_content.published_environment = 10 OR live_content.published_environment = 12 OR live_content.published_environment = 14)        WHERE t.version = (SELECT MAX(version) FROM wiser_dynamic_content x2 WHERE x2.content_id = t.content_id)        AND NOT EXISTS(SELECT * FROM wiser_commit_dynamic_content dt WHERE dt.dynamic_content_id = t.content_id and dt.version = t.version)        ORDER BY t.content_id ASC, version ";
        var countQuery = "";
        var gridOptions = '{        "gridViewMode": true,            "gridViewSettings": {            "pageSize": 100,                "selectable": "multiple, row",                    "columns": [                       { "selectable": "true", "width": "50px"},     { "field": "content_id", "title": "ID" },                        { "field": "component", "title": "Component" },                        { "field": "component_mode", "title": "Component mode" },                        { "field": "version", "title": "Versie" },                        { "field": "changed_on", "format":"{0:F}", "title": "Aangepast op" },   { "field": "test", "title": "Test" },{ "field": "accept", "title": "Acceptatie" },{ "field": "live", "title": "Live" },                      { "field": "changed_by", "title": "Door" }                    ]        }    }';
        this.setupGridViewMode(customQuery, countQuery, gridOptions, "#dynamicContentGrid");
    }

    generateDeployDynamicContentGrid() {
        var customQuery = "SELECT wcdc.*, t.id, t.content_id, t.settings, t.component, t.component_mode, t.version, t.title,t.changed_on, t.changed_by, t.published_environment, dc.id, dc.description\nFROM wiser_commit_dynamic_content wcdc\n LEFT JOIN wiser_dynamic_content t ON wcdc.dynamic_content_id = t.content_id and wcdc.version = t.version\n LEFT JOIN dev_commit dc ON dc.id = wcdc.commit_id\n WHERE t.published_environment != 14\n AND t.published_environment != 0";
        var countQuery = "";
        var gridOptions = '{\r\n\t"gridViewMode": true,\r\n\t"gridViewSettings":\r\n\t\t{\r\n\t\t"pageSize": 100,\r\n\t"selectable": "row",\r\n\t"columns": [\r\n\t\t\t{ "field": "dynamic_content_id", "title": "ID" },\r\n\t\t\t{ "field": "component", "title": "Component"},\r\n\t\t\t{ "field": "component_mode", "title": "Component mode" },\r\n\t\t\t{ "field": "description", "title": "Commit Message" },\r\n\t\t\t{ "field": "version", "title": "Versie" },\r\n\t\t\t{ "field": "changed_on", "title": "Aangepast op" },\r\n\t\t\t{ "field": "published_environment", "title": "Live" },\r\n\t\t\t{ "field": "published_environment", "title": "Acceptatie" },\r\n\t\t\t{ "field": "published_environment", "title": "Test" },\r\n\t\t\t{ "field": "changed_by", "title": "Door" }\r\n\t\t]\r\n\t}\r\n}';
        this.setupGridViewMode(customQuery, countQuery, gridOptions, "#DynamicContentDeployGrid");
    }

    generateHistoryDynamicContentGrid() {
        var customQuery = "SELECT t.*, test_content.version AS test_version, acceptatie_content.version AS accepatatie_version, live_content.version AS live_version        FROM wiser_dynamic_content t        LEFT JOIN wiser_commit_dynamic_content wcdc ON wcdc.commit_id = t.content_id AND wcdc.version = t.version        LEFT JOIN wiser_dynamic_content test_content ON test_content.content_id = t.content_id AND(test_content.published_environment = 2 OR test_content.published_environment = 6 OR test_content.published_environment = 10)        LEFT JOIN wiser_dynamic_content acceptatie_content ON acceptatie_content.content_id = t.content_id AND(acceptatie_content.published_environment = 4 OR acceptatie_content.published_environment = 6 OR acceptatie_content.published_environment = 12)        LEFT JOIN wiser_dynamic_content live_content ON live_content.content_id = t.content_id AND(live_content.published_environment = 8 OR live_content.published_environment = 10 OR live_content.published_environment = 12 OR live_content.published_environment = 14)        WHERE EXISTS(SELECT * FROM wiser_commit_dynamic_content dt WHERE t.content_id = dt.dynamic_content_id and t.version = dt.version)        AND t.published_environment = 0";
        var countQuery = "";
        var gridOptions = '{        "gridViewMode": true,            "checkboxes": true,                "gridViewSettings": {            "selectable": "row",                "pageSize": 100,                    "columns": [                        { "field": "content_id", "title": "ID" },                        { "field": "component", "title": "Component" },                        { "field": "settings", "title": "Settings" },                        { "field": "version", "title": "Versie" }, { "field": "test_version", "title": "Versie Test" },{ "field": "accepatatie_version", "title": "Versie Accept" },{ "field": "live_version", "title": "Versie Accept" },                       { "field": "changed_on", "title": "Datum" }                    ]        }    }';
        this.setupGridViewMode(customQuery, countQuery, gridOptions, "#historyDynamicContentGridId");
        
    }*/


    async getData() {
        const getStuff = await Wiser2.api({
            url: `${this.base.settings.wiserApiRoot}/PublishedTemplateVersion`,
            method: "GET",
            contentType: "application/json"
        });
        return getStuff;
    }

    /**
     * Setup the main information block for when the module has gridViewMode enabled and the informationBlock enabled.
     * @returns {boolean} Whether or not the grid view should be hidden.
     */
    async setupInformationBlock() {
     
        let hideGrid = false;
        const informationBlockSettings = this.base.settings.gridViewOptionParse.informationBlock;

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
            this.informationBlockIframe[0].onload = () => {
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
                        this.informationBlockIframe.attr("src", `${"/Modules/DynamicItems"}?itemId=${itemId}&moduleId=${this.base.settings.moduleId}&iframe=true&readonly=${!!informationBlockSettings.initialItem.readOnly}&hideFooter=${!!informationBlockSettings.initialItem.hideFooter}&hideHeader=${!!informationBlockSettings.initialItem.hideHeader}`);
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

            this.informationBlockIframe.attr("src", `${"/Modules/DynamicItems"}?itemId=${itemId}&moduleId=${this.base.settings.moduleId}&iframe=true&readonly=${!!informationBlockSettings.initialItem.readOnly}&hideFooter=${!!informationBlockSettings.initialItem.hideFooter}&hideHeader=${!!informationBlockSettings.initialItem.hideHeader}`);
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


    async setupGridViewMode(customQuery,countQuery, gridOptions, gridViewId) {

        var gridViewOptionParse = JSON.parse(gridOptions);

        console.log(gridViewOptionParse);

        const initialProcess = `loadMainGrid_${Date.now()}`;

        try {
            window.processing.addProcess(initialProcess);
           
            const gridViewOptionsSettings = JSON.parse(gridOptions);


            let gridViewSettings = gridViewOptionsSettings.gridViewSettings;
            let gridViewOptionParse = $.extend({}, this.base.settings.gridViewOptionParse);
            let gridDataResult;
            let previousFilters = null;

            console.log(gridViewSettings);

            const usingDataSelector = !!gridViewOptionParse.dataSelectorId;
            
            const options = {
                page: 1,
                pageSize: gridViewOptionParse.pageSize || 100,
                skip: 0,
                take: gridViewOptionParse.clientSidePaging ? 0 : (gridViewOptionParse.pageSize || 100),
                firstLoad: true
            }

            const test = {
                customQuery: customQuery,
                countQuery: countQuery,
                gridOptions: gridOptions,
                gridReadOptions: options
            }



            if (gridViewOptionParse.dataSource && gridViewOptionParse.dataSource.filter) {
                options.filter = gridViewOptionParse.dataSource.filter;
                previousFilters = JSON.stringify(options.filter);
            }

                //gets grid options and executes sql querry in database
                gridDataResult = await Wiser2.api({
                    url: `${this.base.settings.wiserApiRoot}VersionControl/${encodeURIComponent(this.base.settings.moduleId)}/overview-grid`,
                    method: "POST",
                    contentType: "application/json",
                    data: JSON.stringify(test)
                });

                
                console.log(gridDataResult);
                //foreach row in result get commit_data and set it to html
           


       
            
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
                            await Wiser2.showConfirmDialog(`Weet u zeker dat u ${deleteConfirmationText} wilt verwijderen?`)
                        }

                        await Wiser2.api({
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
                /*
                if (gridDataResult.columns) {
                    gridDataResult.columns.push({
                        title: "&nbsp123;",
                        width: commandColumnWidth,
                        command: commands
                    });
                }*/
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

           
            console.log("test");
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

                                //list of grids with bools
                                //if (this.mainGridFirstLoad) {
                                    console.log("First Load");
                                    transportOptions.success(gridDataResult);
                                    this.mainGridFirstLoad = false;
                                    window.processing.removeProcess(initialProcess);
                                    return;
                                //}
                                console.log("Not First Load");

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
                                if (usingDataSelector) {
                                   
                                    newGridDataResult = {
                                        columns: gridViewSettings.columns,
                                        pageSize: gridViewSettings.pageSize || 100,
                                        data: await Wiser2.api({
                                            url: `${this.base.settings.getItemsUrl}?encryptedDataSelectorId=${encodeURIComponent(gridViewSettings.dataSelectorId)}`,
                                            contentType: "application/json"
                                        })
                                    };
                                } else {

                                    var uid = finalGridViewSettings.dataSource.fields[0].uid;
                                    var uidElement = document.getElementById(uid);
                                    var table = uidElement.closest("[data-role='grid']");
                                    console.log(table);


                                    newGridDataResult = await Wiser2.api({
                                       
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
                columnHide: this.saveGridViewState.bind(this, `main_grid_columns_${this.base.settings.moduleId}`),
                columnShow: this.saveGridViewState.bind(this, `main_grid_columns_${this.base.settings.moduleId}`),
                dataBound: (event) => {
                    const totalCount = event.sender.dataSource.total();
                    const counterContainer = event.sender.element.find(".k-grid-toolbar .counterContainer");
                    counterContainer.find(".counter").html(kendo.toString(totalCount, "n0"));
                    counterContainer.find(".plural").toggle(totalCount !== 1);
                    counterContainer.find(".singular").toggle(totalCount === 1);

                    // To hide toolbar buttons that require a row to be selected.
                    this.onGridSelectionChange(event);
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


           
            console.log(finalGridViewSettings);
            

            this.mainGrid = $(gridViewId).kendoGrid(finalGridViewSettings).data("kendoGrid");

            console.log(this.mainGrid);
            //get table live accept and test version

            
            /*
           
            await this.mainGrid._data.forEach(async (element) => {
                try {
                    //console.log(element);
                    var templateId = element["template_id"];
                    var version = element["version"];
                    // console.log(templateId + "|" + version);


                    var currentPublishedVersions = await Wiser2.api({
                        url: `${this.base.settings.wiserApiRoot}VersionControl/current-published-enviornments/${templateId}/${version}`,
                        method: "GET",
                        contentType: "application/json"
                    });


                    if (currentPublishedVersions != "") {

                        var templateId = currentPublishedVersions["templateId"];


                        var publishedEnvironments = currentPublishedVersions["publishedEnvironments"];

                        var templateVersion = publishedEnvironments["versionList"];






                        for (const [key, value] of Object.entries(publishedEnvironments)) {

                            if (key == "testVersion" && value == 1) {
                                //change html on website

                                element["published_environment"] = templateVersion[0];
                            }


                        }
                    }
                } catch (e) {
                    console.log(e);
                }


                //console.log(element);

            });/*

            //this.mainGrid = $(gridViewId).kendoGrid(finalGridViewSettings).data("kendoGrid");
            /* if (this.mainGrid.element[0].classList.contains("k-master-row")) {

                var testVersion = this.mainGrid.element[0].getElementsByClassName("k-master-row")[0].getElementsByTagName("td")[5];
                var acceptatieVersion = this.mainGrid.element[0].getElementsByClassName("k-master-row")[0].getElementsByTagName("td")[6];
                var liveVersion = this.mainGrid.element[0].getElementsByClassName("k-master-row")[0].getElementsByTagName("td")[7];



                if (testVersion.innerHTML == "1") {


                } else if (acceptatieVersion.innerHTML == "1") {

                } else if (liveVersion == "1") {

                }


            }*/
           
            await this.loadGridViewState(`main_grid_columns_${this.base.settings.moduleId}`, this.mainGrid);

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




    


    async loadGridViewState(key, grid) {

        let value;

        value = sessionStorage.getItem(key);
        
        if (!value) {
            value = await Wiser2.api({
                url: `${this.base.settings.wiserApiRoot}users/grid-settings/${encodeURIComponent(key)}`,
                method: "GET",
                contentType: "application/json"
            });

            sessionStorage.setItem(key, value || "[]");
            //console.log(sessionStorage);
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



    async initializeItemsGrid(options, field, loader, itemId, height = undefined, propertyId = 0, extraData = null) {
        // TODO: Implement all functionality of all grids (https://app.asana.com/0/12170024697856/1138392544929161), so that we can use this method for everything.
        try {
            itemId = itemId || this.base.settings.zeroEncrypted;
            let customQueryGrid = options.customQuery === true;
            let kendoGrid;
            options.pageSize = options.pageSize || 25;
            console.log("asdafdasdfasdfasdfsdafsadfsdfasdfasdfasdfasf");
            console.log(options.checkboxes);
            const hideCheckboxColumn = !options.checkboxes || options.checkboxes === "false" || options.checkboxes <= 0;
            const gridOptions = {
                page: 1,
                pageSize: options.pageSize,
                skip: 0,
                take: options.pageSize,
                extraValuesForQuery: extraData
            };

            if (customQueryGrid) {
                const customQueryResults = await Wiser2.api({
                    url: `${this.base.settings.wiserApiRoot}items/${encodeURIComponent(itemId)}/entity-grids/custom?mode=4&queryId=${options.queryId || this.base.settings.zeroEncrypted}&countQueryId=${options.countQueryId || this.base.settings.zeroEncrypted}`,
                    method: "POST",
                    contentType: "application/json",
                    data: JSON.stringify(gridOptions)
                });

                if (customQueryResults.extraJavascript) {
                    $.globalEval(customQueryResults.extraJavascript);
                }

                if (Wiser2.validateArray(options.columns)) {
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

                kendoGrid = this.generateGrid(field, loader, options, customQueryGrid, customQueryResults, propertyId, height, itemId, extraData);
            } else {
                const gridSettings = await Wiser2.api({
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
                    let commandColumnWidth = 80;
                    let commands = [];

                    

                    gridSettings.columns.push({
                        title: "&nbsp;",
                        width: commandColumnWidth,
                        command: commands
                    });
                }

                kendoGrid = this.generateGrid(field, loader, options, customQueryGrid, gridSettings, propertyId, height, itemId, extraData);
            }

        } catch (exception) {
            console.error(exception);
            kendo.alert("Er is iets fout gegaan met het initialiseren van het overzicht. Probeer het a.u.b. nogmaals of neem contact op met ons.");
        }
    }
    
  
    generateGrid(element, loader, options, customQueryGrid, data, propertyId, height, itemId, extraData) {
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
        
       
        if (element.data("kendoGrid")) {
            element.data("kendoGrid").destroy();
            element.empty();
        }

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
                                const customQueryResults = await Wiser2.api({
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
                                const gridSettings = await Wiser2.api({
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






    onFilterMenuInit(event) {
        // Set the format of numeric fields, otherwise numbers will be shown like '3.154.079,00' instead of '3154079'.
        event.container.find("[data-role='numerictextbox']").each((index, element) => {
            $(element).data("kendoNumericTextBox").setOptions({
                format: "0"
            });
        });
    }


    async saveGridViewState(key, event) {
        try {
            const dataToSave = kendo.stringify(event.sender.getOptions().columns);
            sessionStorage.setItem(key, dataToSave);
            await Wiser2.api({
                url: `${this.base.settings.wiserApiRoot}users/grid-settings/${encodeURIComponent(key)}`,
                method: "POST",
                contentType: "application/json",
                data: dataToSave
            });
        } catch (exception) {
            kendo.alert("Er is iets fout gegaan tijdens het opslaan van de instellingen voor dit grid. Probeer het nogmaals, of neem contact op met ons.");
            console.error(exception);
        }
    }

    onFilterMenuOpen(event) {
        // Set the focus on the last textbox in the filter menu.
        event.container.find(".k-textbox:visible, .k-input:visible").last().focus();
    }


    onGridSelectionChange(event) {
        // Some buttons in the toolbar of a grid require that at least one row is selected. Hide these buttons while no row is selected.
        event.sender.wrapper.find(".hide-when-no-selected-rows").toggleClass("hidden", event.sender.select().length === 0);

        // Show/hide button groups where all buttons are hidden/visible.
        for (let buttonGroup of event.sender.wrapper.find(".k-button-drop")) {
            const buttonGroupElement = $(buttonGroup);
            const amountOfToggleableButtons = buttonGroupElement.find(".hide-when-no-selected-rows").length;
            const totalAmountOfButtons = buttonGroupElement.find("a.k-button").length;
            buttonGroupElement.toggleClass("hidden", event.sender.select().length === 0 && amountOfToggleableButtons === totalAmountOfButtons);
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
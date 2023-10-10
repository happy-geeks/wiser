import { Wiser } from "../../Base/Scripts/Utils.js";
import "../../Base/Scripts/Processing.js";

require("@progress/kendo-ui/js/kendo.notification.js");
require("@progress/kendo-ui/js/kendo.button.js");
require("@progress/kendo-ui/js/kendo.combobox.js");
require("@progress/kendo-ui/js/kendo.editor.js");
require("@progress/kendo-ui/js/kendo.splitter.js");
require("@progress/kendo-ui/js/kendo.tabstrip.js");
require("@progress/kendo-ui/js/kendo.treeview.js");
require("@progress/kendo-ui/js/kendo.grid.js");
require("@progress/kendo-ui/js/kendo.datetimepicker.js");
require("@progress/kendo-ui/js/kendo.multiselect.js");
require("@progress/kendo-ui/js/kendo.notification.js");
require("@progress/kendo-ui/js/cultures/kendo.culture.nl-NL.js");
require("@progress/kendo-ui/js/messages/kendo.messages.nl-NL.js");

import "../css/WtsConfiguration.css";

export class WtsConfiguration {

    /**
     * Initializes a new instance of DynamicContent.
     * @param {any} base The base Template class.
     */
    constructor(base) {
        this.base = base;
    
        // TODO: Add all the fields that are used in the configuration tab and initialize them here
        this.commitEnvironmentField = null;
    }

    async reloadWtsConfigurationTab(id) {
        // Empty the tab
        document.getElementById("wtsConfigurationTab").innerHTML = "";
        
        // Check to see if id is set
        if (id === undefined || id === null || id === 0) {
            console.error("id is not set");
            return;
        }
        
        // Tell the user that the tab is loading
        this.base.toggleMainLoader(true);
        
        let templateSettings = null;
        
        // Get the data from the api
        try {
            templateSettings = await Wiser.api({
                url: `${this.base.settings.wiserApiRoot}templates/${id}/configuration`,
                dataType: "json",
                method: "GET"
            });
            console.log(templateSettings);
        }
        catch (e) {
            console.error(e);
            this.base.toggleMainLoader(false); // Hide the loader
            kendo.alert("Er is iets fout gegaan. Sluit a.u.b. deze module, open deze daarna opnieuw en probeer het vervolgens opnieuw. Of neem contact op als dat niet werkt.");
            return;
        }
        
        // Build the view
        try {
            await Wiser.api({
                method: "POST",
                contentType: "application/json",
                url: "/Modules/Templates/WtsConfigurationTab",
                data: JSON.stringify(templateSettings)
            }).then(async (response) => {
                this.base.toggleMainLoader(false); // Hide the loader
                document.getElementById("wtsConfigurationTab").innerHTML = response; // Add the html to the tab
                $("#tabStripConfiguration").kendoTabStrip().data("kendoTabStrip"); // Initialize the tabstrip
            })
        }
        catch (e) {
            console.error(e);
            this.base.toggleMainLoader(false); // Hide the loader
            kendo.alert("Er is iets fout gegaan. Sluit a.u.b. deze module, open deze daarna opnieuw en probeer het vervolgens opnieuw. Of neem contact op als dat niet werkt.");
        }
        
        this.initializeKendoComponents();
    }

    /**
     * Initializes all kendo components for the base class.
     */
    initializeKendoComponents() {
        // TODO: use the correct input types for the different fields
        
        this.commitEnvironmentField = $("#wts-log-level").kendoDropDownList({
            optionLabel: "Selecteer log level",
            dataTextField: "text",
            dataValueField: "value"
        }).data("kendoDropDownList");

        this.commitEnvironmentField = $("#wts-timing-type").kendoDropDownList({
            optionLabel: "Selecteer herhaling",
            dataTextField: "text",
            dataValueField: "value",
            dataSource: [
                {text: "Loopt continue", value: 1},
                {text: "Dagelijks", value: 2},
                {text: "Wekelijks", value: 3},
                {text: "Maandelijks", value: 4}
            ]
        }).data("kendoDropDownList");

        this.commitEnvironmentField = $("#wts-timing-weekly").kendoDropDownList({
            optionLabel: "Selecteer dag",
            dataTextField: "text",
            dataValueField: "value",
            dataSource: [
                {text: "Op maandag", value: 1},
                {text: "Op dinsdag", value: 2},
                {text: "Op woensdag", value: 3},
                {text: "Op donderdag", value: 4},
                {text: "Op vrijdag", value: 5},
                {text: "Op zaterdag", value: 6},
                {text: "Op zondag", value: 7}
            ]
        }).data("kendoDropDownList");

        this.commitEnvironmentField = $("#wts-timing-monthly").kendoDropDownList({
            optionLabel: "Selecteer herhaal dag",
            dataTextField: "text",
            dataValueField: "value",
            dataSource: [
                {text: "Op 1e dag van de maand", value: 1},
                {text: "Op 2e dag van de maand", value: 2},
                {text: "Op 3e dag van de maand", value: 3},
                {text: "Op 4e dag van de maand", value: 4},
                {text: "Op 5e dag van de maand", value: 5},
                {text: "Op 6e dag van de maand", value: 6},
                {text: "Op 7e dag van de maand", value: 7},
                {text: "Op 8e dag van de maand", value: 8},
                {text: "Op 9e dag van de maand", value: 9},
                {text: "Op 10e dag van de maand", value: 10}
            ]
        }).data("kendoDropDownList");

        this.commitEnvironmentField = $("#wts-timing-log").kendoDropDownList({
            optionLabel: "Selecteer herhaling",
            dataTextField: "text",
            dataValueField: "value",
            dataSource: [
                {text: "Loopt continue", value: 1},
                {text: "Dagelijks", value: 2},
                {text: "Wekelijks", value: 3},
                {text: "Maandelijks", value: 4}
            ]
        }).data("kendoDropDownList");

        this.commitEnvironmentField = $("#wts-timing-hold").kendoDropDownList({
            optionLabel: "Selecteer dag",
            dataTextField: "text",
            dataValueField: "value",
            dataSource: [
                {text: "Op maandag", value: 1},
                {text: "Op dinsdag", value: 2},
                {text: "Op woensdag", value: 3},
                {text: "Op donderdag", value: 4},
                {text: "Op vrijdag", value: 5},
                {text: "Op zaterdag", value: 6},
                {text: "Op zondag", value: 7}
            ]
        }).data("kendoDropDownList");

        this.commitEnvironmentField = $("#wts-continuous-start").kendoDropDownList({
            optionLabel: "Start tijd",
            dataTextField: "text",
            dataValueField: "value",
            dataSource: [
                {text: "0:00", value: 1},
                {text: "0:30", value: 2},
                {text: "1:00", value: 3},
                {text: "1:30", value: 4},
                {text: "2:00", value: 5},
                {text: "2:30", value: 6},
                {text: "3:00", value: 7},
                {text: "3:30", value: 8},
                {text: "4:00", value: 9},
                {text: "4:30", value: 10},
                {text: "5:00", value: 11},
                {text: "5:30", value: 12},
                {text: "6:00", value: 13},
                {text: "6:30", value: 14},
                {text: "7:00", value: 15},
                {text: "7:30", value: 16},
                {text: "8:00", value: 17},
                {text: "8:30", value: 18},
                {text: "9:00", value: 19},
                {text: "9:30", value: 20},
                {text: "10:00", value: 21},
                {text: "10:30", value: 22},
                {text: "11:00", value: 23},
                {text: "11:30", value: 24},
                {text: "12:00", value: 25},
            ]
        }).data("kendoDropDownList");

        this.commitEnvironmentField = $("#wts-continuous-from").kendoDropDownList({
            optionLabel: "Start tijd",
            dataTextField: "text",
            dataValueField: "value",
            dataSource: [
                {text: "0:00", value: 1},
                {text: "0:30", value: 2},
                {text: "1:00", value: 3},
                {text: "1:30", value: 4},
                {text: "2:00", value: 5},
                {text: "2:30", value: 6},
                {text: "3:00", value: 7},
                {text: "3:30", value: 8},
                {text: "4:00", value: 9},
                {text: "4:30", value: 10},
                {text: "5:00", value: 11},
                {text: "5:30", value: 12},
                {text: "6:00", value: 13},
                {text: "6:30", value: 14},
                {text: "7:00", value: 15},
                {text: "7:30", value: 16},
                {text: "8:00", value: 17},
                {text: "8:30", value: 18},
                {text: "9:00", value: 19},
                {text: "9:30", value: 20},
                {text: "10:00", value: 21},
                {text: "10:30", value: 22},
                {text: "11:00", value: 23},
                {text: "11:30", value: 24},
                {text: "12:00", value: 25},
            ]
        }).data("kendoDropDownList");

        this.commitEnvironmentField = $("#wts-continuous-till").kendoDropDownList({
            optionLabel: "Start tijd",
            dataTextField: "text",
            dataValueField: "value",
            dataSource: [
                {text: "0:00", value: 1},
                {text: "0:30", value: 2},
                {text: "1:00", value: 3},
                {text: "1:30", value: 4},
                {text: "2:00", value: 5},
                {text: "2:30", value: 6},
                {text: "3:00", value: 7},
                {text: "3:30", value: 8},
                {text: "4:00", value: 9},
                {text: "4:30", value: 10},
                {text: "5:00", value: 11},
                {text: "5:30", value: 12},
                {text: "6:00", value: 13},
                {text: "6:30", value: 14},
                {text: "7:00", value: 15},
                {text: "7:30", value: 16},
                {text: "8:00", value: 17},
                {text: "8:30", value: 18},
                {text: "9:00", value: 19},
                {text: "9:30", value: 20},
                {text: "10:00", value: 21},
                {text: "10:30", value: 22},
                {text: "11:00", value: 23},
                {text: "11:30", value: 24},
                {text: "12:00", value: 25},
            ]
        }).data("kendoDropDownList");

        this.commitEnvironmentField = $("#wts-continuous-interval").kendoDropDownList({
            optionLabel: "Start tijd",
            dataTextField: "text",
            dataValueField: "value",
            dataSource: [
                {text: "0:00", value: 1},
                {text: "0:30", value: 2},
                {text: "1:00", value: 3},
                {text: "1:30", value: 4},
                {text: "2:00", value: 5},
                {text: "2:30", value: 6},
                {text: "3:00", value: 7},
                {text: "3:30", value: 8},
                {text: "4:00", value: 9},
                {text: "4:30", value: 10},
                {text: "5:00", value: 11},
                {text: "5:30", value: 12},
                {text: "6:00", value: 13},
                {text: "6:30", value: 14},
                {text: "7:00", value: 15},
                {text: "7:30", value: 16},
                {text: "8:00", value: 17},
                {text: "8:30", value: 18},
                {text: "9:00", value: 19},
                {text: "9:30", value: 20},
                {text: "10:00", value: 21},
                {text: "10:30", value: 22},
                {text: "11:00", value: 23},
                {text: "11:30", value: 24},
                {text: "12:00", value: 25},
            ]
        }).data("kendoDropDownList");

        this.wtsTimersGrid = $("#wtsTimers").kendoGrid({
            resizable: true,
            height: 280,
            columns: [
                {
                    field: "ID",
                    title: "ID"
                }, {
                    field: "Type",
                    title: "Type"
                }, {
                    field: "Moment",
                    title: "Moment"
                }, {
                    field: "LogLevel",
                    title: "Log level"
                }, {
                    command: "destroy",
                    title: "&nbsp;",
                    width: 120
                }
            ],
        }).data("kendoGrid");

        this.wtsLinkedActionsGrid = $("#wtsLinkedActions").kendoGrid({
            resizable: true,
            height: 280,
            columns: [
                {
                    field: "Type",
                    title: "Type"
                }, {
                    field: "Naam",
                    title: "Naam"
                }, {
                    field: "LaatsteRun",
                    title: "Laatste Run"
                }, {
                    field: "LaatsteRunTijd",
                    title: "Laatste Run-Tijd"
                }, {
                    command: "destroy",
                    title: "&nbsp;",
                    width: 120
                }
            ],
        }).data("kendoGrid");

        this.wtsServiceActions = $("#wtsServiceActions").kendoGrid({
            resizable: true,
            height: 280,
            columns: [
                {
                    field: "Type",
                    title: "Type"
                }, {
                    field: "Naam",
                    title: "Naam"
                }, {
                    field: "LaatsteRun",
                    title: "Laatste Run"
                }, {
                    field: "LaatsteRunTijd",
                    title: "Laatste Run-Tijd"
                }, {
                    command: "destroy",
                    title: "&nbsp;",
                    width: 120
                }
            ],
        }).data("kendoGrid");
    }

    /**
     * Gathers the values of the input fields and returns them as an object.
     * @returns {any} The object containing the values of the input fields.
     */
    getCurrentSettings() {
        const serviceName = document.getElementById("wts-name").value;
        const connectionString = document.getElementById("wts-connection-string").value;
        const logLevelField = document.getElementById('wts-log-level');
        const logLevelSelectedIndex = logLevelField.selectedIndex;
        const logLevel = logLevelField.options[logLevelSelectedIndex].value;
        const logStopStart = document.getElementById("wts-log-stop-start").checked;
        const logRunCyclus = document.getElementById("wts-log-run-cyclus").checked;
        const logBody = document.getElementById("wts-log-body").checked;
        
        // Create the object according to the model
        return {
            "ServiceName": serviceName,
            "ConnectionString": connectionString,
            "LogSettings": {
                "LogLevel": logLevel,
                "LogStopStart": logStopStart,
                "LogRunCyclus": logRunCyclus,
                "LogBody": logBody
            }
        };
    }

    /**
     * Shows or hides the main (full screen) loader.
     * @param {boolean} show True to show the loader, false to hide it.
     */
    toggleMainLoader(show) {
        this.mainLoader.toggleClass("loading", show);
    }
}
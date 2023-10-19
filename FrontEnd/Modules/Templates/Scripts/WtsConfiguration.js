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
        this.template = null;
        this.selectedTimer = null;
        this.serviceName = null;
        this.connectionString = null;
        this.logLevel = null;
        this.logStartAndStop = null;
        this.logRunStartAndStop = null;
        this.logRunBody = null;
        this.wtsTimersGrid = null;
        this.timingId = null;
        this.timingType = null;
        this.timingDayOfWeek = null;
        this.timingDayOfMonth = null;
        this.timingStart = null;
        this.timingStop = null;
        this.timingDelay = null;
        this.timingHour = null;
        this.timingRunImmediately = null;
        this.timingSkipWeekend = null;
        this.timingSkipDays = null;
        this.timingLogMinimumLevel = null;
        this.timingLogStartAndStop = null;
        this.timingLogRunStartAndStop = null;
        this.timingLogRunBody = null;
        this.wtsLinkedActionsGrid = null;
        this.wtsServiceActions = null;
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
                url: `${this.base.settings.wiserApiRoot}templates/${id}/wtsconfiguration`,
                dataType: "json",
                method: "GET"
            });
            this.template = templateSettings;
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
        this.bindEvents();
        this.onTimerChange();
    }
    
    /**
     * Add the events to the elements
     */
    bindEvents() {
        document.getElementById("wtsDebuggerButton").addEventListener("click", () => {
            console.log("After changes: ", this.getCurrentSettings());
        });
        
        document.getElementById("saveButtonWtsConfiguration").addEventListener("click", () => {
            this.base.saveTemplate(false);
        });
        document.getElementById("saveAndDeployToTestButtonWtsConfiguration").addEventListener("click", () => {
            this.base.saveTemplate(true);
        });
    }
    
    onTimerChange(e) {
        // Hide input view
        $("#wts-timing-fields").hide();
        this.resetAndHideTimerFields(true);
        
        // Check if event was sent
        if (!e) {
            return;
        }
        // Get the selected row
        var row = e.sender.select().first();
        if (!row) {
            return;
        }
        // Get the selected timer
        this.selectedTimer = this.wtsTimersGrid.dataItem(row);
        if (!this.selectedTimer) {
            return;
        }
        console.log("Selected timer: ", this.selectedTimer);
        
        // Show input view
        $("#wts-timing-fields").show();
        
        // Fill in the fields that are always shown
        this.timingId.val(this.selectedTimer["timeId"]);
        this.timingType.value(this.selectedTimer["type"]);
        this.timingRunImmediately.value(this.selectedTimer["runImmediately"]);
        this.timingSkipWeekend.value(this.selectedTimer["skipWeekend"]);
        
        // Convert the skipdays string of numbers to an array of numbers if it exists
        let skipDays = [];
        if (this.selectedTimer["skipDays"]) {
            skipDays = this.selectedTimer["skipDays"].split(",");
        }
        this.timingSkipDays.value(skipDays);
        
        if (this.selectedTimer["logSettings"]) {
            this.timingLogMinimumLevel.value(this.selectedTimer["logSettings"]["logMinimumLevel"] ?? "");
            this.timingLogStartAndStop.value(this.selectedTimer["logSettings"]["logStartAndStop"] ?? "");
            this.timingLogRunStartAndStop.value(this.selectedTimer["logSettings"]["logRunStartAndStop"] ?? "");
            this.timingLogRunBody.value(this.selectedTimer["logSettings"]["logRunBody"] ?? "");
        }
        
        // Fill in the fields that are shown based on the type
        this.showAndFillTypeFields(this.selectedTimer["type"], this.selectedTimer);
    }

    onTimingTypeChange(e) {
        console.log("Type has changed: ", this.timingType.value());
        if (this.selectedTimer !== null) {
            this.showAndFillTypeFields(this.timingType.value(), this.selectedTimer);
        }
    }
    
    showAndFillTypeFields(type, timer) {
        // Clear the previous fields
        this.resetAndHideTimerFields(false);
        // Fill in the fields that are shown based on the type
        switch (type) {
            case "Continuous":
                $("#wts-timing-delay").show();
                this.timingDelay.value(timer["delay"]);
                $("#wts-timing-start").parent().parent().show();
                this.timingStart.value(timer["startTime"]);
                $("#wts-timing-stop").parent().parent().show();
                this.timingStop.value(timer["stopTime"]);
                $("#wts-timing-delay").parent().parent().show();
                this.timingDelay.value(timer["delay"]);
                break;
            case "Daily":
                $("#wts-timing-hour").parent().parent().show();
                this.timingHour.value(timer["hour"]);
                break;
            case "Weekly":
                $("#wts-timing-dayofweek").parent().show();
                this.timingDayOfWeek.value(timer["dayOfWeek"]);
                break;
            case "Monthly":
                $("#wts-timing-dayofmonth").parent().parent().show();
                this.timingDayOfMonth.value(timer["dayOfMonth"]);
                break;
        }
    }

    resetAndHideTimerFields(resetAll) {
        if (resetAll) {
            // These fields are always shown
            this.timingId.val("");
            this.timingType.value("");
            this.timingRunImmediately.value(false);
            this.timingSkipWeekend.value(false);
            this.timingSkipDays.value("");
            this.timingLogMinimumLevel.value("");
            this.timingLogStartAndStop.value(false);
            this.timingLogRunStartAndStop.value(false);
            this.timingLogRunBody.value(false);
        }
        
        // These fields are only shown with their corresponding type
        $("#wts-timing-dayofweek").parent().hide();
        this.timingDayOfWeek.value("");
        
        $("#wts-timing-dayofmonth").parent().parent().hide();
        this.timingDayOfMonth.value("");
        
        $("#wts-timing-start").parent().parent().hide();
        this.timingStart.value("");
        
        $("#wts-timing-stop").parent().parent().hide();
        this.timingStop.value("");
        
        $("#wts-timing-delay").parent().parent().hide();
        this.timingDelay.value("");
        
        $("#wts-timing-hour").parent().parent().hide();
        this.timingHour.value("");
    }

    onTimerDelete(e) {
        // TODO: Add a function for removing a runscheme, or should it exist as a button below the grid in the detail view?
        console.log("Removing: ", e);
    }

    /**
     * Initializes all kendo components for the base class.
     */
    initializeKendoComponents() {
        $("#wtsDebuggerButton").kendoButton();
        $("#saveButtonWtsConfiguration").kendoButton({
            icon: "save"
        });
        $("#saveAndDeployToTestButtonWtsConfiguration").kendoButton();
        
        // TODO: use the correct input types for the different fields
        
        this.serviceName = $("#wts-name").kendoTextBox();
        
        this.connectionString = $("#wts-connection-string").kendoTextBox();
        
        this.logLevel = $("#wts-log-level").kendoDropDownList({
            optionLabel: "Selecteer log level",
            dataTextField: "text",
            dataValueField: "value"
        }).data("kendoDropDownList");
        
        this.logStartAndStop = $("#wts-log-start-stop").kendoCheckBox().data("kendoCheckBox");
        
        this.logRunStartAndStop = $("#wts-log-run-startandstop").kendoCheckBox().data("kendoCheckBox");
        
        this.logRunBody = $("#wts-log-body").kendoCheckBox().data("kendoCheckBox");

        this.wtsTimersGrid = $("#wtsTimers").kendoGrid({
            resizable: true,
            height: 280,
            selectable: "row",
            dataSource: this.template.runSchemes,
            change: this.onTimerChange.bind(this),
            columns: [
                {
                    field: "timeId",
                    title: "ID"
                }, {
                    field: "type",
                    title: "Type"
                }
            ]
        }).data("kendoGrid");

        this.timingId = $("#wts-timing-id").kendoTextBox();
        
        this.timingType = $("#wts-timing-type").kendoDropDownList({
            optionLabel: "Selecteer type",
            dataTextField: "text",
            dataValueField: "value",
            change: this.onTimingTypeChange.bind(this)
        }).data("kendoDropDownList");

        this.timingDayOfWeek = $("#wts-timing-dayofweek").kendoDropDownList({
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

        this.timingDayOfMonth = $("#wts-timing-dayofmonth").kendoNumericTextBox({
            format: "#",
            decimals: 0,
            min: 1,
            max: 31
        }).data("kendoNumericTextBox");

        this.timingStart = $("#wts-timing-start").kendoTimePicker({
            dateInput: true,
            componentType: "modern",
            format: "HH:mm:ss"
        }).data("kendoTimePicker");

        this.timingStop = $("#wts-timing-stop").kendoTimePicker({
            dateInput: true,
            componentType: "modern",
            format: "HH:mm:ss"
        }).data("kendoTimePicker");

        this.timingDelay = $("#wts-timing-delay").kendoTimePicker({
            dateInput: true,
            componentType: "modern",
            format: "HH:mm:ss"
        }).data("kendoTimePicker");
        
        this.timingHour = $("#wts-timing-hour").kendoTimePicker({
            dateInput: true,
            componentType: "modern",
            format: "HH:mm:ss"
        }).data("kendoTimePicker");
        
        this.timingRunImmediately = $("#wts-timing-runimmediately").kendoCheckBox().data("kendoCheckBox");
        
        this.timingSkipWeekend = $("#wts-timing-skipweekend").kendoCheckBox().data("kendoCheckBox");
        
        this.timingSkipDays = $("#wts-timing-skipdays").kendoMultiSelect({
            placeholder: "Selecteer dag(en)",
            downArrow: true,
            dataTextField: "text",
            dataValueField: "value",
            dataSource: [
                {text: "Maandag", value: 1},
                {text: "Dinsdag", value: 2},
                {text: "Woensdag", value: 3},
                {text: "Donderdag", value: 4},
                {text: "Vrijdag", value: 5},
                {text: "Zaterdag", value: 6},
                {text: "Zondag", value: 7}
            ]
        }).data("kendoMultiSelect");
        
        this.timingLogMinimumLevel = $("#wts-timing-logminimumlevel").kendoDropDownList({
            optionLabel: "Selecteer log level",
            dataTextField: "text",
            dataValueField: "value"
        }).data("kendoDropDownList");
        
        this.timingLogStartAndStop = $("#wts-timing-logstartstop").kendoCheckBox().data("kendoCheckBox");
        
        this.timingLogRunStartAndStop = $("#wts-timing-logrunstartandstop").kendoCheckBox().data("kendoCheckBox");
        
        this.timingLogRunBody = $("#wts-timing-logrunbody").kendoCheckBox().data("kendoCheckBox");

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
            ]
        }).data("kendoGrid")

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
            ]
        }).data("kendoGrid");
    }

    /**
     * Gathers the values of the input fields and returns them as an object.
     * @returns {any} The object containing the values of the input fields.
     */
    getCurrentSettings() {
        this.template.serviceName = document.getElementById("wts-name").value; // Service name
        this.template.connectionString = document.getElementById("wts-connection-string").value; // Connection string
        const logLevelField = document.getElementById('wts-log-level');
        const logLevelSelectedIndex = logLevelField.selectedIndex;
        this.template.logSettings.logMinimumLevel = logLevelField.options[logLevelSelectedIndex].value; // Log minimum level
        this.template.logSettings.logStartAndStop = document.getElementById("wts-log-start-stop").checked; // Log start and stop
        this.template.logSettings.logRunStartAndStop = document.getElementById("wts-log-run-startandstop").checked; // Log run start and stop
        this.template.logSettings.logRunBody = document.getElementById("wts-log-body").checked; // Log run body
        return this.template;
    }

    /**
     * Shows or hides the main (full screen) loader.
     * @param {boolean} show True to show the loader, false to hide it.
     */
    toggleMainLoader(show) {
        this.mainLoader.toggleClass("loading", show);
    }
}
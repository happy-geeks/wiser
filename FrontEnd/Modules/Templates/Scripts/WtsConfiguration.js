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
        
        this.template = null;
        this.selectedTimer = null;
        this.wtsTimersForm = null;
        this.deleteConfirmationWindow = null;
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
        this.timingSaveChanges = null;
        this.wtsLinkedActionsGrid = null;
        this.wtsServiceActions = null;
        this.wtsActionTimerId = null;
        this.wtsActionOrder = null;
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
    }
    
    /**
     * Add the events to the elements
     */
    bindEvents() {
        // Remove all bindings first
        let wtsTimingCreateButton = document.getElementById("wtsTimingCreateButton");
        let wtsTimingSaveChanges = document.getElementById("wtsTimingSaveChanges");
        let saveButtonWtsConfiguration = document.getElementById("saveButtonWtsConfiguration");
        let saveAndDeployToTestButtonWtsConfiguration = document.getElementById("saveAndDeployToTestButtonWtsConfiguration");
        if (wtsTimingCreateButton) {
            wtsTimingCreateButton.removeEventListener(null, null);
            wtsTimingCreateButton.addEventListener("click", () => {
                // Clear the selected timer
                this.selectedTimer = null;
                // Initialize all the components in the window
                this.initializeTimersFormWindow();
                // Clear the previous fields
                this.resetAndHideTimerFields(true);
                // Find the highest ID and add 1 to it, this will be the new ID
                const highestId = Math.max.apply(Math, this.template.runSchemes.map((timer) => timer.timeId));
                this.timingId.value(highestId + 1);
                // Show form window
                this.wtsTimersForm.open().center();
                this.wtsTimersForm.wrapper.find('.k-window-content').scrollTop(0);
            });
        }
        if (wtsTimingSaveChanges) {
            wtsTimingSaveChanges.removeEventListener(null, null);
            wtsTimingSaveChanges.addEventListener("click", () => {
                // Check if we're creating a new timer or editing an existing one
                if (this.selectedTimer === null) {
                    // Creating a new timer
                    try {
                        this.saveTimer();
                    } catch (e) {
                        kendo.alert(e.message);
                    }
                    console.log("All timers: ", this.template.runSchemes);
                }
                else {
                    // Editing an existing timer
                    this.editTimer();
                }
            });
        }
        if (saveButtonWtsConfiguration) {
            saveButtonWtsConfiguration.removeEventListener(null, null);
            saveButtonWtsConfiguration.addEventListener("click", () => {
                this.base.saveTemplate(false);
            });
        }
        if (saveAndDeployToTestButtonWtsConfiguration) {
            saveAndDeployToTestButtonWtsConfiguration.removeEventListener(null, null);
            saveAndDeployToTestButtonWtsConfiguration.addEventListener("click", () => {
                this.base.saveTemplate(true);
            });
        }
    }

    onEditTimer(e) {
        // Get the selected timer
        this.selectedTimer = this.wtsTimersGrid.dataItem(e.currentTarget.closest("tr")) ?? null;
        if (!this.selectedTimer) {
            return;
        }
        console.log("Selected timer: ", this.selectedTimer);

        // Initialize all the components in the window
        this.initializeTimersFormWindow();

        // Clear the previous fields
        this.resetAndHideTimerFields(true);

        // Show form window
        this.wtsTimersForm.open().center(); // Open the window and center it
        this.wtsTimersForm.wrapper.find('.k-window-content').scrollTop(0); // Scroll to the top of the window

        // Fill in the fields that are always shown
        this.timingId.value(this.selectedTimer?.timeId ?? "");
        this.timingType.value(this.selectedTimer?.type ?? "");
        this.timingRunImmediately.value(this.selectedTimer?.runImmediately ?? "");
        this.timingSkipWeekend.value(this.selectedTimer?.skipWeekend ?? "");

        // Convert the skipdays string of numbers to an array of numbers if it exists
        let skipDays = [];
        if (this.selectedTimer?.skipDays) {
            skipDays = this.selectedTimer.skipDays.split(",");
        }
        this.timingSkipDays.value(skipDays);

        if (this.selectedTimer?.logSettings) {
            this.timingLogMinimumLevel.value(this.selectedTimer?.logSettings?.logMinimumLevel ?? "");
            this.timingLogStartAndStop.value(this.selectedTimer?.logSettings?.logStartAndStop ?? "");
            this.timingLogRunStartAndStop.value(this.selectedTimer?.logSettings?.logRunStartAndStop ?? "");
            this.timingLogRunBody.value(this.selectedTimer?.logSettings?.logRunBody ?? "");
        }

        // Fill in the fields that are shown based on the type
        this.showAndFillTypeFields(this.selectedTimer);
    }

    async onDeleteTimer(e) {
        // Get the selected timer
        let selectedTimer = this.wtsTimersGrid.dataItem(e.currentTarget.closest("tr"));
        if (!selectedTimer) {
            return;
        }
        console.log("Selected timer: ", selectedTimer);

        // Show confirmation dialog
        try {
            await Wiser.showConfirmDialog(`Weet u zeker dat u timer ${selectedTimer.timeId} wilt verwijderen?`);
        }
        catch (error) {
            // User canceled the dialog
            return;
        }

        // Remove the timer from the template
        this.template.runSchemes = this.template.runSchemes.filter((t) => t.timeId !== selectedTimer.timeId);

        // Remove timer from the grid
        this.wtsTimersGrid.dataSource.remove(selectedTimer);
    }

    onTimingTypeChange(e) {
        console.log("Type has changed: ", this.timingType.value());
        this.showAndFillTypeFields(this.selectedTimer, this.timingType.value());
    }
    
    showAndFillTypeFields(timer, type) {
        // Check if type was given, if not use the type of the timer
        if (type === undefined || type === null) {
            type = timer?.type;
        }
        // Clear the previous fields
        this.resetAndHideTimerFields(false);
        // Fill in the fields that are shown based on the type
        switch (type) {
            case "Continuous":
                $("#wts-timing-delay").show();
                this.timingDelay.value(timer?.delay ?? "");
                $("#wts-timing-start").parent().parent().show();
                this.timingStart.value(timer?.startTime ?? "");
                $("#wts-timing-stop").parent().parent().show();
                this.timingStop.value(timer?.stopTime ?? "");
                $("#wts-timing-delay").parent().parent().show();
                this.timingDelay.value(timer?.delay ?? "");
                break;
            case "Daily":
                $("#wts-timing-hour").parent().parent().show();
                this.timingHour.value(timer?.hour ?? "");
                break;
            case "Weekly":
                $("#wts-timing-dayofweek").parent().show();
                this.timingDayOfWeek.value(timer?.dayOfWeek ?? "");
                $("#wts-timing-hour").parent().parent().show();
                this.timingHour.value(timer?.hour ?? "");
                break;
            case "Monthly":
                $("#wts-timing-dayofmonth").parent().parent().show();
                this.timingDayOfMonth.value(timer?.dayOfMonth ?? "");
                $("#wts-timing-hour").parent().parent().show();
                this.timingHour.value(timer?.hour ?? "");
                break;
        }
    }

    resetAndHideTimerFields(resetAll) {
        if (resetAll) {
            // These fields are always shown
            this.timingId.value("");
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
    
    saveTimer() {
        // Check if the timer already exists
        const timerExists = this.template.runSchemes.some((timer) => timer.timeId === this.timingId.value());
        if (timerExists) {
            // Timer already exists, show confirmation dialog
            throw new Error("Het gebruikte ID bestaat al. Gebruik een ander ID of bewerk de bestaande timer.");
        }
        console.log("Creating a new timer");
        // Create a new timer
        const newTimer = {
            timeId: this.timingId.value(),
            type: this.timingType.value()
        };
        
        // Check the type and get the corresponding values
        switch (this.timingType.value()) {
            case "Continuous":
                newTimer.startTime = this.getTimeOnly(this.timingStart.value());
                newTimer.stopTime = this.getTimeOnly(this.timingStop.value());
                newTimer.delay = this.getTimeOnly(this.timingDelay.value());
                break;
            case "Daily":
                newTimer.hour = this.getTimeOnly(this.timingHour.value());
                break;
            case "Weekly":
                newTimer.dayOfWeek = this.timingDayOfWeek.value();
                newTimer.hour = this.getTimeOnly(this.timingHour.value());
                break;
            case "Monthly":
                newTimer.dayOfMonth = this.timingDayOfMonth.value();
                newTimer.hour = this.getTimeOnly(this.timingHour.value());
                break;
        }
        
        // Only set the runImmediately, skipWeekend and skipDays if they are not null/false (Default value is false)
        if (this.timingRunImmediately.value() !== false) {
            newTimer.runImmediately = this.timingRunImmediately.value();
        }
        if (this.timingSkipWeekend.value() !== false) {
            newTimer.skipWeekend = this.timingSkipWeekend.value();
        }
        if (this.timingSkipDays.value().length > 0) {
            newTimer.skipDays = this.timingSkipDays.value().sort((a, b) => a - b).join(",");
        }
        
        // Check if any log settings are set
        if (this.timingLogMinimumLevel.value() !== "" || this.timingLogStartAndStop.value() !== false || this.timingLogRunStartAndStop.value() !== false || this.timingLogRunBody.value() !== false) {
            newTimer.logSettings = {};
            newTimer.logSettings.logMinimumLevel = this.timingLogMinimumLevel.value();
            newTimer.logSettings.logStartAndStop = this.timingLogStartAndStop.value();
            newTimer.logSettings.logRunStartAndStop = this.timingLogRunStartAndStop.value();
            newTimer.logSettings.logRunBody = this.timingLogRunBody.value();
        }
        
        // Add the new timer to the template
        this.template.runSchemes.push(newTimer);
        
        // Refresh the grid
        this.wtsTimersGrid.dataSource.read();
        
        // Close the popup
        this.wtsTimersForm.close();
    }
    
    editTimer() {
        // Find the timer in the array
        const timerIndex = this.template.runSchemes.findIndex((timer) => timer.timeId === this.selectedTimer.timeId);

        this.template.runSchemes[timerIndex].timeId = this.timingId.value();
        this.template.runSchemes[timerIndex].type = this.timingType.value();
        
        // Only set the runImmediately, skipWeekend and skipDays if they are not null/false (Default value is false)
        if (this.timingRunImmediately.value() !== false) {
            this.template.runSchemes[timerIndex].runImmediately = this.timingRunImmediately.value();
        }
        else {
            delete this.template.runSchemes[timerIndex].runImmediately;
        }
        if (this.timingSkipWeekend.value() !== false) {
            this.template.runSchemes[timerIndex].skipWeekend = this.timingSkipWeekend.value();
        }
        else {
            delete this.template.runSchemes[timerIndex].skipWeekend;
        }
        if (this.timingSkipDays.value().length > 0) {
            this.template.runSchemes[timerIndex].skipDays = this.timingSkipDays.value().sort((a, b) => a - b).join(",");
        }
        else {
            delete this.template.runSchemes[timerIndex].skipDays;
        }
        
        // Check if any log settings are set
        if (this.timingLogMinimumLevel.value() !== "" || this.timingLogStartAndStop.value() !== false || this.timingLogRunStartAndStop.value() !== false || this.timingLogRunBody.value() !== false) {
            if (this.template.runSchemes[timerIndex].logSettings === undefined) {
                this.template.runSchemes[timerIndex].logSettings = {};
            }
            this.template.runSchemes[timerIndex].logSettings.logMinimumLevel = this.timingLogMinimumLevel.value();
            this.template.runSchemes[timerIndex].logSettings.logStartAndStop = this.timingLogStartAndStop.value();
            this.template.runSchemes[timerIndex].logSettings.logRunStartAndStop = this.timingLogRunStartAndStop.value();
            this.template.runSchemes[timerIndex].logSettings.logRunBody = this.timingLogRunBody.value();
        }
        else {
            // Remove the log settings if they are not set
            delete this.template.runSchemes[timerIndex].logSettings;
        }
        
        // Check the type and set the corresponding fields
        switch (this.timingType.value()) {
            case "Continuous":
                // Set the new values
                this.template.runSchemes[timerIndex].startTime = this.getTimeOnly(this.timingStart.value());
                this.template.runSchemes[timerIndex].stopTime = this.getTimeOnly(this.timingStop.value());
                this.template.runSchemes[timerIndex].delay = this.getTimeOnly(this.timingDelay.value());
                // Remove the fields that are not used
                delete this.template.runSchemes[timerIndex].hour;
                delete this.template.runSchemes[timerIndex].dayOfWeek;
                delete this.template.runSchemes[timerIndex].dayOfMonth;
                break;
            case "Daily":
                // Set the new values
                this.template.runSchemes[timerIndex].hour = this.getTimeOnly(this.timingHour.value());
                // Remove the fields that are not used
                delete this.template.runSchemes[timerIndex].startTime;
                delete this.template.runSchemes[timerIndex].stopTime;
                delete this.template.runSchemes[timerIndex].delay;
                delete this.template.runSchemes[timerIndex].dayOfWeek;
                delete this.template.runSchemes[timerIndex].dayOfMonth;
                break;
            case "Weekly":
                // Set the new values
                this.template.runSchemes[timerIndex].dayOfWeek = this.timingDayOfWeek.value();
                this.template.runSchemes[timerIndex].hour = this.getTimeOnly(this.timingHour.value());
                // Remove the fields that are not used
                delete this.template.runSchemes[timerIndex].startTime;
                delete this.template.runSchemes[timerIndex].stopTime;
                delete this.template.runSchemes[timerIndex].delay;
                delete this.template.runSchemes[timerIndex].dayOfMonth;
                break;
            case "Monthly":
                // Set the new values
                this.template.runSchemes[timerIndex].dayOfMonth = this.timingDayOfMonth.value();
                this.template.runSchemes[timerIndex].hour = this.getTimeOnly(this.timingHour.value());
                // Remove the fields that are not used
                delete this.template.runSchemes[timerIndex].startTime;
                delete this.template.runSchemes[timerIndex].stopTime;
                delete this.template.runSchemes[timerIndex].delay;
                delete this.template.runSchemes[timerIndex].dayOfWeek;
                break;
        }
        
        // Refresh the grid
        this.wtsTimersGrid.dataSource.read();
        
        // Close the popup
        this.wtsTimersForm.close();
    }
    
    getTimeOnly(time) {
        if (time === null || time === undefined || time === "") {
            return null;
        }
        const dateTime = new Date(time);
        const hours = dateTime.getHours();
        const minutes = dateTime.getMinutes();
        const seconds = dateTime.getSeconds();

        return `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
    }

    /**
     * Initializes all kendo components for the base class.
     */
    initializeKendoComponents() {
        $("#wtsTimingCreateButton").kendoButton();
        $("#wtsDebuggerButton").kendoButton();
        $("#saveButtonWtsConfiguration").kendoButton({
            icon: "save"
        });
        $("#saveAndDeployToTestButtonWtsConfiguration").kendoButton();

        // Don't initialize these windows here, because it will be initialized when the user clicks on the edit or create button
        $("#wts-timing-form").hide();
        $("#wts-action-form").hide();
        
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
            columns: [
                {
                    field: "timeId",
                    title: "ID"
                }, {
                    field: "type",
                    title: "Type"
                }, {
                    command: [
                        {
                            text: "Edit",
                            icon: "edit",
                            click: this.onEditTimer.bind(this)
                        },
                        {
                            text: "Delete",
                            icon: "delete",
                            className: "k-button-solid-primary delete-button deleteButton",
                            click: this.onDeleteTimer.bind(this)
                        }
                    ]
                }
            ]
        }).data("kendoGrid");

        this.timingId = $("#wts-timing-id").kendoNumericTextBox({
            format: "#",
            decimals: 0
        }).data("kendoNumericTextBox");
        
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
        
        this.timingSaveChanges = $("#wtsTimingSaveChanges").kendoButton({
            icon: "edit"
        }).data("kendoButton");

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
        
        this.wtsActionTimerId = $("#wts-action-timerid").kendoNumericTextBox({
            format: "#",
            decimals: 0
        }).data("kendoNumericTextBox");
        
        this.wtsActionOrder = $("#wts-action-order").kendoNumericTextBox({
            format: "#",
            decimals: 0
        }).data("kendoNumericTextBox");
    }
    
    initializeTimersFormWindow() {
        this.wtsTimersForm = $("#wts-timing-form").kendoWindow({
            title: "Nieuwe timer aanmaken",
            width: 800,
            height: 700,
            center: true,
            visible: false,
            modal: true,
            close: () => {
                // Refresh the grid
                console.log("Closed popup");
                this.selectedTimer = null;
            }
        }).data("kendoWindow");
        
        // TODO: FIX THIS STUFF
        // This part seems to do something atleast but it's not working properly yet
        // Swapping to another tab and back to this tab will cause the type and fields below to be all messed up
        // But closing the window and opening it again will fix it (partly)
        
        // // Uninitialize the type dropdownlist
        // this.timingType.destroy();
        //
        // // Reinitialize the type dropdownlist
        // this.timingType = $("#wts-timing-type").kendoDropDownList({
        //     optionLabel: "Selecteer type",
        //     dataTextField: "text",
        //     dataValueField: "value",
        //     change: this.onTimingTypeChange.bind(this),
        //     value: this.selectedTimer["type"],
        //     dataSource: [
        //         {text: "Continu", value: "Continuous"},
        //         {text: "Dagelijks", value: "Daily"},
        //         {text: "Wekelijks", value: "Weekly"},
        //         {text: "Maandelijks", value: "Monthly"}
        //     ]
        // }).data("kendoDropDownList");
    }

    initializeActionsFormWindow() {
        this.wtsActionForm = $("#wts-action-form").kendoWindow({
            title: "Nieuwe actie aanmaken",
            width: 800,
            height: 700,
            center: true,
            visible: false,
            modal: true,
            close: () => {
                // Refresh the grid
                console.log("Closed popup");
                this.selectedAction = null;
            }
        }).data("kendoWindow");
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
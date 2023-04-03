import { TrackJS } from "trackjs";
import { Wiser, Misc } from "../../Base/Scripts/Utils.js";
import "../../Base/Scripts/Processing.js";
import { Preview } from "./Preview.js";

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
     * @param {any} settings An object containing the settings for this class.
     */
    constructor(settings) {
        this.base = this;

        // Kendo components.
        this.mainSplitter = null;
        this.mainWindow = null;
        this.componentTypeComboBox = null;
        this.componentModeComboBox = null;
        this.selectedComponentData = null;
        this.saving = false;
        this.changesTimeout = null;

        // Default settings
        this.settings = {
            moduleId: 0,
            customerId: 0,
            username: "Onbekend",
            userEmailAddress: "",
            userType: ""
        };
        Object.assign(this.settings, settings);

        // Other.
        this.mainLoader = null;

        // Set the Kendo culture to Dutch. TODO: Base this on the language in Wiser.
        kendo.culture("nl-NL");

        // Add logged in user access token to default authorization headers for all jQuery ajax requests.
        $.ajaxSetup({
            headers: { "Authorization": `Bearer ${localStorage.getItem("accessToken")}` }
        });

        // Fire event on page ready for direct actions
        $(document).ready(() => {
            this.onPageReady();
        });
    }

    /**
     * Event that will be fired when the page is ready.
     */
    async onPageReady() {
        this.mainLoader = $("#mainLoader");

        // Setup processing.
        document.addEventListener("processing.Busy", this.toggleMainLoader.bind(this, true));
        document.addEventListener("processing.Idle", this.toggleMainLoader.bind(this, false));

        const process = `initialize_${Date.now()}`;
        window.processing.addProcess(process);

        // Fullscreen event for elements that can go fullscreen, such as HTML editors.
        const classHolder = $(document.documentElement);
        const fullscreenChange = "webkitfullscreenchange mozfullscreenchange fullscreenchange MSFullscreenChange";
        $(document).bind(fullscreenChange, $.proxy(classHolder.toggleClass, classHolder, "k-fullscreen"));

        // Setup any settings from the body element data. These settings are added via the Wiser backend and they take preference.
        Object.assign(this.settings, $("body").data());
        this.selectedId = this.settings.templateId;

        if (this.settings.trackJsToken) {
            TrackJS.install({
                token: this.settings.trackJsToken
            });
        }

        const user = JSON.parse(localStorage.getItem("userData"));
        this.settings.oldStyleUserId = user.oldStyleUserId;
        this.settings.username = user.adminAccountName ? `${user.adminAccountName} (Admin)` : user.name;
        this.settings.adminAccountLoggedIn = !!user.adminAccountName;

        const userData = await Wiser.getLoggedInUserData(this.settings.wiserApiRoot);
        this.settings.userId = userData.encryptedId;
        this.settings.customerId = userData.encryptedCustomerId;
        this.settings.zeroEncrypted = userData.zeroEncrypted;
        this.settings.filesRootId = userData.filesRootId;
        this.settings.imagesRootId = userData.imagesRootId;
        this.settings.templatesRootId = userData.templatesRootId;
        this.settings.mainDomain = userData.mainDomain;

        if (!this.settings.wiserApiRoot.endsWith("/")) {
            this.settings.wiserApiRoot += "/";
        }

        this.initializeKendoComponents();

        window.processing.removeProcess(process);
    }

    /**
     * Initializes all kendo components for the base class.
     */
    initializeKendoComponents() {
        window.popupNotification = $("#popupNotification").kendoNotification().data("kendoNotification");

        this.commitEnvironmentField = $("#wts-log-level").kendoDropDownList({
            optionLabel: "Selecteer log level",
            dataTextField: "text",
            dataValueField: "value",
            dataSource: [
                {text: "Debug", value: 1},
                {text: "Information", value: 2},
                {text: "Warning", value: 3},
                {text: "Error", value: 4},
                {text: "Critical", value: 5}
            ]
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
     * Shows or hides the main (full screen) loader.
     * @param {boolean} show True to show the loader, false to hide it.
     */
    toggleMainLoader(show) {
        this.mainLoader.toggleClass("loading", show);
    }
}
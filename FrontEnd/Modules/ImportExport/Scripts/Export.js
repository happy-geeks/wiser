import { TrackJS } from "trackjs";
import { Wiser2, Misc } from "../../Base/Scripts/Utils.js";
require("@progress/kendo-ui/js/kendo.all.js");
require("@progress/kendo-ui/js/cultures/kendo.culture.nl-NL.js");
require("@progress/kendo-ui/js/messages/kendo.messages.nl-NL.js");

import "../css/ImportExport.css";

// Any custom settings can be added here. They will overwrite most default settings inside the module.
const exportModuleSettings = {
};

((settings) => {
    /**
     * Main class.
     */
    class Export {
        /**
         * Initializes a new instance of AisDashboard.
         * @param {any} settings An object containing the settings for this class.
         */
        constructor(settings) {
            this.base = this;

            // Kendo components.
            this.mainWindow = null;

            this.mainLoader = null;

            this.exportHtml = null;

            // Set the Kendo culture to Dutch. TODO: Base this on the language in Wiser.
            kendo.culture("nl-NL");

            // Default settings
            this.settings = {
                customerId: 0,
                username: "Onbekend"
            };
            Object.assign(this.settings, settings);

            // Fire event on page ready for direct actions
            $(document).ready(() => {
                this.onPageReady();
            });
        }

        /**
         * Event that will be fired when the page is ready.
         */
        async onPageReady() {
            this.exportHtml = document.getElementById("ExportHtml");
            if (!this.exportHtml) {
                return;
            }
            
            // Add logged in user access token to default authorization headers for all jQuery ajax requests.
            $.ajaxSetup({
                headers: { "Authorization": `Bearer ${localStorage.getItem("access_token")}` }
            });

            const html = await Wiser2.api({ url: "/Modules/ImportExport/Export/Html" });
            this.exportHtml.insertAdjacentHTML("beforeend", html);

            this.mainLoader = $("#mainLoader");

            // Setup JJL processing.
            document.addEventListener("processing.Busy", this.toggleMainLoader.bind(this, true));
            document.addEventListener("processing.Idle", this.toggleMainLoader.bind(this, false));
            
            // Show an error if the user is no longer logged in.
            const accessTokenExpires = localStorage.getItem("access_token_expires_on");
            if (!accessTokenExpires || accessTokenExpires <= new Date()) {
                Wiser2.alert({
                    title: "Niet ingelogd",
                    content: "U bent niet (meer) ingelogd. Ververs a.u.b. de pagina en probeer het opnieuw."
                });
                
                this.toggleMainLoader(false);
                return;
            }

            // Setup any settings from the body element data. These settings are added via the Wiser backend and they take preference.
            Object.assign(this.settings, $("body").data());

            if (this.settings.trackJsToken) {
                TrackJS.install({
                    token: this.settings.trackJsToken
                });
            }
            
            const user = JSON.parse(localStorage.getItem("userData"));
            this.settings.oldStyleUserId = user.oldStyleUserId;
            this.settings.username = user.adminAccountName ? `Happy Horizon (${user.adminAccountName})` : user.name;
            this.settings.adminAccountLoggedIn = !!user.adminAccountName;
            
            const userData = await Wiser2.getLoggedInUserData(this.settings.wiserApiRoot, this.settings.isTestEnvironment);
            this.settings.userId = userData.encrypted_id;
            this.settings.customerId = userData.encrypted_customer_id;
            this.settings.zeroEncrypted = userData.zero_encrypted;
            
            this.settings.serviceRoot = `${this.settings.wiserApiRoot}templates/get-and-execute-query`;
            this.settings.getItemsUrl = `${this.settings.wiserApiRoot}data-selectors`;

            this.setupBindings();

            this.initializeKendoWindows();
            this.initializeKendoComponents(this.exportHtml);

            this.toggleMainLoader(false);
        }

        /**
         * Shows or hides the main (full screen) loader.
         * @param {boolean} show True to show the loader, false to hide it.
         */
        toggleMainLoader(show) {
            this.mainLoader.toggleClass("loading", show);
        }

        /**
         * Setup all basis bindings for this module.
         * Specific bindings (for buttons in certain pop-ups for example) will be set when they are needed.
         */
        setupBindings() {
            this.startExportButton = document.getElementById("startExportButton");
            this.startExportButton.addEventListener("click", this.performExport.bind(this));

            if (!window.importExport) {
                $(document).on("moduleClosing", (e) => {
                    // You can do anything here that needs to happen before closing the module.
                    e.success();
                });
            }
        }

        async performExport() {
            const exportType = $("[name='exportType']:checked").val();
            if (!exportType) {
                kendo.alert("Kies a.u.b. eerst een exporttype.");
                return;
            }

            switch (exportType) {
                case "dataselector": {
                    const dropDownList = $("#DataSelectorList").data("kendoDropDownList");
                    const dataItem = dropDownList.dataItem();
                    const dataSelectorId = dataItem.encrypted_id;
                    const fileName = `${dataItem.name}.xlsx`;

                    const process = `exportToExcel_${Date.now()}`;
                    jjl.processing.addProcess(process);
                    const result = await fetch(`${this.settings.getItemsUrl}/excel?encryptedDataSelectorId=${encodeURIComponent(dataSelectorId)}&fileName=${encodeURIComponent(fileName)}`, {
                        method: "POST",
                        headers: {
                            "Content-Type": "application/json",
                            "Authorization": `Bearer ${localStorage.getItem("access_token")}`
                        }
                    });
                    await Misc.downloadFile(result, fileName);
                    jjl.processing.removeProcess(process);
                    break;
                }
                case "query": {
                    const dropDownList = $("#QueryList").data("kendoDropDownList");
                    const dataItem = dropDownList.dataItem();
                    const queryId = dataItem.encrypted_id;
                    const fileName = `${dataItem.description}.xlsx`;

                    const process = `exportToExcel_${Date.now()}`;
                    jjl.processing.addProcess(process);
                    const result = await fetch(`${this.settings.getItemsUrl}/excel?queryid=${encodeURIComponent(queryId)}&fileName=${encodeURIComponent(fileName)}`, {
                        method: "POST",
                        headers: {
                            "Content-Type": "application/json",
                            "Authorization": `Bearer ${localStorage.getItem("access_token")}`
                        }
                    });
                    await Misc.downloadFile(result, fileName);
                    jjl.processing.removeProcess(process);
                    break;
                }
                case "module": {
                    const dropDownList = $("#ModuleList").data("kendoDropDownList");
                    const dataItem = dropDownList.dataItem();
                    
                    const moduleId = dataItem.module_id;
                    const fileName = `${dataItem.name}.xlsx`;
                    
                    const process = `exportToExcel_${Date.now()}`;
                    jjl.processing.addProcess(process);
                    const result = await fetch(`${this.settings.wiserApiRoot}modules/${moduleId}/export?fileName=${encodeURIComponent(fileName)}`, {
                        method: "GET",
                        headers: {
                            "Content-Type": "application/json",
                            "Authorization": `Bearer ${localStorage.getItem("access_token")}`
                        }
                    });
                    await Misc.downloadFile(result, fileName);
                    jjl.processing.removeProcess(process);
                    break;
                }
                default:
                    kendo.alert("Ongeldig exporttype geselecteerd.");
                    return;
            }
        }

        /**
         * Initializes all Kendo Window components for the base class.
         */
        initializeKendoWindows() {
            if (!window.importExport) {
                // The main window of the module.
                this.mainWindow = $("#window").kendoWindow({
                    width: "90%",
                    height: "90%",
                    title: false,
                    visible: true,
                    resizable: false
                }).data("kendoWindow").maximize().open();
            }
        }

        /**
         * Initializes all Kendo components for the base class.
         * @param {HTMLElement} context The context (HTML element) in which items will have their elements initialized with Kendo.
         */
        async initializeKendoComponents(context = null) {
            if (!context || !(context instanceof HTMLElement)) {
                context = document.body;
            }

            //COMBOBOX
            const process = `loadDropdowns_${Date.now()}`;
            jjl.processing.addProcess(process);

            try {
                const promiseResults = await Promise.all([
                    Wiser2.api({ url: `${this.settings.wiserApiRoot}data-selectors` }),
                    Wiser2.api({ url: `${this.settings.wiserApiRoot}queries/export-module` }),
                    Wiser2.api({ url: `${this.settings.wiserApiRoot}modules` })
                ]);
                const dataSelectors = promiseResults[0];
                const queries = promiseResults[1];
                const modules = promiseResults[2];

                if (!dataSelectors || !dataSelectors.length) {
                    $(context).find("#DataSelectorContainer").hide();
                } else {
                    $(context).find("#DataSelectorList").kendoDropDownList({
                        dataTextField: "name",
                        dataValueField: "id",
                        dataSource: dataSelectors
                    });
                }

                if (!queries || !queries.length) {
                    $(context).find("#QueryContainer").hide();
                } else {
                    $(context).find("#QueryList").kendoDropDownList({
                        dataTextField: "description",
                        dataValueField: "id",
                        dataSource: queries
                    });
                }

                if (!modules) {
                    $(context).find("#ModuleContainer").hide();
                } else {
                    let flatModulesList = [];
                    for (let key in modules) {
                        if (!modules.hasOwnProperty(key)) {
                            continue;
                        }

                        if (!key) {
                            modules[key] = modules[key].map(m => {
                                m.group = "Overige";
                                return m;
                            });
                        }

                        flatModulesList = flatModulesList.concat(modules[key].filter(m => m.name && m.type === "DynamicItems"));
                    }

                    $(context).find("#ModuleList").kendoDropDownList({
                        dataTextField: "name",
                        dataValueField: "module_id",
                        dataSource: {
                            data: flatModulesList,
                            group: { field: "group" }
                        }
                    });
                }
            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan. Probeer het a.u.b. opnieuw of neem contact op met ons.");
            }

            jjl.processing.removeProcess(process);

            //BUTTONS
            $(context).find(".saveButton").kendoButton({
                icon: "save"
            });

            $(context).find(".button").kendoButton();
        }
    }

    // Initialize the Export class and make one instance of it globally available.
    window.export = new Export(settings);
})(exportModuleSettings);
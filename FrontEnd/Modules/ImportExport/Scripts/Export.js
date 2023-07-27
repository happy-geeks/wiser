import {TrackJS} from "trackjs";
import {Misc, Wiser} from "../../Base/Scripts/Utils.js";
import "../../Base/Scripts/Processing.js";
import "../Css/ImportExport.css";

require("@progress/kendo-ui/js/kendo.all.js");
require("@progress/kendo-ui/js/cultures/kendo.culture.nl-NL.js");
require("@progress/kendo-ui/js/messages/kendo.messages.nl-NL.js");

// Any custom settings can be added here. They will overwrite most default settings inside the module.
const exportModuleSettings = {
};

((settings) => {
    /**
     * Main class.
     */
    class Export {
        /**
         * Initializes a new instance of Export.
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
                headers: { "Authorization": `Bearer ${localStorage.getItem("accessToken")}` }
            });

            const html = await Wiser.api({ url: "/Modules/ImportExport/Export/Html" });
            this.exportHtml.insertAdjacentHTML("beforeend", html);

            this.mainLoader = $("#mainLoader");

            // Setup processing.
            document.addEventListener("processing.Busy", this.toggleMainLoader.bind(this, true));
            document.addEventListener("processing.Idle", this.toggleMainLoader.bind(this, false));

            // Show an error if the user is no longer logged in.
            const accessTokenExpires = localStorage.getItem("accessTokenExpiresOn");
            if (!accessTokenExpires || accessTokenExpires <= new Date()) {
                Wiser.alert({
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
            this.settings.username = user.adminAccountName ? `${user.adminAccountName} (Admin)` : user.name;
            this.settings.adminAccountLoggedIn = !!user.adminAccountName;

            const userData = await Wiser.getLoggedInUserData(this.settings.wiserApiRoot);
            this.settings.userId = userData.encryptedId;
            this.settings.customerId = userData.encryptedCustomerId;
            this.settings.zeroEncrypted = userData.zeroEncrypted;

            this.settings.serviceRoot = `${this.settings.wiserApiRoot}templates/get-and-execute-query`;
            this.settings.getItemsUrl = `${this.settings.wiserApiRoot}data-selectors`;

            this.setupBindings();

            this.initializeKendoWindows();
            await this.initializeKendoComponents(this.exportHtml);

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
                document.addEventListener("moduleClosing", (event) => {
                    // You can do anything here that needs to happen before closing the module.
                    event.detail();
                });
            }
        }

        async performExport() {
            const exportType = $("[name='exportType']:checked").val();
            if (!exportType) {
                kendo.alert("Kies a.u.b. eerst een exporttype.");
                return;
            }

            const fileFormat = $("#fileFormatList").data("kendoDropDownList").dataItem();
            if (!fileFormat) {
                kendo.alert("Kies a.u.b. eerst een bestandsformaat");
                return;
            }

            const process = `exportToFile_${Date.now()}`;
            window.processing.addProcess(process);
            let result;
            let fileName;

            switch (exportType) {
                case "dataselector": {
                    const dropDownList = $("#DataSelectorList").data("kendoDropDownList");
                    const dataItem = dropDownList.dataItem();
                    const dataSelectorId = dataItem.encryptedId;
                    fileName = `${dataItem.name}.${fileFormat.extension}`;
                    
                    result = await fetch(`${this.settings.getItemsUrl}/${fileFormat.value}?encryptedDataSelectorId=${encodeURIComponent(dataSelectorId)}&fileName=${encodeURIComponent(fileName)}`, {
                        method: "POST",
                        headers: {
                            "Content-Type": "application/json",
                            "Authorization": `Bearer ${localStorage.getItem("accessToken")}`
                        }
                    });
                    break;
                }
                case "query": {
                    const dropDownList = $("#QueryList").data("kendoDropDownList");
                    const dataItem = dropDownList.dataItem();
                    const queryId = dataItem.encryptedId;
                    fileName = `${dataItem.description}.${fileFormat.extension}`;
                    
                    result = await fetch(`${this.settings.getItemsUrl}/${fileFormat.value}?queryid=${encodeURIComponent(queryId)}&fileName=${encodeURIComponent(fileName)}`, {
                        method: "POST",
                        headers: {
                            "Content-Type": "application/json",
                            "Authorization": `Bearer ${localStorage.getItem("accessToken")}`
                        }
                    });
                    break;
                }
                case "module": {
                    const dropDownList = $("#ModuleList").data("kendoDropDownList");
                    const dataItem = dropDownList.dataItem();

                    const moduleId = dataItem.moduleId;
                    fileName = `${dataItem.name}.${fileFormat.extension}`;

                    result = await fetch(`${this.settings.wiserApiRoot}modules/${moduleId}/export?fileName=${encodeURIComponent(fileName)}&fileFormat=${fileFormat.value}`, {
                        method: "GET",
                        headers: {
                            "Content-Type": "application/json",
                            "Authorization": `Bearer ${localStorage.getItem("accessToken")}`
                        }
                    });

                    break;
                }
                default:
                    window.processing.removeProcess(process);
                    kendo.alert("Ongeldig exporttype geselecteerd.");
                    return;
            }

            if (result.status !== 200) {
                const error = await result.text();
                kendo.alert(`Er is iets fout gegaan met het exporteren. Probeer het a.u.b. nogmaals of neem contact op met ons.<br>De fout was:<br>${error}`);
            }
            else {
                await Misc.downloadFile(result, fileName);
            }

            window.processing.removeProcess(process);
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
            window.processing.addProcess(process);

            try {
                const promiseResults = await Promise.all([
                    Wiser.api({ url: `${this.settings.wiserApiRoot}data-selectors?forExportModule=true` }),
                    Wiser.api({ url: `${this.settings.wiserApiRoot}queries/export-module` }),
                    Wiser.api({ url: `${this.settings.wiserApiRoot}modules` })
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

                        flatModulesList = flatModulesList.concat(modules[key].filter(m => m.name && m.type === "DynamicItems" && m.hasCustomQuery));
                    }

                    $(context).find("#ModuleList").kendoDropDownList({
                        dataTextField: "name",
                        dataValueField: "moduleId",
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

            window.processing.removeProcess(process);
            
            let fileFormatDataSource = [
                {
                    name: 'Excel',
                    value: 'excel',
                    extension: 'xlsx',
                },
                {
                    name: 'CSV',
                    value: 'csv',
                    extension: 'csv',
                }
            ]

            $(context).find('#fileFormatList').kendoDropDownList(
                {
                    dataTextField: "name",
                    dataValueField: "value",
                    dataSource: fileFormatDataSource
                }
            );

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
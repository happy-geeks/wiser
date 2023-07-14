import { TrackJS } from "trackjs";
import { Wiser } from "../../Base/Scripts/Utils.js";
require("@progress/kendo-ui/js/kendo.all.js");
require("@progress/kendo-ui/js/cultures/kendo.culture.nl-NL.js");
require("@progress/kendo-ui/js/messages/kendo.messages.nl-NL.js");

import "../Scss/configuration.scss";

// Any custom settings can be added here. They will overwrite most default settings inside the module.
const moduleSettings = {
    
};

((settings) => {
    /**
     * Main class.
     */
    class Configuration {

        /**
         * Initializes a new instance of Search.
         * @param {any} settings An object containing the settings for this class.
         */
        constructor(settings) {
            // Set the Kendo culture to Dutch. TODO: Base this on the language in Wiser.
            kendo.culture("nl-NL");

            // Default settings
            this.settings = {
                customerId: 0,
                username: "Onbekend"
            };
            Object.assign(this.settings, settings);
            
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
            this.isLoadedInIframe = window.parent && window.parent.main && window.parent.main.vueApp;

            // Setup processing.
            document.addEventListener("processing.Busy", this.toggleMainLoader.bind(this, true));
            document.addEventListener("processing.Idle", this.toggleMainLoader.bind(this, false));
            
            // Setup any settings from the body element data. These settings are added via the Wiser backend and they take preference.
            Object.assign(this.settings, $("body").data());

            if (this.settings.trackJsToken) {
                TrackJS.install({
                    token: this.settings.trackJsToken
                });
            }
            
            const user = JSON.parse(localStorage.getItem("userData"));
            this.settings.oldStyleUserId = user.oldStyleUserId;
            this.settings.adminAccountLoggedIn = !!user.adminAccountName;
            
            this.settings.serviceRoot = `${this.settings.wiserApiRoot}templates/get-and-execute-query`;
            
            const userData = await Wiser.getLoggedInUserData(this.settings.wiserApiRoot);
            this.settings.userId = userData.encryptedId;
            this.settings.plainUserId = userData.id;
            this.settings.zeroEncrypted = userData.zeroEncrypted;
            
            await this.toggleModules();
            
            this.toggleMainLoader(false);
        }

        /**
         * Setup all basis bindings for this module.
         * Specific bindings (for buttons in certain pop-ups for example) will be set when they are needed.
         */
        setupBindings() {
            document.addEventListener("moduleClosing", (event) => {
                // You can do anything here that needs to happen before closing the module.
                event.detail();
            });
        }

        /**
         * Shows or hides the main (full screen) loader.
         * @param {boolean} show True to show the loader, false to hide it.
         */
        toggleMainLoader(show) {
            this.mainLoader.toggleClass("loading", show);
        }

        /**
         * Gets all modules that the authenticated user can access.
         */
        async toggleModules() {
            // Get all modules from parent frame if we can, otherwise get them from API.
            if (this.isLoadedInIframe) {
                this.modules = window.parent.main.vueApp.modules;
            }
            
            if (!this.modules || !this.modules.length) {
                const modules = await Wiser.api({
                    url: `${this.settings.wiserApiRoot}modules`,
                    contentType: "application/json; charset=utf-8",
                    dataType: "json",
                    method: "GET"
                });

                this.modules = [];
                for (let groupName in modules) {
                    if (!modules.hasOwnProperty(groupName)) {
                        continue;
                    }

                    for (let module of modules[groupName]) {
                        if (!module.name) {
                            console.warn("Found module without name, so skipping it", module);
                            continue;
                        }

                        this.modules.push(module);
                    }
                }
            }
            
            // Only show modules that the user has access to and bind click events to them.
            for (let module of this.modules.filter(module => module.type !== "DynamicItems" && module.type !== "Import" && module.type !== "Export" && module.type !== "ImportExport")) {
                const moduleElement = $(`.group-item[data-module-type='${module.type}']`);
                moduleElement.removeClass("hidden");
                
                moduleElement.click((event) => {
                    event.preventDefault();
                    
                    if (!this.isLoadedInIframe) {
                        kendo.alert("Kan module niet openen omdat Wiser parent frame niet gevonden is. Ververs a.u.b. de pagina en probeer het opnieuw, of neem contact op met ons.");
                        return;
                    }
                    
                    window.parent.postMessage({
                        action: "OpenModule",
                        actionData: {
                            moduleId: module.moduleId
                        }
                    });
                });
            }
            
            // Exception for import/export module, because it can be opened 3 different ways.
            const importExportModules = this.modules.filter(module => module.type === "Import" || module.type === "Export" || module.type === "ImportExport");
            if (importExportModules.length > 0) {
                const moduleElement = $(`.group-item[data-module-type='ImportExport']`);
                moduleElement.removeClass("hidden");
                
                let modules = importExportModules.filter(module => module.fileName === "");
                if (!modules.length) {
                    modules = importExportModules;
                }
                
                moduleElement.click((event) => {
                    event.preventDefault();

                    if (!this.isLoadedInIframe) {
                        kendo.alert("Kan module niet openen omdat Wiser parent frame niet gevonden is. Ververs a.u.b. de pagina en probeer het opnieuw, of neem contact op met ons.");
                        return;
                    }

                    for (let module of modules) {
                        window.parent.postMessage({
                            action: "OpenModule",
                            actionData: {
                                moduleId: module.moduleId
                            }
                        });
                    }
                });
            }

            // Bind click events to other functionality.
            $(`.group-item[data-action]`).click((event) => {
                event.preventDefault();
                
                if (!this.isLoadedInIframe) {
                    kendo.alert("Kan module niet openen omdat Wiser parent frame niet gevonden is. Ververs a.u.b. de pagina en probeer het opnieuw, of neem contact op met ons.");
                    return;
                }

                window.parent.postMessage({
                    action: event.currentTarget.dataset.action
                });
            });

            // Modules that are only available for admins.
            $(`.group-item[data-requires-admin='true']`).toggleClass("hidden", !this.settings.adminAccountLoggedIn);
        }
    }

    // Initialize the Search class and make one instance of it globally available.
    window.configuration = new Configuration(settings);
})(moduleSettings);
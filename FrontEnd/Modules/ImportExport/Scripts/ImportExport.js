import { TrackJS } from "trackjs";
import { RemoveItems } from "./RemoveItems.js";
import { RemoveConnections } from "./RemoveConnections.js";

require("@progress/kendo-ui/js/kendo.all.js");
require("@progress/kendo-ui/js/cultures/kendo.culture.nl-NL.js");
require("@progress/kendo-ui/js/messages/kendo.messages.nl-NL.js");

import "../Css/ImportExport.css";

// Any custom settings can be added here. They will overwrite most default settings inside the module.
const importModuleSettings = {
};

((settings) => {
    /**
     * Main class.
     */
    class ImportExport {
        /**
         * Initializes a new instance of ImportExport.
         * @param {any} settings An object containing the settings for this class.
         */
        constructor() {
            this.base = this;

            // Kendo components.
            this.mainWindow = null;
            this.tabStrip = null;

            // Set the Kendo culture to Dutch. TODO: Base this on the language in Wiser.
            kendo.culture("nl-NL");

            // Default settings
            this.settings = {
                customerId: 0,
                username: "Onbekend",
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
            // Setup any settings from the body element data. These settings are added via the Wiser backend and they take preference.
            Object.assign(this.settings, $("body").data());

            if (this.settings.trackJsToken) {
                TrackJS.install({
                    token: this.settings.trackJsToken
                });
            }

            this.setupBindings();
            this.initializeKendoComponents();
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
         * Initializes all kendo components for the base class.
         */
        initializeKendoComponents() {
            // The main window of the module.
            this.mainWindow = $("#window").kendoWindow({
                width: "90%",
                height: "90%",
                title: false,
                visible: true,
                resizable: false
            }).data("kendoWindow").maximize().open();

            this.tabStrip = $("#tabstrip").kendoTabStrip({
                animation: {
                    open: {
                        effects: "fadeIn"
                    }
                }
            }).data("kendoTabStrip");

            this.tabStrip.select(0);
        }
    }
    
    // Initialize the classes and make one instance of them globally available.
    window.importExport = new ImportExport();
    window.removeItems = new RemoveItems(settings);
    window.removeConnections = new RemoveConnections(settings);
})(importModuleSettings);
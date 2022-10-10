import { TrackJS } from "trackjs";
import { Wiser, Misc } from "../../Base/Scripts/Utils.js";
import "../../Base/Scripts/Processing.js";
require("@progress/kendo-ui/js/kendo.all.js");
require("@progress/kendo-ui/js/cultures/kendo.culture.nl-NL.js");
require("@progress/kendo-ui/js/messages/kendo.messages.nl-NL.js");

import "../css/Communication.css";

// Any custom settings can be added here. They will overwrite most default settings inside the module.
const communicationModuleSettings = {
};

((settings) => {
    /**
     * Main class.
     */
    class Communication {
        /**
         * Initializes a new instance of Communication.
         * @param {any} settings An object containing the settings for this class.
         */
        constructor(settings) {
            this.base = this;
            this.mainLoader = null;
            
            // Kendo components.
            this.mainTabStrip = null;
            this.dataSelectorDropDown = null;
            this.queryDropDown = null;
            this.mailTemplateDropDown = null;

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
            // Add logged in user access token to default authorization headers for all jQuery ajax requests.
            $.ajaxSetup({
                headers: { "Authorization": `Bearer ${localStorage.getItem("accessToken")}` }
            });

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
            this.settings.username = user.adminAccountName ? `Happy Horizon (${user.adminAccountName})` : user.name;
            this.settings.adminAccountLoggedIn = !!user.adminAccountName;

            const userData = await Wiser.getLoggedInUserData(this.settings.wiserApiRoot);
            this.settings.userId = userData.encryptedId;
            this.settings.customerId = userData.encryptedCustomerId;
            this.settings.zeroEncrypted = userData.zeroEncrypted;

            this.settings.serviceRoot = `${this.settings.wiserApiRoot}templates/get-and-execute-query`;
            this.settings.getItemsUrl = `${this.settings.wiserApiRoot}data-selectors`;

            this.setupBindings();

            await this.initializeKendoComponents();

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
            document.addEventListener("moduleClosing", (event) => {
                // You can do anything here that needs to happen before closing the module.
                event.detail();
            });
        }

        /**
         * Initializes all Kendo components for the base class.
         * @param {HTMLElement} context The context (HTML element) in which items will have their elements initialized with Kendo.
         */
        async initializeKendoComponents() {
            const process = `loadDropdowns_${Date.now()}`;
            window.processing.addProcess(process);
            
            this.mainTabStrip = $("#tabStrip").kendoTabStrip({
                animation: {
                    open: {
                        effects: "fadeIn"
                    }
                }
            }).data("kendoTabStrip");

            try {
                const promiseResults = await Promise.all([
                    Wiser.api({ url: `${this.settings.wiserApiRoot}data-selectors?forCommunicationModule=true` }),
                    Wiser.api({ url: `${this.settings.wiserApiRoot}queries/communication-module` }),
                    Wiser.api({ url: `${this.settings.wiserApiRoot}items?entityType=mailtemplate` })
                ]);
                
                const dataSelectors = promiseResults[0];
                const queries = promiseResults[1];
                const mailTemplates = promiseResults[2];

                if (!dataSelectors || !dataSelectors.length) {
                    $("#DataSelectorContainer").hide();
                } else {
                    this.dataSelectorDropDown = $("#DataSelectorList").kendoDropDownList({
                        dataTextField: "name",
                        dataValueField: "id",
                        dataSource: dataSelectors
                    }).data("kendoDropDownList");
                }

                if (!queries || !queries.length) {
                    $("#QueryContainer").hide();
                } else {
                    this.queryDropDown = $("#QueryList").kendoDropDownList({
                        dataTextField: "description",
                        dataValueField: "id",
                        dataSource: queries
                    }).data("kendoDropDownList");
                }

                this.mailTemplateDropDown = $("#MailTemplateDropDown").kendoDropDownList({
                    dataTextField: "title",
                    dataValueField: "id",
                    dataSource: mailTemplates.results
                }).data("kendoDropDownList");
            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan. Probeer het a.u.b. opnieuw of neem contact op met ons.");
            }

            window.processing.removeProcess(process);
        }
    }

    // Initialize the Communication class and make one instance of it globally available.
    window.communication = new Communication(settings);
})(communicationModuleSettings);
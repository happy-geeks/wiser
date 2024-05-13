import { TrackJS } from "trackjs";
import { Wiser, Misc } from "../../Base/Scripts/Utils.js";
import "../../Base/Scripts/Processing.js";
require("@progress/kendo-ui/js/kendo.all.js");
require("@progress/kendo-ui/js/cultures/kendo.culture.nl-NL.js");
require("@progress/kendo-ui/js/messages/kendo.messages.nl-NL.js");

import "../Css/Index.css";

// Any custom settings can be added here. They will overwrite most default settings inside the module.
const communicationModuleSettings = {
};

((settings) => {
    /**
     * Main class.
     */
    class CommunicationIndex {
        /**
         * Initializes a new instance of Communication.
         * @param {any} settings An object containing the settings for this class.
         */
        constructor(settings) {
            this.base = this;
            this.mainLoader = null;
            
            // Components.
            this.createNewCommunicationSettingsButton = null;
            this.openCommunicationSettingsButton = null;
            this.communicationSettingsDropDown = null;

            // Set the Kendo culture to Dutch. TODO: Base this on the language in Wiser.
            kendo.culture("nl-NL");

            // Default settings
            this.settings = {
                tenantId: 0,
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
            this.settings.username = user.adminAccountName ? `${user.adminAccountName} (Admin)` : user.name;
            this.settings.adminAccountLoggedIn = !!user.adminAccountName;

            const userData = await Wiser.getLoggedInUserData(this.settings.wiserApiRoot);
            this.settings.userId = userData.encryptedId;
            this.settings.tenantId = userData.encryptedTenantId;
            this.settings.zeroEncrypted = userData.zeroEncrypted;

            this.settings.serviceRoot = `${this.settings.wiserApiRoot}templates/get-and-execute-query`;
            this.settings.getItemsUrl = `${this.settings.wiserApiRoot}data-selectors`;

            this.setupBindings();

            await this.initializeComponents();

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
         */
        async initializeComponents() {
            const process = `loadDropdowns_${Date.now()}`;
            window.processing.addProcess(process);
            
            try {
                // Dropdown with existing communication settings.
                const communicationSettings = await Wiser.api({url: `${this.settings.wiserApiRoot}communications?namesOnly=true`});
                
                if (!communicationSettings || !communicationSettings.length) {
                    $("#OpenCommunicationSettingsContainer").hide();
                    $("#CreateNewCommunicationSettingsContainer").removeClass("col-6").addClass("col-12");
                } else {
                    this.communicationSettingsDropDown = $("#CommunicationSettingsDropDown").kendoDropDownList({
                        optionLabel: "Selecteer een reeds bestaande communicatie-uiting",
                        dataTextField: "name",
                        dataValueField: "id",
                        dataSource: communicationSettings
                    }).data("kendoDropDownList");
                }
                
                // Buttons.
                this.createNewCommunicationSettingsButton = $("#CreateNewCommunicationSettingsButton").kendoButton({
                    icon: "folder-add",
                    click: this.onCreateNewCommunicationSettingsButton.bind(this)
                });
                
                this.openCommunicationSettingsButton = $("#OpenCommunicationSettingsButton").kendoButton({
                    icon: "folder-open",
                    click: this.onOpenCommunicationSettingsButtonClick.bind(this)
                });
            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan. Probeer het a.u.b. opnieuw of neem contact op met ons.");
            } finally {
                window.processing.removeProcess(process);
            }
        }

        /**
         * Event for when the user clicks the button to create new communication settings.
         * @param event The event of the Kendo Button click.
         */
        onCreateNewCommunicationSettingsButton(event) {
            // Async doesn't work for kendo button events.
            kendo.prompt("Vul een naam in voor de nieuwe communicatie-uiting").then((newName) => {
                if (!newName) {
                    return;
                }
                
                window.location = `/Modules/Communication/Settings?settingsName=${encodeURIComponent(newName)}`;
            });
        }

        /**
         * Event for when the user clicks the button to open existing communication settings.
         * @param event The event of the Kendo Button click.
         */
        onOpenCommunicationSettingsButtonClick(event) {
            const selectedId = this.communicationSettingsDropDown.value();
            if (!selectedId) {
                kendo.alert("Kies a.u.b. eerst een waarde uit de lijst.");
                return;
            }

            window.location = `/Modules/Communication/Settings?settingsId=${encodeURIComponent(selectedId)}`;
        }
    }

    // Initialize the Communication class and make one instance of it globally available.
    window.communicationIndex = new CommunicationIndex(settings);
})(communicationModuleSettings);
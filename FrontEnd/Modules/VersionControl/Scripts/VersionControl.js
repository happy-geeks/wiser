import { TrackJS } from "trackjs";
import { Wiser } from "../../Base/Scripts/Utils";
import "../../Base/Scripts/Processing.js";
import "../Css/VersionControl.css"

window.JSZip = require("jszip");

require("@progress/kendo-ui/js/kendo.tabstrip.js");
require("@progress/kendo-ui/js/kendo.tooltip.js");
require("@progress/kendo-ui/js/kendo.button.js");
require("@progress/kendo-ui/js/kendo.dialog.js");
require("@progress/kendo-ui/js/kendo.grid.js");
require("@progress/kendo-ui/js/cultures/kendo.culture.nl-NL.js");
require("@progress/kendo-ui/js/messages/kendo.messages.nl-NL.js");

// Any custom settings can be added here. They will overwrite most default settings inside the module.
const moduleSettings = {

};

((settings) => {
    
    class VersionControl {
        /**
         * Initializes a new instance of DynamicItems.
         * @param {any} settings An object containing the settings for this class.
         */
        constructor(settings) {
            this.base = this;

            // Components.
            this.mainLoader = null;
            this.commitEnvironmentField = null;
            this.commitDescriptionField = null;
            this.templateChangesGrid = null;
            this.dynamicContentChangesGrid = null;
            this.mainTabStrip = null;
            this.commitButton = null;
            this.deployGrid = null;

            // Set the Kendo culture to Dutch. TODO: Base this on the language in Wiser.
            kendo.culture("nl-NL");

            // Default settings
            this.settings = {
                customerId: 0,
                username: "Onbekend",
                userEmailAddress: "",
                userType: ""
            };
            Object.assign(this.settings, settings);

            // Add logged in user access token to default authorization headers for all jQuery ajax requests.
            $.ajaxSetup({
                headers: { "Authorization": `Bearer ${localStorage.getItem("accessToken")}` }
            });

            // Fire event on page ready for direct actions
            $(document).ready(async () => {
                await this.onPageReady();
            });
        }

        /**
         * Event that will be fired when the DOM is ready.
         */
        async onPageReady() {
            this.mainLoader = $("#mainLoader");

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
            this.settings.username = user.adminAccountName ? `Happy Horizon (${user.adminAccountName})` : user.name;
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

            // Initialize sub classes.
            await this.initializeComponents();
            
            this.toggleMainLoader(false);
        }

        /**
         * Initialize all components on the first tab.
         */
        async initializeComponents() {
            this.commitDescriptionField = document.getElementById("commitDescription");
            
            // Tab strip.
            this.mainTabStrip = $("#tabstrip").kendoTabStrip({
                activate: this.onMainTabStripActivate.bind(this)
            }).data("kendoTabStrip");

            // Commit button
            this.commitButton = $("#commitButton").kendoButton({
                click: this.onCommit.bind(this),
                icon: "save"
            }).data();
            
            this.commitEnvironmentField = $("#commitEnvironment").kendoDropDownList({
                optionLabel: "Selecteer omgeving",
                dataTextField: "text",
                dataValueField: "value",
                dataSource: [
                    { text: "Test", value: 2 },
                    { text: "Acceptatie", value: 4 },
                    { text: "Live", value: 8 }
                ]
            }).data("kendoDropDownList");
            
            // noinspection ES6MissingAwait
            this.setupTemplateChangesGrid();
            // noinspection ES6MissingAwait
            this.setupDynamicContentChangesGrid();
        }

        /**
         * Shows or hides the main (full screen) loader.
         * @param {boolean} show True to show the loader, false to hide it.
         */
        toggleMainLoader(show) {
            this.mainLoader.toggleClass("loading", show);
        }

        /**
         * Event for when the user switches to a different tab.
         * This will initialize all components on that tab, if the tab was opened for the first time.
         * @param event The activate event of the kendoTabStrip component.
         */
        async onMainTabStripActivate(event) {
            switch (event.sender.select().attr("id")) {
                case "deployTab":
                    await this.setupDeployGrid();
                    break;
            }
        }

        /**
         * Event for when the user clicks the commit button in the first tab.
         * @param event The click event of the kendoButton component.
         */
        async onCommit(event) {
            const initialProcess = `CreateCommit_${Date.now()}`;
            window.processing.addProcess(initialProcess);

            try {
                event.preventDefault();
                const selectedTemplates = this.templateChangesGrid.getSelectedData();
                const selectedDynamicContent = this.dynamicContentChangesGrid.getSelectedData();

                const result = await Wiser.api({
                    url: `${this.base.settings.wiserApiRoot}version-control`,
                    method: "POST",
                    contentType: "application/json",
                    data: JSON.stringify({
                        environment: this.commitEnvironmentField.value(),
                        description: this.commitDescriptionField.value,
                        templates: selectedTemplates.map(t => {
                            return {templateId: t.templateId, version: t.version};
                        }),
                        dynamicContents: selectedDynamicContent.map(d => {
                            return {dynamicContentId: d.dynamicContentId, version: d.version};
                        })
                    })
                });

                this.templateChangesGrid.dataSource.read();
                this.dynamicContentChangesGrid.dataSource.read();
                this.commitDescriptionField.value = "";
                this.commitEnvironmentField.value("");
            }
            catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan met het maken van de commit. Probeer het a.u.b. opnieuw of neem contact op als dat niet werkt.");
            }

            window.processing.removeProcess(initialProcess);
        }

        /**
         * Setup/initialize the grid with all uncommitted template changes.
         */
        async setupTemplateChangesGrid() {
            try {
                if (this.templateChangesGrid) {
                    return;
                }
                
                const gridSettings = {
                    dataSource: {
                        transport: {
                            read: async (transportOptions) => {
                                const initialProcess = `GetTemplatesToCommit_${Date.now()}`;
                                window.processing.addProcess(initialProcess);

                                try {
                                    const templatesToCommit = await Wiser.api({
                                        url: `${this.base.settings.wiserApiRoot}version-control/templates-to-commit`,
                                        method: "GET",
                                        contentType: "application/json"
                                    });

                                    transportOptions.success(templatesToCommit);
                                } catch (exception) {
                                    console.error(exception);
                                    kendo.alert("Er is iets fout gegaan met het laden van de wijzigingen in templates. Sluit a.u.b. deze module, open deze daarna opnieuw en probeer het vervolgens opnieuw. Of neem contact op als dat niet werkt.");
                                    transportOptions.error(exception);
                                }

                                window.processing.removeProcess(initialProcess);
                            }
                        },
                        schema: {
                            model: {
                                id: "templateId",
                                fields: {
                                    changedOn: {
                                        type: "date"
                                    }
                                }
                            }
                        }
                    },
                    selectable: "multiple, row",
                    columns: [
                        {
                            "field": "templateId",
                            "title": "ID",
                            "width": "50px"
                        },
                        {
                            "field": "templateType",
                            "title": "Type",
                            "width": "75px"
                        },
                        {
                            "field": "templateParentName",
                            "title": "Map",
                            "width": "150px"
                        },
                        {
                            "field": "templateName",
                            "title": "Template",
                            "width": "150px"
                        },
                        {
                            "field": "version",
                            "title": "Versie",
                            "width": "75px"
                        },
                        {
                            "field": "versionTest",
                            "title": "Versie test",
                            "width": "75px"
                        },
                        {
                            "field": "versionAcceptance",
                            "title": "Versie acceptatie",
                            "width": "100px"
                        },
                        {
                            "field": "versionLive",
                            "title": "Versie live",
                            "width": "75px"
                        },
                        {
                            "field": "changedOn",
                            "format": "{0:dd-MM-yyyy HH:mm:ss}",
                            "title": "Datum",
                            "width": "100px"
                        },
                        {
                            "field": "changedBy",
                            "title": "Door",
                            "width": "100px"
                        }
                    ]
                };

                this.templateChangesGrid = $("#templateChangesGrid").kendoGrid(gridSettings).data("kendoGrid");

            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan met het laden van de wijzigingen in templates. Sluit a.u.b. deze module, open deze daarna opnieuw en probeer het vervolgens opnieuw. Of neem contact op als dat niet werkt.");
            }
        }

        /**
         * Setup/initialize the grid with all uncommitted dynamic content changes.
         */
        async setupDynamicContentChangesGrid() {
            try {
                if (this.dynamicContentChangesGrid) {
                    return;
                }

                const gridSettings = {
                    dataSource: {
                        transport: {
                            read: async (transportOptions) => {
                                const initialProcess = `GetDynamicContentToCommit_${Date.now()}`;
                                window.processing.addProcess(initialProcess);

                                try {
                                    const templatesToCommit = await Wiser.api({
                                        url: `${this.base.settings.wiserApiRoot}version-control/dynamic-content-to-commit`,
                                        method: "GET",
                                        contentType: "application/json"
                                    });

                                    transportOptions.success(templatesToCommit);
                                } catch (exception) {
                                    console.error(exception);
                                    kendo.alert("Er is iets fout gegaan met het laden van de wijzigingen in templates. Sluit a.u.b. deze module, open deze daarna opnieuw en probeer het vervolgens opnieuw. Of neem contact op als dat niet werkt.");
                                    transportOptions.error(exception);
                                }

                                window.processing.removeProcess(initialProcess);
                            }
                        },
                        schema: {
                            model: {
                                id: "dynamicContentId",
                                fields: {
                                    changedOn: {
                                        type: "date"
                                    },
                                    templateNames: {
                                        type: "array"
                                    }
                                }
                            }
                        }
                    },
                    selectable: "multiple, row",
                    columns: [
                        {
                            "field": "dynamicContentId",
                            "title": "ID",
                            "width": "50px"
                        },
                        {
                            "field": "templateNames",
                            "title": "Gebruikt in",
                            "width": "150px",
                            "template": "#: templateNames ? templateNames.join(', ') : '' #"
                        },
                        {
                            "field": "title",
                            "title": "Naam",
                            "width": "150px"
                        },
                        {
                            "field": "version",
                            "title": "Versie",
                            "width": "75px"
                        },
                        {
                            "field": "versionTest",
                            "title": "Versie test",
                            "width": "75px"
                        },
                        {
                            "field": "versionAcceptance",
                            "title": "Versie acceptatie",
                            "width": "100px"
                        },
                        {
                            "field": "versionLive",
                            "title": "Versie live",
                            "width": "75px"
                        },
                        {
                            "field": "changedOn",
                            "format": "{0:dd-MM-yyyy HH:mm:ss}",
                            "title": "Datum",
                            "width": "100px"
                        },
                        {
                            "field": "changedBy",
                            "title": "Door",
                            "width": "100px"
                        }
                    ]
                };

                this.dynamicContentChangesGrid = $("#dynamicContentChangesGrid").kendoGrid(gridSettings).data("kendoGrid");

            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan met het laden van de wijzigingen in templates. Sluit a.u.b. deze module, open deze daarna opnieuw en probeer het vervolgens opnieuw. Of neem contact op als dat niet werkt.");
            }
        }

        /**
         * Setup/initialize the grid with all unfinished commits (commits that haven't been deployed to live yet).
         */
        async setupDeployGrid() {
            try {
                if (this.deployGrid) {
                    return;
                }

                const gridSettings = {
                    dataSource: {
                        transport: {
                            read: async (transportOptions) => {
                                const initialProcess = `GetNotCompletedCommits_${Date.now()}`;
                                window.processing.addProcess(initialProcess);

                                try {
                                    const templatesToCommit = await Wiser.api({
                                        url: `${this.base.settings.wiserApiRoot}version-control/not-completed-commits`,
                                        method: "GET",
                                        contentType: "application/json"
                                    });

                                    transportOptions.success(templatesToCommit);
                                } catch (exception) {
                                    console.error(exception);
                                    kendo.alert("Er is iets fout gegaan met het laden van niet afgeronde commits. Sluit a.u.b. deze module, open deze daarna opnieuw en probeer het vervolgens opnieuw. Of neem contact op als dat niet werkt.");
                                    transportOptions.error(exception);
                                }

                                window.processing.removeProcess(initialProcess);
                            }
                        },
                        schema: {
                            model: {
                                id: "id",
                                fields: {
                                    addedOn: {
                                        type: "date"
                                    },
                                    templateNames: {
                                        type: "array"
                                    }
                                }
                            }
                        }
                    },
                    selectable: "multiple, row",
                    columns: [
                        {
                            "field": "id",
                            "title": "ID",
                            "width": "50px"
                        },
                        {
                            "field": "description",
                            "title": "Omschrijving",
                            "width": "150px"
                        },
                        {
                            "field": "isTest",
                            "title": "Test",
                            "width": "50px"
                        },
                        {
                            "field": "isAcceptance",
                            "title": "Acceptatie",
                            "width": "50px"
                        },
                        {
                            "field": "isLive",
                            "title": "Live",
                            "width": "50px"
                        },
                        {
                            "field": "addedOn",
                            "format": "{0:dd-MM-yyyy HH:mm:ss}",
                            "title": "Datum",
                            "width": "100px"
                        },
                        {
                            "field": "addedBy",
                            "title": "Door",
                            "width": "100px"
                        }
                    ]
                };

                this.deployGrid = $("#deployGrid").kendoGrid(gridSettings).data("kendoGrid");

            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan met het laden van de nog niet afgeronde commits. Sluit a.u.b. deze module, open deze daarna opnieuw en probeer het vervolgens opnieuw. Of neem contact op als dat niet werkt.");
            }
        }
    }

    window.versionControl = new VersionControl(settings);
})(moduleSettings);
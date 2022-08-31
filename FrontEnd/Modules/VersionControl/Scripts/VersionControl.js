import {TrackJS} from "trackjs";
import {Wiser} from "../../Base/Scripts/Utils";
import {Grids} from "./Grids";
import "../../Base/Scripts/Processing.js";
import "../Css/VersionControl.css"

window.JSZip = require("jszip");

require("@progress/kendo-ui/js/kendo.tabstrip.js");

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

            // Sub classes.
            this.grids = null;
            this.commit = null;
            this.template = null;

            this.newItemId = null;

            // Kendo components.
            this.notification = null;

            // Other.
            this.mainLoader = null;

            // Set the Kendo culture to Dutch. TODO: Base this on the language in Wiser.
            kendo.culture("nl-NL");

            // Flags to use in wiser_field_templates, so that we can add code there that depends on code in this file, without having to deploy this to live right away.
            this.fieldTemplateFlags = {
                enableSubEntitiesGridsOrdering: true
            };

            // Default settings
            this.settings = {
                moduleId: 6001,
                encryptedModuleId: "",
                customerId: 0,
                initialItemId: null,
                iframeMode: false,
                gridViewMode: false,
                openGridItemsInBlock: false,
                username: "Onbekend",
                userEmailAddress: "",
                userType: ""
            };
            Object.assign(this.settings, settings);

            this.permissionsEnum = Object.freeze({
                read: 1 << 0,
                create: 1 << 1,
                update: 1 << 2,
                delete: 1 << 3
            });

            this.environmentsEnum = Object.freeze({
                hidden: 0,
                development: 1 << 0,
                test: 1 << 1,
                acceptance: 1 << 2,
                live: 1 << 3
            });

            this.comparisonOperatorsEnum = Object.freeze({
                equals: "eq",
                doesNotEqual: "neq",
                contains: "contains",
                doesNotContain: "doesnotcontain",
                startsWith: "startswith",
                doesNotStartWith: "doesnotstartwith",
                endsWith: "endswith",
                doesNotEndWith: "doesnotendwith",
                isEmpty: "isempty",
                isNotEmpty: "isnotempty",
                isGreaterThan: "gt",
                isGreaterThanOrEquals: "gte",
                isLessThan: "lt",
                isLessThanOrEquals: "lte"
            });

            this.dependencyActionsEnum = Object.freeze({
                toggleVisibility: "toggle-visibility",
                refresh: "refresh"
            });

            // Tabstrip
            this.mainTabStrip = $("#tabstrip").kendoTabStrip({
                animation: {
                    open: {
                        effects: "fadeIn"
                    }
                }
            }).data("kendoTabStrip");
            
            // Commit button
            this.commitButton = $("#commit-button").kendoButton({
                click: this.onCommit.bind(this),
                icon: "save"
            }).data();
            
            // Click action for each checkbox. 
            document.querySelectorAll("#commit-data .commit-checkbox").forEach(x => {
                x.addEventListener("click", (event) => {
                    // (Un)check all checkboxes when commit to live is checked
                    if (event.target.id === "commit-live") {
                        document.querySelector("#commit-test").checked = event.target.checked;
                        document.querySelector("#commit-stage").checked = event.target.checked;
                    }
                });
            });

             // Create instances of sub classes.
            this.grids = new Grids(this);
            // this.commit = new Commit(this);
            // this.template = new Template(this);

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
         * Event that will be fired when the page is ready.
         */
        async onPageReady() {
            this.mainLoader = $("#mainLoader");

            // Setup processing.
            document.addEventListener("processing.Busy", this.toggleMainLoader.bind(this, true));
            document.addEventListener("processing.Idle", this.toggleMainLoader.bind(this, false));

            // Fullscreen event for elements that can go fullscreen, such as HTML editors.
            const classHolder = $(document.documentElement);
            const fullscreenChange = "webkitfullscreenchange mozfullscreenchange fullscreenchange MSFullscreenChange";
            $(document).bind(fullscreenChange, $.proxy(classHolder.toggleClass, classHolder, "k-fullscreen"));

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

            console.log(this.settings.wiserApiRoot);

            if (!this.settings.wiserApiRoot.endsWith("/")) {
                this.settings.wiserApiRoot += "/";
            }
            this.settings.serviceRoot = `${this.settings.wiserApiRoot}templates/get-and-execute-query`;
            this.settings.htmlEditorCssUrl = `${this.settings.wiserApiRoot}templates/css-for-html-editors?encryptedCustomerId=${encodeURIComponent(this.base.settings.customerId)}&isTest=${this.base.settings.isTestEnvironment}&encryptedUserId=${encodeURIComponent(this.base.settings.userId)}&username=${encodeURIComponent(this.base.settings.username)}&userType=${encodeURIComponent(this.base.settings.userType)}&subDomain=${encodeURIComponent(this.base.settings.subDomain)}`
            this.settings.getItemsUrl = `${this.settings.wiserApiRoot}data-selectors`;
            $("body").toggleClass("gridViewMode", this.settings.gridViewMode);

            //this.setupBindings();
            //this.initializeKendoComponents();

            // Initialize sub classes.
            await this.grids.initialize();
            //this.commit.initialize();
            //this.template.initialize();

            if (this.settings.iframeMode && this.settings.hideHeader) {
                $("#tabstrip > ul").addClass("hidden");
            }
            if (this.settings.iframeMode && this.settings.hideFooter) {
                $("#right-pane > footer").addClass("hidden");
            }

            this.toggleMainLoader(false);
        }
        
        async onCommit(event) {
            let results = [];
            const selectedData = this.grids.getSelectedData();
            console.log("selectedData", selectedData);
            
            for (const x of selectedData) {
                results.push(await this.dynamicContentInTemplates(x.templateId));
            }
        }

        async dynamicContentInTemplates(templateId) {
            try {
                return await Wiser.api({
                    url: `${this.base.settings.wiserApiRoot}version-control/dynamic-content-in-template/${templateId}`,
                    method: "GET",
                    contentType: "application/json",
                });
            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan. Sluit a.u.b. deze module, open deze daarna opnieuw en probeer het vervolgens opnieuw. Of neem contact op als dat niet werkt.");
            }
        }

        /**
         * Shows or hides the main (full screen) loader.
         * @param {boolean} show True to show the loader, false to hide it.
         */
        toggleMainLoader(show) {
            this.mainLoader.toggleClass("loading", show);
        }
    }

    window.versionControl = new VersionControl(settings);
})(moduleSettings);
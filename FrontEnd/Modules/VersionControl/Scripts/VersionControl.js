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
require("@progress/kendo-ui/js/kendo.multiselect.js");
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
            this.mainWindow = null;
            this.mainLoader = null;
            this.commitEnvironmentField = null;
            this.commitDescriptionField = null;
            this.codeReviewUsersField = null;
            this.templateChangesGrid = null;
            this.dynamicContentChangesGrid = null;
            this.mainTabStrip = null;
            this.commitButton = null;
            this.reloadUncommittedChangesButton = null;
            this.reloadCommitsButton = null;
            this.deployGrid = null;
            this.deployTestButton = null;
            this.deployAcceptanceButton = null;
            this.deployLiveButton = null;
            this.deployToBranchButton = null;
            this.branchesDropDown = null;
            this.historyGrid = null;
            this.reloadHistoryButton = null;
            this.deployToBranchContainer = null;
            this.reviewGrid = null;
            this.reloadReviewsButton = null;

            // Other data.
            this.branches = null;

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
                headers: {"Authorization": `Bearer ${localStorage.getItem("accessToken")}`}
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

            // Don't allow users to use this module in a branch, only in the main/production environment of a tenant.
            if (!userData.currentBranchIsMainBranch) {
                $("#NotMainBranchNotification").removeClass("hidden");
                $("#wiser").addClass("hidden");
                this.toggleMainLoader(false);
                return;
            }

            try {
                this.branches = await Wiser.api({
                    url: `${this.settings.wiserApiRoot}branches`,
                    dataType: "json",
                    method: "GET"
                });
            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan met het ophalen van de beschikbare branches. Mogelijk kan de templatemodule nog wel gebruikt worden, maar mist alleen de functionaliteit om templates over te zetten naar een branch.")
                this.branches = [];
            }

            // Initialize sub classes.
            await this.initializeComponents();

            this.toggleMainLoader(false);
        }

        /**
         * Initialize all components on the first tab.
         */
        async initializeComponents() {
            // Main window
            this.mainWindow = $("#window").kendoWindow({
                width: "1500",
                height: "650",
                title: "Versiebeheer",
                visible: true,
                actions: [],
                draggable: false
            }).data("kendoWindow").maximize().open();

            this.commitDescriptionField = document.getElementById("commitDescription");
            this.deployToBranchContainer = document.getElementById("deployToBranchContainer");

            // Tab strip.
            this.mainTabStrip = $("#tabstrip").kendoTabStrip({
                activate: this.onMainTabStripActivate.bind(this)
            }).data("kendoTabStrip");

            // Commit button
            this.commitButton = $("#commitButton").kendoButton({
                click: this.onCommit.bind(this),
                icon: "save"
            }).data("kendoButton");

            this.reloadUncommittedChangesButton = $("#reloadUncommittedChangesButton").kendoButton({
                click: this.onReloadUncommittedChanges.bind(this),
                icon: "reload"
            }).data("kendoButton");

            this.reloadCommitsButton = $("#reloadCommitsButton").kendoButton({
                click: this.onReloadCommits.bind(this),
                icon: "reload"
            }).data("kendoButton");

            this.reloadHistoryButton = $("#reloadHistoryButton").kendoButton({
                click: this.onReloadHistory.bind(this),
                icon: "reload"
            }).data("kendoButton");

            this.reloadReviewsButton = $("#reloadReviewsButton").kendoButton({
                click: this.onReloadReviews.bind(this),
                icon: "reload"
            }).data("kendoButton");

            this.commitEnvironmentField = $("#commitEnvironment").kendoDropDownList({
                optionLabel: "Selecteer omgeving",
                dataTextField: "text",
                dataValueField: "value",
                dataSource: [
                    {text: "Ontwikkeling", value: 1},
                    {text: "Test", value: 2},
                    {text: "Acceptatie", value: 4},
                    {text: "Live", value: 8}
                ]
            }).data("kendoDropDownList");

            this.codeReviewUsersField = $("#usersForCodeReview").kendoMultiSelect({
                dataSource: {
                    transport: {
                        read: {
                            url: `${this.settings.wiserApiRoot}users?includeAdminUsers=true`,
                            dataType: "json"
                        }
                    },
                    group: {field: "group"}
                },
                dataTextField: "title",
                dataValueField: "id"
            }).data("kendoMultiSelect");

            // Deploy buttons (from second tab).
            this.deployTestButton = $("#deployCommitToTest").kendoButton({
                click: this.onDeploy.bind(this, 2),
                icon: "data",
                enable: false
            }).data("kendoButton");

            this.deployAcceptanceButton = $("#deployCommitToAcceptance").kendoButton({
                click: this.onDeploy.bind(this, 4),
                icon: "data",
                enable: false
            }).data("kendoButton");

            this.deployLiveButton = $("#deployCommitToLive").kendoButton({
                click: this.onDeploy.bind(this, 8),
                icon: "data",
                enable: false
            }).data("kendoButton");

            this.deployToBranchButton = $("#deployCommitToBranch").kendoButton({
                click: this.onDeployToBranch.bind(this)
            }).data("kendoButton");

            this.branchesDropDown = $("#branchesDropDown").kendoDropDownList({
                dataSource: this.branches,
                dataValueField: "id",
                dataTextField: "name",
                optionLabel: "Kies een branch..."
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
                    await this.setupDeployTab();
                    break;
                case "reviewTab":
                    await this.setupReviewTab();
                    break;
                case "historyTab":
                    await this.setupHistoryTab();
                    break;
            }
        }

        /**
         * Event for when the user clicks the commit button in the first tab.
         * @param event The click event of the kendoButton component.
         */
        async onCommit(event) {
            const selectedEnvironment = this.commitEnvironmentField.value();
            if (!selectedEnvironment) {
                kendo.alert("Selecteer een omgeving om de wijzigingen naar te deployen.");
                return;
            }

            const selectedUsersForCodeReview = this.codeReviewUsersField.dataItems();
            if (selectedUsersForCodeReview.length > 0 && selectedEnvironment === "8") {
                kendo.alert("Het is niet mogelijk om code reviews aan te vragen voor een live omgeving. Code review moet gedaan worden voordat het live wordt gezet.");
                return;
            }

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
                        environment: selectedEnvironment,
                        description: this.commitDescriptionField.value,
                        templates: selectedTemplates.map(t => {
                            return {templateId: t.templateId, version: t.version};
                        }),
                        dynamicContents: selectedDynamicContent.map(d => {
                            return {dynamicContentId: d.dynamicContentId, version: d.version};
                        }),
                        reviewRequestedUsers: selectedUsersForCodeReview
                    })
                });

                this.templateChangesGrid.dataSource.read();
                this.dynamicContentChangesGrid.dataSource.read();
                this.commitDescriptionField.value = "";
                this.commitEnvironmentField.value("");
            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan met het maken van de commit. Probeer het a.u.b. opnieuw of neem contact op als dat niet werkt.");
            }

            window.processing.removeProcess(initialProcess);
        }

        /**
         * Event for when the user clicks the reload button in the first tab.
         * @param event The click event of the kendoButton component.
         */
        async onReloadUncommittedChanges(event) {
            const initialProcess = `Reload_${Date.now()}`;
            window.processing.addProcess(initialProcess);

            try {
                event.preventDefault();
                this.templateChangesGrid.dataSource.read();
                this.dynamicContentChangesGrid.dataSource.read();
            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan met het verversen van de openstaande wijzigingen. Probeer het a.u.b. opnieuw of neem contact op als dat niet werkt.");
            }

            window.processing.removeProcess(initialProcess);
        }

        /**
         * Event for when the user clicks the reload button in the second tab.
         * @param event The click event of the kendoButton component.
         */
        async onReloadCommits(event) {
            const initialProcess = `Reload_${Date.now()}`;
            window.processing.addProcess(initialProcess);

            try {
                event.preventDefault();
                this.deployGrid.dataSource.read();
            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan met het verversen van de commits. Probeer het a.u.b. opnieuw of neem contact op als dat niet werkt.");
            }

            window.processing.removeProcess(initialProcess);
        }

        /**
         * Event for when the user clicks the reload button in the second tab.
         * @param event The click event of the kendoButton component.
         */
        async onReloadHistory(event) {
            const initialProcess = `Reload_${Date.now()}`;
            window.processing.addProcess(initialProcess);

            try {
                event.preventDefault();
                this.historyGrid.dataSource.read();
            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan met het verversen van de historie. Probeer het a.u.b. opnieuw of neem contact op als dat niet werkt.");
            }

            window.processing.removeProcess(initialProcess);
        }

        /**
         * Event for when the user clicks the reload button in the third tab.
         * @param event The click event of the kendoButton component.
         */
        async onReloadReviews(event) {
            const initialProcess = `Reload_${Date.now()}`;
            window.processing.addProcess(initialProcess);

            try {
                event.preventDefault();
                this.reviewGrid.dataSource.read();
            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan met het verversen van de reviews. Probeer het a.u.b. opnieuw of neem contact op als dat niet werkt.");
            }

            window.processing.removeProcess(initialProcess);
        }

        /**
         * Event for when the user (de)selects one or more rows in the deploy grid.
         * This will show/hide the commit buttons so that the user can't deploy something to an environment that it's already on.
         * @param event The change event of the grid.
         */
        onDeployGridChange(event) {
            const selectedCommits = event.sender.getSelectedData();

            // If there are now rows selected, disable all deploy buttons.
            if (!selectedCommits || !selectedCommits.length) {
                this.deployTestButton.enable(false);
                this.deployAcceptanceButton.enable(false);
                this.deployLiveButton.enable(false);
                this.deployToBranchContainer.classList.add("hidden");
                return;
            }

            // Only enable deploy buttons if at least one selected commit is not deployed to that environment yet.
            let everythingIsOnTest = true;
            let everythingIsOnAcceptance = true;
            let everythingIsOnLive = true;
            for (let selectedCommit of selectedCommits) {
                if (!selectedCommit.isTest) {
                    everythingIsOnTest = false;
                }

                if (!selectedCommit.isAcceptance) {
                    everythingIsOnAcceptance = false;
                }

                if (!selectedCommit.isLive) {
                    everythingIsOnLive = false;
                }
            }

            this.deployTestButton.enable(!everythingIsOnTest);
            this.deployAcceptanceButton.enable(!everythingIsOnAcceptance);
            this.deployLiveButton.enable(!everythingIsOnLive);
            this.deployToBranchContainer.classList.remove("hidden");
        }

        /**
         * Event for when the deploy grid gets (re)loaded.
         * This will disable all deploy buttons, because the user will no longer have any rows selected after reloading the grid.
         * @param event
         */
        onDeployGridDataBound(event) {
            this.deployTestButton.enable(false);
            this.deployAcceptanceButton.enable(false);
            this.deployLiveButton.enable(false);
            this.deployToBranchContainer.classList.add("hidden");
        }

        /**
         * Event for when the user clicks one of the deploy buttons to deploy one or more unfinished commits.
         * This will push the commit(s) to the selected environment.
         * @param environment The environment to deploy the selected commits to.
         * @param event The click event of the Kendo button.
         */
        async onDeploy(environment, event) {
            if (!environment) {
                return;
            }

            const selectedCommits = [];
            this.deployGrid.select().each((index, element) => {
                const dataItem = this.deployGrid.dataItem(element);
                if (dataItem) {
                    selectedCommits.push(dataItem);
                }
            });

            if (!selectedCommits || !selectedCommits.length) {
                kendo.alert("Kies a.u.b. eerst een of meer commits om te deployen.");
                return;
            }

            // Don't allow deploying to live if there is a pending code review.
            if (environment === 8 && selectedCommits.filter(c => c.review.status === "Pending" || c.review.status === "RequestChanges").length > 0) {
                kendo.alert("Er zijn nog code reviews die nog niet afgerond zijn. Wacht a.u.b. tot deze code reviews zijn afgerond voordat je naar live deployt.");
                return;
            }

            const initialProcess = `DeployCommit_${Date.now()}`;
            window.processing.addProcess(initialProcess);

            try {
                event.preventDefault();

                // We used to do these all at the same time, but that caused problems in some cases.
                // The problem was that if there are multiple commits for the same template/component,
                // then it would sometimes happen that version 10 would be deployed to live first and then version 9.
                // So we have to do them one by one in the correct order, to make sure the correct versions will be deployed.
                for (let selectedCommit of selectedCommits) {
                    await Wiser.api({
                        url: `${this.base.settings.wiserApiRoot}version-control`,
                        method: "POST",
                        contentType: "application/json",
                        data: JSON.stringify({
                            id: selectedCommit.id,
                            environment: environment
                        })
                    });
                }

                this.deployGrid.dataSource.read();
            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan met het deployen van de commit. Probeer het a.u.b. opnieuw of neem contact op als dat niet werkt.");
            } finally {
                window.processing.removeProcess(initialProcess);
            }
        }

        /**
         * Event for when the user clicks the button to deploy a commit to a branch.
         * @param event The click event of the Kendo button.
         */
        async onDeployToBranch(event) {
            const selectedCommits = this.deployGrid.getSelectedData();
            if (!selectedCommits || !selectedCommits.length) {
                kendo.alert("Kies a.u.b. eerst een of meer commits om te deployen.");
                return;
            }

            const selectedBranch = this.branchesDropDown.value();
            if (!selectedBranch) {
                kendo.alert("Kies a.u.b. eerst een branch om naar te deployen.");
                return;
            }

            const initialProcess = `DeployCommitToBranch_${Date.now()}`;
            window.processing.addProcess(initialProcess);

            try {
                event.preventDefault();

                await Wiser.api({
                    url: `${this.base.settings.wiserApiRoot}version-control/deploy-to-branch/${selectedBranch}`,
                    method: "POST",
                    contentType: "application/json",
                    data: JSON.stringify(selectedCommits.map(commit => commit.id))
                });

                kendo.alert(selectedCommits.length === 1 ? "De geselecteerde commit is succesvol naar de geselecteerde branch gezet" : "De geselecteerde commits zijn succesvol naar de geselecteerde branch gezet.");
            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan met het deployen van de commit. Probeer het a.u.b. opnieuw of neem contact op als dat niet werkt.");
            } finally {
                window.processing.removeProcess(initialProcess);
            }
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
                    resizable: true,
                    columns: [
                        {
                            field: "templateId",
                            title: "ID",
                            width: "50px"
                        },
                        {
                            field: "templateType",
                            title: "Type",
                            width: "75px"
                        },
                        {
                            field: "templateParentName",
                            title: "Map",
                            width: "150px"
                        },
                        {
                            field: "templateName",
                            title: "Template",
                            width: "150px"
                        },
                        {
                            field: "version",
                            title: "Versie",
                            width: "75px"
                        },
                        {
                            field: "versionTest",
                            title: "Versie test",
                            width: "75px"
                        },
                        {
                            field: "versionAcceptance",
                            title: "Versie acceptatie",
                            width: "100px"
                        },
                        {
                            field: "versionLive",
                            title: "Versie live",
                            width: "75px"
                        },
                        {
                            field: "changedOn",
                            format: "{0:dd-MM-yyyy HH:mm:ss}",
                            title: "Datum",
                            width: "100px"
                        },
                        {
                            field: "changedBy",
                            title: "Door",
                            width: "100px"
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
                    resizable: true,
                    columns: [
                        {
                            field: "dynamicContentId",
                            title: "ID",
                            width: "50px"
                        },
                        {
                            field: "templateNames",
                            title: "Gebruikt in",
                            width: "150px",
                            template: "#: templateNames ? templateNames.join(', ') : '' #"
                        },
                        {
                            field: "title",
                            title: "Naam",
                            width: "150px"
                        },
                        {
                            field: "version",
                            title: "Versie",
                            width: "75px"
                        },
                        {
                            field: "versionTest",
                            title: "Versie test",
                            width: "75px"
                        },
                        {
                            field: "versionAcceptance",
                            title: "Versie acceptatie",
                            width: "100px"
                        },
                        {
                            field: "versionLive",
                            title: "Versie live",
                            width: "75px"
                        },
                        {
                            field: "changedOn",
                            format: "{0:dd-MM-yyyy HH:mm:ss}",
                            title: "Datum",
                            width: "100px"
                        },
                        {
                            field: "changedBy",
                            title: "Door",
                            width: "100px"
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
        async setupDeployTab() {
            try {
                if (this.deployGrid) {
                    return;
                }

                const gridSettings = {
                    dataSource: {
                        transport: {
                            read: async (transportOptions) => {
                                const initialProcess = `GetCompletedCommits_${Date.now()}`;
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
                    resizable: true,
                    columns: [
                        {
                            "field": "id",
                            "title": "ID",
                            "width": "50px"
                        },
                        {
                            "field": "description",
                            "title": "Omschrijving"
                        },
                        {
                            "field": "isTest",
                            "title": "Test",
                            "width": "50px",
                            "template": "<span class='k-icon k-i-#:(isTest ? \"check k-syntax-str\" : \"cancel k-syntax-error\")#'></span>"
                        },
                        {
                            "field": "isAcceptance",
                            "title": "Acceptatie",
                            "width": "50px",
                            "template": "<span class='k-icon k-i-#:(isAcceptance ? \"check k-syntax-str\" : \"cancel k-syntax-error\")#'></span>"
                        },
                        {
                            "field": "isLive",
                            "title": "Live",
                            "width": "50px",
                            "template": "<span class='k-icon k-i-#:(isLive ? \"check k-syntax-str\" : \"cancel k-syntax-error\")#'></span>"
                        },
                        {
                            "field": "addedOn",
                            "format": "{0:dd-MM-yyyy HH:mm:ss}",
                            "title": "Datum",
                            "width": "150px"
                        },
                        {
                            "field": "addedBy",
                            "title": "Door",
                            "width": "150px"
                        },
                        {
                            "field": "dynamicContents",
                            "title": "Dynamic contents",
                            "template": (dataItem) => {
                                if (!dataItem.dynamicContents || !dataItem.dynamicContents.length) {
                                    return `<span class="dynamic-content">Geen dynamic content in deze commit</span>`;
                                }

                                const html = [];
                                for (let dynamicContent of dataItem.dynamicContents) {
                                    let templateName = dynamicContent.templateNames && dynamicContent.templateNames.length ? dynamicContent.templateNames[0] : "Geen template";
                                    html.push(`<span class="dynamic-content">${templateName} ${dynamicContent.component} - ${dynamicContent.title} (${dynamicContent.dynamicContentId}) - Versie ${dynamicContent.version}</span>`);
                                }

                                return html.join("<br />")
                            }
                        },
                        {
                            "field": "templates",
                            "title": "Templates",
                            "template": (dataItem) => {
                                if (!dataItem.templates || !dataItem.templates.length) {
                                    return `<span class="template">Geen templates in deze commit</span>`;
                                }

                                const html = [];
                                for (let template of dataItem.templates) {
                                    html.push(`<span class="template">${template.templateParentName} (${template.templateParentId}) => ${template.templateName} (${template.templateId}) - Versie ${template.version}</span>`);
                                }

                                return html.join("<br />")
                            }
                        }
                    ],
                    change: this.onDeployGridChange.bind(this),
                    dataBound: this.onDeployGridDataBound.bind(this)
                };

                this.deployGrid = $("#deployGrid").kendoGrid(gridSettings).data("kendoGrid");

            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan met het laden van de nog niet afgeronde commits. Sluit a.u.b. deze module, open deze daarna opnieuw en probeer het vervolgens opnieuw. Of neem contact op als dat niet werkt.");
            }
        }

        /**
         * Setup/initialize the grid with all finished commits (commits that have been deployed to live).
         */
        async setupHistoryTab() {
            try {
                if (this.historyGrid) {
                    return;
                }

                const gridSettings = {
                    dataSource: {
                        transport: {
                            read: async (transportOptions) => {
                                const initialProcess = `GetCompletedCommits_${Date.now()}`;
                                window.processing.addProcess(initialProcess);

                                try {
                                    const templatesToCommit = await Wiser.api({
                                        url: `${this.base.settings.wiserApiRoot}version-control/completed-commits`,
                                        method: "GET",
                                        contentType: "application/json"
                                    });

                                    transportOptions.success(templatesToCommit);
                                } catch (exception) {
                                    console.error(exception);
                                    kendo.alert("Er is iets fout gegaan met het laden van de commit historie. Sluit a.u.b. deze module, open deze daarna opnieuw en probeer het vervolgens opnieuw. Of neem contact op als dat niet werkt.");
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
                                    deployedToDevelopmentOn: {
                                        type: "date"
                                    },
                                    deployedToTestOn: {
                                        type: "date"
                                    },
                                    deployedToAcceptanceOn: {
                                        type: "date"
                                    },
                                    deployedToLiveOn: {
                                        type: "date"
                                    },
                                    templateNames: {
                                        type: "array"
                                    }
                                }
                            }
                        }
                    },
                    selectable: false,
                    resizable: true,
                    columns: [
                        {
                            "field": "id",
                            "title": "ID",
                            "width": "50px"
                        },
                        {
                            "field": "description",
                            "title": "Omschrijving"
                        },
                        {
                            "title": "Deploy historie",
                            "template": `
                                Dev: #:(typeof(deployedToDevelopmentOn) !== "undefined" && deployedToDevelopmentOn ? kendo.toString(deployedToDevelopmentOn, "dd-MM-yyyy HH:mm:ss") : "Onbekend")# door #:(typeof(deployedToDevelopmentBy) !== "undefined" && deployedToDevelopmentBy ? deployedToDevelopmentBy : "Onbekend")#<br />
                                Test: #:(typeof(deployedToTestOn) !== "undefined" && deployedToTestOn ? kendo.toString(deployedToTestOn, "dd-MM-yyyy HH:mm:ss") : "Onbekend")# door #:(typeof(deployedToTestBy) !== "undefined" && deployedToTestOn ? deployedToTestBy : "Onbekend")#<br />
                                Acceptatie: #:(typeof(deployedToAcceptanceOn) !== "undefined" && deployedToAcceptanceOn ? kendo.toString(deployedToAcceptanceOn, "dd-MM-yyyy HH:mm:ss") : "Onbekend")# door #:(typeof(deployedToAcceptanceBy) !== "undefined" ? deployedToAcceptanceBy : "Onbekend")#<br />
                                Live: #:(typeof(deployedToLiveOn) !== "undefined" && deployedToLiveOn ? kendo.toString(deployedToLiveOn, "dd-MM-yyyy HH:mm:ss") : "Onbekend")# door #:(typeof(deployedToLiveBy) !== "undefined" && deployedToLiveOn ? deployedToLiveBy : "Onbekend")#
                            `
                        },
                        {
                            "field": "dynamicContents",
                            "title": "Dynamic contents",
                            "template": (dataItem) => {
                                if (!dataItem.dynamicContents || !dataItem.dynamicContents.length) {
                                    return `<span class="dynamic-content">Geen dynamic content in deze commit</span>`;
                                }

                                const html = [];
                                for (let dynamicContent of dataItem.dynamicContents) {
                                    let templateName = dynamicContent.templateNames && dynamicContent.templateNames.length ? dynamicContent.templateNames[0] : "Geen template";
                                    html.push(`<span class="dynamic-content">${templateName} ${dynamicContent.component} - ${dynamicContent.title} (${dynamicContent.dynamicContentId}) - Versie ${dynamicContent.version}</span>`);
                                }

                                return html.join("<br />")
                            }
                        },
                        {
                            "field": "templates",
                            "title": "Templates",
                            "template": (dataItem) => {
                                if (!dataItem.templates || !dataItem.templates.length) {
                                    return `<span class="template">Geen templates in deze commit</span>`;
                                }

                                const html = [];
                                for (let template of dataItem.templates) {
                                    html.push(`<span class="template">${template.templateParentName} (${template.templateParentId}) => ${template.templateName} (${template.templateId}) - Versie ${template.version}</span>`);
                                }

                                return html.join("<br />")
                            }
                        }
                    ]
                };

                this.historyGrid = $("#historyGrid").kendoGrid(gridSettings).data("kendoGrid");

            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan met het laden van de commit historie. Sluit a.u.b. deze module, open deze daarna opnieuw en probeer het vervolgens opnieuw. Of neem contact op als dat niet werkt.");
            }
        }

        /**
         * Setup/initialize the grid with all reviews.
         */
        async setupReviewTab() {
            try {
                if (this.reviewGrid) {
                    return;
                }

                this.reviewGrid = $("#reviewGrid").kendoGrid({
                    dataSource: {
                        transport: {
                            read: async (transportOptions) => {
                                const initialProcess = `GetReviews_${Date.now()}`;
                                window.processing.addProcess(initialProcess);

                                try {
                                    const templatesToCommit = await Wiser.api({
                                        url: `${this.base.settings.wiserApiRoot}version-control/reviews`,
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
                                    requestedOn: {
                                        type: "date"
                                    },
                                    requestedUsers: {
                                        type: "array"
                                    },
                                    comments: {
                                        type: "array"
                                    },
                                    commit: {
                                        type: "object"
                                    }
                                }
                            }
                        }
                    },
                    resizable: true,
                    columns: [
                        {
                            field: "id",
                            title: "ID",
                            width: "50px"
                        },
                        {
                            field: "commitDescription",
                            title: "Omschrijving",
                            width: "150px"
                        },
                        {
                            field: "requestedByName",
                            title: "Aangevraagd door",
                            width: "100px"
                        },
                        {
                            field: "status",
                            title: "Status",
                            width: "100px"
                        },
                        {
                            title: "&nbsp;",
                            width: "300px",
                            command: [
                                {
                                    name: "approve",
                                    text: "Goedkeuren",
                                    iconClass: "k-icon k-i-check-outline",
                                    click: (event) => {
                                        const dataItem = $(event.delegateTarget).data("kendoGrid").dataItem(event.currentTarget.closest("tr"));
                                        this.approveReview(dataItem.id);
                                    }
                                },
                                {
                                    name: "requestChanges",
                                    text: "Afkeuren",
                                    iconClass: "k-icon k-i-x-outline",
                                    click: (event) => {
                                        const dataItem = $(event.delegateTarget).data("kendoGrid").dataItem(event.currentTarget.closest("tr"));
                                        this.reviewRequestChanges(dataItem.id);
                                    }
                                },
                                {
                                    name: "addComment",
                                    text: "Reactie plaatsen",
                                    iconClass: "k-icon k-i-comment",
                                    click: (event) => {
                                        const dataItem = $(event.delegateTarget).data("kendoGrid").dataItem(event.currentTarget.closest("tr"));
                                        const prompt = $("<div class='review-comment-prompt'></div>").kendoPrompt({
                                            title: "Reactie plaatsen",
                                            content: "<textarea class='k-textbox'></textarea>",
                                        }).data("kendoPrompt");
                                        prompt.open();
                                        console.log("prompt", prompt, prompt.result);
                                        prompt.result.done(() => {
                                            const value = prompt.element.find("textarea").val();
                                            if (!value) {
                                                kendo.alert("Vul a.u.b. een waarde in");
                                                return;
                                            }
                                            this.reviewAddComment(dataItem.id, value);
                                        });
                                    }
                                }
                            ]
                        }
                    ],
                    detailInit: this.onReviewGridDetailInit.bind(this)
                }).data("kendoGrid");

            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan met het laden van de reviews. Sluit a.u.b. deze module, open deze daarna opnieuw en probeer het vervolgens opnieuw. Of neem contact op als dat niet werkt.");
            }
        }

        /**
         * Setup/initialize the sub grid with all comments of a review.
         * @param event The detail init event of the grid.
         */
        onReviewGridDetailInit(event) {
            const comments = event.data.comments;
            event.data.commit = event.data.commit || {};
            const templates = event.data.commit.templates || [];
            const dynamicContents = event.data.commit.dynamicContents || [];

            $("<h2>Reacties</h2>").appendTo(event.detailCell);
            $("<div id='reaction-grid'/>").appendTo(event.detailCell).kendoGrid({
                dataSource: {
                    data: comments,
                    schema: {
                        model: {
                            id: "id",
                            fields: {
                                addedOn: {
                                    type: "date"
                                }
                            }
                        }
                    }
                },
                scrollable: false,
                sortable: false,
                selectable: "cell",
                pageable: false,
                columns: [
                    {field: "addedOn", title: "Datum", width: "150px", format: "{0:dd-MM-yyyy HH:mm}"},
                    {field: "addedByName", title: "Door", width: "100px"},
                    {field: "text", title: "Bericht"}
                ],
                noRecords: {
                    template: "Deze review heeft nog geen reacties"
                }
            });

            $("<br><h2>Templates</h2>").appendTo(event.detailCell);
            $("<div/>").appendTo(event.detailCell).kendoGrid({
                dataSource: {
                    data: templates
                },
                scrollable: false,
                sortable: false,
                pageable: false,
                columns: [
                    {field: "templateName", title: "Template"},
                    {field: "templateParentName", title: "Map"},
                    {
                        title: "&nbsp;",
                        width: "150px",
                        command: [{
                            name: "open",
                            text: "Bekijk historie",
                            iconClass: "k-icon k-i-hyperlink-open",
                            click: (event) => {
                                const dataItem = $(event.delegateTarget).data("kendoGrid").dataItem(event.currentTarget.closest("tr"));
                                this.openTemplateHistory(dataItem.templateId);
                            }
                        }]
                    }
                ],
                noRecords: {
                    template: "Deze commmit bevat geen templates"
                }
            });

            $("<br><h2>Dynamic content</h2>").appendTo(event.detailCell);
            $("<div/>").appendTo(event.detailCell).kendoGrid({
                dataSource: {
                    data: dynamicContents,
                    schema: {
                        model: {
                            id: "id",
                            fields: {
                                templateNames: {
                                    type: "array"
                                }
                            }
                        }
                    }
                },
                scrollable: false,
                sortable: false,
                pageable: false,
                columns: [
                    {field: "templateNames", title: "Gekoppelde templates", template: "#= templateNames.join(', ') #"},
                    {field: "title", title: "Naam"},
                    {field: "component", title: "Component"},
                    {
                        title: "&nbsp;",
                        width: "150px",
                        command: [{
                            name: "open",
                            text: "Bekijk historie",
                            iconClass: "k-icon k-i-hyperlink-open",
                            click: (event) => {
                                const dataItem = $(event.delegateTarget).data("kendoGrid").dataItem(event.currentTarget.closest("tr"));
                                for (const template of dataItem.templateIds) {
                                    this.openTemplateHistory(template);
                                }
                            }
                        }]
                    }
                ],
                noRecords: {
                    template: "Deze commmit bevat geen dynamic content"
                }
            });
        }

        /**
         * Open the templates module with the history of a specific template.
         * @param templateId The ID of the template to open.
         */
        async openTemplateHistory(templateId) {
            const templateModuleWindow = $("<div />").kendoWindow({
                actions: ["Close"],
                content: {
                    url: `/Modules/Templates?templateId=${templateId}&initialTab=history`,
                    iframe: true
                },
                title: `Template: ${templateId}`
            }).data("kendoWindow");

            templateModuleWindow.open().maximize();
        }

        /**
         * Approve a commit.
         * @param reviewId The ID of the review to approve.
         */
        async approveReview(reviewId) {
            const initialProcess = `ApproveCommit_${Date.now()}`;
            window.processing.addProcess(initialProcess);

            try {
                await Wiser.api({
                    url: `${this.base.settings.wiserApiRoot}version-control/reviews/${reviewId}/approve`,
                    method: "PUT",
                    contentType: "application/json"
                });

                this.reloadReviewsButton.trigger("click");
            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan met het goedkeuren van deze review/commit. Probeer het opnieuw of neem contact op als dat niet werkt.");
            } finally {
                window.processing.removeProcess(initialProcess);
            }
        }

        /**
         * Denies a commit, the owner needs to make some changes before it can be approved.
         * @param reviewId The ID of the review to request changes for.
         */
        async reviewRequestChanges(reviewId) {
            const initialProcess = `DenyCommit_${Date.now()}`;
            window.processing.addProcess(initialProcess);

            try {
                await Wiser.api({
                    url: `${this.base.settings.wiserApiRoot}version-control/reviews/${reviewId}/request-changes`,
                    method: "PUT",
                    contentType: "application/json"
                });

                this.reloadReviewsButton.trigger("click");
            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan met het goedkeuren van deze review/commit. Probeer het opnieuw of neem contact op als dat niet werkt.");
            } finally {
                window.processing.removeProcess(initialProcess);
            }
        }

        /**
         * Add a comment to a review.
         * @param reviewId The ID of the review to request changes for.
         * @param comment The comment to add to the review.
         */
        async reviewAddComment(reviewId, comment) {
            const initialProcess = `AddComment_${Date.now()}`;
            window.processing.addProcess(initialProcess);

            try {
                await Wiser.api({
                    url: `${this.base.settings.wiserApiRoot}version-control/reviews/${reviewId}/comment`,
                    method: "POST",
                    contentType: "application/json",
                    data: JSON.stringify(comment)
                });

                this.reloadReviewsButton.trigger("click");
            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan met het plaatsen van de reactie. Probeer het opnieuw of neem contact op als dat niet werkt.");
            } finally {
                window.processing.removeProcess(initialProcess);
            }
        }
    }

    window.versionControl = new VersionControl(settings);
})(moduleSettings);
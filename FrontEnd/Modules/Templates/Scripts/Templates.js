import {TrackJS} from "trackjs";
import {Wiser} from "../../Base/Scripts/Utils.js";
import "../../Base/Scripts/Processing.js";
import {TemplateConnectedUsers} from "./TemplateConnectedUsers.js";
import "../Css/Templates.css";
import "../Css/Measurements.css";
// Disabled until we can complete this feature.
//import {WtsConfiguration} from "./WtsConfiguration.js";

require("@progress/kendo-ui/js/kendo.notification.js");
require("@progress/kendo-ui/js/kendo.button.js");
require("@progress/kendo-ui/js/kendo.combobox.js");
require("@progress/kendo-ui/js/kendo.multiselect.js");
require("@progress/kendo-ui/js/kendo.editor.js");
require("@progress/kendo-ui/js/kendo.splitter.js");
require("@progress/kendo-ui/js/kendo.tabstrip.js");
require("@progress/kendo-ui/js/kendo.tooltip.js");
require("@progress/kendo-ui/js/kendo.treeview.js");
require("@progress/kendo-ui/js/kendo.grid.js");
require("@progress/kendo-ui/js/kendo.notification.js");
require("@progress/kendo-ui/js/kendo.datepicker.js");
require("@progress/kendo-ui/js/kendo.daterangepicker.js");
require("@progress/kendo-ui/js/dataviz/chart/chart.js");
require("@progress/kendo-ui/js/dataviz/chart/kendo-chart.js");
require("@progress/kendo-ui/js/cultures/kendo.culture.nl-NL.js");
require("@progress/kendo-ui/js/messages/kendo.messages.nl-NL.js");

// Any custom settings can be added here. They will overwrite most default settings inside the module.
const moduleSettings = {

};

((settings) => {
    /**
     * Main class.
     */
    class Templates {

        /**
         * Initializes a new instance of DynamicContent.
         * @param {any} settings An object containing the settings for this class.
         */
        constructor(settings) {
            this.base = this;

            // Kendo components.
            this.mainSplitter = null;
            this.mainTreeView = null;
            this.searchResultsTreeView = null;
            this.mainTabStrip = null;
            this.treeViewTabStrip = null;
            this.treeViewTabs = null;
            this.mainWindow = null;
            this.mainComboInput = null;
            this.mainMultiSelect = null;
            this.mainNumericTextBox = null;
            this.mainDatePicker = null;
            this.mainDateTimePicker = null;
            this.selectedId = 0;
            this.templateSettings = {};
            this.linkedTemplates = null;
            this.templateHistory = null;
            this.treeViewContextMenu = null;
            this.mainHtmlEditor = null;
            this.dynamicContentGrid = null;
            this.newContentId = 0;
            this.newContentTitle = null;
            this.saving = false;
            this.initialTemplateSettings = null;
            this.branches = null;
            this.renderLogsGrid = null;
            this.measurementsLoaded = false;
            this.allHistoryPartsLoaded = false;
            this.lastLoadedHistoryPartNumber = 0;
            this.loadingNextPart = false;
            this.templateTypes = Object.freeze({
                "UNKNOWN": 0,
                "HTML": 1,
                "CSS": 2,
                "SCSS": 3,
                "JS": 4,
                "SCRIPTS": 4,
                "QUERY": 5,
                "SQL": 5,
                "NORMAL": 6,
                "DIRECTORY": 7,
                "XML": 8,
                "AIS": 8, // Legacy AIS
                "SERVICES": 8, // WTS
                "ROUTINES": 9,
                "VIEWS": 10,
                "TRIGGERS": 11
            });

            // Default settings
            this.settings = {
                moduleId: 0,
                tenantId: 0,
                username: "Onbekend",
                userEmailAddress: "",
                userType: "",
                templateId: 0,
                initialTab: null
            };
            Object.assign(this.settings, settings);

            // Other.
            this.mainLoader = null;
            this.connectedUsers = new TemplateConnectedUsers(this);
            //this.wtsConfiguration = new WtsConfiguration(this);

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
                window.processing.removeProcess(process);
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

            await this.initializeKendoComponents();
            this.bindEvents();

            // Start the Pusher connection.
            await this.connectedUsers.init();

            // If we have a template ID in the query string, load that template immediately.
            if (this.settings.templateId) {
                await this.loadTemplate(this.settings.templateId);
                this.selectedId = this.settings.templateId;
                if (this.settings.initialTab) {
                    this.mainTabStrip.select(`li.${this.settings.initialTab}-tab`);
                    setTimeout(()=> { // sometimes the tab switching doesn't work yet (because not everything is loaded?), doing it again after a second just in case
                        this.mainTabStrip.select(`li.${this.settings.initialTab}-tab`);
                        }, 1000);
                }
            }
            window.processing.removeProcess(process);
        }

        /**
         * Initializes all kendo components for the base class.
         */
        async initializeKendoComponents() {
            window.popupNotification = $("#popupNotification").kendoNotification().data("kendoNotification");

            // Buttons
            $("#addButton").kendoButton({
                icon: "plus",
                click: () => this.openCreateNewItemDialog(false)
            });

            $("#importLegacyButton").kendoButton({
                icon: "import",
                click: this.importLegacyTemplates.bind(this)
            });

            // Main window
            this.mainWindow = $("#window").kendoWindow({
                width: "1500",
                height: "650",
                title: "Templates",
                visible: true,
                actions: [],
                draggable: false
            }).data("kendoWindow").maximize().open();

            // Splitter
            this.mainSplitter = $("#horizontal").kendoSplitter({
                panes: [{
                    collapsible: true,
                    size: "15%"
                }, {
                    collapsible: false
                }]
            }).data("kendoSplitter");
            this.mainSplitter.resize(true);

            // Tabstrip
            this.mainTabStrip = $(".tabstrip").kendoTabStrip({
                animation: {
                    open: {
                        effects: "fadeIn"
                    }
                },
                activate: this.onMainTabStripActivate.bind(this)
            }).data("kendoTabStrip");

            this.treeViewTabStrip = $(".tabstrip-treeview").kendoTabStrip({
                animation: {
                    open: {
                        effects: "fadeIn"
                    }
                }
            }).data("kendoTabStrip");

            if (!this.treeViewTabStrip) {
                return;
            }

            await this.loadTabsAndTreeViews();

            this.searchResultsTreeView = $("#search-results-treeview").kendoTreeView({
                loadOnDemand: false,
                dragAndDrop: false,
                dataTextField: "templateName",
                dataSpriteCssClassField: "spriteCssClass",
                select: this.onTreeViewSelect.bind(this),
            }).data("kendoTreeView");

            this.treeViewContextMenu = $("#treeViewContextMenu").kendoContextMenu({
                dataSource: [
                    { text: "Item toevoegen", attr: { action: "addNewItem" } },
                    { text: "Hernoemen", attr: { action: "rename" } },
                    { text: "Verwijderen", attr: { action: "delete" } }
                ],
                target: ".tabstrip-treeview",
                filter: ".k-item",
                open: this.onContextMenuOpen.bind(this),
                select: this.onContextMenuSelect.bind(this)
            }).data("kendoContextMenu");
        }

        /**
         * Initializes and loads the Kendo components for the main tab strip and the tree views of every tab.
         */
        async loadTabsAndTreeViews() {
            // Load the tabs via the API.
            this.treeViewTabs = await Wiser.api({
                url: `${this.settings.wiserApiRoot}templates/0/tree-view`,
                dataType: "json",
                method: "GET"
            });

            for (let tab of this.treeViewTabs) {
                this.treeViewTabStrip.append({
                    text: tab.templateName,
                    content: `<ul id="${tab.templateId}-treeview" class="treeview" data-id="${tab.templateId}" data-title="${tab.templateName}"></ul>`
                });
            }

            this.treeViewTabStrip.append({
                text: "Zoekresultaten",
                content: `<ul id="search-results-treeview" class="treeview" data-id="0" data-title="Zoekresultaten"></ul>`
            });

            this.treeViewTabStrip.tabGroup.find("li:last-child").addClass("hidden");

            // Select first tab.
            this.treeViewTabStrip.select(0);

            // Treeview
            this.mainTreeView = [];
            $(".treeview:not(#search-results-treeview)").each((index, element) => {
                const treeViewElement = $(element);
                this.mainTreeView[index] = treeViewElement.kendoTreeView({
                    loadOnDemand: true,
                    dragAndDrop: true,
                    collapse: this.onTreeViewCollapseItem.bind(this),
                    expand: this.onTreeViewExpandItem.bind(this),
                    select: this.onTreeViewSelect.bind(this),
                    dragstart: this.onTreeViewDragStart.bind(this),
                    drop: this.onTreeViewDropItem.bind(this),
                    dataSource: {
                        transport: {
                            read: (readOptions) => {
                                Wiser.api({
                                    url: `${this.settings.wiserApiRoot}templates/${readOptions.data.templateId || treeViewElement.data("id")}/tree-view`,
                                    dataType: "json",
                                    type: "GET"
                                }).then((result) => {
                                    readOptions.success(result);
                                }).catch((result) => {
                                    readOptions.error(result);
                                });
                            }
                        },
                        schema: {
                            model: {
                                id: "templateId",
                                hasChildren: "hasChildren"
                            }
                        }
                    },
                    dataTextField: "templateName",
                    dataSpriteCssClassField: "spriteCssClass"
                }).data("kendoTreeView");
            });

            this.searchResultsTreeView = $("#search-results-treeview").kendoTreeView({
                loadOnDemand: false,
                dragAndDrop: false,
                dataTextField: "templateName",
                dataSpriteCssClassField: "spriteCssClass",
                select: this.onTreeViewSelect.bind(this),
            }).data("kendoTreeView");

            this.treeViewContextMenu = $("#treeViewContextMenu").kendoContextMenu({
                dataSource: [
                    {text: "Item toevoegen", attr: {action: "addNewItem"}},
                    {text: "Hernoemen", attr: {action: "rename"}},
                    {text: "Verwijderen", attr: {action: "delete"}}
                ],
                target: ".tabstrip-treeview",
                filter: ".k-item",
                open: this.onContextMenuOpen.bind(this),
                select: this.onContextMenuSelect.bind(this)
            }).data("kendoContextMenu");
        }

        /**
         * Shows or hides the main (full screen) loader.
         * @param {boolean} show True to show the loader, false to hide it.
         */
        toggleMainLoader(show) {
            this.mainLoader.toggleClass("loading", show);
        }

        /**
         * Opens the dialog for creating a new item.
         * @param {boolean} isFromContextMenu When calling this from context menu, set this to true.
         * @param {any} dataItem When calling this from context menu, the selected data item from the tree view or tab sheet should be entered here.
         * @param {boolean} isFromRootItem When calling this from context menu, indicate whether this was a context menu of a root item or a tree node.
         * @param {any} selectedTreeViewNode When calling this from context menu, the selected node of the tree view or tab sheet should be entered here.
         */
        async openCreateNewItemDialog(isFromContextMenu = false, dataItem = null, isFromRootItem = false, selectedTreeViewNode = null) {
            try {
                const selectedTabIndex = this.treeViewTabStrip.select().index();
                const selectedTabContentElement = this.treeViewTabStrip.contentElement(selectedTabIndex);
                const treeViewElement = selectedTabContentElement.querySelector("ul");
                const treeView = $(treeViewElement).data("kendoTreeView");
                dataItem = dataItem || (this.selectedId === 0 ? { templateId: treeViewElement.dataset.id, templateName: treeViewElement.dataset.title, isFolder: true } : treeView.dataItem(treeView.select()));
                const parentId = dataItem.templateId || this.selectedId || parseInt(treeViewElement.dataset.id);
                const newItemIsDirectoryCheckBox = $("#newItemIsDirectoryCheckBox").prop("checked", false);
                const newItemTitleField = $("#newItemTitleField").val("");
                const parentIsDirectory = dataItem.isFolder;

                if (!isFromContextMenu) {
                    selectedTreeViewNode = treeView.select();
                }

                newItemIsDirectoryCheckBox.toggleClass("hidden", !parentIsDirectory);

                const dialog = $("#createNewItemDialog").kendoDialog({
                    width: "500px",
                    title: `Nieuw item aanmaken onder '${dataItem.templateName}'`,
                    closable: true,
                    modal: true,
                    actions: [
                        {
                            text: "Annuleren"
                        },
                        {
                            text: "Opslaan",
                            primary: true,
                            action: () => {
                                try {
                                    const isDirectory = parentIsDirectory && newItemIsDirectoryCheckBox.prop("checked");
                                    const title = newItemTitleField.val();
                                    if (!title) {
                                        kendo.alert("Vul a.u.b. een naam in.");
                                        return;
                                    }

                                    const type = isDirectory ? this.templateTypes.DIRECTORY : this.templateTypes[treeViewElement.dataset.title.toUpperCase()];

                                    this.createNewTemplate(parentId, title, type, treeView, !parentId || isFromRootItem ? undefined : selectedTreeViewNode);
                                } catch (exception) {
                                    console.error(exception);
                                    kendo.alert("Er is iets fout gegaan. Sluit a.u.b. deze module, open deze daarna opnieuw en probeer het vervolgens opnieuw. Of neem contact op als dat niet werkt.");
                                }
                            }
                        }
                    ]
                }).data("kendoDialog");

                dialog.open();
            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan. Sluit a.u.b. deze module, open deze daarna opnieuw en probeer het vervolgens opnieuw. Of neem contact op als dat niet werkt.");
            }
        }

        onMainTabStripActivate(event) {
            switch (this.mainTabStrip.select().data("name")) {
                case "history":
                    this.reloadHistoryTab();
                    break;
                case "measurements":
                    this.reloadMeasurementsTab();
                    break;
                case "configuration":
                    // Check if the template is an XML template.
                    const templateType = this.templateSettings.type ? this.templateSettings.type.toUpperCase() : "UNKNOWN";
                    // Prevent loading the configuration tab if the template is not an XML template.
                    if (templateType === "XML") {
                        //this.wtsConfiguration.reloadWtsConfigurationTab(this.selectedId);
                    }
                    break;
            }
        }

        /**
         * Event for when an item in a kendoTreeView gets collapsed.
         * @param {any} event The collapsed event of a kendoTreeView.
         */
        onTreeViewCollapseItem(event) {
            const dataItem = event.sender.dataItem(event.node);
            dataItem.spriteCssClass = dataItem.collapsedSpriteCssClass;

            // Changing the text causes kendo to actually update the icon. If we don't change the test, the icon will not change.
            event.sender.text(event.node, event.sender.text(event.node).trim());
        }

        /**
         * Event for when an item in a kendoTreeView gets expanded.
         * @param {any} event The expanded event of a kendoTreeView.
         */
        onTreeViewExpandItem(event) {
            const dataItem = event.sender.dataItem(event.node);
            dataItem.spriteCssClass = dataItem.expandedSpriteCssClass || dataItem.collapsedSpriteCssClass;

            // Changing the text causes kendo to actually update the icon. If we don't change the test, the icon will not change.
            event.sender.text(event.node, event.sender.text(event.node) + " ");
        }

        /**
         * Event for when the user selects an item in a tree view.
         * @param {any} event The select event of a kendoTreeView.
         */
        async onTreeViewSelect(event) {
            const dataItem = event.sender.dataItem(event.node);

            // Check if this is a virtual item.
            let virtualItem = null;
            if (dataItem.isVirtualItem) {
                virtualItem = {
                    objectName: dataItem.templateName,
                    templateType: dataItem.templateType
                };
            }

            // All virtual items have an ID of 0, so also check if it's not switching to a virtual item.
            if (dataItem.id === this.selectedId && virtualItem === null) {
                return;
            }

            if (this.selectedId && !this.canUnloadTemplate()) {
                try {
                    await kendo.confirm("U heeft nog openstaande wijzigingen. Weet u zeker dat u door wilt gaan?");
                } catch {
                    event.preventDefault();
                    // Select the previous item again, otherwise the tree view will still show the other item to be selected.
                    const dataItem = event.sender.dataSource.get(this.selectedId);
                    if (dataItem) {
                        event.sender.select(event.sender.findByUid(dataItem.uid));
                    } else {
                        event.sender.select($());
                    }
                    return;
                }
            }

            $("#addButton").toggleClass("hidden", !dataItem.isFolder);

            // Deselect all tree views in other tabs, otherwise they will stay selected even though the user selected a different template.
            for (let index = 0; index < this.mainTreeView.length; index++) {
                const treeView = this.mainTreeView[index];
                if (this.treeViewTabStrip.select().index() !== index) {
                    treeView.select($());
                }
            }

            this.selectedId = dataItem.id;
            this.lastLoadedHistoryPartNumber = 0;
            this.measurementsLoaded = false;
            this.onMainTabStripActivate();

            if (dataItem.isFolder) {
                await this.loadTemplate(0);
                this.initialTemplateSettings = this.getCurrentTemplateSettings();
                return;
            }

            await this.loadTemplate(dataItem.id, virtualItem);

            // Check the type of the template
            const templateType = this.templateSettings.type.toUpperCase();
            const configurationTab = this.mainTabStrip.element.find(".config-tab");
            const developmentTab = this.mainTabStrip.element.find(".development-tab");
            if (templateType !== "XML") {
                this.mainTabStrip.disable(configurationTab);
                // If the template is not an XML template, and the currently
                // selected tab is the configuration tab, switch to the development tab.
                if (this.mainTabStrip.select().hasClass("config-tab")) {
                    this.mainTabStrip.select(developmentTab);
                }
            }
            else {
                this.mainTabStrip.enable(configurationTab);
            }
        }

        /**
         * Event for when the user starts dragging an item in the tree view.
         * @param {any} event The dragstart event of the a kendoTreeView.
         */
        async onTreeViewDragStart(event) {
            // Virtual items cannot be dragged.
            if (event.sourceNode.isVirtualItem) event.preventDefault();
        }

        /**
         * Event for when the user finished dragging an item in the tree view.
         * @param {any} event The drop event of a kendoTreeView.
         */
        async onTreeViewDropItem(event) {
            if (!event.valid) {
                return;
            }

            try {
                const sourceDataItem = event.sender.dataItem(event.sourceNode);
                const destinationDataItem = event.sender.dataItem(event.destinationNode);

                if (sourceDataItem.isVirtualItem || destinationDataItem.isVirtualItem) {
                    // Cannot drag to or from virtual items.
                    event.setValid(false);
                    return;
                }

                await Wiser.api({
                    url: `${this.base.settings.wiserApiRoot}templates/${encodeURIComponent(sourceDataItem.templateId)}/move/${encodeURIComponent(destinationDataItem.templateId)}?dropPosition=${encodeURIComponent(event.dropPosition)}`,
                    method: "PUT",
                    contentType: "application/json"
                });
            } catch (exception) {
                console.error(exception);
                kendo.alert(`Er is iets fout gegaan met het verplaatsen van dit item. De fout was:<br>${exception.responseText || exception.statusText}`);
                event.setValid(false);
            }
        }

        async onDropFile(event) {
            event.preventDefault();

            if (!event.originalEvent.dataTransfer.items) {
                return;
            }

            for (let i = 0; i < event.originalEvent.dataTransfer.items.length; i++) {
                if (event.originalEvent.dataTransfer.items[i].kind !== "file") {
                    continue;
                }

                let file = event.originalEvent.dataTransfer.items[i].getAsFile();
                let reader = new FileReader();
                reader.onload = ((file) => {
                    return async (event) => {
                        const content = event.target.result;
                        let filename = file.name.split(".");
                        filename.splice(-1);
                        filename = filename.join(".");
                        const treeviewtab = window.Templates.treeViewTabs[window.Templates.treeViewTabStrip.select().index()];
                        const treeview = $(window.Templates.treeViewTabStrip.contentElement(window.Templates.treeViewTabStrip.select().index()).querySelector("ul")).data("kendoTreeView");
                        let parentId = treeviewtab.templateId;

                        await window.Templates.createNewTemplate(
                            parentId,
                            filename,
                            window.Templates.templateTypes[treeviewtab.templateName.toUpperCase()],
                            treeview,
                            !parentId ? undefined : treeview.select(),
                            content
                        );
                    }
                })(file);
                reader.readAsText(file);
            }
        }

        /**
         * Event for when the context menu of the tree view gets opened.
         * @param {any} event The open event of a kendoContextMenu.
         */
        onContextMenuOpen(event) {
            if (event.target.closest("#search-results-treeview")) {
                event.preventDefault();
                return;
            }

            let selectedItemIsRootDirectory = false;
            let selectedItem;
            if (!event.target.closest(".k-treeview")) {
                selectedItemIsRootDirectory = true;
                const tabIndex = $(event.target).index();
                selectedItem = this.treeViewTabs[tabIndex];
                this.treeViewTabStrip.select(tabIndex);
            } else {
                const treeView = this.mainTreeView[this.treeViewTabStrip.select().index()];
                selectedItem = treeView.dataItem(event.target);
            }

            // Virtual items do not have a context menu.
            if (selectedItem.isVirtualItem) {
                event.preventDefault();
                return;
            }

            event.item.find("[action='addNewItem']").toggleClass("hidden", !selectedItem || !selectedItem.isFolder);
            event.item.find("[action='rename'], [action='delete']").toggleClass("hidden", selectedItemIsRootDirectory);
        }

        /**
         * Event for when the user selects an option in the context menu of the main tree view.
         * @param {any} event The select event of a kendoContextMenu.
         */
        onContextMenuSelect(event) {
            const selectedOption = $(event.item);
            const node = $(event.target);
            const treeView = this.mainTreeView[this.treeViewTabStrip.select().index()];

            let isFromRootItem;
            let selectedItem;
            if (!event.target.closest(".k-treeview")) {
                selectedItem = this.treeViewTabs[$(event.target).index()];
                isFromRootItem = true;
            } else {
                selectedItem = treeView.dataItem(node);
                isFromRootItem = false;
            }

            const action = selectedOption.attr("action");

            switch (action) {
                case "addNewItem":
                    this.openCreateNewItemDialog(true, selectedItem, isFromRootItem, node);
                    break;
                case "rename":
                    kendo.prompt("Vul een nieuwe naam in", selectedItem.templateName).then((newName) => {
                        // Show extra warning for views, routines, and triggers.
                        if ([this.templateTypes.VIEWS, this.templateTypes.ROUTINES, this.templateTypes.TRIGGERS].includes(selectedItem.templateType)) {
                            let type;
                            switch (selectedItem.templateType) {
                                case this.templateTypes.VIEWS:
                                    type = "view";
                                    break;
                                case this.templateTypes.ROUTINES:
                                    type = "routine";
                                    break;
                                case this.templateTypes.TRIGGERS:
                                    type = "trigger";
                                    break;
                            }

                            Wiser.showConfirmDialog(`Let op: Als u de template hernoemt dan wordt de bijbehorende ${type} ook hernoemd. Wilt u toch doorgaan?`, `Hernoemen van ${type}`, "Nee", "Ja").then(() => {
                                this.renameItem(selectedItem.templateId, newName).then(() => treeView.text(node, newName));
                            });
                        } else {
                            this.renameItem(selectedItem.templateId, newName).then(() => treeView.text(node, newName));
                        }
                    });
                    break;
                case "delete":
                    Wiser.showConfirmDialog(`Weet u zeker dat u het item "${selectedItem.templateName}" en alle onderliggende items wilt verwijderen?`).then(() => {
                        if ([this.templateTypes.VIEWS, this.templateTypes.ROUTINES, this.templateTypes.TRIGGERS].includes(selectedItem.templateType)) {
                            let type;
                            switch (selectedItem.templateType) {
                                case this.templateTypes.VIEWS:
                                    type = "view";
                                    break;
                                case this.templateTypes.ROUTINES:
                                    type = "routine";
                                    break;
                                case this.templateTypes.TRIGGERS:
                                    type = "trigger";
                                    break;
                            }

                            Wiser.showConfirmDialog(`Let op: Als u de template verwijdert dan wordt de bijbehorende ${type} ook verwijderd. Wilt u toch doorgaan?`, `Verwijderen van ${type}`, "Nee", "Ja").then(() => {
                                this.deleteItem(selectedItem.templateId).then(() => treeView.remove(node));
                            });
                        } else {
                            this.deleteItem(selectedItem.templateId).then(() => treeView.remove(node));
                        }
                    });
                    break;
                default:
                    kendo.alert(`Onbekende actie '${action}'. Probeer het a.u.b. opnieuw op neem contact op.`);
                    break;
            }
        }

        /**
         * Load a template for editing.
         * @param {any} id The ID of the template to load.
         * @param {Object} [virtualItem=null] If the item is a virtual item, this parameter will contain the necessary data to open it.
         */
        async loadTemplate(id, virtualItem = null) {
            const dynamicContentTab = this.mainTabStrip.element.find(".dynamic-tab");
            const historyTab = this.mainTabStrip.element.find(".history-tab");

            if (id <= 0 && (virtualItem === null || virtualItem.templateType === 0)) {
                this.templateSettings = {};
                this.linkedTemplates = null;
                this.templateHistory = null;

                document.getElementById("developmentTab").innerHTML = "";
                this.mainTabStrip.disable(dynamicContentTab);
                this.mainTabStrip.disable(historyTab);
                return;
            }

            const process = `onTreeViewSelect_${Date.now()}`;
            window.processing.addProcess(process);

            try {
                let promises;

                let templateSettings, linkedTemplates, templateHistory;
                let isVirtualTemplate = false;
                if (virtualItem !== null) {
                    isVirtualTemplate = true;

                    promises = [
                        Wiser.api({
                            url: `${this.settings.wiserApiRoot}templates/get-virtual-item?objectName=${virtualItem.objectName}&templateType=${virtualItem.templateType}`,
                            dataType: "json",
                            method: "GET"
                        })
                    ];

                    [templateSettings] = await Promise.all(promises);
                    linkedTemplates = {
                        linkedScssTemplates: [],
                        linkedCssTemplates: [],
                        linkedJavascript: [],
                        linkOptionsTemplates: []
                    };

                    // Retrieve parent ID so it can be set on the template settings.
                    const selectedTabIndex = this.treeViewTabStrip.select().index();
                    const selectedTabContentElement = this.treeViewTabStrip.contentElement(selectedTabIndex);
                    const treeViewElement = selectedTabContentElement.querySelector("ul");
                    templateSettings.parentId = Number(treeViewElement.dataset.id);

                    this.templateSettings = templateSettings;
                    this.linkedTemplates = linkedTemplates;
                    this.templateHistory = null;
                } else {
                    // Get template settings and linked templates.
                    promises = [
                        Wiser.api({
                            url: `${this.settings.wiserApiRoot}templates/${id}/settings`,
                            dataType: "json",
                            method: "GET"
                        }),
                        Wiser.api({
                            url: `${this.settings.wiserApiRoot}templates/${id}/linked-templates`,
                            dataType: "json",
                            method: "GET"
                        })
                    ];

                    [templateSettings, linkedTemplates, templateHistory] = await Promise.all(promises);
                    this.templateSettings = templateSettings;
                    this.linkedTemplates = linkedTemplates;
                    this.templateHistory = templateHistory;
                }

                // Load the different tabs.
                promises = [];

                // Development
                promises.push(
                    Wiser.api({
                        method: "POST",
                        contentType: "application/json",
                        url: "/Modules/Templates/DevelopmentTab",
                        data: JSON.stringify({
                            templateSettings: templateSettings,
                            linkedTemplates: linkedTemplates
                        })
                    }).then(async (response) =>  {
                        document.getElementById("developmentTab").innerHTML = response;
                        await this.initKendoDeploymentTab();
                        this.updateAlwaysLoadAndUrlRegexAvailability();
                        this.bindDeployButtons(id);
                        this.bindDevelopmentTabEvents();
                    })
                );

                await Promise.all(promises);

                // Check if the table name select exists and if so, populate it.
                const triggerTableNameSelect = document.getElementById("triggerTable");
                if (triggerTableNameSelect) {
                    // Retrieve all table names.
                    const tableNames = await Wiser.api({
                        method: "GET",
                        url: `${this.settings.wiserApiRoot}templates/get-trigger-table-names`,
                        dataType: "json"
                    });

                    // Add the table names to the data source.
                    const kendoDropDownList = $(triggerTableNameSelect).getKendoDropDownList();
                    tableNames.forEach((tableName) => {
                        kendoDropDownList.dataSource.add({
                            text: tableName,
                            value: tableName
                        });
                    });

                    // Select the correct table name.
                    kendoDropDownList.value(templateSettings.triggerTableName);
                    this.initialTemplateSettings.triggerTableName = templateSettings.triggerTableName;
                }

                window.processing.removeProcess(process);

                // Add user to the connected users (uses Pusher).
                this.connectedUsers.switchTemplate(id);

                // Only load dynamic content for HTML templates.
                const isHtmlTemplate = this.templateSettings.type.toUpperCase() === "HTML";

                // Database elements (views, routines and templates) disable some functionality that do not apply to these functions.
                this.toggleElementsForDatabaseTemplates(this.templateSettings.type);

                if (isVirtualTemplate) {
                    // History tab is not available for virtual items.
                    this.mainTabStrip.disable(historyTab);

                    // Connected users information is not available for virtual items.
                    const connectedUsers = document.querySelector("div.connected-users");
                    connectedUsers.classList.add("hidden");

                    // Hide the "last update" status.
                    document.getElementById("published-environments").querySelector("h4").classList.add("hidden");
                } else {
                    this.mainTabStrip.enable(historyTab);
                }

                if (!isHtmlTemplate) {
                    this.mainTabStrip.disable(dynamicContentTab);

                    const selectedTab = this.mainTabStrip.select();
                    if (selectedTab.hasClass("dynamic-tab")) {
                        this.mainTabStrip.select(0);
                    }

                    return;
                }

                this.mainTabStrip.enable(dynamicContentTab);

                // Dynamic content
                const dynamicGridDiv = $("#dynamic-grid");
                this.dynamicContentGrid = dynamicGridDiv.kendoGrid({
                    dataSource: {
                        transport: {
                            read: (readOptions) => {
                                Wiser.api({
                                    url: `${this.settings.wiserApiRoot}templates/${id}/linked-dynamic-content`,
                                    dataType: "json",
                                    method: "GET"
                                }).then((response) => {
                                    readOptions.success(response);
                                    this.initDynamicContentDisplayFields(response);
                                }).catch((error) => {
                                    readOptions.error(error);
                                });
                            }
                        },
                        pageSize: 20
                    },
                    scrollable: true,
                    resizable: true,
                    selectable: "row",
                    filterable: {
                        extra: false,
                        messages: {
                            isTrue: "<span>Ja</span>",
                            isFalse: "<span>Nee</span>"
                        }
                    },
                    pageable: true,
                    columns: [
                        {
                            field: "id",
                            title: "ID"
                        },
                        {
                            field: "title",
                            title: "Naam",
                            width: 150,
                            filterable: true
                        },
                        {
                            field: "component",
                            title: "Type",
                            width: "10%",
                            filterable: true
                        },
                        {
                            field: "displayUsages",
                            title: "Gebruikt in",
                            width: 150,
                            filterable: true
                        },
                        {
                            field: "renders",
                            title: "Aantal renders",
                            filterable: true
                        },
                        {
                            field: "avgRenderTime",
                            title: "Gem. rendertijd",
                            filterable: false
                        },
                        {
                            field: "displayDate",
                            title: "Laatst aangepast op",
                            width: 150,
                            filterable: {
                                ui: "datepicker"
                            }
                        },
                        {
                            title: "Versies",
                            width: 225,
                            filterable: false,
                            template: "<span class='version'>#=Math.max(...versions.versionList)#</span> | <span class='version'><ins class='live'></ins>#=versions.liveVersion#</span> | <span class='version'><ins class='accept'></ins>#=versions.acceptVersion#</span> | <span class='version'><ins class='test'></ins>#=versions.testVersion#</span>"
                        },
                        {
                            field: "changedBy",
                            title: "Door",
                            width: 120,
                            filterable: true
                        },
                        {
                            command: [
                                {
                                    name: "Open",
                                    text: "",
                                    iconClass: "k-icon k-i-edit",
                                    click: this.onDynamicContentOpenClick.bind(this)
                                },
                                {
                                    name: "Duplicate",
                                    text: "",
                                    iconClass: "k-icon k-i-copy",
                                    click: this.onDynamicContentDuplicateClick.bind(this, id)
                                },
                                {
                                    name: "Delete",
                                    text: "",
                                    iconClass: "k-icon k-i-trash",
                                    click: this.onDynamicContentDeleteClick.bind(this)
                                },
                            ],
                            title: "&nbsp;",
                            width: 160,
                            filterable: false
                        }
                    ],
                    toolbar: [
                        {
                            name: "add",
                            text: "Nieuw",
                            template: `<a class='k-button k-button-icontext' href='\\#' onclick='return window.Templates.openDynamicContentWindow(0, "Nieuw dynamische content toevoegen")'><span class='k-icon k-i-file-add'></span>Nieuw item toevoegen</a>`
                        },
                        {
                            name: "linkExisting",
                            text: "Component van andere template koppelen",
                            template: `<a class='k-button k-button-icontext' href='\\#' onclick='return window.Templates.openLinkableComponentsDialog(${id})'><span class='k-icon k-i-hyperlink-insert'></span>Component van andere template koppelen</a>`
                        },
                        {
                            name: "publishToEnvironments",
                            text: "Deploy",
                            template: `<a class='k-button k-button-icontext deploy-button hidden' href='\\#' onclick='return window.Templates.openDeployDynamicContentWindow()'><span class='k-icon k-i-cloud'></span>&nbsp;Deploy</a>`
                        }
                    ],
                    change: this.onDynamicContentGridChange.bind(this),
                    dataBound: this.onDynamicContentGridChange.bind(this)
                }).data("kendoGrid");
                dynamicGridDiv.kendoTooltip({ filter: ".k-grid-Open", content: "Bewerken" });
                dynamicGridDiv.kendoTooltip({ filter: ".k-grid-Duplicate", content: "Dupliceren" });
                dynamicGridDiv.kendoTooltip({ filter: ".k-grid-Delete", content: "Verwijderen" });

                // Open dynamic content by double clicking on a row.
                dynamicGridDiv.on("dblclick", "tr.k-state-selected", this.onDynamicContentOpenClick.bind(this));

            } catch (exception) {
                console.error(exception);
                kendo.alert(`Er is iets fout gegaan. Probeer het a.u.b. opnieuw of neem contact op met ons.<br>${exception.responseText || exception}`);
                window.processing.removeProcess(process);
            }
        }

        /**
         * Database elements (views, routines and templates) disable some functionality that do not apply to these functions.
         * @param {string} templateType The type of template that is opened.
         */
        toggleElementsForDatabaseTemplates(templateType = "") {
            const isDatabaseElementTemplate = ["VIEW", "ROUTINE", "TRIGGER"].includes(templateType.toUpperCase());

            if (!isDatabaseElementTemplate) {
                return;
            }

            const saveAndDeployToTestButton = document.getElementById("saveAndDeployToTestButton");
            $(saveAndDeployToTestButton).getKendoButton().enable(false);
            saveAndDeployToTestButton.classList.add("hidden");
            const publishedEnvironments = document.getElementById("published-environments");
            publishedEnvironments.querySelectorAll(".version-accept, .version-test").forEach((element) => {
                element.classList.add("hidden");
            });
        }

        /**
         * Checks if the current template will conflict with another template based on the default header/footer settings.
         * @param {number} templateId The current template's ID.
         * @param {boolean} isDefaultHeader Whether this template should act as a default header.
         * @param {boolean} isDefaultFooter Whether this template should act as a default footer.
         * @param {string} defaultHeaderFooterRegex The regular expression that will be used to check the URL to limit which pages can use this default header and/or footer.
         * @returns {Object} An object with keys "hasConflict" (boolean) and "conflictedWith" (a string array).
         */
        async checkDefaultHeaderOrFooterConflict(templateId, isDefaultHeader, isDefaultFooter, defaultHeaderFooterRegex) {
            if (!isDefaultHeader && !isDefaultFooter) {
                return false;
            }

            const process = `checkDefaultHeaderOrFooterConflict_${Date.now()}`;
            window.processing.addProcess(process);

            try {
                const promises = [];

                // Add promises based on parameters.
                if (isDefaultHeader) {
                    promises.push(Wiser.api({
                        url: `${this.settings.wiserApiRoot}templates/${templateId}/check-default-header-conflict`,
                        data: {
                            regexString: defaultHeaderFooterRegex
                        },
                        dataType: "json",
                        method: "GET"
                    }));
                }
                if (isDefaultFooter) {
                    promises.push(Wiser.api({
                        url: `${this.settings.wiserApiRoot}templates/${templateId}/check-default-footer-conflict`,
                        data: {
                            regexString: defaultHeaderFooterRegex
                        },
                        dataType: "json",
                        method: "GET"
                    }));
                }

                // Result will be an array of responses.
                const result = await Promise.all(promises);
                let hasConflict = false;
                const conflictedWith = [];

                // The value of "conflict" will be the name of a template that this template will conflict with.
                result.forEach((conflict) => {
                    if (typeof conflict !== "string" || conflict === "") return;

                    hasConflict = true;
                    conflictedWith.push(conflict);
                });

                window.processing.removeProcess(process);

                // Return whether there's a conflict and which template(s) this template conflicts with.
                return {
                    hasConflict: hasConflict,
                    conflictedWith: conflictedWith
                };
            } catch (exception) {
                kendo.alert(`Er is iets fout gegaan. Probeer het a.u.b. opnieuw of neem contact op met ons.<br>${exception.responseText || exception}`);
                window.processing.removeProcess(process);
            }
        }

        //Initializes the kendo components on the deployment tab. These are seperated from other components since these can be reloaded by the application.
        async initKendoDeploymentTab() {
            $("#deployLive, #deployAccept, #deployTest, #deployToBranchButton").kendoButton();

            $("#saveAndDeployToTestButton").kendoButton();
            $("#saveButton").kendoButton({
                icon: "save"
            });
            $("#closeButton").kendoButton();

            if (!this.branches || !this.branches.length) {
                $(".branch-container").addClass("hidden");
            } else {
                $(".branch-container").removeClass("hidden");
                $("#branchesDropDown").kendoDropDownList({
                    dataSource: this.branches,
                    dataValueField: "id",
                    dataTextField: "name",
                    optionLabel: "Kies een branch..."
                });
            }

            this.bindDeploymentTabEvents();

            // ComboBox
            $(".combo-select").each((index, select) => {
                const filter = select.dataset.filter || "none";
                $(select).kendoDropDownList({
                    filter: filter
                });
            });

            const editorElement = $(".editor");
            const editorType = editorElement.data("editorType");

            if (editorType === "text/html") {
                // HTML editor
                const insertDynamicContentTool = {
                    name: "wiserDynamicContent",
                    tooltip: "Dynamische inhoud toevoegen",
                    exec: this.onHtmlEditorDynamicContentExec.bind(this)
                };
                const htmlSourceTool = {
                    name: "wiserHtmlSource",
                    tooltip: "HTML bekijken/aanpassen",
                    exec: this.onHtmlEditorHtmlSourceExec.bind(this)
                };

                const wiserApiRoot = this.settings.wiserApiRoot;
                const imagesRootId = this.settings.imagesRootId;
                const filesRootId = this.settings.filesRootId;

                const translationsTool = {
                    name: "wiserTranslation",
                    tooltip: "Vertaling invoegen",
                    exec: function(e) { Wiser.onHtmlEditorTranslationExec.call(Wiser, e, $(this).data("kendoEditor"), wiserApiRoot); }
                };

                const imageTool = {
                    name: "wiserImage",
                    tooltip: "Afbeelding toevoegen",
                    exec: function(e){ Wiser.onHtmlEditorImageExec.call(Wiser, e, $(this).data("kendoEditor"), "templates", imagesRootId); }
                };

                const fileTool = {
                    name: "wiserFile",
                    tooltip: "Link naar bestand toevoegen",
                    exec: function(e) { Wiser.onHtmlEditorFileExec.call(Wiser, e, $(this).data("kendoEditor"), "templates", filesRootId); }
                };

                this.mainHtmlEditor = $(".editor").kendoEditor({
                    resizable: true,
                    tools: [
                        insertDynamicContentTool,
                        "bold",
                        "italic",
                        "underline",
                        "strikethrough",
                        "justifyLeft",
                        "justifyCenter",
                        "justifyRight",
                        "justifyFull",
                        "insertUnorderedList",
                        "insertOrderedList",
                        "indent",
                        "outdent",
                        "createLink",
                        "unlink",
                        imageTool,
                        fileTool,
                        "subscript",
                        translationsTool,
                        "superscript",
                        "tableWizard",
                        "createTable",
                        "addRowAbove",
                        "addRowBelow",
                        "addColumnLeft",
                        "addColumnRight",
                        "deleteRow",
                        "deleteColumn",
                        htmlSourceTool,
                        "formatting",
                        "cleanFormatting"
                    ],
                    serialization: {
                        custom: function(html) {
                            return html.replace(/\[(>|&gt;)\]([\w:?]+)\[(<|&lt;)\]/g, "{$2}");
                        }
                    },
                    deserialization: {
                        custom: function(html) {
                            return html.replace(/{([\w:?]+)}/g, "[>]$1[<]");
                        }
                    }
                }).data("kendoEditor");
            } else {
                // Initialize Code Mirror.
                await Misc.ensureCodeMirror();
                const codeMirrorInstance = CodeMirror.fromTextArea(editorElement[0], {
                    lineNumbers: true,
                    indentUnit: 4,
                    lineWrapping: true,
                    foldGutter: true,
                    gutters: ["CodeMirror-linenumbers", "CodeMirror-foldgutter", "CodeMirror-lint-markers"],
                    lint: {
                        options: {
                            esversion: 2022,
                            rules: {
                                "no-empty-rulesets": 0,
                                "no-ids": 0,
                                "indentation": [1, { size: 4 }],
                                "variable-for-property": 0,
                                "property-sort-order": 0,
                                "no-important": 0
                            }
                        }
                    },
                    extraKeys: {
                        "Ctrl-Q": (sender) => {
                            sender.foldCode(sender.getCursor());
                        },
                        "F11": (sender) => {
                            sender.setOption("fullScreen", !sender.getOption("fullScreen"));
                        },
                        "Esc": (sender) => {
                            if (sender.getOption("fullScreen")) sender.setOption("fullScreen", false);
                        },
                        "Ctrl-Space": "autocomplete"
                    },
                    mode: editorType
                });

                editorElement.data("CodeMirrorInstance", codeMirrorInstance);
            }

            const routineParametersInput = document.getElementById("routineParameters");
            if (routineParametersInput) {
                const editorType = routineParametersInput.dataset.editorType;

                // Initialize Code Mirror.
                await Misc.ensureCodeMirror();
                const codeMirrorInstance = CodeMirror.fromTextArea(routineParametersInput, {
                    lineNumbers: true,
                    indentUnit: 4,
                    lineWrapping: true,
                    foldGutter: true,
                    gutters: ["CodeMirror-linenumbers", "CodeMirror-foldgutter", "CodeMirror-lint-markers"],
                    lint: {
                        options: {
                            esversion: 2022,
                            rules: {
                                "no-empty-rulesets": 0,
                                "no-ids": 0,
                                "indentation": [1, { size: 4 }],
                                "variable-for-property": 0,
                                "property-sort-order": 0,
                                "no-important": 0
                            }
                        }
                    },
                    extraKeys: {
                        "Ctrl-Q": (sender) => {
                            sender.foldCode(sender.getCursor());
                        },
                        "F11": (sender) => {
                            sender.setOption("fullScreen", !sender.getOption("fullScreen"));
                        },
                        "Esc": (sender) => {
                            if (sender.getOption("fullScreen")) sender.setOption("fullScreen", false);
                        },
                        "Ctrl-Space": "autocomplete"
                    },
                    mode: editorType
                });
                codeMirrorInstance.setSize(null, 60);

                $(routineParametersInput).data("CodeMirrorInstance", codeMirrorInstance);
            }

            const dataSource = (this.templateSettings.externalFiles || []).sort((a, b) => {
                if (a.ordering < b.ordering) return -1;
                if (a.ordering > b.ordering) return 1;
                return 0;
            });

            const externalFilesGridElement = $("#externalFiles");
            if (externalFilesGridElement.length > 0) {
                externalFilesGridElement.kendoGrid({
                    height: 500,
                    editable: {
                        createAt: "bottom"
                    },
                    batch: true,
                    toolbar: ["create"],
                    columns: [
                        { field: "uri" },
                        { command: { name: "destroy", text: "", iconClass: "k-icon k-i-delete" }, width: 140 }
                    ],
                    dataSource: dataSource,
                    edit: (event) => {
                        if (event.model.ordering >= 0) return;
                        const orderings = event.sender.dataSource.data().filter(i => i.hasOwnProperty("ordering")).map(i => i.ordering);
                        event.model.ordering = orderings.length > 0 ? orderings[orderings.length - 1] + 1 : 1;
                    }
                });
                externalFilesGridElement.kendoTooltip({ filter: ".k-grid-delete", content: "Verwijderen" });

                const externalFilesGrid = $(externalFilesGridElement).getKendoGrid();
                externalFilesGrid.table.kendoSortable({
                    autoScroll: true,
                    hint: function (element) {
                        const table = externalFilesGrid.table.clone();
                        const wrapperWidth = externalFilesGrid.wrapper.width();
                        const wrapper = $('<div class="k-grid k-widget"></div>').width(wrapperWidth);

                        table.find("thead").remove();
                        table.find("tbody").empty();
                        table.wrap(wrapper);
                        table.append(element.clone().removeAttr("uid"));

                        const hint = table.parent();
                        return hint;
                    },
                    cursor: "move",
                    placeholder: function (element) {
                        return element.clone().addClass("k-state-hover").css("opacity", 0.65);
                    },
                    container: "#externalFiles",
                    filter: ">tbody >tr",
                    change: (event) => {
                        const oldIndex = event.oldIndex; // The old position.
                        const newIndex = event.newIndex; // The new position.
                        const view = externalFilesGrid.dataSource.view();
                        const dataItem = externalFilesGrid.dataSource.getByUid(event.item.data("uid")); // Retrieve the moved dataItem.

                        dataItem.ordering = newIndex; // Update the order
                        dataItem.dirty = true;

                        // Shift the order of the records.
                        if (oldIndex < newIndex) {
                            for (let i = oldIndex + 1; i <= newIndex; i++) {
                                view[i].ordering--;
                                view[i].dirty = true;
                            }
                        } else {
                            for (let i = oldIndex - 1; i >= newIndex; i--) {
                                view[i].ordering++;
                                view[i].dirty = true;
                            }
                        }
                    }
                });
            }

            // Pre load query field for HTML templates.
            const preLoadQueryField = $("#preLoadQuery");
            if (preLoadQueryField.length > 0) {
                // Initialize Code Mirror.
                await Misc.ensureCodeMirror();
                const codeMirrorInstance = CodeMirror.fromTextArea(preLoadQueryField[0], {
                    lineNumbers: true,
                    indentUnit: 4,
                    lineWrapping: true,
                    foldGutter: true,
                    gutters: ["CodeMirror-linenumbers", "CodeMirror-foldgutter", "CodeMirror-lint-markers"],
                    lint: true,
                    extraKeys: {
                        "Ctrl-Q": (sender) => {
                            sender.foldCode(sender.getCursor());
                        },
                        "F11": (sender) => {
                            sender.setOption("fullScreen", !sender.getOption("fullScreen"));
                        },
                        "Esc": (sender) => {
                            if (sender.getOption("fullScreen")) sender.setOption("fullScreen", false);
                        },
                        "Ctrl-Space": "autocomplete"
                    },
                    mode: preLoadQueryField.data("editorType")
                });

                preLoadQueryField.data("CodeMirrorInstance", codeMirrorInstance);
            }

            // Pre load query field for HTML templates.
            const widgetContentField = $("#widgetContent");
            if (widgetContentField.length > 0) {
                // Initialize Code Mirror.
                await Misc.ensureCodeMirror();
                const codeMirrorInstance = CodeMirror.fromTextArea(widgetContentField[0], {
                    lineNumbers: true,
                    indentUnit: 4,
                    lineWrapping: true,
                    foldGutter: true,
                    gutters: ["CodeMirror-linenumbers", "CodeMirror-foldgutter", "CodeMirror-lint-markers"],
                    lint: true,
                    extraKeys: {
                        "Ctrl-Q": (sender) => {
                            sender.foldCode(sender.getCursor());
                        },
                        "F11": (sender) => {
                            sender.setOption("fullScreen", !sender.getOption("fullScreen"));
                        },
                        "Esc": (sender) => {
                            if (sender.getOption("fullScreen")) sender.setOption("fullScreen", false);
                        },
                        "Ctrl-Space": "autocomplete"
                    },
                    mode: widgetContentField.data("editorType")
                });

                widgetContentField.data("CodeMirrorInstance", codeMirrorInstance);
            }

            const advancedSettingsToggle = $("#advanced");
            advancedSettingsToggle.change((event) => {
                if (advancedSettingsToggle.prop("checked")) {
                    if (preLoadQueryField.length > 0) {
                        preLoadQueryField.data("CodeMirrorInstance").refresh();
                    }
                    if (widgetContentField.length > 0) {
                        widgetContentField.data("CodeMirrorInstance").refresh();
                    }
                }
            });

            this.userRolesDropDown = $("select#loginRoles").kendoMultiSelect({
                placeholder: "Selecteer rol(len)...",
                clearButton: true,
                filter: "contains",
                multiple: "multiple",
                dataTextField: "name",
                dataValueField: "id",
                dataSource: {
                    transport: {
                        read: (readOptions) => {
                            Wiser.api({
                                url: `${this.settings.wiserApiRoot}users/roles`,
                                dataType: "json",
                                type: "GET"
                            }).then((result) => {
                                readOptions.success(result);
                            }).catch((result) => {
                                readOptions.error(result);
                            });
                        }
                    }
                }
            }).data("kendoMultiSelect");

            // Save the current settings so that we can keep track of any changes and warn the user if they're about to leave without saving.
            this.initialTemplateSettings = this.getCurrentTemplateSettings();
        }

        //Initialize display variable for the fields containing objects and dates within the grid.
        initDynamicContentDisplayFields(datasource) {
            datasource.forEach((row) => {
                row.displayDate = kendo.format("{0:dd MMMM yyyy}", new Date(row.changedOn));
                row.displayUsages = row.usages.join(",");
                row.displayVersions = Math.max(...row.versions.versionList) + " live: " + row.versions.liveVersion + ", Acceptatie: " + row.versions.acceptVersion + ", test: " + row.versions.testVersion;
            });
        }

        /**
         * Event that gets called when the user executes the custom action for adding dynamic content from Wiser to the HTML editor.
         * This will open a dialog where they can select any component that is linked to the current template, or add a new one.
         * @param {any} event The event from the execute action.
         * @param {any} codeMirror Optional: The CodeMirror editor where the action is executed in.
         */
        async onHtmlEditorDynamicContentExec(event, codeMirror = null) {
            let dropDown = $("#dynamicContentDropDown").data("kendoDropDownList");
            if (!dropDown) {
                dropDown = $("#dynamicContentDropDown").kendoDropDownList({
                    dataTextField: "title",
                    dataValueField: "id",
                    optionLabel: "Nieuw component"
                }).data("kendoDropDownList");
            }

            dropDown.setDataSource(this.dynamicContentGrid.dataSource);

            const dialog = $("#addDynamicContentToHtmlDialog").kendoDialog({
                width: "500px",
                title: `Dynamische inhoud invoegen`,
                closable: true,
                modal: true,
                actions: [
                    {
                        text: "Annuleren"
                    },
                    {
                        text: "Invoegen",
                        primary: true,
                        action: async () => {
                            let id = dropDown.value();
                            let title = dropDown.dataItem().title;

                            if (!id) {
                                const newContentData = await this.openDynamicContentWindow(0, "Nieuw dynamische content toevoegen");
                                if (!newContentData) {
                                    console.warn("Dynamic content was not (properly) created.");
                                    return;
                                }

                                id = newContentData.id;
                                title = newContentData.title;
                            }

                            if (!id) {
                                console.warn("Dynamic content was not (properly) created.");
                                return;
                            }

                            const html = `<div class="dynamic-content" content-id="${id}"><h2>${title}</h2></div>`;

                            if (codeMirror) {
                                const doc = codeMirror.getDoc();
                                const cursor = doc.getCursor();
                                doc.replaceRange(html, cursor);
                            } else {
                                this.mainHtmlEditor.exec("inserthtml", { value: html });
                            }
                        }
                    }
                ]
            }).data("kendoDialog");

            dialog.open();
        }

        /**
         * Event that gets called when the user executes the custom action for viewing / changing the HTML source of the editor.
         * @param {any} event The event from the execute action.
         * @param {any} editor The HTML editor where the action is executed in.
         * @param {any} itemId The ID of the current item.
         */
        async onHtmlEditorHtmlSourceExec(event, editor) {
            const htmlWindow = $("#htmlSourceWindow").clone(true);
            const textArea = htmlWindow.find("textarea").val(this.mainHtmlEditor.value());
            // Prettify code from minified text.
            const pretty = await require("pretty");
            textArea[0].value = pretty(textArea[0].value, { ocd: false });
            let codeMirrorInstance;

            htmlWindow.kendoWindow({
                width: "100%",
                height: "100%",
                title: "HTML van editor",
                activate: async (activateEvent) => {
                    const codeMirrorSettings = {
                        lineNumbers: true,
                        indentUnit: 4,
                        lineWrapping: true,
                        foldGutter: true,
                        gutters: ["CodeMirror-linenumbers", "CodeMirror-foldgutter", "CodeMirror-lint-markers"],
                        lint: true,
                        extraKeys: {
                            "Ctrl-Q": function (cm) {
                                cm.foldCode(cm.getCursor());
                            },
                            "Ctrl-Space": "autocomplete"
                        },
                        mode: "text/html"
                    };

                    // Only load code mirror when we actually need it.
                    await Misc.ensureCodeMirror();
                    codeMirrorInstance = CodeMirror.fromTextArea(textArea[0], codeMirrorSettings);
                },
                resize: (resizeEvent) => {
                    codeMirrorInstance.refresh();
                },
                close: (closeEvent) => {
                    closeEvent.sender.destroy();
                    htmlWindow.remove();
                }
            });

            const kendoWindow = htmlWindow.data("kendoWindow").maximize().open();

            htmlWindow.find(".addDynamicContent").kendoButton({
                click: () => {
                    this.onHtmlEditorDynamicContentExec(event, codeMirrorInstance);
                },
                icon: "css"
            });

            htmlWindow.find(".k-primary").kendoButton({
                click: () => {
                    this.mainHtmlEditor.value(codeMirrorInstance.getValue());
                    kendoWindow.close();
                },
                icon: "save"
            });
            htmlWindow.find(".k-secondary").kendoButton({
                click: () => {
                    kendoWindow.close();
                },
                icon: "cancel"
            });
        }

        onDynamicContentOpenClick(event) {
            const tr = $(event.currentTarget).closest("tr");
            const data = this.dynamicContentGrid.dataItem(tr);
            this.openDynamicContentWindow(data.id, data.title);
        }

        onDynamicContentDuplicateClick(templateId, event) {
            const process = `duplicateComponent_${Date.now()}`;
            window.processing.addProcess(process);

            const tr = $(event.currentTarget).closest("tr");
            const data = this.dynamicContentGrid.dataItem(tr);

            Wiser.api({
                url: `${this.settings.wiserApiRoot}dynamic-content/${data.id}/duplicate?templateId=${templateId}`,
                dataType: "json",
                type: "POST",
                contentType: "application/json"
            }).then(() => {
                this.dynamicContentGrid.dataSource.read();
            }).finally(() => {
                window.processing.removeProcess(process);
            });
        }

        onDynamicContentDeleteClick(event) {
            const tr = $(event.currentTarget).closest("tr");
            const data = this.dynamicContentGrid.dataItem(tr);

            Wiser.showConfirmDialog(`Weet u zeker dat u het item '${data.title}' wilt verwijderen?`).then(async () => {
                    Wiser.api({
                        url: `${this.settings.wiserApiRoot}dynamic-content/${data.id}`,
                        dataType: "json",
                        type: "DELETE",
                        contentType: "application/json"
                    }).then(() => {
                        this.dynamicContentGrid.dataSource.read();
                    }).fail((jqXhr, textStatus, errorThrown) => {
                        console.error(errorThrown);
                        kendo.alert("Er is iets fout gegaan tijdens het verwijderen van dit item. Probeer het a.u.b. nogmaals of neem contact op met ons.");
                    });
            })

        }

        onDynamicContentGridChange(event) {
            this.dynamicContentGrid.element.find(".k-toolbar .deploy-button").toggleClass("hidden", this.dynamicContentGrid.select().length === 0);
        }

        openDynamicContentWindow(contentId, title) {
            return new Promise((resolve) => {
                this.newContentId = 0;
                this.newContentTitle = null;

                const dynamicContentWindow = $("#dynamicContentWindow").kendoWindow({
                    width: "100%",
                    height: "100%",
                    actions: ["close"],
                    draggable: false,
                    iframe: true,
                    content: `/Modules/DynamicContent/${contentId || 0}?templateId=${this.selectedId}`,
                    close: (closeWindowEvent) => {
                        this.dynamicContentGrid.dataSource.read();
                        resolve({ id: this.newContentId, title: this.newContentTitle });
                    }
                }).data("kendoWindow").maximize().open();

                dynamicContentWindow.title(title);
                dynamicContentWindow.maximize().open();
            });
        }

        async openLinkableComponentsDialog(templateId) {
            let dropDown = $("#allDynamicContentDropDown").data("kendoDropDownList");
            if (!dropDown) {
                dropDown = $("#allDynamicContentDropDown").kendoDropDownList({
                    dataTextField: "title",
                    dataValueField: "id",
                    optionLabel: "Kies een component"
                }).data("kendoDropDownList");
            }

            const allDynamicContent = await Wiser.api({
                url: `${this.settings.wiserApiRoot}dynamic-content/linkable?templateId=${templateId}`,
                dataType: "json",
                method: "GET"
            });

            dropDown.setDataSource({
                data: allDynamicContent,
                group: { field: "templatePath" }
            });

            const dialog = $("#linkExistingDynamicContentDialog").kendoDialog({
                width: "500px",
                title: `Dynamische inhoud koppelen`,
                closable: true,
                modal: true,
                actions: [
                    {
                        text: "Annuleren"
                    },
                    {
                        text: "Koppelen",
                        primary: true,
                        action: async () => {
                            let id = dropDown.value();

                            if (!id) {
                                return;
                            }

                            await Wiser.api({
                                url: `${this.settings.wiserApiRoot}dynamic-content/${id}/link/${templateId}`,
                                dataType: "json",
                                method: "PUT",
                                contentType: "application/json"
                            });

                            this.dynamicContentGrid.dataSource.read();
                        }
                    }
                ]
            }).data("kendoDialog");

            dialog.open();
        }

        dynamicContentWindowIsOpen() {
            return $("#dynamicContentWindow").data("kendoWindow") && $("#dynamicContentWindow").is(":visible");
        }

        openDeployDynamicContentWindow() {
            return new Promise(async (resolve) => {
                const selectedDataItem = this.dynamicContentGrid.dataItem(this.dynamicContentGrid.select());
                if (!selectedDataItem) {
                    resolve();
                    return;
                }

                const selectedComponentData = await Wiser.api({
                    url: `${this.settings.wiserApiRoot}dynamic-content/${selectedDataItem.id}?includeSettings=false`,
                    dataType: "json",
                    method: "GET"
                });

                const html = await Wiser.api({
                    url: `/Modules/DynamicContent/PublishedEnvironments`,
                    method: "POST",
                    contentType: "application/json",
                    data: JSON.stringify(selectedComponentData)
                });

                const window = $("#deployDynamicContentWindow").kendoWindow({
                    title: `Deploy ${selectedComponentData.title}`,
                    width: "500px",
                    height: "200px",
                    actions: ["close"],
                    modal: true,
                    close: (closeWindowEvent) => {
                        this.dynamicContentGrid.dataSource.read();
                        resolve();
                    }
                }).data("kendoWindow").content(html).maximize().open();

                $("#deployLiveComponent, #deployAcceptComponent, #deployTestComponent, #deployComponentToBranchButton").kendoButton();
                $("#published-environments-dynamic-component .combo-select").kendoDropDownList();

                if (!this.branches || !this.branches.length) {
                    $(".component-branch-container").addClass("hidden");
                } else {
                    $(".component-branch-container").removeClass("hidden");
                    $("#componentBranchesDropDown").kendoDropDownList({
                        dataSource: this.branches,
                        dataValueField: "id",
                        dataTextField: "name",
                        optionLabel: "Kies een branch..."
                    });
                }
                this.bindDynamicComponentDeployButtons(selectedDataItem.id);
            });
        }

        //Bind the deploybuttons for the template versions
        bindDeployButtons(templateId) {
            $("#deployLive").on("click", this.deployEnvironment.bind(this, "live", templateId, null));
            $("#deployAccept").on("click", this.deployEnvironment.bind(this, "accept", templateId, null));
            $("#deployTest").on("click", this.deployEnvironment.bind(this, "test", templateId, null));
        }

        bindDynamicComponentDeployButtons(contentId) {
            $("#deployLiveComponent").on("click", this.deployDynamicContentEnvironment.bind(this, "live", contentId, null));
            $("#deployAcceptComponent").on("click", this.deployDynamicContentEnvironment.bind(this, "accept", contentId, null));
            $("#deployTestComponent").on("click", this.deployDynamicContentEnvironment.bind(this, "test", contentId, null));
            $("#deployComponentToBranchButton").on("click", this.deployComponentToBranch.bind(this, "test", contentId, null));
        }

        //Deploy a version to an enviorenment
        async deployEnvironment(environment, templateId, version) {
            try {
                version = parseInt(version || document.querySelector(`#published-environments .version-${environment} select.combo-select`).value);
                if (!version) {
                    kendo.alert("U heeft geen geldige versie geselecteerd.");
                    return;
                }

                let environmentEnum;
                switch (environment) {
                    case "test":
                        environmentEnum = 2;
                        break;
                    case "accept":
                        environmentEnum = 4;
                        break;
                    case "live":
                        environmentEnum = 8;
                        break;
                    default:
                        environmentEnum = 1;
                        break;
                }

                await Wiser.api({
                    url: `${this.settings.wiserApiRoot}templates/${templateId}/publish/${environmentEnum}/${version}`,
                    dataType: "json",
                    type: "POST",
                    contentType: "application/json"
                });

                // No message needs to be shown if deployed to the development environment because this is default.
                if (environmentEnum !== 1) {
                    window.popupNotification.show(`Template is succesvol naar de ${environment} omgeving gezet`, "info");
                }
                this.lastLoadedHistoryPartNumber = 0;
                this.measurementsLoaded = false;
                await this.reloadMetaData(templateId);
            } catch (exception) {
                console.error(exception);
                kendo.alert(`Er is een fout opgetreden bij het deployen van de template: ${exception.responseText || exception}`);
            }
        }

        async deployDynamicContentEnvironment(environment, contentId, version) {
            version = version || document.querySelector(`#published-environments-dynamic-component .version-${environment} select.combo-select`).value;
            if (!version) {
                kendo.alert("U heeft geen geldige versie geselecteerd.");
                return;
            }

            let environmentEnum;
            switch (environment) {
                case "test":
                    environmentEnum = 2;
                    break;
                case "accept":
                    environmentEnum = 4;
                    break;
                case "live":
                    environmentEnum = 8;
                    break;
                default:
                    environmentEnum = 1;
                    break;
            }

            await Wiser.api({
                url: `${this.settings.wiserApiRoot}dynamic-content/${contentId}/publish/${environmentEnum}/${version}`,
                dataType: "json",
                type: "POST",
                contentType: "application/json"
            });

            window.popupNotification.show(`Dynamisch component is succesvol naar de ${environment} omgeving gezet`, "info");
            this.lastLoadedHistoryPartNumber = 0;
            this.measurementsLoaded = false;
            $("#deployDynamicContentWindow").data("kendoWindow").close();
        }

        bindEvents() {
            window.addEventListener("beforeunload", async (event) => {
                if (!this.canUnloadTemplate()) {
                    event.preventDefault();
                    event.returnValue = "";
                }
            });

            window.addEventListener("unload", async () => {
                // Remove this user from the list.
                await this.connectedUsers.removeUser();
            });

            document.addEventListener("moduleClosing", async (event) => {
                // You can do anything here that needs to happen before closing the module.
                // Remove this user from the list.
                await this.connectedUsers.removeUser();

                event.detail();
            });

            document.body.addEventListener("keydown", (event) => {
                if ((event.ctrlKey || event.metaKey) && event.keyCode === 83) {
                    event.preventDefault();

                    if (!this.dynamicContentWindowIsOpen()) {
                        this.saveTemplate();
                    } else {
                        const dynamicContentIframe = document.querySelector("#dynamicContentWindow iframe");
                        if (!dynamicContentIframe || !dynamicContentIframe.contentWindow || !dynamicContentIframe.contentWindow.DynamicContent) {
                            return;
                        }

                        dynamicContentIframe.contentWindow.DynamicContent.save();
                    }
                }
            });

            const searchForm = document.getElementById("searchForm");
            if (searchForm) {
                searchForm.addEventListener("submit", this.onSearchFormSubmit.bind(this));
            }

            $(".window-content #left-pane div.k-content").on("dragover", (event) => {
                event.preventDefault();
            });
            $(".window-content #left-pane div.k-content").on("drop", this.onDropFile.bind(this));

            document.addEventListener("TemplateConnectedUsers:UsersUpdate", (event) => {
                console.log("TemplateConnectedUsers:UsersUpdate", event.detail)
                document.querySelectorAll("div.connected-users").forEach(div => {
                    const list = div.querySelector("div.connected-users-list");
                    list.innerHTML = event.detail.join(", ");
                });
            });
        }

        /**
         * Binds events for inputs on the development tab.
         */
        bindDevelopmentTabEvents() {
            const alwaysLoadCheckbox = document.getElementById("loadAlways");
            const urlRegexInput = document.getElementById("urlRegex");
            if (alwaysLoadCheckbox && urlRegexInput) {
                alwaysLoadCheckbox.addEventListener("change", this.updateAlwaysLoadAndUrlRegexAvailability.bind(this));
                urlRegexInput.addEventListener("input", this.updateAlwaysLoadAndUrlRegexAvailability.bind(this));
            }
        }

        bindDeploymentTabEvents() {
            document.getElementById("closeButton").addEventListener("click", this.closeTemplate.bind(this));
            document.getElementById("saveButton").addEventListener("click", this.saveTemplate.bind(this));
            document.getElementById("saveAndDeployToTestButton").addEventListener("click", this.saveTemplate.bind(this, true));
            document.getElementById("deployToBranchButton").addEventListener("click", this.deployToBranch.bind(this, true));
        }

        /**
         * Change the name of a template or directory.
         * @param {any} id
         * @param {any} newName
         */
        async renameItem(id, newName) {
            const process = `renameItem_${Date.now()}`;
            window.processing.addProcess(process);

            let success = true;
            try {
                const response = await Wiser.api({
                    url: `${this.settings.wiserApiRoot}templates/${id}/rename?newName=${encodeURIComponent(newName)}`,
                    dataType: "json",
                    type: "POST",
                    contentType: "application/json"
                });

                window.popupNotification.show(`Template '${id}' is succesvol hernoemd naar '${newName}'`, "info");
            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan, probeer het a.u.b. opnieuw of neem contact op.");
                success = false;
            }

            window.processing.removeProcess(process);
            return success;
        }

        /**
         * Deletes a template or directory.
         * @param {any} id The ID of the template to delete.
         */
        async deleteItem(id) {
            const process = `deleteItem_${Date.now()}`;
            window.processing.addProcess(process);

            let success = true;
            try {
                await Wiser.api({
                    url: `${this.settings.wiserApiRoot}templates/${id}`,
                    dataType: "json",
                    type: "DELETE",
                    contentType: "application/json"
                });

                window.popupNotification.show(`Template '${id}' is succesvol verwijderd`, "info");
            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan, probeer het a.u.b. opnieuw of neem contact op.");
                success = false;
            }

            window.processing.removeProcess(process);
            return success;
        }

        /**
         * Gets all settings of the currently opened template, this object can be used for saving the template or for generating a preview of it.
         */
        getCurrentTemplateSettings() {
            const scssLinks = [];
            const jsLinks = [];
            document.querySelectorAll("#scss-checklist input[type=checkbox]:checked").forEach(el => { scssLinks.push({ templateId: el.dataset.template }) });
            document.querySelectorAll("#js-checklist input[type=checkbox]:checked").forEach(el => { jsLinks.push({ templateId: el.dataset.template }) });
            const editorElement = $(".editor");
            const kendoEditor = editorElement.data("kendoEditor");
            const codeMirror = editorElement.data("CodeMirrorInstance");
            let editorValue = "";
            if (kendoEditor) {
                editorValue = kendoEditor.value();
            } else if (codeMirror) {
                editorValue = codeMirror.getValue();
            }

            // Extra settings for routines.
            const routineType = document.querySelector("input[type=radio][name=routineType]:checked");
            let routineParameters = null;
            const routineReturnType = document.getElementById("routineReturnType");

            const routineParametersElement = document.getElementById("routineParameters");
            if (routineParametersElement) {
                const routineParametersEditor = $(routineParametersElement).data("CodeMirrorInstance");
                if (routineParametersEditor) {
                    routineParameters = routineParametersEditor.getValue();
                }
            }

            // Extra settings for triggers.
            const triggerTable = document.getElementById("triggerTable");
            const triggerTiming = document.querySelector("input[type=radio][name=triggerTiming]:checked");
            const triggerEvent = document.getElementById("triggerEvent");

            const urlRegexElement = document.getElementById("urlRegex");

            const settings = Object.assign({
                templateId: this.selectedId || this.settings.templateId || 0,
                name: this.templateSettings.name || "",
                type: this.templateSettings.type,
                parentId: this.templateSettings.parentId,
                editorValue: editorValue,
                linkedTemplates: {
                    linkedScssTemplates: scssLinks,
                    linkedJavascript: jsLinks
                },
                routineType: routineType ? Number(routineType.value) : 0,
                routineParameters: routineParameters,
                routineReturnType: routineReturnType ? routineReturnType.value : null,
                triggerTableName: triggerTable ? triggerTable.value : null,
                triggerTiming: triggerTiming ? Number(triggerTiming.value) : 0,
                triggerEvent: triggerEvent ? Number(triggerEvent.value) : 0,
                urlRegex: urlRegexElement ? urlRegexElement.value : null
            }, this.getNewSettings());

            const externalFilesGrid = $("#externalFiles").data("kendoGrid");
            if (externalFilesGrid) {
                settings.externalFiles = externalFilesGrid.dataSource.data().sort((a, b) => {
                    if (a.ordering < b.ordering) return -1;
                    if (a.ordering > b.ordering) return 1;
                    return 0;
                });
            }

            return settings;
        }

        /**
         * Deploy the selected template to the selected branch.
         */
        async deployToBranch() {
            if (this.saving) {
                return;
            }

            const selectedBranch = $("#branchesDropDown").data("kendoDropDownList").value();
            if (!selectedBranch) {
                kendo.alert("Selecteer a.u.b. eerst een branch.")
                return;
            }

            const process = `deployToBranch_${Date.now()}`;
            window.processing.addProcess(process);
            try {
                await Wiser.api({
                    url: `${this.settings.wiserApiRoot}templates/${this.selectedId}/deploy-to-branch/${selectedBranch}`,
                    dataType: "json",
                    type: "POST",
                    contentType: "application/json"
                });

                window.popupNotification.show(`Component is succesvol overgezet naar de geselecteerde branch.`, "info");
            }
            catch (exception) {
                console.error(exception);
                if (exception.responseText) {
                    kendo.alert(`Er is iets fout gegaan met deployen naar de gekozen branch:<br><pre>${exception.responseText}</pre>`);
                } else {
                    kendo.alert("Er is iets fout gegaan met deployen naar de gekozen branch. Probeer het a.u.b. opnieuw.");
                }
            }
            finally {
                window.processing.removeProcess(process);
            }
        }

        /**
         * Deploy the selected component to the selected branch.
         */
        async deployComponentToBranch() {
            if (this.saving) {
                return;
            }

            const selectedDataItem = this.dynamicContentGrid.dataItem(this.dynamicContentGrid.select());
            if (!selectedDataItem) {
                return;
            }

            const selectedBranch = $("#componentBranchesDropDown").data("kendoDropDownList").value();
            if (!selectedBranch) {
                kendo.alert("Selecteer a.u.b. eerst een branch.")
                return;
            }

            const process = `deployToBranch_${Date.now()}`;
            window.processing.addProcess(process);
            try {
                await Wiser.api({
                    url: `${this.settings.wiserApiRoot}dynamic-content/${selectedDataItem.id}/deploy-to-branch/${selectedBranch}`,
                    dataType: "json",
                    type: "POST",
                    contentType: "application/json"
                });

                window.popupNotification.show(`Component is succesvol overgezet naar de geselecteerde branch.`, "info");
            }
            catch (exception) {
                console.error(exception);
                if (exception.responseText) {
                    kendo.alert(`Er is iets fout gegaan met deployen naar de gekozen branch:<br><pre>${exception.responseText}</pre>`);
                } else {
                    kendo.alert("Er is iets fout gegaan met deployen naar de gekozen branch. Probeer het a.u.b. opnieuw.");
                }
            }
            finally {
                window.processing.removeProcess(process);
            }
        }
        
        async closeTemplate() {
            // check for unsaved changes
            if (this.selectedId && !this.canUnloadTemplate()) {
                try {
                    await kendo.confirm("U heeft nog openstaande wijzigingen. Weet u zeker dat u door wilt gaan?");
                } catch {
                    // Abort if cancelled
                    return;
                }
            }
            // reset treeview selections
            for (let index = 0; index < this.mainTreeView.length; index++) {
                this.mainTreeView[index].select($());
            }
            // unload loaded template
            this.loadTemplate(0);
            this.selectedId = 0;
            this.lastLoadedHistoryPartNumber = 0;
            this.measurementsLoaded = false;
            // enable add button
            $("#addButton").toggleClass("hidden", false);
        }

        /**
         * Save a new version of the selected template.
         */
        async saveTemplate(alsoDeployToTest = false) {
            if (this.saving) {
                return false;
            }

            const selectedTabIndex = this.treeViewTabStrip.select().index();
            const selectedTabContentElement = this.treeViewTabStrip.contentElement(selectedTabIndex);
            const treeViewElement = selectedTabContentElement.querySelector("ul");
            const treeView = $(treeViewElement).data("kendoTreeView");
            const dataItem = treeView.dataItem(treeView.select());
            if (!dataItem || dataItem.isFolder) {
                return false;
            }

            const process = `saveTemplate_${Date.now()}`;
            window.processing.addProcess(process);
            let success = true;
            let templateId;
            let reloadTemplateAfterSave = false;

            try {
                if (dataItem.isVirtualItem) {
                    // Virtual items don't actually have a template yet, so create one first.
                    this.saving = true;
                    const createResult = await this.createNewTemplate(Number(treeViewElement.dataset.id), dataItem.templateName, dataItem.templateType, treeView, undefined, "", treeView.select());

                    if (!createResult.success) {
                        window.processing.removeProcess(process);
                        this.saving = false;
                        return false;
                    }

                    templateId = createResult.result.templateId;
                    reloadTemplateAfterSave = true;
                    this.saving = false;
                } else {
                    templateId = this.selectedId;
                }

                if (!templateId) {
                    window.processing.removeProcess(process);
                    return false;
                }

                // Check to see if we're only uploading xml or the whole template
                let data = null;
                if (this.mainTabStrip.select().data("name") === "configuration") {
                    data = this.wtsConfiguration.getCurrentSettings();
                    this.saving = true;

                    const response = await Wiser.api({
                        url: `${this.settings.wiserApiRoot}templates/${templateId}/wtsconfiguration`,
                        dataType: "json",
                        type: "POST",
                        contentType: "application/json",
                        data: JSON.stringify(data)
                    });
                    reloadTemplateAfterSave = true;
                } else {
                    data = this.getCurrentTemplateSettings();

                    // Check if there's a conflict if the template is marked as default header and/or footer.
                    const defaultHeaderCheckbox = document.getElementById("isDefaultHeader");
                    const defaultFooterCheckbox = document.getElementById("isDefaultFooter");
                    if (defaultHeaderCheckbox && defaultFooterCheckbox) {
                        const defaultHeaderFooterRegexInput = document.getElementById("defaultHeaderFooterRegex");
                        const conflictCheck = await this.checkDefaultHeaderOrFooterConflict(data.templateId, defaultHeaderCheckbox.checked, defaultFooterCheckbox.checked, defaultHeaderFooterRegexInput.value);

                        if (conflictCheck.hasConflict) {
                            kendo.alert(`Er is al een standaard header en/of footer met dezelfde regex. Conflicterende template(s): ${conflictCheck.conflictedWith.join(", ")}`);
                            window.processing.removeProcess(process);
                            return false;
                        }
                    }

                    // No conflicts, continue saving.
                    this.saving = true;

                    const response = await Wiser.api({
                        url: `${this.settings.wiserApiRoot}templates/${templateId}`,
                        dataType: "json",
                        type: "POST",
                        contentType: "application/json",
                        data: JSON.stringify(data)
                    });
                }

                // Q: Replaced the data.name since data from wtsconfiguration doesn't grab that same name
                // and what data.name used, is what is used here now. Is that okay?
                window.popupNotification.show(`Template '${this.templateSettings.name}' is succesvol opgeslagen`, "info");
                this.lastLoadedHistoryPartNumber = 0;

                const version = (parseInt(document.querySelector(`#published-environments .version-test select.combo-select option:last-child`).value) || 0) + 1;
                await this.deployEnvironment(alsoDeployToTest === true ? "test" : "development", templateId, version);

                if (reloadTemplateAfterSave) {
                    await this.loadTemplate(templateId);
                } else {
                    await this.reloadMetaData(templateId);
                }

                // Save the current settings so that we can keep track of any changes and warn the user if they're about to leave without saving.
                if (this.mainTabStrip.select().data("name") !== "configuration") {
                    // Q: This currently only works for the development tab, should a feature be added to also make this work for the configuration tab?
                    this.initialTemplateSettings = data;
                }
            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan, probeer het a.u.b. opnieuw of neem contact op.");
                success = false;
            }

            this.saving = false;
            window.processing.removeProcess(process);
            return success;
        }

        /**
         * Search for a template
         */
        async onSearchFormSubmit(event) {
            event.preventDefault(true);
            const container = $("#searchForm").addClass("loading");
            const searchField = container.find("input");

            try {
                // If there is no search term, do nothing.
                const value = searchField.val();
                if (!value) {
                    container.removeClass("loading");
                    return;
                }

                // Call back-end to search.
                const response = await Wiser.api({
                    url: `${this.settings.wiserApiRoot}templates/search?searchValue=${encodeURIComponent(value)}`,
                    dataType: "json",
                    type: "GET",
                    contentType: "application/json"
                });

                // Show error if there are no search results.
                if (!response || !response.length) {
                    kendo.alert("Geen resultaten gevonden met de opgegeven zoekwaarde");
                    container.removeClass("loading");
                    return;
                }

                const dataSource = new kendo.data.HierarchicalDataSource({
                    data: response,
                    schema: {
                        model: {
                            id: "templateId",
                            children: "childNodes"
                        }
                    }
                });
                this.searchResultsTreeView.setDataSource(dataSource);

                const searchResultsTab = this.treeViewTabStrip.tabGroup.find("li:last-child");
                searchResultsTab.removeClass("hidden");
                this.treeViewTabStrip.select(searchResultsTab);
            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan, probeer het a.u.b. opnieuw of neem contact op.");
            }

            container.removeClass("loading");
        }

        /**
         * Reloads the publishedEnvironments and history of the template.
         * @param {any} templateId The ID of the template.
         */
        async reloadMetaData(templateId) {
            const templateMetaData = await Wiser.api({
                url: `${this.settings.wiserApiRoot}templates/${templateId}/meta`,
                dataType: "json",
                type: "GET",
                contentType: "application/json"
            });

            const response = await Wiser.api({
                method: "POST",
                contentType: "application/json",
                url: "/Modules/Templates/PublishedEnvironments",
                data: JSON.stringify(templateMetaData)
            });

            document.querySelector("#published-environments").outerHTML = response;

            // Bind deploy buttons.
            $("#deployLive, #deployAccept, #deployTest").kendoButton();
            $("#published-environments .combo-select").kendoDropDownList();
            this.bindDeployButtons(templateId);

            // Database elements (views, routines and templates) disable some functionality that do not apply to these functions.
            this.toggleElementsForDatabaseTemplates(this.templateSettings.type);
        }

        /**
         * Reloads history of the template.
         * @param {any} templateId The ID of the template.
         */
        async reloadHistoryTab(templateId) {
            if (this.lastLoadedHistoryPartNumber > 0) {
                return;
            }

            templateId = templateId || this.selectedId;

            const process = `reloadHistoryTab_${Date.now()}`;
            window.processing.addProcess(process);

            try {
                const templateHistory = await Wiser.api({
                    url: `${this.settings.wiserApiRoot}templates/${templateId}/history`,
                    dataType: "json",
                    method: "GET"
                });

                const historyTabHTML = await Wiser.api({
                    method: "POST",
                    contentType: "application/json",
                    url: "/Modules/Templates/HistoryTab",
                    data: JSON.stringify(templateHistory)
                });

                const historyTab = document.getElementById("historyTab");
                historyTab.innerHTML = historyTabHTML;
                this.lastLoadedHistoryPartNumber = 1;
                this.allHistoryPartsLoaded = false;
                historyTab.addEventListener("scroll", event => {
                    const {scrollHeight, scrollTop, clientHeight} = event.target;

                    // if user scrolled to bottom load next part of the history
                    // < treshold is used to account for rounding of scrollHeight and clientHeight
                    let treshold = 1;
                    if (Math.abs(scrollHeight - clientHeight - scrollTop) < treshold) {
                        this.loadNextHistoryPart();
                    }
                });
                window.Wiser.createHistoryDiffFields(document.getElementById("historyContainer"));
            } catch (exception) {
                kendo.alert("Er is iets fout gegaan met het laden van de historie. Probeer het a.u.b. opnieuw of neem contact op met ons.");
                console.error(exception);
            }

            window.processing.removeProcess(process);
        }

        async loadNextHistoryPart() {
            if (this.loadingNextPart || this.allHistoryPartsLoaded || this.lastLoadedHistoryPartNumber < 1) {
                return;
            }

            this.loadingNextPart = true;

            const process = `loadHistoryTabNextPart_${Date.now()}`;
            window.processing.addProcess(process);

            try {
                const templateHistory = await Wiser.api({
                    url: `${this.settings.wiserApiRoot}templates/${this.selectedId}/history?pageNumber=${this.lastLoadedHistoryPartNumber + 1}`,
                    dataType: "json",
                    method: "GET"
                });

                if (templateHistory.templateHistory.length === 0) {
                    this.allHistoryPartsLoaded = true;
                    this.loadingNextPart = false;
                    window.processing.removeProcess(process);
                    return;
                }

                const historyTabPart = await Wiser.api({
                    method: "POST",
                    contentType: "application/json",
                    url: "/Modules/Templates/HistoryTabRows",
                    data: JSON.stringify(templateHistory)
                });

                document.getElementById("historyContainer").insertAdjacentHTML("beforeend", historyTabPart);
                window.Wiser.createHistoryDiffFields(document.getElementById("historyContainer"));
                this.lastLoadedHistoryPartNumber++;
            } catch (exception) {
                kendo.alert("Er is iets fout gegaan met het laden van de historie. Probeer het a.u.b. opnieuw of neem contact op met ons.");
                console.error(exception);
            }

            window.processing.removeProcess(process);
            this.loadingNextPart = false;
        }

        /**
         * Reloads measurements of the template.
         * @param {any} templateId The ID of the template.
         */
        async reloadMeasurementsTab(templateId) {
            if (this.measurementsLoaded) {
                return;
            }

            templateId = templateId || this.selectedId;
            this.measurementsLoaded = true;

            const process = `reloadMeasurementsTab_${Date.now()}`;
            window.processing.addProcess(process);

            try {
                // Get the measurement settings.
                const measurementSettings = await Wiser.api({
                    url: `${this.settings.wiserApiRoot}templates/${templateId}/measurement-settings`,
                    dataType: "json",
                    method: "GET"
                });

                const measurementsTab = await Wiser.api({
                    method: "POST",
                    contentType: "application/json",
                    url: "/Modules/Templates/MeasurementsTab",
                    data: JSON.stringify(measurementSettings)
                });

                document.getElementById("measurementsTab").innerHTML = measurementsTab;

                // Initialize save button for settings.
                $("#saveMeasuringSettingsButton").kendoButton({
                    icon: "save",
                    click: (event) => {
                        event.preventDefault();
                        this.saveMeasurementSettings(templateId);
                    }
                });

                // Initialize the grid with rendering logs.
                this.renderLogsGrid = $("#renderLogsGrid").kendoGrid({
                    dataSource: {
                        schema: {
                            model: {
                                fields: {
                                    id: { type: "number"},
                                    version: { type: "number" },
                                    url: { type: "string" },
                                    environment: { type: "string" },
                                    start: { type: "datetime" },
                                    end: { type: "datetime" },
                                    timeTaken: { type: "string" },
                                    userId: { type: "number" },
                                    languageCode: { type: "string" },
                                    error: { type: "string" }
                                }
                            }
                        }
                    },
                    noRecords: {
                        template: "Er zijn geen logs gevonden met de opgegeven filters."
                    },
                    height: 400,
                    scrollable: true,
                    resizable: true,
                    selectable: false,
                    filterable: false,
                    sortable: false,
                    pageable: false,
                    columns: [
                        {
                            field: "name",
                            title: "Template of component",
                            filterable: true
                        },
                        {
                            field: "environment",
                            title: "Omgeving",
                            width: 150,
                            filterable: true
                        },
                        {
                            field: "languageCode",
                            title: "Taal",
                            width: 100,
                            filterable: true
                        },
                        {
                            field: "userId",
                            title: "Gebruiker",
                            width: 100,
                            filterable: true
                        },
                        {
                            field: "start",
                            title: "Datum",
                            width: 150,
                            template: "#= kendo.toString(kendo.parseDate(start), 'dd MMM \\'yy') #",
                            filterable: {
                                ui: "datepicker"
                            }
                        },
                        {
                            field: "timeTakenFormatted",
                            title: "Gemeten tijd",
                            width: 150,
                            filterable: false
                        },
                        {
                            field: "url",
                            title: "Url",
                            filterable: true
                        },
                        {
                            field: "version",
                            title: "Versie",
                            width: 100,
                            filterable: true
                        },
                        {
                            field: "error",
                            title: "Gelukt",
                            width: 150,
                            filterable: false,
                            template: `# if (!error) { # Ja # } else { # Nee # } #`
                        }
                    ]
                }).data("kendoGrid");

                this.renderingLogsChart = $("#measurementCharts").kendoChart({
                    title: {
                        text: "Rendertijden"
                    },
                    legend: {
                        position: "top"
                    },
                    seriesDefaults: {
                        type: "line"
                    },
                    series: [{
                        field: "timeTakenInSeconds",
                        categoryField: "date",
                        name: "#= group.value #",
                        aggregate: "avg"
                    }],
                    categoryAxis: {
                        type: "date",
                        baseUnit: "days",
                        baseUnitStep: 1,
                        labels: {
                            rotation: "auto",
                            dateFormats: {
                                days: "dd-MM"
                            }
                        }
                    },
                    valueAxis: {
                        labels: {
                            format: "N3"
                        },
                        majorUnit: 1
                    },
                    tooltip: {
                        visible: true,
                        shared: true,
                        format: "N3"
                    }
                }).data("kendoChart");

                this.measurementUserIdFilter = $("#measurementUserIdFilter").kendoNumericTextBox({
                    decimals: 0,
                    format: "#",
                    change: this.updateRenderingDataOnMeasurementsTab.bind(this, templateId)
                }).data("kendoNumericTextBox");

                this.measurementEnvironmentFilter = $("#measurementEnvironmentFilter").kendoDropDownList({
                    optionLabel: "Alle omgevingen",
                    change: this.updateRenderingDataOnMeasurementsTab.bind(this, templateId)
                }).data("kendoDropDownList");
                this.measurementEnvironmentFilter.value("Live");

                const languages = await Wiser.api({
                    url: `${this.settings.wiserApiRoot}languages`,
                    dataType: "json",
                    method: "GET"
                });
                this.measurementLanguageCodeFilter = $("#measurementLanguageCodeFilter").kendoDropDownList({
                    dataSource: languages,
                    optionLabel: "Alle talen",
                    dataValueField: "code",
                    dataTextField: "name",
                    change: this.updateRenderingDataOnMeasurementsTab.bind(this, templateId)
                }).data("kendoDropDownList");
                this.measurementUrlFilter = $("#measurementUrlFilter").change(this.updateRenderingDataOnMeasurementsTab.bind(this, templateId));

                const currentDate = new Date();
                const start = new Date(currentDate.getFullYear(), currentDate.getMonth(), currentDate.getDate() - 7);
                const end = new Date(currentDate.getFullYear(), currentDate.getMonth(), currentDate.getDate() + 1);
                this.measurementChartDateRangeFilter = $("#measurementChartDateRangeFilter").kendoDateRangePicker({
                    range: {
                        start: start,
                        end: end
                    },
                    change: (event) => {
                        const dateRange = this.measurementChartDateRangeFilter.range();
                        if (!dateRange || !dateRange.start || !dateRange.end) {
                            return;
                        }
                        this.updateRenderingDataOnMeasurementsTab(templateId);
                    }
                }).data("kendoDateRangePicker");

                await this.updateRenderingDataOnMeasurementsTab(templateId);
                this.renderingLogsChart.resize();
            } catch (exception) {
                kendo.alert("Er is iets fout gegaan met het laden van de metingen. Probeer het a.u.b. opnieuw of neem contact op met ons.");
                console.error(exception);
            }

            window.processing.removeProcess(process);
        }

        /**
         * Save the current measurement settings to database.
         * @param templateId The ID of the template to save the settings for.
         * @returns {Promise<void>}
         */
        async saveMeasurementSettings(templateId) {
            const saveProcess = `saveMeasurementSettings_${Date.now()}`;
            window.processing.addProcess(saveProcess);

            try {
                await Wiser.api({
                    url: `${this.settings.wiserApiRoot}templates/${templateId}/measurement-settings`,
                    dataType: "json",
                    method: "PUT",
                    contentType: "application/json",
                    data: JSON.stringify({
                        measureRenderTimesOnDevelopmentForCurrent: document.querySelector("#measureInDevelopment").checked,
                        measureRenderTimesOnTestForCurrent: document.querySelector("#measureInTest").checked,
                        measureRenderTimesOnAcceptanceForCurrent: document.querySelector("#measureInAcceptance").checked,
                        measureRenderTimesOnLiveForCurrent: document.querySelector("#measureInLive").checked,
                    })
                });

                window.popupNotification.show(`Instellingen succesvol opgslagen`, "info");
            }
            catch (exception) {
                console.error(error);
                kendo.alert("Er is iets fout gegaan met het opslaan van de instellingen. Probeer het a.u.b. opnieuw of neem contact op met ons.");
            }
            finally {
                window.processing.removeProcess(saveProcess);
            }
        }

        /**
         * This method will update the grid and chart on the measurements tabs with the latest data and using the values that the user entered in the filters.
         * @returns {Promise<void>}
         */
        async updateRenderingDataOnMeasurementsTab(templateId) {
            const process = `updateRenderingData_${Date.now()}`;
            window.processing.addProcess(process);

            try {
                const parametersForGrid = ["pageSize=500"];
                const parametersForChart = [
                    "getDailyAverage=true",
                    "pageSize=0"
                ];

                const userId = this.measurementUserIdFilter.value();
                const languageCode = this.measurementLanguageCodeFilter.value();
                const environment = this.measurementEnvironmentFilter.value();
                const urlRegex = this.measurementUrlFilter.val();
                const dateRange = this.measurementChartDateRangeFilter.range();

                parametersForChart.push(`start=${dateRange.start.toISOString()}`)
                parametersForChart.push(`end=${dateRange.end.toISOString()}`)

                if (userId) {
                    parametersForChart.push(`userId=${userId}`)
                    parametersForGrid.push(`userId=${userId}`)
                }
                if (languageCode) {
                    parametersForChart.push(`languageCode=${encodeURIComponent(languageCode)}`)
                    parametersForGrid.push(`languageCode=${encodeURIComponent(languageCode)}`)
                }
                if (environment) {
                    parametersForChart.push(`environment=${encodeURIComponent(environment)}`)
                    parametersForGrid.push(`environment=${encodeURIComponent(environment)}`)
                }
                if (urlRegex) {
                    parametersForChart.push(`urlRegex=${encodeURIComponent(urlRegex)}`)
                    parametersForGrid.push(`urlRegex=${encodeURIComponent(urlRegex)}`)
                }

                const promises = [];
                promises.push(Wiser.api({
                    url: `${this.settings.wiserApiRoot}templates/${templateId}/render-logs?${parametersForChart.join("&")}`,
                    dataType: "json",
                    method: "GET"
                }));
                promises.push(Wiser.api({
                    url: `${this.settings.wiserApiRoot}templates/${templateId}/render-logs?${parametersForGrid.join("&")}`,
                    dataType: "json",
                    method: "GET"
                }));

                const promiseResults = await Promise.all(promises);
                const chartDataSource = new kendo.data.DataSource({
                    data: promiseResults[0],
                    group: {
                        field: "name"
                    },
                    schema: {
                        model: {
                            fields: {
                                date: {
                                    type: "date"
                                },
                                start: {
                                    type: "date"
                                },
                                end: {
                                    type: "date"
                                }
                            }
                        }
                    }
                });
                this.renderingLogsChart.setDataSource(chartDataSource);
                this.renderLogsGrid.setDataSource(promiseResults[1]);
            }
            catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan met het ophalen van gegevens. Probeer het a.u.b. opnieuw of neem contact op met ons.")
            }
            finally {
                window.processing.removeProcess(process);
            }
        }

        /**
         * Creates a new template, adds it to the tree view and finally opens it.
         * @param {any} parentId The ID of the parent to add the template to.
         * @param {any} title The name of the template.
         * @param {any} type The template type.
         * @param {any} treeView The tree view that the template should be added to.
         * @param {any} parentElement The parent node in the tree view to add the parent to.
         * @param {string} body Optional: The body of the template.
         * @param {any} treeViewElement Optional: Which tree view element to update instead of adding a new one.
         */
        async createNewTemplate(parentId, title, type, treeView, parentElement, body = "", treeViewElement = null) {
            const process = `createNewTemplate_${Date.now()}`;
            window.processing.addProcess(process);

            let result = null;
            let success = true;
            try {
                result = await Wiser.api({
                    url: `${this.settings.wiserApiRoot}templates/${parentId}`,
                    dataType: "json",
                    type: "PUT",
                    contentType: "application/json",
                    data: JSON.stringify({ name: title, type: type, editorvalue: body })
                });

                if (treeViewElement) {
                    const dataItem = treeView.dataItem(treeViewElement);
                    if (dataItem) {
                        dataItem.set("id", result.templateId);
                        dataItem.set("templateId", result.templateId);
                        dataItem.set("isVirtualItem", false);
                    }
                } else {
                    const dataItem = treeView.dataItem(parentElement);
                    if (dataItem && dataItem.hasChildren && parentElement.attr("aria-expanded") !== "true") {
                        treeView.one("dataBound", () => {
                            const node = treeView.findByUid(treeView.dataSource.get(result.templateId).uid);
                            treeView.select(node);
                            treeView.trigger("select", {
                                node: node
                            });
                        });
                        treeView.expand(parentElement);
                    } else {
                        const newTreeViewElement = parentElement && parentElement.length > 0 ? treeView.append(result, parentElement) : treeView.append(result);
                        treeView.select(newTreeViewElement);
                        treeView.trigger("select", {
                            node: newTreeViewElement
                        });
                    }
                }
            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan. Probeer het a.u.b. opnieuw of neem contact op.");
                success = false;
            }

            window.processing.removeProcess(process);
            return {
                success: success,
                result: result
            };
        }

        /**
         * Retrieve the new values entered by the user.
         * */
        getNewSettings() {
            const settingsList = {};

            const preLoadQueryField = $("#preLoadQuery");
            if (preLoadQueryField.length > 0 && preLoadQueryField.data("CodeMirrorInstance")) {
                settingsList.preLoadQuery = preLoadQueryField.data("CodeMirrorInstance").getValue();
            }

            const widgetContentField = $("#widgetContent");
            if (widgetContentField.length > 0 && widgetContentField.data("CodeMirrorInstance")) {
                settingsList.widgetContent = widgetContentField.data("CodeMirrorInstance").getValue();
            }

            $(".advanced input, .advanced select").each((index, element) => {
                const field = $(element);
                const propertyName = field.attr("name");
                if (!propertyName) {
                    return;
                }

                const kendoControlName = field.data("kendoControl");

                if (kendoControlName) {
                    const kendoControl = field.data(kendoControlName);

                    if (kendoControl) {
                        settingsList[propertyName] = kendoControl.value();
                        return;
                    } else {
                        console.warn(`Kendo control found for '${propertyName}', but it's not initialized, so skipping this property.`, kendoControlName, data);
                        return;
                    }
                }

                // If we reach this point in the code, this element is not a Kendo control, so just get the normal value.
                switch (field.prop("tagName")) {
                    case "SELECT":
                        settingsList[propertyName] = field.val();
                        break;
                    case "INPUT":
                    case "TEXTAREA":
                        switch ((field.attr("type") || "").toUpperCase()) {
                            case "CHECKBOX":
                                settingsList[propertyName] = field.prop("checked");
                                break;
                            default:
                                settingsList[propertyName] = field.val();
                                break;
                        }
                        break;
                    default:
                        console.error("TODO: Unsupported tag name:", field.prop("tagName"));
                        return;
                }
            });

            return settingsList;
        }

        canUnloadTemplate() {
            if (!this.initialTemplateSettings) {
                return true;
            }

            const initialData = JSON.stringify(this.initialTemplateSettings);
            const currentData = JSON.stringify(this.getCurrentTemplateSettings());
            return initialData === currentData;
        }

        /**
         * The "always load" checkbox and the "URL regex" input cannot be used at the same time.
         * If one input is used, the other is disabled.
         */
        updateAlwaysLoadAndUrlRegexAvailability() {
            const alwaysLoadCheckbox = document.getElementById("loadAlways");
            const urlRegexInput = document.getElementById("urlRegex");
            if (!alwaysLoadCheckbox || !urlRegexInput) {
                return;
            }

            // URL regex input is disabled if the "always load" checkbox is checked.
            urlRegexInput.disabled = alwaysLoadCheckbox.checked;
            urlRegexInput.readOnly = alwaysLoadCheckbox.checked;

            // The "always load" checkbox is disabled if the "URL regex" input has a value.
            const urlRegexHasValue = urlRegexInput.value !== "";
            alwaysLoadCheckbox.disabled = urlRegexHasValue;
            alwaysLoadCheckbox.readOnly = urlRegexHasValue;
        }

        /**
         * Imports the templates from the Wiser 1 templates modules into the Wiser 3 templates module.
         */
        async importLegacyTemplates() {
            await kendo.confirm("Weet u zeker dat u de templatemodule van Wiser 1 wilt importeren? Dit heeft het meeste nut wanneer de klant al Wiser 2/3 databasestructuur gebruikt. Als de klant nog Wiser 1 gebruikt, dan moeten alle query's toch opnieuw geschreven worden en is het misschien efficiënter om handmatig de templatemodule te vullen.<br><br>De tabellen 'wiser_template' en 'wiser_dynamic_content' moeten leeg zijn voordat u dit doet.");

            const process = `importLegacyTemplates_${Date.now()}`;
            window.processing.addProcess(process);

            try {
                const response = await Wiser.api({
                    url: `${this.settings.wiserApiRoot}templates/import-legacy`,
                    dataType: "json",
                    type: "POST",
                    contentType: "application/json"
                });

                await this.loadTabsAndTreeViews();

                window.popupNotification.show(`Templates van Wiser 1 zijn succesvol geïmporteerd.`, "info");
            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan, probeer het a.u.b. opnieuw of neem contact op.");
            }

            window.processing.removeProcess(process);
        }
    }



    // Initialize the DynamicItems class and make one instance of it globally available.
    window.Templates = new Templates(settings);
})(moduleSettings);
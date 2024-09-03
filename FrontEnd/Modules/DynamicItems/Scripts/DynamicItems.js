import {TrackJS} from "trackjs";
import {Dates, Modules, Wiser} from "../../Base/Scripts/Utils.js";
import "../../Base/Scripts/Processing.js";
import {DateTime} from "luxon";
import {Fields} from "./Fields.js";
import {Dialogs} from "./Dialogs.js";
import {Windows} from "./Windows.js";
import {Grids} from "./Grids.js";
import {DragAndDrop} from "./DragAndDrop.js";
import "../Css/DynamicItems.css";

window.JSZip = require("jszip");

require("@progress/kendo-ui/js/kendo.notification.js");
require("@progress/kendo-ui/js/kendo.tooltip.js");
require("@progress/kendo-ui/js/kendo.button.js");
require("@progress/kendo-ui/js/kendo.dialog.js");
require("@progress/kendo-ui/js/kendo.grid.js");
require("@progress/kendo-ui/js/kendo.tabstrip.js");
require("@progress/kendo-ui/js/kendo.sortable.js");
require("@progress/kendo-ui/js/kendo.validator.js");
require("@progress/kendo-ui/js/kendo.splitter.js");
require("@progress/kendo-ui/js/kendo.treeview.js");
require("@progress/kendo-ui/js/kendo.notification.js");
require("@progress/kendo-ui/js/cultures/kendo.culture.nl-NL.js");
require("@progress/kendo-ui/js/messages/kendo.messages.nl-NL.js");

// Any custom settings can be added here. They will overwrite most default settings inside the module.
const moduleSettings = {

};

((settings) => {
    /**
     * Main class.
     */
    class DynamicItems {

        /**
         * Initializes a new instance of DynamicItems.
         * @param {any} settings An object containing the settings for this class.
         */
        constructor(settings) {
            this.base = this;

            // Sub classes.
            this.dragAndDrop = null;
            this.dialogs = null;
            this.windows = null;
            this.grids = null;
            this.fields = null;

            // Kendo components.
            this.notification = null;
            this.mainSplitter = null;
            this.mainTreeView = null;
            this.mainTreeViewContextMenu = null;
            this.mainTabStrip = null;
            this.mainTabStripSortable = null;
            this.mainValidator = null;

            // Other.
            this.mainLoader = null;
            this.newItemId = null;
            this.selectedItem = null;
            this.selectedItemTitle = null;
            this.allEntityTypes = [];
            this.allLanguages = [];

            // Set the Kendo culture to Dutch. TODO: Base this on the language in Wiser.
            kendo.culture("nl-NL");

            // Flags to use in wiser_field_templates, so that we can add code there that depends on code in this file, without having to deploy this to live right away.
            this.fieldTemplateFlags = {
                enableSubEntitiesGridsOrdering: true
            };

            // Default settings
            this.settings = {
                moduleId: 0,
                encryptedModuleId: "",
                tenantId: 0,
                initialItemId: null,
                iframeMode: false,
                gridViewMode: false,
                openGridItemsInBlock: false,
                username: "Onbekend",
                userEmailAddress: "",
                userType: ""
            };
            Object.assign(this.settings, settings);

            // Enumerations.
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

            // Create instances of sub classes.
            this.dragAndDrop = new DragAndDrop(this);
            this.dialogs = new Dialogs(this);
            this.windows = new Windows(this);
            this.grids = new Grids(this);
            this.fields = new Fields(this);

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

            // Get user data from local storage.
            const user = JSON.parse(localStorage.getItem("userData"));
            this.settings.oldStyleUserId = user.oldStyleUserId;
            this.settings.username = user.adminAccountName ? `${user.adminAccountName} (Admin)` : user.name;
            this.settings.adminAccountLoggedIn = !!user.adminAccountName;

            if (!this.settings.wiserApiRoot.endsWith("/")) {
                this.settings.wiserApiRoot += "/";
            }

            // Get user data from API.
            const userData = await Wiser.getLoggedInUserData(this.settings.wiserApiRoot);
            this.settings.userId = userData.encryptedId;
            this.settings.tenantId = userData.encryptedTenantId;
            this.settings.zeroEncrypted = userData.zeroEncrypted;
            this.settings.filesRootId = userData.filesRootId;
            this.settings.imagesRootId = userData.imagesRootId;
            this.settings.templatesRootId = userData.templatesRootId;
            this.settings.mainDomain = userData.mainDomain;

            this.settings.serviceRoot = `${this.settings.wiserApiRoot}templates/get-and-execute-query`;
            this.settings.htmlEditorCssUrl = `${this.settings.wiserApiRoot}templates/css-for-html-editors?encryptedUserId=${encodeURIComponent(this.base.settings.userId)}&subDomain=${encodeURIComponent(this.base.settings.subDomain)}`

            // Get list of all entity types, so we can show friendly names wherever we need to and don't have to get them from database via different places.
            try {
                this.allEntityTypes = (await Wiser.api({url: `${this.settings.wiserApiRoot}entity-types?onlyEntityTypesWithDisplayName=false`})) || [];
            } catch (exception) {
                console.error("Error occurred while trying to load all entity types", exception);
                this.allEntityTypes = [];
            }

            // Get list of all languages, we need this later for the option for translating items.
            try {
                this.allLanguages = (await Wiser.api({url: `${this.settings.wiserApiRoot}languages`})) || [];
            } catch (exception) {
                console.error("Error occurred while trying to load all languages", exception);
                this.allLanguages = [];
            }

            // Get extra module settings.
            if (this.settings.moduleId > 0) {
                const extraModuleSettings = await Modules.getModuleSettings(this.settings.wiserApiRoot, this.settings.moduleId);
                Object.assign(this.settings, extraModuleSettings.options);
                let permissions = Object.assign({}, extraModuleSettings);
                delete permissions.options;
                this.settings.permissions = permissions;
            } else {
                this.settings.permissions = {};
            }

            this.settings.getItemsUrl = `${this.settings.wiserApiRoot}data-selectors`;
            $("body").toggleClass("gridViewMode", this.settings.gridViewMode);

            this.setupBindings();
            this.initializeKendoComponents();

            // Initialize sub classes.
            this.dragAndDrop.initialize();
            await this.dialogs.initialize();
            this.windows.initialize();
            this.grids.initialize();
            this.fields.initialize();

            if (this.settings.createNewItem && this.settings.entityType) {
                const process = `createNewItem_${Date.now()}`;
                window.processing.addProcess(process);
                const newItemResult = await this.createItem(this.settings.entityType, this.settings.parentId || this.settings.zeroEncrypted, "", 1, this.settings.newItemData || [], false, this.settings.moduleId);
                this.settings.initialItemId = newItemResult.itemId;
                await this.loadItem(newItemResult.itemId, 0, newItemResult.entityType);
                window.processing.removeProcess(process);
            } else if (this.settings.initialItemId) {
                await this.loadItem(this.settings.initialItemId, 0, this.settings.entityType);
            }

            if (this.settings.iframeMode && this.settings.hideHeader) {
                $("#tabstrip > ul").addClass("hidden");
            }
            if (this.settings.iframeMode && this.settings.hideFooter) {
                $("#right-pane > footer").addClass("hidden");
            }
        }

        /**
         * Setup all basis bindings for this module.
         * Specific bindings (for buttons in certain pop-ups for example) will be set when they are needed.
         */
        setupBindings() {
            // Do stuff when the module is being closed in Wiser.
            document.addEventListener("moduleClosing", async (event) => {
                try {
                    const kendoWindows = $(".popup-container:not(#itemWindow_template)");
                    if (!kendoWindows.length) {
                        event.detail();
                        return;
                    }

                    var promises = [];
                    for (let element of kendoWindows) {
                        // If the current item is a new item and it's not being saved at the moment, then delete it because it was a temporary item.
                        if (!$(element).data("isNewItem") || $(element).data("saving")) {
                            continue;
                        }

                        let canDelete = true;
                        for (let gridElement of $(element).find(".grid")) {
                            const kendoGrid = $(gridElement).data("kendoGrid");
                            if (!kendoGrid) {
                                continue;
                            }

                            // Don't delete this item if someone added something in one of the grids on the item.
                            if (kendoGrid.dataSource.data().length > 0) {
                                canDelete = false;
                            }
                        }

                        if (canDelete) {
                            promises.push(this.base.deleteItem($(element).data("itemId"), $(element).data("entityType")));
                        }
                    }

                    await Promise.all(promises);
                    event.detail();
                } catch (exception) {
                    console.error(exception);
                    // To make sure the module can always be closed.
                    event.detail();
                }
            });

            // Keyboard shortcuts
            $("body").on("keydown", async (event) => {
                const target = $(event.target);

                if ((event.ctrlKey || event.metaKey) && event.key.toUpperCase() === "S") {
                    event.preventDefault();

                    const entityContainer = target.closest(".entity-container");
                    if (entityContainer.length > 0) {
                        entityContainer.find(".saveButton").first().click();
                    }
                }
            });

            $("body").on("keyup", async (event) => {
                const target = $(event.target);

                if (target.prop("tagName") === "INPUT" || target.prop("tagName") === "TEXTAREA" || !this.mainTreeView) {
                    return;
                }

                const selectedItem = this.mainTreeView.select();
                switch (event.key.toUpperCase()) {
                    case "N": {
                        const addButton = $("#addButton");
                        if (event.shiftKey && addButton.is(":visible")) {
                            addButton.click();
                        }
                        break;
                    }
                    case "F2": {
                        if (!selectedItem.length) {
                            break;
                        }

                        await this.handleContextMenuAction(selectedItem, "RENAME_ITEM");
                        break;
                    }
                    case "D": {
                        if (!selectedItem.length || !event.shiftKey) {
                            break;
                        }

                        await this.handleContextMenuAction(selectedItem, "DUPLICATE_ITEM");
                        break;
                    }
                    case "DELETE": {
                        if (!selectedItem.length) {
                            break;
                        }

                        await this.handleContextMenuAction(selectedItem, "REMOVE_ITEM");
                        break;
                    }
                }
            });

            // Binding to unselect the main tree view.
            $("body").on("click", "#left-pane, .main-window .k-window-titlebar", async (event) => {
                const target = $(event.target);

                if (target.closest(".k-window-titlebar").length === 0 && (target.hasClass("k-treeview-leaf") || target.hasClass("k-treeview-leaf-text") || target.hasClass("k-i-expand") || target.hasClass("k-treeview-toggle") || target.prop("tagName") === "BUTTON" || target.prop("tagName") === "INPUT")) {
                    return;
                }

                if (!this.selectedItem) {
                    return;
                }

                this.mainTreeView.select($());
                this.selectedItem = null;
                $("#right-pane-content").html("");
                this.mainTabStrip.element.find("> ul > li .addedFromDatabase").each((index, element) => {
                    this.mainTabStrip.remove($(element).closest("li.k-item"));
                });

                this.mainTabStrip.select(0);

                await this.dialogs.loadAvailableEntityTypesInDropDown(this.settings.zeroEncrypted);

                if (!this.settings.initialItemId) {
                    $("#alert-first").removeClass("hidden");
                } else {
                    await this.loadItem(this.settings.initialItemId, 0, this.settings.entityType);
                }
            });

            // Close first alert.
            $(".alert-close").click((event) => {
                var target = $(event.target);

                target.closest(".alert-overlay").hide();
            });

            // Close panel
            $(".close-panel").click((event) => {
                var target = $(event.target);

                target.closest(".k-window").find(".entity-container").removeClass("info-active");
                target.closest(".k-window").find("#right-pane").removeClass("info-active");
            });

            $("body").on("click", ".imgZoom", (event) => {
                const image = $(event.currentTarget).parents(".product").find("img");
                const dialogElement = $("#imageDialog");
                let dialog = dialogElement.data("kendoDialog");
                if (!dialog) {
                    dialog = dialogElement.kendoDialog({
                        title: "Afbeelding",
                        closable: true,
                        modal: false,
                        resizable: true
                    }).data("kendoDialog");
                }

                dialog.content(image.clone());
                dialog.open();
            });

            $("body").on("click", ".imgTools .imgEdit", this.fields.onImageEdit.bind(this.fields));

            $("body").on("click", ".imgTools .imgDelete", this.fields.onImageDelete.bind(this.fields));

            $("#mainScreenForm").submit(event => { event.preventDefault(); });

            $("#mainEditMenu .reloadItem").click(async (event) => {
                const previouslySelectedTab = this.mainTabStrip.select().index();
                await this.loadItem(this.selectedItem && this.selectedItem.plainItemId ? this.selectedItem.id : this.settings.initialItemId, previouslySelectedTab, this.selectedItem && this.selectedItem.plainItemId ? this.selectedItem.entityType : this.settings.entityType);
            });

            $("#mainEditMenu .deleteItem").click(async (event) => {
                await this.onDeleteItemClick(event, this.selectedItem && this.selectedItem.plainItemId ? this.selectedItem.id : this.settings.initialItemId, this.selectedItem && this.selectedItem.plainItemId ? this.selectedItem.entityType : this.settings.entityType);
            });

            $("#mainEditMenu .undeleteItem").click(async (event) => {
                await this.onUndeleteItemClick(event, this.selectedItem && this.selectedItem.plainItemId ? this.selectedItem.id : this.settings.initialItemId);
            });

            $("#mainEditMenu .copyToEnvironment").click(async (event) => {
                this.dialogs.copyItemToEnvironmentDialog.element.find("input[type=checkbox]").prop("checked", false);
                this.dialogs.copyItemToEnvironmentDialog.element.data("id", this.selectedItemMetaData.plainOriginalItemId);
                this.dialogs.copyItemToEnvironmentDialog.open();
            });

            $("#mainEditMenu .translateItem").click(async (event) => {
                await this.onTranslateItemClick(event, this.selectedItem && this.selectedItem.plainItemId ? this.selectedItem.id : this.settings.initialItemId, this.selectedItem && this.selectedItem.plainItemId ? this.selectedItem.entityType : this.settings.entityType);
            });
        }

        /**
         * Initializes all kendo components for the base class.
         */
        initializeKendoComponents() {
            // Buttons.
            $("#saveButton, #saveBottom").kendoButton({
                click: this.onSaveButtonClick.bind(this),
                icon: "save"
            });

            if (!this.settings.iframeMode) {
                $("#addButton").kendoButton({
                    click: this.dialogs.openCreateItemDialog.bind(this.dialogs),
                    icon: "plus"
                });
            }

            // Normal notifications.
            this.notification = $("#alert").kendoNotification({
                button: true,
                autoHideAfter: 5000,
                stacking: "up",
                position: {
                    top: null,
                    left: null,
                    right: 15,
                    bottom: 120,
                    pinned: true
                },
                show: this.onShowNotification,
                templates: [{
                    type: "error",
                    template: $("#errorTemplate").html()
                }, {
                    type: "success",
                    template: $("#successTemplate").html()
                }]
            }).data("kendoNotification");

            // Main tab strip.
            this.mainTabStrip = $("#tabstrip").kendoTabStrip({
                scrollable: {
                    distance: 50
                },
                animation: {
                    open: {
                        effects: "fadeIn"
                    }
                },
                select: (event) => { this.onTabStripSelect((!this.selectedItem || !this.selectedItem.id ? 0 : this.selectedItem.id), "mainScreen", event); }
            }).data("kendoTabStrip");
            this.mainTabStrip.select(0);
            this.mainTabStrip.element.find(".overview-tab").toggleClass("hidden", this.settings.iframeMode);

            this.mainTabStripSortable = $("#tabstrip ul.k-tabstrip-items").kendoSortable({
                filter: "li.k-item",
                axis: "x",
                container: "ul.k-tabstrip-items",
                hint: (element) => {
                    return $(`<div id='hint' class='k-widget k-header k-tabstrip'><ul class='k-tabstrip-items k-reset'><li class='k-item k-state-active k-tab-on-top'>${element.html()}</li></ul></div>`);
                },
                start: (event) => {
                    this.mainTabStrip.activateTab(event.item);
                },
                change: (event) => {
                    const reference = this.mainTabStrip.tabGroup.children().eq(event.newIndex);

                    if (event.oldIndex < event.newIndex) {
                        this.mainTabStrip.insertAfter(event.item, reference);
                    } else {
                        this.mainTabStrip.insertBefore(event.item, reference);
                    }
                }
            }).data("kendoSortable");

            // Validator.
            this.mainValidator = $("#right-pane").kendoValidator({
                validate: this.onValidateForm.bind(this, this.mainTabStrip),
                validateOnBlur: false,
                rules: {
                    required: (input) => {
                        if (input.prop("required") && !input.closest(".item").hasClass("dependency-hidden")) {
                            return $.trim(input.val()) !== "";
                        }

                        return true;
                    }
                },
                messages: {
                    required: (input) => {
                        const fieldDisplayName = $(input).closest(".item").find("> h4 > label").text() || $(input).attr("name");
                        return `${fieldDisplayName} is verplicht`;
                    },
                    pattern: (input) => {
                        const fieldDisplayName = $(input).closest(".item").find("> h4 > label").text() || $(input).attr("name");
                        return `${fieldDisplayName} is niet correct`;
                    },
                    step: (input) => {
                        const fieldDisplayName = $(input).closest(".item").find("> h4 > label").text() || $(input).attr("name");
                        return `${fieldDisplayName} is niet correct`;
                    }
                }
            }).data("kendoValidator");

            // Some things should not be done if we're in iframe mode.
            if (this.settings.iframeMode || this.settings.gridViewMode) {
                return;
            }

            /***** NOTE: Only add code below this line that should NOT be executed if the module is loaded inside an iframe *****/
            // Splitter.
            this.mainSplitter = $("#horizontal").kendoSplitter({
                panes: [{
                    collapsible: true,
                    size: "20%"
                }, {
                    collapsible: false
                }]
            }).data("kendoSplitter");
            this.mainSplitter.resize(true);

            // Main tree view.
            this.mainTreeView = $("#treeview").kendoTreeView({
                dragAndDrop: true,
                dataSource: {
                    transport: {
                        read: (options) => {
                            Wiser.api({
                                url: `${this.base.settings.wiserApiRoot}items/tree-view?moduleId=${this.base.settings.moduleId}`,
                                dataType: "json",
                                method: "GET",
                                data: options.data
                            }).then((result) => {
                                options.success(result);
                            }).catch((result) => {
                                options.error(result);
                            });
                        }
                    },
                    schema: {
                        model: {
                            id: "encryptedItemId",
                            hasChildren: "hasChildren"
                        }
                    }
                },
                dataBound: this.onTreeViewDataBound.bind(this),
                select: this.onTreeViewItemClick.bind(this),
                collapse: this.onTreeViewCollapseItem.bind(this),
                expand: this.onTreeViewExpandItem.bind(this),
                drop: this.onTreeViewDropItem.bind(this),
                drag: this.onTreeViewDragItem.bind(this),
                dataValueField: "encryptedItemId",
                dataTextField: "title",
                dataSpriteCssClassField: "spriteCssClass"
            }).data("kendoTreeView");

            this.mainTreeViewContextMenu = $("#menu").kendoContextMenu({
                target: "#treeview",
                filter: ".k-in",
                open: this.onContextMenuOpen.bind(this),
                select: this.onContextMenuClick.bind(this)
            }).data("kendoContextMenu");
        }

        /**
         * Load all Kendo scripts of components that are used in a script template that will be used in an eval() function.
         * @param {string} scriptTemplate The script template that is going to be loaded in an eval().
         */
        async loadKendoScripts(scriptTemplate) {
            if (scriptTemplate.indexOf("kendoDateTimePicker") > -1) {
                await require("@progress/kendo-ui/js/kendo.datetimepicker.js");
                await require("/kendo/messages/kendo.upload.nl-NL.js");
            }
            if (scriptTemplate.indexOf("kendoDatePicker") > -1) {
                await require("@progress/kendo-ui/js/kendo.datepicker.js");
            }
            if (scriptTemplate.indexOf("kendoTimePicker") > -1) {
                await require("@progress/kendo-ui/js/kendo.timepicker.js");
            }
            if (scriptTemplate.indexOf("kendoChart") > -1) {
                await require("@progress/kendo-ui/js/dataviz/chart/chart.js");
            }
            if (scriptTemplate.indexOf("kendoColorPicker") > -1) {
                await require("@progress/kendo-ui/js/kendo.colorpicker.js");
            }
            if (scriptTemplate.indexOf("kendoDropDownList") > -1) {
                await require("@progress/kendo-ui/js/kendo.dropdownlist.js");
            }
            if (scriptTemplate.indexOf("kendoComboBox") > -1) {
                await require("@progress/kendo-ui/js/kendo.combobox.js");
            }
            if (scriptTemplate.indexOf("kendoUpload") > -1) {
                await require("@progress/kendo-ui/js/kendo.upload.js");
                await require("/kendo/messages/kendo.upload.nl-NL.js");
            }
            if (scriptTemplate.indexOf("kendoEditor") > -1) {
                await require("@progress/kendo-ui/js/kendo.editor.js");
                await require("/kendo/messages/kendo.editor.nl-NL.js");
            }
            if (scriptTemplate.indexOf("kendoNumericTextBox") > -1) {
                await require("@progress/kendo-ui/js/kendo.numerictextbox.js");
            }
            if (scriptTemplate.indexOf("kendoMultiSelect") > -1) {
                await require("@progress/kendo-ui/js/kendo.multiselect.js");
            }
            if (scriptTemplate.indexOf("kendoScheduler") > -1) {
                await require("@progress/kendo-ui/js/kendo.scheduler.js");
                await require("/kendo/messages/kendo.scheduler.nl-NL.js");
            }
            if (scriptTemplate.indexOf("kendoTimeline") > -1) {
                await require("@progress/kendo-ui/js/kendo.timeline.js");
            }
            if (scriptTemplate.indexOf("kendoGrid") > -1) {
                await require("@progress/kendo-ui/js/kendo.grid.js");
                await require("/kendo/messages/kendo.grid.nl-NL.js");
            }

            await require("@progress/kendo-ui/js/messages/kendo.messages.nl-NL.js");
        }

        /**
         * Shows or hides the main (full screen) loader.
         * @param {boolean} show True to show the loader, false to hide it.
         */
        toggleMainLoader(show) {
            this.mainLoader.toggleClass("loading", show);
        }

        /**
         * Parses a bitwise environments value to an array of text values.
         * @param {any} publishedEnvironment
         */
        parseEnvironments(publishedEnvironment) {
            const environmentLabel = [];
            if (publishedEnvironment === this.environmentsEnum.hidden) {
                environmentLabel.push("verborgen");
                return environmentLabel;
            }

            if ((publishedEnvironment & this.environmentsEnum.development) > 0) {
                environmentLabel.push("ontwikkeling");
            }
            if ((publishedEnvironment & this.environmentsEnum.test) > 0) {
                environmentLabel.push("test");
            }
            if ((publishedEnvironment & this.environmentsEnum.acceptance) > 0) {
                environmentLabel.push("acceptatie");
            }
            if ((publishedEnvironment & this.environmentsEnum.live) > 0) {
                environmentLabel.push("live");
            }
            return environmentLabel;
        }

        /**
         * Function for event of validator, it will open the tab sheet that contains the first field with an error.
         * @param {any} tabStrip The tab strip that we're validating.
         * @param {any} event The validate event.
         */
        onValidateForm(tabStrip, event) {
            if (event.valid) {
                return;
            }

            // Switch to the tab sheet that contains the first error.
            const fieldName = Object.keys(event.sender._errors)[0];
            const tabSheet = tabStrip.element.find(`[name='${fieldName}']`).closest("div[role=tabpanel]");
            tabStrip.select(tabSheet.index() - 1); // -1 because the first element is the <ul> with tab names, which we don't want to count.
        }

        /**
         * Event that gets fired when the users opens the context menu.
         * @param {any} event The context open event.
         */
        async onContextMenuOpen(event) {
            try {
                const nodeId = this.mainTreeView.dataItem(event.target).id;
                let contextMenu = await Wiser.api({ url: `${this.base.settings.serviceRoot}/GET_CONTEXT_MENU?moduleId=${encodeURIComponent(this.base.settings.moduleId)}&itemId=${encodeURIComponent(nodeId)}` });
                //TODO: DIT MOET ANDERS MAAR KOMT ZO VERKEERD UIT WISER
                contextMenu = JSON.parse(JSON.stringify(contextMenu).replace(/"attr":\[/g, '"attr":').replace(/\}\]\},/g, "}},").replace("}]}]", "}}]"));
                this.mainTreeViewContextMenu.setOptions({
                    dataSource: contextMenu
                });
            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan tijdens het ophalen van het rechtermuismenu van dit item. Probeer het a.u.b. nogmaals of neem contact op met ons.");
            }
        }

        /**
         * This event gets fired when the user clicks an option in the context menu.
         * @param {any} event The click event.
         */
        async onContextMenuClick(event) {
            const button = $(event.item);
            const action = button.attr("action");
            await this.handleContextMenuAction($(event.target), action);
        }

        async handleContextMenuAction(selectedNode, action) {
            if (!selectedNode || !action) {
                return;
            }

            const treeView = this.base.mainTreeView;
            const dataItem = treeView.dataItem(selectedNode);
            // For some reason the JCL already encodes the values, so decode them here, otherwise they will be encoded twice in some cases, which can cause problems.
            const itemId = decodeURIComponent(dataItem.id);
            const entityType = dataItem.entityType;

            try {
                switch (action) {
                    case "RENAME_ITEM":
                    {
                        kendo.prompt("Vul een nieuwe naam in", selectedNode.text()).done((newName) => {
                            this.base.updateItem(itemId, [], null, false, newName, false, true, entityType).then(() => {
                                this.base.notification.show({ message: "Succesvol gewijzigd" }, "success");
                                treeView.text(selectedNode, newName);
                                $("#right-pane input[name='_nameForExistingItem']").val(newName);
                            });
                        }).fail(() => { });
                        break;
                    }
                    case "CREATE_ITEM":
                    {
                        await this.base.dialogs.openCreateItemDialog(itemId, selectedNode, entityType);
                        break;
                    }
                    case "DUPLICATE_ITEM":
                    {
                        // Duplicate the item.
                        // For some reason the JCL already encodes the values, so decode them here, otherwise they will be encoded twice in some cases, which can cause problems.
                        const parentId = decodeURIComponent(dataItem.destinationItemId || this.base.settings.zeroEncrypted);
                        const parentItem = treeView.dataItem(this.base.mainTreeView.parent(selectedNode));
                        const duplicateItemResults = await this.base.duplicateItem(itemId, parentId, dataItem.entityType, parentItem ? parentItem.entityType : "");
                        this.base.notification.show({ message: `Het item '${dataItem.name || dataItem.title}' is gedupliceerd.` }, "success");

                        // Reload the parent item in the tree view, so that the new item becomes visible.
                        if (parentItem) {
                            parentItem.loaded(false);
                            parentItem.load();
                        } else {
                            treeView.dataSource.read();
                        }

                        break;
                    }
                    case "REMOVE_ITEM":
                    {
                        Wiser.showConfirmDialog(`Weet u zeker dat u het item '${dataItem.title}' wilt verwijderen?`).then(async () => {
                            try {
                                await this.base.deleteItem(itemId, entityType);
                                this.base.mainTreeView.remove(selectedNode);
                            } catch (exception) {
                                console.error(exception);
                                if (exception.status === 409) {
                                    const message = exception.responseText || "Het is niet meer mogelijk om dit item te verwijderen.";
                                    kendo.alert(message);
                                } else {
                                    kendo.alert("Er is iets fout gegaan tijdens het verwijderen van dit item. Probeer het a.u.b. nogmaals of neem contact op met ons.");
                                }
                            }
                        }).catch(() => { });

                        break;
                    }
                    case "HIDE_ITEM":
                    {
                        await Wiser.api({
                            url: `${this.base.settings.wiserApiRoot}items/${encodeURIComponent(itemId)}/environment/${this.environmentsEnum.hidden}?entityType=${encodeURIComponent(entityType)}`,
                            method: "PATCH",
                            contentType: "application/json",
                        });
                        selectedNode.closest("li").addClass("hiddenOnWebsite");
                        window.dynamicItems.notification.show({ message: "Item is verborgen" }, "success");
                        break;
                    }
                    case "PUBLISH_LIVE":
                    case "PUBLISH_ITEM":
                    {
                        const environments = this.environmentsEnum.development
                            + this.environmentsEnum.test
                            + this.environmentsEnum.acceptance
                            + this.environmentsEnum.live;

                        await Wiser.api({
                            url: `${this.base.settings.wiserApiRoot}items/${encodeURIComponent(itemId)}/environment/${environments}?entityType=${encodeURIComponent(entityType)}`,
                            method: "PATCH",
                            contentType: "application/json",
                        });
                        selectedNode.closest("li").removeClass("hiddenOnWebsite");
                        window.dynamicItems.notification.show({ message: "Item is zichtbaar gemaakt" }, "success");
                        break;
                    }
                    default:
                    {
                        await Wiser.api({ url: `${this.settings.serviceRoot}/${encodeURIComponent(action)}?itemid=${encodeURIComponent(itemId)}` });
                        window.dynamicItems.notification.show({ message: "Succesvol gewijzigd" }, "success");
                        break;
                    }
                }
            } catch (onContextMenuClickException) {
                console.error("Error during onContextMenuClick", onContextMenuClickException);
                if (onContextMenuClickException === undefined) {
                    return;
                }


                kendo.alert("Er is iets fout gegaan met het uitvoeren van deze actie. Probeer het a.u.b. nogmaals of neem contact op met ons");
            }
        }

        /**
         * Refreshes the current view.
         * If it's a gridView, it refreshed the grid.
         * If it's the normal view, it refreshes the tree view and currently opened item.
         * @param {any} event The click event of the button.
         */
        onMainRefreshButtonClick(event) {
            event.preventDefault();
            if (this.settings.gridViewMode) {
                this.grids.mainGridForceRecount = true;
                this.grids.mainGrid.dataSource.read();

                if (this.grids.informationBlockIframe && this.grids.informationBlockIframe[0] && this.grids.informationBlockIframe[0].contentWindow) {
                    this.grids.informationBlockIframe[0].contentWindow.location.reload();
                }
            } else {
                if (!this.settings.iframeMode) {
                    this.mainTreeView.dataSource.read();
                }
                if (this.selectedItem || (this.settings.iframeMode && this.settings.initialItemId)) {
                    const previouslySelectedTab = this.mainTabStrip.select().index();
                    this.loadItem(this.settings.iframeMode ? this.settings.initialItemId : this.selectedItem.id, previouslySelectedTab, this.settings.iframeMode ? this.settings.entityType : this.selectedItem.entityType);
                }
            }
        }

        /**
         * Saves the current active item.
         * @param {any} event The save button click event.
         */
        async onSaveButtonClick(event) {
            event.preventDefault();
            const process = `saveItem_${Date.now()}`;
            window.processing.addProcess(process);

            try {
                const itemId = this.selectedItem && this.selectedItem.id ? this.selectedItem.id : this.settings.initialItemId;
                const inputData = this.base.fields.getInputData($("#right-pane-content, .dynamicTabContent"));
                const title = $("#tabstrip .itemNameFieldContainer .itemNameField").val();

                if (!this.mainValidator.validate()) {
                    window.processing.removeProcess(process);
                    return false;
                }

                const updateItemResult = await this.base.updateItem(itemId, inputData, $("#right-pane"), false, title, true, true, this.selectedItem && this.selectedItem.entityType ? this.selectedItem.entityType : this.selectedItemMetaData.entityType);
                document.dispatchEvent(new CustomEvent("dynamicItems.onSaveButtonClick", { detail: updateItemResult }));
                if (window.parent && window.parent.document) {
                    window.parent.document.dispatchEvent(new CustomEvent("dynamicItems.onSaveButtonClick", { detail: updateItemResult }));
                }

                window.processing.removeProcess(process);
                return true;
            } catch (exception) {
                console.error(exception);
                switch (exception.status) {
                    case 409: {
                        const message = exception.responseText || "Het is niet meer mogelijk om aanpassingen te maken in dit item.";
                        kendo.alert(message);
                        break;
                    }
                    case 403: {
                        const message = exception.responseText || "U heeft niet de juiste rechten om dit item te wijzigen.";
                        kendo.alert(message);
                        break;
                    }
                    default:
                        kendo.alert("Er is iets fout gegaan tijdens het opslaan van dit item. Probeer het a.u.b. nogmaals of neem contact op met ons.");
                        break;
                }

                window.processing.removeProcess(process);
                return false;
            }
        }

        /**
         * Event that gets called once a notification is being shown.
         */
        onShowNotification(event) {
            event.element.parent().css({
                width: "auto",
                minWidth: "200px",
                left: "",
                right: "15px"
            });
        }

        /**
         * Handles the main tree view data bound event.
         * @param {any} event The Kendo data bound event.
         */
        onTreeViewDataBound(event) {
            (event.node || event.sender.element).find("li").each((index, element) => {
                const dataItem = event.sender.dataItem(element);
                if (!dataItem.nodeCssClass) {
                    return;
                }

                $(element).addClass(dataItem.nodeCssClass);
            });

            this.base.toggleMainLoader(false);
        }

        /**
         * Handles the click on a node in the tree view (open the item).
         * @param {any} event The click event of the Kendo tree view.
         */
        async onTreeViewItemClick(event) {
            const treeView = event.sender;
            const dataItem = treeView.dataItem(event.node);
            if (!dataItem) {
                console.warn("onTreeViewItemClick triggered, but could not get data item of selected node", event);
                return;
            }

            let itemId = dataItem.id;
            this.base.selectedItem = dataItem;

            // If we have an original item id, it means this item has multiple version. Then we want to check what the latest version is and open that one.
            // This used to be done in the query that gets the items for the tree view, but that made the query really slow for some tenants, so now we do it here.
            if (dataItem.plainOriginalItemId > 0) {
                let itemToUse = null;
                const itemEnvironments = await this.getItemEnvironments(dataItem.encryptedOriginalItemId);
                if (itemEnvironments && itemEnvironments.length) {
                    for (let itemVersion of itemEnvironments) {
                        if (itemVersion.changedOn) {
                            itemVersion.changedOn = new Date(itemVersion.changedOn);
                        }

                        if (!itemToUse || itemVersion.changedOn > itemToUse.changedOn) {
                            itemToUse = itemVersion;
                        }
                    }
                }

                if (itemToUse) {
                    itemId = itemToUse.id;
                    // Change the ID of the selected item, otherwise the save button will overwrite the wrong item.
                    this.base.selectedItem.id = itemId;
                    this.base.selectedItem.plainItemId = itemToUse.plainItemId;
                }
            }

            // Set the correct values in the crumb trail.
            const crumbTrail = $("#crumbTrail").empty();
            const parents = $(event.node).add($(event.node).parentsUntil(".k-treeview", ".k-item"));
            const amountOfItems = parents.length;
            let counter = 0;
            const fullPath = [];

            const texts = $.map(parents, (node) => {
                counter++;

                const text = $(node).find(">div span.k-in").text();
                fullPath.push(text);
                const newCrumbTrailNode = $("<li/>");

                if (counter < amountOfItems) {
                    const link = $("<a href='#' />").appendTo(newCrumbTrailNode);
                    link.text(text);
                    link.click((event) => {
                        event.preventDefault();
                        treeView.select(node);
                        treeView.trigger("select", {
                            node: node
                        });
                    });
                } else {
                    newCrumbTrailNode.text(text);
                }

                return newCrumbTrailNode;
            });

            crumbTrail.html(texts);

            this.base.mainTabStrip.tabGroup.children().each((index, element) => {
                if ($(element).text().trim().toLowerCase() !== "overzicht") {
                    return;
                }

                $(element).toggle(dataItem.hasChildren);
            });

            await this.base.loadItem(itemId, 0, dataItem.entityType || dataItem.entityType);

            const pathString = `/${fullPath.join("/")}/`;
            // Show / hide fields based on path regex.
            $("#right-pane .item").each((index, element) => {
                const fieldContainer = $(element);
                const pathRegex = fieldContainer.data("visibilityPathRegex");
                if (!pathRegex) {
                    return;
                }

                try {
                    const regex = new RegExp(pathRegex);
                    const showField = regex.test(pathString);
                    fieldContainer.toggleClass("hidden", !showField);
                    if (!showField) {
                        console.log(`Field '${fieldContainer.data("propertyName")}' has been hidden because of visibility_path_regex '${pathRegex}'`);
                    }
                } catch(exception) {
                    console.error(`Error occurred while trying to hide/show field '${fieldContainer.data("propertyName")}' based on regex '${pathRegex}'`, exception);
                }
            });

            // Get available entity types, for creating new sub items.
            await this.base.dialogs.loadAvailableEntityTypesInDropDown(itemId);
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
         * Event for when an item in a kendoTreeView gets dragged.
         * @param {any} event The drop item event of a kendoTreeView.
         */
        onTreeViewDragItem(event) {
            // Scroll left pane up/down when moving items.
            const leftPane = $("#left-pane");
            const topOfTreeView = leftPane.offset().top;
            const bottomOfTreeView = topOfTreeView + leftPane.height();
            const dropTargetOffset = $(event.dropTarget).offset().top;
            if (dropTargetOffset > 0 && dropTargetOffset > bottomOfTreeView - 50) {
                leftPane.scrollTop(leftPane.scrollTop() + $(event.dropTarget).height() + 50);
            } else if (dropTargetOffset > 0 && dropTargetOffset - 10 < topOfTreeView) {
                leftPane.scrollTop(leftPane.scrollTop() - $(event.dropTarget).height() - 50);
            }

            if (event.statusClass === "i-cancel") {
                // Tree view already denies this operation
                return;
            }

            const dropTarget = $(event.dropTarget);
            let destinationNode = dropTarget.closest("li.k-item");
            if (dropTarget.hasClass("k-mid")) {
                // If the dropTarget is an element with class k-mid we need to go higher, because those elements are located inside an li.k-item instead of after/before them.
                destinationNode = destinationNode.parentsUntil("li.k-item");
            }
            if (event.statusClass === "i-insert-down" || (event.statusClass === "i-insert-middle" && (dropTarget.hasClass("k-bot") || dropTarget.hasClass("k-in")))) {
                // If the statusClass is i-insert-down, it means we are adding the item below the destination, so we need to check it's parent.
                destinationNode = destinationNode.parentsUntil("li.k-item");
            }

            const sourceDataItem = event.sender.dataItem(event.sourceNode) || {};
            const destinationDataItem = event.sender.dataItem(destinationNode) || {};

            if ((destinationDataItem.acceptedChildTypes || "").toLowerCase().split(",").indexOf(sourceDataItem.entityType.toLowerCase()) === -1) {
                // Tell the kendo tree view to deny the drag to this item, if the current item is of a type that is not allowed to be linked to the destination.
                event.setStatusClass("k-i-cancel");
            }
        }

        /**
         * Event for when an item in a kendoTreeView gets dropped.
         * @param {any} event The drop item event of a kendoTreeView.
         */
        async onTreeViewDropItem(event) {
            if (!event.valid) {
                return;
            }

            try {
                const sourceDataItem = event.sender.dataItem(event.sourceNode);
                const destinationDataItem = event.sender.dataItem(event.destinationNode);

                await Wiser.api({
                    url: `${this.base.settings.wiserApiRoot}items/${encodeURIComponent(sourceDataItem.id)}/move/${encodeURIComponent(destinationDataItem.id)}`,
                    method: "PUT",
                    contentType: "application/json",
                    data: JSON.stringify({
                        position: event.dropPosition,
                        encryptedSourceParentId: sourceDataItem.destinationItemId,
                        encryptedDestinationParentId: destinationDataItem.destinationItemId,
                        sourceEntityType: sourceDataItem.entityType,
                        destinationEntityType: destinationDataItem.entityType,
                        moduleId: this.base.settings.moduleId
                    })
                });

                sourceDataItem.destinationItemId = destinationDataItem.destinationItemId;
            } catch (exception) {
                console.error(exception);
                kendo.alert(`Er is iets fout gegaan met het verplaatsen van dit item. De fout was:<br>${exception.responseText || exception.statusText}`);
                event.setValid(false);
            }
        }

        /**
         * Event for when a tab gets selected in a tab strip.
         * @param {any} itemId The ID of the currently opened item.
         * @param {string} windowId The ID of the window that contains the tabs and fields. If this is for the default/main screen/window, enter "mainScreen" in this parameter.
         * @param {any} event The activate event of a kendoTabStrip.
         */
        async onTabStripSelect(itemId, windowId, event) {
            // Initialize dynamic fields on the current tab, if that hasn't been done yet.
            const contentElement = $(event.contentElement);
            await this.fields.initializeDynamicFields(windowId, $(event.item).text(), contentElement);

            // Refresh code mirror instances, otherwise they get strange styling issues.
            setTimeout(() => {
                contentElement.find("textarea").each((index, element) => {
                    const codeMirrorInstance = $(element).data("CodeMirrorInstance");
                    if (!codeMirrorInstance) {
                        return;
                    }

                    codeMirrorInstance.refresh();
                });
            }, 500);

            // Load overview grid, if it exists on the current tab.
            const overviewGridElement = contentElement.find("#overViewGrid");
            this.loadOverviewGrid(itemId, overviewGridElement);
        }

        /**
         * Initializes the grid that shows the history of an item.
         * @param {any} itemId The ID of the item.
         * @param {any} entityType The entity type of the item.
         * @param {any} moduleId The module ID of the item.
         */
        async loadHistoryGrid(itemId, entityType, moduleId) {
            const kendoHistoryGridWindow = this.windows.historyGridWindow;
            const historyGridElement = $("#historyWindowGrid");
            kendoHistoryGridWindow.maximize().open();
            if (historyGridElement) historyGridElement.empty();

            try {

                const options = {
                    page: 1,
                    pageSize: 100,
                    skip: 0,
                    take: 100
                };

                const gridDataResult = await Wiser.api({
                    url: `${this.base.settings.wiserApiRoot}items/${encodeURIComponent(itemId)}/entity-grids/${entityType}?mode=3&moduleId=${moduleId}`,
                    method: "POST",
                    contentType: "application/json",
                    data: JSON.stringify(options)
                });

                if (gridDataResult.extraJavascript) {
                    $.globalEval(gridDataResult.extraJavascript);
                }

                this.windows.historyGridFirstLoad = true;

                let previousFilters = null;
                let totalResults = gridDataResult.totalResults;

                await require("/kendo/messages/kendo.grid.nl-NL.js");

                this.windows.historyGrid = historyGridElement.kendoGrid({
                    dataSource: {
                        serverPaging: true,
                        serverSorting: true,
                        serverFiltering: true,
                        pageSize: gridDataResult.pageSize,
                        transport: {
                            read: async (transportOptions) => {
                                try {
                                    if (this.windows.historyGridFirstLoad) {
                                        transportOptions.success(gridDataResult);
                                        this.windows.historyGridFirstLoad = false;
                                        return;
                                    }

                                    // If we're using the same filters as before, we don't need to count the total amount of results again,
                                    // so we tell the API whether this is the case, so that it can skip the execution of the count query, to make scrolling through the grid faster.
                                    let currentFilters = null;
                                    if (transportOptions.data.filter) {
                                        currentFilters = JSON.stringify(transportOptions.data.filter);
                                    }

                                    transportOptions.data.firstLoad = currentFilters !== previousFilters;
                                    transportOptions.data.pageSize = transportOptions.data.pageSize;
                                    previousFilters = currentFilters;

                                    const newGridDataResult = await Wiser.api({
                                        url: `${this.base.settings.wiserApiRoot}items/${encodeURIComponent(itemId)}/entity-grids/${this.settings.entityType}?mode=3&moduleId=${this.base.settings.moduleId}`,
                                        method: "POST",
                                        contentType: "application/json",
                                        data: JSON.stringify(transportOptions.data)
                                    });

                                    if (typeof newGridDataResult.totalResults !== "number" || !transportOptions.data.firstLoad) {
                                        newGridDataResult.totalResults = totalResults;
                                    } else if (transportOptions.data.firstLoad) {
                                        totalResults = newGridDataResult.totalResults;
                                    }

                                    transportOptions.success(newGridDataResult);
                                } catch (exception) {
                                    console.error(exception);
                                    transportOptions.error(exception);
                                }
                            }
                        },
                        schema: {
                            data: "data",
                            total: "totalResults",
                            model: gridDataResult.schemaModel
                        }
                    },
                    // height: "500",
                    columns: gridDataResult.columns,
                    resizable: true,
                    sortable: false,
                    scrollable: {
                        virtual: true
                    },
                    filterable: {
                        extra: false,
                        operators: {
                            string: {
                                startswith: "Begint met"
                            }
                        },
                        messages: {
                            isTrue: "<span>Ja</span>",
                            isFalse: "<span>Nee</span>"
                        }
                    },
                    filterMenuInit: this.base.grids.onFilterMenuInit.bind(this),
                    filterMenuOpen: this.base.grids.onFilterMenuOpen.bind(this)
                }).data("kendoGrid");
            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan met het initialiseren van de historie. Probeer het a.u.b. nogmaals of neem contact op met ons.");
            }

        }

        /**
         * Load the basic overview grid. This is a grid in a separate tab that only contains this grid.
         * This grid shows all items linked to the current item.
         * @param {any} itemId The ID of the current item.
         * @param {any} overviewGridElement The DOM element of the overview grid.
         */
        async loadOverviewGrid(itemId, overviewGridElement) {
            try {
                if (!itemId || overviewGridElement.length <= 0) {
                    return;
                }

                const customColumns = await Wiser.api({ url: `${this.settings.serviceRoot}/GET_COLUMNS_FOR_TABLE?itemId=${encodeURIComponent(itemId)}` });
                const model = {
                    id: "id",
                    fields: {
                        id: {
                            type: "number"
                        },
                        publishedEnvironment: {
                            type: "string"
                        },
                        title: {
                            type: "string"
                        },
                        entityType: {
                            type: "string"
                        },
                        property_: {
                            type: "object"
                        }
                    }
                };

                const columns = [
                    {
                        field: "id",
                        title: "Id",
                        width: 55
                    },
                    {
                        field: "entityType",
                        title: "Type",
                        width: 100
                    },
                    {
                        template: "<ins title='#: publishedEnvironment #' class='icon-#: publishedEnvironment #'></ins>",
                        field: "publishedEnvironment",
                        title: "Gepubliceerde omgeving",
                        width: 50
                    },
                    {
                        field: "title",
                        title: "Naam"
                    }
                ];

                if (customColumns && customColumns.length > 0) {
                    for (let column of customColumns) {
                        if (column.field) {
                            column.field = column.field.toLowerCase();
                        }

                        columns.push(column);
                    }
                }

                let grid;
                columns.push({
                    title: "&nbsp;",
                    width: 80,
                    command: [{
                        name: "openDetails",
                        iconClass: "k-icon k-i-hyperlink-open",
                        text: "",
                        click: (event) => { this.base.grids.onShowDetailsClick(event, grid, {}); }
                    }]
                });

                if (overviewGridElement.data("kendoGrid")) {
                    overviewGridElement.data("kendoGrid").destroy();
                    overviewGridElement.empty();
                }

                grid = overviewGridElement.kendoGrid({
                    dataSource: {
                        transport: {
                            read: async (options) => {
                                try {
                                    const results = await Wiser.api({ url: `${this.settings.serviceRoot}/GET_DATA_FOR_TABLE?itemId=${encodeURIComponent(itemId)}` });
                                    if (!results) {
                                        options.success(results);
                                        return;
                                    }

                                    for (let i = 0; i < results.length; i++) {
                                        const row = results[i];
                                        if (!row.property_) {
                                            row.property_ = {};
                                        }
                                    }

                                    options.success(results);
                                } catch (exception) {
                                    console.error(exception);
                                    options.error(exception);
                                }
                            }
                        },
                        pageSize: 100,
                        schema: {
                            model: model
                        }
                    },
                    toolbar: ["excel"],
                    excel: {
                        fileName: "Export overzicht " + this.selectedItem.name + ".xlsx",
                        //proxyURL: "https://demos.telerik.com/kendo-ui/service/export",
                        filterable: true
                    },
                    columns: columns,
                    pageable: true,
                    sortable: true,
                    resizable: true,
                    scrollable: true,
                    height: "600px",
                    filterable: {
                        extra: false,
                        messages: {
                            isTrue: "<span>Ja</span>",
                            isFalse: "<span>Nee</span>"
                        }
                    },
                    filterMenuInit: this.base.grids.onFilterMenuInit.bind(this),
                    filterMenuOpen: this.base.grids.onFilterMenuOpen.bind(this)
                }).data("kendoGrid");

                grid.thead.kendoTooltip({
                    filter: "th",
                    content: (event) => {
                        const target = event.target; // element for which the tooltip is shown
                        return $(target).text();
                    }
                });

                grid.element.on("dblclick", "tbody tr[data-uid] td", (event) => { this.base.grids.onShowDetailsClick(event, grid, {}); });
            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan tijdens het laden van het overzicht. Probeer het a.u.b. nogmaals door het item te sluiten en opnieuw te openen, of neem contact op met ons.");
            }
        }

        /**
         * The click event for the delete button.
         * @param {any} event The click event.
         * @param {string} encryptedItemId The encrypted ID of the item to undelete.
         */
        async onDeleteItemClick(event, encryptedItemId, entityType) {
            event.preventDefault();

            if (this.base.selectedItem && this.base.selectedItem.title) {
                await Wiser.showConfirmDialog(`Weet u zeker dat u het item '${this.base.selectedItem.title}' wilt verwijderen?`);
            } else {
                await Wiser.showConfirmDialog(`Weet u zeker dat u dit item wilt verwijderen?`);
            }

            try {
                await this.deleteItem(encryptedItemId, entityType);

                if (!this.settings.iframeMode) {
                    // Close the opened item.
                    $("#left-pane, .k-window-titlebar").click();

                    // Refresh the tree view.
                    this.windows.mainWindow.wrapper.find(".k-i-refresh").parent().click();
                } else {
                    $("#mainEditMenu .reloadItem").click();
                }

            } catch (exception) {
                console.error(exception);
                if (exception.status === 409) {
                    const message = exception.responseText || "Het is niet meer mogelijk om dit item te verwijderen.";
                    kendo.alert(message);
                } else {
                    kendo.alert("Er is iets fout gegaan tijdens het verwijderen van dit item. Probeer het a.u.b. nogmaals of neem contact op met ons.");
                }
            }
        }

        /**
         * The click event for the undelete button.
         * @param {any} event The click event.
         * @param {string} encryptedItemId The encrypted ID of the item to undelete.
         */
        async onUndeleteItemClick(event, encryptedItemId) {
            event.preventDefault();

            const title = $("#tabstrip .itemNameFieldContainer .itemNameField").val();
            await Wiser.showConfirmDialog(`Weet u zeker dat u het verwijderen ongedaan wilt maken voor '${title}'?`, "Verwijderen ongedaan maken", "Annuleren", "Terugzetten");

            const process = `undeleteItem_${Date.now()}`;
            window.processing.addProcess(process);

            const popupWindowContainer = $(event.currentTarget).closest(".popup-container");

            try {
                popupWindowContainer.find(".popup-loader").addClass("loading");
                popupWindowContainer.data("saving", true);

                let entityType = popupWindowContainer.data("entityTypeDetails") || this.settings.entityType;

                if (Wiser.validateArray(entityType)) {
                    entityType = entityType[0];
                }

                await this.base.undeleteItem(encryptedItemId, entityType);

                const kendoWindow = popupWindowContainer.data("kendoWindow");
                if (kendoWindow) {
                    const data = kendoWindow.element.data();
                    if (data.senderGrid) {
                        data.senderGrid.dataSource.read();
                    }
                }

                $(event.currentTarget).closest(".entity-container").find(".k-i-verversen").click();
            } catch (exception) {
                console.error(exception);
                popupWindowContainer.find(".popup-loader").removeClass("loading");

                if (exception.status === 409) {
                    const message = exception.responseText || "Het is niet meer mogelijk om het verwijderen ongedaan te maken.";
                    kendo.alert(message);
                } else {
                    kendo.alert("Er is iets fout gegaan tijdens het verwijderen ongedaan maken van dit item. Probeer het a.u.b. nogmaals of neem contact op met ons.");
                }
            }

            window.processing.removeProcess(process);
            popupWindowContainer.find(".popup-loader").removeClass("loading");
            popupWindowContainer.data("saving", false);
        }

        /**
         * The click event for the button to translate all fields on an item.
         * @param {any} event The click event.
         * @param {string} encryptedItemId The encrypted ID of the item to undelete.
         * @param {string} entityType The entity type of the item.
         */
        async onTranslateItemClick(event, encryptedItemId, entityType) {
            event.preventDefault();

            try {
                const dialogElement = $("#translateItemDialog");
                // Set encrypted item ID and entity type in dialog element, so that it will be updated everytime. Otherwise it will keep the old item ID when translating multiple items in a row.
                dialogElement.data("encryptedItemId", encryptedItemId);
                dialogElement.data("entityType", entityType);
                let translateItemDialog = dialogElement.data("kendoDialog");

                await require("@progress/kendo-ui/js/kendo.multiselect.js");

                const sourceLanguageDropDownElement = dialogElement.find("#sourceLanguageDropDown");
                const targetLanguagesMultiSelectElement = dialogElement.find("#targetLanguagesMultiSelect");

                let sourceLanguageDropDown = sourceLanguageDropDownElement.data("kendoDropDownList");
                let targetLanguagesMultiSelect = targetLanguagesMultiSelectElement.data("kendoMultiSelect");

                if (!sourceLanguageDropDown) {
                    sourceLanguageDropDown = sourceLanguageDropDownElement.kendoDropDownList({
                        dataSource: this.allLanguages,
                        dataTextField: "name",
                        dataValueField: "code"
                    }).data("kendoDropDownList");
                }

                if (!targetLanguagesMultiSelect) {
                    targetLanguagesMultiSelect = targetLanguagesMultiSelectElement.kendoMultiSelect({
                        dataSource: this.allLanguages,
                        dataTextField: "name",
                        dataValueField: "code"
                    }).data("kendoMultiSelect");
                }

                let defaultLanguage = this.allLanguages.find(l => l.isDefaultLanguage);
                if (!defaultLanguage) {
                    defaultLanguage = this.allLanguages[0];
                }

                sourceLanguageDropDown.value(defaultLanguage.code);
                targetLanguagesMultiSelect.value("-1");

                if (!translateItemDialog) {
                    translateItemDialog = dialogElement.kendoDialog({
                        width: "900px",
                        title: "Item vertalen",
                        closable: false,
                        modal: true,
                        actions: [
                            {
                                text: "Annuleren"
                            },
                            {
                                text: "Vertalen",
                                primary: true,
                                action: async () => {
                                    const process = `translateItem_${Date.now()}`;
                                    window.processing.addProcess(process);

                                    Wiser.api({
                                        url: `${this.settings.wiserApiRoot}items/${encodeURIComponent(dialogElement.data("encryptedItemId"))}/translate`,
                                        method: "PUT",
                                        contentType: "application/json",
                                        data: JSON.stringify({
                                            entityType: dialogElement.data("entityType"),
                                            sourceLanguageCode: sourceLanguageDropDown.value(),
                                            targetLanguageCodes: targetLanguagesMultiSelect.value()
                                        })
                                    }).then(() => {
                                        translateItemDialog.close();
                                        $(event.currentTarget).closest(".entity-container").find(".reloadItem").click();
                                        $(event.currentTarget).closest(".k-window").find(".k-i-verversen").parent().click()
                                        this.notification.show({message: "Vertalen is gelukt"}, "success");
                                    }).catch((error) => {
                                        console.error("An error occurred while translating an item", error);
                                        let errorMessage = "";
                                        if (error.responseJSON && error.responseJSON.error) {
                                            errorMessage = error.responseJSON.error;
                                        } else if (error.responseText) {
                                            errorMessage = error.responseText;
                                        } else if (error.statusText) {
                                            errorMessage = error.statusText;
                                        }

                                        if (errorMessage) {
                                            kendo.alert(`Er is iets fout gegaan met vertalen. De fout was:<br><pre>${errorMessage}</pre>`);
                                        } else {
                                            kendo.alert(`Er is iets fout gegaan met vertalen. Probeer het a.u.b. nogmaals of neem contact op.`);
                                        }
                                    }).finally(() => {
                                        window.processing.removeProcess(process);
                                    });
                                }
                            }
                        ]
                    }).data("kendoDialog");
                }

                translateItemDialog.open();
            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan. Probeer het a.u.b. nogmaals of neem contact op met ons.");
            }
        }

        /**
         * Load a specific item in the main container / tab strip.
         * @param {any} itemId The ID of the item to load.
         * @param {number} tabToSelect Optional: The tab index to initially open after the item has been loaded. Default is 0.
         */
        async loadItem(itemId, tabToSelect = 0, entityType = null) {
            const process = `loadItem_${Date.now()}`;
            window.processing.addProcess(process);

            // Set meta data of the selected item in the footer.
            try {
                const entityContainer = $("#right-pane .entity-container");
                const itemMetaData = await this.base.addItemMetaData(itemId, entityType, $("#metaData"), false, entityContainer);
                if (!itemMetaData) {
                    console.warn("No meta data found for item, the user probably doesn't have rights for it anymore.");
                    window.processing.removeProcess(process);
                    return;
                }

                const entityTypeSettings = await this.base.getEntityType(itemMetaData.entityType);
                entityTypeSettings.showTitleField = entityTypeSettings.showTitleField || false;
                this.selectedItemTitle = itemMetaData.title;
                this.selectedItemMetaData = itemMetaData;
                const itemTitleFieldContainer = $("#tabstrip .itemNameFieldContainer");
                itemTitleFieldContainer.find(".itemNameField").val(itemMetaData.title);
                itemTitleFieldContainer.toggle(entityTypeSettings.showTitleField && this.base.settings.iframeMode);

                // Set the HTML of the fields tab.
                const itemHtmlResult = await this.getItemHtml(itemId, itemMetaData.entityType);

                this.mainTabStrip.element.find("> .k-tabstrip-items-wrapper > ul > li .addedFromDatabase").each((index, element) => {
                    this.mainTabStrip.remove($(element).closest("li.k-item"));
                });

                // Reset the main screen field initializers so that they don't stay in memory. We don't need them anymore once we load a new item.
                this.base.fields.fieldInitializers.mainScreen = {};

                // Load the HTML and javascript for all fields of every tab.
                // Only the javascript of the first tab will be executed right away, the rest will be done if and when the user switches to that tab.
                const container = $("#right-pane-content").html("");

                // Handle access rights.
                itemTitleFieldContainer.prop("readonly", !itemHtmlResult.canWrite).prop("disabled", !itemHtmlResult.canWrite);
                $("#saveButton, #saveBottom, #saveAndCreateNewItemButton").each((index, element) => {
                    element = $(element);
                    if (itemHtmlResult.tabs.length === 0) {
                        element.addClass("hidden");
                        return;
                    }

                    if (element.data("hidden-via-parent")) {
                        return;
                    }

                    if (element.attr("id") === "saveAndCreateNewItemButton") {
                        element.toggleClass("hidden", !itemHtmlResult.canWrite || !element.data("shown-via-parent"));
                    } else {
                        element.toggleClass("hidden", !itemHtmlResult.canWrite);
                    }
                });

                let genericTabHasFields = false;
                for (let i = itemHtmlResult.tabs.length - 1; i >= 0; i--) {
                    const tabData = itemHtmlResult.tabs[i];
                    if (!tabData.name) {
                        genericTabHasFields = true;
                        container.html(tabData.htmlTemplate);
                        await this.base.loadKendoScripts(tabData.scriptTemplate);
                        $.globalEval(tabData.scriptTemplate);
                    } else {
                        this.mainTabStrip.insertAfter({
                            text: tabData.name,
                            content: "<div class='dynamicTabContent'>" + tabData.htmlTemplate + "</div>",
                            spriteCssClass: "addedFromDatabase"
                        }, this.mainTabStrip.tabGroup.children().eq(0));

                        this.base.fields.fieldInitializers.mainScreen[tabData.name] = {
                            executed: false,
                            script: tabData.scriptTemplate,
                            entityType: itemMetaData.entityType
                        };
                    }
                }

                const translateButton = entityContainer.find(".editMenu .translateItem").closest("li");
                translateButton.toggle(this.allLanguages.length > 1 && entityContainer.find(".item[data-language-code]:not([data-language-code=''])").length > 0);

                // Setup dependencies for all tabs.
                for (let i = itemHtmlResult.tabs.length - 1; i >= 0; i--) {
                    const tabData = itemHtmlResult.tabs[i];
                    const container = this.mainTabStrip.contentHolder(i);
                    this.base.fields.setupDependencies(container, itemMetaData.entityType, tabData.name || "Gegevens");
                }

                // Handle dependencies for the first tab, to make sure all the correct fields are hidden/shown on the first tab. The other tabs will be done once they are opened.
                this.base.fields.handleAllDependenciesOfContainer(this.mainTabStrip.contentHolder(0), itemMetaData.entityType, "Gegevens", "mainScreen");

                $(this.mainTabStrip.items()[0]).toggle(genericTabHasFields || itemTitleFieldContainer.is(":visible"));

                // Figure our which tab to select (don't select hidden or empty tabs).
                const indexBefore = this.mainTabStrip.select().index();
                tabToSelect = tabToSelect || (genericTabHasFields ? 0 : 1);
                const allTabs = this.mainTabStrip.tabGroup.find("li");
                if (allTabs.length <= tabToSelect || !$(allTabs[tabToSelect]).is(":visible")) {
                    tabToSelect = 0;
                }

                this.mainTabStrip.select(tabToSelect);
                const indexAfter = this.mainTabStrip.select().index();
                if (indexBefore === indexAfter) {
                    // Kendo does trigger the select event if you select the same tab again, so we have to do it manually to make sure the contents of the newly loaded item will be shown, instead of the contents of the previous item.
                    await this.onTabStripSelect((!this.selectedItem || !this.selectedItem.id ? 0 : this.selectedItem.id), "mainScreen", { item: this.mainTabStrip.select(), contentElement: this.mainTabStrip.contentElement(this.mainTabStrip.select().index()) });
                }

                // If the mode for changing field widths is enabled, call the method that show the current width of each field,
                // so that the width numbers don't disappear after opening a different item.
                if ($("#widthToggle").prop("checked")) {
                    container.find(".item").each((index, element) => {
                        this.dragAndDrop.showElementWidth($(element));
                    });
                }
            } catch (exception) {
                console.error(exception);
                if (exception.status === 404) {
                    kendo.alert("Het opgevraagde item bestaat niet.");
                } else {
                    kendo.alert("Er is iets fout gegaan met het laden van dit item. Probeer het a.u.b. nogmaals of neem contact op met ons.");
                }
            }

            window.processing.removeProcess(process);
        }

        /**
         * Updates an item in the database.
         * @param {string} encryptedItemId The encrypted item ID.
         * @param {Array<any>} inputData All values of all fields.
         * @param {any} fieldsContainer The container that contains all fields.
         * @param {boolean} isNewItem Whether or not this is a new item.
         * @param {string} title The title of the item.
         * @param {boolean} showSuccessMessage Whether or not to show a message to the user if/when the update has succeeded.
         * @param {boolean} executeWorkFlow Whether or not to execute any workflow that might be set up, if/when the update has succeeded.
         * @param {string} entityType The entity type of the item.
         * @returns {any} A promise with the result of the AJAX call.
         */
        async updateItem(encryptedItemId, inputData, fieldsContainer, isNewItem, title = null, showSuccessMessage = true, executeWorkFlow = true, entityType = null) {
            const updateResult = await Wiser.updateItem(this.settings, encryptedItemId, inputData, isNewItem, title, executeWorkFlow, entityType);

            if (fieldsContainer) {
                const windowId = fieldsContainer.hasClass("popup-container") ? fieldsContainer.attr("id") : "mainScreen";

                if (this.base.fields.originalItemValues[windowId] && this.base.fields.unsavedItemValues[windowId]) {
                    $.extend(true, this.base.fields.originalItemValues[windowId], this.base.fields.unsavedItemValues[windowId]);
                }
                this.base.fields.unsavedItemValues[windowId] = {};

                if (updateResult && updateResult.details && updateResult.details.length) {
                    updateResult.details.forEach((itemDetail) => {
                        if (!itemDetail || !itemDetail.id) {
                            return;
                        }

                        const field = fieldsContainer.find(`[name='${itemDetail.key}']`);
                        if (field.is(":disabled,[readonly]")) {
                            field.val(itemDetail.value);
                        }
                    });
                }
            }

            if (updateResult && showSuccessMessage) {
                this.notification.show({ message: "Opslaan is gelukt" }, "success");
            }
        }

        /**
         * Loads the meta data of an item and add it to the given meta data container.
         * @param {string} itemId The encrypted ID of the item.
         * @param {string} entityType The entity type of the item.
         * @param {any} metaDataContainer The container that contains the list (<ul>) with meta data values.
         * @param {boolean} isForItemWindow Optional: Indicates whether or not the current item is being loaded in a window. If it's not, some extra things will be done such as setting up dependencies on fields.
         * @param {any} mainFieldsContainer Optional: The container that contains all the fields of the item. Required if isForItemWindow is set to false.
         * @param {any} callerWindow Optional: The window that called this method. Is required when opening items in a new window, so that we know in which window to load an item if the user switches environments.
         * @returns {any} A promise with the result of the AJAX call.
         */
        async addItemMetaData(itemId, entityType, metaDataContainer, isForItemWindow = false, mainFieldsContainer = null, callerWindow = null) {
            const process = `loadMetaData_${Date.now()}`;
            if (!isForItemWindow) {
                window.processing.addProcess(process);
            }

            try {
                const itemMetaData = await this.getItemMetaData(itemId, entityType);
                if (!itemMetaData) {
                    console.warn("No meta data found for item, the user probably doesn't have rights for it anymore.");
                    window.processing.removeProcess(process);
                    return null;
                }

                // Warn the user if they opened a deleted item.
                if (itemMetaData.removed) {
                    kendo.alert("<h1>Let op! Dit item is verwijderd!</h1>");
                }

                // Check permissions and hide buttons that users are not allowed to use.
                // Default permissions are everything enabled, so if the user has no role or the role has no permissions on this item, they are allowed everything.
                const itemContainer = metaDataContainer.closest("#right-pane, .popup-container");
                const editMenu = itemContainer.find(".editMenu .editSub");
                const saveButtons = itemContainer.find(".saveButton");
                const deleteButtons = itemContainer.find(".k-i-verwijderen, .editMenu .deleteItem").parent();
                const undeleteButtons = editMenu.find(".undeleteItem").closest("li");

                saveButtons.toggleClass("hidden", !itemMetaData.canWrite);

                // If there are still options for switching environment, remove them and re-add them, because they are probably left overs of a different item.
                editMenu.find("li.otherEnvironment").remove();
                if (itemMetaData.plainOriginalItemId) {
                    const itemEnvironments = await this.getItemEnvironments(itemMetaData.plainOriginalItemId);
                    if (itemEnvironments && itemEnvironments.length) {
                        for (let otherItem of itemEnvironments) {
                            if (otherItem.plainItemId === itemMetaData.id) {
                                continue;
                            }

                            const otherItemEnvironmentLabel = this.base.parseEnvironments(otherItem.publishedEnvironment);
                            const newElement = $(`<li class="otherEnvironment"><label><span>Wisselen naar versie voor: ${otherItemEnvironmentLabel.join("/")}</span></label></li>`);
                            editMenu.append(newElement);

                            newElement.find("label").click((event) => {
                                if (!callerWindow) {
                                    // Change the ID of the selected item to that of the selected version, otherwise the save button will overwrite the wrong item.
                                    this.base.selectedItem.id = otherItem.id;
                                    this.base.selectedItem.plainItemId = otherItem.plainId;

                                    // Load the selected version.
                                    this.base.loadItem(otherItem.id, 0, otherItem.entityType);
                                } else {
                                    callerWindow.close();
                                    this.base.windows.loadItemInWindow(false, otherItem.plainItemId, otherItem.id, otherItem.entityType, otherItem.title, callerWindow.element.data("showTitleField"), null, { hideTitleColumn: false }, callerWindow.element.data("linkId"));
                                }
                            });
                        }
                    }
                }

                editMenu.find(".copyToEnvironment").closest("li").toggle(itemMetaData.enableMultipleEnvironments > 0);
                deleteButtons.toggle(itemMetaData.canDelete && !itemMetaData.removed);
                undeleteButtons.toggle(itemMetaData.canDelete && !!itemMetaData.removed); // Double exclamation mark, because jQuery expects a true/false, but removed has a 0 or 1 most of the time.

                $("#alert-first").addClass("hidden");

                metaDataContainer.find(".no-selection").addClass("hidden");

                if (!isForItemWindow && mainFieldsContainer) {
                    // Setup field dependencies.
                    mainFieldsContainer.data("entityType", itemMetaData.entityType);

                    // Setup the overview tab.
                    if (!this.settings.iframeMode) {
                        const entityTypeDetails = await this.base.getEntityType(itemMetaData.entityType);
                        this.base.mainTabStrip.element.find(".overview-tab").toggleClass("hidden", !entityTypeDetails.showOverviewTab);
                    }
                }

                const environmentLabel = this.base.parseEnvironments(itemMetaData.publishedEnvironment);

                let friendlyEntityName = this.getEntityTypeFriendlyName(itemMetaData.entityType);
                if (friendlyEntityName !== itemMetaData.entityType) {
                    friendlyEntityName += ` (${itemMetaData.entityType})`;
                }

                const metaDataListElement = metaDataContainer.find(".meta-data").removeClass("hidden");
                metaDataListElement.find(".id").html(itemMetaData.id);
                metaDataListElement.find(".title").html(itemMetaData.title || "(leeg)");
                metaDataListElement.find(".entity-type").html(friendlyEntityName);
                metaDataListElement.find(".published-environment").html(environmentLabel.join(", "));
                metaDataListElement.find(".read-only").html(itemMetaData.readonly);
                metaDataListElement.find(".added-by").html(itemMetaData.addedBy);

                let addedOn = DateTime.fromISO(itemMetaData.addedOn, { locale: "nl-NL" }).toLocaleString(Dates.LongDateTimeFormat);
                metaDataListElement.find(".added-on").html(addedOn);

                if (itemMetaData.changedOn) {
                    let changedOn = DateTime.fromISO(itemMetaData.changedOn, { locale: "nl-NL" }).toLocaleString(Dates.LongDateTimeFormat);

                    metaDataListElement.find(".changed-on").html(changedOn).closest("li").removeClass("hidden");

                    metaDataListElement.find(".changedon-footer").off("click");
                    metaDataListElement.find(".changedon-footer").on("click", () => {
                        this.base.loadHistoryGrid(itemId, entityType, itemMetaData.moduleId || this.settings.moduleId);
                    });
                } else {
                    metaDataListElement.find(".changed-on").html("").closest("li").addClass("hidden");
                }

                if (itemMetaData.changedBy) {
                    metaDataListElement.find(".changed-by").html(itemMetaData.changedBy).closest("li").removeClass("hidden");
                } else {
                    metaDataListElement.find(".changed-by").html("").closest("li").addClass("hidden");
                }

                if (itemMetaData.uniqueUuid) {
                    metaDataListElement.find(".uuid").html(itemMetaData.uniqueUuid).closest("li").removeClass("hidden");
                } else {
                    metaDataListElement.find(".uuid").html("").closest("li").addClass("hidden");
                }

                if (!isForItemWindow) {
                    window.processing.removeProcess(process);
                }

                return itemMetaData;
            } catch (exception) {
                console.error(`Error while loading meta data for item ${itemId}:`, exception);

                if (!isForItemWindow) {
                    window.processing.removeProcess(process);
                }

                throw exception;
            }
        }

        /**
         * Update the link of an item, to link/move that item to another item.
         * @param {number} linkId The ID of the current link that you want to update.
         * @param {number} newDestinationId The ID of the item where you want to move the item to.
         * @returns {any} A promise with the result of the AJAX call.
         */
        async updateItemLink(linkId, newDestinationId) {
            return Wiser.api({ url: `${this.settings.serviceRoot}/UPDATE_LINK?linkId=${encodeURIComponent(linkId)}&destinationId=${encodeURIComponent(newDestinationId)}` });
        }

        /**
         * Removes a link between two items.
         * @param {string} sourceId The ID of the item that you want to link.
         * @param {string} destinationId The ID of the item that you want to link to.
         * @param {number} linkTypeNumber The link type number.
         * @returns {Promise} A promise with the result of the AJAX call.
         */
        async removeItemLink(sourceId, destinationId, linkTypeNumber) {
            return Wiser.api({
                url: `${this.base.settings.wiserApiRoot}items/remove-links?moduleId=${this.base.settings.moduleId}`,
                method: "DELETE",
                contentType: "application/json",
                data: JSON.stringify({
                    encryptedSourceIds: [sourceId],
                    encryptedDestinationIds: [destinationId],
                    linkType: linkTypeNumber
                })
            });
        }

        /**
         * Creates a new item in the database and executes any workflow for creating an item.
         * @param {string} entityType The type of item to create.
         * @param {string} parentId The (encrypted) ID of the parent to add the new item to.
         * @param {string} name Optional: The name of the new item.
         * @param {number} linkTypeNumber Optional: The type number of the link between the new item and it's parent.
         * @param {any} data Optional: The data to save with the new item.
         * @returns {Object<string, any>} An object with the properties 'itemId', 'icon' and 'workflowResult'.
         * @param {number} moduleId Optional: The id of the module in which the item should be created.
         * @param {bool} alsoCreateInMainBranch Optional: Whether or not to create the item in the main branch as well to match IDs for merging later.
         */
        async createItem(entityType, parentId, name, linkTypeNumber, data = [], skipUpdate = false, moduleId = null, alsoCreateInMainBranch = false) {
            return Wiser.createItem(this.settings, entityType, parentId, name, linkTypeNumber, data, skipUpdate, moduleId, alsoCreateInMainBranch);
        }

        /**
         * Marks an item as deleted.
         * @param {string} encryptedItemId The encrypted item ID.
         * @param {string} entityType The entity type of the item to delete. This is required for workflows.
         * @returns {Promise} A promise with the result of the AJAX call.
         */
        async deleteItem(encryptedItemId, entityType) {
            return Wiser.deleteItem(this.settings, encryptedItemId, entityType);
        }

        /**
         * Marks an item as deleted.
         * @param {string} encryptedItemId The encrypted item ID.
         * @param {string} entityType The entity type of the item to undelete.
         * @returns {Promise} A promise with the result of the AJAX call.
         */
        async undeleteItem(encryptedItemId, entityType) {
            return Wiser.undeleteItem(this.settings, encryptedItemId, entityType);
        }

        /**
         * Duplicates an item (including values of fields, excluding linked items).
         * @param {string} itemId The (encrypted) ID of the item to get the HTML for.
         * @param {string} parentId The (encrypted) ID of the parent item, so that the duplicated item will be linked to the same parent.
         * @param {string} entityType Optional: The entity type of the item to duplicate, so that the API can use the correct table and settings.
         * @param {string} parentEntityType Optional: The entity type of the parent of item to duplicate, so that the API can use the correct table and settings.
         * @returns {Promise} The details about the newly created item.
         */
        async duplicateItem(itemId, parentId, entityType = null, parentEntityType = null) {
            return Wiser.duplicateItem(this.settings, itemId, parentId, entityType, parentEntityType);
        }

        /**
         * Marks an item as deleted.
         * @param {string} encryptedItemId The encrypted item ID.
         * @param {int} newEnvironments The environments to copy this item to.
         * @returns {Promise} A promise with the result of the AJAX call.
         */
        async copyToEnvironment(encryptedItemId, newEnvironments) {
            return Wiser.api({
                url: `${this.base.settings.wiserApiRoot}items/${encodeURIComponent(encryptedItemId)}/copy-to-environment/${newEnvironments}`,
                method: "POST",
                contentType: "application/json",
                dataType: "JSON"
            });
        }

        /**
         * Gets the title of an item
         * @param {string} itemId The encrypted item ID.
         * @returns {Promise} A promise with the results.
         */
        async getTitle(itemId) {
            return Wiser.api({ url: `${this.settings.serviceRoot}/GET_TITLE?itemId=${encodeURIComponent(itemId)}` });
        }

        /**
         * Gets the HTML for an item. This contains all fields and the javascript for those fields.
         * @param {string} itemId The (encrypted) ID of the item to get the HTML for.
         * @param {string} entityType The entity type of the item to get the HTML for.
         * @param {string} propertyIdSuffix Optional: The suffix for property IDs, this is required when opening items in a popup, so that the fields always have unique ID, even if they already exist in the main tab sheet.
         * @param {number} linkId Optional: The ID of the link between this item and another item. If you're opening this item via a specific link, you should enter the ID of that link, because it's possible to have fields/properties on a link instead of an item.
         * @param {number} linkType Optional: The type number of the link between this item and another item. If you're opening this item via a specific link, you should enter the ID of that link, because it's possible to have fields/properties on a link instead of an item.
         * @returns {Promise} A promise with the results.
         */
        async getItemHtml(itemId, entityType, propertyIdSuffix = "", linkId = 0, linkType = 0) {
            let url = `${this.settings.wiserApiRoot}items/${encodeURIComponent(itemId)}?entityType=${encodeURIComponent(entityType)}&encryptedModuleId=${encodeURIComponent(this.base.settings.encryptedModuleId)}`;
            if (propertyIdSuffix) {
                url += `&propertyIdSuffix=${encodeURIComponent(propertyIdSuffix)}`;
            }
            if (linkId) {
                url += `&itemLinkId=${encodeURIComponent(linkId)}`;
            }
            if (linkType) {
                url += `&linkType=${encodeURIComponent(linkType)}`;
            }

            return Wiser.api({ url: url });
        }

        /**
         * Get all properties / fields from a single item.
         * @param {any} itemId The ID of the item to get the details of.
         * @param {string} entityType The entity type of the item.
         * @returns {Promise} A promise, which will return an array with 1 item. That item will contain it's basic properties and a property called "property_" which contains an object with all fields and their values.
         */
        async getItemDetails(itemId, entityType) {
            const entityTypeUrlPart = entityType ? `?entityType=${encodeURIComponent(entityType)}` : "";
            return Wiser.api({ url: `${this.settings.wiserApiRoot}items/${encodeURIComponent(itemId)}/details${entityTypeUrlPart}` });
        }

        /**
         * Get all properties / fields from a single item.
         * @param {any} itemId The ID of the item to get the details of.
         * @returns {Promise} A promise, which will return an array with 1 item. That item will contain it's basic properties and a property called "property_" which contains an object with all fields and their values.
         */
        async getEntityBlock(itemId) {
            return Wiser.api({ url: `${this.settings.wiserApiRoot}items/${encodeURIComponent(itemId)}/block/` });
        }

        /**
         * Get the value of a specific field from a specific item.
         * @param {number} encryptedItemId The ID of the item.
         * @param {string} propertyName The name of the property / field.
         * @returns {Promise} A promise, which will return an array with 1 item. That item will contain information about the property / field, including it's value.
         */
        async getItemValue(encryptedItemId, propertyName) {
            return Wiser.api({ url: `${this.settings.serviceRoot}/GET_ITEM_VALUE?itemId=${encodeURIComponent(encryptedItemId)}&propertyName=${encodeURIComponent(propertyName)}` });
        }

        /**
         * Get all meta data from a single item.
         * @param {string} itemId The ID of the item to get the meta data of.
         * @param {string} itemId The entity type of the item to get the meta data of.
         * @returns {Promise} A promise, which will return an array with 1 item. That item will contain it's basic properties and a property called "property_" which contains an object with all fields and their values.
         */
        async getItemMetaData(itemId, entityType) {
            const entityTypeUrlPart = entityType ? `?entityType=${encodeURIComponent(entityType)}` : "";
            return Wiser.api({ url: `${this.settings.wiserApiRoot}items/${encodeURIComponent(itemId)}/meta${entityTypeUrlPart}` });
        }

        /**
         * Get all versions of an item for different environments.
         * @param {any} mainItemId The ID of the main/original item.
         * @returns {Promise} A promise, which will return an array with the results.
         */
        async getItemEnvironments(mainItemId) {
            return Wiser.api({ url: `${this.settings.serviceRoot}/GET_ITEM_ENVIRONMENTS?mainItemId=${encodeURIComponent(mainItemId)}` });
        }

        /**
         * Gets all available entity types that can be added as a child to the given parent.
         * @param {string} parentId The (encrypted) ID of the parent to get the available entity types of.
         * @return {any} An array with all the available entity types.
         */
        async getAvailableEntityTypes(parentId) {
            return await Wiser.api({ url: `${this.base.settings.wiserApiRoot}entity-types/${encodeURIComponent(this.settings.moduleId)}?parentId=${encodeURIComponent(parentId)}` });
        }

        /**
         * Get the details for an entity type.
         * @param {string} name The name of the entity type.
         * @param {number} moduleId The ID of the module (different modules can have entity types with the same name).
         * @returns {Promise} A promise with the results.
         */
        async getEntityType(name, moduleId = 0) {
            const sessionStorageKey = `wiserEntityTypeInformation${name}${moduleId}`;
            let result = sessionStorage.getItem(sessionStorageKey);
            if (result) {
                return JSON.parse(result);
            }

            result = await Wiser.api({ url: `${this.base.settings.wiserApiRoot}entity-types/${encodeURIComponent(name)}?moduleId=${moduleId}` });
            sessionStorage.setItem(sessionStorageKey, JSON.stringify(result));
            return result;
        }

        /**
         * Gets a certain API action for a certain entity type. This will return the ID that can be used for executing an API action.
         * @param {string} actionType The type of action to get. Possible values: "after_insert", "after_update", "before_update" and "before_delete".
         * @param {string} entityType The name of the entity type to get the action for.
         * @returns {number} The ID of the API action, or 0 if there is no action set.
         */
        async getApiAction(actionType, entityType) {
            return Wiser.getApiAction(this.settings, actionType, entityType);
        }

        /**
         * Gets the friendly name of the specified entity type. If there is no friendly name, the input will be returned.
         * @param {string} entityType The name of the entity type to get the friendly name of.
         * @param {number} moduleId Optional: If an entity exists in multiple modules, you can enter the ID of the module here. Default value is the ID of the currently opened module.
         * @returns {string} The friendly name to show to the user.
         */
        getEntityTypeFriendlyName(entityType, moduleId = 0) {
            if (!entityType) {
                return entityType;
            }

            moduleId = moduleId || this.settings.moduleId;

            let entityTypes = this.allEntityTypes.filter(e => e.id === entityType);
            if (entityTypes.length === 0) {
                return entityType;
            }

            if (entityTypes.length === 1 || !moduleId) {
                return entityTypes[0].displayName || entityType;
            }

            const entityTypeForModule = entityTypes.find(e => e.moduleId === moduleId);
            if (!entityTypeForModule) {
                return entityTypes[0].displayName || entityType;
            }

            return entityTypeForModule.displayName || entityType;
        }
    }

    // Initialize the DynamicItems class and make one instance of it globally available.
    window.dynamicItems = new DynamicItems(settings);
})(moduleSettings);
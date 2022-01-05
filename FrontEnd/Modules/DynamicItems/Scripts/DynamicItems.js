﻿import { TrackJS } from "trackjs";
import { Modules, Dates, Wiser2 } from "../../Base/Scripts/Utils.js";
import { DateTime } from "luxon";
import { Fields } from "./Fields.js";
import { Dialogs } from "./Dialogs.js";
import { Windows } from "./Windows.js";
import { Grids } from "./Grids.js";
import { DragAndDrop } from "./DragAndDrop.js";
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
require("@progress/kendo-ui/js/cultures/kendo.culture.nl-NL.js");
require("@progress/kendo-ui/js/messages/kendo.messages.nl-NL.js");

import "../Css/DynamicItems.css";

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

            this.newItemId = null;
            this.selectedItem = null;
            this.selectedItemTitle = null;

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

            // Create instances of sub classes.
            this.dragAndDrop = new DragAndDrop(this);
            this.dialogs = new Dialogs(this);
            this.windows = new Windows(this);
            this.grids = new Grids(this);
            this.fields = new Fields(this);
            
            // Add logged in user access token to default authorization headers for all jQuery ajax requests.
            $.ajaxSetup({
                headers: { "Authorization": `Bearer ${localStorage.getItem("access_token")}` }
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

            // Setup JJL processing.
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
                
            const userData = await Wiser2.getLoggedInUserData(this.settings.wiserApiRoot, this.settings.isTestEnvironment);
            this.settings.userId = userData.encrypted_id;
            this.settings.customerId = userData.encrypted_customer_id;
            this.settings.zeroEncrypted = userData.zero_encrypted;
            this.settings.filesRootId = userData.files_root_id;
            this.settings.imagesRootId = userData.images_root_id;
            this.settings.templatesRootId = userData.templates_root_id;
            this.settings.mainDomain = userData.main_domain;

            if (!this.settings.wiserApiRoot.endsWith("/")) {
                this.settings.wiserApiRoot += "/";
            }
            
            this.settings.serviceRoot = `${this.settings.wiserApiRoot}templates/get-and-execute-query`;
            this.settings.htmlEditorCssUrl = `${this.settings.wiserApiRoot}templates/css-for-html-editors?encryptedCustomerId=${encodeURIComponent(this.base.settings.customerId)}&isTest=${this.base.settings.isTestEnvironment}&encryptedUserId=${encodeURIComponent(this.base.settings.userId)}&username=${encodeURIComponent(this.base.settings.username)}&userType=${encodeURIComponent(this.base.settings.userType)}&subDomain=${encodeURIComponent(this.base.settings.subDomain)}`

            const extraModuleSettings = await Modules.getModuleSettings(this.settings.wiserApiRoot, this.settings.moduleId, this.settings.customerId, this.settings.userId, this.settings.isTestEnvironment, this.settings.subDomain);
            Object.assign(this.settings, extraModuleSettings.options);
            let permissions = Object.assign({}, extraModuleSettings);
            delete permissions.options;
            this.settings.permissions = permissions;
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
                jjl.processing.addProcess(process);
                const newItemResult = await this.createItem(this.settings.entityType, this.settings.parentId || this.settings.zeroEncrypted, "", 1, this.settings.newItemData || [], false, this.settings.moduleId);
                this.settings.initialItemId = newItemResult.itemId;
                await this.loadItem(newItemResult.itemId, 0, newItemResult.entity_type);
                jjl.processing.removeProcess(process);
            } else if (this.settings.initialItemId) {
                this.loadItem(this.settings.initialItemId, 0, this.settings.entityType);
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
            // Do stuff when the module is being closed in Wiser 1.0.
            $(document).on("moduleClosing", (event) => {
                try {
                    const kendoWindows = $(".popup-container:not(#itemWindow_template)");
                    if (!kendoWindows.length) {
                        event.success();
                        return;
                    }

                    var promises = [];
                    kendoWindows.each((index, element) => {
                        // If the current item is a new item and it's not being saved at the moment, then delete it because it was a temporary item.
                        if (!$(element).data("isNewItem") || $(element).data("saving")) {
                            return;
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
                    });

                    Promise.all(promises).then(event.success);
                } catch (exception) {
                    console.error(exception);
                    // To make sure the module can always be closed.
                    event.success();
                }
            });

            // Keyboard shortcuts
            $("body").on("keyup", (event) => {
                const target = $(event.target);

                if (target.prop("tagName") === "INPUT" || target.prop("tagName") === "TEXTAREA") {
                    return;
                }

                switch (event.key) {
                    case "N":
                    {
                        const addButton = $("#addButton");
                        if (event.shiftKey && addButton.is(":visible")) {
                            addButton.click();
                        }
                        break;
                    }
                }
            });

            // Binding to unselect the main tree view.
            $("#left-pane, .k-window-titlebar").click((event) => {
                var target = $(event.target);

                if (target.hasClass("k-in") || target.hasClass("k-i-expand") || target.prop("tagName") === "BUTTON" || target.prop("tagName") === "INPUT" || (target.closest(".k-window-titlebar").length > 0 && target.siblings("#window").length === 0)) {
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

                this.dialogs.loadAvailableEntityTypesInDropDown(this.settings.zeroEncrypted);

                $("#alert-first").removeClass("hidden");
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

            $("body").on("click", ".imgZoom", function () {
                const image = $(this).parents(".product").find("img");
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

            $("body").on("click", ".imgEdit", function () {
                const image = $(this).parents(".product").find("img");
                kendo.alert("Deze functionaliteit is nog niet geïmplementeerd");
            });

            $("body").on("click", ".imgTools .imgDelete", this.fields.onImageDelete.bind(this.fields));

            $("#mainScreenForm").submit(event => { event.preventDefault(); });

            $("#mainEditMenu .reloadItem").click(async (event) => {
                const previouslySelectedTab = this.mainTabStrip.select().index();
                this.loadItem(this.settings.iframeMode ? this.settings.initialItemId : this.selectedItem.id, previouslySelectedTab, this.settings.iframeMode ? this.settings.entityType : this.selectedItem.entity_type);
            });

            $("#mainEditMenu .deleteItem").click(async (event) => {
                this.onDeleteItemClick(event, this.settings.iframeMode ? this.settings.initialItemId : this.selectedItem.id, this.settings.iframeMode ? this.settings.entityType : this.selectedItem.entity_type);
            });

            $("#mainEditMenu .undeleteItem").click(async (event) => {
                this.onUndeleteItemClick(event, this.settings.iframeMode ? this.settings.initialItemId : this.selectedItem.id);
            });

            $("#mainEditMenu .copyToEnvironment").click(async (event) => {
                this.dialogs.copyItemToEnvironmentDialog.element.find("input[type=checkbox]").prop("checked", false);
                this.dialogs.copyItemToEnvironmentDialog.element.data("id", this.selectedItemMetaData.plain_original_item_id);
                this.dialogs.copyItemToEnvironmentDialog.open();
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
                stacking: "down",
                position: {
                    top: 0,
                    left: 0,
                    right: 0,
                    bottom: null,
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
                messages: {
                    required: (input) => {
                        const fieldDisplayName = $(input).closest(".item").find("> h4 > label").text() || $(input).attr("name");
                        return `${fieldDisplayName} is verplicht`;
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
                            Wiser2.api({
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
                            id: "encrypted_item_id",
                            hasChildren: "has_children"
                        }
                    }
                },
                dataBound: this.onTreeViewDataBound.bind(this),
                select: this.onTreeViewItemClick.bind(this),
                collapse: this.onTreeViewCollapseItem.bind(this),
                expand: this.onTreeViewExpandItem.bind(this),
                drop: this.onTreeViewDropItem.bind(this),
                drag: this.onTreeViewDragItem.bind(this),
                dataValueField: "encrypted_item_id",
                dataTextField: "title",
                dataSpriteCssClassField: "sprite_css_class"
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
            }
            if (scriptTemplate.indexOf("kendoEditor") > -1) {
                await require("@progress/kendo-ui/js/kendo.editor.js");
            }
            if (scriptTemplate.indexOf("kendoNumericTextBox") > -1) {
                await require("@progress/kendo-ui/js/kendo.numerictextbox.js");
            }
            if (scriptTemplate.indexOf("kendoMultiSelect") > -1) {
                await require("@progress/kendo-ui/js/kendo.multiselect.js");
            }
            if (scriptTemplate.indexOf("kendoScheduler") > -1) {
                await require("@progress/kendo-ui/js/kendo.scheduler.js");
            }
            if (scriptTemplate.indexOf("kendoTimeline") > -1) {
                await require("@progress/kendo-ui/js/kendo.timeline.js");
            }
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
            const tabSheet = tabStrip.element.find(`[name=${fieldName}]`).closest("div[role=tabpanel]");
            tabStrip.select(tabSheet.index() - 1); // -1 because the first element is the <ul> with tab names, which we don't want to count.
        }

        /**
         * Event that gets fired when the users opens the context menu.
         * @param {any} event The context open event.
         */
        async onContextMenuOpen(event) {
            try {
                const nodeId = this.mainTreeView.dataItem(event.target).id;
                let contextMenu = await Wiser2.api({ url: `${this.base.settings.serviceRoot}/GET_CONTEXT_MENU?module_id=${encodeURIComponent(this.base.settings.moduleId)}&item_id=${encodeURIComponent(nodeId)}` });
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
            try {
                const button = $(event.item);
                const node = $(event.target);
                const treeView = this.base.mainTreeView;
                const dataItem = treeView.dataItem(event.target);
                // For some reason the JCL already encodes the values, so decode them here, otherwise they will be encoded twice in some cases, which can cause problems.
                const itemId = decodeURIComponent(dataItem.id);
                const action = button.attr("action");
                const entityType = button.attr("entity_type");

                switch (action) {
                    case "RENAME_ITEM":
                        {
                            kendo.prompt("Vul een nieuwe naam in", node.text()).done((newName) => {
                                this.base.updateItem(itemId, [], null, false, newName, false, true, entityType).then(() => {
                                    this.base.notification.show({ message: "Succesvol gewijzigd" }, "success");
                                    treeView.text(node, newName);
                                    $("#right-pane input[name='_nameForExistingItem']").val(newName);
                                });
                            }).fail(() => {});
                            break;
                        }
                    case "CREATE_ITEM":
                        {
                            this.base.dialogs.openCreateItemDialog(itemId, node, entityType);
                            break;
                        }
                    case "DUPLICATE_ITEM":
                        {
                            // Duplicate the item.
                            // For some reason the JCL already encodes the values, so decode them here, otherwise they will be encoded twice in some cases, which can cause problems.
                            const parentId = decodeURIComponent(dataItem.destination_item_id || this.base.settings.zeroEncrypted);
                            const parentItem = treeView.dataItem(this.base.mainTreeView.parent(node));
                            const duplicateItemResults = await this.base.duplicateItem(itemId, parentId, dataItem.entity_type, parentItem ? parentItem.entity_type : "");
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
                            await kendo.confirm("Weet u zeker dat u dit item wilt verwijderen?");

                            try {
                                await this.base.deleteItem(itemId, entityType);
                                this.base.mainTreeView.remove(node);
                            } catch (exception) {
                                console.error(exception);
                                if (exception.status === 409) {
                                    const message = exception.responseText || "Het is niet meer mogelijk om dit item te verwijderen.";
                                    kendo.alert(message);
                                } else {
                                    kendo.alert("Er is iets fout gegaan tijdens het verwijderen van dit item. Probeer het a.u.b. nogmaals of neem contact op met ons.");
                                }
                            }
                            break;
                        }
                    case "HIDE_ITEM":
                        {
                            await Wiser2.api({ url: `${this.settings.serviceRoot}/${encodeURIComponent(action)}?itemid=${encodeURIComponent(itemId)}` });
                            node.closest("li").addClass("hiddenOnWebsite");
                            window.dynamicItems.notification.show({ message: "Item is verborgen" }, "success");
                            break;
                        }
                    case "PUBLISH_LIVE":
                    case "PUBLISH_ITEM":
                        {
                            await Wiser2.api({ url: `${this.settings.serviceRoot}/${encodeURIComponent(action)}?itemid=${encodeURIComponent(itemId)}` });
                            node.closest("li").removeClass("hiddenOnWebsite");
                            window.dynamicItems.notification.show({ message: "Item is zichtbaar gemaakt" }, "success");
                            break;
                        }
                    default:
                        {
                            await Wiser2.api({ url: `${this.settings.serviceRoot}/${encodeURIComponent(action)}?itemid=${encodeURIComponent(itemId)}` });
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
                if (this.selectedItem || this.settings.initialItemId) {
                    const previouslySelectedTab = this.mainTabStrip.select().index();
                    this.loadItem(this.settings.iframeMode ? this.settings.initialItemId : this.selectedItem.id, previouslySelectedTab, this.settings.iframeMode ? this.settings.entityType : this.selectedItem.entity_type);
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
            jjl.processing.addProcess(process);

            try {
                const itemId = this.selectedItem && this.selectedItem.id ? this.selectedItem.id : this.settings.initialItemId;
                const inputData = this.base.fields.getInputData($("#right-pane-content, .dynamicTabContent"));
                const title = $("#tabstrip .itemNameFieldContainer .itemNameField").val();

                if (!this.mainValidator.validate()) {
                    jjl.processing.removeProcess(process);
                    return false;
                }
                
                const updateItemResult = await this.base.updateItem(itemId, inputData, $("#right-pane"), false, title, true, true, this.selectedItem && this.selectedItem.entity_type ? this.selectedItem.entity_type : this.selectedItemMetaData.entity_type);
                document.dispatchEvent(new CustomEvent("dynamicItems.onSaveButtonClick", { detail: updateItemResult }));
                if (window.parent && window.parent.document) {
                    window.parent.document.dispatchEvent(new CustomEvent("dynamicItems.onSaveButtonClick", { detail: updateItemResult }));
                }
                
                jjl.processing.removeProcess(process);
                return true;
            } catch (exception) {
                console.error(exception);
                if (exception.status === 409) {
                    const message = exception.responseText || "Het is niet meer mogelijk om aanpassingen te maken in dit item.";
                    kendo.alert(message);
                } else {
                    kendo.alert("Er is iets fout gegaan tijdens het opslaan van dit item. Probeer het a.u.b. nogmaals of neem contact op met ons.");
                }
                
                jjl.processing.removeProcess(process);
                return false;
            }
        }
        
        /**
         * Event that gets called once a notification is being shown.
         */
        onShowNotification(event) {
            event.element.parent().css("width", "100%").css("height", "auto");
        }

        /**
         * Handles the main tree view data bound event.
         * @param {any} event The Kendo data bound event.
         */
        onTreeViewDataBound(event) {
            (event.node || event.sender.element).find("li").each((index, element) => {
                const dataItem = event.sender.dataItem(element);
                if (!dataItem.node_css_class) {
                    return;
                }

                $(element).addClass(dataItem.node_css_class);
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
            // This used to be done in the query that gets the items for the tree view, but that made the query really slow for some customers, so now we do it here.
            if (dataItem.plain_original_item_id > 0) {
                let itemToUse = null;
                const itemEnvironments = await this.getItemEnvironments(dataItem.encrypted_original_item_id);
                if (itemEnvironments && itemEnvironments.length) {
                    for (let itemVersion of itemEnvironments) {
                        if (itemVersion.changed_on) {
                            itemVersion.changed_on = new Date(itemVersion.changed_on);
                        }

                        if (!itemToUse || itemVersion.changed_on > itemToUse.changed_on) {
                            itemToUse = itemVersion;
                        }
                    }
                }

                if (itemToUse) {
                    itemId = itemToUse.id;
                    // Change the ID of the selected item, otherwise the save button will overwrite the wrong item.
                    this.base.selectedItem.id = itemId;
                    this.base.selectedItem.plain_item_id = itemToUse.plainItemId;
                }
            }

            // Set the correct values in the crumb trail.
            const crumbTrail = $("#crumbTrail").empty();
            const parents = $(event.node).add($(event.node).parentsUntil(".k-treeview", ".k-item"));
            const amountOfItems = parents.length;
            let counter = 0;

            const texts = $.map(parents, (node) => {
                counter++;

                const text = $(node).find(">div span.k-in").text();
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

            await this.base.loadItem(itemId, 0, dataItem.entity_type || dataItem.entityType);

            // Get available entity types, for creating new sub items.
            this.base.dialogs.loadAvailableEntityTypesInDropDown(itemId);
        }

        /**
         * Event for when an item in a kendoTreeView gets collapsed.
         * @param {any} event The collapsed event of a kendoTreeView.
         */
        onTreeViewCollapseItem(event) {
            const dataItem = event.sender.dataItem(event.node);
            dataItem.sprite_css_class = dataItem.collapsed_sprite_css_class;

            // Changing the text causes kendo to actually update the icon. If we don't change the test, the icon will not change.
            event.sender.text(event.node, event.sender.text(event.node).trim());
        }

        /**
         * Event for when an item in a kendoTreeView gets expanded.
         * @param {any} event The expanded event of a kendoTreeView.
         */
        onTreeViewExpandItem(event) {
            const dataItem = event.sender.dataItem(event.node);
            dataItem.sprite_css_class = dataItem.expanded_sprite_css_class || dataItem.collapsed_sprite_css_class;

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

            if ((destinationDataItem.accepted_child_types || "").toLowerCase().split(",").indexOf(sourceDataItem.entity_type.toLowerCase()) === -1) {
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
                
                const moveItemResult = await Wiser2.api({
                    url: `${this.base.settings.wiserApiRoot}items/${encodeURIComponent(sourceDataItem.id)}/move/${encodeURIComponent(destinationDataItem.id)}`,
                    method: "PUT",
                    contentType: "application/json",
                    data: JSON.stringify({
                        position: event.dropPosition,
                        encrypted_source_parent_id: sourceDataItem.destination_item_id,
                        encrypted_destination_parent_id: destinationDataItem.destination_item_id,
                        source_entity_type: sourceDataItem.entity_type,
                        destination_entity_type: destinationDataItem.entity_type,
                        module_id: this.base.settings.moduleId
                    })
                });
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
         */
        async loadHistoryGrid(itemId) {
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

                const gridDataResult = await Wiser2.api({
                    url: `${this.base.settings.wiserApiRoot}items/${encodeURIComponent(itemId)}/entity-grids/history?mode=3&moduleId=${this.base.settings.moduleId}`,
                    method: "POST",
                    contentType: "application/json",
                    data: JSON.stringify(options)
                });

                if (gridDataResult.extra_javascript) {
                    $.globalEval(gridDataResult.extra_javascript);
                }

                this.windows.historyGridFirstLoad = true;

                let previousFilters = null;
                let totalResults = gridDataResult.total_results;

                this.windows.historyGrid = historyGridElement.kendoGrid({
                    dataSource: {
                        serverPaging: true,
                        serverSorting: true,
                        serverFiltering: true,
                        pageSize: gridDataResult.page_size,
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

                                    transportOptions.data.first_load = currentFilters !== previousFilters;
                                    transportOptions.data.page_size = transportOptions.data.pageSize;
                                    previousFilters = currentFilters;

                                    const newGridDataResult = await Wiser2.api({
                                        url: `${this.base.settings.wiserApiRoot}items/${encodeURIComponent(itemId)}/entity-grids/history?mode=3&moduleId=${this.base.settings.moduleId}`,
                                        method: "POST",
                                        contentType: "application/json",
                                        data: JSON.stringify(transportOptions.data)
                                    });

                                    if (typeof newGridDataResult.total_results !== "number" || !transportOptions.data.first_load) {
                                        newGridDataResult.total_results = totalResults;
                                    } else if (transportOptions.data.first_load) {
                                        totalResults = newGridDataResult.total_results;
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
                            total: "total_results",
                            model: gridDataResult.schema_model
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

                const customColumns = await Wiser2.api({ url: `${this.settings.serviceRoot}/GET_COLUMNS_FOR_TABLE?itemId=${encodeURIComponent(itemId)}` });
                const model = {
                    id: "id",
                    fields: {
                        id: {
                            type: "number"
                        },
                        published_environment: {
                            type: "string"
                        },
                        title: {
                            type: "string"
                        },
                        entity_type: {
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
                        field: "entity_type",
                        title: "Type",
                        width: 100
                    },
                    {
                        template: "<ins title='#: published_environment #' class='icon-#: published_environment #'></ins>",
                        field: "published_environment",
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
                                    const results = await Wiser2.api({ url: `${this.settings.serviceRoot}/GET_DATA_FOR_TABLE?itemId=${encodeURIComponent(itemId)}` });
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
         * The click event for the undelete button.
         * @param {any} event The click event.
         * @param {string} encryptedItemId The encrypted ID of the item to undelete.
         */
        async onDeleteItemClick(event, encryptedItemId, entityType) {
            event.preventDefault();
            
            await kendo.confirm("Weet u zeker dat u dit item wilt verwijderen?");

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

            await kendo.confirm("Weet u zeker dat u het verwijderen ongedaan wilt maken voor dit item?");

            const process = `undeleteItem_${Date.now()}`;
            jjl.processing.addProcess(process);

            const popupWindowContainer = $(event.currentTarget).closest(".popup-container");

            try {
                popupWindowContainer.find(".popup-loader").addClass("loading");
                popupWindowContainer.data("saving", true);

                let entityType = popupWindowContainer.data("entityTypeDetails");

                if (Wiser2.validateArray(entityType)) {
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

            jjl.processing.removeProcess(process);
            popupWindowContainer.find(".popup-loader").removeClass("loading");
            popupWindowContainer.data("saving", false);
        }

        /**
         * Load a specific item in the main container / tab strip.
         * @param {any} itemId The ID of the item to load.
         * @param {number} tabToSelect Optional: The tab index to initially open after the item has been loaded. Default is 0.
         */
        async loadItem(itemId, tabToSelect = 0, entityType = null) {
            const process = `loadItem_${Date.now()}`;
            jjl.processing.addProcess(process);

            // Set meta data of the selected item in the footer.
            try {
                const itemMetaData = await this.base.addItemMetaData(itemId, entityType, $("#metaData"), false, $("#right-pane .entity-container"));
                if (!itemMetaData) {
                    console.warn("No meta data found for item, the user probably doesn't have rights for it anymore.");
                    jjl.processing.removeProcess(process);
                    return;
                }

                let entityTypeSettings = await this.base.getEntityType(itemMetaData.entity_type);
                if (Wiser2.validateArray(entityTypeSettings)) {
                    entityTypeSettings = entityTypeSettings[0];
                }
                this.selectedItemTitle = itemMetaData.title;
                this.selectedItemMetaData = itemMetaData;
                const itemTitleFieldContainer = $("#tabstrip .itemNameFieldContainer");
                itemTitleFieldContainer.find(".itemNameField").val(itemMetaData.title);
                itemTitleFieldContainer.toggle(entityTypeSettings.show_title_field && this.base.settings.iframeMode);

                // Set the HTML of the fields tab.
                const itemHtmlResult = await this.getItemHtml(itemId, itemMetaData.entity_type);

                this.mainTabStrip.element.find("> ul > li .addedFromDatabase").each((index, element) => {
                    this.mainTabStrip.remove($(element).closest("li.k-item"));
                });

                // Reset the main screen field initializers so that they don't stay in memory. We don't need them anymore once we load a new item.
                this.base.fields.fieldInitializers.mainScreen = {};

                // Load the HTML and javascript for all fields of every tab.
                // Only the javascript of the first tab will be executed right away, the rest will be done if and when the user switches to that tab.
                const container = $("#right-pane-content").html("");

                // Handle access rights.
                itemTitleFieldContainer.prop("readonly", !itemHtmlResult.can_write).prop("disabled", !itemHtmlResult.can_write);
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
                        element.toggleClass("hidden", !itemHtmlResult.can_write || !element.data("shown-via-parent"));
                    } else {
                        element.toggleClass("hidden", !itemHtmlResult.can_write);
                    }
                });

                let genericTabHasFields = false;
                for (let i = itemHtmlResult.tabs.length - 1; i >= 0; i--) {
                    const tabData = itemHtmlResult.tabs[i];
                    if (!tabData.name) {
                        genericTabHasFields = true;
                        container.html(tabData.html_template);
                        await this.base.loadKendoScripts(tabData.script_template);
                        $.globalEval(tabData.script_template);
                    } else {
                        this.mainTabStrip.insertAfter({
                            text: tabData.name,
                            content: "<div class='dynamicTabContent'>" + tabData.html_template + "</div>",
                            spriteCssClass: "addedFromDatabase"
                        }, this.mainTabStrip.tabGroup.children().eq(0));

                        this.base.fields.fieldInitializers.mainScreen[tabData.name] = {
                            executed: false,
                            script: tabData.script_template,
                            entityType: itemMetaData.entity_type
                        };
                    }
                }

                // Setup dependencies for all tabs.
                for (let i = itemHtmlResult.tabs.length - 1; i >= 0; i--) {
                    const tabData = itemHtmlResult.tabs[i];
                    const container = this.mainTabStrip.contentHolder(i);
                    this.base.fields.setupDependencies(container, itemMetaData.entity_type, tabData.name || "Gegevens");
                }

                // Handle dependencies for the first tab, to make sure all the correct fields are hidden/shown on the first tab. The other tabs will be done once they are opened.
                this.base.fields.handleAllDependenciesOfContainer(this.mainTabStrip.contentHolder(0), itemMetaData.entity_type, "Gegevens", "mainScreen");
                
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
                    this.onTabStripSelect((!this.selectedItem || !this.selectedItem.id ? 0 : this.selectedItem.id), "mainScreen", { item: this.mainTabStrip.select(), contentElement: this.mainTabStrip.contentElement(this.mainTabStrip.select().index()) });
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

            jjl.processing.removeProcess(process);
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
            const updateItemData = {
                title: title,
                details: inputData,
                changed_by: this.settings.username,
                entity_type: entityType
            };

            if (executeWorkFlow) {
                const apiActionId = await this.getApiAction("before_update", entityType);
                if (apiActionId) {
                    await Wiser2.doApiCall(this.settings, apiActionId, updateItemData);
                }
            }

            return Wiser2.api({
                url: `${this.base.settings.wiserApiRoot}items/${encodeURIComponent(encryptedItemId)}?isNewItem=${!!isNewItem}`,
                method: "PUT",
                contentType: "application/json",
                dataType: "JSON",
                data: JSON.stringify(updateItemData)
            }).then((updateResult) => {
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

                            const field = fieldsContainer.find(`[name=${itemDetail.key}]`);
                            if (field.is(":disabled,[readonly]")) {
                                field.val(itemDetail.value);
                            }
                        });
                    }
                }

                // Check if we need to execute any API action and do that.
                try {
                    if (executeWorkFlow) {
                        this.getApiAction("after_update", updateResult.entity_type).then((apiActionId) => {
                            if (apiActionId) {
                                Wiser2.doApiCall(this.settings, apiActionId, updateResult).then(() => {
                                    if (showSuccessMessage) {
                                        this.notification.show({ message: "Opslaan is gelukt" }, "success");
                                    }
                                }).catch((error) => {
                                    console.error(error);
                                    kendo.alert("Er is iets fout gegaan tijdens het uitvoeren (of opzoeken) van de actie 'api_after_update'. Indien er een koppeling is opgezet met een extern systeem, dan zijn de wijzigingen nu niet gesynchroniseerd naar dat systeem. Probeer het a.u.b. nogmaals, of neem contact op met ons.");
                                });
                            }
                        });
                    } else if (showSuccessMessage) {
                        this.notification.show({ message: "Opslaan is gelukt" }, "success");
                    }
                } catch(exception) {
                    console.error(exception);
                    kendo.alert("Er is iets fout gegaan tijdens het uitvoeren (of opzoeken) van de actie 'api_after_update'. Indien er een koppeling is opgezet met een extern systeem, dan zijn de wijzigingen nu niet gesynchroniseerd naar dat systeem. Probeer het a.u.b. nogmaals, of neem contact op met ons.");
                }
            });
        }

        /**
         * Loads the meta data of an item and add it to the given meta data container.
         * @param {string} itemId The encrypted ID of the item.
         * @param {string} itemId The entity type of the item.
         * @param {any} metaDataContainer The container that contains the list (<ul>) with meta data values.
         * @param {boolean} isForItemWindow Optional: Indicates whether or not the current item is being loaded in a window. If it's not, some extra things will be done such as setting up dependencies on fields.
         * @param {any} mainFieldsContainer Optional: The container that contains all the fields of the item. Required if isForItemWindow is set to false.
         * @param {any} callerWindow Optional: The window that called this method. Is required when opening items in a new window, so that we know in which window to load an item if the user switches environments.
         * @returns {any} A promise with the result of the AJAX call.
         */
        async addItemMetaData(itemId, entityType, metaDataContainer, isForItemWindow = false, mainFieldsContainer = null, callerWindow = null) {
            const process = `loadMetaData_${Date.now()}`;
            if (!isForItemWindow) {
                jjl.processing.addProcess(process);
            }

            try {
                const itemMetaData = await this.getItemMetaData(itemId, entityType);
                if (!itemMetaData) {
                    console.warn("No meta data found for item, the user probably doesn't have rights for it anymore.");
                    jjl.processing.removeProcess(process);
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
                
                saveButtons.toggleClass("hidden", !itemMetaData.can_write);

                // If there are still options for switching environment, remove them and re-add them, because they are probably left overs of a different item.
                editMenu.find("li.otherEnvironment").remove();
                if (itemMetaData.plain_original_item_id) {
                    const itemEnvironments = await this.getItemEnvironments(itemMetaData.plain_original_item_id);
                    if (itemEnvironments && itemEnvironments.length) {
                        for (let otherItem of itemEnvironments) {
                            if (otherItem.plainItemId === itemMetaData.id) {
                                continue;
                            }

                            const otherItemEnvironmentLabel = this.base.parseEnvironments(otherItem.published_environment);
                            const newElement = $(`<li class="otherEnvironment"><label><span>Wisselen naar versie voor: ${otherItemEnvironmentLabel.join("/")}</span></label></li>`);
                            editMenu.append(newElement);

                            newElement.find("label").click((event) => {
                                if (!callerWindow) {
                                    // Change the ID of the selected item to that of the selected version, otherwise the save button will overwrite the wrong item.
                                    this.base.selectedItem.id = otherItem.id;
                                    this.base.selectedItem.plain_item_id = otherItem.plainId;

                                    // Load the selected version.
                                    this.base.loadItem(otherItem.id, 0, otherItem.entity_type);
                                } else {
                                    callerWindow.close();
                                    this.base.windows.loadItemInWindow(false, otherItem.plainItemId, otherItem.id, otherItem.entity_type, otherItem.title, callerWindow.element.data("showTitleField"), null, { hideTitleColumn: false }, callerWindow.element.data("linkId"));
                                }
                            });
                        }
                    }
                }

                editMenu.find(".copyToEnvironment").closest("li").toggle(itemMetaData.enable_multiple_environments > 0);
                deleteButtons.toggle(itemMetaData.can_delete && !itemMetaData.removed);
                undeleteButtons.toggle(itemMetaData.can_delete && !!itemMetaData.removed); // Double exclamation mark, because jQuery expects a true/false, but removed has a 0 or 1 most of the time.

                $("#alert-first").addClass("hidden");

                metaDataContainer.find(".no-selection").addClass("hidden");

                if (!isForItemWindow && mainFieldsContainer) {
                    // Setup field dependencies.
                    mainFieldsContainer.data("entityType", itemMetaData.entity_type);

                    // Setup the overview tab.
                    if (!this.settings.iframeMode) {
                        const entityTypeDetails = await this.base.getEntityType(itemMetaData.entity_type);
                        this.base.mainTabStrip.element.find(".overview-tab").toggleClass("hidden", !entityTypeDetails[0].show_overview_tab);
                    }
                }

                const environmentLabel = this.base.parseEnvironments(itemMetaData.published_environment);

                const metaDataListElement = metaDataContainer.find(".meta-data").removeClass("hidden");
                metaDataListElement.find(".id").html(itemMetaData.id);
                metaDataListElement.find(".title").html(itemMetaData.title || "(leeg)");
                metaDataListElement.find(".entity-type").html(itemMetaData.entity_type);
                metaDataListElement.find(".published-environment").html(environmentLabel.join(", "));
                metaDataListElement.find(".read-only").html(itemMetaData.readonly);
                metaDataListElement.find(".added-by").html(itemMetaData.added_by);

                let addedOn = DateTime.fromISO(itemMetaData.added_on, { locale: "nl-NL" }).toLocaleString(Dates.LongDateTimeFormat);
                metaDataListElement.find(".added-on").html(addedOn);
            
                if (itemMetaData.changed_on) {
                    let changedOn = DateTime.fromISO(itemMetaData.changed_on, { locale: "nl-NL" }).toLocaleString(Dates.LongDateTimeFormat);
                        
                    metaDataListElement.find(".changed-on").html(changedOn).closest("li").removeClass("hidden");

                    metaDataListElement.find(".changedon-footer").off("click");
                    metaDataListElement.find(".changedon-footer").on("click", () => {
                        this.base.loadHistoryGrid(itemId);
                    });
                } else {
                    metaDataListElement.find(".changed-on").html("").closest("li").addClass("hidden");
                }

                if (itemMetaData.changed_by) {
                    metaDataListElement.find(".changed-by").html(itemMetaData.changed_by).closest("li").removeClass("hidden");
                } else {
                    metaDataListElement.find(".changed-by").html("").closest("li").addClass("hidden");
                }

                if (itemMetaData.unique_uuid) {
                    metaDataListElement.find(".uuid").html(itemMetaData.unique_uuid).closest("li").removeClass("hidden");
                } else {
                    metaDataListElement.find(".uuid").html("").closest("li").addClass("hidden");
                }
                
                if (!isForItemWindow) {
                    jjl.processing.removeProcess(process);
                }

                return itemMetaData;
            } catch (exception) {
                console.error(`Error while loading meta data for item ${itemId}:`, exception);
                
                if (!isForItemWindow) {
                    jjl.processing.removeProcess(process);
                }

                throw exception;
            }
        }

        /**
         * Links two items together.
         * @param {string} sourceId The ID of the item that you want to link.
         * @param {string} destinationId The ID of the item that you want to link to.
         * @param {number} linkTypeNumber The link type number.
         * @returns {any} A promise with the result of the AJAX call.
         */
        async addItemLink(sourceId, destinationId, linkTypeNumber) {
            return Wiser2.api({ url: `${this.settings.serviceRoot}/ADD_LINK?source=${encodeURIComponent(sourceId)}&destination=${encodeURIComponent(destinationId)}&linkTypeNumber=${encodeURIComponent(linkTypeNumber)}` });
        }

        /**
         * Update the link of an item, to link/move that item to another item.
         * @param {number} linkId The ID of the current link that you want to update.
         * @param {number} newDestinationId The ID of the item where you want to move the item to.
         * @returns {any} A promise with the result of the AJAX call.
         */
        async updateItemLink(linkId, newDestinationId) {
            return Wiser2.api({ url: `${this.settings.serviceRoot}/UPDATE_LINK?linkId=${encodeURIComponent(linkId)}&destinationId=${encodeURIComponent(newDestinationId)}` });
        }

        /**
         * Removes a link between two items.
         * @param {string} sourceId The ID of the item that you want to link.
         * @param {string} destinationId The ID of the item that you want to link to.
         * @param {number} linkTypeNumber The link type number.
         * @returns {Promise} A promise with the result of the AJAX call.
         */
        async removeItemLink(sourceId, destinationId, linkTypeNumber) {
            return Wiser2.api({ url: `${this.settings.serviceRoot}/REMOVE_LINK?source=${encodeURIComponent(sourceId)}&destination=${encodeURIComponent(destinationId)}&linkTypeNumber=${encodeURIComponent(linkTypeNumber)}` });
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
         */
        async createItem(entityType, parentId, name, linkTypeNumber, data = [], skipUpdate = false, moduleId = null) {
            try {
                const newItem = {
                    entity_type: entityType,
                    title: name,
                    module_id: moduleId || this.settings.moduleId
                };

                const parentIdUrlPart = parentId ? `&parentId=${encodeURIComponent(parentId)}` : "";
                const createItemResult = await Wiser2.api({
                    url: `${this.settings.wiserApiRoot}items?linkType=${linkTypeNumber || 0}${parentIdUrlPart}&isNewItem=true`,
                    method: "POST",
                    contentType: "application/json",
                    dataType: "JSON",
                    data: JSON.stringify(newItem)
                });

                // Call updateItem with only the title, to make sure the SEO value of the title gets saved if needed.
                let newItemDetails = [];
                if (!skipUpdate) newItemDetails = await this.base.updateItem(createItemResult.new_item_id, data || [], null, false, name, false, false, entityType);
                
                const workflowResult = await Wiser2.api({
                    url: `${this.settings.wiserApiRoot}items/${encodeURIComponent(createItemResult.new_item_id)}/workflow?isNewItem=true`,
                    method: "POST",
                    contentType: "application/json",
                    dataType: "JSON",
                    data: JSON.stringify(newItem)
                });
                let apiActionResult = null;

                // Check if we need to execute any API action and do that.
                try {
                    const apiActionId = await this.getApiAction("after_insert", entityType);
                    if (apiActionId) {
                        apiActionResult = await Wiser2.doApiCall(this.settings, apiActionId, newItemDetails);
                    }
                } catch(exception) {
                    console.error(exception);
                    kendo.alert("Er is iets fout gegaan tijdens het uitvoeren (of opzoeken) van de actie 'api_after_update'. Indien er een koppeling is opgezet met een extern systeem, dan zijn de wijzigingen nu niet gesynchroniseerd naar dat systeem. Probeer het a.u.b. nogmaals, of neem contact op met ons.");
                }

                return {
                    itemId: createItemResult.new_item_id,
                    itemIdPlain: createItemResult.new_item_id_plain,
                    linkId: createItemResult.new_link_id,
                    icon: createItemResult.icon,
                    workflowResult: workflowResult,
                    apiActionResult: apiActionResult
                };
            } catch (exception) {
                console.error(exception);
                let error = exception;
                if (exception.responseText) {
                    error = exception.responseText;
                } else if (exception.statusText) {
                    error = exception.statusText;
                }
                kendo.alert(`Er is iets fout gegaan met het aanmaken van het item. Probeer het a.u.b. nogmaals of neem contact op met ons.<br><br>De fout was:<br><pre>${kendo.htmlEncode(error)}</pre>`);
                return null;
            }
        }

        /**
         * Marks an item as deleted.
         * @param {string} encryptedItemId The encrypted item ID.
         * @param {string} entityType The entity type of the item to delete. This is required for workflows.
         * @returns {Promise} A promise with the result of the AJAX call.
         */
        async deleteItem(encryptedItemId, entityType) {
            console.warn("deleteItem in dynamicItems.js called");

            try {
                const apiActionId = await this.getApiAction("before_delete", entityType);
                if (apiActionId) {
                    await Wiser2.doApiCall(this.settings, apiActionId, { encryptedId: encryptedItemId });
                }
            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan tijdens het uitvoeren (of opzoeken) van de actie 'api_before_delete'. Hierdoor is het betreffende item ook niet uit Wiser verwijderd. Probeer het a.u.b. nogmaals of neem contact op met ons.");
                return new Promise((resolve, reject) => {
                    reject(exception);
                });
            }

            return Wiser2.api({
                url: `${this.base.settings.wiserApiRoot}items/${encodeURIComponent(encryptedItemId)}?entityType=${entityType || ""}`,
                method: "DELETE",
                contentType: "application/json",
                dataType: "JSON"
            });
        }

        /**
         * Marks an item as deleted.
         * @param {string} encryptedItemId The encrypted item ID.
         * @param {string} entityType The entity type of the item to undelete.
         * @returns {Promise} A promise with the result of the AJAX call.
         */
        async undeleteItem(encryptedItemId, entityType) {
            console.warn("undeleteItem in dynamicItems.js called");

            return Wiser2.api({
                url: `${this.base.settings.wiserApiRoot}items/${encodeURIComponent(encryptedItemId)}?undelete=true&entityType=${entityType || ""}`,
                method: "DELETE",
                contentType: "application/json",
                dataType: "JSON"
            });
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
            try {
                const entityTypeQueryString = !entityType ? "" : `?entityType=${encodeURIComponent(entityType)}`;
                const parentEntityTypeQueryString = !parentEntityType ? "" : `${!entityType ? "?" : "&"}parentEntityType=${encodeURIComponent(parentEntityType)}`;
                const createItemResult = await Wiser2.api({ 
                    method: "POST", 
                    url: `${this.base.settings.wiserApiRoot}items/${encodeURIComponent(itemId)}/duplicate/${encodeURIComponent(parentId)}${entityTypeQueryString}${parentEntityTypeQueryString}`,
                    contentType: "application/json",
                    dataType: "JSON"
                });
                const workflowResult = await Wiser2.api({ 
                    method: "POST", 
                    url: `${this.settings.wiserApiRoot}items/${encodeURIComponent(createItemResult.new_item_id)}/workflow?isNewItem=true`,
                    contentType: "application/json",
                    dataType: "JSON"
                });
                return {
                    itemId: createItemResult.new_item_id,
                    itemIdPlain: createItemResult.new_item_id_plain,
                    linkId: createItemResult.new_link_id,
                    icon: createItemResult.icon,
                    workflowResult: workflowResult,
                    title: createItemResult.title
                };
            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan met het dupliceren van het item. Neem a.u.b. contact op met ons.");
                return {};
            }
        }

        /**
         * Marks an item as deleted.
         * @param {string} encryptedItemId The encrypted item ID.
         * @param {int} newEnvironments The environments to copy this item to.
         * @returns {Promise} A promise with the result of the AJAX call.
         */
        async copyToEnvironment(encryptedItemId, newEnvironments) {
            return Wiser2.api({
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
            return Wiser2.api({ url: `${this.settings.serviceRoot}/GET_TITLE?itemId=${encodeURIComponent(itemId)}` });
        }

        /**
         * Gets the HTML for an item. This contains all fields and the javascript for those fields.
         * @param {string} itemId The (encrypted) ID of the item to get the HTML for.
         * @param {string} entityType The entity type of the item to get the HTML for.
         * @param {string} propertyIdSuffix Optional: The suffix for property IDs, this is required when opening items in a popup, so that the fields always have unique ID, even if they already exist in the main tab sheet.
         * @param {number} linkId Optional: The ID of the link between this item and another item. If you're opening this item via a specific link, you should enter the ID of that link, because it's possible to have fields/properties on a link instead of an item.
         * @returns {Promise} A promise with the results.
         */
        async getItemHtml(itemId, entityType, propertyIdSuffix, linkId) {
            let url = `${this.settings.wiserApiRoot}items/${encodeURIComponent(itemId)}?entityType=${encodeURIComponent(entityType)}&encryptedModuleId=${encodeURIComponent(this.base.settings.encryptedModuleId)}`;
            if (propertyIdSuffix) {
                url += `&propertyIdSuffix=${encodeURIComponent(propertyIdSuffix)}`;
            }
            if (linkId) {
                url += `&itemLinkId=${encodeURIComponent(linkId)}`;
            }
            return Wiser2.api({ url: url });
        }

        /**
         * Get all properties / fields from a single item.
         * @param {any} itemId The ID of the item to get the details of.
         * @returns {Promise} A promise, which will return an array with 1 item. That item will contain it's basic properties and a property called "property_" which contains an object with all fields and their values.
         */
        async getItemDetails(itemId) {
            return Wiser2.api({ url: `${this.settings.serviceRoot}/GET_ITEM_DETAILS?itemId=${encodeURIComponent(itemId)}` });
        }

        /**
         * Get all properties / fields from a single item.
         * @param {any} itemId The ID of the item to get the details of.
         * @returns {Promise} A promise, which will return an array with 1 item. That item will contain it's basic properties and a property called "property_" which contains an object with all fields and their values.
         */
        async getEntityBlock(itemId) {
            return Wiser2.api({ url: `${this.settings.wiserApiRoot}items/${encodeURIComponent(itemId)}/block/` });
        }

        /**
         * Get the value of a specific field from a specific item.
         * @param {number} encryptedItemId The ID of the item.
         * @param {string} propertyName The name of the property / field.
         * @returns {Promise} A promise, which will return an array with 1 item. That item will contain information about the property / field, including it's value.
         */
        async getItemValue(encryptedItemId, propertyName) {
            return Wiser2.api({ url: `${this.settings.serviceRoot}/GET_ITEM_VALUE?itemId=${encodeURIComponent(encryptedItemId)}&propertyName=${encodeURIComponent(propertyName)}` });
        }

        /**
         * Get all meta data from a single item.
         * @param {string} itemId The ID of the item to get the meta data of.
         * @param {string} itemId The entity type of the item to get the meta data of.
         * @returns {Promise} A promise, which will return an array with 1 item. That item will contain it's basic properties and a property called "property_" which contains an object with all fields and their values.
         */
        async getItemMetaData(itemId, entityType) {
            const entityTypeUrlPart = entityType ? `?entityType=${encodeURIComponent(entityType)}` : "";
            return Wiser2.api({ url: `${this.settings.wiserApiRoot}items/${encodeURIComponent(itemId)}/meta${entityTypeUrlPart}` });
        }

        /**
         * Get all versions of an item for different environments.
         * @param {any} mainItemId The ID of the main/original item.
         * @returns {Promise} A promise, which will return an array with the results.
         */
        async getItemEnvironments(mainItemId) {
            return Wiser2.api({ url: `${this.settings.serviceRoot}/GET_ITEM_ENVIRONMENTS?mainItemId=${encodeURIComponent(mainItemId)}` });
        }

        /**
         * Gets all available entity types that can be added as a child to the given parent.
         * @param {string} parentId The (encrypted) ID of the parent to get the available entity types of.
         * @return {any} An array with all the available entity types.
         */
        async getAvailableEntityTypes(parentId) {
            const names =  await Wiser2.api({ url: `${this.base.settings.wiserApiRoot}entity-types/${encodeURIComponent(this.settings.moduleId)}?parentId=${encodeURIComponent(parentId)}` });
            return names.map(name => { return { name: name }; });
        }

        /**
         * Get the details for an entity type.
         * @param {string} name The name of the entity type.
         * @param {number} moduleId The ID of the module (different modules can have entity types with the same name).
         * @returns {Promise} A promise with the results.
         */
        async getEntityType(name, moduleId) {
            const sessionStorageKey = `wiser_entity_type_info_${name}`;
            let result = sessionStorage.getItem(sessionStorageKey);
            if (result) {
                return JSON.parse(result);
            }
            
            result = await Wiser2.api({ url: `${this.settings.serviceRoot}/GET_ENTITY_TYPE?entityType=${encodeURIComponent(name)}&moduleId=${encodeURIComponent(moduleId || "")}` });
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
            const result = await Wiser2.api({ url: `${this.settings.serviceRoot}/GET_API_ACTION?entityType=${encodeURIComponent(entityType)}&actionType=${encodeURIComponent(actionType)}` });
            return !result || !result.length ? 0 : result[0].apiConnectionId || 0;
        }
    }



    // Initialize the DynamicItems class and make one instance of it globally available.
    window.dynamicItems = new DynamicItems(settings);
})(moduleSettings);

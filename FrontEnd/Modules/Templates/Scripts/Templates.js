import { TrackJS } from "trackjs";
import { Wiser2 } from "../../Base/Scripts/Utils.js";
import "../../Base/Scripts/Processing.js";

require("@progress/kendo-ui/js/kendo.notification.js");
require("@progress/kendo-ui/js/kendo.button.js");
require("@progress/kendo-ui/js/kendo.combobox.js");
require("@progress/kendo-ui/js/kendo.editor.js");
require("@progress/kendo-ui/js/kendo.splitter.js");
require("@progress/kendo-ui/js/kendo.tabstrip.js");
require("@progress/kendo-ui/js/kendo.treeview.js");
require("@progress/kendo-ui/js/kendo.grid.js");
require("@progress/kendo-ui/js/cultures/kendo.culture.nl-NL.js");
require("@progress/kendo-ui/js/messages/kendo.messages.nl-NL.js");

import "../css/Templates.css";

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
            this.templateSettings = null;
            this.linkedTemplates = null;
            this.templateHistory = null;
            this.previewProfiles = null;
            this.treeViewContextMenu = null;
            this.mainHtmlEditor = null;
            this.dynamicContentGrid = null;
            this.newContentId = 0;
            this.newContentTitle = null;

            this.templateTypes = Object.freeze({
                "UNKNOWN": 0,
                "HTML": 1,
                "CSS": 2,
                "SCSS": 3,
                "JS": 4,
                "SCRIPTS": 4,
                "QUERY": 5,
                "NORMAL": 6,
                "DIRECTORY": 7,
                "XML": 8,
                "AIS": 8
            });

            // Default settings
            this.settings = {
                moduleId: 0,
                customerId: 0,
                username: "Onbekend",
                userEmailAddress: "",
                userType: ""
            };
            Object.assign(this.settings, settings);

            // Other.
            this.mainLoader = null;

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
            this.settings.username = user.adminAccountName ? `Happy Horizon (${user.adminAccountName})` : user.name;
            this.settings.adminAccountLoggedIn = !!user.adminAccountName;

            const userData = await Wiser2.getLoggedInUserData(this.settings.wiserApiRoot);
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

            await this.initializeKendoComponents();
            this.bindSaveButton();
            this.bindPreviewButtons();
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
                click: () => this.openCreateNewItemDialog()
            });

            $("#saveButton").kendoButton({
                icon: "save"
            });

            // Main window
            this.mainWindow = $("#window").kendoWindow({
                width: "1500",
                height: "650",
                title: "Templates",
                visible: true,
                actions: ["refresh"],
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
                }
            }).data("kendoTabStrip");

            this.treeViewTabStrip = $(".tabstrip-treeview").kendoTabStrip({
                animation: {
                    open: {
                        effects: "fadeIn"
                    }
                }
            }).data("kendoTabStrip");

            // Load the tabs via the API.
            this.treeViewTabs = await Wiser2.api({
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

            // Select first tab.
            this.treeViewTabStrip.select(0);

            // Treeview 
            this.mainTreeView = [];
            $(".treeview").each((index, element) => {
                const treeViewElement = $(element);
                this.mainTreeView[index] = treeViewElement.kendoTreeView({
                    dragAndDrop: true,
                    collapse: this.onTreeViewCollapseItem.bind(this),
                    expand: this.onTreeViewExpandItem.bind(this),
                    select: this.onTreeViewSelect.bind(this),
                    drop: this.onTreeViewDropItem.bind(this),
                    dataSource: {
                        transport: {
                            read: (readOptions) => {
                                Wiser2.api({
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

            var tempPreviewData = [
                {
                    "id": 1,
                    "component": "MLSimplemenu",
                    "naam": "naam 1",
                    "size": "10kb",
                    "laadtijd": "450ms"
                },
                {
                    "id": 2,
                    "component": "Basket",
                    "naam": "naam 2",
                    "size": "35kb",
                    "laadtijd": "6450ms"
                }
            ]

            $("#preview-results").kendoGrid({
                dataSource: tempPreviewData,
                scrollable: true,
                resizable: true,
                filterable: {
                    extra: false,
                    operators: {
                        string: {
                            startswith: "Begint met",
                            eq: "Is gelijk aan",
                            neq: "Is ongelijk aan",
                            contains: "Bevat",
                            doesnotcontain: "Bevat niet",
                            endswith: "Eindigt op"
                        }
                    },
                    messages: {
                        isTrue: "<span>Ja</span>",
                        isFalse: "<span>Nee</span>"
                    }
                },
                pageable: true,
                columns: [
                    {
                        field: "component",
                        title: "Component",
                        width: "40%",
                        filterable: true
                    },
                    {
                        field: "naam",
                        title: "naam",
                        width: "20%",
                        filterable: true
                    },
                    {
                        field: "size",
                        title: "size",
                        width: "20%",
                        filterable: true
                    },
                    {
                        field: "laadtijd",
                        title: "laadtijd",
                        width: "40%",
                        filterable: true
                    },
                ]
            }).data("kendoGrid");
        }

        /**
         * Shows or hides the main (full screen) loader.
         * @param {boolean} show True to show the loader, false to hide it.
         */
        toggleMainLoader(show) {
            this.mainLoader.toggleClass("loading", show);
        }

        customBoolEditor(container, options) {
            $('<input class="checkbox" type="checkbox" name="encrypt" data-type="boolean" data-bind="checked:encrypt">').appendTo(container);
        }
        customDopdownEditor(container, options) {
            $("<select name='type' data-type='string' data-bind='type'><option value='POST'>POST</option><option value='SESSION'>SESSION</option></select>").appendTo(container);
        }

        initPreviewProfileInputs(profiles, index) {
            let tempPreviewVariablesData = null;
            if (profiles.length !== 0) {
                tempPreviewVariablesData = profiles[index].variables;
                document.getElementById("profile-url").value = profiles[index].url;
            }

            $("#preview-variables").kendoGrid({
                dataSource: {
                    data: tempPreviewVariablesData,
                    schema: {
                        model: {
                            fields: {
                                id: { type: "int" },
                                type: { type: "string", defaultValue: "POST" },
                                key: { type: "string" },
                                value: { type: "string" },
                                encrypt: { type: "boolean" }
                            }
                        }
                    }
                },
                scrollable: true,
                resizable: true,
                filterable: {
                    extra: false,
                    operators: {
                        string: {
                            startswith: "Begint met",
                            eq: "Is gelijk aan",
                            neq: "Is ongelijk aan",
                            contains: "Bevat",
                            doesnotcontain: "Bevat niet",
                            endswith: "Eindigt op"
                        }
                    },
                    messages: {
                        isTrue: "<span>Ja</span>",
                        isFalse: "<span>Nee</span>"
                    }
                },
                pageable: true,
                toolbar: [{ name: "create", text: "Add variable" }],
                columns: [
                    {
                        field: "type",
                        title: "Type",
                        width: "20%",
                        filterable: true,
                        editor: window.Templates.customDopdownEditor
                    },
                    {
                        field: "key",
                        title: "Key",
                        width: "20%",
                        filterable: true
                    },
                    {
                        field: "value",
                        title: "Value",
                        width: "20%",
                        filterable: true
                    },
                    {
                        field: "encrypt",
                        width: "calc(40% - 150px)",
                        filterable: true,
                        editor: window.Templates.customBoolEditor

                    },
                    {
                        command: ["edit",
                            {
                                name: "delete", text: "",
                                iconClass: "k-icon k-i-trash"
                            }
                        ],
                        title: "&nbsp;",
                        width: 150,
                        filterable: false
                    }
                ],
                editable: "inline"
            }).data("kendoGrid");
        }

        /**
         * Opens the dialog for creating a new item.
         * @param {any} dataItem When calling this from context menu, the selected data item from the tree view or tab sheet should be entered here.
         */
        async openCreateNewItemDialog(dataItem) {
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

                                    this.createNewTemplate(parentId, title, type, treeView, !parentId ? undefined : treeView.select());
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
            if (dataItem.id === this.selectedId) {
                return;
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

            if (dataItem.isFolder) {
                return;
            }

            this.loadTemplate(dataItem.id);
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

                await Wiser2.api({
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

        /**
         * Event for when the context menu of the tree view gets opened.
         * @param {any} event The open event of a kendoContextMenu.
         */
        onContextMenuOpen(event) {
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

            let selectedItem;
            if (!event.target.closest(".k-treeview")) {
                selectedItem = this.treeViewTabs[$(event.target).index()];
            } else {
                selectedItem = treeView.dataItem(node);;
            }

            const action = selectedOption.attr("action");

            switch (action) {
                case "addNewItem":
                    this.openCreateNewItemDialog(selectedItem);
                    break;
                case "rename":
                    kendo.prompt("Vul een nieuwe naam in", selectedItem.templateName).then((newName) => {
                        this.renameItem(selectedItem.templateId, newName).then(() => {
                            treeView.text(node, newName);
                        });
                    });
                    break;
                case "delete":
                    kendo.alert("Functionaliteit voor het verwijderen van templates is nog niet gemaakt.");
                    break;
                default:
                    kendo.alert(`Onbekende actie '${action}'. Probeer het a.u.b. opnieuw op neem contact op.`);
                    break;
            }
        }

        /**
         * Load a template for editing.
         * @param {any} id The ID of the template to load.
         */
        async loadTemplate(id) {
            const process = `onTreeViewSelect_${Date.now()}`;
            window.processing.addProcess(process);

            try {
                // Get template settings and linked templates.
                let promises = [
                    Wiser2.api({
                        url: `${this.settings.wiserApiRoot}templates/${id}/settings`,
                        dataType: "json",
                        method: "GET"
                    }),
                    Wiser2.api({
                        url: `${this.settings.wiserApiRoot}templates/${id}/linked-templates`,
                        dataType: "json",
                        method: "GET"
                    }),
                    Wiser2.api({
                        url: `${this.settings.wiserApiRoot}templates/${id}/history`,
                        dataType: "json",
                        method: "GET"
                    })
                ];

                const [templateSettings, linkedTemplates, templateHistory] = await Promise.all(promises);
                this.templateSettings = templateSettings;
                this.linkedTemplates = linkedTemplates;
                this.templateHistory = templateHistory;

                // Load the different tabs.
                promises = [];

                // Development
                promises.push(
                    Wiser2.api({
                        method: "POST",
                        contentType: "application/json",
                        url: "/Modules/Templates/DevelopmentTab",
                        data: JSON.stringify({
                            templateSettings: templateSettings,
                            linkedTemplates: linkedTemplates
                        })
                    }).then((response) => {
                        document.getElementById("developmentTab").innerHTML = response;
                        this.initKendoDeploymentTab();
                        this.bindDeployButtons(id);
                    })
                );

                // History
                promises.push(
                    Wiser2.api({
                        method: "POST",
                        contentType: "application/json",
                        url: "/Modules/Templates/HistoryTab",
                        data: JSON.stringify(templateHistory)
                    }).then((response) => {
                        document.getElementById("historyTab").innerHTML = response;
                    })
                );

                await Promise.all(promises);
                window.processing.removeProcess(process);

                // Only load dynamic content and previews for HTML templates.
                const isHtmlTemplate = this.templateSettings.type.toUpperCase() === "HTML";
                const dynamicContentTab = this.mainTabStrip.element.find(".dynamic-tab");
                const previewTab = this.mainTabStrip.element.find(".preview-tab");

                if (!isHtmlTemplate) {
                    this.mainTabStrip.disable(dynamicContentTab);
                    this.mainTabStrip.disable(previewTab);

                    const selectedTab = this.mainTabStrip.select();
                    if (selectedTab.hasClass("dynamic-tab") || selectedTab.hasClass("preview-tab")) {
                        this.mainTabStrip.select(0);
                    }

                    return;
                }

                this.mainTabStrip.enable(dynamicContentTab);
                this.mainTabStrip.enable(previewTab);

                // Dynamic content
                this.dynamicContentGrid = $("#dynamic-grid").kendoGrid({
                    dataSource: {
                        transport: {
                            read: (readOptions) => {
                                Wiser2.api({
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
                        }
                    },
                    scrollable: true,
                    resizable: true,
                    filterable: {
                        extra: false,
                        operators: {
                            string: {
                                startswith: "Begint met",
                                eq: "Is gelijk aan",
                                neq: "Is ongelijk aan",
                                contains: "Bevat",
                                doesnotcontain: "Bevat niet",
                                endswith: "Eindigt op"
                            }
                        },
                        messages: {
                            isTrue: "<span>Ja</span>",
                            isFalse: "<span>Nee</span>"
                        }
                    },
                    pageable: true,
                    columns: [
                        {
                            field: "id",
                            title: "ID",
                            hidden: true
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
                                /*TODO {
                                    name: "duplicate",
                                    text: "",
                                    iconClass: "k-icon k-i-copy",
                                    click: this.kendoGridCopy.bind(this)
                                },*/
                                {
                                    name: "Open",
                                    text: "",
                                    iconClass: "k-icon k-i-edit",
                                    click: this.onDynamicContentOpenClick.bind(this)
                                }/* TODO,
                                        {
                                            name: "Preview",
                                            text: "",
                                            iconClass: "k-icon k-i-preview",
                                            click: this.kendoGridPreview.bind(this)
                                        }*/
                            ],
                            title: "&nbsp;",
                            width: 160,
                            filterable: false
                        }
                    ],
                    toolbar: [{
                        name: "add",
                        text: "Nieuw",
                        template: `<a class='k-button k-button-icontext' href='\\#' onclick='return window.Templates.openDynamicContentWindow(0, "Nieuw dynamische content toevoegen")'><span class='k-icon k-i-file-add'></span>Nieuw item toevoegen</a>`
                    }]
                }).data("kendoGrid");

                // Preview
                Wiser2.api({
                    url: `${this.settings.wiserApiRoot}templates/${id}/profiles`,
                    dataType: "json",
                    method: "GET"
                }).then((previewProfiles) => {
                    this.previewProfiles = previewProfiles;

                    Wiser2.api({
                        method: "POST",
                        contentType: "application/json",
                        url: "/Modules/Templates/PreviewTab",
                        data: JSON.stringify(previewProfiles)
                    }).then((response) => {
                        document.getElementById("previewTab").innerHTML = response;
                        $("#preview-combo-select").kendoDropDownList({
                            change: (event) => {
                                if (event.sender.dataItem()) {
                                    this.initPreviewProfileInputs(previewProfiles, event.sender.select());
                                }
                            }
                        });

                        this.initPreviewProfileInputs(previewProfiles, 0);
                    })
                });
            } catch (exception) {
                console.error(exception);
                kendo.alert(`Er is iets fout gegaan. Probeer het a.u.b. opnieuw of neem contact op met ons.<br>${exception.responseText || exception}`);
                window.processing.removeProcess(process);
            }
        }

        //Initializes the kendo components on the deployment tab. These are seperated from other components since these can be reloaded by the application.
        async initKendoDeploymentTab() {
            $("#deployLive, #deployAccept, #deployTest").kendoButton();

            // ComboBox
            $(".combo-select").kendoDropDownList();

            const editorElement = $(".editor");
            const editorType = editorElement.data("editorType");
            if (editorType === "text/html") {
                // HTML editor
                const insertDynamicContentTool = {
                    name: "wiserDynamicContent",
                    tooltip: "Dynamische inhoud toevoegen",
                    exec: this.onHtmlEditorDynamicContentExec.bind(this)
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
                        "insertImage",
                        "insertFile",
                        "subscript",
                        "superscript",
                        "tableWizard",
                        "createTable",
                        "addRowAbove",
                        "addRowBelow",
                        "addColumnLeft",
                        "addColumnRight",
                        "deleteRow",
                        "deleteColumn",
                        "viewHtml",
                        "formatting",
                        "cleanFormatting"
                    ]
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
                    mode: editorType
                });

                editorElement.data("CodeMirrorInstance", codeMirrorInstance);
            }

            const dataSource = (this.templateSettings.externalFiles || []).map(url => { return { url: url } })
            const externalFilesGridElement = $("#externalFiles");
            if (externalFilesGridElement.length > 0) {
                externalFilesGridElement.kendoGrid({
                    height: 500,
                    editable: true,
                    batch: true,
                    toolbar: ["create"],
                    columns: [
                        { field: "url" },
                        { command: "destroy", width: 140 }
                    ],
                    dataSource: dataSource
                });
            }
        }

        //Initialize display variable for the fields containing objects and dates within the grid.
        initDynamicContentDisplayFields(datasource) {
            datasource.forEach((row) => {
                row.displayDate = kendo.format("{0:dd MMMM yyyy}", new Date(row.changedOn));
                row.displayUsages = row.usages.join(",");
                row.displayVersions = Math.max(...row.versions.versionList) + " live: " + row.versions.liveVersion + ", Acceptatie: " + row.versions.acceptVersion + ", test: " + row.versions.testVersion;
            });
        }

        kendoGridCopy(x) {
            //TODO
        }

        kendoGridPreview() {
            //TODO
        }

        /**
         * Event that gets called when the user executes the custom action for adding dynamic content from Wiser to the HTML editor.
         * This will open a dialog where they can select any component that is linked to the current template, or add a new one.
         * @param {any} event The event from the execute action.
         */
        async onHtmlEditorDynamicContentExec(event) {
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
                            this.mainHtmlEditor.exec("inserthtml", { value: html });
                        }
                    }
                ]
            }).data("kendoDialog");

            dialog.open();
        }

        onDynamicContentOpenClick(event) {
            const grid = $("#dynamic-grid").data("kendoGrid");
            const tr = $(event.currentTarget).closest("tr");
            const data = grid.dataItem(tr);
            this.openDynamicContentWindow(data.id, data.title);
        }

        openDynamicContentWindow(contentId, title) {
            return new Promise((resolve) => {
                const grid = $("#dynamic-grid").data("kendoGrid");

                this.newContentId = 0;
                this.newContentTitle = null;

                $("#dynamicContentWindow").kendoWindow({
                    title: title,
                    width: "100%",
                    height: "100%",
                    content: `/Modules/DynamicContent/${contentId || 0}?templateId=${this.selectedId}`,
                    actions: ["close"],
                    draggable: false,
                    iframe: true,
                    close: (closeWindowEvent) => {
                        grid.dataSource.read();
                        resolve({ id: this.newContentId, title: this.newContentTitle });
                    }
                }).data("kendoWindow").maximize().open();
            });
        }

        //Bind the deploybuttons for the template versions
        bindDeployButtons(templateId) {
            $("#deployLive").on("click", this.deployEnvironment.bind(this, "live", templateId));
            $("#deployAccept").on("click", this.deployEnvironment.bind(this, "accept", templateId));
            $("#deployTest").on("click", this.deployEnvironment.bind(this, "test", templateId));
        }

        //Deploy a version to an enviorenment
        async deployEnvironment(environment, templateId) {
            const version = document.querySelector(`.version-${environment} select.combo-select`).value;
            if (!version) {
                kendo.alert("U heeft geen geldige versie geselecteerd.");
                return;
            }

            await Wiser2.api({
                url: `${this.settings.wiserApiRoot}templates/${templateId}/publish/${encodeURIComponent(environment)}/${version}`,
                dataType: "json",
                type: "POST",
                contentType: "application/json"
            });

            window.popupNotification.show(`Template is succesvol naar de ${environment} omgeving gezet`, "info");
            await this.reloadTabs(templateId);
        }

        //Save the template data
        bindSaveButton() {
            document.getElementById("saveButton").addEventListener("click", this.saveTemplate.bind(this));
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
                const response = await Wiser2.api({
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
         * Save a new version of the selected template.
         */
        async saveTemplate() {
            if (!this.selectedId) {
                return false;
            }

            const process = `saveTemplate_${Date.now()}`;
            window.processing.addProcess(process);
            let success = true;

            try {
                const scssLinks = [];
                const jsLinks = [];
                document.querySelectorAll("#scss-checklist input[type=checkbox]:checked").forEach(el => { scssLinks.push({ templateId: el.dataset.template }) });
                document.querySelectorAll("#js-checklist input[type=checkbox]:checked").forEach(el => { jsLinks.push({ templateId: el.dataset.template }) });
                const editorElement = $(".editor");
                const kendoEditor = editorElement.data("kendoEditor");
                const codeMirror = editorElement.data("CodeMirrorInstance");
                const editorValue = kendoEditor ? kendoEditor.value() : codeMirror.getValue();

                const data = Object.assign({
                    templateId: this.selectedId,
                    name: this.templateSettings.name || "",
                    type: this.templateSettings.type,
                    parentId: this.templateSettings.parentId,
                    editorValue: editorValue,
                    linkedTemplates: {
                        linkedSccsTemplates: scssLinks,
                        linkedJavascript: jsLinks
                    }
                }, this.getNewSettings());
                
                const externalFilesGrid = $("#externalFiles").data("kendoGrid");
                if (externalFilesGrid) {
                    data.externalFiles = externalFilesGrid.dataSource.data().map(d => d.url);
                }

                const response = await Wiser2.api({
                    url: `${this.settings.wiserApiRoot}templates/${data.templateId}`,
                    dataType: "json",
                    type: "POST",
                    contentType: "application/json",
                    data: JSON.stringify(data)
                });

                window.popupNotification.show(`Template '${data.name}' is succesvol opgeslagen`, "info");
                await this.reloadTabs(this.selectedId);
            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan, probeer het a.u.b. opnieuw of neem contact op.");
                success = false;
            }

            window.processing.removeProcess(process);
            return success;
        }

        /**
         * Reloads the publishedEnvironments and history of the template.
         * @param {any} templateId The ID of the template.
         */
        async reloadTabs(templateId) {
            let promises = [
                Wiser2.api({
                    url: `${this.settings.wiserApiRoot}templates/${templateId}/meta`,
                    dataType: "json",
                    type: "GET",
                    contentType: "application/json"
                }),
                Wiser2.api({
                    url: `${this.settings.wiserApiRoot}templates/${templateId}/history`,
                    dataType: "json",
                    method: "GET"
                })
            ];

            const [templateMetaData, templateHistory] = await Promise.all(promises);

            promises = [
                Wiser2.api({
                    method: "POST",
                    contentType: "application/json",
                    url: "/Modules/Templates/PublishedEnvironments",
                    data: JSON.stringify(templateMetaData)
                }).then((response) => {
                    document.querySelector("#published-environments").outerHTML = response;
                    $("#deployLive, #deployAccept, #deployTest").kendoButton();
                    $("#published-environments .combo-select").kendoDropDownList();
                    this.bindDeployButtons(templateId);
                }),
                Wiser2.api({
                    method: "POST",
                    contentType: "application/json",
                    url: "/Modules/Templates/HistoryTab",
                    data: JSON.stringify(templateHistory)
                }).then((response) => {
                    document.getElementById("historyTab").innerHTML = response;
                })
            ];

            await Promise.all(promises);
        }

        //Bind buttons in the preview tab of the template overview
        bindPreviewButtons() {
            $("#addPreviewRow").on("click", () => {
                $("#preview-variables").data("kendoGrid").addRow();
            });

            $("#preview-remove-profile").on("click", () => {
                if ($("#preview-combo-select").data("kendoDropDownList").dataItem()) {
                    Wiser2.api({
                        url: `${this.settings.wiserApiRoot}templates/${this.selectedId}/profiles/${$("#preview-combo-select").data("kendoDropDownList").dataItem().value}`,
                        type: "DELETE"
                    }).then((response) => {
                        window.popupNotification.show(`Het profiel '${document.getElementById("preview-combo-select").innerText}' is verwijderd`, "info");
                    });
                }
            });

            $("#preview-save-profile-as").on("click", async () => {
                Wiser2.api({
                    url: `${this.settings.wiserApiRoot}templates/${this.selectedId}/profiles/${$("#preview-combo-select").data("kendoDropDownList").dataItem().value}`,
                    type: "POST",
                    data: JSON.stringify({
                        id: document.getElementById("preview-combo-select").value,
                        name: await kendo.prompt("Kies een naam"),
                        url: "https://www.domeinnaam.nl/pad/pagina.html?cat=2",
                        variables: [{ type: "POST", key: "loggedin_user", value: 444, encrypt: false }, { type: "SESSION", key: "product_id", value: 151515, encrypt: false }]
                    })
                }).then((response) => {
                    window.popupNotification.show(`Het profiel '${document.getElementById("preview-combo-select").innerText}' is opgeslagen`, "info");
                });
            });

            $("#preview-save-profile").on("click", () => {
                if ($("#preview-combo-select").data("kendoDropDownList").dataItem()) {
                    var variables = [];
                    $("#preview-variables").data("kendoGrid")._data.forEach((e) => {
                        variables.push({ type: e.type, key: e.key, value: e.value, encrypt: e.encrypt })
                    });

                    Wiser2.api({
                        url: `${this.settings.wiserApiRoot}templates/${this.selectedId}/profiles/${$("#preview-combo-select").data("kendoDropDownList").dataItem().value}`,
                        type: "POST",
                        data: JSON.stringify({
                            id: document.getElementById("preview-combo-select").value,
                            name: "",
                            url: "https://www.domeinnaam.nl/pad/pagina.html?cat=2",
                            variables: [{ type: "POST", key: "loggedin_user", value: 444, encrypt: false }, { type: "SESSION", key: "product_id", value: 151515, encrypt: false }]
                        })
                    }).then((response) => {
                        window.popupNotification.show(`Het profiel '${document.getElementById("preview-combo-select").innerText}' is opgeslagen`, "info");
                    });
                }
            });
        }

        /**
         * Creates a new template, adds it to the tree view and finally opens it.
         * @param {any} parentId The ID of the parent to add the template to.
         * @param {any} title The name of the template.
         * @param {any} type The template type.
         * @param {any} treeView The tree view that the template should be added to.
         * @param {any} parentElement The parent node in the tree view to add the parent to.
         */
        async createNewTemplate(parentId, title, type, treeView, parentElement) {
            const process = `createNewTemplate_${Date.now()}`;
            window.processing.addProcess(process);

            let success = true;
            try {
                const result = await Wiser2.api({
                    url: `${this.settings.wiserApiRoot}templates/${parentId}?name=${encodeURIComponent(title)}&type=${type}`,
                    dataType: "json",
                    type: "PUT",
                    contentType: "application/json"
                });

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
                    const newTreeViewElement = parentElement.length > 0 ? treeView.append(result, parentElement) : treeView.append(result);
                    treeView.select(newTreeViewElement);
                    treeView.trigger("select", {
                        node: newTreeViewElement
                    });
                }

            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan. Probeer het a.u.b. opnieuw of neem contact op.");
                success = false;
            }

            window.processing.removeProcess(process);
            return success;
        }
        
        /**
         * Retrieve the new values entered by the user.
         * */
        getNewSettings() {
            const settingsList = {};
            
            $(".advanced input, .advanced select").each((index, element) => {
                const field = $(element);
                const propertyName = field.attr("name");
                if (!propertyName) {
                    console.warn("No property name found for field", field);
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
    }

    // Initialize the DynamicItems class and make one instance of it globally available.
    window.Templates = new Templates(settings);
})(moduleSettings);
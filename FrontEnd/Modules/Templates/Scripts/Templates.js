import { TrackJS } from "trackjs";
import { Modules, Dates, Strings, Wiser2, Misc } from "../../Base/Scripts/Utils.js";
import "../../Base/Scripts/Processing.js";
//require("@progress/kendo-ui/js/kendo.all.js");
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
            this.treeviewTabStrip = null;
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
                icon: "plus"
            });

            $("#saveButton").kendoButton({
                icon: "save"
            });

            this.initKendoDeploymentTab();

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

            this.treeviewTabStrip = $(".tabstrip-treeview").kendoTabStrip({
                animation: {
                    open: {
                        effects: "fadeIn"
                    }
                }
            }).data("kendoTabStrip");

            // Load the tabs via the API.
            const treeViewTabs = await Wiser2.api({
                url: `${this.settings.wiserApiRoot}templates/0/tree-view`,
                dataType: "json",
                method: "GET"
            });

            for (let tab of treeViewTabs) {
                this.treeviewTabStrip.append({
                    text: tab.templateName,
                    content: `<ul id="${tab.templateId}-treeview" class="treeview" data-id="${tab.templateId}"></ul>`
                });
            }

            // Select first tab.
            this.treeviewTabStrip.select(0);

            // Treeview 
            this.mainTreeView = [];
            $(".treeview").each((index, element) => {
                const treeViewElement = $(element);
                this.mainTreeView[index] = treeViewElement.kendoTreeView({
                    dragAndDrop: true,
                    collapse: this.onTreeViewCollapseItem.bind(this),
                    expand: this.onTreeViewExpandItem.bind(this),
                    select: this.onTreeViewSelect.bind(this),
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
            var dataItem = event.sender.dataItem(event.node);
            if (dataItem.isFolder || dataItem.id === this.selectedId) {
                return;
            }

            // Deselect all tree views in other tabs, otherwise they will stay selected even though the user selected a different template.
            for (let index in this.mainTreeView) {
                const treeView = this.mainTreeView[index];
                if (this.treeviewTabStrip.select().index() !== index) {
                    treeView.select($());
                }
            };

            this.selectedId = dataItem.id;
            const process = `onTreeViewSelect_${Date.now()}`;
            window.processing.addProcess(process);

            try {
                // Get template settings and linked templates.
                let promises = [
                    Wiser2.api({
                        url: `${this.settings.wiserApiRoot}templates/${dataItem.id}/settings`,
                        dataType: "json",
                        method: "GET"
                    }),
                    Wiser2.api({
                        url: `${this.settings.wiserApiRoot}templates/${dataItem.id}/linked-templates`,
                        dataType: "json",
                        method: "GET"
                    }),
                    Wiser2.api({
                        url: `${this.settings.wiserApiRoot}templates/${dataItem.id}/history`,
                        dataType: "json",
                        method: "GET"
                    }),
                    Wiser2.api({
                        url: `${this.settings.wiserApiRoot}templates/${dataItem.id}/profiles`,
                        dataType: "json",
                        method: "GET"
                    })
                ];

                const [templateSettings, linkedTemplates, templateHistory, previewProfiles] = await Promise.all(promises);
                this.templateSettings = templateSettings;
                this.linkedTemplates = linkedTemplates;
                this.templateHistory = templateHistory;
                this.previewProfiles = previewProfiles;

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
                        this.bindDeployButtons(dataItem.id);
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

                // Dynamic content
                promises.push(
                    Wiser2.api({
                        url: `${this.settings.wiserApiRoot}templates/${dataItem.id}/linked-dynamic-content`,
                        dataType: "json",
                        method: "GET"
                    }).then((response) => {
                        $("#dynamic-grid").kendoGrid().data("KendoGrid");
                        this.initDynamicContentDisplayFields(response);

                        $("#dynamic-grid").kendoGrid({
                            dataSource: response,
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
                                        {
                                            name: "duplicate",
                                            text: "",
                                            iconClass: "k-icon k-i-copy",
                                            click: this.kendoGridCopy
                                        },
                                        {
                                            name: "Open",
                                            text: "",
                                            iconClass: "k-icon k-i-edit",
                                            click: this.kendoGridOpen
                                        },
                                        {
                                            name: "Preview",
                                            text: "",
                                            iconClass: "k-icon k-i-preview",
                                            click: this.kendoGridPreview
                                        }
                                    ],
                                    title: "&nbsp;",
                                    width: 160,
                                    filterable: false
                                }
                            ]
                        }).data("kendoGrid");
                    })
                );

                // Preview
                promises.push(
                    Wiser2.api({
                        method: "POST",
                        contentType: "application/json",
                        url: "/Modules/Templates/PreviewTab",
                        data: JSON.stringify(previewProfiles)
                    }).then((response) => {
                        document.getElementById("previewTab").innerHTML = response;
                        $("#preview-combo-select").kendoComboBox({
                            change: (event) => {
                                if (event.sender.dataItem()) {
                                    window.Templates.initPreviewProfileInputs(previewProfiles, event.sender.select());
                                }
                            }
                        });

                        window.Templates.initPreviewProfileInputs(previewProfiles, 0);
                    })
                );

                await Promise.all(promises);
            } catch (exception) {
                console.error(exception);
                kendo.alert(`Er is iets fout gegaan. Probeer het a.u.b. opnieuw of neem contact op met ons.<br>${exception.responseText || exception}`);
            }

            window.processing.removeProcess(process);
        }

        //Initializes the kendo components on the deployment tab. These are seperated from other components since these can be reloaded by the application.
        initKendoDeploymentTab() {
            $("#deployLive, #deployAccept, #deployTest").kendoButton();

            // ComboBox
            $(".combo-select").kendoComboBox();

            // HTML editor
            this.mainTabStrip = $(".editor").kendoEditor({
                resizable: true,
                tools: [
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

        kendoGridOpen(e) {
            var tr = $(e.target).closest("tr");
            var data = this.dataItem(tr);
            window.location.pathname = "dynamiccontent/overview/" + data.id;
        }

        kendoGridPreview() {
            //TODO
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

        async saveTemplate() {
            if (!this.selectedId) {
                return;
            }

            const scssLinks = [];
            const jsLinks = [];
            document.querySelectorAll("#scss-checklist input[type=checkbox]:checked").forEach(el => { scssLinks.push({ templateId: el.dataset.template }) });
            document.querySelectorAll("#js-checklist input[type=checkbox]:checked").forEach(el => { jsLinks.push({ templateId: el.dataset.template }) });

            const data = {
                templateId: this.selectedId,
                name: this.templateSettings.name || "",
                editorValue: $(".editor").data("kendoEditor").value(),
                useCache: document.getElementById("combo-cache").value,
                cacheMinutes: document.getElementById("cache-duration").value,
                handleRequests: document.getElementById("handleRequests").checked,
                handleSession: document.getElementById("handleSession").checked,
                handleObjects: document.getElementById("handleObjects").checked,
                handleStandards: document.getElementById("handleStandards").checked,
                handleTranslations: document.getElementById("handleTranslations").checked,
                handleDynamicContent: document.getElementById("handleDynamicContent").checked,
                handleLogicBlocks: document.getElementById("handleLogicBlocks").checked,
                handleMutators: document.getElementById("handleMutators").checked,
                loginRequired: document.getElementById("user-check").checked,
                linkedTemplates: {
                    linkedSccsTemplates: scssLinks,
                    linkedJavascript: jsLinks
                }
            }

            if (document.getElementById("user-check").checked) {
                data.loginUserType = document.getElementById("combo-user1").value;
                data.loginSessionPrefix = document.getElementById("combo-user2").value;
                data.loginRole = document.getElementById("combo-user3").value;
            }

            const response = await Wiser2.api({
                url: `${this.settings.wiserApiRoot}templates/${data.templateId}`,
                dataType: "json",
                type: "PUT",
                contentType: "application/json",
                data: JSON.stringify(data)
            });

            window.popupNotification.show(`Template '${data.name}' is succesvol opgeslagen`, "info");
            await this.reloadTabs(this.selectedId);
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
                    $("#published-environments .combo-select").kendoComboBox();
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
            $("#addPreviewRow").on("click", function () {
                $("#preview-variables").data("kendoGrid").addRow();
            });

            $("#preview-remove-profile").on("click", function () {
                if ($("#preview-combo-select").data('kendoComboBox').dataItem()) {
                    $.ajax({
                        type: "POST",
                        url: "/Template/DeletePreviewProfiles",
                        data: { templateId: window.Templates.selectedId, profileId: $("#preview-combo-select").data('kendoComboBox').dataItem().value },
                        success: function (response) {
                            window.popupNotification.show("Het profiel '" + document.getElementById("preview-combo-select").innerText + "' is verwijderd", "info");
                        }
                    });
                }
            });

            $("#preview-save-profile-as").on("click", function () {
                $.ajax({
                    type: "POST",
                    url: "/Template/EditPreviewProfile",
                    data: {
                        profile: {
                            id: document.getElementById("preview-combo-select").value,
                            name: prompt("Enter the profile's name"),
                            url: "https://www.domeinnaam.nl/pad/pagina.html?cat=2",
                            variables: [{ type: "POST", key: "loggedin_user", value: 444, encrypt: false }, { type: "SESSION", key: "product_id", value: 151515, encrypt: false }]
                        },
                        templateId: window.Templates.selectedId
                    }
                });
            });

            $("#preview-save-profile").on("click", function () {
                if ($("#preview-combo-select").data('kendoComboBox').dataItem()) {
                    var variables = [];
                    $("#preview-variables").data("kendoGrid")._data.forEach((e) => {
                        variables.push({ type: e.type, key: e.key, value: e.value, encrypt: e.encrypt })
                    });

                    $.ajax({
                        type: "POST",
                        url: "/Template/EditPreviewProfile",
                        data: {
                            profile: {
                                id: $("#preview-combo-select").data('kendoComboBox').dataItem().value,
                                name: "",
                                url: document.getElementById("profile-url").value,
                                variables: variables
                            },
                            templateId: window.Templates.selectedId
                        }
                    });
                }
            });
        }
    }

    // Initialize the DynamicItems class and make one instance of it globally available.
    window.Templates = new Templates(settings);
})(moduleSettings);
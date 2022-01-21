import { Modules, Dates, Strings, Wiser2, Misc } from "../../Base/Scripts/Utils.js";
require("@progress/kendo-ui/js/kendo.all.js");
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
            this.mainTreeview = null;
            this.mainTabStrip = null;
            this.treeviewTabStrip = null;
            this.mainWindow = null;
            this.mainComboBox = null;
            this.mainComboInput = null;
            this.mainMultiSelect = null;
            this.mainNumericTextBox = null;
            this.mainDatePicker = null;
            this.mainDateTimePicker = null;

            // Other.
            this.mainLoader = null;

            // Set the Kendo culture to Dutch. TODO: Base this on the language in Wiser.
            kendo.culture("nl-NL");

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
            this.initializeKendoComponents();
            this.bindSaveButton();
            this.bindPreviewButtons();
        }

        /**
         * Initializes all kendo components for the base class.
         */
        initializeKendoComponents() {
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

            // Treeview 
            this.mainTreeview = [];
            $(".treeview").each((i, e) => {
                this.mainTreeview[i] = $(e).kendoTreeView({
                    dragAndDrop: true,
                    expand: this.treeViewExpand,
                    select: function (e) {
                        var dataItem = window.Templates.mainTreeview[i].dataItem(e.node);
                        if (!dataItem.isFolder && dataItem.id != window.Templates.selectedId) {
                            //Development
                            $.ajax({
                                type: "GET",
                                url: "/Template/DevelopmentTab",
                                data: {
                                    templateId: dataItem.id
                                },
                                success: function (response) {
                                    document.getElementById("developmentTab").innerHTML = response;
                                    window.Templates.initKendoDeploymentTab();
                                    window.Templates.bindDeployButtons(dataItem.id);
                                    window.Templates.selectedId = dataItem.id;
                                }
                            });
                            //History
                            $.ajax({
                                type: "GET",
                                url: "/Template/HistoryTab",
                                data: {
                                    templateId: dataItem.id
                                },
                                success: function (response) {
                                    document.getElementById("historyTab").innerHTML = response;
                                }
                            });

                            // Dynamic content Grid
                            $.ajax({
                                type: "GET",
                                url: "/Template/GetLinkedDynamicContent",
                                data: { templateId: dataItem.id },
                                success: function (response) {
                                    $("#dynamic-grid").kendoGrid().data("KendoGrid")
                                    window.Templates.initDynamicContentDisplayFields(response);

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
                                                format: "{0:MM DD yyyy}",
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
                                                field: "changed_by",
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
                                                        click: window.Templates.kendoGridCopy
                                                    },
                                                    {
                                                        name: "Open", text: "",
                                                        iconClass: "k-icon k-i-edit",
                                                        click: window.Templates.kendoGridOpen
                                                    },
                                                    {
                                                        name: "Preview", text: "",
                                                        iconClass: "k-icon k-i-preview",
                                                        click: window.Templates.kendoGridPreview
                                                    }
                                                ],
                                                title: "&nbsp;",
                                                width: 160,
                                                filterable: false
                                            }
                                        ]
                                    }).data("kendoGrid");
                                }
                            });


                            $.ajax({
                                type: "GET",
                                url: "/Template/PreviewTab",
                                data: {
                                    templateId: dataItem.id
                                },
                                success: function (response) {
                                    document.getElementById("previewTab").innerHTML = response;

                                    $.ajax({
                                        type: "GET",
                                        url: "/Template/GetPreviewProfiles",
                                        data: { templateId: dataItem.id },
                                        success: function (response) {
                                            //Combo box
                                            $("#preview-combo-select").kendoComboBox({
                                                change: function () {
                                                    if ($("#preview-combo-select").data('kendoComboBox').dataItem()) {
                                                        window.Templates.initPreviewProfileInputs(response, $("#preview-combo-select").data('kendoComboBox').select());
                                                    }
                                                }
                                            });

                                            window.Templates.initPreviewProfileInputs(response, 0);

                                        }
                                    });
                                }
                            });                            
                        }
                    }
                }).data("kendoTreeView");
            });

            //Fill Treeviewroots
            this.mainTreeview.forEach((e, i) => {
                $.ajax({
                    type: "GET",
                    url: "/Template/GetTreeviewSection",
                    data: { templateId: e.element[0].dataset.id },
                    success: function (response) {
                        response.forEach((folderItem) => {
                            var toAppendItem = { id: folderItem.templateId, text: folderItem.templateName, hasChildren: folderItem.hasChildren, isFolder: folderItem.isFolder };
                            if (folderItem.isFolder) {
                                toAppendItem.spriteCssClass = "folder";
                            }
                            window.Templates.mainTreeview[i].append(
                                toAppendItem
                            );
                        });
                    }
                });
            });

            // Tabstrip
            this.mainTabStrip = $(".tabstrip").kendoTabStrip({
                animation: {
                    open: {
                        effects: "fadeIn"
                    }
                }
            }).data("kendoTabStrip");

            this.treeviewTabStrip = $(".tabstrip-treeview").kendoTabStrip({
                select: function (e) {
                    //Deselect other treeview tabs.
                    window.Templates.mainTreeview.forEach((e, i) => {
                        if (window.Templates.treeviewTabStrip.select().index() != i) {
                            e.select($());
                        }
                    });
                },
                animation: {
                    open: {
                        effects: "fadeIn"
                    }
                }
            }).data("kendoTabStrip");
            //select first tab
            this.treeviewTabStrip.select(0);

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

        customBoolEditor(container, options) {
            $('<input class="checkbox" type="checkbox" name="encrypt" data-type="boolean" data-bind="checked:encrypt">').appendTo(container);
        }
        customDopdownEditor(container, options) {
            $("<select name='type' data-type='string' data-bind='type'><option value='POST'>POST</option><option value='SESSION'>SESSION</option></select>").appendTo(container);
        }

        initPreviewProfileInputs(profiles, index) {
            console.log(profiles);
            var tempPreviewVariablesData = null;
            if (profiles.length!=0) {
                tempPreviewVariablesData = profiles[index].variables;
                document.getElementById("profile-url").value = profiles[index].url;
                console.log(tempPreviewVariablesData);
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

        treeViewExpand(nodeElement) {
            var currentTreeview = window.Templates.treeviewTabStrip.select().index();
            var nodeId = window.Templates.mainTreeview[currentTreeview].dataItem(nodeElement.node).id;
            var dataNode = window.Templates.mainTreeview[currentTreeview].dataSource.get(nodeId);
            if (dataNode.hasLoaded != true) {
                //Display loading element
                window.Templates.mainTreeview[currentTreeview].append(
                    {text: "Loading..."},
                    window.Templates.mainTreeview[currentTreeview].findByUid(dataNode.uid)
                );
                $.ajax({
                    type: "GET",
                    url: "/Template/GetTreeviewSection",
                    data: { templateId: nodeId },
                    success: function (response) {
                        console.log(response);
                        //Removes children
                        $(nodeElement.node).children('.k-group').remove();
                        //Append the subitems
                        response.forEach((folderItem) => {
                            var toAppendItem = { id: folderItem.templateId, text: folderItem.templateName, hasChildren: folderItem.hasChildren };
                            if (folderItem.isFolder) {
                                toAppendItem.spriteCssClass = "folder";
                            }
                            window.Templates.mainTreeview[currentTreeview].append(
                                toAppendItem,
                                window.Templates.mainTreeview[currentTreeview].findByUid(dataNode.uid)
                            );
                        });
                        //Mark element as loaded
                        dataNode.hasLoaded = true
                    }
                });
            }
        }

        //Initializes the kendo components on the deployment tab. These are seperated from other components since these can be reloaded by the application.
        initKendoDeploymentTab() {
            $("#deployLive, #deployAccept, #deployTest").kendoButton();

            // ComboBox
            this.mainComboBox = $(".combo-select").kendoComboBox();

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
                row.displayDate = kendo.format("{0:dd MMMM yyyy}", new Date(row.changed_on));
                row.displayUsages = row.usages.join(",");
                row.displayVersions = Math.max(...row.versions.versionList) + " live: " + row.versions.liveVersion + ", Acceptatie: " + row.versions.acceptVersion + ", test: " + row.versions.testVersion
            });
        }

        kendoGridCopy(x) {
            //TODO
            console.log(x);
        }

        kendoGridOpen(e) {
            var tr = $(e.target).closest("tr");
            var data = this.dataItem(tr);
            console.log(data);
            window.location.pathname = "dynamiccontent/overview/" + data.id;
        }

        kendoGridPreview() {
            //TODO
        }

        //Bind the deploybuttons for the template versions
        bindDeployButtons(templateId) {
            $("#deployLive").on("click", function () { window.Templates.deployEnvironment("live", templateId) });
            $("#deployAccept").on("click", function () { window.Templates.deployEnvironment("accept", templateId) });
            $("#deployTest").on("click", function () { window.Templates.deployEnvironment("test", templateId)});
        }

        //Deploy a version to an enviorenment
        deployEnvironment(environment, templateId) {
            console.log("clicked " + environment);
            $.ajax({
                type: "GET",
                url: "/Template/PublishToEnvironment",
                data: { templateId: templateId, version: document.querySelector(".version-" + environment + " select.combo-select").value, environment: environment },
                success: function (response) {
                    window.popupNotification.show("Template is succesvol naar de " + environment + " omgeving gezet", "info");
                    setTimeout(function () { console.log(response); window.Templates.reloadTabs(templateId); }, 1000);}
            });
        }

        //Save the template data
        bindSaveButton() {
            document.getElementById("saveButton").addEventListener("click", this.saveTemplate);
        }
        saveTemplate() {
            if (!window.Templates.selectedId) {
                return;
            }
            var data = {
                templateid: window.Templates.selectedId,
                name: "Test",
                editorValue: document.querySelector(".editor").value,
                //version: 0,
                //changed_on: Now(),
                //changed_by: "",

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
                loginRequired: document.getElementById("user-check").checked
            }
            if (document.getElementById("user-check").checked) {
                data.loginUserType = document.getElementById("combo-user1").value;
                data.loginSessionPrefix = document.getElementById("combo-user2").value;
                data.loginRole = document.getElementById("combo-user3").value;
            }
            var scssLinks = [];
            document.querySelectorAll("#scss-checklist input[type=checkbox]:checked").forEach(el => { scssLinks.push(el.dataset.template) })
            var jsLinks = [];
            document.querySelectorAll("#js-checklist input[type=checkbox]:checked").forEach(el => { jsLinks.push(el.dataset.template) })

            $.ajax({
                type: "POST",
                url: "/Template/SaveTemplate",
                data: {
                    templateData: JSON.stringify(data),
                    scssLinks: scssLinks,
                    jsLinks: jsLinks
                },
                success: function (response) {
                    window.popupNotification.show("Template '"+data.name+"' is succesvol opgeslagen", "info");
                    setTimeout(function () { window.Templates.reloadTabs(window.Templates.selectedId); }, 1000);
                }
            });
        }

        //Reloads the publishedEnvironments and history of the template.
        reloadTabs(templateId) {
            $.ajax({
                type: "GET",
                url: "/Template/PublishedEnvironments",
                data: { templateId: templateId },
                success: function (response) {
                    document.querySelector("#published-environments").outerHTML = response;
                    $("#deployLive, #deployAccept, #deployTest").kendoButton();
                    this.mainComboBox = $("#published-environments .combo-select").kendoComboBox();
                    window.Templates.bindDeployButtons(templateId);
                }
            });
            $.ajax({
                type: "GET",
                url: "/Template/HistoryTab",
                data: {
                    templateId: templateId
                },
                success: function (response) {
                    document.getElementById("historyTab").innerHTML = response;
                }
            });
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
                            console.log(response);
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
                    },
                    success: function (response) { console.log(response); }
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
                        },
                        success: function (response) { console.log(response); }
                    });
                }
            });
        }
    }


    // Initialize the DynamicItems class and make one instance of it globally available.
    window.Templates = new Templates(settings);
})(moduleSettings);
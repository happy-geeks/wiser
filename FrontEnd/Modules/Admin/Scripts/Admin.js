import { InfoText } from "../Scripts/InfoText";
import { ModuleTab } from "../Scripts/ModuleTab.js";
import { RoleTab } from "../Scripts/RoleTab.js";
import { EntityTab } from "../Scripts/EntityTab.js";
import { EntityFieldTab } from "../Scripts/EntityFieldTab.js";
import { EntityPropertyTab } from "../Scripts/EntityPropertyTab.js";
import { WiserQueryTab } from "../Scripts/WiserQueryTab.js";
import { WiserLinkTab } from "../Scripts/WiserLinkTab.js";
import { Wiser, Misc } from "../../Base/Scripts/Utils.js";


require("@progress/kendo-ui/js/kendo.all.js");
require("@progress/kendo-ui/js/cultures/kendo.culture.nl-NL.js");
require("@progress/kendo-ui/js/messages/kendo.messages.nl-NL.js");

import "../css/Admin.css";

// Any custom settings can be added here. They will overwrite most default settings inside the module.
const moduleSettings = {
};

((settings) => {
    /**
     * Main class.
     */
    class Admin {

        /**
         * Initializes a new instance of Admin.
         * @param {any} settings An object containing the settings for this class.
         */
        constructor(settings) {
            this.base = this;
            // Kendo components.
            this.mainWindow = null;

            this.activeMainTab = "Modules"; 
            
            //classes
            this.entityTab = null;
            this.entityFieldTab = null;
            this.entityPropertyTab = null;
            this.moduleTab = null;
            this.translations = null;
            this.roleTab = null;
            this.wiserQueryTab = null;
            this.wiserLinkTab = null;

            // Set the Kendo culture to Dutch. TODO: Base this on the language in Wiser.
            kendo.culture("nl-NL");

            // Base settings.
            this.settings = {};
            Object.assign(this.settings, settings);

            // Fire event on page ready for direct actions
            $(document).ready(() => {
                this.onPageReady();

            });

            // enum of available kendo prompts
            this.kendoPromptType = Object.freeze({
                PROMPT: "prompt",
                CONFIRM: "confirm",
                ALERT: "alert"
            });

            // enum of available inputtypes
            this.inputTypes = Object.freeze({
                ACTIONBUTTON: "ActionButton",
                AUTOINCREMENT: "AutoIncrement",
                CHART: "Chart",
                CHECKBOX: "CheckBox",
                COMBOBOX: "ComboBox",
                COLORPICKER: "ColorPicker",
                DATASELECTOR: "DataSelector",
                DATETIMEPICKER: "DateTimePicker",
                EMPTY: "Empty",
                FILEUPLOAD: "FileUpload",
                GPSLOCATION: "GpsLocation",
                HTMLEDITOR: "HtmlEditor",
                IFRAME: "Iframe",
                IMAGEUPLOAD: "ImageUpload",
                INPUT: "Input",
                ITEMLINKER: "ItemLinker",
                LINKEDITEM: "LinkedItem",
                MULTISELECT: "MultiSelect",
                NUMERICINPUT: "NumericInput",
                RADIOBUTTON: "RadioButton",
                SECUREINPUT: "SecureInput",
                SUBENTITIESGRID: "SubEntitiesGrid",
                TEXTBOX: "TextBox",
                TIMELINE: "TimeLine",
                QR : "Qr",
                SCHEDULER : "Scheduler"
            });

            this.dataSourceType = Object.freeze({
                PANEL1: { text: "Vaste waardes", id: "panel1" },
                PANEL2: { text: "Lijst van entiteiten", id: "panel2" },
                PANEL3: { text: "Query", id: "panel3" }
            });

            this.textboxType = Object.freeze({
                EMPTY: { text: "", id: "" },
                CSS: { text: "css", id: "css" },
                JAVASCRIPT: { text: "javascript", id: "javascript" },
                MYSQL: { text: "mysql", id: "mysql" },
                XML: { text: "xml", id: "xml" },
                HTML: { text: "html", id: "html" },
                JSON: { text: "json", id: "json" }
            });

            this.actionButtonTypes = Object.freeze({
                OPENURL: { text: "Open url", id: "openUrl" },
                OPENURLONCE: { text: "Open url eenmalig", id: "openUrlOnce" },
                OPENWINDOW: { text: "Open venster", id: "openWindow" },
                EXECUTEQUERY: { text: "Voer query uit", id: "executeQuery" },
                EXECUTEQUERYONCE: { text: "Voer query eenmalig uit", id: "executeQueryOnce" },
                GENERATEFILE: { text: "Genereer bestand", id: "generateFile" },
                REFRESHCURRENTITEM: { text: "Ververs het item", id: "refreshCurrentItem" },
                ACTIONCONFIRMDIALOG: { text: "Bevestigingsvenster", id: "actionConfirmDialog" },
                CUSTOM: { text: "Custom javascript", id: "custom" }
            });
            this.fieldTypesDropDown = Object.freeze({
                INPUT: { text: "Tekstveld", id: "input" },
                DATETIME: { text: "Datumtijd", id: "datetime" },
                DATE: { text: "Datum", id: "date" },
                TIME: { text: "Tijd", id: "time" },
                NUMBER: { text: "Numeriek", id: "number" },
                COMBOBOX: { text: "Combobox", id: "comboBox" },
                GRID: { text: "Grid", id: "grid" }
            });
        }

        /**
         * Event that will be fired when the page is ready.
         */
        async onPageReady() {
            // Setup any settings from the body element data. These settings are added via the Wiser backend and they take preference.
            Object.assign(this.settings, $("body").data());
            
            // Add logged in user access token to default authorization headers for all jQuery ajax requests.
            $.ajaxSetup({
                headers: { "Authorization": `Bearer ${localStorage.getItem("accessToken")}` }
            });

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

            const user = JSON.parse(localStorage.getItem("userData"));
            this.settings.oldStyleUserId = user.oldStyleUserId;
            this.settings.username = user.adminAccountName ? `${user.adminAccountName} (Admin)` : user.name;
            this.settings.adminAccountLoggedIn = !!user.adminAccountName;

            const userData = await Wiser.getLoggedInUserData(this.settings.wiserApiRoot);
            this.settings.userId = userData.encryptedId;
            this.settings.customerId = userData.encryptedCustomerId;
            this.settings.zeroEncrypted = userData.zeroEncrypted;
            this.settings.wiserUserId = userData.id;

            this.settings.serviceRoot = `${this.settings.wiserApiRoot}templates/get-and-execute-query`;
            this.settings.getItemsUrl = `${this.settings.wiserApiRoot}data-selectors`;


            // These are all entities, including duplicate ones that have the same name in different modules.
            try {
                this.entityList = (await Wiser.api({url: `${this.base.settings.serviceRoot}/GET_ENTITY_LIST`})) || [];
            } catch (exception) {
                console.error("Error occurred while trying to load all entity types 1", exception);
                this.entityList = [];
            }

            // These are all entities grouped by name.
            try {
                this.allUniqueEntityTypes = (await Wiser.api({url: `${this.base.settings.wiserApiRoot}entity-types?onlyEntityTypesWithDisplayName=false`})) || [];
            } catch (exception) {
                console.error("Error occurred while trying to load all entity types", exception);
                this.allUniqueEntityTypes = [];
            }

            this.moduleTab = new ModuleTab(this);
            this.entityTab = new EntityTab(this);
            this.entityFieldsTab = new EntityFieldTab(this);
            this.entityPropertyTab = new EntityPropertyTab(this);
            this.roleTab = new RoleTab(this);
            this.wiserQueryTab = new WiserQueryTab(this);
            this.wiserLinkTab = new WiserLinkTab(this);
            this.setupBindings();
            this.initializeKendoComponents();

            //translations from external file InfoText.js
            this.translations = new InfoText();
        }

        isJson(jsonField) {
            try {
                JSON.parse(jsonField);
                return true;
            } catch (e) {
                return false;
            }
        }

        moveUp(e) {
            e.preventDefault();
            const kendo = $(e.currentTarget).closest("div.k-grid").data("kendoGrid");
            const dataItem = kendo.dataItem($(e.currentTarget).closest("tr"));
            this.moveRow(kendo, dataItem, -1);
        }

        moveDown(e) {
            e.preventDefault();
            const kendo = $(e.currentTarget).closest("div.k-grid").data("kendoGrid");
            const dataItem = kendo.dataItem($(e.currentTarget).closest("tr"));
            this.moveRow(kendo, dataItem, 1);
        }
        swap(a, b, propertyName) {
            const temp = a[propertyName];
            a[propertyName] = b[propertyName];
            b[propertyName] = temp;
        }

        moveRow(grid, dataItem, direction) {
            const record = dataItem;
            if (!record) {
                return;
            }
            let newIndex = grid.dataSource.indexOf(record);
            direction < 0 ? newIndex-- : newIndex++;
            if (newIndex < 0 || newIndex >= grid.dataSource.total()) {
                return;
            }
            this.swap(grid.dataSource._data[newIndex], grid.dataSource._data[newIndex], 'position');
            grid.dataSource.remove(record);
            grid.dataSource.insert(newIndex, record);
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

            //BUTTONS
            $(".saveButton").kendoButton({
                click: this.saveChanges.bind(this),
                icon: "save"
            });

            Misc.addEventToFixToolTipPositions();
        }

        async saveChanges(e) {
            if (!this.activeMainTab || this.activeMainTab === "undefined") {
                console.error("activeMainTab property is not set");
                return;
            }

            //Call save function based on active tab
            switch (this.activeMainTab.toLowerCase()) {
                case "entityProperty":
                    await this.entityTab.beforeSave();
                    break;
                case "query's":
                    await this.wiserQueryTab.beforeSave();
                    break;
                case "modules":
                    await this.moduleTab.beforeSave();
                    break;
                case "links":
                    await this.wiserLinkTab.beforeSave();
                    break;
                default:
                    await this.entityTab.beforeSave();
                    break;
            }
        }

        // show a simple notifcation
        showNotification(appendTo, text, type = "info", autoHideAfter = 5000) {
            // hide  box after 1 second instead of default 5 seconds
            if (type === "success" || type === "info") {
                autoHideAfter = 1000;
            }
            const notification = $("<div />").kendoNotification({
                appendTo: appendTo,
                autoHideAfter: autoHideAfter
            }).data("kendoNotification");
            notification.show(text, type);
        }

        // Behaviour item checkboxes onclick
        setCheckboxForItems(targetElement, tagetType, itemId, itemIdentifier)
        {
            let permissionValue = -1;

            switch (tagetType) {
                case "all":
                    if (targetElement.checked) {
                        permissionValue = 15;
                        const checkboxes = [...targetElement.closest("tr").querySelectorAll("input[type=checkbox]")];
                        for (let checkbox of checkboxes) {
                            checkbox.checked = true;
                        }
                        document.getElementById("role-" + itemIdentifier +"-disable-" + itemId).checked = false;
                    }
                    break;
                case "nothing":
                    if (targetElement.checked) {
                        permissionValue = 0;
                        document.getElementById("role-" + itemIdentifier +"-all-" + itemId).checked = false;
                        document.getElementById("role-" + itemIdentifier +"-read-" + itemId).checked = false;
                        document.getElementById("role-" + itemIdentifier +"-create-" + itemId).checked = false;
                        document.getElementById("role-" + itemIdentifier +"-edit-" + itemId).checked = false;
                        document.getElementById("role-" + itemIdentifier +"-delete-" + itemId).checked = false;
                    }
                    break;
                default:
                    if ([...targetElement.closest("tr").querySelectorAll("input[type=checkbox]:checked")].length == 0)
                        permissionValue = 0;
                    else
                        permissionValue = [...targetElement.closest("tr").querySelectorAll("input[type=checkbox]:checked")].map(e => parseInt(e.dataset.permission)).reduce((a, b) => a + b);

                    document.getElementById("role-" + itemIdentifier + "-disable-" + itemId).checked = (permissionValue == 0);
                    document.getElementById("role-" + itemIdentifier + "-all-" + itemId).checked = (permissionValue == 15);
                    break;
            }

            return permissionValue;
        }

        // Behaviour header checkboxes onclick
        setCheckboxForHeaderItems(clickedElement) {
            var cb = $(clickedElement),
                th = cb.closest("th"),
                col = th.index() + 1,
                chk = cb.closest('.k-grid').find('tbody td:nth-child(' + col + ') input[type=checkbox]');

            chk.prop("checked", cb.is(":checked"));
            chk.trigger("change");
        }

        /**
         * Initializes all kendo components for the base class.
         */
        initializeKendoComponents() {
            // The main window of the module.
            // Almost all modules start with a Window. If yours doesn't, you can remove this code.
            this.mainWindow = $("#window").kendoWindow({
                width: "90%",
                height: "90%",
                title: false,
                visible: true,
                resizable: false
            }).data("kendoWindow").maximize().open();

            this.mainTabStrip = $("#MainTabStrip").kendoTabStrip({
                animation: {
                    open: {
                        effects: "expand:vertical",
                        duration: 0
                    },
                    close: {
                        effects: "expand:vertical",
                        duration: 0
                    }
                },
                select: (event) => {
                    const tabName = event.item.querySelector(".k-link").innerHTML.toLowerCase();

                    if (tabName === "query's" || tabName === "entiteiten" || tabName === "modules") {
                        $("footer").show();
                    } else {
                        $("footer").hide();
                    }
                },
                activate: (event) => {
                    const tabName = event.item.querySelector(".k-link").innerHTML.toLowerCase();
                    this.activeMainTab = tabName;

                    // Refresh code mirrors in the currently activated tab.
                    if (event.contentElement) {
                        event.contentElement.querySelectorAll("div.CodeMirror").forEach((div) => {
                            if (div.CodeMirror) {
                                div.CodeMirror.refresh();
                            }
                        });
                    }
                }
            }).data("kendoTabStrip");
        }

        // open prompt/dialog winodw
        openDialog(title, content, type = this.kendoPromptType.PROMPT, defaultValue = "") {
            const properties = {
                title: title,
                value: defaultValue,
                content: content
            };

            switch (type) {
                case this.kendoPromptType.PROMPT:
                    var prompt = $("<div id='kendoPrompt'></div>").kendoPrompt(properties).data("kendoPrompt");
                    // pressing enter confirms prompt
                    prompt.element.next().find("input[type=text]").keyup((event) => {
                        if (!event.key || event.key.toLowerCase() !== "enter") {
                            return;
                        }
                        $(event.currentTarget).closest(".k-prompt-container").next().find(".k-primary, .k-button-solid-primary").trigger("click");
                    });
                    return prompt.open().result;
                case this.kendoPromptType.CONFIRM:
                    return $("<div id='kendoPrompt'></div>").kendoConfirm(properties).data("kendoConfirm").open().result;
                case this.kendoPromptType.ALERT:
                    return $("<div></div>").kendoAlert(properties).data("kendoAlert").open().result;
            }
        }

        /**
         * Reload the list of modules in the side bar (on the left) of Wiser.
         */
        async reloadModulesOnParentFrame() {
            if (!window.parent || !window.parent.main || !window.parent.main.vueApp || typeof(window.parent.main.vueApp.reloadModules) !== "function") {
                return;
            }

            await window.parent.main.vueApp.reloadModules();
        }
    }

    // Initialize the DynamicItems class and make one instance of it globally available.
    window.admin = new Admin(settings);
})(moduleSettings);
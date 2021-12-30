import { InfoText } from "../Scripts/InfoText";
import { Wiser2 } from "../../Base/Scripts/Utils.js";

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
         * Initializes a new instance of AisDashboard.
         * @param {any} settings An object containing the settings for this class.
         */
        constructor(settings) {
            this.base = this;
            // Kendo components.
            this.mainWindow = null;

            //classes
            this.entityPropertiesTab = null;
            this.moduleTab = null;
            this.translations = null;
            this.roleTab = null;

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
                ACTIONBUTTON: "action-button",
                AUTOINCREMENT: "auto-increment",
                // BUTTON:"button",
                CHART: "chart",
                CHECKBOX: "checkbox",
                COMBOBOX: "combobox",
                COLORPICKER: "color-picker",
                DATASELECTOR: "data-selector",
                DATETIMEPICKER: "date-time picker",
                //DATERANGE: "daterange",
                EMPTY: "empty",
                FILEUPLOAD: "file-upload",
                GPSLOCATION: "gpslocation",
                // GRID: "grid",
                HTMLEDITOR: "HTMLeditor",
                // IMAGECOORDS:"imagecoords",
                IMAGEUPLOAD: "image-upload",
                INPUT: "input",
                ITEMLINKER: "item-linker",
                LINKEDITEM: "linked-item",
                MULTISELECT: "multiselect",
                NUMERIC: "numeric-input",
                RADIOBUTTON: "radiobutton",
                SECUREINPUT: "secure-input",
                SUBENTITIESGRID: "sub-entities-grid",
                TEXTBOX: "textbox",
                TIMELINE: "timeline",
                QR : "qr",
                SCHEDULER : "scheduler"
                //QUERYBUILDER: "querybuilder"
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
            this.settings.wiserVersion = parseInt(window.wiserVersion.replace(/\./g, ""));

            // Setup any settings from the body element data. These settings are added via the Wiser backend and they take preference.
            Object.assign(this.settings, $("body").data());

            if (this.settings.wiserVersion >= 210) {
                // Add logged in user access token to default authorization headers for all jQuery ajax requests.
                $.ajaxSetup({
                    headers: { "Authorization": `Bearer ${localStorage.getItem("access_token")}` }
                });

                // Show an error if the user is no longer logged in.
                const accessTokenExpires = localStorage.getItem("access_token_expires_on");
                if (!accessTokenExpires || accessTokenExpires <= new Date()) {
                    Wiser2.alert({
                        title: "Niet ingelogd",
                        content: "U bent niet (meer) ingelogd. Ververs a.u.b. de pagina en probeer het opnieuw."
                    });

                    this.toggleMainLoader(false);
                    return;
                }

                const user = JSON.parse(localStorage.getItem("userData"));
                this.settings.dataUrl = this.settings.isTestEnvironment ? user.wiser2DataUrlTest : user.wiser2DataUrlLive;
                this.settings.oldStyleUserId = user.oldStyleUserId;
                this.settings.username = user.adminAccountName ? `Happy Horizon (${user.adminAccountName})` : user.name;
                this.settings.happyEmployeeLoggedIn = user.juiceEmployeeName;

                const userData = await Wiser2.getLoggedInUserData(this.settings.wiserApiV21Root, this.settings.isTestEnvironment);
                this.settings.userId = userData.encrypted_id;
                this.settings.customerId = userData.encrypted_customer_id;
                this.settings.zeroEncrypted = userData.zero_encrypted;
                this.settings.wiser2UserId = userData.wiser2_id;
            }

            if (!this.settings.dataUrl) {
                return;
            }

            if (!this.settings.dataUrl.endsWith("/")) {
                this.settings.dataUrl += "/";
            }

            this.settings.serviceRoot = `${this.settings.wiserVersion >= 210 ? this.settings.wiserApiV21Root : this.settings.wiserApiRoot}templates/get-and-execute-query`;
            this.settings.getItemsUrl = `${this.settings.wiserVersion >= 210 ? this.settings.wiserApiV21Root : this.settings.wiserApiRoot}data-selectors`;

            this.moduleTab = new ModuleTab(this);
            this.entityPropertiesTab = new EntityPropertiesTab(this);
            this.roleTab = new RoleTab(this);
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
            // footer gets shown when user switches tabs, if footer is allowed to show there.
            //$("footer").hide();

            $(document).on("moduleClosing", (event) => {
                // You can do anything here that needs to happen before closing the module.
                event.success();
            });

            //BUTTONS
            $("#generateStandardEntities").kendoButton({
                click: () => {
                    const entitiesToGenerate = [], entitiesToGeneratePrettyName = [];
                    document.querySelectorAll("#generateEntitiesGroup input[type=checkbox]:checked").forEach((e) => {
                        entitiesToGenerate.push(e.dataset.entityGroup);
                        entitiesToGeneratePrettyName.push(e.dataset.entityPrettyName);
                    });
                    if (entitiesToGenerate.length === 0) return;
                    this.openDialog("Standaard entiteiten genereren", `Weet u zeker dat u de entiteiten ${entitiesToGeneratePrettyName.join()} voor wilt genereren?`, this.kendoPromptType.CONFIRM).then((data) => {

                        entitiesToGenerate.forEach(async (e) => {
                            const templateName = e;
                            if (!templateName) {
                                this.openDialog("Oeps...!", `Entiteiten groep "${e}", is nog niet correct ingesteld. Probeer het later opnieuw.`, this.kendoPromptType.ALERT);
                                return;
                            }
                            try {
                                const qResult = await $.get(`${this.settings.serviceRoot}/${templateName}?isTest=${encodeURIComponent(this.settings.isTestEnvironment)}`);
                                if (qResult.success) {
                                    this.showNotification(null, "Entiteiten zijn succesvol aangemaakt of bijgewerkt!", "success", 2000);
                                }
                            } catch (e) {
                                console.log(e);
                                this.openDialog("Oeps...!", `Er ging iets mis bij het genereren van de entiteiten, neem contact op met ons.`, this.kendoPromptType.ALERT);
                            }
                        });

                    });
                },
                icon: "gear"
            });
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
                        $(event.currentTarget).closest(".k-prompt-container").next().find(".k-primary").trigger("click");
                    });
                    return prompt.open().result;
                case this.kendoPromptType.CONFIRM:
                    return $("<div id='kendoPrompt'></div>").kendoConfirm(properties).data("kendoConfirm").open().result;
                case this.kendoPromptType.ALERT:
                    return $("<div></div>").kendoAlert(properties).data("kendoAlert").open().result;
            }
        }
    }

    class EntityPropertiesTab {
        constructor(base) {
            this.base = base;
            this.setupBindings();
            this.initializeKendoComponents();
            // init hide/show elements
            this.hideShowElementsBasedOnValue();
            this.fieldOptions = {};
        }

        checkIfEntityIsSet() {
            if ((!this.entitiesCombobox || !this.entitiesCombobox.dataItem() || this.entitiesCombobox.dataItem().id === "") && this.entityListInitialized === true) {
                this.base.showNotification("notification", `Selecteer eerst een entiteit!`, "error");
                return false;
            }
            return true;
        }

        checkIfTabNameIsSet() {
            if (this.tabNameDropDownList.value() === "") {
                return false;
            }
            return true;
        }

        /**
        * Setup all basis bindings for this module.
        * Specific bindings (for buttons in certain pop-ups for example) will be set when they are needed.
        */
        async setupBindings() {

            //BUTTONS
            $(".saveButton").kendoButton({
                click: this.beforeSave.bind(this),
                icon: "save"
            });

            // add an entity property
            $(".addBtn").kendoButton({
                click: () => {
                    if (!this.checkIfEntityIsSet()) {
                        return;
                    }
                    this.base.openDialog("Nieuw veld toevoegen", "Voer de naam in van het veld").then((data) => {
                        this.addRemoveEntityProperty(data);
                    });
                },
                icon: "file"
            });

            // delete an entity property
            $(".delBtn").kendoButton({
                click: () => {
                    if (!this.checkIfEntityIsSet()) {
                        return;
                    }
                    const tabNameProp = this.listOfTabProperties;
                    const index = tabNameProp.select().index();
                    const dataItem = tabNameProp.dataSource.view()[index];
                    if (!dataItem) {
                        this.base.showNotification("notification", "Item is niet succesvol verwijderd, probeer het opnieuw", "error");
                        return;
                    }

                    // ask for user confirmation before deleting
                    this.base.openDialog("Item verwijderen", "Weet u zeker dat u dit item wil verwijderen?", this.base.kendoPromptType.CONFIRM).then(() => {
                        this.addRemoveEntityProperty("", dataItem.id);
                    });
                },
                icon: "delete"
            });

            if (this.base.settings.wiserVersion >= 210) {
                // In Wiser 2.1, we only load code mirror when we actually need it.
                await Misc.ensureCodeMirror();
            }

            this.cssField = CodeMirror.fromTextArea(document.getElementById("cssField"), {
                mode: "text/css",
                lineNumbers: true
            });

            this.scriptField = CodeMirror.fromTextArea(document.getElementById("customScriptField"), {
                mode: "text/javascript",
                lineNumbers: true
            });

            this.jsonField = CodeMirror.fromTextArea(document.getElementById("jsonField"), {
                mode: "application/json",
                lineNumbers: true
            });

            this.queryField = CodeMirror.fromTextArea(document.getElementById("queryWindow"), {
                mode: "text/x-mysql",
                lineNumbers: true
            });

            this.queryFieldSubEntities = CodeMirror.fromTextArea(document.getElementById("queryFieldSubEntities"), {
                mode: "text/x-mysql",
                lineNumbers: true
            });

            this.queryDeleteField = CodeMirror.fromTextArea(document.getElementById("queryDelete"), {
                mode: "text/x-mysql",
                lineNumbers: true
            });

            this.queryInsertField = CodeMirror.fromTextArea(document.getElementById("queryInsert"), {
                mode: "text/x-mysql",
                lineNumbers: true
            });

            this.queryUpdateField = CodeMirror.fromTextArea(document.getElementById("queryUpdate"), {
                mode: "text/x-mysql",
                lineNumbers: true
            });

            this.queryContentField = CodeMirror.fromTextArea(document.getElementById("queryContent"), {
                mode: "text/x-mysql",
                lineNumbers: true
            });

            document.getElementById("hasCustomInsertQuery").addEventListener("change", (e) => {
                const element = document.querySelector(".customInsert");
                if (e.target.checked) {
                    element.style.display = "block";
                } else {
                    element.style.display = "none";
                }
            });

            document.getElementById("hasCustomUpdateQuery").addEventListener("change", (e) => {
                const element = document.querySelector(".customUpdate");
                if (e.target.checked) {
                    element.style.display = "block";
                } else {
                    element.style.display = "none";
                }
            });

            document.getElementById("hasCustomDeleteQuery").addEventListener("change", (e) => {
                const element = document.querySelector(".customDelete");
                if (e.target.checked) {
                    element.style.display = "block";
                } else {
                    element.style.display = "none";
                }
            });

            document.getElementById("customQuery").addEventListener("change", (e) => {
                const element = document.querySelector(".customQuery");
                if (e.target.checked) {
                    element.style.display = "block";
                    // abuse dataSourceFilter select
                    this.dataSourceFilter.select(2);
                } else {
                    element.style.display = "none";
                    // default
                    this.dataSourceFilter.select(0);
                }
            });
        }

        // adding or removing an entity property function
        addRemoveEntityProperty(name = "", id = 0) {
            if (name === "" && id === 0) { return; }
            let qs = {
                entityName: this.entitiesCombobox.dataItem().id,
                tabName: this.tabNameDropDownList.value() === "Gegevens" ? "" : this.tabNameDropDownList.value(),
                isTest: encodeURIComponent(this.base.settings.isTestEnvironment)
            };

            let notification;
            if (id !== 0) {
                qs.remove = true;
                qs.displayName = name;
                qs.entityPropertyId = id;
                notification = "verwijderd";
            } else {
                qs.add = true;
                qs.displayName = name;
                qs.propertyName = name;
                notification = "toegevoegd";
            }

            if (id !== 0) {
                $.get(`${this.base.settings.serviceRoot}/DELETE_ENTITYPROPERTY${jjl.convert.toQueryString(qs, true)}`)
                    .done(() => {
                        this.base.showNotification("notification", `Item succesvol ${notification}`, "success");
                        this.tabNameDropDownListSelect(this.tabNameDropDownList.dataItem());

                        // Select first item in list
                        const firstElement = this.listOfTabProperties.element.find("[data-item]").first()
                        this.listOfTabProperties.one("dataBound", () => {
                            this.selectPropertyInListView(firstElement.data("displayName"));
                        });
                    })
                    .fail(() => {
                        this.base.showNotification("notification", `Item is niet succesvol ${notification}, probeer het opnieuw`, "error");
                    });
            }
            else {
                $.post(`${this.base.settings.serviceRoot}/INSERT_ENTITYPROPERTY${jjl.convert.toQueryString(qs, true)}`)
                    .done(() => {
                        this.base.showNotification("notification", `Item succesvol ${notification}`, "success");

                        if (qs.add !== null && qs.add === true ) {
                            this.listOfTabProperties.one("dataBound", () => {
                                // select created item, except if tit is the only one.
                                 this.selectPropertyInListView(qs.displayName);
                            });
                        }
                        // if we have no items yet, and no data item of the tabname combobox. refresh entities combobox so the first tab will automatically gets selected
                        if (!this.tabNameDropDownList.dataItem()) {
                            // reset tab names if we didnt have any before
                            this.onEntitiesComboBoxSelect();
                        } else {
                            // select the right tab
                            this.tabNameDropDownListSelect(this.tabNameDropDownList.dataItem());
                        }
                    })
                    .fail(() => {
                        this.base.showNotification("notification", `Item is niet succesvol ${notification}, probeer het opnieuw`, "error");
                    });
            }
        }

        /**
       * Initializes all kendo components for the base class.
       */
        initializeKendoComponents() {

            //NUBERIC FIELDS
            this.widthInTable = $("#widthIfVisible").kendoNumericTextBox({
                decimals: 0,
                format: "# px"
            }).data("kendoNumericTextBox");

            this.width = $("#width").kendoNumericTextBox({
                decimals: 0,
                format: "# \\%"
            }).data("kendoNumericTextBox");

            this.height = $("#height").kendoNumericTextBox({
                decimals: 0,
                format: "# px"
            }).data("kendoNumericTextBox");

            this.numberOfDec = $("#numberOfDecimals").kendoNumericTextBox({
                decimals: 0,
                format: "#",
                min: 0,
                max: 100,
                step: 1
            }).data("kendoNumericTextBox");

            this.defaultNumeric = $("#defaultNumeric").kendoNumericTextBox({
                decimals: 0,
                min: 0,
                format: "#"
            }).data("kendoNumericTextBox");

            this.maxNumber = $("#maxNumber").kendoNumericTextBox({
                decimals: 0,
                format: "#"
            }).data("kendoNumericTextBox");

            this.minNumber = $("#minNumber").kendoNumericTextBox({
                decimals: 0,
                format: "#"
            }).data("kendoNumericTextBox");

            this.stepNumber = $("#stepNumber").kendoNumericTextBox({
                decimals: 0,
                format: "#"
            }).data("kendoNumericTextBox");

            this.factorNumber = $("#factorNumber").kendoNumericTextBox({
                decimals: 0,
                format: "#"
            }).data("kendoNumericTextBox");

            this.lastSelectedProperty = -1;
            // we use this property to check if the select in the tabname properties listview is by editing 
            // an item and it gets reloaded when you save it, or when a user manually selects it.
            this.isSaveSelect = false;
            //LISTVIEWS
            this.listOfTabProperties = $("#tabNameProperties").kendoListView(
                {
                    template: '<li class="sortable" data-item="${id}" data-ordering="${ordering}" data-display-name="${display_name}">${display_name}</li>',
                    dataTextField: "display_name",
                    dataValueField: "id",
                    selectable: true,
                    change: this.optionSelected.bind(this)

                }).data("kendoListView");


            //SORTABLE
            this.sortableContainer = $("#tabNameProperties div").kendoSortable({
                axis: "y",
                hint: (element) => {
                    return element.clone().addClass("hint");
                },
                change: (e) => {
                    const dataItem = this.listOfTabProperties.dataSource.view();
                    if (!dataItem || !dataItem[e.oldIndex] || !dataItem[e.newIndex] || !e.sender.draggedElement[0].dataset.item) {
                        // todo show error, fix if statement
                        return;
                    }
                    if (!this.checkIfEntityIsSet()) {
                        return;
                    }
                    const id = e.sender.draggedElement[0].dataset.item;
                    this.updateEntityPropertyOrdering(e.oldIndex, e.newIndex, id);
                },
                cursorOffset: {
                    top: -10,
                    left: -230
                }
            }).data("kendoSortable");

            //Sets the correct style for the sortable-object
            $("body").on("click", ".sortable", (event) => {
                if ($(event.currentTarget).data('dragging')) return;
                $('.sortable').removeClass('selected');
                $(event.currentTarget).addClass('selected');
            });

            //Opens combobox on click anywhere in fieldselection
            $(function () {
                $("[data-role=combobox]").each(function () {
                    const widget = $(this).getKendoComboBox();
                    widget.input.on("focus", function () {
                        widget.open();
                    });
                });
            });

            //TIMEPICKER
            this.minTimeBox = $("#minimumTime").kendoTimePicker({
                dateInput: false,
                culture: "nl-NL",
                format: "HH:mm"
            }).data("kendoTimePicker");
            // disable input to prevent bogus input.
            //  $("#minimumTime").attr("readonly", true);

            //GRID
            this.grid = $("#valuegrid").kendoGrid({
                resizable: true,
                toolbar: ["create"],
                remove: (e) => {
                    if (e.model.autoIndex) {
                        this.removeOnAutoIndex(this.fieldOptions, e.model.autoIndex);
                    }
                },
                editable: {
                    createAt: "bottom"
                },
                columns: [{
                    field: "name",
                    title: "Naam"
                }, {
                    field: "id",
                    title: "Id"
                }, {
                    command: [

                        { text: "↑", click: this.base.moveUp.bind(this.base) },
                        { text: "↓", click: this.base.moveDown.bind(this.base) },
                        "destroy"
                    ]
                }]
            }).data("kendoGrid");

            ////COMBOBOX - GENERAL
            $("#checkedCheckbox").kendoComboBox({
                select: (e) => {
                    const dataItem = e.dataItem;
                    this.filterOptions = dataItem.value;
                    $(`.item[data-visible*="${dataItem.value}"], label[data-visible*="${dataItem.value}"]`).show();
                }
            }).data("kendoComboBox");

            this.numberFormat = $("#numberFormat").kendoDropDownList({
                clearButton: false,
                dataTextField: "text",
                dataValueField: "value",
                filter: "contains",
                optionLabel: {
                    value: "",
                    text: "Selecteer het gewenste format..."
                },
                dataSource: [
                    { text: "Getalnotatie (1.00)", value: "#" },
                    { text: "Valuta (€50,00)", value: "c" },
                    { text: "Anders...", value: "anders" }
                ],
                cascade: function () {
                    if (this.dataItem().value === "anders") {
                        $("#differentFormatHolder").show();
                    } else {
                        $("#differentFormatHolder").hide();
                    }
                }
            }).data("kendoDropDownList");

            $("#dateTimeDropDown").kendoDropDownList({
                clearButton: false,
                dataTextField: "text",
                dataValueField: "value",
                filter: "contains",
                optionLabel: {
                    value: "",
                    text: "Maak uw keuze..."
                },
                cascade: function (e) {
                    const dataItem = e.dataItem || e.sender.dataItem();
                    $('.item.datetime [data-invisible]').show();
                    $('.item.datetime [data-invisible*="' + dataItem.value + '"]').hide();
                },
                dataSource: [
                    { text: "Datum", value: "date" },
                    { text: "Tijd", value: "time" },
                    { text: "Datum + tijd", value: "datetime" }
                ]
            }).data("kendoDropDownList");

            //Main combobox for selecting a entity
            this.entitiesCombobox = $("#entities").kendoDropDownList({
                placeholder: "Select gewenste entiteit...",
                clearButton: false,
                height: 400,
                dataTextField: "name",
                dataValueField: "id",
                filter: "contains",
                optionLabel: {
                    id: "",
                    name: "Maak uw keuze..."
                },
                minLength: 1,
                dataSource: {},
                cascade: this.onEntitiesComboBoxSelect.bind(this)
            }).data("kendoDropDownList");

            this.entityListInitialized = false;
            this.entitiesCombobox.one("dataBound", () => { this.entityListInitialized = true; });


            //combobox to select the correct tabname
            this.tabNameDropDownList = $("#tabnames").kendoDropDownList({
                placeholder: "Selecteer gewenste tab...",
                clearButton: false,
                dataTextField: "tab_name",
                dataValueField: "tab_name",
                filter: "contains",
                minLength: 1,
                cascade: this.tabNameDropDownListSelect.bind(this)
            }).data("kendoDropDownList");

            // property tabname
            this.tabNameProperty = $("#tabNameProperty").kendoComboBox({
                placeholder: "Selecteer gewenste tab...",
                clearButton: false,
                dataTextField: "tab_name",
                dataValueField: "tab_name",
                minLength: 1,
                dataSource: []
            }).data("kendoComboBox");

            //Combobox to get all possible inputtypes used in the database
            //TODO set to kendodropdownlist and add filter?
            this.inputTypeSelector = $("#inputtypes").kendoDropDownList({
                placeholder: "Selecteer invoertype...",
                height: 400,
                clearButton: false,
                dataTextField: "text",
                dataValueField: "id",
                filter: "contains",
                optionLabel: {
                    text: "Selecteer invoertype...",
                    id: ""
                },
                dataSource: this.createDataSourceFromEnum(this.base.inputTypes),
                change: (changeEvent) => {
                    if (changeEvent.sender.fieldOptions !== {} && (typeof changeEvent.sender.fieldOptions !== 'undefined')) {
                        this.base.openDialog("Invoertype wijzigen", "Weet u zeker dat u het invoertype wilt wijzigen?", this.base.kendoPromptType.CONFIRM).then(() => {
                            this.fieldOptions = {};
                            this.setPropertiesToDefault();
                            this.hideShowElementsBasedOnValue(changeEvent.sender.dataItem().text);
                        });
                    } else {
                        this.hideShowElementsBasedOnValue(changeEvent.sender.dataItem().text);
                    }
                    this.hideShowElementsBasedOnValue(changeEvent.sender.dataItem().text);
                }
            }).data("kendoDropDownList");

            // cache entity list because we have multiple dropdowns with the entity list
            this.entityList = new kendo.data.DataSource({
                transport: {
                    cache: "inmemory",
                    read: {
                        url: `${this.base.settings.serviceRoot}/GET_ENTITY_LIST?encryptedCustomerId=${encodeURIComponent(this.base.settings.customerId)}&isTest=${encodeURIComponent(this.base.settings.isTestEnvironment)}`
                    }
                }
            });

            this.dataSourceEntities = $("#dataSourceEntities").kendoDropDownList({
                clearButton: false,
                dataTextField: "name",
                dataValueField: "id",
                filter: "contains",
                minLength: 1,
                dataSource: {}
            }).data("kendoDropDownList");

            this.linkedItemEntity = $("#linkedItemEntity").kendoDropDownList({
                clearButton: false,
                dataTextField: "name",
                dataValueField: "id",
                filter: "contains",
                minLength: 1,
                dataSource: {},
                cascade: (e) => {
                    const dataItem = e.dataItem || e.sender.dataItem();
                    if (!dataItem) {
                        return;
                    }
                    // own ajax request to get data from GET_ITEMLINKS_BY_ENTITY and set to both datasources
                    const linkTypeList = new kendo.data.DataSource({
                        transport: {
                            async: true,
                            cache: "inmemory",
                            read: {
                                cache: "inmemory",
                                url: `${this.base.settings.serviceRoot}/GET_ITEMLINKS_BY_ENTITY?entity_name=${encodeURIComponent(dataItem.id)}&isTest=${encodeURIComponent(this.base.settings.isTestEnvironment)}`
                            }
                        }
                    });
                    // set linked item dropdown lists
                    this.linkType.setDataSource(linkTypeList);
                }
            }).data("kendoDropDownList");

            this.linkType = $("#linkType").kendoComboBox({
                placeholder: "Maak uw keuze...",
                dataTextField: "type_text",
                dataValueField: "type_value",
                dataSource: {},
                optionLabel: {
                    type_value: "",
                    type_text: "Maak uw keuze..."
                },
                change: function () {
                    const value = parseFloat(this.value()); //parse value

                    if (isNaN(value)) {
                        this.value(""); //clear the value
                    }
                }
            }).data("kendoComboBox");

            //Combobox for the "Groep" combobox
            this.groupNameComboBox = $("#groupName").kendoComboBox({
                placeholder: "Selecteer de gewenste groep...",
                clearButton: false,
                dataTextField: "group_name",
                dataValueField: "group_name"
            }).data("kendoComboBox");

            this.dependencyFields = $("#dependingField").kendoDropDownList({
                placeholder: "Maak uw keuze...",
                clearButton: false,
                dataTextField: "display_name",
                dataValueField: "property_name",
                optionLabel: {
                    property_name: "",
                    display_name: "Maak uw keuze..."
                }
            }).data("kendoDropDownList");
            
            $("#typeSecureInput").kendoDropDownList({
                placeholder: "Maak uw keuze...",
                clearButton: false,
                dataSource: [
                    { text: "Tekst", value: "text" },
                    { text: "Wachtwoord", value: "password" }
                ],
                dataTextField: "text",
                dataValueField: "value"
            }).data("kendoDropDownList");

            $("#securityMethod").kendoDropDownList({
                placeholder: "Maak uw keuze...",
                clearButton: false,
                dataSource: [
                    { text: "JCL Advanced Encryption Standard", value: "JCL_AES" },
                    { text: "Advanced Encryption Standard", value: "AES" },
                    { text: "Secure Hash Algorithm 512 bits", value: "JCL_SHA512" }
                ],
                dataTextField: "text",
                dataValueField: "value",
                optionLabel: {
                    value: "",
                    text: "Maak uw keuze..."
                },
                cascade: (e) => {
                    const dataItem = e.dataItem || e.sender.dataItem();
                    $(".item.secureInput[data-visible]").hide();
                    $('.item.secureInput[data-visible*="' + dataItem.value + '"]').show();
                }
            }).data("kendoDropDownList");

            this.dependingFilter = $("#combodepfilt").kendoDropDownList({
                placeholder: "Maak uw keuze...",
                clearButton: false,
                dataSource: [
                    { text: "is gelijk aan", value: "eq" },
                    { text: "is ongelijk aan", value: "neq" },
                    { text: "bevat", value: "contains" },
                    { text: "bevat niet", value: "doesnotcontain" },
                    { text: "begint met", value: "startswith" },
                    { text: "begint niet met", value: "doesnotstartwith" },
                    { text: "eindigt met", value: "endswith" },
                    { text: "eindigt niet met", value: "doesnotendwith" },
                    { text: "is leeg", value: "isempty" },
                    { text: "is niet leeg", value: "isnotempty" },
                    { text: "is groter dan", value: "gt" },
                    { text: "is groter dan of gelijk aan", value: "gte" },
                    { text: "is kleiner dan", value: "lt" },
                    { text: "is kleiner dan of gelijk aan", value: "lte" }
                ],
                dataTextField: "text",
                dataValueField: "value",
                optionLabel: {
                    value: "",
                    text: "Maak uw keuze..."
                },
                select: (e) => {
                    const dataItem = e.dataItem;
                    this.filterOptions = dataItem.value;
                    //$('.item[data-visible]').hide();combo-toggle
                    $('.item[data-visible*="' + dataItem.value + '"]').show();
                }
            }).data("kendoDropDownList");

            function onDataBound(e) {
                $('.k-multiselect .k-input').unbind('keyup');
                $('.k-multiselect .k-input').on('keyup', onClickEnter);
            }

            function onClickEnter(e) {
                if (e.keyCode === 13) {
                    const widget = $('#searchFields').getKendoMultiSelect();
                    const value = $(`.item.tagList .k-multiselect .k-input`).val().trim();
                    if (!value || value.length === 0) {
                        return;
                    }
                    const newItem = {
                        name: value
                    };

                    widget.dataSource.add(newItem);
                    widget.value(widget.value().concat([newItem.name]));
                }
            }

            this.searchFields = $("#searchFields").kendoMultiSelect({
                dataTextField: "name",
                dataValueField: "name",
                dataSource: {
                    data: []
                },
                dataBound: onDataBound
            }).data("kendoMultiSelect");

            //COMBOBOX - INPUT-TYPE
            this.dataSourceFilter = $("#dataSourcefilter").kendoDropDownList({
                dataSource: this.createDataSourceFromEnum(this.base.dataSourceType, true),
                dataTextField: "text",
                dataValueField: "value",
                cascade: (cascadeEvent) => {
                    const dataItem = cascadeEvent.sender.dataItem();
                    $('.togglePanel.dataSource').removeClass('active');
                    $('.togglePanel.dataSource[data-panel="' + dataItem.id + '"]').addClass("active");
                    dataItem.id === "panel2" ? $("[data-show-for-panel=panel2]").show() : $("[data-show-for-panel=panel2]").hide();
                }
            }).data("kendoDropDownList");

            // textbox type
            this.textboxTypeDropDown = $("#textboxTypeDropDown").kendoDropDownList({
                dataSource: this.createDataSourceFromEnum(this.base.textboxType, true),
                dataTextField: "text",
                dataValueField: "value"
            }).data("kendoDropDownList");

            /*
             * item linker
             */
            this.itemLinkerTypeNumber = $("#itemLinkerTypeNumber").kendoComboBox({
                placeholder: "Maak uw keuze...",
                clearButton: false,
                dataSource: {
                    transport: {
                        read: {
                            url: `${this.base.settings.serviceRoot}/GET_ITEMLINKS_BY_ENTITY?isTest=${encodeURIComponent(this.base.settings.isTestEnvironment)}`
                        }
                    }
                },
                dataTextField: "type_text",
                dataValueField: "type_text"
            }).data("kendoComboBox");

            this.itemLinkerEntity = $("#itemLinkerEntity").kendoMultiSelect({
                autoClose: false,
                clearButton: false,
                dataTextField: "name",
                dataValueField: "id",
                filter: "contains",
                minLength: 1,
                dataSource: {}
            }).data("kendoMultiSelect");

            $("#itemLinkerModuleId").kendoNumericTextBox({
                decimals: 0,
                format: "#",
                min: 0,
                step: 1
            }).data("kendoNumericTextBox");

            $("#itemLinkerDeletionOfItems").kendoDropDownList({
                placeholder: "Maak uw keuze...",
                clearButton: false,
                dataSource: [
                    { text: "Het is niet mogelijk om items te verwijderen", value: "off" },
                    { text: "De verwijder-knop verwijdert alleen de koppeling tussen de 2 items", value: "deleteLink" },
                    { text: "De verwijder-knop verwijdert altijd de koppeling en het item zelf", value: "deleteItem" },
                    { text: "De verwijder-knop vraagt aan de gebruiker of alleen de koppeling verwijdert moet worden, of ook het item zelf", value: "askUser" }
                ],
                dataTextField: "text",
                dataValueField: "value"
            }).data("kendoDropDownList");

            const actionButtonType = (container, options) => {
                $('<input required name="' + options.field + '"/>')
                    .appendTo(container)
                    .kendoDropDownList({
                        autoBind: false,
                        dataSource: this.createDataSourceFromEnum(this.base.actionButtonTypes, true),
                        dataTextField: "text",
                        dataValueField: "id",
                        valuePrimitive: true,
                        select: (e) => {
                            const editButton = e.sender.element.closest("tr[role=row]").find("td.k-command-cell a[role=button].k-grid-Wijzigen");
                            if (e.dataItem.id === "refreshCurrentItem" || e.dataItem.id === "custom") {
                                editButton.hide();
                            } else {
                                editButton.show();
                            }
                        },
                        optionLabel: {
                            id: "",
                            text: "Maak uw keuze..."
                        }
                    });
            };

            this.actionButtonActionsGridDataSourceSettings = {
                schema: {
                    model: {
                        fields: {
                            type: { defaultValue: { text: "Maak uw keuze...", id: "" } }
                        }
                    }
                }
            };

            this.actionButtonActionsGrid = $("#actionButtonActionsGrid").kendoGrid({
                dataSource: this.actionButtonActionsGridDataSourceSettings,
                resizable: true,
                toolbar: ["create"],
                editable: {
                    createAt: "bottom"
                },
                remove: (e) => {
                    if (e.model.action.autoIndex) {
                        this.removeOnAutoIndex(this.fieldOptions, e.model.action.autoIndex);
                    }
                },
                columns: [
                    {
                        field: "type",
                        title: "Actie type",
                        width: "350px",
                        editor: actionButtonType,
                        template: (event) => {
                            const typeValue = typeof event.type === "string" ? event.type : event.type.text;
                            const enumValue = this.base.actionButtonTypes[typeValue.toUpperCase()];
                            return enumValue ? enumValue.text : typeValue;
                        }
                    },
                    {
                        command: [
                            {
                                text: "Wijzigen",
                                click: this.onActionButtonGridEditButtonClick.bind(this),
                                visible: function (dataItem) {
                                    // NOTE: Don't use arrow function here because Kendo throws an error with it.
                                    return dataItem.type !== "refreshCurrentItem" && dataItem.type !== "custom";
                                }
                            },
                            { text: "↑", click: this.base.moveUp.bind(this.base) },
                            { text: "↓", click: this.base.moveDown.bind(this.base) },
                            "destroy"],
                        title: " ",
                        width: "170px"
                    }]
            }).data("kendoGrid");

            this.actionButtonGrid = $("#actionButtonGrid").kendoGrid({
                dataSource: {},
                resizable: true,
                toolbar: ["create"],
                editable: {
                    createAt: "bottom"
                },
                remove: (e) => {
                    if (e.model.autoIndex) {
                        this.removeOnAutoIndex(this.fieldOptions, e.model.autoIndex);
                    }
                },
                dataBound: (e) => {
                    if (this.inputTypeSelector.dataItem().text === this.base.inputTypes.ACTIONBUTTON && this.actionButtonGrid.dataSource.data().length >= 1) {
                        $("#actionButtonGrid .k-button.k-grid-add").removeClass("k-grid-add").addClass("k-state-disabled").removeAttr("href");
                    } else {
                        $("#actionButtonGrid .k-button.k-state-disabled").removeClass("k-state-disabled").addClass("k-grid-add").attr("href", "#");
                    }
                },
                columns: [
                    {
                        field: "text",
                        title: "Knop tekst",
                        width: "250px"
                    },
                    {
                        field: "icon",
                        title: "Knop icoon",
                        width: "250px"
                    },
                    {
                        command: [
                            {
                                text: "Wijzigen",
                                click: this.onActionGridEditButtonClick.bind(this)
                            },
                            { text: "↑", click: this.base.moveUp.bind(this.base) },
                            { text: "↓", click: this.base.moveDown.bind(this.base) },
                            "destroy"
                        ],
                        title: " ",
                        width: "250px"
                    }]
            }).data("kendoGrid");

            this.subEntityGridEntity = $("#subEntityGridEntity").kendoDropDownList({
                autoClose: false,
                clearButton: false,
                dataTextField: "name",
                dataValueField: "id",
                filter: "contains",
                minLength: 1,
                dataSource: {}
            }).data("kendoDropDownList");

            this.dataSelectorIdSubEntitiesGrid = $("#dataSelectorIdSubEntitiesGrid").kendoNumericTextBox({
                decimals: 0,
                format: "#",
                min: 0,
                step: 1
            }).data("kendoNumericTextBox");

            this.subEntitiesGridSelectOptions = $("#subEntitiesGridSelectOptions").kendoDropDownList({
                placeholder: "Maak uw keuze...",
                clearButton: false,
                dataSource: [
                    { text: "Geen selectie mogelijk", value: "false" },
                    { text: "De gebruiker kan 1 regel selecteren.", value: "row" },
                    { text: "De gebruiker kan 1 of meer regels selecteren", value: "multiple, row" },
                    { text: "De gebruiker kan 1 cell in het grid selecteren", value: "cell" },
                    { text: "De gebruiker kan 1 of meer cellen in het grid selecteren", value: "multiple, cell" }
                ],
                dataTextField: "text",
                dataValueField: "value"
            }).data("kendoDropDownList");

            //timeline
            this.timelineEntity = $("#timelineEntity").kendoDropDownList({
                autoClose: false,
                dataTextField: "name",
                dataValueField: "id",
                filter: "contains",
                minLength: 1,
                dataSource: {}
            }).data("kendoDropDownList");

            $("#queryId").kendoNumericTextBox({
                decimals: 0,
                min: 0,
                format: "#"
            }).data("kendoNumericTextBox");

            $("#timelineEventHeight").kendoNumericTextBox({
                decimals: 0,
                min: 0,
                format: "#"
            }).data("kendoNumericTextBox");

            // daterange
            $("#daterangeFrom").kendoDatePicker({
                dateInput: true,
                format: "dd-MM-yyyy",
                culture: "nl-NL",
                //change: function (e) {
                change: (changeEvent) => {
                    const tillPicker = $("#daterangeTill").data("kendoDatePicker");
                    if (changeEvent.sender.value() > tillPicker.value()) {
                        tillPicker.value("");
                    }
                    tillPicker.min(changeEvent.sender.value());
                }
            }).data("kendoDatePicker");

            $("#daterangeTill").kendoDatePicker({
                dateInput: true,
                format: "dd-MM-yyyy",
                culture: "nl-NL"
            }).data("kendoDatePicker");

            // set entity dropdown lists 
            this.entitiesCombobox.setDataSource(this.entityList);
            this.dataSourceEntities.setDataSource(this.entityList);
            this.linkedItemEntity.setDataSource(this.entityList);
            this.itemLinkerEntity.setDataSource(this.entityList);
            this.subEntityGridEntity.setDataSource(this.entityList);
            this.timelineEntity.setDataSource(this.entityList);
        }
        /**
         * Function for handling the action grid edit click
         * @param {any} event
         */
        onActionGridEditButtonClick(event) {
            const gridDataItem = this.actionButtonGrid.dataItem(event.currentTarget.closest("tr"));
            let popUpHtml = $("#actionGridPopupHtml");
            let window = popUpHtml.data("kendoWindow");

            if (window) {
                document.getElementById("actionButtonText").value = "";
                document.getElementById("actionButtonIcon").value = "";
                $("#actionGridPopupHtml").show();
                //  action button actions grid
                const settings = this.actionButtonActionsGridDataSourceSettings;
                settings.data = [];
                this.actionButtonActionsGrid.setDataSource(settings);
            } else {
                window = $("#actionGridPopupHtml").kendoWindow({
                    width: 1000,
                    height: 800
                }).data("kendoWindow");
                $(".actionGridSave").kendoButton({
                    icon: "save"
                });
            }

            $(".actionGridSave").unbind("click").bind("click", () => {
                gridDataItem.button = {};
                gridDataItem.text = document.getElementById("actionButtonText").value;
                gridDataItem.icon = document.getElementById("actionButtonIcon").value;
                gridDataItem.button.actions = [];
                const abag = this.actionButtonActionsGrid.dataSource.data();
                const emptyActions = [];
                for (let i = 0; i < abag.length; i++) {
                    let action = abag[i].action;
                    if (abag[i].type === "refreshCurrentItem" || abag[i].type === "custom") {
                        action = { type: abag[i].type };
                    } else if (!action) {
                        emptyActions.push(this.base.actionButtonTypes[abag[i].type.toUpperCase()].text || abag[i].type);
                    }

                    gridDataItem.button.actions.push(action);
                }
                if (emptyActions.length) {
                    this.base.openDialog("Sluiten?",
                        `U heeft bij actie: ${emptyActions.join()} niets ingevuld, wilt u opslaan en het venster sluiten?`,
                        this.base.kendoPromptType.CONFIRM).then(() => {
                            window.close();
                            this.actionButtonGrid.refresh();
                        });
                } else {
                    window.close();
                    this.actionButtonGrid.refresh();
                }
            });

            if (gridDataItem.button) {
                // set action button actions grid to the appropriate fields/settings
                const actionsArray = gridDataItem.button.actions;
                const actions = [];
                for (let i = 0; i < actionsArray.length; i++) {
                    if (!actionsArray[i]) {
                        continue;
                    }
                    actions.push({
                        type: actionsArray[i].type,
                        action: actionsArray[i]
                    });
                }

                const ds = this.actionButtonActionsGridDataSourceSettings;
                ds.data = actions;
                this.actionButtonActionsGrid.setDataSource(ds);
            }
            const name = gridDataItem.text || "";
            document.getElementById("actionButtonText").value = name;
            document.getElementById("actionButtonIcon").value = gridDataItem.icon || "";
            window.title(`Knop: ${name}`);
            window.center().open();
        }

        /**
         * Function for handling the action BUTTON grid edit click
         * @param {any} event
         */
        onActionButtonGridEditButtonClick(event) {
            const gridDataItem = this.actionButtonActionsGrid.dataItem(event.currentTarget.closest("tr"));

            // init fields
            let popUpHtml = $("#actionButtonPopupHtml");
            let window = popUpHtml.data("kendoWindow");
            let tabStrip = popUpHtml.find(".tabStripActionButton");
            let userParametersGrid = popUpHtml.find(".actionButtonUserParametersGrid");
            let itemLink = popUpHtml.find(".actionButtonItemLink");
            let actionQueryId = popUpHtml.find("#actionButtonQueryItemId");
            let dataSelectorId = popUpHtml.find("#dataSelectorId");
            let actionButtonUrlWindowHeight = popUpHtml.find("#actionButtonUrlWindowHeight");
            let actionButtonUrlWindowWidth = popUpHtml.find("#actionButtonUrlWindowWidth");
            let contentItemId = popUpHtml.find("#contentItemId");
            let emailDataQueryId = popUpHtml.find("#emailDataQueryId");
            let actionButtonUrlWindowOpen = popUpHtml.find("#actionButtonUrlWindowOpen");

            const showFields = (fieldType) => {
                const fieldTypes = this.base.fieldTypesDropDown;
                switch (fieldType) {
                    case fieldTypes.COMBOBOX.id:
                        const cbFields = ["dataSource", "queryId", "userTypes", "dataTextField", "dataValueField"];
                        cbFields.forEach((v) => {
                            this.userParametersGrid.showColumn(v);
                        });
                        window.setOptions({
                            width: 1200,
                            height: 800
                        });
                        window.maximize();
                        break;
                }
            };

            if (window) {
                // open window and set to default
                $("#actionButtonPopupHtml").show();
                window.setOptions({
                    width: 1000,
                    height: 800
                });
                // set to fields default
                this.actionButtonItemLink.value("");
                document.getElementById("actionButtonItemId").value = "";
                // url
                document.getElementById("actionButtonUrl").value = "";
                this.actionButtonUrlWindowOpen.select(0);
                this.actionButtonUrlWindowHeight.value("");
                this.actionButtonUrlWindowWidth.value("");
                // query
                this.actionButtonQueryItemId.value("");
                // hide extra fields
                popUpHtml.find("[data-visible]").hide();
                // empty generate file fields
                this.dataSelectorId.value("");
                this.contentItemId.value("");
                this.emailDataQueryId.value("");
                document.getElementById("contentPropertyName").value = "";
                document.getElementById("pdfBackgroundPropertyName").value = "";
                document.getElementById("pdfDocumentOptionsPropertyName").value = "";
                document.getElementById("pdfFilename").value = "";
                // reset user parameters grid
                const resetDs = this.userParametersGridDataSourceSettings;
                resetDs.data = [];
                this.userParametersGrid.setDataSource(resetDs);
            }
            else {
                window = $("#actionButtonPopupHtml").kendoWindow({
                    width: 1000,
                    height: 800
                }).data("kendoWindow");

                tabStrip.kendoTabStrip({
                    select: (e) => {
                        if (e.item.dataset.visible === "generateFile") {
                            // show execute query tab if generateFile is selected, because we need the same properties.
                            tabStrip.data("kendoTabStrip").activateTab(e.item.parentElement.querySelector("[data-visible*=executeQuery]"));
                            // get tab content of selected 
                            const content = tabStrip.data("kendoTabStrip").contentElement(tabStrip.data("kendoTabStrip").select().index());
                            $(content).find("[data-visible=generateFile]").show();
                        }
                    },
                    animation: {
                        open: { effects: "fadeIn" }
                    }
                });

                const fieldTypeDropDownList = (container, options) => {
                    $('<input required name="' + options.field + '"/>')
                        .appendTo(container)
                        .kendoDropDownList({
                            autoBind: false,
                            valuePrimitive: true,
                            dataTextField: "text",
                            dataValueField: "id",
                            change: (me) => {
                                const dataItem = me.sender.dataItem();
                                showFields(dataItem.id);

                            },
                            dataSource: this.createDataSourceFromEnum(this.base.fieldTypesDropDown, true)
                        });
                };

                this.userParametersGridDataSourceSettings = {
                    schema: {
                        model: {
                            fields: {
                                fieldType: { defaultValue: this.base.fieldTypesDropDown["INPUT"], type: "object" },
                                fieldTypeId: { from: "fieldType.id" },
                                queryId: { type: "number" },
                                gridHeight: { type: "number" }
                            }
                        }
                    }
                };
                this.userParametersGrid = userParametersGrid.kendoGrid({
                    dataSource: this.userParametersGridDataSourceSettings,
                    resizable: true,
                    remove: (e) => {
                        if (e.model.autoIndex) {
                            this.removeOnAutoIndex(this.fieldOptions, e.model.autoIndex);
                        }
                    },
                    toolbar: ["create"],
                    editable: {
                        createAt: "bottom"
                    },
                    columns: [
                        {
                            field: "name",
                            title: "Naam parameter"
                        },
                        {
                            field: "question",
                            title: "Vraagtekst"
                        },
                        {
                            field: "fieldTypeId",
                            title: "Veldtype",
                            editor: fieldTypeDropDownList,
                            template: (event) => {
                                return event.fieldTypeId === "" ? event.fieldType.text : this.base.fieldTypesDropDown[event.fieldTypeId.toUpperCase()].text;
                            }
                        },
                        {
                            field: "value",
                            title: "Standaard waarde"
                        },
                        {
                            field: "format",
                            title: "Format"
                        },
                        {
                            field: "queryId",
                            title: "Query id"
                        },
                        {
                            field: "gridHeight",
                            title: "Grid hoogte"
                        },
                        {
                            field: "dataTextField",
                            title: "Data tekst veld", hidden: true
                        },
                        {
                            field: "dataValueField",
                            title: "Data waarde veld", hidden: true
                        },
                        {
                            // todo what to do with this?
                            field: "dataSource",
                            title: "DataSource", hidden: true
                        },
                        {
                            // todo set user types to multi select
                            field: "userTypes",
                            title: "Gebruiker types", hidden: true
                        },
                        {
                            command: [
                                { text: "↑", click: this.base.moveUp.bind(this.base) },
                                { text: "↓", click: this.base.moveDown.bind(this.base) },
                                "destroy"
                            ]
                        }

                    ]
                }).data("kendoGrid");

                this.actionButtonItemLink = itemLink.kendoNumericTextBox({
                    decimals: 0,
                    min: 0,
                    format: "#"
                }).data("kendoNumericTextBox");

                this.actionButtonQueryItemId = actionQueryId.kendoNumericTextBox({
                    decimals: 0,
                    min: 0,
                    format: "#"
                }).data("kendoNumericTextBox");

                this.dataSelectorId = dataSelectorId.kendoNumericTextBox({
                    decimals: 0,
                    min: 0,
                    format: "#"
                }).data("kendoNumericTextBox");

                this.contentItemId = contentItemId.kendoNumericTextBox({
                    decimals: 0,
                    min: 0,
                    format: "#"
                }).data("kendoNumericTextBox");

                this.emailDataQueryId = emailDataQueryId.kendoNumericTextBox({
                    decimals: 0,
                    min: 0,
                    format: "#"
                }).data("kendoNumericTextBox");

                $(".actionButtonSave").kendoButton({
                    icon: "save"
                });

                this.actionButtonUrlWindowHeight = actionButtonUrlWindowHeight.kendoNumericTextBox({
                    decimals: 0,
                    min: 0,
                    format: "#"
                }).data("kendoNumericTextBox");

                this.actionButtonUrlWindowWidth = actionButtonUrlWindowWidth.kendoNumericTextBox({
                    decimals: 0,
                    min: 0,
                    format: "#"
                }).data("kendoNumericTextBox");

                this.actionButtonUrlWindowOpen = actionButtonUrlWindowOpen.kendoDropDownList({
                    clearButton: false,
                    dataTextField: "text",
                    dataValueField: "value",
                    filter: "contains",
                    optionLabel: {
                        value: "",
                        text: "Maak uw keuze..."
                    },
                    dataSource: [
                        { text: "In een apart scherm", value: "window.open" },
                        { text: "In een popup", value: "kendoWindow " }
                    ]
                }).data("kendoDropDownList");
            }
            // bind and unbind to get the appropriate dataitem 
            $(".actionButtonSave").unbind("click").bind("click",
                () => {
                    if (!this.beforeCreateActionDataItem(gridDataItem)) {
                        return;
                    }
                    gridDataItem.action = this.createActionDataItem(gridDataItem);
                    window.close();
                });

            // hide / show elements based on type
            const tagStrip = tabStrip.data("kendoTabStrip");
            const tagGroup = tagStrip.tabGroup;
            tagGroup.children().hide();
            tagGroup.find(`[data-visible*=${gridDataItem.type}]`).show().trigger("click");
            tagStrip.activateTab(tagGroup.find(`[data-visible*=${gridDataItem.type}]`));

            // set properties accordingly
            if (gridDataItem.action) {
                const actionTypes = this.base.actionButtonTypes;
                switch (gridDataItem.type) {
                    case actionTypes.OPENURL.id:
                    case actionTypes.OPENURLONCE.id:
                        document.getElementById("actionButtonUrl").value = gridDataItem.action.url;
                        this.actionButtonUrlWindowOpen.select((dataItem) => { return dataItem.value === gridDataItem.action.openIn; });
                        this.actionButtonUrlWindowWidth.value(gridDataItem.action.windowWidth);
                        this.actionButtonUrlWindowHeight.value(gridDataItem.action.windowHeight);
                        break;
                    case actionTypes.OPENWINDOW.id:
                        document.getElementById("actionButtonItemId").value = gridDataItem.action.itemId;
                        this.actionButtonItemLink.value(gridDataItem.action.linkId);
                        break;
                    case actionTypes.EXECUTEQUERY.id:
                    case actionTypes.EXECUTEQUERYONCE.id:
                    case actionTypes.GENERATEFILE.id:
                        this.actionButtonQueryItemId.value(gridDataItem.action.queryId);
                        let up = gridDataItem.action.userParameters;
                        let rows = [];

                        for (let i = 0; i < up.length; i++) {
                            showFields(up[i].fieldType);
                            rows.push({
                                name: up[i].name,
                                question: up[i].question,
                                fieldType: this.base.fieldTypesDropDown[up[i].fieldTypeId.toUpperCase()] || this.base.fieldTypesDropDown["INPUT"],
                                value: up[i].value,
                                format: up[i].format,
                                dataTextField: up[i].dataTextField,
                                dataValueField: up[i].dataValueField,
                                userTypes: up[i].userTypes,
                                queryId: up[i].queryId,
                                gridHeight: up[i].gridHeight,
                                dataSource: JSON.stringify(up[i].dataSource),
                                autoIndex: up[i].autoIndex
                            });
                        }
                        let userParametersGridDataSourceSettings = this.userParametersGridDataSourceSettings;
                        userParametersGridDataSourceSettings.data = rows;
                        this.userParametersGrid.setDataSource(userParametersGridDataSourceSettings);
                        if (gridDataItem.type === actionTypes.GENERATEFILE.id) {
                            this.dataSelectorId.value(gridDataItem.action.dataSelectorId);
                            this.contentItemId.value(gridDataItem.action.contentItemId);
                            this.emailDataQueryId.value(gridDataItem.action.emailDataQueryId);
                            document.getElementById("contentPropertyName").value = gridDataItem.action.contentPropertyName;
                            document.getElementById("pdfBackgroundPropertyName").value = gridDataItem.action.pdfBackgroundPropertyName;
                            document.getElementById("pdfDocumentOptionsPropertyName").value = gridDataItem.action.pdfDocumentOptionsPropertyName;
                            document.getElementById("pdfFilename").value = gridDataItem.action.pdfFilename;
                        }
                        break;
                }
            }
            window.title("Actie wijzigen");
            window.center().open();
        }
        beforeCreateActionDataItem(dataItem) {
            const actionTypes = this.base.actionButtonTypes;
            const actionType = dataItem.type;
            switch (actionType) {
                case actionTypes.OPENURL.id:
                case actionTypes.OPENURLONCE.id:
                    if (document.getElementById("actionButtonUrl").value === "") {
                        this.base.showNotification("notification", "Voer eerst een url in!", "error");
                        return false;
                    }
                    break;
                case actionTypes.OPENWINDOW.id:
                    var itemid = document.getElementById("actionButtonItemId").value;
                    if ((itemid !== "{itemid}" && !parseInt(itemid)) || itemid === "") {
                        this.base.showNotification("notification", "Voer een numerieke waarde in bij item id!", "error");
                        return false;
                    }
                    break;
                case actionTypes.EXECUTEQUERY.id:
                case actionTypes.EXECUTEQUERYONCE.id:
                case actionTypes.GENERATEFILE.id:
                    var upg = this.userParametersGrid.dataSource.data();
                    for (let i = 0; i < upg.length; i++) {
                        let field = upg[i].fieldType;
                        let typeValue = typeof field === "string" ? field : field.id;
                        let enumValue = this.base.fieldTypesDropDown;
                        switch (enumValue) {
                            case typeValue.toUpperCase():

                                if (upg[i].dataTextField === "") {
                                    this.base.showNotification("notification", "Voer eerst een data tekst veld in!", "error");
                                    return false;
                                }
                                if (upg[i].dataValueField === "") {
                                    this.base.showNotification("notification", "Voer eerst een data waarde veld in!", "error");
                                    return false;
                                }
                                break;
                        }
                    }
                    if (actionType === actionTypes.GENERATEFILE.id) {
                        if (!this.dataSelectorId.value()) {
                            this.base.showNotification("notification", "Voer eerst het data selectie id veld in!", "error");
                            return false;
                        }
                        if (!this.contentItemId.value()) {
                            this.base.showNotification("notification", "Voer eerst het content item id veld in!", "error");
                            return false;
                        }
                        if (document.getElementById("contentPropertyName").value === "") {
                            this.base.showNotification("notification", "Voer eerst het content property naam veld in!", "error");
                            return false;
                        }
                    }
                    //todo add checks
                    break;
            }
            return true;
        }

        createActionDataItem(gridDataItem) {
            const action = {};
            action.type = gridDataItem.type;
            const actionTypes = this.base.actionButtonTypes;
            switch (action.type) {
                case actionTypes.OPENURL.id:
                case actionTypes.OPENURLONCE.id:
                    action.url = document.getElementById("actionButtonUrl").value;
                    action.openIn = this.actionButtonUrlWindowOpen.dataItem().value;
                    action.windowWidth = this.actionButtonUrlWindowWidth.value();
                    action.windowHeight = this.actionButtonUrlWindowHeight.value();
                    break;
                case actionTypes.OPENWINDOW.id:
                    var itemId = document.getElementById("actionButtonItemId").value;
                    action.itemId = !parseInt(itemId) ? itemId : parseInt(itemId);
                    action.linkId = this.actionButtonItemLink.value();
                    break;
                case actionTypes.EXECUTEQUERY.id:
                case actionTypes.EXECUTEQUERYONCE.id:
                case actionTypes.GENERATEFILE.id:
                    // shared among executequery and generate file
                    action.queryId = this.actionButtonQueryItemId.value();
                    action.userParameters = [];
                    var upg = this.userParametersGrid.dataSource.data();
                    for (let i = 0; i < upg.length; i++) {
                        let field = upg[i].fieldTypeId;
                        let typeValue = typeof field === "string" ? field : field.id;
                        let enumValue = typeValue !== "" ? this.base.fieldTypesDropDown[typeValue.toUpperCase()] : this.base.fieldTypesDropDown["INPUT"];
                        action.userParameters.push({
                            name: upg[i].name,
                            question: upg[i].question,
                            fieldType: enumValue ? enumValue.id : typeValue,
                            value: upg[i].value,
                            format: upg[i].format,
                            dataTextField: upg[i].dataTextField,
                            fieldTypeId: field,
                            dataValueField: upg[i].dataValueField,
                            userTypes: upg[i].userTypes,
                            queryId: upg[i].queryId,
                            gridHeight: upg[i].gridHeight,
                            dataSource: !this.base.isJson(upg[i].dataSource) ? JSON.stringify(upg[i].dataSource) : JSON.parse(upg[i].dataSource)
                        });
                    }
                    // generate file specific
                    if (action.type === actionTypes.GENERATEFILE.id) {
                        //todo actions for generatefile
                        action.dataSelectorId = this.dataSelectorId.value();
                        action.contentItemId = this.contentItemId.value();
                        action.contentPropertyName = document.getElementById("contentPropertyName").value;
                        action.pdfBackgroundPropertyName = document.getElementById("pdfBackgroundPropertyName").value;
                        action.pdfDocumentOptionsPropertyName = document.getElementById("pdfDocumentOptionsPropertyName").value;
                        action.pdfFilename = document.getElementById("pdfFilename").value;
                        action.emailDataQueryId = this.emailDataQueryId.value();
                    }
                    break;
            }
            return action;
        }

        // get all tabnames of selected entity
        async onEntitiesComboBoxSelect() {
            if (!this.checkIfEntityIsSet() || !this.tabNameDropDownList || !this.tabNameProperty) {
                return;
            }

            // set tabnames 
            this.setTabNameDropDown();
            // set properties of tab
            this.tabNameDropDownList.one("dataBound", () => {
                this.tabNameDropDownList.select((dataItem) => { return dataItem.tab_name === "Gegevens"; });
            });
        }

        async setTabNameDropDown() {
            this.tabNameDropDownList.text("");
            this.tabNameProperty.text("");
            const tabNames = await $.get(`${this.base.settings.serviceRoot}/GET_ENTITY_PROPERTIES_TABNAMES?entityName=${encodeURIComponent(this.entitiesCombobox.dataItem().id)}&isTest=${encodeURIComponent(this.base.settings.isTestEnvironment)}`);
            this.tabNameDropDownList.setDataSource(tabNames);
            this.tabNameProperty.setDataSource(tabNames);
        }

        // update property ordering
        async updateEntityPropertyOrdering(oldIndex, newIndex, id) {
            await $.ajax({
                url: `${this.base.settings.serviceRoot}/UPDATE_ORDERING_ENTITY_PROPERTY?isTest=${encodeURIComponent(this.base.settings.isTestEnvironment)}`,
                method: "POST",
                data: {
                    oldIndex: oldIndex,
                    newIndex: newIndex,
                    current_id: id,
                    tabName: (this.tabNameDropDownList.dataItem().tab_name === "Gegevens") ? "" : this.tabNameDropDownList.dataItem().tab_name,
                    entityName: this.entitiesCombobox.dataItem().name
                }
            });
        }

        // get entity properties of tab when tabname is selected
        async tabNameDropDownListSelect(event) {
            // check if entity is set, if entityList isnt initialized checkIfEntityIsSet returns true, so we check that as well
            if (!this.checkIfEntityIsSet() || (this.checkIfEntityIsSet() && this.entityListInitialized === false) || !event || !this.checkIfTabNameIsSet()) {
                return;
            }
            let tabName = "";
            tabName = event.sender && event.sender.dataItem() ? event.sender.dataItem().tab_name : event.tab_name;
            tabName = tabName === "Gegevens" || !tabName ? "" : tabName;
            this.listOfTabProperties.setDataSource(new kendo.data.DataSource({
                serverFiltering: true,
                transport: {
                    read: {
                        url: `${this.base.settings.serviceRoot}/GET_ENTITY_PROPERTIES_ADMIN?entityName=${encodeURIComponent(this.entitiesCombobox.dataItem().id)}&tabName=${encodeURIComponent(tabName)}&isTest=${encodeURIComponent(this.base.settings.isTestEnvironment)}`
                    }
                }
            }));
        }

        // get properties of entity and fill fields
        async optionSelected(event) {
            const index = event.sender.select().index();
            const dataItem = event.sender.dataItem(event.sender.select());
            const selectedEntityName = dataItem.entity_name;
            const selectedTabname = dataItem.tab_name;
            if (this.lastSelectedProperty === index && this.lastSelectedTabname === selectedTabname &&  !this.isSaveSelect) {
                this.base.openDialog("Item opnieuw openen", "Wilt u dit item opnieuw openen? (u raakt gewijzigde gegevens kwijt)", this.base.kendoPromptType.CONFIRM).then(() => {
                    // get properties if user accepts to overwrite possible changes made to the same item
                    this.getPropertiesOfSelected(dataItem.id, selectedEntityName, selectedTabname);
                });
            } else {
                this.isSaveSelect = false;
                this.lastSelectedProperty = index;
                this.lastSelectedTabname = selectedTabname;
                this.getPropertiesOfSelected(dataItem.id, selectedEntityName, selectedTabname);
            }
        }

        async getPropertiesOfSelected(id, selectedEntityName, selectedTabName) {
            const results = await $.ajax({
                url: `${this.base.settings.serviceRoot}/GET_ENTITY_PROPERTIES_FOR_SELECTED?isTest=${encodeURIComponent(this.base.settings.isTestEnvironment)}&entityName=${selectedEntityName}&id=${id}`,
                method: "POST",
                data: {
                    id: id,
                    entityName: selectedEntityName,
                    tabName: selectedTabName
                }
            });

            const resultSet = results[0];

            this.groupNameComboBox.setDataSource(new kendo.data.DataSource({
                transport: {
                    read: {
                        url: `${this.base.settings.serviceRoot}/GET_GROUPNAME_FOR_SELECTION?selectedEntityName=${encodeURIComponent(selectedEntityName)}&selectedTabName=${encodeURIComponent(selectedTabName)}&isTest=${encodeURIComponent(this.base.settings.isTestEnvironment)}`
                    }
                }
            }));

            this.dependencyFields.setDataSource(new kendo.data.DataSource({
                transport: {
                    read: {
                        url: `${this.base.settings.serviceRoot}/GET_OPTIONS_FOR_DEPENDENCY?entityName=${encodeURIComponent(selectedEntityName)}&isTest=${encodeURIComponent(this.base.settings.isTestEnvironment)}`
                    }
                }
            }));
            // first set all properties to default;
            this.setPropertiesToDefault();
            // then set all the properties accordingly
            this.setProperties(resultSet);
        }

        // actions handled before save, such as checks
        beforeSave() {
            // check if entity is selected
            if (!this.checkIfEntityIsSet()) {
                return false;
            }

            // check if tab is selected
            if (!this.tabNameDropDownList.dataItem()) {
                this.base.showNotification("notification", "Selecteer eerst een bestaand tab!", "error");
                return false;
            }

            // check if property is selected
            if (this.listOfTabProperties.select().index() === -1) {
                this.base.showNotification("notification", "Selecteer eerst een eigenschap!", "error");
                return false;
            }

            // check if group name isn't too long for db
            if (!this.groupNameComboBox.dataItem() && this.groupNameComboBox.value().length > 100) {
                this.base.showNotification("notification", "Gebruik een groepsnaam die niet langer is dan 100 karakters!", "error");
                return false;
            }

            // check if tab name isn't too long for db
            if (!this.tabNameProperty.dataItem() && this.tabNameProperty.value().length > 100) {
                this.base.showNotification("notification", "Gebruik een tabnaam die niet langer is dan 100 karakters!", "error");
                return false;
            }

            // check if input type is selected
            if ($("#inputtypes").closest(".item").is(":visible") && this.inputTypeSelector.dataItem().id === "") {
                this.base.showNotification("notification", "Selecteer eerst een bestaand invoertype!", "error");
                return false;
            }

            //inputtype specific
            const inputTypes = this.base.inputTypes;
            switch (this.inputTypeSelector.dataItem().text) {
                case inputTypes.NUMERIC:
                    if (this.minNumber.value() && this.minNumber.value() >= this.maxNumber.value()) {
                        this.base.showNotification("notification", "Minimale waarde mag niet hoger zijn dan de maximale waarde!", "error");
                        return false;
                    }
                    break;
                case inputTypes.HTMLEDITOR:
                    // check if html editor format is set, checking with == instead of === because we're checking null and undefined.
                    if ($("[name=html-editor]").is(":visible") && $("[name=html-editor]:checked").val() == null) {
                        this.base.showNotification("notification", "Selecteer soort html editor - opmaak!", "error");
                        return false;
                    }
                    break;
                case inputTypes.DATETIMEPICKER:
                    // check if datetime dropdown is set
                    if ($("#dateTimeDropDown").closest(".item").is(":visible") && $("#dateTimeDropDown").data("kendoDropDownList").dataItem().value === "") {
                        this.base.showNotification("notification", "Selecteer soort datum/tijd picker!", "error");
                        return false;
                    }
                    break;
                case inputTypes.SECUREINPUT:
                    if ($("#securityMethod").closest(".item").is(":visible") && $("#securityMethod").data("kendoDropDownList").value() === "") {
                        this.base.showNotification("notification", "Selecteer soort beveiligingsmethode!", "error");
                        return false;
                    }
                    break;
                case inputTypes.LINKEDITEM:
                    if ($("#linkedItemEntity").closest(".item").is(":visible") && this.linkedItemEntity.value() === "") {
                        this.base.showNotification("notification", "Selecteer soort entiteit om te linken!", "error");
                        return false;
                    }

                    break;
                case inputTypes.ACTIONBUTTON:
                    break;
                case inputTypes.SUBENTITIESGRID:
                    if (this.subEntityGridEntity.value() === "") {
                        this.base.showNotification("notification", "Selecteer soort entiteit om te linken!", "error");
                        return false;
                    }
                    if (this.subEntitiesGridSelectOptions.value() === "") {
                        this.base.showNotification("notification", "Kies een selecteer optie!", "error");
                        return false;
                    }
                    if (document.getElementById("customQuery").checked && this.queryFieldSubEntities.getValue() === "") {
                        this.base.showNotification("notification", "Voer iets in bij query!", "error");
                        return false;
                    }
                    if (document.getElementById("hasCustomDeleteQuery").checked && this.queryDeleteField.getValue() === "") {
                        this.base.showNotification("notification", "Voer iets in bij de delete query!", "error");
                        return false;
                    }
                    if (document.getElementById("hasCustomUpdateQuery").checked && this.queryUpdateField.getValue() === "") {
                        this.base.showNotification("notification", "Voer iets in bij de update query!", "error");
                        return false;
                    }
                    if (document.getElementById("hasCustomInsertQuery").checked && this.queryInsertField.getValue() === "") {
                        this.base.showNotification("notification", "Voer iets in bij de insert query!", "error");
                        return false;
                    }
                    break;
                case inputTypes.TIMELINE:
                    if (!$("#queryId").data("kendoNumericTextBox").value()) {
                        this.base.showNotification("notification", "Vul een query id in!", "error");
                        return false;
                    }
                    //we need to select a entity type if we dont disable opening items
                    if (!this.timelineEntity.dataItem()) {
                        this.base.showNotification("notification", "Selecteer soort entiteit om te linken wanneer de items te openen zijn!", "error");
                        return false;
                    }
                    break;
                case inputTypes.DATERANGE:
                    // check if dates are set
                    if (!$("#daterangeFrom").data("kendoDatePicker").value() || !$("#daterangeTill").data("kendoDatePicker").value()) {
                        this.base.showNotification("notification", "Selecteer een datum!", "error");
                        return false;
                    }
                    // check if till date is later than the from date
                    if ($("#daterangeFrom").data("kendoDatePicker").value() > $("#daterangeTill").data("kendoDatePicker").value()) {
                        this.base.showNotification("notification", "Selecteer een datum die na de 'Van' datum ligt!", "error");
                        return false;
                    }
                    break;
                case inputTypes.QUERYBUILDER:
                    if (!$("#queryId").data("kendoNumericTextBox").value()) {
                        this.base.showNotification("notification", "Vul een query id in!", "error");
                        return false;
                    }
                    break;
                case inputTypes.CHART:
                    var jsonField = this.jsonField.getValue();
                    if (jsonField === "" || !this.base.isJson(jsonField)) {
                        this.base.showNotification("notification", "Vul de json data in van de chart opties!", "error");
                        return false;
                    }
                    break;
            }

            this.isSaveSelect = true;
            // if everything went right, we move on to the save function.
            this.save();
        }

        // save entity properties to database
        async save() {
            // create entity property model
            const entityProperties = new EntityPropertyModel();
            let index = this.listOfTabProperties.select().index();
            let dataItem = this.listOfTabProperties.dataSource.view()[index];
            entityProperties.id = dataItem.id;
            entityProperties.entity_name = this.entitiesCombobox.value();
            entityProperties.visible_in_overview = document.getElementById("visible-in-table").checked;
            entityProperties.overview_width = this.widthInTable.value();
            entityProperties.tab_name = this.tabNameProperty.value();
            entityProperties.group_name = this.groupNameComboBox.value();
            entityProperties.inputtype = this.inputTypeSelector.dataItem().text;
            entityProperties.display_name = $("#displayname").val();
            entityProperties.property_name = $("#propertyname").val();
            entityProperties.explanation = $("textarea#explanation").val();
            entityProperties.regex_validation = $('#regexValidation').val();
            entityProperties.mandatory = $("#mandatory").is(":checked");
            entityProperties.readonly = $("#readonly").is(":checked");
            entityProperties.width = $("#width").data("kendoNumericTextBox").value();
            entityProperties.height = $("#height").data("kendoNumericTextBox").value();
            entityProperties.depends_on_field = this.dependencyFields.value();
            entityProperties.depends_on_operator = this.dependingFilter.value();
            entityProperties.depends_on_value = $("#dependingValue").val();
            entityProperties.language_code = $('#langCode').val();
            // get value through codemirror function getValue() because textarea is empty
            entityProperties.custom_script = this.scriptField.getValue();
            entityProperties.css = this.cssField.getValue();
            entityProperties.also_save_seo_value = document.getElementById("seofriendly").checked;
            entityProperties.default_value = $('#defaultValue').val();

            // declare empty options
            entityProperties.options = {};

            //inputtype specific
            const inputTypes = this.base.inputTypes;
            switch (entityProperties.inputtype) {
                case inputTypes.RADIOBUTTON:
                    entityProperties.default_value = $("#checkedCheckbox").data("kendoComboBox").value();
                    entityProperties.data_query = this.queryContentField.getValue();
                    break;
                case inputTypes.CHECKBOX:
                    // set default value to checkbox checked(1) or unchecked (0)
                    entityProperties.default_value = $("#checkedCheckbox").data("kendoComboBox").value();
                    break;
                case inputTypes.NUMERIC:
                    entityProperties.options.decimals = this.numberOfDec.value();
                    entityProperties.options.format = this.numberFormat.value() === "anders" ? document.getElementById("differentFormat").value : this.numberFormat.value();
                    entityProperties.options.round = document.getElementById("roundNumeric").checked;
                    entityProperties.options.max = this.maxNumber.value();
                    entityProperties.options.min = this.minNumber.value();
                    entityProperties.options.step = this.stepNumber.value() || 1;
                    entityProperties.options.factor = this.factorNumber.value() || 1;
                    var culture = document.getElementById("cultureNumber").value;
                    entityProperties.options.culture = culture === "" ? null : culture;
                    entityProperties.default_value = $("#defaultNumeric").val();
                    break;
                case inputTypes.AUTOINCREMENT:
                    entityProperties.default_value = $("#defaultNumeric").val();
                    break;
                case inputTypes.HTMLEDITOR:
                    entityProperties.options.mode = parseInt($("[name=html-editor]:checked").val());
                    break;
                case inputTypes.DATETIMEPICKER:
                    entityProperties.options.type = $("#dateTimeDropDown").data("kendoDropDownList").value();
                    // we only need a value if checkbox is checked and type is date or datetime
                    entityProperties.options.value = document.getElementById("dateTimePickerSetNow").checked && (entityProperties.options.type === "date" || entityProperties.options.type === "datetime") ? "NOW()" : null;
                    //getting mintime by value of the element, not the kendo element; it returns a full datetime range.
                    var minTime = document.getElementById("minimumTime").value;
                    // we only need minimum time if type is type or datetime
                    entityProperties.options.min = (entityProperties.options.type === "time" || entityProperties.options.type === "datetime") && minTime !== "" ? minTime : null;
                    break;
                case inputTypes.COMBOBOX:
                case inputTypes.MULTISELECT:
                    if (entityProperties.inputtype === inputTypes.COMBOBOX) {
                        entityProperties.options.useDropDownList = document.getElementById("useDropDownList").checked;
                    } else {
                        entityProperties.options.useDropDownList = null;
                    }
                    // check if panel 1 is selected, which is "Vaste waardes"
                    if (this.dataSourceFilter.dataItem().id === this.base.dataSourceType.PANEL1.id) {
                        var data = this.grid.dataSource.data();
                        var dataSource = [];
                        // specific check if all itemrows are filled.
                        for (var i = 0; i < data.length; i++) {
                            if (data[i].id == null || data[i].id === "" || data[i].name == null || data[i].name === "") {
                                this.base.showNotification("notification", `Vul bij "Vaste waardes" alle items met naam en id in!`, "error");
                                return;
                            }
                            dataSource.push({ id: data[i].id, name: data[i].name });
                        }
                        entityProperties.options.dataSource = dataSource;
                        entityProperties.options.entityType = null;
                        entityProperties.options.searchInTitle = null;
                        entityProperties.options.searchEverywhere = null;

                        // check if panel 2 is selected, which is "Lijst van entiteiten"
                    } else if (this.dataSourceFilter.dataItem().id === this.base.dataSourceType.PANEL2.id) {
                        // check if entity to search for is set, show error if not
                        if (!this.dataSourceEntities.dataItem()) {
                            this.base.showNotification("notification", `Selecteer eerst een entiteit waar naar gezocht moet worden!`, "error");
                            return;
                        }
                        entityProperties.options.entityType = this.dataSourceEntities.dataItem().id;
                        entityProperties.options.dataSource = null;
                        entityProperties.options.searchInTitle = document.getElementById("searchInTitle").checked;
                        entityProperties.options.searchEverywhere = document.getElementById("searchEverywhere").checked;
                        entityProperties.options.searchFields = this.searchFields.value();

                        // overwrite the preserved options, else the options would 
                        this.fieldOptions.searchFields = this.searchFields.value();

                        // check if panel 1 is selected, which is "Query"
                    } else if (this.dataSourceFilter.dataItem().id === this.base.dataSourceType.PANEL3.id) {
                        // get value through codemirror function getValue() because textarea is empty
                        entityProperties.data_query = this.queryField.getValue();
                        entityProperties.options.dataSource = null;
                        entityProperties.options.entityType = null;
                        entityProperties.options.searchInTitle = null;
                        entityProperties.options.searchEverywhere = null;
                    }
                    break;
                case inputTypes.SECUREINPUT:
                    entityProperties.options.type = $("#typeSecureInput").data("kendoDropDownList").value();
                    entityProperties.options.securityMethod = $("#securityMethod").data("kendoDropDownList").value();
                    // set securitykey, but only when security method is JCL_AES
                    if (entityProperties.options.securityMethod === "JCL_AES" || entityProperties.options.securityMethod === "AES") {
                        entityProperties.options.securityKey = document.getElementById("securityKey").value;
                    } else {
                        entityProperties.options.securityKey = null;
                    }
                    break;
                case inputTypes.LINKEDITEM:
                    entityProperties.options.entityType = this.linkedItemEntity.value();
                    entityProperties.options.template = document.getElementById("linkedItemTemplate").value;
                    entityProperties.options.noLinkText = document.getElementById("noLinkText").value;
                    entityProperties.options.reverse = document.getElementById("reverse").checked;
                    entityProperties.options.hideFieldIfNoLink = document.getElementById("hideFieldIfNoLink").checked;
                    entityProperties.options.textOnly = document.getElementById("textOnly").checked;
                    // no check if filled because these are not required properties
                    // use dataItem().type because value() gives back the string value instead of int value
                    entityProperties.options.linkType = !this.linkType.dataItem() && this.linkType.value() === "" ? 1 : (!this.linkType.dataItem() ? parseInt(this.linkType.value()) : this.linkType.dataItem().type_value);
                    break;
                case inputTypes.DATASELECTOR:
                    entityProperties.options.text = document.getElementById("dataSelectorText").value;
                    break;
                case inputTypes.ITEMLINKER:
                case inputTypes.SUBENTITIESGRID:
                case inputTypes.ACTIONBUTTON:

                    // shared properties through out sub entities grid and item linker
                    if (entityProperties.inputtype !== inputTypes.ACTIONBUTTON) {
                        // check if set, if not use the manual input
                        entityProperties.options.linkTypeNumber = (!this.itemLinkerTypeNumber.dataItem() && this.itemLinkerTypeNumber.value() === "" ? "" : (!this.itemLinkerTypeNumber.dataItem() ? this.itemLinkerTypeNumber.value() : this.itemLinkerTypeNumber.dataItem().type_value));
                        entityProperties.options.hideCommandColumn = document.getElementById("hideCommandColumn").checked;
                        entityProperties.options.disableInlineEditing = document.getElementById("disableInlineEditing").checked;
                        entityProperties.options.disableOpeningOfItems = document.getElementById("disableOpeningOfItems").checked;
                        entityProperties.options.deletionOfItems = $("#itemLinkerDeletionOfItems").data("kendoDropDownList").value();
                        // toolbar is an extra object within the options
                        entityProperties.options.toolbar = {};
                        entityProperties.options.toolbar.hideExportButton = document.getElementById("hideExportButton").checked;
                        entityProperties.options.toolbar.hideCheckAllButton = document.getElementById("hideCheckAllButton").checked;
                        entityProperties.options.toolbar.hideUncheckAllButton = document.getElementById("hideUncheckAllButton").checked;
                        entityProperties.options.toolbar.hideCreateButton = document.getElementById("hideCreateButton").checked;
                    }

                    // module id is only available for item linker 
                    // entity is a multiselect for item linker
                    if (entityProperties.inputtype === inputTypes.ITEMLINKER) {
                        const moduleId = $("#itemLinkerModuleId").data("kendoNumericTextBox").value();
                        // 0 is the default value
                        entityProperties.options.moduleId = moduleId === "" ? 0 : moduleId;
                        // .value returns a string array of all selected entities
                        entityProperties.options.entityTypes = this.itemLinkerEntity.value();

                        // overwrite the preserved options, else the options would 
                        this.fieldOptions.entityTypes = this.itemLinkerEntity.value();

                        // order by is itemlinker only
                        entityProperties.options.orderBy = document.getElementById("itemLinkerOrderBy").value;
                    } else {
                        const buttons = [];
                        const actionGridData = this.actionButtonGrid.dataSource.data();
                        // loop through the data of the actionButtonGrid
                        for (let i = 0; i < actionGridData.length; i++) {
                            // create action array
                            const actions = [];
                            const actionButtonData = (actionGridData[i].button ? actionGridData[i].button.actions : []);
                            // loop through actions defined in the dataItem of the currently iterated item
                            for (let i = 0; i < actionButtonData.length; i++) {
                                // push nothing if no type is selected
                                if (!actionButtonData[i] || actionButtonData[i].type === "") {
                                    continue;
                                    // push just the type if type is refreshcurrentitem or custom
                                } else {
                                    actions.push(actionButtonData[i]);
                                }
                            }
                            buttons.push({
                                text: actionGridData[i].text,
                                icon: actionGridData[i].icon,
                                actions: actions
                            });
                        }
                        if (entityProperties.inputtype === inputTypes.ACTIONBUTTON) {
                            if (buttons.length === 0 || buttons[0].actions.length === 0) {
                                console.warn("entityProperties.options.actions is missing!", entityProperties.options);
                                this.base.showNotification("notification", `Item is niet succesvol toegevoegd, actie(s) ontbreken, probeer het opnieuw`, "error");
                                return;
                            }
                            entityProperties.options.text = buttons[0].text || "";
                            entityProperties.options.icon = buttons[0].icon || "";
                            entityProperties.options.actions = buttons[0].actions;
                        } else {
                            // toolbar object has been created above already, but still we're checking it once more.
                            if (!entityProperties.options.toolbar) {
                                console.warn("entityProperties.options.toolbar is missing!", entityProperties.options);
                                this.base.showNotification("notification", `Item is niet succesvol aangepast, probeer het opnieuw`, "error");
                                return;
                            }
                            entityProperties.options.toolbar.customActions = buttons;
                        }
                    }

                    // properties for sub entities grid
                    if (entityProperties.inputtype === inputTypes.SUBENTITIESGRID) {
                        entityProperties.options.dataSelectorId = this.dataSelectorIdSubEntitiesGrid.value();
                        entityProperties.options.entityType = this.subEntityGridEntity.value();
                        entityProperties.options.selectable = (this.subEntitiesGridSelectOptions.value() === "false") ? false : this.subEntitiesGridSelectOptions.value();
                        entityProperties.options.refreshGridAfterInlineEdit = document.getElementById("refreshGridAfterInlineEdit").checked;
                        entityProperties.options.showDeleteConformations = document.getElementById("showDeleteConformations").checked;
                        entityProperties.options.checkboxes = document.getElementById("checkboxes").checked;

                        entityProperties.options.showChangedByColumn = document.getElementById("showChangedByColumn").checked;
                        entityProperties.options.showChangedOnColumn = document.getElementById("showChangedOnColumn").checked;
                        entityProperties.options.showAddedByColumn = document.getElementById("showAddedByColumn").checked;
                        entityProperties.options.showAddedOnColumn = document.getElementById("showAddedOnColumn").checked;

                        entityProperties.options.customQuery = document.getElementById("customQuery").checked;
                        if (entityProperties.options.customQuery) {
                            entityProperties.data_query = this.queryFieldSubEntities.getValue();
                        }

                        entityProperties.options.hasCustomDeleteQuery = document.getElementById("hasCustomDeleteQuery").checked;
                        if (entityProperties.options.hasCustomDeleteQuery) {
                            entityProperties.grid_delete_query = this.queryDeleteField.getValue();
                        }

                        entityProperties.options.hasCustomUpdateQuery = document.getElementById("hasCustomUpdateQuery").checked;
                        if (entityProperties.options.hasCustomUpdateQuery) {
                            entityProperties.grid_update_query = this.queryUpdateField.getValue();
                        }

                        entityProperties.options.hasCustomInsertQuery = document.getElementById("hasCustomInsertQuery").checked;
                        if (entityProperties.options.hasCustomInsertQuery) {
                            entityProperties.grid_insert_query = this.queryInsertField.getValue();
                        }

                        entityProperties.options.disableInlineEditing = document.getElementById("disableInlineEditing").checked;
                        entityProperties.options.disableOpeningOfItems = document.getElementById("disableOpeningOfItems").checked;
                        entityProperties.options.hideTitleColumn = document.getElementById("hideTitleColumn").checked;
                        entityProperties.options.hideEnvironmentColumn = document.getElementById("hideEnvironmentColumn").checked;
                        entityProperties.options.hideTypeColumn = document.getElementById("hideTypeColumn").checked;
                        entityProperties.options.hideLinkIdColumn = document.getElementById("hideLinkIdColumn").checked;
                        entityProperties.options.hideIdColumn = document.getElementById("hideIdColumn").checked;
                        entityProperties.options.hideTitleFieldInWindow = document.getElementById("hideTitleFieldInWindow").checked;
                        entityProperties.options.toolbar.hideLinkButton = document.getElementById("hideLinkButton").checked;
                        entityProperties.options.toolbar.hideCount = document.getElementById("hideCount").checked;
                        entityProperties.options.toolbar.hideClearFiltersButton = document.getElementById("hideClearFiltersButton").checked;
                    }
                    break;
                case inputTypes.TIMELINE:
                    entityProperties.options.entityType = this.timelineEntity.dataItem().id;
                    entityProperties.options.queryId = $("#queryId").data("kendoNumericTextBox").value();
                    entityProperties.options.eventHeight = $("#timelineEventHeight").data("kendoNumericTextBox").value() || 600;
                    entityProperties.options.disableOpeningOfItems = document.getElementById("disableOpeningOfItemsTimeLine").checked;
                    break;
                case inputTypes.FILEUPLOAD:
                    entityProperties.options.validation = {};
                    entityProperties.options.queryId = $("#queryId").data("kendoNumericTextBox").value();
                    entityProperties.options.multiple = document.getElementById("allowMultipleFiles").checked;
                    const allowedExtensions = document.getElementById("allowedExtensions").value;
                    console.log("allowedExtensions", allowedExtensions);
                    const extentionCount = (allowedExtensions && allowedExtensions !== '') ? allowedExtensions.split(',').length : 0;
                    console.log("extentionCount", extentionCount);
                    if (extentionCount > 0)
                        entityProperties.options.validation.allowedExtensions = allowedExtensions.split(',');
                    else
                        this.fieldOptions.validation.allowedExtensions = [];
                    break;
                case inputTypes.SCHEDULER:
                    entityProperties.options.queryId = $("#queryId").data("kendoNumericTextBox").value();
                    break;
                case inputTypes.DATERANGE:
                    // TODO how to save values?
                    // kendo.toString(new Date(), "MM/dd/yyyy")
                    entityProperties.options.from = $("#daterangeFrom").data("kendoDatePicker").value();
                    entityProperties.options.till = $("#daterangeTill").data("kendoDatePicker").value();
                    break;
                case inputTypes.QUERYBUILDER:
                    // TODO is this the correct way?
                    entityProperties.options.queryId = $("#queryId").data("kendoNumericTextBox").value();
                    break;
                case inputTypes.CHART:
                    // options field is already set in the field
                    entityProperties.options = this.jsonField.getValue();
                    break;
                case inputTypes.TEXTBOX:
                    entityProperties.options.type = this.textboxTypeDropDown.dataItem() && this.textboxTypeDropDown.dataItem().id !== "" ? this.textboxTypeDropDown.dataItem().id : null;
                    break;
                case inputTypes.QR:
                    entityProperties.options.size = parseInt((document.getElementById("pixelSize").value == null || document.getElementById("pixelSize").value == "") ? 250: document.getElementById("pixelSize").value);
                    // get value through codemirror function getValue() because textarea is empty
                    entityProperties.data_query = this.queryContentField.getValue();
                    break;
            }

            function clearAutoIncIdsFromObject(targetObject = {}) {
                for (let prop in targetObject) {
                    if (targetObject.hasOwnProperty(prop)) {
                        const value = targetObject[prop];
                        if (prop === "autoIndex") delete targetObject[prop];
                        if (typeof value === "object") clearAutoIncIdsFromObject(value);
                    }
                }
            }

            // we create the json for chart in the module
            if (entityProperties.inputtype !== inputTypes.CHART) {
                // when the admin tool hasnt been updated to handle options that might appear in the options json, dont want to lose any options that were entered previously
                entityProperties.options = $.extend(true, this.fieldOptions, entityProperties.options);

                clearAutoIncIdsFromObject(entityProperties.options);
                // populate options field with json
                entityProperties.createOptionsJson();
            }


            document.querySelector(".loaderWrap").classList.add("active");
            // save to database
            $.post(`${this.base.settings.serviceRoot}/SAVE_INITIAL_VALUES?isTest=${encodeURIComponent(this.base.settings.isTestEnvironment)}`, entityProperties)
                .done(() => {
                    this.base.showNotification("notification", `Item succesvol aangepast`, "success");
                    this.afterSave(entityProperties);
                    document.querySelector(".loaderWrap").classList.remove("active");
                })
                .fail(() => {
                    this.base.showNotification("notification", `Item is niet succesvol aangepast, probeer het opnieuw`, "error");
                });
        }

        selectPropertyInListView(displayName) {
            // remove selected class
            $(".sortable").removeClass("selected");
            const elementToSelect = this.listOfTabProperties.element.find(`[data-display-name='${displayName}']`);
            // select element in listview
            this.listOfTabProperties.select(elementToSelect);
            // add selected class
            elementToSelect.addClass('selected');
        }

        removeOnAutoIndex(targetObject = {}, autoIndexId = 0) {
            let found = false;

            const findIndex = (target, index, parent = null, targetName = null) => {
                if (found) return;
                for (let prop in target) {
                    if (target.hasOwnProperty(prop)) {
                        if (found) return;
                        const value = target[prop];
                        if (prop === "autoIndex" && value == index) {
                            console.log("FOUND!!");
                            // Found, remove depending on type
                            if (jjl.utils.isArray(parent)) {
                                // base type is an array item
                                parent.splice(parent.findIndex(e => e === target), 1);
                            } else {
                                // Just a property
                                delete parent[targetName];
                            }
                            found = true;
                            return;
                        }

                        if (jjl.utils.isArray(value)) {
                            // Loop through array items
                            for (let arrItem of value) {
                                if (found) return;
                                if (typeof value === "object") findIndex(value, index, target, null);
                            }
                        } else if (typeof value === "object") {
                            findIndex(value, index, target, prop);
                        }
                    }
                }
            }

            findIndex(targetObject, autoIndexId);

            console.log(found ? "Found and removed.." : "Object not found");
            return found;
        }

        // actions handled after saving, selecting right tab and such
        afterSave(entityProperties) {
            const selectCorrectListViewItem = () => {
                // select correct entity display name
                this.listOfTabProperties.one("dataBound",
                    () => {
                        this.selectPropertyInListView(entityProperties.display_name);
                    });
            };

            // only update tabname list if tab has been added/changed
            if (this.tabNameDropDownList.value() !== entityProperties.tab_name) {
                // trigger select to get the new tab in the list
                this.onEntitiesComboBoxSelect();
                this.tabNameDropDownList.one("dataBound", (event) => {
                    // automatically select the newly added tab 
                    this.tabNameDropDownList.select((dataItem) => {
                        return dataItem.tab_name === (entityProperties.tab_name === "" ? "Gegevens" : entityProperties.tab_name);
                    });
                    selectCorrectListViewItem();
                });
            }

            const index = this.listOfTabProperties.select().index();
            const dataItem = this.listOfTabProperties.dataSource.view()[index];
            // only update tabname properties list if display name has been changed
            if (this.tabNameDropDownList.value() === entityProperties.tab_name && dataItem !== null && dataItem !== undefined && dataItem.display_name !== entityProperties.display_name) {
                this.tabNameDropDownListSelect(dataItem);
                selectCorrectListViewItem();
            } else if (dataItem !== null && dataItem !== undefined && this.tabNameDropDownList.value() === entityProperties.tab_name) {
                // update if name and tab havent been changed
                this.isSaveSelect = false;
                this.getPropertiesOfSelected(dataItem.id, this.entitiesCombobox.dataItem().name, this.tabNameDropDownList.dataItem().tab_name);
            }
        }
        /**
         * 
         * @param {any} curValue show or hide the sent value
         */
        hideShowElementsBasedOnValue(curValue) {
            // hide all elements which are shown based on a type of input
            $('.item[data-visible], label[data-visible]').hide();
            // show all elements which are hidden based on a type of input
            $('.item[data-invisible], label[data-invisible]').show();

            // show all related inputs, check if input is 'input' jquery selector works globally which means 'numeric-input' items would be shown too
            curValue === "input" ? $(`.item[data-visible^="${curValue}"], label[data-visible^="${curValue}"]`).show() : $(`.item[data-visible*="${curValue}"], label[data-visible*="${curValue}"]`).show();
            // hide all related inputs,
            curValue === "input" ? $(`.item[data-invisible^="${curValue}"], label[data-invisible^="${curValue}"]`).show() : $(`.item[data-invisible*="${curValue}"], label[data-invisible*="${curValue}"]`).hide();
        }

        setPropertiesToDefault() {
            // Set default values for all properties that are filled based off input type
            // we dont need to fill the variables that are shown always, the db value should be sufficient

            // numeric default
            this.defaultNumeric.value(0);
            this.numberOfDec.value(2);
            this.numberFormat.select("");
            $("#differentFormatHolder").hide();
            document.getElementById("differentFormat").value = "";
            document.getElementById("roundNumeric").checked = true;
            this.maxNumber.value("");
            this.minNumber.value("");
            this.stepNumber.value(1);
            this.factorNumber.value(1);
            document.getElementById("cultureNumber").value = "";

            // htmleditor default
            document.querySelector("[id*=mode]:checked") ? document.querySelector("[id*=mode]:checked").checked = false : $.noop();

            // datetime default
            $("#dateTimeDropDown").data("kendoDropDownList").select("");
            document.getElementById("dateTimePickerSetNow").checked = false;
            this.minTimeBox.value("");

            // multi select / combobox default
            this.grid.setDataSource(null);
            this.dataSourceEntities.select("");
            this.dataSourceFilter.select(0);
            this.searchFields.value([]);
            document.getElementById("useDropDownList").checked = false;
            document.getElementById("searchInTitle").checked = false;
            document.getElementById("searchEverywhere").checked = false;
            $("[data-show-for-panel=panel2]").hide();


            // secure input default
            $("#typeSecureInput").data("kendoDropDownList").select(0);
            $("#securityMethod").data("kendoDropDownList").select(0);
            document.getElementById("securityKey").value = "";

            // linked item default
            this.linkedItemEntity.select("");
            this.linkType.value("");
            document.getElementById("textOnly").checked = false;
            document.getElementById("linkedItemTemplate").value = "";

            // data selector default
            document.getElementById("dataSelectorText").value = "";

            //item linker default
            $("#itemLinkerModuleId").data("kendoNumericTextBox").value("");
            this.itemLinkerEntity.value("");
            this.itemLinkerTypeNumber.text("");
            $("#itemLinkerDeletionOfItems").data("kendoDropDownList").select(0);
            document.getElementById("itemLinkerOrderBy").value = "";
            document.querySelectorAll("#item-linker-checkboxes input[type=checkbox]:checked")
                .forEach((element) => {
                    element.checked = false;
                });

            // dependencies
            this.dependencyFields.select("");
            this.dependingFilter.select("");

            // action button
            this.actionButtonGrid.setDataSource([]);

            // sub entity grid entity selector
            this.subEntityGridEntity.select("");
            this.dataSelectorIdSubEntitiesGrid.value("");
            this.subEntitiesGridSelectOptions.select("");
            document.getElementById("customQuery").checked = false;
            document.getElementById("hasCustomDeleteQuery").checked = false;
            document.getElementById("hasCustomUpdateQuery").checked = false;
            document.getElementById("hasCustomInsertQuery").checked = false;
            document.getElementById("hideCommandColumn").checked = false;
            document.getElementById("disableInlineEditing").checked = false;
            document.getElementById("disableOpeningOfItems").checked = false;
            document.getElementById("hideExportButton").checked = false;
            document.getElementById("hideCheckAllButton").checked = false;
            document.getElementById("hideUncheckAllButton").checked = false;
            document.getElementById("hideCreateButton").checked = false;
            document.getElementById("refreshGridAfterInlineEdit").checked = false;
            document.getElementById("showDeleteConformations").checked = false;
            document.getElementById("checkboxes").checked = false;
            document.getElementById("showChangedByColumn").checked = false;
            document.getElementById("showChangedOnColumn").checked = false;
            document.getElementById("showAddedByColumn").checked = false;
            document.getElementById("showAddedOnColumn").checked = false;
            document.getElementById("hideLinkButton").checked = false;
            document.getElementById("hideClearFiltersButton").checked = false;
            document.getElementById("hideTitleFieldInWindow").checked = false;
            document.getElementById("hideCount").checked = false;
            document.getElementById("hideTitleColumn").checked = false;
            document.getElementById("hideEnvironmentColumn").checked = false;
            document.getElementById("hideTypeColumn").checked = false;
            document.getElementById("hideLinkIdColumn").checked = false;
            document.getElementById("hideIdColumn").checked = false;


            // timeline
            this.timelineEntity.select("");
            $("#timelineEventHeight").data("kendoNumericTextBox").value("");
            document.getElementById("disableOpeningOfItemsTimeLine").checked = false;
            // timeline / querybuilder shared
            $("#queryId").data("kendoNumericTextBox").value("");

            // daterange
            $("#daterangeFrom").data("kendoDatePicker").value("");
            $("#daterangeTill").data("kendoDatePicker").value("");

            // codemirror fields
            this.cssField.setValue("");
            this.scriptField.setValue("");
            this.jsonField.setValue("");
            this.queryField.setValue("");
            this.queryFieldSubEntities.setValue("");
            this.queryDeleteField.setValue("");
            this.queryInsertField.setValue("");
            this.queryUpdateField.setValue("");
            this.queryContentField.setValue("");

            //textbox
            this.textboxTypeDropDown.select(0);

            // set options field to default, empty object
            this.fieldOptions = {};
        }
        // set all properties values to the fields accordingly
        setProperties(resultSet) {

            // set dropdown value for inputtype field
            this.inputTypeSelector.select((dataItem) => {
                return dataItem.text === resultSet.inputtype;
            });

            // hide/show all elements which are shown based on a type of input
            this.hideShowElementsBasedOnValue(resultSet.inputtype);
            // checkboxes proper set
            document.getElementById("visible-in-table").checked = resultSet.visible_in_overview;
            document.getElementById("mandatory").checked = resultSet.mandatory;
            document.getElementById("readonly").checked = resultSet.readonly;
            document.getElementById("seofriendly").checked = resultSet.also_save_seo_value;

            // numeric textboxes
            this.widthInTable.value(resultSet.overview_width);
            this.width.value(resultSet.width);
            this.height.value(resultSet.height);

            // textboxes / textareas
            document.getElementById("displayname").value = resultSet.display_name;
            document.getElementById("propertyname").value = resultSet.property_name;
            document.getElementById("regexValidation").value = resultSet.regex_validation;
            document.getElementById("langCode").value = resultSet.language_code;
            document.getElementById("explanation").value = resultSet.explanation;
            document.getElementById("defaultValue").value = resultSet.default_value;

            // dependencies
            document.getElementById("dependingValue").value = resultSet.depends_on_value;

            //set depending field using one time dataBound because of the racing condition when filling.
            this.dependencyFields.one("dataBound",
                (e) => {
                    this.dependencyFields.select((dataItem) => {
                        return dataItem.property_name === resultSet.depends_on_field;
                        //return dataItem.property_name === 'test_inputx';
                    });
                });
            // set depending filter
            this.dependingFilter.select((dataItem) => {
                return dataItem.value === resultSet.depends_on_operator;
            });

            // set codemirror fields
            if (resultSet.css && resultSet.css !== "") {
                this.cssField.setValue(resultSet.css);
                this.cssField.refresh();
            }
            if (resultSet.custom_script && resultSet.custom_script !== "") {
                this.scriptField.setValue(resultSet.custom_script);
                this.scriptField.refresh();
            }

            // set dropdown value for tabname field
            this.tabNameProperty.select((dataItem) => {
                return dataItem.tab_name === resultSet.tab_name;
            });
            //set groupNameComboBox field using one time dataBound because of the racing condition when filling.
            this.groupNameComboBox.one("dataBound",
                (e) => {
                    // set dropdown value for groupname field
                    this.groupNameComboBox.select((dataItem) => {
                        return dataItem.group_name === resultSet.group_name;
                    });
                });

            // get options from resultset and parse them as json, if options are empty check
            const options = JSON.parse(resultSet.options === "" || !resultSet.options ? "{}" : resultSet.options);

            function addIdsToArrayObjectItems(targetObject = {}) {
                let autoIncrement = 0;

                const addIds = (target) => {
                    if (target === null) return;
                    target.autoIndex = autoIncrement;
                    autoIncrement++;

                    for (let prop in target) {
                        if (!target.hasOwnProperty(prop)) return;

                        const key = prop;
                        const value = target[key];

                        if (jjl.utils.isArray(value)) {
                            // Loop through array items
                            for (let arrItem of value) {
                                if (typeof arrItem === "object" && arrItem !== null) {
                                    arrItem.autoIndex = autoIncrement;
                                    autoIncrement++;
                                }
                                if (typeof value === "object") addIds(value);
                            }
                        } else if (typeof value === "object") {
                            addIds(value);
                        }
                    }
                };
                addIds(targetObject);
            }

            addIdsToArrayObjectItems(options);
            this.fieldOptions = options;

            const inputTypes = this.base.inputTypes;
            switch (resultSet.inputtype) {
                case inputTypes.TEXTBOX:
                    this.textboxTypeDropDown.select((dataItem) => {
                        return dataItem.id === options.type;
                    });
                    break;
                case inputTypes.AUTOINCREMENT:
                    this.defaultNumeric.value(resultSet.default_value);
                    break;
                case inputTypes.NUMERIC:
                    this.defaultNumeric.value(resultSet.default_value);
                    document.getElementById("roundNumeric").checked = options.round;
                    document.getElementById("cultureNumber").value = options.culture || "";

                    this.maxNumber.value(options.max);
                    this.minNumber.value(options.min);
                    this.stepNumber.value(options.step);
                    this.factorNumber.value(options.factor);

                    // set decimals from options
                    this.numberOfDec.value(options.decimals);
                    // set format dropdown 
                    let found = false;
                    this.numberFormat.select((dataItem) => {
                        if (dataItem.value === options.format) {
                            return found = true;
                        }
                    });
                    if (!found && options.format !== "") {
                        this.numberFormat.select((dataItem) => { return dataItem.value === "anders"; });
                        $("#differentFormatHolder").show();
                        document.getElementById("differentFormat").value = options.format;
                    }
                    break;
                case inputTypes.HTMLEDITOR:
                    // check if mode is null or undefined
                    if (options.mode != null) {
                        document.getElementById(`mode${options.mode}`).checked = true;
                    }
                    break;
                case inputTypes.DATETIMEPICKER:
                    // set dropdown to mode
                    $("#dateTimeDropDown").data("kendoDropDownList").select((dataItem) => {
                        return dataItem.value === options.type;
                    });
                    // check if value isnt null, undefined or empty string and value should be set to NOW()
                    if (options.value !== undefined && options.value !== "" && options.value === "NOW()") {
                        document.getElementById("dateTimePickerSetNow").checked = true;
                    }
                    // check if min is not null or empty string, set minTimeBox to the value
                    if (options.min !== undefined && options.min !== "") {
                        this.minTimeBox.value(options.min);
                    }
                    break;
                case inputTypes.COMBOBOX:
                case inputTypes.MULTISELECT:
                    if (resultSet.inputtype === inputTypes.COMBOBOX) {
                        document.getElementById("useDropDownList").checked = options.useDropDownList;
                    }
                    var panel = "";
                    // if data_query is set, set the codemirror field to the field's value
                    if (resultSet.data_query !== "") {
                        panel = this.base.dataSourceType.PANEL3.id;
                        this.queryField.setValue(resultSet.data_query);
                        this.queryField.refresh();
                    } // if entity type is set, set datasource dropdown to entities and select right option
                    else if (options.entityType !== undefined) {
                        panel = this.base.dataSourceType.PANEL2.id;
                        this.dataSourceEntities.select((dataItem) => {
                            return dataItem.id === options.entityType;
                        });
                        document.getElementById("searchInTitle").checked = options.searchInTitle;
                        document.getElementById("searchEverywhere").checked = options.searchEverywhere;

                        $.each(options.searchFields, (i, v) => {
                            const newItem = {
                                name: v
                            };
                            const widget = this.searchFields;
                            widget.dataSource.add(newItem);
                            widget.value(widget.value().concat([newItem.name]));
                        });

                    } // if dataSource is set, set the grid datasource to the options dataSource
                    else if (options.dataSource !== undefined) {
                        panel = this.base.dataSourceType.PANEL1.id;
                        this.grid.setDataSource(options.dataSource);
                    }
                    // set dropdown to right panel
                    this.dataSourceFilter.select((dataItem) => {
                        return dataItem.id === panel;
                    });
                    break;
                case inputTypes.SECUREINPUT:
                    // set type of secure input
                    $("#typeSecureInput").data("kendoDropDownList").select((dataItem) => {
                        return dataItem.value === options.type;
                    });

                    // set securty method
                    $("#securityMethod").data("kendoDropDownList").select((dataItem) => {
                        return dataItem.value === options.securityMethod;
                    });
                    if (options.securityMethod === "JCL_AES" || options.securityMethod === "AES") {
                        // set securitykey, but only when security method is JCL_AES or AES
                        document.getElementById("securityKey").value = options.securityKey;
                    }
                    break;
                case inputTypes.LINKEDITEM:
                    // set link type on databound of field
                    this.linkType.one("dataBound", () => {
                        this.linkType.select((dataItem) => {
                            return dataItem.type_value === options.linkType;
                        });
                        // if no linkType has been found in the selection, link typ hasnt been made yet and we set it manually
                        if (this.linkType.value() === "") {
                            // toString because kendo does a .toLowerCase in the background and linkType is an integer
                            this.linkType.text(options.linkType.toString());
                        }
                    });

                    // set linked item entity to option defined type
                    this.linkedItemEntity.select((dataItem) => {
                        return dataItem.id === options.entityType;
                    });
                    // set the template
                    document.getElementById("linkedItemTemplate").value = options.template;
                    document.getElementById("textOnly").checked = options.textOnly;
                    break;
                case inputTypes.DATASELECTOR:
                    document.getElementById("dataSelectorText").value = options.text;
                    break;
                case inputTypes.ITEMLINKER:
                case inputTypes.SUBENTITIESGRID:
                case inputTypes.ACTIONBUTTON:

                    // module id is only available for item linker 
                    // entity is a multiselect for item linker
                    if (resultSet.inputtype === inputTypes.ITEMLINKER) {
                        $("#itemLinkerModuleId").data("kendoNumericTextBox").value(options.moduleId);
                        // select multiselect options
                        this.itemLinkerEntity.value(options.entityTypes);
                        document.getElementById("itemLinkerOrderBy").value = options.orderBy;
                    }

                    // shared properties through out sub entities grid and item linker
                    if (resultSet.inputtype !== inputTypes.ACTIONBUTTON) {
                        // select item linker type number
                        this.itemLinkerTypeNumber.select((dataItem) => {
                            return dataItem.type_value === options.linkTypeNumber;
                        });

                        // if no linkTypeNumber has been found in the selection, link typ name hasnt been made yet and we set it manually
                        if (this.itemLinkerTypeNumber.value() === "") {
                            this.itemLinkerTypeNumber.text(options.linkTypeNumber);
                        }

                        $("#itemLinkerDeletionOfItems").data("kendoDropDownList").select((dataItem) => {
                            return dataItem.value === options.deletionOfItems;
                        });
                        // set checkboxes
                        document.getElementById("hideCommandColumn").checked = options.hideCommandColumn;
                        document.getElementById("disableInlineEditing").checked = options.disableInlineEditing;
                        document.getElementById("disableOpeningOfItems").checked = options.disableOpeningOfItems;
                        document.getElementById("hideExportButton").checked = options.toolbar.hideExportButton;
                        document.getElementById("hideCheckAllButton").checked = options.toolbar.hideCheckAllButton;
                        document.getElementById("hideUncheckAllButton").checked = options.toolbar.hideUncheckAllButton;
                        document.getElementById("hideCreateButton").checked = options.toolbar.hideCreateButton;
                    }

                    // actions which are available for action button and sub entities grid.
                    if (resultSet.inputtype !== inputTypes.ITEMLINKER) {
                        const buttonArray = [];
                        if (resultSet.inputtype === inputTypes.ACTIONBUTTON) {
                            // set button array options
                            buttonArray.push({
                                text: options.text,
                                icon: options.icon,
                                autoIndex: options.autoIndex,
                                button: {
                                    actions: options.actions
                                }
                            });
                        } else {
                            // actions come from custom actions property within toolbar for sub entity grid
                            const customActions = options.toolbar.customActions;
                            for (let i = 0; i < customActions.length; i++) {
                                buttonArray.push(
                                    {
                                        text: customActions[i].text,
                                        icon: customActions[i].icon,
                                        autoIndex: customActions[i].autoIndex,
                                        button: {
                                            actions: customActions[i].actions
                                        }
                                    });
                            }
                        }
                        this.actionButtonGrid.setDataSource(buttonArray);
                    }

                    // only available to sub entities grid
                    if (resultSet.inputtype === inputTypes.SUBENTITIESGRID) {
                        // set entity dropdown 
                        this.subEntityGridEntity.select((dataItem) => {
                            return dataItem.id === options.entityType;
                        });

                        this.subEntitiesGridSelectOptions.select((dataItem) => {
                            //Cast options.selectable to string because it could be a boolean
                            const selectableString = String(options.selectable);
                            return dataItem.value === selectableString;
                        });

                        // set data selector id
                        this.dataSelectorIdSubEntitiesGrid.value(options.dataSelectorId);
                        // set checkboxes
                        document.getElementById("refreshGridAfterInlineEdit").checked = options.refreshGridAfterInlineEdit;
                        document.getElementById("showChangedByColumn").checked = options.showChangedByColumn;
                        document.getElementById("showChangedOnColumn").checked = options.showChangedOnColumn;
                        document.getElementById("showAddedByColumn").checked = options.showAddedByColumn;
                        document.getElementById("showAddedOnColumn").checked = options.showAddedOnColumn;
                        document.getElementById("showDeleteConformations").checked = options.showDeleteConformations;
                        document.getElementById("checkboxes").checked = options.checkboxes;

                        if (options.customQuery && resultSet.data_query && resultSet.data_query !== "") {
                            $("#customQuery").trigger("click");
                            this.queryFieldSubEntities.setValue(resultSet.data_query);
                            this.queryFieldSubEntities.refresh();
                        }

                        if (options.hasCustomDeleteQuery && resultSet.grid_delete_query && resultSet.grid_delete_query !== "") {
                            $("#hasCustomDeleteQuery").trigger("click");
                            this.queryDeleteField.setValue(resultSet.grid_delete_query);
                            this.queryDeleteField.refresh();
                        }

                        if (options.hasCustomUpdateQuery && resultSet.grid_insert_query && resultSet.grid_update_query !== "") {
                            $("#hasCustomUpdateQuery").trigger("click");
                            this.queryUpdateField.setValue(resultSet.grid_update_query);
                            this.queryUpdateField.refresh();
                        }


                        if (options.hasCustomInsertQuery && resultSet.grid_insert_query && resultSet.grid_insert_query !== "") {
                            $("#hasCustomInsertQuery").trigger("click");
                            this.queryInsertField.setValue(resultSet.grid_insert_query);
                            this.queryInsertField.refresh();
                        }

                        document.getElementById("disableInlineEditing").checked = options.disableInlineEditing;
                        document.getElementById("disableOpeningOfItems").checked = options.disableOpeningOfItems;
                        document.getElementById("hideTitleColumn").checked = options.hideTitleColumn;
                        document.getElementById("hideEnvironmentColumn").checked = options.hideEnvironmentColumn;
                        document.getElementById("hideTypeColumn").checked = options.hideTypeColumn;
                        document.getElementById("hideLinkIdColumn").checked = options.hideLinkIdColumn;
                        document.getElementById("hideIdColumn").checked = options.hideIdColumn;
                        document.getElementById("hideTitleFieldInWindow").checked = options.hideTitleFieldInWindow;
                        document.getElementById("hideLinkButton").checked = options.toolbar.hideLinkButton;
                        document.getElementById("hideCount").checked = options.toolbar.hideCount;
                        document.getElementById("hideClearFiltersButton").checked = options.toolbar.hideClearFiltersButton;
                    }
                    break;
                case inputTypes.TIMELINE:
                    this.timelineEntity.select((dataItem) => {
                        return dataItem.id === options.entityType;
                    });
                    $("#queryId").data("kendoNumericTextBox").value(options.queryId);
                    $("#timelineEventHeight").data("kendoNumericTextBox").value(options.eventHeight);
                    document.getElementById("disableOpeningOfItemsTimeLine").checked = options.disableOpeningOfItems;
                    break;
                case inputTypes.FILEUPLOAD:
                    document.getElementById("allowMultipleFiles").checked = options.multiple;
                    if (options.validation && options.validation.allowedExtensions)
                        document.getElementById("allowedExtensions").value = options.validation.allowedExtensions;
                    else
                        document.getElementById("allowedExtensions").value = "";
                    break;
                case inputTypes.DATERANGE:
                    // TODO parse given date like so? kendo.parseDate("10-11-2019", "dd-MM-yyyy")
                    $("#daterangeFrom").data("kendoDatePicker").value(options.from);
                    $("#daterangeTill").data("kendoDatePicker").value(options.till);
                    break;
                case inputTypes.QUERYBUILDER:
                case inputTypes.SCHEDULER:
                    $("#queryId").data("kendoNumericTextBox").value(options.queryId);
                    break;
                case inputTypes.CHART:
                    this.jsonField.setValue(JSON.stringify(options, null, 2));
                    this.jsonField.refresh();
                    break;
                case inputTypes.QR:
                    document.getElementById("pixelSize").value = options.size ;
                    if (resultSet.data_query !== "") {
                        this.queryContentField.setValue(resultSet.data_query);
                        this.queryContentField.refresh();
                    }
                    break;
                case inputTypes.RADIOBUTTON:
                    if (resultSet.data_query !== "") {
                        this.queryContentField.setValue(resultSet.data_query);
                        this.queryContentField.refresh();
                    }
                    break;
            }
        }

        // return array of of different input types from inputtypes enum
        createDataSourceFromEnum(list, useObjects = false) {
            const returnVal = [];
            if (useObjects) {
                const newList = {};
                $.each(list, (i, v) => {
                    newList[v.id] = v.text;
                });
                list = newList;
            }
            $.each(list, (i, v) => { returnVal.push({ text: v, id: i }); });
            return returnVal;
        }
    }

    // model for entity properties
    class EntityPropertyModel {
        constructor(id, entity_name, tab_name, visible_in_overview, overview_fieldtype, overview_width, group_name, inputtype, display_name, property_name,
            explanation, regex_validation, mandatory, readonly, default_value, automation, css, width, height, depends_on_field, depends_on_operator, depends_on_value,
            language_code, custom_script, also_save_seo_value, data_query, options = {}, grid_delete_query = null, grid_insert_query = null, grid_update_query = null) {
            this.id = id;
            this.entity_name = entity_name;
            this.tab_name = tab_name;
            this.display_name = display_name;
            this.property_name = property_name;
            this.visible_in_overview = visible_in_overview;
            this.overview_fieldtype = overview_fieldtype;
            this.overview_width = overview_width;
            this.group_name = group_name;
            this.inputtype = inputtype;
            this.explanation = explanation;
            this.regex_validation = regex_validation;
            this.mandatory = mandatory;
            this.readonly = readonly;
            this.default_value = default_value;
            this.automation = automation;
            this.css = css;
            this.width = width;
            this.height = height;
            this.depends_on_field = depends_on_field;
            this.depends_on_operator = depends_on_operator;
            this.depends_on_value = depends_on_value;
            this.language_code = language_code;
            this.custom_script = custom_script;
            this.also_save_seo_value = also_save_seo_value;
            this.options = options;
            this.data_query = data_query;
            this.grid_delete_query = grid_delete_query;
            this.grid_insert_query = grid_insert_query;
            this.grid_update_query = grid_update_query;
        }

        createOptionsJson(clean = true) {
            // const options = new EntityPropertyOptions(this.baseClass.base, this.inputtype, this);
            // overwrite options with json
            this.options = this.createJson(clean);
        }
        // create json for options
        createJson(clean = true) {
            const options = this.options;
            //  return null if no options have been added
            if (!Object.keys(options).length) {
                return null;
            }
            // remove null properties if set to true
            if (clean) {
                for (let propName in options) {
                    if (options.hasOwnProperty(propName)) {
                        if (options[propName] === null || options[propName] === undefined) {
                            delete options[propName];
                        }
                    }
                }
            }
            // return options
            return JSON.stringify(options);
        }
    }

    class ModuleTab {
        constructor(base) {
            this.base = base;

            this.setupBindings();
            this.getModules();
        }

        /**
        * Setup all basis bindings for this module.
        * Specific bindings (for buttons in certain pop-ups for example) will be set when they are needed.
        */
        setupBindings() {
            $("#addModuleButton").kendoButton({
                click: () => {
                    this.base.openDialog("Module toevoegen", "Voer het nummer van de module in").then((data) => {
                        this.createNewModule(data);
                    });

                },
                icon: "plus"
            });
        }

        hideShowComponents(itemClassname, targetElement) {
            let topElement;

            topElement = targetElement.sender.input.closest(".modulebar");

            if (itemClassname === "gridview") {
                const items = topElement[0].querySelectorAll(".gridview");
                $(items).show();
            } else {
                const items = topElement[0].querySelectorAll(".gridview");
                $(items).hide();
            }
        }

        async createNewModule(id) {
            const querystring = {
                moduleId: id,
                isTest: this.settings.isTestEnvironment
            };

            const results = await $.get(`${this.base.settings.serviceRoot}/CHECK_IF_MODULE_EXISTS${jjl.convert.toQueryString(querystring, true)}`);

            if (results.length > 0) {
                kendo.alert(`De ingevoerde module is al toegevoegd`);
                return;
            }

            const qs = {
                moduleId: id,
                isTest: this.base.settings.isTestEnvironment
            };

            let notification;
            qs.add = true;
            notification = "toegevoegd";

            try {
                await $.get(`${this.base.settings.serviceRoot}/INSERT_NEW_MODULE${jjl.convert.toQueryString(qs, true)}`);

                this.base.showNotification("notification", `De nieuwe module is toegevoegd`, "success");
                this.getModules();
            } catch (exception) {
                this.base.showNotification("notification", `De nieuwe module is niet succesvol ${notification}, probeer het opnieuw`, "error");
            }
        }

        /** Initializes all kendo components for the base class. */
        async initializeKendoComponents() {
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
                    console.log("mainTabStrip select", tabName);

                    if (tabName === "rollen" || tabName === "modules" || tabName === "entiteiten") {
                        $("footer").hide();
                    } else {
                        $("footer").show();
                    }
                },
                activate: (event) => {
                    const tabName = event.item.querySelector(".k-link").innerHTML.toLowerCase();
                    console.log("mainTabStrip activate", tabName);
                }
            }).data("kendoTabStrip");

            this.modeSelect = $(".combo-select").kendoComboBox({
                select: (element) => {
                    var currentValue = element.dataItem.value;

                    this.hideShowComponents(currentValue, element);
                }
            }).data("kendoComboBox");

            $(".recordsPerPage").kendoNumericTextBox({
                decimals: 0,
                format: "#",
                min: 0,
                step: 1
            }).data("kendoNumericTextBox");
            var me = this;
            this.panelbar = $(".panelbar").kendoPanelBar({
                expandMode: "single",
                activate: async (element) => {
                    const targetElement = element.item;
                    const moduleId = targetElement.dataset.moduleId;
                    const moduleType = targetElement.dataset.moduleType;

                    let fieldsJson = {};

                    if (moduleType === "gridview") {
                        const result = await $.get(`${this.base.settings.serviceRoot}/GET_MODULE_FIELDS?module_id=${encodeURIComponent(moduleId)}&isTest=${encodeURIComponent(this.base.settings.isTestEnvironment)}`);

                        fieldsJson = JSON.parse(result[0].fields);
                    }

                    this.fieldsGrid = $(targetElement.querySelector(".fieldsGrid")).kendoGrid({
                        columns: [{
                            field: "field",
                            title: "Veld"
                        }, {
                            field: "title",
                            title: "Titel"
                        }, {
                            field: "width",
                            title: "Breedte"
                        }, {
                            field: "filterable",
                            title: "Filterable"
                        }],
                        dataSource: fieldsJson,
                        toolbar: [{ name: "create", text: "Veld toevoegen" }],
                        resizable: false,
                        editable: {
                            createAt: "bottom"
                        }
                    });

                    $(targetElement).find(".CodeMirror").remove();
                    this.codeMirrorCustomQuery = CodeMirror.fromTextArea(targetElement.querySelector("textarea.customQueryBuilder"), {
                        mode: "text/x-mysql",
                        lineNumbers: true
                    });
                    this.codeMirrorCountUQuery = CodeMirror.fromTextArea(targetElement.querySelector("textarea.countQueryBuilder"), {
                        mode: "text/x-mysql",
                        lineNumbers: true
                    });
                },
                expand: function (e) {
                    const targetElement = e.item;
                    if (targetElement.dataset.isValidJson === "0" && targetElement.dataset.moduleType === "gridview") {
                        e.preventDefault();
                        me.panelbar.enable(targetElement, false);
                        me.base.showNotification("notification", `Het lijkt er op dat de module niet correct is ingericht, neem contact op met ons.`, "error");
                        return;
                    }
                }
            }).data("kendoPanelBar");

            $(".modulebar").each((index, element) => {
                const moduleType = element.dataset.moduleType;
                const items = element.querySelectorAll(".gridview");

                $(items).toggle(moduleType === "gridview")
            });

            $(".saveModuleSettings").kendoButton({
                click: (element) => {
                    const moduleElement = element.sender.element.closest(".modulebar")[0];
                    const moduleId = moduleElement.dataset.moduleId;

                    this.saveModuleSettings(moduleId, moduleElement);
                },
                icon: "save"
            });

            $(".deleteModule").kendoButton({
                click: (element) => {
                    const moduleElement = element.sender.element.closest(".modulebar")[0];
                    const moduleId = moduleElement.dataset.moduleId;

                    this.base.openDialog("Module verwijderen", "Weet u zeker dat u de module wilt verwijderen?", this.base.kendoPromptType.CONFIRM).then(() => {
                        this.deleteModule(moduleId);
                    });
                },
                icon: "delete"
            });
        }

        async deleteModule(moduleId) {
            const results = await $.get(`${this.base.settings.serviceRoot}/DELETE_MODULE?module_id=${encodeURIComponent(moduleId)}&isTest=${encodeURIComponent(this.base.settings.isTestEnvironment)}`);

            this.getModules();
        }

        async saveModuleSettings(module, moduleElement) {
            const customQuery = this.codeMirrorCustomQuery.getValue();
            const countQuery = this.codeMirrorCountUQuery.getValue();
            const moduleType = moduleElement.querySelector("input.combo-select").value.toLowerCase();

            let dataToSend = "";
            if (moduleType === "gridview") {
                const pageSize = moduleElement.querySelector("input.recordsPerPage").value;
                const hideCommandColumn = $(moduleElement.querySelector("input.clickOption")).is(":checked");
                const hideCreateButton = $(moduleElement.querySelector("input.addNewItemOption")).is(":checked");
                const kendoGridColumns = $(this.fieldsGrid[0]).data("kendoGrid").dataSource.view();

                dataToSend = {
                    trace: false,
                    options: JSON.stringify({
                        gridViewMode: true,
                        gridViewSettings: {
                            pageSize: parseInt(pageSize),
                            hideCommandColumn: hideCommandColumn,
                            toolbar: {
                                hideCreateButton: hideCreateButton
                            },
                            columns: kendoGridColumns
                        }
                    }),
                    custom_query: customQuery,
                    count_query: countQuery,
                    module_id: module,
                    module_type: moduleType
                };
            } else {
                dataToSend = {
                    options: "",
                    custom_query: customQuery,
                    count_query: countQuery,
                    module_id: module,
                    module_type: moduleType
                };
            }
            const result = await $.ajax({
                url: `${this.base.settings.serviceRoot}/SAVE_MODULE_SETTINGS?isTest=${encodeURIComponent(this.base.settings.isTestEnvironment)}`,
                method: "POST",
                data: dataToSend
            });

            if (result.success) {
                this.base.showNotification("notification", `De module instellingen zijn successvol opgeslagen`, "success");
            } else {
                this.base.showNotification("notification", `De instellingen kunnen niet worden opgeslagen, probeer het nogmaals`, "error");
            }
        }

        /** Get the modules */
        async getModules() {
            const results = $.get(`${this.base.settings.serviceRoot}/GET_ALL_MODULES_INFORMATION?isTest=${encodeURIComponent(this.base.settings.isTestEnvironment)}`);

            const templateContent = $("#myTemplate").html();
            const template = kendo.template(templateContent);
            const templateResult = kendo.render(template, results);

            $("#moduleList").html(templateResult);

            this.initializeKendoComponents();

        }
    }

    class RoleTab {
        constructor(base) {
            this.base = base;

            this.setupBindings();
            this.initializeKendoComponents();
        }

        /**
        * Setup all basis bindings for this module.
        * Specific bindings (for buttons in certain pop-ups for example) will be set when they are needed.
        */
        setupBindings() {
            // Add role button
            $(".addRoleBtn").kendoButton({
                click: () => {
                    this.base.openDialog("Rol toevoegen", "Voer de naam in van de nieuwe rol").then((data) => {
                        this.addRemoveRoles(data);
                    });
                },
                icon: "file"
            });

            // Delete role button
            $(".delRoleBtn").kendoButton({
                click: () => {
                    const roleList = this.roleList;
                    const index = roleList.select().index();
                    const dataItem = roleList.dataSource.view()[index];

                    this.base.openDialog("Item verwijderen", "Weet u zeker dat u dit item wil verwijderen?", this.base.kendoPromptType.CONFIRM).then(() => {
                        this.addRemoveRoles("", dataItem.id);
                    });
                },
                icon: "delete"
            });
        }

        /**
         * Add or remove rights from the database based on the given parameters
         * @param {any} role The id of the role
         * @param {any} entity The name of the entity property
         * @param {any} permissionCode The code of the permission to add or delete
         */
        updateEntityPropertyPermissions(role, entity, permissionCode) {
            const qs = {
                entity_id: entity,
                role_id: role,
                permission_code: permissionCode,
                isTest: this.base.settings.isTestEnvironment
            };

            return $.get(`${this.base.settings.serviceRoot}/UPDATE_ENTITY_PROPERTY_PERMISSIONS${jjl.convert.toQueryString(qs, true)}`)
                .done(() => {
                    this.base.showNotification("notification", `De wijzigingen zijn opgeslagen`, "success");
                }).fail(() => {
                    this.base.showNotification("notification", `Er is iets fout gegaan, probeer het opnieuw`, "error");
                });
        }

        /**
         * Add or remove module rights from the database based on the given parameters
         * @param {any} role The id of the role
         * @param {any} module The id of the module
         * @param {any} permissionCode The code of the permission to add or delete
         */
        addRemoveModuleRightAssignment(role, module, permissionCode) {
            const qs = {
                role_id: role,
                module_id: module,
                permission_code: permissionCode,
                isTest: this.base.settings.isTestEnvironment
            };

            return $.get(`${this.base.settings.serviceRoot}/UPDATE_MODULE_PERMISSION${jjl.convert.toQueryString(qs, true)}`)
                .done(() => {
                    this.base.showNotification("notification", `De wijzigingen zijn opgeslagen.`, "success");
                }).fail(() => {
                    this.base.showNotification("notification", `Er is iets fout gegaan, probeer het opnieuw`, "error");
                });
        }

        /**
         * Add or remove roles from the database based on the given parameters 
         * @param {string} name The specified name of the role that must be added
         * @param {any} id The id of the role that must be deleted
         */
        async addRemoveRoles(name = "", id = 0) {
            if (name === "" && id === 0) {
                return;
            }

            const qs = {
                entityName: this.entitySelected,
                isTest: this.base.settings.isTestEnvironment
            };

            let template;
            let notification;
            if (id !== 0) {
                qs.remove = true;
                template = "DELETE_ROLE";
                qs.roleId = id;
                notification = "verwijderd";
            } else {
                qs.add = true;
                template = "INSERT_ROLE";
                qs.displayName = name;
                notification = "toegevoegd";
            }

            try {
                await $.get(`${this.base.settings.serviceRoot}/${template}${jjl.convert.toQueryString(qs, true)}`);

                this.base.showNotification("notification", `Item succesvol ${notification}`, "success");
                this.roleList.dataSource.read();
            } catch (exception) {
                this.base.showNotification("notification", `Item is niet succesvol ${notification}, probeer het opnieuw`, "error");
            }
        }

        /**
         * Init Kendo grid component
         * @param {any} item The item id of the selected role
         */
        initializeOrRefreshRolesEntityPropertiesGrid(item) {
            if (!this.entityPropertiesGrid) {
                this.entityPropertiesGrid = $("#EntityPropertiesGrid").kendoGrid({
                    resizable: true,
                    filterable: {
                        mode: "row"
                    },
                    columns: [
                        {
                            field: "entity_name",
                            title: "Entiteit"
                        },
                        {
                            field: "display_name",
                            title: "Veld"
                        },
                        {
                            title: "Alle rechten",
                            width: "100px",
                            attributes: {
                                style: "text-align: center;"
                            },
                            headerTemplate: () => {
                                return `<div class="checkAll"><span>Alle rechten</span><input type="checkbox" id="role-check-all" class="k-checkbox role"><label class="k-checkbox-label" for="role-check-all"></label></div>`;
                            },
                            template: (dataItem) => {
                                return `<input type="checkbox" ${dataItem.permission === 15 ? "checked" : ""} id="role-entity-property-all-${dataItem.property_id}" data-type="all" data-role-id="${dataItem.role_id}" data-entity="${dataItem.property_id}" data-permission="15" class="k-checkbox role"><label class="k-checkbox-label" for="role-entity-property-all-${dataItem.property_id}"></label>`;
                            }
                        },
                        {
                            title: "Geen rechten",
                            width: "100px",
                            attributes: {
                                style: "text-align: center;"
                            },
                            headerTemplate: () => {
                                return `<div class="checkAll"><span>Geen rechten</span><input type="checkbox" id="role-check-disable" class="k-checkbox role"><label class="k-checkbox-label" for="role-check-disable"></label></div>`;
                            },
                            template: (dataItem) => {
                                return `<input type="checkbox" id="role-entity-property-disable-${dataItem.property_id}" data-role-id="${dataItem.role_id}" data-type="nothing" data-entity="${dataItem.property_id}" data-permission="0" ${dataItem.permission === 0 ? "checked" : ""} class="k-checkbox role"><label class="k-checkbox-label" for="role-entity-property-disable-${dataItem.property_id}"></label>`;
                            }
                        },
                        {
                            title: "Lezen",
                            width: "100px",
                            attributes: {
                                style: "text-align: center;"
                            },
                            headerTemplate: () => {
                                return `<div class="checkAll"><span>Lezen</span><input type="checkbox" id="role-check-read" class="k-checkbox role"><label class="k-checkbox-label" for="role-check-read"></label></div>`;
                            },
                            template: (dataItem) => {
                                return `<input type="checkbox" id="role-entity-property-read-${dataItem.property_id}" data-role-id="${dataItem.role_id}" data-type="read" data-entity="${dataItem.property_id}" data-permission="1" ${(1 << 0 & dataItem.permission) > 0 ? "checked" : ""} class="k-checkbox role"><label class="k-checkbox-label" for="role-entity-property-read-${dataItem.property_id}"></label>`;
                            }
                        },
                        {
                            title: "Aanmaken",
                            width: "100px",
                            attributes: {
                                style: "text-align: center;"
                            },
                            headerTemplate: () => {
                                return `<div class="checkAll"><span>Aanmaken</span><input type="checkbox" id="role-check-edit" class="k-checkbox"><label class="k-checkbox-label" for="role-check-edit"></label></div>`;
                            },
                            template: (dataItem) => {
                                return `<input type="checkbox" id="role-entity-property-create-${dataItem.property_id}" data-role-id="${dataItem.role_id}" data-type="create" data-entity="${dataItem.property_id}" data-permission="2" ${(1 << 1 & dataItem.permission) > 0 ? "checked" : ""} class="k-checkbox role"><label class="k-checkbox-label" for="role-entity-property-create-${dataItem.property_id}"></label>`;
                            }
                        },
                        {
                            title: "Wijzigen",
                            width: "100px",
                            attributes: {
                                style: "text-align: center;"
                            },
                            headerTemplate: () => {
                                return `<div class="checkAll"><span>Wijzigen</span><input type="checkbox" id="role-check-edit" class="k-checkbox"><label class="k-checkbox-label" for="role-check-edit"></label></div>`;
                            },
                            template: (dataItem) => {
                                return `<input type="checkbox" id="role-entity-property-edit-${dataItem.property_id}" data-role-id="${dataItem.role_id}" data-type="edit" data-entity="${dataItem.property_id}" data-permission="4" ${(1 << 2 & dataItem.permission) > 0 ? "checked" : ""} class="k-checkbox role"><label class="k-checkbox-label" for="role-entity-property-edit-${dataItem.property_id}"></label>`;
                            }
                        },
                        {
                            title: "Verwijderen",
                            width: "100px",
                            attributes: {
                                style: "text-align: center;"
                            },
                            headerTemplate: () => {
                                return `<div class="checkAll"><span>Verwijderen</span><input type="checkbox" id="role-check-edit" class="k-checkbox"><label class="k-checkbox-label" for="role-check-edit"></label></div>`;
                            },
                            template: (dataItem) => {
                                return `<input type="checkbox" id="role-entity-property-delete-${dataItem.property_id}" data-role-id="${dataItem.role_id}" data-type="remove" data-entity="${dataItem.property_id}" data-permission="8" ${(1 << 3 & dataItem.permission) > 0 ? "checked" : ""} class="k-checkbox role"><label class="k-checkbox-label" for="role-entity-property-delete-${dataItem.property_id}"></label>`;
                            }
                        }
                    ],
                    dataBound: (e) => {
                        // When a item in the header is selected
                        e.sender.thead.find(".checkAll > input").off("change").change((element) => {
                            this.base.openDialog("Meerdere rechten wijzigen", "U staat op het punt voor meer dan een regel de rechten te zetten, weet u zeker dat u wilt doorgaan?", this.base.kendoPromptType.CONFIRM).then(() => {
                                const clickedElement = element.currentTarget;
                                this.base.setCheckboxForHeaderItems(clickedElement);
                            });
                        });

                        // When a item in the grid is checked
                        e.sender.tbody.find(".k-checkbox").off("change").change((element) => {
                            const targetElement = element.currentTarget;
                            const tagetType = targetElement.dataset.type;
                            const roleId = parseInt(targetElement.dataset.roleId);
                            const entityId = parseInt(targetElement.dataset.entity);
                            const permissionValue = this.base.setCheckboxForItems(targetElement, tagetType, entityId, "entity-property");

                            this.updateEntityPropertyPermissions(roleId, entityId, permissionValue);
                        });
                    }
                }).data("kendoGrid");
            }

            const queryStringForEntityPropertiesGrid = {
                role_id: item,
                isTest: this.base.settings.isTestEnvironment
            };
            this.entityPropertiesGrid.setDataSource({
                transport: {
                    read: {
                        url: `${this.base.settings.serviceRoot}/GET_ROLE_RIGHTS${jjl.convert.toQueryString(queryStringForEntityPropertiesGrid, true)}`
                    }
                }
            });
        }

        /**
         * Init Kendo grid component
         * @param {any} item The item id of the selected role
         */
        initializeOrRefreshRolesModulesGrid(item) {
            if (!this.modulesGrid) {
                this.modulesGrid = $("#ModulesGrid").kendoGrid({
                    resizable: true,
                    filterable: {
                        mode: "row"
                    },
                    columns: [
                        {
                            title: "Module naam",
                            field: "module_name"
                        },
                        {
                            title: "Alle rechten",
                            width: "100px",
                            attributes: {
                                style: "text-align: center;"
                            },
                            headerTemplate: () => {
                                return `<div class="checkAll"><span>Alle rechten</span><input type="checkbox" id="role-check-all" class="k-checkbox module"><label class="k-checkbox-label" for="role-check-all"></label></div>`;
                            },
                            template: (dataItem) => {
                                return `<input type="checkbox" id="role-module-all-${dataItem.module_id}" data-type="all" data-role-id="${dataItem.role_id}" data-module="${dataItem.module_id}" data-permission="0" ${dataItem.permission === 15 ? "checked" : ""} class="k-checkbox module"><label class="k-checkbox-label" for="role-module-all-${dataItem.module_id}"></label>`;
                            }
                        },
                        {
                            title: "Geen rechten",
                            width: "100px",
                            attributes: {
                                style: "text-align: center;"
                            },
                            headerTemplate: () => {
                                return `<div class="checkAll"><span>Geen rechten</span><input type="checkbox" id="role-check-disable" class="k-checkbox module"><label class="k-checkbox-label" for="role-check-disable"></label></div>`;
                            },
                            template: (dataItem) => {
                                return `<input type="checkbox" id="role-module-disable-${dataItem.module_id}" data-type="nothing" data-role-id="${dataItem.role_id}" data-module="${dataItem.module_id}" data-permission="0" ${dataItem.permission === 0 ? "checked" : ""} class="k-checkbox module"><label class="k-checkbox-label" for="role-module-disable-${dataItem.module_id}"></label>`;
                            }
                        },
                        {
                            title: "Lezen",
                            width: "100px",
                            attributes: {
                                style: "text-align: center;"
                            },
                            headerTemplate: () => {
                                return `<div class="checkAll"><span>Lezen</span><input type="checkbox" id="role-check-read" class="k-checkbox module"><label class="k-checkbox-label" for="role-check-read"></label></div>`;
                            },
                            template: (dataItem) => {
                                return `<input type="checkbox" id="role-module-read-${dataItem.module_id}" data-type="read" data-role-id="${dataItem.role_id}" data-module="${dataItem.module_id}" data-permission="1" ${(1 << 0 & dataItem.permission) > 0 ? "checked" : ""} class="k-checkbox"><label class="k-checkbox-label" for="role-module-read-${dataItem.module_id}"></label>`;
                            }
                        },
                        {
                            title: "Aanmaken",
                            width: "100px",
                            attributes: {
                                style: "text-align: center;"
                            },
                            headerTemplate: () => {
                                return `<div class="checkAll"><span>Aanmaken</span><input type="checkbox" id="role-check-edit" class="k-checkbox module"><label class="k-checkbox-label" for="role-check-edit"></label></div>`;
                            },
                            template: (dataItem) => {
                                return `<input type="checkbox" id="role-module-create-${dataItem.module_id}" data-type="create" data-role-id="${dataItem.role_id}" data-module="${dataItem.module_id}" data-permission="2" ${(1 << 1 & dataItem.permission) > 0 ? "checked" : ""} class="k-checkbox"><label class="k-checkbox-label" for="role-module-create-${dataItem.module_id}"></label>`;
                            }
                        },
                        {
                            title: "Wijzigen",
                            width: "100px",
                            attributes: {
                                style: "text-align: center;"
                            },
                            headerTemplate: () => {
                                return `<div class="checkAll"><span>Wijzigen</span><input type="checkbox" id="role-check-edit" class="k-checkbox module"><label class="k-checkbox-label" for="role-check-edit"></label></div>`;
                            },
                            template: (dataItem) => {
                                return `<input type="checkbox" id="role-module-edit-${dataItem.module_id}" data-type="edit" data-role-id="${dataItem.role_id}" data-module="${dataItem.module_id}" data-permission="4" ${(1 << 2 & dataItem.permission) > 0 ? "checked" : ""} class="k-checkbox"><label class="k-checkbox-label" for="role-module-edit-${dataItem.module_id}"></label>`;
                            }
                        },
                        {
                            title: "Verwijderen",
                            width: "100px",
                            attributes: {
                                style: "text-align: center;"
                            },
                            headerTemplate: () => {
                                return `<div class="checkAll"><span>Verwijderen</span><input type="checkbox" id="role-check-edit" class="k-checkbox module"><label class="k-checkbox-label" for="role-check-edit"></label></div>`;
                            },
                            template: (dataItem) => {
                                return `<input type="checkbox" id="role-module-delete-${dataItem.module_id}" data-type="remove"  data-role-id="${dataItem.role_id}" data-module="${dataItem.module_id}" data-permission="8" ${(1 << 3 & dataItem.permission) > 0 ? "checked" : ""} class="k-checkbox"><label class="k-checkbox-label" for="role-module-delete-${dataItem.module_id}"></label>`;
                            }
                        }
                    ],
                    dataBound: (e) => {
                        // When a item in the header is selected
                        e.sender.thead.find(".checkAll > input").off("change").change((element) => {
                            this.base.openDialog("Meerdere rechten wijzigen", "U staat op het punt voor meer dan een regel de rechten te zetten, weet u zeker dat u wilt doorgaan?", this.base.kendoPromptType.CONFIRM).then(() => {
                                const clickedElement = element.currentTarget;
                                this.base.setCheckboxForHeaderItems(clickedElement);
                            })
                        });

                        // When a item in the grid is checked
                        e.sender.tbody.find(".k-checkbox").off("change").change((element) => {
                            const targetElement = element.currentTarget;
                            const tagetType = targetElement.dataset.type;
                            const roleId = parseInt(targetElement.dataset.roleId);
                            const moduleId = parseInt(targetElement.dataset.module);

                            let permissionValue = this.base.setCheckboxForItems(targetElement, tagetType, moduleId, "module");

                            if (permissionValue !== -1)
                                this.addRemoveModuleRightAssignment(roleId, moduleId, permissionValue);
                        });
                    }
                }).data("kendoGrid");
            }

            const queryStringForModulesGrid = {
                role_id: item,
                isTest: this.base.settings.isTestEnvironment
            };

            this.modulesGrid.setDataSource({
                transport: {
                    read: {
                        url: `${this.base.settings.serviceRoot}/GET_MODULE_PERMISSIONS${jjl.convert.toQueryString(queryStringForModulesGrid, true)}`
                    }
                }
            });
        }

        /**
         * Check if a right checkbox has been checked otherwise the default is `all rights`
         * @param {any} targetElement The checked checkbox element
         */
        checkRights(targetElement) {
            const selectedRightsCount = targetElement.closest("tr").querySelectorAll("input:checked").length;

            targetElement.closest("tr").querySelectorAll("input").forEach((element) => {
                element.checked = false;
            });

            if (selectedRightsCount === 0) {
                targetElement.closest("tr").querySelector("input").checked = true;
            } else {
                targetElement.checked = true;
            }
        }

        getSelectedTabName() {
            return this.rolesTabStrip.select().find(".k-link").text();
        }

        /** Init Kendo listview component */
        initializeKendoComponents() {
            this.rolesTabStrip = $("#RolesTabStrip").kendoTabStrip({
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
                    const selectedTab = event.item.querySelector(".k-link").innerHTML.toLowerCase();
                    console.log("rolesTabStrip select", selectedTab);
                },
                activate: (event) => {
                    const selectedTab = event.item.querySelector(".k-link").innerHTML.toLowerCase();
                    const dataItem = this.roleList.dataItem(this.roleList.select());
                    console.log("rolesTabStrip activate", selectedTab, dataItem);
                    if (typeof dataItem !== "undefined") {
                        if (selectedTab === "velden") {
                            this.initializeOrRefreshRolesEntityPropertiesGrid(dataItem.id);
                        }
                        if (selectedTab === "modules") {
                            this.initializeOrRefreshRolesModulesGrid(dataItem.id);
                        }
                    }
                }
            }).data("kendoTabStrip");

            this.roleList = $("#roleList").kendoListView({
                template: "<li class='sortable' data-item='${id}' >${role_name}</li>",
                dataSource: {
                    transport: {
                        read: {
                            url: `${this.base.settings.serviceRoot}/GET_ROLES?isTest=${encodeURIComponent(this.base.settings.isTestEnvironment)}`
                        }
                    }
                },
                dataTextField: "display_name",
                dataValueField: "id",
                selectable: true,
                change: () => {
                    const dataItem = this.roleList.dataItem(this.roleList.select());

                    const selectedTab = this.getSelectedTabName().toLowerCase();
                    if (selectedTab === "velden") {
                        this.initializeOrRefreshRolesEntityPropertiesGrid(dataItem.id);
                    }
                    if (selectedTab === "modules") {
                        this.initializeOrRefreshRolesModulesGrid(dataItem.id);
                    }
                }
            }).data("kendoListView");
        }
    }

    // Initialize the DynamicItems class and make one instance of it globally available.
    window.admin = new Admin(settings);
})(moduleSettings);
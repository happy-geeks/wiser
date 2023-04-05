import {Dates, Wiser, Misc, Utils} from "../../Base/Scripts/Utils.js";
import "../../Base/Scripts/Processing.js";
import { DateTime } from "luxon";

require("@progress/kendo-ui/js/kendo.button.js");
require("@progress/kendo-ui/js/kendo.dialog.js");
require("@progress/kendo-ui/js/kendo.window.js");
require("@progress/kendo-ui/js/kendo.tabstrip.js");
require("@progress/kendo-ui/js/kendo.validator.js");
require("@progress/kendo-ui/js/kendo.combobox.js");
require("@progress/kendo-ui/js/cultures/kendo.culture.nl-NL.js");
require("@progress/kendo-ui/js/messages/kendo.messages.nl-NL.js");

/**
* Class for any and all functionality for fields.
*/
export class Fields {

    /**
     * Initializes a new instance of the Fields class.
     * @param {DynamicItems} base An instance of the base class (DynamicItems).
     */
    constructor(base) {
        this.base = base;
        this.dependencies = {};
        this.fieldInitializers = {};
        this.fieldSelector = "select:not(:disabled,[readonly]), input:not(:disabled,[readonly]), [data-kendo-control], textarea:not(:disabled,[readonly])";
        this.originalItemValues = {};
        this.unsavedItemValues = {};
    }

    /**
     * Do all initializations for the Fields class, such as adding bindings.
     */
    initialize() {
        // Bind tooltip click events.
        $("#right-pane").on("click", ".item h4.tooltip .info-link", this.onTooltipClick.bind(this, $("#infoPanel_main")));
        $("#right-pane").on("contextmenu", ".item > h4", this.onFieldLabelContextMenu.bind(this));
    }

    /**
     * Gets the entered data of all fields in a container.
     * @param {any} container The contains to get the data from.
     * @returns {any} An object with all entered data in the container.
     */
    getInputData(container) {
        const results = [];
        const allControls = container.find(this.fieldSelector);

        allControls.each((index, element) => {
            const field = $(element);
            const fieldData = field.closest(".item").data() || {};
            let fieldName = fieldData.propertyName;

            // If we have no name attribute, then it's not an element that we need to use.
            // It's probably a sub element of some Kendo component then.
            if (!field.attr("name") || field.hasClass("skip-when-saving")) {
                return;
            }

            const kendoControlName = field.data("kendoControl");
            if (!kendoControlName && (field.attr("role") === "combobox" || field.attr("role") === "dropdownlist") && field.prop("tagName") === "INPUT") {
                fieldName = `${fieldName}_input`;
            }

            const data = {
                key: fieldName,
                itemLinkId: parseInt(fieldData.itemLinkId) || 0,
                languageCode: fieldData.languageCode || ""
            };

            data.isLinkProperty = data.itemLinkId > 0;

            const advancedCheckBoxContainer = field.closest(".checkbox-full-container");
            if (advancedCheckBoxContainer.length > 0) {
                // This is a check box group.
                const checkBoxes = advancedCheckBoxContainer.find(".checkbox-full-panel input[type='checkbox']:checked");
                const ids = [];
                const names = [];
                checkBoxes.each((index, element) => {
                    const optionData = $(element).data();
                    if (!optionData.id) {
                        return;
                    }

                    ids.push(optionData.id);
                    names.push(optionData.name);
                });

                data.value = ids;
                results.push(data);

                const extraData = $.extend({}, data);
                extraData.key += "_value";
                extraData.value = names;
                results.push(extraData);
                return;
            }

            if (kendoControlName) {
                let kendoControl = field.data(kendoControlName);

                if (!kendoControl && kendoControlName === "kendoComboBox") {
                    // If the kendoControl is not defined and it's a kendoComboBox, it means that the combobox was configured to be a dropdown list, so use that instead.
                    kendoControl = field.data("kendoDropDownList");
                    if (kendoControl) {
                        // Add the text value of the dropdown list. This happens automatically for combobox (because a Kendo combobox creates an extra input), but not for dropdown list.
                        const kendoDropDownListData = $.extend({}, data, { key: `${fieldName}_input`, value: kendoControl.text() });
                        results.push(kendoDropDownListData);
                    }
                }

                if (kendoControl) {
                    data.value = kendoControl.value();
                    if (kendoControlName === "kendoDatePicker" && data.value) {
                        data.value.setHours(5);
                    } else if (kendoControlName === "kendoMultiSelect") {
                        const extraData = $.extend({}, data);
                        extraData.key += "_value";
                        extraData.value = kendoControl.dataItems().map(d => d.name);
                        results.push(extraData);
                    }
                    results.push(data);
                    return;
                } else {
                    console.log(`Kendo control found for '${fieldName}', but it's not initialized, so using default value.`, kendoControlName, data);
                    data.value = field.data("defaultValue");
                    if (data.value) {
                        results.push(data);
                    }
                    return;
                }
            }

            const codeMirrorInstance = field.data("CodeMirrorInstance");
            if (codeMirrorInstance) {
                data.value = codeMirrorInstance.getValue();
                results.push(data);
                return;
            }

            // If we reach this point in the code, this element is not a Kendo control, so just get the normal value.
            switch (field.prop("tagName")) {
                case "SELECT":
                    data.value = field.val();
                    break;
                case "INPUT":
                case "TEXTAREA":
                    switch ((field.attr("type") || "").toUpperCase()) {
                        case "CHECKBOX":
                            data.value = field.prop("checked");
                            break;
                        default:
                            data.value = field.val();
                            break;
                    }
                    break;
                default:
                    console.error("TODO: Unsupported tag name:", field.prop("tagName"));
                    return;
            }

            results.push(data);
        });

        return results;
    }

    /**
     * Setup dependencies for all fields.
     * @param {any} container The container that contains all the fields to setup the dependencies for.
     * @param {string} entityType The entity type of the loaded item.
     * @param {any} tabName The name of the tab to setup the dependencies for.
     * @returns {any} An object with all dependencies.
     */
    setupDependencies(container, entityType, tabName) {
        // Reset dependencies of entity type, so that we're always using the latest ones.
        if (!this.dependencies[entityType]) {
            this.dependencies[entityType] = {};
        }
        this.dependencies[entityType][tabName] = {};

        const allControls = container.find(".item");

        allControls.each((index, element) => {
            const controlContainer = $(element);
            const data = controlContainer.data();

            if (!data.dependsOnField) {
                return;
            }

            if (!this.dependencies[entityType][tabName][data.dependsOnField]) {
                this.dependencies[entityType][tabName][data.dependsOnField] = [];
            }

            this.dependencies[entityType][tabName][data.dependsOnField].push(data);
        });

        return this.dependencies[entityType][tabName];
    }

    /**
     * Handles the dependencies for all fields within a certain container.
     * @param {any} container The container.
     * @param {any} entityType The entity type of the current item.
     * @param {any} tabName The name of the tab to setup the dependencies for.
     * @param {string} windowId The ID of the window that contains the tabs and fields. If this is for the default/main screen/window, enter "mainScreen" in this parameter.
     */
    handleAllDependenciesOfContainer(container, entityType, tabName, windowId) {
        this.originalItemValues[windowId] = {};
        this.unsavedItemValues[windowId] = {};

        const allControls = container.find("select, input, [data-kendo-control], textarea");
        allControls.each((index, element) => {
            const field = $(element);
            const container = field.closest(".item");
            const propertyName = container.data("propertyName");

            const kendoControlName = field.data("kendoControl");
            if (kendoControlName) {
                let kendoControl = field.data(kendoControlName);
                if (!kendoControl && kendoControlName === "kendoComboBox") {
                    kendoControl = field.data("kendoDropDownList");
                }

                if (kendoControl) {
                    if (typeof kendoControl.value === "function") {
                        this.originalItemValues[windowId][propertyName] = kendoControl.value();
                    }

                    this.handleDependencies({ sender: kendoControl }, tabName);
                    return;
                }
            }


            if (element.tagName.toUpperCase() !== "INPUT") {
                this.originalItemValues[windowId][propertyName] = field.val();
            } else {
                switch ((field.attr("type") || "").toLowerCase()) {
                    case "checkbox":
                        this.originalItemValues[windowId][propertyName] = field.prop("checked");
                        break;
                    default:
                        this.originalItemValues[windowId][propertyName] = field.val();
                        break;
                }
            }

            this.handleDependencies({ currentTarget: element }, tabName);
        });
    }

    /**
     * Handles dependencies for fields; Fields can be dependent on values of other fields.
     * They should be made invisible if the dependent fields don't have the required values.
     * This method should be called every time a value of a field has been changed.
     * @param {any} event The change event of the field where the value was changed.
     * @param {string} selectedTab The name of the selected tab.
     */
    handleDependencies(event, selectedTab) {
        let valueOfElement;
        let currentDependencies = [];
        let container;
        let entityType;
        let tabStrip;

        if (event.sender) {
            if (typeof event.sender.value !== "function") {
                console.warn("Kendo control found, but it doesn't have a value function.", event.sender);
                return;
            }

            container = event.sender.element.closest(".item");
            tabStrip = container.closest(".k-tabstrip").data("kendoTabStrip");

            if (!selectedTab) {
                if (!tabStrip) {
                    console.error("Could not find kendoTabStrip and therefor cannot handle dependencies!", event.sender);
                    return;
                }

                selectedTab = tabStrip.select().text() || "Gegevens";
            }

            entityType = container.closest(".entity-container").data("entityType");

            valueOfElement = event.sender.value();
        } else {
            const currentElement = $(event.currentTarget);
            container = currentElement.closest(".item");
            entityType = container.closest(".entity-container").data("entityType");
            tabStrip = container.closest(".k-tabstrip").data("kendoTabStrip");

            if (!selectedTab) {
                if (!tabStrip) {
                    console.error("Could not find kendoTabStrip and therefor cannot handle dependencies!", event.currentTarget);
                    return;
                }

                selectedTab = tabStrip.select().text();
            }


            if (currentElement[0].tagName.toUpperCase() !== "INPUT") {
                valueOfElement = currentElement.val();
            } else {
                switch ((currentElement.attr("type") || "").toLowerCase()) {
                    case "checkbox":
                        valueOfElement = currentElement.prop("checked");
                        break;
                    default:
                        valueOfElement = currentElement.val();
                        break;
                }
            }
        }

        if (!this.dependencies[entityType]) {
            this.dependencies[entityType] = {};
        }
        if (!this.dependencies[entityType][selectedTab]) {
            this.dependencies[entityType][selectedTab] = {};
        }

        for (let tab in this.dependencies[entityType]) {
            if (!this.dependencies[entityType].hasOwnProperty(tab)) {
                continue;
            }

            const propertyDependencies = this.dependencies[entityType][tab][container.data("propertyName")];
            if (!propertyDependencies || !propertyDependencies.length) {
                continue;
            }

            currentDependencies = [...currentDependencies, ...propertyDependencies];
        }

        // Remember the change.
        const entityContainer = container.closest(".entity-container");
        const windowId = entityContainer.hasClass("popup-container") ? entityContainer.attr("id") : "mainScreen";
        const propertyName = container.data("propertyName");
        if (!this.unsavedItemValues[windowId]) {
            this.unsavedItemValues[windowId] = {};
        }

        if ((typeof valueOfElement === "object" && JSON.stringify(this.base.fields.originalItemValues[windowId][propertyName]) !== JSON.stringify(valueOfElement)) || (typeof valueOfElement !== "object" && this.base.fields.originalItemValues[windowId][propertyName] !== valueOfElement)) {
            this.unsavedItemValues[windowId][propertyName] = valueOfElement;
        }

        if (!currentDependencies || !currentDependencies.length) {
            return;
        }

        valueOfElement = typeof valueOfElement === "undefined" || valueOfElement === null ? "" : valueOfElement;
        currentDependencies.forEach((dependency) => {
            const values = (typeof dependency.dependsOnValue === "undefined" || dependency.dependsOnValue === null ? "" : dependency.dependsOnValue.toString()).split(",");

            const parsedValues = values.map(dependsOnValue => {
                switch (Object.prototype.toString.call(valueOfElement)) {
                    case "[object Date]":
                        dependsOnValue = new Date(dependsOnValue);
                        break;
                    case "[object Number]":
                        dependsOnValue = parseFloat(dependsOnValue);
                        break;
                    case "[object String]":
                        dependsOnValue = dependsOnValue.toString().toLowerCase();
                        valueOfElement = valueOfElement.toLowerCase();
                        break;
                    case "[object Boolean]":
                        if (typeof dependsOnValue === "string") {
                            dependsOnValue = dependsOnValue.toLowerCase() === "true" || parseInt(dependsOnValue) > 0;
                        } else if (typeof dependsOnValue === "number") {
                            dependsOnValue = dependsOnValue > 0;
                        }
                        break;
                    default:
                        console.warn(`Value '${valueOfElement}' of field '${dependency.id}' has an unsupported type (${Object.prototype.toString.call(valueOfElement)}) for dependancy comparison, so this might not work as expected.`);
                        break;
                }

                return dependsOnValue;
            });

            switch (dependency.dependsOnAction || this.base.dependencyActionsEnum.toggleVisibility) {
                case this.base.dependencyActionsEnum.refresh: {
                    const fields = container.closest(".k-tabstrip").find(`[data-property-id='${dependency.propertyId}'].item`).find(this.fieldSelector);

                    for (let field of fields) {
                        field = $(field);
                        const kendoControlName = field.data("kendoControl");
                        if (!kendoControlName && (field.attr("name") || "").indexOf("_input") !== -1) {
                            console.warn(`Refreshing non-kendo fields is not implemented yet!`);
                            continue;
                        }

                        let kendoControl = field.data(kendoControlName);

                        if (!kendoControl && kendoControlName === "kendoComboBox") {
                            kendoControl = field.data("kendoDropDownList");
                        }

                        if (!kendoControl) {
                            console.warn(`Kendo control found, but it hasn't been initialized properly, so we can't refresh it.`);
                            continue;
                        }

                        if (!kendoControl.dataSource) {
                            console.warn(`Kendo control found, but it has no data source property. Refreshing is only implemented for kendo controls with a data source.`);
                            continue;
                        }

                        // Reload the data source.
                        kendoControl.dataSource.read();

                        // If the dependent element has a value and this element has en open function, call that function.
                        // This way the user can immediately enter a value in the next element.
                        /* TODO: Enable and maybe edit this code after I have discussed it with Ferry.
                         if (valueOfElement && kendoControl.open) {
                            kendoControl.one("dataBound", dataBoundEvent => dataBoundEvent.sender.open());
                        }*/
                    }

                    break;
                }

                case this.base.dependencyActionsEnum.toggleVisibility: {
                    let showElement = false;

                    switch (dependency.dependsOnOperator || this.base.comparisonOperatorsEnum.equals) {
                        case this.base.comparisonOperatorsEnum.equals:
                            showElement = parsedValues.filter(dependsOnValue => dependsOnValue === valueOfElement).length > 0;
                            break;
                        case this.base.comparisonOperatorsEnum.doesNotEqual:
                            showElement = parsedValues.filter(dependsOnValue => dependsOnValue === valueOfElement).length === 0;
                            break;
                        case this.base.comparisonOperatorsEnum.contains:
                            showElement = parsedValues.filter(dependsOnValue => valueOfElement.indexOf(dependsOnValue) > -1).length > 0;
                            break;
                        case this.base.comparisonOperatorsEnum.doesnotcontain:
                            showElement = parsedValues.filter(dependsOnValue => valueOfElement.indexOf(dependsOnValue) > -1).length === 0;
                            break;
                        case this.base.comparisonOperatorsEnum.startsWith:
                            showElement = parsedValues.filter(dependsOnValue => valueOfElement.indexOf(dependsOnValue) === 0).length > 0;
                            break;
                        case this.base.comparisonOperatorsEnum.doesNotStartWith:
                            showElement = parsedValues.filter(dependsOnValue => valueOfElement.indexOf(dependsOnValue) === 0).length === 0;
                            break;
                        case this.base.comparisonOperatorsEnum.endsWith:
                            showElement = parsedValues.filter(dependsOnValue => valueOfElement.indexOf(dependsOnValue) === valueOfElement.length - dependsOnValue.length - 1).length > 0;
                            break;
                        case this.base.comparisonOperatorsEnum.doesNotEndWith:
                            showElement = parsedValues.filter(dependsOnValue => valueOfElement.indexOf(dependsOnValue) === valueOfElement.length - dependsOnValue.length - 1).length === 0;
                            break;
                        case this.base.comparisonOperatorsEnum.isEmpty:
                            showElement = valueOfElement === "";
                            break;
                        case this.base.comparisonOperatorsEnum.isNotEmpty:
                            showElement = valueOfElement !== "";
                            break;
                        case this.base.comparisonOperatorsEnum.isGreaterThanOrEquals:
                            showElement = parsedValues.filter(dependsOnValue => valueOfElement >= dependsOnValue).length > 0;
                            break;
                        case this.base.comparisonOperatorsEnum.isGreaterThan:
                            showElement = parsedValues.filter(dependsOnValue => valueOfElement > dependsOnValue).length > 0;
                            break;
                        case this.base.comparisonOperatorsEnum.isLessThanOrEquals:
                            showElement = parsedValues.filter(dependsOnValue => valueOfElement <= dependsOnValue).length > 0;
                            break;
                        case this.base.comparisonOperatorsEnum.isLessThan:
                            showElement = parsedValues.filter(dependsOnValue => valueOfElement < dependsOnValue).length > 0;
                            break;
                        default:
                            console.error("Unknown dependsOnOperator found!", dependency.dependsOnOperator);
                            break;
                    }

                    const fieldContainer = container.closest(".k-tabstrip").find(`[data-property-id='${dependency.propertyId}'].item`).toggle(showElement);
                    const tabContainer = fieldContainer.closest(".k-content");
                    const allFields = tabContainer.find(".item");
                    const visibleFields = allFields.filter(index => allFields[index].style.display !== "none");
                    const tabIndex = tabContainer.index() - 1; // -1 because the first item in the DOM is always the tab strip (<ul>), we shouldn't count that one.
                    tabStrip.tabGroup.children().eq(tabIndex).toggle(visibleFields.length > 0);

                    break;
                }
                default:
                    {
                        console.warn(`Unknown dependency action '${dependency.dependsOnAction}'!`);
                        break;
                    }
            }
        });
    }

    /**
     * Initializes fields for a tab by executing any and all javascript for all fields on a certain tab in a certain window (or the main screen).
     * This function should be called every time the user opens a different tab.
     * This function can safely be called multiple times, it will keep track of which tabs have already been initialized before and won't do it a second time.
     * @param {string} windowId The ID of the window that contains the tabs and fields. If this is for the default/main screen/window, enter "mainScreen" in this parameter.
     * @param {string} tabName The name of the tab that the user opened.
     * @param {any} tabContentContainer The container that contains the contents of the selected tab.
     */
    async initializeDynamicFields(windowId, tabName, tabContentContainer) {
        const windowFields = this.fieldInitializers[windowId];
        if (!windowFields) {
            console.warn(`initializeDynamicFields called with non-existing windowId: ${windowId}`);
            return;
        }

        const tabFields = windowFields[tabName];
        if (!tabFields) {
            if (tabName !== "Overzicht" && tabName !== "Gegevens" && tabName !== "Historie") {
                console.warn(`initializeDynamicFields called with non-existing tab. Window: '${windowId}', tab: ${tabName}`);
            }
            return;
        }

        if (!tabFields.script) {
            console.log(`initializeDynamicFields called, but script is empty. Window: '${windowId}', tab: ${tabName}`);
            return;
        }

        if (tabFields.executed) {
            return;
        }

        const process = `initializeDynamicFields_${Date.now()}`;
        window.processing.addProcess(process);

        try {
            await this.base.loadKendoScripts(tabFields.script);
            $.globalEval(tabFields.script);
            tabFields.executed = true;

            this.base.fields.handleAllDependenciesOfContainer(tabContentContainer, tabFields.entityType, tabName, windowId);
        } catch (exception) {
            console.error(exception);
            kendo.alert("Er is iets fout gegaan tijdens het uitvoeren van scripts voor velden op dit tabblad. Neem a.u.b. contact op met ons.");
        }

        window.processing.removeProcess(process);
    }

    /**
     * Event that gets triggered when the user clicks the link-icon in a field containing an URL.
     * @param {any} field The input field.
     * @param {any} fieldOptions The options/settings of the input field.
     * @param {any} event The click event.
     */
    onInputLinkIconClick(field, fieldOptions, event) {
        event.preventDefault();

        const fieldValue = field.val();
        if (!fieldValue) {
            return;
        }

        const urlToOpen = (fieldOptions.prefix || "") + fieldValue + (fieldOptions.suffix || "");
        if (fieldOptions.skipOpenUrlDialog) {
            window.open(urlToOpen);
            return;
        }

        let openLinkDialog = $("<div />").kendoDialog({
            width: "500px",
            buttonLayout: "normal",
            title: "Deze URL openen?",
            closable: true,
            modal: true,
            content: "<p>Wilt u deze URL in een nieuw venster openen of binnen Wiser (let op, niet alle webistes kunnen geladen worden binnen Wiser)?<p>",
            actions: [
                {
                    text: "Annuleren"
                },
                {
                    text: "Open in Wiser",
                    action: (kendoEvent) => {
                        $("#openLinkWindow").kendoWindow({
                            width: "100%",
                            height: "100%",
                            title: "Externe URL",
                            content: urlToOpen
                        }).data("kendoWindow").open();
                    }
                },
                {
                    text: "Open in een nieuw venster",
                    primary: true,
                    action: (kendoEvent) => {
                        window.open(urlToOpen);
                    }
                }
            ]
        });

        openLinkDialog.data("kendoDialog").open();
    }

    /**
     * Event that gets triggered when the user right clicks the label of a field.
     * @param {any} event
     */
    onFieldLabelContextMenu(event) {
        if (!this.base.settings.adminAccountLoggedIn) {
            return;
        }

        event.preventDefault();
        const element = $(event.currentTarget);
        const propertyName = element.closest(".item").data("propertyName");
        element.html(`${element.html()} <span class="property-name">(${propertyName})</span>`);

        // Copy to clip board.
        const copyText = document.createElement("input");
        copyText.value = propertyName;
        document.body.appendChild(copyText);
        copyText.focus();
        copyText.select();
        document.execCommand("copy");
        document.body.removeChild(copyText);
    }

    /**
     * Event that gets fired when someone clicks the tooltip icon of a field.
     * @param {any} event
     * @param {any} infoPanel
     */
    onTooltipClick(infoPanel, event) {
        const currentTarget = $(event.currentTarget);
        const fieldContainer = currentTarget.closest(".item");
        const splitContainer = currentTarget.closest("#right-pane");
        const infoText = fieldContainer.find(".form-hint").html();
        const fieldName = fieldContainer.find("h4").text();

        if (splitContainer.length <= 0) {
            currentTarget.closest(".entity-container").addClass("info-active");
        } else {
            currentTarget.closest("#right-pane").addClass("info-active");
        }
        infoPanel.find(".info-title").text(fieldName);
        infoPanel.find(".info-content").html(infoText);
    }

    /**
     * Event that gets called when a file upload succeeded in a kendoUpload field.
     * @param {any} event The kendo upload success event.
     */
    async onUploaderSuccess(event) {
        event.sender.wrapper.find(`li[data-uid='${event.files[0].uid}'] .fileId`).html(event.response[0].fileId);
        event.sender.wrapper.find(`li[data-uid='${event.files[0].uid}'] .title`).html(kendo.htmlEncode(event.response[0].title || "(leeg)"));
        event.sender.wrapper.find(`li[data-uid='${event.files[0].uid}'] .fileContainer`).data("fileId", event.response[0].fileId).data("itemId", event.response[0].itemId);
        event.sender.wrapper.find(`li[data-uid='${event.files[0].uid}'] .name`).attr("href", `${this.base.settings.wiserApiRoot}items/${encodeURIComponent(event.response[0].itemId)}/files/${encodeURIComponent(event.response[0].fileId)}/${encodeURIComponent(event.response[0].name)}?itemLinkId=${event.response[0].itemLinkId || 0}&entityType=${encodeURIComponent(event.response[0].entityType || "")}&linkType=${event.response[0].linkType || 0}&encryptedCustomerId=${encodeURIComponent(this.base.settings.customerId)}&encryptedUserId=${encodeURIComponent(this.base.settings.userId)}&isTest=${this.base.settings.isTestEnvironment}&subDomain=${encodeURIComponent(this.base.settings.subDomain)}`);
        let addedOn = (event.response[0].addedOn ? DateTime.fromISO(event.response[0].addedOn, { locale: "nl-NL" }) : DateTime.now()).toLocaleString(Dates.LongDateTimeFormat);
        event.sender.wrapper.find(`li[data-uid='${event.files[0].uid}'] .fileDate`).html(kendo.htmlEncode(addedOn));
        event.sender.wrapper.find(".editTitle").click(this.onUploaderEditTitleClick.bind(this));
        event.sender.wrapper.find(".editName").click(this.onUploaderEditNameClick.bind(this));
    }

    /**
     * Event that gets fired when the users changes the value of a kendoDropdown.
     * @param {any} event The change event of the kendoDropdown.
     * @param {any} options The field options.
     */
    onDropDownChange(event, options) {
        this.onFieldValueChange(event);

        if (options.allowOpeningOfSelectedItem) {
            event.sender.element.closest(".item").find(".openItemButton").toggleClass("hidden", !event.sender.value());
        }

        if (!options.linkedFields || !options.linkedFields.length) {
            return;
        }

        const fieldsContainer = event.sender.element.closest(".k-content");
        const selectedItem = event.sender.dataItem();
        options.linkedFields.forEach((fieldName) => {
            if (!selectedItem[fieldName]) {
                return;
            }

            const value = selectedItem[fieldName];
            const fieldContainer = fieldsContainer.find(`[data-property-name='${fieldName}']`);
            const field = fieldContainer.find(`[name='${fieldName}']`);
            const kendoControlName = field.data("kendoControl");

            if (kendoControlName) {
                const kendoControl = field.data(kendoControlName);
                if (kendoControl) {
                    if (options.overwriteExistingValues || !kendoControl.value()) {
                        kendoControl.value(value);
                    }
                    return;
                }
            }

            if (options.overwriteExistingValues || !field.val()) {
                field.val(value);
            }
        });
    }

    /**
     * Event that gets triggered when the user searches in a combobox field.
     * @param {any} event The search event from Kendo.
     * @param {any} itemId The ID of the currently opened item.
     * @param {any} options The field options.
     */
    async onComboBoxFiltering(event, itemId, options) {
        event.preventDefault();
        const comboBoxContainer = event.sender.element.closest(".item");
        if (!options) {
            console.error("Cannot find options for combo box.");
            return;
        }

        const searchEverywhere = options.searchEverywhere && (options.searchEverywhere > 0 || options.searchEverywhere.toLowerCase() === "true") ? 1 : 0;
        const icon = comboBoxContainer.find(".k-i-arrow-s").addClass("k-loading");
        const searchFields = options.searchFields || [];
        const searchInTitle = options.searchInTitle === true || options.searchInTitle === "true" || options.searchInTitle > 0 ? 1 : 0;
        let searchModuleId = options.moduleId || 0;
        if (!searchEverywhere && !searchModuleId) {
            searchModuleId = moduleId;
        }

        const result = await Wiser.api({
            url: `${this.base.settings.serviceRoot}/SEARCH_ITEMS?id=${encodeURIComponent(itemId)}&moduleid=${encodeURIComponent(searchModuleId)}&entityType=${encodeURIComponent(options.entityType)}&search=${encodeURIComponent(event.filter.value)}&searchInTitle=${encodeURIComponent(searchInTitle)}&searchFields=${encodeURIComponent(searchFields.join())}&searchEverywhere=${encodeURIComponent(searchEverywhere)}`,
            method: "GET",
            contentType: "application/json",
            dataType: "JSON"
        });

        event.sender.setDataSource(result);
        icon.removeClass("k-loading");
    }

    /**
     * Event that gets called when the user clicks the edit icon/button/link for editing a name of a file in a kendoUpload field.
     * @param {any} event The click event.
     */
    async onUploaderEditNameClick(event) {
        event.preventDefault();
        const container = $(event.currentTarget).closest(".fileContainer");
        const containerData = container.data();
        const value = await kendo.prompt("", containerData.name);

        await Wiser.api({
            url: `${this.base.settings.wiserApiRoot}items/${encodeURIComponent(containerData.itemId)}/files/${encodeURIComponent(containerData.fileId)}/rename/${encodeURIComponent(value)}?itemLinkId=${encodeURIComponent(containerData.itemLinkId || 0)}&entityType=${encodeURIComponent(containerData.entityType || "")}&linkType=${containerData.linkType || 0}`,
            method: "PUT",
            contentType: "application/json",
            dataType: "JSON"
        });
        this.base.notification.show({ message: `Bestandsnaam is succesvol aangepast` }, "success");
        container.find(".name").html(kendo.htmlEncode(value));
    }

    /**
     * Event that gets called when the user clicks the edit icon/button/link for editing a title of a file in a kendoUpload field.
     * @param {any} event The click event.
     */
    async onUploaderEditTitleClick(event) {
        event.preventDefault();
        const container = $(event.currentTarget).closest(".fileContainer");
        const containerData = container.data();
        const value = await kendo.prompt("", containerData.title);

        await Wiser.api({
            url: `${this.base.settings.wiserApiRoot}items/${encodeURIComponent(containerData.itemId)}/files/${encodeURIComponent(containerData.fileId)}/title/${encodeURIComponent(value)}?itemLinkId=${containerData.itemLinkId || 0}&entityType=${encodeURIComponent(containerData.entityType || "")}&linkType=${containerData.linkType || 0}`,
            method: "PUT",
            contentType: "application/json",
            dataType: "JSON"
        });
        this.base.notification.show({ message: `Bestandsomschrijving is succesvol aangepast` }, "success");
        container.find(".title").html(kendo.htmlEncode(value));
    }

    /**
     * Event that gets fired when the user clicks the button for a custom action in the toolbar of a sub entities grid.
     * @param {string} gridSelector A javascript selector to get the grid element with.
     * @param {number} itemId The ID of the item that contains the grid.
     * @param {number} propertyId The ID of the property / field.
     * @param {any} actionDetails The details of the clicked action.
     * @param {any} event The click event.
     * @param {string} entityType The entity type of the item that contains the grid.
     */
    async onSubEntitiesGridToolbarActionClick(gridSelector, itemId, propertyId, actionDetails, event, entityType) {
        event.preventDefault();

        // Get the grid and the selected data items.
        const senderGrid = $(gridSelector).data("kendoGrid");
        const selectedItems = [];
        if (senderGrid) {
            for (const selectedElement of senderGrid.select()) {
                const row = selectedElement.closest("tr");
                let selectedColumn = null;

                if (selectedElement.tagName === "TD") {
                    const selectedIndex = $(selectedElement).index();
                    selectedColumn = senderGrid.columns[selectedIndex];
                }

                selectedItems.push({ dataItem: senderGrid.dataItem(row), selectedColumn: selectedColumn });
            }
        }

        actionDetails.minSelectedRows = actionDetails.minSelectedRows || 1;
        actionDetails.maxSelectedRows = actionDetails.maxSelectedRows || 0;
        const selectedRowsCount = selectedItems.length;
        if (selectedRowsCount === 0) {
            if (!actionDetails.allowNoSelection) {
                kendo.alert("Selecteer a.u.b. eerst 1 of meer items in het grid.");
                return;
            }
        }
        else if (selectedRowsCount < actionDetails.minSelectedRows) {
            kendo.alert(`U heeft ${selectedRowsCount} regel(s) geselecteerd, maar deze actie vereist dat u minimaal ${actionDetails.minSelectedRows} regel(s) in het grid heeft geselecteerd.`);
            return;
        }
        else if (actionDetails.maxSelectedRows > 0 && selectedRowsCount > actionDetails.maxSelectedRows) {
            kendo.alert(`U heeft ${selectedRowsCount} regel(s) geselecteerd, maar deze actie vereist dat u maximaal ${actionDetails.minSelectedRows} regel(s) in het grid heeft geselecteerd.`);
            return;
        }

        if (senderGrid) {
            senderGrid.element.siblings(".grid-loader").addClass("loading");
        }

        // Get the item details so that those values can be used as variables in a string.
        const itemDetails = !itemId ? { encryptedId: this.base.settings.zeroEncrypted } : (await this.base.getItemDetails(itemId, entityType));

        const userParametersWithValues = {};
        const success = await this.executeActionButtonActions(actionDetails.actions, userParametersWithValues, itemDetails, propertyId, selectedItems, senderGrid.element);

        if (senderGrid && senderGrid.element) {
            senderGrid.element.siblings(".grid-loader").removeClass("loading");
        }

        if (success) {
            if (senderGrid) {
                if (actionDetails.rebuildMainGridAfterActions && this.base.settings.gridViewMode) {
                    // Destroy and recreate the grid.
                    this.base.grids.mainGrid.destroy();
                    $("#gridView").empty();
                    this.base.grids.mainGridFirstLoad = true;
                    this.base.grids.setupGridViewMode();
                } else {
                    senderGrid.dataSource.read();
                }
            }

            if (!actionDetails.disableSuccessMessages) {
                this.base.notification.show({ message: `Alle acties zijn uitgevoerd.` }, "success");
            }
        }
    }

    /**
     * Event that gets called when the user clicks an action button (this is a field type).
     * @param {any} event The click event, or the selector of the grid that is executing the action.
     * @param {number} itemId The ID of the opened item.
     * @param {number} propertyId The ID of the property.
     * @param {any} options The field options.
     * @param {any} button The action button.
     */
    async onActionButtonClick(event, itemId, propertyId, options, button) {
        try {
            event.preventDefault();
            // An action button should have at least one action, otherwise it's configured incorrectly.
            if (!options.actions || !options.actions.length) {
                kendo.alert("Deze knop is niet goed ingesteld. Neem a.u.b. contact op met ons.");
                return;
            }

            if (event.sender.element.hasClass("loading")) {
                // Don't do anything if a previous event was already loading.
                return;
            }

            event.sender.element.addClass("loading");

            // Try to determine the entity type. If this button is located within a window, that window element
            // might have the entity type set as one of its data properties.
            let entityType;
            const window = event.sender.element.closest("div.k-window-content");
            if (window) {
                entityType = window.data("entityType");
                if (!entityType && window.data("entityTypeDetails")) {
                    window.data("entityTypeDetails").entityType;
                }
            }

            // Get the item details so that those values can be used as variables in a string.
            const itemDetails = (await this.base.getItemDetails(itemId, entityType));

            // Execute all actions that are configured for this button.
            const userParametersWithValues = {};
            const success = await this.executeActionButtonActions(options.actions, userParametersWithValues, itemDetails, propertyId, [], button);
            event.sender.element.removeClass("loading");
            if (success && !options.disableSuccessMessages) {
                this.base.notification.show({ message: `Alle acties zijn uitgevoerd.` }, "success");
            }
        } catch (exception) {
            console.error(exception);
            kendo.alert("Er is iets fout gegaan tijdens het uitvoeren van deze actie. Probeer het a.u.b. nogmaals of neem contact op met ons.");
            event.sender.element.removeClass("loading");
        }
    }

    /**
     * Event that gets called when the user clicks a data selector button (this is a field type).
     * @param {any} event The click event, or the selector of the grid that is executing the action.
     * @param {number} value The currently saved ID of the data selector.
     * @param {number} itemId The ID of the opened item.
     * @param {number} propertyId The ID of the property.
     * @param {any} options The field options.
     * @param {any} field The field container.
     */
    onDataSelectorButtonClick(event, value, itemId, propertyId, options, field) {
        const dataSelectorWindow = $("<div />").kendoWindow({
            title: "Data selector",
            content: `${"Modules/DataSelector"}?load=${value || ""}`,
            iframe: true
        }).data("kendoWindow");

        dataSelectorWindow.maximize().open();

        const iframe = dataSelectorWindow.element.find("iframe")[0];
        if (!iframe) {
            console.warn("Iframe not found");
            return;
        }

        const iframeWindow = iframe.contentWindow || (iframe.contentDocument.document || iframe.contentDocument);
        if (!iframeWindow) {
            console.warn("Iframe window not found");
            return;
        }

        iframe.onload = () => {
            // Need to wait until the iframe is loaded, before we can add event listeners.
            iframeWindow.document.addEventListener("dataSelectorAfterSave", (saveEvent) => {
                if (!saveEvent) {
                    kendo.alert("Er is iets fout gegaan tijdens het opslaan van de data selector. Probeer het a.u.b. nogmaals of neem contact op met ons.");
                    return;
                }

                // Save the value in the hidden input, so that it will be saved in wiser_itemdetail once the user saves the item.
                field.prev("input.valueField").val(saveEvent);
                dataSelectorWindow.close().destroy();
            });
        };
    }

    /**
     * Add the uploaded image to the current list of images.
     * @param {any} event The success event.
     */
    onImageUploadSuccess(event) {
        if (event.operation !== "upload") {
            return;
        }

        const newIds = event.response;
        const container = event.sender.wrapper.closest(".item");

        event.sender.options.files.push(...newIds);

        event.files.forEach((item, i) => {
            const file = item.rawFile;

            if (!file) {
                return;
            }

            var reader = new FileReader();

            reader.onloadend = function () {
                const newImageElement = $(`<div class='product' data-item-id='${container.data("encryptedItemId")}' data-image-id='${newIds[i].fileId}'><img src=${this.result} /><div class='imgTools'><button type='button' class='imgZoom' title='Preview'></button><button type='button' class='imgEdit' title='Edit'></button><button type='button' class='imgDelete' title='Delete'></button></div></div>`);
                container.find(".uploader .imagesContainer").append(newImageElement);
            };

            reader.readAsDataURL(file);
        });
    }

    /**
     * Handle errors during upload.
     * @param {any} event The success event.
     */
    onFileUploadError(event) {
        console.error("onFileUploadError", event);

        let errorMessage = "Er is iets fout gegaan met het uploaden. Probeer het a.u.b. nogmaals of neem contact op met ons.";
        if (event && event.XMLHttpRequest) {
            if (event.XMLHttpRequest.responseText === "File is to large for database.") {
                errorMessage = "Het bestand dat u probeert te uploaden is te groot. Kies a.u.b. een kleiner bestand of neem contact op met ons om het limiet te laten verhogen.";
            } else {
                try {
                    // If the responseText is a JSON object, it is a .NET exception, which will always say "An error has occurred" on production, so we just want to show a generic error.
                    JSON.parse(event.XMLHttpRequest.responseText);
                } catch (exception) {
                    // If it's not a valid JSON object, it should be an error message that we can directly show to the user.
                    // Javascript has no other way to detect if a string contains a valid JSON object, so we have to do it with a try/catch.
                    errorMessage = event.XMLHttpRequest.responseText;
                }
            }
        }

        kendo.alert(errorMessage);
    }

    /**
     * Method for deleting files in a kendo upload component.
     * @param {any} event The delete event from Kendo.
     */
    async onFileDelete(event) {
        event.preventDefault();

        try {
            for (let fileData of event.files) {
                const fileElement = event.sender.wrapper.find(`[data-uid='${fileData.uid}']`);
                const fileContainer = fileElement.find(".fileContainer");
                const containerData = fileContainer.data();
                const fileId = fileData.fileId || fileContainer.data("fileId");
                const itemId = fileData.itemId || fileContainer.data("itemId");
                const itemLinkId = fileData.itemLinkId || fileContainer.data("itemLinkId") || 0;

                await Wiser.api({
                    url: `${this.base.settings.wiserApiRoot}items/${encodeURIComponent(itemId)}/files/${encodeURIComponent(fileId)}?itemLinkId=${encodeURIComponent(itemLinkId || 0)}&entityType=${encodeURIComponent(containerData.entityType || "")}&linkType=${containerData.linkType || 0}`,
                    method: "DELETE",
                    contentType: "application/json",
                    dataType: "JSON"
                });

                fileElement.remove();
                window.dynamicItems.notification.show({ message: "Verwijderen van bestand is gelukt" }, "success");
            }
        } catch (exception) {
            console.error("Error on onFileDelete", exception);
            window.dynamicItems.notification.show({ message: `Er is iets fout gegaan met opslaan: ${exception}` }, "error");
        }
    }

    async onImageEdit(event) {
        const imageContainer = $(event.currentTarget).closest(".product");
        const data = imageContainer.data();

        try {
            const dialogElement = $("#changeImageDataDialog");
            let changeImageDataDialog = dialogElement.data("kendoDialog");

            // Set the initial values from the query.
            dialogElement.find("input[name=fileName]").val(data.fileName);
            dialogElement.find("input[name=title]").val(data.title);

            data.extraData = data.extraData || {};
            data.extraData.AltTexts = data.extraData.AltTexts || {};
            dialogElement.find(".alt-text").remove();
            const altTextTemplateElement = dialogElement.find(".alt-text-template");
            if (!this.base.allLanguages || !this.base.allLanguages.length) {
                const clone = altTextTemplateElement.clone(true);
                clone.removeClass("hidden").removeClass("alt-text-template").addClass("alt-text");

                const cloneLabel = clone.find("label");
                cloneLabel.attr("for", `${cloneLabel.attr("for")}General`);

                const cloneInput = clone.find("input");
                cloneInput.attr("name", "altText_general");
                cloneInput.attr("id", `${cloneInput.attr("id")}General`);
                cloneInput.data("language", "general");
                cloneInput.val(data.extraData.AltTexts.general || "");
                dialogElement.find(".formview").append(clone);
            } else {
                for (let language of this.base.allLanguages) {
                    const languageCode = language.code.toLowerCase();
                    const clone = altTextTemplateElement.clone(true);
                    clone.removeClass("hidden").removeClass("alt-text-template").addClass("alt-text");

                    const cloneLabel = clone.find("label");
                    cloneLabel.attr("for", `${cloneLabel.attr("for")}${languageCode}`);
                    cloneLabel.find(".language").text(language.name);

                    const cloneInput = clone.find("input");
                    cloneInput.attr("name", `altText_${language.code}`);
                    cloneInput.attr("id", `${cloneInput.attr("id")}${languageCode}`);
                    cloneInput.attr("data-language", languageCode);
                    cloneInput.val(data.extraData.AltTexts[languageCode] || "");
                    dialogElement.find(".formview").append(clone);
                }
            }

            if (changeImageDataDialog) {
                changeImageDataDialog.destroy();
            }

            changeImageDataDialog = dialogElement.kendoDialog({
                width: "900px",
                title: "Afbeelding eigenschappen wijzigen",
                closable: false,
                modal: true,
                actions: [
                    {
                        text: "Annuleren"
                    },
                    {
                        text: "Opslaan",
                        primary: true,
                        action: (event) => {
                            const process = `updateFile_${Date.now()}`;
                            window.processing.addProcess(process);

                            try {
                                const newFileName = dialogElement.find("input[name=fileName]").val();
                                const newTitle = dialogElement.find("input[name=title]").val();
                                const extraData = $.extend(true, {}, data.extraData);
                                extraData.AltTexts = {};
                                dialogElement.find(".alt-text input").each((index, element) => {
                                    const input = $(element);
                                    extraData.AltTexts[input.data("language").toLowerCase()] = input.val();
                                });

                                imageContainer.data("extraData", extraData);

                                const promises = [
                                    Wiser.api({
                                        url: `${this.base.settings.wiserApiRoot}items/${encodeURIComponent(data.itemId)}/files/${encodeURIComponent(data.imageId)}/rename/${encodeURIComponent(newFileName)}?itemLinkId=${encodeURIComponent(data.itemLinkId || 0)}&entityType=${encodeURIComponent(data.entityType || "")}&linkType=${data.linkType || 0}`,
                                        method: "PUT",
                                        contentType: "application/json",
                                        dataType: "JSON"
                                    }),
                                    Wiser.api({
                                        url: `${this.base.settings.wiserApiRoot}items/${encodeURIComponent(data.itemId)}/files/${encodeURIComponent(data.imageId)}/extra-data/?itemLinkId=${encodeURIComponent(data.itemLinkId || 0)}&entityType=${encodeURIComponent(data.entityType || "")}&linkType=${data.linkType || 0}`,
                                        method: "PUT",
                                        contentType: "application/json",
                                        dataType: "JSON",
                                        data: JSON.stringify(extraData)
                                    })
                                ];

                                if (newTitle) {
                                    promises.push(Wiser.api({
                                        url: `${this.base.settings.wiserApiRoot}items/${encodeURIComponent(data.itemId)}/files/${encodeURIComponent(data.imageId)}/title/${encodeURIComponent(newTitle)}?itemLinkId=${encodeURIComponent(data.itemLinkId || 0)}&entityType=${encodeURIComponent(data.entityType || "")}&linkType=${data.linkType || 0}`,
                                        method: "PUT",
                                        contentType: "application/json",
                                        dataType: "JSON"
                                    }));
                                }

                                Promise.all(promises).then(() => {
                                    imageContainer.data("fileName", newFileName);
                                    imageContainer.data("title", newTitle);
                                    changeImageDataDialog.close();
                                    this.base.notification.show({ message: `Afbeelding is succesvol aangepast` }, "success");
                                }).catch((error) => {
                                    console.error(error);
                                    kendo.alert("Er is iets fout gegaan. Probeer het a.u.b. nogmaals of neem contact op met ons.");
                                }).finally(() => {
                                    window.processing.removeProcess(process);
                                });
                            } catch (exception) {
                                console.error(exception);
                                kendo.alert("Er is iets fout gegaan. Probeer het a.u.b. nogmaals of neem contact op met ons.");
                                window.processing.removeProcess(process);
                            }
                        }
                    }
                ]
            }).data("kendoDialog");

            changeImageDataDialog.open();
        } catch (exception) {
            console.error(exception);
            kendo.alert("Er is iets fout gegaan met het verwijderen van de afbeelding. Probeer het a.u.b. nogmaals of neem contact op met ons.");
        }
    }

    /**
     * Delete an image from an item.
     * @param {any} event The delete event.
     */
    async onImageDelete(event) {
        event.preventDefault();
        // If event.currentTarget is not undefined, it means the user clicked the delete button manually.
        if (event.currentTarget) {
            await Wiser.showConfirmDialog(`Weet u zeker dat u deze afbeelding wilt verwijderen?`);

            const imageContainer = $(event.currentTarget).closest(".product");
            const data = imageContainer.data();

            try {
                await Wiser.api({
                    url: `${this.base.settings.wiserApiRoot}items/${encodeURIComponent(data.encryptedItemId || data.itemId)}/files/${encodeURIComponent(data.imageId || data.fileId)}?itemLinkId=${encodeURIComponent(data.itemLinkId || 0)}&entityType=${encodeURIComponent(data.entityType || "")}&linkType=${data.linkType || 0}`,
                    method: "DELETE",
                    contentType: "application/json",
                    dataType: "JSON"
                });

                imageContainer.remove();
                window.dynamicItems.notification.show({ message: "Verwijderen van afbeelding is gelukt" }, "success");
            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan met het verwijderen van de afbeelding. Probeer het a.u.b. nogmaals of neem contact op met ons.");
            }
        } else {
            // This happens when the component automatically deletes files because of the setting "multiple=false".
            const imageContainer = event.sender.wrapper.closest(".item").find(`.product`).first();
            if (!imageContainer.length) {
                return;
            }
            const fileId = imageContainer.data("imageId");
            if (!fileId) {
                return;
            }
            const itemId = imageContainer.data("itemId");
            const itemLinkId = imageContainer.data("itemLinkId") || 0;
            const data = imageContainer.data();

            await Wiser.api({
                url: `${this.base.settings.wiserApiRoot}items/${encodeURIComponent(itemId)}/files/${encodeURIComponent(fileId)}?itemLinkId=${encodeURIComponent(itemLinkId || 0)}&entityType=${encodeURIComponent(data.entityType || "")}&linkType=${data.linkType || 0}`,
                method: "DELETE",
                contentType: "application/json",
                dataType: "JSON"
            });

            imageContainer.remove();
        }
    }

    /**
     * Execute all actions of an action button.
     * @param {any} actions An array with one or more actions that should be executed.
     * @param {any} userParametersWithValues An object in which to remember all variables that the user entered values for.
     * @param {any} mainItemDetails The details of the main item that contains the action button.
     * @param {number} propertyId The ID of the property/field that contains the action button.
     * @param {Array<any>} selectedItems Optional: If the action button is part of a grid, this parameter should contain all the selected items of that grid, so that the actions will be executed for all those items.
     * @returns {boolean} Whether the actions were all successful or not.
     * @param {any} element The action button or grid.
     */
    async executeActionButtonActions(actions, userParametersWithValues, mainItemDetails, propertyId, selectedItems = [], element = null) {
        userParametersWithValues = userParametersWithValues || {};

        const getSuffixFromSelectedColumn = (selectedItem) => {
            let suffixToUse = "";
            if (selectedItem.selectedColumn) {
                const selectedColumnName = selectedItem.selectedColumn.field;
                const split = selectedColumnName.split("_");
                if (split.length > 1) {
                    suffixToUse = split[split.length - 1];
                }
            }

            return suffixToUse;
        };

        let queryActionResult;
        for (let index = 0; index < actions.length; index++) {
            const action = actions[index];
            var exception;

            // First ask the user to enter values for all user parameters, only once for each parameter.
            if (action.userParameters && action.userParameters.length) {
                try {
                    const requestParameterValue = (parameter) => {
                        return new Promise(async (resolve, reject) => {
                            const dialogTitle = parameter.question || `Vul een waarde voor '${parameter.name}' in:`;
                            let dialogContent = kendo.template($("#userNormalInputParameterTemplate").html());

                            switch ((parameter.fieldType || "").toLowerCase()) {
                                case "combobox":
                                case "dropdownlist":
                                case "multiselect":
                                    dialogContent = kendo.template($("#userComboBoxParameterTemplate").html());
                                    break;
                                case "grid":
                                    dialogContent = kendo.template($("#userGridParameterTemplate").html());
                                    break;
                                case "multiline":
                                    dialogContent = kendo.template($("#userMultiLineInputParameterTemplate").html());
                                    break;
                                case "fileupload":
                                    dialogContent = kendo.template($("#userFileUploadParameterTemplate").html());
                                    break;
                            }

                            let dialog;

                            // Function for when the user clicks the OK button in a dialog for a user variable.
                            // It will get the value from the correct kendo component and return it in a Promise.
                            const okButtonAction = (parameter, event) => {
                                let value = dialog.element.find("input").val();

                                switch ((parameter.fieldType || "").toLowerCase()) {
                                    case "datetime":
                                        value = DateTime.fromJSDate(dialog.element.find("input").data("kendoDateTimePicker").value(), { locale: "nl-NL" }).toFormat(Dates.convertMomentFormatToLuxonFormat(parameter.format || "YYYY-MM-DD HH:mm"));
                                        break;
                                    case "date":
                                        value = DateTime.fromJSDate(dialog.element.find("input").data("kendoDatePicker").value(), { locale: "nl-NL" }).toFormat(Dates.convertMomentFormatToLuxonFormat(parameter.format || "YYYY-MM-DD"));
                                        break;
                                    case "time":
                                        value = DateTime.fromJSDate(dialog.element.find("input").data("kendoTimePicker").value(), { locale: "nl-NL" }).toFormat(Dates.convertMomentFormatToLuxonFormat(parameter.format || "HH:mm"));
                                        break;
                                    case "number":
                                        value = dialog.element.find("input").last().data("kendoNumericTextBox").value();
                                        break;
                                    case "combobox":
                                        {
                                            const comboBox = dialog.element.find("select").data("kendoComboBox");
                                            const dataItem = comboBox.dataItem();
                                            // Decode here to prevent duplicate encoding, because the JCL already encodes the value and then javascript will do it again later.
                                            value = decodeURIComponent(dataItem.encryptedId || dataItem.encryptedid || dataItem.encrypted_id || comboBox.value());
                                            break;
                                        }
                                    case "dropdownlist":
                                        {
                                            const comboBox = dialog.element.find("select").data("kendoDropDownList");
                                            const dataItem = comboBox.dataItem();
                                            // Decode here to prevent duplicate encoding, because the JCL already encodes the value and then javascript will do it again later.
                                            value = decodeURIComponent(dataItem.encryptedId || dataItem.encryptedid || dataItem.encrypted_id || comboBox.value());
                                            break;
                                        }
                                    case "multiselect":
                                        {
                                            const multiSelect = dialog.element.find("select").data("kendoMultiSelect");
                                            const dataItems = multiSelect.dataItems();
                                            const encryptedIds = dataItems.filter(i => i.encryptedId || i.encryptedid || i.encrypted_id).map(i => i.encryptedId || i.encryptedid || i.encrypted_id);
                                            // Decode here to prevent duplicate encoding, because the JCL already encodes the value and then javascript will do it again later.
                                            value = decodeURIComponent((encryptedIds.length > 0 ? encryptedIds : multiSelect.value()).join());
                                            break;
                                        }
                                    case "grid":
                                        {
                                            const grid = dialog.element.find("#gridUserParameter").data("kendoGrid");

                                            const dialogSelectedItems = [];
                                            for (const row of grid.select()) {
                                                const selectedItem = grid.dataItem(row);
                                                dialogSelectedItems.push(selectedItem.id || selectedItem.itemId || selectedItem.itemid || selectedItem.item_id);
                                            }

                                            if (dialogSelectedItems.length === 0) {
                                                kendo.alert("Kies a.u.b. eerst een item in het grid.");
                                                return false;
                                            }

                                            value = dialogSelectedItems.join(",");
                                            break;
                                        }
                                    case "multiline":
                                        value = dialog.element.find("textarea").val();
                                        break;
                                    case "fileupload":
                                        {
                                            const fileData = dialog.element.find("input").last().data("fileData");
                                            if (!fileData || !fileData.fileId) {
                                                kendo.alert("U heeft nog geen bestand geselecteerd.");
                                                reject("No file selected");
                                                return;
                                            }
                                            value = fileData.fileId;
                                            break;
                                        }
                                }

                                resolve(value);
                            };

                            const dialogActions = [
                                {
                                    text: "Annuleren",
                                    action: () => reject({ userPressedCancel: true })
                                },
                                {
                                    text: "OK",
                                    primary: true,
                                    action: okButtonAction.bind(this, parameter)
                                }
                            ];

                            let width;
                            let height;
                            let gridHeight = parseInt(parameter.gridHeight) || 600;
                            const extraDialogSize = 200;

                            // Make sure that the dialog and grid fit in the user's window.
                            if (window.innerHeight < gridHeight + extraDialogSize) {
                                gridHeight = window.innerHeight - extraDialogSize;
                            }

                            switch ((parameter.fieldType || "").toLowerCase()) {
                                case "grid":
                                    width = "80%";
                                    height = `${gridHeight + extraDialogSize}px`;
                                    break;
                            }

                            // Initialize the dialog.
                            dialog = $("<div/>").kendoDialog({
                                title: dialogTitle,
                                visible: false,
                                closable: false,
                                modal: true,
                                content: dialogContent,
                                actions: dialogActions,
                                width: width,
                                height: height,
                                open: (event) => {
                                    setTimeout(() => {
                                        event.sender.element.find("input:visible, textarea:visible").focus();
                                        if (parameter.fieldType === "grid") {
                                            $("#gridUserParameter").data("kendoGrid").resize();
                                        }
                                    }, 100);
                                },
                                close: (event) => { event.sender.destroy(); }
                            }).data("kendoDialog");

                            // Trigger the OK button click when the user presses enter in the dialog.
                            dialog.element.keyup((event) => {
                                if (!event.key || event.key.toLowerCase() !== "enter" || $(event.target).closest(".k-grid-header").length || event.target.tagName === "TEXTAREA") {
                                    return;
                                }

                                $(event.currentTarget).next().find(".k-primary, .k-button-solid-primary").trigger("click");
                            });

                            // Build the options object for the kendo component.
                            const options = $.extend({ culture: "nl-NL" }, parameter);

                            if (parameter.value === "NOW()") {
                                options.value = new Date();
                            }

                            if (options.defaultValueQueryId) {
                                try {
                                    let extraData = {};
                                    if (selectedItems && selectedItems.length) {
                                        for (let item of selectedItems) {
                                            // Enter the values of all properties in userParametersWithValues, so that they can be used in actions.
                                            for (let key in item.dataItem) {
                                                if (!item.dataItem.hasOwnProperty(key) || (typeof item.dataItem[key] === "object" && !(item.dataItem[key] || {}).getDate)) {
                                                    continue;
                                                }

                                                extraData[`selected_${key}`] = (item.dataItem[key] || {}).getDate ? DateTime.fromJSDate(item.dataItem[key], { locale: "nl-NL" }).toFormat("yyyy-LL-dd HH:mm:ss") : item.dataItem[key];
                                            }
                                        }
                                    }
                                    const queryResult = await Wiser.api({
                                        method: "POST",
                                        url: `${this.base.settings.wiserApiRoot}items/${encodeURIComponent(mainItemDetails.encryptedId || mainItemDetails.encrypted_id || mainItemDetails.encryptedid)}/action-button/${propertyId}?queryId=${encodeURIComponent(options.defaultValueQueryId)}`,
                                        data: JSON.stringify(extraData),
                                        contentType: "application/json"
                                    });

                                    if (queryResult.otherData.length > 0 && queryResult.otherData[0].value) {
                                        options.value = queryResult.otherData[0].value;
                                        options.defaultValue = queryResult.otherData[0].value;
                                    }

                                } catch (exception) {
                                    console.error(exception);
                                    kendo.alert("Er is iets fout gegaan met het laden van de standaardwaarde voor deze combobox. Neem a.u.b. contact op met ons.");
                                }
                            }

                            if (options.queryId && (parameter.fieldType || "").toLowerCase() !== "grid") {
                                const queryId = options.queryId;
                                options.dataSource = {
                                    transport: {
                                        read: async (kendoOptions) => {
                                            try {
                                                const queryResult = await Wiser.api({
                                                    method: "POST",
                                                    url: `${this.base.settings.wiserApiRoot}items/${encodeURIComponent(mainItemDetails.encryptedId || mainItemDetails.encrypted_id || mainItemDetails.encryptedid)}/action-button/${propertyId}?queryId=${encodeURIComponent(queryId)}`,
                                                    contentType: "application/json"
                                                });

                                                kendoOptions.success(queryResult.otherData);
                                            } catch (exception) {
                                                kendoOptions.error(exception);
                                                kendo.alert("Er is iets fout gegaan met het laden van de gegevens voor deze combobox. Neem a.u.b. contact op met ons.");
                                            }
                                        }
                                    }
                                };
                            } else if (typeof options.dataSource === "string") {
                                switch (options.dataSource.toLowerCase()) {
                                    case "wiserusers":
                                        var userTypesString = "";
                                        if (options.userTypes) {
                                            if (typeof options.userTypes === "string") {
                                                userTypesString = options.userTypes;
                                            } else {
                                                userTypesString = options.userTypes.join();
                                            }
                                        }

                                        options.dataSource = {
                                            transport: {
                                                read: (options) => {
                                                    Wiser.api({
                                                        url: `${this.base.settings.wiserApiRoot}users`,
                                                        dataType: "json",
                                                        method: "GET",
                                                        data: options.data
                                                    }).then((result) => {
                                                        options.success(result);
                                                    }).catch((result) => {
                                                        options.error(result);
                                                    });
                                                }
                                            }
                                        };

                                        options.dataTextField = "title";
                                        options.dataValueField = "id";
                                        break;
                                    default:
                                        kendo.alert(`Onbekende datasource (' ${options.dataSource}') opgegeven bij combobox-veld ('${options.name}'). Neem a.u.b. contact op met ons.`);
                                        break;
                                }
                            }

                            // Delete properties that are not meant for Kendo, they cause problems for Kendo.
                            delete options.name;
                            delete options.question;
                            delete options.fieldType;
                            if ((parameter.fieldType || "").toLowerCase() !== "grid") {
                                delete options.queryId;
                            }
                            delete options.format;

                            // Initialize the correct kendo component.
                            switch ((parameter.fieldType || "").toLowerCase()) {
                                case "datetime":
                                    await require("@progress/kendo-ui/js/kendo.datetimepicker.js");
                                    const dateTimePicker = dialog.element.find("input").addClass("dateTimeField").kendoDateTimePicker(options).data("kendoDateTimePicker");
                                    setTimeout(() => { dateTimePicker.open(); }, 100);
                                    break;
                                case "date":
                                    await require("@progress/kendo-ui/js/kendo.datepicker.js");
                                    const datePicker = dialog.element.find("input").addClass("dateTimeField").kendoDatePicker(options).data("kendoDatePicker");
                                    setTimeout(() => { datePicker.open(); }, 100);
                                    break;
                                case "time":
                                    await require("@progress/kendo-ui/js/kendo.timepicker.js");
                                    const timePicker = dialog.element.find("input").addClass("dateTimeField").kendoTimePicker(options).data("kendoTimePicker");
                                    setTimeout(() => { timePicker.open(); }, 100);
                                    break;
                                case "number":
                                    await require("@progress/kendo-ui/js/kendo.numerictextbox.js");
                                    dialog.element.find("input").addClass("textField").kendoNumericTextBox(options);
                                    break;
                                case "combobox":
                                    await require("@progress/kendo-ui/js/kendo.combobox.js");
                                    options.autoWidth = true;
                                    dialog.element.find("select").kendoComboBox(options);
                                    break;
                                case "dropdownlist":
                                    await require("@progress/kendo-ui/js/kendo.dropdownlist.js");
                                    options.autoWidth = true;
                                    dialog.element.find("select").kendoDropDownList(options);
                                    break;
                                case "multiselect":
                                    await require("@progress/kendo-ui/js/kendo.multiselect.js");
                                    options.autoWidth = true;
                                    dialog.element.find("select").kendoMultiSelect(options);
                                    break;
                                case "grid":
                                    if (typeof options.checkboxes === "undefined") {
                                        options.checkboxes = true;
                                    }
                                    // We have an array with selected items, which means this is an action button in a grid and we want to execute this action once for every selected item.
                                    for (let item of selectedItems) {
                                        // Remove any properties from the previous item so that we don't get confusing conflicts.
                                        for (let key in userParametersWithValues) {
                                            if (item.dataItem.hasOwnProperty(key) && key.indexOf("selected_") === 0) {
                                                delete userParametersWithValues[key];
                                            }
                                        }

                                        // If there is a certain column selected, use only values with the same suffix, that makes it possible to execute action buttons on specific columns instead of an entire row.
                                        const suffixToUse = getSuffixFromSelectedColumn(item);

                                        // Enter the values of all properties in userParametersWithValues, so that they can be used in actions.
                                        for (let key in item.dataItem) {
                                            if (!item.dataItem.hasOwnProperty(key) || (typeof item.dataItem[key] === "object" && !(item.dataItem[key] || {}).getDate)) {
                                                continue;
                                            }

                                            // If we have a suffix from a selected column, skip properties with a different suffix.
                                            if (suffixToUse && !key.endsWith(`_${suffixToUse}`)) {
                                                continue;
                                            }

                                            let newKey = key;
                                            if (suffixToUse) {
                                                newKey = newKey.substr(0, newKey.length - suffixToUse.length - 1);
                                            }

                                            userParametersWithValues[`selected_${newKey}`] = (item.dataItem[key] || {}).getDate ? DateTime.fromJSDate(item.dataItem[key], { locale: "nl-NL" }).toFormat("yyyy-LL-dd HH:mm:ss") : item.dataItem[key];
                                        }
                                    }

                                    const gridOptions = $.extend({ toolbar: { hideFullScreenButton: true } }, options);
                                    await this.base.grids.initializeItemsGrid(gridOptions, dialog.element.find("#gridUserParameter"), null, mainItemDetails.encryptedId || mainItemDetails.encrypted_id || mainItemDetails.encryptedid, `${gridHeight}px`, 0, userParametersWithValues);
                                    break;
                                case "fileupload":
                                    {
                                        await require("@progress/kendo-ui/js/kendo.upload.js");
                                        let itemId;
                                        let itemLinkId;
                                        if (selectedItems) {
                                            if (selectedItems.length > 1) {
                                                kendo.alert("Het uploaden van bestanden kan maar met 1 item tegelijk.");
                                                break;
                                            }

                                            const selectedItem = selectedItems[0].dataItem;
                                            const suffixToUse = getSuffixFromSelectedColumn(selectedItem);
                                            itemId = selectedItem[`encryptedItemId_${suffixToUse}`] || selectedItem[`encryptedId_${suffixToUse}`] || selectedItem[`itemId_${suffixToUse}`] || selectedItem[`id_${suffixToUse}`] || selectedItem.encryptedItemId || selectedItem.encryptedId || selectedItem.itemId || selectedItem.id || this.base.settings.zeroEncrypted;
                                            itemLinkId = selectedItem[`linkId_${suffixToUse}`] || selectedItem.linkId|| 0;
                                        } else {
                                            itemId = mainItemDetails.encryptedId || mainItemDetails.encrypted_id || mainItemDetails.encryptedid || this.base.settings.zeroEncrypted;
                                            itemLinkId = mainItemDetails.linkId || mainItemDetails.link_id || 0;
                                        }

                                        const uploadOptions = $.extend(true, {
                                            async: {
                                                saveUrl: `${this.base.settings.wiserApiRoot}items/${encodeURIComponent(itemId)}/upload?propertyName=${encodeURIComponent(options.propertyName)}&itemLinkId=${itemLinkId}`,
                                                removeUrl: "remove",
                                                withCredentials: false
                                            },
                                            upload: (e) => {
                                                let xhr = e.XMLHttpRequest;
                                                if (xhr) {
                                                    xhr.addEventListener("readystatechange", (e) => {
                                                        if (xhr.readyState === 1 /* OPENED */) {
                                                            xhr.setRequestHeader("authorization", `Bearer ${localStorage.getItem("accessToken")}`);
                                                        }
                                                    });
                                                }
                                            },
                                            success: (uploadSuccessEvent) => {
                                                uploadSuccessEvent.sender.element.data("fileData", uploadSuccessEvent.response[0]);
                                            }
                                        }, options);

                                        dialog.element.find("input").kendoUpload(uploadOptions);
                                        break;
                                    }
                                default:
                                    if (options.defaultValue) {
                                        dialog.element.find("input").val(options.defaultValue);
                                    }
                                    break;
                            }

                            dialog.open();
                        });
                    };

                    for (const parameter of action.userParameters) {
                        if (typeof userParametersWithValues[parameter.name] !== "undefined") {
                            // Only ask for each parameter once.
                            continue;
                        }

                        const value = await requestParameterValue(parameter);
                        userParametersWithValues[parameter.name] = value;
                    }
                } catch (exception) {
                    if (exception && !exception.userPressedCancel) {
                        console.error(exception);
                    }
                    // A failed promise will also result in an exception, including if the user presses cancel in a dialog.
                    // If an actual exception occured, or if the user didn't enter one of the values, we want to skip doing any other action.
                    return false;
                }
            }

            // Then execute the actions, using the entered user parameters if there are any.
            try {
                const executeQuery = () => {
                    return Wiser.api({
                        method: "POST",
                        url: `${this.base.settings.wiserApiRoot}items/${encodeURIComponent(mainItemDetails.encryptedId || mainItemDetails.encrypted_id || mainItemDetails.encryptedid)}/action-button/${propertyId}?queryId=${encodeURIComponent(action.queryId || this.base.settings.zeroEncrypted)}&itemLinkId=${encodeURIComponent(mainItemDetails.linkId || mainItemDetails.link_id || 0)}`,
                        data: JSON.stringify(userParametersWithValues),
                        contentType: "application/json"
                    });
                };

                const combineValuesFromAllSelectedItemsAndAddToUserParameters = async () => {
                    const temporaryValues = {};

                    // First build an object with an array for every property.
                    for (let item of selectedItems) {
                        // If there is a certain column selected, use only values with the same suffix, that makes it possible to execute action buttons on specific columns instead of an entire row.
                        const suffixToUse = getSuffixFromSelectedColumn(item);

                        // Enter the values of all properties in userParametersWithValues, so that they can be used in actions.
                        for (let key in item.dataItem) {
                            if (!item.dataItem.hasOwnProperty(key) || (typeof item.dataItem[key] === "object" && !(item.dataItem[key] || {}).getDate)) {
                                continue;
                            }

                            // If we have a suffix from a selected column, skip properties with a different suffix.
                            if (suffixToUse && !key.endsWith(`_${suffixToUse}`)) {
                                continue;
                            }

                            let selectedKey = `selected_${key}`;

                            if (suffixToUse) {
                                selectedKey = selectedKey.substr(0, selectedKey.length - suffixToUse.length - 1);
                            }

                            if (!temporaryValues[selectedKey]) {
                                temporaryValues[selectedKey] = [];
                            }

                            // Don't add empty or duplicate values.
                            if (item.dataItem[key] === "" || temporaryValues[selectedKey].indexOf(item.dataItem[key]) > -1) {
                                continue;
                            }

                            temporaryValues[selectedKey].push((item.dataItem[key] || {}).getDate ? DateTime.fromJSDate(item.dataItem[key], { locale: "nl-NL" }).toFormat("yyyy-LL-dd HH:mm:ss") : item.dataItem[key]);
                        }
                    }

                    // Then combine all values in each array to create strings that can be used in the query.
                    for (let key in temporaryValues) {
                        if (!temporaryValues.hasOwnProperty(key)) {
                            continue;
                        }

                        userParametersWithValues[key] = temporaryValues[key].join();
                    }
                };

                const openUrl = (contentUrl) => {
                    return new Promise((resolve, reject) => {
                        switch ((action.openIn || "window").toLowerCase()) {
                            case "kendowindow":
                                $("<div/>").kendoWindow({
                                    width: action.windowWidth || "500px",
                                    height: action.windowHeight || "500px",
                                    title: "",
                                    modal: true,
                                    actions: ["Maximize", "Close"],
                                    content: contentUrl,
                                    iframe: true,
                                    close: (closeEvent) => {
                                        resolve(closeEvent);
                                    }
                                }).data("kendoWindow").center().open();
                                break;
                            default:
                                window.open(contentUrl);
                                resolve();
                                break;
                        }
                    });
                };
                
                switch (action.type) {
                    // Opens a new tab/window in the browser of the user with the given URL. A tab will be opened for every selected item.
                    case "openUrl": {
                        if (!action.url) {
                            kendo.alert(`Er werd geprobeerd om actie type '${action.type}' uit te voeren, echter is er geen URL ingevuld. Neem a.u.b. contact op met ons.`);
                            break;
                        }

                        if (action.dataFromQuery) {
                            const executeQueryResult = await executeQuery();
                            if (!executeQueryResult.success) {
                                kendo.alert(executeQueryResult.errorMessage || "Er is iets fout gegaan met het uitvoeren van de actie (executeQuery), probeer het a.u.b. nogmaals of neem contact op met ons.");
                                return false;
                            }
                        }

                        if (userParametersWithValues) {
                            for (const parameter in userParametersWithValues) {
                                if (!userParametersWithValues.hasOwnProperty(parameter)) {
                                    continue;
                                }

                                const value = userParametersWithValues[parameter];

                                const replace = new RegExp(`{${parameter}}`, "gi");
                                action.url = action.url.replace(replace, encodeURIComponent(value));
                            }
                        }

                        let finalUrl = Wiser.doWiserItemReplacements(action.url, mainItemDetails, true);
                        if (!selectedItems || !selectedItems.length) {
                            await openUrl(finalUrl);
                            break;
                        }

                        for (let selectedItem of selectedItems) {
                            // If there is a certain column selected, use only values with the same suffix, that makes it possible to execute action buttons on specific columns instead of an entire row.
                            const suffixToUse = getSuffixFromSelectedColumn(selectedItem);

                            let urlToOpen = finalUrl;
                            // Enter the values of all properties in userParametersWithValues, so that they can be used in actions.
                            for (let key in selectedItem.dataItem) {
                                if (!selectedItem.dataItem.hasOwnProperty(key) || (typeof selectedItem.dataItem[key] === "object" && !(selectedItem.dataItem[key] || {}).getDate)) {
                                    continue;
                                }

                                // If we have a suffix from a selected column, skip properties with a different suffix.
                                if (suffixToUse && !key.endsWith(`_${suffixToUse}`)) {
                                    continue;
                                }

                                let newKey = key;
                                if (suffixToUse) {
                                    newKey = newKey.substr(0, newKey.length - suffixToUse.length - 1);
                                }

                                const replace = new RegExp(`{selected_${newKey}}`, "gi");
                                urlToOpen = urlToOpen.replace(replace, encodeURIComponent((selectedItem.dataItem[key] || {}).getDate ? DateTime.fromJSDate(selectedItem.dataItem[key], { locale: "nl-NL" }).toFormat("yyyy-LL-dd HH:mm:ss") : selectedItem.dataItem[key]));
                            }

                            await openUrl(urlToOpen);
                        }
                        break;
                    }

                    // Opens a new tab/window in the browser of the user with the given URL. If multiple
                    case "openUrlOnce": {
                        if (!action.url) {
                            kendo.alert(`Er werd geprobeerd om actie type '${action.type}' uit te voeren, echter is er geen URL ingevuld. Neem a.u.b. contact op met ons.`);
                            break;
                        }

                        if (selectedItems && selectedItems.length > 0) {
                            // We have an array with selected items, which means this is an action button in a grid and we want to execute this action by using values from all selected items.
                            await combineValuesFromAllSelectedItemsAndAddToUserParameters();
                        }

                        if (action.dataFromQuery) {
                            const executeQueryResult = await executeQuery();
                            if (!executeQueryResult.success) {
                                kendo.alert(executeQueryResult.errorMessage || "Er is iets fout gegaan met het uitvoeren van de actie (executeQuery), probeer het a.u.b. nogmaals of neem contact op met ons.");
                                return false;
                            }
                        }

                        if (userParametersWithValues) {
                            for (const parameter in userParametersWithValues) {
                                if (!userParametersWithValues.hasOwnProperty(parameter)) {
                                    continue;
                                }

                                const value = userParametersWithValues[parameter];

                                const replace = new RegExp(`{${parameter}}`, "gi");
                                action.url = action.url.replace(replace, encodeURIComponent(value));
                            }
                        }

                        let finalUrl = Wiser.doWiserItemReplacements(action.url, mainItemDetails, true);
                        await openUrl(finalUrl);

                        break;
                    }

                    // Executes a query that is saved in the action_query column of wiser_entityproperty. This query will be executed separately for every selected item.
                    case "executeQuery": {
                        if (!selectedItems || !selectedItems.length) {
                            // No selected items, which means that this is an action from a stand-alone action button and we only need to execute the action once.
                            queryActionResult = await executeQuery();
                            if (!queryActionResult.success) {
                                kendo.alert(queryActionResult.errorMessage || "Er is iets fout gegaan met het uitvoeren van de actie (executeQuery), probeer het a.u.b. nogmaals of neem contact op met ons.");
                                return false;
                            }
                        } else {
                            // We have an array with selected items, which means this is an action button in a grid and we want to execute this action once for every selected item.
                            for (let item of selectedItems) {
                                // If there is a certain column selected, use only values with the same suffix, that makes it possible to execute action buttons on specific columns instead of an entire row.
                                const suffixToUse = getSuffixFromSelectedColumn(item);

                                // Remove any properties from the previous item so that we don't get confusing conflicts.
                                for (let key in userParametersWithValues) {
                                    if (item.dataItem.hasOwnProperty(key) && key.indexOf("selected_") === 0) {
                                        delete userParametersWithValues[key];
                                    }
                                }

                                // Enter the values of all properties in userParametersWithValues, so that they can be used in actions.
                                for (let key in item.dataItem) {
                                    if (!item.dataItem.hasOwnProperty(key) || (typeof item.dataItem[key] === "object" && !(item.dataItem[key] || {}).getDate)) {
                                        continue;
                                    }

                                    // If we have a suffix from a selected column, skip properties with a different suffix.
                                    if (suffixToUse && !key.endsWith(`_${suffixToUse}`)) {
                                        continue;
                                    }

                                    let newKey = key;
                                    if (suffixToUse) {
                                        newKey = newKey.substr(0, newKey.length - suffixToUse.length - 1);
                                    }

                                    userParametersWithValues[`selected_${newKey}`] = (item.dataItem[key] || {}).getDate ? DateTime.fromJSDate(item.dataItem[key], { locale: "nl-NL" }).toFormat("yyyy-LL-dd HH:mm:ss") : item.dataItem[key];
                                }

                                queryActionResult = await executeQuery();
                                if (!queryActionResult.success) {
                                    kendo.alert(queryActionResult.errorMessage || "Er is iets fout gegaan met het uitvoeren van de actie (executeQuery), probeer het a.u.b. nogmaals of neem contact op met ons.");
                                    return false;
                                }
                            }
                        }

                        break;
                    }

                    // Executes a query that is saved in the action_query column of wiser_entityproperty. This query will only be executed once for all selected items.
                    case "executeQueryOnce": {
                        if (!selectedItems || !selectedItems.length) {
                            // No selected items, which means that this is an action from a stand-alone action button and we only need to execute the action once.
                            queryActionResult = await executeQuery();
                            if (!queryActionResult.success) {
                                kendo.alert(queryActionResult.errorMessage || "Er is iets fout gegaan met het uitvoeren van de actie (executeQuery), probeer het a.u.b. nogmaals of neem contact op met ons.");
                                return false;
                            }
                        } else {
                            // We have an array with selected items, which means this is an action button in a grid and we want to execute this action by using values from all selected items.
                            await combineValuesFromAllSelectedItemsAndAddToUserParameters();

                            // Finally, execute the query.
                            queryActionResult = await executeQuery();
                            if (!queryActionResult.success) {
                                kendo.alert(queryActionResult.errorMessage || "Er is iets fout gegaan met het uitvoeren van de actie (executeQuery), probeer het a.u.b. nogmaals of neem contact op met ons.");
                                return false;
                            }
                        }

                        break;
                    }

                    // Opens an item in a kendoWindow.
                    case "openWindow": {
                        let windowItemId = action.itemId || "{itemId}";
                        let windowLinkId = action.linkId || "{linkId}";
                        let windowEntityType = action.entityType || null;
                        let windowLinkType = action.linkType || action.linkTypeNumber || "{linkType}";

                        // The queryActionResult are from a previously executed query. This way you can combine the actions executeQuery(Once) and openWindow to open a newly created or updated item.
                        if (queryActionResult) {
                            windowItemId = windowItemId.replace(/{itemId}/gi, queryActionResult.itemId || 0);
                            windowLinkId = windowLinkId.replace(/{linkId}/gi, queryActionResult.linkId || 0);
                            windowLinkType = windowLinkType.replace(/{linkType}/gi, queryActionResult.linkType || queryActionResult.linkTypeNumber || 0);
                        }
                        windowItemId = Wiser.doWiserItemReplacements(windowItemId, mainItemDetails);

                        if (!windowItemId) {
                            // We can't open a window with an item if we have no item ID, so show an error.
                            kendo.alert(`Er werd geprobeerd om actie type '${action.type}' uit te voeren, echter is er geen item ID ingevuld. Neem a.u.b. contact op met ons.`);
                            break;
                        }

                        const windowItemDetails = (await this.base.getItemDetails(windowItemId, windowEntityType));

                        const itemId = windowItemDetails.id || windowItemDetails.itemId || windowItemDetails.itemid || windowItemDetails.item_id;
                        const encryptedId = windowItemDetails.encryptedId || windowItemDetails.encrypted_id || windowItemDetails.encryptedid;
                        this.base.windows.loadItemInWindow(false, itemId, encryptedId, windowItemDetails.entityType, windowItemDetails.title, true, null, { hideTitleColumn: false }, windowLinkId, null, null, windowLinkType);

                        break;
                    }

                    // Generates a text file based on query results.
                    case "generateTextFile": {
                        if (!action.queryId) {
                            kendo.alert(`Er werd geprobeerd om actie type '${action.type}' uit te voeren, echter zijn niet alle instellingen daarvoor ingevuld. Neem a.u.b. contact op met ons.`);
                            return false;
                        }

                        queryActionResult = await executeQuery();

                        if (!queryActionResult.success) {
                            kendo.alert(queryActionResult.errorMessage || `Er werd geprobeerd om actie type '${action.type}' uit te voeren, echter is er iets fout gegaan bij het uitvoeren van de query. Neem a.u.b. contact op met ons.`);
                            return false;
                        } else if (queryActionResult.otherData.length !== 1 || !queryActionResult.otherData[0].filename || !queryActionResult.otherData[0].result) {
                            kendo.alert(`Er werd geprobeerd om actie type '${action.type}' uit te voeren, echter voldoet het resultaat niet aan de eisen. Neem a.u.b. contact op met ons.`);
                            return false;
                        }

                        //Download the result to a file with the given filename.
                        const blob = new Blob([queryActionResult.otherData[0].result], { type: 'text/csv' });
                        const fileUrl = window.URL.createObjectURL(blob);
                        const anchor = document.createElement("a");
                        anchor.href = fileUrl;
                        anchor.download = queryActionResult.otherData[0].filename;
                        document.body.appendChild(anchor);
                        anchor.click();
                        document.body.removeChild(anchor);
                        window.URL.revokeObjectURL(fileUrl);

                        break;
                    }

                    // Generates a (HTML) file via get_items.jcl.
                    case "generateFile": {
                        if ((!action.dataSelectorId && !action.queryId) || (!action.contentItemId && !userParametersWithValues.contentItemId) || !action.contentPropertyName) {
                            kendo.alert(`Er werd geprobeerd om actie type '${action.type}' uit te voeren, echter zijn niet alle instellingen daarvoor ingevuld. Neem a.u.b. contact op met ons.`);
                            break;
                        }

                        const templateDetails = await this.base.getItemDetails(userParametersWithValues.contentItemId || action.contentItemId);

                        if (!templateDetails) {
                            kendo.alert(`Er werd geprobeerd om actie type '${action.type}' uit te voeren, echter kon de template voor het bestand niet gevonden worden. Neem a.u.b. contact op met ons.`);
                            break;
                        }

                        let url = "";
                        let allUrls = [];
                        if (action.queryId) {
                            url = `${this.base.settings.getItemsUrl}/html?queryId=${encodeURIComponent(action.queryId)}`;
                        }
                        else {
                            url = `${this.base.settings.getItemsUrl}/html?encryptedDataSelectorId=${encodeURIComponent(action.dataSelectorId)}`;
                        }

                        if (action.contentItemId && !userParametersWithValues.contentItemId) {
                            url += `&contentItemId=${encodeURIComponent(action.contentItemId)}`;
                        }
                        if (action.contentPropertyName) {
                            url += `&contentPropertyName=${encodeURIComponent(action.contentPropertyName)}`;
                        }

                        if (typeof action.itemId !== "undefined" && action.itemId !== null && action.itemId !== "") {
                            let itemIdForUrl = action.itemId;
                            // The queryActionResult are from a previously executed query. This way you can combine the actions executeQuery(Once) and openWindow to open a newly created or updated item.
                            if (queryActionResult) {
                                itemIdForUrl = itemIdForUrl.replace(/{itemId}/gi, queryActionResult.itemId || 0);
                            }
                            itemIdForUrl = Wiser.doWiserItemReplacements(itemIdForUrl, mainItemDetails);
                            url += `&itemId=${itemIdForUrl}`;
                        } else {
                            url += `&itemId=${encodeURIComponent(mainItemDetails.encryptedId)}`;
                        }

                        if (userParametersWithValues) {
                            for (const parameter in userParametersWithValues) {
                                if (!userParametersWithValues.hasOwnProperty(parameter)) {
                                    continue;
                                }

                                const value = userParametersWithValues[parameter];

                                url += `&${encodeURIComponent(parameter)}=${encodeURIComponent(value)}`;
                            }
                        }

                        // Make string of selected id's and link-id's for adding to url
                        if (selectedItems.length > 0) {
                            if (!action.createSeparatePdfForEachSelectedItem) {
                                let ids = [];
                                let linkIds = [];
                                for (let item of selectedItems) {
                                    ids.push(item.dataItem["id"]);
                                    linkIds.push(item.dataItem["linkId"] || item.dataItem["link_id"]);
                                }

                                // The camel case parameters are for backwards compatibility, because we used snake case in the past for some things like this.
                                url += `&selectedId=${ids.join(",")}&selected_id=${ids.join(",")}`;
                                url += `&selectedLinkId=${linkIds.join(",")}&selected_link_id=${linkIds.join(",")}`;
                                allUrls.push(url);
                            } else {
                                for (let item of selectedItems) {
                                    // The camel case parameters are for backwards compatibility, because we used snake case in the past for some things like this.
                                    allUrls.push(`${url}&selectedId=${item.dataItem["id"]}&selected_id=${item.dataItem["id"]}&selectedLinkId=${item.dataItem["linkId"] || item.dataItem["link_id"]}&selected_link_id=${item.dataItem["linkId"] || item.dataItem["link_id"]}`);
                                }
                            }
                        }
                        else {
                            allUrls.push(url);
                        }

                        let emailData = null;
                        const mainItemId = mainItemDetails.encryptedId || mainItemDetails.encrypted_id || mainItemDetails.encryptedid;
                        let itemId = mainItemId;
                        let linkId = mainItemDetails.linkId || mainItemDetails.link_id || 0;
                        const extraParameters = {};
                        if (selectedItems.length > 0) {
                            const selectedId = selectedItems[0].dataItem.itemId || selectedItems[0].dataItem.item_id || selectedItems[0].dataItem.id;
                            const selectedLinkId = selectedItems[0].dataItem.linkId || selectedItems[0].dataItem.link_id;
                            itemId = selectedItems[0].dataItem.encryptedId || selectedItems[0].dataItem.encrypted_id || selectedItems[0].dataItem.encryptedid || mainItemId;
                            linkId = selectedLinkId || 0;

                            if (selectedId) {
                                extraParameters.selectedId = selectedId;
                            }
                            if (selectedId) {
                                extraParameters.selectedLinkId = selectedLinkId;
                            }
                        }
                        if (action.emailDataQueryId) {
                            emailData = await Wiser.api({
                                method: "POST",
                                url: `${this.base.settings.wiserApiRoot}items/${encodeURIComponent(itemId)}/action-button/${propertyId}?queryId=${encodeURIComponent(action.emailDataQueryId)}&itemLinkId=${encodeURIComponent(linkId)}`,
                                data: JSON.stringify($.extend(extraParameters, userParametersWithValues)),
                                contentType: "application/json"
                            });

                            if (emailData && emailData.otherData) {
                                emailData = emailData.otherData[0];
                            }
                        }

                        await this.initializeGenerateFileWindow(allUrls, templateDetails, emailData, action, element, userParametersWithValues, itemId, linkId, propertyId, selectedItems);

                        break;
                    }

                    // Updates the link of one or more items. In other words; move the item(s) to a new parent.
                    case "updateItemLink": {
                        // The function that actually updates the link in the database.
                        const updateItemLink = async function () {
                            if (!userParametersWithValues || (!userParametersWithValues.selected_linkId && !userParametersWithValues.selected_link_id)) {
                                kendo.alert(`Geen link ID gevonden voor actie '${action.type}'. Neem a.u.b. contact op met ons.`);
                                return false;
                            }

                            let destinationId = userParametersWithValues.newItemId;
                            if (!destinationId) {
                                destinationId = await kendo.prompt("Vul het ID in van het nieuwe item:");
                                userParametersWithValues.newItemId = destinationId;
                            }

                            await this.base.updateItemLink(userParametersWithValues.selectedLinkId || userParametersWithValues.selected_linkId || userParametersWithValues.selected_link_id, destinationId);
                            return true;
                        }.bind(this);

                        if (!selectedItems || !selectedItems.length) {
                            // No selected items, which means that this is an action from a stand-alone action button and we only need to execute the action once.
                            const success = await updateItemLink();
                            if (!success) {
                                return false;
                            }
                        } else {
                            // We have an array with selected items, which means this is an action button in a grid and we want to execute this action once for every selected item.
                            for (let item of selectedItems) {
                                // If there is a certain column selected, use only values with the same suffix, that makes it possible to execute action buttons on specific columns instead of an entire row.
                                const suffixToUse = getSuffixFromSelectedColumn(item);

                                // Remove any properties from the previous item so that we don't get confusing conflicts.
                                for (let key in userParametersWithValues) {
                                    if (item.dataItem.hasOwnProperty(key) && key.indexOf("selected_") === 0) {
                                        delete userParametersWithValues[key];
                                    }
                                }

                                // Enter the values of all properties in userParametersWithValues, so that they can be used in actions.
                                for (let key in item.dataItem) {
                                    if (!item.dataItem.hasOwnProperty(key) || typeof (typeof item.dataItem[key] === "object" && !(item.dataItem[key] || {}).getDate)) {
                                        continue;
                                    }

                                    // If we have a suffix from a selected column, skip properties with a different suffix.
                                    if (suffixToUse && !key.endsWith(`_${suffixToUse}`)) {
                                        continue;
                                    }

                                    let newKey = key;
                                    if (suffixToUse) {
                                        newKey = newKey.substr(0, newKey.length - suffixToUse.length - 1);
                                    }

                                    userParametersWithValues[`selected_${newKey}`] = (item.dataItem[key] || {}).getDate ? DateTime.fromJSDate(item.dataItem[key], { locale: "nl-NL" }).toFormat("yyyy-LL-dd HH:mm:ss") : item.dataItem[key];
                                }

                                const success = await updateItemLink();
                                if (!success) {
                                    return false;
                                }
                            }
                        }

                        break;
                    }

                    // Refreshes the currently opened item.
                    case "refreshCurrentItem": {
                        const kendoWindow = element.closest(".popup-container");

                        if (kendoWindow.length === 0) {
                            // The opened item is in the main window.
                            const previouslySelectedTab = this.base.mainTabStrip.select().index();
                            await this.base.loadItem(this.base.settings.initialItemId ? this.base.settings.initialItemId : this.base.selectedItem.id, previouslySelectedTab);
                        } else {
                            // The opened item is in a window.
                            const previouslySelectedTab = kendoWindow.find(".tabStripPopup").data("kendoTabStrip").select().index();
                            const reloadFunction = kendoWindow.data("reloadFunction");
                            await reloadFunction(previouslySelectedTab);
                        }

                        break;
                    }

                    // Refreshes the currently opened item.
                    case "refreshAllOpenedWindows": {
                        const ignoreWindowIds = ["itemWindow_template", "imagesUploaderWindow", "filesUploaderWindow", "templatesUploaderWindow"];
                        for (let windowElement of $(".popup-container")) {
                            const kendoWindow = $(windowElement);
                            if (ignoreWindowIds.includes(kendoWindow.attr("id"))) {
                                continue;
                            }

                            // Extra check to make sure the window has a tab strip.
                            const tabStrip = kendoWindow.find(".tabStripPopup").data("kendoTabStrip");
                            if (!tabStrip) {
                                continue;
                            }

                            const previouslySelectedTab = tabStrip.select().index();
                            const reloadFunction = kendoWindow.data("reloadFunction");
                            if (!reloadFunction) {
                                console.log("Trying to refresh window, but it has no reloadFunction", kendoWindow);
                                continue;
                            }

                            await reloadFunction(previouslySelectedTab);
                        }

                        this.base.onMainRefreshButtonClick({ preventDefault: () => { } });

                        break;
                    }

                    // Calls an API.
                    case "apiCall": {
                        try {
                            if (selectedItems && selectedItems.length > 0) {
                                // We have an array with selected items, which means this is an action button in a grid and we want to execute this action by using values from all selected items.
                                await combineValuesFromAllSelectedItemsAndAddToUserParameters();
                            }

                            const apiCallResult = await Wiser.doApiCall(this.base.settings, action.apiConnectionId, mainItemDetails, userParametersWithValues);
                        } catch (apiCallException) {
                            if (typeof apiCallException === "string") {
                                kendo.alert(apiCallException);
                            } else {
                                throw apiCallException;
                            }
                        }
                        break;
                    }

                    // Send a notification to a Wiser user via Pusher.
                    case "pusher": {
                        const userId = action.pusherUserId || userParametersWithValues.pusherUserId;
                        if (!userId) {
                            kendo.alert("Er is geen ontvanger ingesteld voor pusher. Neem a.u.b. contact op met ons.");
                            return false;
                        }

                        let eventData = action.eventData;
                        if (typeof eventData === "object") {
                            eventData = JSON.stringify(eventData);
                        }

                        // Send a pusher to notify the receiving user.
                        await Wiser.api({
                            method: "POST",
                            url: `${this.base.settings.wiserApiRoot}pusher/message`,
                            contentType: "application/json",
                            data: JSON.stringify({
                                userId: userId,
                                channel: action.channel || "agendering",
                                eventData: eventData || ""
                            })
                        });

                        break;
                    }
                    
                    case "actionConfirmDialog": {
                        try {
                            await Wiser.showConfirmDialog(action.text || "Wilt u doorgaan met de actie?", action.title || "Doorgaan", "Annuleren", "Doorgaan");
                            break;
                        } catch {
                            return false;
                        }
                    }

                    // Custom actions with custom javascript.
                    case "custom": {
                        // Custom actions use custom javascript that is already executed while loading the field, so no need to do anything here.
                        break;
                    }

                    // Unknown action, show an error.
                    default: {
                        kendo.alert(`Onbekend actie-type '${action.type}'. Neem a.u.b. contact op met ons.`);
                        break;
                    }
                }
            } catch (exception) {
                console.error(exception);
                let error = exception;
                if (exception.responseText) {
                    error = exception.responseText;
                } else if (exception.statusText) {
                    error = exception.statusText;
                }
                kendo.alert(`Er is iets fout gegaan met het uitvoeren van actie type '${action.type}'. Probeer het a.u.b. nogmaals of neem contact op met ons.<br><br>De fout was:<br><pre>${kendo.htmlEncode(error)}</pre>`);
                // Exit for loop.
                return false;
            }
        }

        return true;
    }

    /**
     * Initialize HTML editor for preview of file generator for action buttons.
     * @param {Array} urls The URLs of the data selector.
     * @param {any} templateDetails The details of the template.
     * @param {any} emailData An object that contains the default subject, receiver and body for the e-mail to send.
     * @param {any} action An object that contains the setting for the current action.
     * @param {any} element The action button or grid.
     * @param {any} userParametersWithValues The user parameters of the action button.
     * @param {any} itemId The ID of the opened/selected item.
     * @param {any} linkId The item link ID of the opened/selected item.
     * @param {any} propertyId The ID of the property / field that contains the action button.
     * @param {any} selectedItems Array of all selected items in the grid.
     */
    initializeGenerateFileWindow(urls, templateDetails, emailData = {}, action = {}, element = null, userParametersWithValues = {}, itemId = null, linkId = null, propertyId = 0, selectedItems = []) {
        return new Promise(async (resolve, reject) => {
            emailData = emailData || {};

            if (!action || !action.contentPropertyName) {
                kendo.alert("Deze functionaliteit is nog niet volledig ingesteld ('contentPropertyName' is leeg). Neem a.u.b. contact op met ons.");
                resolve();
                return;
            }

            const process = `initializeGenerateFileWindow_${Date.now()}`;
            if (element && element.siblings(".grid-loader").length) {
                element.siblings(".grid-loader").addClass("loading");
            } else {
                window.processing.addProcess(process);
            }

            try {
                const loadContentInPreviewFrame = (iframe, content, printAfterLoad) => {
                    iframe.document.open();
                    iframe.onload = () => {
                        if (printAfterLoad) {
                            iframe.print();
                        }
                    };
                    iframe.document.write(content);
                    iframe.document.close();
                };

                // Initialize the kendo Window.
                let previewWindow = $("#previewFrame").data("kendoWindow");
                let isNewWindow = false;
                if (!previewWindow) {
                    isNewWindow = true;
                    previewWindow = $("#previewFrame").kendoWindow({
                        width: "90%",
                        height: "90%",
                        actions: ["Close"],
                        title: "Preview"
                    }).data("kendoWindow");
                }

                previewWindow.one("close", (event) => resolve());

                const container = previewWindow.element.find("div.k-content-frame");
                console.log("container", container);

                // Save the email data in the container, otherwise the email popup will show out dated data after opening it for a second time.
                container.data("emailData", emailData);
                container.data("action", action);
                container.data("templateDetails", templateDetails);

                // Initialize the tab strip.
                const tabStripElement = container.find("#previewTabStrip");
                let tabStrip = tabStripElement.data("kendoTabStrip");
                if (tabStrip) {
                    tabStrip.destroy();
                    tabStripElement.html("");
                }

                tabStrip = tabStripElement.kendoTabStrip({
                    animation: {
                        open: {
                            effects: "fadeIn"
                        }
                    }
                }).data("kendoTabStrip");

                for (let i = 0; i < urls.length; i++) {
                    const url = urls[i];

                    // Execute the data selector and get the HTML result.
                    const dataSelectorResult = await Wiser.api({
                        method: "POST",
                        contentType: "application/json",
                        url: url
                    });

                    // Add new tab.
                    tabStrip.append({
                        text: `Document ${i + 1}`,
                        content: `<iframe id="previewBlock${i}" class="iframe"></iframe><textarea id="previewEditor${i}" class="editor"></textarea>`
                    });

                    // Load dataSelectorResult into iframe. This is an iframe so that the CSS from the template cannot affect Wiser in any way.
                    let iframe = container.find(`iframe#previewBlock${i}`)[0];
                    iframe = iframe.contentWindow || (iframe.contentDocument.document || iframe.contentDocument);

                    loadContentInPreviewFrame(iframe, dataSelectorResult);

                    // Initialize the kendo HTML editor.
                    const kendoEditorElement = container.find(`#previewEditor${i}`);
                    let kendoEditor = kendoEditorElement.data("kendoEditor");
                    if (!kendoEditor) {
                        await require("@progress/kendo-ui/js/kendo.editor.js");
                        kendoEditor = kendoEditorElement.kendoEditor({
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
                            ],
                            stylesheets: [
                                this.base.settings.htmlEditorCssUrl
                            ],
                            serialization: {
                                custom: this.onHtmlEditorSerialization
                            },
                            deserialization: {
                                custom: this.onHtmlEditorDeserialization
                            }
                        }).data("kendoEditor");
                    }

                    kendoEditor.value(dataSelectorResult);
                }

                tabStrip.select(0);

                const htmlEditorButton = previewWindow.element.find("#htmlPreview");
                const previewButton = previewWindow.element.find("#normalPreview");
                htmlEditorButton.toggle(!action.disableHtmlEditor);
                previewButton.toggle(!action.disableHtmlEditor);

                // Initialize all buttons in the window.
                if (isNewWindow) {
                    previewButton.kendoButton({
                        click: (event) => {
                            const currentTabStrip = container.find("#previewTabStrip").data("kendoTabStrip");
                            const selectedTabContainer = $(currentTabStrip.contentElement(currentTabStrip.select().index()));
                            const kendoEditor = selectedTabContainer.find(".editor").data("kendoEditor");
                            let iframe = selectedTabContainer.find(".iframe")[0];
                            iframe = iframe.contentWindow || (iframe.contentDocument.document || iframe.contentDocument);
                            loadContentInPreviewFrame(iframe, kendoEditor.value());
                            selectedTabContainer.find(".iframe").removeClass("hidden");
                        },
                        icon: "preview"
                    });

                    htmlEditorButton.kendoButton({
                        click: (event) => {
                            const currentTabStrip = container.find("#previewTabStrip").data("kendoTabStrip");
                            const selectedTabContainer = $(currentTabStrip.contentElement(currentTabStrip.select().index()));
                            selectedTabContainer.find(".iframe").addClass("hidden");
                        },
                        icon: "html5"
                    });

                    previewWindow.element.find("#savePreview").kendoButton({
                        click: async (event) => {
                            const currentTabStrip = container.find("#previewTabStrip").data("kendoTabStrip");
                            const selectedTabContainer = $(currentTabStrip.contentElement(currentTabStrip.select().index()));
                            const currentTemplateDetails = container.data("templateDetails");
                            const currentAction = container.data("action");
                            const kendoEditor = selectedTabContainer.find(".editor").data("kendoEditor");
                            const pdfToHtmlData = {
                                html: kendo.htmlEncode(kendoEditor.value()),
                                backgroundPropertyName: currentAction.pdfBackgroundPropertyName || "",
                                itemId: currentTemplateDetails.id
                            };

                            if (currentAction.pdfFilename) {
                                pdfToHtmlData.fileName = currentAction.pdfFilename.replace("{itemId}", currentTemplateDetails.id);
                            }

                            pdfToHtmlData.documentOptions = "";
                            pdfToHtmlData.header = "";
                            pdfToHtmlData.footer = "";

                            if (currentTemplateDetails.details && currentTemplateDetails.details.length > 0) {
                                if (currentAction.pdfDocumentOptionsPropertyName) {
                                    const documentOptions = currentTemplateDetails.details.find(detail => detail.key === currentAction.pdfDocumentOptionsPropertyName);
                                    pdfToHtmlData.documentOptions = documentOptions ? (documentOptions.value || "") : "";
                                }
                                if (currentAction.pdfHeaderPropertyName) {
                                    const header = currentTemplateDetails.details.find(detail => detail.key === currentAction.pdfHeaderPropertyName);
                                    pdfToHtmlData.header = header ? (header.value || "") : "";
                                }
                                if (currentAction.pdfFooterPropertyName) {
                                    const footer = currentTemplateDetails.details.find(detail => detail.key === currentAction.pdfFooterPropertyName);
                                    pdfToHtmlData.footer = footer ? (footer.value || "") : "";
                                }
                            }

                            const process = `convertHtmlToPdf_${Date.now()}`;
                            window.processing.addProcess(process);
                            const pdfResult = await fetch(`${this.base.settings.wiserApiRoot}pdf/from-html`, {
                                method: "POST",
                                headers: {
                                    "Content-Type": "application/json",
                                    "Authorization": `Bearer ${localStorage.getItem("accessToken")}`
                                },
                                body: JSON.stringify(pdfToHtmlData)
                            });
                            await Misc.downloadFile(pdfResult, pdfToHtmlData.fileName || "Pdf.pdf");
                            window.processing.removeProcess(process);
                        },
                        icon: "pdf"
                    });

                    previewWindow.element.find("#printPreview").kendoButton({
                        click: (event) => {
                            const currentTabStrip = container.find("#previewTabStrip").data("kendoTabStrip");
                            const selectedTabContainer = $(currentTabStrip.contentElement(currentTabStrip.select().index()));
                            const kendoEditor = selectedTabContainer.find(".editor").data("kendoEditor");
                            let printIframe = selectedTabContainer.find(".iframe")[0];
                            printIframe = printIframe.contentWindow || (printIframe.contentDocument.document || printIframe.contentDocument);
                            loadContentInPreviewFrame(printIframe, kendoEditor.value(), true);
                        },
                        icon: "print"
                    });

                    previewWindow.element.find("#mailPreview").kendoButton({
                        click: async (event) => {
                            try {
                                const dialogElement = $("#sendMailDialog");
                                const validator = dialogElement.find(".formview").kendoValidator().data("kendoValidator");
                                let mailDialog = dialogElement.data("kendoDialog");

                                // Set the initial values from the query.
                                const currentEmailData = container.data("emailData");
                                const currentAction = container.data("action");
                                const currentTemplateDetails = container.data("templateDetails");
                                dialogElement.find("input[name=senderName]").val(currentEmailData.senderName);
                                dialogElement.find("input[name=senderEmail]").val(currentEmailData.senderEmail);
                                dialogElement.find("input[name=receiverName]").val(currentEmailData.receiverName);
                                dialogElement.find("input[name=receiverEmail]").val(currentEmailData.receiverEmail);
                                dialogElement.find("input[name=cc]").val(currentEmailData.cc);
                                dialogElement.find("input[name=bcc]").val(currentEmailData.bcc);
                                dialogElement.find("input[name=subject]").val(currentEmailData.subject);

                                let emailBodyEditor = dialogElement.find("textarea.editor").data("kendoEditor");
                                let attachmentsUploader = dialogElement.find("input[name=files]").data("kendoUpload");

                                if (mailDialog) {
                                    mailDialog.destroy();
                                }

                                mailDialog = dialogElement.kendoDialog({
                                    width: "900px",
                                    title: "Mail versturen",
                                    closable: false,
                                    modal: true,
                                    actions: [
                                        {
                                            text: "Annuleren"
                                        },
                                        {
                                            text: "Verstuur",
                                            primary: true,
                                            action: (event) => {
                                                if (!validator.validate()) {
                                                    return false;
                                                }

                                                const loader = mailDialog.element.find(".popup-loader").addClass("loading");
                                                let documentOptions = "";
                                                if (currentTemplateDetails.details && currentTemplateDetails.details.length > 0) {
                                                    if (currentAction.pdfDocumentOptionsPropertyName) {
                                                        const documentOptionsDetail = currentTemplateDetails.details.find(detail => detail.key === currentAction.pdfDocumentOptionsPropertyName);
                                                        documentOptions = documentOptionsDetail ? (documentOptionsDetail.value || "") : "";
                                                    }
                                                }

                                                // We cant use await here, because for some reason the event does not get fired anymore if we make this method async.
                                                const promises = [];
                                                const allEditors = container.find(".editor");
                                                for (let index = 0; index < allEditors.length; index++) {
                                                    const kendoEditor = $(allEditors[index]).data("kendoEditor");
                                                    let ajaxOptions = {
                                                        url: `${this.base.settings.wiserApiRoot}pdf/save-html-as-pdf`,
                                                        method: "POST",
                                                        contentType: "application/json",
                                                        data: JSON.stringify({
                                                            html: $("<div/>").text(kendoEditor.value()).html(), // alternative htmlEncode, because kendo.htmlEncode makes from a single quote &#039; (which goes wrong when posted to URL)
                                                            backgroundPropertyName: currentAction.pdfBackgroundPropertyName || "",
                                                            documentOptions: documentOptions,
                                                            itemId: currentTemplateDetails.id,
                                                            saveInDatabase: true
                                                        })
                                                    };
                                                    promises.push(Wiser.api(ajaxOptions));
                                                }

                                                Promise.all(promises).then((results) => {
                                                    const allFiles = dialogElement.find("input[name=files]").data("kendoUpload").getFiles();
                                                    const wiserFileAttachments = allFiles.filter(file => file.fileId > 0).map(file => file.fileId) || [];

                                                    for (let fileId of results) {
                                                        wiserFileAttachments.push(parseInt(fileId.replace(/\"/g, "")));
                                                    }

                                                    const success = () => {
                                                        const successDialog = kendo.alert("De mail is succesvol verstuurd.");
                                                        successDialog.bind("close", () => {
                                                            mailDialog.close();

                                                            // For some reason the previewWindow disappears behind another window once the mailDialog gets closed. So move it back to the front.
                                                            setTimeout(() => { previewWindow.toFront(); }, 100);
                                                        });
                                                    };
                                                    const afterMail = () => {
                                                        if (!action.executeQueryAfterEmail) {
                                                            success();
                                                            return;
                                                        }

                                                        loader.addClass("loading");

                                                        if (!selectedItems || !selectedItems.length) {
                                                            selectedItems = [{ dataItem: { encryptedId: itemId, linkId: linkId } }];
                                                        }

                                                        const queryPromises = [];
                                                        for (let selectedItem of selectedItems) {
                                                            queryPromises.push(Wiser.api({
                                                                method: "POST",
                                                                url: `${this.base.settings.wiserApiRoot}items/${encodeURIComponent(selectedItem.dataItem.encryptedId || selectedItem.dataItem.encrypted_id)}/action-button/${propertyId}?queryId=${encodeURIComponent(action.executeQueryAfterEmail)}&itemLinkId=${encodeURIComponent(selectedItem.dataItem.linkId || selectedItem.dataItem.link_id || 0)}`,
                                                                data: JSON.stringify(userParametersWithValues),
                                                                contentType: "application/json"
                                                            }));
                                                        }

                                                        Promise.all(queryPromises).then(success).catch((error) => {
                                                            console.error(error);
                                                            kendo.alert(`Er is iets fout gegaan tijdens het uitvoeren van actie '${action.executeQueryAfterEmail}' na het sturen van de e-mail. De e-mail zelf is wel gestuurd. Neem a..u.b. contact op met ons.`);
                                                        });
                                                        Promise.allSettled(queryPromises).then(() => {
                                                            loader.removeClass("loading");
                                                        });
                                                    };

                                                    const cc = mailDialog.element.find("input[name=cc]").val();
                                                    const bcc = mailDialog.element.find("input[name=bcc]").val();

                                                    Wiser.api({
                                                        url: `${this.base.settings.wiserApiRoot}communications/email`,
                                                        method: "POST",
                                                        contentType: "application/json",
                                                        data: JSON.stringify({
                                                            senderName: mailDialog.element.find("input[name=senderName]").val(),
                                                            sender: mailDialog.element.find("input[name=senderEmail]").val(),
                                                            receivers: [{
                                                                displayName: mailDialog.element.find("input[name=receiverName]").val(),
                                                                address: mailDialog.element.find("input[name=receiverEmail]").val(),
                                                            }],
                                                            cc: cc ? [cc] : null,
                                                            bcc: bcc ? [bcc] : null,
                                                            subject: mailDialog.element.find("input[name=subject]").val(),
                                                            wiserItemFiles: wiserFileAttachments,
                                                            content: emailBodyEditor.value()
                                                        })
                                                    }).catch((jqXHR, textStatus, errorThrown) => {
                                                        console.error(jqXHR, textStatus, errorThrown);
                                                        kendo.alert("Er is iets fout gegaan tijdens het sturen van de mail. Probeer het a.u.b. nogmaals of neem contact op met ons");
                                                    }).finally(() => {
                                                        loader.removeClass("loading");
                                                    }).then((mailResult) => {
                                                        afterMail();
                                                    });
                                                }).catch((error) => {
                                                    console.error(error);
                                                    loader.removeClass("loading");
                                                    kendo.alert("Er is iets fout gegaan met het genereren van de PDF. Probeer het a.u.b. nogmaals of neem contact op met ons");
                                                });

                                                return false;
                                            }
                                        }
                                    ]
                                }).data("kendoDialog");

                                // Initialize the file uploader.
                                if (attachmentsUploader) {
                                    // Destroy the kendo widget instance.
                                    attachmentsUploader.destroy();

                                    // Remove the HTML that was generated by Kendo (otherwise we'll still get duplicate elements after recreating the widget).
                                    dialogElement.find("#attachmentFilesContainer").append(dialogElement.find("input[name=files]"));
                                    dialogElement.find(".k-upload").remove();
                                }

                                let files = [];
                                if (currentAction.emailDefaultAttachmentsPropertyName) {
                                    const initialFilesJson = currentTemplateDetails.property_[currentAction.emailDefaultAttachmentsPropertyName] || "[]";
                                    files = JSON.parse(initialFilesJson);
                                }

                                // Always re-create the upload widget because it's not possible to add files to programmatically a widget after it's been initialized, without using weird hacks.
                                attachmentsUploader = dialogElement.find("input[name=files]").kendoUpload({
                                    files: files,
                                    enabled: false
                                }).data("kendoUpload");

                                // Initialize the kendo HTML editor.
                                if (emailBodyEditor) {
                                    // Destroy Kendo instance.
                                    emailBodyEditor.destroy();

                                    // Clean up HTML of destroyed Kendo instance.
                                    const textArea = dialogElement.find("textarea.editor");
                                    const parent = textArea.closest("span.k-input");
                                    parent.append(textArea);
                                    parent.find("table.k-editor").remove();
                                }

                                await require("@progress/kendo-ui/js/kendo.editor.js");
                                emailBodyEditor = dialogElement.find("textarea.editor").kendoEditor({
                                    tools: [
                                        "bold",
                                        "italic",
                                        "underline",
                                        "justifyLeft",
                                        "justifyCenter",
                                        "justifyRight",
                                        "justifyFull",
                                        "insertUnorderedList",
                                        "insertOrderedList",
                                        "createLink",
                                        "unlink"
                                    ],
                                    stylesheets: [
                                        this.base.settings.htmlEditorCssUrl
                                    ],
                                    serialization: {
                                        custom: this.onHtmlEditorSerialization
                                    },
                                    deserialization: {
                                        custom: this.onHtmlEditorDeserialization
                                    }
                                }).data("kendoEditor");

                                emailBodyEditor.value(currentEmailData.body || "");

                                mailDialog.open();
                            } catch (exception) {
                                console.error(exception);
                                let error = exception;
                                if (exception.responseText) {
                                    error = exception.responseText;
                                } else if (exception.statusText) {
                                    error = exception.statusText;
                                }
                                kendo.alert(`Er is iets fout gegaan met het openen van het scherm om een e-mail te versturen. Probeer het a.u.b. nogmaals of neem contact op met ons.<br><br>De fout was:<br><pre>${kendo.htmlEncode(error)}</pre>`);
                            }
                        },
                        icon: "email"
                    });
                }

                // Open the window.
                previewWindow.maximize().open();
            } catch (exception) {
                console.error(exception);
                let error = exception;
                if (exception.responseText) {
                    error = exception.responseText;
                } else if (exception.statusText) {
                    error = exception.statusText;
                }
                kendo.alert(`Er is iets fout gegaan met het laden of verwerken van data voor dit scherm. Probeer het a.u.b. nogmaals of neem contact op met ons.<br><br>De fout was:<br><pre>${kendo.htmlEncode(error)}</pre>`);
                resolve();
            }

            if (element && element.siblings(".grid-loader").length) {
                element.siblings(".grid-loader").removeClass("loading");
            } else {
                window.processing.removeProcess(process);
            }
        });
    }

    /**
     * Event that gets called when the user executes the custom action for adding an image from Wiser to the HTML editor.
     * This will open the fileHandler from Wiser 1.0 via the parent frame. Therefor this function only works while Wiser is being loaded in an iframe.
     * @param {any} event The event from the execute action.
     * @param {any} kendoEditor The Kendo HTML editor where the action is executed in.
     * @param {any} codeMirror The CodeMirror editor where the action is executed in.
     * @param {any} contentbuilder The Contentbuilder editor where the action is executed in.
     */
    async onHtmlEditorImageExec(event, kendoEditor, codeMirror, contentbuilder) {
         if (!this.base.settings.imagesRootId) {
            kendo.alert("Er is nog geen 'imagesRootId' ingesteld in de database. Neem a.u.b. contact op met ons om dit te laten instellen.");
        } else {
            this.base.windows.fileManagerWindowSender = { kendoEditor: kendoEditor, codeMirror: codeMirror, contentbuilder: contentbuilder };
            this.base.windows.fileManagerWindowMode = this.base.windows.fileManagerModes.images;
            this.base.windows.fileManagerWindow.center().open();
        }
    }

    /**
     * Event that gets called when the user executes the custom action for adding a link to a file from Wiser to the HTML editor.
     * This will open the fileHandler from Wiser 1.0 via the parent frame. Therefor this function only works while Wiser is being loaded in an iframe.
     * @param {any} event The event from the execute action.
     * @param {any} kendoEditor The Kendo HTML editor where the action is executed in.
     * @param {any} codeMirror The CodeMirror editor where the action is executed in.
     * @param {any} contentbuilder The Contentbuilder editor where the action is executed in.
     */
    async onHtmlEditorFileExec(event, kendoEditor, codeMirror, contentbuilder) {
        if (!this.base.settings.filesRootId) {
            kendo.alert("Er is nog geen 'filesRootId' ingesteld in de database. Neem a.u.b. contact op met ons om dit te laten instellen.");
        } else {
            this.base.windows.fileManagerWindowSender = { kendoEditor: kendoEditor, codeMirror: codeMirror, contentbuilder: contentbuilder };
            this.base.windows.fileManagerWindowMode = this.base.windows.fileManagerModes.files;
            this.base.windows.fileManagerWindow.center().open();
        }
    }

    /**
     * Event that gets called when the user executes the custom action for adding a template from Wiser to the HTML editor.
     * This will open the fileHandler from Wiser 1.0 via the parent frame. Therefor this function only works while Wiser is being loaded in an iframe.
     * @param {any} event The event from the execute action.
     * @param {any} kendoEditor The Kendo HTML editor where the action is executed in.
     * @param {any} codeMirror The CodeMirror editor where the action is executed in.
     * @param {any} contentbuilder The Contentbuilder editor where the action is executed in.
     */
    async onHtmlEditorTemplateExec(event, kendoEditor, codeMirror, contentbuilder) {
        if (!this.base.settings.templatesRootId) {
            kendo.alert("Er is nog geen 'templatesRootId' ingesteld in de database. Neem a.u.b. contact op met ons om dit te laten instellen.");
        } else {
            this.base.windows.fileManagerWindowSender = { kendoEditor: kendoEditor, codeMirror: codeMirror, contentbuilder: contentbuilder };
            this.base.windows.fileManagerWindowMode = this.base.windows.fileManagerModes.templates;
            this.base.windows.fileManagerWindow.center().open();
        }
    }

    /**
     * Event that gets called when the user executes the custom action for viewing / changing the HTML source of the editor.
     * @param {any} event The event from the execute action.
     * @param {any} editor The HTML editor where the action is executed in.
     * @param {any} itemId The ID of the current item.
     */
    async onHtmlEditorHtmlSourceExec(event, editor, itemId) {
        const htmlWindow = $("#htmlSourceWindow").clone(true);
        const textArea = htmlWindow.find("textarea").val(editor.value());
        // Prettify code from minified text.
        const pretty = await require('pretty');
        textArea[0].value = pretty(textArea[0].value, {
            ocd: false,
            indent_size: 4,
            unformatted: [],
            inline: []
        });
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

        htmlWindow.find(".addImage").kendoButton({
            click: (event) => {
                this.onHtmlEditorImageExec(event, null, codeMirrorInstance);
            },
            icon: "image-insert"
        });
        htmlWindow.find(".addTemplate").kendoButton({
            click: () => {
                this.onHtmlEditorTemplateExec(event, null, codeMirrorInstance);
            },
            icon: "template-manager"
        });

        htmlWindow.find(".k-primary, .k-button-solid-primary").kendoButton({
            click: () => {
                editor.value(codeMirrorInstance.getValue());
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

    /**
     * Event that gets called when the user executes the custom action for viewing / changing the HTML source of the editor.
     * @param {any} event The event from the execute action.
     * @param {any} editor The HTML editor where the action is executed in.
     * @param {any} itemId The ID of the current item.
     * @param {string} propertyName The property name that contains the HTML of the item.
     * @param {string} languageCode The language code of the property to use for the HTML.
     * @param {string} contentBuilderMode The mode in which to put the ContentBuilder.
     */
    async onHtmlEditorContentBuilderExec(event, editor, itemId, propertyName, languageCode, contentBuilderMode) {
        const htmlWindow = $("#contentBuilderWindow").clone(true);

        const iframe = htmlWindow.find("iframe");
        let moduleName = "ContentBuilder";
        if (contentBuilderMode === "ContentBox") {
            moduleName = "ContentBox";
        }
        iframe.attr("src", `/Modules/${moduleName}?wiserItemId=${encodeURIComponent(itemId)}&propertyName=${encodeURIComponent(propertyName)}&languageCode=${encodeURIComponent(languageCode || "")}&userId=${encodeURIComponent(this.base.settings.userId)}`);

        htmlWindow.kendoWindow({
            width: "100%",
            height: "100%",
            title: "Content builder",
            close: (closeEvent) => {
                closeEvent.sender.destroy();
                htmlWindow.remove();
            }
        });

        const kendoWindow = htmlWindow.data("kendoWindow").maximize().open();

        htmlWindow.find(".k-primary, .k-button-solid-primary").kendoButton({
            click: () => {
                const html = typeof(iframe[0].contentWindow.main.vueApp.contentBox) === "undefined"
                    ? iframe[0].contentWindow.main.vueApp.contentBuilder.html()
                    : iframe[0].contentWindow.main.vueApp.contentBox.html();
                editor.value(html);

                const container = editor.element.closest(".entity-container");
                container.find(".saveBottomPopup, .saveButton").trigger("click");

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

    /**
     * Event that gets called when the user executes the custom action for maximizing an HTML editor.
     * @param {any} event The event from the execute action.
     * @param {any} editor The HTML editor where the action is executed in.
     * @param {any} itemId The ID of the current item.
     */
    async onHtmlEditorFullScreenExec(event, editor) {
        const htmlWindow = $("#maximizeEditorWindow").clone(true);
        const textArea = htmlWindow.find("textarea").val(editor.value());
        let windowKendoEditor;

        htmlWindow.kendoWindow({
            width: "100%",
            height: "100%",
            title: "HTML-editor",
            close: (closeEvent) => {
                windowKendoEditor.destroy();
                closeEvent.sender.destroy();
                htmlWindow.remove();
            }
        });

        const kendoWindow = htmlWindow.data("kendoWindow").center().open();

        const options = $.extend(true, {}, editor.options);
        if (options && options.tools && options.tools.length > 0) {
            let maximizeButtonIndex = -1;
            for (let i = 0; i < options.tools.length; i++) {
                const tool = options.tools[i];
                if (tool.name === "wiserMaximizeEditor") {
                    maximizeButtonIndex = i;
                    break;
                }
            }

            if (maximizeButtonIndex > -1) {
                const minimizeTool = {
                    name: "wiserMinimizeEditor",
                    tooltip: "Verkleinen",
                    exec: (e) => {
                        editor.value(windowKendoEditor.value());
                        kendoWindow.close();
                    }
                };
                options.tools.splice(maximizeButtonIndex, 1, minimizeTool);
            }
        }

        await require("@progress/kendo-ui/js/kendo.editor.js");
        windowKendoEditor = textArea.kendoEditor(options).data("kendoEditor");
    }

    /**
     * Event that gets called when the user executes the custom action for entering an HTML block for an entity.
     * @param {any} event The event from the execute action.
     * @param {any} editor The HTML editor where the action is executed in.
     */
    async onHtmlEditorEntityBlockExec(event, editor) {
        const itemId = await kendo.prompt("Vul een item ID in");
        let html = "";
        try {
            html = await this.base.getEntityBlock(itemId);
        } catch (exception) {
            html = `<!-- ${exception} -->`;
            console.error(exception);
        }

        const originalOptions = editor.options.pasteCleanup;
        editor.options.pasteCleanup.none = true;
        editor.options.pasteCleanup.span = false;
        editor.exec("inserthtml", { value: `<!-- Start entity block with id ${itemId} -->${html}<!-- End entity block with id ${itemId} -->` });
        editor.options.pasteCleanup.none = originalOptions.none;
        editor.options.pasteCleanup.span = originalOptions.span;
    }

    /**
     * Event that gets called when the user executes the custom action for entering an HTML block for an entity.
     * @param {any} event The event from the execute action.
     * @param {any} editor The HTML editor where the action is executed in.
     */
    async onHtmlEditorDataSelectorExec(event, editor) {
        try {
            const dialogElement = $("#dataSelectorTemplateDialog");
            let dataSelectorTemplateDialog = dialogElement.data("kendoDialog");

            if (dataSelectorTemplateDialog) {
                dataSelectorTemplateDialog.destroy();
            }

            const dataSelectorDropDown = dialogElement.find("#dataSelectorDropDown").kendoDropDownList({
                optionLabel: "Selecteer data selector",
                dataTextField: "name",
                dataValueField: "id",
                dataSource: {
                    transport: {
                        read: async (options) => {
                            try {
                                const results = await Wiser.api({ url: `${this.base.settings.wiserApiRoot}data-selectors?forRendering=true` });
                                options.success(results);
                            } catch (exception) {
                                console.error(exception);
                                options.error(exception);
                            }
                        }
                    }
                }
            }).data("kendoDropDownList");

            const dataSelectorTemplateDropDown = dialogElement.find("#dataSelectorTemplateDropDown").kendoDropDownList({
                optionLabel: "Selecteer template",
                dataTextField: "title",
                dataValueField: "id",
                dataSource: {
                    transport: {
                        read: async (options) => {
                            try {
                                const results = await Wiser.api({ url: `${this.base.settings.wiserApiRoot}data-selectors/templates` });
                                options.success(results);
                            } catch (exception) {
                                console.error(exception);
                                options.error(exception);
                            }
                        }
                    }
                }
            }).data("kendoDropDownList");

            dataSelectorTemplateDialog = dialogElement.kendoDialog({
                width: "900px",
                title: "Data selector met template invoegen",
                closable: false,
                modal: true,
                actions: [
                    {
                        text: "Annuleren"
                    },
                    {
                        text: "Invoegen",
                        primary: true,
                        action: (event) => {
                            const selectedDataSelector = dataSelectorDropDown.value();
                            const selectedTemplate = dataSelectorTemplateDropDown.value();
                            if (!selectedDataSelector || !selectedTemplate) {
                                kendo.alert("Kies a.u.b. een data selector en een template.")
                                return false;
                            }

                            let html = `<div class="dynamic-content" data-selector-id="${selectedDataSelector}" template-id="${selectedTemplate}"><h2>Data selector '${dataSelectorDropDown.text()}' met template '${dataSelectorTemplateDropDown.text()}'</h2></div>`;
                            Wiser.api({
                                url: `${this.base.settings.wiserApiRoot}data-selectors/preview-for-html-editor`,
                                method: "POST",
                                contentType: "application/json",
                                data: html
                            }).then((newHtml) => {
                                html = newHtml;
                            }).catch((error) => {
                                console.error(error);
                            }).finally(() => {
                                const originalOptions = editor.options.pasteCleanup;
                                editor.options.pasteCleanup.none = true;
                                editor.options.pasteCleanup.span = false;
                                editor.exec("inserthtml", { value: html });
                                editor.options.pasteCleanup.none = originalOptions.none;
                                editor.options.pasteCleanup.span = originalOptions.span;
                            });
                        }
                    }
                ]
            }).data("kendoDialog");

            dataSelectorTemplateDialog.open();
        } catch (exception) {
            console.error(exception);
            kendo.alert("Er is iets fout gegaan. Probeer het a.u.b. nogmaals of neem contact op met ons.");
        }
    }

    /**
     * Event that gets called when the user executes the custom action for embedding a youtube video.
     * @param {any} event The event from the execute action.
     * @param {any} editor The HTML editor where the action is executed in.
     */
    async onHtmlEditorYouTubeExec(event, editor) {
        try {
            const dialogElement = $("#youTubeDialog");
            let youtubeDialog = dialogElement.data("kendoDialog");

            if (youtubeDialog) {
                youtubeDialog.destroy();
            }

            youtubeDialog = dialogElement.kendoDialog({
                width: "900px",
                title: "YouTube video invoegen",
                closable: false,
                modal: true,
                actions: [
                    {
                        text: "Annuleren"
                    },
                    {
                        text: "Invoegen",
                        primary: true,
                        action: (event) => {
                            const videoId = dialogElement.find("#youTubeVideoId").val();
                            const width = dialogElement.find("#youTubeVideoWidth").val();
                            const height = dialogElement.find("#youTubeVideoHeight").val();
                            if (!videoId || !width || !height) {
                                kendo.alert("Vul a.u.b. een video-ID, hoogte en breedte in.")
                                return false;
                            }

                            const queryString = {
                                rel: dialogElement.find("#youTubeShowRelatedVideos").prop("checked"),
                                autoplay: dialogElement.find("#youTubeAutoPlay").prop("checked")
                            };

                            let fullScreenAttribute = "";
                            if (dialogElement.find("#youTubeAllowFullScreen").prop("checked")) {
                                fullScreenAttribute = 'allowfullscreen="allowfullscreen"';
                            }

                            let html = `<iframe width="${width}" height="${height}" src="//www.youtube.com/embed/${videoId}${Utils.toQueryString(queryString, true)}" frameborder="0" ${fullScreenAttribute}></iframe>`;

                            const originalOptions = editor.options.pasteCleanup;
                            editor.options.pasteCleanup.none = true;
                            editor.options.pasteCleanup.span = false;
                            editor.exec("inserthtml", { value: html });
                            editor.options.pasteCleanup.none = originalOptions.none;
                            editor.options.pasteCleanup.span = originalOptions.span;
                        }
                    }
                ]
            }).data("kendoDialog");

            youtubeDialog.open();
        } catch (exception) {
            console.error(exception);
            kendo.alert("Er is iets fout gegaan. Probeer het a.u.b. nogmaals of neem contact op met ons.");
        }
    }

    /**
     * Event that gets called when the user double clicks a dynamic content block in a HTML editor.
     * This will open the dynamicHandler from Wiser 1.0 via the parent frame. Therefor this function only works while Wiser is being loaded in an iframe.
     * @param {any} event The event from the execute action.
     * @param {any} editor The HTML editor where the action is executed in.
     * @param {any} itemId The ID of the current item.
     */
    async onHtmlEditorDblClick(event, editor, itemId) {
        if (!window.parent || !window.parent.$ || !window.parent.$.dynamicHandler) {
            console.warn("No parent window found, or no file handler found on parent window.");
            return;
        }

        let clickedElement = $(event.target);

        if (event.target.tagName !== "IMG" && event.target.tagName !== "TABLE") {
            clickedElement = clickedElement.closest("table.dyn-content");
        }

        if (!clickedElement.length) {
            return;
        }

        const contentId = parseInt(clickedElement.data("contentid")) || parseInt(clickedElement.attr("contentid"));

        if (!contentId) {
            if (parseInt(clickedElement.attr("pageid"))) {
                kendo.alert("Het wijzigen van dynamische content type webpagina wordt nog niet ondersteund.");
            }
            else if (clickedElement.is("img")) {
                this.onHtmlEditorImageExec(event, editor);
            }
            return;
        }

        // Select the element in the editor so that it will be replaced if the user updates the dynamic content.
        const range = editor.createRange();
        range.setStart(clickedElement[0], 0);
        range.setEnd(clickedElement[0], clickedElement[0].childNodes.length);
        editor.selectRange(range);

        window.parent.$.core.loadCodeMirror(() => {
            new window.parent.$.dynamicHandler(this, contentId, null, null, { id: this.base.settings.moduleId }, itemId, null, (dynamicContentData) => {
                const originalOptions = editor.options.pasteCleanup;
                editor.options.pasteCleanup.none = true;
                editor.options.pasteCleanup.span = false;
                editor.exec("inserthtml", { value: dynamicContentData.img || dynamicContentData.html });
                editor.options.pasteCleanup.none = originalOptions.none;
                editor.options.pasteCleanup.span = originalOptions.span;
            });
        });
    }

    /**
     * Event that gets called when the user presses a key in a HTML editor.
     * This will execute certain actions if the correct key combination was pressed.
     * @param {any} event The event from the execute action.
     * @param {any} editor The HTML editor where the action is executed in.
     */
    async onHtmlEditorKeyUp(event, editor) {
        switch (event.key) {
            case "M":
            case "m":
                {
                    if (event.ctrlKey) {
                        let currentDateTime = DateTime.now().setLocale("nl-NL").toLocaleString()
                        const signature = `[${currentDateTime} - ${this.base.settings.username}]<br/><br/><span class="comment"></span>`;
                        const newText = `${signature}<br/><br/><br/><hr><br/>${editor.value()}`;

                        // add text
                        editor.value(newText);

                        // Set caret
                        const range = editor.getRange();
                        const caretElement = editor.body.querySelector(".comment") || editor.body;
                        range.setStart(caretElement, 0);
                        editor.selectRange(range);
                        range.collapse(true);
                    }
                    break;
                }
        }
    }

    /**
     * Event that gets called when the Kendo editor serializes it's contents.
     * @param html The HTML contents of the editor.
     * @returns {*} The HTML contents of the editor.
     */
    onHtmlEditorSerialization(html) {
        return html.replace(/\[(>|&gt;)\]([\w:?]+)\[(<|&lt;)\]/g, "{$2}");
    }

    /**
     * Event that gets called when the Kendo editor deserializes it's contents.
     * @param html The HTML contents of the editor.
     * @returns {*} The HTML contents of the editor.
     */
    onHtmlEditorDeserialization(html) {
        return html.replace(/{([\w:?]+)}/g, "[>]$1[<]");
    }

    /**
     * Event that gets called when the user presses a key in a text field.
     * This will execute certain actions if the correct key combination was pressed.
     * @param {any} event The event from the execute action.
     */
    async onTextFieldKeyUp(event) {
        switch (event.key) {
            case "m":
                {
                    if (event.ctrlKey) {
                        const target = event.currentTarget;
                        let currentDateTime = DateTime.now().setLocale("nl-NL").toLocaleString()
                        const signature = `[${currentDateTime} - ${this.base.settings.username}]\n`;
                        const newText = `${signature}\n\n\n------------\n\n${target.value}`;

                        target.value = newText;

                        // setCursor
                        target.selectionStart = signature.length + 1;
                        target.selectionEnd = signature.length + 1;
                    }
                    break;
                }
        }
    }

    /**
     * Event that gets called when the user presses a key in an input field.
     * This will execute certain actions if the correct key combination was pressed.
     * @param {any} event The event from the execute action.
     */
    onInputFieldKeyUp(event, fieldOptions) {
        if (fieldOptions.saveOnEnter && event.key.toLowerCase() === "enter") {
            let container = $(event.currentTarget).closest("#right-pane");
            if (!container.length) {
                container = $(event.currentTarget).closest(".entity-container");
                container.find(".saveBottomPopup").trigger("click");
            } else {
                const saveAndCreateNewButton = $("#saveAndCreateNewItemButton");
                if (saveAndCreateNewButton.is(":visible")) {
                    saveAndCreateNewButton.trigger("click");
                } else {
                    container.find("#saveBottom").trigger("click");
                }
            }
        }
    }

    /**
     * Event that gets fired when the value of any input has been changed.
     * @param {any} event The event from the change action.
     */
    onFieldValueChange(event) {
        this.handleDependencies(event);

        const fieldContainer = (event.sender ? event.sender.element : $(event.currentTarget)).closest(".item");
        const itemContainer = fieldContainer.closest("#right-pane, .popup-container");
        const saveOnChange = fieldContainer.data("saveOnChange");
        if (saveOnChange) {
            let saveButton = itemContainer.find(".saveBottomPopup");
            if (!saveButton.length) {
                saveButton = itemContainer.find(".saveButton");
            }
            saveButton.first().trigger("click");
        }
    }

    /**
     * Method to insert text at the position of the cursus in a text field.
     * Code is from http://stackoverflow.com/questions/11076975/insert-text-into-textarea-at-cursor-position-javascript
     * @param {any} myField The field to insert the text in.
     * @param {any} myValue The text to insert.
     */
    insertAtCursor(myField, myValue) {
        //IE support
        if (document.selection) {
            myField.focus();
            const selection = document.selection.createRange();
            selection.text = myValue;
        }
        //MOZILLA and others
        else if (myField.selectionStart || myField.selectionStart == "0") {
            const startPos = myField.selectionStart;
            const endPos = myField.selectionEnd;
            myField.value = myField.value.substring(0, startPos) + myValue + myField.value.substring(endPos, myField.value.length);
            myField.selectionStart = startPos + myValue.length;
            myField.selectionEnd = startPos + myValue.length;
        } else {
            myField.value += myValue;
        }
    }

    /**
     * Updates the width of a field/property.
     * This is done via the API, so that the API can double check the user's rights, because not all users are allowed to do this.
     * @param {number} propertyId The ID of the property.
     * @param {number} width The new width.
     * @returns {any} A promise.
     */
    updateWidth(propertyId, width) {
        return Wiser.api({
            url: `${this.base.settings.wiserApiRoot}properties/${encodeURIComponent(propertyId)}/width/${encodeURIComponent(width)}`,
            method: "PUT",
            contentType: "application/json"
        });
    }
}
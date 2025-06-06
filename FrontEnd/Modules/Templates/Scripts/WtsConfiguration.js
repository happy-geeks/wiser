import {Wiser} from "../../Base/Scripts/Utils.js";
import "../../Base/Scripts/Processing.js";
import "../css/WtsConfiguration.css";
import codeMirrorComponents from "../../Base/Scripts/codemirror/scsslint";
import index from "vuex";
import {forEach} from "jszip";

require("@progress/kendo-ui/js/kendo.notification.js");
require("@progress/kendo-ui/js/kendo.button.js");
require("@progress/kendo-ui/js/kendo.combobox.js");
require("@progress/kendo-ui/js/kendo.editor.js");
require("@progress/kendo-ui/js/kendo.splitter.js");
require("@progress/kendo-ui/js/kendo.tabstrip.js");
require("@progress/kendo-ui/js/kendo.treeview.js");
require("@progress/kendo-ui/js/kendo.grid.js");
require("@progress/kendo-ui/js/kendo.datetimepicker.js");
require("@progress/kendo-ui/js/kendo.multiselect.js");
require("@progress/kendo-ui/js/cultures/kendo.culture.nl-NL.js");
require("@progress/kendo-ui/js/messages/kendo.messages.nl-NL.js");

export class WtsConfiguration {

    /**
     * Initializes a new instance of DynamicContent.
     * @param {any} base The base Template class.
     */
    constructor(base) {
        this.base = base;

        this.template = null;
        this.serviceInputFields = [];
        this.serviceKendoFields = [];
        this.timersInputFields = [];
        this.timersKendoFields = [];
        this.queriesInputFields = [];
        this.queriesKendoFields = [];
        this.httpapisInputFields = [];
        this.httpapisKendoFields = [];
        this.editorSql = [];
    }

    async reloadWtsConfigurationTab(id) {
        // Empty the tab
        document.getElementById("wtsConfigurationTab").innerHTML = "";

        // Empty all the arrays
        this.serviceInputFields = [];
        this.serviceKendoFields = [];
        this.timersInputFields = [];
        this.timersKendoFields = [];
        this.queriesInputFields = [];
        this.queriesKendoFields = [];
        this.httpapisInputFields = [];
        this.httpapisKendoFields = [];

        // Check if the ID is set
        if (id === undefined || id === null || id === 0) {
            console.error("id is not set");
            return;
        }

        // Tell the user that the tab is loading
        this.base.toggleMainLoader(true);
        let templateSettings = null;

        // Get the data from the API
        try {
            templateSettings = await Wiser.api({
                url: `${this.base.settings.wiserApiRoot}templates/${id}/wtsconfiguration`,
                dataType: "json",
                method: "GET"
            });
            this.template = templateSettings;
        } catch (e) {
            console.error(e);
            kendo.alert("Er is iets fout gegaan. Sluit a.u.b. deze module, open deze daarna opnieuw en probeer het vervolgens opnieuw. Of neem contact op als dat niet werkt.");
            return;
        } finally {
            this.base.toggleMainLoader(false); // Hide the loader
        }

        // Build the view
        try {
            let response = await Wiser.api({
                method: "POST",
                contentType: "application/json",
                url: "/Modules/Templates/WtsConfigurationTab",
                data: JSON.stringify(templateSettings)
            })

            document.getElementById("wtsConfigurationTab").innerHTML = response; // Add the HTML to the tab
            $("#tabStripConfiguration").kendoTabStrip().data("kendoTabStrip"); // Initialize the tabstrip

        } catch (e) {
            console.error(e);
            kendo.alert("Er is iets fout gegaan. Sluit a.u.b. deze module, open deze daarna opnieuw en probeer het vervolgens opnieuw. Of neem contact op als dat niet werkt.");
        } finally {
            this.base.toggleMainLoader(false); // Hide the loader
        }

        this.initializeKendoComponents();
        await this.initializeCodeMirror();
        this.updateDropDownLists("httpapis");
        this.updateDropDownLists("queries");
        this.bindEvents();
    }

    initializeKendoComponents() {
        // Find all the kendo components and initialize them
        let kendoComponents = document.querySelectorAll("[data-kendo-component]");
        // Empty the array
        this.serviceInputFields = [];

        // Loop through all the components
        for (let i = 0; i < kendoComponents.length; i++) {
            let component = $(kendoComponents[i]);
            let componentName = `${component.attr("data-kendo-component")}`;

            // Set the value of the component name and ensure the first letter is lowercase.
            componentName = this.uncapitalizeFirstLetter(componentName);
            let componentTab = component.attr("data-kendo-tab");
            let componentOptions = component.attr("data-kendo-options");

            // Check if the options are set
            if (componentOptions === undefined || componentOptions === null || componentOptions === "") {
                componentOptions = {};
            } else {
                componentOptions = JSON.parse(componentOptions);
            }

            // Add a list change event to the grid if it allows editing.
            if (component.attr("data-kendo-component") === "KendoGrid" && component.attr("allow-edit") === "true") {
                componentOptions.change = this.onListChange.bind(this);
            }

            // Check if the component has UseDataSource set to true.
            if (component.attr("use-datasource") === "true") {
                // Find the name of the property.
                let propertyName = this.uncapitalizeFirstLetter(component.attr("name"));
                // Set the dataSource to the correct property.
                let data = this.template[propertyName]

                let schema = {model: {}};
                if (component.attr("data-kendo-component") === "KendoGrid") {
                    schema = {
                        model: {
                            id: `${this.uncapitalizeFirstLetter(component.attr("id-property"))}`,
                        }
                    }
                }
                let dataSource = new kendo.data.DataSource({
                    data: data,
                    schema: schema
                });

                componentOptions.dataSource = dataSource;
            }

            // Check if any other field depends on this field, if so add a change event.
            let isDependedOn = document.querySelectorAll(`[data-depend-on-field="${component.attr("name")}"]`);
            if (isDependedOn.length > 0) {
                // Add an event listener to the options.
                componentOptions.change = this.onDependFieldChange.bind(this);
                // Add an attribute to the component to indicate it is being depended on.
                component.attr("data-is-depended-on", true);
            }

            // Make sure the method exists on componentSelector and if so create the component.
            if (componentName === "textArea") {
                switch (componentTab) {
                    case "Service":
                        this.serviceInputFields.push(component[0]);
                        break;
                    case "Timers":
                        this.timersInputFields.push(component[0]);
                        break;
                    case "Queries":
                        this.queriesInputFields.push(component[0]);
                        break;
                    case "HttpApis":
                        this.httpapisInputFields.push(component[0]);
                        break;
                }
            } else if (component[componentName] && typeof component[componentName] === "function") {
                let newComponent = component[componentName](componentOptions).data(componentName);

                if (componentName == "kendoGrid") {
                    var newDataSource = new kendo.data.DataSource({
                        data: newComponent.dataSource.data(),
                        schema: newComponent.dataSource.options.schema
                    });
                    newComponent.setDataSource(newDataSource);
                }
                // Save the component and field so we can access it later.
                switch (componentTab) {
                    case "Service":
                        this.serviceInputFields.push(component[0]);
                        this.serviceKendoFields.push(newComponent);
                        break;
                    case "Timers":
                        this.timersInputFields.push(component[0]);
                        this.timersKendoFields.push(newComponent);
                        break;
                    case "Queries":
                        this.queriesInputFields.push(component[0]);
                        this.queriesKendoFields.push(newComponent);
                        break;
                    case "HttpApis":
                        this.httpapisInputFields.push(component[0]);
                        this.httpapisKendoFields.push(newComponent);
                        break;
                }
            } else {
                console.error(`Method ${componentName}+${i} does not exist on componentSelector.`);
                return;
            }

            // Check if the component allows editing.
            if (component.attr("allow-edit") === "true") {
                // Find the corresponding buttons and attach click events.
                let createButton = $(`#${component.attr('name')}CreateButton`);
                let saveButton = $(`#${component.attr('name')}SaveButton`);
                let deleteButton = $(`#${component.attr('name')}DeleteButton`);

                // Check if the buttons exist.
                if (createButton.length > 0) {
                    createButton.on("click", this.onCreateButtonClick.bind(this));
                }
                if (saveButton.length > 0) {
                    saveButton.on("click", this.onSaveButtonClick.bind(this));
                }
                if (deleteButton.length > 0) {
                    deleteButton.on("click", this.onDeleteButtonClick.bind(this));
                }
            }
        }

        // Loading is done, fire all change events.
        this.fireAllChangeEvents();
    }

    async initializeCodeMirror() {
        await Misc.ensureCodeMirror();

        //Clear the array of editors.
        this.editorSql = []

        // Get all components that need to be CodeMirror instances.
        let CodeMirrorComponents = document.querySelectorAll("[data-wts-editor-type]");

        for (let i = 0; i < CodeMirrorComponents.length; i++) {
            // Create a CodeMirror instance.
            var editortmp = CodeMirror.fromTextArea(CodeMirrorComponents[i], {
                mode: `${$(CodeMirrorComponents[i]).attr("data-wts-editor-type")}`,
                lineNumbers: true
            });
            // Refresh the CodeMirror instance to avoid UI errors.
            editortmp.refresh();
            // Add the CodeMirror instance to an array for later access while maintaining a link to the original component.
            this.editorSql.push(
                {
                    editor: editortmp,
                    original: CodeMirrorComponents[i]
                }
            );
        }
    }


    fireAllChangeEvents(limitToCurrentTab = false) {
        // Fire any change events that are set.
        let elementsWithChangeEvent = document.querySelectorAll('[data-is-depended-on]');

        elementsWithChangeEvent.forEach((changeEvent) => {
            let component = $(changeEvent);
            let componentName = `${component.attr("data-kendo-component")}`;
            componentName = this.uncapitalizeFirstLetter(componentName);
            if (limitToCurrentTab) {
                let tabStrip = $("#tabStripConfiguration").data("kendoTabStrip");
                let currentTab = tabStrip.select();
                let currentTabName = $(currentTab).attr("aria-controls");
                currentTabName = currentTabName.replace("Tab", "");
                currentTabName = this.capitaliseFirstLetter(currentTabName);

                if (component.attr("data-kendo-tab") !== currentTabName) {
                    return;
                }
            }
            // Check if the component exists.
            if (component[componentName] && typeof component[componentName] === "function") {
                component[componentName]("trigger", "change");
            } else {
                console.error(`Method ${componentName} does not exist on componentSelector.`);
            }
        });
    }

    bindEvents() {
        // Bind the save button.
        // (Currently used as a debugging button, will be replaced with the proper functionality.)
        // (To test saving the configuration, press ctrl + s.)
        $("#saveButtonWtsConfiguration").on("click", this.getCurrentSettings.bind(this));
    }

    onCreateButtonClick(e) {
        // Clear the input fields.
        let tabStrip = $("#tabStripConfiguration").data("kendoTabStrip");
        let currentTab = tabStrip.select();
        let currentTabName = $(currentTab).attr("aria-controls").toLowerCase();
        currentTabName = currentTabName.replace("tab", "");

        // Find the corresponding grid in the corresponding KendoFields array.
        let grid = this[`${currentTabName}KendoFields`].find((grid) => {
            return grid.element[0].getAttribute("name") === e.target.getAttribute("for-list");
        });

        let gridName = grid.element[0].getAttribute("name");

        this.clearInputFieldsForTab(currentTabName, gridName);
        // Clear the selected item
        grid.clearSelection();

        // Fire any change events that are set.
        this.fireAllChangeEvents();
    }

    onSaveButtonClick(e) {

        // Get the current tab name.
        let tabStrip = $("#tabStripConfiguration").data("kendoTabStrip");
        let currentTab = tabStrip.select();
        let currentTabName = $(currentTab).attr("aria-controls").toLowerCase();
        currentTabName = currentTabName.replace("tab", "");

        this.editorSql.forEach((editor) => {
            editor.original.value = editor.editor.getValue();
        })

        // Find the corresponding grid in the corresponding KendoFields array.
        const currentTabFields = this[`${currentTabName}KendoFields`];
        let grid = currentTabFields.find((grid) => {
            return grid.element[0].getAttribute("name") === e.target.getAttribute("for-list");
        });

        // Get the selected item.
        const selectedRow = grid.select();
        let selectedItem = grid.dataItem(selectedRow);

        // Find the name of the grid.
        let gridName = grid.element[0].getAttribute("name");
        let newItem = false;
        // Get all the input fields for the given tab.
        let inputFields = this[`${currentTabName}InputFields`];


        let newActionId = this.GetActionIdFromCurrentInputfields(inputFields, gridName);
        const regex = /^\d+-\d+$/;

        if (selectedItem == null || (selectedItem["actionId"] && selectedItem["actionId"] !== newActionId)) {

            if (!regex.test(newActionId) && newActionId !== null) {
                kendo.alert("De action id is niet correct");
                return;
            }

            let currentActionIds = this.generateListOfActionsIds();
            if (currentActionIds.includes(newActionId)) {
                kendo.alert("De action id van dit item bestaat al. Zorg combinatie tussen de order en timeId uniek is.");
                return;
            }
        }
        if (selectedItem == null) {
            selectedItem = {};
            newItem = true;
        }

        // Loop through all the input fields.
        for (const inputField of inputFields) {
            const groupName = inputField.closest("div.group").dataset.groupName;

            if (this.checkTraceValueForNearestGrid(inputField, currentTabFields) !== gridName) {
                continue;
            }
            // Get the name of the input field.
            let name = inputField.getAttribute("name");

            // Convert the name to camelCase.
            name = this.uncapitalizeFirstLetter(name);

            // Get the value of the input field.
            let value = this.getValueOfElement(inputField);

            // Convert value to the correct type if possible.
            if (inputField.getAttribute("data-kendo-component") === "KendoNumericTextBox") {
                value = parseInt(value);
            }
            if (name === "actionId") {
                let newActionId = `${selectedItem.timeId}-${selectedItem.order}`
                this.setValueOfElement(inputField, newActionId);
                selectedItem[name] = newActionId;
            } else {
                // Set the value of the selected item.
                let path = this.GetTracePathToNearestGridExcludingTheGridComponent(inputField, currentTabFields);
                path.reverse();
                path.push(name)
                this.setNestedValue(selectedItem, path, value);

            }

        }
        if (newItem) {
            let idProp = this.uncapitalizeFirstLetter(`${grid.element[0].getAttribute("id-property")}`)
            if (!selectedItem[`${idProp}`]) {
                selectedItem[`${idProp}`] = crypto.randomUUID();
            }
            if (grid.dataSource.total() > 0) {
                grid.dataSource.add(selectedItem);
            } else {
                let schema = {model: {}};
                if (grid) {
                    schema = {
                        model: {
                            id: `${this.uncapitalizeFirstLetter(grid.element[0].getAttribute("id-property"))}`,
                        }
                    }
                }
                let dataSource = new kendo.data.DataSource({
                    data: [selectedItem],
                    schema: schema
                });
                grid.setDataSource(dataSource);

                // Using the second datasource is needed for the following situation to work:
                // - Add an item to an empty grid
                // - Edit the item without first saving and reloading the file
                // Leaving the grids datasource as the first datasource makes it so the value of the item won't actually be updated
                let dataSource2 = new kendo.data.DataSource({
                    data: grid.dataSource.data(),
                    schema: grid.dataSource.options.schema
                });
                grid.setDataSource(dataSource2);
                grid.refresh();
            }
        }

        // Refresh the grid.
        grid.dataSource.read();
        // Store the new data in the global object.
        for (const field of currentTabFields) {
            const fieldName = this.uncapitalizeFirstLetter(field.element.attr("name"));
            if (!field.element || field.element.data("kendoComponent") !== "KendoGrid" || !fieldName || !this.template.hasOwnProperty(fieldName)) {
                continue;
            }

            this.template[fieldName] = this.objectToArray(field.dataSource.data());
        }
        // Clear the input fields.
        this.clearInputFieldsForTab(currentTabName, gridName);
        grid.clearSelection();
        // Fire any change events that are set.
        this.fireAllChangeEvents();
        this.updateAllDropDownLists();
    }

    onDeleteButtonClick(e) {
        // Get the current tab name.
        let tabStrip = $("#tabStripConfiguration").data("kendoTabStrip");
        let currentTab = tabStrip.select();
        let currentTabName = $(currentTab).attr("aria-controls").toLowerCase();
        currentTabName = currentTabName.replace("tab", "");

        // Find the corresponding grid in the corresponding KendoFields array.
        let grid = this[`${currentTabName}KendoFields`].find((grid) => {
            return grid.element[0].getAttribute("name") === e.target.getAttribute("for-list");
        });

        // Get the selected item.
        const selectedRow = grid.select();
        let selectedItem = grid.dataItem(selectedRow);

        // Find the name of the grid.
        let gridName = grid.element[0].getAttribute("name");

        // Convert the name to camelCase.
        gridName = this.uncapitalizeFirstLetter(gridName);

        // Get all the input fields for the given tab.
        let inputFields = this[`${currentTabName}InputFields`];

        // Get the datasource of the grid.
        let dataSource = this.template[gridName];

        // Get the index of the selected item.
        let indexOfSelectedItem = grid.dataSource.indexOf(selectedItem);

        // Remove the item from the data source.
        var parent = selectedItem.parent();
        delete parent[`${indexOfSelectedItem}`];
        parent = this.repairObjectArray(parent)

        // Refresh the grid.
        grid.dataSource.read();

        // Clear the input fields.
        this.clearInputFieldsForTab(currentTabName, this.capitaliseFirstLetter(gridName));
        // Fire any change events that are set.
        this.fireAllChangeEvents();

        this.UpdateTemplateCurrentTab();
    }

    //The object used for grid data that is like an object acting as an array from kendo input fields. Use this function immediately after removing an item from such an object to avoid errors related to the indexes/lengths not matching up.
    repairObjectArray(obj) {
        let oldLength = obj.length;
        let OldIndex = 0
        let newIndex = 0
        let newArray = []
        while (OldIndex <= oldLength) {

            if (obj[OldIndex] === undefined) {
                OldIndex++
                continue
            }
            newArray.push(obj[OldIndex]);
            delete obj[OldIndex];
            OldIndex++
        }
        newArray.forEach((item) => {
            obj[newIndex] = item;
            newIndex++;
        })
        obj.length = newArray.length;
        return obj
    }

    //Converts objects to arrays.
    //Only works with a specific object structure the Kendo components sometimes use.
    objectToArray(obj) {
        let array = [];
        if (!obj.length) {
            return
        }
        obj = this.repairObjectArray(obj);

        for (let i = 0; i < obj.length; i++) {
            array[i] = obj[i];
        }
        return array;
    }

    clearInputFieldsForTab(tab, groupname = undefined) {
        // Find all the input fields for the given tab.
        let inputFields = this[`${tab}InputFields`];
        // Loop through all the input fields.
        inputFields.forEach((inputField) => {
            if (groupname !== undefined) {
                if (!this.checkIfInputFieldTraceContainsValue(inputField, groupname)) {
                    return;
                }
            }
            // If the input field is a dropdownlist and is required, set the value to the first item.
            // Only run this if statement if .getAttribute is a function.
            if (typeof inputField.getAttribute !== "function") {
                return;
            }
            if (inputField.getAttribute("data-kendo-component") === "KendoDropDownList" && inputField.getAttribute("is-required") === "true") {
                let dropDownList = $(inputField).data("kendoDropDownList");
                let options = dropDownList.dataSource.data();
                this.setValueOfElement(inputField, options[0].value);
                return;
            }

            // Clear the value of the input field.
            this.setValueOfElement(inputField, "");
        });
        // Clear the value of any CodeMirror instance.
        this.editorSql.forEach((editor) => {
            editor.editor.getDoc().setValue("");
        })

    }

    onListChange(e) {
        // Check if the selected item is null.
        if (e.sender.select() === null) {
            return;
        }

        // Retrieve the selected item.
        let selectedItem = e.sender.dataItem(e.sender.select());
        let gridName = e.sender.element[0].getAttribute("name")
        // Get the current tab name.
        let tabStrip = $("#tabStripConfiguration").data("kendoTabStrip");
        let currentTab = tabStrip.select();
        let currentTabName = $(currentTab).attr("aria-controls").toLowerCase();

        currentTabName = currentTabName.replace("tab", "");

        this.clearInputFieldsForTab(currentTabName, gridName);

        let inputFields = this[`${currentTabName}InputFields`];

        // Set the values of the input fields.
        this.findAndSetValuesOfInputFields(selectedItem, inputFields, gridName);

        // Fire any change events that are set.
        this.fireAllChangeEvents(true);

        //Firing the change event might cause changes in the fields active on screen. so this function is called a seconds time to make sure these inputs are also properly loaded
        this.findAndSetValuesOfInputFields(selectedItem, inputFields, gridName);

        //Update the values of any CodeMirror instances.
        this.editorSql.forEach((editor) => {
            editor.editor.getDoc().setValue(editor.original.value);
        })
    }

    findAndSetValuesOfInputFields(obj, inputFields, gridname = undefined) {
        for (let prop in obj) {
            if (typeof obj[prop] === "object") {
                if (obj[prop].length && obj[prop].length > 0) {
                    this.findAndSetValuesOfInputFields(obj[prop], inputFields, gridname);

                    inputFields.forEach((inputField) => {

                        let name = inputField.getAttribute("name");
                        name = this.uncapitalizeFirstLetter(name);

                        if (prop === name) {
                            if (gridname !== undefined) {

                                let tabStrip = $("#tabStripConfiguration").data("kendoTabStrip");
                                let currentTab = tabStrip.select();
                                let currentTabName = $(currentTab).attr("aria-controls").toLowerCase();
                                currentTabName = currentTabName.replace("tab", "");

                                const currentTabFields = this[`${currentTabName}KendoFields`];

                                if (this.checkTraceValueForNearestGrid(inputField, currentTabFields) === gridname) {
                                    this.setValueOfElement(inputField, obj[prop]);
                                }
                            } else {
                                this.setValueOfElement(inputField, obj[prop]);
                            }
                        }
                    });
                } else {
                    //Avoids the "defaults" in the prototype to prevent unwanted resets.
                    if (prop !== "defaults") {
                        this.findAndSetValuesOfInputFields(obj[prop], inputFields, gridname);
                    }
                }

            } else {
                inputFields.forEach((inputField) => {

                    let name = inputField.getAttribute("name");

                    // Convert the name to camelCase.
                    name = this.uncapitalizeFirstLetter(name);

                    if (prop === name) {
                        if (gridname !== undefined) {

                            let tabStrip = $("#tabStripConfiguration").data("kendoTabStrip");
                            let currentTab = tabStrip.select();
                            let currentTabName = $(currentTab).attr("aria-controls").toLowerCase();
                            currentTabName = currentTabName.replace("tab", "");

                            const currentTabFields = this[`${currentTabName}KendoFields`];

                            if (this.checkTraceValueForNearestGrid(inputField, currentTabFields) === gridname) {
                                this.setValueOfElement(inputField, obj[prop]);
                            }
                        } else {
                            this.setValueOfElement(inputField, obj[prop]);
                        }
                    }

                });

            }
        }
    }

    onDependFieldChange(e) {
        // Get the selected item.
        let selectedItem = (e.sender.dataItem(e.sender.select())).value;

        let tab = e.sender.element.attr("data-kendo-tab")
        // Find all fields that depend on this field.
        let dependantFields = document.querySelectorAll(`[data-depend-on-field="${e.sender.element[0].getAttribute("name")}"]`);

        // Loop through all the fields.
        dependantFields.forEach((dependantField) => {

            if (tab === dependantField.getAttribute("data-kendo-tab")) {
                // Get all the dependant values for this field.
                let dependantValues = dependantField.getAttribute("data-depend-on-value");

                // Split the values on a comma.
                dependantValues = dependantValues.split(",");

                // Check if the selected item is in the dependent values.
                if (dependantValues.includes(selectedItem)) {
                    // Show the field.
                    this.showField(dependantField.getAttribute("data-kendo-component"), $(dependantField));
                } else {
                    // Hide the field.
                    this.hideField(dependantField.getAttribute("data-kendo-component"), $(dependantField));
                    // Clear the value of the field.
                    this.setValueOfElement(dependantField, "");
                }
            }
        });
    }

    setValueOfElement(inputField, value) {
        if (inputField === undefined || inputField === null) {
            return;
        }

        let type = inputField.getAttribute("data-kendo-component");

        switch (type) {
            case "KendoCheckBox":
                ($(inputField).data("kendoCheckBox")).value(value);
                break;
            case "KendoTimePicker":
                inputField.value = value; // Using the value function from kendo doesn't set the correct value. (Cuts off the last 2 digits)
                break;
            case "KendoNumericTextBox":
                ($(inputField).data("kendoNumericTextBox")).value(value);
                break;
            case "KendoTextBox":
                ($(inputField).data("kendoTextBox")).value(value);
                break;
            case "TextArea":
                inputField.value = value;
                break;
            case "KendoDropDownList":
                ($(inputField).data("kendoDropDownList")).value(value);
                break;
            case "KendoGrid":
                if (value === undefined) {
                    break;
                }
                let dataSource = new kendo.data.DataSource({
                    data: value,
                    schema: ($(inputField).data("kendoGrid")).dataSource.options.schema
                });
                ($(inputField).data("kendoGrid")).setDataSource(dataSource);
                ($(inputField).data("kendoGrid")).refresh();
                break;
            default:
                inputField.value = value;
                break;
        }
    }

    hideField(type, element) {

        switch (type) {
            case "KendoCheckBox":
            case "KendoTimePicker":
            case "KendoNumericTextBox":
            case "KendoTextBox":
            case "TextArea":
            case "KendoDropDownList":
                element.closest(".item").hide();
                break;
        }
    }

    showField(type, element) {

        switch (type) {
            case "KendoCheckBox":
            case "KendoTimePicker":
            case "KendoNumericTextBox":
            case "KendoTextBox":
            case "TextArea":
            case "KendoDropDownList":
                element.closest(".item").show();
                break;
        }
    }

    addFieldToData(data, name, value, trace) {
        const traceParts = trace.split('/').filter(part => part !== ''); // Split the trace on '/' and remove empty parts.
        let currentObj = data;

        traceParts.forEach(part => {
            if (!currentObj[part]) {
                currentObj[part] = {}; // Create the object if it doesn't exist.
            }
            currentObj = currentObj[part]; // Go to the next object.
        });

        currentObj[name] = value; // Add the value to the object.
        return data;
    }

    getCurrentSettings() {
        let data = {};

        // Get all the values from the service input fields.
        this.serviceInputFields.forEach((inputField) => {
            let name = inputField.getAttribute("name");
            let trace = inputField.getAttribute("trace");
            let value = this.getValueOfElement(inputField);

            if (trace) {
                // If there is a trace, add the field to the data based on the trace.
                data = this.addFieldToData(data, name, value, trace);
            } else {
                // If there is no trace, add the field to the data.
                data[name] = value;
            }
        });

        this.correctValues(this.template.runSchemes, this.timersInputFields);

        // Manually add RunSchemes to the data.
        data["RunSchemes"] = this.template.runSchemes;
        data["Queries"] = this.template.queries;
        data["HttpApis"] = this.template.httpApis;
        return data;
    }

    // Recursively search through all the values of given object and corresponding inputfields and correct the values.
    // This is needed because the values are not always the correct type.
    // For example: the value of a dropdown is a empty string instead of being undefined.
    correctValues(obj, inputFields) {
        for (let prop in obj) {
            if (typeof obj[prop] === "object") {
                this.correctValues(obj[prop], inputFields);
            } else {
                inputFields.forEach((inputField) => {
                    let name = inputField.getAttribute("name");

                    // Convert the name to camelCase
                    name = this.uncapitalizeFirstLetter(name);

                    if (prop === name) {
                        // Check if value is undefined
                        if (obj[prop] === undefined || obj[prop] === null || obj[prop] === "") {
                            delete obj[prop];
                        }
                    }
                });
            }
        }
    }

    getValueOfElement(element) {
        //check if element is a grid
        var grid = $(element).data("kendoGrid")
        if (grid !== undefined) {
            return (grid).dataSource.data();
        }

        //If the element isn't a grid. Check what type of element it is, and return it's value
        switch (element.tagName) {
            case "INPUT":
            case "TEXTAREA":
                switch (element.type) {
                    case "checkbox":
                        return element.checked;
                    default:
                        return element.value;
                }
            case "SELECT":
                return element.value;
            default:
                return null;
        }
    }

    capitaliseFirstLetter(string) {
        return `${string.charAt(0).toUpperCase()}${string.slice(1)}`;
    }

    uncapitalizeFirstLetter(string) {
        return !string ? string : `${string.charAt(0).toLowerCase()}${string.slice(1)}`;
    }

    //Check if a inputField's trace value contains a string value.
    checkIfInputFieldTraceContainsValue(inputField, Value) {
        let trace = inputField.getAttribute("trace");
        let traceList = trace.split("/")
        if (traceList.includes(Value)) {
            return true;
        }
        return false;
    }

    GetActionIdFromCurrentInputfields(inputFields, gridname) {

        let OrderValue = 0
        let TimeValue = 0
        let finished = -2

        for (const inputField of inputFields) {
            const groupName = inputField.closest("div.group").dataset.groupName;

            if (!this.checkIfInputFieldTraceEndsWithValue(inputField, gridname)) {
                continue;
            }

            // Get the name of the input field.
            let name = inputField.getAttribute("name");

            // Convert the name to camelCase.
            name = this.uncapitalizeFirstLetter(name);

            if (name === "order") {
                OrderValue = this.getValueOfElement(inputField);
                finished++
            }
            if (name === "timeId") {
                TimeValue = this.getValueOfElement(inputField);
                finished++
            }
        }
        if (finished !== 0) {
            return null
        }
        return `${TimeValue}-${OrderValue}`
    }

    generateListOfActionsIds() {
        let actionIds = []
        this.checkItemForActionsIds(actionIds, this.template);
        return actionIds;
    }

    // Checks all Action IDs in an object.
    // Needed since Action IDs need to be unique, even between a query and an HTTP API call, for example.

    checkItemForActionsIds(actionIds, currentobject) {
        if (typeof currentobject === "object") {
            for (let prop in currentobject) {
                this.checkItemForActionsIds(actionIds, currentobject[prop]);
            }
            if ('timeId' in currentobject && 'order' in currentobject) {
                let newActionId = `${currentobject.actionid}`;
                actionIds.push(newActionId);
            }
        }

    }


    //Check if a inputField's trace value Ends with a string value.
    checkIfInputFieldTraceEndsWithValue(inputField, Value) {
        let trace = inputField.getAttribute("trace");
        let traceList = trace.split("/")
        let finalTrace = traceList[traceList.length - 1];
        if (finalTrace === Value) {
            return true;
        }
        return false;
    }

    checkTraceValueForNearestGrid(inputField, Tabfields) {
        let trace = inputField.getAttribute("trace");
        let traceList = trace.split("/")
        let done = false
        let index = 0;
        while (!done) {
            index = index + 1;
            var reverseIndex = traceList.length - index
            if (reverseIndex <= 0) {//First item of the array is always empty there for 0 does not need to be checked
                done = true;
            } else {
                let traceItem = traceList[traceList.length - index];

                let grid = Tabfields.find((grid) => {
                    return grid.element[0].getAttribute("name") === traceItem;
                });
                if (grid !== undefined) {
                    return traceItem
                }
            }
        }
        return null;
    }

    GetTracePathToNearestGridExcludingTheGridComponent(inputField, Tabfields) {
        let trace = inputField.getAttribute("trace");
        let traceList = trace.split("/")
        let done = false
        let index = 0;
        let returnTrace = [];
        while (!done) {
            index = index + 1;
            var reverseIndex = traceList.length - index
            if (reverseIndex <= 0) {//First item of the array is always empty there for 0 does not need to be checked
                done = true;
            } else {
                let traceItem = traceList[traceList.length - index];

                let grid = Tabfields.find((grid) => {
                    return grid.element[0].getAttribute("name") === traceItem;
                });

                if (grid !== undefined) {
                    done = true;
                } else {
                    returnTrace.push(traceItem);
                }
            }
        }
        return returnTrace
    }


    updateAllDropDownLists() {
        this.updateDropDownLists("queries");
        this.updateDropDownLists("httpapis");
        this.updateDropDownLists("service");
        this.updateDropDownLists("timers");
    }

    updateDropDownLists(TabName) {

        // Get all the input fields for the given tab.
        let inputFields = this[`${TabName}InputFields`];

        for (const inputField of inputFields) {

            if (inputField.getAttribute("data-kendo-component") !== "KendoDropDownList") {
                continue;
            }

            let value = inputField.getAttribute("drop-down-list-data-variable-name");
            let path = inputField.getAttribute("drop-down-list-data-source");
            if (!value || !path) {
                continue;
            }
            let valueArray = []
            this.GetValuesWithName(valueArray, this.template[path], value);

            let dataSource = new kendo.data.DataSource({});

            for (const item of valueArray) {
                dataSource.add({text: `${item}`, value: `${item}`});

            }

            ($(inputField).data("kendoDropDownList")).setDataSource(dataSource);

        }

    }

    GetValuesWithName(returnArray, Object, valuename) {
        if (typeof Object === "object") {
            for (let prop in Object) {
                if (Object.hasOwnProperty(prop)) {
                    this.GetValuesWithName(returnArray, Object[prop], valuename);
                }
            }
            if (valuename in Object) {
                let newValue = Object[valuename];
                returnArray.push(newValue);
            }
        }
    }

    UpdateTemplateCurrentTab() {
        let tabStrip = $("#tabStripConfiguration").data("kendoTabStrip");
        let currentTab = tabStrip.select();
        let currentTabName = $(currentTab).attr("aria-controls").toLowerCase();
        currentTabName = currentTabName.replace("tab", "");
        const currentTabFields = this[`${currentTabName}KendoFields`];

        for (const field of currentTabFields) {
            const fieldName = this.uncapitalizeFirstLetter(field.element.attr("name"));
            if (!field.element || field.element.data("kendoComponent") !== "KendoGrid" || !fieldName || !this.template[fieldName]) {
                continue;
            }

            this.template[fieldName] = this.objectToArray(field.dataSource.data());
        }
    }

    setNestedValue(obj, path, value) {
        let current = obj;
        for (let i = 0; i < path.length - 1; i++) {
            let key = this.uncapitalizeFirstLetter(path[i]);
            // Create nested objects if they don't exist
            if (!(key in current) || typeof current[key] !== 'object') {
                current[key] = {};
            }
            current = current[key];
        }
        current[this.uncapitalizeFirstLetter(path[path.length - 1])] = value;
    }

    /**
     * Shows or hides the main (full screen) loader.
     * @param {boolean} show True to show the loader, false to hide it.
     */
    toggleMainLoader(show) {
        this.mainLoader.toggleClass("loading", show);
    }
}
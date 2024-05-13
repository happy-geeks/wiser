import {Wiser} from "../../Base/Scripts/Utils.js";
import "../../Base/Scripts/Processing.js";
import "../css/WtsConfiguration.css";

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
require("@progress/kendo-ui/js/kendo.notification.js");
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
    }

    async reloadWtsConfigurationTab(id) {
        // Empty the tab
        document.getElementById("wtsConfigurationTab").innerHTML = "";

        // Empty all the arrays
        this.serviceInputFields = [];
        this.serviceKendoFields = [];
        this.timersInputFields = [];
        this.timersKendoFields = [];

        // Check to see if id is set
        if (id === undefined || id === null || id === 0) {
            console.error("id is not set");
            return;
        }

        // Tell the user that the tab is loading
        this.base.toggleMainLoader(true);

        let templateSettings = null;

        // Get the data from the api
        try {
            templateSettings = await Wiser.api({
                url: `${this.base.settings.wiserApiRoot}templates/${id}/wtsconfiguration`,
                dataType: "json",
                method: "GET"
            });
            this.template = templateSettings;
            console.clear();
            console.log(templateSettings);
        }
        catch (e) {
            console.error(e);
            this.base.toggleMainLoader(false); // Hide the loader
            kendo.alert("Er is iets fout gegaan. Sluit a.u.b. deze module, open deze daarna opnieuw en probeer het vervolgens opnieuw. Of neem contact op als dat niet werkt.");
            return;
        }

        // Build the view
        try {
            await Wiser.api({
                method: "POST",
                contentType: "application/json",
                url: "/Modules/Templates/WtsConfigurationTab",
                data: JSON.stringify(templateSettings)
            }).then(async (response) => {
                this.base.toggleMainLoader(false); // Hide the loader
                document.getElementById("wtsConfigurationTab").innerHTML = response; // Add the html to the tab
                $("#tabStripConfiguration").kendoTabStrip().data("kendoTabStrip"); // Initialize the tabstrip
            })
        }
        catch (e) {
            console.error(e);
            this.base.toggleMainLoader(false); // Hide the loader
            kendo.alert("Er is iets fout gegaan. Sluit a.u.b. deze module, open deze daarna opnieuw en probeer het vervolgens opnieuw. Of neem contact op als dat niet werkt.");
        }

        this.initializeKendoComponents();
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
            let componentName = `kendo${component.attr("data-kendo-component")}`;
            let componentTab = component.attr("data-kendo-tab");
            let componentOptions = component.attr("data-kendo-options");

            // Check if the options are set
            if (componentOptions === undefined || componentOptions === null || componentOptions === "") {
                componentOptions = {};
            } else {
                componentOptions = JSON.parse(componentOptions);
                // Check if dataSource is set, if so make it a object instead of a string to assign it to the component
                if (componentOptions.dataSource) {
                    componentOptions.dataSource = eval(componentOptions.dataSource);
                }
                // Check if a change event is set, if so make it a function
                if (componentOptions.change) {
                    componentOptions.change = eval(componentOptions.change);
                }
            }

            // Add a list change event to the grid if it allows editing
            if (component.attr("data-kendo-component") === "Grid" && component.attr("allow-edit") === "true") {
                componentOptions.change = eval("this.onListChange.bind(this)");
            }

            // Check if the component has UseDataSource set to true
            if (component.attr("use-datasource") === "true") {
                // Find the name of the property
                let propertyName = this.uncapitalizeFirstLetter(component.attr("name"));
                // Set the dataSource to the correct property
                componentOptions.dataSource = eval(`this.template.${propertyName}`);
            }

            // Check if any other field depends on this field, if so add a change event
            let isDependedOn = document.querySelectorAll(`[data-depend-on-field="${component.attr("name")}"]`);
            if (isDependedOn.length > 0) {
                // Add a event listener to the options
                componentOptions.change = eval("this.onDependFieldChange.bind(this)");
                // Add an attribute to the component to indicate that it is depended on
                component.attr("data-is-depended-on", true);
            }

            // Make sure the method exists on componentSelector and if so create the component
            if (component[componentName] && typeof component[componentName] === "function") {
                let newComponent = component[componentName](componentOptions).data(componentName);
                // Save the component and field so we can access it later
                switch (componentTab) {
                    case "Service":
                        this.serviceInputFields.push(component[0]);
                        this.serviceKendoFields.push(newComponent);
                        break;
                    case "Timers":
                        this.timersInputFields.push(component[0]);
                        this.timersKendoFields.push(newComponent);
                        break;
                }
            } else {
                console.error(`Method ${componentName} does not exist on componentSelector.`);
                return;
            }

            // Check if the component allows editing
            if (component.attr("allow-edit") === "true") {
                // Find the corresponding buttons and attach click events
                let createButton = $(`#${component.attr('name')}CreateButton`);
                let saveButton = $(`#${component.attr('name')}SaveButton`);
                let deleteButton = $(`#${component.attr('name')}DeleteButton`);

                // Check if the buttons exist
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

        // Loading is done, fire all change events
        this.fireAllChangeEvents();
    }

    fireAllChangeEvents() {
        // Fire any change events that are set
        let elementsWithChangeEvent = document.querySelectorAll('[data-is-depended-on]');

        elementsWithChangeEvent.forEach((changeEvent) => {
            let component = $(changeEvent);
            let componentName = `kendo${component.attr("data-kendo-component")}`;

            // Check if the component exists
            if (component[componentName] && typeof component[componentName] === "function") {
                component[componentName]("trigger", "change");
            } else {
                console.error(`Method ${componentName} does not exist on componentSelector.`);
            }
        });
    }

    bindEvents() {
        // Bind the save button
        // (Currently used as a debugging button, will be replaced with the proper functionality)
        // (To test saving the configuration, press ctrl + s)
        $("#saveButtonWtsConfiguration").on("click", this.getCurrentSettings.bind(this));
    }

    onCreateButtonClick(e) {
        // Clear the input fields
        let tabStrip = $("#tabStripConfiguration").data("kendoTabStrip");
        let currentTab = tabStrip.select();
        let currentTabName = $(currentTab).attr("aria-controls").toLowerCase();
        currentTabName = currentTabName.replace("tab", "");

        this.clearInputFieldsForTab(currentTabName);

        // Find the corresponding grid in the corresponding KendoFields array
        let grid = this[`${currentTabName}KendoFields`].find((grid) => {
            return grid.element[0].getAttribute("name") === e.target.getAttribute("for-list");
        });

        // Clear the selected item
        grid.clearSelection();

        // Find the id field related to the grid and set an according value
        let idField = grid.element[0].getAttribute("id-property");
        if (idField !== undefined && idField !== null) {
            // Get the id field
            let idFieldElement = $(`[name="${idField}"]`);

            // Check for the highest id in the datasource and add 1 to it
            let dataSource = this.template[this.uncapitalizeFirstLetter(grid.element[0].getAttribute("name"))];
            let highestId = 0;
            dataSource.forEach((item) => {
                if (item[this.uncapitalizeFirstLetter(idField)] > highestId) {
                    highestId = item[this.uncapitalizeFirstLetter(idField)];
                }
            });
            highestId++;

            // Set the value of the id field
            this.setValueOfElement(idFieldElement[0], highestId);
        }

        // Fire any change events that are set
        this.fireAllChangeEvents();
    }

    onSaveButtonClick(e) {
        // Get the current tab name
        let tabStrip = $("#tabStripConfiguration").data("kendoTabStrip");
        let currentTab = tabStrip.select();
        let currentTabName = $(currentTab).attr("aria-controls").toLowerCase();
        currentTabName = currentTabName.replace("tab", "");

        // Find the corresponding grid in the corresponding KendoFields array
        let grid = this[`${currentTabName}KendoFields`].find((grid) => {
            return grid.element[0].getAttribute("name") === e.target.getAttribute("for-list");
        });

        // Get the selected item
        let selectedItem = grid.dataItem(grid.select());

        // Find the name of the grid
        let gridName = grid.element[0].getAttribute("name");

        // Convert the name to camelCase
        gridName = this.uncapitalizeFirstLetter(gridName);

        // Get the datasource of the grid
        let dataSource = this.template[gridName];

        // The index of the selected item in the datasource
        let indexOfSelectedItem = null;

        // If there is a selected item, save the index of the selected item
        if (selectedItem !== undefined && selectedItem !== null) {
            indexOfSelectedItem = grid.dataSource.indexOf(selectedItem);
        }

        selectedItem = {};

        // Get all the input fields for the given tab
        let inputFields = this[`${currentTabName}InputFields`];

        // Loop through all the input fields
        inputFields.forEach((inputField) => {
            // Get the name of the input field
            let name = inputField.getAttribute("name");

            // Ignore field if it is a grid
            if (inputField.getAttribute("data-kendo-component") === "Grid") {
                return;
            }

            // Convert the name to camelCase
            name = this.uncapitalizeFirstLetter(name);

            // Get the value of the input field
            let value = this.getValueOfElement(inputField);

            // Convert value to the correct type if possible
            if (inputField.getAttribute("data-kendo-component") === "NumericTextBox") {
                value = parseInt(value);
            }

            // Find the trace of the input field
            let trace = inputField.getAttribute("trace");

            // Remove the first part of the trace (the name of the grid)
            trace = trace.replace(`/${this.capitaliseFirstLetter(gridName)}`, "");

            // If there is a trace, add the field to the data based on the trace
            if (trace) {
                selectedItem = this.addFieldToData(selectedItem, name, value, trace);
                return;
            }

            // Set the value of the selected item
            selectedItem[name] = value;
        });

        // Error checking for using an id that is already in use
        if (indexOfSelectedItem === null) {
            // For new items, check if the ID is already in use
            let idField = grid.element[0].getAttribute("id-property");
            let idFieldElement = $(`[name="${idField}"]`);
            let idFieldValue = parseInt(this.getValueOfElement(idFieldElement[0]));

            if (dataSource.find((item) => {
                return item[this.uncapitalizeFirstLetter(idField)] === idFieldValue;
            })) {
                kendo.alert("Het ingevulde ID is al in gebruik. Kies een ander, uniek ID om de wijzigingen op te slaan.");
                return;
            }
        }
        else {
            // For existing items, check if the ID is already in use by another item
            let idField = grid.element[0].getAttribute("id-property");
            let idFieldElement = $(`[name="${idField}"]`);
            let idFieldValue = parseInt(this.getValueOfElement(idFieldElement[0]));

            if (dataSource.find((item) => {
                return item[this.uncapitalizeFirstLetter(idField)] === idFieldValue && item !== dataSource[indexOfSelectedItem];
            })) {
                kendo.alert("Het ingevulde ID is al in gebruik. Kies een ander, uniek ID om de wijzigingen op te slaan.");
                return;
            }
        }

        if (indexOfSelectedItem != null && indexOfSelectedItem >= 0) {
            dataSource[indexOfSelectedItem] = selectedItem; // Update the item
        }
        else {
            dataSource.push(selectedItem); // Add the item
        }

        // Refresh the grid
        grid.dataSource.read();

        // Clear the input fields
        this.clearInputFieldsForTab(currentTabName);

        // Fire any change events that are set
        this.fireAllChangeEvents();
    }

    onDeleteButtonClick(e) {
        // Get the current tab name
        let tabStrip = $("#tabStripConfiguration").data("kendoTabStrip");
        let currentTab = tabStrip.select();
        let currentTabName = $(currentTab).attr("aria-controls").toLowerCase();
        currentTabName = currentTabName.replace("tab", "");

        // Find the corresponding grid in the corresponding KendoFields array
        let grid = this[`${currentTabName}KendoFields`].find((grid) => {
            return grid.element[0].getAttribute("name") === e.target.getAttribute("for-list");
        });

        // Get the selected item
        let selectedItem = grid.dataItem(grid.select());

        if (selectedItem === undefined || selectedItem === null) {
            return;
        }

        // Find the name of the grid
        let gridName = grid.element[0].getAttribute("name");

        // Convert the name to camelCase
        gridName = this.uncapitalizeFirstLetter(gridName);

        // Get the datasource of the grid
        let dataSource = this.template[gridName];

        // Get the index of the selected item
        let indexOfSelectedItem = grid.dataSource.indexOf(selectedItem);

        // Remove the item from the datasource
        if (indexOfSelectedItem !== -1) {
            dataSource.splice(indexOfSelectedItem, 1);
        }
        else {
            console.error("Could not find the selected item in the datasource.");
        }

        // Refresh the grid
        grid.dataSource.read();

        // Clear the input fields
        this.clearInputFieldsForTab(currentTabName);

        // Fire any change events that are set
        this.fireAllChangeEvents();
    }

    clearInputFieldsForTab(tab) {
        // Find all the input fields for the given tab
        let inputFields = this[`${tab}InputFields`];

        // Loop through all the input fields
        inputFields.forEach((inputField) => {
            // If the input field is a dropdownlist and is required, set the value to the first item
            // Only run this if statement if .getAttribute is a function
            if (typeof inputField.getAttribute !== "function") {
                return;
            }
            if (inputField.getAttribute("data-kendo-component") === "DropDownList" && inputField.getAttribute("is-required") === "true") {
                let dropDownList = $(inputField).data("kendoDropDownList");
                let options = dropDownList.dataSource.data();
                this.setValueOfElement(inputField, options[0].value);
                return;
            }
            // Clear the value of the input field
            this.setValueOfElement(inputField, "");
        });
    }

    onListChange(e) {
        // Check if the selected item is null
        if (e.sender.select() === null) {
            return;
        }

        // Retrieve the selected item
        let selectedItem = e.sender.dataItem(e.sender.select());

        // Get the current tab name
        let tabStrip = $("#tabStripConfiguration").data("kendoTabStrip");
        let currentTab = tabStrip.select();
        let currentTabName = $(currentTab).attr("aria-controls").toLowerCase();

        currentTabName = currentTabName.replace("tab", "");

        this.clearInputFieldsForTab(currentTabName);

        let inputFields = this[`${currentTabName}InputFields`];

        // Set the values of the input fields
        this.findAndSetValuesOfInputFields(selectedItem, inputFields);

        // Fire any change events that are set
        this.fireAllChangeEvents();
    }

    findAndSetValuesOfInputFields(obj, inputFields) {
        for (let prop in obj) {
            if (typeof obj[prop] === "object") {
                this.findAndSetValuesOfInputFields(obj[prop], inputFields);
            } else {
                inputFields.forEach((inputField) => {
                    let name = inputField.getAttribute("name");

                    // Convert the name to camelCase
                    name = this.uncapitalizeFirstLetter(name);

                    if (prop === name) {
                        this.setValueOfElement(inputField, obj[prop]);
                    }
                });
            }
        }
    }

    onDependFieldChange(e) {
        // Get the selected item
        let selectedItem = (e.sender.dataItem(e.sender.select())).value;

        // Find all fields that depend on this field
        let dependantFields = document.querySelectorAll(`[data-depend-on-field="${e.sender.element[0].getAttribute("name")}"]`);

        // Loop through all the fields
        dependantFields.forEach((dependantField) => {
            // Get all the dependant values for this field
            let dependantValues = dependantField.getAttribute("data-depend-on-value");

            // Split the values on a comma
            dependantValues = dependantValues.split(",");

            // Check if the selected item is in the dependant values
            if (dependantValues.includes(selectedItem)) {
                // Show the field
                this.showField(dependantField.getAttribute("data-kendo-component"), $(dependantField));
            } else {
                // Hide the field
                this.hideField(dependantField.getAttribute("data-kendo-component"), $(dependantField));
                // Clear the value of the field
                this.setValueOfElement(dependantField, "");
            }
        });
    }

    setValueOfElement(e, v) {
        if (e === undefined || e === null) {
            return;
        }

        let type = e.getAttribute("data-kendo-component");

        switch (type) {
            case "CheckBox":
                ($(e).data("kendoCheckBox")).value(v);
                break;
            case "TimePicker":
                e.value = v; // Using the value function from kendo doesn't set the correct value (Cuts off the last 2 digits)
                break;
            case "NumericTextBox":
                ($(e).data("kendoNumericTextBox")).value(v);
                break;
            case "TextBox":
                ($(e).data("kendoTextBox")).value(v);
                break;
            case "DropDownList":
                ($(e).data("kendoDropDownList")).value(v);
                break;
            default:
                e.value = v;
                break;
        }
    }

    hideField(type, element) {
        switch (type) {
            case "CheckBox":
            case "TimePicker":
            case "NumericTextBox":
            case "TextBox":
            case "DropDownList":
                element.closest(".item").hide();
                break;
        }
    }

    showField(type, element) {
        switch (type) {
            case "CheckBox":
            case "TimePicker":
            case "NumericTextBox":
            case "TextBox":
            case "DropDownList":
                element.closest(".item").show();
                break;
        }
    }

    addFieldToData(data, name, value, trace) {
        const traceParts = trace.split('/').filter(part => part !== ''); // Split the trace on / and remove empty parts
        let currentObj = data;

        traceParts.forEach(part => {
            if (!currentObj[part]) {
                currentObj[part] = {}; // Create the object if it doesn't exist
            }
            currentObj = currentObj[part]; // Go to the next object
        });

        currentObj[name] = value; // Add the value to the object
        return data;
    }

    getCurrentSettings() {
        console.log("Saving configuration...");

        let data = {};

        // Get all the values from the service input fields
        this.serviceInputFields.forEach((inputField) => {
            let name = inputField.getAttribute("name");
            let trace = inputField.getAttribute("trace");
            let value = this.getValueOfElement(inputField);

            if (trace) {
                // If there is a trace, add the field to the data based on the trace
                data = this.addFieldToData(data, name, value, trace);
            } else {
                // If there is no trace, add the field to the data
                data[name] = value;
            }
        });

        this.correctValues(this.template.runSchemes, this.timersInputFields);

        // Manually add runschemes to the data
        data["RunSchemes"] = this.template.runSchemes;

        console.log("Data: ", data);

        return data;
    }

    // Recursively search through all the values of given object and corresponding inputfields and correct the values
    // This is needed because the values are not always the correct type
    // For example: the value of a dropdown is a empty string instead of being undefined
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
        // Check what type of element it is
        switch (element.tagName) {
            case "INPUT":
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
        return string.charAt(0).toUpperCase() + string.slice(1);
    }

    uncapitalizeFirstLetter(string) {
        return string.charAt(0).toLowerCase() + string.slice(1);
    }

    /**
     * Shows or hides the main (full screen) loader.
     * @param {boolean} show True to show the loader, false to hide it.
     */
    toggleMainLoader(show) {
        this.mainLoader.toggleClass("loading", show);
    }
}
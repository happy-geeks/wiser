import { Wiser } from "../../Base/Scripts/Utils.js";
import "../../Base/Scripts/Processing.js";

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

import "../css/WtsConfiguration.css";

export class WtsConfiguration {

    /**
     * Initializes a new instance of DynamicContent.
     * @param {any} base The base Template class.
     */
    constructor(base) {
        this.base = base;
        
        this.template = null;
        this.serviceInputFields = [];
    }

    async reloadWtsConfigurationTab(id) {
        // Empty the tab
        document.getElementById("wtsConfigurationTab").innerHTML = "";
        
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
            let componentName = "kendo" + component.attr("data-kendo-component");
            let componentTab = component.attr("data-kendo-tab");
            let componentOptions = component.attr("data-kendo-options");
            
            // Save the component if the tab is Service
            if (componentTab === "Service") {
                this.serviceInputFields.push(component[0]);
            }
            
            // Make sure componentOptions is not null or undefined
            if (componentOptions === undefined || componentOptions === null) {
                componentOptions = {};
            } else {
                componentOptions = JSON.parse(componentOptions);
            }
            
            // Make sure the method exists on componentSelector
            if (component[componentName] && typeof component[componentName] === "function") {
                component[componentName](componentOptions).data(componentName);
            } else {
                console.error(`Method ${componentName} does not exist on componentSelector.`);
            }
        }
    }
    
    bindEvents() {
        // Bind the save button
        $("#saveButtonWtsConfiguration").on("click", this.getCurrentSettings.bind(this));
    }
    
    getCurrentSettings() {
        console.log("Saving configuration...");
        
        let data = {};
        
        // Get all the values from the service input fields
        this.serviceInputFields.forEach((inputField) => {
            let inputFieldName = inputField.getAttribute("name");
            data[inputFieldName] = this.getValueOfElement(inputField);
        });

        console.log("Data: ", data);
        
        return data;
        // const schemaValidator = new Ajv();
        // const isValid = schemaValidator.validate(schema, this.serviceInputFields);
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

    /**
     * Shows or hides the main (full screen) loader.
     * @param {boolean} show True to show the loader, false to hide it.
     */
    toggleMainLoader(show) {
        this.mainLoader.toggleClass("loading", show);
    }
}
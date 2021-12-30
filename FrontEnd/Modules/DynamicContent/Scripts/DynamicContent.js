import { Modules, Dates, Strings, Wiser2, Misc } from "../../Base/Scripts/Utils.js";
require("@progress/kendo-ui/js/kendo.all.js");
require("@progress/kendo-ui/js/cultures/kendo.culture.nl-NL.js");
require("@progress/kendo-ui/js/messages/kendo.messages.nl-NL.js");

import "../css/DynamicContent.css";

// Any custom settings can be added here. They will overwrite most default settings inside the module.
const moduleSettings = {
    
};

((settings) => {
    /**
     * Main class.
     */
    class DynamicContent {

        /**
         * Initializes a new instance of DynamicContent.
         * @param {any} settings An object containing the settings for this class.
         */
        constructor(settings) {
            this.base = this;

            // Kendo components.
            this.mainSplitter = null;
            this.mainTabStrip = null;
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
        }

        /**
         * Initializes all kendo components for the base class.
         */
        initializeKendoComponents() {

            // Main window
            this.mainWindow = $("#window").kendoWindow({
                width: "1500",
                height: "650",
                title: "Dynamic content",
                visible: true,
                actions: ["close"],
                draggable: false
            }).data("kendoWindow").maximize().open();

            // Splitter
            this.mainSplitter = $("#horizontal").kendoSplitter({
                panes: [{
                    collapsible: true,
                    scrollable: false,
                    size: "60%"
                }, {
                    collapsible: false
                }]
            }).data("kendoSplitter");
            this.mainSplitter.resize(true);

            // Tabtrip
            this.mainTabStrip = $(".tabstrip").kendoTabStrip({
                animation: {
                    open: {
                        effects: "fadeIn"
                    }
                }
            }).data("kendoTabStrip");

            //NUBERIC FIELD
            this.mainNumericTextBox = $(".numeric").kendoNumericTextBox();

            // ComboBox
            this.mainComboBox = $(".combo-select").kendoComboBox();

            this.mainComboInput = $(".combo-input").kendoComboBox({
                dataTextField: "text",
                dataValueField: "value",
                dataSource: [{
                    text: "Netherlands",
                    value: "1"
                }, {
                    text: "Belgium",
                    value: "2"
                }, {
                    text: "Germany",
                    value: "3"
                }, {
                    text: "France",
                    value: "4"
                }, {
                    text: "Spain",
                    value: "5"
                }, {
                    text: "United Kingdom",
                    value: "6"
                }, {
                    text: "Italy",
                    value: "7"
                }, {
                    text: "Luxembourg",
                    value: "8"
                }],
                filter: "contains",
                suggest: true,
                index: 3
            });

            //MULTISELECT
            this.mainMultiSelect = $(".multi-select").kendoMultiSelect({
                autoClose: false
            }).data("kendoMultiSelect");

            //DATE PICKER
            this.mainDatePicker = $(".datepicker").kendoDatePicker({
                format: "dd MMMM yyyy",
                culture: "nl-NL"
            }).data("kendoDatePicker");

            $(".datepicker").click(function () {
                this.mainDatePicker.open();
            });

            //DATE & TIME PICKER
            this.mainDateTimePicker = $(".datetimepicker").kendoDateTimePicker({
                value: new Date(),
                dateInput: true,
                format: "dd MMMM yyyy HH:mm",
                culture: "nl-NL"
            }).data("kendoDateTimePicker");

            $("input.datetimepicker").click(function () {
                this.mainDateTimePicker.close("time");
                this.mainDateTimePicker.open("date");
            });

            this.mainDateTimePicker.dateView.options.change = function () {
                this.mainDateTimePicker._change(this.value());
                this.mainDateTimePicker.close("date");
                this.mainDateTimePicker.open("time");
            };
        }
    }


    // Initialize the DynamicItems class and make one instance of it globally available.
    window.DynamicContent = new DynamicContent(settings);
})(moduleSettings);
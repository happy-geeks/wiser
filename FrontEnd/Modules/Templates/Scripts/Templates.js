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
            // Buttons
            $("#addButton").kendoButton({
                icon: "plus"
            });

            $("#saveButton").kendoButton({
                icon: "save"
            });

            $("#deployLive, #deployAccept, #deployTest").kendoButton();

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
                    size: "20%"
                }, {
                    collapsible: false
                }]
            }).data("kendoSplitter");
            this.mainSplitter.resize(true);

            // Treeview 
            this.mainTreeview = $("#treeview").kendoTreeView({
                dragAndDrop: true
            }).data("kendoTreeView");

            // Tabstrip
            this.mainTabStrip = $(".tabstrip").kendoTabStrip({
                animation: {
                    open: {
                        effects: "fadeIn"
                    }
                }
            }).data("kendoTabStrip");

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

            // ComboBox
            this.mainComboBox = $(".combo-select").kendoComboBox();
        }
    }


    // Initialize the DynamicItems class and make one instance of it globally available.
    window.Templates = new Templates(settings);
})(moduleSettings);
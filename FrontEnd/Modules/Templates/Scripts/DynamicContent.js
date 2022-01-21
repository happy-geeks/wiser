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

            await this.initCurrentComponentData();

            this.bindSaveButton();
            this.loadComponentHistory();
            this.transformCodeMirrorViews();
        }

        async initCurrentComponentData() {
            $.ajax({
                type: "GET",
                url: "/DynamicContent/GetComponentAndModeForContentId",
                data: { contentId: 1 },
                success: function (response) {
                    $(window.DynamicContent.mainComboBox[0]).data("kendoComboBox").value(response[0]);
                    window.DynamicContent.ChangeComponent(response[0], response[1]);
                },
            });
        }
        /**
         * Initializes all kendo components for the base class.
         */
        initializeKendoComponents() {
            window.popupNotification = $("#popupNotification").kendoNotification().data("kendoNotification");
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
                    size: "75%"
                }, {
                    collapsible: true
                }]
            }).data("kendoSplitter");
            this.mainSplitter.resize(true);

            // Tabstrip, NUMERIC FIELD, MULTISELECT, Date Picker, DATE & TIME PICKER
            this.intializeDynamicKendoComponents();

            // ComboBox
            this.mainComboBox = $(".combo-select").kendoComboBox();

                //Components
                $("#combo-01").data("kendoComboBox").bind("change", function (e) {
                    window.DynamicContent.ChangeComponent(document.getElementById("combo-01").value);
                });
                // ComponentMode
                $("#combo-02").data("kendoComboBox").bind("change", function (e) {
                    window.DynamicContent.updateComponentModeVisibility(document.getElementById("combo-02").value);
                });

            
        }

        //Initialize the dynamic kendo components. This method will also be called when reloading component fields.
        intializeDynamicKendoComponents() {
            // Tabstrip
            this.mainTabStrip = $(".tabstrip").kendoTabStrip({
                animation: {
                    open: {
                        effects: "fadeIn"
                    }
                }
            }).data("kendoTabStrip").select(0);

            //NUMERIC FIELD
            this.mainNumericTextBox = $(".numeric").kendoNumericTextBox();

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
            if ($(".datepicker").length) {
                this.mainDatePicker = $(".datepicker").kendoDatePicker({
                    format: "dd MMMM yyyy",
                    culture: "nl-NL"
                }).data("kendoDatePicker");

                $(".datepicker").click(function () {
                    this.mainDatePicker.open();
                });
            }

            //DATE & TIME PICKER
            if ($(".datetimepicker").length) {
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

        ChangeComponent(newComponent, newComponentMode) {
            $.ajax({
                type: "GET",
                url: "/DynamicContent/DynamicContentTabPane/" + document.getElementById("wiser").dataset.dcid,
                data: {
                    templateId: document.getElementById("wiser").dataset.dcid,
                    component: newComponent
                },
                success: function (response) {
                    //force reload on component modes
                    window.componentModus = null;

                    $(".k-tabstrip-wrapper").each(function (i, el) { el.innerHTML = response });
                    window.DynamicContent.ReloadComponentModes(newComponent, newComponentMode);
                    window.DynamicContent.intializeDynamicKendoComponents();
                    window.DynamicContent.transformCodeMirrorViews();
                },
                failure: function (response) {
                    window.DynamicContent.DisplayErrorInDynamicContent(response);
                },
                error: function (response) {
                    window.DynamicContent.DisplayErrorInDynamicContent(response);
                },
            });
        }

        ReloadComponentModes(newComponent, newComponentMode) {
            $.ajax({
                type: "GET",
                url: "/DynamicContent/ReloadComponentModeOptions",
                data: { component: newComponent },
                success: function (response) {
                    //Set HTML
                    document.getElementById("combo-02").innerHTML = response;
                    //Reload kendo component
                    var testlist = [];
                    var selectVal = $("#combo-02>option")[0].value;
                    $("#combo-02>option").each(function (i, el) {
                        testlist.push({ text: el.innerText, value: el.value });
                        if (el.innerText === newComponentMode) {
                            selectVal = el.value;
                        }
                    });
                    $("#combo-02").data("kendoComboBox").dataSource.data(testlist);
                    $("#combo-02").data("kendoComboBox").value(selectVal);
                    //Update Field display
                    window.DynamicContent.updateComponentModeVisibility(selectVal);
                },
                failure: function (response) {
                    window.DynamicContent.DisplayErrorInDynamicContent(response);
                },
                error: function (response) {
                    window.DynamicContent.DisplayErrorInDynamicContent(response);
                },
            });
        }

        /**
         * On opening the dynamic content and switching between component modes this method will check which groups and properties should be visible. 
         * @param {number} componentModeKey The key value of the componentMode. This key will be used to retrieve the associated value.
         */
        async updateComponentModeVisibility(componentModeKey) {
            var componentMode = await this.getComponentModeFromKey(componentModeKey);
            //Group visibility
            $(".item-group").hide();
            if (componentMode) {
                $(".item-group:has(> [data-componentmode*='" + componentMode + "'])").show();
                $(".item-group:has(> [data-componentmode=''])").show();
            }

            //Property visibility
            $('[data-componentmode]').hide();
            if (componentMode) {
                $('[data-componentmode*="' + componentMode + '"]').show();
                $('[data-componentmode=""]').show();
            }
        }

        /**
         * Retrieves the associated value from the given component key.
         * @param {number} componentModeKey The key value for retrieving the componentMode.
         */
        async getComponentModeFromKey(componentModeKey) {
            if (!window.componentModus) {
                return new Promise(res => {
                    $.ajax({
                        type: "GET",
                        url: "/DynamicContent/GetComponentModesAsJsonResult",
                        data: { component: document.getElementById("combo-01").value },
                        success: function (response) {
                            if (response) {
                                window.componentModus = response;
                                res(window.componentModus[parseInt(componentModeKey)]);
                            } else {
                                res(null);
                            }
                        },
                        failure: function (response) { alert(response.responseText); },
                        error: function (response) { alert(response.responseText); }
                    })
                }); 
            } else {
                return window.componentModus[parseInt(componentModeKey)];
            }
        }

        /**
         *  Bind the save button to the event for saving the newly acquired settings.
         * */
        bindSaveButton() {

            document.getElementsByClassName("btn-primary")[0].onclick = function (el) {
                $.ajax({
                    type: "POST",
                    url: "/DynamicContent/SaveSettings",
                    dataType: "JSON",
                    data: {
                        templateid: parseInt(document.getElementById("wiser").dataset.dcid), component: document.getElementById("combo-01").value, componentMode: document.getElementById("combo-02").value, template_name: document.querySelector('input[name="visibleDescription"]').value, settings: JSON.stringify(window.DynamicContent.getNewSettings()) },
                    success: function () { window.popupNotification.show("Dynamic content '" + document.querySelector('input[name="visibleDescription"]').value+"' is succesvol opgeslagen.", "info"); window.DynamicContent.loadComponentHistory() },
                    failure: function (response) { alert(response.responseText); },
                    error: function (response) { alert(response.responseText); }
                })
            }
        }

        /**
         * Retrieve the new values entered by the user and their properties.
         * */
        getNewSettings() {
            var settingsList = {};

            $("[data-property]").each(function (i, el) {
                var val;
                if (el.type === "checkbox") {
                    val = el.checked;
                } else if (el.title === "numeric") {
                    val = parseFloat(el.value)
                    if (!val) {
                        val = null;
                    }
                } else {
                    val = el.value;
                }

                settingsList[el.dataset.property] = val;
            });

            return settingsList;
        }

        /**
         * Loads the History HTML and updates the right panel.
         * */
        loadComponentHistory() {
            $.ajax({
                type: "GET",
                url: "/DynamicContent/GetHistoryOfComponent",
                data: { templateid: parseInt(document.getElementById("wiser").dataset.dcid) },
                success: function (response) { document.getElementsByClassName("historyContainer")[0].innerHTML = response; window.DynamicContent.bindHistoryButtons(); }
            });
        }

        transformCodeMirrorViews() {
            $("textarea[data-fieldtype][data-property]").each(function (i, el) {
                var cmObject = CodeMirror.fromTextArea(el, {
                    lineNumbers: true,
                    styleActiveLine: true,
                    matchBrackets: true,
                    //theme: "darcula",
                    mode: el.dataset.fieldtype
                });

                cmObject.on("change", function () {
                    cmObject.getTextArea().value = cmObject.getValue();
                });

                //$(".item-group").click(function () {
                //    if (cmObject.getOption("theme") === "darcula") {
                //        cmObject.setOption("theme", "neo")
                //    }
                //    else {
                //        cmObject.setOption("theme", "darcula")
                //    };
                //});
            });
        }

        /**
         * Bind the buttons in the generated history html.
         * */
        bindHistoryButtons() {
            $("#revertChanges").hide();
            // Select history changes and change revert button visibility
            $(".col-6>.item").on("click", function (el) {
                var currentProperty = $(el.currentTarget).find("[data-historyproperty]").data("historyproperty");
                $(el.currentTarget.closest(".historyLine")).find(".col-6>.item").has("[data-historyproperty='" + currentProperty + "']").toggleClass("selected");

                if (document.querySelectorAll(".col-6>.item.selected").length) {
                    $("#revertChanges").show();
                    document.getElementsByClassName("btn-primary")[0].disabled = true;
                } else {
                    $("#revertChanges").hide();
                    document.getElementsByClassName("btn-primary")[0].disabled = false;
                }
            });

            // Clicking the revert button.
            $(".historyTagline button").on("click", function () {
                var changelist = [];
                $("[data-historyversion]:has(.selected)").each(function (i, versionElement) {
                    var reverted = [];
                    $(versionElement).find(".selected [data-historyproperty]").each(function (ii, propertyElement) {
                        if (!reverted.includes(propertyElement.dataset.historyproperty)) {
                            reverted.push(propertyElement.dataset.historyproperty)
                        }
                    });

                    changelist.push({
                        version: parseInt(versionElement.dataset.historyversion),
                        revertedProperties: reverted
                    });
                });

                $.ajax({
                    type: "POST",
                    url: "/DynamicContent/UndoChanges",
                    data: { changes: JSON.stringify(changelist), templateId: parseInt(document.getElementById("wiser").dataset.dcid) },
                    success: function (response) {
                        window.popupNotification.show("Dynamic content(" + document.getElementById("wiser").dataset.dcid+") wijzigingen zijn succesvol teruggezet", "info");
                        setTimeout(function () {
                            console.log("Refresh")
                            window.DynamicContent.loadComponentHistory();
                            window.DynamicContent.ChangeComponent(document.getElementById("combo-01").value);
                        },1000);
                    }
                });
            });
        }

        DisplayErrorInDynamicContent(errorResponse) {
            console.log(errorResponse);
            $(".k-tabstrip-wrapper").each(function (i, el) {
                el.innerHTML = errorResponse.responseText
            });
        }
    }

    // Initialize the DynamicItems class and make one instance of it globally available.
    window.DynamicContent = new DynamicContent(settings);
})(moduleSettings);
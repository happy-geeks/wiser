﻿import { TrackJS } from "trackjs";
import { Wiser2, Misc } from "../../Base/Scripts/Utils.js";
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
            this.mainWindow = null;
            this.componentTypeComboBox = null;
            this.componentModeComboBox = null;
            this.selectedComponentData = null;

            // Default settings
            this.settings = {
                moduleId: 0,
                customerId: 0,
                username: "Onbekend",
                userEmailAddress: "",
                userType: ""
            };
            Object.assign(this.settings, settings);

            // Other.
            this.mainLoader = null;

            // Set the Kendo culture to Dutch. TODO: Base this on the language in Wiser.
            kendo.culture("nl-NL");

            // Add logged in user access token to default authorization headers for all jQuery ajax requests.
            $.ajaxSetup({
                headers: { "Authorization": `Bearer ${localStorage.getItem("accessToken")}` }
            });

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

            // Setup processing.
            document.addEventListener("processing.Busy", this.toggleMainLoader.bind(this, true));
            document.addEventListener("processing.Idle", this.toggleMainLoader.bind(this, false));

            const process = `initialize_${Date.now()}`;
            window.processing.addProcess(process);

            // Fullscreen event for elements that can go fullscreen, such as HTML editors.
            const classHolder = $(document.documentElement);
            const fullscreenChange = "webkitfullscreenchange mozfullscreenchange fullscreenchange MSFullscreenChange";
            $(document).bind(fullscreenChange, $.proxy(classHolder.toggleClass, classHolder, "k-fullscreen"));

            // Setup any settings from the body element data. These settings are added via the Wiser backend and they take preference.
            Object.assign(this.settings, $("body").data());

            if (this.settings.trackJsToken) {
                TrackJS.install({
                    token: this.settings.trackJsToken
                });
            }

            const user = JSON.parse(localStorage.getItem("userData"));
            this.settings.oldStyleUserId = user.oldStyleUserId;
            this.settings.username = user.adminAccountName ? `Happy Horizon (${user.adminAccountName})` : user.name;
            this.settings.adminAccountLoggedIn = !!user.adminAccountName;

            const userData = await Wiser2.getLoggedInUserData(this.settings.wiserApiRoot);
            this.settings.userId = userData.encryptedId;
            this.settings.customerId = userData.encryptedCustomerId;
            this.settings.zeroEncrypted = userData.zeroEncrypted;
            this.settings.filesRootId = userData.filesRootId;
            this.settings.imagesRootId = userData.imagesRootId;
            this.settings.templatesRootId = userData.templatesRootId;
            this.settings.mainDomain = userData.mainDomain;

            if (!this.settings.wiserApiRoot.endsWith("/")) {
                this.settings.wiserApiRoot += "/";
            }

            this.initializeKendoComponents();

            await this.initCurrentComponentData();

            this.bindSaveButton();
            await this.loadComponentHistory();
            window.processing.removeProcess(process);
        }

        async initCurrentComponentData() {
            try {
                this.selectedComponentData = await Wiser2.api({
                    url: `${this.settings.wiserApiRoot}dynamic-content/${this.settings.selectedId}`,
                    dataType: "json",
                    method: "GET"
                });

                this.componentTypeComboBox.value(this.selectedComponentData.component);
                $("#visibleDescription").val(this.selectedComponentData.title);
                this.changeComponent(this.selectedComponentData.component, this.selectedComponentData.componentMode);
            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan met het laden van deze component. Probeer het opnieuw of neem contact op.");
            }
        }

        /**
         * Initializes all kendo components for the base class.
         */
        initializeKendoComponents() {
            window.popupNotification = $("#popupNotification").kendoNotification().data("kendoNotification");

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
            this.initializeDynamicKendoComponents();

            //Components
            this.componentTypeComboBox = $("#componentTypeDropDown").kendoComboBox({
                change: this.onComponentTypeDropDownChange.bind(this)
            }).data("kendoComboBox");
        }

        //Initialize the dynamic kendo components. This method will also be called when reloading component fields.
        initializeDynamicKendoComponents() {
            // Tabstrip
            $(".tabstrip").kendoTabStrip({
                activate: this.onTabStripActivate.bind(this),
                animation: {
                    open: {
                        effects: "fadeIn"
                    }
                }
            }).data("kendoTabStrip").select(0);

            //NUMERIC FIELD
            $(".numeric").kendoNumericTextBox();
            
            //MULTISELECT
            $(".multi-select").kendoMultiSelect({
                autoClose: false
            });

            $(".select").kendoDropDownList();
        }

        /**
         * Shows or hides the main (full screen) loader.
         * @param {boolean} show True to show the loader, false to hide it.
         */
        toggleMainLoader(show) {
            this.mainLoader.toggleClass("loading", show);
        }

        async onComponentTypeDropDownChange(event) {
            console.log("onComponentTypeDropDownChange", event);
            await this.changeComponent(event.sender.value(), 0);
        }

        async changeComponent(newComponent, newComponentMode) {
            const process = `changeComponent_${Date.now()}`;
            window.processing.addProcess(process);

            try {
                const response = await Wiser2.api({
                    url: `/Modules/DynamicContent/${encodeURIComponent(newComponent)}/DynamicContentTabPane`,
                    method: "POST",
                    contentType: "application/json",
                    data: JSON.stringify(this.selectedComponentData)
                });

                //force reload on component modes
                this.componentModus = null;

                $("#DynamicContentTabPane").html(response);
                this.reloadComponentModes(newComponent, newComponentMode);
                this.initializeDynamicKendoComponents();
                await this.transformCodeMirrorViews();
            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan. Probeer het a.u.b. opnieuw");
            }

            window.processing.removeProcess(process);
        }

        onTabStripActivate(event) {
            // Refresh all code mirror instances after switching tab, otherwise they won't work properly.
            $(event.contentElement).find("textarea[data-field-type][data-property]").each((index, element) => {
                const codeMirrorInstance = $(element).data("CodeMirrorInstance");
                if (!codeMirrorInstance) {
                    return;
                }

                codeMirrorInstance.refresh();
            });
        }

        async reloadComponentModes(newComponent, newComponentMode) {
            const componentModes = await Wiser2.api({
                url: `${this.settings.wiserApiRoot}dynamic-content/${encodeURIComponent(newComponent)}/component-modes`,
                dataType: "json",
                method: "GET"
            });

            if (!newComponentMode) {
                newComponentMode = componentModes[0].name;
            }

            if (!this.componentModeComboBox) {
                this.componentModeComboBox = $("#componentMode").kendoComboBox({
                    change: this.updateComponentModeVisibility.bind(this),
                    dataTextField: "name",
                    dataValueField: "id"
                }).data("kendoComboBox");
            }

            this.componentModeComboBox.setDataSource(componentModes);
            this.componentModeComboBox.value(this.getComponentModeFromKey(newComponentMode).id);
            this.updateComponentModeVisibility(newComponentMode);
        }

        /**
         * On opening the dynamic content and switching between component modes this method will check which groups and properties should be visible. 
         * @param {number} componentModeKey The key value of the componentMode. This key will be used to retrieve the associated value.
         */
        updateComponentModeVisibility(componentModeKey) {
            let componentMode;
            if (typeof componentModeKey === "string") {
                componentMode = this.getComponentModeFromKey(componentModeKey).name;
            } else if (typeof componentModeKey === "number") {
                componentMode = componentModeKey.toString();
            } else {
                componentMode = this.componentModeComboBox.value();
            }

            //Group visibility
            $(".item-group").hide();
            if (componentMode) {
                $(`.item-group:has(> [data-componentmode*='${componentMode}'])`).show();
            }
            $(".item-group:has(> [data-componentmode=''])").show();

            //Property visibility
            $("[data-componentmode]").hide();
            if (componentMode) {
                $(`[data-componentmode*="${componentMode}"]`).show();
            }
            $("[data-componentmode='']").show();
        }

        /**
         * Retrieves the associated value from the given component key.
         * @param {number} componentModeKey The key value for retrieving the componentMode.
         */
        getComponentModeFromKey(componentModeKey) {
            if (!componentModeKey) {
                console.warn("getComponentModeFromKey called with invalid componentModeKey", componentModeKey);
                return { id: 0, name: "" };
            }

            const result = this.componentModeComboBox.dataSource.data().filter(c => c.name === componentModeKey || c.id === parseInt(componentModeKey))[0];
            if (!result) {
                console.warn("getComponentModeFromKey called with invalid componentModeKey", componentModeKey);
                return { id: 0, name: "" };
            }

            return result;
        }

        /**
         *  Bind the save button to the event for saving the newly acquired settings.
         * */
        bindSaveButton() {
            document.getElementsByClassName("btn-primary")[0].addEventListener("click", (event) => {
                event.preventDefault();
                this.save();
            });

            document.getElementsByClassName("btn-secondary")[0].addEventListener("click", async (event) => {
                event.preventDefault();
                await this.save();
                if (!window.parent || !window.parent.Templates) {
                    console.warn("No parent window found, or parent window has no Templates class.");
                } else {
                    window.parent.Templates.newContentId = this.settings.selectedId;
                    window.parent.Templates.newContentTitle = this.settings.selectedTitle;
                    window.parent.$("#dynamicContentWindow").data("kendoWindow").close();
                }
            });
        }

        async save() {
            const process = `save_${Date.now()}`;
            window.processing.addProcess(process);

            try {
                const title = document.querySelector('input[name="visibleDescription"]').value;
                const contentId = await Wiser2.api({
                    url: `${this.settings.wiserApiRoot}dynamic-content/${this.settings.selectedId}`,
                    dataType: "json",
                    method: "POST",
                    contentType: "application/json",
                    data: JSON.stringify({
                        component: document.getElementById("componentTypeDropDown").value,
                        componentModeId: document.getElementById("componentMode").value,
                        title: title,
                        data: this.getNewSettings()
                    })
                });

                if (!this.settings.selectedId) {
                    this.settings.selectedId = contentId;
                    this.settings.selectedTitle = title;
                    await this.addLinkToTemplate(this.settings.templateId);
                }

                window.popupNotification.show(`Dynamic content '${document.querySelector('input[name="visibleDescription"]').value}' is succesvol opgeslagen.`, "info");
                this.loadComponentHistory();
            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan met opslaan. Probeer het a.u.b. opnieuw");
            }

            window.processing.removeProcess(process);
        }

        async addLinkToTemplate(templateId) {
            await Wiser2.api({
                url: `${this.settings.wiserApiRoot}dynamic-content/${this.settings.selectedId}/link/${templateId}`,
                dataType: "json",
                method: "PUT",
                contentType: "application/json"
            });
        }

        /**
         * Retrieve the new values entered by the user and their properties.
         * */
        getNewSettings() {
            var settingsList = {};
            
            $("[data-property]").each((index, element) => {
                const field = $(element);
                const propertyName = field.data("property");
                const kendoControlName = field.data("kendoControl");
                
                if (kendoControlName) {
                    const kendoControl = field.data(kendoControlName);
                    
                    if (kendoControl) {
                        settingsList[propertyName] = kendoControl.value();
                        return;
                    } else {
                        console.warn(`Kendo control found for '${propertyName}', but it's not initialized, so skipping this property.`, kendoControlName, data);
                        return;
                    }
                }

                const codeMirrorInstance = field.data("CodeMirrorInstance");
                if (codeMirrorInstance) {
                    settingsList[propertyName] = codeMirrorInstance.getValue();
                    return;
                }

                // If we reach this point in the code, this element is not a Kendo control, so just get the normal value.
                switch (field.prop("tagName")) {
                    case "SELECT":
                        settingsList[propertyName] = field.val();
                        break;
                    case "INPUT":
                    case "TEXTAREA":
                        switch ((field.attr("type") || "").toUpperCase()) {
                            case "CHECKBOX":
                                settingsList[propertyName] = field.prop("checked");
                                break;
                            default:
                                settingsList[propertyName] = field.val();
                                break;
                        }
                        break;
                    default:
                        console.error("TODO: Unsupported tag name:", field.prop("tagName"));
                        return;
                }
            });

            return settingsList;
        }

        /**
         * Loads the History HTML and updates the right panel.
         * */
        async loadComponentHistory() {
            const history = await Wiser2.api({
                url: `${this.settings.wiserApiRoot}dynamic-content/${this.settings.selectedId}/history`,
                dataType: "json",
                method: "GET"
            });

            const historyHtml = await Wiser2.api({
                url: `/Modules/DynamicContent/History`,
                method: "POST",
                contentType: "application/json",
                data: JSON.stringify(history)
            });

            document.getElementsByClassName("historyContainer")[0].innerHTML = historyHtml;
            this.bindHistoryButtons();
        }

        async transformCodeMirrorViews() {
            await Misc.ensureCodeMirror();
            $("textarea[data-field-type][data-property]").each((index, element) => {
                const codeMirrorInstance = CodeMirror.fromTextArea(element, {
                    lineNumbers: true,
                    styleActiveLine: true,
                    matchBrackets: true,
                    mode: element.dataset.fieldType
                });

                $(element).data("CodeMirrorInstance", codeMirrorInstance);
            });
        }

        /**
         * Bind the buttons in the generated history html.
         * */
        bindHistoryButtons() {
            $("#revertChanges").hide();
            // Select history changes and change revert button visibility
            $(".col-6>.item").on("click", function (el) {
                const currentProperty = $(el.currentTarget).find("[data-history-property]").data("historyProperty");
                $(el.currentTarget.closest(".historyLine")).find(".col-6>.item").has("[data-history-property='" + currentProperty + "']").toggleClass("selected");

                if (document.querySelectorAll(".col-6>.item.selected").length) {
                    $("#revertChanges").show();
                    document.getElementsByClassName("btn-primary")[0].disabled = true;
                } else {
                    $("#revertChanges").hide();
                    document.getElementsByClassName("btn-primary")[0].disabled = false;
                }
            });

            // Clicking the revert button.
            $(".historyTagline button").on("click", async () => {
                const process = `revertChanges_${Date.now()}`;
                window.processing.addProcess(process);

                try {
                    const changeList = [];
                    $("[data-history-version]:has(.selected)").each((i, versionElement) => {
                        const reverted = [];
                        $(versionElement).find(".selected [data-history-property]").each((ii, propertyElement) => {
                            if (!reverted.includes(propertyElement.dataset.historyProperty)) {
                                reverted.push(propertyElement.dataset.historyProperty);
                            }
                        });

                        changeList.push({
                            version: parseInt(versionElement.dataset.historyVersion),
                            revertedProperties: reverted
                        });
                    });

                    await Wiser2.api({
                        url: `${this.settings.wiserApiRoot}dynamic-content/${this.settings.selectedId}/undo-changes`,
                        dataType: "json",
                        method: "POST",
                        contentType: "application/json",
                        data: JSON.stringify(changeList)
                    });

                    window.popupNotification.show(`Dynamic content(${this.settings.selectedId}) wijzigingen zijn succesvol teruggezet`, "info");
                    await this.loadComponentHistory();
                    await this.initCurrentComponentData();
                } catch (exception) {
                    console.error(exception);
                    kendo.alert("Er is iets fout gegaan met ongedaan maken van deze wijzigingen. Probeer het a.u.b. opnieuw");
                }

                window.processing.removeProcess(process);
            });
        }
    }

    // Initialize the DynamicItems class and make one instance of it globally available.
    window.DynamicContent = new DynamicContent(settings);
})(moduleSettings);
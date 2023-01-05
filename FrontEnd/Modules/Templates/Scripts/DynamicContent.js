﻿import { TrackJS } from "trackjs";
import { Wiser, Misc } from "../../Base/Scripts/Utils.js";
import "../../Base/Scripts/Processing.js";
import { Preview } from "./Preview.js";

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
            this.saving = false;
            this.changesTimeout = null;

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
            this.preview = new Preview(this);

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
            this.selectedId = this.settings.templateId;

            if (this.settings.trackJsToken) {
                TrackJS.install({
                    token: this.settings.trackJsToken
                });
            }

            const user = JSON.parse(localStorage.getItem("userData"));
            this.settings.oldStyleUserId = user.oldStyleUserId;
            this.settings.username = user.adminAccountName ? `${user.adminAccountName} (Admin)` : user.name;
            this.settings.adminAccountLoggedIn = !!user.adminAccountName;

            const userData = await Wiser.getLoggedInUserData(this.settings.wiserApiRoot);
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
            
            this.stickyHeader();

            this.initializeKendoComponents();

            await this.initCurrentComponentData();

            this.initializeButtons();
            await this.loadComponentHistory();
            await this.loadPreviewTab();
            window.processing.removeProcess(process);
        }

        async initCurrentComponentData() {
            try {
                this.selectedComponentData = await Wiser.api({
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
         * Sticky header within Dynamic Content.
         */
        stickyHeader() {
            const elem = document.getElementById('DynamicContentPane');
            let lastScrollTop = 0;

            elem.onscroll = (e) => {
                if (elem.scrollTop < lastScrollTop){
                    elem.classList.add('sticky');
                } else {
                    elem.classList.remove('sticky');
                }
                lastScrollTop = elem.scrollTop <= 0 ? 0 : elem.scrollTop;
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
            this.componentTypeComboBox = $("#componentTypeDropDown").kendoDropDownList({
                change: this.onComponentTypeDropDownChange.bind(this)
            }).data("kendoDropDownList");
        }

        //Initialize the dynamic kendo components. This method will also be called when reloading component fields.
        initializeDynamicKendoComponents(container = null) {
            container = container || $("body");
            // Tabstrip
            const tabStripElements = container.find(".tabstrip");
            if (tabStripElements.length > 0) {
                tabStripElements.kendoTabStrip({
                    activate: this.onTabStripActivate.bind(this),
                    animation: {
                        open: {
                            effects: "fadeIn"
                        }
                    }
                }).data("kendoTabStrip").select(0);
            }

            //NUMERIC FIELD
            container.find(".numeric").each((index, element) => {
                const isDecimal = $(element).data("decimal") === true;
                $(element).kendoNumericTextBox({
                    decimals: isDecimal ? 2 : 0,
                    format: isDecimal ? "n2" : "n0",
                    change: () => this.onInputChange(true),
                    spin: () => this.onInputChange(false)
                });
            });
            
            //MULTISELECT
            container.find(".multi-select").kendoMultiSelect({
                autoClose: false,
                change: () => this.onInputChange(true)
            });

            container.find(".select").kendoDropDownList({
                change: () => this.onInputChange(true)
            });
            
            container.find(".add-subgroup-button").off("click").click(this.onAddSubGroupButtonClick.bind(this));
            container.find(".remove-subgroup-button").off("click").click(this.onRemoveSubGroupButtonClick.bind(this));

            container.find("input.textField, label.checkbox > input[type='checkbox']").change(() => this.onInputChange(true));
        }

        /**
         * Shows or hides the main (full screen) loader.
         * @param {boolean} show True to show the loader, false to hide it.
         */
        toggleMainLoader(show) {
            this.mainLoader.toggleClass("loading", show);
        }

        async onComponentTypeDropDownChange(event) {
            await this.changeComponent(event.sender.value(), 0);
        }

        async changeComponent(newComponent, newComponentMode) {
            const process = `changeComponent_${Date.now()}`;
            window.processing.addProcess(process);

            try {
                this.selectedComponentData.component = newComponent;
                await this.reloadComponentModes(newComponent, newComponentMode);
                this.selectedComponentData.componentMode = this.componentModeComboBox.text();

                const response = await Wiser.api({
                    url: `/Modules/DynamicContent/${encodeURIComponent(newComponent)}/DynamicContentTabPane`,
                    method: "POST",
                    contentType: "application/json",
                    data: JSON.stringify(this.selectedComponentData)
                });

                $("#DynamicContentTabPane").html(response);
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
            const componentModes = await Wiser.api({
                url: `${this.settings.wiserApiRoot}dynamic-content/${encodeURIComponent(newComponent)}/component-modes`,
                dataType: "json",
                method: "GET"
            });

            if (!newComponentMode) {
                newComponentMode = componentModes[0].name;
            }

            if (!this.componentModeComboBox) {
                this.componentModeComboBox = $("#componentMode").kendoDropDownList({
                    change: this.updateComponentModeVisibility.bind(this),
                    dataTextField: "name",
                    dataValueField: "id"
                }).data("kendoDropDownList");
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
                $(`.item-group:has(> [data-component-mode*='${componentMode}'])`).show();
            }
            $(".item-group:has(> [data-component-mode=''])").show();

            //Property visibility
            $("[data-component-mode]").hide();
            if (componentMode) {
                $(`[data-component-mode*="${componentMode}"]`).show();
            }
            $("[data-component-mode='']").show();
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
        initializeButtons() {
            document.body.addEventListener("keydown", (event) => {
                if ((event.ctrlKey || event.metaKey) && event.keyCode === 83) {
                    event.preventDefault();
                    this.save();
                }
            });

            $("#saveButton").click((event) => {
                event.preventDefault();
                this.save();
            });

            $("#saveAndDeployToTestButton").click((event) => {
                event.preventDefault();
                this.save(true);
            });

            $("#saveAndCloseButton").click(async (event) => {
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

            $("#previewHtml").click((event) => {
                event.preventDefault();
                this.preview.generateHtmlPreviewForComponent(this.settings.selectedId, this.getDynamicContentPreviewSettings());
            });
        }

        async save(alsoDeployToTest = false) {
            if (this.saving) {
                return;
            }

            const process = `save_${Date.now()}`;
            window.processing.addProcess(process);

            try {
                this.saving = true;
                const title = document.querySelector('input[name="visibleDescription"]').value;
                if (!title) {
                    kendo.alert("Naam is verplicht! Vul een naam in om verder te gaan");
                    this.saving = false;
                    window.processing.removeProcess(process);
                    return;
                }
                    const contentId = await Wiser.api({
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


                    window.popupNotification.show(`Dynamisch component '${document.querySelector('input[name="visibleDescription"]').value}' is succesvol opgeslagen.`, "info");

                
                if (alsoDeployToTest) {
                    const version = (parseInt($(".historyContainer .historyLine:first").data("historyVersion")) || 0) + 1;
    
                    await Wiser.api({
                        url: `${this.settings.wiserApiRoot}dynamic-content/${contentId}/publish/test/${version}`,
                        dataType: "json",
                        type: "POST",
                        contentType: "application/json"
                    });
    
                    window.popupNotification.show(`Dynamisch component is succesvol naar de test-omgeving gezet`, "info");
                }
                
                await this.loadComponentHistory();
            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan met opslaan. Probeer het a.u.b. opnieuw");
            }

            this.saving = false;
            window.processing.removeProcess(process);
        }

        async addLinkToTemplate(templateId) {
            await Wiser.api({
                url: `${this.settings.wiserApiRoot}dynamic-content/${this.settings.selectedId}/link/${templateId}`,
                dataType: "json",
                method: "PUT",
                contentType: "application/json"
            });
        }

        getDynamicContentPreviewSettings() {
            return [
                {
                    id: this.settings.selectedId,
                    name: document.getElementById("componentTypeDropDown").value,
                    settingsJson: JSON.stringify(this.getNewSettings())
                }
            ];
        }

        /**
         * Retrieve the new values entered by the user.
         * */
        getNewSettings(fields = null) {
            const settingsList = {};
            fields = fields || $("[data-property]").not(".sub-groups [data-property]");
            
            fields.each((index, element) => {
                const field = $(element);
                const propertyName = field.data("property");
                const kendoControlName = field.data("kendoControl");
                const subFieldsGroupName = field.closest(".sub-group").data("key");
                let settingsListToUse = settingsList;
                if (typeof(subFieldsGroupName) !== "undefined" && subFieldsGroupName !== null) {
                    if (subFieldsGroupName === "_template") {
                        // A fieldset with this name is only used as a template for adding groups via javascript, don't save these.
                        return;
                    }

                    if (!settingsListToUse[subFieldsGroupName]) {
                        settingsListToUse[subFieldsGroupName] = {};
                    }

                    settingsListToUse = settingsListToUse[subFieldsGroupName];
                }

                if (kendoControlName) {
                    const kendoControl = field.data(kendoControlName);
                    
                    if (kendoControl) {
                        settingsListToUse[propertyName] = kendoControl.value();
                        return;
                    } else {
                        console.warn(`Kendo control found for '${propertyName}', but it's not initialized, so skipping this property.`, kendoControlName, data);
                        return;
                    }
                }

                const codeMirrorInstance = field.data("CodeMirrorInstance");
                if (codeMirrorInstance) {
                    settingsListToUse[propertyName] = codeMirrorInstance.getValue();
                    return;
                }

                // If we reach this point in the code, this element is not a Kendo control, so just get the normal value.
                switch (field.prop("tagName")) {
                    case "SELECT":
                        settingsListToUse[propertyName] = field.val();
                        break;
                    case "INPUT":
                    case "TEXTAREA":
                        switch ((field.attr("type") || "").toUpperCase()) {
                            case "CHECKBOX":
                                settingsListToUse[propertyName] = field.prop("checked");
                                break;
                            default:
                                settingsListToUse[propertyName] = field.val();
                                break;
                        }
                        break;
                    case "DIV":
                        // This means it's a container with sub fields.
                        settingsListToUse[propertyName] = this.getNewSettings(field.find("[data-property]"));
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
            const history = await Wiser.api({
                url: `${this.settings.wiserApiRoot}dynamic-content/${this.settings.selectedId}/history`,
                dataType: "json",
                method: "GET"
            });

            const historyHtml = await Wiser.api({
                url: `/Modules/DynamicContent/History`,
                method: "POST",
                contentType: "application/json",
                data: JSON.stringify(history)
            });

            document.getElementsByClassName("historyContainer")[0].innerHTML = historyHtml;
            this.bindHistoryButtons();
        }

        async loadPreviewTab() {
            // Preview
            await this.preview.loadProfiles();
            const response = await Wiser.api({
                method: "GET",
                url: "/Modules/Templates/PreviewTab"
            });
            
            document.getElementById("previewTab").innerHTML = response;

            this.preview.initPreviewProfileInputs(true, true);
            this.preview.bindPreviewButtons();
            this.preview.generatePreview(false);
        }

        async transformCodeMirrorViews(container = null) {
            await Misc.ensureCodeMirror();
            container = container || $("body");
            container.find("textarea[data-field-type][data-property]").not("fieldset.hidden textarea[data-field-type][data-property]").each((index, element) => {
                if ($(element).data("CodeMirrorInstance")) {
                    return;
                }

                const codeMirrorInstance = CodeMirror.fromTextArea(element, {
                    lineNumbers: true,
                    indentUnit: 4,
                    lineWrapping: true,
                    foldGutter: true,
                    gutters: ["CodeMirror-linenumbers", "CodeMirror-foldgutter", "CodeMirror-lint-markers"],
                    lint: true,
                    extraKeys: {
                        "Ctrl-Q": (sender) => {
                            sender.foldCode(sender.getCursor());
                        },
                        "F11": (sender) => {
                            const isFullScreen = sender.getOption("fullScreen");
                            sender.setOption("fullScreen", !isFullScreen);
                            $(sender.getTextArea()).closest(".item.has-default-value").css("opacity", isFullScreen ? "" : "1");
                        },
                        "Esc": (sender) => {
                            if (sender.getOption("fullScreen")) sender.setOption("fullScreen", false);
                        },
                        "Ctrl-Space": "autocomplete"
                    },
                    mode: element.dataset.fieldType
                });

                codeMirrorInstance.on("change", () => this.onInputChange(false));

                $(element).data("CodeMirrorInstance", codeMirrorInstance);
            });
        }

        /**
         * Event for when the user changes a value in an input.
         * This will generate a new preview, 500ms after the user's last change.
         */
        onInputChange(instant = false) {
            if (this.changesTimeout) {
                clearTimeout(this.changesTimeout);
            }

            this.changesTimeout = setTimeout(() => {
                this.preview.generatePreview(false);
            }, instant ? 0 : 500);
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

                    await Wiser.api({
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

        /**
         * Gets the template settings. This method will be called from the Preview class.
         * This will call the getCurrentTemplateSettings() method from the parent frame, which should be the templates module.
         */
        getCurrentTemplateSettings() {
            if (!window.parent || !window.parent.Templates) {
                console.warn("No parent window found, or parent window has no Templates class.");
                return {};
            } else {
                return window.parent.Templates.getCurrentTemplateSettings();
            }
        }

        async onAddSubGroupButtonClick(event) {
            event.preventDefault();

            const buttonElement = $(event.currentTarget);
            const mainContainer = buttonElement.closest(".item");
            const subGroupsContainer = mainContainer.find(".sub-groups");
            const templateFieldSet = subGroupsContainer.find("#subGroup__template");
            if (templateFieldSet.length === 0) {
                kendo.alert("Er zijn geen subvelden gevonden voor dit veld. Sluit a.u.b. dit component en probeer het opnieuw, of neem contact op.");
                console.error("No sub fields template found for container", subGroupsContainer);
                return;
            }

            const newIndex = (parseInt(subGroupsContainer.find("fieldset").last().data("index")) || 0) + 1;
            const cloneFieldSet = templateFieldSet.clone(true);
            cloneFieldSet.removeClass("hidden");
            cloneFieldSet.data("index", newIndex);
            cloneFieldSet.attr("id", `subGroup_id${newIndex - 1}`);
            cloneFieldSet.data("key", `id${newIndex-1}`);
            cloneFieldSet.find("legend .index").html(newIndex-1);
            subGroupsContainer.append(cloneFieldSet);
            
            this.initializeDynamicKendoComponents(cloneFieldSet);
            this.transformCodeMirrorViews(cloneFieldSet);
        }

        async onRemoveSubGroupButtonClick(event) {
            event.preventDefault();
            
            const buttonElement = $(event.currentTarget);
            const container = buttonElement.closest(".sub-group");
            if (container.data("key") === "_template") {
                // Don't allow the template to be deleted if the user was somehow able to click the button there.
                return;
            }

            await Wiser.showConfirmDialog(`Are you sure you want to delete layer ${container.find(".index").text()}?`);
            container.remove();
        }
    }

    // Initialize the DynamicItems class and make one instance of it globally available.
    window.DynamicContent = new DynamicContent(settings);
})(moduleSettings);
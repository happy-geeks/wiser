import { TrackJS } from "trackjs";
import { Wiser, Misc } from "../../Base/Scripts/Utils.js";
import "../../Base/Scripts/Processing.js";
require("@progress/kendo-ui/js/kendo.all.js");
require("@progress/kendo-ui/js/cultures/kendo.culture.nl-NL.js");
require("@progress/kendo-ui/js/messages/kendo.messages.nl-NL.js");

import "../css/Settings.css";

// Any custom settings can be added here. They will overwrite most default settings inside the module.
const communicationModuleSettings = {
};

((settings) => {
    /**
     * Main class.
     */
    class CommunicationSettings {
        /**
         * Initializes a new instance of Communication.
         * @param {any} settings An object containing the settings for this class.
         */
        constructor(settings) {
            this.base = this;
            this.mainLoader = null;
            
            // Components.
            this.nameElement = null;
            this.editNameButton = null;
            this.editNameField = null;
            this.deleteButton = null;
            this.saveButton = null;
            this.mainTabStrip = null;
            this.dataSelectorDropDown = null;
            this.queryDropDown = null;
            this.mailTemplateDropDown = null;
            this.previouslySelectedMailTemplate = null;
            this.languageDropDown = null;
            this.previouslySelectedLanguage = null;
            this.fixedDateTimePicker = null;
            this.recurringDateRangePicker = null;
            this.recurringTimePicker = null;
            this.recurringPeriodValueField = null;
            this.recurringPeriodTypeDropDown = null;
            this.recurringDayOfMonthField = null;
            this.staticReceiverInput = null;
            this.mailBodyEditor = null;
            this.mailSubjectField = null;
            this.emailSelectorField = null;

            // Set the Kendo culture to Dutch. TODO: Base this on the language in Wiser.
            kendo.culture("nl-NL");

            // Default settings
            this.settings = {
                customerId: 0,
                username: "Onbekend"
            };
            Object.assign(this.settings, settings);

            // Fire event on page ready for direct actions
            $(document).ready(() => {
                this.onPageReady();
            });
        }

        /**
         * Event that will be fired when the page is ready.
         */
        async onPageReady() {
            // Add logged in user access token to default authorization headers for all jQuery ajax requests.
            $.ajaxSetup({
                headers: { "Authorization": `Bearer ${localStorage.getItem("accessToken")}` }
            });

            this.mainLoader = $("#mainLoader");

            // Setup processing.
            document.addEventListener("processing.Busy", this.toggleMainLoader.bind(this, true));
            document.addEventListener("processing.Idle", this.toggleMainLoader.bind(this, false));

            // Show an error if the user is no longer logged in.
            const accessTokenExpires = localStorage.getItem("accessTokenExpiresOn");
            if (!accessTokenExpires || accessTokenExpires <= new Date()) {
                Wiser.alert({
                    title: "Niet ingelogd",
                    content: "U bent niet (meer) ingelogd. Ververs a.u.b. de pagina en probeer het opnieuw."
                });

                this.toggleMainLoader(false);
                return;
            }

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

            const userData = await Wiser.getLoggedInUserData(this.settings.wiserApiRoot);
            this.settings.userId = userData.encryptedId;
            this.settings.customerId = userData.encryptedCustomerId;
            this.settings.zeroEncrypted = userData.zeroEncrypted;

            this.settings.serviceRoot = `${this.settings.wiserApiRoot}templates/get-and-execute-query`;
            this.settings.getItemsUrl = `${this.settings.wiserApiRoot}data-selectors`;

            this.setupBindings();

            await this.initializeComponents();
            
            if (this.settings.settingsId > 0) {
                await this.loadSettings(this.settings.settingsId);
            } else {
                this.nameElement.text(this.settings.settingsName);
                this.editNameField.val(this.settings.settingsName);
            }

            this.toggleMainLoader(false);
        }

        /**
         * Shows or hides the main (full screen) loader.
         * @param {boolean} show True to show the loader, false to hide it.
         */
        toggleMainLoader(show) {
            this.mainLoader.toggleClass("loading", show);
        }

        /**
         * Setup all basis bindings for this module.
         * Specific bindings (for buttons in certain pop-ups for example) will be set when they are needed.
         */
        setupBindings() {
            document.addEventListener("moduleClosing", (event) => {
                // You can do anything here that needs to happen before closing the module.
                event.detail();
            });

            // Handle textareas with counters and max lengths.
            $("textarea[data-limit]").on({
                keyup: (event) => {
                    const charLength = event.currentTarget.value.length;
                    const charLimit = parseInt(event.currentTarget.dataset.limit);
                    const element = $(event.currentTarget);

                    element.next("span").html(charLength + " / " + charLimit);

                    if (charLength > charLimit) {
                        element.next("span").html(`<strong>Je bericht mag maximaal ${charLimit} karakters bevatten.</strong>`);
                    }
                }
            });
            
            this.editNameButton = $("#EditNameButton").click(this.onEditNameButtonClick.bind(this));
            this.deleteButton = $("#DeleteButton").click(this.onDeleteButtonClick.bind(this));
        }

        /**
         * Initializes all Kendo components for the base class.
         */
        async initializeComponents() {
            const process = `loadDropdowns_${Date.now()}`;
            window.processing.addProcess(process);
            
            this.nameElement = $("#CurrentName");
            this.editNameField = $("#EditNameField");
            this.staticReceiverInput = $("#StaticReceiverInput");
            this.mailSubjectField = $("#MailSubject");
            this.emailSelectorField = $("#EmailSelector");

            try {
                // Header buttons.
                this.saveButton = $("#SaveButton").kendoButton({
                    icon: "save",
                    click: this.onSaveButtonClick.bind(this)
                });

                // Main tab strip.
                this.mainTabStrip = $("#tabStrip").kendoTabStrip({
                    animation: {
                        open: {
                            effects: "fadeIn"
                        }
                    }
                }).data("kendoTabStrip");

                // Drop downs that need to load values from database, for tabs receivers and content.
                const promiseResults = await Promise.all([
                    Wiser.api({url: `${this.settings.wiserApiRoot}data-selectors?forCommunicationModule=true`}),
                    Wiser.api({url: `${this.settings.wiserApiRoot}queries/communication-module`}),
                    Wiser.api({url: `${this.settings.wiserApiRoot}items?entityType=mailtemplate`}),
                    Wiser.api({url: `${this.settings.wiserApiRoot}languages`})
                ]);

                const dataSelectors = promiseResults[0];
                const queries = promiseResults[1];
                const mailTemplates = promiseResults[2];
                const languages = promiseResults[3];

                if (!dataSelectors || !dataSelectors.length) {
                    $("#DataSelectorContainer").hide();
                } else {
                    this.dataSelectorDropDown = $("#DataSelectorList").kendoDropDownList({
                        optionLabel: "Selecteer een reeds bestaande dataselector",
                        dataTextField: "name",
                        dataValueField: "id",
                        dataSource: dataSelectors
                    }).data("kendoDropDownList");
                }

                if (!queries || !queries.length) {
                    $("#QueryContainer").hide();
                } else {
                    this.queryDropDown = $("#QueryList").kendoDropDownList({
                        optionLabel: "Selecteer de ontvangers via een vooraf ingestelde query",
                        dataTextField: "description",
                        dataValueField: "id",
                        dataSource: queries
                    }).data("kendoDropDownList");
                }

                this.mailTemplateDropDown = $("#MailTemplateDropDown").kendoDropDownList({
                    optionLabel: "Selecteer optioneel een template voor de inhoud van de mail",
                    dataTextField: "title",
                    dataValueField: "id",
                    dataSource: mailTemplates.results,
                    change: this.onMailTemplateDropDownChange.bind(this)
                }).data("kendoDropDownList");

                if (!languages.length) {
                    $("#LanguageDropDownContainer").hide();
                } else {
                    this.languageDropDown = $("#LanguageDropDown").kendoDropDownList({
                        optionLabel: "Selecteer optioneel een taal voor de inhoud van de mail",
                        dataTextField: "name",
                        dataValueField: "id",
                        dataSource: languages,
                        change: this.onLanguageDropDownChange.bind(this)
                    }).data("kendoDropDownList");
                }

                // Content tab.
                this.mailBodyEditor = $("#MailBodyEditor").kendoEditor({
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

                // Sending pattern tab.
                this.fixedDateTimePicker = $("#FixedDateTimePicker").kendoDateTimePicker({
                    value: new Date(),
                    dateInput: true,
                    format: "dd MMMM yyyy HH:mm"
                }).data("kendoDateTimePicker");

                const start = new Date();
                const end = new Date(start.getFullYear(), start.getMonth(), start.getDate() + 20);
                this.recurringDateRangePicker = $("#RecurringDateRangePicker").kendoDateRangePicker({
                    range: {
                        start: start,
                        end: end
                    },
                    messages: {
                        startLabel: "Periode start",
                        endLabel: "Periode einde"
                    },
                    format: "dd MMMM yyyy",
                    culture: "nl-NL"
                }).data("kendoDateRangePicker");

                this.recurringTimePicker = $("#RecurringTimePicker").kendoTimePicker({
                    dateInput: true,
                    format: "HH:mm"
                }).data("kendoTimePicker");

                this.recurringPeriodValueField = $("#RecurringPeriodValueField").kendoNumericTextBox({
                    decimals: 0,
                    format: "#"
                }).data("kendoNumericTextBox");
                
                this.recurringPeriodTypeDropDown = $("#RecurringPeriodTypeDropDown").kendoDropDownList({
                    change: this.onRecurringPeriodTypeDropDownChange.bind(this)
                });

                this.recurringDayOfMonthField = $("#RecurringDayOfMonth").kendoNumericTextBox({
                    decimals: 0,
                    format: "#"
                }).data("kendoNumericTextBox");
            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan. Probeer het a.u.b. opnieuw of neem contact op met ons.");
            } finally {
                window.processing.removeProcess(process);
            }
        }

        /**
         * Event for when the user selects a value in the mail template drop down.
         * @param event The change event of the Kendo dropdown list.
         */
        async onMailTemplateDropDownChange(event) {
            await this.setMailBodyEditorValue();
            this.previouslySelectedMailTemplate = event.sender.dataItem();
        }

        /**
         * Event for when the user selects a value in the mail template drop down.
         * @param event The change event of the Kendo dropdown list.
         */
        async onLanguageDropDownChange(event) {
            await this.setMailBodyEditorValue();
            this.previouslySelectedLanguage = event.sender.dataItem();
        }

        /**
         * Event for when the user changes the value of the period type drop down.
         * @param event The change event of the Kendo dropdown list.
         */
        onRecurringPeriodTypeDropDownChange(event) {
            const recurringWeeklyContainer = $("#RecurringWeeklyContainer");
            const recurringMonthlyContainer = $("#RecurringMonthlyContainer");
            
            switch (event.sender.value()) {
                case "week":
                    recurringWeeklyContainer.removeClass("hidden");
                    recurringMonthlyContainer.addClass("hidden");
                    break;
                case "month":
                    recurringWeeklyContainer.addClass("hidden");
                    recurringMonthlyContainer.removeClass("hidden");
                    break;
            }
        }

        /**
         * Event for when the user clicks the button to edit the name of the communication.
         * @param event The click event of the anchor element.
         */
        async onEditNameButtonClick(event) {
            event.preventDefault();
            this.nameElement.addClass("hidden");
            this.editNameField.removeClass("hidden");
            this.editNameButton.addClass("hidden");
        }

        /**
         * Event for when the user clicks the button to delete the communication.
         * @param event The click event of the anchor element.
         */
        async onDeleteButtonClick(event) {
            event.preventDefault();

            await Wiser.showConfirmDialog(`Wilt u de communicatie-instellingen met de naam "${this.nameElement.text()}" wilt verwijderen?`);
            const process = `deleteSettings_${Date.now()}`;
            window.processing.addProcess(process);
            
            try {
                await Wiser.api({
                    url: `${this.settings.wiserApiRoot}communications/${id}`,
                    method: "DELETE"
                });
                
                // Don't remove the process if everything succeeded, so that the loader will stay visible until the index page has finished loading.
                window.location = "/Modules/Communication";
            } catch (exception) {
                console.error(exception);
                kendo.alert(`Er is iets fout gegaan tijdens het laden van de communicatie-instellingen met ID '${id}'. Probeer het a.u.b. opnieuw of neem contact op met ons.`);
                window.processing.removeProcess(process);
            }
        }

        /**
         * Event for when the user clicks the save button to save changes in the communication.
         * @param event The click event of the Kendo button.
         */
        async onSaveButtonClick(event) {
            const process = `saveSettings_${Date.now()}`;
            window.processing.addProcess(process);
            
            try {
                const settings = this.getCurrentSettings();
                
                const result = await Wiser.api({
                    url: `${this.settings.wiserApiRoot}communications`,
                    method: "POST",
                    contentType: "application/json",
                    data: JSON.stringify(settings)
                });
                
                this.settings.settingsId = result.id;

                kendo.alert("De instellingen zijn succesvol opgeslagen.");
            } catch (exception) {
                console.error(exception);
                kendo.alert(`Er is iets fout gegaan tijdens het laden van de communicatie-instellingen met ID '${id}'. Probeer het a.u.b. opnieuw of neem contact op met ons.`);
            } finally {
                window.processing.removeProcess(process);
            }
        }

        /**
         * Set the mail body editor value, based on the selected mail template and language.
         */
        async setMailBodyEditorValue() {
            const selectedMailTemplate = this.mailTemplateDropDown.dataItem();
            const selectedLanguage = !this.languageDropDown ? null : this.languageDropDown.dataItem();
            if (!selectedMailTemplate || !selectedMailTemplate.id || (this.languageDropDown && (!selectedLanguage || !selectedLanguage.id))) {
                return;
            }
            
            if (this.mailBodyEditor.value()) {
                try {
                    await Wiser.showConfirmDialog("U heeft al een waarde ingevuld in de inhoud van de mail. Wilt u die overschrijven met de nieuw gekozen mailtemplate/taal?", "Overschrijven inhoud", "Annuleren", "Overschrijven");
                }
                catch {
                    // If the user cancelled the selection, return the dropdowns to their previous values.
                    this.mailTemplateDropDown.value(!this.previouslySelectedMailTemplate ? "" : this.previouslySelectedMailTemplate.id);
                    if (this.languageDropDown) this.languageDropDown.value(!this.previouslySelectedLanguage ? "" : this.previouslySelectedLanguage.id);
                    return;
                }
            }
            
            let templatePropertyName = "Template";
            if (selectedLanguage && selectedLanguage.code) {
                templatePropertyName += ` (${selectedLanguage.code})`;
            }
            
            const html = selectedMailTemplate[templatePropertyName] || selectedMailTemplate.Template || selectedMailTemplate.template || "";
            this.mailBodyEditor.value(html);
        }

        /**
         * Load all communication settings with a specific ID.
         * @param {int} id The ID of the settings to load.
         */
        async loadSettings(id) {
            const process = `loadSettings_${Date.now()}`;
            window.processing.addProcess(process);
            
            try {
                // Get the settings from the API.
                const settings = await Wiser.api({
                    url: `${this.settings.wiserApiRoot}communications/${id}`
                });

                // Set default values for easier access later.
                settings.settings = settings.settings || [];

                // The name of the settings.
                this.nameElement.text(settings.name);
                this.editNameField.val(settings.name);

                // Set the values in the first tab (receivers).
                if (settings.receiversList && settings.receiversList.length > 0) {
                    $("#StaticReceiver").prop("checked", true);
                    this.staticReceiverInput.val(settings.receiversList.join("\r\n"));
                } else if (settings.receiversDataSelectorId > 0) {
                    $("#ReceiverDataSelector").prop("checked", true);
                    this.dataSelectorDropDown.value(settings.receiversDataSelectorId);
                } else if (settings.receiversQueryId > 0) {
                    $("#ReceiverQuery").prop("checked", true);
                    this.queryDropDown.value(settings.receiversQueryId);
                }
                
                // Set the values in the second tab (data).
                // TODO
                
                // Set the values in the third tab (content).
                for (let setting of settings.settings) {
                    switch (setting.type) {
                        case "Email":
                            $("#MailToggle").prop("checked", true);
                            this.mailTemplateDropDown.value(setting.templateId || 0);
                            this.languageDropDown.value(setting.languageCode);
                            this.mailSubjectField.val(setting.subject);
                            this.mailBodyEditor.value(setting.content);
                            this.emailSelectorField.val(setting.selector);
                            break;
                        case "Sms":
                            $("#SmsToggle").prop("checked", true);
                            // TODO;
                            break;
                        default:
                            console.error(`Unknown communication type set: ${setting.Type}`);
                            break;
                    }
                }
                
                // Set the values in the fourth tab (pattern).
                // TODO
                
            } catch (exception) {
                console.error(exception);
                kendo.alert(`Er is iets fout gegaan tijdens het laden van de communicatie-instellingen met ID '${id}'. Probeer het a.u.b. opnieuw of neem contact op met ons.`);
            } finally {
                window.processing.removeProcess(process);
            }
        }

        /**
         * Get the settings as they're currently entered in all the fields by the user.
         */
        getCurrentSettings() {
            return {
                id: this.settings.settingsId,
                name: this.editNameField.val()
                // TODO: Rest of the settings.
            };
        }
    }

    // Initialize the Communication class and make one instance of it globally available.
    window.communicationSettings = new CommunicationSettings(settings);
})(communicationModuleSettings);
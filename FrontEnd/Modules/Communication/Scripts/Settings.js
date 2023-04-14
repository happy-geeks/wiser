import { TrackJS } from "trackjs";
import { Wiser, Misc } from "../../Base/Scripts/Utils.js";
import { DateTime } from "luxon";
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

            // Enumerations.
            this.triggerWeekDays = Object.freeze({
                Monday: 1,
                Tuesday: 2,
                Wednesday: 4,
                Thursday: 8,
                Friday: 16,
                Saturday: 32,
                Sunday: 64
            });

            // Components.
            this.nameElement = null;
            this.editNameButton = null;
            this.editNameField = null;
            this.deleteButton = null;
            this.saveButton = null;
            this.mainTabStrip = null;
            this.dataSelectorForReceiversDropDown = null;
            this.queryForReceiversDropDown = null;
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
            this.recurringWeeklyContainer = null;
            this.recurringMonthlyContainer = null;
            this.phoneNumberSelectorField = null;
            this.smsMessageField = null;
            this.mailToggleCheckBox = null;
            this.smsToggleCheckBox = null;
            this.dataSelectorForContentDropDown = null;
            this.queryForContentDropDown = null;

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
            this.settings.username = user.adminAccountName ? `${user.adminAccountName} (Admin)` : user.name;
            this.settings.adminAccountLoggedIn = !!user.adminAccountName;

            const userData = await Wiser.getLoggedInUserData(this.settings.wiserApiRoot);
            this.settings.userId = userData.encryptedId;
            this.settings.customerId = userData.encryptedCustomerId;
            this.settings.zeroEncrypted = userData.zeroEncrypted;

            this.settings.serviceRoot = `${this.settings.wiserApiRoot}templates/get-and-execute-query`;
            this.settings.getItemsUrl = `${this.settings.wiserApiRoot}data-selectors`;

            await this.initializeComponents();

            this.setupBindings();

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
         * Initializes all Kendo components for the base class.
         */
        async initializeComponents() {
            const process = `loadDropdowns_${Date.now()}`;
            window.processing.addProcess(process);

            this.editNameButton = $("#EditNameButton");
            this.deleteButton = $("#DeleteButton");
            this.nameElement = $("#CurrentName");
            this.editNameField = $("#EditNameField");
            this.staticReceiverInput = $("#StaticReceiverInput");
            this.mailSubjectField = $("#MailSubject");
            this.emailSelectorField = $("#EmailSelector");
            this.recurringWeeklyContainer = $("#RecurringWeeklyContainer");
            this.recurringMonthlyContainer = $("#RecurringMonthlyContainer");
            this.phoneNumberSelectorField = $("#PhoneNumberSelector");
            this.smsMessageField = $("#SmsMessage");
            this.mailToggleCheckBox = $("#MailToggle");
            this.smsToggleCheckBox = $("#SmsToggle");

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
                    $("#DataSelectorForReceiversContainer").hide();
                } else {
                    this.dataSelectorForReceiversDropDown = $("#DataSelectorForReceiverDropDown").kendoDropDownList({
                        optionLabel: "Selecteer een reeds bestaande dataselector",
                        dataTextField: "name",
                        dataValueField: "id",
                        dataSource: dataSelectors
                    }).data("kendoDropDownList");
                }

                if (!queries || !queries.length) {
                    $("#QueryForReceiversContainer").hide();
                } else {
                    this.queryForReceiversDropDown = $("#QueryForReceiversDropDown").kendoDropDownList({
                        optionLabel: "Selecteer een vooraf ingestelde query",
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
                        dataValueField: "code",
                        dataSource: languages,
                        change: this.onLanguageDropDownChange.bind(this)
                    }).data("kendoDropDownList");
                }

                // Data tab.
                if (!dataSelectors || !dataSelectors.length) {
                    $("#DataSelectorForContentContainer").hide();
                } else {
                    this.dataSelectorForContentDropDown = $("#DataSelectorForContentDropDown").kendoDropDownList({
                        optionLabel: "Selecteer een reeds bestaande dataselector",
                        dataTextField: "name",
                        dataValueField: "id",
                        dataSource: dataSelectors
                    }).data("kendoDropDownList");
                }

                if (!queries || !queries.length) {
                    $("#QueryForContentContainer").hide();
                } else {
                    this.queryForContentDropDown = $("#QueryForContentDropDown").kendoDropDownList({
                        optionLabel: "Selecteer een vooraf ingestelde query",
                        dataTextField: "description",
                        dataValueField: "id",
                        dataSource: queries
                    }).data("kendoDropDownList");
                }

                const wiserApiRoot = this.settings.wiserApiRoot;

                const translationsTool = {
                    name: "wiserTranslation",
                    tooltip: "Vertaling invoegen",
                    exec: function(e) { Wiser.onHtmlEditorTranslationExec.call(Wiser, e, $(this).data("kendoEditor"), wiserApiRoot); }
                };

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
                        translationsTool,
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
                }).data("kendoDropDownList");

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

            this.editNameButton.click(this.onEditNameButtonClick.bind(this));
            this.deleteButton.click(this.onDeleteButtonClick.bind(this));
            this.editNameField.blur(this.onEditNameFieldBlur.bind(this));
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
            switch (event.sender.value().toLowerCase()) {
                case "week":
                    this.recurringWeeklyContainer.removeClass("hidden");
                    this.recurringMonthlyContainer.addClass("hidden");
                    break;
                case "month":
                    this.recurringWeeklyContainer.addClass("hidden");
                    this.recurringMonthlyContainer.removeClass("hidden");
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
                    url: `${this.settings.wiserApiRoot}communications/${this.settings.settingsId}`,
                    method: "DELETE"
                });

                // Don't remove the process if everything succeeded, so that the loader will stay visible until the index page has finished loading.
                window.location = "/Modules/Communication";
            } catch (exception) {
                console.error(exception);
                kendo.alert(`Er is iets fout gegaan tijdens het verwijderen van de communicatie-instellingen met ID '${this.settings.settingsId}'. Probeer het a.u.b. opnieuw of neem contact op met ons.`);
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

                // Check if all mandatory settings have been entered.
                if (!settings.name) {
                    kendo.alert("Vul a.u.b. een naam in");
                    return;
                }

                if (!settings.receiversDataSelectorId && !settings.receiversQueryId && (!settings.receiversList || !settings.receiversList.length)) {
                    kendo.alert("Vul a.u.b. de ontvangers in");
                    return;
                }

                if (!settings.settings || !settings.settings.length || !settings.settings.some(x => !!x.content)) {
                    kendo.alert("Vul a.u.b. in wat voor bericht er gestuurd moet worden.");
                    return;
                }

                if (!settings.sendTriggerType) {
                    kendo.alert("Vul a.u.b. een verzendpatroon in.")
                    return;
                }

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
                kendo.alert(`Er is iets fout gegaan tijdens het laden van de communicatie-instellingen met ID '${this.settings.settingsId}'. Probeer het a.u.b. opnieuw of neem contact op met ons.`);
            } finally {
                window.processing.removeProcess(process);
            }
        }

        /**
         * Event for then the user leaves the edit name field.
         * @param event The blur event.
         */
        onEditNameFieldBlur(event) {
            this.nameElement.removeClass("hidden").text(this.editNameField.val());
            this.editNameField.addClass("hidden");
            this.editNameButton.removeClass("hidden");
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

            if (this.mailBodyEditor.value() || this.mailSubjectField.val()) {
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
            let subjectPropertyName = "Onderwerp"
            if (selectedLanguage && selectedLanguage.code) {
                templatePropertyName += ` (${selectedLanguage.code})`;
                subjectPropertyName += ` (${selectedLanguage.code})`;
            }

            const html = selectedMailTemplate[templatePropertyName] || selectedMailTemplate.Template || selectedMailTemplate.template || "";
            const subject = selectedMailTemplate[subjectPropertyName] || selectedMailTemplate.Onderwerp || selectedMailTemplate.onderwerp || "";
            this.mailBodyEditor.value(html);
            this.mailSubjectField.val(subject);
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
                    $("#DataSelectorForReceiver").prop("checked", true);
                    this.dataSelectorForReceiversDropDown.value(settings.receiversDataSelectorId);
                } else if (settings.receiversQueryId > 0) {
                    $("#QueryForReceivers").prop("checked", true);
                    this.queryForReceiversDropDown.value(settings.receiversQueryId);
                }

                // Set the values in the second tab (data).
                if (settings.contentDataSelectorId > 0) {
                    $("#DataSelectorForContent").prop("checked", true);
                    this.dataSelectorForContentDropDown.value(settings.contentDataSelectorId);
                } else if (settings.contentQueryId > 0) {
                    $("#QueryForContent").prop("checked", true);
                    this.queryForContentDropDown.value(settings.contentQueryId);
                }

                // Set the values in the third tab (content).
                for (let setting of settings.settings) {
                    switch (setting.type) {
                        case "Email":
                            this.mailToggleCheckBox.prop("checked", true);
                            this.mailTemplateDropDown.value(setting.templateId || 0);
                            this.languageDropDown.value(setting.languageCode);
                            this.mailSubjectField.val(setting.subject);
                            this.mailBodyEditor.value(setting.content);
                            this.emailSelectorField.val(setting.selector);
                            break;
                        case "Sms":
                            this.smsToggleCheckBox.prop("checked", true);
                            this.phoneNumberSelectorField.val(setting.selector);
                            this.smsMessageField.val(setting.content);
                            break;
                        default:
                            console.error(`Unknown communication type set: ${setting.type}`);
                            break;
                    }
                }

                // Set the values in the fourth tab (pattern).
                switch (settings.sendTriggerType) {
                    case "Direct":
                        $("#Direct").prop("checked", true);
                        break;
                    case "Fixed":
                        $("#Fixed").prop("checked", true);
                        this.fixedDateTimePicker.value(settings.triggerStart);
                        break;
                    case "Recurring":
                        $("#Recurring").prop("checked", true);
                        this.recurringDateRangePicker.range({
                            start: new Date(settings.triggerStart),
                            end: new Date(settings.triggerEnd)
                        });
                        if (settings.triggerTime) {
                            this.recurringTimePicker.value(settings.triggerTime);
                        }
                        this.recurringPeriodValueField.value(settings.triggerPeriodValue);
                        this.recurringPeriodTypeDropDown.value(settings.triggerPeriodType);
                        this.recurringPeriodTypeDropDown.trigger("change");

                        switch (settings.triggerPeriodType) {
                            case "Week":
                                for (let weekDay in this.triggerWeekDays) {
                                    if (!this.triggerWeekDays.hasOwnProperty(weekDay)) {
                                        continue;
                                    }

                                    const day = this.triggerWeekDays[weekDay];
                                    if ((settings.triggerWeekDays & day) === day) {
                                        this.recurringWeeklyContainer.find(`input[type='checkbox'][name='recurringWeekDay'][value='${day}']`).prop("checked", true);
                                    }
                                }
                                break;
                            case "Month":
                                this.recurringDayOfMonthField.value(settings.triggerDayOfMonth);
                                break;
                            default:
                                console.error(`Unknown trigger period type set: ${settings.triggerPeriodType}`);
                                break;
                        }
                        break;
                    default:
                        console.error(`Unknown send trigger type set: ${settings.sendTriggerType}`);
                        break;
                }
            } catch (exception) {
                console.error(exception);
                kendo.alert(`Er is iets fout gegaan tijdens het opslaan van de communicatie-instellingen met ID '${id}'. Probeer het a.u.b. opnieuw of neem contact op met ons.`);
            } finally {
                window.processing.removeProcess(process);
            }
        }

        /**
         * Get the settings as they're currently entered in all the fields by the user.
         */
        getCurrentSettings() {
            // Basic settings.
            const result = {
                id: this.settings.settingsId,
                name: this.editNameField.val(),
                settings: []
            };

            // Settings for first tab (receivers).
            const selectedReceiverType = $("input[type='radio'][name='receiverType']:checked").val();
            switch (selectedReceiverType) {
                case "static": {
                    result.receiversList = [];
                    const inputValue = this.staticReceiverInput.val();
                    if (inputValue) {
                        // Split receivers on comma, semicolon and newlines, so that it doesn't matter much how users separate them.
                        result.receiversList = inputValue.split(/[;,\r\n]/).map(value => value.trim());
                    }
                    break;
                }
                case "dataSelector":
                    result.receiversDataSelectorId = parseInt(this.dataSelectorForReceiversDropDown.value()) || 0;
                    break;
                case "query":
                    result.receiversQueryId = parseInt(this.queryForReceiversDropDown.value()) || 0;
                    break;
                default:
                    console.error(`Unknown receiver type set: ${selectedReceiverType}`);
                    break;
            }

            // Settings for second tab (data).
            const selectedContentDataType = $("input[type='radio'][name='contentDataType']:checked").val();
            switch (selectedContentDataType) {
                case "dataSelector":
                    result.contentDataSelectorId = parseInt(this.dataSelectorForContentDropDown.value()) || 0;
                    break;
                case "query":
                    result.contentQueryId = parseInt(this.queryForContentDropDown.value()) || 0;
                    break;
                default:
                    console.error(`Unknown receiver type set: ${selectedReceiverType}`);
                    break;
            }

            // Settings for third tab (content).
            if (this.mailToggleCheckBox.prop("checked")) {
                result.settings.push({
                    type: "Email",
                    content: this.mailBodyEditor.value(),
                    templateId: parseInt(this.mailTemplateDropDown.value()) || 0,
                    languageCode: this.languageDropDown.value(),
                    subject: this.mailSubjectField.val(),
                    selector: this.emailSelectorField.val()
                });
            }

            if (this.smsToggleCheckBox.prop("checked")) {
                // No else-if here, it's possible to send both an e-mail and SMS at the same time.
                result.settings.push({
                    type: "Sms",
                    content: this.smsMessageField.val(),
                    selector: this.phoneNumberSelectorField.val()
                });
            }

            // Settings for fourth tab (pattern).
            result.sendTriggerType = $("input[type='radio'][name='sendMoment']:checked").val();
            switch (result.sendTriggerType) {
                case "Direct":
                    // Nothing to do here, the direct option does not have any extra settings.
                    break;
                case "Fixed":
                    result.triggerStart = this.fixedDateTimePicker.value();
                    break;
                case "Recurring":
                    const dateRange = this.recurringDateRangePicker.range();
                    result.triggerStart = dateRange.start;
                    result.triggerEnd = dateRange.end;
                    result.triggerTIme = DateTime.fromJSDate(this.recurringTimePicker.value()).toSQLTime({ includeOffset: false });
                    result.triggerPeriodValue = this.recurringPeriodValueField.value();
                    result.triggerPeriodType = this.recurringPeriodTypeDropDown.value();
                    switch (result.triggerPeriodType) {
                        case "Week":
                            let triggerWeekDays = 0;

                            this.recurringWeeklyContainer.find(`input[type='checkbox'][name='recurringWeekDay']:checked`).each((index, element) => {
                                triggerWeekDays += parseInt(element.value);
                            });

                            result.triggerWeekDays = triggerWeekDays;
                            break;
                        case "Month":
                            result.triggerDayOfMonth = this.recurringDayOfMonthField.value();
                            break;
                        default:
                            console.error(`Unknown trigger period type set: ${result.triggerPeriodType}`);
                            break;
                    }
                    break;
                default:
                    console.error(`Unknown send trigger type set: ${result.sendTriggerType}`);
                    break;
            }

            return result;
        }
    }

    // Initialize the Communication class and make one instance of it globally available.
    window.communicationSettings = new CommunicationSettings(settings);
})(communicationModuleSettings);
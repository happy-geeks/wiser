import { Wiser } from "../../Base/Scripts/Utils.js";
import "../../Base/Scripts/Processing.js";

require("@progress/kendo-ui/js/kendo.all.js");
require("@progress/kendo-ui/js/cultures/kendo.culture.nl-NL.js");
require("@progress/kendo-ui/js/messages/kendo.messages.nl-NL.js");

export class RemoveItems {
    constructor(settings) {
        this.mainLoader = null;
        this.messageEventCallbacks = {};

        // Default settings
        this.settings = {
            tenantId: 0,
            username: "Onbekend"
        };
        Object.assign(this.settings, settings);

        // Fire event on page ready for direct actions
        $(document).ready(() => {
            this.onPageReady();
        });
    }

    async onPageReady() {
        this.dataSelectorIframe = document.getElementById("dataSelectorIframe");
        
        // Add logged in user access token to default authorization headers for all jQuery ajax requests.
        $.ajaxSetup({
            headers: { "Authorization": `Bearer ${localStorage.getItem("accessToken")}` }
        });

        this.mainLoader = $("#mainLoader");

        // Setup processing.
        document.addEventListener("processing.Busy", this.toggleMainLoader.bind(this, true));
        document.addEventListener("processing.Idle", this.toggleMainLoader.bind(this, false));

        // Setup any settings from the body element data. These settings are added via the Wiser backend and they take preference.
        Object.assign(this.settings, $("body").data());

        if (!this.settings.wiserApiRoot.endsWith("/")) {
            this.settings.wiserApiRoot += "/";
        }

        window.addEventListener("message", (e) => {
            if (e.data.hasOwnProperty("from") && e.data.from === "DataSelector") {
                const callback = e.data.callback || "";
                if (callback !== "") {
                    this.executeMessageEventCallback(callback, e.data);
                }
            }
        });

        this.initializeKendoComponents();
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
     * @param {HTMLElement} context The context (HTML element) in which items will have their elements initialized with Kendo.
     */
    async initializeKendoComponents(context = null) {
        if (!context || !(context instanceof HTMLElement)) {
            context = document.body;
        }

        let me = this;

        //File upload
        $(".removeItemsFileUpload").kendoUpload({
            async: {
                saveUrl: "/Modules/ImportExport/Import/Upload?type=feed",
                autoUpload: true
            },
            localization: {
                select: "Selecteer bestand",
                invalidFileExtension: "Data bestand moet een CSV bestand zijn",
                invalidMaxFileSize: "Bestand mag maar maximaal 25 MB zijn"
            },
            validation: {
                allowedExtensions: [".csv"],
                maxFileSize: 26214400 // 25 MB = 25 * 1024 * 1024
            },
            multiple: false,
            success: (e) => {
                if (e.operation !== "upload") {
                    return;
                }

                this.importFilename = e.response.filename;
                if (e.response.rowCount > e.response.importLimit) {
                    Wiser.alert({
                        title: "Import limiet overschreden",
                        content: `De import bevat meer dan ${e.response.importLimit} rijen. Alleen de eerste ${e.response.importLimit} van de ${e.response.rowCount} rijen zullen worden geïmporteerd.`
                    });
                }
            }
        });

        //Combox
        const process = `loadDropdowns_${Date.now()}`;
        window.processing.addProcess(process);

        try {
            const promiseResults = await Promise.all([
                Wiser.api({ url: `${this.settings.wiserApiRoot}entity-types?onlyEntityTypesWithDisplayName=false` })
            ]);
            const entityTypes = promiseResults[0];

            if (!entityTypes || !entityTypes.length) {
                $(context).find("#EntityTypesContainer").hide();
            } else {
                $(context).find("#EntityTypesContainer").kendoComboBox({
                    dataTextField: "displayName",
                    dataValueField: "id",
                    dataSource: entityTypes,
                    change: function (e) {
                        me.loadEntityProperties(this.value(), true);
                    },
                    optionLabel: "Maak uw keuze...",
                    template: "#: displayName # # if(typeof(moduleName) !== 'undefined' && moduleName) { # (#: moduleName #) # } #"
                });

                await this.loadEntityProperties("<none>", false, context);
            }
        } catch (exception) {
            console.error(exception);
            kendo.alert("Er is iets fout gegaan. Probeer het a.u.b. opnieuw of neem contact op met ons.");
        }

        window.processing.removeProcess(process);

        //Button
        $(context).find("#deleteItemsButton").kendoButton({
            click: async function (e) {
                await me.prepareDelete();
            }
        });
    }

    //Get all the properties of a specific entity and replace the combox with it's values.
    async loadEntityProperties(entityName, ownProcess = false, context = null) {
        if (!context || !(context instanceof HTMLElement)) {
            context = document.body;
        }

        let process = null;
        if (ownProcess) {
            process = `loadEntityProperties_${Date.now()}`;
            window.processing.addProcess(process);
        }

        try {
            const promiseResults = await Promise.all([
                Wiser.api({ url: `${this.settings.wiserApiRoot}entity-properties/${entityName}?onlyEntityTypesWithDisplayName=false&onlyEntityTypesWithPropertyName=true&addIdProperty=true` })
            ]);
            const entityProperties = promiseResults[0];

            if (!entityProperties || !entityProperties.length) {
                $(context).find("#EntityPropertiesContainer").hide();
            } else {
                $(context).find("#EntityPropertiesContainer").kendoDropDownList({
                    dataTextField: "displayName",
                    dataValueField: "propertyName",
                    dataSource: entityProperties,
                    optionLabel: "Maak uw keuze...",
                    template: "#: displayName # # if (tabName != null && tabName !== '') { # - Tab: #: tabName # # } #"
                });
            }
        } catch (exception) {
            console.error(exception);
            kendo.alert("Er is iets fout gegaan. Probeer het a.u.b. opnieuw of neem contact op met ons.");
        }

        if (ownProcess && process) {
            window.processing.removeProcess(process);
        }
    }

    //Let the API prepare the delete to retrieve all information for delete.
    async prepareDelete() {
        let context = document.body;

        let deleteByFile = $(context).find("#DeleteItemsFile").is(":checked");
        let deleteByDataSelector = $(context).find("#DeleteItemsDataSelector").is(":checked");

        if (deleteByFile) {
            if (!this.importFilename || this.importFilename === "") {
                Wiser.showMessage({
                    title: "Ongeldig bestand",
                    content: "Er is geen bestand geüpload om te gebruiken voor het verwijderen van items."
                });
                return;
            }

            let entityName = $(context).find("#EntityTypesContainer").data("kendoComboBox").value();
            let propertyName = $(context).find("#EntityPropertiesContainer").data("kendoDropDownList").value();

            if (entityName === "") {
                Wiser.showMessage({
                    title: "Entiteit mist",
                    content: "Er is geen entiteit gekozen waarbinnen items verwijdert moeten worden."
                });
                return;
            }

            if (propertyName === "") {
                Wiser.showMessage({
                    title: "Eigenschap mist",
                    content: "Er is geen eigenschap gekozen om de waardes in het bestand mee te vergelijken."
                });
                return;
            }

            const result = await Wiser.api({
                url: `${this.settings.wiserApiRoot}imports/delete-items/prepare`,
                method: "POST",
                contentType: "application/json",
                data: JSON.stringify({
                    filePath: this.importFilename,
                    entityName: entityName,
                    propertyName: propertyName
                })
            });

            await this.prepareDeleteFinished(result);
        }
        else if (deleteByDataSelector) {
            this.messageEventCallbacks["dataSelectorResults"] = async (response) => {
                this.messageEventCallbacks["dataSelectorEntityType"] = async (entityType) => {
                    var result = {
                        entityType: entityType.actionResult,
                        ids: []
                    };

                    response.actionResult.forEach(element => {
                        result.ids.push(element.id);
                    });

                    await this.prepareDeleteFinished(result);
                }

                this.dataSelectorIframe.contentWindow.postMessage(Object.assign({
                    action: "get-entity-type",
                    callback: "dataSelectorEntityType"
                }, {}));
            };

            this.dataSelectorIframe.contentWindow.postMessage(Object.assign({
                action: "get-result",
                callback: "dataSelectorResults"
            }, {}));
        }
    }

    //Handle preparation response from the API.
    async prepareDeleteFinished(results) {
        Wiser.confirm({
            title: "Bevestig items verwijderen",
            content: `U staat op het punt om ${results.ids.length} item(s) te verwijderen. Wilt u doorgaan?`,
            actions: [{
                text: "Ok",
                action: async function (e) {
                    const result = await Wiser.api({
                        url: `${window.removeItems.settings.wiserApiRoot}imports/delete-items/confirm`,
                        method: "POST",
                        contentType: "application/json",
                        data: JSON.stringify(results)
                    });
                    
                    if (result === true) {
                        Wiser.showMessage({
                            title: "Items verwijderd",
                            content: "De items zijn verwijderd."
                        });
                    } else {
                        Wiser.showMessage({
                            title: "Items verwijderen mislukt.",
                            content: "Er is iets mis gegaan tijdens het verwijderen van de items, de actie is teruggedraaid."
                        });
                    }
                }
            }]
        });
    }

    executeMessageEventCallback(name, data = {}, keepCallback = false) {
        if (typeof name !== "string" || name.trim() === "" || !this.messageEventCallbacks.hasOwnProperty(name)) {
            return;
        }

        const callback = this.messageEventCallbacks[name];
        if (typeof callback !== "function") {
            return;
        }

        callback.call(this, data);

        // Check if callback should be remembered. In most cases you'll want to delete the callback after one use.
        if (!keepCallback) {
            delete this.messageEventCallbacks[name];
        }
    }
}
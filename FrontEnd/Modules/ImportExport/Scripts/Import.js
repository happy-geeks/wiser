import { Wiser } from "../../Base/Scripts/Utils.js";
import "../../Base/Scripts/Processing.js";
require("@progress/kendo-ui/js/kendo.all.js");
require("@progress/kendo-ui/js/cultures/kendo.culture.nl-NL.js");
require("@progress/kendo-ui/js/messages/kendo.messages.nl-NL.js");

import "../Css/ImportExport.css";

// Any custom settings can be added here. They will overwrite most default settings inside the module.
const importModuleSettings = {
};

((settings) => {
    /**
     * Main class.
     */
    class Import {
        /**
         * Initializes a new instance of Import.
         * @param {any} settings An object containing the settings for this class.
         */
        constructor(settings) {
            this.base = this;

            // Kendo components.
            this.mainWindow = null;

            this.importGrid = null;
            this.importLinksGrid = null;
            this.importLinkDetailsGrid = null;
            
            this.importUpload = null;

            this.entityNames = null;
            this.linkTypes = null;
            this.importFilename = "";
            this.importImagesFilename = "";
            this.importImagesFilePath = "";

            this.mainLoader = null;

            this.importHtml = null;

            // Set the Kendo culture to Dutch. TODO: Base this on the language in Wiser.
            kendo.culture("nl-NL");

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

        /**
         * Event that will be fired when the page is ready.
         */
        async onPageReady() {
            this.importHtml = document.getElementById("ImportHtml");
            this.importUpload = document.getElementById("import-upload");
            if (!this.importHtml) {
                this.initializeKendoWindows();
                return;
            }
            
            this.settings.importRequestsUrl = "/Modules/ImportExport/Import";

            // Add logged in user access token to default authorization headers for all jQuery ajax requests.
            $.ajaxSetup({
                headers: { "Authorization": `Bearer ${localStorage.getItem("accessToken")}` }
            });

            const html = await Wiser.api({ url: "/Modules/ImportExport/Import/Html" });
            this.importHtml.insertAdjacentHTML("beforeend", html);

            this.mainLoader = $("#mainLoader");

            // Setup processing.
            document.addEventListener("processing.Busy", this.toggleMainLoader.bind(this, true));
            document.addEventListener("processing.Idle", this.toggleMainLoader.bind(this, false));

            // Setup any settings from the body element data. These settings are added via the Wiser backend and they take preference.
            Object.assign(this.settings, $("body").data());

            if (!this.settings.wiserApiRoot.endsWith("/")) {
                this.settings.wiserApiRoot += "/";
            }
            
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

            const user = JSON.parse(localStorage.getItem("userData"));
            this.settings.oldStyleUserId = user.oldStyleUserId;
            this.settings.username = user.adminAccountName ? `${user.adminAccountName} (Admin)` : user.name;
            this.settings.adminAccountLoggedIn = user.adminAccountName;
            
            const userData = await Wiser.getLoggedInUserData(this.settings.wiserApiRoot);
            this.settings.userId = userData.encryptedId;
            this.settings.tenantId = userData.encryptedTenantId;
            this.settings.zeroEncrypted = userData.zeroEncrypted;
            this.settings.hasEmailAddress = !!userData.emailAddress;
            $("#EmailAddressContainer").toggle(!this.settings.hasEmailAddress);
            
            this.settings.serviceRoot = `${this.settings.wiserApiRoot}templates/get-and-execute-query`;
            this.settings.getItemsUrl = `${this.settings.wiserApiRoot}data-selectors`;

            await this.readServerData();
            this.setupBindings();

            this.initializeKendoWindows();
            this.initializeKendoComponents(this.importHtml);
            this.initializeKendoComponents(this.importUpload);

            this.toggleMainLoader(false);
        }

        async readServerData() {
            const promises = [];
            promises.push(new Promise((resolve) => {
                Wiser.api({ url: `${this.settings.serviceRoot}/IMPORTEXPORT_GET_ENTITY_NAMES` }).then((data) => {
                    this.entityNames = data;
                    resolve();
                });
            }));
            promises.push(new Promise((resolve) => {
                Wiser.api({ url: `${this.settings.serviceRoot}/IMPORTEXPORT_GET_LINK_TYPES` }).then((data) => {
                    this.linkTypes = data;
                    resolve();
                });
            }));

            await Promise.all(promises);
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
            this.startImportButton = document.getElementById("startImportButton");
            this.startImportButton.addEventListener("click", this.performImport.bind(this));

            if (!window.importExport) {
                document.addEventListener("moduleClosing", (event) => {
                    // You can do anything here that needs to happen before closing the module.
                    event.detail();
                });
            }
        }

        updateImportGrid(data) {
            const dataSource = {
                data: [],
                autoSync: true,
                schema: {
                    model: {
                        fields: {
                            column: { editable: false },
                            moduleId: { editable: true, type: "number" },
                            importTo: { editable: true, type: "string" },
                            specName: { editable: true, type: "string" },
                            propertyName: { editable: true, type: "string" },
                            languageCode: { editable: true, type: "string" },
                            isImageField: { editable: true, type: "boolean" },
                            allowMultipleImages: { editable: true, type: "boolean" }
                        }
                    }
                }
            };

            const linksDataSource = {
                data: [],
                autoSync: true,
                schema: {
                    model: {
                        fields: {
                            column: { editable: false },
                            linkType: { editable: true, type: "number" },
                            linkName: { editable: true, type: "string" },
                            linkIsDestination: { editable: true, type: "boolean" },
                            deleteExistingLinks: { editable: true, type: "boolean" }
                        }
                    }
                }
            };
            const linkDetailsDataSource = {
                data: [],
                autoSync: true,
                schema: {
                    model: {
                        fields: {
                            column: { editable: false },
                            moduleId: { editable: true, type: "number" },
                            linkType: { editable: true, type: "number" },
                            linkName: { editable: true, type: "string" },
                            specName: { editable: true, type: "string" },
                            propertyName: { editable: true, type: "string" },
                            languageCode: { editable: true, type: "string" },
                            isImageField: { editable: true, type: "boolean" },
                            allowMultipleImages: { editable: true, type: "boolean" }
                        }
                    }
                }
            };

            data.forEach((element) => {
                dataSource.data.push({
                    column: element,
                    moduleId: 0,
                    importTo: "",
                    specName: ""
                });

                linksDataSource.data.push({
                    column: element,
                    linkType: 0,
                    linkName: "",
                    linkIsDestination: true,
                    deleteExistingLinks: false
                });

                linkDetailsDataSource.data.push({
                    column: element,
                    moduleId: 0,
                    linkType: 0,
                    linkName: "",
                    specName: ""
                });
            });

            this.importGrid.setDataSource(dataSource);
            this.importLinksGrid.setDataSource(linksDataSource);
            this.importLinkDetailsGrid.setDataSource(linkDetailsDataSource);
        }

        validateImportSettings(selectedImportFields) {
            if (!Array.isArray(selectedImportFields)) {
                return { valid: false, message: "Er is een fout opgetreden in de validatie. Probeer het nogmaals." };
            }

            // A length of 0 is not invalid, as it's possible to only import item links.
            if (selectedImportFields.length === 0) {
                return { valid: true, message: "" };
            }

            // Validate entity name.
            const missingEntityName = selectedImportFields.some((item) => {
                const dataItem = this.importGrid.dataItem(item);
                return typeof dataItem.importTo !== "string" || dataItem.importTo === "";
            });
            if (missingEntityName) {
                return { valid: false, message: "Kies eerst een entiteit om de data naar toe te importeren." };
            }

            // Validate property names.
            const missingPropertyName = selectedImportFields.some((item) => {
                const dataItem = this.importGrid.dataItem(item);
                return typeof dataItem.propertyName !== "string" || dataItem.propertyName === "";
            });
            if (missingPropertyName) {
                return { valid: false, message: "Voor een of meerdere velden is nog geen specificatienaam gekozen." };
            }

            // No errors found, assume settings are valid.
            return { valid: true, message: "" };
        }

        validateImportLinksSettings(selectedImportLinksFields) {
            if (!Array.isArray(selectedImportLinksFields)) {
                return { valid: false, message: "Er is een fout opgetreden in de validatie. Probeer het nogmaals." };
            }

            // A length of 0 is not invalid, as it's not required to import item links.
            if (selectedImportLinksFields.length === 0) {
                return { valid: true, message: "" };
            }

            // Validate link types.
            const missingLinkType = selectedImportLinksFields.some((item) => {
                const dataItem = this.importLinksGrid.dataItem(item);
                return typeof dataItem.linkType !== "number" || dataItem.linkType === 0;
            });
            if (missingLinkType) {
                return { valid: false, message: "Voor een of meerdere velden is nog geen koppeltype gekozen." };
            }

            // No errors found, assume settings are valid.
            return { valid: true, message: "" };
        }

        validateImportLinkDetailsSettings(selectedImportLinkDetailsFields) {
            if (!Array.isArray(selectedImportLinkDetailsFields)) {
                return { valid: false, message: "Er is een fout opgetreden in de validatie. Probeer het nogmaals." };
            }

            // A length of 0 is not invalid, as it's not required to import item links.
            if (selectedImportLinkDetailsFields.length === 0) {
                return { valid: true, message: "" };
            }

            // Validate link types.
            const missingLinkType = selectedImportLinkDetailsFields.some((item) => {
                const dataItem = this.importLinkDetailsGrid.dataItem(item);
                return typeof dataItem.linkType !== "number" || dataItem.linkType === 0;
            });
            if (missingLinkType) {
                return { valid: false, message: "Voor een of meerdere velden is nog geen koppeltype gekozen." };
            }

            // Validate property names.
            const missingPropertyName = selectedImportLinkDetailsFields.some((item) => {
                const dataItem = this.importLinkDetailsGrid.dataItem(item);
                return typeof dataItem.propertyName !== "string" || dataItem.propertyName === "";
            });
            if (missingPropertyName) {
                return { valid: false, message: "Voor een of meerdere velden is nog geen specificatienaam gekozen." };
            }

            // No errors found, assume settings are valid.
            return { valid: true, message: "" };
        }

        async performImport() {
            const button = $(this.startImportButton).data("kendoButton");
            const process = `performImport_${Date.now()}`;

            try {
                const importSettings = [];
                const importLinkSettings = [];
                const importLinkDetailSettings = [];

                const selectedImportFields = this.importGrid.select().toArray();
                const selectedImportLinksFields = this.importLinksGrid.select().toArray();
                const selectedImportLinkDetailsFields = this.importLinkDetailsGrid.select().toArray();

                if (selectedImportFields.length === 0 && selectedImportLinksFields.length === 0) {
                    Wiser.showMessage({ title: "Import ongeldig", content: "Kies eerst eigenschappen of koppelingen om te importeren." });
                    return;
                }

                if (selectedImportFields.length > 0) {
                    // Import item details grid.
                    const importFieldsValidation = this.validateImportSettings(selectedImportFields);
                    if (!importFieldsValidation.valid) {
                        Wiser.showMessage({ title: "Eigenschappen ongeldig", content: importFieldsValidation.message });
                        return;
                    }

                    selectedImportFields.forEach((element) => {
                        const dataItem = this.importGrid.dataItem(element);

                        importSettings.push({
                            column: dataItem.column,
                            entityType: dataItem.importTo,
                            moduleId: dataItem.moduleId,
                            propertyName: dataItem.propertyName,
                            languageCode: dataItem.languageCode || "",
                            isImageField: dataItem.isImageField,
                            allowMultipleImages: dataItem.allowMultipleImages
                        });
                    });
                }

                if (selectedImportLinksFields.length > 0) {
                    // Import item links grid.
                    const importLinksFieldsValidation = this.validateImportLinksSettings(selectedImportLinksFields);
                    if (!importLinksFieldsValidation.valid) {
                        Wiser.showMessage({ title: "Koppelingen ongeldig", content: importLinksFieldsValidation.message });
                        return;
                    }

                    selectedImportLinksFields.forEach((element) => {
                        const dataItem = this.importLinksGrid.dataItem(element);

                        importLinkSettings.push({
                            column: dataItem.column,
                            linkType: dataItem.linkType,
                            linkName: dataItem.linkName,
                            linkIsDestination: dataItem.linkIsDestination,
                            deleteExistingLinks: dataItem.deleteExistingLinks
                        });
                    });

                    // We can only import details/fields on item links if we know the parent ID. This is why this code is inside the selectedImportLinksFields block.
                    if (selectedImportLinkDetailsFields.length > 0) {
                        // Import link details grid.
                        const importLinkFieldsValidation = this.validateImportLinkDetailsSettings(selectedImportLinkDetailsFields);
                        if (!importLinkFieldsValidation.valid) {
                            Wiser.showMessage({ title: "Eigenschappen van koppeling ongeldig", content: importLinkFieldsValidation.message });
                            return;
                        }

                        selectedImportLinkDetailsFields.forEach((element) => {
                            const dataItem = this.importLinkDetailsGrid.dataItem(element);

                            importLinkDetailSettings.push({
                                column: dataItem.column,
                                linkType: dataItem.linkType,
                                linkName: dataItem.linkName,
                                moduleId: dataItem.moduleId,
                                propertyName: dataItem.propertyName,
                                languageCode: dataItem.languageCode || "",
                                isImageField: dataItem.isImageField,
                                allowMultipleImages: dataItem.allowMultipleImages
                            });
                        });
                    }
                }

                window.processing.addProcess(process);

                button.enable(false);
                const data = {
                    emailAddress: (this.settings.hasEmailAddress ? null : document.getElementById("UserEmailAddress").value),
                    name: document.getElementById("ImportName").value,
                    startDate: JSON.stringify($("#StartDateTime").data("kendoDateTimePicker").value()).replace(/"/g, "")
                };

                data["fileName"] = this.importFilename;
                data["imagesFileName"] = this.importImagesFilename;
                data["imagesFilePath"] = this.importImagesFilePath;
                data.importSettings = importSettings;
                data.importLinkSettings = importLinkSettings;
                data.importLinkDetailSettings = importLinkDetailSettings;

                const results = await Wiser.api({
                    url: `${this.settings.wiserApiRoot}imports/prepare`,
                    method: "POST",
                    contentType: "application/json",
                    data: JSON.stringify({
                        filePath: this.importFilename,
                        imagesFileName: this.importImagesFilename,
                        imagesFilePath: this.importImagesFilePath,
                        importSettings: importSettings,
                        importLinkSettings: importLinkSettings,
                        importLinkDetailSettings: importLinkDetailSettings,
                        emailAddress: (window.import.settings.hasEmailAddress ? null : document.getElementById("UserEmailAddress").value),
                        name: document.getElementById("ImportName").value,
                        startDate: $("#StartDateTime").data("kendoDateTimePicker").value()
                    })
                });

                if (!results || results.failed > 0) {
                    if (results && Wiser.validateArray(results.errors)) {
                        console.error(`Import reported these errors:\n${results.errors.join("\n")}`);
                    }

                    let userFriendlyErrors = "";
                    if (results && Wiser.validateArray(results.userFriendlyErrors)) {
                        userFriendlyErrors = `<br><br><ul><li>${results.userFriendlyErrors.join("</li><li>")}</li></ul>`;
                    }
                    if (results && Wiser.validateArray(results.userFriendlyErrors)) {
                        userFriendlyErrors = `<br><br><ul><li>${results.userFriendlyErrors.join("</li><li>")}</li></ul>`;
                    }

                    Wiser.showMessage({
                        title: "Import gefaald",
                        content: `De import kan niet uitgevoerd worden vanwege een fout. Controleer of alles goed is ingevuld en probeer het opnieuw, of neem contact met ons op.${userFriendlyErrors}`
                    });
                } else {
                    // Remove user settings from session storage if we just updated the email address, so that it will be up-to-date.
                    if (!window.import.settings.hasEmailAddress) {
                        sessionStorage.removeItem("userSettings");
                    }

                    Wiser.showMessage({
                        title: "Import geslaagd",
                        content: "De import is bezig. U ontvangt een bericht wanneer deze klaar is."
                    });
                }
            } catch (exception) {
                console.error(exception);
                Wiser.showMessage({
                    title: "Import gefaald",
                    content: "De import kan niet uitgevoerd worden vanwege een fout. Controleer of alles goed is ingevuld en probeer het opnieuw, of neem contact met ons op."
                });
            }
            
            button.enable(true);
            window.processing.removeProcess(process);
        }

        /**
         * Initializes all Kendo Window components for the base class.
         */
        initializeKendoWindows() {
            if (!window.importExport) {
                // The main window of the module.
                this.mainWindow = $("#window").kendoWindow({
                    width: "90%",
                    height: "90%",
                    title: false,
                    visible: true,
                    resizable: false
                }).data("kendoWindow").maximize().open();
            }

            this.importUploadWindow = $(this.importUpload).kendoWindow({
                width: "350",
                title: "Bestanden uploaden",
                visible: false
            }).data("kendoWindow");
        }

        /**
         * Initializes all Kendo components for the base class.
         * @param {HTMLElement} context The context (HTML element) in which items will have their elements initialized with Kendo.
         */
        initializeKendoComponents(context = null) {
            if (!context || !(context instanceof HTMLElement)) {
                context = document.body;
            }

            //UPLOADER
            $(context).find(".fileUpload").kendoUpload({
                async: {
                    saveUrl: `${this.settings.importRequestsUrl}/Upload?type=feed`,
                    removeUrl: `${this.settings.importRequestsUrl}/Delete`,
                    autoUpload: true
                },
                localization: {
                    select: "Selecteer bestand",
                    invalidFileExtension: "Data bestand moet een CSV bestand zijn",
                    invalidMaxFileSize: "Bestand mag maar maximaal 25 MB zijn"
                },
                validation: {
                    allowedExtensions: [".csv", ".xlsx"],
                    maxFileSize: 26214400 // 25 MB = 25 * 1024 * 1024
                },
                multiple: false,
                success: (e) => {
                    if (e.operation !== "upload") {
                        return;
                    }

                    this.updateImportGrid(e.response.columns);
                    this.importFilename = e.response.filename;
                    if (e.response.rowCount > e.response.importLimit) {
                        Wiser.alert({
                            title: "Import limiet overschreden",
                            content: `De import bevat meer dan ${e.response.importLimit} rijen. Alleen de eerste ${e.response.importLimit} van de ${e.response.rowCount} rijen zullen worden geïmporteerd.`
                        });
                    }
                }
            });

            $(context).find(".imgUpload").kendoUpload({
                async: {
                    chunkSize: 2097152, // 2 MB
                    saveUrl: `${this.settings.importRequestsUrl}/Upload?type=images`,
                    removeUrl: `${this.settings.importRequestsUrl}/Delete`,
                    autoUpload: true
                },
                localization: {
                    select: "Selecteer afbeeldingen",
                    invalidFileExtension: "Alleen ZIP bestanden zijn toegestaan",
                    invalidMaxFileSize: "Totale grootte mag maar maximaal 400 MB zijn"
                },
                validation: {
                    allowedExtensions: [".zip"],
                    maxFileSize: 419430400 // 400 MB
                },
                multiple: false,
                success: (e) => {
                    if (e.operation !== "upload") {
                        return;
                    }

                    if (e.response.uploaded) {
                        this.importImagesFilename = e.response.filename;
                        this.importImagesFilePath = e.response.filePath;
                    }
                }
            });

            //COMBOBOX
            $(context).find(".combo-select").kendoComboBox();
            $(context).find(".kendo-dropdown-list").kendoDropDownList();

            const gridDataBound = (e) => {
                const selectedIndexes = e.sender.element.data("selectedIndexes");
                if (!Wiser.validateArray(selectedIndexes)) {
                    return;
                }

                const tbody = e.sender.tbody;
                selectedIndexes.forEach((index) => {
                    const row = tbody.find(`tr`).eq(index);
                    if (row.length === 0) {
                        return;
                    }

                    e.sender.select(row);
                });
            };

            const gridChange = (e) => {
                const selectedRows = e.sender.select();
                const selectedIndexes = [];
                selectedRows.toArray().forEach((row) => {
                    selectedIndexes.push($(row).index());
                });

                e.sender.element.data("selectedIndexes", selectedIndexes);
                
                if (e.sender.element.attr("id") === "importLinksGrid") {
                    $("#importLinkDetailsContainer").toggleClass("hidden", selectedIndexes.length === 0);

                    if (selectedIndexes.length > 0) {
                        // Refresh the grid, otherwise it won't get drawn on the page properly, because it was initially invisible.
                        this.importLinkDetailsGrid.refresh();
                    }
                }
            };

            //BUTTONS
            $(context).find(".saveButton").kendoButton({
                icon: "save"
            });

            $(context).find(".button").kendoButton();

            //DATE PICKER
            $(context).find(".datepicker").kendoDatePicker({
                format: "dd MMMM yyyy",
                culture: "nl-NL"
            }).data("kendoDatePicker");

            //DATE & TIME PICKER
            $(context).find(".datetimepicker").kendoDateTimePicker({
                format: "dd MMMM yyyy HH:mm",
                culture: "nl-NL"
            }).data("kendoDateTimePicker");

            //GRID - IMPORT
            if (this.importGrid) {
                return;
            }
            
            this.importGrid = $("#importGrid").kendoGrid({
                height: 350,
                resizable: true,
                scrollable: true,
                sortable: true,
                editable: true,
                columns: [
                    {
                        field: "column",
                        title: "Kolom uit import bestand"
                    },
                    {
                        field: "moduleId",
                        hidden: true
                    },
                    {
                        field: "importTo",
                        title: "Importeren naar",
                        editor: (container, options) => {
                            $(`<input name="${options.field}" />`).appendTo(container).kendoDropDownList({
                                autoBind: false,
                                dataTextField: "name",
                                dataValueField: "name",
                                filter: "contains",
                                optionLabel: "Kies een entiteit",
                                dataSource: this.entityNames,
                                change: async (e) => {
                                    const widget = e.sender;
                                    const thisDataItem = this.importGrid.dataSource.getByUid(widget.element.closest("tr").data("uid"));

                                    //Get properties of selected entity.
                                    const promiseResults = await Promise.all([
                                        Wiser.api({ url: `${this.settings.wiserApiRoot}imports/entity-properties?entityName=${encodeURIComponent(options.model.importTo)}` })
                                    ]);

                                    const propertiesOfEntity = [];
                                    promiseResults[0].forEach(prop => {
                                        const options = prop.options !== "" ? JSON.parse(prop.options) : {};

                                        // Convert the properties to objects used to determine which field should be auto-selected.
                                        propertiesOfEntity.push({
                                            name: prop.displayName,
                                            value: prop.propertyName,
                                            languageCode: prop.languageCode,
                                            isImageField: prop.inputType === "ImageUpload",
                                            allowMultipleImages: options.hasOwnProperty("multiple") && options.multiple,
                                            propertyOrder: `${prop.ordering}_${prop.id}`
                                        });
                                    });

                                    this.importGrid.dataSource.data().forEach((dataItem) => {
                                        dataItem.set("moduleId", widget.dataItem().moduleId);
                                        if (dataItem.uid !== thisDataItem.uid) {
                                            dataItem.set("importTo", thisDataItem.importTo);
                                        }

                                        let columnName = dataItem.column.toLowerCase();
                                        let propertyMatchMade = false;

                                        // Find matches for the column name on a property.
                                        const matchingProperties = propertiesOfEntity.filter(prop => {
                                            return prop.name.toLowerCase() === columnName || prop.value.toLowerCase() === columnName;
                                        });
                                        if (matchingProperties.length > 0) {
                                            const sorted = matchingProperties.sort((a, b) => {
                                                if (a.propertyOrder < b.propertyOrder) return -1;
                                                if (a.propertyOrder > b.propertyOrder) return 1;
                                                return 0;
                                            });

                                            dataItem.set("specName", sorted[0].name);
                                            this.selectProperty(dataItem, sorted[0]);
                                            propertyMatchMade = true;
                                        }

                                        //If no match has been made reset all values (in case entity changed).
                                        if (!propertyMatchMade) {
                                            dataItem.set("specName", "");
                                            dataItem.set("propertyName", undefined);
                                            dataItem.set("languageCode", undefined);
                                            dataItem.set("isImageField", undefined);
                                            dataItem.set("allowMultipleImages", undefined);
                                        }
                                    });
                                }
                            });
                        }
                    },
                    {
                        field: "specName",
                        title: "Specificatienaam",
                        editor: (container, options) => {
                            if (typeof options.model.importTo !== "string" || options.model.importTo.trim() === "") {
                                $("<em>Kies eerst een entiteit om naar te importeren.</em>").appendTo(container);
                                return;
                            }

                            $(`<input name="${options.field}" />`).appendTo(container).kendoDropDownList({
                                autoBind: false,
                                dataTextField: "name",
                                dataValueField: "name",
                                filter: "contains",
                                optionLabel: "Kies een eigenschap",
                                dataSource: {
                                    transport: {
                                        read: (kendoReadOptions) => {
                                            Wiser.api({
                                                url: `${this.settings.wiserApiRoot}imports/entity-properties?entityName=${encodeURIComponent(options.model.importTo)}`,
                                                dataType: "json",
                                                method: "GET",
                                                data: kendoReadOptions.data
                                            }).then((result) => {
                                                const properties = [];
                                                result.forEach(prop => {
                                                    const options = prop.options !== "" ? JSON.parse(prop.options) : {};

                                                    // Create data items out of the retrieved properties.
                                                    properties.push({
                                                        name: prop.displayName,
                                                        value: prop.propertyName,
                                                        languageCode: prop.languageCode,
                                                        isImageField: prop.inputType === "ImageUpload",
                                                        allowMultipleImages: options.hasOwnProperty("multiple") && options.multiple,
                                                        propertyOrder: `${prop.ordering}_${prop.id}`
                                                    });
                                                });

                                                kendoReadOptions.success(properties);
                                            }).catch((result) => {
                                                kendoReadOptions.error(result);
                                            });
                                        }
                                    }
                                },
                                change: (e) => {
                                    const widget = e.sender;
                                    const thisDataItem = this.importGrid.dataSource.getByUid(widget.element.closest("tr").data("uid"));
                                    this.selectProperty(thisDataItem, widget.dataItem());
                                }
                            });
                        }
                    },
                    {
                        width: "50px",
                        selectable: true
                    }
                ],
                dataBound: gridDataBound,
                change: gridChange
            }).data("kendoGrid");

            //GRID - IMPORT LINKS
            this.importLinksGrid = $("#importLinksGrid").kendoGrid({
                height: 350,
                resizable: true,
                scrollable: true,
                sortable: true,
                editable: true,
                columns: [
                    {
                        field: "column",
                        title: "Kolom uit import bestand"
                    },
                    {
                        field: "linkType",
                        hidden: true
                    },
                    {
                        field: "linkName",
                        title: "Koppeltype",
                        editor: (container, options) => {
                            $(`<input name="${options.field}" />`).appendTo(container).kendoDropDownList({
                                autoBind: false,
                                dataTextField: "name",
                                dataValueField: "name",
                                filter: "contains",
                                optionLabel: "Kies een koppeltype",
                                dataSource: this.linkTypes,
                                change: (e) => {
                                    const widget = e.sender;
                                    const thisDataItem = this.importLinksGrid.dataSource.getByUid(widget.element.closest("tr").data("uid"));
                                    thisDataItem.set("linkType", widget.dataItem().id);
                                }
                            });
                        }
                    },
                    {
                        field: "linkIsDestination",
                        title: "Waarde is het doelitem",
                        template: "#: linkIsDestination === true ? 'Ja' : 'Nee' #"
                    },
                    {
                        field: "deleteExistingLinks",
                        title: "Huidige koppelingen verwijderen",
                        template: "#: deleteExistingLinks === true ? 'Ja' : 'Nee' #"
                    },
                    {
                        width: "50px",
                        selectable: true
                    }
                ],
                dataBound: gridDataBound,
                change: gridChange
            }).data("kendoGrid");

            //GRID - IMPORT LINKS
            this.importLinkDetailsGrid = $("#importLinkDetailsGrid").kendoGrid({
                height: 350,
                resizable: true,
                scrollable: true,
                sortable: true,
                editable: true,
                columns: [
                    {
                        field: "column",
                        title: "Kolom uit import bestand"
                    },
                    {
                        field: "linkType",
                        hidden: true
                    },
                    {
                        field: "linkName",
                        title: "Koppeltype",
                        editor: (container, options) => {
                            $(`<input name="${options.field}" />`).appendTo(container).kendoDropDownList({
                                autoBind: false,
                                dataTextField: "name",
                                dataValueField: "name",
                                filter: "contains",
                                optionLabel: "Kies een koppeltype",
                                dataSource: this.linkTypes,
                                change: (e) => {
                                    const widget = e.sender;
                                    const thisDataItem = this.importLinkDetailsGrid.dataSource.getByUid(widget.element.closest("tr").data("uid"));
                                    thisDataItem.set("linkType", widget.dataItem().id);
                                }
                            });
                        }
                    },
                    {
                        field: "specName",
                        title: "Specificatienaam",
                        editor: (container, options) => {
                            if (typeof options.model.linkType !== "number" || options.model.linkType === 0) {
                                $("<em>Kies eerst een koppeltype om naar te importeren.</em>").appendTo(container);
                                return;
                            }

                            $(`<input name="${options.field}" />`).appendTo(container).kendoDropDownList({
                                autoBind: false,
                                dataTextField: "name",
                                dataValueField: "name",
                                filter: "contains",
                                optionLabel: "Kies een eigenschap",
                                dataSource: {
                                    transport: {
                                        read: (kendoReadOptions) => {
                                            Wiser.api({
                                                url: `${this.settings.wiserApiRoot}imports/entity-properties?linkType=${options.model.linkType}`,
                                                dataType: "json",
                                                method: "GET",
                                                data: kendoReadOptions.data
                                            }).then((result) => {
                                                const properties = [];
                                                result.forEach(prop => {
                                                    const options = prop.options !== "" ? JSON.parse(prop.options) : {};

                                                    // Create data items out of the retrieved properties.
                                                    properties.push({
                                                        name: prop.displayName,
                                                        value: prop.propertyName,
                                                        languageCode: prop.languageCode,
                                                        isImageField: prop.inputType === "ImageUpload",
                                                        allowMultipleImages: options.hasOwnProperty("multiple") && options.multiple,
                                                        propertyOrder: `${prop.ordering}_${prop.id}`
                                                    });
                                                });

                                                kendoReadOptions.success(properties);
                                            }).catch((result) => {
                                                kendoReadOptions.error(result);
                                            });
                                        }
                                    }
                                },
                                change: (e) => {
                                    const widget = e.sender;
                                    const thisDataItem = this.importLinkDetailsGrid.dataSource.getByUid(widget.element.closest("tr").data("uid"));
                                    thisDataItem.set("propertyName", widget.dataItem().value);
                                    thisDataItem.set("languageCode", widget.dataItem().languageCode);
                                    thisDataItem.set("isImageField", widget.dataItem().isImageField === 1);
                                    thisDataItem.set("allowMultipleImages", widget.dataItem().allowMultipleImages === 1);
                                }
                            });
                        }
                    },
                    {
                        width: "50px",
                        selectable: true
                    }
                ],
                dataBound: gridDataBound,
                change: gridChange
            }).data("kendoGrid");
        }

        // Set the information for a data item for the selected property.
        selectProperty(dataItem, property) {
            dataItem.set("propertyName", property.value);
            dataItem.set("languageCode", property.languageCode);
            dataItem.set("isImageField", property.isImageField);
            dataItem.set("allowMultipleImages", property.allowMultipleImages);
        }
    }

    // Initialize the DynamicItems class and make one instance of it globally available.
    window.import = new Import(settings);
})(importModuleSettings);
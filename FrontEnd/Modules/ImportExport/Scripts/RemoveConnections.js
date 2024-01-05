import { Wiser } from "../../Base/Scripts/Utils.js";
import "../../Base/Scripts/Processing.js";

require("@progress/kendo-ui/js/kendo.all.js");
require("@progress/kendo-ui/js/cultures/kendo.culture.nl-NL.js");
require("@progress/kendo-ui/js/messages/kendo.messages.nl-NL.js");

export class RemoveConnections {
    constructor(settings) {
        this.mainLoader = null;
        this.connectionsGrid = null;
        this.numberOfColumns = 0;

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

        $(context).find(".singleColumnItem").hide();
        $(context).find(".multipleColumnsItem").hide();

        let me = this;

        //File upload
        $(".removeConnectionsFileUpload").kendoUpload({
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

                this.setupCorrectInputBasedOnUploadedFile(context, e.response.columns);
                this.importFilename = e.response.filename;
                if (e.response.rowCount > e.response.importLimit) {
                    Wiser.alert({
                        title: "Import limiet overschreden",
                        content: `De import bevat meer dan ${e.response.importLimit} rijen. Alleen de eerste ${e.response.importLimit} van de ${e.response.rowCount} rijen zullen worden geïmporteerd.`
                    });
                }
            }
        });

        //Dropdown
        const process = `loadDropdowns_${Date.now()}`;
        window.processing.addProcess(process);

        try {
            const promiseResults = await Promise.all([
                Wiser.api({ url: `${this.settings.wiserApiRoot}link-settings` })
            ]);
            const linkTypes = promiseResults[0];

            if (!linkTypes || !linkTypes.length) {
                $(context).find("#deleteConnectionsLinkCombobox").hide();
            } else {
                $(context).find("#deleteConnectionsLinkCombobox").kendoDropDownList({
                    dataTextField: "name",
                    dataValueField: "id",
                    dataSource: linkTypes,
                    optionLabel: "Maak uw keuze..."
                });
            }
        } catch (exception) {
            console.error(exception);
            kendo.alert("Er is iets fout gegaan. Probeer het a.u.b. opnieuw of neem contact op met ons.");
        }

        window.processing.removeProcess(process);

        const promiseResults = await Promise.all([
            Wiser.api({ url: `${this.settings.wiserApiRoot}entity-types?onlyEntityTypesWithDisplayName=false` })
        ]);
        const entityNames = promiseResults[0];

        // Grid
        this.connectionsGrid = $("#DeleteConnectionsGrid").kendoGrid({
            height: 250,
            resizable: true,
            scrollable: true,
            sortable: true,
            editable: true,
            pageable: {
                pageSize: 20
            },
            columns: [{
                field: "column",
                title: "Kolom uit bestand"
            },
            {
                field: "entity",
                title: "Entiteit",
                editor: (container, options) => {
                    $(`<input name="${options.field}" />`).appendTo(container).kendoDropDownList({
                        autoBind: false,
                        dataTextField: "displayName",
                        dataValueField: "id",
                        filter: "contains",
                        optionLabel: "Kies een entiteit",
                        dataSource: entityNames,
                        template: "#: displayName # # if(moduleName) { `(moduleName)` } #"
                    });
                }
            },
            {
                field: "matchTo",
                title: "Matchen tegen",
                editor: (container, options) => {
                    if (typeof options.model.entity !== "string" || options.model.entity.trim() === "") {
                        $("<em>Kies eerst een entiteit om binnen te verwijderen.</em>").appendTo(container);
                        return;
                    }

                    $(`<input name="${options.field}" />`).appendTo(container).kendoDropDownList({
                        autoBind: false,
                        dataTextField: "displayName",
                        dataValueField: "propertyName",
                        filter: "contains",
                        optionLabel: "Kies een eigenschap",
                        dataSource: {
                            transport: {
                                read: `${this.settings.wiserApiRoot}entity-properties/${options.model.entity}?onlyEntityTypesWithDisplayName=false&onlyEntityTypesWithPropertyName=true&addIdProperty=true`,
                            }
                        },
                        template: "#: displayName # # if (tabName != null && tabName !== '') { # - Tab: #: tabName # # } #"
                    });
                }
            },
            {
                selectable: true,
                width: "50px"
            }
            ]
        }).data("kendoGrid");

        //Button
        $(context).find("#deleteConnectionsButton").kendoButton({
            click: async function (e) {
                await me.prepareDeleteConnections();
            }
        });
    }

    //Handle if the dropdown or the grid needs to be shown based on the number of columns in the uploaded file.
    setupCorrectInputBasedOnUploadedFile(context, columns) {
        this.numberOfColumns = columns.length;

        if (columns.length === 1) {
            $(context).find(".singleColumnItem").show();
            $(context).find(".multipleColumnsItem").hide();
        }
        else if (columns.length >= 1) {
            $(context).find(".singleColumnItem").hide();
            $(context).find(".multipleColumnsItem").show();

            const dataSource = {
                data: [],
                autoSync: true,
                schema: {
                    model: {
                        fields: {
                            column: { editable: false },
                            entity: { editable: true, type: "string" },
                            matchTo: { editable: true, type: "string" }
                        }
                    }
                }
            };

            columns.forEach((column) => {
                dataSource.data.push({
                    column: column,
                    entity: "",
                    matchTo: ""
                });
            });

            this.connectionsGrid.setDataSource(dataSource);
        }
    }

    //Let the API prepare the delete to retrieve all information for delete.
    async prepareDeleteConnections() {
        if (!this.importFilename || this.importFilename === "") {
            Wiser.showMessage({
                title: "Ongeldig bestand",
                content: "Er is geen bestand geüpload om te gebruiken voor het verwijderen van koppelingen."
            });
            return;
        }

        let context = document.body;
        let request = {
            filePath: this.importFilename
        }

        if (this.numberOfColumns === 1) {
            const linkId = $(context).find("#deleteConnectionsLinkCombobox").data("kendoDropDownList").value();

            if (linkId === "") {
                Wiser.showMessage({
                    title: "Link type mist",
                    content: "Er is geen koppeltype gekozen om de koppelingen binnen te verwijderen."
                });
                return;
            }

            request.deleteLinksType = 0;
            request.linkId = linkId;
        }
        else if (this.numberOfColumns >= 1) {
            request.deleteLinksType = 1;
            request.deleteSettings = [];

            this.connectionsGrid.dataItems().forEach(item => {
                if (item.entity === "" || item.matchTo === "") {
                    Wiser.showMessage({
                        title: "Lege kolommen",
                        content: "Niet alle kolommen zijn ingevuld."
                    });
                    return;
                }

                request.deleteSettings.push({
                    column: item.column,
                    entity: item.entity,
                    matchTo: item.matchTo
                });
            });
        }

        const result = await Wiser.api({
            url: `${this.settings.wiserApiRoot}imports/delete-links/prepare`,
            method: "POST",
            contentType: "application/json",
            data: JSON.stringify(request)
        });

        this.prepareDeleteFinished(result);
    }

    //Handle preparation response from the API.
    prepareDeleteFinished(results) {
        let totalLinksToDelete = 0;
        results.forEach(result => totalLinksToDelete += result.ids.length);

        Wiser.confirm({
            title: "Bevestig links verwijderen",
            content: `U staat op het punt om ${totalLinksToDelete} link(s) te verwijderen. Wilt u doorgaan?`,
            actions: [{
                text: "Ok",
                action: async function (e) {
                    const result = await Wiser.api({
                        url: `${window.removeConnections.settings.wiserApiRoot}imports/delete-links/confirm`,
                        method: "POST",
                        contentType: "application/json",
                        data: JSON.stringify(results)
                    });
                    
                    if (result === true) {
                        Wiser.showMessage({
                            title: "Koppelingen verwijderd",
                            content: "De koppelingen zijn verwijderd."
                        });
                    } else {
                        Wiser.showMessage({
                            title: "Koppelingen verwijderen mislukt.",
                            content: "Er is iets mis gegaan tijdens het verwijderen van de koppelingen, de actie is teruggedraaid."
                        });
                    }
                }
            }]
        });
    }
}
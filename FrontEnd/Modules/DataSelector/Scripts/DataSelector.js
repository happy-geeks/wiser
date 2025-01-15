import {TrackJS} from "trackjs";
import {Wiser} from "../../Base/Scripts/Utils.js";
import "../../Base/Scripts/Processing.js";
import "../Css/DataSelector.css";

require("@progress/kendo-ui/js/kendo.all.js");
require("@progress/kendo-ui/js/cultures/kendo.culture.nl-NL.js");
require("@progress/kendo-ui/js/messages/kendo.messages.nl-NL.js");

// Any custom settings can be added here. They will overwrite most default settings inside the module.
const moduleSettings = {
};

((moduleSettings) => {
    class DataSelector {
        constructor(settings) {
            kendo.culture("nl-NL");

            // Base settings.
            this.settings = {};
            Object.assign(this.settings, settings);

            // Variables for easy access to certain elements.
            this.container = null;
            this.connectionBlocksContainer = null;
            this.jsonCodeMirrorEditor = null;
            this.queryCodeMirrorEditor = null;
            this.havingContainer = null;
            this.mainConnection = null;

            // Data selector settings.
            this.useExportMode = false;
            this.allEntityTypes = [];
            this.availableEntityTypes = [];
            this.availableLinkTypes = [];
            this.selectedFields = [];

            // Selections.
            this.selectedModules = [];
            this.selectedEntityType = "";

            // Saving and loading.
            this.currentId = 0;
            this.currentName = "";
            this.dataLoad = new DataLoad(this);

            // Other.
            this.mainLoader = null;
            this.dialogZindex = 20000;

            // Fire event on page ready for direct actions
            document.addEventListener("DOMContentLoaded", () => {
                this.onPageReady();
            });
        }

        async onPageReady() {
            this.mainLoader = $("#mainLoader");

            // Setup processing.
            document.addEventListener("processing.Busy", this.toggleMainLoader.bind(this, true));
            document.addEventListener("processing.Idle", this.toggleMainLoader.bind(this, false));

            // Setup any settings from the body element data. These settings are added via the Wiser backend and they take preference.
            Object.assign(this.settings, $(document.body).data());

            this.useExportMode = document.getElementById("useExportMode").checked;

            if (this.settings.trackJsToken) {
                TrackJS.install({
                    token: this.settings.trackJsToken
                });
            }

            // Add logged in user access token to default authorization headers for all jQuery ajax requests.
            $.ajaxSetup({
                headers: { "Authorization": `Bearer ${localStorage.getItem("accessToken")}` }
            });

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
            this.settings.adminAccountLoggedIn = !!user.adminlogin;

            const userData = await Wiser.getLoggedInUserData(this.settings.wiserApiRoot);
            this.settings.userId = userData.encryptedId;
            this.settings.tenantId = userData.encryptedTenantId;
            this.settings.zeroEncrypted = userData.zeroEncrypted;
            this.settings.wiserUserId = userData.id;

            this.settings.serviceRoot = `${this.settings.wiserApiRoot}templates/get-and-execute-query`;
            this.settings.getItemsUrl = `${this.settings.wiserApiRoot}data-selectors`;

            this.container = document.getElementById("dataBuilder");
            this.connectionBlocksContainer = document.getElementById("connectionBlocks");
            this.havingContainer = $(document.getElementById("havingContainer"));

            // Initialize the rest.
            this.initializeWindow();
            this.initializeKendoElements();
            this.initializeCodeMirrorElements();

            this.dataLoad.initialize();

            // Create a single connection to start with.
            this.mainConnection = this.addConnection(this.connectionBlocksContainer, true);

            this.setBindings();

            // Load all entity types once.
            await this.getAllEntityTypes();

            // Check if a data selector should be loaded.
            const urlParams = new URLSearchParams(window.location.search);
            const loadId = urlParams.get("load");

            if (loadId !== null) {
                try {
                    await this.dataLoad.loadById(Number.parseInt(loadId, 10));
                } catch (e) {
                    window.processing.removeProcess("dataSelectorLoad");
                    Wiser.alert({
                        title: "Laden mislukt",
                        content: "Er is een fout opgetreden tijdens het laden van de data selector. Probeer het a.u.b. nogmaals."
                    });
                    console.error(e);
                }
            }

            // Hide loader at the end.
            this.toggleMainLoader(false);
        }

        /**
         * Shows or hides the main (full screen) loader.
         * @param {boolean} show True to show the loader, false to hide it.
         */
        toggleMainLoader(show) {
            this.mainLoader.toggleClass("loading", show);
        }

        setBindings() {
            const exportModeCheckbox = document.getElementById("useExportMode");
            const showInDashboardCheckbox = document.getElementById("showInDashboard");
            const newButton = document.getElementById("newButton");
            const saveButton = document.getElementById("saveButton");
            const loadButton = document.getElementById("loadButton");
            const viewJsonRequestButton = document.getElementById("viewJsonRequestButton");
            const viewJsonResultButton = document.getElementById("viewJsonResultButton");
            const viewResult = document.getElementById("viewResult");
            const resultClose = document.getElementById("resultClose");
            const viewQueryButton = document.getElementById("viewQueryButton");

            if (exportModeCheckbox) {
                exportModeCheckbox.addEventListener("change", () => {
                    Wiser.confirm({
                        title: "Data Selector",
                        content: "Let op! Dit zorgt ervoor dat een nieuwe data selector geopend wordt. Gegevens die niet zijn opgeslagen zullen verloren gaan. Wilt u doorgaan?",
                        messages: {
                            okText: "Ja",
                            cancel: "Nee"
                        }
                    }).result.done(() => {
                        window.processing.addProcess("newDataSelector");
                        location.assign(`${location.pathname}?exportMode=${exportModeCheckbox.checked ? "true" : "false"}`);
                    }).fail(() => {
                        // Revert to previous state.
                        exportModeCheckbox.checked = !exportModeCheckbox.checked;
                    });
                });
            }
            if (showInDashboardCheckbox) {
                showInDashboardCheckbox.addEventListener("change", this.checkForDashboardConflict.bind(this));
            }

            if (newButton) {
                $(newButton).getKendoButton().bind("click", () => {
                    Wiser.confirm({
                        title: "Data Selector",
                        content: "Weet u zeker dat u een nieuwe data selector wilt beginnen? Gegevens die niet zijn opgeslagen zullen verloren gaan.",
                        messages: {
                            okText: "Ja",
                            cancel: "Nee"
                        }
                    }).result.done(() => {
                        window.processing.addProcess("newDataSelector");
                        location.assign(`${location.pathname}?exportMode=${exportModeCheckbox.checked ? "true" : "false"}`);
                    });
                });
            }
            if (saveButton) {
                $(saveButton).getKendoButton().bind("click", this.saveWithPrompt.bind(this));
            }
            if (loadButton) {
                $(loadButton).getKendoButton().bind("click", () => {
                    this.dataLoad.showLoadDialog();
                });
            }

            if (viewJsonRequestButton) {
                $(viewJsonRequestButton).getKendoButton().bind("click", () => {
                    viewResult.classList.add("active");
                    viewResult.dataset.headerText = "JSON request";

                    this.jsonCodeMirrorEditor.getWrapperElement().style.display = "";
                    this.queryCodeMirrorEditor.getWrapperElement().style.display = "none";

                    this.jsonCodeMirrorEditor.getDoc().setValue(JSON.stringify(this.createJsonRequest(), null, 2));
                });
            }
            if (viewJsonResultButton) {
                $(viewJsonResultButton).getKendoButton().bind("click", () => {
                    viewResult.classList.add("active");
                    viewResult.dataset.headerText = "JSON result";

                    this.jsonCodeMirrorEditor.getWrapperElement().style.display = "";
                    this.queryCodeMirrorEditor.getWrapperElement().style.display = "none";

                    // Empty the editor first.
                    this.jsonCodeMirrorEditor.getDoc().setValue("");

                    // Wrapping it into an anonymous function will allow for an async function to be called without needing to declare the outer function as async.
                    (async () => {
                        window.processing.addProcess("getJsonResult");
                        try {
                            const resultJson = await this.getJsonResult();
                            this.jsonCodeMirrorEditor.getDoc().setValue(JSON.stringify(resultJson, null, 2));
                        } catch (e) {
                            Wiser.alert({
                                title: "Ophalen resultaat mislukt",
                                content: "Er is iets fout gegaan bij het ophalen van het JSON resultaat. Probeer het a.u.b. nogmaals."
                            });
                            console.error(e);
                        }
                        window.processing.removeProcess("getJsonResult");
                    })();
                });
            }
            if (viewQueryButton) {
                $(viewQueryButton).getKendoButton().bind("click", () => {
                    viewResult.classList.add("active");
                    viewResult.dataset.headerText = "Query";

                    this.jsonCodeMirrorEditor.getWrapperElement().style.display = "none";
                    this.queryCodeMirrorEditor.getWrapperElement().style.display = "";

                    setTimeout(() => {
                        this.queryCodeMirrorEditor.refresh();
                    }, 300);

                    // Wrapping it into an anonymous function will allow for an async function to be called without needing to declare the outer function as async.
                    (async () => {
                        window.processing.addProcess("getQuery");
                        try {
                            const resultQuery = await this.getQuery();
                            this.queryCodeMirrorEditor.getDoc().setValue(resultQuery);
                        } catch (e) {
                            Wiser.alert({
                                title: "Ophalen query mislukt",
                                content: "Er is iets fout gegaan bij het ophalen van de query. Probeer het a.u.b. nogmaals."
                            });
                            console.error(e);
                        }
                        window.processing.removeProcess("getQuery");
                    })();
                });
            }
            if (resultClose) {
                resultClose.addEventListener("click", () => {
                    viewResult.classList.remove("active");
                });
            }

            $(this.container).on("change", "input[name='module-picker']", (e) => {
                const checkbox = e.currentTarget;
                const value = Number.parseInt(checkbox.value, 10);
                const index = this.selectedModules.indexOf(value);
                let hasChange = false;

                if (checkbox.checked) {
                    // Index must be -1 to make sure the module ID is not already in the array.
                    if (index === -1) {
                        this.selectedModules.push(value);
                        hasChange = true;
                    }
                } else {
                    // Need an index to know which index needs deleting.
                    if (index !== -1) {
                        this.selectedModules.splice(index, 1);
                        hasChange = true;
                    }
                }

                if (!hasChange) {
                    return;
                }

                // This isn't really necessary, but it's nice to have the modules in a set order.
                this.selectedModules.sort();
            });

            const selectEntity = $("#selectEntity").getKendoDropDownList();
            selectEntity.bind("change", () => {
                this.selectedEntityType = selectEntity.value();

                const connection = $(this.mainConnection.container).data("connection");
                connection.updateAvailableProperties().then(() => {
                    // Update the data sources of the main scopes and field selection.
                    const connectionBlock = this.container.querySelector(".connectionBlock");
                    const elements = Array.from(connectionBlock.querySelector(".scopesContainer").querySelectorAll("select.scope-property-select"));
                    elements.push(connectionBlock.querySelector("select.select-details"));
                    connection.setAvailablePropertiesDataSources(elements);
                });

                this.updateAvailableLinkTypes();
            });

            $(this.container.querySelector("div.havingList button.add-scope-button")).getKendoButton().bind("click", () => {
                this.addHaving();
            });

            $(this.container).find(".item.havingList").on("click", "button.edit-field-button", (e) => {
                const button = $(e.currentTarget);
                const propertyDropdown = button.closest(".inputRow").find("select.scope-property-select").getKendoDropDownList();
                this.openFieldEditor(propertyDropdown.dataItem(), {
                    includeDataTypeField: true,
                    includeLanguageCodeField: false,
                    includeFieldAliasField: false,
                    includeIsItemIdField: false
                }, true);
            });

            const orderBy = $("#sorting");
            const orderByWidget = orderBy.getKendoMultiSelect();
            orderByWidget.wrapper.on("click", "li.k-button", (e) => {
                const clickedElement = $(e.target);
                if (clickedElement.has(".k-i-close").length > 0 || clickedElement.closest(".k-i-close").length > 0) {
                    return;
                }

                // This will select the tag itself (a li element).
                const tagElement = $(e.currentTarget);

                const index = tagElement.index();
                const selectedDataItem = orderByWidget.dataItems()[index];
                const dataItem = orderByWidget.dataSource.view().find((di) => di.value === selectedDataItem.value);

                if (typeof dataItem.direction !== "string" || dataItem.direction === "ASC") {
                    dataItem.set("direction", "DESC");
                } else {
                    dataItem.set("direction", "ASC");
                }
            });

            this.giveCustomClickLogic(orderByWidget);

            document.addEventListener("entitySelectionUpdate", () => {
                this.updateSelectedFields();
                this.setSelectedFieldsDataSources();
            });

            // Other events.
            window.addEventListener("message", (e) => {
                this.handleWindowMessage(e.data);
            });

            document.addEventListener("moduleClosing", (event) => {
                // You can do anything here that needs to happen before closing the module.
                event.detail();
            });
        }

        async getAllEntityTypes() {
            this.allEntityTypes = await Wiser.api({ url: `${this.settings.wiserApiRoot}entity-types` });
            $("#selectEntity").getKendoDropDownList().setDataSource(this.allEntityTypes);
        }

        updateWidgetDataSource(widget, baseDataSource, includeAliasCheck = false) {
            const dataItems = widget.dataSource.view();
            const valueProperty = widget.options.dataValueField;

            const itemsToAdd = [];

            // For the "order by" widget a special item called "Treeview" should always be present. Add it if the widget doesn't have it yet.
            if (widget.element.prop("id") === "sorting") {
                const item = dataItems.find((dataItem) => {
                    return dataItem.entityName === "" && dataItem[valueProperty] === "treeview";
                });
                if (!item) {
                    itemsToAdd.push({
                        value: "treeview",
                        entityName: "",
                        displayName: "Treeview",
                        languageCode: [],
                        aggregation: "",
                        formatting: "",
                        fieldAlias: "",
                        direction: "ASC",
                        text: "Treeview",
                        originalText: "Treeview",
                        aliasOrValue: "treeview"
                    });
                }
            }

            // Determine items that need to be added. These are items that are in the new data source, but are not present in the widget.
            baseDataSource.filter((property) => {
                const item = dataItems.filter((dataItem) => {
                    return dataItem.entityName === property.entityName && dataItem[valueProperty] === property[valueProperty] && (!includeAliasCheck || property.fieldAlias === dataItem.fieldAlias);
                });
                return item.length === 0;
            }).forEach((item) => {
                itemsToAdd.push(item);
            });
            // Determine items that need to be removed. These are items that are in currently in the widget, but are not present in the new data source.
            const itemsToRemove = dataItems.filter((dataItem) => {
                const item = baseDataSource.filter((property) => {
                    return property.entityName === dataItem.entityName && property[valueProperty] === dataItem[valueProperty] && (!includeAliasCheck || property.fieldAlias === dataItem.fieldAlias);
                });
                return item.length === 0;
            });

            itemsToAdd.forEach((item) => {
                widget.dataSource.add(item);
            });
            itemsToRemove.forEach((item) => {
                // The "treeview" item should never be removed, as it's a default option.
                if (item.entityName === "" && item[valueProperty] === "treeview") {
                    return;
                }
                widget.dataSource.remove(item);
            });

            if (widget instanceof window.kendo.ui.MultiSelect) {
                const itemsToKeep = widget.dataItems().filter((dataItem) => {
                    const item = widget.dataSource.view().filter((sourceItem) => {
                        return sourceItem.entityName === dataItem.entityName && sourceItem[valueProperty] === dataItem[valueProperty] && (!includeAliasCheck || sourceItem.fieldAlias === dataItem.fieldAlias);
                    });
                    return item.length > 0;
                });

                const newValue = itemsToKeep.map(item => item[valueProperty]);
                widget.value(newValue);
            }

            widget.refresh();

            widget.trigger("dataSourceUpdated");
        }

        updateSelectedFields() {
            // Retrieve all "select-details" dropdowns.
            const selects = this.container.querySelectorAll("select.select-details");

            let dataItems = [];

            selects.forEach((select) => {
                dataItems = dataItems.concat(Array.from($(select).getKendoMultiSelect().dataItems()));
            });

            const itemsToAdd = dataItems.filter((dataItem) => {
                const item = this.selectedFields.filter((property) => {
                    return property.entityName === dataItem.entityName && property.value === dataItem.value && property.fieldAlias === dataItem.fieldAlias;
                });
                return item.length === 0;
            });

            const itemsToRemove = this.selectedFields.filter((property) => {
                const item = dataItems.filter((dataItem) => {
                    return dataItem.entityName === property.entityName && dataItem.value === property.value && property.fieldAlias === dataItem.fieldAlias;
                });
                return item.length === 0;
            });

            itemsToAdd.forEach((itemToAdd) => {
                this.selectedFields.push(itemToAdd);
            });
            itemsToRemove.forEach((itemToRemove) => {
                const index = this.selectedFields.findIndex((field) => {
                    return field.entityName === itemToRemove.entityName && field.value === itemToRemove.value && field.fieldAlias === itemToRemove.fieldAlias;
                });
                this.selectedFields.splice(index, 1);
            });
        }

        setSelectedFieldsDataSources(havingScopes = null, alsoUpdateSortAndGroupingOptions = true) {
            if (havingScopes instanceof NodeList === false) {
                havingScopes = this.container.querySelector("div.item.havingList").querySelectorAll("select.scope-property-select");
            }

            havingScopes.forEach((havingScope) => {
                $(havingScope).getKendoDropDownList().setDataSource({
                    data: this.selectedFields
                });
            });

            if (alsoUpdateSortAndGroupingOptions) {
                // Also update sorting options select and group by select, which use the same fields.
                this.updateWidgetDataSource($("#sorting").getKendoMultiSelect(), this.selectedFields, true);
                this.updateWidgetDataSource($("#groupBy").getKendoMultiSelect(), this.selectedFields, true);
            }
        }

        updateAvailableLinkTypes() {
            $(this.container).find("select.linked-to-property-select").each((i, elem) => {
                $(elem).getKendoDropDownList().dataSource.read();
            });
        }

        updateScopePropertyValueDataSource(scopeRow) {
            const propertySelect = scopeRow.find("select.scope-property-select").getKendoDropDownList();
            const dataItem = propertySelect.dataItem(propertySelect.select());

            scopeRow.find("select.scope-value-select").getKendoMultiSelect().setDataSource({
                transport: {
                    read: (options) => {
                        Wiser.api({
                            url: `${this.settings.wiserApiRoot}entity-properties/${encodeURIComponent(dataItem.entityName)}/unique-values/${encodeURIComponent(dataItem.propertyName)}?languageCode=${dataItem.languageCode}`,
                            dataType: "json"
                        }).then((result) => {
                            const items = [
                                {
                                    text: "Actuele datum",
                                    value: "{NowMysqlDate}"
                                },
                                {
                                    text: "Actuele datum en tijd",
                                    value: "{NowMysqlTime}"
                                },
                                ...result.map(item => {
                                    return {
                                        text: item,
                                        value: item
                                    };
                                })
                            ];

                            options.success(items);
                        }).catch((result) => {
                            options.error(result);
                        });
                    }
                }
            });
        }

        /**
         * Adds a connection block.
         * @param {HTMLElement} parentContainer The element this connection will be added to.
         * @param {boolean} isMainConnection Whether the connection will serve as the main block.
         * @returns {HTMLElement} The container DOM element of the connection that was created.
         */
        addConnection(parentContainer, isMainConnection = false) {
            const connection = new Connection(this, parentContainer, isMainConnection);
            connection.initialize();
            return connection;
        }

        initializeWindow() {
            if (parent === self) {
                //WINDOW
                $("#window").kendoWindow({
                    width: "1500",
                    height: "650",
                    title: "Data Selector",
                    visible: true,
                    actions: []
                }).getKendoWindow().center().open().maximize();
            }
        }

        getAliasOrFieldName(dataItem) {
            if (typeof dataItem.fieldAlias === "string" && dataItem.fieldAlias !== "") {
                return dataItem.fieldAlias;
            } else {
                return dataItem.propertyName;
            }
        }

        /**
         * Retrieves all aliases that are set for fields.
         * @returns {Array<string>} An array containing all aliases.
         */
        getAllAliases() {
            const detailSelects = Array.from(this.container.querySelectorAll("select.select-details")).concat(Array.from(this.container.querySelectorAll("select.select-details-links")));
            if (detailSelects.length === 0) {
                return [];
            }

            const result = [];

            detailSelects.forEach((select) => {
                $(select).getKendoMultiSelect().dataSource.view().forEach((dataItem) => {
                    if (typeof dataItem.fieldAlias !== "string" || dataItem.fieldAlias === "") {
                        return;
                    }
                    result.push(dataItem.fieldAlias);
                });
            });

            return result;
        }

        /**
         * Checks if the given field alias is already in use.
         * @param {string} alias The alias.
         * @returns {boolean} True if the alias is in use; false otherwise.
         */
        checkIfAliasInUse(alias) {
            const aliases = this.getAllAliases();
            if (aliases.length === 0) {
                return false;
            }

            return aliases.findIndex((e) => {
                return e.toLowerCase() === alias.toLowerCase();
            }) >= 0;
        }

        /**
         * Generates the JSON request body.
         * @param {boolean} forSaving Whether the created JSON is meant for saving the data selector.
         * @returns {object} A json object that represents the data selector.
         */
        createJsonRequest(forSaving = false) {
            const result = {
                main: {}
            };

            if (forSaving) {
                result.useExportMode = this.useExportMode;
            }

            // First scope array is always the first block directly in "connectionBlocks".
            const entityName = $("#selectEntity").getKendoDropDownList().value();

            if (entityName === "") {
                return result;
            }

            // Retrieve various elements and values to handle.
            const connectionBlocksContainer = document.getElementById("connectionBlocks");
            const firstConnectionBlock = connectionBlocksContainer.querySelector(".connectionBlock");
            const blockItems = Array.from(firstConnectionBlock.querySelector(".blockItems").children);

            result.main.entityName = entityName;

            let connections = [];
            blockItems.forEach((item) => {
                let tempArray;

                if (item.classList.contains("scopesList")) {
                    tempArray = this.createJsonRequestScopesList(item.querySelector(".scopesContainer"), forSaving);
                    if (tempArray.length > 0) {
                        result.main.scope = tempArray;
                    }
                } else if (item.classList.contains("exportFields")) {
                    tempArray = [];
                    const selectDetails = $(item.querySelector("select.select-details")).getKendoMultiSelect();
                    selectDetails.dataItems().forEach((dataItem) => {
                        const languageCodes = [];
                        if (typeof dataItem.languageCode === "string") {
                            languageCodes.push(dataItem.languageCode);
                        } else {
                            languageCodes.push(...Array.from(dataItem.languageCode));
                        }

                        const newItem = {
                            fieldname: dataItem.propertyName,
                            fieldalias: dataItem.fieldAlias,
                            dataType: dataItem.dataType || "string",
                            havingDataType: dataItem.havingDataType || "string",
                            languageCodes: languageCodes,
                            aggregationfunction: dataItem.aggregation,
                            formatting: dataItem.formatting
                        };

                        if (dataItem.subSelection) {
                            const subTempArray = [];

                            dataItem.subSelection.fields.forEach((subDataItem) => {
                                const languageCodes = [];
                                if (typeof subDataItem.languageCode === "string") {
                                    languageCodes.push(subDataItem.languageCode);
                                } else {
                                    languageCodes.push(...Array.from(subDataItem.languageCode));
                                }

                                subTempArray.push({
                                    fieldname: subDataItem.propertyName,
                                    fieldalias: subDataItem.fieldAlias,
                                    dataType: subDataItem.dataType || "string",
                                    havingDataType: subDataItem.havingDataType || "string",
                                    languageCodes: languageCodes,
                                    aggregationfunction: subDataItem.aggregation,
                                    formatting: subDataItem.formatting
                                });
                            });

                            if (!forSaving) {
                                newItem.fields = subTempArray;
                            } else {
                                newItem.subSelection = dataItem.subSelection;
                            }
                        }

                        tempArray.push(newItem);
                    });

                    if (tempArray.length > 0) {
                        result.main.fields = tempArray;
                    }
                } else if (item.classList.contains("exportFieldsLinks")) {
                    tempArray = $(item.querySelector("select.select-details-links")).getKendoMultiSelect().value();
                    if (tempArray.length > 0) {
                        result.main.linkfields = tempArray;
                    }
                } else if (item.classList.contains("linkedToList")) {
                    if (item.classList.contains("direction-down")) {
                        connections = connections.concat(this.createJsonRequestConnectionsList(item.querySelector(".linkedToContainer"), "down", forSaving));
                    } else if (item.classList.contains("direction-up")) {
                        connections = connections.concat(this.createJsonRequestConnectionsList(item.querySelector(".linkedToContainer"), "up", forSaving));
                    }
                }
            });

            if (connections.length > 0) {
                result.connections = connections;
            }

            // Set the group by value, but only if at least one field was selected.
            const groupBy = Array.from($(document.getElementById("groupBy")).getKendoMultiSelect().dataItems());
            if (groupBy.length > 0) {
                const groupByFields = [];
                groupBy.forEach((field) => {
                    if (!forSaving) {
                        groupByFields.push(this.getAliasOrFieldName(field));
                    } else {
                        groupByFields.push({
                            entityName: field.entityName,
                            fieldName: field.propertyName,
                            fieldAlias: field.fieldAlias
                        });
                    }
                });
                result.groupBy = groupByFields;
            }

            const having = this.createJsonRequestScopesList(this.container.querySelector(".havingContainer"), forSaving, true);
            if (having.length > 0) {
                result.having = having;
            }

            // Set the order by value, but only if at least one field was selected.
            const orderBy = Array.from($(document.getElementById("sorting")).getKendoMultiSelect().dataItems());
            if (orderBy.length > 0) {
                const orderByFields = [];
                orderBy.forEach((field) => {
                    if (!forSaving) {
                        orderByFields.push({
                            fieldname: this.getAliasOrFieldName(field),
                            direction: field.direction
                        });
                    } else {
                        orderByFields.push({
                            entityName: field.entityName,
                            fieldName: field.propertyName,
                            fieldAlias: field.fieldAlias,
                            direction: field.direction
                        });
                    }
                });
                result.orderBy = orderByFields;
            }

            // Validate and set the export limit.
            const exportLimitInput = document.getElementById("exportLimit");
            let exportLimit = exportLimitInput.value.trim();
            if (!/^\d+(?:,\d+)?$/.test(exportLimit)) {
                if (limit !== "") {
                    Wiser.alert({
                        title: "Limiet ongeldig",
                        content: "Waarde bij limiet is ongeldig. Dit moet een getal zijn, of twee getallen gescheiden door een komma."
                    });
                }
                exportLimitInput.value = "0";
                exportLimit = "0";
            }
            result.limit = exportLimit;

            // Whether the data is allowed to be insecure. Only place it if checked.
            const insecureData = document.getElementById("insecureData").checked;
            if (insecureData === true) {
                result.insecure = insecureData;
            }

            return result;
        }

        createJsonRequestScopesList(scopesContainer, forSaving = false, forHaving = false) {
            const result = [];

            const rowsArrayName = forHaving ? "havingrows" : "scoperows";

            scopesContainer.querySelectorAll("section").forEach((section) => {
                const scopeSection = {};
                scopeSection[rowsArrayName] = [];
                section.querySelectorAll(".inputRow").forEach((scope) => {
                    const dataItem = $(scope.querySelector("select.scope-property-select")).getKendoDropDownList().dataItem();
                    if (!dataItem || !dataItem.value || dataItem.value.trim() === "") {
                        // Skip if nothing was selected.
                        return;
                    }

                    let value;
                    if (getComputedStyle(scope.querySelector("span.scope-value-select")).display !== "none") {
                        value = $(scope).find("select.scope-value-select").getKendoMultiSelect().value();
                    } else if (getComputedStyle(scope.querySelector("div.free-input")).display !== "none") {
                        value = scope.querySelector("div.free-input > input").value;
                    } else {
                        value = "";
                    }

                    // Determine field name.
                    let fieldName;
                    if (forHaving && dataItem.hasOwnProperty("fieldAlias") && dataItem.fieldAlias !== "") {
                        fieldName = dataItem.fieldAlias;
                    } else {
                        fieldName = dataItem.propertyName;
                    }

                    if (!forSaving) {
                        const languageCodes = [];
                        if (typeof dataItem.languageCode === "string") {
                            languageCodes.push(dataItem.languageCode);
                        } else {
                            languageCodes.push(...Array.from(dataItem.languageCode));
                        }

                        scopeSection[rowsArrayName].push({
                            key: {
                                fieldname: fieldName,
                                languageCodes: languageCodes,
                                dataType: dataItem.dataType || "string",
                                havingDataType: dataItem.havingDataType || "string",
                                aggregationfunction: forHaving ? dataItem.havingAggregation : dataItem.aggregation,
                                formatting: forHaving ? dataItem.havingFormatting : dataItem.formatting
                            },
                            operator: $(scope.querySelector("select.scope-comparison-select")).getKendoDropDownList().value(),
                            value: value
                        });
                    } else {
                        const languageCodes = [];
                        if (typeof dataItem.languageCode === "string") {
                            languageCodes.push(dataItem.languageCode);
                        } else {
                            languageCodes.push(...Array.from(dataItem.languageCode));
                        }

                        scopeSection[rowsArrayName].push({
                            key: {
                                entityName: dataItem.entityName,
                                fieldName: dataItem.propertyName,
                                fieldAlias: dataItem.fieldAlias,
                                dataType: dataItem.dataType || "string",
                                havingDataType: dataItem.havingDataType || "string",
                                languageCodes: languageCodes,
                                aggregation: forHaving ? dataItem.havingAggregation : dataItem.aggregation,
                                formatting: forHaving ? dataItem.havingFormatting : dataItem.formatting
                            },
                            operator: $(scope.querySelector("select.scope-comparison-select")).getKendoDropDownList().value(),
                            value: value
                        });
                    }
                });
                result.push(scopeSection);
            });

            return result;
        }

        createJsonRequestConnectionsList(linkedToContainer, direction, forSaving = false) {
            const result = [];

            const sections = Array.from(linkedToContainer.querySelectorAll("section")).filter((section) => {
                return section.parentNode === linkedToContainer;
            });
            sections.forEach((section) => {
                const linkedToSection = {
                    connectionrows: []
                };

                const rows = Array.from(section.querySelectorAll(".inputRow")).filter((row) => {
                    return row.parentNode === section;
                });

                rows.forEach((linkedTo) => {
                    const dropDown = $(linkedTo.querySelector("select.linked-to-property-select")).getKendoDropDownList();
                    const options = dropDown.dataItem();

                    const linkedToRow = {
                        modes: []
                    };

                    if (options.inputType === "sub-entities-grid") {
                        linkedToRow.entity = options.type;
                        linkedToRow.typenr = options.typeNumber;
                        if (forSaving) {
                            linkedToRow.typeName = options.linkTypeName;
                        }
                    } else if (options.inputType === "item-linker") {
                        linkedToRow.connectionType = options.type;

                        const itemIds = [];
                        const treeView = $(linkedTo.querySelector("div.checkTree")).getKendoTreeView();
                        const checkedItems = treeView.getCheckedItems();
                        for (let item of checkedItems) {
                            itemIds.push(item.id);
                        }

                        if (itemIds.length > 0) {
                            linkedToRow.itemids = itemIds;
                        }
                    }

                    linkedToRow.modes.push(direction);
                    if (linkedTo.querySelector("div.optional-checkbox input[type='checkbox']").checked === true) {
                        linkedToRow.modes.push("optional");
                    }

                    const subConnectionBlock = linkedTo.querySelector(".connectionBlock");
                    if (subConnectionBlock !== undefined && subConnectionBlock !== null) {
                        const blockItems = Array.from(subConnectionBlock.querySelector(".blockItems").children);

                        let connections = [];
                        blockItems.forEach((item) => {
                            let tempArray;

                            if (item.classList.contains("scopesList")) {
                                tempArray = this.createJsonRequestScopesList(item.querySelector(".scopesContainer"), forSaving);
                                if (tempArray.length > 0) {
                                    linkedToRow.scope = tempArray;
                                }
                            } else if (item.classList.contains("exportFields")) {
                                tempArray = [];
                                const selectDetails = $(item.querySelector("select.select-details")).getKendoMultiSelect();
                                Array.from(selectDetails.dataItems()).forEach((dataItem) => {
                                    const languageCodes = [];
                                    if (typeof dataItem.languageCode === "string") {
                                        languageCodes.push(dataItem.languageCode);
                                    } else {
                                        languageCodes.push(...Array.from(dataItem.languageCode));
                                    }

                                    const newItem = {
                                        fieldname: dataItem.propertyName,
                                        fieldalias: dataItem.fieldAlias,
                                        dataType: dataItem.dataType || "string",
                                        havingDataType: dataItem.havingDataType || "string",
                                        languageCodes: languageCodes,
                                        aggregationfunction: dataItem.aggregation,
                                        formatting: dataItem.formatting
                                    };

                                    if (dataItem.subSelection) {
                                        const subTempArray = [];

                                        dataItem.subSelection.fields.forEach((subDataItem) => {
                                            const languageCodes = [];
                                            if (typeof dataItem.languageCode === "string") {
                                                languageCodes.push(subDataItem.languageCode);
                                            } else {
                                                languageCodes.push(...Array.from(subDataItem.languageCode));
                                            }

                                            subTempArray.push({
                                                fieldname: subDataItem.propertyName,
                                                fieldalias: subDataItem.fieldAlias,
                                                dataType: subDataItem.dataType || "string",
                                                havingDataType: subDataItem.havingDataType || "string",
                                                languageCodes: languageCodes,
                                                aggregationfunction: subDataItem.aggregation,
                                                formatting: subDataItem.formatting
                                            });
                                        });

                                        if (!forSaving) {
                                            newItem.fields = subTempArray;
                                        } else {
                                            newItem.subSelection = dataItem.subSelection;
                                        }
                                    }

                                    tempArray.push(newItem);
                                });

                                if (tempArray.length > 0) {
                                    linkedToRow.fields = tempArray;
                                }
                            } else if (item.classList.contains("exportFieldsLinks")) {
                                tempArray = $(item.querySelector("select.select-details-links")).getKendoMultiSelect().value();
                                if (tempArray.length > 0) {
                                    linkedToRow.linkfields = tempArray;
                                }
                            } else if (item.classList.contains("linkedToList")) {
                                if (item.classList.contains("direction-down")) {
                                    connections = connections.concat(this.createJsonRequestConnectionsList(item.querySelector(".linkedToContainer"), "down", forSaving));
                                } else if (item.classList.contains("direction-up")) {
                                    connections = connections.concat(this.createJsonRequestConnectionsList(item.querySelector(".linkedToContainer"), "up", forSaving));
                                }
                            }
                        });

                        if (connections.length > 0) {
                            linkedToRow.connections = connections;
                        }
                    }

                    linkedToSection.connectionrows.push(linkedToRow);
                });

                result.push(linkedToSection);
            });

            return result;
        }

        /**
         * Retrieves the JSON via get_items.jcl based on the current settings.
         * @param {object} requestVariables Additional parameters for the request.
         * @returns {object} A JSON object with all the data that was requested.
         */
        async getJsonResult(requestVariables = null) {
            let rootUrl = `${this.settings.getItemsUrl}`;
            let parameters = { settings: this.createJsonRequest() };

            if (requestVariables) {
                parameters = Object.assign({}, requestVariables, parameters);
            }

            return Wiser.api({
                method: "POST",
                contentType: "application/json",
                url: rootUrl,
                data: JSON.stringify(parameters)
            });
        }

        /**
         * Retrieves the query that get_items.jcl would execute with the current data selector.
         * @returns {string} A string containing the entire query.
         */
        async getQuery() {
            let rootUrl = `${this.settings.getItemsUrl}/query`;
            let parameters = { settings: this.createJsonRequest() };

            return Wiser.api({
                method: "POST",
                contentType: "application/json",
                url: rootUrl,
                data: JSON.stringify(parameters)
            });
        }

        async save(name) {
            const postData = {
                name: name,
                modules: this.selectedModules.join(","),
                requestJson: JSON.stringify(this.createJsonRequest()),
                savedJson: JSON.stringify(this.createJsonRequest(true)),
                showInExportModule: document.getElementById("showInExportModule").checked ? 1 : 0,
                showInCommunicationModule: document.getElementById("showInCommunicationModule").checked ? 1 : 0,
                availableForRendering: document.getElementById("availableForRendering").checked ? 1 : 0,
                showInDashboard: document.getElementById("showInDashboard").checked ? 1 : 0,
                availableForBranches: document.getElementById("availableForBranches").checked ? 1 : 0,
                allowedRoles: this.allowedRoles.value().join()
            };

            const saveResult = await Wiser.api({
                url: `${this.settings.wiserApiRoot}data-selectors/save`,
                method: "POST",
                contentType: "application/json",
                dataType: "json",
                data: JSON.stringify(postData)
            });

            // Check if the load select exists.
            const dropdown = $("#dataSelectorItems");
            if (dropdown.length > 0) {
                // Refresh the dropdown to include the newly made item.
                dropdown.getKendoDropDownList().dataSource.read();
            }

            // Remember current ID and name.
            this.currentId = saveResult;
            this.currentName = name;

            // Set ID and name in header.
            const header = document.getElementById("dataSelectorId");
            header.querySelector("h3 > label").innerHTML = `${this.currentName} (ID: ${this.currentId})`;
            header.style.display = "";

            // Trigger save event. This event can be used on places that load the data selector in an iframe, such as the module DynamicItems.
            const afterSaveEvent = new CustomEvent("dataSelectorAfterSave", { detail: saveResult });
            document.dispatchEvent(afterSaveEvent);

            return saveResult;
        }

        saveWithPrompt() {
            const kendoPrompt = $("<div />").kendoPrompt({
                title: "Opslaan",
                content: "Geef een naam op voor deze data selector.",
                value: this.currentName,
                visible: false
            }).getKendoPrompt();

            if (kendoPrompt.wrapper && kendoPrompt.wrapper[0]) {
                const input = kendoPrompt.wrapper[0].querySelector("input[type='text']");
                input.maxLength = 100;
            }

            kendoPrompt.open().result.done((input) => {
                window.processing.addProcess("checkSavedNameExists");
                Wiser.api({ url: `${this.settings.wiserApiRoot}data-selectors/${encodeURIComponent(input)}/exists` }).then((existsResult) => {
                    window.processing.removeProcess("checkSavedNameExists");

                    if (existsResult === 0) {
                        this.currentName = input;

                        window.processing.addProcess("dataSelectorSave");
                        this.save(input).then(
                            () => {
                                window.processing.removeProcess("dataSelectorSave");
                                Wiser.showMessage({
                                    title: "Opslaan succesvol",
                                    content: "De data selector is succesvol opgeslagen."
                                });
                            },
                            (error) => {
                                console.error(error);
                                window.processing.removeProcess("dataSelectorSave");
                                Wiser.alert({
                                    title: "Opslaan mislukt",
                                    content: "Er is een fout opgetreden tijdens het opslaan van de data selector. Probeer het a.u.b. nogmaals."
                                });
                            }
                        );

                        return;
                    }

                    // Data selector with the given name already exists; ask for overwrite confirmation.
                    Wiser.confirm({
                        title: "Data Selector",
                        content: "Een data selector met deze naam bestaat al. Wilt u deze overschrijven?",
                        messages: {
                            okText: "Ja",
                            cancel: "Nee"
                        }
                    }).result.done(() => {
                        this.currentName = input;

                        window.processing.addProcess("dataSelectorSave");
                        this.save(input).then(
                            () => {
                                window.processing.removeProcess("dataSelectorSave");
                                Wiser.showMessage({
                                    title: "Opslaan succesvol",
                                    content: "De data selector is succesvol opgeslagen."
                                });
                            },
                            () => {
                                window.processing.removeProcess("dataSelectorSave");
                                Wiser.alert({
                                    title: "Opslaan mislukt",
                                    content: "Er is een fout opgetreden tijdens het opslaan van de data selector. Probeer het a.u.b. nogmaals."
                                });
                            }
                        );
                    });
                });
            });
        }

        loadById(id) {
            this.dataLoad.loadById(id);
        }

        showRequestInputWindow() {
            return new Promise((resolve) => {
                const inputGrid = $("<div />").kendoGrid({
                    toolbar: [
                        { name: "create" }
                    ],
                    columns: [
                        { field: "k", title: "Naam" },
                        { field: "v", title: "Waarde" },
                        { command: { name: "destroy", text: "", iconClass: "k-icon k-i-delete" }, width: 150 }
                    ],
                    editable: {
                        mode: "incell",
                        createAt: "bottom",
                        confirmation: false
                    },
                    selectable: true
                }).getKendoGrid();
                $("<div />").kendoTooltip({ filter: ".k-grid-delete", content: "Verwijderen" });

                const closeButton = $('<button type="button" style="margin-top: 1em;">Sluiten</button>').kendoButton().getKendoButton();

                const windowContent = $("<div />");
                windowContent.append(
                    inputGrid.element,
                    closeButton.element
                );

                const inputWindow = windowContent.kendoWindow({
                    width: "80%",
                    title: "Request variabelen",
                    appendTo: "#dataBuilder",
                    modal: true,
                    resizable: false,
                    visible: false,
                    close: () => {
                        const gridData = inputGrid.dataSource.view().toJSON();
                        const returnData = {};
                        gridData.forEach((item) => {
                            returnData[item.k] = item.v;
                        });

                        // Resolve the promise.
                        resolve({
                            data: returnData
                        });
                    }
                }).getKendoWindow().open().center();

                closeButton.bind("click", () => {
                    inputWindow.close();
                });
            });
        }

        async handleWindowMessage(data) {
            if (!data || !data.action) {
                return;
            }

            let actionResult = null;
            switch (data.action) {
                case "save":
                    // Saves the data selector with the given name. Will overwrite without warning.
                    window.processing.addProcess("dataSelectorSave");
                    try {
                        actionResult = await this.save(data.name);
                    } catch (e) {
                        Wiser.alert({
                            title: "Opslaan mislukt",
                            content: "Er is een fout opgetreden tijdens het opslaan van de data selector. Probeer het a.u.b. nogmaals."
                        });
                    }
                    window.processing.removeProcess("dataSelectorSave");
                    break;
                case "get-result":
                    window.processing.addProcess("getJsonResult");
                    try {
                        actionResult = await this.getJsonResult();
                    } catch (e) {
                        Wiser.alert({
                            title: "Ophalen data mislukt",
                            content: "Er is een fout opgetreden tijdens het ophalen van het data selector resultaat. Probeer het a.u.b. nogmaals."
                        });
                    }
                    window.processing.removeProcess("getJsonResult");
                    break;
                case "get-entity-type":
                    window.processing.addProcess("getEntityType");
                    actionResult = this.selectedEntityType;
                    window.processing.removeProcess("getEntityType");
                    break;
            }

            if (typeof data.callback === "string" && data.callback.trim() !== "") {
                this.postMessageToParent({
                    callback: data.callback,
                    actionResult: actionResult
                });
            }
        }

        postMessageToParent(data = {}) {
            const postData = {
                from: "DataSelector"
            };
            Object.assign(postData, data);

            parent.postMessage(postData);
        }

        async initializeCodeMirrorElements() {
            const jsonText = document.getElementById("jsonText");
            if (jsonText) {
                // We only load code mirror when we actually need it.
                await Misc.ensureCodeMirror();

                this.jsonCodeMirrorEditor = CodeMirror.fromTextArea(jsonText, {
                    mode: "application/json",
                    lineNumbers: true,
                    readOnly: true
                });
            }

            const queryText = document.getElementById("queryText");
            if (queryText) {
                // We only load code mirror when we actually need it.
                await Misc.ensureCodeMirror();

                this.queryCodeMirrorEditor = CodeMirror.fromTextArea(queryText, {
                    mode: "text/x-mysql",
                    lineNumbers: true,
                    readOnly: true
                });
            }
        }

        setHavingDynamicBindings(context) {
            let containerElement;
            if (context !== undefined && context !== null) {
                containerElement = $(context);
            } else {
                containerElement = $(this.container).find(".blockItems");
            }

            // BUTTONS.
            containerElement.find(".or-button-scope").on("click", (e) => {
                this.addOrHaving($(e.currentTarget).closest("section"));
            });

            containerElement.find(".delete-button").data("kendoButton").bind("click", (e) => {
                const cont = e.sender.element.closest("section");

                if (cont.find(".inputRow").length <= 1) {
                    cont.remove();
                } else {
                    e.sender.element.closest(".inputRow").remove();
                }
            });

            containerElement.find("select.scope-property-select").each((i, elem) => {
                const dbInput = $(elem).closest(".inputRow");
                $(elem).data("kendoDropDownList").bind("change", (e) => {
                    const dataItem = e.sender.dataItem(e.sender.select());

                    dbInput.find("select.scope-value-select").data("kendoMultiSelect").setDataSource({
                        transport: {
                            read: (options) => {
                                Wiser.api({
                                    url: `${this.settings.wiserApiRoot}entity-properties/${encodeURIComponent(dataItem.entityName)}/unique-values/${encodeURIComponent(dataItem.propertyName)}?languageCode=${dataItem.languageCode}`,
                                    dataType: "json"
                                }).then((result) => {
                                    const items = [
                                        {
                                            text: "Actuele datum",
                                            value: "{NowMysqlDate}"
                                        },
                                        {
                                            text: "Actuele datum en tijd",
                                            value: "{NowMysqlTime}"
                                        },
                                        ...result.map(item => {
                                            return {
                                                text: item,
                                                value: item
                                            };
                                        })
                                    ];

                                    options.success(items);
                                }).catch((result) => {
                                    options.error(result);
                                });
                            }
                        }
                    });
                });
            });

            containerElement.find("select.scope-comparison-select").each((i, elem) => {
                const dbInput = $(elem).closest(".inputRow");
                $(elem).getKendoDropDownList().bind("change", (e) => {
                    const value = e.sender.value();

                    switch (value) {
                        case "is equal to":
                        case "is not equal to":
                            dbInput.find("span.scope-value-select").show();
                            dbInput.find("div.free-input").hide();
                            break;
                        case "is empty":
                        case "is not empty":
                            dbInput.find("span.scope-value-select").hide();
                            dbInput.find("div.free-input").hide();
                            break;
                        default:
                            dbInput.find("span.scope-value-select").hide();
                            dbInput.find("div.free-input").show();
                            break;
                    }
                });
            });
        }

        addOrHaving(section) {
            const elem = $(document.getElementById("addScopeTemplate").innerHTML);
            section.append(elem);

            // Because the having scopes can have multiple fields with the same field name it must match on either alias or value instead of only value.
            const newHavingSelects = elem.get(0).querySelectorAll("select.scope-property-select");
            newHavingSelects.forEach((select) => {
                select.dataset.dataValueField = "aliasOrValue";
            });

            this.initializeKendoElements(elem.get(0));
            this.setHavingDynamicBindings(elem);

            this.setSelectedFieldsDataSources(newHavingSelects, false);

            return elem.get(0);
        }

        addHaving() {
            const newSection = $(`<section>${document.getElementById("addScopeTemplate").innerHTML}</section>`);
            this.havingContainer.append(newSection);

            // Because the having scopes can have multiple fields with the same field name it must match on either alias or value instead of only value.
            const newHavingSelects = newSection.get(0).querySelectorAll("select.scope-property-select");
            newHavingSelects.forEach((select) => {
                select.dataset.dataValueField = "aliasOrValue";
            });

            this.initializeKendoElements(newSection.get(0));
            this.setHavingDynamicBindings(newSection);

            this.setSelectedFieldsDataSources(newHavingSelects, false);

            return newSection.get(0);
        }

        createFieldEditor(options) {
            const settings = Object.assign({
                includeDataTypeField: false,
                includeHavingDataTypeField: false,
                includeLanguageCodeField: true,
                includeFieldAliasField: true,
                includeIsItemIdField: true
            }, options);

            const editor = $($("#fieldEditorTemplate").html());
            if (!settings.includeDataTypeField) {
                editor.find(".dataTypeWrapper").remove();
            }
            if (!settings.includeHavingDataTypeField) {
                editor.find(".havingDataTypeWrapper").remove();
            }
            if (!settings.includeLanguageCodeField) {
                editor.find(".languageCodeWrapper").remove();
            }
            if (!settings.includeFieldAliasField) {
                editor.find(".fieldAliasWrapper").remove();
            }
            if (!settings.includeIsItemIdField) {
                editor.find(".isItemIdWrapper").remove();
            }

            editor.appendTo(document.body);

            this.initializeKendoElements(editor.get(0));

            // Turn it into a dialog.
            editor.kendoDialog({
                closable: false,
                width: "80%",
                height: "80%",
                visible: false,
                initOpen: () => {
                    editor.css("height", "auto");
                }
            }).getKendoDialog();

            return editor;
        }

        /**
         * Opens the field editor that allows the user to set some additional information about fields.
         * @param {any} dataItem The data item of the widget's data source that will be updated.
         * @param {object} options A settings object.
         * @param {boolean} forHaving Whether the field editor is for a having input.
         */
        async openFieldEditor(dataItem, options, forHaving = false) {
            if (dataItem.value === "") {
                return;
            }

            if (forHaving) {
                options.includeDataTypeField = false;
                options.includeHavingDataTypeField = true;
            }

            const itemProperties = this.createFieldEditor(options);

            const saveButton = itemProperties.find("button.saveItemProperties").getKendoButton();
            const closeButton = itemProperties.find("button.closeItemProperties").getKendoButton();
            const closeEditor = () => {
                // Unbind clicks to make sure these buttons don't accidentally do things anymore.
                saveButton.unbind("click");
                closeButton.unbind("click");

                // Close and immediately remove the entire dialog.
                itemProperties.getKendoDialog().close().destroy();
            };

            // First unbind previous handlers.
            saveButton.unbind("click");
            closeButton.unbind("click");

            // Check if the value being edited is the same as the value of the data item that was passed to the function.
            // The editor will be closed if it's the same value, and it will continue otherwise.
            const currentValue = itemProperties.data("currentValue");
            if (!itemProperties.hasClass("hidden") && currentValue !== undefined && currentValue === dataItem.value) {
                closeEditor();
                return;
            }

            // Save current value.
            itemProperties.data("currentValue", dataItem.value);

            itemProperties.find("h5 span").text(dataItem.displayName);

            const dataTypeField = itemProperties.find("select.dataType").getKendoDropDownList();
            const havingDataTypeField = itemProperties.find("select.havingDataType").getKendoDropDownList();
            const languageCodeField = itemProperties.find("select.languageCode");
            const aggregation = itemProperties.find("select.aggregation").getKendoDropDownList();
            const formatting = itemProperties.find("select.formatting").getKendoComboBox();
            const fieldAlias = itemProperties.find("input.fieldAlias");
            const isItemId = itemProperties.find("input.isItemId");

            if (dataTypeField) {
                dataTypeField.value(dataItem.dataType || "string");
            }

            if (havingDataTypeField) {
                havingDataTypeField.value(dataItem.havingDataType || "string");
            }

            if (languageCodeField.length > 0) {
                const languageCode = languageCodeField.getKendoMultiSelect();
                languageCode.bind("change", (event) => {
                    itemProperties.find("div.havingDataTypeWrapper, div.aggregationWrapper, div.formattingWrapper, div.fieldAliasWrapper, div.isItemIdWrapper, div.subSelectionWrapper").toggleClass("hidden", event.sender.value().length > 1);
                });

                // Update language codes.
                try {
                    const languageCodesData = await Wiser.api({ url: `${this.settings.serviceRoot}/GET_LANGUAGE_CODES?entityName=${dataItem.entityName || ""}&linkType=${dataItem.linkType || "0"}&propertyName=${dataItem.propertyName}` });
                    console.log("languageCodesData", languageCodesData);
                    languageCode.setDataSource({
                        data: [...languageCodesData]
                    })

                    if (dataItem.languageCode) {
                        if (typeof dataItem.languageCode === "string") {
                            // Old method; single string value.
                            languageCode.value(dataItem.languageCode || "");
                        } else {
                            // New method; array with language codes.
                            const selectedLanguageCodes = Array.from(dataItem.languageCode);

                            // Check if there are custom language codes in the array which should be added first.
                            selectedLanguageCodes.forEach((lc) => {
                                if (languageCodesData.findIndex((item) => item.value === lc) === -1) {
                                    // Custom language code; add it first.
                                    languageCode.dataSource.add({ text: lc, value: lc });
                                }
                            });

                            languageCode.value(selectedLanguageCodes);
                        }
                    }

                    // Set current language code.
                    languageCode.value(dataItem.languageCode || "");
                    if (languageCode.value().length > 1) {
                        itemProperties.find("div.havingDataTypeWrapper, div.aggregationWrapper, div.formattingWrapper, div.fieldAliasWrapper, div.isItemIdWrapper, div.subSelectionWrapper").addClass("hidden");
                    }
                } catch (e) {
                    console.error("Error while trying to update language codes", e);
                }
            }

            // Update values.
            if (forHaving) {
                aggregation.value(dataItem.havingAggregation || "");
                formatting.value(dataItem.havingFormatting || "");
            } else {
                aggregation.value(dataItem.aggregation || "");
                formatting.value(dataItem.formatting || "");
            }

            if (fieldAlias.length > 0) {
                fieldAlias.val(dataItem.fieldAlias);
            }

            if (isItemId.length > 0) {
                const checkbox = isItemId.get(0);
                const subSelectionWrapper = itemProperties.find(".subSelectionWrapper");

                const toggleSubSelection = () => {
                    subSelectionWrapper.toggle(checkbox.checked);
                    if (checkbox.checked && !subSelectionWrapper.data("subSelectionCreated")) {
                        this.createSubSelection(itemProperties, dataItem);
                        subSelectionWrapper.data("subSelectionCreated", true);
                    }
                };

                if (dataItem.subSelection) {
                    checkbox.checked = true;
                    toggleSubSelection();
                }

                isItemId.on("change", () => {
                    toggleSubSelection();
                });
            }

            // Bind new handlers.
            saveButton.bind("click", () => {
                if (fieldAlias.length > 0) {
                    const newFieldAlias = fieldAlias.val().trim();

                    // Check if alias is already in use. The check only needs to be performed if one has been set, and if it's not the same as the previous value.
                    if (newFieldAlias.length > 0 && dataItem.fieldAlias.toLowerCase() !== newFieldAlias.toLowerCase() && this.checkIfAliasInUse(newFieldAlias)) {
                        Wiser.alert({
                            title: "Alias in gebruik",
                            content: "Deze alias is al in gebruik. Kies een andere alias."
                        });
                        return;
                    }

                    dataItem.set("fieldAlias", newFieldAlias);

                    // The field "aliasOrValue" is used by the group by select because it doesn't know which entity a field belongs to.
                    // It can only match on either alias or value.
                    if (newFieldAlias.length > 0) {
                        dataItem.set("aliasOrValue", newFieldAlias);
                    } else {
                        dataItem.set("aliasOrValue", dataItem.value);
                    }
                }

                if (dataTypeField) {
                    dataItem.set("dataType", dataTypeField.value());
                }

                if (languageCodeField.length > 0) {
                    const languageCode = languageCodeField.getKendoMultiSelect();
                    dataItem.set("languageCode", languageCode.value());
                }

                if (forHaving) {
                    dataItem.set("havingAggregation", aggregation.value());
                    dataItem.set("havingFormatting", formatting.value());
                } else {
                    dataItem.set("aggregation", aggregation.value());
                    dataItem.set("formatting", formatting.value());
                }

                if (isItemId.length > 0) {
                    const subSelectionWrapper = itemProperties.find(".subSelectionWrapper");

                    if (!isItemId.prop("checked")) {
                        dataItem.set("subSelection", null);
                    } else {
                        const subSelection = {
                            entityType: subSelectionWrapper.find("select.sub-entity-select").getKendoDropDownList().value(),
                            fields: Array.from(subSelectionWrapper.find("select.sub-select-details").getKendoMultiSelect().dataItems())
                        };
                        dataItem.set("subSelection", subSelection);
                    }
                }

                this.updateSelectedFields();
                this.setSelectedFieldsDataSources();

                closeEditor();
            });
            closeButton.bind("click", () => {
                closeEditor();
            });

            // The field editor has been prepared; show it.
            const dialog = itemProperties.getKendoDialog();
            if (dataItem.fieldAlias !== "") {
                dialog.title(`Eigenschappen van '${dataItem.fieldAlias}'`);
            } else {
                dialog.title(`Eigenschappen van '${dataItem.displayName}'`);
            }
            dialog.open();

            // Increase z-index, because otherwise the k-animation-container of the multiselect will be painted on top of this dialog, causing you to not be able to click everything in the dialog.
            this.dialogZindex++;
            dialog.wrapper.css("z-index", this.dialogZindex);
        }

        /**
         * Creates an export fields selection for a field editor.
         * @param {HTMLElement} fieldEditor The field editor element.
         * @param {any} dataItem The dataItem object that this sub selection will be applied to.
         */
        createSubSelection(fieldEditor, dataItem) {
            const subSelection = $(document.getElementById("subSelectionTemplate").innerHTML);

            const subSelectionWrapper = fieldEditor.find(".subSelectionWrapper");
            subSelectionWrapper.empty().append(subSelection);

            this.initializeKendoElements(subSelectionWrapper.get(0));

            const subEntitySelect = subSelectionWrapper.find("select.sub-entity-select");
            const subPropertySelect = subSelectionWrapper.find("select.sub-select-details");

            const subEntitySelectWidget = subEntitySelect.getKendoDropDownList();
            const subPropertySelectWidget = subPropertySelect.getKendoMultiSelect();

            subEntitySelectWidget.setDataSource({
                data: this.allEntityTypes.map((entityType) => {
                    return { text: entityType, value: entityType };
                })
            });
            subEntitySelectWidget.bind("cascade", async (e) => {
                const response = await Wiser.api({
                    url: `${this.settings.wiserApiRoot}data-selectors/entity-properties/${e.sender.value()}/?forExportMode=${this.useExportMode}`
                });

                // Create clone of "response" so it doesn't use the reference value, but a completely new object.
                // Although it's also possible to use "[...response]", this JSON trick works better as it also clones deep properties.
                const availableProperties = JSON.parse(JSON.stringify(response));

                // Create a "unique value" for every property, based on the normal value.
                // The group by select uses this.
                availableProperties.forEach((property) => {
                    // Initialize some additional properties.
                    property.aggregation = "";
                    property.formatting = "";
                    property.fieldAlias = "";
                    property.direction = "ASC";

                    // Initial value of the "alias or value" should just be the value.
                    property.aliasOrValue = property.value;
                });

                this.updateWidgetDataSource(subPropertySelectWidget, availableProperties);
            });

            // Update value. The "cascade" event automatically updates the multi-select as well.
            if (dataItem.subSelection && dataItem.subSelection.entityType) {
                if (dataItem.subSelection.fields && dataItem.subSelection.fields.length > 0) {
                    subPropertySelectWidget.one("dataSourceUpdated", (e) => {
                        subPropertySelectWidget.dataSource.view().forEach((propertyDataItem) => {
                            const field = dataItem.subSelection.fields.find((subField) => {
                                return subField.value === propertyDataItem.value;
                            });

                            if (!field) {
                                // Skip if no matching field found.
                                return;
                            }

                            propertyDataItem.set("languageCode", field.languageCode);
                            propertyDataItem.set("aggregation", field.aggregation);
                            propertyDataItem.set("formatting", field.formatting);
                            propertyDataItem.set("fieldAlias", field.fieldAlias);
                        });

                        const values = dataItem.subSelection.fields.map((field) => field.value);
                        e.sender.value(values);
                    });
                }

                subEntitySelectWidget.value(dataItem.subSelection.entityType);
            }

            // Clicking on the tags.
            subPropertySelectWidget.wrapper.find("div.k-chip-list").on("click", "span.k-chip", (e) => {
                const clickedElement = $(e.target);
                if (clickedElement.closest("span.k-chip-remove-action").length > 0) {
                    return;
                }

                // This will select the tag itself (a li element).
                const tagElement = $(e.currentTarget);

                const index = tagElement.index();
                const selectedDataItem = subPropertySelectWidget.dataItems()[index];
                const propertyDataItem = subPropertySelectWidget.dataSource.view().find((di) => {
                    return di.value === selectedDataItem.value;
                });

                this.openFieldEditor(propertyDataItem, {
                    includeLanguageCodeField: !this.useExportMode,
                    includeIsItemIdField: false
                });
            });

            this.giveCustomClickLogic(subPropertySelectWidget);
        }

        initializeKendoElements(context) {
            if (!context) {
                context = window.document;
            }

            //NUMERIC FIELD
            context.querySelectorAll(".numeric").forEach((e) => {
                const element = $(e);
                const options = Object.assign({
                    decimals: 0,
                    format: "#"
                }, element.data());
                element.kendoNumericTextBox(options);
            });

            //BUTTONS
            context.querySelectorAll(".kendoButton").forEach((e) => {
                const element = $(e);
                const options = Object.assign({}, element.data());
                element.kendoButton(options);
            });

            //DROPDOWNLIST
            context.querySelectorAll(".drop-down-list").forEach((e) => {
                const element = $(e);
                const data = element.data();

                // Check if there's a template ID defined for the tag template.
                const templateId = data.templateId || "";
                delete data.templateId;

                // Check if there's a template ID defined for the tag template.
                const valueTemplateId = data.valueTemplateId || "";
                delete data.valueTemplateId;

                const options = Object.assign({
                    filter: "contains",
                    height: 400
                }, data);

                if (templateId !== "") {
                    options.template = document.getElementById(templateId).innerHTML;
                }

                if (valueTemplateId !== "") {
                    options.valueTemplate = document.getElementById(valueTemplateId).innerHTML;

                    if (typeof options.optionLabel === "string") {
                        const newOptionLabel = {};
                        newOptionLabel[options.dataTextField] = options.optionLabel;
                        newOptionLabel[options.dataValueField] = "";

                        // These fields are to make sure no fields are missing for the entity properties.
                        newOptionLabel.displayName = options.optionLabel;
                        newOptionLabel.entityName = "";
                        newOptionLabel.fieldAlias = "";

                        options.optionLabel = newOptionLabel;
                    }
                }

                const widget = element.kendoDropDownList(options).getKendoDropDownList();
                Wiser.fixKendoDropDownScrolling(widget);
            });

            //COMBOBOX
            context.querySelectorAll(".combo-select").forEach((e) => {
                const element = $(e);

                let openOnFocus = false;
                if (element.data("openOnFocus") === true) {
                    openOnFocus = true;
                    element.removeData("openOnFocus");
                }

                const options = Object.assign({
                    height: 400
                }, element.data());
                const widget = element.kendoComboBox(options).getKendoComboBox();
                Wiser.fixKendoDropDownScrolling(widget);

                if (openOnFocus) {
                    widget.input.on("click", () => {
                        if (widget.dataSource.view().length > 0) {
                            widget.open();
                        }
                    });
                }
            });

            //MULTISELECT
            context.querySelectorAll(".multi-select, .multi-select-add").forEach((e) => {
                const element = $(e);
                const data = element.data();

                const acceptCustomInput = data.acceptCustomInput;
                delete data.acceptCustomInput;

                // Check if there's a template ID defined for the tag template.
                const itemTemplateId = data.itemTemplateId || "";
                delete data.itemTemplateId;

                // Check if there's a template ID defined for the tag template.
                const tagTemplateId = data.tagTemplateId || "";
                delete data.tagTemplateId;

                const sortable = data.sortable;
                delete data.sortable;

                const options = Object.assign({
                    autoClose: false,
                    select: (event) => {
                        event.sender.input.val("");
                        setTimeout(() => { event.sender.search(""); }, 50);
                    }
                }, data);

                if (itemTemplateId !== "") {
                    options.itemTemplate = document.getElementById(itemTemplateId).innerHTML;
                }

                if (tagTemplateId !== "") {
                    options.tagTemplate = document.getElementById(tagTemplateId).innerHTML;
                }

                if (acceptCustomInput) {
                    this.extendMultiSelectWithCustomInput(options);
                }

                const widget = element.kendoMultiSelect(options).getKendoMultiSelect();
                Wiser.fixKendoDropDownScrolling(widget);

                if (sortable) {
                    this.extendMultiSelectWithSortable(widget);
                }
            });

            this.allowedRoles = $("#allowedRoles").kendoMultiSelect({
                dataSource: {
                    transport: {
                        read: {
                            url: `${this.settings.serviceRoot}/GET_ROLES`
                        }
                    }
                },
                dataTextField: "roleName",
                dataValueField: "id",
                multiple: "multiple"
            }).data("kendoMultiSelect");

            //TREEVIEW
            context.querySelectorAll(".checkTree").forEach((e) => {
                const element = $(e);
                const options = Object.assign({
                    checkboxes: {
                        checkChildren: true
                    }
                }, element.data());
                element.kendoTreeView(options);
            });

            //HORIZONTAL SCROLL
            context.querySelectorAll(".hScroll").forEach((e) => {
                const element = $(e);
                const options = Object.assign({ scrollable: true }, element.data());
                element.kendoMenu(options);
            });
        }

        extendMultiSelectWithCustomInput(options) {
            const onClickEnter = (e) => {
                if (e.keyCode !== 13) {
                    return;
                }

                const widget = e.data.widget;
                const value = widget.input.val().trim();
                if (!value || value.length === 0) {
                    return;
                }

                this.addOrSelectItem(widget, value);

                widget.input.val("");
            };

            const onDataBound = (e) => {
                const element = e.sender.element.closest(".k-multiselect");
                element.find(".k-input-inner").off("keyup");
                element.find(".k-input-inner").on("keyup", { widget: e.sender }, onClickEnter);
            };

            return Object.assign(options, {
                dataBound: onDataBound
            });
        }

        extendMultiSelectWithSortable(multiSelect) {
            if (!multiSelect || !multiSelect.tagList) {
                return;
            }

            multiSelect.tagList.kendoSortable({
                change: (e) => {
                    const multiSelectItems = multiSelect.dataSource.data();
                    const sortedValues = [];

                    e.sender.items().toArray().forEach((item) => {
                        const i = $.grep(multiSelectItems, (gItm) => {
                            return gItm.uid === $(item).find("span[data-item-uid]").first().data("itemUid");
                        })[0];
                        sortedValues.push(i[multiSelect.options.dataValueField]);
                    });
                    multiSelect.value(sortedValues);
                }
            });
        }

        /**
         * Extends the functionality of a MultiSelect to have custom clicking logic, which means the dropdown won't open when clicking on one of the tags.
         * @param {kendo.ui.Widget} widget The MultiSelect widget that will receive the custom clicking logic.
         */
        giveCustomClickLogic(widget) {
            const widgetElement = widget.element;

            widgetElement.data("canOpen", false);
            widget.bind("open", (e) => {
                if (widgetElement.data("canOpen") === false) {
                    e.preventDefault();
                }
                widgetElement.data("canOpen", false);
            });
            widget.wrapper.on("click", (e) => {
                if (e.target.closest("span.k-chip") !== null) {
                    widget.close();
                    return;
                }

                widgetElement.data("canOpen", true);
                widget.open();
            });
        }

        /**
         * Adds or selects an item in a multi-select data source. Nothing happens if the value already exists and is already selected.
         * @param {any} widget The Kendo MultiSelect widget.
         * @param {string} value The value that should be selected or added as a string value.
         */
        addOrSelectItem(widget, value) {
            const dataSource = widget.dataSource;
            if (!value || value.length === 0) {
                return;
            }

            // First check if another item with the same exact value already exists.
            const existingItem = dataSource.view().find((dataItem) => {
                return dataItem.value === value;
            });
            if (existingItem === undefined) {
                // Item is new, so add it to the data source.
                dataSource.add({
                    text: value,
                    value: value
                });
            }

            widget.value(widget.value().concat([value]));
        }

        /**
         * Destroys all child Kendo elements that are child elements of an element.
         * @param {HTMLElement | Document | jQuery} context The context, which can be a DOM element, Document, or jQuery object.
         */
        destroyChildKendoWidgets(context) {
            if (!(context instanceof HTMLElement) && !(context instanceof Document) && !(context instanceof $)) {
                return;
            }

            $("select.drop-down-list, select.multi-select, select.multi-select-add, div.checkTree", context).each((i, element) => {
                let dataKey;
                if (element.classList.contains("drop-down-list")) {
                    dataKey = "kendoDropDownList";
                } else if (element.classList.contains("multi-select") || element.classList.contains("multi-select-add")) {
                    dataKey = "kendoMultiSelect";
                } else if (element.classList.contains("checkTree")) {
                    dataKey = "kendoTreeView";
                } else {
                    return;
                }

                // Try to retrieve the Kendo widget.
                const kendoWidget = $(element).data(dataKey);
                if (kendoWidget === undefined || kendoWidget === null) {
                    return;
                }
                kendoWidget.destroy();
            });
        }

        /**
         * Checks if there's a data selector that has "show in dashboard" already enabled. This will only occur if the
         * checkbox for "show in dashboard" is be enabled.
         */
        async checkForDashboardConflict(event) {
            if (!event.currentTarget.checked) return;

            const result = await Wiser.api({
                url: `${this.settings.wiserApiRoot}data-selectors/${this.currentId}/check-dashboard-conflict`,
                method: "GET"
            });

            if (!result) return;

            Wiser.alert({
                title: "Andere data selector in gebruik",
                content: `De data selector '${result}' wordt al gebruikt om te tonen in het dashboard. Als u deze data selector opslaat, dan zal '${result}' niet meer gebruikt worden in het dashboard.`
            });
        }
    }

    window.dataSelector = new DataSelector(moduleSettings);
})(moduleSettings);
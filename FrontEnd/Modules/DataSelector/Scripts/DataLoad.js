import "../../Base/Scripts/Processing.js";

(() => {
    class DataLoad {
        constructor(dataSelector) {
            this.dataSelector = dataSelector || this;
            this.loadWindow = null;
        }

        initialize() {
            this.initializeLoadWindow();
        }

        initializeLoadWindow() {
            this.loadWindow = $("<div />").kendoDialog({
                width: "450px",
                title: "Laden",
                appendTo: "#dataBuilder",
                modal: true,
                resizable: false,
                visible: false,
                actions: [
                    {
                        text: "Laden",
                        primary: true,
                        action: () => {
                            const dataSelectorId = $("#dataSelectorItems").getKendoDropDownList().value();
                            location.assign(`?load=${dataSelectorId}`);
                        }
                    },
                    {
                        text: "Verwijderen",
                        action: () => {
                            const dataSelectorPicker = $("#dataSelectorItems").getKendoDropDownList();
                            const chosenDataSelector = dataSelectorPicker.dataItem();

                            if (!Number.isInteger(chosenDataSelector.id) || chosenDataSelector.id <= 0) {
                                return false;
                            }

                            Wiser.confirm({
                                title: "Data Selector",
                                content: `Weet je zeker dat je de data selector met de naam '${chosenDataSelector.name}' wilt verwijderen? Deze actie is onomkeerbaar.`,
                                messages: {
                                    okText: "Ja",
                                    cancel: "Nee"
                                }
                            }).result.done(() => {
                                window.processing.addProcess("removeDataSelector");
                                try {
                                    this.removeById(chosenDataSelector.id).then(() => {
                                        dataSelectorPicker.select("");
                                        dataSelectorPicker.dataSource.read();
                                        window.processing.removeProcess("removeDataSelector");
                                        Wiser.showMessage({
                                            title: "Data Selector verwijderd",
                                            content: "De data selector is succesvol verwijderd."
                                        });
                                    });
                                } catch (e) {
                                    window.processing.removeProcess("removeDataSelector");
                                    Wiser.alert({
                                        title: "Data Selector verwijderen mislukt",
                                        content: "Er is een fout opgetreden tijdens het verwijderen van de data selector. Probeer het a.u.b. nogmaals."
                                    });
                                }
                            });

                            // Return false will prevent dialog from closing.
                            return false;
                        }
                    },
                    {
                        text: "Annuleren"
                    }
                ]
            }).getKendoDialog();

            const dropdown = $('<select id="dataSelectorItems" />');
            this.loadWindow.content(dropdown);

            dropdown.kendoDropDownList({
                optionLabel: "Selecteer data selector",
                dataTextField: "name",
                dataValueField: "id",
                filter: "contains",
                autoBind: false,
                dataSource: {
                    transport: {
                        read: async (options) => {
                            try {
                                const results = await Wiser.api({ url: `${this.dataSelector.settings.wiserApiRoot}data-selectors` });
                                options.success(results);
                            } catch (exception) {
                                console.error(exception);
                                options.error(exception);
                            }
                        }
                    }
                }
            });

            Wiser.fixKendoDropDownScrolling(dropdown.getKendoDropDownList());
        }

        showLoadDialog() {
            this.loadWindow.open().center();
        }

        async loadById(id) {
            if (typeof id !== "number" || id <= 0) {
                return;
            }

            const connectionBlocksContainer = document.getElementById("connectionBlocks");
            const firstConnectionBlock = connectionBlocksContainer.querySelector(".connectionBlock");

            window.processing.addProcess("dataSelectorLoad");
            const response = await Wiser.api({
                url: `${this.dataSelector.settings.serviceRoot}/GET_DATA_SELECTOR_BY_ID`,
                method: "GET",
                data: { id: id }
            });

            if (!Wiser.validateArray(response)) {
                window.processing.removeProcess("dataSelectorLoad");
                return;
            }

            const savedData = response[0];

            // Set ID and name in header.
            const header = document.getElementById("dataSelectorId");
            header.querySelector("h3 > label").innerHTML = `${savedData.name} (ID: ${savedData.id})`;
            header.style.display = "";

            // Will make the save dialog open with the current name.
            this.dataSelector.currentId = savedData.id;
            this.dataSelector.currentName = savedData.name;

            // Parse saved JSON string to an object.
            // The "savedJson" string contains the new save format, while the "requestJson" string contains the normal request JSON.
            const json = typeof savedData.savedJson === "string" && savedData.savedJson !== "" ? JSON.parse(savedData.savedJson) : JSON.parse(savedData.requestJson);

            this.dataSelector.useExportMode = json.hasOwnProperty("useExportMode") && json.useExportMode;
            document.getElementById("useExportMode").checked = this.dataSelector.useExportMode;

            if (savedData.modules !== "") {
                this.dataSelector.selectedModules = savedData.modules.split(",").map((m) => {
                    return Number.parseInt(m, 10);
                });
                this.dataSelector.selectedModules.sort();

                const moduleSelect = document.getElementById("moduleSelect");
                for (let moduleId of this.dataSelector.selectedModules) {
                    moduleSelect.querySelector(`li[data-module-id='${moduleId}'] input[type='checkbox']`).checked = true;
                }
            }

            // Set entity type selection.
            let mainEntityName;
            if (json.main.hasOwnProperty("entityName")) {
                mainEntityName = json.main.entityName;
            } else if (Wiser.validateArray(json.main.entities)) {
                mainEntityName = json.main.entities[0];

                // Show a warning when multiple main entities were saved in this data selector.
                if (json.main.entities.length > 1) {
                    Wiser.alert({
                        title: "Data Selector",
                        content: "Let op! Deze data selector was opgeslagen met meerdere hoofdentiteiten, maar dit wordt niet meer ondersteund. Alleen de eerste entiteit wordt geladen."
                    });
                }
            }

            // Stop if there's no main entity saved with this data selector.
            if (typeof mainEntityName !== "string" || mainEntityName === "") {
                window.processing.removeProcess("dataSelectorLoad");
                return;
            }

            this.dataSelector.selectedEntityType = mainEntityName;

            const selectEntity = $("#selectEntity");
            selectEntity.getKendoDropDownList().value(mainEntityName);
            selectEntity.trigger("change");

            // Set field selection.
            const mainFieldSelect = firstConnectionBlock.querySelector("select.select-details");

            const connection = $(firstConnectionBlock).data("connection");
            await connection.updateAvailableProperties();
            connection.setAvailablePropertiesDataSources([mainFieldSelect]);

            if (Wiser.validateArray(json.main.fields)) {
                const selectDetails = $(mainFieldSelect).getKendoMultiSelect();
                this.setFieldsSelectValue(selectDetails, json.main.fields);
            }
            if (Wiser.validateArray(json.main.linkfields)) {
                const widget = $(firstConnectionBlock.querySelector("select.select-details-links")).getKendoMultiSelect();
                const dataSource = widget.dataSource;
                json.main.linkfields.forEach((field) => {
                    dataSource.add({
                        text: field,
                        value: field
                    });
                });
                widget.value(json.main.linkfields);
            }

            // Limit.
            document.getElementById("exportLimit").value = json.limit || "0";

            // Set main scopes.
            if (Wiser.validateArray(json.main.scope)) {
                this.createScopes(json.main.scope, { main: true }).then((scopeCreationResult) => {
                    const elementsToUpdate = scopeCreationResult.createdScopes.map(scope => scope.inputRow.querySelector("select.scope-property-select"));
                    connection.setAvailablePropertiesDataSources(elementsToUpdate);
                });
            }

            // This function will be called after connections are handled, or immediately if there are no connections.
            const onConnectionsCreated = () => {
                this.dataSelector.updateSelectedFields();
                this.dataSelector.setSelectedFieldsDataSources();

                // Group by.
                if (Wiser.validateArray(json.groupBy)) {
                    const groupByFields = [];
                    const widget = $("#groupBy").getKendoMultiSelect();
                    const dataItems = widget.dataSource.view();
                    json.groupBy.forEach((field) => {
                        let existingItem;

                        if (typeof field === "string") {
                            // It's a string (old format).
                            existingItem = dataItems.find(dataItem => dataItem.fieldAlias === field);
                            if (!existingItem) {
                                // Check if match can be made based on property name.
                                existingItem = dataItems.find(dataItem => dataItem.value === field);

                                // Skip row if item couldn't be found.
                                if (!existingItem) {
                                    return;
                                }
                            }
                        } else {
                            // It's an object.
                            // Check older format ("fieldname" and "entity").
                            if (field.hasOwnProperty("fieldname")) {
                                existingItem = dataItems.find(dataItem => dataItem.entityName === field.entity && dataItem.fieldAlias === field.fieldname);
                                if (!existingItem) {
                                    const fieldName = this.dataSelector.useExportMode ? `${field.fieldname}_${field.languagecode}` : field.fieldname;

                                    // Check if match can be made based on property name.
                                    existingItem = dataItems.find(dataItem => dataItem.entityName === field.entity && dataItem.value === fieldName);

                                    // Skip row if item couldn't be found.
                                    if (!existingItem) {
                                        return;
                                    }
                                }
                            } else {
                                // New format.
                                const fieldName = this.dataSelector.useExportMode ? `${field.fieldName}_${field.languageCode}` : field.fieldName;
                                if (typeof field.fieldAlias === "string" && field.fieldAlias !== "") {
                                    existingItem = dataItems.find(dataItem => dataItem.entityName === field.entityName && dataItem.value === fieldName && dataItem.fieldAlias === field.fieldAlias);
                                } else {
                                    existingItem = dataItems.find(dataItem => dataItem.entityName === field.entityName && dataItem.value === fieldName);
                                }

                                // Skip row if item couldn't be found.
                                if (!existingItem) {
                                    return;
                                }
                            }
                        }

                        groupByFields.push(existingItem.aliasOrValue);
                    });
                    widget.value(groupByFields);
                }

                // Having.
                if (Wiser.validateArray(json.having)) {
                    this.createScopes(json.having, {
                        context: this.dataSelector.container.querySelector("div.item.havingList"),
                        forHaving: true
                    });
                }

                // Order by.
                if (Wiser.validateArray(json.orderBy)) {
                    const orderByFields = [];
                    const widget = $("#sorting").getKendoMultiSelect();
                    const dataItems = widget.dataSource.view();
                    json.orderBy.forEach((field) => {
                        let existingItem;

                        if (field.hasOwnProperty("entityName")) {
                            const fieldName = this.dataSelector.useExportMode ? `${field.fieldName}_${field.languageCode}` : field.fieldName;
                            if (typeof field.fieldAlias === "string" && field.fieldAlias !== "") {
                                existingItem = dataItems.find(dataItem => dataItem.entityName === field.entityName && dataItem.value === fieldName && dataItem.fieldAlias === field.fieldAlias);
                            } else {
                                existingItem = dataItems.find(dataItem => dataItem.entityName === field.entityName && dataItem.value === fieldName);
                            }

                            // Skip row if item couldn't be found.
                            if (!existingItem) {
                                return;
                            }
                        } else {
                            existingItem = dataItems.find(dataItem => dataItem.fieldAlias === field.fieldname);
                            if (!existingItem) {
                                const fieldName = this.dataSelector.useExportMode ? `${field.fieldname}_${field.languagecode}` : field.fieldname;
                                // Check if match can be made based on property name (for old format only).
                                existingItem = dataItems.find(dataItem => dataItem.value === fieldName);

                                // Skip row if item couldn't be found.
                                if (!existingItem) {
                                    return;
                                }
                            }
                        }

                        existingItem.set("direction", field.direction);
                        orderByFields.push(existingItem.aliasOrValue);
                    });
                    widget.value(orderByFields);
                }

                // Insecure data checkbox.
                if (typeof json.insecure === "boolean") {
                    document.getElementById("insecureData").checked = json.insecure;
                }

                if (typeof savedData.showInExportModule === "boolean") {
                    document.getElementById("showInExportModule").checked = savedData.showInExportModule;
                }

                if (typeof savedData.showInCommunicationModule === "boolean") {
                    document.getElementById("showInCommunicationModule").checked = savedData.showInCommunicationModule;
                }

                if (typeof savedData.availableForRendering === "boolean") {
                    document.getElementById("availableForRendering").checked = savedData.availableForRendering;
                }

                if (typeof savedData.showInDashboard === "boolean") {
                    document.getElementById("showInDashboard").checked = savedData.showInDashboard;
                }
                
                if (typeof savedData.availableForBranches === "boolean") {
                    document.getElementById("availableForBranches").checked = savedData.availableForBranches;
                }

                if (savedData.allowedRoles) {
                    $("#allowedRoles").getKendoMultiSelect().value(savedData.allowedRoles.split(","));
                }

                // This is where the data selector will complete loading.
                window.processing.removeProcess("dataSelectorLoad");
            };

            // Do connections now.
            if (Wiser.validateArray(json.connections)) {
                // Connections first, rest later.
                this.createConnections(json.connections).then(() => {
                    onConnectionsCreated();
                });
            } else {
                // No connections; do rest immediately.
                onConnectionsCreated();
            }
        }

        async removeById(id) {
            return await Wiser.api({
                url: `${this.dataSelector.settings.serviceRoot}/SET_DATA_SELECTOR_REMOVED`,
                method: "GET",
                data: { itemId: id }
            });
        }

        /**
         * Creates scopes rows for a certain connection. This function is also used to create the "having" rows.
         * @param {Array<any>} scopes Object array of scopes.
         * @param {any} settings Additional settings to determine how the function will behave.
         * @returns {Promise} Returns a Promise that will contain an object with a single property called "createdScopes". This property contains the inputRow elements of all scopes the function created.
         */
        createScopes(scopes, settings = null) {
            return new Promise((resolve) => {
                if (!scopes) {
                    resolve({
                        createdScopes: []
                    });
                    return;
                }

                const options = Object.assign({
                    main: false,
                    context: null,
                    forHaving: false
                }, settings);

                let connectionBlock = options.context;
                if (connectionBlock === null) {
                    const connectionBlocksContainer = document.getElementById("connectionBlocks");
                    connectionBlock = connectionBlocksContainer.querySelector(".connectionBlock");
                }

                if (!connectionBlock) {
                    resolve({
                        createdScopes: []
                    });
                    return;
                }

                const createdScopes = [];
                if (!options.forHaving) {
                    const connectionObject = $(connectionBlock).data("connection");

                    scopes.forEach((scope) => {
                        if (!scope.hasOwnProperty("scoperows")) {
                            return;
                        }

                        let section;
                        let firstPass = true;
                        scope.scoperows.forEach((scopeRow) => {
                            let inputRow;
                            if (firstPass) {
                                section = connectionObject.addScope();
                                inputRow = section.querySelector(".inputRow");
                                firstPass = false;
                            } else {
                                inputRow = connectionObject.addOrScope($(section));
                            }

                            const propertySelectElement = inputRow.querySelector("select.scope-property-select");
                            const propertySelect = $(propertySelectElement).getKendoDropDownList();
                            const comparisonSelect = $(inputRow.querySelector("select.scope-comparison-select")).getKendoDropDownList();
                            const valueSelect = $(inputRow.querySelector("select.scope-value-select")).getKendoMultiSelect();
                            const freeInput = inputRow.querySelector("div.free-input > input[type='text']");
                            connectionObject.setAvailablePropertiesDataSources([propertySelectElement]);

                            createdScopes.push({
                                inputRow: inputRow,
                                widget: propertySelect,
                                value: scopeRow.key
                            });

                            // Have to bind the value select first, so it will trigger once the property select gets updated.
                            switch (scopeRow.operator) {
                                case "is equal to":
                                case "is not equal to":
                                    inputRow.querySelector("span.scope-value-select").style.display = "";
                                    inputRow.querySelector("div.free-input").style.display = "none";
                                    valueSelect.one("dataBound", (e) => {
                                        // Scopes might also have custom values that are not part of the data source.
                                        if (Wiser.validateArray(scopeRow.value, true)) {
                                            scopeRow.value.forEach((item) => {
                                                this.dataSelector.addOrSelectItem(e.sender, item);
                                            });
                                        }
                                    });
                                    break;
                                case "is empty":
                                case "is not empty":
                                    inputRow.querySelector("span.scope-value-select").style.display = "none";
                                    inputRow.querySelector("div.free-input").style.display = "none";
                                    break;
                                default:
                                    inputRow.querySelector("span.scope-value-select").style.display = "none";
                                    inputRow.querySelector("div.free-input").style.display = "";
                                    freeInput.value = scopeRow.value;
                                    break;
                            }

                            this.setScopeFieldValue(propertySelect, scopeRow.key);
                            this.dataSelector.updateScopePropertyValueDataSource($(inputRow));

                            comparisonSelect.value(scopeRow.operator);
                            comparisonSelect.trigger("change");
                        });
                    });
                } else {
                    scopes.forEach((having) => {
                        if (!having.hasOwnProperty("havingrows")) {
                            return;
                        }

                        let section;
                        let firstPass = true;
                        having.havingrows.forEach((havingRow) => {
                            let inputRow;
                            if (firstPass) {
                                section = this.dataSelector.addHaving();
                                inputRow = section.querySelector(".inputRow");
                                firstPass = false;
                            } else {
                                inputRow = this.dataSelector.addOrHaving($(section));
                            }

                            const propertySelectElement = inputRow.querySelector("select.scope-property-select");
                            const propertySelect = $(propertySelectElement).getKendoDropDownList();
                            const comparisonSelect = $(inputRow.querySelector("select.scope-comparison-select")).getKendoDropDownList();
                            const valueSelect = $(inputRow.querySelector("select.scope-value-select")).getKendoMultiSelect();
                            const freeInput = inputRow.querySelector("div.free-input > input[type='text']");

                            createdScopes.push({
                                inputRow: inputRow,
                                widget: propertySelect,
                                value: havingRow.key
                            });

                            // Have to bind the value select first, so it will trigger once the property select gets updated.
                            switch (havingRow.operator) {
                                case "is equal to":
                                case "is not equal to":
                                    inputRow.querySelector("span.scope-value-select").style.display = "";
                                    inputRow.querySelector("div.free-input").style.display = "none";
                                    valueSelect.one("dataBound", (e) => {
                                        // Scopes might also have custom values that are not part of the data source.
                                        if (Wiser.validateArray(havingRow.value, true)) {
                                            havingRow.value.forEach((item) => {
                                                this.dataSelector.addOrSelectItem(e.sender, item);
                                            });
                                        }
                                    });
                                    break;
                                case "is empty":
                                case "is not empty":
                                    inputRow.querySelector("span.scope-value-select").style.display = "none";
                                    inputRow.querySelector("div.free-input").style.display = "none";
                                    break;
                                default:
                                    inputRow.querySelector("span.scope-value-select").style.display = "none";
                                    inputRow.querySelector("div.free-input").style.display = "";
                                    freeInput.value = havingRow.value;
                                    break;
                            }

                            this.setScopeFieldValue(propertySelect, havingRow.key, true);
                            this.dataSelector.updateScopePropertyValueDataSource($(inputRow));

                            comparisonSelect.value(havingRow.operator);
                            comparisonSelect.trigger("change");
                        });
                    });
                }

                resolve({
                    createdScopes: createdScopes
                });
            });
        }

        createConnections(connections, context = null) {
            return new Promise((resolve) => {
                if (!connections) {
                    return;
                }

                let connectionBlock = context;
                if (connectionBlock === null) {
                    const connectionBlocksContainer = document.getElementById("connectionBlocks");
                    connectionBlock = connectionBlocksContainer.querySelector(".connectionBlock");
                }

                if (!connectionBlock) {
                    return;
                }

                const connectionObject = $(connectionBlock).data("connection");

                // Check how many connections (not counting sub-connections) need to be created.
                const connectionsTodo = connections.length;
                let connectionsDone = 0;
                const onConnectionDone = () => {
                    connectionsDone++;
                    if (connectionsDone >= connectionsTodo) {
                        resolve();
                    }
                };

                connections.forEach((connection) => {
                    if (!Wiser.validateArray(connection.connectionrows)) {
                        // Skip row.
                        onConnectionDone();
                        return;
                    }

                    let section;
                    let firstPass = true;

                    // Check how many rows for this connection need to be created.
                    const connectionRowsTodo = connection.connectionrows.length;
                    let connectionRowsDone = 0;
                    const onConnectionRowDone = () => {
                        connectionRowsDone++;
                        if (connectionRowsDone >= connectionRowsTodo) {
                            onConnectionDone();
                        }
                    };

                    connection.connectionrows.forEach((connectionRow) => {
                        let direction = "down";
                        if (Wiser.validateArray(connectionRow.modes, true) && connectionRow.modes.includes("up")) {
                            direction = "up";
                        }

                        let inputRow;
                        if (firstPass) {
                            section = connectionObject.addLinkedTo(direction);
                            inputRow = section.querySelector(".inputRow");
                            firstPass = false;
                        } else {
                            inputRow = connectionObject.addOrLinkedTo(section, direction);
                        }

                        if (Wiser.validateArray(connectionRow.modes, true) && connectionRow.modes.indexOf("optional") !== -1) {
                            inputRow.querySelector(".optional-checkbox input[type='checkbox']").checked = true;
                        }

                        if (!connectionRow.hasOwnProperty("entity")) {
                            onConnectionRowDone();
                            return;
                        }

                        const propertySelect = $(inputRow.querySelector("select.linked-to-property-select")).getKendoDropDownList();
                        const treeView = $(inputRow.querySelector("div.checkTree")).getKendoTreeView();

                        $(connectionObject.container).data("loading", true);

                        propertySelect.one("dataBound", (e) => {
                            e.sender.one("cascade", () => {
                                const fieldsSelectElement = inputRow.querySelector("select.select-details");
                                const fieldsSelect = $(fieldsSelectElement).getKendoMultiSelect();

                                // When an entity is selected, a new connection block is made.
                                const newConnectionBlock = inputRow.querySelector(".connectionBlock");
                                const newConnectionObject = $(newConnectionBlock).data("connection");

                                newConnectionObject.updateAvailableProperties($(newConnectionObject.container)).then(() => {
                                    if (Wiser.validateArray(connectionRow.scope)) {
                                        this.createScopes(connectionRow.scope, {
                                            context: inputRow.querySelector("div.connectionBlock")
                                        });
                                    }

                                    if (Wiser.validateArray(connectionRow.fields)) {
                                        newConnectionObject.setAvailablePropertiesDataSources([fieldsSelectElement]);
                                        this.setFieldsSelectValue(fieldsSelect, connectionRow.fields);
                                        fieldsSelect.trigger("change");
                                    }
                                    if (Wiser.validateArray(connectionRow.linkfields)) {
                                        const widget = $(inputRow.querySelector("select.select-details-links")).getKendoMultiSelect();
                                        const dataSource = widget.dataSource;
                                        connectionRow.linkfields.forEach((field) => {
                                            dataSource.add({
                                                text: field,
                                                value: field
                                            });
                                        });
                                        widget.value(connectionRow.linkfields);
                                    }

                                    $(connectionObject.container).data("loading", false);

                                    if (Wiser.validateArray(connectionRow.connections)) {
                                        this.createConnections(connectionRow.connections, inputRow.querySelector("div.connectionBlock")).then(() => {
                                            onConnectionRowDone();
                                        });
                                    } else {
                                        onConnectionRowDone();
                                    }
                                });
                            });

                            // The value of the items of the connection dropdown are JSON strings.
                            // So to find the item that needs to be selected, each item has to be looped through individually.
                            const items = Array.from(e.sender.items()).map(el => $(el));
                            let entityFound = false;

                            items.forEach((item, index) => {
                                const json = e.sender.dataItem(item);

                                let matchFound;
                                if (connectionRow.hasOwnProperty("typeName")) {
                                    matchFound = json.typeNumber === connectionRow.typenr && json.linkTypeName === connectionRow.typeName;
                                } else {
                                    matchFound = json.type === connectionRow.entity && json.typeNumber === connectionRow.typenr;
                                }

                                if (matchFound) {
                                    // It needs to be the index + 1, because there's also a dummy row that takes up the first index.
                                    e.sender.select(index + 1);
                                    entityFound = true;
                                }

                                if (Wiser.validateArray(connectionRow.itemids) && json.inputType === "item-linker") {
                                    treeView.one("dataBound", (tve) => {
                                        tve.sender.setCheckedItems(connectionRow.itemids, "id");
                                    });
                                }
                            });

                            // Consider the connection to be done if the connection couldn't be found.
                            // Also raise a warning the data selector couldn't finish loading.
                            if (!entityFound) {
                                onConnectionRowDone();

                                // Figure out the what the parent entity type is.
                                let parentEntityType;
                                if ($(connectionBlock).data("linkedToPropertySelect")) {
                                    parentEntityType = $(connectionBlock).data("linkedToPropertySelect").dataItem().entityType;
                                } else {
                                    parentEntityType = this.dataSelector.selectedEntityType;
                                }

                                Wiser.alert({
                                    title: "Data Selector koppeling fout",
                                    content: `Het laden van een koppeling is mislukt omdat de entiteit '<strong>${connectionRow.entity}</strong>' niet gevonden kon worden, of deze is niet meer gekoppeld aan de entiteit '<strong>${parentEntityType}</strong>'.`
                                });
                            }
                        });
                    });
                });
            });
        }

        setFieldsSelectValue(widget, fields) {
            const newValue = [];
            fields.forEach((field) => {
                let fieldValue;
                if (typeof field === "string") {
                    // Old method.
                    newValue.push(field);
                } else {
                    fieldValue = this.dataSelector.useExportMode ? `${field.fieldname}_${field.languagecode}` : field.fieldname;
                    newValue.push(fieldValue);
                    let existingItem = widget.dataSource.view().find((dataItem) => {
                        return dataItem.fieldAlias === fieldValue;
                    });

                    if (!existingItem) {
                        existingItem = widget.dataSource.view().find((dataItem) => {
                            return dataItem.value === fieldValue;
                        });
                    }

                    if (existingItem !== undefined && existingItem !== null) {
                        if (field.hasOwnProperty("fieldalias") && field.fieldalias !== "") {
                            existingItem.set("fieldAlias", field.fieldalias);
                            existingItem.set("aliasOrValue", field.fieldalias);
                        } else {
                            existingItem.set("aliasOrValue", fieldValue);
                        }

                        existingItem.set("dataType", field.dataType || "string");
                        existingItem.set("havingDataType", field.havingDataType || "string");

                        // Support for older "languagecode" property, which is a single value.
                        const languagesCodes = [];
                        if (field.hasOwnProperty("languagecode") && field.languagecode !== "") {
                            languagesCodes.push(field.languagecode);
                        }
                        // Add the language codes in the "languageCodes" property, which should be an array.
                        if (field.hasOwnProperty("languageCodes") && Array.isArray(field.languageCodes)) {
                            languagesCodes.push(...field.languageCodes);
                        }
                        existingItem.set("languageCode", languagesCodes);

                        existingItem.set("aggregation", field.aggregationfunction);
                        existingItem.set("formatting", field.formatting);

                        if (field.subSelection) {
                            existingItem.set("subSelection", field.subSelection);
                        }
                    }
                }
            });
            widget.value(newValue);
        }

        setScopeFieldValue(widget, field, forHaving = false) {
            if (typeof field === "string") {
                // Old method (field is a string).
                widget.value(field);
                return;
            }

            // New method (field is an object).
            let existingItem;

            if (field.hasOwnProperty("entityName")) {
                const fieldName = this.dataSelector.useExportMode ? `${field.fieldName}_${field.languageCode}` : field.fieldName;
                if (typeof field.fieldAlias === "string" && field.fieldAlias !== "") {
                    existingItem = widget.dataItems().find(dataItem => dataItem.entityName === field.entityName && dataItem.value === fieldName && dataItem.fieldAlias === field.fieldAlias);
                } else {
                    existingItem = widget.dataItems().find(dataItem => dataItem.entityName === field.entityName && dataItem.value === fieldName);
                }
            } else {
                const fieldName = this.dataSelector.useExportMode ? `${field.fieldname}_${field.languagecode}` : field.fieldname;
                existingItem = widget.dataItems().find(dataItem => dataItem.fieldAlias === fieldName);
                if (!existingItem) {
                    // Check if match can be made based on field alias (needed for having rows).
                    existingItem = widget.dataItems().find((dataItem) => {
                        return dataItem.value === fieldName;
                    });
                }
            }

            if (!existingItem) {
                return;
            }

            if (field.hasOwnProperty("entityName")) {
                // New format.
                if (field.hasOwnProperty("fieldAlias") && field.fieldAlias !== "") {
                    existingItem.set("fieldAlias", field.fieldAlias);
                    existingItem.set("aliasOrValue", field.fieldAlias);
                } else {
                    existingItem.set("aliasOrValue", field.fieldName);
                }

                existingItem.set("dataType", field.dataType || "string");
                existingItem.set("havingDataType", field.havingDataType || "string");

                if (field.hasOwnProperty("languageCode") && field.languageCode !== "") {
                    existingItem.set("languageCode", field.languageCode);
                }

                if (forHaving) {
                    existingItem.set("havingAggregation", field.aggregation);
                    existingItem.set("havingFormatting", field.formatting);
                } else {
                    existingItem.set("aggregation", field.aggregation);
                    existingItem.set("formatting", field.formatting);
                }
            } else {
                // Old format.
                if (field.hasOwnProperty("fieldalias") && field.fieldalias !== "") {
                    existingItem.set("fieldAlias", field.fieldalias);
                    existingItem.set("aliasOrValue", field.fieldalias);
                } else {
                    existingItem.set("aliasOrValue", field.fieldname);
                }

                existingItem.set("dataType", field.dataType || "string");
                existingItem.set("havingDataType", field.havingDataType || "string");

                if (field.hasOwnProperty("languagecode") && field.languagecode !== "") {
                    existingItem.set("languageCode", field.languagecode);
                }

                if (forHaving) {
                    existingItem.set("havingAggregation", field.aggregationfunction);
                    existingItem.set("havingFormatting", field.formatting);
                } else {
                    existingItem.set("aggregation", field.aggregationfunction);
                    existingItem.set("formatting", field.formatting);
                }
            }

            widget.select(dataItem => dataItem === existingItem);
        }
    }

    window.DataLoad = DataLoad;
})();
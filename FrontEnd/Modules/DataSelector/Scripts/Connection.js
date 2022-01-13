((jQuery) => {
    class Connection {
        constructor(dataSelector, parentContainer, isMainConnection = false) {
            this.dataSelector = dataSelector;
            this.parentContainer = parentContainer;
            this.isMainConnection = isMainConnection;

            this.container = null;
            this.scopesContainer = null;
            this.linkedToDownContainer = null;
            this.linkedToUpContainer = null;

            this.availableProperties = [];
            this.availableLinkProperties = [];
        }

        initialize() {
            this.createHtml();
            this.setInitialBindings();

            // After creation, set the Connection object as a data element in the block.
            $(this.container).data("connection", this);
        }

        createHtml() {
            const block = $(document.getElementById("connectionTemplate").innerHTML);
            $(this.parentContainer).append(block);

            if (this.isMainConnection) {
                const linkFieldsItem = Array.from(block.get(0).querySelector(".blockItems").children).find(e => e.classList.contains("exportFieldsLinks"));
                if (linkFieldsItem instanceof HTMLElement) {
                    linkFieldsItem.parentNode.removeChild(linkFieldsItem);
                }
            }

            this.scopesContainer = block.find(".scopesContainer");
            this.linkedToDownContainer = block.find(".linkedToList.direction-down > .linkedToContainer");
            this.linkedToUpContainer = block.find(".linkedToList.direction-up > .linkedToContainer");

            this.dataSelector.initializeKendoElements(block.get(0));

            // Container should be a DOM element, so get the first element from the jQuery object.
            this.container = block.get(0);
        }

        setInitialBindings() {
            const containerElement = $(this.container).find(".blockItems");
            containerElement.find(".add-scope-button").getKendoButton().bind("click", () => {
                this.addScope();
            });
            containerElement.find(".linkedToList.direction-down .add-linked-to-button").getKendoButton().bind("click", () => {
                this.addLinkedTo("down");
            });
            containerElement.find(".linkedToList.direction-up .add-linked-to-button").getKendoButton().bind("click", () => {
                this.addLinkedTo("up");
            });

            const selectDetails = containerElement.find("select.select-details");
            const selectDetailsWidget = selectDetails.getKendoMultiSelect();

            selectDetailsWidget.bind("change", () => {
                document.dispatchEvent(new CustomEvent("entitySelectionUpdate"));
            });

            selectDetailsWidget.wrapper.on("click", "li.k-button", (e) => {
                const clickedElement = $(e.target);
                if (clickedElement.has(".k-i-close").length > 0 || clickedElement.closest(".k-i-close").length > 0) {
                    return;
                }

                // This will select the tag itself (a li element).
                const tagElement = $(e.currentTarget);

                const index = tagElement.index();
                const selectedDataItem = selectDetailsWidget.dataItems()[index];
                const dataItem = selectDetailsWidget.dataSource.view().find((di) => {
                    return di.value === selectedDataItem.value;
                });
                
                this.dataSelector.openFieldEditor(dataItem, {
                    includeLanguageCodeField: !this.dataSelector.useExportMode
                });
            });
            
            this.dataSelector.giveCustomClickLogic(selectDetailsWidget);

            containerElement.find(".item.scopesList").on("click", "button.edit-field-button", (e) => {
                const button = $(e.currentTarget);
                const propertyDropdown = button.closest(".inputRow").find("select.scope-property-select").getKendoDropDownList();
                this.dataSelector.openFieldEditor(propertyDropdown.dataItem(), {
                    includeLanguageCodeField: !this.dataSelector.useExportMode,
                    includeFieldAliasField: false,
                    includeIsItemIdField: false
                });
            });
        }

        setDynamicBindings(context) {
            let containerElement;
            if (context !== undefined && context !== null) {
                containerElement = $(context);
            } else {
                containerElement = $(this.container).find(".blockItems");
            }

            // BUTTONS.
            containerElement.find(".or-button-scope").on("click", (e) => {
                const newScope = this.addOrScope($(e.currentTarget).closest("section"));
                this.setAvailablePropertiesDataSources([newScope.querySelector("select.scope-property-select")]);
            });

            containerElement.find(".or-button-linked-to").on("click", (e) => {
                let direction;

                const linkedToList = $(e.currentTarget).closest(".linkedToList");
                if (linkedToList.hasClass("direction-down")) {
                    direction = "down";
                } else if (linkedToList.hasClass("direction-up")) {
                    direction = "up";
                } else {
                    return;
                }

                this.addOrLinkedTo($(e.currentTarget).closest("section"), direction);
            });

            containerElement.find(".delete-button").getKendoButton().bind("click", (e) => {
                const cont = e.sender.element.closest("section");

                if (cont.find(".inputRow").length <= 1) {
                    cont.remove();
                } else {
                    e.sender.element.closest(".inputRow").remove();
                }
            });

            containerElement.find("select.scope-property-select").each((i, elem) => {
                const inputRow = $(elem).closest(".inputRow");
                $(elem).getKendoDropDownList().bind("change", () => {
                    this.dataSelector.updateScopePropertyValueDataSource(inputRow);
                });
            });

            containerElement.find("select.scope-comparison-select").each((i, elem) => {
                const dbInput = $(elem).closest(".inputRow");
                $(elem).getKendoDropDownList().bind("change", (e) => {
                    const value = e.sender.value();

                    switch (value) {
                        case "is equal to":
                        case "is not equal to":
                            dbInput.find("div.scope-value-select").show();
                            dbInput.find("div.free-input").hide();
                            break;
                        case "is empty":
                        case "is not empty":
                            dbInput.find("div.scope-value-select").hide();
                            dbInput.find("div.free-input").hide();
                            break;
                        default:
                            dbInput.find("div.scope-value-select").hide();
                            dbInput.find("div.free-input").show();
                            break;
                    }
                });
            });
        }

        /**
         * Updates entity properties list that are available for selection in the scopes and fields export list.
         * @param {any} connectionBlock jQuery object of the relevant element that will have its properties updated. This is optional because the main connection doesn't need it.
         */
        async updateAvailableProperties(connectionBlock = null) {
            if (!this.isMainConnection && !connectionBlock) {
                return;
            }

            let entityType;
            if (this.isMainConnection) {
                entityType = this.dataSelector.selectedEntityType;
            } else {
                const linkedToPropertySelect = connectionBlock.data("linkedToPropertySelect");
                if (!linkedToPropertySelect) {
                    return;
                }

                entityType = linkedToPropertySelect.dataItem().entityType;
            }
            
            const response = await $.get(`${this.dataSelector.settings.serviceRoot}/GET_ENTITY_PROPERTIES?entityName=${entityType}&useExportMode=${this.dataSelector.useExportMode ? "1" : "0"}`);

            // Create clone of "response" so it doesn't use the reference value, but a completely new object.
            // Although it's also possible to use "[...response]", this JSON trick works better as it also clones deep properties.
            this.availableProperties = JSON.parse(JSON.stringify(response));

            // Create a "unique value" for every property, based on the normal value.
            // A few inputs use this, like the group by input, order by input, and having inputs.
            this.availableProperties.forEach((property) => {
                property.aliasOrValue = property.value;
            });
        }

        async updateAvailableLinkProperties(connectionBlock = null) {
            if (!this.isMainConnection && !connectionBlock) {
                return;
            }

            console.log("connectionBlock:", connectionBlock);

            let linkType;
            if (this.isMainConnection) {
                linkType = this.dataSelector.selectedLinkType;
            } else {
                const linkedToPropertySelect = connectionBlock.data("linkedToPropertySelect");
                if (!linkedToPropertySelect) {
                    return;
                }

                linkType = linkedToPropertySelect.dataItem().typeNumber;
            }

            const response = await Wiser2.api({ url: `${this.dataSelector.settings.serviceRoot}/GET_ENTITY_LINK_PROPERTIES?linkType=${linkType}` });

            // Create clone of "response" so it doesn't use the reference value, but a completely new object.
            // Although it's also possible to use "[...response]", this JSON trick works better as it also clones deep properties.
            this.availableLinkProperties = JSON.parse(JSON.stringify(response));

            // Create a "unique value" for every property, based on the normal value.
            // A few inputs use this, like the group by input, order by input, and having inputs.
            this.availableLinkProperties.forEach((property) => {
                property.aliasOrValue = property.value;
            });
        }

        setAvailablePropertiesDataSources(elementsToUpdate) {
            let elements;

            if (elementsToUpdate instanceof NodeList) {
                elements = Array.from(elementsToUpdate);
            } else if (Wiser2.validateArray(elementsToUpdate)) {
                elements = elementsToUpdate;
            } else if (elementsToUpdate instanceof jQuery && elementsToUpdate.length > 0) {
                elements = elementsToUpdate.toArray();
            } else {
                return;
            }

            elements.forEach((element) => {
                let widget = null;

                switch (element.dataset.role) {
                    case "dropdownlist":
                        widget = $(element).getKendoDropDownList();
                        break;
                    case "multiselect":
                        widget = $(element).getKendoMultiSelect();
                        break;
                }

                if (!(widget instanceof kendo.ui.Widget)) {
                    return;
                }

                this.dataSelector.updateWidgetDataSource(widget, this.availableProperties);
            });
        }

        setAvailableLinkPropertiesDataSources(elementsToUpdate) {
            let elements;

            if (elementsToUpdate instanceof NodeList) {
                elements = Array.from(elementsToUpdate);
            } else if (Wiser2.validateArray(elementsToUpdate)) {
                elements = elementsToUpdate;
            } else if (elementsToUpdate instanceof jQuery && elementsToUpdate.length > 0) {
                elements = elementsToUpdate.toArray();
            } else {
                return;
            }

            elements.forEach((element) => {
                let widget = null;

                switch (element.dataset.role) {
                    case "dropdownlist":
                        widget = $(element).getKendoDropDownList();
                        break;
                    case "multiselect":
                        widget = $(element).getKendoMultiSelect();
                        break;
                }

                if (!(widget instanceof kendo.ui.Widget)) {
                    return;
                }

                this.dataSelector.updateWidgetDataSource(widget, this.availableLinkProperties);
            });
        }

        addOrScope(section) {
            const elem = $(document.getElementById("addScopeTemplate").innerHTML);
            section.append(elem.get(0));
            this.dataSelector.initializeKendoElements(elem.get(0));
            this.setDynamicBindings(elem);

            this.setAvailablePropertiesDataSources([elem.find("select.scope-property-select").get(0)]);

            return elem.get(0);
        }

        addScope() {
            const newSection = $(`<section>${document.getElementById("addScopeTemplate").innerHTML}</section>`);
            this.scopesContainer.append(newSection);

            this.dataSelector.initializeKendoElements(newSection.get(0));
            this.setDynamicBindings(newSection);

            this.setAvailablePropertiesDataSources([newSection.find("select.scope-property-select").get(0)]);

            return newSection.get(0);
        }

        addOrLinkedTo(section, direction) {
            const elem = $(document.getElementById("addLinkedToTemplate").innerHTML);
            section.append(elem.get(0));
            this.dataSelector.initializeKendoElements(elem.get(0));
            this.setDynamicBindings(elem);
            this.setLinkedToEvents(elem, direction);

            return elem.get(0);
        }

        /**
         * Adds a "linked to" dropdown.
         * @param {string} direction Which direction this "linked to" dropdown should look. Can be either "down" or "up".
         * @returns {HTMLElement} The newly made section.
         */
        addLinkedTo(direction) {
            const newSection = $(`<section>${document.getElementById("addLinkedToTemplate").innerHTML}</section>`);

            switch (direction) {
                case "down":
                    this.linkedToDownContainer.append(newSection);
                    break;
                case "up":
                    this.linkedToUpContainer.append(newSection);
                    break;
            }

            this.dataSelector.initializeKendoElements(newSection.get(0));
            this.setDynamicBindings(newSection);
            this.setLinkedToEvents(newSection, direction);

            return newSection.get(0);
        }

        setLinkedToEvents(context, direction) {
            const selectElement = $("select.linked-to-property-select", context);
            const dropDown = selectElement.getKendoDropDownList();

            dropDown.setDataSource({
                transport: {
                    read: (options) => {
                        let selectedEntityType;
                        const connectionBlock = selectElement.closest("div.connectionBlock");
                        if (connectionBlock.data("linkedToPropertySelect")) {
                            const dataItem = connectionBlock.data("linkedToPropertySelect").dataItem();
                            selectedEntityType = dataItem.entityType;
                        } else {
                            selectedEntityType = this.dataSelector.selectedEntityType;
                        }

                        let templateName = "";
                        switch (direction) {
                            case "down":
                                templateName = "GET_UNDERLYING_LINKED_TYPES";
                                break;
                            case "up":
                                templateName = "GET_PARENT_LINKED_TYPES";
                                break;
                        }

                        if (templateName === "") {
                            options.error();
                            return;
                        }

                        Wiser2.api({
                            url: `${this.dataSelector.settings.serviceRoot}/${templateName}?entityName=${selectedEntityType}`,
                            dataType: "json"
                        }).then((result) => {
                            const dataSource = [];
                            const handledEntityTypes = [];

                            result.forEach((entity) => {
                                const typeNumber = entity.typeNumber;
                                const linkName = entity.linkTypeName;
                                const uniqueKey = `${typeNumber}_${linkName}`;
                                if (linkName === "" || handledEntityTypes.includes(uniqueKey)) {
                                    return;
                                }

                                dataSource.push({
                                    inputType: "sub-entities-grid",
                                    name: linkName,
                                    type: entity.entityType,
                                    entityType: entity.entityType,
                                    typeNumber: entity.linkTypeNumber,
                                    linkKey: uniqueKey
                                });

                                handledEntityTypes.push(uniqueKey);
                            });
                            options.success(dataSource);
                        }).catch((result) => {
                            options.error(result);
                        });
                    }
                }
            });

            dropDown.bind("cascade", (e) => {
                const options = e.sender.dataItem();
                const inputRow = e.sender.element.closest(".inputRow");
                const treeViewDiv = inputRow.find(".checkTree").eq(0);
                const treeView = treeViewDiv.getKendoTreeView();

                let connectionBlock = inputRow.get(0).querySelector("div.connectionBlock");

                if (e.sender.value === "") {
                    // Hide tree view (there's nothing to show).
                    treeView.setDataSource({ data: null });
                    treeViewDiv.hide();

                    // First destroy all Kendo widgets for safe removal.
                    this.dataSelector.destroyChildKendoWidgets(connectionBlock);
                    $(connectionBlock).remove();
                } else {
                    if (connectionBlock === undefined || connectionBlock === null) {
                        connectionBlock = this.dataSelector.addConnection(inputRow.get(0));
                        $(connectionBlock).data("linkedToPropertySelect", e.sender);
                    }

                    if (options.inputType === "sub-entities-grid") {
                        // Hide tree view.
                        treeView.setDataSource({ data: null });
                        treeViewDiv.hide();
                    } else {
                        treeView.setDataSource({
                            transport: {
                                read: {
                                    url: `${this.dataSelector.settings.serviceRoot}/GET_LINKED_TO_ITEMS?module=${options.moduleId}`,
                                    dataType: "json"
                                }
                            },
                            schema: {
                                model: {
                                    id: "id",
                                    hasChildren: "haschilds"
                                }
                            }
                        });
                        treeViewDiv.show();
                    }
                }

                // Update scopes and item select.
                if (connectionBlock !== undefined && connectionBlock !== null && !$(connectionBlock).data("loading")) {
                    const connection = $(connectionBlock).data("connection");
                    connection.updateAvailableProperties($(connectionBlock)).then(() => {
                        // Update the data sources of the scopes and field selection.
                        const elements = Array.from(connectionBlock.querySelector(".scopesContainer").querySelectorAll("select.scope-property-select"));
                        elements.push(connectionBlock.querySelector("select.select-details"));
                        connection.setAvailablePropertiesDataSources(elements);
                    });
                    connection.updateAvailableLinkProperties($(connectionBlock)).then(() => {
                        // Update the data sources of the scopes.
                        const elements = [connectionBlock.querySelector("select.select-details-links")];
                        connection.setAvailableLinkPropertiesDataSources(elements);
                    });
                }
            });
        }
    }

    window.Connection = Connection;
})($);
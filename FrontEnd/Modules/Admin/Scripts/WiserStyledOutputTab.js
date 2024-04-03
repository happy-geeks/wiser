import { QueryModel } from "../Scripts/StyledOutputModel.js";
import {Wiser} from "../../Base/Scripts/Utils";

export class WiserStyledOutputTab {
    constructor(base) {
        this.base = base;
        this.setupBindings();
        this.initializeKendoComponents();
        this.mainTreeView = null;
    }

    async initializeKendoComponents() {
		
		this.mainTreeView = $("#treeview").kendoTreeView({
                dragAndDrop: true,
            
                dataSource: {
                    transport: {
                        read: (options) => {
                            Wiser.api({
                                /* url: `${this.base.settings.wiserApiRoot}items/tree-view?moduleId=${this.base.settings.moduleId}`,*/
                                url: `${this.base.settings.wiserApiRoot}items/tree-view?moduleId=700`,
                                dataType: "json",
                                method: "GET",
                                data: options.data
                            }).then((result) => {
                                options.success(result);
                            }).catch((result) => {
                                options.error(result);
                            });
                        }
                    },
                    schema: {
                        model: {
                            id: "templateId",
                            hasChildren: "hasChildren"
                        } 
                    }
                },
            
                dataBound: this.onTreeViewDataBound.bind(this),
                select: this.onTreeViewItemClick.bind(this),
                collapse: this.onTreeViewCollapseItem.bind(this),
                expand: this.onTreeViewExpandItem.bind(this),
                drop: this.onTreeViewDropItem.bind(this),
                drag: this.onTreeViewDragItem.bind(this),
             
                dataValueField: "id",
                dataTextField: "title",
                dataSpriteCssClassField: "spriteCssClass"
                
             
            }).data("kendoTreeView");
/*
            this.mainTreeViewContextMenu = $("#menu").kendoContextMenu({
                target: "#treeview",
                filter: ".k-in",
                open: this.onContextMenuOpen.bind(this),
                select: this.onContextMenuClick.bind(this)
            }).data("kendoContextMenu");
            
            
 */
		/*
        this.queryCombobox = $("#queryList").kendoDropDownList({
            placeholder: "Selecteer een query...",
            clearButton: false,
            height: 400,
            dataTextField: "description",
            dataValueField: "id",
            filter: "contains",
            optionLabel: {
                id: "",
                description: "Maak uw keuze..."
            },
            minLength: 1,
            dataSource: {},
            cascade: this.onQueryComboBoxSelect.bind(this)
        }).data("kendoDropDownList");

        this.queryCombobox.one("dataBound", () => { this.queryListInitialized = true; });

        this.rolesWithPermissions = $("#rolesWithPermissions").kendoMultiSelect({
            dataSource: {
                transport: {
                    read: {
                        url: `${this.base.settings.serviceRoot}/GET_ROLES`
                    }
                }
            },
            dataTextField: "roleName",
            dataValueField: "id",
            multiple: "multiple"
        }).data("kendoMultiSelect");

        await Misc.ensureCodeMirror();

        this.queryFromWiser = CodeMirror.fromTextArea(document.getElementById("queryFromWiser"), {
            mode: "text/x-mysql",
            lineNumbers: true
        });

        // set query dropdown list
        this.getQueries();
		*/
    }

    async setupBindings() {
		/*
        $(".addQueryBtn").kendoButton({
            click: (e) => {
                this.base.openDialog("Nieuwe query toevoegen", "Voer de beschrijving in van query").then((data) => {
                    this.addQuery(data);
                });
            },
            icon: "file"
        });

        $(".delQueryBtn").kendoButton({
            click: () => {
                if (!this.checkIfQueryIsSet()) {
                    return;
                }
                const dataItemId = this.queryCombobox.dataItem().id;
                if (!dataItemId) {
                    this.base.showNotification("notification",
                        "Item is niet succesvol verwijderd, probeer het opnieuw",
                        "error");
                    return;
                }

                // ask for user confirmation before deleting
                this.base.openDialog("Query verwijderen", "Weet u zeker dat u deze query wilt verwijderen?", this.base.kendoPromptType.CONFIRM).then(() => {
                    this.deleteQueryById(dataItemId);
                });
            },
            icon: "delete"
        });
		*/
    }

    // actions handled before save, such as checks
    async beforeSave() {
        /*if (this.checkIfQueryIsSet(true)) {
            const queryModel = new QueryModel(this.queryCombobox.dataItem().id, document.getElementById("queryDescription").value, this.queryFromWiser.getValue(), document.getElementById("showInExportModule").checked, false, this.rolesWithPermissions.value().join(), document.getElementById("showInCommunicationModule").checked);
            await this.updateQuery(queryModel.id, queryModel);
        }
		*/
    }

    async onQueryComboBoxSelect(event) {
		/*
        if (this.checkIfQueryIsSet((event.userTriggered === true))) {
            this.getQueryById(this.queryCombobox.dataItem().id);
        }
		*/
    }

    async getQueries(reloadDataSource = true, queryIdToSelect = null) {
		/*
        if (reloadDataSource) {
            this.queryList = await Wiser.api({
                url: `${this.base.settings.wiserApiRoot}queries`,
                method: "GET"
            });

            if (!queryList) {
                this.base.showNotification("notification", "Het ophalen van de query's is mislukt, probeer het opnieuw", "error");
            }
        }

        this.queryCombobox.value("");
        this.queryCombobox.setDataSource(this.queryList);

        if (queryIdToSelect !== null) {
            if (queryIdToSelect === 0) {
                this.queryCombobox.select(0);
            } else {
                this.queryCombobox.select((dataItem) => {
                    return dataItem.id === queryIdToSelect;
                });
            }
        }
		*/
    }

    async updateQuery(id, queryModel) {
		/*
        try {
            await Wiser.api({
                url: `${this.base.settings.wiserApiRoot}queries/${id}`,
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                data: JSON.stringify(queryModel),
                method: "PUT"
            });

            this.base.showNotification("notification", `Query is succesvol bijgewerkt`, "success");
            await this.getQueries(true, id);
        }
        catch (exception) {
            console.error("Error while updating query", exception);
            this.base.showNotification("notification", `Het bijwerken van de queries is mislukt, probeer het opnieuw`, "error");
        }
		*/
    }

    async getQueryById(id) {
		/*
        const results = await Wiser.api({
            url: `${this.base.settings.wiserApiRoot}queries/${id}`,
            method: "GET"
        });

        await this.setQueryPropertiesToDefault();
        await this.setQueryProperties(results);
		*/
    }

    async addQuery(description) {
		/*
        if (!description) {
            return;
        }

        try {
            const result = await Wiser.api({
                url: `${this.base.settings.wiserApiRoot}queries`,
                contentType: "application/json",
                dataType: "json",
                data: JSON.stringify(description),
                method: "POST"
            });

            this.base.showNotification("notification", `Query succesvol toegevoegd`, "success");
            await this.getQueries(true, result.id);
        }
        catch (exception) {
            console.error("Error while creating query", exception);
            this.base.showNotification("notification", `Query is niet succesvol toegevoegd, probeer het opnieuw`, "error");
        }
		*/
    }

    async deleteQueryById(id) {
		/*
        try {
            const result = await Wiser.api({
                url: `${this.base.settings.wiserApiRoot}queries/${id}`,
                method: "DELETE"
            });

            await this.getQueries(true, 0);
            await this.setQueryPropertiesToDefault();
            this.base.showNotification("notification", `Query succesvol verwijderd`, "success");
        }
        catch (exception) {
            console.error("Error while deleting query", exception);
            this.base.showNotification("notification", `Query is niet succesvol verwijderd, probeer het opnieuw`, "error");
        }
		*/
    }

    async setQueryProperties(resultSet) {
		/*
        document.getElementById("queryIdLbl").innerHTML = `id: ${resultSet.id}`;
        document.getElementById("queryDescription").value = resultSet.description;
        document.getElementById("showInExportModule").checked = resultSet.showInExportModule;
        document.getElementById("showInCommunicationModule").checked = resultSet.showInCommunicationModule;
        this.rolesWithPermissions.value(resultSet.rolesWithPermissions.split(","));
        await this.setCodeMirrorFields(this.queryFromWiser, resultSet.query);
		*/
    }

    async setQueryPropertiesToDefault() {
		/*
        document.getElementById("queryDescription").value = "";
        document.getElementById("showInExportModule").checked = false;
        document.getElementById("showInCommunicationModule").checked = false;
        this.queryFromWiser.setValue("")
        this.rolesWithPermissions.value([]);
		*/
    }

    async setCodeMirrorFields(field, value) {
		/*
        if (value && value !== "null" && field != null) {
            field.setValue(value);
            field.refresh();
        }
		*/
    }

    checkIfQueryIsSet(showNotification = true) {
		/*
        if (this.queryCombobox &&
            this.queryCombobox.dataItem() &&
            this.queryCombobox.dataItem().id !== "" &&
            this.queryListInitialized === true) {
            return true;
        } else {
            if (showNotification)
                this.base.showNotification("notification", `Selecteer eerst een query!`, "error");

            return false;
        }
		*/
    }

    hasChanges() {
		/*
        return false;
		*/
    }


    /**
     * Handles the main tree view data bound event.
     * @param {any} event The Kendo data bound event.
     */
    onTreeViewDataBound(event) {
        (event.node || event.sender.element).find("li").each((index, element) => {
            const dataItem = event.sender.dataItem(element);
            if (!dataItem.nodeCssClass) {
                return;
            }

            $(element).addClass(dataItem.nodeCssClass);
        });

        this.base.toggleMainLoader(false);
    }

    /**
     * Handles the click on a node in the tree view (open the item).
     * @param {any} event The click event of the Kendo tree view.
     */
    async onTreeViewItemClick(event) {
        const treeView = event.sender;
        const dataItem = treeView.dataItem(event.node);
        if (!dataItem) {
            console.warn("onTreeViewItemClick triggered, but could not get data item of selected node", event);
            return;
        }

        let itemId = dataItem.id;
        this.base.selectedItem = dataItem;

        // If we have an original item id, it means this item has multiple version. Then we want to check what the latest version is and open that one.
        // This used to be done in the query that gets the items for the tree view, but that made the query really slow for some tenants, so now we do it here.
        if (dataItem.plainOriginalItemId > 0) {
            let itemToUse = null;
            const itemEnvironments = await this.getItemEnvironments(dataItem.encryptedOriginalItemId);
            if (itemEnvironments && itemEnvironments.length) {
                for (let itemVersion of itemEnvironments) {
                    if (itemVersion.changedOn) {
                        itemVersion.changedOn = new Date(itemVersion.changedOn);
                    }

                    if (!itemToUse || itemVersion.changedOn > itemToUse.changedOn) {
                        itemToUse = itemVersion;
                    }
                }
            }

            if (itemToUse) {
                itemId = itemToUse.id;
                // Change the ID of the selected item, otherwise the save button will overwrite the wrong item.
                this.base.selectedItem.id = itemId;
                this.base.selectedItem.plainItemId = itemToUse.plainItemId;
            }
        }

        // Set the correct values in the crumb trail.
        const crumbTrail = $("#crumbTrail").empty();
        const parents = $(event.node).add($(event.node).parentsUntil(".k-treeview", ".k-item"));
        const amountOfItems = parents.length;
        let counter = 0;
        const fullPath = [];

        const texts = $.map(parents, (node) => {
            counter++;

            const text = $(node).find(">div span.k-in").text();
            fullPath.push(text);
            const newCrumbTrailNode = $("<li/>");

            if (counter < amountOfItems) {
                const link = $("<a href='#' />").appendTo(newCrumbTrailNode);
                link.text(text);
                link.click((event) => {
                    event.preventDefault();
                    treeView.select(node);
                    treeView.trigger("select", {
                        node: node
                    });
                });
            } else {
                newCrumbTrailNode.text(text);
            }

            return newCrumbTrailNode;
        });

        crumbTrail.html(texts);

        this.base.mainTabStrip.tabGroup.children().each((index, element) => {
            if ($(element).text().trim().toLowerCase() !== "overzicht") {
                return;
            }

            $(element).toggle(dataItem.hasChildren);
        });

        await this.base.loadItem(itemId, 0, dataItem.entityType || dataItem.entityType);

        const pathString = `/${fullPath.join("/")}/`;
        // Show / hide fields based on path regex.
        $("#right-pane .item").each((index, element) => {
            const fieldContainer = $(element);
            const pathRegex = fieldContainer.data("visibilityPathRegex");
            if (!pathRegex) {
                return;
            }

            try {
                const regex = new RegExp(pathRegex);
                const showField = regex.test(pathString);
                fieldContainer.toggleClass("hidden", !showField);
                if (!showField) {
                    console.log(`Field '${fieldContainer.data("propertyName")}' has been hidden because of visibility_path_regex '${pathRegex}'`);
                }
            } catch(exception) {
                console.error(`Error occurred while trying to hide/show field '${fieldContainer.data("propertyName")}' based on regex '${pathRegex}'`, exception);
            }
        });

        // Get available entity types, for creating new sub items.
        await this.base.dialogs.loadAvailableEntityTypesInDropDown(itemId);
    }

    /**
     * Event for when an item in a kendoTreeView gets collapsed.
     * @param {any} event The collapsed event of a kendoTreeView.
     */
    onTreeViewCollapseItem(event) {
        const dataItem = event.sender.dataItem(event.node);
        dataItem.spriteCssClass = dataItem.collapsedSpriteCssClass;

        // Changing the text causes kendo to actually update the icon. If we don't change the test, the icon will not change.
        event.sender.text(event.node, event.sender.text(event.node).trim());
    }

    /**
     * Event for when an item in a kendoTreeView gets expanded.
     * @param {any} event The expanded event of a kendoTreeView.
     */
    onTreeViewExpandItem(event) {
        const dataItem = event.sender.dataItem(event.node);
        dataItem.spriteCssClass = dataItem.expandedSpriteCssClass || dataItem.collapsedSpriteCssClass;

        // Changing the text causes kendo to actually update the icon. If we don't change the test, the icon will not change.
        event.sender.text(event.node, event.sender.text(event.node) + " ");
    }

    /**
     * Event for when an item in a kendoTreeView gets dragged.
     * @param {any} event The drop item event of a kendoTreeView.
     */
    onTreeViewDragItem(event) {
        // Scroll left pane up/down when moving items.
        const leftPane = $("#left-pane");
        const topOfTreeView = leftPane.offset().top;
        const bottomOfTreeView = topOfTreeView + leftPane.height();
        const dropTargetOffset = $(event.dropTarget).offset().top;
        if (dropTargetOffset > 0 && dropTargetOffset > bottomOfTreeView - 50) {
            leftPane.scrollTop(leftPane.scrollTop() + $(event.dropTarget).height() + 50);
        } else if (dropTargetOffset > 0 && dropTargetOffset - 10 < topOfTreeView) {
            leftPane.scrollTop(leftPane.scrollTop() - $(event.dropTarget).height() - 50);
        }

        if (event.statusClass === "i-cancel") {
            // Tree view already denies this operation
            return;
        }

        const dropTarget = $(event.dropTarget);
        let destinationNode = dropTarget.closest("li.k-item");
        if (dropTarget.hasClass("k-mid")) {
            // If the dropTarget is an element with class k-mid we need to go higher, because those elements are located inside an li.k-item instead of after/before them.
            destinationNode = destinationNode.parentsUntil("li.k-item");
        }
        if (event.statusClass === "i-insert-down" || (event.statusClass === "i-insert-middle" && (dropTarget.hasClass("k-bot") || dropTarget.hasClass("k-in")))) {
            // If the statusClass is i-insert-down, it means we are adding the item below the destination, so we need to check it's parent.
            destinationNode = destinationNode.parentsUntil("li.k-item");
        }

        const sourceDataItem = event.sender.dataItem(event.sourceNode) || {};
        const destinationDataItem = event.sender.dataItem(destinationNode) || {};

        if ((destinationDataItem.acceptedChildTypes || "").toLowerCase().split(",").indexOf(sourceDataItem.entityType.toLowerCase()) === -1) {
            // Tell the kendo tree view to deny the drag to this item, if the current item is of a type that is not allowed to be linked to the destination.
            event.setStatusClass("k-i-cancel");
        }
    }

    /**
     * Event for when an item in a kendoTreeView gets dropped.
     * @param {any} event The drop item event of a kendoTreeView.
     */
    async onTreeViewDropItem(event) {
        if (!event.valid) {
            return;
        }

        try {
            const sourceDataItem = event.sender.dataItem(event.sourceNode);
            const destinationDataItem = event.sender.dataItem(event.destinationNode);

            await Wiser.api({
                url: `${this.base.settings.wiserApiRoot}items/${encodeURIComponent(sourceDataItem.id)}/move/${encodeURIComponent(destinationDataItem.id)}`,
                method: "PUT",
                contentType: "application/json",
                data: JSON.stringify({
                    position: event.dropPosition,
                    encryptedSourceParentId: sourceDataItem.destinationItemId,
                    encryptedDestinationParentId: destinationDataItem.destinationItemId,
                    sourceEntityType: sourceDataItem.entityType,
                    destinationEntityType: destinationDataItem.entityType,
                    moduleId: this.base.settings.moduleId
                })
            });

            sourceDataItem.destinationItemId = destinationDataItem.destinationItemId;
        } catch (exception) {
            console.error(exception);
            kendo.alert(`Er is iets fout gegaan met het verplaatsen van dit item. De fout was:<br>${exception.responseText || exception.statusText}`);
            event.setValid(false);
        }
    }
}
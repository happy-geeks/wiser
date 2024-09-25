import "../../Base/Scripts/Processing.js";

require("@progress/kendo-ui/js/kendo.dialog.js");
require("@progress/kendo-ui/js/kendo.dropdownlist.js");
require("@progress/kendo-ui/js/cultures/kendo.culture.nl-NL.js");
require("@progress/kendo-ui/js/messages/kendo.messages.nl-NL.js");

/**
 * Class for any and all functionality for dialogs (not windows).
 */
export class Dialogs {
    /**
     * Initializes a new instance of the Dialogs class.
     * @param {DynamicItems} base An instance of the base class (DynamicItems).
     */
    constructor(base) {
        this.base = base;

        this.newItemDialog = null;
        this.newItemDialogEntityTypeDropDown = null;
    }

    /**
     * Do all initializations for the Dialogs class, such as adding bindings.
     */
    async initialize() {
        // Dialog for creating new items via the tree view. This is different from the window as this only has a field for the item title and for the entity type (if there is more than one available).
        this.newItemDialog = $("#createNewItemDialog").kendoDialog({
            width: "400px",
            visible: false,
            title: "Nieuw item aanmaken",
            closable: true,
            modal: true,
            content: kendo.template($("#createNewItemDialogTemplate").html()),
            actions: [
                { text: "Annuleren" },
                { text: "Opslaan", primary: true, action: (event) => this.createItem() }
            ]
        }).data("kendoDialog");
        
        if (window.parent?.main?.branchesService !== undefined && (await window.parent.main.branchesService.isMainBranch()).data && this.newItemDialog.element.find("#alsoCreateInMainBranch").closest(".new-item-dialog-row").length > 0) {
            this.newItemDialog.element.find("#alsoCreateInMainBranch").closest(".new-item-dialog-row")[0].classList.add("hidden");
        }

        this.newItemDialog.element.find("#newItemNameField").keyup((event) => {
            if (!event.key || event.key.toLowerCase() !== "enter") {
                return;
            }

            this.createItem();
        });

        this.newItemDialogEntityTypeDropDown = $("#newItemEntityTypeField").kendoDropDownList({
            dataTextField: "displayName",
            dataValueField: "id",
            dataBound: this.onNewItemDialogEntityTypeDropDownDataBound.bind(this),
            dataSource: []
        }).data("kendoDropDownList");

        this.loadAvailableEntityTypesInDropDown(this.base.settings.zeroEncrypted);

        this.copyItemToEnvironmentDialog = $("#copyItemToEnvironmentDialog").kendoDialog({
            title: "Item kopieren",
            closable: true,
            modal: true,
            resizable: false,
            visible: false,
            open: (event) => {
                $("#copyItemToEnvironmentDialog").removeClass("hidden");
            },
            close: (event) => {
                $("#copyItemToEnvironmentDialog").addClass("hidden");
            },
            actions: [
                {
                    text: "Annuleren"
                },
                {
                    text: "OK",
                    primary: true,
                    action: this.onCopyItemToEnvironmentDialogOkClick.bind(this)
                }]
        }).data("kendoDialog");
    }

    /**
     * Gets all available entity types that can be added as a child to the given parent and adds those to the newItemDialogEntityTypeDropDown.
     * @param {string} parentId The (encrypted) ID of the parent to get the available entity types of.
     */
    async loadAvailableEntityTypesInDropDown(parentId) {
        const process = `loadAvailableEntityTypesInDropDown_${Date.now()}`;
        window.processing.addProcess(process);

        try {
            // Get available entity types, for creating new sub items.
            const entityTypes = await this.base.getAvailableEntityTypes(parentId);
            this.newItemDialogEntityTypeDropDown.setDataSource({ data: entityTypes });
            $("#addButton").toggle(entityTypes && entityTypes.length > 0);
        } catch (exception) {
            console.error("Error while getting available entity types", exception);
            kendo.alert("Er is iets fout gegaan met het ophalen van de beschikbare entiteitstypes voor deze module. Neem a.u.b. contact op met ons.");
        }

        window.processing.removeProcess(process);
    }

    /**
     * Event that gets called once the user clicks "OK" in the dialog for copying an item to different environments.
     */
    onCopyItemToEnvironmentDialogOkClick(event) {
        const checkedElements = event.sender.element.find("input[type=checkbox]:checked");
        if (checkedElements.length === 0) {
            kendo.alert("Kies a.u.b. een of meer omgevingen");
            return false;
        }

        try {
            const currentItemId = event.sender.element.data("id");
            const currentItemWindow = event.sender.element.data("currentItemWindow");
            let value = 0;
            checkedElements.each((_, element) => value += parseInt(element.value));
            const process = `copyToEnvironment_${Date.now()}`;
            window.processing.addProcess(process);
            this.base.copyToEnvironment(currentItemId, value).then((result) => {
                this.base.notification.show({ message: "Succesvol opgeslagen" }, "success");
                event.sender.element.data("currentItemWindow", null);
                window.processing.removeProcess(process);
                if (!currentItemWindow) {
                    this.base.loadItem(result.encryptedId, 0, result.entityType);
                } else {
                    currentItemWindow.close();
                    this.base.windows.loadItemInWindow(false, result.id, result.encryptedId, result.entityType, result.title, currentItemWindow.element.data("showTitleField"), null, { hideTitleColumn: false }, currentItemWindow.element.data("linkId"));
                }
            }).catch((error) => {
                kendo.alert("Er is iets fout gegaan. Probeer het nogmaals of neem contact op met ons.");
                console.error(error);
            });

            return true;
        } catch (exception) {
            kendo.alert("Er is iets fout gegaan. Probeer het nogmaals of neem contact op met ons.");
            console.error(exception);
            return false;
        }
    }

    /**
     * Opens the dialog for creating a new item.
     * The user can enter a name for the new item here and if there is more than one available entity type, the user can also choose one.
     * @param {string} parentId Optional: The (encrypted) ID of the parent to add the item to. Default value will be the selected item in the main tree view, or 0 if no item is selected.
     * @param {any} node Optional: The node in a tree view of the parent item. Default value will be the selected item in the main tree view.
     * @param {string} entityType Optional: The entity type to create. If no value has been given, the user can select an entity type.
     * @param {boolean} skipName Optional: If set the true and there is only one available entity type, the item will be created immediately, without a name. If there is more than 1 entity type, only the dropdown for entity type will be shown.
     * @param {boolean} skipInitialDialog Optional: If set the true, the item will be created immediately.
     * @param {number} linkTypeNumber Optional: The type number of the link between the new item and it's parent.
     * @param {number} moduleId Optional: The id of the module in which the item should be created.
     * @param {any} kendoComponent Optional: If this item is being created via a field with a kendo component (such as a grid or dropdown), add the instance of it here, so we can refresh the data source after.
     */
    async openCreateItemDialog(parentId, node, entityType, skipName = false, skipInitialDialog = false, linkTypeNumber = 1, moduleId = 0, kendoComponent = null) {
        node = node || (this.base.mainTreeView ? this.base.mainTreeView.select() : null);
        if (typeof parentId !== "string") {
            if (this.base.selectedItem && this.base.selectedItem.id) {
                parentId = this.base.selectedItem.id;
            } else {
                parentId = this.base.settings.zeroEncrypted;
            }
        }

        await this.loadAvailableEntityTypesInDropDown(parentId);

        this.newItemDialog.element.find("#alsoCreateInMainBranch").prop("checked", false);
        const newItemNameField = this.newItemDialog.element.find("#newItemNameField").val("");

        this.newItemDialog.element.data("parentId", parentId);
        this.newItemDialog.element.data("entityType", entityType);
        this.newItemDialog.element.data("treeNode", node);
        this.newItemDialog.element.data("linkTypeNumber", linkTypeNumber);
        this.newItemDialog.element.data("moduleId", moduleId);
        this.newItemDialog.element.data("kendoComponent", kendoComponent);

        const hasOnlyOneOption = this.newItemDialogEntityTypeDropDown.dataSource.data().length === 1;

        if (skipInitialDialog || (skipName && hasOnlyOneOption)) {
            this.createItem(skipName, skipInitialDialog);
            return;
        }

        this.newItemDialog.open();

        newItemNameField.closest(".new-item-dialog-row").toggle(!skipName);
        if (skipName) {
            return;
        }

        if (!hasOnlyOneOption) {
            this.newItemDialogEntityTypeDropDown.focus();
            this.newItemDialogEntityTypeDropDown.open();
        } else {
            newItemNameField.focus();
        }
    }

    /**
     * Function to create a new item and add it to the tree view.
     * This function only works together with the newItemDialog.
     * @param {boolean} skipName Set to true to remove the requirement of adding a name/title to the item.
     * @param {boolean} skipInitialDialog Optional: If set the true, the item will be created immediately.
     */
    async createItem(skipName = false, skipInitialDialog = false) {
        const options = this.newItemDialog.element.data();
        const entityType = skipInitialDialog ? options.entityType : (this.newItemDialogEntityTypeDropDown.value() || options.entityType);
        const parentId = options.parentId;
        let node = options.treeNode;
        const newName = $("#newItemNameField").val() || "";
        const alsoCreateInMainBranch = this.newItemDialog.element.find("#alsoCreateInMainBranch").prop("checked") || false;

        if (!entityType) {
            kendo.alert("Kies eerst een entiteit-type.");
            return;
        }

        if (!newName && skipName !== true && skipInitialDialog !== true) {
            kendo.alert("Vul eerst een naam in.");
            return;
        }

        // Create the item in database.
        const createItemResult = await this.base.createItem(entityType, parentId, newName, options.linkTypeNumber, [], false, options.moduleId, alsoCreateInMainBranch);
        if (!createItemResult) {
            return;
        }

        if (this.base.settings.gridViewMode || skipInitialDialog) {
            this.base.windows.loadItemInWindow(true, createItemResult.itemIdPlain, createItemResult.itemId, entityType, newName, !skipName, this.base.grids.mainGrid, { hideTitleColumn: true }, createItemResult.linkId, null, options.kendoComponent);
        } else {
            if (node && node.length > 0 && node[0].tagName !== "LI") {
                node = node.closest("li.k-item");
            }
            const parentName = node ? (node.find("> .k-top > .k-in, > .k-bot > .k-in").text() || node.text()) : "Root";

            this.base.notification.show({ message: `${entityType} '${kendo.htmlEncode(newName)}' toegevoegd onder '${kendo.htmlEncode(parentName)}'` }, "success");

            const dataItem = this.base.mainTreeView.dataItem(node) || {};
            if (!dataItem.newlyAdded && dataItem.hasChildren && node.attr("aria-expanded") !== "true") {
                this.base.mainTreeView.expand(node);
            } else {
                // Add the new item to the main tree view and select it.
                const newNode = this.base.mainTreeView.append({
                    encryptedItemId: createItemResult.itemId,
                    plainItemId: createItemResult.itemIdPlain,
                    spriteCssClass: createItemResult.icon,
                    title: newName,
                    destinationItemId: parentId,
                    newlyAdded: true,
                    entityType: entityType,
                    hasChildren: false
                }, node);

                this.base.mainTreeView.select(newNode);
                this.base.mainTreeView.trigger("select", {
                    node: newNode
                });

                if (newNode && newNode.length > 0 && newNode[0].scrollIntoView) {
                    newNode[0].scrollIntoView(false);
                }
            }
        }

        this.newItemDialog.close();
    }

    /**
     * Handles the data bound event of the newItemDialogEntityTypeDropDown.
     */
    onNewItemDialogEntityTypeDropDownDataBound() {
        if (!this.newItemDialogEntityTypeDropDown || !this.newItemDialogEntityTypeDropDown.dataSource) {
            return;
        }

        const data = this.newItemDialogEntityTypeDropDown.dataSource.data();
        if (!data.length) {
            return;
        }

        const hasMoreThanOneItem = data.length > 1;
        $("#newItemEntityTypeContainer").toggle(hasMoreThanOneItem);
        if (data.length > 0) {
            this.newItemDialogEntityTypeDropDown.value(data[0].id);
        }

        let title = "Nieuw item aanmaken";
        if (data.length === 1) {
            title = `${data[0].displayName} aanmaken`;
        }
        if (this.base.selectedItem) {
            title += ` onder '${this.base.selectedItem.name || this.base.selectedItem.title}'`;
        }

        this.newItemDialog.title(title);
    }
}
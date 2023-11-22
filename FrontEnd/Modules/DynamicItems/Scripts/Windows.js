import { Utils, Wiser } from "../../Base/Scripts/Utils.js";

require("@progress/kendo-ui/js/kendo.button.js");
require("@progress/kendo-ui/js/kendo.window.js");
require("@progress/kendo-ui/js/kendo.grid.js");
require("@progress/kendo-ui/js/kendo.tabstrip.js");
require("@progress/kendo-ui/js/kendo.validator.js");
require("@progress/kendo-ui/js/kendo.splitter.js");
require("@progress/kendo-ui/js/kendo.treeview.js");
require("@progress/kendo-ui/js/cultures/kendo.culture.nl-NL.js");
require("@progress/kendo-ui/js/messages/kendo.messages.nl-NL.js");

/**
 * Class for any and all functionality for windows (not dialogs).
 */
export class Windows {

    /**
     * Initializes a new instance of the Windows class.
     * @param {DynamicItems} base An instance of the base class (DynamicItems).
     */
    constructor(base) {
        this.base = base;

        this.mainWindow = null;

        this.searchItemsWindow = null;
        this.searchItemsWindowSettings = {
            parentId: null,
            plainParentId: 0,
            senderGrid: null,
            entityType: null
        };
        this.searchItemsGrid = null;
        this.searchItemsGridFirstLoad = true;
        this.searchGridSettings = {
            hideSelectableColumn: false,
            hideIdColumn: false,
            hideTypeColumn: false,
            hideEnvironmentColumn: false,
            hideTitleColumn: false,
            pageSize: 50
        };
        this.searchGridLoading = true;

        // HistoryGrid
        this.historyGridFirstLoad = true;
        this.historyGridWindow = null;
        this.historyGrid = null;

        this.fileManagerModes = Object.freeze({
            images: "images",
            files: "files",
            templates: "templates"
        });

        // File manager
        this.fileManagerWindow = null;
        this.fileManagerIframe = null;
        this.fileManagerWindowSender = null;
        this.fileManagerWindowMode = null;
        this.fileManagerWindowAddButton = null;
    }

    /**
     * Do all initializations for the Windows class, such as adding bindings.
     */
    initialize() {
        this.fileManagerWindow = Wiser.initializeFileManager(this.fileManagerWindowSender,
            this.fileManagerWindowMode,this.base.settings.iframeMode, this.base.settings.gridViewMode, 
            this.base.settings.moduleName);

        this.mainWindow?.wrapper.find(".k-i-refresh").parent().click(this.base.onMainRefreshButtonClick.bind(this.base));
    }

    /**
     * Open a new window and load an item in that window.
     * That item can then be edited in that window, the same as it was loaded in the main screen.
     * @param {boolean} isNewItem Indicates whether or not this an item that has just been created by a previous function.
     * @param {number} itemId The plain item ID.
     * @param {string} encryptedItemId The encrypted item ID.
     * @param {string} entityType The entity type.
     * @param {string} title The title of the window.
     * @param {boolean} showTitleField Indicates whether or not the show a field where the user can edit the title of the item.
     * @param {kendoGrid} senderGrid Optional: If the item is opened via a kendoGrid, enter that grid here.
     * @param {any} fieldOptions Optional: The options for the kendo Grid.
     * @param {number} linkId Optional: If the item was opened via a specific link, enter the ID of that link here.
     * @param {string} windowTitle Optional: The title of the window. If empty, it will use the item title.
     * @param {any} kendoComponent Optional: If this item is being created via a field with a kendo component (such as a grid or dropdown), add the instance of it here, so we can refresh the data source after.
     * @param {int} linkType Optional: If the item was opened via a specific link, enter the type number of that link here.
     */
    async loadItemInWindow(isNewItem, itemId, encryptedItemId, entityType, title, showTitleField, senderGrid, fieldOptions, linkId, windowTitle = null, kendoComponent = null, linkType = 0) {
        let currentItemWindow;
        try {
            // Clone the window template and initialize a new window from that clone, then open it.
            const windowId = `existingItemWindow_${itemId || decodeURIComponent(encryptedItemId).replace(/-/g, "").replace(/\+/g, "").replace(/=/g, "").replace(/\//g, "")}`;

            currentItemWindow = $(`#${windowId}`).data("kendoWindow");

            // If the window still exists, we just want to bring that window to the front, to prevent people from opening an item in multiple windows.
            // This prevents confusion ("I thought I already closed this item before.") and also prevents problems with fields that would have duplicate IDs then.
            if (currentItemWindow) {
                currentItemWindow.maximize().center().open();
                return;
            }

            currentItemWindow = $("#itemWindow_template").clone(true).attr("id", windowId).kendoWindow({
                width: "90%",
                height: "90%",
                visible: false,
                modal: true,
                actions: ["Verwijderen", "Verversen", "Vertalen", "Close"],
                close: (closeEvent) => {
                    const closeFunction = () => {
                        try {
                            // If the current item is a new item and it's not being saved at the moment, then delete it because it was a temporary item.
                            if (isNewItem && !currentItemWindow.element.data("saving")) {
                                let canDelete = true;
                                for (let gridElement of currentItemWindow.element.find(".grid")) {
                                    const kendoGrid = $(gridElement).data("kendoGrid");
                                    if (!kendoGrid) {
                                        continue;
                                    }

                                    // Don't delete this item if someone added something in one of the grids on the item.
                                    if (kendoGrid.dataSource.data().length > 0) {
                                        canDelete = false;
                                    }
                                }

                                if (canDelete) {
                                    this.base.deleteItem(encryptedItemId, entityType);
                                }
                            }
                        } catch (exception) {
                            console.error(exception);
                            kendo.alert("Er is iets fout gegaan met het verwijderen van het tijdelijk aangemaakt item. Neem a.u.b. contact op met ons.");
                        }

                        // Delete all field initializers of the current window, so they don't stay in memory. We don't need them anymore once the window is closed.
                        delete this.base.fields.fieldInitializers[windowId];

                        // Destroy the window.
                        try {
                            currentItemWindow.destroy();
                        } catch (exception) {
                            console.error(exception);
                        }
                        currentItemWindow.element.remove();
                    };

                    if (!currentItemWindow.element.data("saving") && !$.isEmptyObject(this.base.fields.unsavedItemValues[windowId])) {
                        Wiser.showConfirmDialog("Weet u zeker dat u wilt annuleren en gewijzigde of ingevoerde gegevens wilt verwijderen?").then(closeFunction.bind(this));
                        closeEvent.preventDefault();
                        return false;
                    }

                    closeFunction();
                }
            }).data("kendoWindow");

            const infoPanel = $("#infoPanel_template").clone(true).attr("id", `${windowId}_infoPanel`).insertAfter(currentItemWindow.element);
            const newMetaToggleElementId = `${windowId}_meta-toggle`;
            currentItemWindow.element.find("#meta-toggle_template").attr("id", newMetaToggleElementId);
            currentItemWindow.element.find("[for=meta-toggle_template]").attr("for", newMetaToggleElementId);

            currentItemWindow.element.on("click", "h4.tooltip .info-link", this.base.fields.onTooltipClick.bind(this, infoPanel));
            currentItemWindow.element.on("contextmenu", ".item > h4", this.base.fields.onFieldLabelContextMenu.bind(this));

            currentItemWindow.element.find("form.tabStripPopup").on("submit", (event) => {
                event.preventDefault();
                currentItemWindow.element.find(".saveAndCloseBottomPopup").trigger("click");
            });

            currentItemWindow.maximize().center().open();

            // Initialize the tab strip on the new window.
            const currentItemTabStrip = currentItemWindow.element.find(".tabStripPopup").kendoTabStrip({
                animation: {
                    open: {
                        effects: "fadeIn"
                    }
                },
                scrollable: true,
                select: this.base.onTabStripSelect.bind(this.base, itemId, windowId)
            }).data("kendoTabStrip");

            const element = currentItemWindow.element;

            // Setup validator.
            const validator = element.kendoValidator({
                validate: this.base.onValidateForm.bind(this.base, currentItemTabStrip),
                validateOnBlur: false,
                messages: {
                    required: (input) => {
                        const fieldDisplayName = $(input).closest(".item").find("> h4 > label").text() || $(input).attr("name");
                        return `${fieldDisplayName} is verplicht`;
                    },
                    pattern: (input) => {
                        const fieldDisplayName = $(input).closest(".item").find("> h4 > label").text() || $(input).attr("name");
                        return `${fieldDisplayName} is niet correct`;
                    },
                    step: (input) => {
                        const fieldDisplayName = $(input).closest(".item").find("> h4 > label").text() || $(input).attr("name");
                        return `${fieldDisplayName} is niet correct`;
                    }
                }
            }).data("kendoValidator");

            // Save data that we need later, when saving the values of the new item.
            element.data("isNewItem", isNewItem);
            element.data("entityType", entityType);
            element.data("senderGrid", senderGrid);
            element.data("fieldOptions", fieldOptions);
            element.data("itemId", encryptedItemId);
            element.data("title", title);
            element.data("validator", validator);
            element.data("showTitleField", showTitleField);
            element.data("linkId", linkId);

            if (!windowTitle && title) {
                windowTitle = title
            }

            windowTitle = !windowTitle ? "" : ` &quot;${windowTitle}&quot; <strong> bewerken</strong>`;
            currentItemWindow.title({
                text: `<button type='button' class='btn btn-cancel'><ins class='icon-line-exit'></ins><span>Annuleren</span></button>${windowTitle}`,
                encoded: false
            });

            // Initialize the cancel button on the top left of the window.
            currentItemWindow.wrapper.find(".btn-cancel").click((event) => {
                currentItemWindow.close();
            });

            const afterSave = async () => {
                if (!kendoComponent || !kendoComponent.dataSource) return;

                await kendoComponent.dataSource.read();

                if (!kendoComponent.element) return;

                const role = kendoComponent.element.data("role");
                if (role === "dropdownlist" || role === "combobox") {
                    kendoComponent.value(itemId);
                    validator.validateInput(kendoComponent.element);
                }
            };

            // Initialize the buttons on the new window.
            currentItemWindow.element.find(".saveBottomPopup").kendoButton({
                icon: "save",
                click: async (event) => {
                    await this.onSaveItemPopupClick(event, false, !senderGrid);
                    afterSave();
                }
            });
            currentItemWindow.element.find(".saveAndCloseBottomPopup").kendoButton({
                icon: "save",
                click: async (event) => {
                    await this.onSaveItemPopupClick(event, true, !senderGrid);
                    afterSave();
                }
            });

            currentItemWindow.element.find(".cancelItemPopup").kendoButton({
                click: (event) => {
                    currentItemWindow.close();
                },
                icon: "cancel"
            });

            const loadPopupContents = async (tabIndex = 0) => {
                try {
                    currentItemWindow.element.find(".popup-loader").addClass("loading");

                    // Set meta data of the selected item in the footer.
                    // No await here so that this runs concurrently with the getItemHtml below, they don't need to wait for each other.
                    this.base.addItemMetaData(encryptedItemId, entityType, currentItemWindow.element.find("footer .metaData"), true, null, currentItemWindow).then((itemMetaData) => {
                        const newTitle = isNewItem ? (title || "") : itemMetaData.title;
                        currentItemWindow.element.find(".itemNameField").val(newTitle);

                        currentItemWindow.element.find(".editMenu .copyToEnvironment").off("click").click(async (event) => {
                            this.base.dialogs.copyItemToEnvironmentDialog.element.find("input[type=checkbox]").prop("checked", false);
                            this.base.dialogs.copyItemToEnvironmentDialog.element.data("id", itemMetaData.plainOriginalItemId);
                            this.base.dialogs.copyItemToEnvironmentDialog.element.data("currentItemWindow", currentItemWindow);
                            this.base.dialogs.copyItemToEnvironmentDialog.open();
                        });
                    });

                    // Get the information that we need about the opened item.
                    const promises = [
                        this.base.getEntityType(entityType),
                        this.base.getItemHtml(encryptedItemId, entityType, windowId, linkId, linkType)
                    ];

                    if (!isNewItem) {
                        promises.push(this.base.getTitle(encryptedItemId));
                    }

                    const data = await Promise.all(promises);
                    // Returned values will be in order of the Promises passed, regardless of completion order.
                    let lastUsedEntityType = data[0];
                    const htmlData = data[1];

                    lastUsedEntityType.showTitleField = lastUsedEntityType.showTitleField || false;
                    currentItemWindow.element.data("entityTypeDetails", lastUsedEntityType);
                    currentItemWindow.element.data("entityType", entityType);
                    const nameField = currentItemWindow.element.find(".itemNameField");
                    currentItemWindow.element.find(".itemNameFieldContainer").toggle(lastUsedEntityType.showTitleField && showTitleField);

                    currentItemTabStrip.element.find("> .k-tabstrip-items-wrapper > ul > li .addedFromDatabase").each((index, element) => {
                        currentItemTabStrip.remove($(element).closest("li.k-item"));
                    });

                    // Handle access rights.
                    currentItemWindow.wrapper.find(".itemNameField").prop("readonly", !htmlData.canWrite).prop("disabled", !htmlData.canWrite);
                    currentItemWindow.wrapper.find(".saveButton").toggleClass("hidden", !htmlData.canWrite);
                    currentItemWindow.wrapper.find(".k-i-verwijderen").parent().toggleClass("hidden", !htmlData.canDelete);
                    currentItemWindow.element.find(".editMenu .undeleteItem").closest("li").toggleClass("hidden", !htmlData.canDelete);

                    // Add all fields and tabs to the window.
                    let genericTabHasFields = false;
                    for (let i = htmlData.tabs.length - 1; i >= 0; i--) {
                        const tabData = htmlData.tabs[i];
                        if (!tabData.name) {
                            genericTabHasFields = true;
                            const container = currentItemWindow.element.find(".right-pane-content-popup").html(tabData.htmlTemplate);
                            await this.base.loadKendoScripts(tabData.scriptTemplate);
                            $.globalEval(tabData.scriptTemplate);

                            await Utils.sleep(150);
                            container.find("input").first().focus();
                        } else {
                            currentItemTabStrip.insertAfter({
                                text: tabData.name,
                                content: "<div class='dynamicTabContent'>" + tabData.htmlTemplate + "</div>",
                                spriteCssClass: "addedFromDatabase"
                            }, currentItemTabStrip.tabGroup.children().eq(0));

                            if (!this.base.fields.fieldInitializers[windowId]) {
                                this.base.fields.fieldInitializers[windowId] = {};
                            }

                            this.base.fields.fieldInitializers[windowId][tabData.name] = {
                                executed: false,
                                script: tabData.scriptTemplate,
                                entityType: entityType
                            };
                        }
                    }

                    currentItemWindow.wrapper.find(".k-i-vertalen").parent().toggleClass("hidden", this.base.allLanguages.length <= 1 && currentItemWindow.element.find(".item[data-language-code]:not([data-language-code=''])").length === 0);

                    // Setup dependencies for all tabs.
                    for (let i = htmlData.tabs.length - 1; i >= 0; i--) {
                        const tabData = htmlData.tabs[i];
                        const container = currentItemTabStrip.contentHolder(i);
                        this.base.fields.setupDependencies(container, entityType, tabData.name || "Gegevens");
                    }

                    // Handle dependencies for the first tab, to make sure all the correct fields are hidden/shown on the first tab. The other tabs will be done once they are opened.
                    this.base.fields.handleAllDependenciesOfContainer(currentItemTabStrip.contentHolder(0), entityType, "Gegevens", windowId);

                    const showGenericTab = genericTabHasFields || !htmlData.tabs.length;
                    $(currentItemTabStrip.items()[0]).toggle(genericTabHasFields || !htmlData.tabs.length);

                    if (!genericTabHasFields && !htmlData.tabs.length) {
                        nameField.keypress((event) => {
                            this.base.fields.onInputFieldKeyUp(event, { saveOnEnter: true });
                        });
                    }

                    currentItemTabStrip.select(tabIndex || (showGenericTab ? 0 : 1));
                } catch (exception) {
                    console.error(exception);
                    kendo.alert("Er is iets fout gegaan tijdens het (her)laden van dit item. Probeer het a.u.b. nogmaals of neem contact op met ons.");
                }

                if (!currentItemWindow) {
                    return;
                }

                currentItemWindow.element.find(".popup-loader").removeClass("loading");
            };

            currentItemWindow.element.data("reloadFunction", loadPopupContents);

            // Bind events for the icons on the top-right of the window.
            currentItemWindow.wrapper.find(".k-i-verversen").parent().click(async (event) => {
                const previouslySelectedTab = currentItemTabStrip.select().index();
                await loadPopupContents(previouslySelectedTab);
            });
            currentItemWindow.wrapper.find(".k-i-verwijderen").parent().click(this.onDeleteItemPopupClick.bind(this));

            currentItemWindow.element.find(".editMenu .undeleteItem").click(async (event) => {
                await this.base.onUndeleteItemClick(event, encryptedItemId);
            });

            currentItemWindow.wrapper.find(".k-i-vertalen").parent().click(async (event) => {
                await this.base.onTranslateItemClick(event, encryptedItemId, entityType);
            });

            await loadPopupContents();
        } catch (exception) {
            console.error(exception);
            kendo.alert("Er is iets fout gegaan tijdens het laden van dit item. Probeer het a.u.b. nogmaals of neem contact op met ons.");
        }
    }

    /**
     * The click event for the delete button of item popups.
     * @param {any} event The click event.
     */
    async onDeleteItemPopupClick(event) {
        event.preventDefault();

        await Wiser.showConfirmDialog("Weet u zeker dat u dit item wilt verwijderen?");

        const popupWindowContainer = $(event.currentTarget).closest(".k-window").find(".popup-container");

        try {
            popupWindowContainer.find(".popup-loader").addClass("loading");
            popupWindowContainer.data("saving", true);

            const kendoWindow = popupWindowContainer.data("kendoWindow");
            let entityType = popupWindowContainer.data("entityTypeDetails").entityType;

            const data = kendoWindow.element.data();
            const encryptedItemId = data.itemId;

            await this.base.deleteItem(encryptedItemId, entityType);

            popupWindowContainer.find(".popup-loader").removeClass("loading");

            kendoWindow.close();

            if (data.senderGrid) {
                data.senderGrid.dataSource.read();
            }
        } catch (exception) {
            console.error(exception);
            popupWindowContainer.find(".popup-loader").removeClass("loading");
            popupWindowContainer.data("saving", false);

            if (exception.status === 409) {
                const message = exception.responseText || "Het is niet meer mogelijk om dit item te verwijderen.";
                kendo.alert(message);
            } else {
                kendo.alert("Er is iets fout gegaan tijdens het verwijderen van dit item. Probeer het a.u.b. nogmaals of neem contact op met ons.");
            }
        }
    }

    /**
     * The click event for the save button of item popups.
     * @param {any} args The arguments of the click event.
     * @param {boolean} alsoCloseWindow Indicates whether or not to close the window/popup after saving.
     * @param {boolean} addToTreeView Indicates whether or not to add the new item to the main tree view.
     */
    async onSaveItemPopupClick(args, alsoCloseWindow = true, addToTreeView = true) {
        args.event.preventDefault();
        const popupWindowContainer = $(args.event.currentTarget).closest(".popup-container");

        try {
            popupWindowContainer.find(".popup-loader").addClass("loading");
            popupWindowContainer.data("saving", true);

            const kendoWindow = popupWindowContainer.data("kendoWindow");
            const isNewItemWindow = popupWindowContainer.data("isNewItem");
            let entityType = popupWindowContainer.data("entityTypeDetails");
            const validator = popupWindowContainer.data("validator");

            if (Wiser.validateArray(entityType)) {
                entityType = entityType[0];
            }

            if (!validator.validate()) {
                popupWindowContainer.find(".popup-loader").removeClass("loading");
                return;
            }

            const data = kendoWindow.element.data();
            const titleField = popupWindowContainer.find(".itemNameField");
            const newTitle = titleField.val();
            const itemId = data.itemId;
            const inputData = this.base.fields.getInputData(popupWindowContainer.find(".right-pane-content-popup, .dynamicTabContent"));

            let titleToSave = data.title || null;
            if (titleField.is(":visible")) {
                titleToSave = newTitle;
            }
            const promises = [this.base.updateItem(itemId, inputData, popupWindowContainer, isNewItemWindow, titleToSave, true, true, entityType.entityType || entityType.name)];

            await Promise.all(promises);

            popupWindowContainer.find(".popup-loader").removeClass("loading");

            if (alsoCloseWindow) {
                kendoWindow.close();
            }

            if (data.senderGrid) {
                this.base.grids.mainGridForceRecount = true;
                data.senderGrid.dataSource.read();
            }

            if (!isNewItemWindow) {
                return;
            }

            const treeView = this.base.mainTreeView;
            if (!treeView || !addToTreeView) {
                return;
            }

            const selectedNode = treeView.select();
            if (selectedNode.attr("aria-expanded") == "true") {
                const parentName = treeView.dataItem(selectedNode).name;
                const newNode = treeView.append({
                    id: itemId,
                    name: titleToSave,
                    destinationItemId: data.parentId
                }, selectedNode);
            }
        } catch (exception) {
            console.error(exception);
            popupWindowContainer.find(".popup-loader").removeClass("loading");
            popupWindowContainer.data("saving", false);

            switch (exception.status) {
                case 409: {
                    const message = exception.responseText || "Het is niet meer mogelijk om aanpassingen te maken in dit item.";
                    kendo.alert(message);
                    break;
                }
                case 403: {
                    const message = exception.responseText || "U heeft niet de juiste rechten om dit item te wijzigen.";
                    kendo.alert(message);
                    break;
                }
                default:
                    kendo.alert("Er is iets fout gegaan tijdens het opslaan van dit item. Probeer het a.u.b. nogmaals of neem contact op met ons.");
                    break;
            }
        }
    }

    /**
     * Function that gets called when the user clicks (un)checks a row in the search items window grid.
     * @param {any} event The click event.
     */
    async onSearchItemsGridChange(event) {
        if (this.searchGridLoading || this.searchGridChangeBusy) {
            return;
        }

        this.searchGridChangeBusy = true;
        const grid = event.sender;

        try {
            const allItemsOnCurrentPage = grid.items();
            const alreadyLinkedItems = this.searchItemsWindowSettings.senderGrid.dataSource.data();

            const promises = [];
            const addLinksRequest = {
                encryptedSourceIds: [],
                encryptedDestinationIds: [],
                linkType: this.searchItemsWindowSettings.linkTypeNumber
            };
            const removeLinksRequest = {
                encryptedSourceIds: [],
                encryptedDestinationIds: [],
                linkType: this.searchItemsWindowSettings.linkTypeNumber
            };
            if (this.searchItemsWindowSettings.currentItemIsSourceId) {
                addLinksRequest.sourceEntityType = this.searchItemsWindowSettings.entityType;
                removeLinksRequest.sourceEntityType = this.searchItemsWindowSettings.entityType;
            }
            kendo.ui.progress(grid.element, true);
            for (let element of allItemsOnCurrentPage) {
                const row = $(element);
                const dataItem = grid.dataItem(row);
                const isChecked = row.find("td > input[type=checkbox]").prop("checked");

                if (isChecked) {
                    if (alreadyLinkedItems.filter((item) => (item.id || item[`ID_${this.searchItemsWindowSettings.entityType}`]) === dataItem.id).length === 0) {
                        if (dataItem.parentItemId > 0 && dataItem.parentItemId !== this.searchItemsWindowSettings.plainParentId) {
                            try {
                                await Wiser.showConfirmDialog(`Let op! Dit item is al gekoppeld aan een ander item (ID ${dataItem.parentItemId}). Als u op "OK" klikt, zal die koppeling vervangen worden door deze nieuwe koppeling.`, "Koppeling vervangen", "Annuleren", "Vervangen");
                            }
                            catch {
                                row.find("td > input[type=checkbox]").prop("checked", false);
                                row.removeClass("k-state-selected");
                                continue;
                            }
                        }

                        if (this.searchItemsWindowSettings.currentItemIsSourceId) {
                            if (addLinksRequest.encryptedSourceIds.length === 0) {
                                addLinksRequest.encryptedSourceIds.push(this.searchItemsWindowSettings.parentId);
                            }
                            addLinksRequest.encryptedDestinationIds.push(dataItem.encryptedId || dataItem.encrypted_id || dataItem.encryptedid);
                        } else {
                            if (addLinksRequest.encryptedDestinationIds.length === 0) {
                                addLinksRequest.encryptedDestinationIds.push(this.searchItemsWindowSettings.parentId);
                            }
                            addLinksRequest.encryptedSourceIds.push(dataItem.encryptedId || dataItem.encrypted_id || dataItem.encryptedid);
                        }
                    }
                } else {
                    if (alreadyLinkedItems.filter((item) => (item.id || item[`ID_${this.searchItemsWindowSettings.entityType}`]) === dataItem.id).length > 0) {
                        if (this.searchItemsWindowSettings.currentItemIsSourceId) {
                            if (removeLinksRequest.encryptedSourceIds.length === 0) {
                                removeLinksRequest.encryptedSourceIds.push(this.searchItemsWindowSettings.parentId);
                            }
                            removeLinksRequest.encryptedDestinationIds.push(dataItem.encryptedId || dataItem.encrypted_id || dataItem.encryptedid);
                        } else {
                            if (removeLinksRequest.encryptedDestinationIds.length === 0) {
                                removeLinksRequest.encryptedDestinationIds.push(this.searchItemsWindowSettings.parentId);
                            }
                            removeLinksRequest.encryptedSourceIds.push(dataItem.encryptedId || dataItem.encrypted_id || dataItem.encryptedid);
                        }
                    }
                }
            }

            if (addLinksRequest.encryptedSourceIds.length > 0 && addLinksRequest.encryptedDestinationIds.length > 0) {
                promises.push(Wiser.api({
                    url: `${this.base.settings.wiserApiRoot}items/add-links?moduleId=${this.base.settings.moduleId}`,
                    method: "POST",
                    contentType: "application/json",
                    data: JSON.stringify(addLinksRequest)
                }));
            }
            if (removeLinksRequest.encryptedSourceIds.length > 0 && removeLinksRequest.encryptedDestinationIds.length > 0) {
                promises.push(Wiser.api({
                    url: `${this.base.settings.wiserApiRoot}items/remove-links?moduleId=${this.base.settings.moduleId}`,
                    method: "DELETE",
                    contentType: "application/json",
                    data: JSON.stringify(removeLinksRequest)
                }));
            }

            await Promise.all(promises);

            this.searchItemsWindowSettings.senderGrid.dataSource.read();
        } catch (exception) {
            console.error(exception);
            kendo.alert("Er is iets fout gegaan tijdens het verwerken van de nieuwe koppeling(en). Probeer het a.u.b. nogmaals of neem contact op met ons");
        }

        this.searchGridChangeBusy = false;
        kendo.ui.progress(grid.element, false);
    }

    /**
     * Function that gets called once the search items grid's data has been loaded.
     * This will select any and all items that are already linked to the current item, so that the user can unlink them.
     * @param {any} event The data bound event from KendoGrid.
     */
    onSearchItemsGridDataBound(event) {
        const grid = event.sender;
        const rows = grid.items();
        const alreadyLinkedItems = this.searchItemsWindowSettings.senderGrid.dataSource.data();

        rows.each((index, element) => {
            const row = $(element);
            const dataItem = grid.dataItem(row);

            if (alreadyLinkedItems.filter((item) => (item.id || item[`id_${this.searchItemsWindowSettings.entityType}`] || item[`ID_${this.searchItemsWindowSettings.entityType}`]) === dataItem.id).length > 0) {
                grid.select(row);
            }
        });

        this.searchGridLoading = false;

        if (this.searchItemsGridSelectAllClicked) {
            this.searchItemsGridSelectAllClicked = false;

            if (grid.thead.find(".k-checkbox").prop("checked")) {
                grid.clearSelection();
            } else {
                grid.select("tr");
            };
        }
    }

    /**
     * Function for doing things that need to be done after the data source of the search grid has been changed.
     * This will set the property 'searchGridLoading' to true.
     * @param {any} event The grid data source change event.
     */
    onSearchGridDataSourceChange(event) {
        this.searchGridLoading = true;
    }

    /**
     * Does everything to make sure that the grid for searching items inside the searchItemsWindow works properly.
     * @param {string} entityType The entity type of items that should be shown in the grid.
     * @param {number} parentId The ID of the currently opened item.
     * @param {number} propertyId The ID of the current property.
     * @param {any} gridOptions The options of the grid.
     */
    async initializeSearchItemsGrid(entityType, parentId, propertyId, gridOptions) {
        try {
            gridOptions = gridOptions || {};
            gridOptions.searchGridSettings = gridOptions.searchGridSettings || {};
            gridOptions.searchGridSettings.gridViewSettings = gridOptions.searchGridSettings.gridViewSettings || {};

            const searchItemsGridElement = this.searchItemsWindow.element.find("#searchItemsWindowGrid");

            if (searchItemsGridElement.data("kendoGrid")) {
                searchItemsGridElement.data("kendoGrid").destroy();
                searchItemsGridElement.empty();
            }

            const options = {
                page: 1,
                pageSize: gridOptions.searchGridSettings.gridViewSettings.pageSize || this.searchGridSettings.pageSize,
                skip: 0,
                take: gridOptions.searchGridSettings.gridViewSettings.pageSize || this.searchGridSettings.pageSize
            };

            let gridTypeQueryString = `&mode=1`;
            if (gridOptions && gridOptions.toolbar && gridOptions.toolbar.linkItemsQueryId) {
                gridTypeQueryString = `&mode=4&queryId=${encodeURIComponent(gridOptions.toolbar.linkItemsQueryId)}`;
                if (gridOptions.toolbar.linkItemsCountQueryId) {
                    gridTypeQueryString += `&countQueryId=${gridOptions.toolbar.linkItemsCountQueryId}`;
                }
            } else {
                gridTypeQueryString += `&currentItemIsSourceId=${gridOptions.currentItemIsSourceId || false}`;
            }

            const gridDataResult = await Wiser.api({
                url: `${this.base.settings.wiserApiRoot}items/${encodeURIComponent(parentId)}/entity-grids/${encodeURIComponent(entityType)}?moduleId=${this.base.settings.moduleId}&propertyId=${propertyId}${gridTypeQueryString}`,
                method: "POST",
                contentType: "application/json",
                data: JSON.stringify(options)
            });

            if (gridDataResult.extraJavascript) {
                $.globalEval(gridDataResult.extraJavascript);
            }

            if (gridDataResult.columns && gridDataResult.columns.length) {
                let checkBoxColumnFound = false;
                gridDataResult.columns.forEach((column) => {
                    if (column.selectable === true) {
                        checkBoxColumnFound = true;
                    }

                    switch (column.field) {
                        case "id":
                            column.hidden = this.searchGridSettings.hideIdColumn || false;
                            break;
                        case "link_id":
                        case "linkid":
                            column.hidden = this.searchGridSettings.hideLinkIdColumn || false;
                            break;
                        case "entity_type":
                        case "entitytype":
                            column.hidden = this.searchGridSettings.hideTypeColumn || false;
                            break;
                        case "published_environment":
                        case "publishedenvironment":
                            column.hidden = this.searchGridSettings.hideEnvironmentColumn || false;
                            break;
                        case "name":
                        case "title":
                            column.hidden = this.searchGridSettings.hideTitleColumn || false;
                            break;
                        case "encrypted_id":
                        case "encryptedid":
                            column.hidden = true;
                            break;
                    }
                });

                if (!checkBoxColumnFound) {
                    gridDataResult.columns.unshift({
                        selectable: true,
                        width: "55px"
                    });
                }
            }

            let previousFilters = null;
            let totalResults = gridDataResult.totalResults;
            this.searchItemsGridFirstLoad = true;
            const finalGridOptions = $.extend(true, {
                dataSource: {
                    serverPaging: true,
                    serverSorting: true,
                    serverFiltering: true,
                    pageSize: gridDataResult.pageSize,
                    transport: {
                        read: async (transportOptions) => {
                            try {
                                if (this.searchItemsGridFirstLoad) {
                                    transportOptions.success(gridDataResult);
                                    this.searchItemsGridFirstLoad = false;
                                    return;
                                }

                                // If we're using the same filters as before, we don't need to count the total amount of results again,
                                // so we tell the API whether this is the case, so that it can skip the execution of the count query, to make scrolling through the grid faster.
                                let currentFilters = null;
                                if (transportOptions.data.filter) {
                                    currentFilters = JSON.stringify(transportOptions.data.filter);
                                }

                                transportOptions.data.firstLoad = currentFilters !== previousFilters;
                                transportOptions.data.take = transportOptions.data.pageSize;
                                previousFilters = currentFilters;

                                const newGridDataResult = await Wiser.api({
                                    url: `${this.base.settings.wiserApiRoot}items/${encodeURIComponent(parentId)}/entity-grids/${entityType}?moduleId=${this.base.settings.moduleId}&propertyId=${propertyId}${gridTypeQueryString}`,
                                    method: "POST",
                                    contentType: "application/json",
                                    data: JSON.stringify(transportOptions.data)
                                });

                                if (typeof newGridDataResult.totalResults !== "number" || !transportOptions.data.firstLoad) {
                                    newGridDataResult.totalResults = totalResults;
                                } else if (transportOptions.data.firstLoad) {
                                    totalResults = newGridDataResult.totalResults;
                                }

                                transportOptions.success(newGridDataResult);
                            } catch (exception) {
                                console.error(exception);
                                transportOptions.error(exception);
                            }
                        }
                    },
                    change: this.onSearchGridDataSourceChange.bind(this),
                    schema: {
                        data: "data",
                        total: "totalResults",
                        model: gridDataResult.schemaModel
                    }
                },
                persistSelection: true,
                columns: gridDataResult.columns,
                resizable: true,
                sortable: true,
                scrollable: {
                    virtual: true
                },
                filterable: {
                    extra: false,
                    operators: {
                        string: {
                            startswith: "Begint met",
                            eq: "Is gelijk aan",
                            neq: "Is ongelijk aan",
                            contains: "Bevat",
                            doesnotcontain: "Bevat niet",
                            endswith: "Eindigt op"
                        }
                    },
                    messages: {
                        isTrue: "<span>Ja</span>",
                        isFalse: "<span>Nee</span>"
                    }
                },
                dataBound: this.onSearchItemsGridDataBound.bind(this),
                change: this.onSearchItemsGridChange.bind(this),
                filterMenuInit: this.base.grids.onFilterMenuInit.bind(this),
                filterMenuOpen: this.base.grids.onFilterMenuOpen.bind(this)
            }, gridOptions.searchGridSettings.gridViewSettings);

            await require("/kendo/messages/kendo.grid.nl-NL.js");
            this.searchItemsGrid = searchItemsGridElement.kendoGrid(finalGridOptions).data("kendoGrid");

            if (this.searchGridSettings.enableSelectAllServerSide) {
                this.searchItemsGrid.thead.find(".k-checkbox").click((event) => {
                    event.preventDefault();
                    this.searchItemsGridSelectAllClicked = true;
                    this.searchItemsGridTotalResults = totalResults;
                    this.searchItemsGridOldPageSize = this.searchItemsGrid.dataSource.pageSize();
                    this.searchItemsGrid.dataSource.pageSize(totalResults);
                });
            }
        } catch (exception) {
            console.error(exception);
            kendo.alert("Er is iets fout gegaan met het initialiseren van het overzicht. Probeer het a.u.b. nogmaals of neem contact op met ons.");
        }
    }
}
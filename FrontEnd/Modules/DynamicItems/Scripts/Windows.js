import { Utils, Wiser2 } from "../../Base/Scripts/Utils.js";

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

        // Upload windows.
        this.imagesUploaderWindow = null;
        this.imagesUploaderWindowTreeView = null;
        this.imagesUploaderWindowTreeViewState = null;
        this.imagesUploaderWindowTreeViewContextMenu = null;
        this.imagesUploaderWindowTreeViewContextMenuTarget = null;
        this.imagesUploaderWindowSplitter = null;
        this.imagesUploaderWindowAddButton = null;
        this.imagesUploaderSender = null;

        this.filesUploaderWindow = null;
        this.filesUploaderWindowTreeView = null;
        this.filesUploaderWindowTreeViewState = null;
        this.filesUploaderWindowTreeViewContextMenu = null;
        this.filesUploaderWindowTreeViewContextMenuTarget = null;
        this.filesUploaderWindowSplitter = null;
        this.filesUploaderWindowAddButton = null;
        this.filesUploaderSender = null;

        this.templatesUploaderWindow = null;
        this.templatesUploaderWindowTreeView = null;
        this.templatesUploaderWindowTreeViewState = null;
        this.templatesUploaderWindowTreeViewContextMenu = null;
        this.templatesUploaderWindowTreeViewContextMenuTarget = null;
        this.templatesUploaderWindowSplitter = null;
        this.templatesUploaderWindowAddButton = null;
        this.templatesUploaderSender = null;

        this.uploaderWindowTreeViewStateLoading = false;
        this.uploaderWindowTreeViewStates = {
            images: null,
            files: null,
            templates: null
        };
    }

    /**
     * Do all initializations for the Windows class, such as adding bindings.
     */
    initialize() {

        // Window for searching for items to link to another item.
        this.historyGridWindow = $("#historyWindowGrid").kendoWindow({
            width: "90%",
            height: "90%",
            title: "History",
            visible: false,
            modal: true,
            actions: ["Close"]
        }).data("kendoWindow");

        // Window for searching for items to link to another item.
        this.searchItemsWindow = $("#searchItemsWindow").kendoWindow({
            width: "90%",
            height: "90%",
            title: "Item zoeken",
            visible: false,
            modal: true,
            actions: ["Close"]
        }).data("kendoWindow");

        // Window for viewing a all generic images and for adding them into an HTML editor.
        this.imagesUploaderWindow = $("#imagesUploaderWindow").kendoWindow({
            width: "90%",
            height: "90%",
            title: "Afbeeldingen",
            visible: false,
            modal: true,
            actions: ["Close"],
            open: (event) => {
                this.imagesUploaderWindowSplitter.resize(true);

                if (this.imagesUploaderWindowTreeView) {
                    this.imagesUploaderWindow.element.find(".popup-loader").addClass("loading");
                    this.imagesUploaderWindowTreeView.dataSource.read();
                    this.imagesUploaderWindow.element.find(".right-pane, footer").addClass("hidden");
                } else {
                    this.initializeImageUploader(event);
                }
            }
        }).data("kendoWindow");

        this.imagesUploaderWindowSplitter = $("#imagesUploaderWindow .horizontal").kendoSplitter({
            panes: [{
                collapsible: true,
                size: "20%"
            }, {
                collapsible: false
            }]
        }).data("kendoSplitter");

        this.imagesUploaderWindowAddButton = $("#imagesUploaderWindow button[name=addImageToEditor]").kendoButton({
            icon: "save",
            click: async (event) => {
                if (!this.imagesUploaderSender) {
                    kendo.alert("Er is geen HTML editor gevonden waar deze afbeelding toegevoegd kan worden. Sluit aub dit scherm en probeer het opnieuw, of neem contact op met ons.");
                    return;
                }

                const html = `<figure>` +
                    `<picture>` +
                    `<source media="(min-width: 0px)" srcset="${this.generateImagePreviewUrl('jpg')}" type="image/jpeg" />` +
                    `<source media="(min-width: 0px)" srcset="${this.generateImagePreviewUrl('webp')}" type="image/webp" />` +
                    `<img width="100%" height="auto" loading="lazy" src="${this.generateImagePreviewUrl('jpg')}" />` +
                    `</picture>` +
                    `</figure>`;
                if (this.imagesUploaderSender.kendoEditor) {
                    this.imagesUploaderSender.kendoEditor.exec("inserthtml", { value: html });
                }

                if (this.imagesUploaderSender.codeMirror) {
                    const doc = this.imagesUploaderSender.codeMirror.getDoc();
                    const cursor = doc.getCursor();
                    doc.replaceRange(html, cursor);
                }

                if (this.imagesUploaderSender.contentbuilder) {
                    $(this.imagesUploaderSender.contentbuilder.activeElement).replaceWith(html);
                }

                this.imagesUploaderWindow.close();
            }
        });

        // Window for viewing a all generic files and for adding them into an HTML editor.
        this.filesUploaderWindow = $("#filesUploaderWindow").kendoWindow({
            width: "90%",
            height: "90%",
            title: "Bestanden",
            visible: false,
            modal: true,
            actions: ["Maximize", "Close"],
            open: (event) => {
                this.filesUploaderWindowSplitter.resize(true);

                if (this.filesUploaderWindowTreeView) {
                    this.filesUploaderWindow.element.find(".popup-loader").addClass("loading");
                    this.filesUploaderWindowTreeView.dataSource.read();
                    this.filesUploaderWindow.element.find(".right-pane, footer").addClass("hidden");
                } else {
                    this.initializeFileUploader(event);
                }

                this.filesUploaderWindow.element.find("#fileLinkText").val(this.filesUploaderSender.kendoEditor.getSelection().toString());
            }
        }).data("kendoWindow");

        this.filesUploaderWindowSplitter = $("#filesUploaderWindow .horizontal").kendoSplitter({
            panes: [{
                collapsible: true,
                size: "20%"
            }, {
                collapsible: false
            }]
        }).data("kendoSplitter");

        this.filesUploaderWindowAddButton = $("#filesUploaderWindow button[name=addFileToEditor]").kendoButton({
            icon: "save",
            click: async (event) => {
                if (!this.filesUploaderSender) {
                    kendo.alert("Er is geen HTML editor gevonden waar dit bestand toegevoegd kan worden. Sluit aub dit scherm en probeer het opnieuw, of neem contact op met ons.");
                    return;
                }

                const fileUrl = this.generateFilePreviewUrl();
                const html = `<a href="${fileUrl}">${(this.filesUploaderWindow.element.find("#fileLinkText").val() || fileUrl)}</a>`;
                if (this.filesUploaderSender.kendoEditor) {
                    this.filesUploaderSender.kendoEditor.exec("inserthtml", { value: html });
                }

                if (this.filesUploaderSender.codeMirror) {
                    const doc = this.filesUploaderSender.codeMirror.getDoc();
                    const cursor = doc.getCursor();
                    doc.replaceRange(html, cursor);
                }

                if (this.imagesUploaderSender.contentbuilder) {
                    this.imagesUploaderSender.contentbuilder.addHtml(html);
                }

                this.filesUploaderWindow.close();
            }
        });

        // Window for viewing a all generic HTML templates and for adding them into an HTML editor.
        this.templatesUploaderWindow = $("#templatesUploaderWindow").kendoWindow({
            width: "90%",
            height: "90%",
            title: "Templates",
            visible: false,
            modal: true,
            actions: ["Maximize", "Close"],
            open: (event) => {
                this.templatesUploaderWindowSplitter.resize(true);

                if (this.templatesUploaderWindowTreeView) {
                    this.templatesUploaderWindow.element.find(".popup-loader").addClass("loading");
                    this.templatesUploaderWindowTreeView.dataSource.read();
                    this.templatesUploaderWindow.element.find(".right-pane, footer").addClass("hidden");
                } else {
                    this.initializeTemplateUploader(event);
                }
            }
        }).data("kendoWindow");

        this.templatesUploaderWindowSplitter = $("#templatesUploaderWindow .horizontal").kendoSplitter({
            panes: [{
                collapsible: true,
                size: "20%"
            }, {
                collapsible: false
            }]
        }).data("kendoSplitter");

        this.templatesUploaderWindowAddButton = $("#templatesUploaderWindow button[name=addTemplateToEditor]").kendoButton({
            icon: "save",
            click: async (event) => {
                if (!this.templatesUploaderSender) {
                    kendo.alert("Er is geen HTML editor gevonden waar deze template toegevoegd kan worden. Sluit aub dit scherm en probeer het opnieuw, of neem contact op met ons.");
                    return;
                }

                const selectedItem = this.templatesUploaderWindowTreeView.dataItem(this.templatesUploaderWindowTreeView.select());
                if (this.templatesUploaderSender.kendoEditor) {
                    const originalOptions = this.templatesUploaderSender.kendoEditor.options.pasteCleanup;
                    this.templatesUploaderSender.kendoEditor.options.pasteCleanup.none = true;
                    this.templatesUploaderSender.kendoEditor.options.pasteCleanup.span = false;
                    this.templatesUploaderSender.kendoEditor.exec("inserthtml", { value: selectedItem.html });
                    this.templatesUploaderSender.kendoEditor.options.pasteCleanup.none = originalOptions.none;
                    this.templatesUploaderSender.kendoEditor.options.pasteCleanup.span = originalOptions.span;
                }

                if (this.templatesUploaderSender.codeMirror) {
                    const doc = this.templatesUploaderSender.codeMirror.getDoc();
                    const cursor = doc.getCursor();
                    doc.replaceRange(selectedItem.html, cursor);
                }

                if (this.templatesUploaderSender.contentbuilder) {
                    this.templatesUploaderSender.contentbuilder.addHtml(html);
                }

                this.templatesUploaderWindow.close();
            }
        });

        // Some things should not be done if we're in iframe mode.
        if (this.base.settings.iframeMode || this.base.settings.gridViewMode) {
            return;
        }

        /***** NOTE: Only add code below this line that should NOT be executed if the module is loaded inside an iframe *****/
        this.mainWindow = $("#window").kendoWindow({
            title: this.base.settings.moduleName || "Modulenaam",
            visible: true,
            actions: ["refresh"]
        }).data("kendoWindow").maximize().open();
        this.mainWindow.wrapper.addClass("main-window");

        this.mainWindow.wrapper.find(".k-i-refresh").parent().click(this.base.onMainRefreshButtonClick.bind(this.base));
    }

    /**
     * Initializes all components in the image uploader window.
     * @param {any} event The event of the open function of the imageUploaderWindow.
     */
    initializeImageUploader(event) {
        // Setup the tree view.
        this.imagesUploaderWindowTreeView = $("#imagesUploaderTreeView").kendoTreeView({
            dragAndDrop: false,
            dataSource: {
                transport: {
                    read: {
                        url: `${this.base.settings.serviceRoot}/GET_ITEM_FILES_AND_DIRECTORIES?rootId=${encodeURIComponent(this.base.settings.imagesRootId)}`,
                        dataType: "json"
                    }
                },
                schema: {
                    model: {
                        id: "id",
                        hasChildren: "childrenCount"
                    }
                }
            },
            dataBound: (dataBoundEvent) => {
                if (!dataBoundEvent.node) {
                    // If the node property is undefined, it means this is the initial load, so we want to load the state of the tree view.
                    this.loadUploaderWindowTreeViewState("images", this.imagesUploaderWindowTreeView, this.imagesUploaderWindowTreeView.dataSource.data(), true);
                } else {
                    // If the node is not undefined, it means the user is expanding one of the nodes, so we want to save the state of the tree view.
                    this.saveUploaderWindowTreeViewState("images", this.imagesUploaderWindowTreeView);
                }

                this.imagesUploaderWindow.element.find(".popup-loader").removeClass("loading");
            },
            change: this.updateImagePreview.bind(this),
            collapse: (collapseEvent) => {
                this.base.onTreeViewCollapseItem(collapseEvent);
                // Timeout because the collapse event gets triggered before the collapse happens, but we need to save the state after it happened.
                setTimeout(() => this.saveUploaderWindowTreeViewState("images", this.imagesUploaderWindowTreeView), 100);
            },
            expand: (expandEvent) => {
                this.base.onTreeViewExpandItem(expandEvent);
            },
            dataValueField: "id",
            dataTextField: "name"
        }).data("kendoTreeView");

        // Setup the number fields.
        event.sender.element.find("#preferredWidth, #preferredHeight").kendoNumericTextBox({
            culture: "nl-NL",
            decimals: 0,
            format: "#",
            change: this.updateImagePreview.bind(this)
        });

        // Setup the dropdowns.
        event.sender.element.find("#resizeMode").kendoDropDownList({
            change: (resizeModeChangeEvent) => {
                const value = resizeModeChangeEvent.sender.value();
                event.sender.element.find(".item.anchorPosition").toggleClass("hidden", value !== "crop" && value !== "fill");
                this.updateImagePreview();
            }
        });

        event.sender.element.find("#anchorPosition").kendoDropDownList({
            change: this.updateImagePreview.bind(this)
        });

        const fileUploader = event.sender.element.find("#newImageUpload");
        fileUploader.change((fileChangeEvent) => {
            const allowedExtensions = [".jpg", ".jpeg", ".png", ".bmp", ".gif", ".svg", ".webp"];
            const selectedFiles = [...fileChangeEvent.currentTarget.files];
            if (selectedFiles.length === 0) {
                // No files selected, don't do anything.
                return;
            }

            const invalidFiles = selectedFiles.filter(file => allowedExtensions.filter(extension => file.name.indexOf(extension) === file.name.length - extension.length).length === 0);
            if (invalidFiles.length > 0) {
                kendo.alert(`U heeft 1 of meer ongeldige bestanden toegevoegd. Alleen de volgende extensies worden toegestaan: ${allowedExtensions.join(", ")}`);
                return;
            }

            const loader = this.imagesUploaderWindow.element.find(".popup-loader").addClass("loading");
            try {
                const selectedItem = this.imagesUploaderWindowTreeView.dataItem(fileUploader.data("source") === "contextMenu" ? this.imagesUploaderWindowTreeViewContextMenuTarget : this.imagesUploaderWindowTreeView.select());
                const itemId = selectedItem ? selectedItem.id : this.base.settings.imagesRootId;
                const promises = [];
                for (let file of selectedFiles) {
                    const formData = new FormData();
                    formData.append(file.name, file);

                    promises.push(Wiser2.api({
                        url: `${this.base.settings.wiserApiRoot}items/${encodeURIComponent(itemId)}/upload?propertyName=globalFile&useTinyPng=false`,
                        method: "POST",
                        processData: false,
                        contentType: false,
                        data: formData
                    }));
                }

                Promise.all(promises).then((results) => {
                    this.base.notification.show({ message: "Afbeelding(en) succesvol toegevoegd" }, "success");
                    this.imagesUploaderWindowTreeView.dataSource.read();
                    // Clear the file input so that the user can upload the same file again if they want to. If we don't clear it, then the change event won't be triggered if they upload the same file twice in a row.
                    fileUploader.val("");
                }).catch((error) => {
                    console.error(error);
                    kendo.alert("Er is iets fout gegaan. Probeer het aub opnieuw of neem contact met ons op.");
                    loader.removeClass("loading");
                });
            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan. Probeer het aub opnieuw of neem contact met ons op.");
                loader.removeClass("loading");
            }
        });

        // Setup the context menu for the tree view.
        this.imagesUploaderWindowTreeViewContextMenu = $("#imagesUploaderTreeViewContextMenu").kendoContextMenu({
            target: "#imagesUploaderTreeView",
            filter: ".k-in",
            open: (openEvent) => {
                this.imagesUploaderWindowTreeViewContextMenuTarget = $(openEvent.target).closest("li.k-item");
                const isDirectory = this.imagesUploaderWindowTreeView.dataItem(this.imagesUploaderWindowTreeViewContextMenuTarget).isDirectory;
                openEvent.sender.element.find("[data-action='add-directory'], [data-action='add-image']").toggleClass("hidden", !isDirectory);
            },
            select: (selectEvent) => {
                const loader = this.imagesUploaderWindow.element.find(".popup-loader").addClass("loading");

                try {
                    const selectedItem = this.imagesUploaderWindowTreeView.dataItem(this.imagesUploaderWindowTreeViewContextMenuTarget);
                    const selectedAction = $(selectEvent.item).data("action");
                    switch (selectedAction) {
                        case "delete":
                            if (selectedItem.isDirectory) {
                                Wiser2.showConfirmDialog(`Weet u zeker dat u de map '${selectedItem.name}' wilt verwijderen? Alle afbeeldingen in deze map zullen dan ook verwijderd worden.`).then(() => {
                                    this.base.deleteItem(selectedItem.id, "filedirectory").then(() => {
                                        this.imagesUploaderWindowTreeView.remove(this.imagesUploaderWindowTreeViewContextMenuTarget);
                                        this.base.notification.show({ message: "Map succesvol verwijderd" }, "success");
                                        loader.removeClass("loading");
                                    });
                                }).catch(() => { loader.removeClass("loading"); });
                            } else {
                                Wiser2.showConfirmDialog(`Weet u zeker dat u de afbeelding '${selectedItem.name}' wilt verwijderen?`).then(() => {
                                    Wiser2.api({
                                        url: `${this.base.settings.wiserApiRoot}items/${encodeURIComponent(selectedItem.itemId)}/files/${encodeURIComponent(selectedItem.plainId)}`,
                                        method: "DELETE",
                                        contentType: "application/json",
                                        dataType: "JSON"
                                    }).then(() => {
                                        this.imagesUploaderWindowTreeView.remove(this.imagesUploaderWindowTreeViewContextMenuTarget);
                                        this.base.notification.show({ message: "Afbeelding succesvol verwijderd" }, "success");
                                        loader.removeClass("loading");
                                    }, (error) => {
                                        console.error(error);
                                        loader.removeClass("loading");
                                        kendo.alert("Er is iets fout gegaan. Probeer het a.u.b. opnieuw of neem contact op met ons.");
                                    });
                                }).catch(() => { loader.removeClass("loading"); });
                            }
                            break;
                        case "rename":
                            kendo.prompt("Geef een nieuwe naam op", selectedItem.name).then((newName) => {
                                if (selectedItem.isDirectory) {
                                    this.base.updateItem(selectedItem.itemId, [], null, false, newName, false, true, "filedirectory").then(() => {
                                        this.imagesUploaderWindowTreeView.text(this.imagesUploaderWindowTreeViewContextMenuTarget, newName);
                                        this.base.notification.show({ message: "Mapnaam is succesvol gewijzigd" }, "success");
                                        loader.removeClass("loading");
                                    }).catch((error) => {
                                        console.error(error);
                                        loader.removeClass("loading");
                                        kendo.alert("Er is iets fout gegaan. Probeer het a.u.b. opnieuw of neem contact op met ons.");
                                    });
                                } else {
                                    Wiser2.api({
                                        url: `${this.base.settings.wiserApiRoot}items/${encodeURIComponent(selectedItem.itemId)}/files/${encodeURIComponent(selectedItem.plainId)}/rename/${encodeURIComponent(newName)}`,
                                        method: "PUT",
                                        contentType: "application/json",
                                        dataType: "JSON"
                                    }).then(() => {
                                        this.imagesUploaderWindowTreeView.dataSource.read();
                                        this.base.notification.show({ message: "Bestandsnaam succesvol gewijzigd" }, "success");
                                        loader.removeClass("loading");
                                    }).catch((error) => {
                                        console.error(error);
                                        loader.removeClass("loading");
                                        kendo.alert("Er is iets fout gegaan. Probeer het a.u.b. opnieuw of neem contact op met ons.");
                                    });
                                }
                            }).fail(() => { loader.removeClass("loading"); });
                            break;
                        case "add-directory":
                            if (!selectedItem.isDirectory) {
                                break;
                            }

                            this.createFileDirectory(selectedItem.id, this.imagesUploaderWindow, this.imagesUploaderWindowTreeView);
                            break;
                        case "add-image":
                            if (!selectedItem.isDirectory) {
                                break;
                            }

                            fileUploader.data("source", "contextMenu");
                            fileUploader.trigger("click");
                            break;
                        default:
                            loader.removeClass("loading");
                            kendo.alert("Onbekende actie, probeer het a.u.b. opnieuw.");
                            break;
                    }
                } catch (exception) {
                    console.error(exception);
                    loader.removeClass("loading");
                    kendo.alert("Er is iets fout gegaan. Probeer het a.u.b. opnieuw of neem contact op met ons.");
                }
            }
        }).data("kendoContextMenu");

        // Setup the buttons.
        event.sender.element.find("#addNewImageButton").kendoButton({
            click: () => {
                fileUploader.data("source", "button");
                fileUploader.trigger("click");
            },
            icon: "image-insert"
        });

        event.sender.element.find("#addNewImageDirectoryButton").kendoButton({
            click: () => {
                const selectedItemTreeView = this.imagesUploaderWindowTreeView.select();
                if (!selectedItemTreeView.length) {
                    this.createFileDirectory(this.base.settings.imagesRootId, this.imagesUploaderWindow, this.imagesUploaderWindowTreeView);
                } else {
                    this.createFileDirectory(this.imagesUploaderWindowTreeView.dataItem(selectedItemTreeView).id, this.imagesUploaderWindow, this.imagesUploaderWindowTreeView);
                }
            },
            icon: "folder-add"
        });
    }

    /**
     * Initializes all components in the file uploader window.
     * @param {any} event The event of the open function of the fileUploaderWindow.
     */
    initializeFileUploader(event) {
        // Setup the tree view.
        this.filesUploaderWindowTreeView = $("#filesUploaderTreeView").kendoTreeView({
            dragAndDrop: false,
            dataSource: {
                transport: {
                    read: {
                        url: `${this.base.settings.serviceRoot}/GET_ITEM_FILES_AND_DIRECTORIES?rootId=${encodeURIComponent(this.base.settings.filesRootId)}`,
                        dataType: "json"
                    }
                },
                schema: {
                    model: {
                        id: "id",
                        hasChildren: "childrenCount"
                    }
                }
            },
            dataBound: (dataBoundEvent) => {
                if (!dataBoundEvent.node) {
                    // If the node property is undefined, it means this is the initial load, so we want to load the state of the tree view.
                    this.loadUploaderWindowTreeViewState("files", this.filesUploaderWindowTreeView, this.filesUploaderWindowTreeView.dataSource.data(), true);
                } else {
                    // If the node is not undefined, it means the user is expanding one of the nodes, so we want to save the state of the tree view.
                    this.saveUploaderWindowTreeViewState("files", this.filesUploaderWindowTreeView);
                }

                this.filesUploaderWindow.element.find(".popup-loader").removeClass("loading");
            },
            change: this.updateFilePreview.bind(this),
            collapse: (collapseEvent) => {
                this.base.onTreeViewCollapseItem(collapseEvent);
                // Timeout because the collapse event gets triggered before the collapse happens, but we need to save the state after it happened.
                setTimeout(() => this.saveUploaderWindowTreeViewState("files", this.filesUploaderWindowTreeView), 100);
            },
            expand: (expandEvent) => {
                this.base.onTreeViewExpandItem(expandEvent);
            },
            dataValueField: "id",
            dataTextField: "name"
        }).data("kendoTreeView");

        const fileUploader = event.sender.element.find("#newFileUpload");
        fileUploader.change((fileChangeEvent) => {
            const selectedFiles = [...fileChangeEvent.currentTarget.files];
            if (selectedFiles.length === 0) {
                // No files selected, don't do anything.
                return;
            }

            const loader = this.filesUploaderWindow.element.find(".popup-loader").addClass("loading");
            try {
                const selectedItem = this.filesUploaderWindowTreeView.dataItem(fileUploader.data("source") === "contextMenu" ? this.filesUploaderWindowTreeViewContextMenuTarget : this.filesUploaderWindowTreeView.select());
                const itemId = selectedItem ? selectedItem.id : this.base.settings.filesRootId;
                const promises = [];
                for (let file of selectedFiles) {
                    const formData = new FormData();
                    formData.append(file.name, file);

                    promises.push(Wiser2.api({
                        url: `${this.base.settings.wiserApiRoot}items/${encodeURIComponent(itemId)}/upload?propertyName=globalFile&useTinyPng=false`,
                        method: "POST",
                        processData: false,
                        contentType: false,
                        data: formData
                    }));
                }

                Promise.all(promises).then((results) => {
                    this.base.notification.show({ message: "Bestand(en) succesvol toegevoegd" }, "success");
                    this.filesUploaderWindowTreeView.dataSource.read();
                    // Clear the file input so that the user can upload the same file again if they want to. If we don't clear it, then the change event won't be triggered if they upload the same file twice in a row.
                    fileUploader.val("");
                }).catch((error) => {
                    console.error(error);
                    kendo.alert("Er is iets fout gegaan. Probeer het aub opnieuw of neem contact met ons op.");
                    loader.removeClass("loading");
                });
            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan. Probeer het aub opnieuw of neem contact met ons op.");
                loader.removeClass("loading");
            }
        });

        // Setup the context menu for the tree view.
        this.filesUploaderWindowTreeViewContextMenu = $("#filesUploaderTreeViewContextMenu").kendoContextMenu({
            target: "#filesUploaderTreeView",
            filter: ".k-in",
            open: (openEvent) => {
                this.filesUploaderWindowTreeViewContextMenuTarget = $(openEvent.target).closest("li.k-item");
                const isDirectory = this.filesUploaderWindowTreeView.dataItem(this.filesUploaderWindowTreeViewContextMenuTarget).isDirectory;
                openEvent.sender.element.find("[data-action='add-directory'], [data-action='add-file']").toggleClass("hidden", !isDirectory);
            },
            select: (selectEvent) => {
                const loader = this.filesUploaderWindow.element.find(".popup-loader").addClass("loading");

                try {
                    const selectedItem = this.filesUploaderWindowTreeView.dataItem(this.filesUploaderWindowTreeViewContextMenuTarget);
                    const selectedAction = $(selectEvent.item).data("action");
                    switch (selectedAction) {
                        case "delete":
                            if (selectedItem.isDirectory) {
                                Wiser2.showConfirmDialog(`Weet u zeker dat u de map '${selectedItem.name}' wilt verwijderen? Alle bestanden in deze map zullen dan ook verwijderd worden.`).then(() => {
                                    this.base.deleteItem(selectedItem.id, "filedirectory").then(() => {
                                        this.filesUploaderWindowTreeView.remove(this.filesUploaderWindowTreeViewContextMenuTarget);
                                        this.base.notification.show({ message: "Map succesvol verwijderd" }, "success");
                                        loader.removeClass("loading");
                                    });
                                }).catch(() => { loader.removeClass("loading"); });
                            } else {
                                Wiser2.showConfirmDialog(`Weet u zeker dat u het bestand '${selectedItem.name}' wilt verwijderen?`).then(() => {
                                    Wiser2.api({
                                        url: `${this.base.settings.wiserApiRoot}items/${encodeURIComponent(selectedItem.itemId)}/files/${encodeURIComponent(selectedItem.plainId)}`,
                                        method: "DELETE",
                                        contentType: "application/json",
                                        dataType: "JSON"
                                    }).then(() => {
                                        this.filesUploaderWindowTreeView.remove(this.filesUploaderWindowTreeViewContextMenuTarget);
                                        this.base.notification.show({ message: "Bestand succesvol verwijderd" }, "success");
                                        loader.removeClass("loading");
                                    }, (error) => {
                                        console.error(error);
                                        loader.removeClass("loading");
                                        kendo.alert("Er is iets fout gegaan. Probeer het a.u.b. opnieuw of neem contact op met ons.");
                                    });
                                }).catch(() => { loader.removeClass("loading"); });
                            }
                            break;
                        case "rename":
                            kendo.prompt("Geef een nieuwe naam op", selectedItem.name).then((newName) => {
                                if (selectedItem.isDirectory) {
                                    this.base.updateItem(selectedItem.itemId, [], null, false, newName, false, true, "filedirectory").then(() => {
                                        this.filesUploaderWindowTreeView.text(this.filesUploaderWindowTreeViewContextMenuTarget, newName);
                                        this.base.notification.show({ message: "Mapnaam is succesvol gewijzigd" }, "success");
                                        loader.removeClass("loading");
                                    }).catch((error) => {
                                        console.error(error);
                                        loader.removeClass("loading");
                                        kendo.alert("Er is iets fout gegaan. Probeer het a.u.b. opnieuw of neem contact op met ons.");
                                    });
                                } else {
                                    Wiser2.api({
                                        url: `${this.base.settings.wiserApiRoot}items/${encodeURIComponent(selectedItem.itemId)}/files/${encodeURIComponent(selectedItem.plainId)}/rename/${encodeURIComponent(newName)}`,
                                        method: "PUT",
                                        contentType: "application/json",
                                        dataType: "JSON"
                                    }).then(() => {
                                        this.filesUploaderWindowTreeView.dataSource.read();
                                        this.base.notification.show({ message: "Bestandsnaam succesvol gewijzigd" }, "success");
                                        loader.removeClass("loading");
                                    }, (error) => {
                                        console.error(error);
                                        loader.removeClass("loading");
                                        kendo.alert("Er is iets fout gegaan. Probeer het a.u.b. opnieuw of neem contact op met ons.");
                                    });
                                }
                            }).fail(() => { loader.removeClass("loading"); });
                            break;
                        case "add-directory":
                            if (!selectedItem.isDirectory) {
                                break;
                            }

                            this.createFileDirectory(selectedItem.id, this.filesUploaderWindow, this.filesUploaderWindowTreeView);
                            break;
                        case "add-file":
                            if (!selectedItem.isDirectory) {
                                break;
                            }

                            fileUploader.data("source", "contextMenu");
                            fileUploader.trigger("click");
                            break;
                        default:
                            loader.removeClass("loading");
                            kendo.alert("Onbekende actie, probeer het a.u.b. opnieuw.");
                            break;
                    }
                } catch (exception) {
                    console.error(exception);
                    loader.removeClass("loading");
                    kendo.alert("Er is iets fout gegaan. Probeer het a.u.b. opnieuw of neem contact op met ons.");
                }
            }
        }).data("kendoContextMenu");

        // Setup the buttons.
        event.sender.element.find("#addNewFileButton").kendoButton({
            click: () => {
                fileUploader.data("source", "button");
                fileUploader.trigger("click");
            },
            icon: "file-add"
        });

        event.sender.element.find("#addNewFileDirectoryButton").kendoButton({
            click: () => {
                const selectedItemTreeView = this.filesUploaderWindowTreeView.select();
                if (!selectedItemTreeView.length) {
                    this.createFileDirectory(this.base.settings.filesRootId, this.filesUploaderWindow, this.filesUploaderWindowTreeView);
                } else {
                    this.createFileDirectory(this.filesUploaderWindowTreeView.dataItem(selectedItemTreeView).id, this.filesUploaderWindow, this.filesUploaderWindowTreeView);
                }
            },
            icon: "folder-add"
        });
    }

    /**
     * Initializes all components in the template uploader window.
     * @param {any} event The event of the open function of the imageUploaderWindow.
     */
    initializeTemplateUploader(event) {
        // Setup the tree view.
        this.templatesUploaderWindowTreeView = $("#templatesUploaderTreeView").kendoTreeView({
            dragAndDrop: false,
            dataSource: {
                transport: {
                    read: {
                        url: `${this.base.settings.serviceRoot}/GET_ITEM_FILES_AND_DIRECTORIES?rootId=${encodeURIComponent(this.base.settings.templatesRootId)}`,
                        dataType: "json"
                    }
                },
                schema: {
                    model: {
                        id: "id",
                        hasChildren: "childrenCount"
                    }
                }
            },
            dataBound: (dataBoundEvent) => {
                if (!dataBoundEvent.node) {
                    // If the node property is undefined, it means this is the initial load, so we want to load the state of the tree view.
                    this.loadUploaderWindowTreeViewState("templates", this.templatesUploaderWindowTreeView, this.templatesUploaderWindowTreeView.dataSource.data(), true);
                } else {
                    // If the node is not undefined, it means the user is expanding one of the nodes, so we want to save the state of the tree view.
                    this.saveUploaderWindowTreeViewState("templates", this.templatesUploaderWindowTreeView);
                }

                this.templatesUploaderWindow.element.find(".popup-loader").removeClass("loading");
            },
            change: this.updateTemplatePreview.bind(this),
            collapse: (collapseEvent) => {
                this.base.onTreeViewCollapseItem(collapseEvent);
                // Timeout because the collapse event gets triggered before the collapse happens, but we need to save the state after it happened.
                setTimeout(() => this.saveUploaderWindowTreeViewState("templates", this.templatesUploaderWindowTreeView), 100);
            },
            expand: (expandEvent) => {
                this.base.onTreeViewExpandItem(expandEvent);
            },
            dataValueField: "id",
            dataTextField: "name"
        }).data("kendoTreeView");

        // Setup the number fields.
        event.sender.element.find("#preferredWidth, #preferredHeight").kendoNumericTextBox({
            culture: "nl-NL",
            decimals: 0,
            format: "#",
            change: this.updateImagePreview.bind(this)
        });

        const fileUploader = event.sender.element.find("#newTemplateUpload");
        fileUploader.change((fileChangeEvent) => {
            const allowedExtensions = [".htm", ".html"];
            const selectedFiles = [...fileChangeEvent.currentTarget.files];
            if (selectedFiles.length === 0) {
                // No files selected, don't do anything.
                return;
            }

            const invalidFiles = selectedFiles.filter(file => allowedExtensions.filter(extension => file.name.indexOf(extension) === file.name.length - extension.length).length === 0);
            if (invalidFiles.length > 0) {
                kendo.alert(`U heeft 1 of meer ongeldige bestanden toegevoegd. Alleen de volgende extensies worden toegestaan: ${allowedExtensions.join(", ")}`);
                return;
            }

            const loader = this.templatesUploaderWindow.element.find(".popup-loader").addClass("loading");
            try {
                const selectedItem = this.templatesUploaderWindowTreeView.dataItem(fileUploader.data("source") === "contextMenu" ? this.templatesUploaderWindowTreeViewContextMenuTarget : this.templatesUploaderWindowTreeView.select());
                const itemId = selectedItem ? selectedItem.id : this.base.settings.templatesRootId;
                const promises = [];
                for (let file of selectedFiles) {
                    const formData = new FormData();
                    formData.append(file.name, file);

                    promises.push(Wiser2.api({
                        url: `${this.base.settings.wiserApiRoot}items/${encodeURIComponent(itemId)}/upload?propertyName=globalFile&useTinyPng=false`,    
                        method: "POST",
                        processData: false,
                        contentType: false,
                        data: formData 
                    }));
                }

                Promise.all(promises).then((results) => {
                    this.base.notification.show({ message: "Template(s) succesvol toegevoegd" }, "success");
                    this.templatesUploaderWindowTreeView.dataSource.read();
                    // Clear the file input so that the user can upload the same file again if they want to. If we don't clear it, then the change event won't be triggered if they upload the same file twice in a row.
                    fileUploader.val("");
                }).catch((error) => {
                    console.error(error);
                    kendo.alert("Er is iets fout gegaan. Probeer het aub opnieuw of neem contact met ons op.");
                    loader.removeClass("loading");
                });
            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan. Probeer het aub opnieuw of neem contact met ons op.");
                loader.removeClass("loading");
            }
        });

        // Setup the context menu for the tree view.
        this.templatesUploaderWindowTreeViewContextMenu = $("#templatesUploaderTreeViewContextMenu").kendoContextMenu({
            target: "#templatesUploaderTreeView",
            filter: ".k-in",
            open: (openEvent) => {
                this.templatesUploaderWindowTreeViewContextMenuTarget = $(openEvent.target).closest("li.k-item");
                const isDirectory = this.templatesUploaderWindowTreeView.dataItem(this.templatesUploaderWindowTreeViewContextMenuTarget).isDirectory;
                openEvent.sender.element.find("[data-action='add-directory'], [data-action='add-template']").toggleClass("hidden", !isDirectory);
            },
            select: (selectEvent) => {
                const loader = this.templatesUploaderWindow.element.find(".popup-loader").addClass("loading");

                try {
                    const selectedItem = this.templatesUploaderWindowTreeView.dataItem(this.templatesUploaderWindowTreeViewContextMenuTarget);
                    const selectedAction = $(selectEvent.item).data("action");
                    switch (selectedAction) {
                        case "delete":
                            if (selectedItem.isDirectory) {
                                Wiser2.showConfirmDialog(`Weet u zeker dat u de map '${selectedItem.name}' wilt verwijderen? Alle templates in deze map zullen dan ook verwijderd worden.`).then(() => {
                                    this.base.deleteItem(selectedItem.id, "filedirectory").then(() => {
                                        this.templatesUploaderWindowTreeView.remove(this.templatesUploaderWindowTreeViewContextMenuTarget);
                                        this.base.notification.show({ message: "Map succesvol verwijderd" }, "success");
                                        loader.removeClass("loading");
                                    });
                                }).catch(() => { loader.removeClass("loading"); });
                            } else {
                                Wiser2.showConfirmDialog(`Weet u zeker dat u de template '${selectedItem.name}' wilt verwijderen?`).then(() => {
                                    Wiser2.api({
                                        url: `${this.base.settings.wiserApiRoot}items/${encodeURIComponent(selectedItem.itemId)}/files/${encodeURIComponent(selectedItem.plainId)}`,
                                        method: "DELETE",
                                        contentType: "application/json",
                                        dataType: "JSON"
                                    }).then(() => {
                                        this.templatesUploaderWindowTreeView.remove(this.templatesUploaderWindowTreeViewContextMenuTarget);
                                        this.base.notification.show({ message: "Template succesvol verwijderd" }, "success");
                                        loader.removeClass("loading");
                                    }, (error) => {
                                        console.error(error);
                                        loader.removeClass("loading");
                                        kendo.alert("Er is iets fout gegaan. Probeer het a.u.b. opnieuw of neem contact op met ons.");
                                    });
                                }).catch(() => { loader.removeClass("loading"); });
                            }
                            break;
                        case "rename":
                            kendo.prompt("Geef een nieuwe naam op", selectedItem.name).then((newName) => {
                                if (selectedItem.isDirectory) {
                                    this.base.updateItem(selectedItem.itemId, [], null, false, newName, false, true, "filedirectory").then(() => {
                                        this.templatesUploaderWindowTreeView.text(this.templatesUploaderWindowTreeViewContextMenuTarget, newName);
                                        this.base.notification.show({ message: "Mapnaam is succesvol gewijzigd" }, "success");
                                        loader.removeClass("loading");
                                    }).catch((error) => {
                                        console.error(error);
                                        loader.removeClass("loading");
                                        kendo.alert("Er is iets fout gegaan. Probeer het a.u.b. opnieuw of neem contact op met ons.");
                                    });
                                } else {
                                    Wiser2.api({
                                        url: `${this.base.settings.wiserApiRoot}items/${encodeURIComponent(selectedItem.itemId)}/files/${encodeURIComponent(selectedItem.plainId)}/rename/${encodeURIComponent(newName)}`,
                                        method: "PUT",
                                        contentType: "application/json",
                                        dataType: "JSON"
                                    }).then(() => {
                                        this.templatesUploaderWindowTreeView.dataSource.read();
                                        this.base.notification.show({ message: "Bestandsnaam succesvol gewijzigd" }, "success");
                                        loader.removeClass("loading");
                                    }, (error) => {
                                        console.error(error);
                                        loader.removeClass("loading");
                                        kendo.alert("Er is iets fout gegaan. Probeer het a.u.b. opnieuw of neem contact op met ons.");
                                    });
                                }
                            }).fail(() => { loader.removeClass("loading"); });
                            break;
                        case "add-directory":
                            if (!selectedItem.isDirectory) {
                                break;
                            }

                            this.createFileDirectory(selectedItem.id, this.templatesUploaderWindow, this.templatesUploaderWindowTreeView);
                            break;
                        case "add-image":
                            if (!selectedItem.isDirectory) {
                                break;
                            }

                            fileUploader.data("source", "contextMenu");
                            fileUploader.trigger("click");
                            break;
                        default:
                            loader.removeClass("loading");
                            kendo.alert("Onbekende actie, probeer het a.u.b. opnieuw.");
                            break;
                    }
                } catch (exception) {
                    console.error(exception);
                    loader.removeClass("loading");
                    kendo.alert("Er is iets fout gegaan. Probeer het a.u.b. opnieuw of neem contact op met ons.");
                }
            }
        }).data("kendoContextMenu");

        // Setup the buttons.
        event.sender.element.find("#addNewTemplateButton").kendoButton({
            click: () => {
                fileUploader.data("source", "button");
                fileUploader.trigger("click");
            },
            icon: "document-insert"
        });

        event.sender.element.find("#addNewTemplateDirectoryButton").kendoButton({
            click: () => {
                const selectedItemTreeView = this.templatesUploaderWindowTreeView.select();
                if (!selectedItemTreeView.length) {
                    this.createFileDirectory(this.base.settings.templatesRootId, this.templatesUploaderWindow, this.templatesUploaderWindowTreeView);
                } else {
                    this.createFileDirectory(this.templatesUploaderWindowTreeView.dataItem(selectedItemTreeView).id, this.templatesUploaderWindow, this.templatesUploaderWindowTreeView);
                }
            },
            icon: "folder-add"
        });
    }

    createFileDirectory(parentId, window, treeView) {
        try {
            kendo.prompt("Vul een naam in").then((name) => {
                // Using then instead of async/await, because kendo can't handle async events.
                this.base.createItem("filedirectory", parentId, name, null, [], true).then((createItemResult) => {
                    if (!createItemResult) {
                        kendo.alert("Er iets iets fout gegaan tijdens het aanmaken van de map. Probeer het a.u.b. opnieuw of neem contact op met ons.");
                    } else {
                        this.base.notification.show({ message: "Map succesvol aangemaakt" }, "success");
                        window.element.find(".popup-loader").addClass("loading");
                        treeView.dataSource.read();
                    }
                });
            });
        } catch (exception) {
            console.error(exception);
            kendo.alert("Er iets iets fout gegaan tijdens het aanmaken van de map. Probeer het a.u.b. opnieuw of neem contact op met ons.");
        }
    }

    saveUploaderWindowTreeViewState(type, treeView) {
        if (this.uploaderWindowTreeViewStateLoading) {
            // Don't save the state while we're loading a state, otherwise things will get messed up.
            return;
        }

        this.uploaderWindowTreeViewStates[type] = {};
        treeView.element.find(".k-item").each((index, element) => {
            const item = treeView.dataItem(element);
            if (item && item.expanded) {
                this.uploaderWindowTreeViewStates[type][item.plainId] = true;
            }
        });
    }

    async loadUploaderWindowTreeViewState(type, treeView, data, initial) {
        if (!this.uploaderWindowTreeViewStates || !this.uploaderWindowTreeViewStates[type] || !data) {
            return;
        }

        if (initial) {
            this.uploaderWindowTreeViewStateLoading = true;
        }

        for (let i = 0; i < data.length; i++) {
            if (this.uploaderWindowTreeViewStates[type][data[i].plainId]) {
                await data[i].load();
                treeView.expand(treeView.findByUid(data[i].uid));
            }
            if (data[i].items && data[i].items.length) {
                await this.loadUploaderWindowTreeViewState(type, treeView, data[i].items, false);
            }
        }

        if (initial) {
            this.uploaderWindowTreeViewStateLoading = false;
        }
    }

    /**
     * Generates the URL for the image preview for the imagesUploaderWindow.
     * @param ext The extension of the image file
     * @returns {string} The URL for the preview image.
     */
    generateImagePreviewUrl(ext) {
        const selectedItem = this.imagesUploaderWindowTreeView.dataItem(this.imagesUploaderWindowTreeView.select());
        let resizeMode = this.imagesUploaderWindow.element.find("#resizeMode").data("kendoDropDownList").value() || "normal";
        if (resizeMode === "crop" || resizeMode === "fill") {
            const anchorPosition = this.imagesUploaderWindow.element.find("#anchorPosition").data("kendoDropDownList").value();
            resizeMode += `-${anchorPosition || "center"}`;
        }

        const width = this.imagesUploaderWindow.element.find("#preferredWidth").val() || 0;
        const height = this.imagesUploaderWindow.element.find("#preferredHeight").val() || 0;
        return `${this.base.settings.mainDomain}/image/wiser2/${selectedItem.plainId}/direct/${selectedItem.propertyName}/${resizeMode}/${width}/${height}/${selectedItem.name}.${ext}`;
    }

    /**
     * Generates the URL for the file preview for the imagesUploaderWindow.
     * @returns {string} The URL for the preview image.
     */
    generateFilePreviewUrl() {
        const selectedItem = this.filesUploaderWindowTreeView.dataItem(this.filesUploaderWindowTreeView.select());
        let result = `${this.base.settings.mainDomain}/file/wiser2/${selectedItem.plainId}/direct/${selectedItem.propertyName}/${selectedItem.name}`;
        return result.replace("//file", "/file");
    }

    /**
     * Updates the preview image URL with all selected options.
     * Hides the preview if no image is selected in the tree view.
     */
    updateImagePreview() {
        const container = this.imagesUploaderWindow.element.find(".right-pane");
        const footer = this.imagesUploaderWindow.element.find("footer");
        const selectedItem = this.imagesUploaderWindowTreeView.dataItem(this.imagesUploaderWindowTreeView.select());
        const anchorElement = this.imagesUploaderWindow.element.find(".image-preview a.image-preview-link");
        const imagePreviewElement = anchorElement.find("img");
        if (selectedItem.isDirectory) {
            container.addClass("hidden");
            footer.addClass("hidden");
            return;
        }

        container.removeClass("hidden");
        footer.removeClass("hidden");

        const newImageUrl = this.generateImagePreviewUrl();
        anchorElement.attr("href", newImageUrl);
        imagePreviewElement.attr("src", newImageUrl);
    }

    /**
     * Updates the preview file URL with all selected options.
     * Hides the preview if no file is selected in the tree view.
     */
    updateFilePreview() {
        const container = this.filesUploaderWindow.element.find(".right-pane");
        const footer = this.filesUploaderWindow.element.find("footer");
        const selectedItem = this.filesUploaderWindowTreeView.dataItem(this.filesUploaderWindowTreeView.select());
        const anchorElement = this.filesUploaderWindow.element.find(".file-preview a.file-preview-link");
        if (selectedItem.isDirectory) {
            container.addClass("hidden");
            footer.addClass("hidden");
            return;
        }

        container.removeClass("hidden");
        footer.removeClass("hidden");

        const newFileUrl = this.generateFilePreviewUrl();
        anchorElement.attr("href", newFileUrl);
        this.filesUploaderWindow.element.find("#fileLinkText").val(this.filesUploaderSender.kendoEditor.getSelection().toString() || selectedItem.name)
    }

    /**
     * Updates the preview template.
     * Hides the preview if no template is selected in the tree view.
     */
    updateTemplatePreview() {
        const container = this.templatesUploaderWindow.element.find(".right-pane");
        const footer = this.templatesUploaderWindow.element.find("footer");
        const selectedItem = this.templatesUploaderWindowTreeView.dataItem(this.templatesUploaderWindowTreeView.select());
        if (selectedItem.isDirectory) {
            container.addClass("hidden");
            footer.addClass("hidden");
            return;
        }

        container.removeClass("hidden");
        footer.removeClass("hidden");

        const iframeElement = this.templatesUploaderWindow.element.find(".template-preview .template-preview-iframe");
        let iframe = iframeElement[0];
        iframe = iframe.contentWindow || (iframe.contentDocument.document || iframe.contentDocument);

        iframe.document.open();
        iframe.document.write(selectedItem.html);
        iframe.document.close();
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
     */
    async loadItemInWindow(isNewItem, itemId, encryptedItemId, entityType, title, showTitleField, senderGrid, fieldOptions, linkId, windowTitle = null, kendoComponent = null) {
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
                actions: ["Verwijderen", "Verversen", "Close"],
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
                        Wiser2.showConfirmDialog("Weet u zeker dat u wilt annuleren en gewijzigde of ingevoerde gegevens wilt verwijderen?").then(closeFunction.bind(this));
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
                        this.base.getItemHtml(encryptedItemId, entityType, windowId, linkId)
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

                    currentItemTabStrip.element.find("> ul > li .addedFromDatabase").each((index, element) => {
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
                loadPopupContents(previouslySelectedTab);
            });
            currentItemWindow.wrapper.find(".k-i-verwijderen").parent().click(this.onDeleteItemPopupClick.bind(this));

            currentItemWindow.element.find(".editMenu .undeleteItem").click(async (event) => {
                this.base.onUndeleteItemClick(event, encryptedItemId);
            });

            loadPopupContents();
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

        await Wiser2.showConfirmDialog("Weet u zeker dat u dit item wilt verwijderen?");

        const popupWindowContainer = $(event.currentTarget).closest(".k-window").find(".popup-container");

        try {
            popupWindowContainer.find(".popup-loader").addClass("loading");
            popupWindowContainer.data("saving", true);

            const kendoWindow = popupWindowContainer.data("kendoWindow");
            let entityType = popupWindowContainer.data("entityTypeDetails");

            if (Wiser2.validateArray(entityType)) {
                entityType = entityType[0];
            }

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

            if (Wiser2.validateArray(entityType)) {
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

            if (exception.status === 409) {
                const message = exception.responseText || "Het is niet meer mogelijk om aanpassingen te maken in dit item.";
                kendo.alert(message);
            } else {
                kendo.alert("Er is iets fout gegaan tijdens het opslaan van dit item. Probeer het a.u.b. nogmaals of neem contact op met ons.");
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
                                await Wiser2.showConfirmDialog(`Let op! Dit item is al gekoppeld aan een ander item (ID ${dataItem.parentItemId}). Als u op "OK" klikt, zal die koppeling vervangen worden door deze nieuwe koppeling.`, "Koppeling vervangen", "Annuleren", "Vervangen");
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
                promises.push(Wiser2.api({
                    url: `${this.base.settings.wiserApiRoot}items/add-links?moduleId=${this.base.settings.moduleId}`,
                    method: "POST",
                    contentType: "application/json",
                    data: JSON.stringify(addLinksRequest)
                }));
            }
            if (removeLinksRequest.encryptedSourceIds.length > 0 && removeLinksRequest.encryptedDestinationIds.length > 0) {
                promises.push(Wiser2.api({
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

            const gridDataResult = await Wiser2.api({
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
                        case "linkId":
                            column.hidden = this.searchGridSettings.hideLinkIdColumn || false;
                            break;
                        case "entityType":
                            column.hidden = this.searchGridSettings.hideTypeColumn || false;
                            break;
                        case "publishedEnvironment":
                            column.hidden = this.searchGridSettings.hideEnvironmentColumn || false;
                            break;
                        case "name":
                            column.hidden = this.searchGridSettings.hideTitleColumn || false;
                            break;
                        case "encryptedId":
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
                                transportOptions.data.pageSize = transportOptions.data.pageSize;
                                transportOptions.data.take = transportOptions.data.pageSize;
                                previousFilters = currentFilters;

                                const newGridDataResult = await Wiser2.api({
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
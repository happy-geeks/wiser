// noinspection JSFileReferences

import { TrackJS } from "trackjs";
import { Wiser } from "../../Base/Scripts/Utils.js";
import "../../Base/Scripts/Processing.js";
window.JSZip = require("jszip");

require("@progress/kendo-ui/js/kendo.notification.js");
require("@progress/kendo-ui/js/kendo.button.js");
require("@progress/kendo-ui/js/kendo.dialog.js");
require("@progress/kendo-ui/js/kendo.splitter.js");
require("@progress/kendo-ui/js/kendo.treeview.js");
require("@progress/kendo-ui/js/kendo.notification.js");
require("@progress/kendo-ui/js/kendo.window.js");
require("@progress/kendo-ui/js/kendo.numerictextbox.js");
require("@progress/kendo-ui/js/kendo.dropdownlist.js");
require("@progress/kendo-ui/js/kendo.tabstrip.js");
require("@progress/kendo-ui/js/filemanager/contextmenu.js");
require("@progress/kendo-ui/js/cultures/kendo.culture.nl-NL.js");
require("@progress/kendo-ui/js/messages/kendo.messages.nl-NL.js");

import "../Css/FileManager.css";

// Any custom settings can be added here. They will overwrite most default settings inside the module.
const moduleSettings = {

};

((settings) => {
    /**
     * Main class.
     */
    class FileManager {
        // noinspection JSIgnoredPromiseFromCall
        /**
         * Initializes a new instance of FileManager.
         * @param {any} settings An object containing the settings for this class.
         */
        constructor(settings) {
            this.base = this;

            this.mainTabStrip = null;

            // Upload windows.
            this.imagesUploaderWindow = null;
            this.imagesUploaderWindowTreeView = null;
            this.imagesUploaderWindowTreeViewContextMenuTarget = null;
            this.imagesUploaderWindowSplitter = null;

            this.filesUploaderWindow = null;
            this.filesUploaderWindowTreeView = null;
            this.filesUploaderWindowTreeViewContextMenuTarget = null;
            this.filesUploaderWindowSplitter = null;

            this.templatesUploaderWindow = null;
            this.templatesUploaderWindowTreeView = null;
            this.templatesUploaderWindowTreeViewContextMenuTarget = null;
            this.templatesUploaderWindowSplitter = null;

            this.uploaderWindowTreeViewStateLoading = false;
            this.uploaderWindowTreeViewStates = {
                images: null,
                files: null,
                templates: null
            };

            this.fileManagerModes = Object.freeze({
                images: "images",
                files: "files",
                templates: "templates"
            });

            // Set the Kendo culture to Dutch. TODO: Base this on the language in Wiser.
            kendo.culture("nl-NL");

            // Default settings
            this.settings = {
                moduleId: 0,
                username: "Onbekend",
                userEmailAddress: "",
                userType: "",
                selectedText: ""
            };
            Object.assign(this.settings, settings);

            // Add logged in user access token to default authorization headers for all jQuery ajax requests.
            $.ajaxSetup({
                headers: { "Authorization": `Bearer ${localStorage.getItem("accessToken")}` }
            });

            // Fire event on page ready for direct actions
            $(document).ready(() => {
                this.onPageReady();
            });
        }

        /**
         * Event that will be fired when the page is ready.
         */
        async onPageReady() {
            this.mainLoader = $("#mainLoader");

            // Setup processing.
            document.addEventListener("processing.Busy", this.toggleMainLoader.bind(this, true));
            document.addEventListener("processing.Idle", this.toggleMainLoader.bind(this, false));

            // Setup any settings from the body element data. These settings are added via the Wiser backend and they take preference.
            Object.assign(this.settings, $("body").data());

            if (this.settings.trackJsToken) {
                TrackJS.install({
                    token: this.settings.trackJsToken
                });
            }

            // Get user data from local storage.
            const user = JSON.parse(localStorage.getItem("userData"));
            this.settings.oldStyleUserId = user.oldStyleUserId;
            this.settings.username = user.adminAccountName ? `${user.adminAccountName} (Admin)` : user.name;
            this.settings.adminAccountLoggedIn = !!user.adminAccountName;

            if (!this.settings.wiserApiRoot.endsWith("/")) {
                this.settings.wiserApiRoot += "/";
            }

            // Get user data from API.
            let userData = await Wiser.getLoggedInUserData(this.settings.wiserApiRoot);

            // If we have no 'plainImagesRootId', then force the refresh from database, because this is a new setting and not everyone has it in their cache
            if (!userData.plainImagesRootId) {
                userData = await Wiser.getLoggedInUserData(this.settings.wiserApiRoot, true);
            }
            this.settings.userId = userData.encryptedId;
            this.settings.customerId = userData.encryptedCustomerId;
            this.settings.zeroEncrypted = userData.zeroEncrypted;
            this.settings.filesRootId = userData.filesRootId;
            this.settings.imagesRootId = userData.imagesRootId;
            this.settings.templatesRootId = userData.templatesRootId;
            this.settings.plainFilesRootId = userData.plainFilesRootId;
            this.settings.plainImagesRootId = userData.plainImagesRootId;
            this.settings.plainTemplatesRootId = userData.plainTemplatesRootId;
            this.settings.mainDomain = userData.mainDomain;
            this.settings.serviceRoot = `${this.settings.wiserApiRoot}templates/get-and-execute-query`;

            await this.initialize();
        }

        /**
         * Do all initializations for this class, such as adding bindings.
         */
        initialize() {
            // Normal notifications.
            this.notification = $("#alert").kendoNotification({
                button: true,
                autoHideAfter: 5000,
                stacking: "down",
                position: {
                    top: 0,
                    left: 0,
                    right: 0,
                    bottom: null,
                    pinned: true
                },
                templates: [{
                    type: "error",
                    template: $("#errorTemplate").html()
                }, {
                    type: "success",
                    template: $("#successTemplate").html()
                }]
            }).data("kendoNotification");

            // Window for viewing a all generic images and for adding them into an HTML editor.
            this.imagesUploaderWindow = $("#imagesUploaderWindow").kendoWindow({
                width: "90%",
                height: "90%",
                title: "Afbeeldingen",
                visible: false,
                modal: true,
                animation: false,
                actions: [],
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

            this.imagesUploaderWindow.element.find(".image-preview .copy-image-url").click((event) => {
                event.preventDefault();
                navigator.clipboard.writeText(this.generateImagePreviewUrl().url)
                    .then(() => console.log("Copied to clipboard"))
                    .catch((error) => console.error("Could not copy to clipboard", error));
            });

            // Window for viewing a all generic files and for adding them into an HTML editor.
            this.filesUploaderWindow = $("#filesUploaderWindow").kendoWindow({
                width: "90%",
                height: "90%",
                title: "Bestanden",
                visible: false,
                modal: true,
                animation: false,
                actions: [],
                open: (event) => {
                    this.filesUploaderWindowSplitter.resize(true);

                    if (this.filesUploaderWindowTreeView) {
                        this.filesUploaderWindow.element.find(".popup-loader").addClass("loading");
                        this.filesUploaderWindowTreeView.dataSource.read();
                        this.filesUploaderWindow.element.find(".right-pane, footer").addClass("hidden");
                    } else {
                        this.initializeFileUploader(event);
                    }

                    if (this.settings.selectedText) {
                        this.filesUploaderWindow.element.find("#fileLinkText").val(this.settings.selectedText);
                    }
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

            this.filesUploaderWindow.element.find(".file-preview .copy-file-url").click((event) => {
                event.preventDefault();
                navigator.clipboard.writeText(this.generateFilePreviewUrl())
                    .then(() => console.log("Copied to clipboard"))
                    .catch((error) => console.error("Could not copy to clipboard", error));
            });

            // Window for viewing a all generic HTML templates and for adding them into an HTML editor.
            this.templatesUploaderWindow = $("#templatesUploaderWindow").kendoWindow({
                width: "90%",
                height: "90%",
                title: "Templates",
                visible: false,
                modal: true,
                animation: false,
                actions: [],
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

            // Open the correct screen based on the selected mode.
            this.settings.mode = this.settings.mode || "";
            switch (this.settings.mode.toLowerCase()) {
                case this.fileManagerModes.images:
                    this.imagesUploaderWindow.maximize().open();
                    break;
                case this.fileManagerModes.files:
                    this.filesUploaderWindow.maximize().open();
                    break;
                case this.fileManagerModes.templates:
                    this.templatesUploaderWindow.maximize().open();
                    break;
                default:
                    this.mainTabStrip = $("#ModeSelector").show().kendoTabStrip({
                        animation: false,
                        select: this.onTabStripSelect.bind(this)
                    }).data("kendoTabStrip");
                    this.mainTabStrip.select(0);
                    break;
            }
        }

        /**
         * Shows or hides the main (full screen) loader.
         * @param {boolean} show True to show the loader, false to hide it.
         */
        toggleMainLoader(show) {
            this.mainLoader.toggleClass("loading", show);
        }

        /**
         * Event for when the user selects a tab in the main tab strip.
         * @param event
         */
        onTabStripSelect(event) {
            const iframeElement = event.contentElement.querySelector("iframe");
            if (!iframeElement || iframeElement.src) {
                return;
            }

            iframeElement.src = iframeElement.getAttribute("data-src");
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
                        read: (transportOptions) => {
                            const node = transportOptions.data;
                            const nodeId = node.id || this.settings.plainImagesRootId;
                            const url = `${this.settings.wiserApiRoot}files/${encodeURIComponent(nodeId)}/tree`;
                            Wiser.api({
                                url: url,
                                dataType: "json"
                            }).then((result) => {
                                transportOptions.success(result);
                            }).catch((error) => {
                                transportOptions.error(error);
                            });
                        }
                    },
                    schema: {
                        model: {
                            id: "id",
                            hasChildren: "hasChildren"
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
                    this.onTreeViewCollapseItem(collapseEvent);
                    // Timeout because the collapse event gets triggered before the collapse happens, but we need to save the state after it happened.
                    setTimeout(() => this.saveUploaderWindowTreeViewState("images", this.imagesUploaderWindowTreeView), 100);
                },
                expand: (expandEvent) => {
                    this.onTreeViewExpandItem(expandEvent);
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
                    const itemId = selectedItem ? selectedItem.id : this.settings.imagesRootId;
                    const promises = [];
                    for (let file of selectedFiles) {
                        const formData = new FormData();
                        formData.append(file.name, file);

                        promises.push(Wiser.api({
                            url: `${this.settings.wiserApiRoot}items/${encodeURIComponent(itemId)}/upload?propertyName=global_file&useTinyPng=false`,
                            method: "POST",
                            processData: false,
                            contentType: false,
                            data: formData
                        }));
                    }

                    Promise.all(promises).then((results) => {
                        this.notification.show({ message: "Afbeelding(en) succesvol toegevoegd" }, "success");
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
                                    Wiser.showConfirmDialog(`Weet u zeker dat u de map '${selectedItem.name}' wilt verwijderen? Alle afbeeldingen in deze map zullen dan ook verwijderd worden.`).then(() => {
                                        Wiser.deleteItem(this.settings, selectedItem.id, "filedirectory").then(() => {
                                            this.imagesUploaderWindowTreeView.remove(this.imagesUploaderWindowTreeViewContextMenuTarget);
                                            this.notification.show({ message: "Map succesvol verwijderd" }, "success");
                                            loader.removeClass("loading");
                                        });
                                    }).catch(() => { loader.removeClass("loading"); });
                                } else {
                                    Wiser.showConfirmDialog(`Weet u zeker dat u de afbeelding '${selectedItem.name}' wilt verwijderen?`).then(() => {
                                        Wiser.api({
                                            url: `${this.settings.wiserApiRoot}items/${encodeURIComponent(selectedItem.encryptedItemId)}/files/${encodeURIComponent(selectedItem.id)}`,
                                            method: "DELETE",
                                            contentType: "application/json",
                                            dataType: "JSON"
                                        }).then(() => {
                                            this.imagesUploaderWindowTreeView.remove(this.imagesUploaderWindowTreeViewContextMenuTarget);
                                            this.notification.show({ message: "Afbeelding succesvol verwijderd" }, "success");
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
                                        Wiser.updateItem(this.settings, selectedItem.encryptedItemId, [], false, newName, true, "filedirectory").then(() => {
                                            this.imagesUploaderWindowTreeView.text(this.imagesUploaderWindowTreeViewContextMenuTarget, newName);
                                            this.notification.show({ message: "Mapnaam is succesvol gewijzigd" }, "success");
                                            loader.removeClass("loading");
                                        }).catch((error) => {
                                            console.error(error);
                                            loader.removeClass("loading");
                                            kendo.alert("Er is iets fout gegaan. Probeer het a.u.b. opnieuw of neem contact op met ons.");
                                        });
                                    } else {
                                        Wiser.api({
                                            url: `${this.settings.wiserApiRoot}items/${encodeURIComponent(selectedItem.encryptedItemId)}/files/${encodeURIComponent(selectedItem.id)}/rename/${encodeURIComponent(newName)}`,
                                            method: "PUT",
                                            contentType: "application/json",
                                            dataType: "JSON"
                                        }).then(() => {
                                            this.imagesUploaderWindowTreeView.dataSource.read();
                                            this.notification.show({ message: "Bestandsnaam succesvol gewijzigd" }, "success");
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
                        this.createFileDirectory(this.settings.imagesRootId, this.imagesUploaderWindow, this.imagesUploaderWindowTreeView);
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
                        read: (transportOptions) => {
                            const node = transportOptions.data;
                            const nodeId = node.id || this.settings.plainFilesRootId;
                            const url = `${this.settings.wiserApiRoot}files/${encodeURIComponent(nodeId)}/tree`;
                            Wiser.api({
                                url: url,
                                dataType: "json"
                            }).then((result) => {
                                transportOptions.success(result);
                            }).catch((error) => {
                                transportOptions.error(error);
                            });
                        }
                    },
                    schema: {
                        model: {
                            id: "id",
                            hasChildren: "hasChildren"
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
                    this.onTreeViewCollapseItem(collapseEvent);
                    // Timeout because the collapse event gets triggered before the collapse happens, but we need to save the state after it happened.
                    setTimeout(() => this.saveUploaderWindowTreeViewState("files", this.filesUploaderWindowTreeView), 100);
                },
                expand: (expandEvent) => {
                    this.onTreeViewExpandItem(expandEvent);
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
                    const itemId = selectedItem ? selectedItem.id : this.settings.filesRootId;
                    const promises = [];
                    for (let file of selectedFiles) {
                        const formData = new FormData();
                        formData.append(file.name, file);

                        promises.push(Wiser.api({
                            url: `${this.settings.wiserApiRoot}items/${encodeURIComponent(itemId)}/upload?propertyName=global_file&useTinyPng=false`,
                            method: "POST",
                            processData: false,
                            contentType: false,
                            data: formData
                        }));
                    }

                    Promise.all(promises).then((results) => {
                        this.notification.show({ message: "Bestand(en) succesvol toegevoegd" }, "success");
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
                                    Wiser.showConfirmDialog(`Weet u zeker dat u de map '${selectedItem.name}' wilt verwijderen? Alle bestanden in deze map zullen dan ook verwijderd worden.`).then(() => {
                                        Wiser.deleteItem(this.settings, selectedItem.id, "filedirectory").then(() => {
                                            this.filesUploaderWindowTreeView.remove(this.filesUploaderWindowTreeViewContextMenuTarget);
                                            this.notification.show({ message: "Map succesvol verwijderd" }, "success");
                                            loader.removeClass("loading");
                                        });
                                    }).catch(() => { loader.removeClass("loading"); });
                                } else {
                                    Wiser.showConfirmDialog(`Weet u zeker dat u het bestand '${selectedItem.name}' wilt verwijderen?`).then(() => {
                                        Wiser.api({
                                            url: `${this.settings.wiserApiRoot}items/${encodeURIComponent(selectedItem.encryptedItemId)}/files/${encodeURIComponent(selectedItem.id)}`,
                                            method: "DELETE",
                                            contentType: "application/json",
                                            dataType: "JSON"
                                        }).then(() => {
                                            this.filesUploaderWindowTreeView.remove(this.filesUploaderWindowTreeViewContextMenuTarget);
                                            this.notification.show({ message: "Bestand succesvol verwijderd" }, "success");
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
                                        Wiser.updateItem(this.settings, selectedItem.encryptedItemId, [], false, newName, true, "filedirectory").then(() => {
                                            this.filesUploaderWindowTreeView.text(this.filesUploaderWindowTreeViewContextMenuTarget, newName);
                                            this.notification.show({ message: "Mapnaam is succesvol gewijzigd" }, "success");
                                            loader.removeClass("loading");
                                        }).catch((error) => {
                                            console.error(error);
                                            loader.removeClass("loading");
                                            kendo.alert("Er is iets fout gegaan. Probeer het a.u.b. opnieuw of neem contact op met ons.");
                                        });
                                    } else {
                                        Wiser.api({
                                            url: `${this.settings.wiserApiRoot}items/${encodeURIComponent(selectedItem.encryptedItemId)}/files/${encodeURIComponent(selectedItem.id)}/rename/${encodeURIComponent(newName)}`,
                                            method: "PUT",
                                            contentType: "application/json",
                                            dataType: "JSON"
                                        }).then(() => {
                                            this.filesUploaderWindowTreeView.dataSource.read();
                                            this.notification.show({ message: "Bestandsnaam succesvol gewijzigd" }, "success");
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
                        this.createFileDirectory(this.settings.filesRootId, this.filesUploaderWindow, this.filesUploaderWindowTreeView);
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
                        read: (transportOptions) => {
                            const node = transportOptions.data;
                            const nodeId = node.id || this.settings.plainTemplatesRootId;
                            const url = `${this.settings.wiserApiRoot}files/${encodeURIComponent(nodeId)}/tree`;
                            Wiser.api({
                                url: url,
                                dataType: "json"
                            }).then((result) => {
                                transportOptions.success(result);
                            }).catch((error) => {
                                transportOptions.error(error);
                            });
                        }
                    },
                    schema: {
                        model: {
                            id: "id",
                            hasChildren: "hasChildren"
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
                    this.onTreeViewCollapseItem(collapseEvent);
                    // Timeout because the collapse event gets triggered before the collapse happens, but we need to save the state after it happened.
                    setTimeout(() => this.saveUploaderWindowTreeViewState("templates", this.templatesUploaderWindowTreeView), 100);
                },
                expand: (expandEvent) => {
                    this.onTreeViewExpandItem(expandEvent);
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
                    const itemId = selectedItem ? selectedItem.id : this.settings.templatesRootId;
                    const promises = [];
                    for (let file of selectedFiles) {
                        const formData = new FormData();
                        formData.append(file.name, file);

                        promises.push(Wiser.api({
                            url: `${this.settings.wiserApiRoot}items/${encodeURIComponent(itemId)}/upload?propertyName=global_file&useTinyPng=false`,
                            method: "POST",
                            processData: false,
                            contentType: false,
                            data: formData
                        }));
                    }

                    Promise.all(promises).then((results) => {
                        this.notification.show({ message: "Template(s) succesvol toegevoegd" }, "success");
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
                                    Wiser.showConfirmDialog(`Weet u zeker dat u de map '${selectedItem.name}' wilt verwijderen? Alle templates in deze map zullen dan ook verwijderd worden.`).then(() => {
                                        this.deleteItem(selectedItem.id, "filedirectory").then(() => {
                                            this.templatesUploaderWindowTreeView.remove(this.templatesUploaderWindowTreeViewContextMenuTarget);
                                            this.notification.show({ message: "Map succesvol verwijderd" }, "success");
                                            loader.removeClass("loading");
                                        });
                                    }).catch(() => { loader.removeClass("loading"); });
                                } else {
                                    Wiser.showConfirmDialog(`Weet u zeker dat u de template '${selectedItem.name}' wilt verwijderen?`).then(() => {
                                        Wiser.api({
                                            url: `${this.settings.wiserApiRoot}items/${encodeURIComponent(selectedItem.encryptedItemId)}/files/${encodeURIComponent(selectedItem.id)}`,
                                            method: "DELETE",
                                            contentType: "application/json",
                                            dataType: "JSON"
                                        }).then(() => {
                                            this.templatesUploaderWindowTreeView.remove(this.templatesUploaderWindowTreeViewContextMenuTarget);
                                            this.notification.show({ message: "Template succesvol verwijderd" }, "success");
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
                                        Wiser.updateItem(this.settings, selectedItem.encryptedItemId, [], false, newName, true, "filedirectory").then(() => {
                                            this.templatesUploaderWindowTreeView.text(this.templatesUploaderWindowTreeViewContextMenuTarget, newName);
                                            this.notification.show({ message: "Mapnaam is succesvol gewijzigd" }, "success");
                                            loader.removeClass("loading");
                                        }).catch((error) => {
                                            console.error(error);
                                            loader.removeClass("loading");
                                            kendo.alert("Er is iets fout gegaan. Probeer het a.u.b. opnieuw of neem contact op met ons.");
                                        });
                                    } else {
                                        Wiser.api({
                                            url: `${this.settings.wiserApiRoot}items/${encodeURIComponent(selectedItem.encryptedItemId)}/files/${encodeURIComponent(selectedItem.id)}/rename/${encodeURIComponent(newName)}`,
                                            method: "PUT",
                                            contentType: "application/json",
                                            dataType: "JSON"
                                        }).then(() => {
                                            this.templatesUploaderWindowTreeView.dataSource.read();
                                            this.notification.show({ message: "Bestandsnaam succesvol gewijzigd" }, "success");
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
                        this.createFileDirectory(this.settings.templatesRootId, this.templatesUploaderWindow, this.templatesUploaderWindowTreeView);
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
                    Wiser.createItem(this.settings, "filedirectory", parentId, name, null, [], true).then((createItemResult) => {
                        if (!createItemResult) {
                            kendo.alert("Er iets iets fout gegaan tijdens het aanmaken van de map. Probeer het a.u.b. opnieuw of neem contact op met ons.");
                        } else {
                            this.notification.show({ message: "Map succesvol aangemaakt" }, "success");
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
                    this.uploaderWindowTreeViewStates[type][item.id] = true;
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
                if (this.uploaderWindowTreeViewStates[type][data[i].id]) {
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
         * @param extension The extension of the image file
         * @returns {string} The URL for the preview image.
         */
        generateImagePreviewUrl(extension = "") {
            const selectedItem = this.imagesUploaderWindowTreeView.dataItem(this.imagesUploaderWindowTreeView.select());
            let resizeMode = this.imagesUploaderWindow.element.find("#resizeMode").data("kendoDropDownList").value() || "normal";
            if (resizeMode === "crop" || resizeMode === "fill") {
                const anchorPosition = this.imagesUploaderWindow.element.find("#anchorPosition").data("kendoDropDownList").value();
                resizeMode += `-${anchorPosition || "center"}`;
            }

            const width = this.imagesUploaderWindow.element.find("#preferredWidth").val() || 0;
            const height = this.imagesUploaderWindow.element.find("#preferredHeight").val() || 0;
            const altText = this.imagesUploaderWindow.element.find("#altText").val() || "";

            let fileName = selectedItem.name;
            const dotIndex = fileName.lastIndexOf(".");
            if (extension) {
                fileName = dotIndex > -1 ? `${fileName.substr(0, dotIndex)}.${extension}` : `${fileName}.${extension}`;
            } else if (!extension && dotIndex === -1) {
                fileName = `${fileName}.png`;
            }

            let domain = this.settings.mainDomain;
            if (!domain.endsWith("/")) {
                domain += "/";
            }

            return {
                url: `${domain}image/wiser2/${selectedItem.id}/direct/${selectedItem.propertyName || "global_file"}/${resizeMode}/${width}/${height}/${fileName}`,
                altText: altText
            };
        }

        /**
         * Generates the URL for the file preview for the imagesUploaderWindow.
         * @returns {string} The URL for the preview image.
         */
        generateFilePreviewUrl() {
            const selectedItem = this.filesUploaderWindowTreeView.dataItem(this.filesUploaderWindowTreeView.select());
            let result = `${this.settings.mainDomain}/file/wiser2/${selectedItem.id}/direct/${selectedItem.propertyName || "global_file"}/${selectedItem.name}`;
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
            anchorElement.attr("href", newImageUrl.url);
            imagePreviewElement.attr("src", newImageUrl.url);
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
            let name = this.settings.selectedText || selectedItem.name;
            this.filesUploaderWindow.element.find("#fileLinkText").val(name)
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
    }

    // Initialize the DynamicItems class and make one instance of it globally available.
    window.fileManager = new FileManager(settings);
})(moduleSettings);
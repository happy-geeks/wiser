﻿import { ModuleSettingsModel } from "../Scripts/ModuleSettingsModel.js";

export class ModuleTab {
    constructor(base) {
        this.base = base;
        this.setupBindings();
        this.initializeKendoComponents();
        // set query dropdown list
        this.getModules();
    }

    /**
    * Setup all basis bindings for this module.
    * Specific bindings (for buttons in certain pop-ups for example) will be set when they are needed.
    */
    setupBindings() {
        $(".addModuleBtn").kendoButton({
            click: () => {
                this.base.openDialog("Module toevoegen", "Voer de naam in van nieuwe module").then((data) => {
                    this.addModule(data);
                });
            },
            icon: "file"
        });

        $(".delModuleBtn").kendoButton({
            click: () => {
                if (!this.checkIfModuleIsSet()) {
                    return;
                }
                
                const moduleId = this.moduleCombobox.dataItem().id;
                if (!moduleId) {
                    this.base.showNotification("notification", "Kies a.u.b. eerst een module om te verwijderen", "error");
                    return;
                }

                // ask for user confirmation before deleting
                Wiser.showConfirmDialog(`Weet u zeker dat u de module "${this.moduleCombobox.dataItem().description}" wilt verwijderen?`).then(() => {
                    this.deleteModule(moduleId);
                });
            },
            icon: "delete"
        });
    }

    async initializeKendoComponents() {
        this.moduleCombobox = $("#moduleList").kendoDropDownList({
            placeholder: "Select een module...",
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
            cascade: this.onModuleComboBoxSelect.bind(this)
        }).data("kendoDropDownList");

        this.moduleCombobox.one("dataBound", () => { this.moduleListInitialized = true; });

        await Misc.ensureCodeMirror();

        this.moduleCustomQuery = window.CodeMirror.fromTextArea(document.getElementById("moduleCustomQuery"), {
            mode: "text/x-mysql",
            lineNumbers: true
        });

        this.moduleCountQuery = window.CodeMirror.fromTextArea(document.getElementById("moduleCountQuery"), {
            mode: "text/x-mysql",
            lineNumbers: true
        });

        this.moduleOptions = window.CodeMirror.fromTextArea(document.getElementById("moduleOptions"), {
            mode: "application/x-json",
            lineNumbers: true,
            lineWrapping: true
        });

        this.moduleIcon = $("#moduleIcon").kendoDropDownList({
            placeholder: "Maak uw keuze...",
            /* 
                This list is generated by copying the contents of icons.css to Notepad++ 
                and then using find & replace with the following regex: \.(icon-([a-z\-0-9A-Z]+)):before { content: "\\e.+ 
                and the following "replace with" value: { text: "$2", value: "$2" },
             */
            dataSource: [
                { text: "add", value: "add" },
                { text: "admin", value: "admin" },
                { text: "affiliate", value: "affiliate" },
                { text: "agenda", value: "agenda" },
                { text: "album", value: "album" },
                { text: "album-add", value: "album-add" },
                { text: "album-delete", value: "album-delete" },
                { text: "alert", value: "alert" },
                { text: "announce", value: "announce" },
                { text: "apply", value: "apply" },
                { text: "arrow-back", value: "arrow-back" },
                { text: "arrow-down", value: "arrow-down" },
                { text: "arrow-forward", value: "arrow-forward" },
                { text: "arrow-left", value: "arrow-left" },
                { text: "arrow-right", value: "arrow-right" },
                { text: "arrow-up", value: "arrow-up" },
                { text: "asana", value: "asana" },
                { text: "auction", value: "auction" },
                { text: "bed", value: "bed" },
                { text: "bell", value: "bell" },
                { text: "binoculars", value: "binoculars" },
                { text: "book", value: "book" },
                { text: "book-group", value: "book-group" },
                { text: "box", value: "box" },
                { text: "box-link", value: "box-link" },
                { text: "brands", value: "brands" },
                { text: "building", value: "building" },
                { text: "business-man", value: "business-man" },
                { text: "calendar", value: "calendar" },
                { text: "calendar-tool", value: "calendar-tool" },
                { text: "camera", value: "camera" },
                { text: "cancel", value: "cancel" },
                { text: "car", value: "car" },
                { text: "cart", value: "cart" },
                { text: "chat-price", value: "chat-price" },
                { text: "chat-user", value: "chat-user" },
                { text: "check", value: "check" },
                { text: "checkbox", value: "checkbox" },
                { text: "chevron-down", value: "chevron-down" },
                { text: "chevron-left", value: "chevron-left" },
                { text: "chevron-right", value: "chevron-right" },
                { text: "chevron-up", value: "chevron-up" },
                { text: "choices", value: "choices" },
                { text: "classroom", value: "classroom" },
                { text: "client-leads", value: "client-leads" },
                { text: "client-prospects", value: "client-prospects" },
                { text: "client-suspects", value: "client-suspects" },
                { text: "clipboard", value: "clipboard" },
                { text: "clock", value: "clock" },
                { text: "close", value: "close" },
                { text: "cloud", value: "cloud" },
                { text: "cloud-down", value: "cloud-down" },
                { text: "cloud-up", value: "cloud-up" },
                { text: "colors", value: "colors" },
                { text: "combination", value: "combination" },
                { text: "communication", value: "communication" },
                { text: "cone", value: "cone" },
                { text: "config", value: "config" },
                { text: "controls", value: "controls" },
                { text: "creditcard", value: "creditcard" },
                { text: "database", value: "database" },
                { text: "date", value: "date" },
                { text: "date-course", value: "date-course" },
                { text: "delete", value: "delete" },
                { text: "desktop", value: "desktop" },
                { text: "dialog-close", value: "dialog-close" },
                { text: "dialog-enlarge", value: "dialog-enlarge" },
                { text: "dialog-minimize", value: "dialog-minimize" },
                { text: "directions", value: "directions" },
                { text: "directions-temp", value: "directions-temp" },
                { text: "discount", value: "discount" },
                { text: "doc", value: "doc" },
                { text: "doc-add", value: "doc-add" },
                { text: "doc-invoice", value: "doc-invoice" },
                { text: "doc-subscription", value: "doc-subscription" },
                { text: "document", value: "document" },
                { text: "document-add", value: "document-add" },
                { text: "document-delete", value: "document-delete" },
                { text: "document-duplicate", value: "document-duplicate" },
                { text: "document-edit", value: "document-edit" },
                { text: "document-exam", value: "document-exam" },
                { text: "document-export", value: "document-export" },
                { text: "document-flat", value: "document-flat" },
                { text: "document-fold", value: "document-fold" },
                { text: "document-hide", value: "document-hide" },
                { text: "document-import", value: "document-import" },
                { text: "document-pdf", value: "document-pdf" },
                { text: "document-web", value: "document-web" },
                { text: "document-xml", value: "document-xml" },
                { text: "domain", value: "domain" },
                { text: "downloaden", value: "downloaden" },
                { text: "dress", value: "dress" },
                { text: "dynamic", value: "dynamic" },
                { text: "euro", value: "euro" },
                { text: "eye-invisible", value: "eye-invisible" },
                { text: "eye-visible", value: "eye-visible" },
                { text: "facebook", value: "facebook" },
                { text: "factory", value: "factory" },
                { text: "filter", value: "filter" },
                { text: "flag", value: "flag" },
                { text: "flag-2", value: "flag-2" },
                { text: "folder", value: "folder" },
                { text: "folder-add", value: "folder-add" },
                { text: "folder-check", value: "folder-check" },
                { text: "folder-closed", value: "folder-closed" },
                { text: "folder-delete", value: "folder-delete" },
                { text: "folder-duplicate", value: "folder-duplicate" },
                { text: "folder-edit", value: "folder-edit" },
                { text: "folder-hide", value: "folder-hide" },
                { text: "folder-hide-2", value: "folder-hide-2" },
                { text: "folder-search", value: "folder-search" },
                { text: "forklift", value: "forklift" },
                { text: "games", value: "games" },
                { text: "git", value: "git" },
                { text: "globe", value: "globe" },
                { text: "golf-clinic", value: "golf-clinic" },
                { text: "golf-course", value: "golf-course" },
                { text: "golf-vacation", value: "golf-vacation" },
                { text: "info", value: "info" },
                { text: "info-full", value: "info-full" },
                { text: "pricelabel", value: "pricelabel" },
                { text: "light-off", value: "light-off" },
                { text: "light-on", value: "light-on" },
                { text: "line-alert", value: "line-alert" },
                { text: "line-arrow-down", value: "line-arrow-down" },
                { text: "line-arrow-left", value: "line-arrow-left" },
                { text: "line-arrow-right", value: "line-arrow-right" },
                { text: "line-arrow-up", value: "line-arrow-up" },
                { text: "line-book", value: "line-book" },
                { text: "line-bug", value: "line-bug" },
                { text: "line-calendar", value: "line-calendar" },
                { text: "line-cart", value: "line-cart" },
                { text: "line-chart", value: "line-chart" },
                { text: "line-chart-seo", value: "line-chart-seo" },
                { text: "line-chevron-down", value: "line-chevron-down" },
                { text: "line-chevron-left", value: "line-chevron-left" },
                { text: "line-chevron-right", value: "line-chevron-right" },
                { text: "line-chevron-up", value: "line-chevron-up" },
                { text: "line-chrome", value: "line-chrome" },
                { text: "line-clipboard", value: "line-clipboard" },
                { text: "line-clock", value: "line-clock" },
                { text: "line-close", value: "line-close" },
                { text: "line-code", value: "line-code" },
                { text: "line-credit-card", value: "line-credit-card" },
                { text: "line-database", value: "line-database" },
                { text: "line-database-search", value: "line-database-search" },
                { text: "line-document-add", value: "line-document-add" },
                { text: "line-download", value: "line-download" },
                { text: "line-download-cloud", value: "line-download-cloud" },
                { text: "line-edit", value: "line-edit" },
                { text: "line-exit", value: "line-exit" },
                { text: "line-export", value: "line-export" },
                { text: "line-file", value: "line-file" },
                { text: "line-file-minus", value: "line-file-minus" },
                { text: "line-file-plus", value: "line-file-plus" },
                { text: "line-file-text", value: "line-file-text" },
                { text: "line-filter", value: "line-filter" },
                { text: "line-flag", value: "line-flag" },
                { text: "line-folder", value: "line-folder" },
                { text: "line-folder-minus", value: "line-folder-minus" },
                { text: "line-folder-plus", value: "line-folder-plus" },
                { text: "line-globe", value: "line-globe" },
                { text: "line-heart", value: "line-heart" },
                { text: "line-home", value: "line-home" },
                { text: "line-image", value: "line-image" },
                { text: "line-im-ex", value: "line-im-ex" },
                { text: "line-import", value: "line-import" },
                { text: "line-info", value: "line-info" },
                { text: "line-lock", value: "line-lock" },
                { text: "line-log-in", value: "line-log-in" },
                { text: "line-log-out", value: "line-log-out" },
                { text: "line-mail", value: "line-mail" },
                { text: "line-menu", value: "line-menu" },
                { text: "line-mic", value: "line-mic" },
                { text: "line-minus", value: "line-minus" },
                { text: "line-monitor", value: "line-monitor" },
                { text: "line-package", value: "line-package" },
                { text: "line-phone", value: "line-phone" },
                { text: "line-phone-call", value: "line-phone-call" },
                { text: "line-picture-add", value: "line-picture-add" },
                { text: "line-pin", value: "line-pin" },
                { text: "line-pin-empty", value: "line-pin-empty" },
                { text: "line-pin-full", value: "line-pin-full" },
                { text: "line-power", value: "line-power" },
                { text: "line-printer", value: "line-printer" },
                { text: "line-refresh", value: "line-refresh" },
                { text: "line-return", value: "line-return" },
                { text: "line-returns", value: "line-returns" },
                { text: "line-scissors", value: "line-scissors" },
                { text: "line-search", value: "line-search" },
                { text: "line-send", value: "line-send" },
                { text: "line-server", value: "line-server" },
                { text: "line-settings", value: "line-settings" },
                { text: "line-share", value: "line-share" },
                { text: "line-shield", value: "line-shield" },
                { text: "line-sliders", value: "line-sliders" },
                { text: "line-thumbs-down", value: "line-thumbs-down" },
                { text: "line-thumbs-up", value: "line-thumbs-up" },
                { text: "line-tool", value: "line-tool" },
                { text: "line-trash", value: "line-trash" },
                { text: "line-truck", value: "line-truck" },
                { text: "line-upload", value: "line-upload" },
                { text: "line-upload-cloud", value: "line-upload-cloud" },
                { text: "line-user", value: "line-user" },
                { text: "line-user-check", value: "line-user-check" },
                { text: "line-user-minus", value: "line-user-minus" },
                { text: "line-user-plus", value: "line-user-plus" },
                { text: "line-users", value: "line-users" },
                { text: "line-user-x", value: "line-user-x" },
                { text: "link", value: "link" },
                { text: "linkedin", value: "linkedin" },
                { text: "link-file", value: "link-file" },
                { text: "link-global", value: "link-global" },
                { text: "list", value: "list" },
                { text: "list-box", value: "list-box" },
                { text: "list-course", value: "list-course" },
                { text: "liveboard", value: "liveboard" },
                { text: "locations", value: "locations" },
                { text: "lock", value: "lock" },
                { text: "log-in", value: "log-in" },
                { text: "log-out", value: "log-out" },
                { text: "mail-forward", value: "mail-forward" },
                { text: "mail-open", value: "mail-open" },
                { text: "man", value: "man" },
                { text: "map", value: "map" },
                { text: "menu", value: "menu" },
                { text: "microphone", value: "microphone" },
                { text: "move", value: "move" },
                { text: "movie", value: "movie" },
                { text: "object", value: "object" },
                { text: "parking", value: "parking" },
                { text: "payment", value: "payment" },
                { text: "pencil", value: "pencil" },
                { text: "people", value: "people" },
                { text: "phone", value: "phone" },
                { text: "picture", value: "picture" },
                { text: "picture-add", value: "picture-add" },
                { text: "picture-delete", value: "picture-delete" },
                { text: "picture-favorite", value: "picture-favorite" },
                { text: "picture-grid", value: "picture-grid" },
                { text: "picture-stack", value: "picture-stack" },
                { text: "plane", value: "plane" },
                { text: "planning", value: "planning" },
                { text: "policeman", value: "policeman" },
                { text: "power", value: "power" },
                { text: "print", value: "print" },
                { text: "project", value: "project" },
                { text: "projects", value: "projects" },
                { text: "push-message", value: "push-message" },
                { text: "puzzle", value: "puzzle" },
                { text: "question", value: "question" },
                { text: "question-2", value: "question-2" },
                { text: "quickmenu", value: "quickmenu" },
                { text: "remove", value: "remove" },
                { text: "rename", value: "rename" },
                { text: "reset", value: "reset" },
                { text: "routes", value: "routes" },
                { text: "save", value: "save" },
                { text: "search", value: "search" },
                { text: "seo", value: "seo" },
                { text: "seo-check", value: "seo-check" },
                { text: "settings", value: "settings" },
                { text: "shop", value: "shop" },
                { text: "sort-asc", value: "sort-asc" },
                { text: "sort-desc", value: "sort-desc" },
                { text: "star-full", value: "star-full" },
                { text: "star-outlined", value: "star-outlined" },
                { text: "stats", value: "stats" },
                { text: "stats-2", value: "stats-2" },
                { text: "stopwatch-pauze", value: "stopwatch-pauze" },
                { text: "stopwatch-start", value: "stopwatch-start" },
                { text: "stopwatch-stop", value: "stopwatch-stop" },
                { text: "student", value: "student" },
                { text: "synonym", value: "synonym" },
                { text: "table", value: "table" },
                { text: "table-add-record", value: "table-add-record" },
                { text: "table-backup", value: "table-backup" },
                { text: "table-delete-record", value: "table-delete-record" },
                { text: "teacher", value: "teacher" },
                { text: "teachers", value: "teachers" },
                { text: "template", value: "template" },
                { text: "tent", value: "tent" },
                { text: "test", value: "test" },
                { text: "theater", value: "theater" },
                { text: "ticket", value: "ticket" },
                { text: "ticket-cart", value: "ticket-cart" },
                { text: "ticket-forward", value: "ticket-forward" },
                { text: "ticket-location", value: "ticket-location" },
                { text: "ticket-time", value: "ticket-time" },
                { text: "time-reset", value: "time-reset" },
                { text: "tool", value: "tool" },
                { text: "tools", value: "tools" },
                { text: "trigger", value: "trigger" },
                { text: "trolley", value: "trolley" },
                { text: "truck", value: "truck" },
                { text: "truck-delivery", value: "truck-delivery" },
                { text: "twitter", value: "twitter" },
                { text: "uploaden", value: "uploaden" },
                { text: "user", value: "user" },
                { text: "user-add", value: "user-add" },
                { text: "user-delete", value: "user-delete" },
                { text: "users", value: "users" },
                { text: "user-status", value: "user-status" },
                { text: "user-time", value: "user-time" },
                { text: "version", value: "version" },
                { text: "views", value: "views" },
                { text: "wiser", value: "wiser" },
                { text: "loader1", value: "loader1" },
                { text: "loader2", value: "loader2" },
                { text: "loader-001", value: "loader-001" },
                { text: "loader-002", value: "loader-002" },
                { text: "loader-003", value: "loader-003" },
                { text: "uniEA2A", value: "uniEA2A" },
                { text: "uniEA2B", value: "uniEA2B" },
                { text: "uniEA2C", value: "uniEA2C" },
                { text: "uniEA2D", value: "uniEA2D" },
                { text: "uniEA2E", value: "uniEA2E" },
                { text: "uniEA2F", value: "uniEA2F" }
            ],
            dataTextField: "text",
            dataValueField: "value",
            height: 400,
            optionLabel: {
                value: "",
                text: "Maak uw keuze..."
            },
            template: `<ins class="iconpreview-icon icon-#: data.value #"></ins><span class="iconpreview-name">#: data.text #</span>`
        }).data("kendoDropDownList");

        this.moduleType = $("#moduleType").kendoComboBox({
            placeholder: "Maak uw keuze...",
            dataSource: [
                { text: "DynamicItems", value: "DynamicItems" },
                { text: "DataSelector", value: "DataSelector" },
                { text: "Admin", value: "Admin" },
                { text: "Templates", value: "Templates" },
                { text: "Scheduler", value: "Scheduler" },
                { text: "Search", value: "Search" },
                { text: "ImportExport", value: "ImportExport" },
                { text: "Communication", value: "Communication" }
            ],
            dataTextField: "text",
            dataValueField: "value"
        }).data("kendoComboBox");
        
        //Combobox for the "Groep" combobox
        this.moduleGroup = $("#moduleGroup").kendoComboBox().data("kendoComboBox");

        this.moduleCustomQuery.setValue("");
        this.moduleCountQuery.setValue("");
        this.moduleOptions.setValue("");
    }

    async addModule(name) {
        if (name === "") { 
            return;
        }
        
        try {
            const result = await Wiser.api({
                url: `${this.base.settings.wiserApiRoot}modules/settings`,
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                data: JSON.stringify(name),
                method: "POST"
            });

            // Also add root entity.
            await this.addRootEntityForNewModule(result);
            this.base.showNotification("notification", "Module succesvol toegevoegd", "success");
            await this.getModules(true, result);
            await this.base.entityTab.reloadEntityList(true);
            
            // Reload the modules list in the side bar of Wiser.
            await this.base.reloadModulesOnParentFrame();
        }
        catch(exception) {
            console.error(exception);
            this.base.showNotification("notification", "Er is iets fout gegaan met het aanmaken van de module. Probeer het a.u.b. opnieuw of neem contact op met ons.", "error");
        }
    }

    /**
     * Will add an entity type with an empty name for the newly created module, otherwise it can't be managed
     * in the entities tab.
     */
    async addRootEntityForNewModule(moduleId) {
        if (typeof moduleId !== "number" || moduleId <= 0) {
            return;
        }

        await Wiser.api({
            url: `${this.base.settings.wiserApiRoot}entity-types?name=&moduleId=${moduleId}`,
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            method: "POST"
        });
    }

    async onModuleComboBoxSelect(event) {
        if (!this.checkIfModuleIsSet((event.userTriggered === true))) {
            return;
        }
        
        const moduleId = this.moduleCombobox.dataItem().id;
        $(".delModuleBtn").toggleClass("hidden", !moduleId);
        
        await this.getModuleById(moduleId);
    }

    async getModules(reloadDataSource = true, moduleIdToSelect = null) {
        if (reloadDataSource) {
            this.moduleList = await Wiser.api({
                url: `${this.base.settings.wiserApiRoot}modules/settings`,
                method: "GET"
            });

            if (!this.moduleList) {
                this.base.showNotification("notification", "Het ophalen van de queries is mislukt, probeer het opnieuw", "error");
            }
            
            this.moduleGroups = await Wiser.api({
                url: `${this.base.settings.wiserApiRoot}modules/groups`,
                method: "GET"
            });
        }

        this.moduleCombobox.setDataSource(this.moduleList);
        this.moduleGroup.setDataSource(this.moduleGroups);
        
        if (moduleIdToSelect !== null) {
            if (moduleIdToSelect === 0) {
                this.moduleCombobox.select(0);
            } else {
                this.moduleCombobox.select((dataItem) => {
                    return dataItem.id === moduleIdToSelect;
                });
            }
        }
    }

    async getModuleById(id) {
        const results = await Wiser.api({
            url: `${this.base.settings.wiserApiRoot}modules/${id}/settings`,
            method: "GET"
        });
        await this.setModulePropertiesToDefault();
        await this.setModuleProperties(results);
    }

    async setModulePropertiesToDefault() {
        document.getElementById("moduleId").value = "";
        document.getElementById("moduleName").value = "";

        this.moduleType.value("");
        this.moduleIcon.value("");
        this.moduleGroup.value("");

        this.moduleCustomQuery.setValue("");
        this.moduleCountQuery.setValue("");
        this.moduleOptions.setValue("");
    }

    async setModuleProperties(resultSet) {
        document.getElementById("moduleId").value = resultSet.id;
        document.getElementById("moduleName").value = typeof (resultSet.name) === "undefined" ? "" : resultSet.name;

        this.moduleIcon.value(resultSet.icon);
        this.moduleType.value(resultSet.type);
        this.moduleGroup.value(resultSet.group);

        document.getElementById("moduleType").value = typeof (resultSet.type) === "undefined" ? "" : resultSet.type;
        document.getElementById("moduleOptions").value = typeof (resultSet.options) === "undefined" ? "" : resultSet.options;

        this.setCodeMirrorFields(this.moduleCustomQuery, typeof (resultSet.customQuery) === "undefined" ? "" : resultSet.customQuery);
        this.setCodeMirrorFields(this.moduleCountQuery, typeof (resultSet.countQuery) === "undefined" ? "" : resultSet.countQuery);
        this.setCodeMirrorFields(this.moduleOptions, JSON.stringify(resultSet.options, null, ' '));
    }

    // actions handled before save, such as checks
    async beforeSave() {
        if (!this.checkIfModuleIsSet(true)) {
            return;
        }
        
        const moduleIdElement = document.getElementById("moduleId");
        if (moduleIdElement == null) {
            return;
        }

        if (isNaN(moduleIdElement.value)) {
            this.base.showNotification("notification", `ID van de module moet een nummerieke waarde zijn!`, "error");
            return;
        }
        
        const moduleId = parseInt(moduleIdElement.value);
        const moduleSettingsModel = new ModuleSettingsModel(
            moduleId,
            this.moduleCustomQuery.getValue(),
            this.moduleCountQuery.getValue(),
            this.moduleOptions.getValue(),
            document.getElementById("moduleName").value,
            this.moduleIcon.value(),
            this.moduleType.value(),
            this.moduleGroup.value()
        );
        
        if (moduleSettingsModel.errors.length > 0) {
            this.base.showNotification("notification", `Controleer de instellingen van de module, er zijn ${moduleSettingsModel.errors.length} fout(en) gevonden: ${moduleSettingsModel.errors.join(", ")}`, "error");
            return;
        }
        
        await this.updateModule(this.moduleCombobox.dataItem().id, moduleSettingsModel);
    }

    async updateModule(id, moduleSettingsModel) {
        try {
            await Wiser.api({
                url: `${this.base.settings.wiserApiRoot}modules/${id}/settings`,
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                data: JSON.stringify(moduleSettingsModel),
                method: "PUT"
            });
            
            this.base.showNotification("notification", `Module is succesvol bijgewerkt`, "success");
            await this.getModules(true, moduleSettingsModel.id);

            // Reload the modules list in the side bar of Wiser.
            await this.base.reloadModulesOnParentFrame();
        } catch(exception) {
            console.error(exception);
            if (exception.responseText.includes("Duplicate entry")) {
                this.base.showNotification("notification", `Het bijwerken van de module is mislukt, de ID van de module bestaat al.`, "error");
            } else {
                this.base.showNotification("notification", `Het bijwerken van de module is mislukt, probeer het opnieuw`, "error");
            }
        }
    }

    setCodeMirrorFields(field, value) {
        if (!field) {
            return;
        }
        
        field.setValue((value != null && value) ? value : "");
        field.refresh();
    }

    checkIfModuleIsSet(showNotification = true) {
        if (this.moduleCombobox &&
            this.moduleCombobox.dataItem() &&
            this.moduleCombobox.dataItem().id !== "" &&
            this.moduleListInitialized === true) {
            return true;
        }
        
        if (showNotification) {
            this.base.showNotification("notification", `Selecteer eerst een module!`, "error");
        }

        return false;
    }

    async deleteModule(id) {
        try {
            await Wiser.api({
                url: `${this.base.settings.wiserApiRoot}modules/${id}`,
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                method: "DELETE"
            });

            this.base.showNotification("notification", `Module is succesvol verwijderd`, "success");
            await this.getModules(true);

            // Reload the modules list in the side bar of Wiser.
            await this.base.reloadModulesOnParentFrame();
        } catch(exception) {
            console.error(exception);
            this.base.showNotification("notification", `Er is iets fout gegaan met het verwijderen van de module. Probeer het a.u.b. opnieuw of neem contact op met ons.`, "error");
        }
    }
}
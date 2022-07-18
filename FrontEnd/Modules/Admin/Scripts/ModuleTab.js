import { ModuleSettingsModel } from "../Scripts/ModuleSettingsModel.js";

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
                const dataItemId = this.moduleCombobox.dataItem().id;
                if (!dataItemId) {
                    this.base.showNotification("notification",
                        "Item is niet succesvol verwijderd, probeer het opnieuw",
                        "error");
                    return;
                }

                // ask for user confirmation before deleting
                this.base.openDialog("Module verwijderen", "Weet u zeker dat u deze query wilt verwijderen?", this.base.kendoPromptType.CONFIRM).then(() => {
                    this.deleteQueryById(dataItemId);
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

        this.moduleIcon = $("#moduleIcon").kendoComboBox({
            placeholder: "Maak uw keuze...",
            clearButton: false,
            dataSource: [
                { text: "Zakenman", value: "icon-business-man" },
                { text: "Wolk", value: "icon-cloud-up" },
                { text: "Bureaublad", value: "icon-desktop" },
                { text: "Document", value: "icon-doc" },
                { text: "Document toevoegen", value: "icon-doc-add" },
                { text: "Document", value: "icon-document" },
                { text: "Document toevoegen", value: "icon-document-add" },
                { text: "Document onderzoeken", value: "icon-document-exam" },
                { text: "Document web", value: "icon-document-web" },
                { text: "Map", value: "icon-folder" },
                { text: "Map toevoegen", value: "icon-folder-add" },
                { text: "Map gesloten", value: "icon-folder-closed" },
                { text: "Gebruiker", value: "icon-user" },
                { text: "Reset", value: "reset" }
            ],
            dataTextField: "text",
            dataValueField: "value",
            optionLabel: {
                value: "",
                text: "Maak uw keuze..."
            },
            template: '<ins class="#: data.value #"></ins><span>#: data.text #</span>'
        }).data("kendoComboBox");

        this.moduleType = $("#moduleType").kendoComboBox({
            placeholder: "Maak uw keuze...",
            clearButton: false,
            dataSource: [
                { text: "DynamicItems", value: "DynamicItems" },
                { text: "DataSelector", value: "DataSelector" },
                { text: "Admin", value: "Admin" },
                { text: "Templates", value: "Templates" },
                { text: "Scheduler", value: "Scheduler" },
                { text: "Search", value: "Search" },
                { text: "ImportExport", value: "ImportExport" }
            ],
            dataTextField: "text",
            dataValueField: "value"
        }).data("kendoComboBox");

        this.moduleCustomQuery.setValue("");
        this.moduleCountQuery.setValue("");
        this.moduleOptions.setValue("");
    }

    async addModule(name) {
        if (name === "") { return; }
        await Wiser2.api({
            url: `${this.base.settings.wiserApiRoot}modules/settings`,
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            data: JSON.stringify(name),
            method: "POST"
        }).then((result) => {
            // Also add root entity.
            this.addRootEntityForNewModule(result).then(() => {
                this.base.showNotification("notification", "Module succesvol toegevoegd", "success");
                this.getModules(true, result);
                this.base.entityTab.reloadEntityList(true);
            });
        }).catch(() => {
            this.base.showNotification("notification", "Module is niet succesvol toegevoegd, probeer het opnieuw", "error");
        });
    }

    /**
     * Will add an entity type with an empty name for the newly created module, otherwise it can't be managed
     * in the entities tab.
     */
    async addRootEntityForNewModule(moduleId) {
        if (typeof moduleId !== "number" || moduleId <= 0) {
            return;
        }

        await Wiser2.api({
            url: `${this.base.settings.wiserApiRoot}entity-types?name=&moduleId=${moduleId}`,
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            method: "POST"
        });
    }

    async onModuleComboBoxSelect(event) {
        if (this.checkIfModuleIsSet((event.userTriggered === true))) {
           this.getModuleById(this.moduleCombobox.dataItem().id);
        }
    }

    async getModules(reloadDataSource = true, moduleIdToSelect = null) {
        if (reloadDataSource) {
            this.moduleList = await Wiser2.api({
                url: `${this.base.settings.wiserApiRoot}modules/settings`,
                method: "GET"
            });

            if (!this.moduleList) {
                this.base.showNotification("notification",
                    "Het ophalen van de queries is mislukt, probeer het opnieuw",
                    "error");
            }
        }

        this.moduleCombobox.setDataSource(this.moduleList);
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
        const results = await Wiser2.api({
            url: `${this.base.settings.wiserApiRoot}modules/${id}/settings`,
            method: "GET"
        });
        this.setModulePropertiesToDefault();
        this.setModuleProperties(results);
    }

    async setModulePropertiesToDefault() {
        document.getElementById("moduleId").value = "";
        document.getElementById("moduleName").value = "";
        document.getElementById("moduleIcon").value = "";
        document.getElementById("moduleType").value = "";
        document.getElementById("moduleGroup").value = "";
        document.getElementById("moduleCustomQuery").value = "";
        document.getElementById("moduleCountQuery").value = "";
        document.getElementById("moduleOptions").value = "";

        this.moduleType.select("");
        this.moduleIcon.select("");

        this.moduleCustomQuery.setValue("");
        this.moduleCountQuery.setValue("");
        this.moduleOptions.setValue("");
    }

    async setModuleProperties(resultSet) {
        document.getElementById("moduleId").value = resultSet.id;
        document.getElementById("moduleName").value = typeof (resultSet.name) === "undefined" ? "" : resultSet.name;

        this.moduleIcon.select((dataItem) => {
            return dataItem.value === resultSet.icon;
        });
        
        this.moduleType.select((dataItem) => {
            return dataItem.value === resultSet.type;
        });

        document.getElementById("moduleType").value = typeof (resultSet.type) === "undefined" ? "" : resultSet.type;
        document.getElementById("moduleGroup").value = typeof (resultSet.group) === "undefined" ? "" : resultSet.group;
        document.getElementById("moduleOptions").value = typeof (resultSet.options) === "undefined" ? "" : resultSet.options;

        this.setCodeMirrorFields(this.moduleCustomQuery, typeof (resultSet.customQuery) === "undefined" ? "" : resultSet.customQuery);
        this.setCodeMirrorFields(this.moduleCountQuery, typeof (resultSet.countQuery) === "undefined" ? "" : resultSet.countQuery);
        this.setCodeMirrorFields(this.moduleOptions, JSON.stringify(resultSet.options, null, ' '));
    }

    // actions handled before save, such as checks
    beforeSave() {
        if (this.checkIfModuleIsSet(true)) {
            const moduleIdElement = document.getElementById("moduleId");

            if (moduleIdElement != null) {
                if (!isNaN(moduleIdElement.value)) {
                    const moduleId = parseInt(moduleIdElement.value);
                    const moduleSettingsModel = new ModuleSettingsModel(
                        moduleId,
                        this.moduleCustomQuery.getValue(),
                        this.moduleCountQuery.getValue(),
                        this.moduleOptions.getValue(),
                        document.getElementById("moduleName").value,
                        this.moduleIcon.value(),
                        this.moduleType.value(),
                        document.getElementById("moduleGroup").value
                    );
                    if (moduleSettingsModel.errors.length > 0) {
                        this.base.showNotification("notification", `Controleer de instellingen van de module, er zijn ${moduleSettingsModel.errors.length} fout(en) gevonden: ${moduleSettingsModel.errors.join(", ")}`, "error");
                        return;
                    }
                    this.updateModule(this.moduleCombobox.dataItem().id, moduleSettingsModel);
                } else {
                    this.base.showNotification("notification", `ID van de module moet een nummerieke waarde zijn!`, "error");
                }
            }
        }
    }

    async updateModule(id, moduleSettingsModel) {
        try {
            await Wiser2.api({
                url: `${this.base.settings.wiserApiRoot}modules/${id}/settings`,
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                data: JSON.stringify(moduleSettingsModel),
                method: "PUT"
            }).then(() => {
                this.base.showNotification("notification", `Module is succesvol bijgewerkt`, "success");
                this.getModules(true, moduleSettingsModel.id);
            });
        } catch(result){
            console.log(result);
            if (result.responseText.includes("Duplicate entry")) {
                this.base.showNotification("notification", `Het bijwerken van de module is mislukt, de ID van de module bestaat al.`, "error");
            } else {
                this.base.showNotification("notification", `Het bijwerken van de module is mislukt, probeer het opnieuw`, "error");
            }
        };
    }

    async setCodeMirrorFields(field, value) {
        if (field != null && field) {
            field.setValue((value != null && value) ? value : "");
            field.refresh();
        }
    }

    checkIfModuleIsSet(showNotification = true) {
        if (this.moduleCombobox &&
            this.moduleCombobox.dataItem() &&
            this.moduleCombobox.dataItem().id !== "" &&
            this.moduleListInitialized === true) {
            return true;
        } else {
            if (showNotification)
                this.base.showNotification("notification", `Selecteer eerst een module!`, "error");

            return false;
        }

    }
}
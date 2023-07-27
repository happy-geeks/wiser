import { QueryModel } from "../Scripts/QueryModel.js";

export class WiserQueryTab {
    constructor(base) {
        this.base = base;
        this.setupBindings();
        this.initializeKendoComponents();
    }

    async initializeKendoComponents() {
        this.queryCombobox = $("#queryList").kendoDropDownList({
            placeholder: "Select een query...",
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
    }

    async setupBindings() {
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
    }

    // actions handled before save, such as checks
    async beforeSave() {
        if (this.checkIfQueryIsSet(true)) {
            const queryModel = new QueryModel(this.queryCombobox.dataItem().id, document.getElementById("queryDescription").value, this.queryFromWiser.getValue(), document.getElementById("showInExportModule").checked, false, this.rolesWithPermissions.value().join(), document.getElementById("showInCommunicationModule").checked);
            await this.updateQuery(queryModel.id, queryModel);
        }
    }

    async onQueryComboBoxSelect(event) {
        if (this.checkIfQueryIsSet((event.userTriggered === true))) {
            this.getQueryById(this.queryCombobox.dataItem().id);
        }
    }

    async getQueries(reloadDataSource = true, queryIdToSelect = null) {
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
    }

    async updateQuery(id, queryModel) {
        try {
            await Wiser.api({
                url: `${this.base.settings.wiserApiRoot}queries/${id}`,
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                data: JSON.stringify(queryModel),
                method: "PUT"
            });

            this.base.showNotification("notification", `Query is succesvol bijgewerkt`, "success");
            await this.getQueries();
        }
        catch (exception) {
            console.error("Error while updating query", exception);
            this.base.showNotification("notification", `Het bijwerken van de queries is mislukt, probeer het opnieuw`, "error");
        }
    }

    async getQueryById(id) {
        const results = await Wiser.api({
            url: `${this.base.settings.wiserApiRoot}queries/${id}`,
            method: "GET"
        });

        await this.setQueryPropertiesToDefault();
        await this.setQueryProperties(results);
    }

    async addQuery(description) {
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
    }

    async deleteQueryById(id) {
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
    }

    async setQueryProperties(resultSet) {
        document.getElementById("queryDescription").value = resultSet.description;
        document.getElementById("showInExportModule").checked = resultSet.showInExportModule;
        document.getElementById("showInCommunicationModule").checked = resultSet.showInCommunicationModule;
        this.rolesWithPermissions.value(resultSet.rolesWithPermissions.split(","));
        await this.setCodeMirrorFields(this.queryFromWiser, resultSet.query);
    }

    async setQueryPropertiesToDefault() {
        document.getElementById("queryDescription").value = "";
        document.getElementById("showInExportModule").checked = false;
        document.getElementById("showInCommunicationModule").checked = false;
        this.queryFromWiser.setValue("")
        this.rolesWithPermissions.value([]);
    }

    async setCodeMirrorFields(field, value) {
        if (value && value !== "null" && field != null) {
            field.setValue(value);
            field.refresh();
        }
    }

    checkIfQueryIsSet(showNotification = true) {
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
    }

    hasChanges() {
        return false;
    }
}
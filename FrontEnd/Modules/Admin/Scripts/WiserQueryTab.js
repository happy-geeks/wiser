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

        if (this.base.settings.wiserVersion >= 210) {
            // In Wiser 2.1, we only load code mirror when we actually need it.
            await Misc.ensureCodeMirror();
        }

        this.queryFromWiser = CodeMirror.fromTextArea(document.getElementById("queryFromWiser"),
            {
                mode: "text/x-mysql",
                lineNumbers: true
            });

        // set query dropdown list
        this.getQueries();
    }

    async setupBindings() {
        // delete an query
        $(".delQueryBtn").kendoButton({
            click: () => {
                if (!this.checkIfEntityIsSet()) {
                    return;
                }
                const tabNameProp = this.listOfTabProperties;
                const index = tabNameProp.select().index();
                const dataItem = tabNameProp.dataSource.view()[index];
                if (!dataItem) {
                    this.base.showNotification("notification",
                        "Item is niet succesvol verwijderd, probeer het opnieuw",
                        "error");
                    return;
                }

                // ask for user confirmation before deleting
                this.base.openDialog("Item verwijderen",
                    "Weet u zeker dat u dit item wil verwijderen?",
                    this.base.kendoPromptType.CONFIRM).then(() => {
                    this.addRemoveEntityProperty("", dataItem.id);
                });
            },
            icon: "delete"
        });
    }

    async onQueryComboBoxSelect(event) {
        console.log("onQueryComboBoxSelect", this.checkIfQueryIsSet(false));
        if (this.checkIfQueryIsSet((event.userTriggered === true))) {
            this.getQueryById(this.queryCombobox.dataItem().id);
        }
    }

    async getQueries(reloadDataSource = true) {
        if (reloadDataSource) {
            this.queryList = new kendo.data.DataSource({
                transport: {
                    cache: "inmemory",
                    read: {
                           url: `${this.base.settings.wiserApiRoot}/queries/`
                    }
                }
            });
        }

        this.queryCombobox.setDataSource(this.queryList);
    }

    async updateQuery(queryModel) {
        const successful = await $.ajax({
            url: `${this.base.settings.wiserApiRoot}/queries/`,
            data: queryModel,
            method: "PUT"
        });

        if (successful)
            this.getQueries();
    }

    async getQueryById(id) {
        const results = await $.ajax({
            url: `${this.base.settings.wiserApiRoot}/queries/${id}`,
            method: "GET"
        });

        this.setQueryPropertiesToDefault();
        this.setQueryProperties(results);
    }

    async deleteQueryById(id) {
        const successful = await $.ajax({
            url: `${this.base.settings.wiserApiRoot}/queries/${id}`,
            method: "DELETE"
        });

        if (successful)
            this.setQueryPropertiesToDefault();

    }

    async setQueryProperties(resultSet) {
        console.log(resultSet);
        document.getElementById("queryDescription").value = resultSet.description;
        document.getElementById("showInExportModule").checked = resultSet.show_in_export_module;
        this.setCodeMirrorFields(this.queryFromWiser, resultSet.query);
    }

    async setQueryPropertiesToDefault() {
        document.getElementById("queryDescription").value = "";
        document.getElementById("showInExportModule").checked = false;
        this.queryFromWiser.setValue("");
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
            if(showNotification)
                this.base.showNotification("notification", `Selecteer eerst een query!`, "error");

            return false;
        }
            
    }
}
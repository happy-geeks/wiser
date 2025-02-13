import {Wiser} from "../../Base/Scripts/Utils";

export class DatabaseMigrationsTab {
    constructor(base) {
        this.base = base;

        this.initialize();
    }

    initialize() {
        this.migrationList = $("#migrationList").kendoGrid({
            dataSource: {
                transport: {
                    read: async (transportOptions) => {
                        try {
                            const migrations = await Wiser.api({
                                url: `${this.base.settings.wiserApiRoot}database/tenant-migrations?manualMigrationsOnly=true`,
                                contentType: 'application/json',
                                method: "GET"
                            });

                            transportOptions.success(migrations);
                        } catch (exception) {
                            console.error(exception);
                            transportOptions.error(exception);
                        }
                    }
                },
                schema: {
                    model: {
                        id: "id",
                        fields: {
                            id: {
                                type: "string"
                            },
                            name: {
                                type: "string"
                            },
                            description: {
                                type: "string"
                            },
                            isCustomMigration: {
                                type: "boolean"
                            },
                            requiresManualTrigger: {
                                type: "boolean"
                            },
                            lastRunOn: {
                                type: "date"
                            },
                            lastUpdateOn: {
                                type: "date"
                            }
                        }
                    }
                }
            },
            pageable: false,
            scrollable: true,
            sortable: true,
            persistSelection: true,
            height: 500,
            width: "100%",
            columns: [
                { selectable: true, width: "25px" },
                { field: "name", title: "Naam", width: "250px" },
                { field: "lastUpdateOn", title: "Laatst toegevoegd/bijgewerkt op", width: "250px", format: "{0:dd MMMM yyyy}" },
                { field: "lastRunOn", title: "Laatst uitgevoerd op", width: "250px", template: "#= lastRunOn && lastRunOn.getFullYear() > 2020 ? kendo.toString(lastRunOn, 'dd MMMM yyyy HH:mm:ss') : 'Nooit' #" },
            ],
            detailTemplate: `<span class="migration-description">#= description #</span>`,
            toolbar: [{
                name: "doManualMigrations",
                text: "Geselecteerde migraties uitvoeren",
                iconClass: "k-icon k-i-upload",
            }]
        }).data("kendoGrid");

        $(".k-grid-doManualMigrations").click(async (event) => {
            let selectedMigrations = [];
            for (const row of this.migrationList.select()) {
                selectedMigrations.push(this.migrationList.dataItem(row));
            }

            if (selectedMigrations.length === 0) {
                await kendo.confirm("Je hebt geen migraties geselecteerd. Wil je alle migraties uitvoeren?");
                selectedMigrations = this.migrationList.dataItems();
            } else {
                await kendo.confirm(`Weet je zeker dat je de volgende migraties wilt uitvoeren? <br /><ul><li>${selectedMigrations.map(item => item.name).join("</li><li>")}</li></ul>`);
            }

            try {
                document.querySelector(".loaderWrap").classList.add("active");
                const migrationIds = selectedMigrations.map(item => item.id);

                await Wiser.api({
                    url: `${this.base.settings.wiserApiRoot}database/manual-tenant-migrations`,
                    contentType: 'application/json',
                    method: "PUT",
                    data: JSON.stringify(migrationIds)
                });

                this.migrationList.dataSource.read();
            }
            catch (exception) {
                console.error(exception);
                kendo.alert("Er is een fout opgetreden bij het uitvoeren van de migraties. Zie de console voor meer informatie.");
            }
            finally {
                document.querySelector(".loaderWrap").classList.remove("active");
            }
        });
    }
}
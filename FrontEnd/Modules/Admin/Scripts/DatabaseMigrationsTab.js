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
                                url: `${this.base.settings.wiserApiRoot}database/tenant-migrations`,
                                contentType: 'application/json',
                                method: "GET"
                            })

                            transportOptions.success(migrations);
                        } catch (exception) {
                            console.error(exception);
                            transportOptions.error(exception);
                        }
                    }
                }
            },
            pageable: false,
            scrollable: true,
            sortable: true,
            height: 500,
            width: "100%"
            //columns: []
        }).data("kendoGrid");
    }
}
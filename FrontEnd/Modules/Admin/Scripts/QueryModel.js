export class QueryModel {
    constructor(id = null, description = null, query = null, showInExportModule = false, availableForRendering = false) {
        this.id = id;
        this.description = description;
        this.query = query;
        this.showInExportModule = showInExportModule;
        this.availableForRendering = availableForRendering;
    }
}

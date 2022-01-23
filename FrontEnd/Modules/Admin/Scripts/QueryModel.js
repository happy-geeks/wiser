export class QueryModel {
    constructor(id = null, description = null, query = null,showInExportModule = false) {
        this.id = id;
		this.description = description;
		this.query = query;
        this.showInExportModule = showInExportModule;
    }
}

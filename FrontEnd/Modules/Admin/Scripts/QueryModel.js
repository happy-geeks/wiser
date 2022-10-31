export class QueryModel {
    constructor(id = null, description = null, query = null, showInExportModule = false, availableForRendering = false, rolesWithPermissions = "", showInCommunicationModule = false) {
        this.id = id;
        this.description = description;
        this.query = query;
        this.showInExportModule = showInExportModule;
        this.showInCommunicationModule = showInCommunicationModule;
        this.availableForRendering = availableForRendering;
        this.rolesWithPermissions = rolesWithPermissions;
    }
}

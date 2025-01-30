export class EntityModel {
    constructor(id=-1, name="", moduleId = 700, acceptedChildtypes = "", icon = "", iconAdd = "", showInTreeView = 1, queryAfterInsert = "", queryAfterUpdate = "",
        queryBeforeUpdate = "", queryBeforeDelete = "", color = "blue", showInSearch = 1, showOverviewTab = 1, saveTitleAsSeo = 1, apiAfterInsert = "",
        apiAfterUpdate = "", apiBeforeUpdate = "", apiBeforeDelete = "", showTitleField = 1, friendlyName = "", saveHistory = 1, defaultOrdering = "", iconExpanded = "", dedicatedTablePrefix = "",
        templateQuery = "", templateHtml = "", showInDashboard = false, enableMultipleEnvironments = false, allowCreationOnMainFromBranch = false) {
        this.id = id;
        this.name = name;
        this.moduleId = moduleId;
        this.acceptedChildtypes = acceptedChildtypes;
        this.icon = icon;
        this.iconAdd = iconAdd;
        this.showInTreeView = showInTreeView;
        this.queryAfterInsert = queryAfterInsert;
        this.queryAfterUpdate = queryAfterUpdate;
        this.queryBeforeUpdate = queryBeforeUpdate;
        this.queryBeforeDelete = queryBeforeDelete;
        this.color = color;
        this.showInSearch = showInSearch;
        this.showOverviewTab = showOverviewTab;
        this.saveTitleAsSeo = saveTitleAsSeo;
        this.apiAfterInsert = apiAfterInsert;
        this.apiAfterUpdate = apiAfterUpdate;
        this.apiBeforeUpdate = apiBeforeUpdate;
        this.apiBeforeDelete = apiBeforeDelete;
        this.showTitleField = showTitleField;
        this.friendlyName = friendlyName;
        this.saveHistory = saveHistory;
        this.defaultOrdering = defaultOrdering;
        this.iconExpanded = iconExpanded;
        this.dedicatedTablePrefix = dedicatedTablePrefix;
        this.templateQuery = templateQuery;
        this.templateHtml = templateHtml;
        this.showInDashboard = showInDashboard;
        this.enableMultipleEnvironments = enableMultipleEnvironments;
        this.allowCreationOnMainFromBranch = allowCreationOnMainFromBranch;
    }
}

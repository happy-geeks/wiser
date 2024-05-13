export class LinkSettingsModel {
    constructor(id = -1, type = null, destinationEntityType = null, sourceEntityType = null, name = null, showInTreeView = null, showInDataSelector =
        null, relationship = null, duplicationMethod = null, useItemParentId = null, useDedicatedTable = null) {

        this.id = id;
        this.type = type;
        this.destinationEntityType = destinationEntityType;
        this.sourceEntityType = sourceEntityType;
        this.name = name;
        this.showInTreeView = showInTreeView;
        this.showInDataSelector = showInDataSelector;
        this.relationship = relationship;
        this.duplicationMethod = duplicationMethod;
        this.useItemParentId = useItemParentId;
        this.useDedicatedTable = useDedicatedTable;
    }
}
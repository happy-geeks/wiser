export class ModuleSettingsModel {
    constructor(id = -1, customQuery = null, countQuery = null, options = null, name = null, icon = null, color = null, type = null, group = null) {
        this.id = id;
        this.customQuery = customQuery;
        this.countQuery = countQuery;
        this.options = options;
        this.name = name;
        this.icon = icon;
        this.color = color;
        this.type = type;
        this.group = group;
    }
}
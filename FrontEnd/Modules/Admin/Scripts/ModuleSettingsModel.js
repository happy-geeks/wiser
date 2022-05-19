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
        this.errors = [];
        this.validation();
    }

    isJsonString(str) {
        try {
            JSON.parse(str);
        } catch (e) {
            return false;
        }
        return true;
    }

    validation() {
        if (this.options !== null && this.options !== "" && !this.isJsonString(this.options)) {
            this.errors.push("Foutieve optie JSON");
        }
    }
}
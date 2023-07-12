export class EntityPropertyModel {
    constructor() {
        this.id = 0;
        this.moduleId = 0;
        this.entityType = null;
        this.linkType = 0;
        this.tabName = null;
        this.displayName = null;
        this.propertyName = null;
        this.overview = null;
        this.groupName = null;
        this.inputType = null;
        this.explanation = null;
        this.regexValidation = null;
        this.mandatory = null;
        this.readOnly = null;
        this.defaultValue = null;
        this.automation = null;
        this.css = null;
        this.width = null;
        this.height = null;
        this.dependsOn = null;
        this.languageCode = null;
        this.customScript = null;
        this.alsoSaveSeoValue = null;
        this.options = null;
        this.dataQuery = null;
        this.gridDeleteQuery = null;
        this.gridInsertQuery = null;
        this.gridUpdateQuery = null;
        this.ordering = 0;
        this.extendedExplanation = false;
        this.actionQuery = null;
        this.searchQuery = null;
        this.searchCountQuery = null;
        this.saveOnChange = false;
        this.labelStyle = null;
        this.labelWidth = null;
        this.enableAggregation = false;
        this.aggregateOptions = null;
        this.accessKey = null;
        this.visibilityPathRegex = null;
    }

    createOptionsJson(clean = true) {
        // overwrite options with json
        this.options = this.createJson(clean);
    }
    // create json for options
    createJson(clean = true) {
        const options = this.options;
        //  return null if no options have been added
        if (!Object.keys(options).length) {
            return null;
        }
        // remove null properties if set to true
        if (clean) {
            for (let propName in options) {
                if (!options.hasOwnProperty(propName)) {
                    continue;
                }

                if (options[propName] === null || options[propName] === undefined || options[propName] === "") {
                    delete options[propName];
                }
            }
        }
        // return options
        return JSON.stringify(options);
    }
}
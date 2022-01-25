export class EntityPropertyModel {
    constructor(id, entityName, tabName, visibleInOverview, overviewFieldtype, overviewWidth, groupName, inputtype, displayName, propertyName,
        explanation, regexValidation, mandatory, readonly, defaultValue, automation, css, width, height, dependsOnField, dependsOnOperator, dependsOnValue,
        languageCode, customScript, alsoSaveSeoValue, dataQuery, options = {}, gridDeleteQuery = null, gridInsertQuery = null, gridUpdateQuery = null) {
        this.id = id;
        this.entityName = entityName;
        this.tabName = tabName;
        this.displayName = displayName;
        this.propertyName = propertyName;
        this.visibleInOverview = visibleInOverview;
        this.overviewFieldtype = overviewFieldtype;
        this.overviewWidth = overviewWidth;
        this.groupName = groupName;
        this.inputtype = inputtype;
        this.explanation = explanation;
        this.regexValidation = regexValidation;
        this.mandatory = mandatory;
        this.readonly = readonly;
        this.defaultValue = defaultValue;
        this.automation = automation;
        this.css = css;
        this.width = width;
        this.height = height;
        this.dependsOnField = dependsOnField;
        this.dependsOnOperator = dependsOnOperator;
        this.dependsOnValue = dependsOnValue;
        this.languageCode = languageCode;
        this.customScript = customScript;
        this.alsoSaveSeoValue = alsoSaveSeoValue;
        this.options = options;
        this.dataQuery = dataQuery;
        this.gridDeleteQuery = gridDeleteQuery;
        this.gridInsertQuery = gridInsertQuery;
        this.gridUpdateQuery = gridUpdateQuery;
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
                if (options.hasOwnProperty(propName)) {
                    if (options[propName] === null || options[propName] === undefined) {
                        delete options[propName];
                    }
                }
            }
        }
        // return options
        return JSON.stringify(options);
    }
}
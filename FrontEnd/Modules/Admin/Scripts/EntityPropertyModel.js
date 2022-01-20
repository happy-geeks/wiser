export class EntityPropertyModel {
    constructor(id, entity_name, tab_name, visible_in_overview, overview_fieldtype, overview_width, group_name, inputtype, display_name, property_name,
        explanation, regex_validation, mandatory, readonly, default_value, automation, css, width, height, depends_on_field, depends_on_operator, depends_on_value,
        language_code, custom_script, also_save_seo_value, data_query, options = {}, grid_delete_query = null, grid_insert_query = null, grid_update_query = null) {
        this.id = id;
        this.entity_name = entity_name;
        this.tab_name = tab_name;
        this.display_name = display_name;
        this.property_name = property_name;
        this.visible_in_overview = visible_in_overview;
        this.overview_fieldtype = overview_fieldtype;
        this.overview_width = overview_width;
        this.group_name = group_name;
        this.inputtype = inputtype;
        this.explanation = explanation;
        this.regex_validation = regex_validation;
        this.mandatory = mandatory;
        this.readonly = readonly;
        this.default_value = default_value;
        this.automation = automation;
        this.css = css;
        this.width = width;
        this.height = height;
        this.depends_on_field = depends_on_field;
        this.depends_on_operator = depends_on_operator;
        this.depends_on_value = depends_on_value;
        this.language_code = language_code;
        this.custom_script = custom_script;
        this.also_save_seo_value = also_save_seo_value;
        this.options = options;
        this.data_query = data_query;
        this.grid_delete_query = grid_delete_query;
        this.grid_insert_query = grid_insert_query;
        this.grid_update_query = grid_update_query;
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
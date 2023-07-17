export class ModuleSettingsModel {
    constructor(id = -1, customQuery = null, countQuery = null, options = null, name = null, icon = null, type = null, group = null) {
        this.id = id;
        this.customQuery = customQuery;
        this.countQuery = countQuery;
        this.options = options;
        this.name = name;
        this.icon = icon;
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

    /**
     * Check if the current model has changes compared to the original data.
     * @param {object} originalData The original data of the module.
     * @returns {boolean} True if the model has changes, false otherwise.
     */
    hasChanges(originalData) {
        // Make copies of the original and current data.
        let original = $.extend(true, {}, originalData);
        let current = $.extend(true, {}, this);

        // Make sure the options property contains a valid JSON object.
        switch (typeof original.options) {
            case "string":
                original.options = JSON.parse(original.options);
                break;
            case "undefined":
                original.options = {};
        }

        switch (typeof current.options) {
            case "string":
                current.options = JSON.parse(current.options);
                break;
            case "undefined":
                current.options = {};
        }

        // Delete properties we don't need to check.
        delete current.errors;
        delete original.canCreate;
        delete original.canDelete;
        delete original.canRead;
        delete original.canWrite;
        delete original.description;

        // Remove empty properties, so that null and empty string are seen as the same.
        for (let key in current) {
            if (current.hasOwnProperty(key) && !current[key]) {
                delete current[key];
            }
        }

        for (let key in original) {
            if (original.hasOwnProperty(key) && !original[key]) {
                delete original[key];
            }
        }

        // Comparer function for JSON.stringify, to sort all properties alphabetically.
        const comparer = (key, value) => {
            if (typeof value !== "object" || Array.isArray(value)) {
                return value;
            }

            const sortedObject = {};
            Object.keys(value).sort().forEach((sortedKey) => {
                sortedObject[sortedKey] = value[sortedKey];
            });

            return sortedObject;
        };

        // And finally, compare the two objects.
        return JSON.stringify(original, comparer) !== JSON.stringify(current, comparer);
    }
}
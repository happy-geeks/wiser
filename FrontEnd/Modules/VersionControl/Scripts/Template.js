import {Wiser} from "../../Base/Scripts/Utils.js";
import "../../Base/Scripts/Processing.js";


require("@progress/kendo-ui/js/kendo.button.js");
require("@progress/kendo-ui/js/kendo.dialog.js");
require("@progress/kendo-ui/js/kendo.window.js");
require("@progress/kendo-ui/js/kendo.tabstrip.js");
require("@progress/kendo-ui/js/kendo.validator.js");
require("@progress/kendo-ui/js/kendo.combobox.js");
require("@progress/kendo-ui/js/cultures/kendo.culture.nl-NL.js");
require("@progress/kendo-ui/js/messages/kendo.messages.nl-NL.js");

/**
* Class for any and all functionality for fields.
*/
export class Template {

    /**
     * Initializes a new instance of the Fields class.
     * @param {VersionControl} base An instance of the base class (DynamicItems).
     */
    constructor(base) {
        this.base = base;
    }

    initialize() {
        
    }

    async PublishTemplate(templateId, environment, version) {
        try {

            console.log(environment);
            const createCommit = await Wiser.api({
                url: `${this.base.settings.wiserApiRoot}/Templates/${templateId}/publish/${environment}/${version}`,
                method: "POST",
                contentType: "application/json",
            });

            return createCommit;

        } catch (exception) {
            console.error(exception);
            kendo.alert("Er is iets fout gegaan. Sluit a.u.b. deze module, open deze daarna opnieuw en probeer het vervolgens opnieuw. Of neem contact op als dat niet werkt.");
        }
    }

    async GetTemplatesWithLowerVersion(templateId, version) {
        try {

            const templateCommitData = {
                TemplateId: templateId,
                Version: version
            }

            const templateData = await Wiser.api({
                url: `${this.base.settings.wiserApiRoot}version-control/${templateId}/${version}`,
                method: "GET",
                contentType: "application/json",
                data: JSON.stringify(templateCommitData)
            });

            return templateData;
        } catch (exception) {
            console.error(exception);
            kendo.alert("Er is iets fout gegaan. Sluit a.u.b. deze module, open deze daarna opnieuw en probeer het vervolgens opnieuw. Of neem contact op als dat niet werkt.");
        }
    }
    
    async GetTemplatesFromCommit(commitId) {

        try {
            const createCommit = await Wiser.api({
                url: `${this.base.settings.wiserApiRoot}version-control/templates-of-commit/${commitId}`,
                method: "GET",
                contentType: "application/json",
            });

            return createCommit;

        } catch (exception) {
            console.error(exception);
            kendo.alert("Er is iets fout gegaan. Sluit a.u.b. deze module, open deze daarna opnieuw en probeer het vervolgens opnieuw. Of neem contact op als dat niet werkt.");
        }
    }
}
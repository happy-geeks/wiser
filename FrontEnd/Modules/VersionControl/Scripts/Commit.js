import { Dates, Wiser2, Misc } from "../../Base/Scripts/Utils.js";
import "../../Base/Scripts/Processing.js";
import { DateTime } from "luxon";

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
export class Commit {

    /**
     * Initializes a new instance of the Fields class.
     * @param {VersionControl} base An instance of the base class (DynamicItems).
     */
    constructor(base) {
        this.base = base;
    }

    /**
     * Do all initializations for the Fields class, such as adding bindings.
     */
    initialize() {
      
    }

    async CreateNewCommit(commitMessage) {
        try {
            const commitData = {
                Description: commitMessage,
            };

            const createCommit = await Wiser2.api({
                url: `${this.base.settings.wiserApiRoot}VersionControl`,
                method: "PUT",
                contentType: "application/json",
                data: JSON.stringify(commitData)
            });

            return createCommit;

        } catch (exception) {
            kendo.alert("Er is iets fout gegaan. Sluit a.u.b. deze module, open deze daarna opnieuw en probeer het vervolgens opnieuw. Of neem contact op als dat niet werkt.");
        }
    }


    async GetCommitWithId() {
        try {

            const templateData = await Wiser2.api({
                url: `${this.base.settings.wiserApiRoot}VersionControl/Commit`,
                method: "GET",
                contentType: "application/json",
            });

            return templateData
        } catch (exception) {
            console.error(exception);
            kendo.alert("Er is iets fout gegaan. Sluit a.u.b. deze module, open deze daarna opnieuw en probeer het vervolgens opnieuw. Of neem contact op als dat niet werkt.");
        };

    }

    async PutTemplateCommit(commitId, templateVersionId, version, enviornment) {
        try {

            const TemplateCommitData = {
                CommitId: commitId,
                TemplateId: templateVersionId,
                Version: version,
                Enviornment: enviornment

            }
            
            const createCommit = await Wiser2.api({
                url: `${this.base.settings.wiserApiRoot}VersionControl/template-commit`,
                method: "PUT",
                contentType: "application/json",
                data: JSON.stringify(TemplateCommitData)
            });

            return createCommit;

        } catch (exception) {
            console.error(exception);
            kendo.alert("Er is iets fout gegaan. Sluit a.u.b. deze module, open deze daarna opnieuw en probeer het vervolgens opnieuw. Of neem contact op als dat niet werkt.");
        }
    }

    async CompleteCommit(commitId, commitCompleted) {
        try {
            const createCommit = await Wiser2.api({
                url: `${this.base.settings.wiserApiRoot}VersionControl/${commitId}/complete-commit/${commitCompleted}`,
                method: "PUT",
                contentType: "application/json",
            });

            return createCommit;

        } catch (exception) {
            kendo.alert("Er is iets fout gegaan. Sluit a.u.b. deze module, open deze daarna opnieuw en probeer het vervolgens opnieuw. Of neem contact op als dat niet werkt.");
        }
    }


}
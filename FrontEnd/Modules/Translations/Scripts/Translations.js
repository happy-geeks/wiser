import {Wiser} from "../../Base/Scripts/Utils";
import * as path from "path";

((settings) => {
    /**
     * Main class.
     */
    class Translations {

        /**
         * Initializes a new instance of Translations.
         */
        constructor(settings) {
            // Default settings
            this.settings = {
                moduleId: 0,
                encryptedModuleId: "",
                customerId: 0,
                initialItemId: null,
                iframeMode: false,
                gridViewMode: false,
                openGridItemsInBlock: false,
                username: "Onbekend",
                userEmailAddress: "",
                userType: ""
            };
            Object.assign(this.settings, settings);

            // Setup any settings from the body element data. These settings are added via the Wiser backend and they take preference.
            Object.assign(this.settings, $("body").data());
            
            
            $(document).ready(() => {
                this.onPageReady();
            });
        }

        /**
         * Event that will be fired when the page is ready.
         */
        async onPageReady() {
        }
        
        /**
         * Get a specified module's resource file (translation) in json form.
         * Should be located somewhere in Modules/Translations/Resources
         * @param {string} pathToModuleInTranslationResources Path to the modules folder using dots, should be Api.Modules.Translations.Resources.<pathtomodule>.
         * @param {string} cultureAndCountry culture and country, formatted as xx-YY e.g. en-GB or nl-NL <pathtomodule>. Country (YY) Optional
         * @returns {Object} Resources file translations in object form
         */
        async getTranslationOfModule(pathToModuleInTranslationResources, cultureAndCountry) {
            const result = await Wiser.api({
                url: `${this.settings.wiserApiRoot}Translations/get-translations-for-module`,
                data: {pathToResourceFileDirectory: pathToModuleInTranslationResources, cultureAndCountry: cultureAndCountry},
                method: "GET",
            });
            try {
                return JSON.parse(result);
            } catch (exception) {
                if (exception instanceof SyntaxError) {
                    console.error("Could not find translation in ${pathToModuleInTranslationResources}," +
                        " or could not find given translation with language code ${cultureAndCountry}.", exception)
                }
                else {
                    // Re-throw the error if we don't want to catch that specific error.
                    throw exception;
                }
                return null;
            }
        }
    }

    // Initialize the Translations class and make one instance of it globally available.
    window.translations = new Translations()
})();
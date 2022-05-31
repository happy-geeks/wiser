import BaseService from "./base.service";

export default class ContentBuildersService extends BaseService {
    /**
     * Gets the HTML from an item, to load in the content builder.
     * @param {int} itemId The ID of the item that contains the HTML.
     * @param {string} languageCode The language code.
     * @param {string} propertyName The name of the property that contains the HTML.
     * @returns {string} The HTML.
     */
    async getHtml(itemId, languageCode = "", propertyName = "html") {
        const result = {};

        try {
            const response = await this.base.api.get(`/api/v3/content-builder/html?itemId=${encodeURIComponent(itemId)}&languageCode=${encodeURIComponent(languageCode)}&propertyName=${encodeURIComponent(propertyName)}`);
            result.success = true;
            result.data = response.data;
        } catch (error) {
            result.success = false;
            console.error("Error while getting HTML for content builder", error);

            if (error.response) {
                console.warn(error.response);
                // The request was made and the server responded with a status code
                // that falls out of the range of 2xx
                result.message = "Er is een onbekende fout opgetreden tijdens het ophalen van de HTML. Probeer het a.u.b. nogmaals of neem contact op met ons.";
            } else if (error.request) {
                // The request was made but no response was received
                // `error.request` is an instance of XMLHttpRequest in the browser and an instance of
                // http.ClientRequest in node.js
                console.warn(error.request);
                result.message = "Er is een onbekende fout opgetreden tijdens het ophalen van de HTML. Probeer het a.u.b. nogmaals of neem contact op met ons.";
            } else {
                // Something happened in setting up the request that triggered an Error
                console.warn(error.message);
                result.message = "Er is een onbekende fout opgetreden tijdens het ophalen van de HTML. Probeer het a.u.b. nogmaals of neem contact op met ons.";
            }
        }

        return result;
    }

    /**
     * Gets the custom content builder snippets.
     * @returns {any} An array with all custom snippets.
     */
    async getCustomerSnippets() {
        const result = {};

        try {
            const response = await this.base.api.get(`/api/v3/content-builder/snippets`);
            result.success = true;

            const customerSnippets = response.data;
            
            const snippetCategories = [];
            for (let snippet of customerSnippets) {
                snippet.fromWiser = true;
                const existingCategory = snippetCategories.filter(c => c[1].toLowerCase() === snippet.category.toLowerCase());
                if (!existingCategory.length) {
                    snippetCategories.push([snippet.categoryId.toString(), snippet.category]);
                    snippet.category = snippet.categoryId.toString();
                } else {
                    snippet.category = existingCategory[0][0].toString();
                }
            }

            // Default content builder categories.
            snippetCategories.push(...[[120, "Basic"], [118, "Article"], [101, "Headline"], [119, "Buttons"], [102, "Photos"], [103, "Profile"], [116, "Contact"], [104, "Products"], [105, "Features"], [106, "Process"], [107, "Pricing"], [108, "Skills"], [109, "Achievements"], [110, "Quotes"], [111, "Partners"], [112, "As Featured On"], [113, "Page Not Found"], [114, "Coming Soon"], [115, "Help, FAQ"]]);

            result.data = {
                customerSnippets: customerSnippets,
                snippetCategories: snippetCategories
            };
        } catch (error) {
            result.success = false;
            console.error("Error while getting HTML for content builder", error);

            if (error.response) {
                console.warn(error.response);
                // The request was made and the server responded with a status code
                // that falls out of the range of 2xx
                result.message = "Er is een onbekende fout opgetreden tijdens het ophalen van de HTML. Probeer het a.u.b. nogmaals of neem contact op met ons.";
            } else if (error.request) {
                // The request was made but no response was received
                // `error.request` is an instance of XMLHttpRequest in the browser and an instance of
                // http.ClientRequest in node.js
                console.warn(error.request);
                result.message = "Er is een onbekende fout opgetreden tijdens het ophalen van de HTML. Probeer het a.u.b. nogmaals of neem contact op met ons.";
            } else {
                // Something happened in setting up the request that triggered an Error
                console.warn(error.message);
                result.message = "Er is een onbekende fout opgetreden tijdens het ophalen van de HTML. Probeer het a.u.b. nogmaals of neem contact op met ons.";
            }
        }

        return result;
    }
}
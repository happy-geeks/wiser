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
    async getTenantSnippets() {
        const result = {};

        try {
            const response = await this.base.api.get(`/api/v3/content-builder/snippets`);
            result.success = true;

            const tenantSnippets = response.data;
            
            const snippetCategories = [];
            for (let snippet of tenantSnippets) {
                snippet.fromWiser = true;
                const existingCategory = snippetCategories.filter(c => c[1].toLowerCase() === snippet.category.toLowerCase());
                if (!existingCategory.length) {
                    snippetCategories.push([snippet.categoryId.toString(), snippet.category]);
                    snippet.category = snippet.categoryId.toString();
                } else {
                    snippet.category = existingCategory[0][0].toString();
                }
            }

            result.data = {
                tenantSnippets: tenantSnippets,
                snippetCategories: snippetCategories
            };
        } catch (error) {
            result.success = false;
            console.error("Error while getting snippets for content builder", error);

            if (error.response) {
                console.warn(error.response);
                // The request was made and the server responded with a status code
                // that falls out of the range of 2xx
                result.message = "Er is een onbekende fout opgetreden tijdens het ophalen van de snippets. Probeer het a.u.b. nogmaals of neem contact op met ons.";
            } else if (error.request) {
                // The request was made but no response was received
                // `error.request` is an instance of XMLHttpRequest in the browser and an instance of
                // http.ClientRequest in node.js
                console.warn(error.request);
                result.message = "Er is een onbekende fout opgetreden tijdens het ophalen van de snippets. Probeer het a.u.b. nogmaals of neem contact op met ons.";
            } else {
                // Something happened in setting up the request that triggered an Error
                console.warn(error.message);
                result.message = "Er is een onbekende fout opgetreden tijdens het ophalen van de snippets. Probeer het a.u.b. nogmaals of neem contact op met ons.";
            }
        }

        return result;
    }

    /**
     * Gets the custom content builder snippets.
     * @returns {any} An array with all custom snippets.
     */
    async getTemplateCategories() {
        const result = {};

        try {
            const response = await this.base.api.get(`/api/v3/content-builder/template-categories`);
            result.success = true;
            result.data = response.data;
            if (result.data && result.data.length > 0) {
                result.data = result.data.map(x => {
                    return {
                        id: x.categoryId,
                        designId: 1,
                        name: x.category
                    };
                });
            }
        } catch (error) {
            result.success = false;
            console.error("Error while getting template categories for content box", error);

            if (error.response) {
                console.warn(error.response);
                // The request was made and the server responded with a status code
                // that falls out of the range of 2xx
                result.message = "Er is een onbekende fout opgetreden tijdens het ophalen van de categorieën voor templates. Probeer het a.u.b. nogmaals of neem contact op met ons.";
            } else if (error.request) {
                // The request was made but no response was received
                // `error.request` is an instance of XMLHttpRequest in the browser and an instance of
                // http.ClientRequest in node.js
                console.warn(error.request);
                result.message = "Er is een onbekende fout opgetreden tijdens het ophalen van de categorieën voor templates. Probeer het a.u.b. nogmaals of neem contact op met ons.";
            } else {
                // Something happened in setting up the request that triggered an Error
                console.warn(error.message);
                result.message = "Er is een onbekende fout opgetreden tijdens het ophalen van de categorieën voor templates. Probeer het a.u.b. nogmaals of neem contact op met ons.";
            }
        }

        return result;
    }
    
    /**
     * Gets the framework to use in the content builder.
     * @returns {string} The name of the framework.
     */
    async getFramework() {
        const result = {};

        try {
            const response = await this.base.api.get(`/api/v3/content-builder/framework`);
            result.success = true;
            result.data = response.data;
        } catch (error) {
            result.success = false;
            console.error("Error while getting framework for content builder", error);

            if (error.response) {
                console.warn(error.response);
                // The request was made and the server responded with a status code
                // that falls out of the range of 2xx
                result.message = "Er is een onbekende fout opgetreden tijdens het ophalen van het framework. Probeer het a.u.b. nogmaals of neem contact op met ons.";
            } else if (error.request) {
                // The request was made but no response was received
                // `error.request` is an instance of XMLHttpRequest in the browser and an instance of
                // http.ClientRequest in node.js
                console.warn(error.request);
                result.message = "Er is een onbekende fout opgetreden tijdens het ophalen van het framework. Probeer het a.u.b. nogmaals of neem contact op met ons.";
            } else {
                // Something happened in setting up the request that triggered an Error
                console.warn(error.message);
                result.message = "Er is een onbekende fout opgetreden tijdens het ophalen van het framework. Probeer het a.u.b. nogmaals of neem contact op met ons.";
            }
        }

        return result;
    }
}
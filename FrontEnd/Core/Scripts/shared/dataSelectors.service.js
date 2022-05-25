import BaseService from "./base.service";

export default class DataSelectorsService extends BaseService {
    /**
     * Gets all data selectors.
     * @param {bool} forRendering Whether to only get data selectors for rendering in HTML editors.
     * @returns {any} An array with all available data selectors.
     */
    async getAll(forRendering = false) {
        const result = {};

        try {
            const response = await this.base.api.get(`/api/v3/data-selectors?forRendering=${forRendering}`);
            result.success = true;
            result.data = response.data;
        } catch (error) {
            result.success = false;
            console.error("Error get data selectors", typeof(error.toJSON) === "function" ? error.toJSON() : error);
            result.message = "Er is een onbekende fout opgetreden tijdens het ophalen van data selectors. Probeer het a.u.b. nogmaals of neem contact op met ons.";

            if (error.response) {
                // The request was made and the server responded with a status code
                // that falls out of the range of 2xx
                console.warn(error.response);
            } else if (error.request) {
                // The request was made but no response was received
                // `error.request` is an instance of XMLHttpRequest in the browser and an instance of
                // http.ClientRequest in node.js
                console.warn(error.request);
            } else {
                // Something happened in setting up the request that triggered an Error
                console.warn(error.message);
            }
        }

        return result;
    }
    
    /**
     * Gets all data selector templates.
     * @returns {any} An array with all available data selector templates.
     */
    async getTemplates() {
        const result = {};

        try {
            const response = await this.base.api.get(`/api/v3/data-selectors/templates`);
            result.success = true;
            result.data = response.data;
        } catch (error) {
            result.success = false;
            console.error("Error get data selectors", typeof(error.toJSON) === "function" ? error.toJSON() : error);
            result.message = "Er is een onbekende fout opgetreden tijdens het ophalen van data selector templates. Probeer het a.u.b. nogmaals of neem contact op met ons.";

            if (error.response) {
                // The request was made and the server responded with a status code
                // that falls out of the range of 2xx
                console.warn(error.response);
            } else if (error.request) {
                // The request was made but no response was received
                // `error.request` is an instance of XMLHttpRequest in the browser and an instance of
                // http.ClientRequest in node.js
                console.warn(error.request);
            } else {
                // Something happened in setting up the request that triggered an Error
                console.warn(error.message);
            }
        }

        return result;
    }

    /**
     * Gets all data selector templates.
     * @param {string} html The HTML to generate the preview for.
     * @returns {string} The HTML with the preview for the data selector.
     */
    async generatePreview(html) {
        const result = {};

        try {
            const options = {
                headers: { "content-type": "application/json" }
            }
            const response = await this.base.api.post(`/api/v3/data-selectors/preview-for-html-editor`, html, options);
            result.success = true;
            result.data = response.data;
        } catch (error) {
            result.success = false;
            console.error("Error get data selectors", typeof(error.toJSON) === "function" ? error.toJSON() : error);
            result.message = "Er is een onbekende fout opgetreden tijdens genereren van de preview voor de gekozen data selector. Probeer het a.u.b. nogmaals of neem contact op met ons.";

            if (error.response) {
                // The request was made and the server responded with a status code
                // that falls out of the range of 2xx
                console.warn(error.response);
            } else if (error.request) {
                // The request was made but no response was received
                // `error.request` is an instance of XMLHttpRequest in the browser and an instance of
                // http.ClientRequest in node.js
                console.warn(error.request);
            } else {
                // Something happened in setting up the request that triggered an Error
                console.warn(error.message);
            }
        }

        return result;
    }
}
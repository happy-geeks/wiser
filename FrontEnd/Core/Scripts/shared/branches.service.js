import BaseService from "./base.service";

export default class BranchesService extends BaseService {
    /**
     * Creates a new environment for the customers. This will create a new database (on the same server/cluster as the current) and copied most data to that new database.
     * It will then also create a new tenant in Wiser so the customer can login to the new environment.
     * @param {any} name The name for the new environment.
     * @returns {any} The information about the new environment.
     */
    async create(name) {
        const result = {};
        try {
            const response = await this.base.api.post(`/api/v3/branches/${encodeURIComponent(name)}`);
            result.success = true;
            result.data = response.data;
        } catch (error) {
            result.success = false;
            console.error("Error create customer", typeof(error.toJSON) === "function" ? error.toJSON() : error);
            
            let errorMessage = error.message;
            if (error.response && error.response.data && error.response.data.error) {
                errorMessage = error.response.data.error;
            } else if (error.response && error.response.data) {
                errorMessage = error.response.data;
            } else if (error.response && error.response.statusText) {
                errorMessage = error.response.statusText;
            }
            result.message = `Er is iets fout gegaan tijdens het aanmaken van deze omgeving. Probeer het a.u.b. nogmaals of neem contact op met ons.<br><br>De fout was:<br>${errorMessage}`;
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
     * Gets the different environments for this customer.
     * @returns {any} An array with all environments.
     */
    async get() {
        const result = {};

        try {
            const response = await this.base.api.get(`/api/v3/branches`);
            result.success = true;
            result.statusCode = 200;
            result.data = response.data;
        } catch (error) {
            result.success = false;
            console.error("Error customer get environments", typeof(error.toJSON) === "function" ? error.toJSON() : error);
            result.message = "Er is een onbekende fout opgetreden tijdens het ophalen van de beschikbare omgevingen.";

            if (error.response) {
                // The request was made and the server responded with a status code
                // that falls out of the range of 2xx
                console.warn(error.response);
                result.statusCode = error.response.status;
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
     * Creates a new environment for the customers. This will create a new database (on the same server/cluster as the current) and copied most data to that new database.
     * It will then also create a new tenant in Wiser so the customer can login to the new environment.
     * @param {int} id The id of the environment to synchronise.
     * @returns {any} The result of the synchronisation.
     */
    async merge(id) {
        const result = {};
        try {
            const response = await this.base.api.patch(`/api/v3/branches/merge/${encodeURIComponent(id)}`);
            result.success = true;
            result.data = response.data;
        } catch (error) {
            result.success = false;
            console.error("Error merging branch", typeof(error.toJSON) === "function" ? error.toJSON() : error);

            let errorMessage = error.message;
            if (error.response && error.response.data && error.response.data.error) {
                errorMessage = error.response.data.error;
            } else if (error.response && error.response.data) {
                errorMessage = error.response.data;
            } else if (error.response && error.response.statusText) {
                errorMessage = error.response.statusText;
            }
            result.message = `Er is iets fout gegaan tijdens het overzetten van de wijzigingen. Probeer het a.u.b. nogmaals of neem contact op met ons.<br><br>De fout was:<br>${errorMessage}`;
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
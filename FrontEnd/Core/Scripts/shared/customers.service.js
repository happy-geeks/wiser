import BaseService from "./base.service";

export default class CustomersService extends BaseService {
	/**
	 * Gets all modules that the user has access to.
	 * @param {string} name The name of the customer.
	 * @param {string} subDomain The sub domain of the customer.
	 * @returns {any} An array with all available modules.
	 */
    async exists(name, subDomain) {
        const result = {};

        try {
            const response = await this.base.api.get(`/api/v3/wiser-customers/${encodeURIComponent(name)}/exists?subDomain=${encodeURIComponent(subDomain)}`);
            result.success = true;
            result.data = response.data;
        } catch (error) {
            result.success = false;
            console.error("Error customer exists check", error.toJSON());
            result.message = "Er is een onbekende fout opgetreden tijdens het controleren of deze klant al bestaat. Probeer het a.u.b. nogmaals of neem contact op met ons.";

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
     * Gets the title of the customer, to show in the browser tab.
     * @param {string} subDomain The sub domain of the customer.
     * @returns {any} An array with all available modules.
     */
    async getTitle(subDomain) {
        const result = {};

        try {
            const response = await this.base.api.get(`/api/v3/wiser-customers/${encodeURIComponent(subDomain)}/title`);
            result.success = true;
            result.statusCode = 200;
            result.data = response.data;
        } catch (error) {
            result.success = false;
            console.error("Error customer get title", error.toJSON());
            result.message = "Er is een onbekende fout opgetreden tijdens het ophalen van de naam van deze klant.";

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
     * Gets all modules that the user has access to.
     * @param {any} data The data for the new customer.
     * @param {boolean} isWebShop: Whether or not this customer is getting a web shop.
     * @returns {any} The newly created customer.
     */
    async create(data, isWebShop) {
        const result = {};

        try {
            const response = await this.base.api.post(`/api/v3/wiser-customers?isWebShop=${isWebShop}`, data);
            result.success = true;
            result.data = response.data;
        } catch (error) {
            result.success = false;
            console.error("Error create customer", error.toJSON());
            result.message = "Er is een onbekende fout opgetreden tijdens het aanmaken van deze klant. Probeer het a.u.b. nogmaals of neem contact op met ons.";

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
     * Gets all database clusters from the Digital Ocean API.
     * @param {any} token The access token to access the Digital Ocean API.
     * @returns {any} The list of clusters.
     */
    async getDbClusters(token) {
        const result = {};
        if (!token) {
            result.success = false;
            result.message = "Vul a.u.b. de access token in.";
            result.data = [];
            return result;
        }

        try {
            const response = await this.base.api.get(`/api/v3/digital-ocean/databases`, {
                headers: {
                    'x-digital-ocean': token
                }
            });

            result.success = response.status === 200 && response.data && response.data.databases && response.data.databases.length;
            if (!result.success) {
                result.message = "Er zijn geen clusters gevonden. Waarschijnlijk heb je een verkeerde access token ingevuld.";
            } else {
                result.data = response.data;
            }
        } catch (error) {
            result.success = false;
            result.data = [];
            console.error("Error create customer", error.toJSON());
            result.message = "Er is een onbekende fout opgetreden tijdens het ophalen van de clusters. Heb je de juiste access token ingevuld? Probeer het a.u.b. nogmaals of neem contact op met ons.";

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
     * Creates a new database schema and database user in Digital Ocean.
     * @param {any} databaseCluster The name of the database cluster.
     * @param {any} database The name of the new database schema.
     * @param {any} user The name of the new database user.
     * @param {any} token
     */
    async createDatabaseAndUser(databaseCluster, database, user, token) {
        const result = {};

        try {
            const response = await this.base.api.post(`/api/v3/digital-ocean/databases`, {
                database,
                database_cluster: databaseCluster,
                user
            }, {
                headers: {
                    'x-digital-ocean': token
                }
            });

            result.success = response.status === 200 && !response.data.error;
            result.data = response.data;
            result.message = response.data.error;
        } catch (error) {
            result.success = false;
            console.error("Error create customer", error.toJSON());
            result.message = "Er is een onbekende fout opgetreden tijdens het aanmaken van de database voor deze klant. Probeer het a.u.b. nogmaals of neem contact op met ons.";

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
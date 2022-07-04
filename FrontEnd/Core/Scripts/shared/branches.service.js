﻿import BaseService from "./base.service";

export default class BranchesService extends BaseService {
    /**
     * Creates a new branch for the customers. This will create a new database (on the same server/cluster as the current) and copied most data to that new database.
     * It will then also create a new tenant in Wiser so the customer can login to the new branch.
     * @param {any} data The data for the new branch.
     */
    async create(data) {
        const result = {};
        try {
            const postData = {
                name: data.name,
                startOn: data.startOn,
                entities: []
            };

            const all = data.entities.all || { mode: -1 };
            
            for (let key in data.entities) {
                if (!data.entities.hasOwnProperty(key) || key === "all") {
                    continue;
                }
                
                // If the user selected something in the "all" option, use that for everything.
                const settings = parseInt(all.mode) === -1 ? data.entities[key] : all;
                
                postData.entities.push({
                    entityType: key,
                    mode: parseInt(settings.mode),
                    amountOfItems: parseInt(settings.amountOfItems) || null,
                    start: settings.start || null,
                    end: settings.end || null
                });
            }
            
            const response = await this.base.api.post(`/api/v3/branches`, postData);
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
     * Gets the different branches for this customer.
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
            console.error("Error get branches", typeof(error.toJSON) === "function" ? error.toJSON() : error);
            result.message = "Er is een onbekende fout opgetreden tijdens het ophalen van de beschikbare branches.";

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
     * Merge changes from a branch to the main/original branch.
     * @param {any} data The data/settings of what to merge.
     * @returns {any} The result of the merge.
     */
    async merge(data) {
        const result = {};
        try {
            const postData = {
                id: data.selectedBranch.id,
                startOn: data.startOn,
                deleteAfterSuccessfulMerge: data.deleteAfterSuccessfulMerge,
                entities: [],
                settings: []
            };
            
            const allEntities = data.entities.all || { everything: false, create: false, update: false, delete: false };

            for (let key in data.entities) {
                if (!data.entities.hasOwnProperty(key) || key === "all") {
                    continue;
                }
                
                const entity = data.entities[key];
                
                postData.entities.push({
                    type: key,
                    create: entity.create || allEntities.create || entity.everything || allEntities.everything,
                    update: entity.update || allEntities.update || entity.everything || allEntities.everything,
                    delete: entity.delete || allEntities.delete || entity.everything || allEntities.everything
                });
            }
            
            const allSettings = data.settings.all || { everything: false, create: false, update: false, delete: false };
            for (let key in data.settings) {
                if (!data.settings.hasOwnProperty(key) || key === "all") {
                    continue;
                }

                const setting = data.settings[key];

                postData.settings.push({
                    type: key,
                    create: setting.create || allSettings.create || setting.everything || allSettings.everything,
                    update: setting.update || allSettings.update || setting.everything || allSettings.everything,
                    delete: setting.delete || allSettings.delete || setting.everything || allSettings.everything
                });
            }
            
            const response = await this.base.api.patch(`/api/v3/branches/merge`, postData);
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

    /**
     * Gets all entities that can be copied to a new branch.
     * @returns {any} An array with all entities.
     */
    async getEntities() {
        const result = {};

        try {
            const response = await this.base.api.get(`/api/v3/entity-types?includeCount=true&skipEntitiesWithoutItems=true`);
            result.success = true;
            result.statusCode = 200;
            result.data = response.data;
        } catch (error) {
            result.success = false;
            console.error("Error get entities", typeof(error.toJSON) === "function" ? error.toJSON() : error);
            result.message = "Er is een onbekende fout opgetreden tijdens het ophalen van de beschikbare entiteiten.";

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
     * Gets whether the current branch is the main branch.
     * @returns {boolean} A boolean indicating whether the current branch is the main branch.
     */
    async isMainBranch() {
        const result = {};

        try {
            const response = await this.base.api.get(`/api/v3/branches/is-main`);
            result.success = true;
            result.statusCode = 200;
            result.data = response.data;
        } catch (error) {
            result.success = false;
            console.error("Error isMainBranch", typeof(error.toJSON) === "function" ? error.toJSON() : error);
            result.message = "Er is een onbekende fout opgetreden tijdens het ophalen van informatie over de huidige branch.";

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
     * Gets all changes that can be merged into the main branch.
     * @param {number} branchId The Id of the branch.
     * @returns {any} An object with all changes counters.
     */
    async getChanges(branchId) {
        const result = {};

        try {
            const response = await this.base.api.get(`/api/v3/branches/changes/${branchId}`);
            result.success = true;
            result.statusCode = 200;
            result.data = response.data;
        } catch (error) {
            result.success = false;
            console.error("Error get entities", typeof(error.toJSON) === "function" ? error.toJSON() : error);
            result.message = "Er is een onbekende fout opgetreden tijdens het ophalen van de wijzigingen van de geselecteerde branch.";

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
}
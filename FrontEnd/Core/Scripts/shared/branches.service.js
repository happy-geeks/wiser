import BaseService from "./base.service";

export default class BranchesService extends BaseService {
    /**
     * Creates a new branch for this tenant. This will create a new database (on the same server/cluster as the current) and copy most data to that new database.
     * It will then also create a new tenant in Wiser, linked to the current one, so users can login to the new branch.
     * @param {any} data The data for the new branch.
     */
    async create(data) {
        const result = {};
        try {
            const postData = {
                name: data.name,
                startOn: data.startOn,
                databaseHost: data.databaseHost,
                databasePort: data.databasePort,
                databaseUsername: data.databaseUsername,
                databasePassword: data.databasePassword,
                entities: []
            };

            for (let key in data.entities) {
                if (!data.entities.hasOwnProperty(key) || key === "all") {
                    continue;
                }

                const settings = data.entities[key];

                postData.entities.push({
                    entityType: key,
                    mode: parseInt(settings.mode),
                    amountOfItems: parseInt(settings.amountOfItems) || null,
                    start: settings.start || null,
                    end: settings.end || null,
                    dataSelector: settings.dataSelector || 0
                });
            }

            const response = await this.base.api.post(`/api/v3/branches`, postData);
            result.success = true;
            result.data = response.data;
        } catch (error) {
            result.success = false;
            console.error("Error create branch", typeof(error.toJSON) === "function" ? error.toJSON() : error);

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
     * Gets the different branches for this tenant.
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
                settings: [],
                checkForConflicts: data.checkForConflicts,
                conflictSettings: data.conflicts.map(conflict => {
                    return {
                        id: conflict.id,
                        objectId: conflict.objectId,
                        acceptChange: conflict.acceptChange
                    };
                })
            };

            for (let key in data.entities) {
                if (!data.entities.hasOwnProperty(key) || key === "all") {
                    continue;
                }

                const entity = data.entities[key];

                postData.entities.push({
                    type: key,
                    create: entity.create || entity.everything,
                    update: entity.update || entity.everything,
                    delete: entity.delete || entity.everything
                });
            }

            for (let key in data.settings) {
                if (!data.settings.hasOwnProperty(key) || key === "all") {
                    continue;
                }

                const setting = data.settings[key];

                postData.settings.push({
                    type: key,
                    create: setting.create || setting.everything,
                    update: setting.update || setting.everything,
                    delete: setting.delete || setting.everything
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
    async getEntities(branchId = 0) {
        const result = {};

        try {
            const response = await this.base.api.get(`/api/v3/entity-types?includeCount=true&skipEntitiesWithoutItems=true&branchId=${branchId || 0}`);
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

    async getDataSelectors() {
        const result = [];

        try {
            const response = await this.base.api.get(`/api/v3/data-selectors?forBranches=true`);
            result.success = true;
            result.statusCode = 200;
            result.data = response.data;
        } catch (error) {
            result.success = false;
            console.error("Error get data selectors", typeof(error.toJSON) === "function" ? error.toJSON() : error);
            result.message = "Er is een onbekende fout opgetreden tijdens het ophalen van de beschikbare dataselectors.";

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
     * Deletes a branch for the tenant. This will delete the corresponding database (on the same server/cluster as the current).
     * It will then also delete the tenant in Wiser so the branch will be completely and permanently removed.
     * @param {int} id The id of the branch to delete.
     */
    async delete(id) {
        const result = {};
        try {
            const response = await this.base.api.delete(`/api/v3/branches/${id}`);
            result.success = true;
            result.data = response.data;
        } catch (error) {
            result.success = false;
            console.error("Error deleting branch", typeof(error.toJSON) === "function" ? error.toJSON() : error);

            let errorMessage = error.message;
            if (error.response && error.response.data && error.response.data.error) {
                errorMessage = error.response.data.error;
            } else if (error.response && error.response.data) {
                errorMessage = error.response.data;
            } else if (error.response && error.response.statusText) {
                errorMessage = error.response.statusText;
            }
            result.message = `Er is iets fout gegaan tijdens het verwijderen van deze omgeving. Probeer het a.u.b. nogmaals of neem contact op met ons.<br><br>De fout was:<br>${errorMessage}`;
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
import BaseService from "./base.service";

export default class ItemsService extends BaseService {
	/**
	 * Gets all modules that the user has access to.
	 * @param {number} id The item ID to encrypt.
	 * @returns {any} An array with all available modules.
	 */
    async encryptId(id) {
        try {
            const result = await this.base.api.get(`/api/v3/items/${encodeURIComponent(id)}/encrypt`);
            return result.data;
        } catch (error) {
            console.error(error);
            return [];
        } 
    }

    /**
     * Gets all entity types from an item ID.
     * Wiser can have multiple wiser_item tables, with a prefix for certain entity types. This means an ID can exists multiple times.
     * This function will get the different entity types with the given ID.
     * @param {number} id The item ID.
     * @returns {any} An array with all entity types.
     */
    async getEntityTypesFromId(id) {
        try {
            const result = await this.base.api.get(`/api/v3/items/${encodeURIComponent(id)}/entity-types`);
            return result.data;
        } catch (error) {
            console.error(error);
            return [];
        } 
    }
}
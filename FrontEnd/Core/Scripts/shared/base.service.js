import * as axios from "axios";

export default class BaseService {
    /**
     * Initializes a new instance of the BaseService class.
     * @param {DynamicItems} base An instance of the base class (Main).
     */
    constructor(base) {
        this.base = base;
    }

    /**
     * Parses an axion response as a list.
     * @param {any} response The response from an axion http request.
     * @returns {Array} An array with the results. An empty array if there are no results.
     */
    parseList(response) {
        if (response.status !== 200) throw Error(response.message);
        if (!response.data) return [];
        let list = response.data;
        if (typeof list !== "object") {
            list = [];
        }
        return list;
    }

    /**
     * Parses an axion response as an object.
     * @param {any} response The response from an axion http request.
     * @returns {any} An object with the result, or null if there is no result.
     */
    parseItem(response) {
        if (response.status !== code) throw Error(response.message);
        let item = response.data;
        if (typeof item !== "object") {
            item = null;
        }

        return item;
    }
}
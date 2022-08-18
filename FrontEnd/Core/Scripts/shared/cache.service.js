import BaseService from "./base.service";

export default class CacheService extends BaseService {
    async clear(clearCacheSettings) {
        const result = {};
        
        try {
            await this.base.api.post(`/api/v3/external-cache/clear`, clearCacheSettings);
            result.success = true;
        } catch (error) {
            result.success = false;
            console.error("Error clear cache", typeof(error.toJSON) === "function" ? error.toJSON() : error);

            let errorMessage = error.message;
            if (error.response && error.response.data && error.response.data.error) {
                errorMessage = error.response.data.error;
            } else if (error.response && error.response.data) {
                errorMessage = error.response.data;
            } else if (error.response && error.response.statusText) {
                errorMessage = error.response.statusText;
            }
            result.message = `Er is iets fout gegaan tijdens het legen van de cache. Probeer het a.u.b. nogmaals of neem contact op met ons.<br><br>De fout was:<br>${errorMessage}`;
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
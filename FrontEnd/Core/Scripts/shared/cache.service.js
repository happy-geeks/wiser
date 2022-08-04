import BaseService from "./base.service";

export default class CacheService extends BaseService {
    async clear(clearCacheSettings) {
        try {
            await this.base.api.post(`/api/v3/cache/clear`, clearCacheSettings);
            return true;
        } catch (error) {
            console.error(error);
            return false;
        }
    }
}
import BaseService from "./base.service";

export default class DatabasesService extends BaseService {
    async doTenantMigrations() {
        try {
            await this.base.api.put(`/api/v3/database/tenant-migrations`);
        } catch (exception) {
            console.error(exception);
        }
    }
}
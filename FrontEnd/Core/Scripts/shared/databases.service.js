import BaseService from "./base.service";

export default class DatabasesService extends BaseService {
    async doTenantMigrations() {
        try {
            await this.base.api.put(`/api/v3/database/tenant-migrations`);
        } catch (exception) {
            if (exception.response.status === 401) {
                console.warn("User is unauthorized to do database migrations.", exception);
            } else {
                console.error("Error while executing database migrations", exception);
                alert("Er is iets fout gegaan tijdens het uitvoeren van de database migraties. Ververs a.u.b. de pagina om het opnieuw te proberen, of neem contact op met de beheerder.");
            }
        }
    }
}
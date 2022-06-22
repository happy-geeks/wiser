import BaseService from "./base.service";

export default class TranslationsService extends BaseService {


    async getTranslation(location, cultureCode) {
        try {
            const translation = await this.base.api.get(`/api/v3/translations?location=${location}&cultureCode=${cultureCode}`);
            const result = this.parseList(translation);

            return result;
        } catch (error) {
            console.error(error);
            return [];
        } 
    }
}
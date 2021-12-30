import BaseService from "./base.service";

export default class ModulesService extends BaseService {
	/**
	 * Gets all modules that the user has access to.
	 * @returns {any} An array with all available modules.
	 */
    async getModules() {
        try {
            const modulesResult = await this.base.api.get(`/api/v3/modules`);
            const result = this.parseList(modulesResult);
            for (let groupName in result) {
                if (!result.hasOwnProperty(groupName)) {
                    continue;
                }

                result[groupName].map(x => {
                    x.icon = `icon-${x.icon}`;

                    switch (x.module_id) {
                        case 5004:
                            x.fileName = `/Import`;
                            break;
                        case 5005:
                            x.fileName = `/Export`;
                            break;
                        default:
                            x.fileName = ``;
                            break;
                    }

                    if (x.type === "DynamicItems") {
                        x.queryString = `?moduleId=${!x.item_id ? x.module_id : 0}&iframe=${x.iframe || false}${(!x.item_id ? "" : `&itemId=${encodeURIComponent(x.item_id)}`)}${(!x.entity_type? "" : `&entityType=${encodeURIComponent(x.entityType)}`)}`;
                    } else {
                        x.queryString = "";
                    }

                    return x;
                });


            }
            return result;
        } catch (error) {
            console.error(error);
            return [];
        } 
    }
}
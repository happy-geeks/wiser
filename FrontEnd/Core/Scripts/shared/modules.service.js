import BaseService from "./base.service";

export default class ModulesService extends BaseService {
    dynamicItemsModules = ["DynamicItems", "Translation", "Seo", "Redirect", "MasterData", "Users", "ImportHistory"];
    
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

                result[groupName].map(module => {
                    module.icon = `icon-${module.icon}`;

                    switch (module.moduleId) {
                        case 5004:
                            module.fileName = `/Import`;
                            break;
                        case 5005:
                            module.fileName = `/Export`;
                            break;
                        default:
                            module.fileName = ``;
                            break;
                    }

                    if (this.dynamicItemsModules.indexOf(module.type) > -1) {
                        module.iframeType = "DynamicItems";
                        module.queryString = `?moduleId=${!module.itemId ? module.moduleId : 0}&iframe=${module.iframe || false}${(!module.itemId ? "" : `&itemId=${encodeURIComponent(module.itemId)}`)}${(!module.entityType? "" : `&entityType=${encodeURIComponent(module.entityType)}`)}`;
                    } else if (module.type === "FileManager") {
                        module.queryString = "?hideFields=true";
                    } else {
                        module.iframeType = module.type;
                        module.queryString = "";
                    }

                    return module;
                });
            }

            return result;
        } catch (error) {
            console.error(error);
            return [];
        } 
    }
}
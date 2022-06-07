import { createStore } from "vuex";
import {
    START_REQUEST,
    END_REQUEST,
    AUTH_REQUEST,
    AUTH_LIST,
    AUTH_SUCCESS,
    AUTH_ERROR,
    AUTH_LOGOUT,
    MODULES_LOADED,
    OPEN_MODULE,
    ACTIVATE_MODULE,
    CLOSE_MODULE,
    CLOSE_ALL_MODULES,
    MODULES_REQUEST,
    LOAD_ENTITY_TYPES_OF_ITEM_ID,
    FORGOT_PASSWORD,
    RESET_PASSWORD_SUCCESS,
    RESET_PASSWORD_ERROR,
    CHANGE_PASSWORD,
    CHANGE_PASSWORD_LOGIN,
    CHANGE_PASSWORD_SUCCESS,
    CHANGE_PASSWORD_ERROR,
    GET_CUSTOMER_TITLE,
    VALID_SUB_DOMAIN,
    TOGGLE_PIN_MODULE,
    CREATE_ENVIRONMENT,
    CREATE_ENVIRONMENT_SUCCESS,
    CREATE_ENVIRONMENT_ERROR,
    GET_ENVIRONMENTS, 
    SYNCHRONISE_CHANGES_TO_PRODUCTION,
    SYNCHRONISE_CHANGES_TO_PRODUCTION_SUCCESS,
    SYNCHRONISE_CHANGES_TO_PRODUCTION_ERROR
} from "./mutation-types";

const baseModule = {
    state: () => ({
        mainLoader: true,
        requestCounter: 0
    }),

    mutations: {
        [START_REQUEST]: (state) => {
            state.requestCounter++;
            state.mainLoader = true;
        },
        [END_REQUEST]: (state) => {
            state.requestCounter--;
            if (state.requestCounter <= 0) {
                state.mainLoader = false;
            }
        }
    },

    actions: {},

    getters: {}
};

const loginModule = {
    state: () => ({
        loginStatus: "",
        loginMessage: "",
        user: {
            name: "",
            role: "",
            lastLoginIpAddress: "",
            lastLoginDate: null,
            loggedIn: false
        },
        listOfUsers: [],
        resetPassword: false,
        requirePasswordChange: false
    }),

    mutations: {
        [AUTH_REQUEST]: (state, user) => {
            state.loginStatus = user && user.selectedUser ? "list_loading" : "loading";
            state.loginMessage = "";
            state.listOfUsers = [];
        },
        [AUTH_LIST]: (state, users) => {
            state.loginStatus = "list";
            state.loginMessage = !users || !users.length ? "Er zijn geen gebruikers gevonden bij deze klant. Waarschijnlijk is er iets niet goed ingesteld." : "";
            state.listOfUsers = users;
        },
        [AUTH_SUCCESS]: (state, user) => {
            state.loginStatus = "success";
            state.user = user;
            state.loginMessage = "";
            state.listOfUsers = [];
            state.requirePasswordChange = user.requirePasswordChange;
        },
        [AUTH_ERROR]: (state, message) => {
            state.loginStatus = "error";
            state.loginMessage = message;
            state.listOfUsers = [];
            state.user = {
                name: "",
                role: "",
                lastLoginIpAddress: "",
                lastLoginDate: null,
                loggedIn: false
            };
        },
        [AUTH_LOGOUT]: (state) => {
            state.loginStatus = "";
            state.loginMessage = "";
            state.listOfUsers = [];
            state.user = {
                name: "",
                role: "",
                lastLoginIpAddress: "",
                lastLoginDate: null,
                loggedIn: false
            };
            state.requirePasswordChange = false;
        },
        [FORGOT_PASSWORD]: (state) => {
            state.resetPassword = false;
            state.loginMessage = "";
        },
        [RESET_PASSWORD_SUCCESS]: (state) => {
            state.resetPassword = true;
        },
        [RESET_PASSWORD_ERROR]: (state) => {
            state.loginMessage = "Er is iets mis gegaan. Probeer het later a.u.b. opnieuw.";
        },
        [CHANGE_PASSWORD_LOGIN]: (state) => {
            state.loginMessage = "";
        },
        [CHANGE_PASSWORD_SUCCESS]: (state) => {
            state.requirePasswordChange = false;
        },
        [CHANGE_PASSWORD_ERROR]: (state, message) => {
            state.loginMessage = message;
        }
    },

    actions: {
        async [AUTH_REQUEST]({ commit }, data = {}) {
            const user = data.user;
            commit(AUTH_REQUEST, user);

            // Check if we have user data in the local storage and if that data is still valid.
            if (!user) {
                const accessTokenExpires = localStorage.getItem("accessTokenExpiresOn");

                // User is still logged in.
                const user = JSON.parse(localStorage.getItem("userData"));

                if (data.gotUnauthorized || !accessTokenExpires || new Date(accessTokenExpires) <= new Date() || user.requirePasswordChange) {
                    if (!user || !user.refresh_token || user.requirePasswordChange) {
                        this.dispatch(AUTH_LOGOUT);
                        return;
                    }

                    const loginResult = await main.usersService.refreshToken(user.refresh_token);
                    if (!loginResult.success) {
                        this.dispatch(AUTH_LOGOUT);
                        return;
                    }

                    localStorage.setItem("accessToken", loginResult.data.access_token);
                    localStorage.setItem("accessTokenExpiresOn", loginResult.data.expiresOn);
                    localStorage.setItem("userData", JSON.stringify(Object.assign({}, user, loginResult.data)));
                }

                window.main.api.defaults.headers.common["Authorization"] = `Bearer ${localStorage.getItem("accessToken")}`;

                user.loggedIn = true;

                const extraUserData = await window.main.usersService.getLoggedInUserData();
                if (extraUserData.success) {
                    Object.assign(user, extraUserData.data);
                }

                commit(AUTH_SUCCESS, user);

                await this.dispatch(MODULES_REQUEST);
                return;
            }

            const loginResult = await main.usersService.loginUser(user.username, user.password, (user.selectedUser || {}).username);
            if (!loginResult.success) {
                commit(AUTH_ERROR, loginResult.message);
                return;
            }
            
            // If the user that is logging in is an admin account, show a list of users for the customer.
            if (loginResult.data.adminLogin) {
                commit(AUTH_LIST, loginResult.data.usersList);
                return;
            }

            localStorage.setItem("accessToken", loginResult.data.access_token);
            localStorage.setItem("accessTokenExpiresOn", loginResult.data.expiresOn);
            localStorage.setItem("userData", JSON.stringify(loginResult.data));
            window.main.api.defaults.headers.common["Authorization"] = `Bearer ${localStorage.getItem("accessToken")}`;

            loginResult.data.loggedIn = true;

            commit(AUTH_SUCCESS, loginResult.data);

            const extraUserData = await window.main.usersService.getLoggedInUserData();
            if (extraUserData.success) {
                Object.assign(loginResult.data, extraUserData.data);
            }

            commit(AUTH_SUCCESS, loginResult.data);

            await this.dispatch(MODULES_REQUEST);
        },

        [AUTH_LOGOUT]({ commit }) {
            localStorage.removeItem("accessToken");
            localStorage.removeItem("accessTokenExpiresOn");
            localStorage.removeItem("userData");
            sessionStorage.removeItem("userSettings");
            delete window.main.api.defaults.headers.common["Authorization"];

            commit(AUTH_LOGOUT);
        },

        async [FORGOT_PASSWORD]({ commit }, data = {}) {
            commit(FORGOT_PASSWORD);
            const result = await main.usersService.forgotPassword(data.user.username, data.user.email);

            if (result) {
                commit(RESET_PASSWORD_SUCCESS);
            } else {
                commit(RESET_PASSWORD_ERROR);
            }
        },

        async [CHANGE_PASSWORD_LOGIN]({ commit }, data = {}) {
            commit(CHANGE_PASSWORD_LOGIN);

            const result = await main.usersService.changePassword(data.user);

            if (result.response) {
                commit(CHANGE_PASSWORD_SUCCESS);
            } else {
                commit(CHANGE_PASSWORD_ERROR, result.error);
            }
        }
    },

    getters: {}
};

const modulesModule = {
    state: () => ({
        allModules: [],
        openedModules: [],
        activeModule: 0,
        moduleGroups: []
    }),

    mutations: {
        [MODULES_LOADED](state, modules) {
            state.allModules = [];
            state.moduleGroups = [];

            if (!modules) {
                return;
            }
            
            for (let groupName in modules) {
                if (!modules.hasOwnProperty(groupName)) {
                    continue;
                }

                const moduleGroup = {
                    name: groupName,
                    modules: []
                }
                state.moduleGroups.push(moduleGroup);

                for (let module of modules[groupName]) {
                    if (!module.name) {
                        console.warn("Found module without name, so skipping it", module);
                        continue;
                    }

                    state.allModules.push(module);
                    moduleGroup.modules.push(module);
                }
            }
        },

        [OPEN_MODULE]: (state, module) => {
            // Check if this module is already open, if the user is not allowed to have multiple instances of this module open at once.
            let activeModule = !module.onlyOneInstanceAllowed ? null : state.openedModules.filter(m => m.moduleId === module.moduleId)[0];

            // Add the module to the list if it isn't open yet, or if the user is allowed to have multiple instanced of the same module open at once.
            if (!activeModule) {
                activeModule = Object.assign({}, module);

                if (module.onlyOneInstanceAllowed) {
                    activeModule.id = activeModule.moduleId;
                    activeModule.index = 0;
                } else {
                    // Set a unique ID and index, to that users can have this module open more than once.
                    const allInstances = state.openedModules.filter(m => m.moduleId === module.moduleId);
                    const newModuleIndex = Math.max(...allInstances.map(m => m.index || 0), 0) + 1;
                    activeModule.id = `${activeModule.moduleId}_${newModuleIndex}`;
                    activeModule.index = newModuleIndex;
                }
                
                // Add module name to query string.
                if (!activeModule.queryString) {
                    activeModule.queryString = `?moduleName=${encodeURIComponent(module.name)}`;
                } else if(activeModule.queryString.indexOf("&moduleName=") === -1) {
                    activeModule.queryString += `&moduleName=${encodeURIComponent(module.name)}`;
                }

                state.openedModules.push(activeModule);
            }

            // Set the newly opened module to active.
            state.activeModule = activeModule.id;
        },
        [ACTIVATE_MODULE]: (state, moduleId) => {
            // Set the newly opened module to active.
            state.activeModule = moduleId;
        },
        [CLOSE_MODULE]: (state, module) => {
            // Do some pre checks.
            const moduleToClose = state.openedModules.filter(m => m.id === module.id)[0];
            if (!moduleToClose) {
                return;
            }

            const moduleIndex = state.openedModules.indexOf(moduleToClose);
            if (moduleIndex === -1) {
                return;
            }

            // If the module still exists, remove it from the array, which will automatically cause Vue to remove it from the HTML.
            state.openedModules.splice(moduleIndex, 1);

            // If the module that was closed was the active module, set the last opened module as active.
            if (state.activeModule === moduleToClose.id) {
                if (state.openedModules.length > 0) {
                    state.activeModule = state.openedModules[state.openedModules.length - 1].id;
                } else {
                    state.activeModule = null;
                }
            }
        },
        [CLOSE_ALL_MODULES]: (state) => {
            state.activeModule = null;
            state.openedModules = [];
        },
        [TOGGLE_PIN_MODULE]: (state, moduleId) => {
            const module = state.allModules.filter(m => m.moduleId === moduleId)[0];

            // Toggle the pin status.
            let pinnedChanged = true;
            if (module.pinned && module.autoLoad) {
                module.pinned = false;
                module.autoLoad = false;
            } else if (module.pinned && !module.autoLoad) {
                module.autoLoad = true;
                pinnedChanged = false;
            } else {
                module.pinned = true;
            }

            const removeFrom = module.pinned ? module.group : module.pinnedGroup;
            const addTo = module.pinned ? module.pinnedGroup : module.group;

            const removeFromGroup = state.moduleGroups.filter(g => g.name === removeFrom)[0];
            let addToGroup = state.moduleGroups.filter(g => g.name === addTo)[0];

            // Don't need to do the rest if only the auto load setting has been changed.
            if (!pinnedChanged) {
                return;
            }

            // It's possible that these groups don't exist yet, so create them if they don't.
            if (!addToGroup) {
                addToGroup = {
                    name: addTo,
                    modules: []
                };
                state.moduleGroups.push(addToGroup);
            }
            
            removeFromGroup.modules.splice(removeFromGroup.modules.indexOf(module), 1);
            addToGroup.modules.push(module);

            // If we just removed the last module from a group, remove the entire group.
            if (removeFromGroup.modules.length === 0) {
                state.moduleGroups.splice(state.moduleGroups.indexOf(removeFromGroup), 1);
            }

            // Order the groups.
            state.moduleGroups = state.moduleGroups.sort((groupA, groupB) => {
                // Make sure the pinned group is always first.
                if (groupA.name === module.pinnedGroup) {
                    return -1;
                }

                if (groupB.name === module.pinnedGroup) {
                    return 1;
                }

                // Then sort the rest alphabetically.
                if (groupA.name < groupB.name) {
                    return -1;
                }

                if (groupA.name > groupB.name) {
                    return 1;
                }
                
                return 0;
            });
            
            // Order the modules in each group.
            for (let group of state.moduleGroups) {
                group.modules = group.modules.sort((moduleA, moduleB) => {
                    if (moduleA.name < moduleB.name) {
                        return -1;
                    }

                    if (moduleA.name > moduleB.name) {
                        return 1;
                    }
                
                    return 0;
                });
            }
        }
    },

    actions: {
        async [MODULES_REQUEST]({ commit }) {
            commit(START_REQUEST);
            const moduleGroups = await main.modulesService.getModules();
            commit(MODULES_LOADED, moduleGroups);

            // Automatically open pinned modules when the modules are first loaded.
            for (let group in moduleGroups) {
                if (!moduleGroups.hasOwnProperty(group)) {
                    continue;
                }
                
                for (let module of moduleGroups[group]) {
                    if (!module.autoLoad) {
                        continue;
                    }

                    commit(OPEN_MODULE, module);
                }
            }
            commit(END_REQUEST);
        },

        [OPEN_MODULE]({ commit }, module) {
            commit(OPEN_MODULE, module);
        },

        [CLOSE_MODULE]({ commit }, module) {
            commit(CLOSE_MODULE, module);
        },

        [ACTIVATE_MODULE]({ commit }, moduleId) {
            commit(ACTIVATE_MODULE, moduleId);
        },

        [CLOSE_ALL_MODULES]({ commit }) {
            commit(CLOSE_ALL_MODULES);
        },

        async [TOGGLE_PIN_MODULE]({ commit, state }, moduleId) {
            commit(TOGGLE_PIN_MODULE, moduleId);
            const pinnedModuleIds = state.allModules.filter(m => m.pinned).map(m => m.moduleId);
            await main.usersService.savePinnedModules(pinnedModuleIds);

            const autoLoadModuleIds = state.allModules.filter(m => m.autoLoad).map(m => m.moduleId);
            await main.usersService.saveAutoLoadModules(autoLoadModuleIds);
        }
    },

    getters: {}
};

const usersModule = {
    state: () => ({
        changePasswordError: null
    }),

    mutations: {
        [RESET_PASSWORD_SUCCESS]: (state, message) => {
            state.changePasswordError = null;
        },
        [CHANGE_PASSWORD_ERROR]: (state, message) => {
            state.changePasswordError = message;
        }
    },

    actions: {
        async [CHANGE_PASSWORD]({ commit }, data = {}) {
            commit(START_REQUEST);
            const result = await main.usersService.changePassword(data);
            commit(END_REQUEST);

            if (result.response) {
                commit(RESET_PASSWORD_SUCCESS);
            } else {
                commit(CHANGE_PASSWORD_ERROR, result.error);
            }
        }
    },

    getters: {}
};

const customersModule = {
    state: () => ({
        title: null,
        validSubDomain: true,
        createEnvironmentError: null,
        createEnvironmentResult: null,
        environments: []
    }),

    mutations: {
        [GET_CUSTOMER_TITLE](state, title) {
            state.title = title;
            if (!title) {
                return;
            }

            document.title = `${title} - Wiser 3.0`;
        },

        [VALID_SUB_DOMAIN](state, valid) {
            state.validSubDomain = valid;
        },
        
        [CREATE_ENVIRONMENT_SUCCESS](state, result) {
            state.createEnvironmentResult = result;
            state.createEnvironmentError = null;
        },

        [CREATE_ENVIRONMENT_ERROR](state, error) {
            state.createEnvironmentResult = null;
            state.createEnvironmentError = error;
        },

        [GET_ENVIRONMENTS](state, environments) {
            state.environments = environments;
        },

        [SYNCHRONISE_CHANGES_TO_PRODUCTION_SUCCESS](state, result) {
            state.synchroniseChangesToProductionResult = result;
            state.synchroniseChangesToProductionError = null;
        },

        [SYNCHRONISE_CHANGES_TO_PRODUCTION_ERROR](state, error) {
            state.synchroniseChangesToProductionResult = null;
            state.synchroniseChangesToProductionError = error;
        }
    },

    actions: {
        async [GET_CUSTOMER_TITLE]({ commit }, subDomain) {
            commit(START_REQUEST);
            const titleResponse = await main.customersService.getTitle(subDomain);
            commit(GET_CUSTOMER_TITLE, titleResponse.data);
            commit(VALID_SUB_DOMAIN, titleResponse.statusCode !== 404);
            commit(END_REQUEST);
        },
        
        async [CREATE_ENVIRONMENT]({ commit }, name) {
            commit(START_REQUEST);
            const result = await main.customersService.createNewEnvironment(name);

            if (result.success) {
                if (result.data) {
                    commit(CREATE_ENVIRONMENT_SUCCESS, result.data);
                } else {
                    commit(CREATE_ENVIRONMENT_ERROR, result.message);
                }
            } else {
                commit(CREATE_ENVIRONMENT_ERROR, result.message);
            }
            commit(END_REQUEST);
        },
        
        async [GET_ENVIRONMENTS]({ commit }) {
            commit(START_REQUEST);
            const environmentsResponse = await main.customersService.getEnvironments();
            commit(GET_ENVIRONMENTS, environmentsResponse.data);
            commit(END_REQUEST);
        },
        
        async [SYNCHRONISE_CHANGES_TO_PRODUCTION]({ commit }, id) {
            commit(START_REQUEST);
            const result = await main.customersService.synchroniseChangesToProduction(id);

            if (result.success) {
                if (result.data) {
                    if (result.data.errors && result.data.errors.length > 0) {
                        let errorMessage;
                        if (result.data.successfulChanges) {
                            errorMessage = `Een deel van het overzetten is goed gegaan en een deel is fout gegaan. Er zijn ${result.data.successfulChanges} wijzigingen succesvol overgezet en de volgende fouten zijn opgetreden: ${result.data.errors.join("<br>")}`;
                        } else {
                            errorMessage = `Het overzetten is mislulkt. De volgende fouten zijn opgetreden: ${result.data.errors.join("<br>")}`;
                        }
                        commit(SYNCHRONISE_CHANGES_TO_PRODUCTION_ERROR, errorMessage);
                    } else {
                        commit(SYNCHRONISE_CHANGES_TO_PRODUCTION_SUCCESS, result.data);
                    }
                } else {
                    commit(SYNCHRONISE_CHANGES_TO_PRODUCTION_ERROR, result.message);
                }
            } else {
                commit(SYNCHRONISE_CHANGES_TO_PRODUCTION_ERROR, result.message);
            }
            commit(END_REQUEST);
        }
    },

    getters: {}
};

const itemsModule = {
    state: () => ({
        listOfEntityTypes: []
    }),

    mutations: {
        [LOAD_ENTITY_TYPES_OF_ITEM_ID](state, entityTypes) {
            state.listOfEntityTypes = entityTypes || [];
        }
    },

    actions: {
        async [LOAD_ENTITY_TYPES_OF_ITEM_ID]({ commit }, itemId) {
            commit(START_REQUEST);
            const entityTypes = await main.itemsService.getEntityTypesFromId(itemId);
            commit(LOAD_ENTITY_TYPES_OF_ITEM_ID, entityTypes);
            commit(END_REQUEST);
        }
    },

    getters: {}
};

export default createStore({
    // Do not enable strict mode when deploying for production! 
    // Strict mode runs a synchronous deep watcher on the state tree for detecting inappropriate mutations, and it can be quite expensive when you make large amount of mutations to the state. 
    // Make sure to turn it off in production to avoid the performance cost.
    strict: process.env.NODE_ENV === "development",

    modules: {
        base: baseModule,
        login: loginModule,
        modules: modulesModule,
        customers: customersModule,
        users: usersModule,
        items: itemsModule
    }
});
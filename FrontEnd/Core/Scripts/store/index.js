import { createStore } from "vuex";
import {
    START_REQUEST,
    END_REQUEST,
    AUTH_REQUEST,
    AUTH_LIST,
    AUTH_SUCCESS,
    AUTH_ERROR,
    AUTH_LOGOUT,
    AUTH_TOTP_SETUP,
    AUTH_TOTP_PIN,
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
    CREATE_BRANCH,
    CREATE_BRANCH_SUCCESS,
    CREATE_BRANCH_ERROR,
    GET_BRANCHES,
    MERGE_BRANCH,
    MERGE_BRANCH_ERROR,
    MERGE_BRANCH_SUCCESS,
    GET_ENTITIES_FOR_BRANCHES,
    IS_MAIN_BRANCH,
    GET_BRANCH_CHANGES,
    HANDLE_CONFLICT,
    HANDLE_MULTIPLE_CONFLICTS,
    CLEAR_CACHE,
    CLEAR_CACHE_SUCCESS,
    CLEAR_CACHE_ERROR,
    START_UPDATE_TIME_ACTIVE_TIMER,
    STOP_UPDATE_TIME_ACTIVE_TIMER,
    SET_ACTIVE_TIMER_INTERVAL,
    CLEAR_ACTIVE_TIMER_INTERVAL,
    UPDATE_ACTIVE_TIME,
    GENERATE_TOTP_BACKUP_CODES,
    GENERATE_TOTP_BACKUP_CODES_SUCCESS,
    GENERATE_TOTP_BACKUP_CODES_ERROR,
    CLEAR_LOCAL_TOTP_BACKUP_CODES,
    USE_TOTP_BACKUP_CODE,
    USE_TOTP_BACKUP_CODE_ERROR,
    USER_BACKUP_CODES_GENERATED
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
            loggedIn: false,
            totpQrImageUrl: ""
        },
        listOfUsers: [],
        resetPassword: false,
        requirePasswordChange: false
    }),

    mutations: {
        [AUTH_REQUEST]: (state, user) => {
            if (user && user.selectedUser) {
                state.loginStatus = "list_loading";
            }
            else if (user && (user.totpPin || user.totpBackupCode)) {
                state.loginStatus = "totp_loading";
            }
            else {
                state.loginStatus = "loading";
            }

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
        [AUTH_TOTP_SETUP]: (state, user) => {
            state.loginStatus = "totp";
            state.user = user;
            state.loginMessage = "";
            state.listOfUsers = [];
            state.totpQrImageUrl = user.totpQrImageUrl;
            state.requirePasswordChange = user.requirePasswordChange;
        },
        [AUTH_TOTP_PIN]: (state, user) => {
            state.loginStatus = "totp";
            state.user = user;
            state.loginMessage = "";
            state.listOfUsers = [];
            state.totpQrImageUrl = "";
            state.requirePasswordChange = user.requirePasswordChange;
        },
        [AUTH_ERROR]: (state, data) => {
            state.loginStatus = data.isTotpError ? "totp_error" : "error";
            state.loginMessage = data.message;
            state.listOfUsers = [];
            state.user = {
                name: "",
                role: "",
                lastLoginIpAddress: "",
                lastLoginDate: null,
                loggedIn: false,
                totpQrImageUrl: ""
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
                loggedIn: false,
                totpQrImageUrl: ""
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
        },
        [USE_TOTP_BACKUP_CODE]: (state) => {
            state.loginMessage = "";
        },
        [USE_TOTP_BACKUP_CODE_ERROR]: (state, message) => {
            state.loginMessage = message;
        },
        [USER_BACKUP_CODES_GENERATED]: (state) => {
            state.user.totpFirstTime = false;
        }
    },

    actions: {
        async [AUTH_REQUEST]({ commit, rootState }, data = {}) {
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

                if (!rootState.modules.allModules || !rootState.modules.allModules.length) {
                    await this.dispatch(MODULES_REQUEST);
                }

                // If a login log ID is also set in the user data, use it to start the "time active" timer.
                if (!rootState.users.updateTimeActiveTimerWorking && user.hasOwnProperty("encryptedLoginLogId")) {
                    await this.dispatch(START_UPDATE_TIME_ACTIVE_TIMER);
                }

                return;
            }

            const loginResult = await main.usersService.loginUser(user.username, user.password, (user.selectedUser || {}).username, user.totpPin, user.totpBackupCode);
            if (!loginResult.success) {
                commit(AUTH_ERROR, {
                    message: loginResult.message,
                    isTotpError: data.loginStatus === "totp"
                });
                return;
            }

            // If TOTP is enabled and not succeeded yet, show the 2FA step.
            if (loginResult.data.totpEnabled && !loginResult.data.totpSuccess) {
                commit(loginResult.data.totpQrImageUrl ? AUTH_TOTP_SETUP : AUTH_TOTP_PIN, loginResult.data);
                return;
            }

            // If the user that is logging in is an admin account, show a list of users for the customer.
            if (loginResult.data.adminLogin && !loginResult.data.adminAccountId) {
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

            if (!rootState.modules.allModules || !rootState.modules.allModules.length) {
                await this.dispatch(MODULES_REQUEST);
            }

            // If a login log ID is also set in the user data, use it to start the "time active" timer.
            if (!rootState.users.updateTimeActiveTimerWorking && user.hasOwnProperty("encryptedLoginLogId")) {
                await this.dispatch(START_UPDATE_TIME_ACTIVE_TIMER);
            }
        },

        [AUTH_LOGOUT]({ commit }) {
            localStorage.removeItem("accessToken");
            localStorage.removeItem("accessTokenExpiresOn");
            localStorage.removeItem("userData");
            sessionStorage.removeItem("userSettings");
            delete window.main.api.defaults.headers.common["Authorization"];
            this.dispatch(STOP_UPDATE_TIME_ACTIVE_TIMER);

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
        },

        async [USE_TOTP_BACKUP_CODE]({ commit }, data = {}) {
            commit(USE_TOTP_BACKUP_CODE);

            const result = await main.usersService.useTotpBackupCode(data);

            if (result.success) {
                commit(CHANGE_PASSWORD_SUCCESS);
            } else {
                commit(CHANGE_PASSWORD_ERROR, result.message);
            }
        },

        [USER_BACKUP_CODES_GENERATED]({ commit }) {
            commit(USER_BACKUP_CODES_GENERATED);
            const localUser = JSON.parse(localStorage.getItem("userData"));
            if (!localUser) {
                return;
            }

            localUser.totpFirstTime = false;
            localStorage.setItem("userData", JSON.stringify(localUser));
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

            let hasAutoload = false;
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

                    if (module.autoLoad) {
                        hasAutoload = true;
                    }
                }
            }

            if (!hasAutoload) {
                if (document.body.classList.contains("on-canvas")) {
                    document.body.classList.add("menu-active");
                    document.body.classList.remove("on-canvas");
                }
            } else {
                if (document.body.classList.contains("off-canvas")) {
                    document.body.classList.remove("off-canvas");
                    document.body.classList.add("on-canvas");
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
        changePasswordError: null,
        updateTimeActiveTimer: null,
        updateTimeActiveTimerWorking: false,
        totpBackupCodes: [],
        generateTotpBackupCodesError: null
    }),

    mutations: {
        [RESET_PASSWORD_SUCCESS]: (state) => {
            state.changePasswordError = null;
        },
        [CHANGE_PASSWORD_ERROR]: (state, message) => {
            state.changePasswordError = message;
        },
        [START_UPDATE_TIME_ACTIVE_TIMER]: (state) => {
            state.updateTimeActiveTimerWorking = true;
        },
        [STOP_UPDATE_TIME_ACTIVE_TIMER]: (state) => {
            state.updateTimeActiveTimerWorking = false;
        },
        [SET_ACTIVE_TIMER_INTERVAL]: (state, interval) => {
            state.updateTimeActiveTimer = interval;
        },
        [CLEAR_ACTIVE_TIMER_INTERVAL]: (state) => {
            if (state.updateTimeActiveTimer) {
                clearInterval(state.updateTimeActiveTimer);
            }
            state.updateTimeActiveTimer = null;
        },
        [GENERATE_TOTP_BACKUP_CODES_SUCCESS]: (state, backupCodes) => {
            state.totpBackupCodes = backupCodes || [];
            state.generateTotpBackupCodesError = null;
        },
        [GENERATE_TOTP_BACKUP_CODES_ERROR]: (state, message) => {
            state.generateTotpBackupCodesError = message;
            state.totpBackupCodes = [];
        },
        [CLEAR_LOCAL_TOTP_BACKUP_CODES]: (state) => {
            state.generateTotpBackupCodesError = null;
            state.totpBackupCodes = [];
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
        },

        async [START_UPDATE_TIME_ACTIVE_TIMER]({ commit }) {
            commit(START_REQUEST);

            // Clear old interval first.
            commit(CLEAR_ACTIVE_TIMER_INTERVAL);

            commit(START_UPDATE_TIME_ACTIVE_TIMER);
            const interval = await main.usersService.startUpdateTimeActiveTimer();
            commit(SET_ACTIVE_TIMER_INTERVAL, interval);

            commit(END_REQUEST);
        },

        [STOP_UPDATE_TIME_ACTIVE_TIMER]({ commit }) {
            commit(STOP_UPDATE_TIME_ACTIVE_TIMER);
            commit(CLEAR_ACTIVE_TIMER_INTERVAL);
        },

        async [UPDATE_ACTIVE_TIME]({ commit }) {
            commit(START_REQUEST);
            const result = await main.usersService.updateActiveTime();
            commit(END_REQUEST);
        },

        async [GENERATE_TOTP_BACKUP_CODES]({ commit }) {
            commit(START_REQUEST);
            const result = await main.usersService.generateTotpBackupCodes();
            commit(END_REQUEST);

            if (result.success) {
                commit(GENERATE_TOTP_BACKUP_CODES_SUCCESS, result.data);
            } else {
                commit(GENERATE_TOTP_BACKUP_CODES_ERROR, result.message);
            }
        },

        [CLEAR_LOCAL_TOTP_BACKUP_CODES]({ commit }) {
            commit(CLEAR_LOCAL_TOTP_BACKUP_CODES);
        }
    },

    getters: {}
};

const customersModule = {
    state: () => ({
        title: null,
        validSubDomain: true,
        createBranchError: null,
        createBranchResult: null,
        branches: []
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
        }
    },

    actions: {
        async [GET_CUSTOMER_TITLE]({ commit }, subDomain) {
            commit(START_REQUEST);
            const titleResponse = await main.customersService.getTitle(subDomain);
            commit(GET_CUSTOMER_TITLE, titleResponse.data);
            commit(VALID_SUB_DOMAIN, titleResponse.statusCode !== 404);
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

const branchesModule = {
    state: () => ({
        createBranchError: null,
        createBranchResult: null,
        branches: [],
        entities: [],
        isMainBranch: false,
        branchChanges: {}
    }),

    mutations: {
        [CREATE_BRANCH_SUCCESS](state, result) {
            state.createBranchResult = result;
            state.createBranchError = null;
        },

        [CREATE_BRANCH_ERROR](state, error) {
            state.createBranchResult = null;
            state.createBranchError = error;
        },

        [GET_BRANCHES](state, branches) {
            state.branches = branches;
        },

        [MERGE_BRANCH_SUCCESS](state, result) {
            state.mergeBranchResult = result;
            state.mergeBranchError = null;
        },

        [MERGE_BRANCH_ERROR](state, error) {
            state.mergeBranchResult = null;
            state.mergeBranchError = error;
        },

        [GET_ENTITIES_FOR_BRANCHES](state, entities) {
            state.entities = entities;
        },

        [IS_MAIN_BRANCH](state, isMainBranch) {
            state.isMainBranch = isMainBranch;
        },

        [GET_BRANCH_CHANGES](state, branchChanges) {
            state.branchChanges = branchChanges;
        },

        [HANDLE_CONFLICT](state, { acceptChange, id }) {
            if (!state.mergeBranchResult || !state.mergeBranchResult.conflicts || !state.mergeBranchResult.conflicts.length) {
                console.warn("Tried to handle conflict, but there are no conflicts.");
                return;
            }

            const conflict = state.mergeBranchResult.conflicts.find(c => c.id === id);
            if (!conflict) {
                console.warn("Tried to handle a conflict that doesn't exist.");
                return;
            }

            conflict.acceptChange = acceptChange;
        },

        [HANDLE_MULTIPLE_CONFLICTS](state, { acceptChange, settings }) {
            if (!settings || !settings.property || !settings.operator || !settings.start) {
                return;
            }

            if (!state.mergeBranchResult || !state.mergeBranchResult.conflicts || !state.mergeBranchResult.conflicts.length) {
                console.warn("Tried to handle conflicts, but there are no conflicts.");
                return;
            }

            for (let conflict of state.mergeBranchResult.conflicts) {
                let isDate = false;
                let valueToCheck;
                let startValue = settings.start;
                let endValue = settings.end;

                // Get the value of the property we want to check for the conflict to accept or deny.
                switch (settings.property) {
                    case "type":
                        valueToCheck = conflict.typeDisplayName;
                        break;
                    case "field":
                        valueToCheck = conflict.fieldDisplayName;
                        break
                    case "changeDateOriginal":
                        isDate = true;
                        valueToCheck = conflict.changeDateInMain;
                        break;
                    case "changeDateBranch":
                        isDate = true;
                        valueToCheck = conflict.changeDateInBranch;
                        break;
                    case "originalValue":
                        valueToCheck = conflict.valueInMain;
                        break;
                    case "branchValue":
                        valueToCheck = conflict.valueInBranch;
                        break;
                    default:
                        console.warn(`${HANDLE_MULTIPLE_CONFLICTS} - Unsupported property '${settings.property}'`)
                        continue;
                }

                if (!valueToCheck) {
                    continue;
                }

                // Format dates so that they're all the same format.
                if (isDate) {
                    valueToCheck = new Date(valueToCheck);
                    valueToCheck = new Date(valueToCheck.getFullYear(), valueToCheck.getMonth(), valueToCheck.getDate());

                    startValue = new Date(startValue);
                    startValue = new Date(startValue.getFullYear(), startValue.getMonth(), startValue.getDate());
                    if (endValue) {
                        endValue = new Date(endValue);
                        endValue = new Date(endValue.getFullYear(), endValue.getMonth(), endValue.getDate());
                    }
                }

                // Make sure string checks are case insensitive.
                if (!isDate) {
                    valueToCheck = valueToCheck.toLowerCase();
                    startValue = startValue.toLowerCase();
                }

                let found = false;
                switch (settings.operator) {
                    case "contains":
                        found = valueToCheck.indexOf(startValue) > -1;
                        break;
                    case "equals":
                        if (isDate) {
                            found = valueToCheck.getTime() === startValue.getTime();
                        } else {
                            found = valueToCheck.toString().toUpperCase() === startValue.toString().toUpperCase();
                        }
                        break;
                    case "greaterThan":
                        found = valueToCheck > startValue;
                        break;
                    case "lessThan":
                        found = valueToCheck < startValue;
                        break;
                    case "between":
                        found = valueToCheck >= startValue && valueToCheck <= endValue;
                        break;
                }

                if (!found) {
                    continue;
                }

                conflict.acceptChange = acceptChange;
            }
        }
    },

    actions: {
        async [CREATE_BRANCH]({ commit }, data) {
            commit(START_REQUEST);
            const result = await main.branchesService.create(data);

            if (result.success) {
                if (result.data) {
                    commit(CREATE_BRANCH_SUCCESS, result.data);
                } else {
                    commit(CREATE_BRANCH_ERROR, result.message);
                }
            } else {
                commit(CREATE_BRANCH_ERROR, result.message);
            }
            commit(END_REQUEST);
        },

        [CREATE_BRANCH_ERROR]({ commit }, error) {
            commit(CREATE_BRANCH_ERROR, error);
        },

        async [GET_BRANCHES]({ commit }) {
            commit(START_REQUEST);
            const branchesResponse = await main.branchesService.get();
            commit(GET_BRANCHES, branchesResponse.data);
            commit(END_REQUEST);
        },

        async [MERGE_BRANCH]({ commit }, data) {
            commit(START_REQUEST);
            const result = await main.branchesService.merge(data);

            if (result.success) {
                if (result.data) {
                    if (result.data.errors && result.data.errors.length > 0) {
                        let errorMessage;
                        if (result.data.successfulChanges) {
                            errorMessage = `Een deel van het overzetten is goed gegaan en een deel is fout gegaan. Er zijn ${result.data.successfulChanges} wijzigingen succesvol overgezet en de volgende fouten zijn opgetreden: ${result.data.errors.join("<br>")}`;
                        } else {
                            errorMessage = `Het overzetten is mislulkt. De volgende fouten zijn opgetreden: ${result.data.errors.join("<br>")}`;
                        }
                        commit(MERGE_BRANCH_ERROR, errorMessage);
                    } else {
                        commit(MERGE_BRANCH_SUCCESS, result.data);
                    }
                } else {
                    commit(MERGE_BRANCH_ERROR, result.message);
                }
            } else {
                commit(MERGE_BRANCH_ERROR, result.message);
            }
            commit(END_REQUEST);
        },

        [MERGE_BRANCH_ERROR]({ commit }, error) {
            commit(MERGE_BRANCH_ERROR, error);
        },

        async [GET_ENTITIES_FOR_BRANCHES]({ commit }) {
            commit(START_REQUEST);
            const entitiesResponse = await main.branchesService.getEntities();
            commit(GET_ENTITIES_FOR_BRANCHES, entitiesResponse.data);
            commit(END_REQUEST);
        },

        async [IS_MAIN_BRANCH]({ commit }) {
            commit(START_REQUEST);
            const entitiesResponse = await main.branchesService.isMainBranch();
            commit(IS_MAIN_BRANCH, entitiesResponse.data);
            commit(END_REQUEST);
        },

        async [GET_BRANCH_CHANGES]({ commit }, branchId) {
            commit(START_REQUEST);
            const changesResponse = await main.branchesService.getChanges(branchId);
            commit(GET_BRANCH_CHANGES, changesResponse.data);
            commit(MERGE_BRANCH_SUCCESS, null);
            commit(END_REQUEST);
        },

        [HANDLE_CONFLICT]({ commit }, payload) {
            commit(HANDLE_CONFLICT, payload);
        },

        [HANDLE_MULTIPLE_CONFLICTS]({ commit }, payload) {
            commit(HANDLE_MULTIPLE_CONFLICTS, payload);
        }
    },

    getters: {}
};

const cacheModule = {
    state: () => ({
        clearCacheError: null
    }),

    mutations: {
        [CLEAR_CACHE_SUCCESS]: (state) => {
            state.clearCacheError = null;
        },
        [CLEAR_CACHE_ERROR]: (state, message) => {
            state.clearCacheError = message;
        }
    },

    actions: {
        async [CLEAR_CACHE]({ commit }, data = {}) {
            commit(START_REQUEST);
            const result = await main.cacheService.clear(data);
            commit(END_REQUEST);

            if (result.success) {
                commit(CLEAR_CACHE_SUCCESS);
            } else {
                commit(CLEAR_CACHE_ERROR, result.message);
            }
        },

        [CLEAR_CACHE_ERROR]({ commit }, error) {
            commit(CLEAR_CACHE_ERROR, error);
        },
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
        items: itemsModule,
        branches: branchesModule,
        cache: cacheModule
    }
});
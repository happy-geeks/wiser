"use strict";

import {TrackJS} from "trackjs";
import {createApp, defineAsyncComponent} from "vue";
import axios from "axios";

import UsersService from "./shared/users.service";
import ModulesService from "./shared/modules.service";
import CustomersService from "./shared/customers.service";
import ItemsService from "./shared/items.service";
import BranchesService from "./shared/branches.service";

import store from "./store/index";
import login from "./components/login";
import taskAlerts from "./components/task-alerts";

import {DropDownList} from "@progress/kendo-vue-dropdowns";
import WiserDialog from "./components/wiser-dialog";

import "../scss/main.scss";
import "../scss/task-alerts.scss";

import {
    ACTIVATE_MODULE,
    AUTH_LOGOUT,
    AUTH_REQUEST,
    CHANGE_PASSWORD,
    CLOSE_ALL_MODULES,
    CLOSE_MODULE,
    CREATE_BRANCH,
    CREATE_BRANCH_ERROR,
    GET_BRANCH_CHANGES,
    GET_BRANCHES,
    GET_CUSTOMER_TITLE,
    GET_ENTITIES_FOR_BRANCHES,
    HANDLE_CONFLICT,
    HANDLE_MULTIPLE_CONFLICTS,
    IS_MAIN_BRANCH,
    LOAD_ENTITY_TYPES_OF_ITEM_ID,
    MERGE_BRANCH,
    OPEN_MODULE,
    TOGGLE_PIN_MODULE,
    CLEAR_CACHE,
    CLEAR_CACHE_ERROR,
    STOP_UPDATE_TIME_ACTIVE_TIMER,
    UPDATE_ACTIVE_TIME,
    GENERATE_TOTP_BACKUP_CODES,
    CLEAR_LOCAL_TOTP_BACKUP_CODES,
    USER_BACKUP_CODES_GENERATED,
    MODULES_REQUEST
} from "./store/mutation-types";
import CacheService from "./shared/cache.service";

class Main {
    constructor(settings) {
        this.vueApp = null;
        this.appSettings = null;

        this.usersService = new UsersService(this);
        this.modulesService = new ModulesService(this);
        this.customersService = new CustomersService(this);
        this.itemsService = new ItemsService(this);
        this.branchesService = new BranchesService(this);
        this.cacheService = new CacheService(this);

        // Fire event on page ready for direct actions
        document.addEventListener("DOMContentLoaded", () => {
            this.onPageReady();
        });
        window.addEventListener("message", this.handlePostMessage.bind(this));
    }

    /**
     * Do things that need to wait until the DOM has been fully loaded.
     */
    async onPageReady() {
        const configElement = document.getElementById("vue-config");
        this.appSettings = JSON.parse(configElement.innerHTML);

        if (this.appSettings.trackJsToken) {
            try {
                TrackJS.install({
                    token: this.appSettings.trackJsToken
                });
            } catch(exception) {
                console.error("Error loading TrackJS widget", exception);
            }
        }

        if (this.appSettings.loadPartnerStyle) {
            import(`../css/partner/${this.appSettings.subDomain}.css`);
        }

        this.api = axios.create({
            baseURL: this.appSettings.apiBase
        });

        this.api.interceptors.response.use(undefined, async (error) => {
            // Automatically re-authenticate with refresh token if login token expired or logout if that doesn't work or it is otherwise invalid.
            if (error.response.status === 401) {
                // If we ever get an unauthorized, logout the user.
                if (error.response.config.url === "/connect/token") {
                    this.vueApp.$store.dispatch(AUTH_LOGOUT);
                } else {
                    await this.vueApp.$store.dispatch(AUTH_REQUEST, { gotUnauthorized: true });
                }
            }

            return Promise.reject(error);
        });

        if (this.appSettings.markerIoToken) {
            try {
                const markerSdk = await import("@marker.io/browser");
                this.markerWidget = await markerSdk.default.loadWidget({ destination: this.appSettings.markerIoToken });
                this.markerWidget.hide();
            } catch(exception) {
                console.error("Error loading marker IO widget", exception);
            }
        }

        this.initVue();
    }

    async handlePostMessage(event) {
        if (!event.data || !event.data.action) {
            return;
        }

        switch (event.data.action) {
            case "OpenModule":
                this.vueApp.openModule(event.data.actionData.moduleId);
                break;
            case "OpenItem": {
                if (!event.data.actionData?.moduleId || (!event.data.actionData?.encryptedItemId && !event.data.actionData?.itemId)) {
                    break;
                }

                this.vueApp.openModule({
                    moduleId: event.data.actionData.moduleId,
                    name: event.data.actionData.name || "Item",
                    type: event.data.actionData.type || "dynamicItems",
                    iframe: true,
                    itemId: event.data.actionData.encryptedItemId ?? event.data.actionData.itemId,
                    fileName: "",
                    queryString: event.data.actionData.queryString ?? ""
                });
                break;
            }
            case "GetAccessToken": {
                // Access tokens can only be requested by origins that share the same main domain and on test environments.
                if (!event.source || (!event.origin.endsWith(`.${this.appSettings.currentDomain}`) && !this.appSettings.isTestEnvironment)) {
                    break;
                }

                // Request authentication, refreshing the token if needed.
                this.vueApp.$store.dispatch(AUTH_REQUEST).then(() => {
                    // Post a message back to the sender with the token and original request.
                    // The original request is sent back to the sender to allow the message data to be validated.
                    event.source.postMessage({
                        accessToken: this.vueApp.user.access_token,
                        originalRequest: event.data
                    }, event.origin);
                });
                break;
            }
            case "OpenClearCachePrompt": {
                this.vueApp.openClearCachePrompt();
                break;
            }
            case "OpenWiserBranchesPrompt": {
                this.vueApp.openWiserBranchesPrompt();
                break;
            }
            case "OpenMarkerIoScreen": {
                this.vueApp.openMarkerIoScreen();
                break;
            }
            case "OpenWiserIdPrompt": {
                this.vueApp.openWiserIdPrompt();
                break;
            }
            case "OpenChangePasswordPrompt": {
                this.vueApp.openChangePasswordPrompt();
                break;
            }
            case "OpenCustomerManagement": {
                this.vueApp.openCustomerManagement();
                break;
            }
            case "OpenGenerateTotpBackupCodesPrompt": {
                this.vueApp.openGenerateTotpBackupCodesPrompt();
                break;
            }
            case "OpenUserData": {
                const encryptedUserId = await main.itemsService.encryptId(this.vueApp.user.id);
                this.vueApp.openModule({
                    moduleId: 0,
                    name: "Mijn gegevens",
                    type: "dynamicItems",
                    iframe: true,
                    itemId: encryptedUserId,
                    fileName: "",
                    queryString: `?itemId=${encodeURIComponent(encryptedUserId)}&moduleId=0&iframe=true&entityType=wiseruser`
                });
                break;
            }
        }
    }

    initVue() {
        this.vueApp = createApp({
            data: () => {
                return {
                    appSettings: this.appSettings,
                    wiserIdPromptValue: null,
                    wiserEntityTypePromptValue: null,
                    markerWidget: this.markerWidget,
                    changePasswordPromptOldPasswordValue: null,
                    changePasswordPromptNewPasswordValue: null,
                    changePasswordPromptNewPasswordRepeatValue: null,
                    generalMessagePromptTitle: "",
                    generalMessagePromptText: "",
                    createBranchSettings: {
                        name: null,
                        startMode: "direct",
                        startOn: null,
                        entities: {
                            all: {
                                mode: 0
                            }
                        }
                    },
                    branchMergeSettings: {
                        selectedBranch: {
                            id: 0
                        },
                        startMode: "direct",
                        startOn: null,
                        deleteAfterSuccessfulMerge: false,
                        entities: {
                            all: {
                                everything: false,
                                create: false,
                                update: false,
                                delete: false
                            }
                        },
                        settings: {
                            all: {
                                everything: false,
                                create: false,
                                update: false,
                                delete: false
                            }
                        },
                        conflicts: []
                    },
                    openBranchTypes: [
                        { id: "wiser", name:  "Wiser" },
                        { id: "website", name: "Website" }
                    ],
                    openBranchSettings: {
                        selectedBranch: {
                            id: 0
                        },
                        websiteUrl: "",
                        selectedBranchType: {
                            id: ""
                        }
                    },
                    batchHandleConflictSettings: { },
                    clearCacheSettings: {
                        areas: [],
                        url: null
                    }
                };
            },
            async created() {
                this.$store.dispatch(GET_CUSTOMER_TITLE, this.appSettings.subDomain);
                document.addEventListener("keydown", this.onAppKeyDown.bind(this));
            },
            computed: {
                loginStatus() {
                    return this.$store.state.login.loginStatus;
                },
                mainLoader() {
                    return this.$store.state.base.mainLoader;
                },
                user() {
                    return this.$store.state.login.user;
                },
                requirePasswordChange() {
                    return this.$store.state.login.requirePasswordChange;
                },
                listOfUsers() {
                    return this.$store.state.login.listOfUsers;
                },
                modules() {
                    return this.$store.state.modules.allModules;
                },
                moduleGroups() {
                    return this.$store.state.modules.moduleGroups;
                },
                openedModules() {
                    return this.$store.state.modules.openedModules;
                },
                openedModulesWithBackend() {
                    return this.$store.state.modules.openedModules.filter(m => !m.javascriptOnly && m.type !== "Wiser1");
                },
                openedDynamicItemsModules() {
                    return this.$store.state.modules.openedModules.filter(m => m.type === "DynamicItems");
                },
                openedWiser1Modules() {
                    return this.$store.state.modules.openedModules.filter(m => m.type === "Wiser1");
                },
                activeModule() {
                    return this.$store.state.modules.activeModule;
                },
                hasModules() {
                    return this.$store.state.modules.allModules && this.$store.state.modules.allModules.length > 0;
                },
                listOfEntityTypes() {
                    return this.$store.state.items.listOfEntityTypes;
                },
                markerIoEnabled() {
                    return !!this.appSettings.markerIoToken;
                },
                customerTitle() {
                    return this.$store.state.customers.title;
                },
                validSubDomain() {
                    return this.$store.state.customers.validSubDomain;
                },
                changePasswordError() {
                    return this.$store.state.users.changePasswordError;
                },
                customerManagementIsOpened() {
                    return this.$store.state.modules.openedModules.filter(m => m.moduleId === "customerManagement").length > 0;
                },
                createBranchError() {
                    return this.$store.state.branches.createBranchError;
                },
                createBranchResult() {
                    return this.$store.state.branches.createBranchResult;
                },
                branches() {
                    return this.$store.state.branches.branches;
                },
                mergeBranchResult() {
                    return this.$store.state.branches.mergeBranchResult;
                },
                mergeBranchError() {
                    return this.$store.state.branches.mergeBranchError;
                },
                entitiesForBranches() {
                    return this.$store.state.branches.entities;
                },
                totalAmountOfItemsForCreatingBranch() {
                    return this.$store.state.branches.entities.reduce((accumulator, entity) => {
                        return accumulator + entity.totalItems;
                    }, 0);
                },
                isMainBranch() {
                    return this.$store.state.branches.isMainBranch;
                },
                branchChanges() {
                    return this.$store.state.branches.branchChanges;
                },
                totalAmountOfItemsCreated() {
                    return this.$store.state.branches.branchChanges.entities.reduce((accumulator, entity) => {
                        return accumulator + entity.created;
                    }, 0);
                },
                totalAmountOfItemsUpdated() {
                    return this.$store.state.branches.branchChanges.entities.reduce((accumulator, entity) => {
                        return accumulator + entity.updated;
                    }, 0);
                },
                totalAmountOfItemsDeleted() {
                    return this.$store.state.branches.branchChanges.entities.reduce((accumulator, entity) => {
                        return accumulator + entity.deleted;
                    }, 0);
                },
                totalAmountOfSettingsCreated() {
                    return this.$store.state.branches.branchChanges.settings.reduce((accumulator, entity) => {
                        return accumulator + entity.created;
                    }, 0);
                },
                totalAmountOfSettingsUpdated() {
                    return this.$store.state.branches.branchChanges.settings.reduce((accumulator, entity) => {
                        return accumulator + entity.updated;
                    }, 0);
                },
                totalAmountOfSettingsDeleted() {
                    return this.$store.state.branches.branchChanges.settings.reduce((accumulator, entity) => {
                        return accumulator + entity.deleted;
                    }, 0);
                },
                totalAmountOfMergeConflicts() {
                    if (!this.$store.state.branches.mergeBranchResult || !this.$store.state.branches.mergeBranchResult.conflicts) {
                        return 0;
                    }

                    return this.$store.state.branches.mergeBranchResult.conflicts.length;
                },
                totalAmountOfApprovedMergeConflicts() {
                    if (!this.$store.state.branches.mergeBranchResult || !this.$store.state.branches.mergeBranchResult.conflicts) {
                        return 0;
                    }

                    return this.$store.state.branches.mergeBranchResult.conflicts.filter(r => r.acceptChange === true).length;
                },
                areAllConflictsHandled() {
                    if (!this.$store.state.branches.mergeBranchResult || !this.$store.state.branches.mergeBranchResult.conflicts) {
                        return true;
                    }

                    return this.$store.state.branches.mergeBranchResult.conflicts.filter(r => r.acceptChange !== true && r.acceptChange !== false).length === 0;
                },
                clearCacheError() {
                    return this.$store.state.cache.clearCacheError;
                },
                openBranchUrl() {
                    if (!this.openBranchSettings || !this.openBranchSettings.selectedBranchType || !this.openBranchSettings.selectedBranchType.id) {
                        return "";
                    }

                    if (this.openBranchSettings.selectedBranchType.id === 'website' && !this.openBranchSettings.websiteUrl) {
                        return "";
                    }

                    if (!this.openBranchSettings || !this.openBranchSettings.selectedBranch|| !this.openBranchSettings.selectedBranch.id) {
                        return "";
                    }

                    let url;
                    switch (this.openBranchSettings.selectedBranchType.id) {
                        case "website":
                            url = this.openBranchSettings.websiteUrl;
                            if (!url.startsWith("http")) {
                                url = `https://${url}`
                            }

                            if (!url.endsWith("/")) {
                                url += "/";
                            }

                            url = `${url.substring(0, url.indexOf("/", 8))}/branches/${encodeURIComponent(this.openBranchSettings.selectedBranch.database.databaseName)}`;

                            break;
                        case "wiser":
                            url = `https://${this.openBranchSettings.selectedBranch.subDomain}.${this.appSettings.currentDomain}`;

                            break;
                        default:
                            return "";
                    }

                    return url;
                },
                generateTotpBackupCodesError() {
                    return this.$store.state.users.generateTotpBackupCodesError;
                },
                totpBackupCodes() {
                    return this.$store.state.users.totpBackupCodes;
                }
            },
            components: {
                "dropdownlist": DropDownList,
                "WiserDialog": WiserDialog,
                "customerManagement": defineAsyncComponent(() => import(/* webpackChunkName: "customer-management" */"./components/customer-management")),
                "login": login,
                "taskAlerts": taskAlerts
            },
            watch: {
                async loginStatus(newValue, oldValue) {
                    if (oldValue !== newValue && newValue === "success" && this.user.totpFirstTime && this.user.totpEnabled && !this.user.adminLogin) {
                        // If the user just finished setting up TOTP (2FA) authentication, then immediately generate backup codes for them.
                        this.openGenerateTotpBackupCodesPrompt();
                        await this.generateNewTotpBackupCodes();
                    }
                }
            },
            methods: {
                onAppKeyDown(event) {
                    // Open Wiser ID prompt when the user presses CTRL+O.
                    if (event.ctrlKey && event.key === "o") {
                        event.preventDefault();
                        this.openWiserIdPrompt();
                    }
                    // Open MarkerToScreen (Bug reporting) prompt when the user presses CTRL+B.
                    if (event.ctrlKey && event.key === "b") {
                        event.preventDefault();
                        this.openMarkerIoScreen();
                    }
                },

                handleBodyClick(event) {
                    if (event.target.id !== "side-menu" && !event.target.closest("#side-menu")) {
                        this.toggleMenuActive(false);
                    }
                },

                toggleMenuActive(show) {
                    if (show === undefined || show === null) {
                        show = !document.body.classList.contains("menu-active");
                    }

                    if (document.body.classList.contains("on-canvas")) {
                        if (show) {
                            document.body.classList.add("menu-active");
                        } else {
                            document.body.classList.remove("menu-active");
                        }
                    }
                },

                toggleMenuVisibility(show) {
                    if (show === undefined || show === null) {
                        show = !document.body.classList.contains("off-canvas");
                    }

                    if (show) {
                        document.body.classList.add("off-canvas");
                    } else {
                        document.body.classList.remove("off-canvas");
                    }
                },

                toggleMenuState() {
                    if (document.body.classList.contains("off-canvas")) {
                        document.body.classList.remove("off-canvas");
                        document.body.classList.add("on-canvas");

                    } else if (document.body.classList.contains("on-canvas")) {
                        document.body.classList.add("menu-active");
                        document.body.classList.remove("on-canvas");
                    } else {
                        document.body.classList.add("off-canvas");
                        document.body.classList.remove("menu-active");
                    }
                },

                showGeneralMessagePrompt(text = "", title = "") {
                    this.generalMessagePromptText = text;
                    this.generalMessagePromptTitle = title;
                    this.$refs.generalMessagePrompt.open();
                },

                async logout(event) {
                    if (event) {
                        event.preventDefault();
                    }

                    // Update the user's active time one last time.
                    await this.$store.dispatch(UPDATE_ACTIVE_TIME);
                    this.$store.dispatch(CLOSE_ALL_MODULES);
                    await this.$store.dispatch(AUTH_LOGOUT);
                },

                openModule(module) {
                    if (typeof module === "number" || typeof module === "string") {
                        module = this.modules.find(m => m.moduleId === module);
                    }
                    if (typeof (module.queryString) === "undefined") {
                        module.queryString = "";
                    }
                    this.$store.dispatch(OPEN_MODULE, module);
                },

                closeModule(module) {
                    let timeout = null;

                    const callback = () => {
                        if (timeout) {
                            clearTimeout(timeout);
                        }

                        this.$store.dispatch(CLOSE_MODULE, module);
                    };

                    // In case the module does not handle the moduleClosing event.
                    timeout = setTimeout(callback, 800);

                    if (!module || !module.id) {
                        callback();
                        return;
                    }

                    const moduleIframe = document.getElementById(module.id);
                    if (!moduleIframe || !moduleIframe.contentWindow || !moduleIframe.contentWindow.document) {
                        callback();
                        return;
                    }

                    moduleIframe.contentWindow.document.dispatchEvent(new CustomEvent("moduleClosing", { detail: callback }));
                },

                setActiveModule(event, moduleId) {
                    if (event.target && event.target.classList.contains("close-module")) {
                        return;
                    }

                    this.$store.dispatch(ACTIVATE_MODULE, moduleId);
                },

                async openCustomerManagement() {
                    this.openModule({
                        moduleId: "customerManagement",
                        name: "Klant toevoegen",
                        type: "customerManagement",
                        javascriptOnly: true,
                        onlyOneInstanceAllowed: true
                    });
                },

                openWiserIdPrompt(event) {
                    if (event) {
                        event.preventDefault();
                    }
                    this.$refs.wiserIdPrompt.open();
                },

                openWiserEntityTypePrompt(event) {
                    if (event) {
                        event.preventDefault();
                    }
                    this.$refs.wiserEntityTypePrompt.open();
                },

                openChangePasswordPrompt(event) {
                    if (event) {
                        event.preventDefault();
                    }
                    this.$refs.changePasswordPrompt.open();
                },

                openWiserBranchesPrompt(event) {
                    if (event) {
                        event.preventDefault();
                    }
                    this.$refs.wiserBranchesPrompt.open();
                },

                openCreateBranchPrompt(event) {
                    if (event) {
                        event.preventDefault();
                    }
                    this.$refs.wiserCreateBranchPrompt.open();
                    this.$refs.wiserBranchesPrompt.close();
                },

                openMergeBranchPrompt(event) {
                    if (event) {
                        event.preventDefault();
                    }
                    this.$refs.wiserMergeBranchPrompt.open();
                    this.$refs.wiserBranchesPrompt.close();
                },

                openMergeConflictsPrompt() {
                    this.$refs.wiserMergeConflictsPrompt.open();
                },

                openClearCachePrompt() {
                    if (localStorage.getItem("clear_cache_url")) {
                        this.clearCacheSettings.url = localStorage.getItem("clear_cache_url");
                    }
                    this.$refs.clearCachePrompt.open();
                },

                async openWiserItem() {
                    if (!this.wiserIdPromptValue || isNaN(parseInt(this.wiserIdPromptValue))) {
                        return false;
                    }

                    await this.$store.dispatch(LOAD_ENTITY_TYPES_OF_ITEM_ID, this.wiserIdPromptValue);
                    const encryptedId = await main.itemsService.encryptId(this.wiserIdPromptValue);

                    if (!this.listOfEntityTypes || !this.listOfEntityTypes.length) {
                        this.openModule({
                            moduleId: `wiserItem_${this.wiserIdPromptValue}`,
                            name: `Wiser item #${this.wiserIdPromptValue}`,
                            type: "dynamicItems",
                            iframe: true,
                            itemId: encryptedId,
                            fileName: "",
                            queryString: `?moduleId=0&iframe=true&itemId=${encodeURIComponent(encryptedId)}`
                        });

                        this.wiserIdPromptValue = null;
                        this.wiserEntityTypePromptValue = null;
                    } else if (this.listOfEntityTypes.length === 1) {
                        this.openModule({
                            moduleId: `wiserItem_${this.wiserIdPromptValue}_${this.listOfEntityTypes[0].id}`,
                            name: `Wiser item #${this.wiserIdPromptValue} (${this.listOfEntityTypes[0].displayName})`,
                            type: "dynamicItems",
                            iframe: true,
                            itemId: encryptedId,
                            fileName: "",
                            queryString: `?moduleId=0&iframe=true&itemId=${encodeURIComponent(encryptedId)}&entityType=${encodeURIComponent(this.listOfEntityTypes[0].id)}`
                        });

                        this.wiserIdPromptValue = null;
                        this.wiserEntityTypePromptValue = null;
                    } else if (!this.wiserEntityTypePromptValue) {
                        this.openWiserEntityTypePrompt();
                    } else {
                        this.openModule({
                            moduleId: `wiserItem_${this.wiserIdPromptValue}_${this.wiserEntityTypePromptValue.id}`,
                            name: `Wiser item #${this.wiserIdPromptValue} (${this.wiserEntityTypePromptValue.displayName})`,
                            type: "dynamicItems",
                            iframe: true,
                            itemId: encryptedId,
                            fileName: "",
                            queryString: `?moduleId=0&iframe=true&itemId=${encodeURIComponent(encryptedId)}&entityType=${encodeURIComponent(this.wiserEntityTypePromptValue.id)}`
                        });

                        this.wiserIdPromptValue = null;
                        this.wiserEntityTypePromptValue = null;
                    }

                    return true;
                },

                openMarkerIoScreen() {
                    this.markerWidget.capture("fullscreen");
                },

                async changePassword() {
                    await this.$store.dispatch(CHANGE_PASSWORD,
                        {
                            oldPassword: this.changePasswordPromptOldPasswordValue,
                            newPassword: this.changePasswordPromptNewPasswordValue,
                            newPasswordRepeat: this.changePasswordPromptNewPasswordRepeatValue
                        });

                    return !this.$store.state.users.changePasswordError;
                },

                async createBranch() {
                    if (!this.createBranchSettings.name) {
                        await this.$store.dispatch(CREATE_BRANCH_ERROR, "Vul a.u.b. een naam in");
                        return false;
                    }

                    await this.$store.dispatch(CREATE_BRANCH, this.createBranchSettings);

                    if (!this.createBranchError) {
                        this.$refs.wiserCreateBranchPrompt.close();
                        this.showGeneralMessagePrompt("De branch staat klaar om gemaakt te worden. U krijgt een bericht wanneer dit voltooid is.");

                        return true;
                    }

                    return false;
                },

                async mergeBranch() {
                    if (this.isMainBranch && (!this.branchMergeSettings.selectedBranch || !this.branchMergeSettings.selectedBranch.id)) {
                        return false;
                    }

                    if (this.mergeBranchResult && this.mergeBranchResult.conflicts && this.mergeBranchResult.conflicts.length > 0 && !this.areAllConflictsHandled) {
                        return false;
                    }

                    // Copy the conflicts to the merge settings, so that the WTS will know what to do with the conflicts.
                    if (this.mergeBranchResult && this.mergeBranchResult.conflicts) {
                        this.branchMergeSettings.conflicts = this.mergeBranchResult.conflicts;
                    }

                    await this.$store.dispatch(MERGE_BRANCH, this.branchMergeSettings);

                    if (this.mergeBranchError) {
                        return false;
                    }

                    if (this.mergeBranchResult.conflicts && this.mergeBranchResult.conflicts.length > 0) {
                        this.openMergeConflictsPrompt();
                        return true;
                    }

                    if (!this.mergeBranchError) {
                        this.$refs.wiserMergeBranchPrompt.close();
                        this.$refs.wiserMergeConflictsPrompt.close();
                        this.showGeneralMessagePrompt("De branch staat klaar om samengevoegd te worden. U krijgt een bericht wanneer dit voltooid is.");
                        return true;
                    }

                    return false;
                },

                async openOrCopyBranch(event, copy = false) {
                    event.preventDefault();

                    if (!this.openBranchSettings || !this.openBranchSettings.selectedBranchType || !this.openBranchSettings.selectedBranchType.id) {
                        this.showGeneralMessagePrompt("Kies a.u.b. of u de branch in Wiser wilt openen of op uw website.");
                        return false;
                    }

                    if (this.openBranchSettings.selectedBranchType.id === 'website' && !this.openBranchSettings.websiteUrl) {
                        this.showGeneralMessagePrompt("Vul a.u.b. de URL van uw website in.");
                        return false;
                    }

                    if (!this.openBranchSettings || !this.openBranchSettings.selectedBranch|| !this.openBranchSettings.selectedBranch.id) {
                        this.showGeneralMessagePrompt("Kies a.u.b. welke branch u wilt openen.");
                        return false;
                    }

                    if (copy) {
                        await navigator.clipboard.writeText(this.openBranchUrl);
                        this.showGeneralMessagePrompt("De URL is gekopieerd naar uw klembord.");
                        return;
                    }

                    window.open(this.openBranchUrl, "_blank");
                    this.$refs.wiserBranchesPrompt.close();
                },

                updateBranchChangeList(isChecked, setting, type, operation) {
                    if (type === "all") {
                        for (let entityOrSettingType of this.branchChanges[setting]) {
                            const key = entityOrSettingType.entityType || entityOrSettingType.type;
                            this.branchMergeSettings[setting][key] = this.branchMergeSettings[setting][key] || {};
                            switch (operation) {
                                case "everything":
                                    this.branchMergeSettings[setting][key].everything = isChecked;
                                    this.branchMergeSettings[setting][key].create = isChecked;
                                    this.branchMergeSettings[setting][key].update = isChecked;
                                    this.branchMergeSettings[setting][key].delete = isChecked;
                                    break;
                                default:
                                    this.branchMergeSettings[setting][key][operation] = isChecked;
                                    break;
                            }
                        }

                        if (operation === "everything") {
                            this.branchMergeSettings[setting].all.create = isChecked;
                            this.branchMergeSettings[setting].all.update = isChecked;
                            this.branchMergeSettings[setting].all.delete = isChecked;
                        }
                    } else if (operation === "everything") {
                        this.branchMergeSettings[setting][type].create = isChecked;
                        this.branchMergeSettings[setting][type].update = isChecked;
                        this.branchMergeSettings[setting][type].delete = isChecked;
                    }
                },

                async clearWebsiteCache() {
                    if (!this.clearCacheSettings.url || this.clearCacheSettings.url.length < 5) {
                        await this.$store.dispatch(CLEAR_CACHE_ERROR, "Vul a.u.b. een geldige URL in.");
                        return false;
                    }

                    if (!this.clearCacheSettings.areas || this.clearCacheSettings.areas.length === 0) {
                        await this.$store.dispatch(CLEAR_CACHE_ERROR, "Kies a.u.b. minimaal 1 cache optie om te legen.");
                        return false;
                    }

                    await this.$store.dispatch(CLEAR_CACHE, this.clearCacheSettings);
                    if (this.clearCacheError) {
                        return false;
                    }
                    window.localStorage.setItem("clear_cache_url", this.clearCacheSettings.url);
                    this.showGeneralMessagePrompt("De cache is succesvol geleegd.");
                    return true;
                },

                openGenerateTotpBackupCodesPrompt(event) {
                    if (event) {
                        event.preventDefault();
                    }
                    this.$refs.generateTotpBackupCodesPrompt.open();
                },

                async generateNewTotpBackupCodes() {
                    await this.$store.dispatch(GENERATE_TOTP_BACKUP_CODES);
                    this.$store.dispatch(USER_BACKUP_CODES_GENERATED);
                    return false;
                },

                async reloadModules() {
                    await this.$store.dispatch(MODULES_REQUEST);
                },

                openConfigurationModule(event) {
                    if (event) {
                        event.preventDefault();
                    }

                    const module = this.modules.find(module => module.type === "Configuration");
                    if (!module) {
                        kendo.alert("Configuratiemodule niet gevonden. Ververs a.u.b. de pagina en probeer het opnieuw, of neem contact op met ons.");
                        return;
                    }

                    this.openModule(module.moduleId);
                },

                onGenerateTotpBackupCodesPromptClose(event) {
                    this.$store.dispatch(CLEAR_LOCAL_TOTP_BACKUP_CODES);
                },

                onOpenModuleClick(event, module) {
                    event.preventDefault();
                    this.openModule(module);
                },

                onWiserIdPromptOpen(sender) {
                    setTimeout(() => document.getElementById("wiserId").focus(), 500);
                },

                onWiserIdFieldKeyPress(event) {
                    // Open the item when pressing enter.
                    if (event.charCode === 13) {
                        this.openWiserItem();
                        this.$refs.wiserIdPrompt.close();
                        return true;
                    }

                    // Only allow numbers. By default an input with type number still allows 'e' and decimal characters, we don't want that here.
                    if (event.charCode < 48 || event.charCode > 57) {
                        event.preventDefault();
                        return false;
                    }

                    return true;
                },

                async onTogglePin(event, moduleId) {
                    event.preventDefault();
                    this.$store.dispatch(TOGGLE_PIN_MODULE, moduleId);
                },

                async onWiserBranchesPromptOpen() {
                    await this.$store.dispatch(IS_MAIN_BRANCH);
                    await this.$store.dispatch(GET_BRANCHES);
                    if (!this.isMainBranch) {
                        this.openBranchSettings.selectedBranchType = {
                            id: "website",
                            name: "Website"
                        };

                        this.openBranchSettings.selectedBranch = this.branches.find(branch => branch.id === this.user.currentBranchId);
                    }
                },

                async onWiserMergeBranchPromptOpen(sender) {
                    if (this.branches && this.branches.length > 0) {
                        this.branchMergeSettings.selectedBranch = this.branches[0];
                        this.onSelectedBranchChange(this.branches[0].id);
                    }
                    else if (!this.isMainBranch) {
                        // If this is not the main branch, you can only synchronise the changes of the current branch, so get the changes immediately.
                        this.onSelectedBranchChange();
                    }
                },

                async onWiserCreateBranchPromptOpen() {
                    await this.$store.dispatch(GET_ENTITIES_FOR_BRANCHES);
                    this.createBranchSettings = {
                        name: null,
                        startMode: "direct",
                        startOn: null,
                        entities: {
                            all: {
                                mode: 0
                            }
                        }
                    };

                    for (let entity of this.entitiesForBranches) {
                        this.createBranchSettings.entities[entity.id] = {
                            mode: 0
                        };
                    }
                },

                async onSelectedBranchChange(event) {
                    let selectedBranchId = event;
                    if (!selectedBranchId) {
                        selectedBranchId = 0;
                    }
                    else if (selectedBranchId.target) {
                        selectedBranchId = event.target.value.id;
                    }

                    await this.$store.dispatch(GET_BRANCH_CHANGES, selectedBranchId);

                    // Clear all checkboxes.
                    this.branchMergeSettings.entities.all.everything = false;
                    this.branchMergeSettings.settings.all.everything = false;
                    this.updateBranchChangeList(false, "entities", "all", "everything");
                    this.updateBranchChangeList(false, "settings", "all", "everything");
                },

                onCreateBranchAllSettingsChange(event) {
                    for (let entity of this.entitiesForBranches) {
                        this.createBranchSettings.entities[entity.id] = Object.assign({}, this.createBranchSettings.entities.all);
                    }
                },

                onBranchMergeSettingChange(event, setting, type, operation) {
                    const isChecked = event.currentTarget.checked;
                    this.updateBranchChangeList(isChecked, setting, type, operation);
                },

                onApproveConflictClick(conflict) {
                    this.$store.dispatch(HANDLE_CONFLICT, { acceptChange: true, id: conflict.id });
                },

                onDenyConflictClick(conflict) {
                    this.$store.dispatch(HANDLE_CONFLICT, { acceptChange: false, id: conflict.id });
                },

                onAcceptMultipleConflictsClick() {
                    this.$store.dispatch(HANDLE_MULTIPLE_CONFLICTS, { acceptChange: true, settings: this.batchHandleConflictSettings });
                },

                onDenyMultipleConflictsClick() {
                    this.$store.dispatch(HANDLE_MULTIPLE_CONFLICTS, { acceptChange: false, settings: this.batchHandleConflictSettings });
                },

                onCacheTypeChecked(event, cacheType) {
                    const isChecked = event.currentTarget.checked;
                    const allTypes = [...document.querySelectorAll(".cache-type")].map(input => input.value);

                    if (cacheType === "all") {
                        if (!isChecked) {
                            this.clearCacheSettings.areas = [];
                        } else {
                            this.clearCacheSettings.areas = ["all", ...allTypes];
                        }
                    } else {
                        if (!isChecked) {
                            this.clearCacheSettings.areas = this.clearCacheSettings.areas.filter(type => type !== "all");
                        } else if (this.clearCacheSettings.areas.filter(type => type !== "all").length === allTypes.length && this.clearCacheSettings.areas.indexOf("all") === -1) {
                            this.clearCacheSettings.areas.push("all");
                        }
                    }
                }
            }
        });

        // Let Vue know about our store.
        this.vueApp.use(store);

        // Mount our app to the main HTML element.
        this.vueApp = this.vueApp.mount("#app");
    }
}

window.main = new Main();
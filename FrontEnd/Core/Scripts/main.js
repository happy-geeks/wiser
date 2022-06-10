"use strict";

import { TrackJS } from "trackjs";
import { createApp, defineAsyncComponent } from "vue";
import * as axios from "axios";

/*import SwiperCore, { Virtual } from "swiper";
import { Swiper, SwiperSlide } from "swiper/vue";
SwiperCore.use([Virtual]);*/

import UsersService from "./shared/users.service";
import ModulesService from "./shared/modules.service";
import CustomersService from "./shared/customers.service";
import ItemsService from "./shared/items.service";
import BranchesService from "./shared/branches.service";

import store from "./store/index";
import login from "./components/login";
import taskAlerts from "./components/task-alerts";

import { DropDownList } from "@progress/kendo-vue-dropdowns";
import WiserDialog from "./components/wiser-dialog";

import "../scss/main.scss";
import "../scss/task-alerts.scss";

import {
    AUTH_LOGOUT,
    AUTH_REQUEST,
    OPEN_MODULE,
    CLOSE_MODULE,
    CLOSE_ALL_MODULES,
    ACTIVATE_MODULE,
    LOAD_ENTITY_TYPES_OF_ITEM_ID,
    GET_CUSTOMER_TITLE,
    TOGGLE_PIN_MODULE,
    CHANGE_PASSWORD,
    CREATE_BRANCH,
    GET_BRANCHES, 
    MERGE_BRANCH,
    GET_ENTITIES_FOR_BRANCHES,
    IS_MAIN_BRANCH
} from "./store/mutation-types";

(() => {
    class Main {
        constructor(settings) {
            this.vueApp = null;
            this.appSettings = null;

            this.usersService = new UsersService(this);
            this.modulesService = new ModulesService(this);
            this.customersService = new CustomersService(this);
            this.itemsService = new ItemsService(this);
            this.branchesService = new BranchesService(this);

            // Fire event on page ready for direct actions
            document.addEventListener("DOMContentLoaded", () => {
                this.onPageReady();
            });
        }

        /**
         * Do things that need to wait until the DOM has been fully loaded.
         */
        async onPageReady() {
            const configElement = document.getElementById("vue-config");
            this.appSettings = JSON.parse(configElement.innerHTML);

            if (this.appSettings.trackJsToken) {
                TrackJS.install({
                    token: this.appSettings.trackJsToken
                });
            }

            if (this.appSettings.loadPartnerStyle) {
                import(`../scss/partner/${this.appSettings.subDomain}.scss`);
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
                const markerSdk = await import("@marker.io/browser");
                this.markerWidget = await markerSdk.default.loadWidget({ destination: this.appSettings.markerIoToken });
                this.markerWidget.hide();
            }

            this.initVue();
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
                        createBranchPromptValue: null,
                        selectedBranchValue: null,
                        entityCopySettings: {
                            all: -1
                        }
                    };
                },
                created() {
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
                    createBranchError() {
                        return this.$store.state.branches.createBranchError;
                    },
                    createBranchResult() {
                        return this.$store.state.branches.createBranchResult;
                    },
                    branches() {
                        return this.$store.state.branches.branches;
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
                    }
                },
                components: {
                    "dropdownlist": DropDownList,
                    "WiserDialog": WiserDialog,
                    "customerManagement": defineAsyncComponent(() => import(/* webpackChunkName: "customer-management" */"./components/customer-management")),
                    "login": login,
                    "taskAlerts": taskAlerts
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

                    logout() {
                        this.$store.dispatch(CLOSE_ALL_MODULES);
                        this.$store.dispatch(AUTH_LOGOUT);
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
                        this.$store.dispatch(CLOSE_MODULE, module);
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
                        event.preventDefault();
                        this.$refs.wiserIdPrompt.open();
                    },

                    openWiserEntityTypePrompt(event) {
                        event.preventDefault();
                        this.$refs.wiserEntityTypePrompt.open();
                    },

                    openChangePasswordPrompt(event) {
                        event.preventDefault();
                        this.$refs.changePasswordPrompt.open();
                    },

                    openWiserBranchesPrompt(event) {
                        event.preventDefault();
                        this.$refs.wiserBranchesPrompt.open();
                    },

                    openCreateBranchPrompt(event) {
                        event.preventDefault();
                        this.$refs.wiserCreateBranchPrompt.open();
                    },

                    openMergeBranchPrompt(event) {
                        event.preventDefault();
                        this.$refs.wiserMergeBranchPrompt.open();
                    },

                    openMergeConflictsPrompt(event) {
                        event.preventDefault();
                        this.$refs.wiserMergeConflictsPrompt.open();
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
                        if (!this.createBranchPromptValue) {
                            return false;
                        }

                        await this.$store.dispatch(CREATE_BRANCH, this.createBranchPromptValue);
                        return !this.createBranchError;
                    },

                    async mergeBranch() {
                        if (!this.selectedBranchValue || !this.selectedBranchValue.id) {
                            return false;
                        }

                        await this.$store.dispatch(MERGE_BRANCH, this.selectedBranchValue.id);
                        return !this.mergeBranchError;
                    },

                    handleMergeConflicts() {
                        alert("Conflicten verwerken");
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
                    },
                    
                    async onWiserMergeBranchPromptOpen(sender) {                        
                        await this.$store.dispatch(GET_BRANCHES);
                        if (this.branches && this.branches.length === 1) {
                            this.selectedBranchValue = this.branches[0];
                        }
                    },
                    
                    async onWiserCreateBranchPromptOpen() {
                        await this.$store.dispatch(GET_ENTITIES_FOR_BRANCHES);
                        for (let entity of this.entitiesForBranches) {
                            this.entityCopySettings[entity.id] = 0;
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
})();

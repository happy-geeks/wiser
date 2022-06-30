"use strict";

import { TrackJS } from "trackjs";
import { createApp, defineAsyncComponent } from "vue";
import * as axios from "axios";

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
    CREATE_BRANCH_ERROR,
    GET_BRANCHES, 
    MERGE_BRANCH,
    GET_ENTITIES_FOR_BRANCHES,
    IS_MAIN_BRANCH,
    GET_BRANCH_CHANGES
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
                                    mode: -1
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
                            }
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
                    
                    showGeneralMessagePrompt(text = "", title = "") {
                        this.generalMessagePromptText = text;
                        this.generalMessagePromptTitle = title;
                        this.$refs.generalMessagePrompt.open();
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
                        this.$refs.wiserBranchesPrompt.close();
                    },

                    openMergeBranchPrompt(event) {
                        event.preventDefault();
                        this.$refs.wiserMergeBranchPrompt.open();
                        this.$refs.wiserBranchesPrompt.close();
                    },

                    openMergeConflictsPrompt(event) {
                        //event.preventDefault();
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

                        await this.$store.dispatch(MERGE_BRANCH, this.branchMergeSettings);

                        if (!this.mergeBranchError) {
                            this.$refs.wiserMergeBranchPrompt.close();
                            this.showGeneralMessagePrompt("De branch staat klaar om samengevoegd te worden. U krijgt een bericht wanneer dit voltooid is.");
                            return true;
                        }

                        return false;
                    },

                    handleMergeConflicts() {
                        this.showGeneralMessagePrompt("Conflicten verwerken");
                    },

                    openBranch() {
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
                        
                        let url;
                        switch (this.openBranchSettings.selectedBranchType.id) {
                            case "website":
                                url = this.openBranchSettings.websiteUrl;
                                if (!url.startsWith("http")) {
                                    url = `https://${url}`
                                }
                                
                                url = `${url.substring(0, url.indexOf("/", 8))}/branches/${encodeURIComponent(this.openBranchSettings.selectedBranch.database.databaseName)}`;
                                
                                break;
                            case "wiser":
                                url = `https://${this.openBranchSettings.selectedBranch.subDomain}.${this.appSettings.currentDomain}`;
                                
                                break;
                            default:
                                console.error("Invalid branch type selected:", this.openBranchSettings.selectedBranchType);
                                this.showGeneralMessagePrompt("Kies a.u.b. of u de branch in Wiser wilt openen of op uw website.");
                                return false;
                        }
                        
                        window.open(url, "_blank");
                        this.$refs.wiserBranchesPrompt.close();
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


                            const extraUserData = await main.usersService.getLoggedInUserData();
                            console.log("extraUserData", extraUserData);
                            this.openBranchSettings.selectedBranch = this.branches.find(branch => branch.id === extraUserData.currentBranchId);
                            console.log("this.openBranchSettings.selectedBranch", this.openBranchSettings.selectedBranch);
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
                        for (let entity of this.branchChanges.entities) {
                            this.branchMergeSettings.entities[entity.entityType] = {
                                everything: false,
                                create: false,
                                update: false,
                                delete: false
                            };
                        }
                        for (let entity of this.branchChanges.settings) {
                            this.branchMergeSettings.settings[entity.type] = {
                                everything: false,
                                create: false,
                                update: false,
                                delete: false
                            };
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

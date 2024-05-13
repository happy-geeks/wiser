"use strict";

import { TrackJS } from "trackjs";
import ContentBox from "@innovastudio/contentbox"
import { createApp, ref, isProxy, toRaw } from "vue";
import axios from "axios";

import ContentBuildersService from "../../../Core/Scripts/shared/contentBuilders.service";
import {AUTH_LOGOUT, AUTH_REQUEST} from "../../../Core/Scripts/store/mutation-types";
import store from "../../../Core/Scripts/store";
import UsersService from "../../../Core/Scripts/shared/users.service";
import ModulesService from "../../../Core/Scripts/shared/modules.service";
import TenantsService from "../../../Core/Scripts/shared/tenants.service";
import ItemsService from "../../../Core/Scripts/shared/items.service";
import DataSelectorsService from "../../../Core/Scripts/shared/dataSelectors.service";

(() => {
    class Main {
        constructor(settings) {
            this.vueApp = null;
            this.appSettings = null;

            this.usersService = new UsersService(this);
            this.modulesService = new ModulesService(this);
            this.tenantsService = new TenantsService(this);
            this.itemsService = new ItemsService(this);
            this.contentBuildersService = new ContentBuildersService(this);
            this.dataSelectorsService = new DataSelectorsService(this);

            // Fire event on page ready for direct actions
            document.addEventListener("DOMContentLoaded", () => {
                this.onPageReady();
            });
        }

        /**
         * Do things that need to wait until the DOM has been fully loaded.
         */
        onPageReady() {
            const configElement = document.getElementById("vue-config");
            this.appSettings = JSON.parse(configElement.innerHTML);

            if (this.appSettings.trackJsToken) {
                TrackJS.install({
                    token: this.appSettings.trackJsToken
                });
            }

            this.api = axios.create({
                baseURL: this.appSettings.apiBase
            });

            this.api.defaults.headers.common["Authorization"] = `Bearer ${localStorage.getItem("accessToken")}`;

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

            this.initVue();
        }

        initVue() {
            this.vueApp = createApp({
                data: () => {
                    return {
                        appSettings: this.appSettings,
                        contentBox: null,
                        html: ""
                    }
                },
                async mounted() {             
                    // Create object with settings for content box.
                    const settings = {
                        wrapper: "#ContentBoxWrapper",

                        imageSelect: "/ContentBox/assets.html",
                        fileSelect: "/ContentBox/assets.html",
                        videoSelect: "/ContentBox/assets.html",

                        slider: "glide",
                        navbar: true,

                        scriptPath: "/ContentBox/scripts/",
                        pluginPath: "/ContentBox/contentbuilder/",
                        assetPath: "/ContentBox/assets/",
                        modulePath: "/ContentBox/assets/modules/",
                        fontAssetPath: "/ContentBox/assets/fonts/",
                        contentStylePath: "/ContentBox/assets/styles/",
                        zoom: 0.97,
                        plugins: [
                            { name: 'WiserDataSelector', showInMainToolbar: true, showInElementToolbar: true }
                        ],
                    };

                    // Get data we need from database.
                    const promises = [];
                    promises.push(main.contentBuildersService.getHtml(this.appSettings.wiserItemId, this.appSettings.languageCode, this.appSettings.propertyName));
                    promises.push(main.contentBuildersService.getTenantSnippets());
                    promises.push(main.contentBuildersService.getTemplateCategories());
                    promises.push(main.contentBuildersService.getFramework());
                    const data = await Promise.all(promises);
                    this.html = data[0].data || "";
                    const snippetJson = data[1].data.tenantSnippets;
                    const snippetCategories = data[1].data.snippetCategories;
                    const templateCategories = data[2].data;
                    settings.framework = (data[3].data || "").toLowerCase();
                    if (settings.framework === "contentbuilder") {
                        settings.framework = "";
                    }
                    
                    if (!snippetJson || !snippetJson.length || !snippetCategories || !snippetCategories.length) {
                        // No snippets found in tenant database, use default snippets that are supplied by ContentBox.
                        settings.snippetUrl ="/ContentBox/assets/minimalist-blocks/content.js";
                        settings.snippetPath = "/ContentBox/assets/minimalist-blocks/";
                        settings.snippetData = "/ContentBox/assets/minimalist-blocks/snippetlist.html";
                        settings.snippetPathReplace = ["assets/", "/ContentBox/assets/"];

                        // Default content builder categories.
                        settings.snippetCategories = [[120, "Basic"], [118, "Article"], [101, "Headline"], [119, "Buttons"], [102, "Photos"], [103, "Profile"], [116, "Contact"], [104, "Products"], [105, "Features"], [106, "Process"], [107, "Pricing"], [108, "Skills"], [109, "Achievements"], [110, "Quotes"], [111, "Partners"], [112, "As Featured On"], [113, "Page Not Found"], [114, "Coming Soon"], [115, "Help, FAQ"]];
                        settings.defaultSnippetCategory = settings.snippetCategories[0][0];
                    } else {
                        // If we have snippets from database, only use those and not the default ones of the ContentBox.
                        let defaultSnippetCategory;
                        if (snippetCategories.length > 0 && snippetCategories[0].length > 0) {
                            defaultSnippetCategory = snippetCategories[0][0];
                        }

                        settings.snippetPath = "";
                        settings.snippetUrl = "";
                        settings.snippetData = "";
                        settings.snippetCategories = snippetCategories;
                        settings.defaultSnippetCategory = defaultSnippetCategory;
                        
                        // This is required to make the snippets work in ContentBox, their code checks for this property.
                        window.data_basic = {snippets: snippetJson};
                        window._snippets_path = "";
                    } 
                    
                    if (!templateCategories || !templateCategories.length) {
                        settings.templates = [
                            {
                                url: "/ContentBox/assets/simplestart/templates.js",
                                path: "/ContentBox/assets/simplestart/",
                                pathReplace: []
                            },
                            {
                                url: "/ContentBox/assets/quickstart/templates.js",
                                path: "/ContentBox/assets/quickstart/",
                                pathReplace: []
                            }
                        ];
                    } else {
                        settings.templates = [
                            {
                                url: `${this.appSettings.apiRoot}content-builder/template.js?encryptedUserId=${encodeURIComponent(this.appSettings.encryptedUserId)}&subDomain=${encodeURIComponent(this.appSettings.subDomain)}`,
                                path: "",
                                pathReplace: []
                            }
                        ];
                        settings.featuredCategories = templateCategories;
                        settings.defaultCategory = templateCategories[0];
                    }
                    
                    this.contentBox = new ContentBox(settings);
                    this.contentBox.loadHtml(this.html);
                },
                computed: {
                },
                components: {
                },
                methods: {
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
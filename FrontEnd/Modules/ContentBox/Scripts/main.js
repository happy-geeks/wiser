"use strict";

import { TrackJS } from "trackjs";
import ContentBox from "@innovastudio/contentbox"
/*import "./lang/en.js";
import "../Css/contentbuilder.css"
import "../Css/contentbuilder-wiser.scss"*/
import { createApp, ref, isProxy, toRaw } from "vue";
import * as axios from "axios";

import ContentBuildersService from "../../../Core/Scripts/shared/contentBuilders.service";
import {AUTH_LOGOUT, AUTH_REQUEST} from "../../../Core/Scripts/store/mutation-types";
import store from "../../../Core/Scripts/store";
import UsersService from "../../../Core/Scripts/shared/users.service";
import ModulesService from "../../../Core/Scripts/shared/modules.service";
import CustomersService from "../../../Core/Scripts/shared/customers.service";
import ItemsService from "../../../Core/Scripts/shared/items.service";
import DataSelectorsService from "../../../Core/Scripts/shared/dataSelectors.service";

(() => {
    class Main {
        constructor(settings) {
            this.vueApp = null;
            this.appSettings = null;

            this.usersService = new UsersService(this);
            this.modulesService = new ModulesService(this);
            this.customersService = new CustomersService(this);
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
                    const html = await main.contentBuildersService.getHtml(this.appSettings.wiserItemId, this.appSettings.languageCode, this.appSettings.propertyName);
                    
                    this.contentBox = new ContentBox({
                        wrapper: "#ContentBoxWrapper",

                        imageSelect: "/ContentBox/assets.html",
                        fileSelect: "/ContentBox/assets.html",
                        videoSelect: "/ContentBox/assets.html",

                        slider: "glide",
                        navbar: true,
                        
                        templates: [
                            {
                                url: "/ContentBox/assets/simplestart/templates.js",
                                path: "/ContentBox/assets/simplestart/",
                                pathReplace: []
                            },
                            {
                                url: "/ContentBox/assets/quickstart/templates.js",
                                path: "/ContentBox/assets/quickstart/",
                                pathReplace: []
                            },
                        ],

                        scriptPath: "/ContentBox/scripts/",
                        pluginPath: "/ContentBox/contentbuilder/",
                        assetPath: "/ContentBox/assets/",
                        modulePath: "/ContentBox/assets/modules/",
                        fontAssetPath: "/ContentBox/assets/fonts/",
                        contentStylePath: "/ContentBox/assets/styles/",
                        snippetUrl: "/ContentBox/assets/minimalist-blocks/content.js",
                        snippetPath: "/ContentBox/assets/minimalist-blocks/",
                        snippetData: "/ContentBox/assets/minimalist-blocks/snippetlist.html",
                        snippetPathReplace: ["assets/", "/ContentBox/assets/"],
                        zoom: 0.97
                    });
                    
                    this.contentBox.loadHtml(html.data || "");
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
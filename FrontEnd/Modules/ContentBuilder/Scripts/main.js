"use strict";

import {TrackJS} from "trackjs";
import ContentBuilder from "@innovastudio/contentbuilder"
import "./lang/en.js";
import "../Css/contentbuilder.css"
import "../Css/contentbuilder-wiser.scss"
import {createApp, toRaw} from "vue";
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
                        contentBuilder: null,
                        html: "",
                        base64Handler: "/upload",
                        largerImageHandler: "/upload",
                        imageSelect: "/images.html"
                    };
                },
                async created() {
                    const promises = [];
                    const uri = window.location.search.substring(1);
                    const params = new URLSearchParams(uri);
                    promises.push(main.contentBuildersService.getHtml(params.get("wiserItemId"), params.get("languageCode"), params.get("propertyName")));
                    promises.push(main.contentBuildersService.getTenantSnippets());
                    promises.push(main.contentBuildersService.getFramework());
                    const data = await Promise.all(promises);
                    this.html = data[0].data || "";
                    window.tenantSnippets = data[1].data.tenantSnippets;
                    const snippetCategories = data[1].data.snippetCategories;
                    let framework = (data[2].data || "").toLowerCase();
                    if (framework === "contentbuilder") {
                        framework = "";
                    }

                    this.contentBuilder = new ContentBuilder({
                        container: ".container",
                        scriptPath: "/ContentBuilder/scripts/",
                        pluginPath: "/ContentBuilder/",
                        modulePath: "/ContentBuilder/assets/modules/",
                        fontAssetPath: "/ContentBuilder/assets/fonts/",
                        assetPath: "/ContentBuilder/assets/",
                        snippetOpen: true,
                        toolbar: "top",
                        snippetUrl: "/ContentBuilder/assets/minimalist-blocks/content.js",
                        snippetPath: "",
                        snippetData: "/ContentBuilder/assets/minimalist-blocks/snippetlist.html",
                        snippetCategories: snippetCategories,
                        defaultSnippetCategory: snippetCategories[0][0],
                        framework: framework,
                        plugins: [
                            { name: 'WiserDataSelector', showInMainToolbar: true, showInElementToolbar: true }
                        ],
                        onImageBrowseClick: () => {
                            window.parent.dynamicItems.fields.onHtmlEditorImageExec(null, null, null, this.contentBuilder);
                        }
                    });

                    // Load initial/saved content
                    this.contentBuilder.loadHtml(this.html);
                },
                computed: {
                },
                components: {
                },
                methods: {
                    viewSnippets() {
                        this.contentBuilder.viewSnippets();
                    },

                    viewHtml() {
                        // We use toRaw here, because Vue adds a Proxy around the object, but that causes problems when calling some functions from the content builder.
                        // For example, the vieHtml function crashes the browser tab when you try to call it via the proxy of Vue.
                        toRaw(this.contentBuilder).viewHtml();
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
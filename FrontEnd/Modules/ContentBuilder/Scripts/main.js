"use strict";

import { TrackJS } from "trackjs";
import ContentBuilder from "@innovastudio/contentbuilder"
import "./lang/en.js";
import "../Css/contentbuilder.css"
import "../Css/contentbuilder-wiser.scss"
import { createApp, ref } from "vue";
import * as axios from "axios";

import ContentBuildersService from "../../../Core/Scripts/shared/contentBuilders.service";

(() => {
    class Main {
        constructor(settings) {
            this.vueApp = null;
            this.appSettings = null;

            this.contentBuildersService = new ContentBuildersService(this);

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

            this.api.defaults.headers.common["Authorization"] = `Bearer ${localStorage.getItem("access_token")}`;

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
                        imageSelect: "/images.html",
                        snippetFile: "/ContentBuilder/assets/minimalist-blocks/content.js"
                    };
                },
                async created() {
                    const promises = [];
                    const uri = window.location.search.substring(1); 
                    const params = new URLSearchParams(uri);
                    promises.push(main.contentBuildersService.getHtml(params.get("wiserItemId"), params.get("languageCode"), params.get("propertyName")));
                    promises.push(main.contentBuildersService.getCustomerSnippets());
                    const data = await Promise.all(promises);
                    this.html = data[0].data || "";
                    window.customerSnippets = data[1].data.customerSnippets;
                    const snippetCategories = data[1].data.snippetCategories;

                    this.contentBuilder = new ContentBuilder({
                        container: ".container",
                        scriptPath: "/ContentBuilder/scripts/",
                        pluginPath: "/ContentBuilder/scripts/",
                        modulePath: "/ContentBuilder/assets/modules/",
                        fontAssetPath: "/ContentBuilder/assets/fonts/",
                        assetPath: "/ContentBuilder/assets/",
                        snippetOpen: true,
                        toolbar: "top",
                        snippetData: "/ContentBuilder/assets/minimalist-blocks/snippetlist.html",
                        snippetCategories: snippetCategories,
                        defaultSnippetCategory: snippetCategories[0][0],
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
                        this.contentBuilder.viewHtml();
                    }
                }
            });

            // Mount our app to the main HTML element.
            this.vueApp = this.vueApp.mount("#app");
        }
    }

    window.main = new Main();
})();
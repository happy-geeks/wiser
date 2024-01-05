import { TrackJS } from "trackjs";
import { Wiser } from "../../Base/Scripts/Utils.js";
require("@progress/kendo-ui/js/kendo.all.js");
require("@progress/kendo-ui/js/cultures/kendo.culture.nl-NL.js");
require("@progress/kendo-ui/js/messages/kendo.messages.nl-NL.js");

import "../Css/TaskHistory.css";
import {TaskUtils} from "./TaskUtils";

// Any custom settings can be added here. They will overwrite most default settings inside the module.
const moduleSettings = {
    
};

((moduleSettings) => {
    // Main Class
    class TaskHistory {
        constructor(settings) {
            kendo.culture("nl-NL");

            // Base settings.
            this.settings = {
                pageSize: 100
            };
            Object.assign(this.settings, settings);
            
            // Add logged in user access token to default authorization headers for all jQuery ajax requests.
            $.ajaxSetup({
                headers: { "Authorization": `Bearer ${localStorage.getItem("accessToken")}` }
            });

            this.mainGrid = null;
            this.mainGridFirstLoad = true;

            this.backendUsers = [];

            // Fire event on page ready for direct actions
            document.addEventListener("DOMContentLoaded", () => {
                this.onPageReady();
            });
        }

        async onPageReady() {
            Object.assign(this.settings, $("body").data());

            if (this.settings.trackJsToken) {
                TrackJS.install({
                    token: this.settings.trackJsToken
                });
            }
            
            // Show an error if the user is no longer logged in.
            const accessTokenExpires = localStorage.getItem("accessTokenExpiresOn");
            if (!accessTokenExpires || accessTokenExpires <= new Date()) {
                Wiser.alert({
                    title: "Niet ingelogd",
                    content: "U bent niet (meer) ingelogd. Ververs a.u.b. de pagina en probeer het opnieuw."
                });
                
                this.toggleMainLoader(false);
                return;
            }

            const user = JSON.parse(localStorage.getItem("userData"));
            this.settings.oldStyleUserId = user.oldStyleUserId;
            this.settings.username = user.adminAccountName ? `${user.adminAccountName} (Admin)` : user.name;
            this.settings.adminAccountLoggedIn = user.adminAccountName;
            
            const userData = await Wiser.getLoggedInUserData(this.settings.wiserApiRoot);
            this.settings.userId = userData.encryptedId;
            this.settings.tenantId = userData.encryptedTenantId;
            this.settings.zeroEncrypted = userData.zeroEncrypted;
            this.settings.wiserUserId = userData.id;
            
            if (!this.settings.wiserApiRoot.endsWith("/")) {
                this.settings.wiserApiRoot += "/";
            }
            
            this.backendUsers = await Wiser.api({ url: `${this.settings.wiserApiRoot}users` });
            console.log(this.backendUsers);

            this.initializeMainGrid();
        }

        /**
         * Get the name of a back end user.
         * @param {number} id The ID of the user.
         * @param {boolean} showUnknownPrefix Whether the prefix "Onbekend" should be added to the result if user couldn't be found.
         * @returns {string} The name of the user, or the ID if the user couldn't be found.
         */
        getBackEndUserName(id, showUnknownPrefix = false) {
            if (!this.backendUsers || !this.backendUsers.length) {
                return id;
            }

            if (typeof id === "string" && id === "") {
                // Check for empty string, otherwise "NaN" will be returned.
                if (showUnknownPrefix) {
                    return `Onbekend`;
                } else {
                    return id;
                }
            }

            if (typeof id !== "number") {
                // Make sure id is a number.
                id = Number.parseInt(id, 10);
            }

            const user = this.backendUsers.find(u => u.id === id);
            if (!user) {
                if (showUnknownPrefix) {
                    return `Onbekend (${id})`;
                } else {
                    return id;
                }
            }

            return user.title;
        }

        /**
         * Does everything to make sure that the main grid works properly.
         */
        async initializeMainGrid() {
            try {
                const mainGridElement = $("#task-grid");

                const options = {
                    page: 1,
                    pageSize: this.settings.pageSize,
                    skip: 0,
                    take: this.settings.pageSize,
                    filter: {
                        logic: "and",
                        filters: [
                            { field: "receiver", operator: "eq", value: this.settings.wiserUserId }
                        ]
                    }
                };

                const gridDataResult = await Wiser.api({
                    url: `${this.settings.wiserApiRoot}items/${encodeURIComponent(this.settings.zeroEncrypted)}/entity-grids/agendering?mode=2&moduleId=${this.settings.moduleId}`,
                    method: "POST",
                    contentType: "application/json",
                    data: JSON.stringify(options)
                });

                if (gridDataResult.extraJavascript) {
                    jQuery.globalEval(gridDataResult.extraJavascript);
                }

                for (let column of gridDataResult.columns) {
                    if (column.field !== "sender" && column.field !== "receiver") {
                        continue;
                    }

                    column.template = `#: window.taskHistory.getBackEndUserName(${column.field}, true) #`;

                    column.filterable = {
                        ui: (element) => {
                            element.kendoDropDownList({
                                dataSource: this.backendUsers,
                                dataTextField: "title",
                                dataValueField: "id",
                                optionLabel: "--Kies een waarde--"
                            });
                        },
                        operators: {
                            string: {
                                eq: "Is gelijk aan",
                                neq: "Is niet gelijk aan"
                            }
                        }
                    };
                }

                gridDataResult.columns.push({
                    title: "Acties",
                    width: 150,
                    template: (dataItem) => {
                        if (dataItem.receiver !== this.settings.wiserUserId.toString() || !dataItem.checkeddate) {
                            return "";
                        }
                        
                        return `<button type="button" class="k-button k-button-md k-rounded-md k-button-solid k-button-solid-base returnTaskButton"><span class="k-icon k-i-undo k-button-icon"></span><span class="k-button-text">Terugzetten</span></button>`;
                    }
                })

                this.mainGridFirstLoad = true;
                this.mainGrid = mainGridElement.kendoGrid({
                    dataSource: {
                        serverPaging: true,
                        serverSorting: true,
                        serverFiltering: true,
                        pageSize: gridDataResult.pageSize,
                        filter: [
                            { field: "receiver", operator: "eq", value: this.settings.wiserUserId }
                        ],
                        sort: { field: "duedate", dir: "desc" },
                        transport: {
                            read: async (transportOptions) => {
                                try {
                                    if (this.mainGridFirstLoad) {
                                        transportOptions.success(gridDataResult);
                                        this.mainGridFirstLoad = false;
                                        return;
                                    }

                                    const newGridDataResult = await Wiser.api({
                                        url: `${this.settings.wiserApiRoot}items/${encodeURIComponent(this.settings.zeroEncrypted)}/entity-grids/agendering?mode=2&moduleId=${this.settings.moduleId}`,
                                        method: "POST",
                                        contentType: "application/json",
                                        data: JSON.stringify(transportOptions.data)
                                    });

                                    transportOptions.success(newGridDataResult);
                                } catch (exception) {
                                    console.error(exception);
                                    transportOptions.error(exception);
                                }
                            }
                        },
                        schema: {
                            data: "data",
                            total: "totalResults",
                            model: gridDataResult.schemaModel
                        }
                    },
                    columns: gridDataResult.columns,
                    resizable: true,
                    sortable: true,
                    scrollable: {
                        virtual: true
                    },
                    filterable: {
                        extra: false
                    },
                    height: "100%",
                    dataBound: (event) => {
                        event.sender.content.find(".returnTaskButton").click((clickEvent) => {
                            clickEvent.preventDefault();
                            const dataItem = event.sender.dataItem(clickEvent.currentTarget.closest("tr"));
                            TaskUtils.returnTask(dataItem.encryptedid, this.settings.username, this.settings.wiserApiRoot).then(() => {
                                event.sender.dataSource.read();

                                const notification = $("<div />").kendoNotification({
                                    autoHideAfter: 3000
                                }).data("kendoNotification");

                                notification.show("De taak is teruggezet naar de todo-lijst.");
                            });
                        })
                    }
                }).getKendoGrid();
            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan met het initialiseren van het overzicht. Probeer het a.u.b. nogmaals of neem contact op met ons.");
            }
        }
    }

    window.taskHistory = new TaskHistory(moduleSettings);
})(moduleSettings);

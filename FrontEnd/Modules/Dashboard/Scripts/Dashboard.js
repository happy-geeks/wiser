import { TrackJS } from "trackjs";
import { Wiser2 } from "../../Base/Scripts/Utils.js";
import "../../Base/Scripts/Processing.js";
require("@progress/kendo-ui/js/kendo.all.js");
require("@progress/kendo-ui/js/cultures/kendo.culture.nl-NL.js");
require("@progress/kendo-ui/js/messages/kendo.messages.nl-NL.js");

import "../../../Core/Scss/fonts.scss"; // TEMP
import "../../../Core/Scss/icons.scss"; // TEMP
import "../css/Dashboard.scss";

// Any custom settings can be added here. They will overwrite most default settings inside the module.
const moduleSettings = {
};

((jQuery, moduleSettings) => {
    class Dashboard {
        constructor(settings) {
            kendo.culture("nl-NL");

            // Base settings.
            this.settings = {};
            Object.assign(this.settings, settings);

            // Other.
            this.mainLoader = null;

            // Fire event on page ready for direct actions
            document.addEventListener("DOMContentLoaded", () => {
                this.onPageReady();
            });
        }

        async onPageReady() {
            this.mainLoader = $("#mainLoader");

            // Setup processing.
            document.addEventListener("processing.Busy", this.toggleMainLoader.bind(this, true));
            document.addEventListener("processing.Idle", this.toggleMainLoader.bind(this, false));

            // Setup any settings from the body element data. These settings are added via the Wiser backend and they take preference.
            Object.assign(this.settings, $(document.body).data());

            // this.useExportMode = document.getElementById("useExportMode").checked;

            if (this.settings.trackJsToken) {
                TrackJS.install({
                    token: this.settings.trackJsToken
                });
            }

            // Add logged in user access token to default authorization headers for all jQuery ajax requests.
            $.ajaxSetup({
                headers: { "Authorization": `Bearer ${localStorage.getItem("accessToken")}` }
            });

            // Show an error if the user is no longer logged in.
            const accessTokenExpires = localStorage.getItem("accessTokenExpiresOn");
            if (!accessTokenExpires || accessTokenExpires <= new Date()) {
                Wiser2.alert({
                    title: "Niet ingelogd",
                    content: "U bent niet (meer) ingelogd. Ververs a.u.b. de pagina en probeer het opnieuw."
                });

                this.toggleMainLoader(false);
                return;
            }

            const user = JSON.parse(localStorage.getItem("userData"));
            this.settings.oldStyleUserId = user.oldStyleUserId;
            this.settings.username = user.adminAccountName ? `Happy Horizon (${user.adminAccountName})` : user.name;
            this.settings.adminAccountLoggedIn = !!user.adminAccountName;

            const userData = await Wiser2.getLoggedInUserData(this.settings.wiserApiRoot);
            this.settings.userId = userData.encryptedId;
            this.settings.customerId = userData.encryptedCustomerId;
            this.settings.zeroEncrypted = userData.zeroEncrypted;
            this.settings.wiser2UserId = userData.id;

            this.settings.serviceRoot = `${this.settings.wiserApiRoot}templates/get-and-execute-query`;
            this.settings.getItemsUrl = `${this.settings.wiserApiRoot}data-selectors`;

            this.container = document.getElementById("dataBuilder");
            this.connectionBlocksContainer = document.getElementById("connectionBlocks");
            this.havingContainer = $(document.getElementById("havingContainer"));

            // Initialize the rest.
            this.initializeWindow();
            this.initializeKendoElements();
            this.initializeCodeMirrorElements();

            this.dataLoad.initialize();

            // Create a single connection to start with.
            this.mainConnection = this.addConnection(this.connectionBlocksContainer, true);

            this.setBindings();
        }

        /**
         * Shows or hides the main (full screen) loader.
         * @param {boolean} show True to show the loader, false to hide it.
         */
        toggleMainLoader(show) {
            this.mainLoader.toggleClass("loading", show);
        }

        setBindings() {
        }

        initializeWindow() {
        }

        initializeKendoElements() {
            // create ComboBox from select HTML element
            $(".combo-select").kendoComboBox();

            // create DateRangePicker
            $(".daterangepicker").kendoDateRangePicker({
                "messages": {
                    "startLabel": "van",
                    "endLabel": "tot"
                },
                culture: "nl-NL",
                format: "dd/MM/yyyy"
            });

            // create Tiles
            var tileLayout = $("#tiles").kendoTileLayout({
                containers: [{
                    colSpan: 7,
                    rowSpan: 2,
                    header: {
                        text: "Data"
                    },
                    bodyTemplate: kendo.template($("#data-chart-template").html())
                }, {
                    colSpan: 5,
                    rowSpan: 2,
                    header: {
                        text: "Gebruikers"
                    },
                    bodyTemplate: kendo.template($("#users-chart-template").html())
                }, {
                    colSpan: 7,
                    rowSpan: 1,
                    header: {
                        text: "Abonnement"
                    },
                    bodyTemplate: kendo.template($("#subscriptions-chart-template").html())
                }, {
                    colSpan: 5,
                    rowSpan: 1,
                    header: {
                        text: "Update log"
                    },
                    bodyTemplate: kendo.template($("#update-log").html())
                }, {
                    colSpan: 12,
                    rowSpan: 1,
                    header: {
                        text: "Services"
                    },
                    bodyTemplate: kendo.template($("#services-grid-template").html())
                }, {
                    colSpan: 4,
                    rowSpan: 1,
                    header: {
                        text: ""
                    },
                    bodyTemplate: kendo.template($("#numbers").html())
                }, {
                    colSpan: 4,
                    rowSpan: 1,
                    header: {
                        text: ""
                    },
                    bodyTemplate: kendo.template($("#status-chart-template").html())
                }, {
                    colSpan: 4,
                    rowSpan: 1,
                    header: {
                        text: ""
                    },
                    bodyTemplate: kendo.template($("#dataselector-rate").html())
                }],
                columns: 12,
                columnsWidth: 300,
                gap: {
                    columns: 30,
                    rows: 30
                },
                rowsHeight: 250,
                reorderable: true,
                resizable: true,
                resize: function (e) {
                    var rowSpan = e.container.css("grid-column-end");
                    var chart = e.container.find(".k-chart").data("kendoChart");
                    // hide chart labels when the space is limited
                    if (rowSpan === "span 1" && chart) {
                        chart.options.categoryAxis.labels.visible = false;
                        chart.redraw();
                    }
                    // show chart labels when the space is enough
                    if (rowSpan !== "span 1" && chart) {
                        chart.options.categoryAxis.labels.visible = true;
                        chart.redraw();
                    }

                    // for widgets that do not auto resize
                    // https://docs.telerik.com/kendo-ui/styles-and-layout/using-kendo-in-responsive-web-pages
                    kendo.resize(e.container, true);
                }
            }).data("kendoTileLayout");
        }

        initializeCodeMirrorElements() {
        }
    }

    window.dashboard = new Dashboard(moduleSettings);
})($, moduleSettings);
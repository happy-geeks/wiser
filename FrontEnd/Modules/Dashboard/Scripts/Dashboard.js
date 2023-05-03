import { TrackJS } from "trackjs";
import { Wiser } from "../../Base/Scripts/Utils.js";
import "../../Base/Scripts/Processing.js";
require("@progress/kendo-ui/js/kendo.all.js");
require("@progress/kendo-ui/js/cultures/kendo.culture.nl-NL.js");
require("@progress/kendo-ui/js/messages/kendo.messages.nl-NL.js");

import "../../../Core/Scss/fonts.scss";
import "../../../Core/Scss/icons.scss";
import "../css/Dashboard.scss";

// Any custom settings can be added here. They will overwrite most default settings inside the module.
const moduleSettings = {
};

(($, moduleSettings) => {
    const defaultLayoutSettings = Object.freeze([
        {
            tileId: "dataChart",
            colSpan: 7,
            rowSpan: 4
        },
        {
            tileId: "usersChart",
            colSpan: 5,
            rowSpan: 4
        },
        {
            tileId: "updateLog",
            colSpan: 5,
            rowSpan: 2
        },
        {
            tileId: "services",
            colSpan: 12,
            rowSpan: 2
        },
        {
            tileId: "entityData",
            colSpan: 4,
            rowSpan: 2
        },
        {
            tileId: "taskAlerts",
            colSpan: 4,
            rowSpan: 2
        },
        {
            tileId: "dataSelector",
            colSpan: 4,
            rowSpan: 2
        }
    ]);

    class Dashboard {
        constructor(settings) {
            kendo.culture("nl-NL");

            // Base settings.
            this.settings = {};
            Object.assign(this.settings, settings);

            // Other.
            this.mainLoader = null;
            this.tileLayout = null;

            this.itemsData = null;
            this.userData = null;
            this.entityData = null;
            this.openTaskAlertsData = null;
            this.dataSelectorResult = null;

            this.servicesGrid = null;
            this.serviceWindow = null;
            this.serviceLogsGrid = null;

            // Fire event on page ready for direct actions
            document.addEventListener("DOMContentLoaded", this.onPageReady.bind(this));
        }

        async onPageReady() {
            this.mainLoader = $("#mainLoader");

            // Setup processing.
            document.addEventListener("processing.Busy", this.toggleMainLoader.bind(this, true));
            document.addEventListener("processing.Idle", this.toggleMainLoader.bind(this, false));

            // Setup any settings from the body element data. These settings are added via the Wiser backend and they take preference.
            Object.assign(this.settings, $(document.body).data());

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
            this.settings.adminAccountLoggedIn = !!user.adminAccountName;

            const userData = await Wiser.getLoggedInUserData(this.settings.wiserApiRoot);
            this.settings.userId = userData.encryptedId;
            this.settings.customerId = userData.encryptedCustomerId;
            this.settings.zeroEncrypted = userData.zeroEncrypted;
            this.settings.wiserUserId = userData.id;

            // Initialize the rest.
            await this.initializeKendoElements();

            // Start the period picker as read-only.
            $("#periodPicker").getKendoDateRangePicker().readonly(true);

            // Update data.
            window.processing.addProcess("dataUpdate");
            await Promise.all([
                this.updateBranches(),
                this.updateData()
            ]);
            window.processing.removeProcess("dataUpdate");

            this.setBindings();
        }

        /**
         * Shows or hides the main (full screen) loader.
         * @param {boolean} show True to show the loader, false to hide it.
         */
        toggleMainLoader(show) {
            this.mainLoader.toggleClass("loading", show);
        }

        /**
         * Bind the various keys and update events.
         */
        setBindings() {
            document.getElementById("editSub").querySelectorAll("input[type='checkbox'][data-toggle-tile]").forEach((checkbox) => {
                checkbox.addEventListener("change", (event) => {
                    if (event.currentTarget.checked) {
                        this.addTile(checkbox.dataset.toggleTile);
                    } else {
                        this.removeTile(checkbox.dataset.toggleTile);
                    }
                });
            });

            const dataChartElement = document.getElementById("data-chart");
            if (dataChartElement) {
                const itemsTypeFilterButtons = Array.from(document.getElementById("itemsTypeFilterButtons").querySelectorAll("button"));
                itemsTypeFilterButtons.forEach((button) => {
                    button.addEventListener("click", (event) => {
                        itemsTypeFilterButtons.filter((btn) => btn !== event.currentTarget).forEach((btn) => btn.classList.remove("selected"));
                        event.currentTarget.classList.add("selected");

                        this.updateItemsDataChart();
                    });
                });
            }

            const usersChartElement = document.getElementById("users-chart");
            if (usersChartElement) {
                const userDataTypeFilterButtons = Array.from(document.getElementById("userDataTypeFilterButtons").querySelectorAll("button"));
                userDataTypeFilterButtons.forEach((button) => {
                    button.addEventListener("click", (event) => {
                        userDataTypeFilterButtons.filter((btn) => btn !== event.currentTarget).forEach((btn) => btn.classList.remove("selected"));
                        event.currentTarget.classList.add("selected");

                        this.updateUserDataChart();
                    });
                });
            }

            const entityDataElement = document.getElementById("entityData");
            if (entityDataElement) {
                const entityDataTypeFilterButtons = Array.from(document.getElementById("entityDataTypeFilterButtons").querySelectorAll("button"));
                entityDataTypeFilterButtons.forEach((button) => {
                    button.addEventListener("click", (event) => {
                        entityDataTypeFilterButtons.filter((btn) => btn !== event.currentTarget).forEach((btn) => btn.classList.remove("selected"));
                        event.currentTarget.classList.add("selected");

                        this.updateEntityUsageData();
                    });
                });
            }

            $("#periodFilter").getKendoDropDownList().bind("change", this.onPeriodFilterChange.bind(this));

            $("#periodPicker").getKendoDateRangePicker().bind("change", async () => {
                window.processing.addProcess("dataUpdate");
                await this.updateData();
                window.processing.removeProcess("dataUpdate");
            });

            $("#branchesSelect").getKendoDropDownList().bind("change", async () => {
                window.processing.addProcess("dataUpdate");
                await this.updateData();
                window.processing.removeProcess("dataUpdate");
            });

            document.getElementById("refreshDataButton").addEventListener("click", () => {
                Wiser.showConfirmDialog("Weet u zeker dat u de data wilt verversen? Dit is mogelijk een zware belasting op de database.",
                    "Verversen",
                    "Annuleren",
                    "Verversen").then(async () => {
                    window.processing.addProcess("dataUpdate");
                    await this.updateData(true);
                    window.processing.removeProcess("dataUpdate");
                });
            });

            document.getElementById("refreshServices").addEventListener("click", async e => {
                await this.updateServices();
            });
        }

        /**
         * Initializes the dashboard's elements with Kendo widgets.
         */
        async initializeKendoElements() {
            // create ComboBox from select HTML element
            $(".combo-select").kendoComboBox();

            // create DropDownList from select HTML element
            $(".drop-down-list").kendoDropDownList();

            // create DateRangePicker
            $(".daterangepicker").kendoDateRangePicker({
                messages: {
                    startLabel: "van",
                    endLabel: "tot"
                },
                culture: "nl-NL",
                format: "dd/MM/yyyy"
            });

            // Retrieve layout settings.
            const layoutJson = await Wiser.api({
                url: `${this.settings.wiserApiRoot}users/dashboard-settings`,
                method: "GET"
            });

            let layoutSettings;
            try {
                layoutSettings = JSON.parse(layoutJson);

                if (!Array.isArray(layoutSettings) || layoutSettings.length === 0) {
                    layoutSettings = [...defaultLayoutSettings];
                }
            } catch (e) {
                layoutSettings = [...defaultLayoutSettings];
            }

            // Build the containers for the Kendo TileLayout widget.
            const containers = [];
            layoutSettings.forEach((tileSettings) => {
                const menuItem = document.getElementById(`${tileSettings.tileId}Checkbox`);
                if (!menuItem) {
                    // If the menu item doesn't exist, then the current tile is invalid and shouldn't be handled.
                    return;
                }

                menuItem.checked = true;

                const tileTemplateSettings = Dashboard.GetTileTemplateSettingsByTileId(tileSettings.tileId);
                containers.push({
                    colSpan: tileSettings.colSpan,
                    rowSpan: tileSettings.rowSpan,
                    header: {
                        text: tileTemplateSettings.headerText
                    },
                    bodyTemplate: kendo.template($(`#${tileTemplateSettings.bodyTemplateId}`).html())
                });
            });

            // create Tiles
            this.tileLayout = $("#tiles").kendoTileLayout({
                containers: containers,
                columns: 12,
                columnsWidth: 300,
                gap: {
                    columns: 30,
                    rows: 30
                },
                rowsHeight: 125,
                reorderable: true,
                resizable: true,
                resize: this.onTileLayoutResize.bind(this),
                reorder: this.onTileLayoutReorder.bind(this)
            }).data("kendoTileLayout");

            this.tileLayout.element.on("click", ".k-close-button", this.onTileClose.bind(this));

            this.initializeItemsDataChart();
            this.initializeUsersDataChart();
            this.initializeTaskAlertsDataChart();
            this.initializeServicesGrid();

            this.serviceWindow = $("#serviceLogWindow").kendoWindow({
                actions: ["Close"],
                visible: false
            }).data("kendoWindow");

            this.serviceTemplateWindow = $("#serviceTemplateWindow").kendoWindow({
                iframe: true,
                actions: ["Close"],
                visible: false
            }).data("kendoWindow");
        }

        /**
         * Initializes the Wiser items count data chart.
         */
        initializeItemsDataChart() {
            const dataChartElement = document.getElementById("data-chart");
            if (!dataChartElement) return;

            $(dataChartElement).kendoChart({
                title: {
                    text: "Aantal items in Wiser"
                },
                legend: {
                    position: "top"
                },
                seriesDefaults: {
                    type: "column"
                },
                series: [{
                    name: "Actief",
                    field: "amountOfItems",
                    color: "#FF6800"
                }, {
                    name: "Archief",
                    field: "amountOfArchivedItems",
                    color: "#2ECC71"
                }],
                valueAxis: {
                    labels: {
                        format: "{0}"
                    },
                    line: {
                        visible: false
                    },
                    axisCrossingValue: 0
                },
                categoryAxis: {
                    categories: [],
                    line: {
                        visible: false
                    },
                    labels: {
                        padding: { top: 10 }
                    }
                },
                tooltip: {
                    visible: true,
                    format: "{0}",
                    template: "#= series.name #: #= value #"
                }
            });
        }

        /**
         * Initializes the user login data chart.
         */
        initializeUsersDataChart() {
            const usersChartElement = document.getElementById("users-chart");
            if (!usersChartElement) return;

            $(usersChartElement).kendoChart({
                title: {
                    position: "top",
                    text: "Top 10 gebruikers met meeste tijd / aantal x ingelogd"
                },
                legend: {
                    visible: false
                },
                chartArea: {
                    background: ""
                },
                seriesDefaults: {
                    labels: {
                        visible: true,
                        background: "transparent",
                        template: "#= category #: \n #= value#%"
                    }
                },
                series: [{
                    type: "pie",
                    startAngle: 90,
                    data: [{
                        category: "Rest",
                        value: 0,
                        color: "#2ECC71"
                    }, {
                        category: "Top 10 gebruikers",
                        value: 0,
                        color: "#FF6800"
                    }]
                }],
                tooltip: {
                    visible: true,
                    format: "{0}%"
                }
            });
        }

        /**
         * Initializes the open task alerts data chart.
         */
        initializeTaskAlertsDataChart() {
            const statusChartElement = document.getElementById("status-chart");
            if (!statusChartElement) return;

            $(statusChartElement).kendoChart({
                title: {
                    text: "Openstaande agenderingen per gebruiker",
                    visible: false
                },
                legend: {
                    visible: false
                },
                chartArea: {
                    background: ""
                },
                seriesDefaults: {
                    labels: {
                        visible: false,
                        background: "transparent",
                        template: "#= category #: \n #= value#"
                    }
                },
                series: [{
                    type: "pie",
                    startAngle: 90,
                    data: []
                }],
                tooltip: {
                    visible: true,
                    template: "#= category #: #= value#"
                }
            });
        }

        /**
         * Initializes the services grid.
         */
        initializeServicesGrid() {
            const servicesGridElement = document.getElementById("services-grid");
            if (!servicesGridElement) return;

            this.servicesGrid = $(servicesGridElement).kendoGrid({
                columns: [
                    {
                        field: "id",
                        hidden: true
                    },
                    {
                        title: "Configuratie",
                        field: "configuration"
                    },
                    {
                        title: "Actie",
                        field: "action",
                        template: "#if(data.action != null) {# #: data.action # #} else {# #: data.timeId # #}#"
                    },
                    {
                        title: "Schema",
                        field: "scheme",
                        values: [
                            { text: "Doorlopend", value: "continuous" },
                            { text: "Dagelijks", value: "daily" },
                            { text: "Wekelijks", value: "weekly" },
                            { text: "Maandelijks", value: "monthly" }
                        ]
                    },
                    {
                        title: "Laatste run",
                        field: "lastRun",
                        format: "{0:dd-MM-yyyy HH:mm}"
                    },
                    {
                        title: "Laatste tijd",
                        field: "runTime",
                        template: "#=kendo.toString(runTime, '0.000')# minuten"
                    },
                    {
                        title: "Status",
                        field: "state",
                        values: [
                            { text: "Actief", value: "active" },
                            { text: "Succesvol", value: "success" },
                            { text: "Waarschuwing", value: "warning" },
                            { text: "Mislukt", value: "failed" },
                            { text: "Gepauzeerd", value: "paused" },
                            { text: "Gestopt", value: "stopped" },
                            { text: "Gecrasht", value: "crashed" },
                            { text: "Bezig", value: "running" }
                        ]
                    },
                    {
                        title: "Volgende run",
                        field: "nextRun",
                        format: "{0:dd-MM-yyyy HH:mm}"
                    },
                    {
                        title: "Beheer",
                        command: [
                            {
                                name: "start",
                                text: "",
                                iconClass: "extra-run-button-icon k-icon k-i-play",
                                click: this.toggleExtraRunService.bind(this)
                            },
                            {
                                name: "pause",
                                text: "",
                                iconClass: "pause-button-icon wiser-icon icon-stopwatch-pauze",
                                click: this.togglePauseService.bind(this)
                            },
                            {
                                name: "logs",
                                text: "",
                                iconClass: "k-icon k-i-file-txt",
                                click: this.openServiceLogs.bind(this)
                            },
                            {
                                name: "edit",
                                text: "",
                                iconClass: "edit-template-button k-icon k-i-edit",
                                click: this.editServiceTemplate.bind(this)
                            }
                        ],
                        attributes: {
                            "class": "admin"
                        }
                    }
                ],
                dataBound: this.setServiceState.bind(this)
            }).data("kendoGrid");
            this.servicesGrid.scrollables[1].classList.add("fixed-table");
        }

        /**
         * Retrieves information about the branches this Wiser installation has.
         */
        async updateBranches() {
            const branches = await Wiser.api({
                url: `${this.settings.wiserApiRoot}branches`
            });

            let dataSource = [
                { text: "Actieve branch", value: 0 },
                { text: "Alle branches", value: -1 }
            ];
            dataSource = dataSource.concat(branches.map(ds => {
                return {
                    text: ds.name,
                    value: ds.id
                };
            }));

            const branchesSelect = $("#branchesSelect").getKendoDropDownList();
            branchesSelect.setDataSource(dataSource);

            // Select first item, which is always the current branch.
            branchesSelect.select(0);
        }

        /**
         * Updates the data selector result by retrieving it from the API.
         */
        async getDataSelectorResult() {
            try {
                this.dataSelectorResult = await Wiser.api({
                    url: `${this.settings.wiserApiRoot}dashboard/dataselector`
                });
                if (!this.dataSelectorResult) {
                    this.dataSelectorResult = {};
                }
                this.updateDataSelectorContent();
            } catch (exception) {
                console.error(exception);
                Wiser.alert({
                    title: "Ophalen dataselector data mislukt",
                    content: "Het ophalen van de data selector data is mislukt. Probeer het a.u.b. nogmaals, of neem contact met ons op."
                });
            }
        }

        /**
         * Retrieves data from the server.
         */
        async updateData(forceRefresh = false) {
            const dateRange = $("#periodPicker").getKendoDateRangePicker().range();

            let periodFrom = null;
            let periodTo = null;
            if (dateRange !== null) {
                periodFrom = kendo.toString(dateRange.start, "yyyy-MM-dd");
                periodTo = kendo.toString(dateRange.end, "yyyy-MM-dd");
            }

            const getParameters = {
                branchId: $("#branchesSelect").getKendoDropDownList().value()
            };
            if (periodFrom !== null) {
                getParameters.periodFrom = periodFrom;
            }
            if (periodTo !== null) {
                getParameters.periodTo = periodTo;
            }
            if (forceRefresh) {
                getParameters.forceRefresh = forceRefresh;
            }

            await this.updateServices();

            const data = await Wiser.api({
                url: `${this.settings.wiserApiRoot}dashboard`,
                data: getParameters
            });
            if (!data) {
                return;
            }

            // Update items usage.
            this.itemsData = data.items;
            this.updateItemsDataChart();

            // Update user data.
            const loginCountTotal = data.userLoginCountTop10 + data.userLoginCountOther;
            let loginCountTop10Percentage = 0;
            let loginCountOtherPercentage = 0;

            if (loginCountTotal > 0) {
                loginCountTop10Percentage = Math.round(((data.userLoginCountTop10 / loginCountTotal) * 100) * 10) / 10;
                loginCountOtherPercentage = 100 - loginCountTop10Percentage;
            }

            const loginTimeTotal = data.userLoginActiveTop10 + data.userLoginActiveOther;
            let loginTimeTop10Percentage = 0;
            let loginTimeOtherPercentage = 0;

            if (loginTimeTotal > 0) {
                loginTimeTop10Percentage = Math.round(((data.userLoginActiveTop10 / loginTimeTotal) * 100) * 10) / 10;
                loginTimeOtherPercentage = 100 - loginTimeTop10Percentage;
            }

            this.userData = {
                loginCount: [
                    {
                        category: "Rest",
                        value: loginCountOtherPercentage,
                        color: "#2ECC71"
                    },
                    {
                        category: "Top 10 gebruikers",
                        value: loginCountTop10Percentage,
                        color: "#FF6800"
                    }
                ],
                loginActive: [
                    {
                        category: "Rest",
                        value: loginTimeOtherPercentage,
                        color: "#2ECC71"
                    },
                    {
                        category: "Top 10 gebruikers",
                        value: loginTimeTop10Percentage,
                        color: "#FF6800"
                    }
                ]
            };

            this.updateUserDataChart();

            // Update entity usage data.
            this.entityData = data.entities;
            this.updateEntityUsageData();

            // Create task alert data.
            const openTaskAlertsData = [];
            for (let prop in data.openTaskAlerts) {
                openTaskAlertsData.push({
                    category: prop,
                    value: data.openTaskAlerts[prop]
                });
            }
            this.openTaskAlertsData = openTaskAlertsData;
            this.updateOpenTaskAlertsChart();

            // Update data selector result.
            await this.getDataSelectorResult();
        }

        /**
         * Update/refresh the Wiser items usage chart.
         */
        updateItemsDataChart() {
            const dataChartElement = document.getElementById("data-chart");
            if (!dataChartElement) return;

            const filter = document.getElementById("itemsTypeFilterButtons").querySelector("button.selected").dataset.filter;
            const categories = this.itemsData[filter].map((e) => e.entityName);

            const dataChart = $(dataChartElement).getKendoChart();
            dataChart.setOptions({
                categoryAxis: {
                    categories: categories
                }
            });
            console.log("bla", filter, this.itemsData[filter]);
            dataChart.setDataSource(this.itemsData[filter]);
        }

        /**
         * Update/refresh the Wiser user data chart.
         */
        updateUserDataChart() {
            const usersChartElement = document.getElementById("users-chart");
            if (!usersChartElement) return;

            const filter = document.getElementById("userDataTypeFilterButtons").querySelector("button.selected").dataset.filter;
            const usersChart = $(usersChartElement).getKendoChart();
            usersChart.findSeriesByIndex(0).data(this.userData[filter]);
        }

        /**
         * Update/refresh the chart with data about up to three specific entities.
         */
        updateEntityUsageData() {
            const entityDataElement = document.getElementById("entityData");
            if (!entityDataElement) return;

            const filter = document.getElementById("entityDataTypeFilterButtons").querySelector("button.selected").dataset.filter;
            $(entityDataElement).find(".number-item").remove();

            this.entityData[filter].forEach((entity) => {
                const numberItem = $($("#entity-data").html());
                numberItem.find("ins").addClass(`icon-${entity.moduleIcon}`);
                numberItem.find("h3").text(kendo.format("{0:N0}", entity.totalItems));
                numberItem.find("span.entity-total-text").text(`${entity.displayName} items`);
                numberItem.on("click", (e) => {
                    e.preventDefault();
                    window.parent.postMessage({
                        action: "OpenModule",
                        actionData: {
                            moduleId: entity.moduleId
                        }
                    });
                });

                $(entityDataElement).find("div.btn-row").before(numberItem);
            });
        }

        /**
         * Updates the open task alerts chart and total.
         */
        updateOpenTaskAlertsChart() {
            const statusChartElement = document.getElementById("status-chart");
            if (!statusChartElement) return;

            const taskAlertsChart = $(statusChartElement).getKendoChart();
            taskAlertsChart.findSeriesByIndex(0).data(this.openTaskAlertsData);

            let totalOpenTaskAlerts = 0;
            this.openTaskAlertsData.forEach((i) => totalOpenTaskAlerts += i.value);
            document.getElementById("totalOpenTaskAlerts").innerText = totalOpenTaskAlerts.toString();
        }

        /**
         * Turns the data selector result into a human readable piece of HTML.
         */
        updateDataSelectorContent() {
            const contentDiv = document.getElementById("data-selector-result");
            if (!contentDiv) return;

            if (!this.dataSelectorResult || (Array.isArray(this.dataSelectorResult) && this.dataSelectorResult.length === 0) || Object.keys(this.dataSelectorResult).length === 0) {
                contentDiv.innerText = "Er is geen dataselector resultaat om te tonen.";
                return;
            }

            // Make sure it's an array (to not have to constantly check the type of the variable).
            const data = !Array.isArray(this.dataSelectorResult) ? [this.dataSelectorResult] : this.dataSelectorResult;

            // Empty the content div first.
            contentDiv.replaceChildren();

            if (data.length === 1) {
                const keys = Object.keys(data[0]);
                const html = document.createElement("div");
                const list = document.createElement("ul");

                if (keys.length === 1) {
                    const listItem = document.createElement("li");
                    listItem.innerText = data[0][keys[0]];
                    list.append(listItem);
                } else {
                    keys.forEach((key, index) => {
                        const listItem = document.createElement("li");
                        listItem.innerText = `${key}: ${data[0][key]}`;
                        list.append(listItem);
                    });
                }

                //html.append(list);
                contentDiv.append(list);
            } else {
                data.forEach((row, index) => {
                    const keys = Object.keys(row);
                    const html = document.createElement("div");
                    const header = document.createElement("strong");
                    const list = document.createElement("ul");

                    header.innerText = `Resultaat #${index + 1}`;

                    if (keys.length === 1) {
                        const listItem = document.createElement("li");
                        listItem.innerText = row[keys[0]];
                        list.append(listItem);
                    } else {
                        keys.forEach((key, index) => {
                            const listItem = document.createElement("li");
                            listItem.innerText = `${key}: ${row[key]}`;
                            list.append(listItem);
                        });
                    }

                    //html.append(header, list);
                    contentDiv.append(header, list);
                });
            }
        }

        /**
         * Update the service grid with information about the WTS services.
         */
        async updateServices() {
            if (!this.servicesGrid) return;

            const dataSource = await Wiser.api({
                url: `${this.settings.wiserApiRoot}dashboard/services`
            });

            this.servicesGrid.setDataSource(new kendo.data.DataSource({
                data: dataSource,
                schema: {
                    model: {
                        fields: {
                            lastRun: {from: "lastRun", type: "date"},
                            nextRun: {from: "nextRun", type: "date"},
                            runTime: {from: "runTime", type: "number"}
                        }
                    }
                }
            }));
        }

        /**
         * Set the visual state for each service for better user experience.
         * Including state colors and correct icons based on settings.
         * @param e The data bound event from Kendo.
         */
        setServiceState(e) {
            const columnIndex = $("#services-grid").find("[data-field=state]").index();

            const rows = e.sender.tbody.children();
            for (let i = 0; i < rows.length; i++) {
                const row = $(rows[i]);
                const dataItem = e.sender.dataItem(row);
                const state = dataItem.get("state");
                const cell = row.children().eq(columnIndex);

                const paused = dataItem.get("paused");
                if (paused) {
                    const pauseButton = rows[i].querySelector(".pause-button-icon");
                    pauseButton.classList.remove("icon-stopwatch-pauze");
                    pauseButton.classList.add("icon-stopwatch-start");
                }

                const extraRun = dataItem.get("extraRun");
                if (extraRun) {
                    const extraRunButton = rows[i].querySelector(".extra-run-button-icon");
                    extraRunButton.classList.remove("k-i-play");
                    extraRunButton.classList.add("k-i-stop");
                }

                const templateId = dataItem.get("templateId");
                // If template ID is -1 there is no template and ID 0 is a local file. In both cases hide the edit action.
                if (templateId <= 0) {
                    const editTemplateButton = rows[i].querySelector(".edit-template-button");
                    editTemplateButton.parentElement.classList.add("hidden");
                }

                switch (state) {
                    case "active":
                    case "success":
                    case "running":
                        cell.addClass("status success");
                        break;
                    case "warning":
                        cell.addClass("status warning");
                        break;
                    case "failed":
                    case "crashed":
                        cell.addClass("status failed");
                        break;
                    case "paused":
                    case "stopped":
                        cell.addClass("status paused");
                        break;
                }
            }
        }

        /**
         * Pause or unpause a service that is executed by the WTS.
         * @param e The click event.
         * @returns {Promise<void>}
         */
        async togglePauseService(e) {
            const serviceId = e.currentTarget.closest("tr").querySelector("td").innerText;
            const currentState = this.servicesGrid.dataItem(e.currentTarget.closest("tr")).paused;

            const result = await Wiser.api({
                url: `${this.settings.wiserApiRoot}dashboard/services/${serviceId}/pause/${!currentState}`,
                method: "PUT"
            });

            if (result === "WillPauseAfterRunFinished") {
                kendo.alert("De service is momenteel nog bezig. Zodra deze klaar is zal deze automatisch gepauzeerd worden.");
            }

            await this.updateServices();
        }

        /**
         * Mark or unmark a service for the WTS to run it an extra time outside of the normal run scheme of that service.
         * @param e The click event.
         * @returns {Promise<void>}
         */
        async toggleExtraRunService(e) {
            const serviceId = e.currentTarget.closest("tr").querySelector("td").innerText;
            const currentState = this.servicesGrid.dataItem(e.currentTarget.closest("tr")).extraRun;

            const result = await Wiser.api({
                url: `${this.settings.wiserApiRoot}dashboard/services/${serviceId}/extra-run/${!currentState}`,
                method: "PUT"
            });

            switch (result)
            {
                case "Marked":
                    kendo.alert("De service zal een extra keer worden uitgevoerd. De tijd waarop dit gebeurd is afhankelijk van de instellingen van de WTS waar deze service op wordt uitgevoerd.")
                    break;
                case "Unmarked":
                    kendo.alert("De service zal niet meer een extra keer worden uitgevoerd.");
                    break;
                case "ServiceRunning":
                    kendo.alert("De service wordt momenteel al uitgevoerd, de huidige status kan niet worden aangepast.")
                    return;
                case "WtsOffline":
                    kendo.alert("De service is momenteel niet beschikbaar op een instantie van de WTS en kan daardoor niet worden uitgevoerd.")
                    return;
            }

            await this.updateServices();
        }

        /**
         * Open the templates module with the template of the service.
         * @param e The click event.
         * @returns {Promise<void>}
         */
        async editServiceTemplate(e) {
            const templateId = this.servicesGrid.dataItem(e.currentTarget.closest("tr")).templateId;
            const newUrl = `/Modules/Templates?templateId=${templateId}`;

            if (!this.serviceTemplateWindow.options || !this.serviceTemplateWindow.options.content || this.serviceTemplateWindow.options.content.url !== newUrl) {
                this.serviceTemplateWindow.setOptions({
                    content: {
                        url: newUrl,
                        iframe: true
                    }
                });

                this.serviceTemplateWindow.refresh();
            }

            this.serviceTemplateWindow.title(`Template: ${templateId}`).open().maximize();
        }

        /**
         * Open a window with the logs written by the WTS for a service.
         * @param e The click event.
         * @returns {Promise<void>}
         */
        async openServiceLogs(e) {
            const columns = e.currentTarget.closest("tr").querySelectorAll("td");
            const serviceId = this.servicesGrid.dataItem(e.currentTarget.closest("tr")).id;

            this.serviceWindow.title(`${columns[1].innerText} - ${columns[2].innerText}`).open().maximize();
            this.serviceWindow.element.data("serviceId", serviceId);

            if (this.serviceLogsGrid) {
                this.serviceLogsGrid.dataSource.read();
                return;
            }

            this.serviceLogsGrid = $("#serviceLogGrid").kendoGrid({
                filterable: true,
                pageable: {
                    refresh: true
                },
                height: "100%",
                dataSource: {
                    transport: {
                        read: async (transportOptions) => {
                            try {
                                const dataSource = await Wiser.api({
                                    url: `${this.settings.wiserApiRoot}dashboard/services/${this.serviceWindow.element.data("serviceId")}/logs`
                                });

                                transportOptions.success(dataSource);
                            } catch(exception) {
                                transportOptions.error(exception);
                            }
                        }
                    },
                    pageSize: 100,
                    schema: {
                        model: {
                            fields: {
                                addedOn: {from: "addedOn", type: "date"}
                            }
                        }
                    }
                },
                columns: [
                    {
                        field: "id",
                        hidden: true
                    },
                    {
                        title: "Level",
                        field: "level",
                        width: "15%"
                    },
                    {
                        title: "Toegevoegd op",
                        field: "addedOn",
                        format: "{0:dd-MM-yyyy HH:mm:ss}",
                        width: "15%"
                    },
                    {
                        title: "Bericht",
                        field: "message",
                        attributes: {
                            "class": "folded-message"
                        }
                    },
                    {
                        title: "Omgeving",
                        field: "isTest",
                        width: "10%",
                        values: [
                            { text: "Test", value: "true" },
                            { text: "Live", value: "false" }
                        ]
                    }
                ],
                dataBound: this.onServiceLogsGridDataBound.bind(this)
            }).data("kendoGrid");
            this.serviceLogsGrid.scrollables[1].classList.add("fixed-table");
        }

        /**
         * Bind (un)fold event when logs are added to the grid.
         * @param event The data bound event from Kendo.
         */
        onServiceLogsGridDataBound(event) {
            event.sender.element.find(".folded-message").on("dblclick", clickEvent => {
                if(clickEvent.currentTarget.classList.contains("folded-message")) {
                    clickEvent.currentTarget.classList.remove("folded-message");
                } else {
                    clickEvent.currentTarget.classList.add("folded-message");
                }
            });
        }

        /**
         * Actions to perform when the period filter changes. The start and end date inputs will
         * be updated and, if possible, the data will be updated to reflect the change on the dates.
         */
        async onPeriodFilterChange(event) {
            const currentDate = new Date();
            const value = event.sender.value();
            let range = null;
            let readonly = true;
            let updateData = true;
            switch (value) {
                case "currentMonth":
                    range = {
                        start: new Date(currentDate.getFullYear(), currentDate.getMonth(), 1),
                        end: new Date(currentDate.getFullYear(), currentDate.getMonth() + 1, 1)
                    };
                    range.end.setDate(range.end.getDate() - 1);
                    break;
                case "lastMonth":
                    range = {
                        start: new Date(currentDate.getFullYear(), currentDate.getMonth() - 1, 1),
                        end: new Date(currentDate.getFullYear(), currentDate.getMonth(), 1)
                    };
                    range.end.setDate(range.end.getDate() - 1);
                    break;
                case "currentYear":
                    range = {
                        start: new Date(currentDate.getFullYear(), 0, 1),
                        end: new Date(currentDate.getFullYear() + 1, 0, 1)
                    };
                    range.end.setDate(range.end.getDate() - 1);
                    break;
                case "lastYear":
                    range = {
                        start: new Date(currentDate.getFullYear() - 1, 0, 1),
                        end: new Date(currentDate.getFullYear(), 0, 1)
                    };
                    range.end.setDate(range.end.getDate() - 1);
                    break;
                case "custom":
                    readonly = false;
                    updateData = false;
                    break;
            }

            const periodPicker = $("#periodPicker").getKendoDateRangePicker();
            if (range !== null) {
                periodPicker.range(range);
            }
            periodPicker.readonly(readonly);

            if (updateData) {
                window.processing.addProcess("dataUpdate");
                await this.updateData();
                window.processing.removeProcess("dataUpdate");
            }
        }

        /**
         * When a tile gets placed in a new location.
         */
        async onTileLayoutReorder(event) {
            const rowSpan = event.container.css("grid-column-end");
            const chart = event.container.find(".k-chart").data("kendoChart");
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
            kendo.resize(event.container, true);

            await this.saveUserSettings();
        }

        /**
         * When a tile gets resized.
         */
        async onTileLayoutResize() {
            await this.saveUserSettings();
        }

        /**
         * When a tile gets closed (the close button click).
         */
        async onTileClose(event) {
            const tileId = event.currentTarget.closest(".k-tilelayout-item").querySelector("[data-tile]").dataset.tile;
            document.getElementById("editSub").querySelector(`input[type='checkbox'][data-toggle-tile='${tileId}']`).checked = false;
            await this.removeTile(tileId);
        }

        /**
         * Saves the tile layout settings in the user's data.
         */
        async saveUserSettings() {
            // Saves the tile layout data.
            if (!this.tileLayout) return;

            const tilesInOrder = [...this.tileLayout.items].sort((a, b) => {
                if (a.order < b.order) return -1;
                if (a.order > b.order) return 1;
                return 0;
            });

            // Create a JSON object out of the data that needs to be saved.
            const data = tilesInOrder.map((tile) => {
                return {
                    tileId: this.tileLayout.element.find(`#${tile.id} [data-tile]`).data("tile"),
                    order: tile.order,
                    colSpan: tile.colSpan,
                    rowSpan: tile.rowSpan
                };
            });

            let saveSuccessful;
            try {
                saveSuccessful = await Wiser.api({
                    url: `${this.settings.wiserApiRoot}users/dashboard-settings`,
                    method: "POST",
                    contentType: "application/json",
                    dataType: "json",
                    data: JSON.stringify(data)
                });
            } catch (exception) {
                console.error(exception);
                saveSuccessful = false;
            }

            if (!saveSuccessful) {
                Wiser.alert({
                    title: "Opslaan mislukt",
                    content: "Het opslaan van het dashboard layout is mislukt. Probeer het a.u.b. nogmaals door de layout nogmaals aan te passen, of neem contact op met ons."
                });
            }
        }

        /**
         * Adds a tile to the tile layout and save the new layout to the user settings.
         */
        async addTile(tileId) {
            const defaultTileSettings = defaultLayoutSettings.find((tile) => tile.tileId === tileId);
            if (!defaultTileSettings) return;

            const tileTemplateSettings = Dashboard.GetTileTemplateSettingsByTileId(tileId);
            const item = {
                colSpan: defaultTileSettings.colSpan,
                rowSpan: defaultTileSettings.rowSpan,
                header: {
                    text: tileTemplateSettings.headerText
                },
                bodyTemplate: kendo.template($(`#${tileTemplateSettings.bodyTemplateId}`).html())
            };

            const items = this.tileLayout.items;
            items.push(item);
            this.tileLayout.setOptions({ containers: items });

            // Re-initialize Kendo widgets.
            this.initializeItemsDataChart();
            this.initializeUsersDataChart();
            this.initializeTaskAlertsDataChart();
            this.initializeServicesGrid();

            // Update Kendo widget data.
            this.updateItemsDataChart();
            this.updateUserDataChart();
            this.updateEntityUsageData();
            this.updateOpenTaskAlertsChart();

            this.updateDataSelectorContent();

            await Promise.all([
                this.updateServices(),
                this.saveUserSettings()
            ]);
        }

        /**
         * Removes a tile from the tile layout and save the new layout to the user settings.
         */
        async removeTile(tileId) {
            const itemId = this.tileLayout.element.find(`[data-tile='${tileId}']`).closest(".k-tilelayout-item").attr("id");
            const mainItems = this.tileLayout.items;
            const item = this.tileLayout.itemsMap[itemId];

            mainItems.splice(mainItems.indexOf(item), 1);

            for (let i = 0; i < mainItems.length; i++) {
                if (mainItems[i]) {
                    mainItems[i].order = i;
                }
            }

            this.tileLayout.setOptions({ containers: mainItems });

            // Re-initialize Kendo widgets.
            this.initializeItemsDataChart();
            this.initializeUsersDataChart();
            this.initializeTaskAlertsDataChart();
            this.initializeServicesGrid();

            // Update Kendo widget data.
            this.updateItemsDataChart();
            this.updateUserDataChart();
            this.updateEntityUsageData();
            this.updateOpenTaskAlertsChart();

            this.updateDataSelectorContent();

            await Promise.all([
                this.updateServices(),
                this.saveUserSettings()
            ]);
        }

        /**
         * Retrieves the header text and template ID for the body template for a specific tile.
         */
        static GetTileTemplateSettingsByTileId(tileId) {
            if (!tileId) return null;

            let headerText, bodyTemplateId;

            switch (tileId) {
                case "dataChart":
                    headerText = "Data";
                    bodyTemplateId = "data-chart-template";
                    break;
                case "usersChart":
                    headerText = "Gebruikers";
                    bodyTemplateId = "users-chart-template";
                    break;
                case "subscription":
                    headerText = "Abonnement";
                    bodyTemplateId = "subscriptions-chart-template";
                    break;
                case "updateLog":
                    headerText = "Update log";
                    bodyTemplateId = "update-log";
                    break;
                case "services":
                    headerText = "Services";
                    bodyTemplateId = "services-grid-template";
                    break;
                case "entityData":
                    headerText = "Entiteiten";
                    bodyTemplateId = "numbers";
                    break;
                case "taskAlerts":
                    headerText = "Agenderingen";
                    bodyTemplateId = "status-chart-template";
                    break;
                case "dataSelector":
                    headerText = "Dataselector";
                    bodyTemplateId = "dataselector-rate";
                    break;
                default:
                    throw new RangeError(`Tile iD "${tileId}" is not recognized as a valid tile ID!`);
            }

            return {
                headerText: headerText,
                bodyTemplateId: bodyTemplateId
            };
        }
    }

    window.dashboard = new Dashboard(moduleSettings);
})($, moduleSettings);
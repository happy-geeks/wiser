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

((jQuery, moduleSettings) => {
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
            tileId: "subscription",
            colSpan: 7,
            rowSpan: 2
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
            
            this.servicesGrid = null;
            this.serviceWindow = null;
            this.serviceLogsGrid = null;

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
                Wiser.alert({
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

            const userData = await Wiser.getLoggedInUserData(this.settings.wiserApiRoot);
            this.settings.userId = userData.encryptedId;
            this.settings.customerId = userData.encryptedCustomerId;
            this.settings.zeroEncrypted = userData.zeroEncrypted;
            this.settings.wiserUserId = userData.id;

            // Initialize the rest.
            this.initializeKendoElements();

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
                document.getElementById(`${tileSettings.tileId}Checkbox`).checked = true;

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

            // create Column Chart
            $("#data-chart").kendoChart({
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

            // create Pie Chart
            $("#users-chart").kendoChart({
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

            // create Pie Chart
            $("#status-chart").kendoChart({
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

            const servicesGridElement = document.getElementById("services-grid");
            if (servicesGridElement) {
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
                                }
                            ],
                            attributes: {
                                "class": "admin"
                            }
                        },
                        {
                            field: "paused",
                            hidden: true,
                            attributes: {
                                "class": "paused-state"
                            }
                        },
                        {
                            field: "extraRun",
                            hidden: true,
                            attributes: {
                                "class": "extra-run-state"
                            }
                        }
                    ],
                    dataBound: this.setServiceState
                }).data("kendoGrid");
                this.servicesGrid.scrollables[1].classList.add("fixed-table");
            }

            const serviceWindowOptions = {
                actions: ["Close"],
                visible: false
            }

            this.serviceWindow = $("#serviceLogWindow").kendoWindow(serviceWindowOptions).data("kendoWindow");
        }

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

            const loginTimeTotal = data.userLoginTimeTop10 + data.userLoginTimeOther;
            let loginTimeTop10Percentage = 0;
            let loginTimeOtherPercentage = 0;

            if (loginTimeTotal > 0) {
                loginTimeTop10Percentage = Math.round(((data.userLoginTimeTop10 / loginTimeTotal) * 100) * 10) / 10;
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
                loginTime: [
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
            this.updateOpenTaskAlertsChart(openTaskAlertsData);
        }

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
            dataChart.setDataSource(this.itemsData[filter]);
        }

        updateUserDataChart() {
            const usersChartElement = document.getElementById("users-chart");
            if (!usersChartElement) return;

            const filter = document.getElementById("userDataTypeFilterButtons").querySelector("button.selected").dataset.filter;
            const usersChart = $(usersChartElement).getKendoChart();
            usersChart.findSeriesByIndex(0).data(this.userData[filter]);
        }

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
         * @param {Array} data Array with the chart data. Will contain objects with a category property and a value property.
         */
        updateOpenTaskAlertsChart(data) {
            const statusChartElement = document.getElementById("status-chart");
            if (!statusChartElement) return;

            const taskAlertsChart = $(statusChartElement).getKendoChart();
            taskAlertsChart.findSeriesByIndex(0).data(data);

            let totalOpenTaskAlerts = 0;
            data.forEach((i) => totalOpenTaskAlerts += i.value);
            document.getElementById("totalOpenTaskAlerts").innerText = totalOpenTaskAlerts.toString();
        }

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
        
        setServiceState(e) {
            const columnIndex = this.wrapper.find("[data-field=state]").index();

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
        
        async togglePauseService(e) {
            const serviceId = e.currentTarget.closest("tr").querySelector("td").innerText;
            const currentState = e.currentTarget.closest("tr").querySelector(".paused-state").innerText === 'true';
            
            const result = await Wiser.api({
                url: `${this.settings.wiserApiRoot}dashboard/services/${serviceId}/pause/${!currentState}`,
                method: "PUT"
            });
            
            if(result === 'WillPauseAfterRunFinished') {
                kendo.alert("De service is momenteel nog bezig. Zodra deze klaar is zal deze automatisch gepauzeerd worden.");
            }
            
            await this.updateServices();
        }

        async toggleExtraRunService(e) {
            const serviceId = e.currentTarget.closest("tr").querySelector("td").innerText;
            const currentState = e.currentTarget.closest("tr").querySelector(".extra-run-state").innerText === 'true';

            const result = await Wiser.api({
                url: `${this.settings.wiserApiRoot}dashboard/services/${serviceId}/extra-run/${!currentState}`,
                method: "PUT"
            });
            
            switch (result)
            {
                case "Marked":
                    kendo.alert("De service zal een extra keer worden uitgevoerd. De tijd waarop dit gebeurd is afhankelijk van de instellingen van de AIS waar deze service op wordt uitgevoerd.")
                    break;
                case "Unmarked":
                    kendo.alert("De service zal niet meer een extra keer worden uitgevoerd.");
                    break;
                case "ServiceRunning":
                    kendo.alert("De service wordt momenteel al uitgevoerd, de huidige status kan niet worden aangepast.")
                    return;
                case "AisOffline":
                    kendo.alert("De service is momenteel niet beschikbaar op een instantie van de AIS en kan daardoor niet worden uitgevoerd.")
                    return;
            }

            await this.updateServices();
        }
        
        async openServiceLogs(e) {
            const columns = e.currentTarget.closest("tr").querySelectorAll("td");
            const serviceId = columns[0].innerText;
            
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

        onServiceLogsGridDataBound(event) {
            event.sender.element.find(".folded-message").on("dblclick", clickEvent => {
                if(clickEvent.currentTarget.classList.contains("folded-message")) {
                    clickEvent.currentTarget.classList.remove("folded-message");
                } else {
                    clickEvent.currentTarget.classList.add("folded-message");
                }
            });
        }
        
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
            console.log("event.currentTarget", event.currentTarget);
            await this.removeTile(event.currentTarget.closest(".k-tilelayout-item").querySelector("[data-tile]").dataset.tile);
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

            const saveResult = await Wiser.api({
                url: `${this.settings.wiserApiRoot}users/dashboard-settings`,
                method: "POST",
                contentType: "application/json",
                dataType: "json",
                data: JSON.stringify(data)
            });

            if (!saveResult) {
                Wiser.alert({
                    title: "Opslaan mislukt",
                    content: "Het opslaan van het dashboard layout is mislukt."
                });
            }
        }

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

            await this.saveUserSettings();
        }

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

            await this.saveUserSettings();
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
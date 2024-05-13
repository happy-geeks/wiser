import { TrackJS } from "trackjs";
import { Wiser } from "../../Base/Scripts/Utils.js";
require("@progress/kendo-ui/js/kendo.all.js");
require("@progress/kendo-ui/js/cultures/kendo.culture.nl-NL.js");
require("@progress/kendo-ui/js/messages/kendo.messages.nl-NL.js");

import "../Css/search.css";

// Any custom settings can be added here. They will overwrite most default settings inside the module.
const moduleSettings = {
    
};

((settings) => {
    /**
     * Main class.
     */
    class Search {

        /**
         * Initializes a new instance of Search.
         * @param {any} settings An object containing the settings for this class.
         */
        constructor(settings) {
            this.resultsGrid = null;
            this.searchTimeout = null;
            this.selectedEntityType = null;
            this.entityTypeComboBox = null;
            this.propertyComboBox = null;
            this.operatorComboBox = null;
            this.searchField = null;
            this.allEntityTypes = [];

            this.allOperators = [
                { value: "eq", text: "Gelijk aan" },
                { value: "lt", text: "Kleiner dan" },
                { value: "gt", text: "Groter dan" },
                { value: "startswith", text: "Begint met" },
                { value: "endswith", text: "Eindigt met" },
                { value: "contains", text: "Bevat" }
            ];
            this.nonPropertyOperators = [{ text: "Begint met", value: "startswith" }];

            // Set the Kendo culture to Dutch. TODO: Base this on the language in Wiser.
            kendo.culture("nl-NL");

            // Default settings
            this.settings = {
                tenantId: 0,
                username: "Onbekend"
            };
            Object.assign(this.settings, settings);
            
            // Add logged in user access token to default authorization headers for all jQuery ajax requests.
            $.ajaxSetup({
                headers: { "Authorization": `Bearer ${localStorage.getItem("accessToken")}` }
            });

            // Fire event on page ready for direct actions
            $(document).ready(() => {
                this.onPageReady();
            });
        }

        /**
         * Event that will be fired when the page is ready.
         */
        async onPageReady() {
            // Setup any settings from the body element data. These settings are added via the Wiser backend and they take preference.
            Object.assign(this.settings, $("body").data());

            if (this.settings.trackJsToken) {
                TrackJS.install({
                    token: this.settings.trackJsToken
                });
            }
            
            const user = JSON.parse(localStorage.getItem("userData"));
            this.settings.oldStyleUserId = user.oldStyleUserId;
            
            this.settings.serviceRoot = `${this.settings.wiserApiRoot}templates/get-and-execute-query`;
            
            const userData = await Wiser.getLoggedInUserData(this.settings.wiserApiRoot);
            this.settings.userId = userData.encryptedId;
            this.settings.tenantId = userData.encryptedTenantId;
            this.settings.zeroEncrypted = userData.zeroEncrypted;

            this.searchField = $("#search-field");
            this.searchField.focus();

            this.setupBindings();
            
            // Get list of all entity types, so we can show friendly names wherever we need to and don't have to get them from database via different places.
            try {
                this.allEntityTypes = (await Wiser.api({url: `${this.settings.wiserApiRoot}entity-types?onlyEntityTypesWithDisplayName=false`})) || [];
            } catch (exception) {
                console.error("Error occurred while trying to load all entity types", exception);
                this.allEntityTypes = [];
            }

            // Initialize search results grid.
            this.resultsGrid = $("#search-grid").kendoGrid({
                height: 650,
                pageable: {
                    pageSize: 20
                },
                resizable: true,
                sortable: true,
                filterable: {
                    mode: "row"
                },
                columns: [
                    {
                        template: "<div class='grid-icon #:icon# icon-bg-#:color#'></div>",
                        field: "icon",
                        filterable: false,
                        title: "&nbsp;",
                        width: 70
                    },
                    {
                        template: (dataItem) => {
                            const entityType = this.allEntityTypes.find(x => x.id === (dataItem.entityType || dataItem.entitytype)) || {};
                            return `<strong>${dataItem.title}</strong><br><small>${entityType.displayName || dataItem.entityType || dataItem.entitytype}</small>`;
                        },
                        field: "title",
                        title: "Titel",
                        filterable: {
                            cell: {
                                operator: "contains"
                            }
                        }
                    },
                    {
                        field: "addedOn",
                        title: "Aangemaakt op",
                        type: "date",
                        format: "{0:dd MMMM yyyy}"
                    },
                    {
                        field: "addedBy",
                        title: "Aangemaakt door",
                        filterable: {
                            cell: {
                                operator: "contains"
                            }
                        }
                    },
                    {
                        field: "moreInfo",
                        title: "Overige info",
                        filterable: {
                            cell: {
                                operator: "contains"
                            }
                        }
                    },
                    {
                        title: "&nbsp;",
                        width: 80,
                        command: [{
                            name: "openDetails",
                            iconClass: "k-icon k-i-hyperlink-open",
                            text: "",
                            click: this.onShowDetailsClick.bind(this)
                        }]
                    }
                ],
                dataBound: this.onResultsGridDataBound.bind(this),
                noRecords: {
                    template: "Er zijn geen resultaten gevonden. Probeer andere zoektermen."
                },
            }).data("kendoGrid");

            this.resultsGrid.element.on("dblclick", "tbody tr[data-uid] td", this.onShowDetailsClick.bind(this));

            this.entityTypeComboBox = $("#entityTypeComboBox").kendoComboBox({
                dataTextField: "displayName",
                dataValueField: "id",
                dataSource: this.allEntityTypes,
                autoWidth: true,
                filter: "contains",
                suggest: true,
                placeholder: "Waar wil je op zoeken?",
                change: this.onComboBoxChange.bind(this)
            }).data("kendoComboBox");

            this.propertyComboBox = $("#propertyComboBox").kendoComboBox({
                dataTextField: "displayName",
                dataValueField: "propertyName",
                autoWidth: true,
                filter: "contains",
                suggest: true,
                placeholder: "Kies een eigenschap...",
                change: this.onComboBoxChange.bind(this)
            }).data("kendoComboBox");

            this.operatorComboBox = $("#operatorComboBox").kendoComboBox({
                dataTextField: "text",
                dataValueField: "value",
                dataSource: this.nonPropertyOperators,
                autoWidth: true,
                filter: "contains",
                suggest: true,
                value: "startswith",
                change: this.onComboBoxChange.bind(this)
            }).data("kendoComboBox");

            this.includeDeletedItemsCheckBox = $("#includingDeletedItems").change(this.onCheckBoxChange.bind(this));
        }

        /**
         * Setup all basis bindings for this module.
         * Specific bindings (for buttons in certain pop-ups for example) will be set when they are needed.
         */
        setupBindings() {
            document.addEventListener("moduleClosing", (event) => {
                // You can do anything here that needs to happen before closing the module.
                event.detail();
            });

            // Search field.
            this.searchField.on("keyup", this.onSearchFieldKeyUp.bind(this));
            $(".icon-line-search").click(this.onSearchIconClick.bind(this));
			
			// Settings toggle
			$("#search-settings > li > ins").on("click", function () {
                $("#search-settings").toggleClass("hover");
            });
        }

        /**
         * Opens a dynamic item in a kendo window, which will load the dynamic items module inside an iframe.
         * @param {string} encryptedItemId The encrypted ID of the item to open.
         * @param {number} moduleId The ID of the module.
         * @param {string} entityType The name of the entity type of the item to open.
         */
        openDynamicItem(encryptedItemId, moduleId, entityType) {
            $("#dynamicItemWindow").kendoWindow({
                content: `${"/Modules/DynamicItems"}?itemId=${encryptedItemId}&moduleId=${moduleId || 0}&iframe=true&entityType=${encodeURIComponent(entityType)}`,
                iframe: true,
                width: "90%",
                height: "90%",
                title: "",
                modal: true,
                actions: ["Close"]
            }).data("kendoWindow").center().open();
        }

        /**
         * Get all properties of an entity.
         * @param {string} entityType The name of the entity type.
         * @returns {Promise} A promise that will return the search results.
         */
        async getPropertiesOfEntity(entityType) {
            return Wiser.api({ url: `${this.settings.serviceRoot}/GET_PROPERTIES_OF_ENTITY?entityType=${entityType}` });
        }

        /**
         * Call this function when the user manually starts a search by pressing enter or clicking the search button.
         */
        startSearchManually() {    
            const value = this.searchField.val() || "";

            if (value.length < (this.selectedPropertyIsId() ? 1 : 3)) {
                this.searchField.addClass("error");
                kendo.alert(`Vul a.u.b. minimaal ${(this.selectedPropertyIsId() ? "1 karakter" : "3 karakters")} in om te zoeken.`);
                return;
            }

            this.startSearch(value, this.entityTypeComboBox.value(), this.propertyComboBox.value(), this.operatorComboBox.value(), this.includeDeletedItemsCheckBox.prop("checked"));
        }

        /**
         * Call this function when the search input value gets changed, to start a new search.
         * @param {string} value The value to search for.
         * @param {string} entityType Optional: The entity type to search for.
         * @param {string} propertyName Optional: The property to search for.
         * @param {string} operator Optional: The type of filtering to to (eq, neq, startswith etc, see https://docs.telerik.com/kendo-ui/api/javascript/data/datasource/configuration/filter#filteroperator).
         * @param {boolean} includingDeletedItems Optional: Whether or not to include deleted items in the search.
         */
        async startSearch(value, entityType = "", propertyName = "", operator = "startswith", includingDeletedItems = false) {
            const container = $(".search-container");

            try {
                if (value.length < (this.selectedPropertyIsId() ? 1 : 3)) {
                    container.removeClass("search-top loading");
                    return;
                }
                
                this.searchField.removeClass("error");
                container.addClass("loading");
                
                const gridOptions = {
                    filter: {
                        logic: "and",
                        filters: [
                            !propertyName ? { field: "search", value: value, operator: operator || "startswith" } : { field: propertyName, value: value, operator: operator || "startswith" }
                        ]
                    }
                };

                if (!includingDeletedItems) {
                    gridOptions.filter.filters.push({
                        field: "removed",
                        operator: "eq",
                        value: "0"
                    });
                }

                const searchResults = await Wiser.api({
                    url: `${this.settings.wiserApiRoot}items/${encodeURIComponent(this.settings.zeroEncrypted)}/entity-grids/${encodeURIComponent(entityType || "all")}?mode=5`,
                    method: "POST",
                    contentType: "application/json",
                    data: JSON.stringify(gridOptions)
                });

                container.addClass("search-top");
                $("#resultsCount").html(searchResults.totalResults);
                this.resultsGrid.setDataSource(searchResults.data);
            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan tijdens het zoeken. Probeer het a.u.b. nogmaals of neem contact op met ons.");
                container.removeClass("loading");
            }
        }

        selectedPropertyIsId() {
            return ((this.propertyComboBox.dataItem() || {}).propertyName || "").toLowerCase() === "id";
        }

        /**
         * For when the user (un)checks a checkbox option.
         * @param {any} event The change event.
         */
        onCheckBoxChange(event) {
            this.startSearch(this.searchField.val(), this.entityTypeComboBox.value(), this.propertyComboBox.value(), this.operatorComboBox.value(), this.includeDeletedItemsCheckBox.prop("checked"));
        }

        /**
         * For when the user presses a button in the search field.
         * This will start the search action, if it is the enter button that the user pressed.
         * @param {any} event The keyup event.
         */
        onSearchFieldKeyUp(event) {
            if (event.key && event.key.toLowerCase() !== "enter") {
                return;
            }
            
            this.startSearchManually();
        }
        
        /**
         * For when the user clicks the search icon in the search field. This will start the search action.
         * @param {any} event The click event.
         */
        onSearchIconClick(event) {
            this.startSearchManually();
        }

        /**
         * For when the user changes the value of a combo box.
         * @param {any} event The change event.
         */
        async onComboBoxChange(event) {
            const entityType = this.entityTypeComboBox.value();

            if (!entityType) {
                this.propertyComboBox.setDataSource([]);
                this.propertyComboBox.value("");
            } else if (entityType !== this.selectedEntityType) {
                this.selectedEntityType = entityType;

                const arrow = this.propertyComboBox.wrapper.find(".k-i-arrow-60-down").addClass("k-i-loading");

                const properties = await this.getPropertiesOfEntity(entityType);
                this.propertyComboBox.setDataSource({
                    group: {
                        field: "tabName"
                    },
                    data: properties
                });

                this.propertyComboBox.value("title");

                arrow.removeClass("k-i-loading");
            }

            const selectedProperty = this.propertyComboBox.dataItem();
            const operator = this.operatorComboBox.value();

            if (!selectedProperty || !selectedProperty.propertyName) {
                this.operatorComboBox.setDataSource(this.nonPropertyOperators);
                this.operatorComboBox.value(this.nonPropertyOperators[0].value);
            } else {
                this.operatorComboBox.setDataSource(this.allOperators);
            }

            this.searchField.focus();
            const value = this.searchField.val();
            this.startSearch(value, entityType, selectedProperty.propertyName, operator, this.includeDeletedItemsCheckBox.prop("checked"));
        }

        /**
         * For when the data source has been fully loaded into the main search grid.
         * @param {any} event The dataBound event.
         */
        onResultsGridDataBound(event) {
            $(".search-container").removeClass("loading");
        }

        /**
         * Event that gets called when the user opens an item from the search results.
         * @param {any} event The click event.
         */
        onShowDetailsClick(event) {
            const dataItem = this.resultsGrid.dataItem($(event.currentTarget).closest("tr"));
            this.openDynamicItem(dataItem.encryptedId || dataItem.encrypted_id || dataItem.encryptedid, dataItem.moduleId || dataItem.moduleid || dataItem.moduleId, dataItem.entityType || dataItem.entitytype);
        }
    }

    // Initialize the Search class and make one instance of it globally available.
    window.search = new Search(settings);
})(moduleSettings);
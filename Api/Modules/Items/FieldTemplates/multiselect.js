(function() {
var options = $.extend({
	autoClose: false,
	dataTextField: "name",
	dataValueField: "id",
	change: window.dynamicItems.fields.onFieldValueChange.bind(window.dynamicItems.fields),
	dataSource: {
		transport: {
			read: {
				dataType: "json",
				url: window.dynamicItems.settings.serviceRoot + "/GET_DATA_FROM_ENTITY_QUERY?propertyid={propertyId}&myItemId={itemId}",
			}
		}
	}
}, {options});

var defaultValue = {default_value};
if (defaultValue) {
    options.value = typeof defaultValue === "string" ? defaultValue.split(",") : defaultValue;
}

if (typeof options.dataSource === "string") {
    switch (options.dataSource.toLowerCase()) {
        case "wiserusers":
            options.dataSource = {
                transport: {
                    read: {
                        dataType: "json",
                        url: window.dynamicItems.settings.wiserApiRoot + "users"
                    }
                }
            }
            break;
        default:
            kendo.alert("Onbekende datasource (' " + options.dataSource + "') opgegeven bij combobox-veld ('{title}'). Neem a.u.b. contact op met ons.");
            break;
    }
} else if (options.entityType) {
    var searchEverywhere = options.searchEverywhere && (options.searchEverywhere > 0 || options.searchEverywhere.toLowerCase() === "true") ? 1 : 0;
    var searchFields = options.searchFields || [];
    var searchInTitle = typeof options.searchInTitle === "undefined" || options.searchInTitle === null || options.searchInTitle === true || options.searchInTitle === "true" || options.searchInTitle > 0 ? 1 : 0;
	var searchModuleId = options.moduleId || 0;
	if (!searchEverywhere && !searchModuleId) {
		searchModuleId = window.dynamicItems.settings.moduleId;
	}
	options.dataSource.transport.read.url = window.dynamicItems.settings.serviceRoot + "/SEARCH_ITEMS?id=" + encodeURIComponent("{itemIdEncrypted}") + "&moduleid=" + searchModuleId.toString() +
                    "&entityType=" + encodeURIComponent(options.entityType) + "&search=&searchInTitle=" + searchInTitle.toString() +
                    "&searchFields=" + encodeURIComponent(searchFields.join()) + "&searchEverywhere=" + searchEverywhere +
                    "&skip=0&take=999999";
	options.filter = "contains";
	options.filtering = function(event) { window.dynamicItems.fields.onComboBoxFiltering(event, '{itemIdEncrypted}', options); };
} else if (options.dataSelectorId > 0) {
    options.dataSource.transport.read.url = window.dynamicItems.settings.getItemsUrl + "?trace=false&encryptedDataSelectorId=" + encodeURIComponent(options.dataSelectorId.toString())
}

var field = $("#field_{propertyIdWithSuffix}");
var kendoComponent = field.kendoMultiSelect(options).data("kendoMultiSelect");
var readonly = {readonly};
kendoComponent.readonly(readonly);
{customScript}
})();
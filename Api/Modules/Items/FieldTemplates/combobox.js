(function() {
var container = $("#container_{propertyIdWithSuffix}");
var field = $("#field_{propertyIdWithSuffix}");
var fieldOptions = {options};
var options = $.extend({
    optionLabel: "Kies een waarde...",
	autoClose: false,
	dataTextField: "name",
	dataValueField: "id",
    minLength: 0,
	change: function(event) { window.dynamicItems.fields.onDropDownChange(event, options); },
	dataSource: {
		transport: {
			read: function(kendoOptions) {
                console.log("read start - {title}");
                
                var inputData = window.dynamicItems.fields.getInputData(field.closest(".popup-container, .pane-content")) || [];
                inputData = inputData.reduce((obj, item) => { obj[item.key] = item.value; return obj; });
                
                Wiser2.api({
                    method: "POST",
                    contentType: "application/json",
                    dataType: "json",
                    url: dynamicItems.settings.wiserApiRoot + "items/" + encodeURIComponent("{itemIdEncrypted}") + "/action-button/{propertyId}?queryId=" + encodeURIComponent(fieldOptions.queryId || dynamicItems.settings.zeroEncrypted) + "&itemLinkId={itemLinkId}&userType=" + encodeURIComponent(dynamicItems.settings.userType),
                    data: JSON.stringify(inputData)
                }).then(function (dataResult) {
                    console.log("read success - {title}", dataResult.otherData);
                    kendoOptions.success(dataResult.otherData);
                }).catch(function(jqXHR, textStatus, errorThrown) {
                    console.log("read error - {title}", jqXHR, textStatus, errorThrown);
                    kendoOptions.error(jqXHR, textStatus, errorThrown);
                });
            }
		}
	}
}, fieldOptions);

var defaultValue = {default_value};
if (defaultValue) {
    options.value = defaultValue;
}

if (typeof options.dataSource === "string") {
    switch (options.dataSource.toLowerCase()) {
        case "wiserusers":
            var userTypesString = "&userTypes=";
            if (options.userTypes) {
                if (typeof options.userTypes === "string") {
                    userTypesString += options.userTypes;
                } else {
                    userTypesString += options.userTypes.join();
                }
            }
            
            options.dataSource = {
                transport: {
                    read: {
                        dataType: "json",
                        url: window.dynamicItems.settings.wiserApiRoot + "users"
                    }
                }
            }

            options.dataTextField = "title";
            options.dataValueField = "id";

            break;
        default:
            kendo.alert("Onbekende datasource ('" + options.dataSource + "') opgegeven bij combobox-veld ('{title}'). Neem a.u.b. contact op met ons.");
            break;
    }
} else if (options.entityType) {
    var searchEverywhere = options.searchEverywhere && (options.searchEverywhere > 0 || options.searchEverywhere.toLowerCase() === "true") ? 1 : 0;
    var searchFields = options.searchFields || [];
    var searchInTitle = typeof options.searchInTitle === "undefined" || options.searchInTitle === null || options.searchInTitle === true || options.searchInTitle === "true" || options.searchInTitle > 0 ? 1 : 0;
	var searchModuleId = options.moduleId || 0;
	if (!searchEverywhere && !searchModuleId) {
		searchModuleId = window.dynamicItems.settings.moduleId || 0;
	}
    
    options.dataSource.transport.read = (kendoReadOptions) => {
        var searchAddition = "";

        if (kendoReadOptions.data && kendoReadOptions.data.filter && kendoReadOptions.data.filter.filters && kendoReadOptions.data.filter.filters.length){
            searchAddition = "&search=" + encodeURIComponent(kendoReadOptions.data.filter.filters[0].value);
        }
        else if (options.minLength <= 0) {
            searchAddition = "&search=";
        }

        Wiser2.api({
            url: window.dynamicItems.settings.serviceRoot + "/SEARCH_ITEMS?id=" + encodeURIComponent("{itemIdEncrypted}") + "&moduleid=" + searchModuleId.toString() +
                "&entityType=" + encodeURIComponent(options.entityType) + "&searchInTitle=" + encodeURIComponent(searchInTitle.toString()) +
                "&searchFields=" + encodeURIComponent(searchFields.join()) + "&searchEverywhere=" + searchEverywhere +
                "&skip=0&take=999999" + searchAddition,
            dataType: "json",
            method: "GET",
            data: kendoReadOptions.data
        }).then((result) => {
            kendoReadOptions.success(result);
        }).catch((result) => {
            kendoReadOptions.error(result);
        });
    };
	
    options.dataSource.pageSize = 80;
    options.dataSource.serverPaging = true;
    options.dataSource.serverFiltering = true;
                    
	options.filter = "contains";
    
    // TODO: Finish virtual scrolling.
    /*options.virtual = {
        valueMapper: function(options) {
            console.log("dropdown {title} valueMapper", options);
            $.ajax({
                url: window.dynamicItems.settings.serviceRoot,
                type: "GET",
                dataType: "jsonp",
                data: {
                    templatename: "SEARCH_ITEMS_VALUE_MAPPER",
                    trace: false,
                    id: "{itemIdEncrypted}",
                    moduleid: searchModuleId,
                    entityType: options.entityType,
                    search: "",
                    searchInTitle: searchInTitle,
                    searchFields: searchFields.join(),
                    searchEverywhere: searchEverywhere
                }
            }).done(function(results) {
                options.success(results);
            });
        }
    }*/
	//options.filtering = function(event) { window.dynamicItems.fields.onComboBoxFiltering(event, '{itemIdEncrypted}', options); };
} else if (options.dataSelectorId) {
    options.dataSource.transport.read = (kendoReadOptions) => {
        Wiser2.api({
            url: window.dynamicItems.settings.getItemsUrl + "?trace=false&encryptedDataSelectorId=" + encodeURIComponent(options.dataSelectorId),
            dataType: "json",
            method: "GET",
            data: kendoReadOptions.data
        }).then((result) => {
            kendoReadOptions.success(result);
        }).catch((result) => {
            kendoReadOptions.error(result);
        });
    }
}

if (options.cascadeFrom && typeof options.cascadeFrom === "string") {
    options.cascadeFrom = "field_" + options.cascadeFrom + "{propertyIdSuffix}";
    options.autoBind = false;
    options.dataSource.serverFiltering = true;
}

var kendoComponent = options.useDropDownList || options.mode === "dropDownList" ? field.kendoDropDownList(options).data("kendoDropDownList") : field.kendoComboBox(options).data("kendoComboBox");
var readonly = {readonly};
kendoComponent.readonly(readonly);

if (options.newItems && options.newItems.allow && (options.newItems.entityType || options.entityType)) {
    container.find(".newItemButton").removeClass("hidden").kendoButton({
        icon: "plus",
        click: window.dynamicItems.dialogs.openCreateItemDialog.bind(window.dynamicItems.dialogs, options.newItems.parentId, null, options.newItems.entityType || options.entityType, false, true, options.newItems.linkTypeNumber, options.newItems.moduleId, kendoComponent)
    });
}
if (options.allowOpeningOfSelectedItem) {
    container.find(".openItemButton").toggleClass("hidden", !kendoComponent.value()).kendoButton({
        icon: "hyperlink-open",
        click: function(event) {
            var dataItem = kendoComponent.dataItem();

            // If the current item is in an information block iframe, open it in the parent.
            var windowToUse = window;
            if (window.parent && window.parent.dynamicItems && window.parent.dynamicItems.settings.gridViewSettings && window.parent.dynamicItems.settings.gridViewSettings.informationBlock) {
                windowToUse = windowToUse.parent;
            }
            windowToUse.dynamicItems.windows.loadItemInWindow(false, kendoComponent.value(), dataItem.encrypted_id || dataItem.encryptedId || dataItem.encryptedid, dataItem.entityType || dataItem.entity_type || dataItem.entitytype, kendoComponent.text(), false, null, options, 0, null, kendoComponent);
        }
    });
}

{customScript}
})();
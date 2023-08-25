(function() {
var options = $.extend({
	autoClose: false,
	dataTextField: "name",
	dataValueField: "id",
	change: window.dynamicItems.fields.onFieldValueChange.bind(window.dynamicItems.fields),
	dataSource: {
		transport: {
            read: (kendoReadOptions) => {
                Wiser.api({
                    url: window.dynamicItems.settings.serviceRoot + "/GET_DATA_FROM_ENTITY_QUERY?propertyid={propertyId}&myItemId={itemId}",
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
                    read: (kendoReadOptions) => {
                        Wiser.api({
                            url: window.dynamicItems.settings.wiserApiRoot + "users",
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
    options.dataSource.transport.read = (kendoReadOptions) => {
        Wiser.api({
            url: window.dynamicItems.settings.serviceRoot + "/SEARCH_ITEMS?id=" + encodeURIComponent("{itemIdEncrypted}") + "&moduleid=" + searchModuleId.toString() +
                "&entityType=" + encodeURIComponent(options.entityType) + "&search=&searchInTitle=" + searchInTitle.toString() +
                "&searchFields=" + encodeURIComponent(searchFields.join()) + "&searchEverywhere=" + searchEverywhere +
                "&skip=0&take=999999",
            dataType: "json",
            method: "GET",
            data: kendoReadOptions.data
        }).then((result) => {
            kendoReadOptions.success(result);
        }).catch((result) => {
            kendoReadOptions.error(result);
        });
    };
	options.filter = "contains";
	options.filtering = function(event) { window.dynamicItems.fields.onComboBoxFiltering(event, '{itemIdEncrypted}', options); };
} else if (options.dataSelectorId > 0) {
    options.dataSource.transport.read = (kendoReadOptions) => {
        Wiser.api({
            url: window.dynamicItems.settings.getItemsUrl + "?trace=false&encryptedDataSelectorId=" + encodeURIComponent(options.dataSelectorId.toString()),
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

const field = $("#field_{propertyIdWithSuffix}");
const container = field.closest(".item");
const readonly = {readonly};

if (options.newItems && options.newItems.allow && (options.newItems.entityType || options.entityType)) {
    container.find(".newItemButton").removeClass("hidden").kendoButton({
        icon: "plus",
        click: window.dynamicItems.dialogs.openCreateItemDialog.bind(window.dynamicItems.dialogs, options.newItems.parentId, null, options.newItems.entityType || options.entityType, false, true, options.newItems.linkTypeNumber, options.newItems.moduleId, kendoComponent)
    });
}

if (options.mode === "checkBoxGroup") {
    // Set the main image.
    let mainImageUrl = options.mainImageUrl;
    if (options.mainImageId) {
        mainImageUrl = `${window.dynamicItems.settings.wiserApiRoot}items/0/files/${options.imageId}/${encodeURIComponent("{propertyName}.png")}?encryptedCustomerId=${encodeURIComponent(window.dynamicItems.settings.customerId)}&encryptedUserId=${encodeURIComponent(window.dynamicItems.settings.userId)}&isTest=${window.dynamicItems.settings.isTestEnvironment}&subDomain=${encodeURIComponent(window.dynamicItems.settings.subDomain)}&entityType=${encodeURIComponent("{entityType}")}&linkType={linkType}`;
    } 
    if (mainImageUrl) {
        container.find("#image_{propertyIdWithSuffix}").attr("src", mainImageUrl);
    }
    else {
        container.find("#image_{propertyIdWithSuffix}").hide();
    }

    const panel = container.find(".checkbox-full-panel");
    const dataSource = new kendo.data.DataSource(options.dataSource);
    const template = kendo.template($("#advancedCheckBoxGroupItemTemplate").html());

    dataSource.fetch().then(() => {
        const data = dataSource.data();

        for (let item of data) {
            const html = $(template(item));

            let imageUrl = item.imageUrl;
            if (item.imageId) {
                imageUrl = `${window.dynamicItems.settings.wiserApiRoot}items/0/files/${item.imageId}/${encodeURIComponent(item.name || item.imageId)}.png?encryptedCustomerId=${encodeURIComponent(window.dynamicItems.settings.customerId)}&encryptedUserId=${encodeURIComponent(window.dynamicItems.settings.userId)}&isTest=${window.dynamicItems.settings.isTestEnvironment}&subDomain=${encodeURIComponent(window.dynamicItems.settings.subDomain)}&entityType=${encodeURIComponent("{entityType}")}&linkType={linkType}`;
            } else if (item.id && (item.imagePropertyName || options.imagePropertyName)) {
                imageUrl = `${window.dynamicItems.settings.mainDomain}image/wiser2/${item.id}/${item.imagePropertyName || options.imagePropertyName}/crop/66/66/${encodeURIComponent(item.name || item.imageId)}.png`;
            }

            if (imageUrl) {
                html.find(".checkbox-img img").attr("src", imageUrl);
            } else {
                html.find(".checkbox-img img").hide();
            }
            
            html.find("input[type='checkbox']").prop("readonly", readonly).prop("checked", options.value.indexOf(item.id.toString()) > -1);

            panel.append(html);
        }

        field.prop("checked", panel.find("input[type='checkbox']:checked").length > 0);
    });
} else {
    var kendoComponent = field.kendoMultiSelect(options).data("kendoMultiSelect");
    kendoComponent.readonly(readonly);
}
{customScript}
})();
(function() {
const container = $("#container_{propertyIdWithSuffix}");
const field = $("#field_{propertyIdWithSuffix}");
const fieldOptions = {options};
const options = $.extend({
    autoClose: false,
    dataTextField: "name",
    dataValueField: "id",
    change: window.dynamicItems.fields.onFieldValueChange.bind(window.dynamicItems.fields),
    dataSource: {
        transport: {
            read: async (kendoReadOptions) => {
                let inputData = window.dynamicItems.fields.getInputData(field.closest(".popup-container, .pane-content")) || [];
                inputData = inputData.reduce((obj, item) => { obj[item.key] = item.value; return obj; });

                try {
                    const dataResult = await Wiser.api({
                        method: "POST",
                        contentType: "application/json",
                        dataType: "json",
                        url: `${dynamicItems.settings.wiserApiRoot}items/${encodeURIComponent("{itemIdEncrypted}")}/action-button/{propertyId}?queryId=${encodeURIComponent(fieldOptions.queryId || dynamicItems.settings.zeroEncrypted)}&itemLinkId={itemLinkId}&userType=${encodeURIComponent(dynamicItems.settings.userType)}`,
                        data: JSON.stringify(inputData)
                    });

                    kendoReadOptions.success(dataResult.otherData);
                } catch (exception) {
                    console.error("read error - {title}", exception);
                    kendoReadOptions.error(exception);
                }
            }
        }
    }
}, fieldOptions);
let kendoComponent;

const defaultValue = {default_value};
if (defaultValue) {
    options.value = typeof defaultValue === "string" ? defaultValue.split(",") : defaultValue;
}

if (typeof options.dataSource === "string") {
    switch (options.dataSource.toLowerCase()) {
        case "wiserusers":
            options.dataSource = {
                transport: {
                    read: async (kendoReadOptions) => {
                        try {
                            const result = await Wiser.api({
                                url: `${window.dynamicItems.settings.wiserApiRoot}users`,
                                dataType: "json",
                                method: "GET",
                                data: kendoReadOptions.data
                            });

                            kendoReadOptions.success(result);
                        } catch (exception) {
                            console.error("read error - {title}", exception);
                            kendoReadOptions.error(exception);
                        }
                    }
                }
            }
            break;
        default:
            kendo.alert(`Onbekende datasource (' ${options.dataSource}') opgegeven bij combobox-veld ('{title}'). Neem a.u.b. contact op met ons.`);
            break;
    }
} else if (options.entityType) {
    const searchEverywhere = options.searchEverywhere && (options.searchEverywhere > 0 || options.searchEverywhere.toLowerCase() === "true");
    const searchFields = options.searchFields || [];
    const searchInTitle = typeof options.searchInTitle === "undefined" || options.searchInTitle === null || options.searchInTitle === true || options.searchInTitle === "true" || options.searchInTitle > 0;

    options.dataSource.transport.read = async (kendoReadOptions) => {
        try {
            let searchValue = "";

            if (kendoReadOptions.data && kendoReadOptions.data.filter && kendoReadOptions.data.filter.filters && kendoReadOptions.data.filter.filters.length) {
                searchValue = encodeURIComponent(kendoReadOptions.data.filter.filters[0].value);
            }

            const result = await Wiser.api({
                url: `${dynamicItems.settings.wiserApiRoot}items/{itemId}/search?entityType=${encodeURIComponent(options.entityType)}&searchValue=${searchValue}&searchInTitle=${searchInTitle}&searchEveryWhere=${searchEverywhere}&searchFields=${encodeURIComponent(searchFields.join())}`,
                dataType: "json",
                method: "GET",
                data: kendoReadOptions.data
            });

            kendoReadOptions.success(result);
        } catch (exception) {
            console.error("read error - {title}", exception);
            kendoReadOptions.error(exception);
        }
    };

    options.dataSource.pageSize = 80;
    options.dataSource.serverPaging = true;
    options.dataSource.serverFiltering = true;
    options.filter = "contains";
} else if (options.dataSelectorId > 0) {
    options.dataSource.transport.read = async (kendoReadOptions) => {
        try {
            const result = await Wiser.api({
                url: `${window.dynamicItems.settings.getItemsUrl}?trace=false&encryptedDataSelectorId=${encodeURIComponent(options.dataSelectorId.toString())}`,
                dataType: "json",
                method: "GET",
                data: kendoReadOptions.data
            });

            kendoReadOptions.success(result);
        } catch (exception) {
            console.error("read error - {title}", exception);
            kendoReadOptions.error(exception);
        }
    }
}

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
    kendoComponent = field.kendoMultiSelect(options).data("kendoMultiSelect");
    kendoComponent.readonly(readonly);
}

{customScript}
})();
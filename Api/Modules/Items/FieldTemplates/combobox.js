(function() {
const container = $("#container_{propertyIdWithSuffix}");
const field = $("#field_{propertyIdWithSuffix}");
const fieldOptions = {options};
const options = $.extend({
    optionLabel: "Kies een waarde...",
    autoClose: false,
    dataTextField: "name",
    dataValueField: "id",
    minLength: 0,
    change: (event) => { window.dynamicItems.fields.onDropDownChange(event, options); },
    dataSource: {
        transport: {
            read: async(kendoOptions) => {
                try {
                let inputData = window.dynamicItems.fields.getInputData(field.closest(".popup-container, .pane-content")) || [];
                inputData = inputData.reduce((obj, item) => { obj[item.key] = item.value; return obj; });

                    const dataResult = await Wiser.api({
                        method: "POST",
                        contentType: "application/json",
                        dataType: "json",
                        url: `${dynamicItems.settings.wiserApiRoot}items/${encodeURIComponent("{itemIdEncrypted}")}/action-button/{propertyId}?queryId=${encodeURIComponent(fieldOptions.queryId || dynamicItems.settings.zeroEncrypted)}&itemLinkId={itemLinkId}&userType=${encodeURIComponent(dynamicItems.settings.userType)}`,
                        data: JSON.stringify(inputData)
                    });

                    kendoOptions.success(dataResult.otherData);
                } catch (exception) {
                    console.error("read error - {title}", exception);
                    kendoReadOptions.error(exception);
                }
            }
        }
    }
}, fieldOptions);

const defaultValue = {default_value};
if (defaultValue) {
    options.value = defaultValue;
}

if (typeof options.dataSource === "string") {
    switch (options.dataSource.toLowerCase()) {
        case "wiserusers":
            let userTypesString = "&userTypes=";
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
                        url: `${window.dynamicItems.settings.wiserApiRoot}users`
                    }
                }
            }

            options.dataTextField = "title";
            options.dataValueField = "id";

            break;
        default:
            kendo.alert(`Onbekende datasource ('${options.dataSource}') opgegeven bij combobox-veld ('{title}'). Neem a.u.b. contact op met ons.`);
            break;
    }
} else if (options.entityType) {
    const searchEverywhere = options.searchEverywhere && (options.searchEverywhere > 0 || options.searchEverywhere.toLowerCase() === "true");
    const searchFields = options.searchFields || [];
    const searchInTitle = typeof options.searchInTitle === "undefined" || options.searchInTitle === null || options.searchInTitle === true || options.searchInTitle === "true" || options.searchInTitle > 0;

    options.dataSource.transport.read = async (kendoReadOptions) => {
        try {
            let searchValue = "";

            if (kendoReadOptions.data && kendoReadOptions.data.filter && kendoReadOptions.data.filter.filters && kendoReadOptions.data.filter.filters.length){
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
} else if (options.dataSelectorId) {
    options.dataSource.transport.read = async (kendoReadOptions) => {
        try {
            const result = await Wiser.api({
                url: `${window.dynamicItems.settings.getItemsUrl}?trace=false&encryptedDataSelectorId=${encodeURIComponent(options.dataSelectorId)}`,
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

if (options.cascadeFrom && typeof options.cascadeFrom === "string") {
    options.cascadeFrom = `field_${options.cascadeFrom}{propertyIdSuffix}`;
    options.autoBind = false;
    options.dataSource.serverFiltering = true;
}

const kendoComponent = options.useDropDownList || options.mode === "dropDownList" ? field.kendoDropDownList(options).data("kendoDropDownList") : field.kendoComboBox(options).data("kendoComboBox");
const readonly = {readonly};
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
        click: (event)=> {
            const dataItem = kendoComponent.dataItem();

            // If the current item is in an information block iframe, open it in the parent.
            let windowToUse = window;
            if (window.parent && window.parent.dynamicItems && window.parent.dynamicItems.settings.gridViewSettings && window.parent.dynamicItems.settings.gridViewSettings.informationBlock) {
                windowToUse = windowToUse.parent;
            }
            windowToUse.dynamicItems.windows.loadItemInWindow(false, kendoComponent.value(), dataItem.encrypted_id || dataItem.encryptedId || dataItem.encryptedid, dataItem.entityType || dataItem.entity_type || dataItem.entitytype, kendoComponent.text(), false, null, options, 0, null, kendoComponent);
        }
    });
}

{customScript}
})();
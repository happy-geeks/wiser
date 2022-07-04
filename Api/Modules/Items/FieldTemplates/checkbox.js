(function() {
    const options = {options};
    const field = $("#field_{propertyIdWithSuffix}").change(window.dynamicItems.fields.onFieldValueChange.bind(window.dynamicItems.fields));
    const container = field.closest(".item");
    let imageUrl = options.imageUrl;
    if (options.imageId) {
        imageUrl = `${window.dynamicItems.settings.wiserApiRoot}items/0/files/${options.imageId}/${encodeURIComponent("{propertyName}.png")}?encryptedCustomerId=${encodeURIComponent(window.dynamicItems.settings.customerId)}&encryptedUserId=${encodeURIComponent(window.dynamicItems.settings.userId)}&isTest=${window.dynamicItems.settings.isTestEnvironment}&subDomain=${encodeURIComponent(window.dynamicItems.settings.subDomain)}&itemLinkId=${encodeURIComponent("{itemLinkId}")}&entityType=${encodeURIComponent("{entityType}")}&linkType={linkType}`;
    } 
    if (imageUrl) {
        container.find(".checkbox-img img").attr("src", imageUrl);
    }
    else {
        container.find(".checkbox-img").hide();
    }

    {customScript}
})();
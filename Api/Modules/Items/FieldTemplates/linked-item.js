(function() {
const options = $.extend({
	"entityType": "item",
	"linkType": 1,
    "template": "{itemTitle}",
    "reversed": false,
    "noLinkText": "Geen koppeling",
    "hideFieldIfNoLink": false
}, {options});

Wiser.api({
    url: `${window.dynamicItems.settings.wiserApiRoot}items/{itemId}/linked-items?entityType=${encodeURIComponent(options.entityType)}&itemIdEntityType=${encodeURIComponent("{entityType}")}&linkType=${encodeURIComponent(options.linkType)}&reverse=${!options.reversed}`
}).then((results) => {
    const field = $("#field_{propertyIdWithSuffix}").html("");

    if (!results || !results.length) {
        if (options.hideFieldIfNoLink) {
            $("#container_{propertyIdWithSuffix}").addClass("forceHidden");
        } else {
            $("<span class='openWindow' />").html(options.noLinkText).appendTo(field);
        }
        return;
    }
    
    
    $(results).each((index, result) => {
        let newValue = options.template.replace(/{itemTitle}/gi, result.title);
        newValue = newValue.replace(/{id}/gi, result.id);
        newValue = newValue.replace(/{environment}/gi, result.publishedEnvironment);
        newValue = newValue.replace(/{entityType}/gi, result.entityType);
        
        for (const detail of result.details) {
            const regExp = new RegExp("{" + detail.key + "}", "gi");
            newValue = newValue.replace(regExp, detail.value);
        }
        
        // Replace left-over variables with empty strings
        const regExp = new RegExp("{.+?}", "gi");
        newValue = newValue.replace(regExp, "");
        
        if (options.textOnly) {
            $("<span class='openWindow' />").html(newValue).appendTo(field);
        } else {
            $("<a class='openWindow' href='#' />").html(newValue + "&nbsp;<span class='k-icon k-i-hyperlink-open-sm'></span>").appendTo(field).click((event) => {
                window.dynamicItems.windows.loadItemInWindow(false, result.id, result.encryptedId, result.entityType, result.title, true, null, { hideTitleColumn: false }, result.linkId, null, null, options.linkType);
            });
        }
    });
});
})();
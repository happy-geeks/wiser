(function() {
var options = $.extend({
	"entityType": "item",
	"linkType": 1,
    "template": "{itemTitle}",
    "reversed": false,
    "noLinkText": "Geen koppeling",
    "hideFieldIfNoLink": false
}, {options});

var templateName = "GET_DESTINATION_ITEMS";
if (options.reversed) {
    templateName = "GET_DESTINATION_ITEMS_REVERSED";
}

Wiser2.api({
    url: window.dynamicItems.settings.serviceRoot + "/" + templateName + "?itemId={itemId}&entityType=" + encodeURIComponent(options.entityType) + "&linkTypeNumber=" + encodeURIComponent(options.linkType)
}).then(function(results) {
    var field = $("#field_{propertyIdWithSuffix}").html("");

    if (!results || !results.length) {
        if (options.hideFieldIfNoLink) {
            $("#container_{propertyIdWithSuffix}").addClass("forceHidden");
        } else {
            $("<span class='openWindow' />").html(options.noLinkText).appendTo(field);
        }
        return;
    }
    
    
    $(results).each(function(index, result) {
        var newValue = options.template.replace(/{itemTitle}/gi, result.title);
        newValue = newValue.replace(/{id}/gi, result.id);
        newValue = newValue.replace(/{environment}/gi, result.publishedEnvironment);
        newValue = newValue.replace(/{entityType}/gi, result.entityType);
        
        for (var key in result.property_) {
            if (!result.property_.hasOwnProperty(key)) {
                continue;
            }

            var regExp = new RegExp("{" + key + "}", "gi");
            newValue = newValue.replace(regExp, result.property_[key]);
        }
        
        if (options.textOnly) {
            $("<span class='openWindow' />").html(newValue).appendTo(field);
        } else {
            $("<a class='openWindow' href='#' />").html(newValue + "&nbsp;<span class='k-icon k-i-hyperlink-open-sm'></span>").appendTo(field).click(function(event) {
                window.dynamicItems.windows.loadItemInWindow(false, result.id, result.encryptedId, result.entityType, result.title, true, null, { hideTitleColumn: false }, result.linkId, null, null, options.linkType);
            });
        }
    });
});
})();
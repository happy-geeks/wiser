(function() {
const options = $.extend({
    "entityType": "item",
    "linkType": 1,
    "template": "{itemTitle}",
    "reversed": false,
    "noLinkText": "Geen koppeling",
    "hideFieldIfNoLink": false,
    "removeUnknownVariables": false
}, {options});

const url = `${window.dynamicItems.settings.wiserApiRoot}items/{itemIdEncrypted}/linked/details?entityType=${encodeURIComponent(options.entityType)}&itemIdEntityType={entityType}&linkType=${encodeURIComponent(options.linkType)}&reversed=${encodeURIComponent(!options.reversed)}`;

Wiser.api({ url: url }).then(function(results) {
    const field = $("#field_{propertyIdWithSuffix}").html("");

    if (!results || !results.length) {
        if (options.hideFieldIfNoLink) {
            $("#container_{propertyIdWithSuffix}").addClass("forceHidden");
        } else {
            $("<span class='openWindow' />").html(options.noLinkText).appendTo(field);
        }
        return;
    }

    // Function to escape all special regex characters.
    const regExpEscape = (input) => {
        return input.replace(/[-[\]{}()*+!<=:?.\/\\^$|#\s,]/g, '\\$&');
    };

    results.forEach((result) => {
        let newValue = options.template.replace(/\{itemTitle}/gi, result.title);
        newValue = newValue.replace(/\{id}/gi, result.id);
        newValue = newValue.replace(/\{environment}/gi, result.publishedEnvironment);
        newValue = newValue.replace(/\{entityType}/gi, result.entityType);

        if (result.hasOwnProperty("details")) {
            result.details.forEach((detail) => {
                const regExp = new RegExp(`\{${regExpEscape(detail.key)}\}`, "gi");
                newValue = newValue.replace(regExp, detail.value);
            });
        }

        if (options.removeUnknownVariables) {
            newValue = newValue.replace(/\{[^}]+?}/gi, "");
        }

        if (options.textOnly) {
            $("<span class='openWindow' />").html(newValue).appendTo(field);
        } else {
            $("<a class='openWindow' href='#' />").html(`${newValue}&nbsp;<span class='k-icon k-i-hyperlink-open-sm'></span>`).appendTo(field).click(function() {
                window.dynamicItems.windows.loadItemInWindow(false, result.id, result.encryptedId, result.entityType, result.title, true, null, { hideTitleColumn: false }, result.linkId, null, null, options.linkType);
            });
        }
    });
});
})();
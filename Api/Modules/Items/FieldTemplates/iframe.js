(() => {
    var options = {options};
    var container = $("#container_{propertyIdWithSuffix}");
    var field = $("#field_{propertyIdWithSuffix}").change(window.dynamicItems.fields.onFieldValueChange.bind(window.dynamicItems.fields));
    var urlMode = options.type === "url" || (field.val() || "").indexOf("http") === 0;
    var hyperlink = container.find(".open-link").toggle(urlMode);
    
    if (urlMode) {
        field.addClass("padding-right");
	
        hyperlink.click(dynamicItems.fields.onInputLinkIconClick.bind(dynamicItems.fields, field, options));
    }

    if (options.saveOnEnter) {
        field.keypress((event) => {
            window.dynamicItems.fields.onInputFieldKeyUp(event, options);
        });
    }

    {customScript}
})();
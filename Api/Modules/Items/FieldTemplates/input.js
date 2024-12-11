(function() {
    let options = {options};
    let container = $("#container_{propertyIdWithSuffix}");
    let field = $("#field_{propertyIdWithSuffix}").change(window.dynamicItems.fields.onFieldValueChange.bind(window.dynamicItems.fields));
    let urlMode = options.type === "url" || (field.val() || "").indexOf("http") === 0;
    let hyperlink = container.find(".open-link").toggle(urlMode);
    
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
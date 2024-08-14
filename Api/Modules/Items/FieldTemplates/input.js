(() => {
    const options = {options};
    const container = $("#container_{propertyIdWithSuffix}");
    const  field = $("#field_{propertyIdWithSuffix}").change(window.dynamicItems.fields.onFieldValueChange.bind(window.dynamicItems.fields));
    
    const urlMode = options.type === "url" || (field.val() || "").indexOf("http") === 0;
    const hyperlink = container.find(".open-link").toggle(urlMode);
    
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
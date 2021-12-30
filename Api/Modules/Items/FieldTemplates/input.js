(function() {
    var options = {options};
    var container = $("#container_{propertyIdWithSuffix}");
    var field = $("#field_{propertyIdWithSuffix}").change(window.dynamicItems.fields.onFieldValueChange.bind(window.dynamicItems.fields));
    var urlMode = options.type === "url";
    var hyperlink = container.find(".open-link").toggle(urlMode);
    if (urlMode) {
        field.addClass("padding-right");
	
        hyperlink.click(function(event) {
            var fieldValue = field.val();
            if (!fieldValue) {
                event.preventDefault();
                return;
            }
		
            hyperlink.attr("href", (options.prefix || "") + fieldValue + (options.suffix || ""));
        });
    }

    if (options.saveOnEnter) {
        field.keypress(function(event) {
            window.dynamicItems.fields.onInputFieldKeyUp(event, options);
        });
    }

    {customScript}
})();
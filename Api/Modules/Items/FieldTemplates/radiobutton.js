(function() {
    Wiser2.api({
        url: window.dynamicItems.settings.serviceRoot + "/GET_DATA_FOR_RADIO_BUTTONS?propertyid={propertyId}&itemId={itemId}",
        method: "GET"
    }).then(function(results) {
        var field = $("#field_{propertyIdWithSuffix}");
        var defaultValue = field.data("defaultValue");
    
        for (var i = 0; i < results.length; i++) {
            var result = results[i];
            var label = $("<label>").addClass("radio");
            var input = $("<input>")
                .attr("type", "radio")
                .attr("name", field.data("name"))
                .attr("value", result.id)
                .prop("required", field.data("required") === "required")
                .attr("pattern", field.data("pattern"))
                .prop("checked", defaultValue == result.id)
                .prop("disabled", {readonly})
                .appendTo(label);
            var span = $("<span>").addClass("label").text(result.name).appendTo(label);
            input.change(window.dynamicItems.fields.onFieldValueChange.bind(window.dynamicItems.fields));
            field.append(label);
        }
	
        {customScript}
    });
})();
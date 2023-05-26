(function() {
    Wiser.api({
        url: dynamicItems.settings.wiserApiRoot + "items/" + encodeURIComponent("{itemIdEncrypted}") + "/action-button/{propertyId}",
        method: "POST"
    }).then(function(results) {
        var field = $("#field_{propertyIdWithSuffix}");
        var defaultValue = field.data("defaultValue");

        for (var i = 0; i < results.otherData.length; i++) {
            var result = results.otherData[i];
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
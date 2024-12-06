(() => {
    Wiser.api({
        url: dynamicItems.settings.wiserApiRoot + "items/" + encodeURIComponent("{itemIdEncrypted}") + "/action-button/{propertyId}",
        method: "POST"
    }).then(function(results) => {
        let field = $("#field_{propertyIdWithSuffix}");
        let defaultValue = field.data("defaultValue");

        for (let i = 0; i < results.otherData.length; i++) {
            let result = results.otherData[i];
            let label = $("<label>").addClass("radio");
            let input = $("<input>")
                .attr("type", "radio")
                .attr("name", field.data("name"))
                .attr("value", result.id)
                .prop("required", field.data("required") === "required")
                .attr("pattern", field.data("pattern"))
                .prop("checked", defaultValue == result.id)
                .prop("disabled", {readonly})
                .appendTo(label);
            let span = $("<span>").addClass("label").text(result.name).appendTo(label);
            input.change(window.dynamicItems.fields.onFieldValueChange.bind(window.dynamicItems.fields));
            field.append(label);
        }

        {customScript}
    });
})();
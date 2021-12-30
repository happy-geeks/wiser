(function() {
    var container = $("#container_{propertyIdWithSuffix}");
    var field = $("#field_{propertyIdWithSuffix}");
    var value = {default_value};
    if (!value) {
        container.find("a, img").addClass("hidden");
        container.find(".empty").removeClass("hidden");
    }

    {customScript}
})();
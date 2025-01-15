(() => {
    let container = $("#container_{propertyIdWithSuffix}");
    let field = $("#field_{propertyIdWithSuffix}");
    let value = {default_value};
    if (!value) {
        container.find("a, img").addClass("hidden");
        container.find(".empty").removeClass("hidden");
    }

    {customScript}
})();
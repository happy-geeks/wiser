(() => {
    const container = $("#container_{propertyIdWithSuffix}");
    const field = $("#field_{propertyIdWithSuffix}");
    const value = {default_value};
    if (!value) {
        container.find("a, img").addClass("hidden");
        container.find(".empty").removeClass("hidden");
    }

    {customScript}
})();
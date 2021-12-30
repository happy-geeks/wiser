(function() {
    var field = $("#field_{propertyIdWithSuffix}");
    var userItemPermissions = {userItemPermissions};
    var options = $.extend({
        click: function(event) {
            window.dynamicItems.fields.onActionButtonClick(event, "{itemIdEncrypted}", {propertyId}, {options}, field); 
        },
        icon: "gear"
    }, {options});

    if (options.doesCreate && (userItemPermissions & window.dynamicItems.permissionsEnum.create) === 0) {
        options.enable = false;
    }
    if (options.doesUpdate && (userItemPermissions & window.dynamicItems.permissionsEnum.update) === 0) {
        options.enable = false;
    }
    if (options.doesDelete && (userItemPermissions & window.dynamicItems.permissionsEnum.delete) === 0) {
        options.enable = false;
    }

    if (field.text) {
        field.find(".originalText").html(options.text);
    }
    var kendoComponent = field.kendoButton(options).data("kendoButton");
    {customScript}
})();
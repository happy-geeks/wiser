(function() {
var container = $("#container_{propertyIdWithSuffix}");
var readonly = {readonly};
var initialFiles = {initialFiles};
if (initialFiles && initialFiles.length > 0) {
    for (var i = 0; i < initialFiles.length; i++) {
        initialFiles[i].readonly = readonly;
        initialFiles[i].entityType = "{entityType}";
        initialFiles[i].linkType = "{linkType}";
    }
}

var options = {options};

options = $.extend({
    async: {
        saveUrl: window.dynamicItems.settings.wiserApiRoot + "items/{itemIdEncrypted}/upload?propertyName=" + encodeURIComponent("{propertyName}") + "&itemLinkId={itemLinkId}&useTinyPng=" + (options.useTinyPng === true).toString() + "&useCloudFlare=" + (options.useCloudFlare === true).toString() + "&entityType=" + encodeURIComponent("{entityType}") + "&linkType={linkType}",
        withCredentials: false,
        removeUrl: "remove"
    },
    validation: {
        allowedExtensions: [".jpg", ".jpeg", ".png", ".bmp", ".gif", ".svg", ".webp", ".tif", ".tiff"]
    },
    files: initialFiles,
    upload: (e) => {
        let xhr = e.XMLHttpRequest;
        if (xhr) {
            xhr.addEventListener("readystatechange", (e) => {
                if (xhr.readyState === 1 /* OPENED */) {
                    xhr.setRequestHeader("authorization", `Bearer ${localStorage.getItem("accessToken")}`);
                }
            });
        }
    },
    remove: window.dynamicItems.fields.onImageDelete.bind(window.dynamicItems.fields),
    success: window.dynamicItems.fields.onImageUploadSuccess.bind(window.dynamicItems.fields),
    error: window.dynamicItems.fields.onFileUploadError.bind(window.dynamicItems.fields),
    showFileList: true,
    template: "# if(files.length && files[0].validationErrors && files[0].validationErrors.indexOf('invalidFileExtension') > -1) { # <div class='k-file-error k-text-error'>Bestandstype niet toegestaan</div> # } #",
    dropZone: ".imageUploader_{propertyIdWithSuffix}"
}, {options});

if (window.dynamicItems.fields.onFileUploadError) {
    options.error = window.dynamicItems.fields.onFileUploadError.bind(window.dynamicItems.fields);
}

var field = $("#field_{propertyIdWithSuffix}");
var kendoComponent = field.kendoUpload(options).data("kendoUpload");

$("#images_{propertyIdWithSuffix}").html(kendo.render(kendo.template($("#uploaderTemplate").html()), initialFiles));

if (readonly === true) {
    kendoComponent.disable();
}

// Add drag & drop functionality for changing the order of files.
container.find(".imagesContainer").kendoSortable({
    cursor: "move",
    autoScroll: true,
    container: "#container_{propertyIdWithSuffix} .imagesContainer",
    hint: function (element) {
        return element.clone().addClass("hint");
    },
    placeholder: function (element) {
        return element.clone().addClass("k-state-hover").css("opacity", 0.65);
    },
    change: function (event) {
        // Kendo starts ordering with 0, but wiser starts with 1.
        const oldIndex = event.oldIndex + 1;
        const newIndex = event.newIndex + 1;
        const fileId = event.item.data("imageId");
        const propertyName = container.data("propertyName");

        Wiser.api({
            method: "PUT",
            contentType: "application/json",
            dataType: "json",
            url: `${dynamicItems.settings.wiserApiRoot}items/{itemId}/files/${fileId}/ordering?previousPosition=${oldIndex}&newPosition=${newIndex}&propertyName=${encodeURIComponent(propertyName)}&itemLinkId={itemLinkId}&entityType=${encodeURIComponent("{entityType}")}&linkType={linkType}`
        }).then(function (dataResult) {
        }).catch(function (jqXHR, textStatus, errorThrown) {
            console.error("Update file order error - {title}", jqXHR, textStatus, errorThrown);
            kendo.alert("Er is iets fout gegaan tijdens het aanpassen van de volgorde. Probeer het a.u.b. nogmaals of neem contact op met ons.");
        });
    }
});

{customScript}
})();
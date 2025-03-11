(() => {
let container = $("#container_{propertyIdWithSuffix}");
let readonly = {readonly};
let initialFiles = {initialFiles};

if (initialFiles && initialFiles.length > 0) {
    for (let i = 0; i < initialFiles.length; i++) {
        initialFiles[i].readonly = readonly;
        initialFiles[i].entityType = "{entityType}";
        initialFiles[i].linkType = "{linkType}";
    }
}

let options = {options};

// Images should almost always be publicly accessible, so we default to true.
const markAsProtected = options.filesCanBeAccessedPublicly === false || options.filesCanBeAccessedPublicly === 0;

options = $.extend({
    async: {
        saveUrl: `${window.dynamicItems.settings.wiserApiRoot}items/{itemIdEncrypted}/upload?propertyName=${encodeURIComponent("{propertyName}")}&itemLinkId={itemLinkId}&useTinyPng=${(options.useTinyPng === true).toString()}&useCloudFlare=${(options.useCloudFlare === true).toString()}&entityType=${encodeURIComponent("{entityType}")}&linkType={linkType}&markAsProtected=${markAsProtected}`,
        withCredentials: false,
        removeUrl: "remove"
    },
    validation: {
        allowedExtensions: [
            ".bmp",
            ".gif",
            ".jpg",
            ".jpeg",
            ".png",
            ".svg",
            ".tif",
            ".tiff",
            ".webp"
        ]
    },
    files: initialFiles,
    upload: (e) => {
        let xhr = e.XMLHttpRequest;
        if (xhr) {
            xhr.addEventListener("readystatechange", (e) => {
                /* OPENED */
                if (xhr.readyState === 1) {
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
}, options);

if (window.dynamicItems.fields.onFileUploadError) {
    options.error = window.dynamicItems.fields.onFileUploadError.bind(window.dynamicItems.fields);
}

let field = $("#field_{propertyIdWithSuffix}");
let kendoComponent = field.kendoUpload(options).data("kendoUpload");

$("#images_{propertyIdWithSuffix}").html(kendo.render(kendo.template($("#uploaderTemplate").html()), initialFiles));

if (readonly === true) {
    kendoComponent.disable();
}

// Add drag & drop functionality for changing the order of files.
container.find(".imagesContainer").kendoSortable({
    cursor: "move",
    autoScroll: true,
    container: "#container_{propertyIdWithSuffix} .imagesContainer",
    hint: (element) => {
        return element.clone().addClass("hint");
    },
    placeholder: (element) => {
        return element.clone().addClass("k-state-hover").css("opacity", 0.65);
    },
    change: async (event) => {
        // Kendo starts ordering with 0, but wiser starts with 1.
        const oldIndex = event.oldIndex + 1;
        const newIndex = event.newIndex + 1;
        const fileId = event.item.data("imageId");
        const propertyName = container.data("propertyName");

        try {
            await Wiser.api({
                method: "PUT",
                contentType: "application/json",
                dataType: "json",
                url: `${dynamicItems.settings.wiserApiRoot}items/{itemId}/files/${fileId}/ordering?previousPosition=${oldIndex}&newPosition=${newIndex}&propertyName=${encodeURIComponent(propertyName)}&itemLinkId={itemLinkId}&entityType=${encodeURIComponent("{entityType}")}&linkType={linkType}`
            });
        }
        catch (exception)
        {
            console.error("Update file order error - {title}", exception);
            kendo.alert("Er is iets fout gegaan tijdens het aanpassen van de volgorde. Probeer het a.u.b. nogmaals of neem contact op met ons.");
        }
    }
});

{customScript}
})();
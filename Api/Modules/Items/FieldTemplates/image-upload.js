(function() {
var readonly = {readonly};
var initialFiles = {initialFiles};
if (initialFiles && initialFiles.length > 0) {
    for (var i = 0; i < initialFiles.length; i++) {
        initialFiles[i].readonly = readonly;
    }
}

var options = {options};
    
options = $.extend({
    async: {
		saveUrl: window.dynamicItems.settings.wiserApiRoot + "items/{itemIdEncrypted}/upload?propertyName=" + encodeURIComponent("{propertyName}") + "&itemLinkId={itemLinkId}&useTinyPng=" + (options.useTinyPng === true).toString(),
		withCredentials: false,
        removeUrl: "remove"
    },
    validation: {
        allowedExtensions: [".jpg", ".jpeg", ".png", ".bmp", ".gif", ".svg"]
    },
    files: initialFiles,
    upload: (e) => {
        let xhr = e.XMLHttpRequest;
        if (xhr) {
            xhr.addEventListener("readystatechange", (e) => {
                if (xhr.readyState === 1 /* OPENED */) {
                    xhr.setRequestHeader("authorization", `Bearer ${localStorage.getItem("access_token")}`);
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

{customScript}
})();
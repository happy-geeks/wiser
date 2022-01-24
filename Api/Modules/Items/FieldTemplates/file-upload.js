(function() {
var container = $("#container_{propertyIdWithSuffix}");
var fileTemplate = kendo.template($("#fileTemplate_{propertyIdWithSuffix}").html());

var options = $.extend({
	multiple: true,
	template: fileTemplate,
	async: {
		saveUrl: window.dynamicItems.settings.wiserApiRoot + "items/" + encodeURIComponent("{itemIdEncrypted}") + "/upload?propertyName=" + encodeURIComponent("{propertyName}") + "&itemLinkId={itemLinkId}",
		removeUrl: "remove",
		withCredentials: false
    },
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
	remove: window.dynamicItems.fields.onFileDelete.bind(window.dynamicItems.fields),
    success: window.dynamicItems.fields.onUploaderSuccess.bind(window.dynamicItems.fields),
    error: window.dynamicItems.fields.onFileUploadError.bind(window.dynamicItems.fields)
}, {options});

var addFileUrl = function(event) {
    var fileUrl = event.sender.element.find("#fileUrl").val();
    if (!fileUrl) {
        kendo.alert("Vul a.u.b. een URL in.");
        return false;
    }
    
    var fileData = {
        contentUrl: fileUrl,
        name: event.sender.element.find("#fileName").val(),
        title: event.sender.element.find("#fileTitle").val()
    };
    
    Wiser2.api({
        method: "POST",
        contentType: "application/json",
        dataType: "json",
        url: dynamicItems.settings.wiserApiRoot + "items/" + encodeURIComponent("{itemIdEncrypted}") + "/files/url?itemLinkId={itemLinkId}&propertyName=" + encodeURIComponent("{propertyName}"),
        data: JSON.stringify(fileData)
    }).then(function(dataResult) {
        var newFile = { 
            files: [dataResult],
            name: dataResult.name,
            fileId: dataResult.fileId,
            size: 0,
            addedOn: new Date()
        };
        
        var filesList = container.find(".k-upload-files");
        if (!filesList.length) {
            filesList = $("<ul class='k-upload-files k-reset' />");
            container.find(".k-upload").append(filesList);
        }
        
        var listItem = $("<li class='k-file k-file-success' />");
        listItem.html(fileTemplate(newFile));
        filesList.append(listItem);
    }).catch(function(jqXHR, textStatus, errorThrown) {
        console.error("read error - {title}", jqXHR, textStatus, errorThrown);
        kendo.alert("Er is iets fout gegaan toevoegen van een bestands-URL voor het veld '{title}'. Probeer het a.u.b. nogmaals of neem contact op met ons.")
    });
};

var files = {initialFiles};
var initialize = function() {
    options.files = files;

    var field = $("#field_{propertyIdWithSuffix}");
    var kendoComponent = $("#field_{propertyIdWithSuffix}").kendoUpload(options).data("kendoUpload");

    kendoComponent.wrapper.find(".editTitle").click(window.dynamicItems.fields.onUploaderEditTitleClick.bind(window.dynamicItems.fields));
    kendoComponent.wrapper.find(".editName").click(window.dynamicItems.fields.onUploaderEditNameClick.bind(window.dynamicItems.fields));
    var readonly = {readonly};
    if (readonly === true || options.queryId) {
        kendoComponent.disable();
    }
    
    var addFileUrlButton = container.find(".addFileUrl");
    if (!options.showAddFileUrlButton) {
        addFileUrlButton.hide();
    } else {
        kendoComponent.wrapper.find(".k-dropzone").append(addFileUrlButton);
        addFileUrlButton.kendoButton({
            click: function(event) {
                $("#addFileUrlDialog_{propertyIdWithSuffix}").kendoDialog({
                    width: "400px",
                    visible: false,
                    title: "Bestands-URL toevoegen",
                    closable: true,
                    modal: true,
                    content: kendo.template($("#addFileUrlDialogTemplate_{propertyIdWithSuffix}").html()),
                    actions: [
                        { text: "Annuleren" },
                        { text: "Opslaan", primary: true, action: addFileUrl }
                    ]
                }).data("kendoDialog").open();
            }
        });
    }
    
    {customScript}
};

if (!options.queryId) {
    initialize();
} else {
    Wiser2.api({
        method: "POST",
        contentType: "application/json",
        dataType: "json",
        url: dynamicItems.settings.wiserApiRoot + "items/" + encodeURIComponent("{itemIdEncrypted}") + "/action-button/{propertyId}?queryId=" + encodeURIComponent(options.queryId) + "&itemLinkId={itemLinkId}"
    }).then(function(dataResult) {
        files = dataResult.otherData;
        initialize();
    }).catch(function(jqXHR, textStatus, errorThrown) {
        console.error("read error - {title}", jqXHR, textStatus, errorThrown);
        kendo.alert("Er is iets fout gegaan tijdens het laden van de bestanden voor het veld '{title}'. Probeer het a.u.b. nogmaals of neem contact op met ons.");
    });
}
})();
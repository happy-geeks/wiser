(() => {
const container = $("#container_{propertyIdWithSuffix}");
const fileTemplate = kendo.template($("#fileTemplate_{propertyIdWithSuffix}").html());

const options = $.extend({
	multiple: true,
	template: fileTemplate,
	async: {
		saveUrl: window.dynamicItems.settings.wiserApiRoot + "items/" + encodeURIComponent("{itemIdEncrypted}") + "/upload?propertyName=" + encodeURIComponent("{propertyName}") + "&itemLinkId={itemLinkId}&entityType=" + encodeURIComponent("{entityType}") + "&linkType={linkType}",
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

const addFileUrl = async (event) => {
    const fileUrl = event.sender.element.find("#fileUrl").val();
    if (!fileUrl) {
        kendo.alert("Vul a.u.b. een URL in.");
        return false;
    }

    const fileData = {
        contentUrl: fileUrl,
        name: event.sender.element.find("#fileName").val(),
        title: event.sender.element.find("#fileTitle").val()
    };

    try {
        let dataResult = Wiser.api({
            method: "POST",
            contentType: "application/json",
            dataType: "json",
            url: dynamicItems.settings.wiserApiRoot + "items/" + encodeURIComponent("{itemIdEncrypted}") + "/files/url?itemLinkId={itemLinkId}&propertyName=" + encodeURIComponent("{propertyName}") + "&entityType=" + encodeURIComponent("{entityType}") + "&linkType={linkType}",
            data: JSON.stringify(fileData)
        });
        
        dataResult.itemId = dataResult.itemId || "{itemIdEncrypted}";
        dataResult.addedOn = dataResult.addedOn || new Date();
        const newFile = {
            files: [dataResult],
            name: dataResult.name,
            fileId: dataResult.fileId,
            size: 0
        };

        let filesList = container.find(".k-upload-files");
        if (!filesList.length) {
            filesList = $("<ul class='k-upload-files k-reset' />");
            container.find(".k-upload").append(filesList);
        }

        const listItem = $("<li class='k-file k-file-success' />");
        listItem.html(fileTemplate(newFile));
        filesList.append(listItem);
    }
    catch(error) {
        console.error("read error - {title}", error);
        kendo.alert("Er is iets fout gegaan toevoegen van een bestands-URL voor het veld '{title}'. Probeer het a.u.b. nogmaals of neem contact op met ons.")
    };
};

let files = {initialFiles};
const initialize = async () => {
    options.files = files;

    const field = $("#field_{propertyIdWithSuffix}");
    const kendoComponent = $("#field_{propertyIdWithSuffix}").kendoUpload(options).data("kendoUpload");

    kendoComponent.wrapper.find(".editTitle").click(window.dynamicItems.fields.onUploaderEditTitleClick.bind(window.dynamicItems.fields));
    kendoComponent.wrapper.find(".editName").click(window.dynamicItems.fields.onUploaderEditNameClick.bind(window.dynamicItems.fields));
    
    const readonly = {readonly};
    
    if (readonly === true || options.queryId) {
        kendoComponent.disable();
    }

    const addFileUrlButton = container.find(".addFileUrl");
    
    if (!options.showAddFileUrlButton) {
        addFileUrlButton.hide();
    } else {
        kendoComponent.wrapper.find(".k-dropzone").append(addFileUrlButton);
        addFileUrlButton.kendoButton({
            click: (event) => {
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

    // Add drag & drop functionality for changing the order of files.
    container.find(".k-upload-files").kendoSortable({
        cursor: "move",
        autoScroll: true,
        container: "#container_{propertyIdWithSuffix} .k-upload-files",
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
            const fileContainer = event.item.find(".fileContainer");
            const fileId = fileContainer.data("fileId");
            const propertyName = container.data("propertyName");

            try {
                let dataResult = await Wiser.api({
                    method: "PUT",
                    contentType: "application/json",
                    dataType: "json",
                    url: `${dynamicItems.settings.wiserApiRoot}items/{itemId}/files/${fileId}/ordering?previousPosition=${oldIndex}&newPosition=${newIndex}&propertyName=${encodeURIComponent(propertyName)}&itemLinkId={itemLinkId}&entityType=${encodeURIComponent("{entityType}")}&linkType={linkType}`
                });
            } catch (exception) {
                console.error("Update file order error - {title}", exception);
                kendo.alert("Er is iets fout gegaan tijdens het aanpassen van de volgorde. Probeer het a.u.b. nogmaals of neem contact op met ons.");
            }
        }
    });

    {customScript}
};

if (!options.queryId) {
    initialize();
} else {
    let dataResult = null;
    try {
        dataResult = Wiser.api({
            method: "POST",
            contentType: "application/json",
            dataType: "json",
            url: `${dynamicItems.settings.wiserApiRoot}items/${encodeURIComponent("{itemIdEncrypted}")}/action-button/{propertyId}?queryId=${encodeURIComponent(options.queryId)}&itemLinkId={itemLinkId}&entityType=${encodeURIComponent("{entityType}")}&linkType={linkType}`,
        })
    }
    catch(error) {
        console.error("read error - {title}", error);
        kendo.alert("Er is iets fout gegaan tijdens het laden van de bestanden voor het veld '{title}'. Probeer het a.u.b. nogmaals of neem contact op met ons.");
    }
    finally {
        files = dataResult.otherData;
        initialize();
    }
}
})();
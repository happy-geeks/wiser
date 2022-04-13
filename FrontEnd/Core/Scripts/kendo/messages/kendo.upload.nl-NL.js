if (kendo.ui.Upload) {
    kendo.ui.Upload.prototype.options.localization =
        $.extend(true, kendo.ui.Upload.prototype.options.localization,{
            "cancel": "Annuleren",
            "dropFilesHere": "Sleep bestanden hier naartoe",
            "headerStatusUploaded": "Gereed",
            "headerStatusUploading": "Uploaden...",
            "remove": "Verwijderen",
            "retry": "Opnieuw",
            "select": "Selecteer",
            "statusFailed": "mislukt",
            "statusUploaded": "gelukt",
            "statusUploading": "bezig met uploaden",
            "uploadSelectedFiles": "Bestanden uploaden"
        });
}
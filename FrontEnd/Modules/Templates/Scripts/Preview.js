import { Wiser } from "../../Base/Scripts/Utils.js";
import "../../Base/Scripts/Processing.js";

require("@progress/kendo-ui/js/kendo.notification.js");
require("@progress/kendo-ui/js/kendo.button.js");
require("@progress/kendo-ui/js/kendo.combobox.js");
require("@progress/kendo-ui/js/kendo.grid.js");
require("@progress/kendo-ui/js/cultures/kendo.culture.nl-NL.js");
require("@progress/kendo-ui/js/messages/kendo.messages.nl-NL.js");

import "../Css/Preview.css";

export class Preview {
    /**
     * Initializes a new instance of DynamicContent.
     * @param {any} settings An object containing the settings for this class.
     */
    constructor(base) {
        this.base = base;

        this.previewProfiles = null;
        this.filterVariablesGrid = null;
        this.previewProfilesDropDown = null;
    }

    customBoolEditor(container, options) {
        $('<input class="checkbox" type="checkbox" name="encrypt" data-type="boolean" data-bind="checked:encrypt">').appendTo(container);
    }

    customDopDownEditor(container, options) {
        $("<select name='type' data-type='string' data-bind='type'><option value='POST'>POST</option><option value='SESSION'>SESSION</option></select>").appendTo(container);
    }

    initPreviewProfileInputs(force = false, skipPreview = false) {
        if (!this.previewProfilesDropDown || force) {
            this.previewProfilesDropDown = $("#preview-combo-select").kendoDropDownList({
                dataSource: this.previewProfiles,
                dataTextField: "name",
                dataValueField: "id",
                optionLabel: "Nieuw profiel",
                change: (event) => {
                    this.initPreviewProfileInputs();
                }
            }).data("kendoDropDownList");
        }

        const selectedProfileId = this.previewProfilesDropDown.value();
        let tempPreviewVariablesData = [];
        if (this.previewProfiles && this.previewProfiles.length > 0) {
            const selectedProfile = this.previewProfiles.find(p => p.id == selectedProfileId);
            if (selectedProfile) {
                tempPreviewVariablesData = selectedProfile.variables || [];
                document.getElementById("profile-url").value = selectedProfile.url || "";
            } else {
                document.getElementById("profile-url").value = "";
            }
        }

        if (!this.filterVariablesGrid || force) {
            this.filterVariablesGrid = $("#preview-variables").kendoGrid({
                noRecords: {
                    template: "Er zijn nog geen variabelen toegevoegd"
                },
                scrollable: true,
                resizable: false,
                filterable: false,
                pageable: false,
                toolbar: [{ name: "create", text: "Add variable" }],
                columns: [
                    {
                        field: "type",
                        title: "Type",
                        editor: this.customDopDownEditor
                    },
                    {
                        field: "key",
                        title: "Key"
                    },
                    {
                        field: "value",
                        title: "Value"
                    },
                    {
                        field: "encrypt",
                        width: 50,
                        editor: this.customBoolEditor

                    },
                    {
                        command: ["edit",
                            {
                                name: "delete", text: "",
                                iconClass: "k-icon k-i-trash"
                            }
                        ],
                        title: "&nbsp;",
                        width: 230
                    }
                ],
                editable: "inline",
                save: (editEvent) => {
                    this.generatePreview(false);
                }
            }).data("kendoGrid");
        }

        this.filterVariablesGrid.setDataSource({
            data: tempPreviewVariablesData,
            schema: {
                model: {
                    id: "key"
                }
            }
        });

        if (skipPreview) {
            return;
        }

        this.generatePreview(false);
    }

    async loadProfiles() {
        this.previewProfiles = await Wiser.api({
            url: `${this.base.settings.wiserApiRoot}templates/${this.base.selectedId}/profiles`,
            dataType: "json",
            method: "GET"
        });

        if (this.previewProfilesDropDown) {
            this.previewProfilesDropDown.setDataSource(this.previewProfiles);
        }
    }

    //Bind buttons in the preview tab of the template overview
    bindPreviewButtons() {
        $("#preview-remove-profile").on("click", this.onDeletePreviewProfileButtonClick.bind(this));
        $("#preview-save-profile-as").on("click", this.onSavePreviewProfileButtonClick.bind(this, true));
        $("#preview-save-profile").on("click", this.onSavePreviewProfileButtonClick.bind(this, false));
        $("#profile-url").change(this.generatePreview.bind(this));
    }

    /**
     * Generates a preview for the current HTML template, this will call the API to generate the HTML and put that HTML in an iframe.
     */
    async generatePreview(showLoader = true) {
        const process = `generatePreview_${Date.now()}`;
        if (showLoader) {
            window.processing.addProcess(process);
        }

        try {
            const data = {
                templateSettings: this.base.getCurrentTemplateSettings(),
                url: $("#profile-url").val(),
                previewVariables: this.filterVariablesGrid.dataSource.data(),
                components: this.base.getDynamicContentPreviewSettings ? this.base.getDynamicContentPreviewSettings() : null
            };

            if (this.filterVariablesGrid.dataSource.data().filter(d => !d.key || !d.value).length > 0) {
                // If there are invalid variables, don't generate preview yet.
                window.processing.removeProcess(process);
                return;
            }

            const generatedHtml = await Wiser.api({
                url: `${this.base.settings.wiserApiRoot}templates/preview`,
                contentType: "application/json",
                type: "POST",
                data: JSON.stringify(data)
            });

            const iframeElement = $("#previewIframe");
            let iframe = iframeElement[0];
            iframe = iframe.contentWindow || (iframe.contentDocument.document || iframe.contentDocument);

            iframe.document.open();
            iframe.document.write(generatedHtml);
            iframe.document.close();
        } catch (exception) {
            console.error(exception);
            window.popupNotification.show(`Er is iets fout gegaan met het genereren van de preview. Probeer het a.u.b. opnieuw of neem contact op.`, "error");
        }

        if (showLoader) {
            window.processing.removeProcess(process);
        }
    }

    /**
     * Generates a preview for a component, this will call the API to generate the HTML and show a dialog with that HTML.
     */
    async generateHtmlPreviewForComponent(componentId, componentSettings) {
        const process = `generateHtmlPreviewForComponent_${Date.now()}`;
        window.processing.addProcess(process);

        try {
            const data = {
                templateSettings: this.base.getCurrentTemplateSettings(),
                url: $("#profile-url").val(),
                previewVariables: this.filterVariablesGrid.dataSource.data(),
                components: componentSettings
            };

            if (this.filterVariablesGrid.dataSource.data().filter(d => !d.key || !d.value).length > 0) {
                // If there are invalid variables, don't generate preview yet.
                window.processing.removeProcess(process);
                return;
            }

            const generatedHtml = await Wiser.api({
                url: `${this.base.settings.wiserApiRoot}dynamic-content/${componentId}/html-preview`,
                contentType: "application/json",
                type: "POST",
                data: JSON.stringify(data)
            });
            
            const htmlWindow = $("#htmlPreviewWindow").clone(true);
            const textArea = htmlWindow.find("textarea").val(generatedHtml);
            let codeMirrorInstance;

            htmlWindow.kendoWindow({
                width: "100%",
                height: "100%",
                title: "HTML van editor",
                activate: async () => {
                    const codeMirrorSettings = {
                        lineNumbers: true,
                        indentUnit: 4,
                        lineWrapping: true,
                        foldGutter: true,
                        gutters: ["CodeMirror-linenumbers", "CodeMirror-foldgutter", "CodeMirror-lint-markers"],
                        lint: true,
                        extraKeys: {
                            "Ctrl-Q": function (cm) {
                                cm.foldCode(cm.getCursor());
                            },
                            "Ctrl-Space": "autocomplete"
                        },
                        mode: "text/html"
                    };

                    // Only load code mirror when we actually need it.
                    await Misc.ensureCodeMirror();
                    codeMirrorInstance = CodeMirror.fromTextArea(textArea[0], codeMirrorSettings);
                },
                resize: (resizeEvent) => {
                    codeMirrorInstance.refresh();
                },
                close: (closeEvent) => {
                    closeEvent.sender.destroy();
                    htmlWindow.remove();
                }
            });

            htmlWindow.data("kendoWindow").maximize().open();
        } catch (exception) {
            console.error(exception);
            window.popupNotification.show(`Er is iets fout gegaan met het genereren van de preview. Probeer het a.u.b. opnieuw of neem contact op.`, "error");
        }
        
        window.processing.removeProcess(process);
    }

    async onDeletePreviewProfileButtonClick(event) {
        if (event) event.preventDefault();

        const selectedPreviewProfile = this.previewProfilesDropDown.value();
        if (!selectedPreviewProfile) {
            kendo.alert("U heeft geen bestaand profiel geselecteerd.");
            return;
        }

        await Wiser.showConfirmDialog(`Weet u zeker dat u het profiel "${this.previewProfilesDropDown.text()}" wilt verwijderen?`);

        const process = `deleteProfile_${Date.now()}`;
        window.processing.addProcess(process);

        try {
            await Wiser.api({
                url: `${this.base.settings.wiserApiRoot}templates/${this.base.selectedId}/profiles/${selectedPreviewProfile}`,
                dataType: "json",
                contentType: "application/json",
                type: "DELETE"
            });

            window.popupNotification.show(`Het profiel '${this.previewProfilesDropDown.text()}' is verwijderd`, "info");
        } catch (exception) {
            console.error(exception);
            kendo.alert("Er is iets fout gegaan met verwijderen. Probeer het a.u.b. nogmaals of neem contact op.");
        }

        window.processing.removeProcess(process);
    }

    async onSavePreviewProfileButtonClick(saveAsNewProfile, event) {
        if (event) event.preventDefault();

        const selectedPreviewProfile = parseInt(this.previewProfilesDropDown.value()) || 0;
        if (selectedPreviewProfile === 0) {
            saveAsNewProfile = true;
        }

        const name = saveAsNewProfile ? await kendo.prompt("Kies een naam") : this.previewProfilesDropDown.text();
        const process = `saveProfile_${Date.now()}`;
        window.processing.addProcess(process);

        try {
            const newProfile = await Wiser.api({
                url: saveAsNewProfile ? `${this.base.settings.wiserApiRoot}templates/${this.base.selectedId}/profiles` : `${this.base.settings.wiserApiRoot}templates/${this.base.selectedId}/profiles/${selectedPreviewProfile}`,
                dataType: "json",
                contentType: "application/json",
                type: saveAsNewProfile ? "POST" : "PUT",
                data: JSON.stringify({
                    name: name,
                    url: $("#profile-url").val(),
                    variables: this.filterVariablesGrid.dataSource.data()
                })
            });

            await this.loadProfiles();
            this.previewProfilesDropDown.value(saveAsNewProfile ? newProfile.id : selectedPreviewProfile);
            this.initPreviewProfileInputs(false, true);

            window.popupNotification.show(`Het profiel '${this.previewProfilesDropDown.text()}' is opgeslagen`, "info");
        } catch (exception) {
            console.error(exception);
            kendo.alert("Er is iets fout gegaan met opslaan. Probeer het a.u.b. nogmaals of neem contact op.");
        }

        window.processing.removeProcess(process);
    }
}
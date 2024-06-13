(function() {
    const readonly = {readonly};
    let kendoComponent = null;

    const imageTool = {
        name: "wiserImage",
        tooltip: "Afbeelding toevoegen",
        exec: (event) => window.dynamicItems.fields.onHtmlEditorImageExec.call(window.dynamicItems.fields, event, kendoComponent)
    };
    const fileTool = {
        name: "wiserFile",
        tooltip: "Link naar bestand toevoegen",
        exec: (event) => window.dynamicItems.fields.onHtmlEditorFileExec.call(window.dynamicItems.fields, event, kendoComponent)
    };
    const templateTool = {
        name: "wiserTemplate",
        tooltip: "Template toevoegen",
        exec: (event) => window.dynamicItems.fields.onHtmlEditorTemplateExec.call(window.dynamicItems.fields, event, kendoComponent)
    };
    const htmlSourceTool = {
        name: "wiserHtmlSource",
        tooltip: "HTML bekijken/aanpassen",
        exec: (e) => {
            window.dynamicItems.fields.onHtmlEditorHtmlSourceExec.call(window.dynamicItems.fields, event, kendoComponent, "{itemId}")
        }
    };
    const maximizeTool = {
        name: "wiserMaximizeEditor",
        tooltip: "Vergroten",
        exec: (event) => window.dynamicItems.fields.onHtmlEditorFullScreenExec.call(window.dynamicItems.fields, event, kendoComponent, "{itemId}")
    };
    const contentBuilderToolNotable = {
        name: "wiserContentBuilder",
        tooltip: "Content builder",
        template: "<button id='contentBuilder_{propertyIdWithSuffix}' tabindex='0' role='button' class='k-button k-tool k-group-start k-group-end content-builder-button' title='Content builder' aria-label='Content builder'><span class='k-icon k-i-wiser-content-builder'></span></button><label class='content-builder-label'>Content builder</label>",
        exec: (event) => window.dynamicItems.fields.onHtmlEditorContentBuilderExec.call(window.dynamicItems.fields, event, kendoComponent, "{itemId}", "{propertyName}", "{languageCode}", "{contentBuilderMode}")
    };
    const contentBuilderToolBasic = {
        name: "wiserContentBuilder",
        tooltip: "Content builder",
        exec: (event) => window.dynamicItems.fields.onHtmlEditorContentBuilderExec.call(window.dynamicItems.fields, event, kendoComponent, "{itemId}", "{propertyName}", "{languageCode}", "{contentBuilderMode}")
    };
    const entityBlockTool = {
        name: "wiserEntityBlock",
        tooltip: "Entiteit-blok",
        exec: (event) => window.dynamicItems.fields.onHtmlEditorEntityBlockExec.call(window.dynamicItems.fields, event, kendoComponent)
    };
    const dataSelectorTool = {
        name: "wiserDataSelector",
        tooltip: "Data selector met template",
        exec: (event) => window.dynamicItems.fields.onHtmlEditorDataSelectorExec.call(window.dynamicItems.fields, event, kendoComponent)
    };
    const youTubeTool = {
        name: "wiserYouTube",
        tooltip: "YouTube video invoegen",
        exec: (event) => window.dynamicItems.fields.onHtmlEditorYouTubeExec.call(window.dynamicItems.fields, event, kendoComponent)
    };

    const wiserApiRoot = window.dynamicItems.fields.base.settings.wiserApiRoot;
    const translationsTool = {
        name: "wiserTranslation",
        tooltip: "Vertaling invoegen",
        exec: (event) => Wiser.onHtmlEditorTranslationExec.call(Wiser, event, kendoComponent, wiserApiRoot)
    };

    const options = $.extend(true, {
        resizable: true,
        pasteCleanup: {
            all: false,
            css: false,
            keepNewLines: false,
            msAllFormatting: true,
            msConvertLists: true,
            msTags: true,
            none: false,
            span: true,
            custom: Strings.cleanupHtml
        },
        stylesheets: [
            window.dynamicItems.settings.htmlEditorCssUrl
        ],
        keyup: (event) => window.dynamicItems.fields.onHtmlEditorKeyUp.call(window.dynamicItems.fields, event, kendoComponent),
        serialization: {
            custom: window.dynamicItems.fields.onHtmlEditorSerialization
        },
        deserialization: {
            custom: window.dynamicItems.fields.onHtmlEditorDeserialization
        }
    }, {options});

    const tools = [];
    options.mode = parseInt(options.mode, 10) || 99;
    options.contentBuilderMode = options.contentBuilderMode || "basic";

    const allTools = {
        "contentBuilderToolNotable": [3, 4, 99],
        "bold": [1, 2, 3, 4, 99],
        "italic": [1, 2, 3, 4, 99],
        "underline": [1, 2, 3, 4, 99],
        "strikethrough": [1, 2, 3, 99],
        "justifyLeft": [2, 3, 99],
        "justifyCenter": [2, 3, 99],
        "justifyRight": [2, 3, 99],
        "justifyFull": [2, 3, 99],
        "insertUnorderedList": [2, 3, 99],
        "insertOrderedList": [2, 3, 99],
        "indent": [2, 3, 99],
        "outdent": [2, 3, 99],
        "createLink": [2, 3, 99],
        "unlink": [2, 3, 99],
        imageTool: [99],
        fileTool: [99],
        templateTool: [3, 99],
        entityBlockTool: [99],
        dataSelectorTool: [99],
        youTubeTool: [2, 3, 99],
        translationsTool: [2, 3, 99],
        "subscript": [99],
        "superscript": [99],
        "tableWizard": [3, 99],
        "createTable": [3, 99],
        "addRowAbove": [3, 99],
        "addRowBelow": [3, 99],
        "addColumnLeft": [3, 99],
        "addColumnRight": [3, 99],
        "deleteRow": [3, 99],
        "deleteColumn": [3, 99],
        "htmlSourceTool": [4, 99],
        "contentBuilderToolBasic": [3, 4, 99],
        "formatting": [99],
        "cleanFormatting": [99],
        "fontName": [99],
        "fontSize": [99],
        "foreColor": [99],
        "backColor": [99],
        "maximizeTool": [1, 2, 3, 4, 99]
    };

    for (let toolName in allTools) {
        if (!allTools.hasOwnProperty(toolName)) {
            continue;
        }

        const toolModes = allTools[toolName];
        // if this tool is manually added to the options OR is supposed to be in this editor mode, do not skip it
        if (((options.tools && options.tools.indexOf(toolName) > -1) || toolModes.indexOf(options.mode) > -1) === false) {
            continue;
        }

        let tool;
        switch (toolName) {
            case "contentBuilderToolNotable":
                if (options.contentBuilderMode !== "notable") {
                    continue;
                }

                tool = contentBuilderToolNotable;
                break;
            case "contentBuilderToolBasic":
                if (options.contentBuilderMode !== "basic") {
                    continue;
                }

                tool = contentBuilderToolBasic;
                break;
            case "imageTool":
                tool = imageTool;
                break;
            case "fileTool":
                tool = fileTool;
                break;
            case "templateTool":
                tool = templateTool;
                break;
            case "htmlSourceTool":
                tool = htmlSourceTool;
                break;
            case "maximizeTool":
                tool = maximizeTool;
                break;
            case "entityBlockTool":
                tool = entityBlockTool;
                break;
            case "dataSelectorTool":
                tool = dataSelectorTool;
                break;
            case "youTubeTool":
                tool = youTubeTool;
                break;
            case "translationsTool":
                tool = translationsTool;
                break;
            default:
                tool = toolName;
                break;
        }

        tools.push(tool);
    }

    if (readonly) {
        options.tools = [];
    } else if (tools) {
        options.tools = tools;
    }

    const container = $("#container_{propertyIdWithSuffix}");
    const defaultField = $("#field_{propertyIdWithSuffix}");
    const windowField = $("#field_window_{propertyIdWithSuffix}");
    const field = options.buttonMode === true ? windowField : defaultField;
    const openDialogButton = container.find(".openEditorInDialogButton");
    const closeDialogButton = container.find(".closeEditorWindowButton");
    const saveDialogButton = container.find(".saveEditorWindowButton");
    const windowElement = container.find("div.editorWindow").hide();

    if (options.buttonMode === true) {
        field.attr("style", "");
        options.resizable = false;
    }

    kendoComponent = field.kendoEditor(options).data("kendoEditor");
    $(kendoComponent.body).on("dblclick", (event) => window.dynamicItems.fields.onHtmlEditorDblClick.call(window.dynamicItems.fields, event, kendoComponent, "{itemId}"));

    $(kendoComponent.body).attr("contenteditable", !readonly);
    container.find(".editor-overlay").toggle(readonly);

    if (options.buttonMode !== true) {
        openDialogButton.hide();
    } else {
        defaultField.hide();

        const resizeEditor = (containerHeight) => {
            let newHeight = containerHeight - kendoComponent.toolbar.element.outerHeight(true) - windowElement.find("footer").outerHeight(true) - 3;
            if (newHeight < 50) {
                newHeight = 50;
            }

            kendoComponent.wrapper.css("height", newHeight);
        }

        let editorWindow;
        openDialogButton.kendoButton({
            icon: "html",
            click: (event) => {
                editorWindow = windowElement.kendoWindow({
                    width: "600px",
                    height: "600px",
                    minHeight: "300px",
                    minWidth: "300px",
                    title: "{title} bewerken",
                    modal: true,
                    actions: ["Minimize", "Maximize"],
                    open: (openEvent) => {
                        kendoComponent.refresh();
                        setTimeout(function () {
                            resizeEditor(editorWindow.element.outerHeight());
                        }, 50);
                    },
                    resize: (resizeEvent) => {
                        resizeEditor(resizeEvent.height);
                    },
                    close: (closeEvent) => {
                        const wrapper = closeEvent.sender.wrapper;
                        closeEvent.sender.destroy();
                        wrapper.remove();
                    }
                }).data("kendoWindow");

                saveDialogButton.kendoButton({
                    click: (closeDialogEvent) => {
                        if (!editorWindow) {
                            return;
                        }

                        defaultField.val(kendoComponent.value());
                        editorWindow.close();
                    }
                });

                closeDialogButton.kendoButton({
                    click: (closeDialogEvent) => {
                        if (!editorWindow) {
                            return;
                        }

                        kendoComponent.value(defaultField.val());
                        editorWindow.close();
                    }
                });

                editorWindow.center().open();
            }
        });
    }

    {customScript}
})();
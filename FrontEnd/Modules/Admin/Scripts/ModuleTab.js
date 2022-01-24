export class ModuleTab {
    constructor(base) {
        this.base = base;

        this.setupBindings();
        this.getModules();
    }

    /**
    * Setup all basis bindings for this module.
    * Specific bindings (for buttons in certain pop-ups for example) will be set when they are needed.
    */
    setupBindings() {
        $("#addModuleButton").kendoButton({
            click: () => {
                this.base.openDialog("Module toevoegen", "Voer het nummer van de module in").then((data) => {
                    this.createNewModule(data);
                });

            },
            icon: "plus"
        });
    }

    hideShowComponents(itemClassname, targetElement) {
        let topElement;

        topElement = targetElement.sender.input.closest(".modulebar");

        if (itemClassname === "gridview") {
            const items = topElement[0].querySelectorAll(".gridview");
            $(items).show();
        } else {
            const items = topElement[0].querySelectorAll(".gridview");
            $(items).hide();
        }
    }

    async createNewModule(id) {
        const querystring = {
            moduleId: id
        };

        const results = await $.get(`${this.base.settings.serviceRoot}/CHECK_IF_MODULE_EXISTS${jjl.convert.toQueryString(querystring, true)}`);

        if (results.length > 0) {
            kendo.alert(`De ingevoerde module is al toegevoegd`);
            return;
        }

        const qs = {
            moduleId: id
        };

        let notification;
        qs.add = true;
        notification = "toegevoegd";

        try {
            await $.get(`${this.base.settings.serviceRoot}/INSERT_NEW_MODULE${jjl.convert.toQueryString(qs, true)}`);

            this.base.showNotification("notification", `De nieuwe module is toegevoegd`, "success");
            this.getModules();
        } catch (exception) {
            this.base.showNotification("notification", `De nieuwe module is niet succesvol ${notification}, probeer het opnieuw`, "error");
        }
    }

    /** Initializes all kendo components for the base class. */
    async initializeKendoComponents() {
        this.mainTabStrip = $("#MainTabStrip").kendoTabStrip({
            animation: {
                open: {
                    effects: "expand:vertical",
                    duration: 0
                },
                close: {
                    effects: "expand:vertical",
                    duration: 0
                }
            },
            select: (event) => {
                const tabName = event.item.querySelector(".k-link").innerHTML.toLowerCase();
                console.log("mainTabStrip select", tabName);

                if (tabName === "rollen" || tabName === "modules" || tabName === "entiteiten") {
                    $("footer").hide();
                } else {
                    $("footer").show();
                }
            },
            activate: (event) => {
                const tabName = event.item.querySelector(".k-link").innerHTML.toLowerCase();
                console.log("mainTabStrip activate", tabName);
            }
        }).data("kendoTabStrip");

        this.modeSelect = $(".combo-select").kendoComboBox({
            select: (element) => {
                var currentValue = element.dataItem.value;

                this.hideShowComponents(currentValue, element);
            }
        }).data("kendoComboBox");

        $(".recordsPerPage").kendoNumericTextBox({
            decimals: 0,
            format: "#",
            min: 0,
            step: 1
        }).data("kendoNumericTextBox");
        var me = this;
        this.panelbar = $(".panelbar").kendoPanelBar({
            expandMode: "single",
            activate: async (element) => {
                const targetElement = element.item;
                const moduleId = targetElement.dataset.moduleId;
                const moduleType = targetElement.dataset.moduleType;

                let fieldsJson = {};

                if (moduleType === "gridview") {
                    const result = await $.get(`${this.base.settings.serviceRoot}/GET_MODULE_FIELDS?moduleId=${encodeURIComponent(moduleId)}`);

                    fieldsJson = JSON.parse(result[0].fields);
                }

                this.fieldsGrid = $(targetElement.querySelector(".fieldsGrid")).kendoGrid({
                    columns: [{
                        field: "field",
                        title: "Veld"
                    }, {
                        field: "title",
                        title: "Titel"
                    }, {
                        field: "width",
                        title: "Breedte"
                    }, {
                        field: "filterable",
                        title: "Filterable"
                    }],
                    dataSource: fieldsJson,
                    toolbar: [{ name: "create", text: "Veld toevoegen" }],
                    resizable: false,
                    editable: {
                        createAt: "bottom"
                    }
                });

                $(targetElement).find(".CodeMirror").remove();
                this.codeMirrorCustomQuery = CodeMirror.fromTextArea(targetElement.querySelector("textarea.customQueryBuilder"), {
                    mode: "text/x-mysql",
                    lineNumbers: true
                });
                this.codeMirrorCountUQuery = CodeMirror.fromTextArea(targetElement.querySelector("textarea.countQueryBuilder"), {
                    mode: "text/x-mysql",
                    lineNumbers: true
                });
            },
            expand: function (e) {
                const targetElement = e.item;
                if (targetElement.dataset.isValidJson === "0" && targetElement.dataset.moduleType === "gridview") {
                    e.preventDefault();
                    me.panelbar.enable(targetElement, false);
                    me.base.showNotification("notification", `Het lijkt er op dat de module niet correct is ingericht, neem contact op met ons.`, "error");
                    return;
                }
            }
        }).data("kendoPanelBar");

        $(".modulebar").each((index, element) => {
            const moduleType = element.dataset.moduleType;
            const items = element.querySelectorAll(".gridview");

            $(items).toggle(moduleType === "gridview")
        });

        $(".saveModuleSettings").kendoButton({
            click: (element) => {
                const moduleElement = element.sender.element.closest(".modulebar")[0];
                const moduleId = moduleElement.dataset.moduleId;

                this.saveModuleSettings(moduleId, moduleElement);
            },
            icon: "save"
        });

        $(".deleteModule").kendoButton({
            click: (element) => {
                const moduleElement = element.sender.element.closest(".modulebar")[0];
                const moduleId = moduleElement.dataset.moduleId;

                this.base.openDialog("Module verwijderen", "Weet u zeker dat u de module wilt verwijderen?", this.base.kendoPromptType.CONFIRM).then(() => {
                    this.deleteModule(moduleId);
                });
            },
            icon: "delete"
        });
    }

    async deleteModule(moduleId) {
        const results = await $.get(`${this.base.settings.serviceRoot}/DELETE_MODULE?moduleId=${encodeURIComponent(moduleId)}`);

        this.getModules();
    }

    async saveModuleSettings(module, moduleElement) {
        const customQuery = this.codeMirrorCustomQuery.getValue();
        const countQuery = this.codeMirrorCountUQuery.getValue();
        const moduleType = moduleElement.querySelector("input.combo-select").value.toLowerCase();

        let dataToSend = "";
        if (moduleType === "gridview") {
            const pageSize = moduleElement.querySelector("input.recordsPerPage").value;
            const hideCommandColumn = $(moduleElement.querySelector("input.clickOption")).is(":checked");
            const hideCreateButton = $(moduleElement.querySelector("input.addNewItemOption")).is(":checked");
            const kendoGridColumns = $(this.fieldsGrid[0]).data("kendoGrid").dataSource.view();

            dataToSend = {
                trace: false,
                options: JSON.stringify({
                    gridViewMode: true,
                    gridViewSettings: {
                        pageSize: parseInt(pageSize),
                        hideCommandColumn: hideCommandColumn,
                        toolbar: {
                            hideCreateButton: hideCreateButton
                        },
                        columns: kendoGridColumns
                    }
                }),
                customQuery: customQuery,
                countQuery: countQuery,
                moduleId: module,
                moduleType: moduleType
            };
        } else {
            dataToSend = {
                options: "",
                customQuery: customQuery,
                countQuery: countQuery,
                moduleId: module,
                moduleType: moduleType
            };
        }
        const result = await $.ajax({
            url: `${this.base.settings.serviceRoot}/SAVE_MODULE_SETTINGS`,
            method: "POST",
            data: dataToSend
        });

        if (result.success) {
            this.base.showNotification("notification", `De module instellingen zijn successvol opgeslagen`, "success");
        } else {
            this.base.showNotification("notification", `De instellingen kunnen niet worden opgeslagen, probeer het nogmaals`, "error");
        }
    }

    /** Get the modules */
    async getModules() {
        const results = $.get(`${this.base.settings.serviceRoot}/GET_ALL_MODULES_INFORMATION`);

        const templateContent = $("#myTemplate").html();
        const template = kendo.template(templateContent);
        const templateResult = kendo.render(template, results);

        $("#moduleList").html(templateResult);

        this.initializeKendoComponents();

    }
}
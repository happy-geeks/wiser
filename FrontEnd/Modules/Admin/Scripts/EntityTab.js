import {EntityPropertyModel} from "../Scripts/EntityPropertyModel.js";
import {EntityModel} from "../Scripts/EntityModel.js";
import {Utils} from "../../Base/Scripts/Utils.js";

export class EntityTab {
    constructor(base) {
        this.base = base;
        this.selectedEntityType = null;
        this.selectedEntityProperty = null;
        this.setupBindings();
        this.initializeKendoComponents();
        // init hide/show elements
        this.hideShowElementsBasedOnValue();
        this.fieldOptions = {};
        this.previouslySelectedEntity = null;
        this.previouslySelectedTab = null;
    }

    checkIfEntityIsSet() {
        // Only make this check if the fields tab is selected.
        const selectedTab = this.entityTabStrip.select().text();
        if (selectedTab !== "Velden") {
            return true;
        }
        
        if ((!this.entitiesCombobox || !this.entitiesCombobox.dataItem() || this.entitiesCombobox.dataItem().id === "") && this.entityListInitialized === true) {
            this.base.showNotification("notification", `Selecteer eerst een entiteit!`, "error");
            return false;
        }
        
        return true;
    }

    /**
     * Setup all basis bindings for this module.
     * Specific bindings (for buttons in certain pop-ups for example) will be set when they are needed.
     */
    async setupBindings() {
        // add an entity property
        $(".addBtn").kendoButton({
            click: (e) => {
                const addType = e.sender.element[0].dataset.type;
                if (!addType || !this.checkIfEntityIsSet(addType)) {
                    return;
                }

                if (addType === "entityProperty") {
                    this.base.openDialog("Nieuw veld toevoegen", "Voer de naam in van het veld (met komma's kunnen meerdere velden tegelijk aangemaakt worden).").then((data) => {
                        this.addEntityProperty(data);
                    });
                } else if (addType === "entity") {
                    this.base.openDialog("Nieuwe entiteit toevoegen", "Voer de naam in van entiteit.").then((data) => {
                        this.addEntity(data);
                    });
                }
            },
            icon: "file"
        });

        // delete an entity property
        $(".delBtn").kendoButton({
            click: (event) => {
                const addType = event.sender.element[0].dataset.type;
                if (!addType || !this.checkIfEntityIsSet(addType)) {
                    return;
                }
                
                if (addType === "entityProperty") {
                    const tabNameProp = this.listOfTabProperties;
                    const index = tabNameProp.select().index();
                    const dataItem = tabNameProp.dataSource.view()[index];
                    if (!dataItem) {
                        this.base.showNotification("notification", "Kies a.u.b. eerst een veld om te verwijderen", "error");
                        return;
                    }
                    
                    Wiser.showConfirmDialog(`Weet u zeker dat u het veld "${dataItem.displayName}" wilt verwijderen?`).then(() => {
                        this.removeEntityProperty(dataItem.id);
                    });
                } else if (addType === "entity") {
                    const selectedEntity = this.entitiesCombobox.dataItem();
                    if (!selectedEntity) {
                        this.base.showNotification("notification", "Kies a.u.b. eerst een entiteit om te verwijderen", "error");
                        return;
                    }
                    
                    Wiser.showConfirmDialog(`Weet u zeker dat u de entiteit "${selectedEntity.name}" wilt verwijderen?`).then(() => {
                        this.removeEntity(selectedEntity.id);
                    });
                }
            },
            icon: "delete"
        });
        
        $(".copyToOtherLanguagesButton").kendoButton({
            click: () => {
                const index = this.listOfTabProperties.select().index();
                const dataItem = this.listOfTabProperties.dataSource.view()[index];
                if (!dataItem) {
                    return;
                }

                $("<div id='copyEntityPropertyToOtherLanguagesDialog'></div>").kendoDialog({
                    width: "550px",
                    title: "Kopieren naar andere talen",
                    closable: true,
                    modal: true,
                    content: "<p>Wilt u de nieuwe velden toevoegen aan de 'Gegevens' tab, of een tab per taal maken?<p>",
                    actions: [
                        { text: "Gegevens tab", primary: true, action: () => { this.copyEntityPropertyToOtherLanguages(dataItem.id, 0) } },
                        { text: "Tab per taal (taalcode)", primary: true, action: () => { this.copyEntityPropertyToOtherLanguages(dataItem.id, 1) } },
                        { text: "Tab per taal (taalnaam)", primary: true, action: () => { this.copyEntityPropertyToOtherLanguages(dataItem.id, 2) } }
                    ],
                }).data("kendoDialog").open();
            },
            icon: "globe"
        });
        
        $(".duplicateEntityPropertyButton").kendoButton({
            click: () => {
                const index = this.listOfTabProperties.select().index();
                const dataItem = this.listOfTabProperties.dataSource.view()[index];
                if (!dataItem) {
                    return;
                }

                this.base.openDialog("Veld dupliceren", "Voer de naam in van het nieuwe veld (vul kommagescheiden meerdere namen in om het veld meerdere keren te dupliceren).").then((data) => {
                    this.duplicateEntityProperty(dataItem.id, data);
                });
            },
            icon: "copy"
        });

        await Misc.ensureCodeMirror();

        this.scriptField = CodeMirror.fromTextArea(document.getElementById("customScriptField"), {
            mode: "text/javascript",
            lineNumbers: true
        });

        this.optionsJsonField = CodeMirror.fromTextArea(document.getElementById("optionsJson"), {
            mode: "application/json",
            lineNumbers: true
        });

        this.queryField = CodeMirror.fromTextArea(document.getElementById("queryWindow"), {
            mode: "text/x-mysql",
            lineNumbers: true
        });

        this.queryFieldSubEntities = CodeMirror.fromTextArea(document.getElementById("queryFieldSubEntities"), {
            mode: "text/x-mysql",
            lineNumbers: true
        });

        this.queryDeleteField = CodeMirror.fromTextArea(document.getElementById("queryDelete"), {
            mode: "text/x-mysql",
            lineNumbers: true
        });

        this.queryInsertField = CodeMirror.fromTextArea(document.getElementById("queryInsert"), {
            mode: "text/x-mysql",
            lineNumbers: true
        });

        this.queryUpdateField = CodeMirror.fromTextArea(document.getElementById("queryUpdate"), {
            mode: "text/x-mysql",
            lineNumbers: true
        });

        this.searchQueryField = CodeMirror.fromTextArea(document.getElementById("searchQuery"), {
            mode: "text/x-mysql",
            lineNumbers: true
        });

        this.searchCountQueryField = CodeMirror.fromTextArea(document.getElementById("searchCountQuery"), {
            mode: "text/x-mysql",
            lineNumbers: true
        });

        this.aggregateOptionsField = CodeMirror.fromTextArea(document.getElementById("aggregateOptions"), {
            mode: "application/json",
            lineNumbers: true
        });

        this.queryContentField = CodeMirror.fromTextArea(document.getElementById("queryContent"), {
            mode: "text/x-mysql",
            lineNumbers: true
        });

        this.queryAfterInsert = CodeMirror.fromTextArea(document.getElementById("queryAfterInsert"), {
            mode: "text/x-mysql",
            lineNumbers: true
        });

        this.queryAfterUpdate = CodeMirror.fromTextArea(document.getElementById("queryAfterUpdate"), {
            mode: "text/x-mysql",
            lineNumbers: true
        });

        this.queryBeforeUpdate = CodeMirror.fromTextArea(document.getElementById("queryBeforeUpdate"), {
            mode: "text/x-mysql",
            lineNumbers: true
        });

        this.queryBeforeDelete = CodeMirror.fromTextArea(document.getElementById("queryBeforeDelete"), {
            mode: "text/x-mysql",
            lineNumbers: true
        });

        this.templateQueryField = CodeMirror.fromTextArea(document.getElementById("templateQuery"), {
            mode: "text/x-mysql",
            lineNumbers: true
        });

        this.templateHtmlField = CodeMirror.fromTextArea(document.getElementById("templateHtml"), {
            mode: "text/html",
            lineNumbers: true
        });

        document.getElementById("hasCustomInsertQuery").addEventListener("change", (e) => {
            const element = document.querySelector(".customInsert");
            if (e.target.checked) {
                element.style.display = "block";
            } else {
                element.style.display = "none";
            }
        });

        document.getElementById("hasCustomUpdateQuery").addEventListener("change", (e) => {
            const element = document.querySelector(".customUpdate");
            if (e.target.checked) {
                element.style.display = "block";
            } else {
                element.style.display = "none";
            }
        });

        document.getElementById("hasCustomDeleteQuery").addEventListener("change", (e) => {
            const element = document.querySelector(".customDelete");
            if (e.target.checked) {
                element.style.display = "block";
            } else {
                element.style.display = "none";
            }
        });

        document.getElementById("customQuery").addEventListener("change", (e) => {
            const element = document.querySelector(".customQuery");
            if (e.target.checked) {
                element.style.display = "block";
                // abuse dataSourceFilter select
                this.dataSourceFilter.select(2);
            } else {
                element.style.display = "none";
                // default
                this.dataSourceFilter.select(0);
            }
        });
    }

    // adding an entity function
    async addEntity(name = "") {
        if (!name) {
            return;
        }

        try {
            const createResult = await Wiser.api({
                url: `${this.base.settings.wiserApiRoot}entity-types?name=${encodeURIComponent(name)}`,
                contentType: 'application/json',
                method: "POST"
            });

            this.base.showNotification("notification", `Entiteit succesvol toegevoegd`, "success");
            await this.reloadEntityList(true);

            this.entitiesCombobox.one("dataBound", () => {
                this.entitiesCombobox.select((dataItem) => {
                    return dataItem.name === name;
                });
            });
            
            // Select the entity tab again after creating a new entity.
            this.entityTabStrip.select(0);
        } catch (exception) {
            console.error(exception);
            this.base.showNotification("notification", `Entiteit is niet succesvol toegevoegd, probeer het opnieuw`, "error");
        }
    }

    /**
     * Create one or more new entity properties.
     * @param name The names of the properties to create. You can create more by separating them with commas.
     */
    async addEntityProperty(name) {
        if (!name) {
            return;
        }

        try {
            const names = name.split(",");
            if (!names.length) {
                return;
            }
            
            const promises = [];
            for (let actualName of names) {
                actualName = actualName.trim();
                
                const queryString = {
                    entityName: this.entitiesCombobox.dataItem().name,
                    tabName: this.tabNameDropDownList.value() === "Gegevens" ? "" : this.tabNameDropDownList.value(),
                    displayName: actualName,
                    propertyName: actualName
                };

                promises.push(Wiser.api({
                    url: `${this.base.settings.serviceRoot}/INSERT_ENTITYPROPERTY${Utils.toQueryString(queryString, true)}`,
                    method: "GET"
                }));
            }
            
            await Promise.all(promises);

            this.listOfTabProperties.one("dataBound", () => {
                // select created item, except if tit is the only one.
                this.selectPropertyInListView(names[0]);
            });

            // if we have no items yet, and no data item of the tabname combobox. refresh entities combobox so the first tab will automatically be selected
            if (!this.tabNameDropDownList.dataItem()) {
                // reset tab names if we didnt have any before
                await this.onEntitiesComboBoxSelect(this);
            } else {
                // select the right tab
                await this.tabNameDropDownListSelect(this.tabNameDropDownList.dataItem(), true);
            }
        }
        catch (exception) {
            console.error("Error while trying to delete an entity property", exception);
            this.base.showNotification("notification", `Veld is niet succesvol aangemaakt, probeer het opnieuw`, "error");
        }
    }

    /**
     * Delete an entity property.
     * @param id The ID of the property to delete.
     */
    async removeEntityProperty(id) {
        if (!id) {
            return;
        }
        
        let queryString = {
            entityName: this.entitiesCombobox.dataItem().name,
            tabName: this.tabNameDropDownList.value() === "Gegevens" ? "" : this.tabNameDropDownList.value(),
            displayName: name,
            entityPropertyId: id
        };

        try {
            await Wiser.api({
                url: `${this.base.settings.serviceRoot}/DELETE_ENTITYPROPERTY${Utils.toQueryString(queryString, true)}`,
                method: "GET"
            });
            
            this.base.showNotification("notification", `Veld succesvol verwijderd`, "success");
            await this.tabNameDropDownListSelect(this.tabNameDropDownList.dataItem(), true);

            // Select first item in list
            const firstElement = this.listOfTabProperties.element.find("[data-item]").first();
            this.listOfTabProperties.one("dataBound", () => {
                this.selectPropertyInListView(firstElement.data("displayName"));
            });
        }
        catch (exception) {
            console.error("Error while trying to delete an entity property", exception);
            this.base.showNotification("notification", `Veld is niet succesvol verwijderd, probeer het opnieuw`, "error");
        }
    }

    /**
     * Deletes an entity and all it's properties from the database.
     * @param id The ID of the entity to delete.
     */
    async removeEntity(id) {
        try {
            await Wiser.api({
                url: `${this.base.settings.wiserApiRoot}entity-types/${id}`,
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                method: "DELETE"
            });

            this.base.showNotification("notification", `Entiteit is succesvol verwijderd`, "success");
            await this.reloadEntityList(true);
            this.entityTabStrip.wrapper.hide();
        } catch(exception) {
            console.error(exception);
            this.base.showNotification("notification", `Er is iets fout gegaan met het verwijderen van de module. Probeer het a.u.b. opnieuw of neem contact op met ons.`, "error");
        }
    }

    /**
     * Copy an entity property to other languages.
     * @param id The ID of the entity property.
     * @param tabOption The option for where to copy it to (0 = to the general tab, 1 = to create a tab per language and use the language code for the name of the tab, 2 = to create a tab per language and use the language name for the name of the tab).
     */
    async copyEntityPropertyToOtherLanguages(id, tabOption) {
        if (!id) {
            return;
        }

        try {
            const selectedTab = this.tabNameDropDownList.dataItem();
            
            // Do the copy action.
            await Wiser.api({
                url: `${this.base.settings.wiserApiRoot}entity-properties/${id}/copy-to-other-languages?tabOption=${tabOption}`,
                method: "POST"
            });

            // Re load everything, so the new entity properties will become visible.
            await this.onEntitiesComboBoxSelect(this);
            // Select the same tab as before.
            this.tabNameDropDownList.value(selectedTab.tabName);
            this.base.showNotification("notification", `Veld is succesvol gekopieerd naar alle andere talen`, "success");
        }
        catch (exception) {
            console.error("Error while trying to copy an entity property to all languages", exception);
            this.base.showNotification("notification", `Item is niet succesvol ${notification}, probeer het opnieuw`, "error");
        }
    }

    /**
     * Duplicate an entity property with a new name.
     * @param id The ID of the entity property to duplicate.
     * @param name The name(s) for the new entity property (comma separated for multiple names).
     */
    async duplicateEntityProperty(id, name) {
        if (!id || !name) {
            return;
        }

        try {
            const names = name.split(",");
            if (!names.length) {
                return;
            }

            const promises = [];
            for (let actualName of names) {
                actualName = actualName.trim();

                promises.push(Wiser.api({
                    url: `${this.base.settings.wiserApiRoot}entity-properties/${id}/duplicate`,
                    method: "POST",
                    contentType: "application/json",
                    data: JSON.stringify(actualName)
                }));
            }

            await Promise.all(promises);

            this.listOfTabProperties.one("dataBound", () => {
                // select created item, except if tit is the only one.
                this.selectPropertyInListView(names[0]);
            });

            // if we have no items yet, and no data item of the tabname combobox. refresh entities combobox so the first tab will automatically be selected
            if (!this.tabNameDropDownList.dataItem()) {
                // reset tab names if we didnt have any before
                await this.onEntitiesComboBoxSelect(this);
            } else {
                // select the right tab
                await this.tabNameDropDownListSelect(this.tabNameDropDownList.dataItem(), true);
            }
        }
        catch (exception) {
            console.error("Error while trying to duplicate an entity property", exception);
            this.base.showNotification("notification", `Veld is niet succesvol gedupliceerd, probeer het opnieuw`, "error");
        }
    }

    setEntityLists() {
        this.entitiesCombobox.setDataSource(this.base.entityList);
        this.dataSourceEntities.setDataSource(this.base.allUniqueEntityTypes);
        this.linkedItemEntity.setDataSource(this.base.allUniqueEntityTypes);
        this.itemLinkerEntity.setDataSource(this.base.allUniqueEntityTypes);
        this.subEntityGridEntity.setDataSource(this.base.allUniqueEntityTypes);
        this.timelineEntity.setDataSource(this.base.allUniqueEntityTypes);
    }

    async reloadEntityList(reloadDataSource = false) {
        if (reloadDataSource) {
            // These are all entities, including duplicate ones that have the same name in different modules.
            try {
                this.base.entityList = (await Wiser.api({url: `${this.base.settings.serviceRoot}/GET_ENTITY_LIST`})) || [];
            } catch (exception) {
                console.error("Error occurred while trying to load all entity types 1", exception);
                this.base.entityList = [];
            }
            
            // These are all entities grouped by name.
            try {
                this.base.allUniqueEntityTypes = (await Wiser.api({url: `${this.base.settings.wiserApiRoot}entity-types?onlyEntityTypesWithDisplayName=false`})) || [];
            } catch (exception) {
                console.error("Error occurred while trying to load all entity types 2", exception);
                this.base.allUniqueEntityTypes = [];
            }
        }
        
        this.setEntityLists();
    }

    /**
     * Initializes all kendo components for the base class.
     */
    async initializeKendoComponents() {
        this.entityTabStrip = $("#EntityTabStrip").kendoTabStrip({
            animation: {
                open: {
                    effects: "fade",
                    duration: 0
                },
                close: {
                    effects: "fade",
                    duration: 0
                }
            },
            select: (event) => {
                const tabName = event.item.querySelector(".k-link").innerHTML.toLowerCase();
                switch (tabName) {
                    case "velden":
                        if (!this.checkIfEntityIsSet())
                            event.preventDefault();
                        else {
                            $("#entityPropertyView").show();
                        }
                        break;
                    default:
                        $("#entityPropertyView").hide();
                        $("#entityView").show();
                        break;
                }
            },
            activate: (event) => {
                const tabName = event.item.querySelector(".k-link").innerHTML.toLowerCase();
                
                if (tabName === "eigenschappen") {
                    // Refresh code mirrors, otherwise they won't work properly because they were invisible when they were initialized.
                    this.queryAfterInsert.refresh();
                    this.queryAfterUpdate.refresh();
                    this.queryBeforeUpdate.refresh();
                    this.queryBeforeDelete.refresh();
                    this.searchQueryField.refresh();
                    this.searchCountQueryField.refresh();
                    this.templateQueryField.refresh();
                    this.templateHtmlField.refresh();
                }
            }
        }).data("kendoTabStrip");

        // Hide the tabstrip initially, show it when the user selects an entity.
        this.entityTabStrip.wrapper.hide();
        $("#EntityTabStrip-2 .right-pane").hide();

        //NUBERIC FIELDS
        this.widthInTable = $("#widthIfVisible").kendoNumericTextBox({
            decimals: 0,
            format: "# px"
        }).data("kendoNumericTextBox");

        this.width = $("#width").kendoNumericTextBox({
            decimals: 0,
            format: "# \\%"
        }).data("kendoNumericTextBox");

        this.height = $("#height").kendoNumericTextBox({
            decimals: 0,
            format: "# px"
        }).data("kendoNumericTextBox");

        this.numberOfDec = $("#numberOfDecimals").kendoNumericTextBox({
            decimals: 0,
            format: "#",
            min: 0,
            max: 100,
            step: 1
        }).data("kendoNumericTextBox");

        this.defaultNumeric = $("#defaultNumeric").kendoNumericTextBox({
            decimals: 0,
            min: 0,
            format: "#"
        }).data("kendoNumericTextBox");

        this.maxNumber = $("#maxNumber").kendoNumericTextBox({
            decimals: 0,
            format: "#"
        }).data("kendoNumericTextBox");

        this.minNumber = $("#minNumber").kendoNumericTextBox({
            decimals: 0,
            format: "#"
        }).data("kendoNumericTextBox");

        this.stepNumber = $("#stepNumber").kendoNumericTextBox({
            decimals: 0,
            format: "#"
        }).data("kendoNumericTextBox");

        this.factorNumber = $("#factorNumber").kendoNumericTextBox({
            decimals: 0,
            format: "#"
        }).data("kendoNumericTextBox");
        
        this.checkBoxImageId = $("#checkBoxImageId").kendoNumericTextBox({
            decimals: 0,
            format: "#"
        }).data("kendoNumericTextBox");

        this.multiSelectMainImageId = $("#multiSelectMainImageId").kendoNumericTextBox({
            decimals: 0,
            format: "#"
        }).data("kendoNumericTextBox");

        this.lastSelectedProperty = -1;
        // we use this property to check if the select in the tabname properties listview is by editing 
        // an item and it gets reloaded when you save it, or when a user manually selects it.
        this.isSaveSelect = false;
        //LISTVIEWS
        this.listOfTabProperties = $("#tabNameProperties").kendoListView({
            template: '<li class="sortable" data-item="${id}" data-ordering="${ordering}" data-display-name="${displayName}">${displayName}</li>',
            dataTextField: "displayName",
            dataValueField: "id",
            selectable: true,
            change: this.optionSelected.bind(this)
        }).data("kendoListView");

        // entity module
        this.entityModule = $("#entityModule").kendoDropDownList({
            placeholder: "Selecteer een module...",
            filter: "contains",
            clearButton: false,
            dataTextField: "moduleName",
            dataValueField: "id",
            dataSource: []
        }).data("kendoDropDownList");


        this.acceptedChildTypes = $("#acceptedChildTypes").kendoMultiSelect({
            placeholder: "Selecteer entiteit(en)...",
            clearButton: false,
            filter: "contains",
            multiple: "multiple",
            dataTextField: "entityType",
            dataValueField: "entityType",
            dataSource: []
        }).data("kendoMultiSelect");

        /* 
            This list is generated by copying the contents of icons.css to Notepad++ 
            and then using find & replace with the following regex: \.(icon-([a-z\-0-9A-Z]+)):before { content: "\\e.+ 
            and the following "replace with" value: { text: "$2", value: "$1" },
         */
        const iconsDataSource = [
            { text: "add", value: "icon-add" },
            { text: "admin", value: "icon-admin" },
            { text: "affiliate", value: "icon-affiliate" },
            { text: "agenda", value: "icon-agenda" },
            { text: "album", value: "icon-album" },
            { text: "album-add", value: "icon-album-add" },
            { text: "album-delete", value: "icon-album-delete" },
            { text: "alert", value: "icon-alert" },
            { text: "announce", value: "icon-announce" },
            { text: "apply", value: "icon-apply" },
            { text: "arrow-back", value: "icon-arrow-back" },
            { text: "arrow-down", value: "icon-arrow-down" },
            { text: "arrow-forward", value: "icon-arrow-forward" },
            { text: "arrow-left", value: "icon-arrow-left" },
            { text: "arrow-right", value: "icon-arrow-right" },
            { text: "arrow-up", value: "icon-arrow-up" },
            { text: "asana", value: "icon-asana" },
            { text: "auction", value: "icon-auction" },
            { text: "bed", value: "icon-bed" },
            { text: "bell", value: "icon-bell" },
            { text: "binoculars", value: "icon-binoculars" },
            { text: "book", value: "icon-book" },
            { text: "book-group", value: "icon-book-group" },
            { text: "box", value: "icon-box" },
            { text: "box-link", value: "icon-box-link" },
            { text: "brands", value: "icon-brands" },
            { text: "building", value: "icon-building" },
            { text: "business-man", value: "icon-business-man" },
            { text: "calendar", value: "icon-calendar" },
            { text: "calendar-tool", value: "icon-calendar-tool" },
            { text: "camera", value: "icon-camera" },
            { text: "cancel", value: "icon-cancel" },
            { text: "car", value: "icon-car" },
            { text: "cart", value: "icon-cart" },
            { text: "chat-price", value: "icon-chat-price" },
            { text: "chat-user", value: "icon-chat-user" },
            { text: "check", value: "icon-check" },
            { text: "checkbox", value: "icon-checkbox" },
            { text: "chevron-down", value: "icon-chevron-down" },
            { text: "chevron-left", value: "icon-chevron-left" },
            { text: "chevron-right", value: "icon-chevron-right" },
            { text: "chevron-up", value: "icon-chevron-up" },
            { text: "choices", value: "icon-choices" },
            { text: "classroom", value: "icon-classroom" },
            { text: "client-leads", value: "icon-client-leads" },
            { text: "client-prospects", value: "icon-client-prospects" },
            { text: "client-suspects", value: "icon-client-suspects" },
            { text: "clipboard", value: "icon-clipboard" },
            { text: "clock", value: "icon-clock" },
            { text: "close", value: "icon-close" },
            { text: "cloud", value: "icon-cloud" },
            { text: "cloud-down", value: "icon-cloud-down" },
            { text: "cloud-up", value: "icon-cloud-up" },
            { text: "colors", value: "icon-colors" },
            { text: "combination", value: "icon-combination" },
            { text: "communication", value: "icon-communication" },
            { text: "cone", value: "icon-cone" },
            { text: "config", value: "icon-config" },
            { text: "controls", value: "icon-controls" },
            { text: "creditcard", value: "icon-creditcard" },
            { text: "database", value: "icon-database" },
            { text: "date", value: "icon-date" },
            { text: "date-course", value: "icon-date-course" },
            { text: "delete", value: "icon-delete" },
            { text: "desktop", value: "icon-desktop" },
            { text: "dialog-close", value: "icon-dialog-close" },
            { text: "dialog-enlarge", value: "icon-dialog-enlarge" },
            { text: "dialog-minimize", value: "icon-dialog-minimize" },
            { text: "directions", value: "icon-directions" },
            { text: "directions-temp", value: "icon-directions-temp" },
            { text: "discount", value: "icon-discount" },
            { text: "doc", value: "icon-doc" },
            { text: "doc-add", value: "icon-doc-add" },
            { text: "doc-invoice", value: "icon-doc-invoice" },
            { text: "doc-subscription", value: "icon-doc-subscription" },
            { text: "document", value: "icon-document" },
            { text: "document-add", value: "icon-document-add" },
            { text: "document-delete", value: "icon-document-delete" },
            { text: "document-duplicate", value: "icon-document-duplicate" },
            { text: "document-edit", value: "icon-document-edit" },
            { text: "document-exam", value: "icon-document-exam" },
            { text: "document-export", value: "icon-document-export" },
            { text: "document-flat", value: "icon-document-flat" },
            { text: "document-fold", value: "icon-document-fold" },
            { text: "document-hide", value: "icon-document-hide" },
            { text: "document-import", value: "icon-document-import" },
            { text: "document-pdf", value: "icon-document-pdf" },
            { text: "document-web", value: "icon-document-web" },
            { text: "document-xml", value: "icon-document-xml" },
            { text: "domain", value: "icon-domain" },
            { text: "downloaden", value: "icon-downloaden" },
            { text: "dress", value: "icon-dress" },
            { text: "dynamic", value: "icon-dynamic" },
            { text: "euro", value: "icon-euro" },
            { text: "eye-invisible", value: "icon-eye-invisible" },
            { text: "eye-visible", value: "icon-eye-visible" },
            { text: "facebook", value: "icon-facebook" },
            { text: "factory", value: "icon-factory" },
            { text: "filter", value: "icon-filter" },
            { text: "flag", value: "icon-flag" },
            { text: "flag-2", value: "icon-flag-2" },
            { text: "folder", value: "icon-folder" },
            { text: "folder-add", value: "icon-folder-add" },
            { text: "folder-check", value: "icon-folder-check" },
            { text: "folder-closed", value: "icon-folder-closed" },
            { text: "folder-delete", value: "icon-folder-delete" },
            { text: "folder-duplicate", value: "icon-folder-duplicate" },
            { text: "folder-edit", value: "icon-folder-edit" },
            { text: "folder-hide", value: "icon-folder-hide" },
            { text: "folder-hide-2", value: "icon-folder-hide-2" },
            { text: "folder-search", value: "icon-folder-search" },
            { text: "forklift", value: "icon-forklift" },
            { text: "games", value: "icon-games" },
            { text: "git", value: "icon-git" },
            { text: "globe", value: "icon-globe" },
            { text: "golf-clinic", value: "icon-golf-clinic" },
            { text: "golf-course", value: "icon-golf-course" },
            { text: "golf-vacation", value: "icon-golf-vacation" },
            { text: "info", value: "icon-info" },
            { text: "info-full", value: "icon-info-full" },
            { text: "pricelabel", value: "icon-pricelabel" },
            { text: "light-off", value: "icon-light-off" },
            { text: "light-on", value: "icon-light-on" },
            { text: "line-alert", value: "icon-line-alert" },
            { text: "line-arrow-down", value: "icon-line-arrow-down" },
            { text: "line-arrow-left", value: "icon-line-arrow-left" },
            { text: "line-arrow-right", value: "icon-line-arrow-right" },
            { text: "line-arrow-up", value: "icon-line-arrow-up" },
            { text: "line-book", value: "icon-line-book" },
            { text: "line-bug", value: "icon-line-bug" },
            { text: "line-calendar", value: "icon-line-calendar" },
            { text: "line-cart", value: "icon-line-cart" },
            { text: "line-chart", value: "icon-line-chart" },
            { text: "line-chart-seo", value: "icon-line-chart-seo" },
            { text: "line-chevron-down", value: "icon-line-chevron-down" },
            { text: "line-chevron-left", value: "icon-line-chevron-left" },
            { text: "line-chevron-right", value: "icon-line-chevron-right" },
            { text: "line-chevron-up", value: "icon-line-chevron-up" },
            { text: "line-chrome", value: "icon-line-chrome" },
            { text: "line-clipboard", value: "icon-line-clipboard" },
            { text: "line-clock", value: "icon-line-clock" },
            { text: "line-close", value: "icon-line-close" },
            { text: "line-code", value: "icon-line-code" },
            { text: "line-credit-card", value: "icon-line-credit-card" },
            { text: "line-database", value: "icon-line-database" },
            { text: "line-database-search", value: "icon-line-database-search" },
            { text: "line-document-add", value: "icon-line-document-add" },
            { text: "line-download", value: "icon-line-download" },
            { text: "line-download-cloud", value: "icon-line-download-cloud" },
            { text: "line-edit", value: "icon-line-edit" },
            { text: "line-exit", value: "icon-line-exit" },
            { text: "line-export", value: "icon-line-export" },
            { text: "line-file", value: "icon-line-file" },
            { text: "line-file-minus", value: "icon-line-file-minus" },
            { text: "line-file-plus", value: "icon-line-file-plus" },
            { text: "line-file-text", value: "icon-line-file-text" },
            { text: "line-filter", value: "icon-line-filter" },
            { text: "line-flag", value: "icon-line-flag" },
            { text: "line-folder", value: "icon-line-folder" },
            { text: "line-folder-minus", value: "icon-line-folder-minus" },
            { text: "line-folder-plus", value: "icon-line-folder-plus" },
            { text: "line-globe", value: "icon-line-globe" },
            { text: "line-heart", value: "icon-line-heart" },
            { text: "line-home", value: "icon-line-home" },
            { text: "line-image", value: "icon-line-image" },
            { text: "line-im-ex", value: "icon-line-im-ex" },
            { text: "line-import", value: "icon-line-import" },
            { text: "line-info", value: "icon-line-info" },
            { text: "line-lock", value: "icon-line-lock" },
            { text: "line-log-in", value: "icon-line-log-in" },
            { text: "line-log-out", value: "icon-line-log-out" },
            { text: "line-mail", value: "icon-line-mail" },
            { text: "line-menu", value: "icon-line-menu" },
            { text: "line-mic", value: "icon-line-mic" },
            { text: "line-minus", value: "icon-line-minus" },
            { text: "line-monitor", value: "icon-line-monitor" },
            { text: "line-package", value: "icon-line-package" },
            { text: "line-phone", value: "icon-line-phone" },
            { text: "line-phone-call", value: "icon-line-phone-call" },
            { text: "line-picture-add", value: "icon-line-picture-add" },
            { text: "line-pin", value: "icon-line-pin" },
            { text: "line-pin-empty", value: "icon-line-pin-empty" },
            { text: "line-pin-full", value: "icon-line-pin-full" },
            { text: "line-power", value: "icon-line-power" },
            { text: "line-printer", value: "icon-line-printer" },
            { text: "line-refresh", value: "icon-line-refresh" },
            { text: "line-return", value: "icon-line-return" },
            { text: "line-returns", value: "icon-line-returns" },
            { text: "line-scissors", value: "icon-line-scissors" },
            { text: "line-search", value: "icon-line-search" },
            { text: "line-send", value: "icon-line-send" },
            { text: "line-server", value: "icon-line-server" },
            { text: "line-settings", value: "icon-line-settings" },
            { text: "line-share", value: "icon-line-share" },
            { text: "line-shield", value: "icon-line-shield" },
            { text: "line-sliders", value: "icon-line-sliders" },
            { text: "line-thumbs-down", value: "icon-line-thumbs-down" },
            { text: "line-thumbs-up", value: "icon-line-thumbs-up" },
            { text: "line-tool", value: "icon-line-tool" },
            { text: "line-trash", value: "icon-line-trash" },
            { text: "line-truck", value: "icon-line-truck" },
            { text: "line-upload", value: "icon-line-upload" },
            { text: "line-upload-cloud", value: "icon-line-upload-cloud" },
            { text: "line-user", value: "icon-line-user" },
            { text: "line-user-check", value: "icon-line-user-check" },
            { text: "line-user-minus", value: "icon-line-user-minus" },
            { text: "line-user-plus", value: "icon-line-user-plus" },
            { text: "line-users", value: "icon-line-users" },
            { text: "line-user-x", value: "icon-line-user-x" },
            { text: "link", value: "icon-link" },
            { text: "linkedin", value: "icon-linkedin" },
            { text: "link-file", value: "icon-link-file" },
            { text: "link-global", value: "icon-link-global" },
            { text: "list", value: "icon-list" },
            { text: "list-box", value: "icon-list-box" },
            { text: "list-course", value: "icon-list-course" },
            { text: "liveboard", value: "icon-liveboard" },
            { text: "locations", value: "icon-locations" },
            { text: "lock", value: "icon-lock" },
            { text: "log-in", value: "icon-log-in" },
            { text: "log-out", value: "icon-log-out" },
            { text: "mail-forward", value: "icon-mail-forward" },
            { text: "mail-open", value: "icon-mail-open" },
            { text: "man", value: "icon-man" },
            { text: "map", value: "icon-map" },
            { text: "menu", value: "icon-menu" },
            { text: "microphone", value: "icon-microphone" },
            { text: "move", value: "icon-move" },
            { text: "movie", value: "icon-movie" },
            { text: "object", value: "icon-object" },
            { text: "parking", value: "icon-parking" },
            { text: "payment", value: "icon-payment" },
            { text: "pencil", value: "icon-pencil" },
            { text: "people", value: "icon-people" },
            { text: "phone", value: "icon-phone" },
            { text: "picture", value: "icon-picture" },
            { text: "picture-add", value: "icon-picture-add" },
            { text: "picture-delete", value: "icon-picture-delete" },
            { text: "picture-favorite", value: "icon-picture-favorite" },
            { text: "picture-grid", value: "icon-picture-grid" },
            { text: "picture-stack", value: "icon-picture-stack" },
            { text: "plane", value: "icon-plane" },
            { text: "planning", value: "icon-planning" },
            { text: "policeman", value: "icon-policeman" },
            { text: "power", value: "icon-power" },
            { text: "print", value: "icon-print" },
            { text: "project", value: "icon-project" },
            { text: "projects", value: "icon-projects" },
            { text: "push-message", value: "icon-push-message" },
            { text: "puzzle", value: "icon-puzzle" },
            { text: "question", value: "icon-question" },
            { text: "question-2", value: "icon-question-2" },
            { text: "quickmenu", value: "icon-quickmenu" },
            { text: "remove", value: "icon-remove" },
            { text: "rename", value: "icon-rename" },
            { text: "reset", value: "icon-reset" },
            { text: "routes", value: "icon-routes" },
            { text: "save", value: "icon-save" },
            { text: "search", value: "icon-search" },
            { text: "seo", value: "icon-seo" },
            { text: "seo-check", value: "icon-seo-check" },
            { text: "settings", value: "icon-settings" },
            { text: "shop", value: "icon-shop" },
            { text: "sort-asc", value: "icon-sort-asc" },
            { text: "sort-desc", value: "icon-sort-desc" },
            { text: "star-full", value: "icon-star-full" },
            { text: "star-outlined", value: "icon-star-outlined" },
            { text: "stats", value: "icon-stats" },
            { text: "stats-2", value: "icon-stats-2" },
            { text: "stopwatch-pauze", value: "icon-stopwatch-pauze" },
            { text: "stopwatch-start", value: "icon-stopwatch-start" },
            { text: "stopwatch-stop", value: "icon-stopwatch-stop" },
            { text: "student", value: "icon-student" },
            { text: "synonym", value: "icon-synonym" },
            { text: "table", value: "icon-table" },
            { text: "table-add-record", value: "icon-table-add-record" },
            { text: "table-backup", value: "icon-table-backup" },
            { text: "table-delete-record", value: "icon-table-delete-record" },
            { text: "teacher", value: "icon-teacher" },
            { text: "teachers", value: "icon-teachers" },
            { text: "template", value: "icon-template" },
            { text: "tent", value: "icon-tent" },
            { text: "test", value: "icon-test" },
            { text: "theater", value: "icon-theater" },
            { text: "ticket", value: "icon-ticket" },
            { text: "ticket-cart", value: "icon-ticket-cart" },
            { text: "ticket-forward", value: "icon-ticket-forward" },
            { text: "ticket-location", value: "icon-ticket-location" },
            { text: "ticket-time", value: "icon-ticket-time" },
            { text: "time-reset", value: "icon-time-reset" },
            { text: "tool", value: "icon-tool" },
            { text: "tools", value: "icon-tools" },
            { text: "trigger", value: "icon-trigger" },
            { text: "trolley", value: "icon-trolley" },
            { text: "truck", value: "icon-truck" },
            { text: "truck-delivery", value: "icon-truck-delivery" },
            { text: "twitter", value: "icon-twitter" },
            { text: "uploaden", value: "icon-uploaden" },
            { text: "user", value: "icon-user" },
            { text: "user-add", value: "icon-user-add" },
            { text: "user-delete", value: "icon-user-delete" },
            { text: "users", value: "icon-users" },
            { text: "user-status", value: "icon-user-status" },
            { text: "user-time", value: "icon-user-time" },
            { text: "version", value: "icon-version" },
            { text: "views", value: "icon-views" },
            { text: "wiser", value: "icon-wiser" },
            { text: "loader1", value: "icon-loader1" },
            { text: "loader2", value: "icon-loader2" },
            { text: "loader-001", value: "icon-loader-001" },
            { text: "loader-002", value: "icon-loader-002" },
            { text: "loader-003", value: "icon-loader-003" },
            { text: "uniEA2A", value: "icon-uniEA2A" },
            { text: "uniEA2B", value: "icon-uniEA2B" },
            { text: "uniEA2C", value: "icon-uniEA2C" },
            { text: "uniEA2D", value: "icon-uniEA2D" },
            { text: "uniEA2E", value: "icon-uniEA2E" },
            { text: "uniEA2F", value: "icon-uniEA2F" }
        ];

        // Icon list
        this.entityIcon = $("#entityIcon").kendoDropDownList({
            placeholder: "Maak uw keuze...",
            clearButton: false,
            dataSource: iconsDataSource,
            dataTextField: "text",
            dataValueField: "value",
            template: `<ins class="iconpreview-icon #: data.value #"></ins><span class="iconpreview-name">#: data.text #</span>`
        }).data("kendoDropDownList");

        this.entityIconAdd = $("#entityIconAdd").kendoDropDownList({
            placeholder: "Maak uw keuze...",
            clearButton: false,
            dataSource: iconsDataSource,
            dataTextField: "text",
            dataValueField: "value",
            optionLabel: {
                value: "",
                text: "Maak uw keuze..."
            },
            template: `<ins class="iconpreview-icon #: data.value #"></ins><span class="iconpreview-name">#: data.text #</span>`
        }).data("kendoDropDownList");

        this.entityIconExpanded = $("#entityIconExpanded").kendoDropDownList({
            placeholder: "Maak uw keuze...",
            clearButton: false,
            dataSource: iconsDataSource,
            dataTextField: "text",
            dataValueField: "value",
            template: `<ins class="iconpreview-icon #: data.value #"></ins><span class="iconpreview-name">#: data.text #</span>`
        }).data("kendoDropDownList");

        this.entityColor = $("#entityColor").kendoDropDownList({
            placeholder: "Maak uw keuze...",
            clearButton: false,
            dataSource: [
                { text: "Blauw", value: "blue" },
                { text: "Oranje", value: "orange" },
                { text: "Geel", value: "yellow" },
                { text: "Groen", value: "green" },
                { text: "Rood", value: "red" }
            ],
            dataTextField: "text",
            dataValueField: "value"
        }).data("kendoDropDownList");

        this.defaultOrdering = $("#defaultOrdering").kendoDropDownList({
            placeholder: "Maak uw keuze...",
            clearButton: false,
            dataSource: [
                { text: "Koppeling", value: "LinkOrdering" },
                { text: "Titel", value: "ItemTitle" }
            ],
            dataTextField: "text",
            dataValueField: "value"
        }).data("kendoDropDownList");

        this.deleteAction = $("#deleteAction").kendoDropDownList({
            placeholder: "Maak uw keuze...",
            clearButton: false,
            dataSource: [
                { text: "Archiveren", value: "Archive" },
                { text: "Permanent verwijderen", value: "Permanent" },
                { text: "Verbergen", value: "Hide" },
                { text: "Niet toestaan", value: "Disallow" },
            ],
            dataTextField: "text",
            dataValueField: "value"
        }).data("kendoDropDownList");

        //SORTABLE
        this.sortableContainer = $("#tabNameProperties div").kendoSortable({
            axis: "y",
            hint: (element) => {
                return element.clone().addClass("hint");
            },
            change: async (e) => {
                const dataSource = this.listOfTabProperties.dataSource.view();
                if (!this.checkIfEntityIsSet() || !dataSource || !dataSource[e.oldIndex] || !dataSource[e.newIndex] || !e.sender.draggedElement[0].dataset.item) {
                    return;
                }
                
                const id = e.sender.draggedElement[0].dataset.item;
                await this.updateEntityPropertyOrdering(dataSource[e.oldIndex].ordering, dataSource[e.newIndex].ordering, id);
            },
            cursorOffset: {
                top: -10,
                left: -230
            }
        }).data("kendoSortable");

        //Sets the correct style for the sortable-object
        $("body").on("click", ".sortable", (event) => {
            if ($(event.currentTarget).data('dragging')) return;
            $('.sortable').removeClass('selected');
            $(event.currentTarget).addClass('selected');
        });

        //Opens combobox on click anywhere in fieldselection
        $(function () {
            $("[data-role=combobox]").each(function () {
                const widget = $(this).getKendoComboBox();
                widget.input.on("focus", function () {
                    widget.open();
                });
            });
        });

        //TIMEPICKER
        this.minTimeBox = $("#minimumTime").kendoTimePicker({
            dateInput: false,
            culture: "nl-NL",
            format: "HH:mm"
        }).data("kendoTimePicker");

        //GRID
        this.grid = $("#valuegrid").kendoGrid({
            resizable: true,
            toolbar: ["create"],
            remove: (e) => {
                if (e.model.autoIndex) {
                    this.removeOnAutoIndex(this.fieldOptions, e.model.autoIndex);
                }
            },
            editable: {
                createAt: "bottom"
            },
            columns: [{
                field: "name",
                title: "Naam"
            }, {
                field: "id",
                title: "Id"
            }, {
                command: [

                    { text: "↑", click: this.base.moveUp.bind(this.base) },
                    { text: "↓", click: this.base.moveDown.bind(this.base) },
                    "destroy"
                ]
            }]
        }).data("kendoGrid");

        ////COMBOBOX - GENERAL
        $("#checkedCheckbox").kendoDropDownList().data("kendoDropDownList");
        
        this.checkBoxMode = $("#checkBoxMode").kendoDropDownList({
            cascade: (cascadeEvent) => {
                const dataItem = cascadeEvent.sender.dataItem();
                $(".togglePanel.checkBoxMode").removeClass("active");
                $(`.togglePanel.checkBoxMode[data-panel="${dataItem.value}"]`).addClass("active");
            }
        }).data("kendoDropDownList");

        this.multiSelectMode = $("#multiSelectMode").kendoDropDownList({
            cascade: (cascadeEvent) => {
                const dataItem = cascadeEvent.sender.dataItem();
                $(".togglePanel.multiSelectMode").removeClass("active");
                $(`.togglePanel.multiSelectMode[data-panel="${dataItem.value}"]`).addClass("active");
            }
        }).data("kendoDropDownList");

        this.numberFormat = $("#numberFormat").kendoDropDownList({
            clearButton: false,
            dataTextField: "text",
            dataValueField: "value",
            filter: "contains",
            optionLabel: {
                value: "",
                text: "Selecteer het gewenste format..."
            },
            dataSource: [
                { text: "Getalnotatie (1.00)", value: "#" },
                { text: "Valuta (€50,00)", value: "c" },
                { text: "Anders...", value: "anders" }
            ],
            cascade: function () {
                if (this.dataItem().value === "anders") {
                    $("#differentFormatHolder").show();
                } else {
                    $("#differentFormatHolder").hide();
                }
            }
        }).data("kendoDropDownList");

        $("#dateTimeDropDown").kendoDropDownList({
            clearButton: false,
            dataTextField: "text",
            dataValueField: "value",
            filter: "contains",
            optionLabel: {
                value: "",
                text: "Maak uw keuze..."
            },
            cascade: function (e) {
                const dataItem = e.dataItem || e.sender.dataItem();
                $('.item.datetime [data-invisible]').show();
                $('.item.datetime [data-invisible*="' + dataItem.value + '"]').hide();
            },
            dataSource: [
                { text: "Datum", value: "date" },
                { text: "Tijd", value: "time" },
                { text: "Datum + tijd", value: "datetime" }
            ]
        }).data("kendoDropDownList");

        //Main combobox for selecting a entity
        this.entitiesCombobox = $("#entities").kendoDropDownList({
            placeholder: "Select gewenste entiteit...",
            clearButton: false,
            height: 400,
            dataTextField: "displayName",
            dataValueField: "id",
            filter: "contains",
            optionLabel: {
                id: "",
                name: "",
                displayName: "Maak uw keuze..."
            },
            minLength: 1,
            dataSource: {},
            cascade: this.onEntitiesComboBoxSelect.bind(this)
        }).data("kendoDropDownList");

        this.entityListInitialized = false;
        this.entitiesCombobox.one("dataBound", () => {
            this.entityListInitialized = true;
        });

        //combobox to select the correct tabname
        this.tabNameDropDownList = $("#tabnames").kendoDropDownList({
            clearButton: false,
            dataTextField: "tabName",
            dataValueField: "tabName",
            filter: "contains",
            minLength: 1,
            cascade: this.tabNameDropDownListSelect.bind(this)
        }).data("kendoDropDownList");

        // property tabname
        this.tabNameProperty = $("#tabNameProperty").kendoComboBox({
            placeholder: "Selecteer gewenste tab...",
            clearButton: false,
            dataTextField: "tabName",
            dataValueField: "tabName",
            minLength: 1,
            dataSource: []
        }).data("kendoComboBox");

        //Combobox to get all possible inputtypes used in the database
        //TODO set to kendodropdownlist and add filter?
        this.inputTypeSelector = $("#inputtypes").kendoDropDownList({
            placeholder: "Selecteer invoertype...",
            height: 400,
            clearButton: false,
            dataTextField: "text",
            dataValueField: "id",
            filter: "contains",
            optionLabel: {
                text: "Selecteer invoertype...",
                id: ""
            },
            dataSource: this.createDataSourceFromEnum(this.base.inputTypes),
            change: (changeEvent) => {
                if (changeEvent.sender.fieldOptions !== {} && (typeof changeEvent.sender.fieldOptions !== 'undefined')) {
                    this.base.openDialog("Invoertype wijzigen", "Weet u zeker dat u het invoertype wilt wijzigen?", this.base.kendoPromptType.CONFIRM).then(() => {
                        this.fieldOptions = {};
                        this.setEntityFieldPropertiesToDefault();
                        this.hideShowElementsBasedOnValue(changeEvent.sender.dataItem().text);
                    });
                } else {
                    this.hideShowElementsBasedOnValue(changeEvent.sender.dataItem().text);
                }
                this.hideShowElementsBasedOnValue(changeEvent.sender.dataItem().text);
            }
        }).data("kendoDropDownList");

        this.dataSourceEntities = $("#dataSourceEntities").kendoDropDownList({
            clearButton: false,
            dataTextField: "displayName",
            dataValueField: "id",
            filter: "contains",
            minLength: 1,
            dataSource: {}
        }).data("kendoDropDownList");

        this.linkedItemEntity = $("#linkedItemEntity").kendoDropDownList({
            clearButton: false,
            dataTextField: "displayName",
            dataValueField: "id",
            filter: "contains",
            minLength: 1,
            dataSource: {},
            cascade: (e) => {
                const dataItem = e.dataItem || e.sender.dataItem();
                if (!dataItem) {
                    return;
                }
                // own ajax request to get data from GET_ITEMLINKS_BY_ENTITY and set to both datasources
                const linkTypeList = new kendo.data.DataSource({
                    transport: {
                        async: true,
                        cache: "inmemory",
                        read: {
                            cache: "inmemory",
                            url: `${this.base.settings.serviceRoot}/GET_ITEMLINKS_BY_ENTITY?entityName=${encodeURIComponent(dataItem.id)}`
                        }
                    }
                });
                // set linked item dropdown lists
                this.linkType.setDataSource(linkTypeList);
            }
        }).data("kendoDropDownList");

        this.linkType = $("#linkType").kendoComboBox({
            placeholder: "Maak uw keuze...",
            dataTextField: "typeText",
            dataValueField: "typeValue",
            dataSource: {},
            optionLabel: {
                typeValue: "",
                typeText: "Maak uw keuze..."
            },
            change: function () {
                const value = parseFloat(this.value()); //parse value

                if (isNaN(value)) {
                    this.value(""); //clear the value
                }
            }
        }).data("kendoComboBox");

        //Combobox for the "Groep" combobox
        this.groupNameComboBox = $("#groupName").kendoComboBox({
            clearButton: false,
            dataTextField: "groupName",
            dataValueField: "groupName"
        }).data("kendoComboBox");

        // Dependencies.
        this.dependencyAction = $("#dependencyAction").kendoDropDownList({
            optionLabel: {
                value: "",
                text: "Maak uw keuze..."
            },
            dataSource: [
                { text: "Aleen zichtbaar maken wanneer...", value: "toggleVisibility" },
                { text: "Verversen wanneer...", value: "refresh" }
            ],
            dataTextField: "text",
            dataValueField: "value",
        }).data("kendoDropDownList");

        this.dependencyFields = $("#dependencyField").kendoDropDownList({
            dataTextField: "displayName",
            dataValueField: "propertyName",
            optionLabel: {
                propertyName: "",
                displayName: "Maak uw keuze..."
            }
        }).data("kendoDropDownList");

        this.dependencyOperator = $("#dependencyOperator").kendoDropDownList({
            dataSource: [
                { text: "gelijk is aan ...", value: "equals" },
                { text: "ongelijk is aan ...", value: "notEquals" },
                { text: "de waarde ... bevat", value: "contains" },
                { text: "niet de waarde ... bevat", value: "doesNotContain" },
                { text: "begint met ...", value: "startsWith" },
                { text: "niet begint met ...", value: "doesNotStartWith" },
                { text: "eindigt met ...", value: "endsWith" },
                { text: "niet eindigt met ...", value: "doesNotEndWith" },
                { text: "leeg is", value: "isEmpty" },
                { text: "niet leeg is", value: "isNotEmpty" },
                { text: "groter is dan ...", value: "greaterThanOrEqualTo" },
                { text: "groter is dan of gelijk is aan ...", value: "greaterThan" },
                { text: "kleiner is dan ...", value: "lessThanOrEqualTo" },
                { text: "kleiner is dan of gelijk is aan ...", value: "lessThan" }
            ],
            dataTextField: "text",
            dataValueField: "value",
            optionLabel: {
                value: "",
                text: "Maak uw keuze..."
            },
            select: (e) => {
                const dataItem = e.dataItem;
                this.filterOptions = dataItem.value;
                $(`.item[data-visible*="${dataItem.value}"]`).show();
            }
        }).data("kendoDropDownList");

        $("#typeSecureInput").kendoDropDownList({
            placeholder: "Maak uw keuze...",
            dataSource: [
                { text: "Tekst", value: "text" },
                { text: "Wachtwoord", value: "password" }
            ],
            dataTextField: "text",
            dataValueField: "value"
        }).data("kendoDropDownList");

        $("#securityMethod").kendoDropDownList({
            dataSource: [
                { text: "JCL Advanced Encryption Standard", value: "JCL_AES" },
                { text: "Advanced Encryption Standard", value: "AES" },
                { text: "Secure Hash Algorithm 512 bits", value: "JCL_SHA512" }
            ],
            dataTextField: "text",
            dataValueField: "value",
            optionLabel: {
                value: "",
                text: "Maak uw keuze..."
            },
            cascade: (e) => {
                const dataItem = e.dataItem || e.sender.dataItem();
                $(".item.secureInput[data-visible]").hide();
                $('.item.secureInput[data-visible*="' + dataItem.value + '"]').show();
            }
        }).data("kendoDropDownList");

        function onDataBound(e) {
            $('.k-multiselect .k-input').unbind('keyup');
            $('.k-multiselect .k-input').on('keyup', onClickEnter);
        }

        function onClickEnter(e) {
            if (e.keyCode === 13) {
                const widget = $('#searchFields').getKendoMultiSelect();
                const value = $(`.item.tagList .k-multiselect .k-input`).val().trim();
                if (!value || value.length === 0) {
                    return;
                }
                const newItem = {
                    name: value
                };

                widget.dataSource.add(newItem);
                widget.value(widget.value().concat([newItem.name]));
            }
        }

        this.searchFields = $("#searchFields").kendoMultiSelect({
            dataTextField: "name",
            dataValueField: "name",
            dataSource: {
                data: []
            },
            dataBound: onDataBound
        }).data("kendoMultiSelect");

        //COMBOBOX - INPUT-TYPE
        this.dataSourceFilter = $("#dataSourcefilter").kendoDropDownList({
            dataSource: this.createDataSourceFromEnum(this.base.dataSourceType, true),
            dataTextField: "text",
            dataValueField: "value",
            cascade: (cascadeEvent) => {
                const dataItem = cascadeEvent.sender.dataItem();
                $(".togglePanel.dataSource").removeClass("active");
                $(`.togglePanel.dataSource[data-panel="${dataItem.id}"]`).addClass("active");
                $("[data-show-for-panel=panel2]").toggle(dataItem.id === "panel2");
            }
        }).data("kendoDropDownList");

        // textbox type
        this.textboxTypeDropDown = $("#textboxTypeDropDown").kendoDropDownList({
            dataSource: this.createDataSourceFromEnum(this.base.textboxType, true),
            dataTextField: "text",
            dataValueField: "value"
        }).data("kendoDropDownList");

        /*
         * item linker
         */
        this.itemLinkerTypeNumber = $("#itemLinkerTypeNumber").kendoNumericTextBox({
            format: "#",
            decimals: 0
        }).data("kendoNumericTextBox");

        this.itemLinkerEntity = $("#itemLinkerEntity").kendoMultiSelect({
            autoClose: false,
            clearButton: false,
            dataTextField: "displayName",
            dataValueField: "id",
            filter: "contains",
            minLength: 1,
            dataSource: {}
        }).data("kendoMultiSelect");

        $("#itemLinkerModuleId").kendoNumericTextBox({
            decimals: 0,
            format: "#",
            min: 0,
            step: 1
        }).data("kendoNumericTextBox");

        $("#itemLinkerDeletionOfItems").kendoDropDownList({
            placeholder: "Maak uw keuze...",
            clearButton: false,
            dataSource: [
                { text: "Het is niet mogelijk om items te verwijderen", value: "off" },
                { text: "De verwijder-knop verwijdert alleen de koppeling tussen de 2 items", value: "deleteLink" },
                { text: "De verwijder-knop verwijdert altijd de koppeling en het item zelf", value: "deleteItem" },
                { text: "De verwijder-knop vraagt aan de gebruiker of alleen de koppeling verwijdert moet worden, of ook het item zelf", value: "askUser" }
            ],
            dataTextField: "text",
            dataValueField: "value"
        }).data("kendoDropDownList");

        const actionButtonType = (container, options) => {
            $('<input required name="' + options.field + '"/>')
                .appendTo(container)
                .kendoDropDownList({
                    autoBind: false,
                    dataSource: this.createDataSourceFromEnum(this.base.actionButtonTypes, true),
                    dataTextField: "text",
                    dataValueField: "id",
                    valuePrimitive: true,
                    select: (e) => {
                        const editButton = e.sender.element.closest("tr[role=row]").find("td.k-command-cell a[role=button].k-grid-Wijzigen");
                        if (e.dataItem.id === "refreshCurrentItem" || e.dataItem.id === "custom") {
                            editButton.hide();
                        } else {
                            editButton.show();
                        }
                    },
                    optionLabel: {
                        id: "",
                        text: "Maak uw keuze..."
                    }
                });
        };

        this.actionButtonActionsGridDataSourceSettings = {
            schema: {
                model: {
                    fields: {
                        type: { defaultValue: { text: "Maak uw keuze...", id: "" } }
                    }
                }
            }
        };

        this.actionButtonActionsGrid = $("#actionButtonActionsGrid").kendoGrid({
            dataSource: this.actionButtonActionsGridDataSourceSettings,
            resizable: true,
            toolbar: ["create"],
            editable: {
                createAt: "bottom"
            },
            remove: (e) => {
                if (e.model.action.autoIndex) {
                    this.removeOnAutoIndex(this.fieldOptions, e.model.action.autoIndex);
                }
            },
            columns: [
                {
                    field: "type",
                    title: "Actie type",
                    width: "350px",
                    editor: actionButtonType,
                    template: (event) => {
                        const typeValue = typeof event.type === "string" ? event.type : event.type.text;
                        const enumValue = this.base.actionButtonTypes[typeValue.toUpperCase()];
                        return enumValue ? enumValue.text : typeValue;
                    }
                },
                {
                    command: [
                        {
                            text: "Wijzigen",
                            click: this.onActionButtonGridEditButtonClick.bind(this),
                            visible: function (dataItem) {
                                // NOTE: Don't use arrow function here because Kendo throws an error with it.
                                return dataItem.type !== "refreshCurrentItem" && dataItem.type !== "custom";
                            }
                        },
                        { text: "↑", click: this.base.moveUp.bind(this.base) },
                        { text: "↓", click: this.base.moveDown.bind(this.base) },
                        "destroy"],
                    title: " ",
                    width: "170px"
                }]
        }).data("kendoGrid");

        this.actionButtonGrid = $("#actionButtonGrid").kendoGrid({
            dataSource: {},
            resizable: true,
            toolbar: ["create"],
            editable: {
                createAt: "bottom"
            },
            remove: (e) => {
                if (e.model.autoIndex) {
                    this.removeOnAutoIndex(this.fieldOptions, e.model.autoIndex);
                }
            },
            dataBound: (e) => {
                if (this.inputTypeSelector.dataItem().text === this.base.inputTypes.ACTIONBUTTON && this.actionButtonGrid.dataSource.data().length >= 1) {
                    $("#actionButtonGrid .k-button.k-grid-add").removeClass("k-grid-add").addClass("k-state-disabled").removeAttr("href");
                } else {
                    $("#actionButtonGrid .k-button.k-state-disabled").removeClass("k-state-disabled").addClass("k-grid-add").attr("href", "#");
                }
            },
            columns: [
                {
                    field: "text",
                    title: "Knop tekst",
                    width: "250px"
                },
                {
                    field: "icon",
                    title: "Knop icoon",
                    width: "250px"
                },
                {
                    command: [
                        {
                            text: "Wijzigen",
                            click: this.onActionGridEditButtonClick.bind(this)
                        },
                        { text: "↑", click: this.base.moveUp.bind(this.base) },
                        { text: "↓", click: this.base.moveDown.bind(this.base) },
                        "destroy"
                    ],
                    title: " ",
                    width: "250px"
                }]
        }).data("kendoGrid");

        this.subEntityGridEntity = $("#subEntityGridEntity").kendoDropDownList({
            autoClose: false,
            clearButton: false,
            dataTextField: "displayName",
            dataValueField: "id",
            filter: "contains",
            minLength: 1,
            dataSource: {}
        }).data("kendoDropDownList");

        this.dataSelectorIdSubEntitiesGrid = $("#dataSelectorIdSubEntitiesGrid").kendoNumericTextBox({
            decimals: 0,
            format: "#",
            min: 0,
            step: 1
        }).data("kendoNumericTextBox");

        this.subEntitiesGridSelectOptions = $("#subEntitiesGridSelectOptions").kendoDropDownList({
            placeholder: "Maak uw keuze...",
            clearButton: false,
            dataSource: [
                { text: "Geen selectie mogelijk", value: "false" },
                { text: "De gebruiker kan 1 regel selecteren.", value: "row" },
                { text: "De gebruiker kan 1 of meer regels selecteren", value: "multiple, row" },
                { text: "De gebruiker kan 1 cell in het grid selecteren", value: "cell" },
                { text: "De gebruiker kan 1 of meer cellen in het grid selecteren", value: "multiple, cell" }
            ],
            dataTextField: "text",
            dataValueField: "value"
        }).data("kendoDropDownList");

        //timeline
        this.timelineEntity = $("#timelineEntity").kendoDropDownList({
            autoClose: false,
            dataTextField: "displayName",
            dataValueField: "id",
            filter: "contains",
            minLength: 1,
            dataSource: {}
        }).data("kendoDropDownList");

        $("#queryId").kendoNumericTextBox({
            decimals: 0,
            min: 0,
            format: "#"
        }).data("kendoNumericTextBox");

        $("#timelineEventHeight").kendoNumericTextBox({
            decimals: 0,
            min: 0,
            format: "#"
        }).data("kendoNumericTextBox");

        // daterange
        $("#daterangeFrom").kendoDatePicker({
            dateInput: true,
            format: "dd-MM-yyyy",
            culture: "nl-NL",
            //change: function (e) {
            change: (changeEvent) => {
                const tillPicker = $("#daterangeTill").data("kendoDatePicker");
                if (changeEvent.sender.value() > tillPicker.value()) {
                    tillPicker.value("");
                }
                tillPicker.min(changeEvent.sender.value());
            }
        }).data("kendoDatePicker");

        $("#daterangeTill").kendoDatePicker({
            dateInput: true,
            format: "dd-MM-yyyy",
            culture: "nl-NL"
        }).data("kendoDatePicker");
        
        $("#explanation").kendoEditor({
            height: "400px"
        });
        
        this.labelStyle = $("#labelStyle").kendoDropDownList({
            dataTextField: "text",
            dataValueField: "value",
            dataSource: [
                { text: "Normal", value: "normal" },
                { text: "Inline", value: "inline" },
                { text: "Float", value: "float" }
            ],
            cascade: (event) => {
                $("#labelWidthContainer").toggle(event.sender.value() === "inline");
            }
        }).data("kendoDropDownList");

        this.labelWidth = $("#labelWidth").kendoDropDownList({
            dataTextField: "text",
            dataValueField: "value",
            dataSource: [
                { text: "0", value: 0 },
                { text: "10%", value: 10 },
                { text: "20%", value: 20 },
                { text: "30%", value: 30 },
                { text: "40%", value: 40 },
                { text: "50%", value: 50 }
            ]
        }).data("kendoDropDownList");

        // set entity dropdown lists 
        this.reloadEntityList();
    }

    /**
     * Function for handling the action grid edit click
     * @param {any} event
     */
    onActionGridEditButtonClick(event) {
        const gridDataItem = this.actionButtonGrid.dataItem(event.currentTarget.closest("tr"));
        let popUpHtml = $("#actionGridPopupHtml");
        let window = popUpHtml.data("kendoWindow");

        if (window) {
            document.getElementById("actionButtonText").value = "";
            document.getElementById("actionButtonIcon").value = "";
            $("#actionGridPopupHtml").show();
            //  action button actions grid
            const settings = this.actionButtonActionsGridDataSourceSettings;
            settings.data = [];
            this.actionButtonActionsGrid.setDataSource(settings);
        } else {
            window = $("#actionGridPopupHtml").kendoWindow({
                width: 1000,
                height: 800
            }).data("kendoWindow");
            $(".actionGridSave").kendoButton({
                icon: "save"
            });
        }

        $(".actionGridSave").unbind("click").bind("click", () => {
            gridDataItem.button = {};
            gridDataItem.text = document.getElementById("actionButtonText").value;
            gridDataItem.icon = document.getElementById("actionButtonIcon").value;
            gridDataItem.button.actions = [];
            const abag = this.actionButtonActionsGrid.dataSource.data();
            const emptyActions = [];
            for (let i = 0; i < abag.length; i++) {
                let action = abag[i].action;
                if (abag[i].type === "refreshCurrentItem" || abag[i].type === "custom") {
                    action = { type: abag[i].type };
                } else if (!action) {
                    emptyActions.push(this.base.actionButtonTypes[abag[i].type.toUpperCase()].text || abag[i].type);
                }

                gridDataItem.button.actions.push(action);
            }
            if (emptyActions.length) {
                this.base.openDialog("Sluiten?",
                    `U heeft bij actie: ${emptyActions.join()} niets ingevuld, wilt u opslaan en het venster sluiten?`,
                    this.base.kendoPromptType.CONFIRM).then(() => {
                    window.close();
                    this.actionButtonGrid.refresh();
                });
            } else {
                window.close();
                this.actionButtonGrid.refresh();
            }
        });

        if (gridDataItem.button) {
            // set action button actions grid to the appropriate fields/settings
            const actionsArray = gridDataItem.button.actions;
            const actions = [];
            for (let i = 0; i < actionsArray.length; i++) {
                if (!actionsArray[i]) {
                    continue;
                }
                actions.push({
                    type: actionsArray[i].type,
                    action: actionsArray[i]
                });
            }

            const ds = this.actionButtonActionsGridDataSourceSettings;
            ds.data = actions;
            this.actionButtonActionsGrid.setDataSource(ds);
        }
        const name = gridDataItem.text || "";
        document.getElementById("actionButtonText").value = name;
        document.getElementById("actionButtonIcon").value = gridDataItem.icon || "";
        window.title(`Knop: ${name}`);
        window.center().open();
    }

    /**
     * Function for handling the action BUTTON grid edit click
     * @param {any} event
     */
    onActionButtonGridEditButtonClick(event) {
        const gridDataItem = this.actionButtonActionsGrid.dataItem(event.currentTarget.closest("tr"));

        // init fields
        let popUpHtml = $("#actionButtonPopupHtml");
        let window = popUpHtml.data("kendoWindow");
        let tabStrip = popUpHtml.find(".tabStripActionButton");
        let userParametersGrid = popUpHtml.find(".actionButtonUserParametersGrid");
        let itemLink = popUpHtml.find(".actionButtonItemLink");
        let actionQueryId = popUpHtml.find("#actionButtonQueryItemId");
        let dataSelectorId = popUpHtml.find("#dataSelectorId");
        let actionButtonUrlWindowHeight = popUpHtml.find("#actionButtonUrlWindowHeight");
        let actionButtonUrlWindowWidth = popUpHtml.find("#actionButtonUrlWindowWidth");
        let contentItemId = popUpHtml.find("#contentItemId");
        let emailDataQueryId = popUpHtml.find("#emailDataQueryId");
        let actionButtonUrlWindowOpen = popUpHtml.find("#actionButtonUrlWindowOpen");

        const showFields = (fieldType) => {
            const fieldTypes = this.base.fieldTypesDropDown;
            switch (fieldType) {
                case fieldTypes.COMBOBOX.id: {
                    const cbFields = ["dataSource", "queryId", "userTypes", "dataTextField", "dataValueField"];
                    cbFields.forEach((v) => {
                        this.userParametersGrid.showColumn(v);
                    });
                    window.setOptions({
                        width: 1200,
                        height: 800
                    });
                    window.maximize();
                    break;
                }
            }
        };

        if (window) {
            // open window and set to default
            $("#actionButtonPopupHtml").show();
            window.setOptions({
                width: 1000,
                height: 800
            });
            // set to fields default
            this.actionButtonItemLink.value("");
            document.getElementById("actionButtonItemId").value = "";
            // url
            document.getElementById("actionButtonUrl").value = "";
            this.actionButtonUrlWindowOpen.select(0);
            this.actionButtonUrlWindowHeight.value("");
            this.actionButtonUrlWindowWidth.value("");
            // query
            this.actionButtonQueryItemId.value("");
            // hide extra fields
            popUpHtml.find("[data-visible]").hide();
            // empty generate file fields
            this.dataSelectorId.value("");
            this.contentItemId.value("");
            this.emailDataQueryId.value("");
            document.getElementById("contentPropertyName").value = "";
            document.getElementById("pdfBackgroundPropertyName").value = "";
            document.getElementById("pdfDocumentOptionsPropertyName").value = "";
            document.getElementById("pdfFilename").value = "";
            // confirm dialog
            document.getElementById("actionButtonConfirmDialogTitle").value = "";
            document.getElementById("actionButtonConfirmDialogText").value = "";
            // reset user parameters grid
            const resetDs = this.userParametersGridDataSourceSettings;
            resetDs.data = [];
            this.userParametersGrid.setDataSource(resetDs);
        } else {
            window = $("#actionButtonPopupHtml").kendoWindow({
                width: 1000,
                height: 800
            }).data("kendoWindow");

            tabStrip.kendoTabStrip({
                select: (e) => {
                    if (e.item.dataset.visible === "generateFile") {
                        // show execute query tab if generateFile is selected, because we need the same properties.
                        tabStrip.data("kendoTabStrip").activateTab(e.item.parentElement.querySelector("[data-visible*=executeQuery]"));
                        // get tab content of selected 
                        const content = tabStrip.data("kendoTabStrip").contentElement(tabStrip.data("kendoTabStrip").select().index());
                        $(content).find("[data-visible=generateFile]").show();
                    }
                },
                animation: {
                    open: { effects: "fadeIn" }
                }
            });

            const fieldTypeDropDownList = (container, options) => {
                $('<input required name="' + options.field + '"/>')
                    .appendTo(container)
                    .kendoDropDownList({
                        autoBind: false,
                        valuePrimitive: true,
                        dataTextField: "text",
                        dataValueField: "id",
                        change: (me) => {
                            const dataItem = me.sender.dataItem();
                            showFields(dataItem.id);

                        },
                        dataSource: this.createDataSourceFromEnum(this.base.fieldTypesDropDown, true)
                    });
            };

            this.userParametersGridDataSourceSettings = {
                schema: {
                    model: {
                        fields: {
                            fieldType: { defaultValue: this.base.fieldTypesDropDown["INPUT"], type: "object" },
                            fieldTypeId: { from: "fieldType.id" },
                            queryId: { type: "number" },
                            gridHeight: { type: "number" }
                        }
                    }
                }
            };
            this.userParametersGrid = userParametersGrid.kendoGrid({
                dataSource: this.userParametersGridDataSourceSettings,
                resizable: true,
                remove: (e) => {
                    if (e.model.autoIndex) {
                        this.removeOnAutoIndex(this.fieldOptions, e.model.autoIndex);
                    }
                },
                toolbar: ["create"],
                editable: {
                    createAt: "bottom"
                },
                columns: [
                    {
                        field: "name",
                        title: "Naam parameter"
                    },
                    {
                        field: "question",
                        title: "Vraagtekst"
                    },
                    {
                        field: "fieldTypeId",
                        title: "Veldtype",
                        editor: fieldTypeDropDownList,
                        template: (event) => {
                            return event.fieldTypeId === "" ? event.fieldType.text : this.base.fieldTypesDropDown[event.fieldTypeId.toUpperCase()].text;
                        }
                    },
                    {
                        field: "value",
                        title: "Standaardwaarde"
                    },
                    {
                        field: "format",
                        title: "Format"
                    },
                    {
                        field: "queryId",
                        title: "Query id"
                    },
                    {
                        field: "gridHeight",
                        title: "Grid hoogte"
                    },
                    {
                        field: "dataTextField",
                        title: "Data tekst veld", hidden: true
                    },
                    {
                        field: "dataValueField",
                        title: "Data waarde veld", hidden: true
                    },
                    {
                        // todo what to do with this?
                        field: "dataSource",
                        title: "DataSource", hidden: true
                    },
                    {
                        // todo set user types to multi select
                        field: "userTypes",
                        title: "Gebruiker types", hidden: true
                    },
                    {
                        command: [
                            { text: "↑", click: this.base.moveUp.bind(this.base) },
                            { text: "↓", click: this.base.moveDown.bind(this.base) },
                            "destroy"
                        ]
                    }

                ]
            }).data("kendoGrid");

            this.actionButtonItemLink = itemLink.kendoNumericTextBox({
                decimals: 0,
                min: 0,
                format: "#"
            }).data("kendoNumericTextBox");

            this.actionButtonQueryItemId = actionQueryId.kendoNumericTextBox({
                decimals: 0,
                min: 0,
                format: "#"
            }).data("kendoNumericTextBox");

            this.dataSelectorId = dataSelectorId.kendoNumericTextBox({
                decimals: 0,
                min: 0,
                format: "#"
            }).data("kendoNumericTextBox");

            this.contentItemId = contentItemId.kendoNumericTextBox({
                decimals: 0,
                min: 0,
                format: "#"
            }).data("kendoNumericTextBox");

            this.emailDataQueryId = emailDataQueryId.kendoNumericTextBox({
                decimals: 0,
                min: 0,
                format: "#"
            }).data("kendoNumericTextBox");

            $(".actionButtonSave").kendoButton({
                icon: "save"
            });

            this.actionButtonUrlWindowHeight = actionButtonUrlWindowHeight.kendoNumericTextBox({
                decimals: 0,
                min: 0,
                format: "#"
            }).data("kendoNumericTextBox");

            this.actionButtonUrlWindowWidth = actionButtonUrlWindowWidth.kendoNumericTextBox({
                decimals: 0,
                min: 0,
                format: "#"
            }).data("kendoNumericTextBox");

            this.actionButtonUrlWindowOpen = actionButtonUrlWindowOpen.kendoDropDownList({
                clearButton: false,
                dataTextField: "text",
                dataValueField: "value",
                filter: "contains",
                optionLabel: {
                    value: "",
                    text: "Maak uw keuze..."
                },
                dataSource: [
                    { text: "In een apart scherm", value: "window.open" },
                    { text: "In een popup", value: "kendoWindow " }
                ]
            }).data("kendoDropDownList");
        }
        // bind and unbind to get the appropriate dataitem 
        $(".actionButtonSave").unbind("click").bind("click",
            () => {
                if (!this.beforeCreateActionDataItem(gridDataItem)) {
                    return;
                }
                gridDataItem.action = this.createActionDataItem(gridDataItem);
                window.close();
            });

        // hide / show elements based on type
        const tagStrip = tabStrip.data("kendoTabStrip");
        const tagGroup = tagStrip.tabGroup;
        tagGroup.children().hide();
        tagGroup.find(`[data-visible*=${gridDataItem.type}]`).show().trigger("click");
        tagStrip.activateTab(tagGroup.find(`[data-visible*=${gridDataItem.type}]`));

        // set properties accordingly
        if (gridDataItem.action) {
            const actionTypes = this.base.actionButtonTypes;
            switch (gridDataItem.type) {
                case actionTypes.OPENURL.id:
                case actionTypes.OPENURLONCE.id:
                    document.getElementById("actionButtonUrl").value = gridDataItem.action.url;
                    this.actionButtonUrlWindowOpen.select((dataItem) => {
                        return dataItem.value === gridDataItem.action.openIn;
                    });
                    this.actionButtonUrlWindowWidth.value(gridDataItem.action.windowWidth);
                    this.actionButtonUrlWindowHeight.value(gridDataItem.action.windowHeight);
                    break;
                case actionTypes.OPENWINDOW.id:
                    document.getElementById("actionButtonItemId").value = gridDataItem.action.itemId;
                    this.actionButtonItemLink.value(gridDataItem.action.linkId);
                    break;
                case actionTypes.EXECUTEQUERY.id:
                case actionTypes.EXECUTEQUERYONCE.id:
                case actionTypes.GENERATEFILE.id: {
                    this.actionButtonQueryItemId.value(gridDataItem.action.queryId);
                    let up = gridDataItem.action.userParameters;
                    let rows = [];

                    for (let i = 0; i < up.length; i++) {
                        showFields(up[i].fieldType);
                        rows.push({
                            name: up[i].name,
                            question: up[i].question,
                            fieldType: this.base.fieldTypesDropDown[up[i].fieldType.toUpperCase()] || this.base.fieldTypesDropDown["INPUT"],
                            value: up[i].value,
                            format: up[i].format,
                            dataTextField: up[i].dataTextField,
                            dataValueField: up[i].dataValueField,
                            userTypes: up[i].userTypes,
                            queryId: up[i].queryId,
                            gridHeight: up[i].gridHeight,
                            dataSource: JSON.stringify(up[i].dataSource),
                            autoIndex: up[i].autoIndex
                        });
                    }
                    let userParametersGridDataSourceSettings = this.userParametersGridDataSourceSettings;
                    userParametersGridDataSourceSettings.data = rows;
                    this.userParametersGrid.setDataSource(userParametersGridDataSourceSettings);
                    if (gridDataItem.type === actionTypes.GENERATEFILE.id) {
                        this.dataSelectorId.value(gridDataItem.action.dataSelectorId);
                        this.contentItemId.value(gridDataItem.action.contentItemId);
                        this.emailDataQueryId.value(gridDataItem.action.emailDataQueryId);
                        document.getElementById("contentPropertyName").value = gridDataItem.action.contentPropertyName;
                        document.getElementById("pdfBackgroundPropertyName").value = gridDataItem.action.pdfBackgroundPropertyName;
                        document.getElementById("pdfDocumentOptionsPropertyName").value = gridDataItem.action.pdfDocumentOptionsPropertyName;
                        document.getElementById("pdfFilename").value = gridDataItem.action.pdfFilename;
                    }
                    break;
                }
                case actionTypes.ACTIONCONFIRMDIALOG.id: {
                    document.getElementById("actionButtonConfirmDialogTitle").value = gridDataItem.action.title;
                    document.getElementById("actionButtonConfirmDialogText").value = gridDataItem.action.text;
                    break;
                }
            }
        }
        window.title("Actie wijzigen");
        window.center().open();
    }

    beforeCreateActionDataItem(dataItem) {
        const actionTypes = this.base.actionButtonTypes;
        const actionType = dataItem.type;
        switch (actionType) {
            case actionTypes.OPENURL.id:
            case actionTypes.OPENURLONCE.id:
                if (document.getElementById("actionButtonUrl").value === "") {
                    this.base.showNotification("notification", "Voer eerst een url in!", "error");
                    return false;
                }
                break;
            case actionTypes.OPENWINDOW.id:
                var itemid = document.getElementById("actionButtonItemId").value;
                if ((itemid !== "{itemid}" && !parseInt(itemid)) || itemid === "") {
                    this.base.showNotification("notification", "Voer een numerieke waarde in bij item id!", "error");
                    return false;
                }
                break;
            case actionTypes.EXECUTEQUERY.id:
            case actionTypes.EXECUTEQUERYONCE.id:
            case actionTypes.GENERATEFILE.id:
                var upg = this.userParametersGrid.dataSource.data();
                for (let i = 0; i < upg.length; i++) {
                    let field = upg[i].fieldType;
                    let typeValue = typeof field === "string" ? field : field.id;
                    let enumValue = this.base.fieldTypesDropDown;
                    switch (enumValue) {
                        case typeValue.toUpperCase():

                            if (upg[i].dataTextField === "") {
                                this.base.showNotification("notification", "Voer eerst een data tekst veld in!", "error");
                                return false;
                            }
                            if (upg[i].dataValueField === "") {
                                this.base.showNotification("notification", "Voer eerst een data waarde veld in!", "error");
                                return false;
                            }
                            break;
                    }
                }
                if (actionType === actionTypes.GENERATEFILE.id) {
                    if (!this.dataSelectorId.value()) {
                        this.base.showNotification("notification", "Voer eerst het data selectie id veld in!", "error");
                        return false;
                    }
                    if (!this.contentItemId.value()) {
                        this.base.showNotification("notification", "Voer eerst het content item id veld in!", "error");
                        return false;
                    }
                    if (document.getElementById("contentPropertyName").value === "") {
                        this.base.showNotification("notification", "Voer eerst het content property naam veld in!", "error");
                        return false;
                    }
                }
                //todo add checks
                break;
        }
        return true;
    }

    createActionDataItem(gridDataItem) {
        const action = {};
        action.type = gridDataItem.type;
        const actionTypes = this.base.actionButtonTypes;
        switch (action.type) {
            case actionTypes.OPENURL.id:
            case actionTypes.OPENURLONCE.id:
                action.url = document.getElementById("actionButtonUrl").value;
                action.openIn = this.actionButtonUrlWindowOpen.dataItem().value;
                action.windowWidth = this.actionButtonUrlWindowWidth.value();
                action.windowHeight = this.actionButtonUrlWindowHeight.value();
                break;
            case actionTypes.OPENWINDOW.id:
                var itemId = document.getElementById("actionButtonItemId").value;
                action.itemId = !parseInt(itemId) ? itemId : parseInt(itemId);
                action.linkId = this.actionButtonItemLink.value();
                break;
            case actionTypes.EXECUTEQUERY.id:
            case actionTypes.EXECUTEQUERYONCE.id:
            case actionTypes.GENERATEFILE.id:
                // shared among executequery and generate file
                action.queryId = this.actionButtonQueryItemId.value();
                action.userParameters = [];
                var upg = this.userParametersGrid.dataSource.data();
                for (let i = 0; i < upg.length; i++) {
                    let field = upg[i].fieldTypeId;
                    let typeValue = typeof field === "string" ? field : field.id;
                    let enumValue = typeValue !== "" ? this.base.fieldTypesDropDown[typeValue.toUpperCase()] : this.base.fieldTypesDropDown["INPUT"];
                    action.userParameters.push({
                        name: upg[i].name,
                        question: upg[i].question,
                        fieldType: enumValue ? enumValue.id : typeValue,
                        value: upg[i].value,
                        format: upg[i].format,
                        dataTextField: upg[i].dataTextField,
                        fieldTypeId: field,
                        dataValueField: upg[i].dataValueField,
                        userTypes: upg[i].userTypes,
                        queryId: upg[i].queryId,
                        gridHeight: upg[i].gridHeight,
                        dataSource: !this.base.isJson(upg[i].dataSource) ? JSON.stringify(upg[i].dataSource) : JSON.parse(upg[i].dataSource)
                    });
                }
                // generate file specific
                if (action.type === actionTypes.GENERATEFILE.id) {
                    //todo actions for generatefile
                    action.dataSelectorId = this.dataSelectorId.value();
                    action.contentItemId = this.contentItemId.value();
                    action.contentPropertyName = document.getElementById("contentPropertyName").value;
                    action.pdfBackgroundPropertyName = document.getElementById("pdfBackgroundPropertyName").value;
                    action.pdfDocumentOptionsPropertyName = document.getElementById("pdfDocumentOptionsPropertyName").value;
                    action.pdfFilename = document.getElementById("pdfFilename").value;
                    action.emailDataQueryId = this.emailDataQueryId.value();
                }
                break;
            case actionTypes.ACTIONCONFIRMDIALOG.id:
                action.title = document.getElementById("actionButtonConfirmDialogTitle").value;
                action.text = document.getElementById("actionButtonConfirmDialogText").value;
                break;
        }
        return action;
    }

    // get all tabnames of selected entity
    async onEntitiesComboBoxSelect() {
        if (!this.tabNameDropDownList || !this.tabNameProperty) {
            this.entityTabStrip.wrapper.hide();
            return;
        }

        this.entityTabStrip.wrapper.show();
        const selectedId = this.entitiesCombobox.dataItem().id;
        if (selectedId) {
            await this.getEntityPropertiesOfSelected(this.entitiesCombobox.dataItem().id);
        }
        
        $("#entityView .delBtn").toggleClass("hidden", !selectedId);

        // set tabnames 
        await this.setTabNameDropDown();

        // Refresh code mirrors, otherwise they won't work properly because they were invisible when they were initialized.
        this.queryAfterInsert.refresh();
        this.queryAfterUpdate.refresh();
        this.queryBeforeUpdate.refresh();
        this.queryBeforeDelete.refresh();
        this.searchQueryField.refresh();
        this.searchCountQueryField.refresh();
        this.templateQueryField.refresh();
        this.templateHtmlField.refresh();
    }

    async setTabNameDropDown() {
        this.tabNameDropDownList.text("");
        this.tabNameProperty.text("");
        const tabNames = await Wiser.api({
            url: `${this.base.settings.serviceRoot}/GET_ENTITY_PROPERTIES_TABNAMES?entityName=${encodeURIComponent(this.entitiesCombobox.dataItem().name)}`,
            method: "GET"
        });
        this.tabNameDropDownList.setDataSource(tabNames);
        this.tabNameProperty.setDataSource(tabNames);

        // set properties of tab
        this.tabNameDropDownList.select((dataItem) => {
            return dataItem.tabName === "Gegevens";
        });
    }

    // update property ordering
    async updateEntityPropertyOrdering(oldIndex, newIndex, id) {
        const selectedTab = this.tabNameDropDownList.dataItem();
        const tabName = !selectedTab || selectedTab.tabName === "Gegevens" ? "" : selectedTab.tabName;
        
        return Wiser.api({
            url: `${this.base.settings.serviceRoot}/UPDATE_ORDERING_ENTITY_PROPERTY`,
            method: "POST",
            data: {
                oldIndex: oldIndex,
                newIndex: newIndex,
                currentId: id,
                tabName: tabName,
                entityName: this.entitiesCombobox.dataItem().name
            }
        });
    }

    // get entity properties of tab when tabname is selected
    async tabNameDropDownListSelect(eventOrDataItem, forceReload = false) {
        if (this.tabNameDropDownListSelectBusy) {
            return;
        }
        
        const entityType = this.entitiesCombobox.dataItem().name;
        if (!eventOrDataItem || !entityType) {
            return;
        }
        
        let tabName = "";
        tabName = eventOrDataItem.sender && eventOrDataItem.sender.dataItem() ? eventOrDataItem.sender.dataItem().tabName : eventOrDataItem.tabName;
        tabName = tabName === "Gegevens" || !tabName ? "Gegevens" : tabName;
        
        if (!forceReload && this.previouslySelectedTab === tabName && this.previouslySelectedEntity === entityType) {
            return;
        }

        this.tabNameDropDownListSelectBusy = true;
        try {
            if (this.previouslySelectedEntity !== entityType) {
                await Wiser.api({
                    type: "PUT",
                    url: `${this.base.settings.wiserApiRoot}entity-properties/${encodeURIComponent(entityType)}/fix-ordering`,
                    contentType: "application/json"
                });
            }

            this.previouslySelectedTab = tabName;
            this.previouslySelectedEntity = entityType;

            this.listOfTabProperties.setDataSource(new kendo.data.DataSource({
                serverFiltering: true,
                transport: {
                    read: {
                        url: `${this.base.settings.serviceRoot}/GET_ENTITY_PROPERTIES_ADMIN?entityName=${encodeURIComponent(entityType)}&tabName=${encodeURIComponent(tabName)}`
                    }
                }
            }));
        } catch (exception) {
            console.error(exception);
            kendo.alert("Er is iets fout gegaan. Probeer het a.u.b. opnieuw of neem contact op met ons.");
        } finally {
            this.tabNameDropDownListSelectBusy = false;
        }
    }

    // get properties of entity fields and fill fields
    async optionSelected(event) {
        $("#EntityTabStrip-2 .right-pane").show();
        const index = event.sender.select().index();
        const dataItem = event.sender.dataItem(event.sender.select());
        const selectedEntityName = dataItem.entityName;
        const selectedTabName = dataItem.tabName;
        if (this.lastSelectedProperty === index && this.lastSelectedTabname === selectedTabName && !this.isSaveSelect) {
            this.base.openDialog("Item opnieuw openen", "Wilt u dit item opnieuw openen? (u raakt gewijzigde gegevens kwijt)", this.base.kendoPromptType.CONFIRM).then(() => {
                // get properties if user accepts to overwrite possible changes made to the same item
                this.getEntityFieldPropertiesOfSelected(dataItem.id, selectedEntityName, selectedTabName);
            });
        } else {
            this.isSaveSelect = false;
            this.lastSelectedProperty = index;
            this.lastSelectedTabname = selectedTabName;
            await this.getEntityFieldPropertiesOfSelected(dataItem.id, selectedEntityName, selectedTabName);
        }

        // Refresh code mirror isntances, otherwise they won't work properly because they were initialized while they were invisible.
        this.scriptField.refresh();
        this.optionsJsonField.refresh();
        this.queryField.refresh();
        this.queryFieldSubEntities.refresh();
        this.queryDeleteField.refresh();
        this.queryInsertField.refresh();
        this.queryUpdateField.refresh();
        this.queryContentField.refresh();
        this.searchQueryField.refresh();
        this.searchCountQueryField.refresh();
        this.aggregateOptionsField.refresh();
    }

    async getEntityPropertiesOfSelected(id) {
        const resultSet = await Wiser.api({
            url: `${this.base.settings.wiserApiRoot}entity-types/id/${id}`,
            contentType: 'application/json',
            method: "GET"
        });
        
        this.selectedEntityType = resultSet;

        this.setEntityPropertiesToDefault();
        this.setEntityProperties(resultSet);
    }

    async getEntityFieldPropertiesOfSelected(id, selectedEntityName, selectedTabName) {
        this.selectedEntityProperty = await Wiser.api({
            url: `${this.base.settings.wiserApiRoot}entity-properties/${id}`,
            method: "GET",
            contentType: 'application/json'
        });

        this.groupNameComboBox.setDataSource(new kendo.data.DataSource({
            transport: {
                read: {
                    url: `${this.base.settings.serviceRoot}/GET_GROUPNAME_FOR_SELECTION?selectedEntityName=${encodeURIComponent(selectedEntityName)}&selectedTabName=${encodeURIComponent(selectedTabName)}`
                }
            }
        }));

        this.dependencyFields.setDataSource(new kendo.data.DataSource({
            transport: {
                read: {
                    url: `${this.base.settings.serviceRoot}/GET_OPTIONS_FOR_DEPENDENCY?entityName=${encodeURIComponent(selectedEntityName)}`
                }
            }
        }));
        
        // first set all properties to default;
        this.setEntityFieldPropertiesToDefault();
        
        // then set all the properties accordingly
        this.setEntityFieldProperties(this.selectedEntityProperty);
    }

    // actions handled before save, such as checks
    async beforeSave() {
        // If no property is selected, we assume we only need to update the entity
        const typeToSave = (this.listOfTabProperties.select().index() === -1) ? "entity" : "entityProperty";

        // check if entity is selected
        if (!this.checkIfEntityIsSet(typeToSave)) {
            return false;
        }

        if (typeToSave === "entity") {
            await this.saveEntityProperties();
        } else {
            // check if tab is selected
            if (!this.tabNameDropDownList.dataItem()) {
                this.base.showNotification("notification", "Selecteer eerst een bestaand tab!", "error");
                return false;
            }

            // check if property is selected
            if (this.listOfTabProperties.select().index() === -1) {
                this.base.showNotification("notification", "Selecteer eerst een eigenschap!", "error");
                return false;
            }

            // check if group name isn't too long for db
            if (!this.groupNameComboBox.dataItem() && this.groupNameComboBox.value().length > 100) {
                this.base.showNotification("notification",
                    "Gebruik een groepsnaam die niet langer is dan 100 karakters!",
                    "error");
                return false;
            }

            // check if tab name isn't too long for db
            if (!this.tabNameProperty.dataItem() && this.tabNameProperty.value().length > 100) {
                this.base.showNotification("notification",
                    "Gebruik een tabnaam die niet langer is dan 100 karakters!",
                    "error");
                return false;
            }

            // check if input type is selected
            if ($("#inputtypes").closest(".item").is(":visible") && this.inputTypeSelector.dataItem().id === "") {
                this.base.showNotification("notification", "Selecteer eerst een bestaand invoertype!", "error");
                return false;
            }

            //inputtype specific
            const inputTypes = this.base.inputTypes;
            switch (this.inputTypeSelector.dataItem().text) {
                case inputTypes.NUMERICINPUT:
                    if (this.minNumber.value() && this.minNumber.value() >= this.maxNumber.value()) {
                        this.base.showNotification("notification",
                            "Minimale waarde mag niet hoger zijn dan de maximale waarde!",
                            "error");
                        return false;
                    }
                    break;
                case inputTypes.HTMLEDITOR:
                    // check if html editor format is set, checking with == instead of === because we're checking null and undefined.
                    if ($("[name=html-editor]").is(":visible") && $("[name=html-editor]:checked").val() == null) {
                        this.base.showNotification("notification", "Selecteer soort html editor - opmaak!", "error");
                        return false;
                    }
                    break;
                case inputTypes.DATETIMEPICKER:
                    // check if datetime dropdown is set
                    if ($("#dateTimeDropDown").closest(".item").is(":visible") &&
                        $("#dateTimeDropDown").data("kendoDropDownList").dataItem().value === "") {
                        this.base.showNotification("notification", "Selecteer soort datum/tijd picker!", "error");
                        return false;
                    }
                    break;
                case inputTypes.SECUREINPUT:
                    if ($("#securityMethod").closest(".item").is(":visible") &&
                        $("#securityMethod").data("kendoDropDownList").value() === "") {
                        this.base.showNotification("notification", "Selecteer soort beveiligingsmethode!", "error");
                        return false;
                    }
                    break;
                case inputTypes.LINKEDITEM:
                    if ($("#linkedItemEntity").closest(".item").is(":visible") && this.linkedItemEntity.value() === "") {
                        this.base.showNotification("notification", "Selecteer soort entiteit om te linken!", "error");
                        return false;
                    }

                    break;
                case inputTypes.ACTIONBUTTON:
                    break;
                case inputTypes.SUBENTITIESGRID:
                    if (document.getElementById("customQuery").checked && this.queryFieldSubEntities.getValue() === "") {
                        this.base.showNotification("notification", "Voer iets in bij query!", "error");
                        return false;
                    }
                    if (document.getElementById("hasCustomDeleteQuery").checked &&
                        this.queryDeleteField.getValue() === "") {
                        this.base.showNotification("notification", "Voer iets in bij de delete query!", "error");
                        return false;
                    }
                    if (document.getElementById("hasCustomUpdateQuery").checked &&
                        this.queryUpdateField.getValue() === "") {
                        this.base.showNotification("notification", "Voer iets in bij de update query!", "error");
                        return false;
                    }
                    if (document.getElementById("hasCustomInsertQuery").checked &&
                        this.queryInsertField.getValue() === "") {
                        this.base.showNotification("notification", "Voer iets in bij de insert query!", "error");
                        return false;
                    }
                    break;
                case inputTypes.TIMELINE:
                    if (!$("#queryId").data("kendoNumericTextBox").value()) {
                        this.base.showNotification("notification", "Vul een query id in!", "error");
                        return false;
                    }
                    //we need to select a entity type if we dont disable opening items
                    if (!this.timelineEntity.dataItem()) {
                        this.base.showNotification("notification",
                            "Selecteer soort entiteit om te linken wanneer de items te openen zijn!",
                            "error");
                        return false;
                    }
                    break;
                case inputTypes.DATERANGE:
                    // check if dates are set
                    if (!$("#daterangeFrom").data("kendoDatePicker").value() ||
                        !$("#daterangeTill").data("kendoDatePicker").value()) {
                        this.base.showNotification("notification", "Selecteer een datum!", "error");
                        return false;
                    }
                    // check if till date is later than the from date
                    if ($("#daterangeFrom").data("kendoDatePicker").value() >
                        $("#daterangeTill").data("kendoDatePicker").value()) {
                        this.base.showNotification("notification",
                            "Selecteer een datum die na de 'Van' datum ligt!",
                            "error");
                        return false;
                    }
                    break;
                case inputTypes.QUERYBUILDER:
                    if (!$("#queryId").data("kendoNumericTextBox").value()) {
                        this.base.showNotification("notification", "Vul een query id in!", "error");
                        return false;
                    }
                    break;
                case inputTypes.CHART:
                    const jsonFieldValue = this.optionsJsonField.getValue();
                    if (jsonFieldValue === "" || !this.base.isJson(jsonFieldValue)) {
                        this.base.showNotification("notification", "Vul de json data in van de chart opties!", "error");
                        return false;
                    }
                    break;
            }

            this.isSaveSelect = true;
            // if everything went right, we move on to the save function.
            await this.saveEntityFieldProperties();
        }
    }

    async saveEntityProperties() {
        try {
            const entity = new EntityModel();
            const entityDataItem = this.entitiesCombobox.dataItem();
            const oldName = entityDataItem.name === "ROOT" ? "" : entityDataItem.name;
            const oldModuleId = entityDataItem.moduleId;

            entity.id = entityDataItem.id;
            entity.entityType = document.getElementById("entityName").value;

            entity.moduleId = this.entityModule.value();
            entity.acceptedChildtypes = this.acceptedChildTypes.value();
            entity.icon = this.entityIcon.value();
            entity.iconAdd = this.entityIconAdd.value();
            entity.iconExpanded = this.entityIconExpanded.value();
            entity.defaultOrdering = this.defaultOrdering.value();
            entity.color = this.entityColor.value();
            entity.deleteAction = this.deleteAction.value();

            entity.showInTreeView = document.getElementById("showInTreeView").checked;
            entity.showInSearch = document.getElementById("showInSearch").checked;
            entity.showOverviewTab = document.getElementById("showInOverviewTab").checked;
            entity.saveTitleAsSeo = document.getElementById("saveTitleAsSEO").checked;
            entity.showTitleField = document.getElementById("showTitleField").checked;
            entity.saveHistory = document.getElementById("saveHistory").checked;
            entity.showInDashboard = document.getElementById("showInDashboard").checked;
            entity.enableMultipleEnvironments = document.getElementById("enableMultipleEnvironments").checked;

            entity.displayName = document.getElementById("friendlyName").value;
            entity.queryAfterInsert = this.queryAfterInsert.getValue();
            entity.queryAfterUpdate = this.queryAfterUpdate.getValue();
            entity.queryBeforeUpdate = this.queryBeforeUpdate.getValue();
            entity.queryBeforeDelete = this.queryBeforeDelete.getValue();
            entity.templateQuery = this.templateQueryField.getValue();
            entity.templateHtml = this.templateHtmlField.getValue();

            entity.dedicatedTablePrefix = document.getElementById("entityDedicatedTablePrefix").value;

            document.querySelector(".loaderWrap").classList.add("active");

            //save to database
            await Wiser.api({
                type: "PUT",
                url: `${this.base.settings.wiserApiRoot}entity-types/${entity.id}`,
                contentType: "application/json",
                data: JSON.stringify(entity)
            });

            this.base.showNotification("notification", `Item succesvol aangepast`, "success");
            document.querySelector(".loaderWrap").classList.remove("active");

            if (oldName !== entity.entityType || parseInt(oldModuleId) !== parseInt(entity.moduleId)) {
                await this.reloadEntityList(true);

                this.entitiesCombobox.one("dataBound",
                    () => {
                        this.entitiesCombobox.select((dataItem) => {
                            return dataItem.name === entity.entityType;
                        });
                    });
            }
        } catch (exception) {
            console.error(exception);
            if (e.responseText.indexOf("Duplicate entry")) {
                this.base.showNotification("notification",
                    `Er bestaat al een entiteit met naam '${entity.name}' gekoppeld aan de module ${this.entityModule
                    .dataItem().moduleName}`,
                    "error");
            } else {
                this.base.showNotification("notification",
                    `Entiteit is niet succesvol aangepast, probeer het opnieuw`,
                    "error");
            }
            document.querySelector(".loaderWrap").classList.remove("active");
        }
    }

    // save entity properties to database
    async saveEntityFieldProperties() {
        // create entity property model
        const entityProperties = new EntityPropertyModel();
        let index = this.listOfTabProperties.select().index();
        let dataItem = this.listOfTabProperties.dataSource.view()[index];
        entityProperties.id = dataItem.id;
        entityProperties.moduleId = this.selectedEntityType.moduleId;
        entityProperties.entityType = this.entitiesCombobox.dataItem().name;
        entityProperties.linkType = 0;
        entityProperties.tabName = this.tabNameProperty.value();
        if (entityProperties.tabName === "Gegevens") {
            entityProperties.tabName = "";
        }
        entityProperties.groupName = this.groupNameComboBox.value();
        entityProperties.inputType = this.inputTypeSelector.value();
        entityProperties.displayName = $("#displayname").val();
        entityProperties.propertyName = $("#propertyname").val();
        entityProperties.explanation = $("textarea#explanation").data("kendoEditor").value();
        entityProperties.regexValidation = $('#regexValidation').val();
        entityProperties.mandatory = $("#mandatory").is(":checked");
        entityProperties.readOnly = $("#readonly").is(":checked");
        entityProperties.width = $("#width").data("kendoNumericTextBox").value();
        entityProperties.height = $("#height").data("kendoNumericTextBox").value();
        entityProperties.languageCode = $('#langCode').val();
        entityProperties.customScript = this.scriptField.getValue();
        entityProperties.alsoSaveSeoValue = document.getElementById("seofriendly").checked;
        entityProperties.defaultValue = $('#defaultValue').val();
        entityProperties.visibleInOverview = document.getElementById("visible-in-table").checked;
        entityProperties.ordering = this.selectedEntityProperty.ordering;
        entityProperties.extendedExplanation = document.getElementById("extendedExplanation").checked;
        entityProperties.saveOnChange = document.getElementById("saveOnChange").checked;
        entityProperties.saveOnEnter = document.getElementById("saveOnEnter").checked;
        entityProperties.labelStyle = this.labelStyle.value();
        entityProperties.labelWidth = this.labelWidth.value();
        entityProperties.accessKey = $("#accessKey").val();
        entityProperties.visibilityPathRegex = $("#visibilityPathRegex").val();
        entityProperties.enableAggregation = document.getElementById("enableAggregation").checked;
        entityProperties.aggregateOptions = this.aggregateOptionsField.getValue();
        
        entityProperties.dependsOn = {
            field: this.dependencyFields.value(),
            operator: this.dependencyOperator.value(),
            value: $("#dependingValue").val(),
            action: this.dependencyAction.value()
        };
        
        entityProperties.overview = {
            visible: document.getElementById("visible-in-table").checked,
            width: this.widthInTable.value()
        };

        // declare empty options
        const optionsJsonValue = this.optionsJsonField.getValue();
        if (optionsJsonValue && this.base.isJson(optionsJsonValue)) {
            entityProperties.options = JSON.parse(optionsJsonValue);
        } else {
            entityProperties.options = {};
        }

        //inputtype specific
        const inputTypes = this.base.inputTypes;
        switch (this.inputTypeSelector.text()) {
            case inputTypes.RADIOBUTTON:
                entityProperties.defaultValue = $("#checkedCheckbox").data("kendoDropDownList").value();
                entityProperties.dataQuery = this.queryContentField.getValue();
                break;
            case inputTypes.CHECKBOX:
                // set default value to checkbox checked(1) or unchecked (0)
                entityProperties.defaultValue = $("#checkedCheckbox").data("kendoDropDownList").value();
                entityProperties.options.mode = this.checkBoxMode.value();
                entityProperties.options.imageId = this.checkBoxImageId.value();
                entityProperties.options.imageUrl = $("#checkBoxImageUrl").val();
                break;
            case inputTypes.NUMERICINPUT:
                entityProperties.options.decimals = this.numberOfDec.value();
                entityProperties.options.format = this.numberFormat.value() === "anders" ? document.getElementById("differentFormat").value : this.numberFormat.value();
                entityProperties.options.round = document.getElementById("roundNumeric").checked;
                entityProperties.options.max = this.maxNumber.value();
                entityProperties.options.min = this.minNumber.value();
                entityProperties.options.step = this.stepNumber.value() || 1;
                entityProperties.options.factor = this.factorNumber.value() || 1;
                const culture = document.getElementById("cultureNumber").value;
                entityProperties.options.culture = culture === "" ? null : culture;
                entityProperties.defaultValue = $("#defaultNumeric").val();
                break;
            case inputTypes.AUTOINCREMENT:
                entityProperties.defaultValue = $("#defaultNumeric").val();
                break;
            case inputTypes.HTMLEDITOR:
                entityProperties.options.mode = parseInt($("[name=html-editor]:checked").val());
                break;
            case inputTypes.DATETIMEPICKER:
                entityProperties.options.type = $("#dateTimeDropDown").data("kendoDropDownList").value();
                // we only need a value if checkbox is checked and type is date or datetime
                entityProperties.options.value = document.getElementById("dateTimePickerSetNow").checked && (entityProperties.options.type === "date" || entityProperties.options.type === "datetime") ? "NOW()" : null;
                //getting mintime by value of the element, not the kendo element; it returns a full datetime range.
                var minTime = document.getElementById("minimumTime").value;
                // we only need minimum time if type is type or datetime
                entityProperties.options.min = (entityProperties.options.type === "time" || entityProperties.options.type === "datetime") && minTime !== "" ? minTime : null;
                break;
            case inputTypes.COMBOBOX:
            case inputTypes.MULTISELECT:
                if (this.inputTypeSelector.text() === inputTypes.COMBOBOX) {
                    entityProperties.options.useDropDownList = document.getElementById("useDropDownList").checked;
                } else {
                    entityProperties.options.useDropDownList = null;
                }
                
                entityProperties.options.mode = this.multiSelectMode.value();
                entityProperties.options.mainImageId = this.multiSelectMainImageId.value();
                entityProperties.options.mainImageUrl = $("#multiSelectMainImageUrl").val();
                entityProperties.options.imagePropertyName = $("#multiSelectImagePropertyName").val();
                
                // check if panel 1 is selected, which is "Vaste waardes"
                if (this.dataSourceFilter.dataItem().id === this.base.dataSourceType.PANEL1.id) {
                    var data = this.grid.dataSource.data();
                    var dataSource = [];
                    // specific check if all itemrows are filled.
                    for (var i = 0; i < data.length; i++) {
                        if (data[i].id == null || data[i].id === "" || data[i].name == null || data[i].name === "") {
                            this.base.showNotification("notification", `Vul bij "Vaste waardes" alle items met naam en id in!`, "error");
                            return;
                        }
                        dataSource.push({id: data[i].id, name: data[i].name});
                    }
                    entityProperties.options.dataSource = dataSource;
                    entityProperties.options.entityType = null;
                    entityProperties.options.searchInTitle = null;
                    entityProperties.options.searchEverywhere = null;

                    // check if panel 2 is selected, which is "Lijst van entiteiten"
                } else if (this.dataSourceFilter.dataItem().id === this.base.dataSourceType.PANEL2.id) {
                    // check if entity to search for is set, show error if not
                    if (!this.dataSourceEntities.dataItem()) {
                        this.base.showNotification("notification", `Selecteer eerst een entiteit waar naar gezocht moet worden!`, "error");
                        return;
                    }
                    entityProperties.options.entityType = this.dataSourceEntities.dataItem().name;
                    entityProperties.options.dataSource = null;
                    entityProperties.options.searchInTitle = document.getElementById("searchInTitle").checked;
                    entityProperties.options.searchEverywhere = document.getElementById("searchEverywhere").checked;
                    entityProperties.options.searchFields = this.searchFields.value();

                    // overwrite the preserved options, else the options would 
                    this.fieldOptions.searchFields = this.searchFields.value();

                    // check if panel 1 is selected, which is "Query"
                } else if (this.dataSourceFilter.dataItem().id === this.base.dataSourceType.PANEL3.id) {
                    // get value through codemirror function getValue() because textarea is empty
                    entityProperties.dataQuery = this.queryField.getValue();
                    entityProperties.options.dataSource = null;
                    entityProperties.options.entityType = null;
                    entityProperties.options.searchInTitle = null;
                    entityProperties.options.searchEverywhere = null;
                }
                break;
            case inputTypes.SECUREINPUT:
                entityProperties.options.type = $("#typeSecureInput").data("kendoDropDownList").value();
                entityProperties.options.securityMethod = $("#securityMethod").data("kendoDropDownList").value();
                // set securitykey, but only when security method is JCL_AES
                if (entityProperties.options.securityMethod === "JCL_AES" || entityProperties.options.securityMethod === "AES") {
                    entityProperties.options.securityKey = document.getElementById("securityKey").value;
                } else {
                    entityProperties.options.securityKey = null;
                }
                break;
            case inputTypes.LINKEDITEM:
                entityProperties.options.entityType = this.linkedItemEntity.value();
                entityProperties.options.template = document.getElementById("linkedItemTemplate").value;
                entityProperties.options.noLinkText = document.getElementById("noLinkText").value;
                entityProperties.options.reverse = document.getElementById("reverse").checked;
                entityProperties.options.hideFieldIfNoLink = document.getElementById("hideFieldIfNoLink").checked;
                entityProperties.options.textOnly = document.getElementById("textOnly").checked;
                // no check if filled because these are not required properties
                // use dataItem().type because value() gives back the string value instead of int value
                entityProperties.options.linkType = !this.linkType.dataItem() && this.linkType.value() === "" ? 1 : (!this.linkType.dataItem() ? parseInt(this.linkType.value()) : this.linkType.dataItem().typeValue);
                break;
            case inputTypes.DATASELECTOR:
                entityProperties.options.text = document.getElementById("dataSelectorText").value;
                break;
            case inputTypes.ITEMLINKER:
            case inputTypes.SUBENTITIESGRID:
            case inputTypes.ACTIONBUTTON:

                // shared properties through out sub entities grid and item linker
                if (this.inputTypeSelector.text() !== inputTypes.ACTIONBUTTON) {
                    // check if set, if not use the manual input
                    entityProperties.options.linkTypeNumber = this.itemLinkerTypeNumber.value();
                    entityProperties.options.hideCommandColumn = document.getElementById("hideCommandColumn").checked;
                    entityProperties.options.disableInlineEditing = document.getElementById("disableInlineEditing").checked;
                    entityProperties.options.disableOpeningOfItems = document.getElementById("disableOpeningOfItems").checked;
                    entityProperties.options.deletionOfItems = $("#itemLinkerDeletionOfItems").data("kendoDropDownList").value();
                    // toolbar is an extra object within the options
                    entityProperties.options.toolbar = {};
                    entityProperties.options.toolbar.hideExportButton = document.getElementById("hideExportButton").checked;
                    entityProperties.options.toolbar.hideCheckAllButton = document.getElementById("hideCheckAllButton").checked;
                    entityProperties.options.toolbar.hideUncheckAllButton = document.getElementById("hideUncheckAllButton").checked;
                    entityProperties.options.toolbar.hideCreateButton = document.getElementById("hideCreateButton").checked;
                }

                // module id is only available for item linker 
                // entity is a multiselect for item linker
                if (this.inputTypeSelector.text() === inputTypes.ITEMLINKER) {
                    const moduleId = $("#itemLinkerModuleId").data("kendoNumericTextBox").value();
                    // 0 is the default value
                    entityProperties.options.moduleId = moduleId === "" ? 0 : moduleId;
                    // .value returns a string array of all selected entities
                    entityProperties.options.entityTypes = this.itemLinkerEntity.value();

                    // overwrite the preserved options, else the options would 
                    this.fieldOptions.entityTypes = this.itemLinkerEntity.value();

                    // order by is itemlinker only
                    entityProperties.options.orderBy = document.getElementById("itemLinkerOrderBy").value;
                } else {
                    const buttons = [];
                    const actionGridData = this.actionButtonGrid.dataSource.data();
                    // loop through the data of the actionButtonGrid
                    for (let i = 0; i < actionGridData.length; i++) {
                        // create action array
                        const actions = [];
                        const actionButtonData = (actionGridData[i].button ? actionGridData[i].button.actions : []);
                        // loop through actions defined in the dataItem of the currently iterated item
                        for (let i = 0; i < actionButtonData.length; i++) {
                            // push nothing if no type is selected
                            if (!actionButtonData[i] || actionButtonData[i].type === "") {
                                continue;
                                // push just the type if type is refreshcurrentitem or custom
                            } else {
                                actions.push(actionButtonData[i]);
                            }
                        }
                        buttons.push({
                            text: actionGridData[i].text,
                            icon: actionGridData[i].icon,
                            actions: actions
                        });
                    }
                    if (this.inputTypeSelector.text() === inputTypes.ACTIONBUTTON) {
                        if (buttons.length === 0 || buttons[0].actions.length === 0) {
                            console.warn("entityProperties.options.actions is missing!", entityProperties.options);
                            this.base.showNotification("notification", `Item is niet succesvol toegevoegd, actie(s) ontbreken, probeer het opnieuw`, "error");
                            return;
                        }
                        entityProperties.options.text = buttons[0].text || "";
                        entityProperties.options.icon = buttons[0].icon || "";
                        entityProperties.options.actions = buttons[0].actions;
                    } else {
                        // toolbar object has been created above already, but still we're checking it once more.
                        if (!entityProperties.options.toolbar) {
                            console.warn("entityProperties.options.toolbar is missing!", entityProperties.options);
                            this.base.showNotification("notification", `Item is niet succesvol aangepast, probeer het opnieuw`, "error");
                            return;
                        }
                        entityProperties.options.toolbar.customActions = buttons;
                    }
                }

                // properties for sub entities grid
                if (this.inputTypeSelector.text() === inputTypes.SUBENTITIESGRID) {
                    entityProperties.options.dataSelectorId = this.dataSelectorIdSubEntitiesGrid.value();
                    entityProperties.options.entityType = this.subEntityGridEntity.value();
                    entityProperties.options.selectable = (this.subEntitiesGridSelectOptions.value() === "false") ? false : this.subEntitiesGridSelectOptions.value();
                    entityProperties.options.refreshGridAfterInlineEdit = document.getElementById("refreshGridAfterInlineEdit").checked;
                    entityProperties.options.showDeleteConformations = document.getElementById("showDeleteConformations").checked;
                    entityProperties.options.checkboxes = document.getElementById("checkboxes").checked;
                    entityProperties.options.keepFiltersState = document.getElementById("keepFiltersState").checked;

                    entityProperties.options.showChangedByColumn = document.getElementById("showChangedByColumn").checked;
                    entityProperties.options.showChangedOnColumn = document.getElementById("showChangedOnColumn").checked;
                    entityProperties.options.showAddedByColumn = document.getElementById("showAddedByColumn").checked;
                    entityProperties.options.showAddedOnColumn = document.getElementById("showAddedOnColumn").checked;

                    entityProperties.options.customQuery = document.getElementById("customQuery").checked;
                    if (entityProperties.options.customQuery) {
                        entityProperties.dataQuery = this.queryFieldSubEntities.getValue();
                    }

                    entityProperties.options.hasCustomDeleteQuery = document.getElementById("hasCustomDeleteQuery").checked;
                    if (entityProperties.options.hasCustomDeleteQuery) {
                        entityProperties.gridDeleteQuery = this.queryDeleteField.getValue();
                    }

                    entityProperties.options.hasCustomUpdateQuery = document.getElementById("hasCustomUpdateQuery").checked;
                    if (entityProperties.options.hasCustomUpdateQuery) {
                        entityProperties.gridUpdateQuery = this.queryUpdateField.getValue();
                    }

                    entityProperties.options.hasCustomInsertQuery = document.getElementById("hasCustomInsertQuery").checked;
                    if (entityProperties.options.hasCustomInsertQuery) {
                        entityProperties.gridInsertQuery = this.queryInsertField.getValue();
                    }

                    entityProperties.searchQuery = this.searchQueryField.getValue();
                    entityProperties.searchCountQuery = this.searchCountQueryField.getValue();
                    entityProperties.options.disableInlineEditing = document.getElementById("disableInlineEditing").checked;
                    entityProperties.options.disableOpeningOfItems = document.getElementById("disableOpeningOfItems").checked;
                    entityProperties.options.hideTitleColumn = document.getElementById("hideTitleColumn").checked;
                    entityProperties.options.hideEnvironmentColumn = document.getElementById("hideEnvironmentColumn").checked;
                    entityProperties.options.hideTypeColumn = document.getElementById("hideTypeColumn").checked;
                    entityProperties.options.hideLinkIdColumn = document.getElementById("hideLinkIdColumn").checked;
                    entityProperties.options.hideIdColumn = document.getElementById("hideIdColumn").checked;
                    entityProperties.options.hideTitleFieldInWindow = document.getElementById("hideTitleFieldInWindow").checked;
                    entityProperties.options.toolbar.hideLinkButton = document.getElementById("hideLinkButton").checked;
                    entityProperties.options.toolbar.hideCount = document.getElementById("hideCount").checked;
                    entityProperties.options.toolbar.hideClearFiltersButton = document.getElementById("hideClearFiltersButton").checked;
                }
                break;
            case inputTypes.TIMELINE:
                entityProperties.options.entityType = this.timelineEntity.dataItem().id;
                entityProperties.options.queryId = $("#queryId").data("kendoNumericTextBox").value();
                entityProperties.options.eventHeight = $("#timelineEventHeight").data("kendoNumericTextBox").value() || 600;
                entityProperties.options.disableOpeningOfItems = document.getElementById("disableOpeningOfItemsTimeLine").checked;
                break;
            case inputTypes.FILEUPLOAD:
            case inputTypes.IMAGEUPLOAD: {
                entityProperties.options.validation = {};
                entityProperties.options.queryId = $("#queryId").data("kendoNumericTextBox").value();
                entityProperties.options.multiple = document.getElementById("allowMultipleFiles").checked;
                const allowedExtensions = document.getElementById("allowedExtensions").value;
                entityProperties.options.validation.allowedExtensions = allowedExtensions && allowedExtensions !== '' ? allowedExtensions.split(',') : [];
                break;
            }
            case inputTypes.SCHEDULER:
                entityProperties.options.queryId = $("#queryId").data("kendoNumericTextBox").value();
                break;
            case inputTypes.DATERANGE:
                // TODO how to save values?
                // kendo.toString(new Date(), "MM/dd/yyyy")
                entityProperties.options.from = $("#daterangeFrom").data("kendoDatePicker").value();
                entityProperties.options.till = $("#daterangeTill").data("kendoDatePicker").value();
                break;
            case inputTypes.QUERYBUILDER:
                // TODO is this the correct way?
                entityProperties.options.queryId = $("#queryId").data("kendoNumericTextBox").value();
                break;
            case inputTypes.CHART:
                break;
            case inputTypes.TEXTBOX:
                entityProperties.options.type = this.textboxTypeDropDown.dataItem() && this.textboxTypeDropDown.dataItem().id !== "" ? this.textboxTypeDropDown.dataItem().id : null;
                break;
            case inputTypes.QR:
                entityProperties.options.size = parseInt((document.getElementById("pixelSize").value == null || document.getElementById("pixelSize").value == "") ? 250 : document.getElementById("pixelSize").value);
                // get value through codemirror function getValue() because textarea is empty
                entityProperties.dataQuery = this.queryContentField.getValue();
                break;
        }
        
        function clearAutoIncIdsFromObject(targetObject = {}) {
            for (let prop in targetObject) {
                if (targetObject.hasOwnProperty(prop)) {
                    const value = targetObject[prop];
                    if (prop === "autoIndex") delete targetObject[prop];
                    if (typeof value === "object") clearAutoIncIdsFromObject(value);
                }
            }
        }

        // we create the json for chart in the module
        if (this.inputTypeSelector.text() !== inputTypes.CHART) {
            // when the admin tool hasnt been updated to handle options that might appear in the options json, dont want to lose any options that were entered previously
            entityProperties.options = $.extend(true, this.fieldOptions, entityProperties.options);

            clearAutoIncIdsFromObject(entityProperties.options);
            // populate options field with json
            entityProperties.createOptionsJson();
        }

        document.querySelector(".loaderWrap").classList.add("active");
        
        try {
            // save to database
            await Wiser.api({
                type: "PUT",
                url: `${this.base.settings.wiserApiRoot}entity-properties/${entityProperties.id}`,
                contentType: "application/json",
                data: JSON.stringify(entityProperties)
            });
            
            this.base.showNotification("notification", `Item succesvol aangepast`, "success");
            this.afterSave(entityProperties);
            document.querySelector(".loaderWrap").classList.remove("active");
        }
        catch (exception) {
            console.error("Error while saving initial values", exception);
            this.base.showNotification("notification", `Item is niet succesvol aangepast, probeer het opnieuw`, "error");
        }
    }

    selectPropertyInListView(displayName) {
        // remove selected class
        $(".sortable").removeClass("selected");
        const elementToSelect = this.listOfTabProperties.element.find(`[data-display-name='${displayName}']`);
        // select element in listview
        this.listOfTabProperties.select(elementToSelect);
        // add selected class
        elementToSelect.addClass('selected');
    }

    removeOnAutoIndex(targetObject = {}, autoIndexId = 0) {
        let found = false;

        const findIndex = (target, index, parent = null, targetName = null) => {
            if (found) return;
            for (let prop in target) {
                if (target.hasOwnProperty(prop)) {
                    if (found) return;
                    const value = target[prop];
                    if (prop === "autoIndex" && value == index) {
                        // Found, remove depending on type
                        if (Utils.isArray(parent)) {
                            // base type is an array item
                            parent.splice(parent.findIndex(e => e === target), 1);
                        } else {
                            // Just a property
                            delete parent[targetName];
                        }
                        found = true;
                        return;
                    }

                    if (Utils.isArray(value)) {
                        // Loop through array items
                        for (let arrItem of value) {
                            if (found) return;
                            if (typeof value === "object") findIndex(value, index, target, null);
                        }
                    } else if (typeof value === "object") {
                        findIndex(value, index, target, prop);
                    }
                }
            }
        }

        findIndex(targetObject, autoIndexId);

        return found;
    }

    // actions handled after saving, selecting right tab and such
    afterSave(entityProperties) {
        const selectCorrectListViewItem = () => {
            // select correct entity display name
            this.listOfTabProperties.one("dataBound",
                () => {
                    this.selectPropertyInListView(entityProperties.displayName);
                });
        };

        // only update tabname list if tab has been added/changed
        if (this.tabNameDropDownList.value() !== entityProperties.tabName) {
            // trigger select to get the new tab in the list
            this.onEntitiesComboBoxSelect(this);
            this.tabNameDropDownList.one("dataBound", (event) => {
                // automatically select the newly added tab 
                this.tabNameDropDownList.select((dataItem) => {
                    return dataItem.tabName === (entityProperties.tabName === "" ? "Gegevens" : entityProperties.tabName);
                });
                selectCorrectListViewItem();
            });
        }

        const index = this.listOfTabProperties.select().index();
        const dataItem = this.listOfTabProperties.dataSource.view()[index];
        // only update tabname properties list if display name has been changed
        if (this.tabNameDropDownList.value() === entityProperties.tabName && dataItem !== null && dataItem !== undefined && dataItem.displayName !== entityProperties.displayName) {
            this.tabNameDropDownListSelect(dataItem);
            selectCorrectListViewItem();
        } else if (dataItem !== null && dataItem !== undefined && this.tabNameDropDownList.value() === entityProperties.tabName) {
            // update if name and tab havent been changed
            this.isSaveSelect = false;
            this.getEntityFieldPropertiesOfSelected(dataItem.id, this.entitiesCombobox.dataItem().name, this.tabNameDropDownList.dataItem().tabName);
        }
    }

    /**
     *
     * @param {any} curValue show or hide the sent value
     */
    hideShowElementsBasedOnValue(curValue) {
        if (!curValue) {
            return;
        }
        
        // Show all related inputs.
        $(".item[data-visible], label[data-visible]").each((index, element) => {
            const items = element.dataset.visible.split(" ");
            const filteredResults = items.filter(item => item.trim().toLowerCase() === curValue.toLowerCase());
            $(element).toggle(filteredResults.length > 0);
        });
        
        // Hide all related inputs.
        $(".item[invisible], label[invisible]").each((index, element) => {
            const items = element.dataset.visible.split(" ");
            const filteredResults = items.filter(item => item.trim().toLowerCase() === curValue.toLowerCase());
            $(element).toggle(filteredResults.length === 0);
        });
        
        // Set the input name in the field group.
        $("#inputTypeNameLegend").text(curValue);
    }

    setEntityPropertiesToDefault() {
        // checkboxes
        document.getElementById("showInTreeView").checked = false;
        document.getElementById("showInSearch").checked = false;
        document.getElementById("showInOverviewTab").checked = false;
        document.getElementById("saveTitleAsSEO").checked = false;
        document.getElementById("showTitleField").checked = false;
        document.getElementById("saveHistory").checked = false;
        document.getElementById("showInDashboard").checked = false;
        document.getElementById("enableMultipleEnvironments").checked = false;

        // codemirror fields
        this.queryAfterInsert.setValue("");
        this.queryAfterUpdate.setValue("");
        this.queryBeforeUpdate.setValue("");
        this.queryBeforeDelete.setValue("");
        this.templateQueryField.setValue("");
        this.templateHtmlField.setValue("");
    }

    setEntityFieldPropertiesToDefault() {
        // Set default values for all properties that are filled based off input type
        // we dont need to fill the variables that are shown always, the db value should be sufficient

        // numeric default
        this.defaultNumeric.value(0);
        this.numberOfDec.value(2);
        this.numberFormat.select("");
        $("#differentFormatHolder").hide();
        document.getElementById("differentFormat").value = "";
        document.getElementById("roundNumeric").checked = true;
        this.maxNumber.value("");
        this.minNumber.value("");
        this.stepNumber.value(1);
        this.factorNumber.value(1);
        document.getElementById("cultureNumber").value = "";

        // htmleditor default
        document.querySelector("[id*=mode]:checked") ? document.querySelector("[id*=mode]:checked").checked = false : $.noop();

        // datetime default
        $("#dateTimeDropDown").data("kendoDropDownList").select("");
        document.getElementById("dateTimePickerSetNow").checked = false;
        this.minTimeBox.value("");

        // multi select / combobox default
        this.grid.setDataSource(null);
        this.dataSourceEntities.select("");
        this.dataSourceFilter.select(0);
        this.searchFields.value([]);
        document.getElementById("useDropDownList").checked = false;
        document.getElementById("searchInTitle").checked = false;
        document.getElementById("searchEverywhere").checked = false;

        // secure input default
        $("#typeSecureInput").data("kendoDropDownList").select(0);
        $("#securityMethod").data("kendoDropDownList").select(0);
        document.getElementById("securityKey").value = "";

        // linked item default
        this.linkedItemEntity.select("");
        this.linkType.value("");
        document.getElementById("textOnly").checked = false;
        document.getElementById("linkedItemTemplate").value = "";

        // data selector default
        document.getElementById("dataSelectorText").value = "";

        //item linker default
        $("#itemLinkerModuleId").data("kendoNumericTextBox").value("");
        this.itemLinkerEntity.value("");
        this.itemLinkerTypeNumber.value("");
        $("#itemLinkerDeletionOfItems").data("kendoDropDownList").select(0);
        document.getElementById("itemLinkerOrderBy").value = "";
        document.querySelectorAll("#item-linker-checkboxes input[type=checkbox]:checked")
            .forEach((element) => {
                element.checked = false;
            });

        // dependencies
        this.dependencyFields.select("");
        this.dependencyOperator.select("");
        this.dependencyAction.select("");

        // action button
        this.actionButtonGrid.setDataSource([]);

        // sub entity grid entity selector
        this.subEntityGridEntity.select("");
        this.dataSelectorIdSubEntitiesGrid.value("");
        this.subEntitiesGridSelectOptions.select("");
        document.getElementById("customQuery").checked = false;
        document.getElementById("hasCustomDeleteQuery").checked = false;
        document.getElementById("hasCustomUpdateQuery").checked = false;
        document.getElementById("hasCustomInsertQuery").checked = false;
        document.getElementById("hideCommandColumn").checked = false;
        document.getElementById("disableInlineEditing").checked = false;
        document.getElementById("disableOpeningOfItems").checked = false;
        document.getElementById("hideExportButton").checked = false;
        document.getElementById("hideCheckAllButton").checked = false;
        document.getElementById("hideUncheckAllButton").checked = false;
        document.getElementById("hideCreateButton").checked = false;
        document.getElementById("refreshGridAfterInlineEdit").checked = false;
        document.getElementById("showDeleteConformations").checked = false;
        document.getElementById("checkboxes").checked = false;
        document.getElementById("showChangedByColumn").checked = false;
        document.getElementById("showChangedOnColumn").checked = false;
        document.getElementById("showAddedByColumn").checked = false;
        document.getElementById("showAddedOnColumn").checked = false;
        document.getElementById("hideLinkButton").checked = false;
        document.getElementById("hideClearFiltersButton").checked = false;
        document.getElementById("hideTitleFieldInWindow").checked = false;
        document.getElementById("hideCount").checked = false;
        document.getElementById("hideTitleColumn").checked = false;
        document.getElementById("hideEnvironmentColumn").checked = false;
        document.getElementById("hideTypeColumn").checked = false;
        document.getElementById("hideLinkIdColumn").checked = false;
        document.getElementById("hideIdColumn").checked = false;


        // timeline
        this.timelineEntity.select("");
        $("#timelineEventHeight").data("kendoNumericTextBox").value("");
        document.getElementById("disableOpeningOfItemsTimeLine").checked = false;
        // timeline / querybuilder shared
        $("#queryId").data("kendoNumericTextBox").value("");

        // daterange
        $("#daterangeFrom").data("kendoDatePicker").value("");
        $("#daterangeTill").data("kendoDatePicker").value("");

        // codemirror fields
        this.scriptField.setValue("");
        this.optionsJsonField.setValue("");
        this.queryField.setValue("");
        this.queryFieldSubEntities.setValue("");
        this.queryDeleteField.setValue("");
        this.queryInsertField.setValue("");
        this.queryUpdateField.setValue("");
        this.queryContentField.setValue("");
        this.searchQueryField.setValue("");
        this.searchCountQueryField.setValue("");
        this.aggregateOptionsField.setValue("");

        //textbox
        this.textboxTypeDropDown.select(0);

        // set options field to default, empty object
        this.fieldOptions = {};
    }

    setEntityProperties(resultSet) {
        document.getElementById("entityName").value = resultSet.entityType || "";
        document.getElementById("showInTreeView").checked = resultSet.showInTreeView;
        document.getElementById("showInSearch").checked = resultSet.showInSearch;
        document.getElementById("showInOverviewTab").checked = resultSet.showOverviewTab;
        document.getElementById("showTitleField").checked = resultSet.showTitleField;
        document.getElementById("saveHistory").checked = resultSet.saveHistory;
        document.getElementById("saveTitleAsSEO").checked = resultSet.saveTitleAsSeo;
        document.getElementById("friendlyName").value = resultSet.displayName || "";
        document.getElementById("entityDedicatedTablePrefix").value = resultSet.dedicatedTablePrefix;
        document.getElementById("showInDashboard").checked = resultSet.showInDashboard;
        document.getElementById("enableMultipleEnvironments").checked = resultSet.enableMultipleEnvironments;

        // CodeMirror fields
        this.setCodeMirrorFields(this.queryAfterInsert, resultSet.queryAfterInsert);
        this.setCodeMirrorFields(this.queryAfterUpdate, resultSet.queryAfterUpdate);
        this.setCodeMirrorFields(this.queryBeforeUpdate, resultSet.queryBeforeUpdate);
        this.setCodeMirrorFields(this.queryBeforeDelete, resultSet.queryBeforeDelete);
        this.setCodeMirrorFields(this.templateQueryField, resultSet.templateQuery);
        this.setCodeMirrorFields(this.templateHtmlField, resultSet.templateHtml);

        //Select items in combobox 
        this.entityIcon.select("");
        this.entityIcon.select((dataItem) => {
            return dataItem.value === resultSet.icon;
        });

        this.entityIconAdd.select("");
        this.entityIconAdd.select((dataItem) => {
            return dataItem.value === resultSet.iconAdd;
        });

        this.entityIconExpanded.select("");
        this.entityIconExpanded.select((dataItem) => {
            return dataItem.value === resultSet.iconExpanded;
        });

        this.defaultOrdering.select("");
        this.defaultOrdering.select((dataItem) => {
            return dataItem.value === resultSet.defaultOrdering;
        });

        this.entityColor.select("");
        this.entityColor.select((dataItem) => {
            return dataItem.value === resultSet.color;
        });

        this.deleteAction.select("");
        this.deleteAction.select((dataItem) => {
            return dataItem.value == resultSet.deleteAction;
        });

        this.getEntityModules(resultSet.moduleId);
        this.getAcceptedChildTypes(resultSet.moduleId, resultSet.acceptedChildTypes);
    }

    async getEntityModules(moduleId) {
        this.entityModule.select("");
        const dsEntityModules = await Wiser.api({
            url: `${this.base.settings.serviceRoot}/GET_MODULES`,
            method: "GET"
        });
        this.entityModule.setDataSource(dsEntityModules);
        this.entityModule.select((dataItem) => {
            return dataItem.id === moduleId;
        });
    }

    async getAcceptedChildTypes(moduleId, acceptedChildTypes) {
        const dsAcceptedChildTypes = await Wiser.api({ 
            url: `${this.base.settings.serviceRoot}/GET_ENTITY_TYPES?modules=${encodeURIComponent(moduleId)}`,
            method: "GET" 
        });
        this.acceptedChildTypes.setDataSource(dsAcceptedChildTypes);
        this.acceptedChildTypes.value(acceptedChildTypes);
    }

    setCodeMirrorFields(field, value) {
        if (value && value !== "null" && field != null) {
            field.setValue(value);
            field.refresh();
        }
    }

    // set all properties values to the fields accordingly
    setEntityFieldProperties(resultSet) {
        resultSet.dependsOn = resultSet.dependsOn || {};
        resultSet.overview = resultSet.overview || {};
        
        // set dropdown value for inputtype field
        this.inputTypeSelector.select((dataItem) => {
            return (dataItem.id || "").toLowerCase() === (resultSet.inputType || "").toLowerCase();
        });

        // hide/show all elements which are shown based on a type of input
        this.hideShowElementsBasedOnValue(resultSet.inputType || "input");
        // checkboxes proper set
        document.getElementById("visible-in-table").checked = resultSet.overview.visible || false;
        document.getElementById("mandatory").checked = resultSet.mandatory || false;
        document.getElementById("readonly").checked = resultSet.readOnly || false;
        document.getElementById("seofriendly").checked = resultSet.alsoSaveSeoValue || false;
        document.getElementById("saveOnChange").checked = resultSet.saveOnChange || false;
        document.getElementById("extendedExplanation").checked = resultSet.extendedExplanation || false;
        document.getElementById("enableAggregation").checked = resultSet.enableAggregation || false;

        // numeric textboxes
        this.widthInTable.value(resultSet.overview.width);
        this.width.value(resultSet.width);
        this.height.value(resultSet.height);

        // textboxes / textareas
        document.getElementById("displayname").value = resultSet.displayName || "";
        document.getElementById("propertyname").value = resultSet.propertyName || "";
        document.getElementById("regexValidation").value = resultSet.regexValidation || "";
        document.getElementById("langCode").value = resultSet.languageCode || "";
        $("#explanation").data("kendoEditor").value(resultSet.explanation || "");
        document.getElementById("defaultValue").value = resultSet.defaultValue || "";
        document.getElementById("accessKey").value = resultSet.accessKey || "";
        document.getElementById("visibilityPathRegex").value = resultSet.visibilityPathRegex || "";

        // dependencies
        document.getElementById("dependingValue").value = resultSet.dependsOn.value || "";

        //set depending field using one time dataBound because of the racing condition when filling.
        this.dependencyFields.one("dataBound",
            (e) => {
                this.dependencyFields.select((dataItem) => {
                    return dataItem.propertyName === resultSet.dependsOn.field;
                });
            });

        // Drop downs
        this.dependencyOperator.select((dataItem) => {
            return (dataItem.value || "").toLowerCase() === (resultSet.dependsOn.operator || "").toLowerCase();
        });

        this.dependencyAction.select((dataItem) => {
            return (dataItem.value || "").toLowerCase() === (resultSet.dependsOn.action || "").toLowerCase();
        });

        this.labelStyle.select((dataItem) => {
            return (dataItem.value || "").toLowerCase() === (resultSet.labelStyle || "").toLowerCase();
        });

        this.labelWidth.select((dataItem) => {
            return (dataItem.value || 0).toString().toLowerCase() === (resultSet.labelWidth || 0).toString().toLowerCase();
        });

        // set codemirror fields
        if (resultSet.customScript && resultSet.customScript !== "") {
            this.scriptField.setValue(resultSet.customScript);
            this.scriptField.refresh();
        }
        if (resultSet.aggregateOptions && resultSet.aggregateOptions !== "") {
            this.aggregateOptionsField.setValue(resultSet.aggregateOptions);
            this.scriptField.refresh();
        }

        // Set dropdown value for tab name field.
        this.tabNameProperty.select((dataItem) => {
            return (dataItem.tabName === "Gegevens" ? "" : dataItem.tabName) === (resultSet.tabName === "Gegevens" ? "" : resultSet.tabName);
        });
        
        // Set groupNameComboBox field using one time dataBound because of the racing condition when filling.
        this.groupNameComboBox.one("dataBound",
            (e) => {
                // set dropdown value for groupname field
                this.groupNameComboBox.select((dataItem) => {
                    return dataItem.groupName === resultSet.groupName;
                });
            });

        // Get options from resultset and parse them as json.
        const options = JSON.parse(!resultSet.options ? "{}" : resultSet.options);
        let remainingOptionsForOptionsJsonField = $.extend(true, {}, options);

        /**
         * Gets a value from the options JSON and then deletes that value from remainingOptionsForOptionsJsonField.
         * This is done so that in the end we only show the options that don't have specific fields, in the general options JSON field.
         * @param key The property name in the options JSON.
         * @param defaultValue The value to return if the requested option was not found.
         */
        const getOptionValueAndDeleteForOptionsField = (key, defaultValue) => {
            delete remainingOptionsForOptionsJsonField[key];
            
            return options[key] || defaultValue;
        };
        const addIdsToArrayObjectItems = (targetObject = {}) => {
            let autoIncrement = 0;

            const addIds = (target) => {
                if (target === null) return;
                target.autoIndex = autoIncrement;
                autoIncrement++;

                for (let prop in target) {
                    if (!target.hasOwnProperty(prop)) return;

                    const key = prop;
                    const value = target[key];

                    if (Utils.isArray(value)) {
                        // Loop through array items
                        for (let arrItem of value) {
                            if (typeof arrItem === "object" && arrItem !== null) {
                                arrItem.autoIndex = autoIncrement;
                                autoIncrement++;
                            }
                            if (typeof value === "object") addIds(value);
                        }
                    } else if (typeof value === "object") {
                        addIds(value);
                    }
                }
            };
            addIds(targetObject);
        }

        addIdsToArrayObjectItems(options);
        this.fieldOptions = options;

        document.getElementById("saveOnEnter").checked = getOptionValueAndDeleteForOptionsField("saveOnEnter", false);

        const optionsEntityType = getOptionValueAndDeleteForOptionsField("entityType", "");
        const optionsDataSource = getOptionValueAndDeleteForOptionsField("dataSource");
        const optionsType = getOptionValueAndDeleteForOptionsField("type");
        const optionsLinkType = getOptionValueAndDeleteForOptionsField("linkType");
        const optionsMode = getOptionValueAndDeleteForOptionsField("mode");
        const inputTypes = this.base.inputTypes;
        switch (resultSet.inputType) {
            case inputTypes.TEXTBOX:
                this.textboxTypeDropDown.select((dataItem) => {
                    return dataItem.id === optionsType;
                });
                
                delete remainingOptionsForOptionsJsonField.type;
                break;
            case inputTypes.AUTOINCREMENT:
                this.defaultNumeric.value(resultSet.defaultValue);
                break;
            case inputTypes.NUMERICINPUT: {
                this.defaultNumeric.value(resultSet.defaultValue);
                document.getElementById("roundNumeric").checked = getOptionValueAndDeleteForOptionsField("round");
                document.getElementById("cultureNumber").value = getOptionValueAndDeleteForOptionsField("culture", "");

                this.maxNumber.value(getOptionValueAndDeleteForOptionsField("max"));
                this.minNumber.value(getOptionValueAndDeleteForOptionsField("min"));
                this.stepNumber.value(getOptionValueAndDeleteForOptionsField("step"));
                this.factorNumber.value(getOptionValueAndDeleteForOptionsField("factor"));

                // set decimals from options
                this.numberOfDec.value(getOptionValueAndDeleteForOptionsField("decimals"));
                // set format dropdown 
                let found = false;
                const format = getOptionValueAndDeleteForOptionsField("format");
                this.numberFormat.select((dataItem) => {
                    if (dataItem.value === format) {
                        return found = true;
                    }
                });
                if (!found && format !== "") {
                    this.numberFormat.select((dataItem) => {
                        return dataItem.value === "anders";
                    });
                    $("#differentFormatHolder").show();
                    document.getElementById("differentFormat").value = format;
                }
                break;
            }
            case inputTypes.HTMLEDITOR:
                // check if mode is null or undefined
                if (optionsMode != null) {
                    document.getElementById(`mode${optionsMode}`).checked = true;
                }
                break;
            case inputTypes.DATETIMEPICKER:
                // Set dropdown to the correct mode.
                $("#dateTimeDropDown").data("kendoDropDownList").select((dataItem) => {
                    return dataItem.value === optionsType;
                });
                
                // Check if value isnt null, undefined or empty string and value should be set to NOW().
                const dateTimePickerValue = getOptionValueAndDeleteForOptionsField("value");
                if (dateTimePickerValue !== undefined && dateTimePickerValue !== "" && dateTimePickerValue === "NOW()") {
                    document.getElementById("dateTimePickerSetNow").checked = true;
                }
                
                // Check if min is not null or empty string, set minTimeBox to the value.
                const dateTimePickerMin = getOptionValueAndDeleteForOptionsField("min");
                if (dateTimePickerMin !== undefined && dateTimePickerMin !== "") {
                    this.minTimeBox.value(dateTimePickerMin);
                }
                break;
            case inputTypes.COMBOBOX:
            case inputTypes.MULTISELECT:
                if (resultSet.inputType === inputTypes.COMBOBOX) {
                    document.getElementById("useDropDownList").checked = getOptionValueAndDeleteForOptionsField("useDropDownList");
                }

                this.multiSelectMode.select((dataItem) => {
                    return (dataItem.value || "").toString().toLowerCase() === (optionsMode || "").toString().toLowerCase();
                });
                this.multiSelectMainImageId.value(getOptionValueAndDeleteForOptionsField("mainImageId", ""));
                $("#multiSelectMainImageUrl").val(getOptionValueAndDeleteForOptionsField("mainImageUrl", ""));
                $("#multiSelectImagePropertyName").val(getOptionValueAndDeleteForOptionsField("imagePropertyName", ""));
                
                let panel = "";
                // if dataQuery is set, set the codemirror field to the field's value
                if (resultSet.dataQuery) {
                    panel = this.base.dataSourceType.PANEL3.id;
                    this.queryField.setValue(resultSet.dataQuery);
                    this.queryField.refresh();
                } // if entity type is set, set datasource dropdown to entities and select right option
                else if (optionsEntityType) {
                    panel = this.base.dataSourceType.PANEL2.id;
                    this.dataSourceEntities.select((dataItem) => {
                        return (dataItem.id || "").toLowerCase() === (optionsEntityType || "").toLowerCase();
                    });
                    
                    document.getElementById("searchInTitle").checked = getOptionValueAndDeleteForOptionsField("searchInTitle", false);
                    document.getElementById("searchEverywhere").checked = getOptionValueAndDeleteForOptionsField("searchEverywhere", false);

                    const searchFields = getOptionValueAndDeleteForOptionsField("searchFields", []);
                    $.each(searchFields, (i, v) => {
                        const newItem = {
                            name: v
                        };
                        const widget = this.searchFields;
                        widget.dataSource.add(newItem);
                        widget.value(widget.value().concat([newItem.name]));
                    });

                } // if dataSource is set, set the grid datasource to the options dataSource
                else if (optionsDataSource) {
                    panel = this.base.dataSourceType.PANEL1.id;
                    this.grid.setDataSource(optionsDataSource);
                }
                // set dropdown to right panel
                this.dataSourceFilter.select((dataItem) => {
                    return dataItem.id === panel;
                });
                break;
            case inputTypes.SECUREINPUT:
                // set type of secure input
                $("#typeSecureInput").data("kendoDropDownList").select((dataItem) => {
                    return dataItem.value === optionsType;
                });

                // set securty method
                const securityMethod = getOptionValueAndDeleteForOptionsField("securityMethod");
                $("#securityMethod").data("kendoDropDownList").select((dataItem) => {
                    return dataItem.value === securityMethod;
                });
                
                if (securityMethod === "JCL_AES" || securityMethod === "AES") {
                    // set securitykey, but only when security method is JCL_AES or AES
                    document.getElementById("securityKey").value = getOptionValueAndDeleteForOptionsField("securityKey");
                }
                break;
            case inputTypes.LINKEDITEM:
                // set link type on databound of field
                this.linkType.one("dataBound", () => {
                    this.linkType.select((dataItem) => {
                        return dataItem.typeValue === optionsLinkType;
                    });
                    // if no linkType has been found in the selection, link typ hasnt been made yet and we set it manually
                    if (this.linkType.value() === "") {
                        // toString because kendo does a .toLowerCase in the background and linkType is an integer
                        this.linkType.text(optionsLinkType.toString());
                    }
                });

                // set linked item entity to option defined type
                this.linkedItemEntity.select((dataItem) => {
                    return dataItem.id === optionsEentityType;
                });
                // set the template
                document.getElementById("linkedItemTemplate").value = getOptionValueAndDeleteForOptionsField("template");
                document.getElementById("textOnly").checked = getOptionValueAndDeleteForOptionsField("textOnly");
                break;
            case inputTypes.DATASELECTOR:
                document.getElementById("dataSelectorText").value = getOptionValueAndDeleteForOptionsField("text");
                break;
            case inputTypes.ITEMLINKER:
            case inputTypes.SUBENTITIESGRID:
            case inputTypes.ACTIONBUTTON:
                // module id is only available for item linker 
                // entity is a multiselect for item linker
                if (resultSet.inputType === inputTypes.ITEMLINKER) {
                    $("#itemLinkerModuleId").data("kendoNumericTextBox").value(getOptionValueAndDeleteForOptionsField("moduleId"));
                    // select multiselect options
                    this.itemLinkerEntity.value(getOptionValueAndDeleteForOptionsField("entityTypes"));
                    document.getElementById("itemLinkerOrderBy").value = getOptionValueAndDeleteForOptionsField("orderBy");
                }
                
                const toolbar = getOptionValueAndDeleteForOptionsField("toolbar", {});

                // shared properties through out sub entities grid and item linker
                if (resultSet.inputType !== inputTypes.ACTIONBUTTON) {
                    // select item linker type number
                    this.itemLinkerTypeNumber.value(getOptionValueAndDeleteForOptionsField("linkTypeNumber"));

                    const deletionOfItems = getOptionValueAndDeleteForOptionsField("deletionOfItems");
                    $("#itemLinkerDeletionOfItems").data("kendoDropDownList").select((dataItem) => {
                        return dataItem.value === deletionOfItems;
                    });
                    
                    // set checkboxes
                    document.getElementById("hideCommandColumn").checked = getOptionValueAndDeleteForOptionsField("hideCommandColumn");
                    document.getElementById("disableInlineEditing").checked = getOptionValueAndDeleteForOptionsField("disableInlineEditing");
                    document.getElementById("disableOpeningOfItems").checked = getOptionValueAndDeleteForOptionsField("disableOpeningOfItems");
                    document.getElementById("hideExportButton").checked = toolbar.hideExportButton;
                    document.getElementById("hideCheckAllButton").checked = toolbar.hideCheckAllButton;
                    document.getElementById("hideUncheckAllButton").checked = toolbar.hideUncheckAllButton;
                    document.getElementById("hideCreateButton").checked = toolbar.hideCreateButton;
                }

                // actions which are available for action button and sub entities grid.
                if (resultSet.inputType !== inputTypes.ITEMLINKER) {
                    const buttonArray = [];
                    if (resultSet.inputType === inputTypes.ACTIONBUTTON) {
                        // set button array options
                        buttonArray.push({
                            text: getOptionValueAndDeleteForOptionsField("text"),
                            icon: getOptionValueAndDeleteForOptionsField("icon"),
                            autoIndex: getOptionValueAndDeleteForOptionsField("autoIndex"),
                            button: {
                                actions: getOptionValueAndDeleteForOptionsField("actions", [])
                            }
                        });
                    } else {
                        // actions come from custom actions property within toolbar for sub entity grid
                        const customActions = toolbar.customActions || [];
                        for (let i = 0; i < customActions.length; i++) {
                            buttonArray.push(
                                {
                                    text: customActions[i].text,
                                    icon: customActions[i].icon,
                                    autoIndex: customActions[i].autoIndex,
                                    button: {
                                        actions: customActions[i].actions
                                    }
                                });
                        }
                    }
                    this.actionButtonGrid.setDataSource(buttonArray);
                }

                // only available to sub entities grid
                if (resultSet.inputType === inputTypes.SUBENTITIESGRID) {
                    // set entity dropdown 
                    this.subEntityGridEntity.select((dataItem) => {
                        return dataItem.id === optionsEntityType;
                    });

                    //Cast options.selectable to string because it could be a boolean
                    const selectableString = getOptionValueAndDeleteForOptionsField("selectable", "").toString();
                    this.subEntitiesGridSelectOptions.select((dataItem) => {
                        return dataItem.value === selectableString;
                    });

                    // set data selector id
                    this.dataSelectorIdSubEntitiesGrid.value(getOptionValueAndDeleteForOptionsField("dataSelectorId"));
                    // set checkboxes
                    document.getElementById("refreshGridAfterInlineEdit").checked = getOptionValueAndDeleteForOptionsField("refreshGridAfterInlineEdit");
                    document.getElementById("showChangedByColumn").checked = getOptionValueAndDeleteForOptionsField("showChangedByColumn");
                    document.getElementById("showChangedOnColumn").checked = getOptionValueAndDeleteForOptionsField("showChangedOnColumn");
                    document.getElementById("showAddedByColumn").checked = getOptionValueAndDeleteForOptionsField("showAddedByColumn");
                    document.getElementById("showAddedOnColumn").checked = getOptionValueAndDeleteForOptionsField("showAddedOnColumn");
                    document.getElementById("showDeleteConformations").checked = getOptionValueAndDeleteForOptionsField("showDeleteConformations");
                    document.getElementById("checkboxes").checked = getOptionValueAndDeleteForOptionsField("checkboxes");

                    if (getOptionValueAndDeleteForOptionsField("customQuery", false) && resultSet.dataQuery) {
                        $("#customQuery").trigger("click");
                        this.queryFieldSubEntities.setValue(resultSet.dataQuery);
                        this.queryFieldSubEntities.refresh();
                    }

                    if (getOptionValueAndDeleteForOptionsField("hasCustomDeleteQuery") && resultSet.gridDeleteQuery) {
                        $("#hasCustomDeleteQuery").trigger("click");
                        this.queryDeleteField.setValue(resultSet.gridDeleteQuery);
                        this.queryDeleteField.refresh();
                    }

                    if (getOptionValueAndDeleteForOptionsField("hasCustomUpdateQuery", false) && resultSet.gridInsertQuery) {
                        $("#hasCustomUpdateQuery").trigger("click");
                        this.queryUpdateField.setValue(resultSet.gridUpdateQuery);
                        this.queryUpdateField.refresh();
                    }

                    if (getOptionValueAndDeleteForOptionsField("hasCustomInsertQuery") && resultSet.gridInsertQuery) {
                        $("#hasCustomInsertQuery").trigger("click");
                        this.queryInsertField.setValue(resultSet.gridInsertQuery);
                        this.queryInsertField.refresh();
                    }

                    if (resultSet.searchQuery) {
                        this.searchQueryField.setValue(resultSet.searchQuery);
                        this.searchQueryField.refresh();
                    }

                    if (resultSet.searchCountQuery) {
                        this.searchCountQueryField.setValue(resultSet.searchCountQuery);
                        this.searchCountQueryField.refresh();
                    }

                    document.getElementById("disableInlineEditing").checked = getOptionValueAndDeleteForOptionsField("disableInlineEditing", false);
                    document.getElementById("disableOpeningOfItems").checked = getOptionValueAndDeleteForOptionsField("disableOpeningOfItems", false);
                    document.getElementById("hideTitleColumn").checked = getOptionValueAndDeleteForOptionsField("hideTitleColumn", false);
                    document.getElementById("hideEnvironmentColumn").checked = getOptionValueAndDeleteForOptionsField("hideEnvironmentColumn", false);
                    document.getElementById("hideTypeColumn").checked = getOptionValueAndDeleteForOptionsField("hideTypeColumn", false);
                    document.getElementById("hideLinkIdColumn").checked = getOptionValueAndDeleteForOptionsField("hideLinkIdColumn", false);
                    document.getElementById("hideIdColumn").checked = getOptionValueAndDeleteForOptionsField("hideIdColumn", false);
                    document.getElementById("hideTitleFieldInWindow").checked = getOptionValueAndDeleteForOptionsField("hideTitleFieldInWindow", false);
                    document.getElementById("hideLinkButton").checked = toolbar.hideLinkButton;
                    document.getElementById("hideCount").checked = toolbar.hideCount;
                    document.getElementById("hideClearFiltersButton").checked = toolbar.hideClearFiltersButton;
                }
                break;
            case inputTypes.TIMELINE:
                this.timelineEntity.select((dataItem) => {
                    return dataItem.id === optionsEntityType;
                });
                $("#queryId").data("kendoNumericTextBox").value(getOptionValueAndDeleteForOptionsField("queryId"));
                $("#timelineEventHeight").data("kendoNumericTextBox").value(getOptionValueAndDeleteForOptionsField("eventHeight"));
                document.getElementById("disableOpeningOfItemsTimeLine").checked = getOptionValueAndDeleteForOptionsField("disableOpeningOfItems");
                break;
            case inputTypes.FILEUPLOAD:
            case inputTypes.IMAGEUPLOAD:
                document.getElementById("allowMultipleFiles").checked = getOptionValueAndDeleteForOptionsField("multiple");
                const validation = getOptionValueAndDeleteForOptionsField("validation");
                if (validation && validation.allowedExtensions && validation.allowedExtensions.length > 0) {
                    document.getElementById("allowedExtensions").value = validation.allowedExtensions.join(",");
                }
                else if (resultSet.inputType === inputTypes.IMAGEUPLOAD) {
                    document.getElementById("allowedExtensions").value = ".jpg,.jpeg,.png,.bmp,.gif,.svg";
                }
                else {
                    document.getElementById("allowedExtensions").value = "";
                }
                break;
            case inputTypes.DATERANGE:
                $("#daterangeFrom").data("kendoDatePicker").value(getOptionValueAndDeleteForOptionsField("from"));
                $("#daterangeTill").data("kendoDatePicker").value(getOptionValueAndDeleteForOptionsField("till"));
                break;
            case inputTypes.QUERYBUILDER:
            case inputTypes.SCHEDULER:
                $("#queryId").data("kendoNumericTextBox").value(getOptionValueAndDeleteForOptionsField("queryId"));
                break;
            case inputTypes.QR:
                document.getElementById("pixelSize").value = getOptionValueAndDeleteForOptionsField("size");
                if (resultSet.dataQuery !== "") {
                    this.queryContentField.setValue(resultSet.dataQuery);
                    this.queryContentField.refresh();
                }
                break;
            case inputTypes.RADIOBUTTON:
                if (resultSet.dataQuery !== "") {
                    this.queryContentField.setValue(resultSet.dataQuery);
                    this.queryContentField.refresh();
                }
                break;
            case inputTypes.CHECKBOX:
                this.checkBoxMode.select((dataItem) => {
                    return (dataItem.value || "").toString().toLowerCase() === (optionsMode || "").toString().toLowerCase();
                });
                this.checkBoxImageId.value(getOptionValueAndDeleteForOptionsField("imageId", ""));
                $("#checkBoxImageUrl").val(getOptionValueAndDeleteForOptionsField("imageUrl", ""));
                break;
            case inputTypes.COLORPICKER:
                if (!remainingOptionsForOptionsJsonField || JSON.stringify(remainingOptionsForOptionsJsonField, null, 4) === "{}") {
                    remainingOptionsForOptionsJsonField = {
                        input: true,
                        preview:false,
                        value: "#ffffff",
                        buttons: false,
                        views: ["gradient","palette"]
                    };
                }
                break;
        }

        const optionsValue = JSON.stringify(remainingOptionsForOptionsJsonField, null, 4);
        this.optionsJsonField.setValue(optionsValue === "{}" ? "" : optionsValue);
        this.optionsJsonField.refresh();   
    }

    // return array of of different input types from inputtypes enum
    createDataSourceFromEnum(list, useObjects = false) {
        const returnVal = [];
        if (useObjects) {
            const newList = {};
            $.each(list, (i, v) => {
                newList[v.id] = v.text;
            });
            list = newList;
        }
        $.each(list, (i, v) => {
            returnVal.push({ text: v, id: i });
        });
        return returnVal;
    }
}
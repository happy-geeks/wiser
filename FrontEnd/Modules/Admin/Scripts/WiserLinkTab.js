﻿import { LinkSettingsModel } from "../Scripts/LinkSettingsModel.js";

export class WiserLinkTab {
    constructor(base) {
        this.base = base;
        this.setupBindings();
    }

    /**
    * Setup all basis bindings for this module.
    * Specific bindings (for buttons in certain pop-ups for example) will be set when they are needed.
    */
    async setupBindings() {
        await this.initializeKendoComponents();

        // setup entitylists
        this.connectedEntity.setDataSource(this.base.entityTab.entityList);
        this.destinationEntity.setDataSource(this.base.entityTab.entityList);
    }

    async editLink() {
        if (!this.wiserLinkCombobox || !this.wiserLinkCombobox.dataItem() || !this.wiserLinkCombobox.dataItem().type) return;
        var linkDataItem = this.wiserLinkCombobox.dataItem();

        var linkSettingsModel = new LinkSettingsModel(
            linkDataItem.id,
            this.linkType.value(),
            this.destinationEntity.dataItem().name,
            this.connectedEntity.dataItem().name,
            document.getElementById("wiserLinkName").value
        );

        linkSettingsModel.relationship = this.linkRelation.dataItem().id;

        await Wiser.api({
            url: `${this.base.settings.wiserApiRoot}link-settings/${linkSettingsModel.id}`,
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            data: JSON.stringify(linkSettingsModel),
            method: "PUT"
        })
        .then((result) => {
            this.base.showNotification("notification", `Link succesvol aangepast`, "success");
        })
        .catch((e) => {
            console.error(e);
            if (e.responseText.indexOf("Duplicate entry")) {
                this.base.showNotification("notification",
                    `Er bestaat al een link met type '${linkSettingsModel.type}' met entiteit van '${linkSettingsModel.destinationEntityType}' naar '${linkSettingsModel.sourceEntityType}'`,
                    "error");
            } else {
                this.base.showNotification("notification",
                    `Wiser link is niet succesvol aangepast, probeer het opnieuw`,
                    "error");
            }
        });
    }

    async addLink() {
        var linkSettingsModel = new LinkSettingsModel(
            -1, // default
            this.linkTypePopup.value(),
            this.destinationEntityPopup.dataItem().name,
            this.connectedEntityPopup.dataItem().name,
            document.getElementById("wiserLinkNamePopup").value
        );

        linkSettingsModel.relationship = this.relationPopup.dataItem().id;

        await Wiser.api({
                url: `${this.base.settings.wiserApiRoot}link-settings`,
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                data: JSON.stringify(linkSettingsModel),
                method: "POST"
            })
            .then(async (result) => {
                this.base.showNotification("notification", `Link succesvol toegevoegd`, "success");
                // close popup
                $('.linkPopupContent').data("kendoWindow").close();
                // reload list
                await this.reloadWiserLinkList();
                // select newly created link
                if (result) {
                    this.wiserLinkCombobox.select((dataItem) => {
                        return dataItem.id === result.id;
                    });
                }

            })
            .catch((e) => {
                console.error(e);
                if (e.responseText.indexOf("Duplicate entry")) {
                    this.base.showNotification("notification",
                        `Er bestaat al een link met type '${linkSettingsModel.type}' met entiteit van '${linkSettingsModel.destinationEntityType}' naar '${linkSettingsModel.sourceEntityType}'`,
                        "error");
                } else {
                    this.base.showNotification("notification",
                        `Wiser link is niet succesvol aangemaakt, probeer het opnieuw`,
                        "error");
                }
            });
    }

    async initializeKendoComponents() {
        return new Promise((resolve) => {
            // initialize kendo components on page 

            $(".addLinkBtn").kendoButton({
                click: () => {
                    const linkPopup = $('.linkPopupContent');
                    const dialog = linkPopup.kendoWindow({
                        width: 350,
                        height: 500,
                        title: "Nieuwe link toevoegen",
                        modal: true,
                        resizable: false,
                        draggable: false
                    }).data("kendoWindow");
                    linkPopup.show();
                    dialog.center().open();
                },
                icon: "file"
            });

            $(".addLinkBtnPopup").kendoButton({
                click: () => {
                    this.addLink();
                },
                icon: "file"
            });


            $(".editLinkBtn").kendoButton({
                click: () => {
                    this.editLink();
                },
                icon: "file"
            });

            this.linkType = $("#wiserLinkType").kendoNumericTextBox({
                decimals: 0,
                format: "#"
            }).data("kendoNumericTextBox"); 

            //Main combobox for selecting a link
            this.wiserLinkCombobox = $("#wiserLinks").kendoDropDownList({
                placeholder: "Select gewenste link...",
                clearButton: false,
                height: 400,
                dataTextField: "formattedName",
                dataValueField: "id",
                filter: "contains",
                optionLabel: {
                    id: "",
                    formattedName: "Maak uw keuze..."
                },
                minLength: 1,
                dataSource: {
                    transport: {
                        read: {
                            url: `${this.base.settings.serviceRoot}/GET_WISER_LINK_LIST`
                        }
                    }
                },
                cascade: this.onWiserLinkComboBoxSelect.bind(this)
            }).data("kendoDropDownList");
            
            this.connectedEntity = $("#connectedEntity").kendoDropDownList({
                clearButton: false,
                dataTextField: "displayName",
                dataValueField: "id",
                filter: "contains",
                optionLabel: {
                    id: "",
                    displayName: "Maak uw keuze..."
                },
                minLength: 1,
                dataSource: {}
            }).data("kendoDropDownList");

            this.destinationEntity = $("#destinationEntity").kendoDropDownList({
                clearButton: false,
                dataTextField: "displayName",
                dataValueField: "id",
                filter: "contains",
                optionLabel: {
                    id: "",
                    displayName: "Maak uw keuze..."
                },
                minLength: 1,
                dataSource: {}
            }).data("kendoDropDownList");


            this.connectedEntityPopup = $("#connectedEntityPopup").kendoDropDownList({
                clearButton: false,
                dataTextField: "displayName",
                dataValueField: "id",
                filter: "contains",
                optionLabel: {
                    id: "",
                    displayName: "Maak uw keuze..."
                },
                minLength: 1,
                dataSource: this.base.entityTab.entityList
            }).data("kendoDropDownList");

            this.destinationEntityPopup = $("#destinationEntityPopup").kendoDropDownList({
                clearButton: false,
                dataTextField: "displayName",
                dataValueField: "id",
                filter: "contains",
                optionLabel: {
                    id: "",
                    displayName: "Maak uw keuze..."
                },
                minLength: 1,
                dataSource: this.base.entityTab.entityList
            }).data("kendoDropDownList");

            const linkDataSource = [
                {
                    id: 0,
                    relationship: "one-to-one",
                    displayName: "1 op 1"
                }, {
                    id: 1,
                    relationship: "one-to-many",
                    displayName: "1 op veel"
                }, {
                    id: 2,
                    relationship: "many-to-many",
                    displayName: "veel op veel"
                }
            ];
            this.relationPopup = $("#relationPopup").kendoDropDownList({
                clearButton: false,
                dataTextField: "displayName",
                dataValueField: "id",
                filter: "contains",
                optionLabel: {
                    id: "",
                    displayName: "Maak uw keuze..."
                } ,
                minLength: 1,
                dataSource: linkDataSource
            }).data("kendoDropDownList");

            this.linkRelation = $("#linkRelation").kendoDropDownList({
                clearButton: false,
                dataTextField: "displayName",
                dataValueField: "id",
                filter: "contains",
                optionLabel: {
                    id: "",
                    displayName: "Maak uw keuze..."
                },
                minLength: 1,
                dataSource: linkDataSource
            }).data("kendoDropDownList");

            this.linkTypePopup = $("#wiserLinkTypePopup").kendoNumericTextBox({
                decimals: 0,
                format: "#"
            }).data("kendoNumericTextBox"); 
            resolve();
        });
    }

    onWiserLinkComboBoxSelect() {
        if (!this.wiserLinkCombobox || !this.wiserLinkCombobox.dataItem() || !this.wiserLinkCombobox.dataItem().type) return;
        var linkDataItem = this.wiserLinkCombobox.dataItem();
        console.log(linkDataItem);
        // set data sources
        this.linkType.value(linkDataItem.type);

        // set values
        this.destinationEntity.select((dataItem) => {
            return dataItem.name === linkDataItem.destination_entity_type;
        });

        this.connectedEntity.select((dataItem) => {
            return dataItem.name === linkDataItem.connected_entity_type;
        });

        document.getElementById("wiserLinkName").value = linkDataItem.name;
        this.linkRelation.select((dataItem) => { return dataItem.relationship == linkDataItem.relationship; });
    }

    async reloadWiserLinkList() {
        return new Promise(async (resolve) => {
            var dataSource = await Wiser.api({
                url: `${this.base.settings.serviceRoot}/GET_WISER_LINK_LIST`,
                method: "GET"
            });

            this.wiserLinkCombobox.setDataSource(dataSource);
            resolve();
        });
    }


}
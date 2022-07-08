import { LinkSettingsModel } from "../Scripts/LinkSettingsModel.js";

export class WiserLinkTab {
    constructor(base) {
        this.base = base;
        this.setupBindings();
        this.initializeKendoComponents();
    }

    

    /**
    * Setup all basis bindings for this module.
    * Specific bindings (for buttons in certain pop-ups for example) will be set when they are needed.
    */
    setupBindings() {
        $(".addLinkBtn").kendoButton({
            click: () => {
                var dialog = $('.linkPopupContent').kendoWindow({
                    width: 350,
                    height: 100,
                    title: "Choose Address",
                    modal: true,
                    resizable: false,
                    draggable: false
                }).data("kendoWindow");
                dialog.center().open();
            },
            icon: "file"
        });

        document.querySelector(".addLinkBtnPopup").addEventListener("click", () => {
            this.addLink();
        });

    }

    async addLink() {
        // todo expand popup to set up linksettingsmodel with type / connected entity / destination entity, name
        var linkSettingsModel = new LinkSettingsModel(
            -1,
            this.linkTypePopup.value(),
            this.destinationEntityPopup.dataItem().name,
            this.connectedEntityPopup.dataItem().name,
            document.getElementById("wiserLinkNamePopup").value);

        linkSettingsModel.relationship = this.relationPopup.dataItem().id;

        await Wiser2.api({
                url: `${this.base.settings.wiserApiRoot}link-settings`,
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                data: JSON.stringify(linkSettingsModel),
                method: "POST"
            })
            .then((result) => {
                this.base.showNotification("notification", `Link succesvol toegevoegd`, "success");
                this.reloadWiserLinkList(result);

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
                this.base.showNotification("notification", `Link is niet succesvol toegevoegd, probeer het opnieuw`, "error");
            });
    }

    async initializeKendoComponents() {
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
                displayName: "Maak uw keuze..."
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
            dataSource: [{
                id: 0,
                displayName: "1 op 1"
            }, {
                id: 1,
                displayName: "1 op veel"
                } , {
            id: 2,
                displayName: "veel op veel"
        }]
        }).data("kendoDropDownList");

        this.linkTypePopup = $("#wiserLinkTypePopup").kendoNumericTextBox({
            decimals: 0,
            format: "#"
        }).data("kendoNumericTextBox"); 
    }
    // actions handled before save, such as checks
    beforeSave() {

    }


    onWiserLinkComboBoxSelect() {
        if (!this.wiserLinkCombobox || !this.wiserLinkCombobox.dataItem() || !this.wiserLinkCombobox.dataItem().type) return;

        console.log(this.wiserLinkCombobox.dataItem());
        this.linkType.value(this.wiserLinkCombobox.dataItem().type);
        this.connectedEntity.setDataSource(this.base.entityTab.entityList);
        this.destinationEntity.setDataSource(this.base.entityTab.entityList);
        
    }

    async reloadWiserLinkList(linkIdToSelect = null) {
        var dataSource = await Wiser2.api({
            url: `${this.base.settings.wiserApiRoot}link-settings`,
            method: "GET"
        });

        this.wiserLinkCombobox.setDataSource(dataSource);

        if (linkIdToSelect) {
            this.wiserLinkCombobox.select((dataItem) => {
                return dataItem.id === linkIdToSelect;
            });
        }
    }


}
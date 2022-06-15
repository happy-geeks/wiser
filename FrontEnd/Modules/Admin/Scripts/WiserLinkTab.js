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
                this.base.openDialog("Link toevoegen", "Voer de naam in van nieuwe link").then((data) => {
                    this.addLink(data);
                });

            },
            icon: "file"
        });
    }

    async addLink(name) {
        if (name === "") return;
        // todo expand popup to set up linksettingsmodel with type / connected entity / destination entity, name
        var linkSettingsModel = new LinkSettingsModel(-1,0,"a","b",name);
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
            .catch(() => {
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
            dataTextField: "formattedName",
            dataValueField: "id",
            filter: "contains",
            optionLabel: {
                id: "",
                formattedName: "Maak uw keuze..."
            },
            minLength: 1,
            dataSource: {}
        }).data("kendoDropDownList");

        this.destinationEntity = $("#destinationEntity").kendoDropDownList({
            clearButton: false,
            dataTextField: "formattedName",
            dataValueField: "id",
            filter: "contains",
            optionLabel: {
                id: "",
                formattedName: "Maak uw keuze..."
            },
            minLength: 1,
            dataSource: {}
        }).data("kendoDropDownList");

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
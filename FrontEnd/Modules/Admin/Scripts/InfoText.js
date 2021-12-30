export class InfoText {
    constructor() {
        this.translations = null;

        $(document).ready(() => {
            this.onPageReady();
        });
    }

    async onPageReady() {
        this.translations = this.getTranslations();
        this.setTranslations();
    }

    // set translations for all info elements
    setTranslations() {
        // loop through all info elements
        document.querySelectorAll("span.info[data-type]").forEach(
            (el) => {
                let translation = this.getTranslation(el.getAttribute("data-type"));
                if (!translation) {
                    // if no translation is found early return and move on to next translation
                    el.remove();
                    return;
                }
                el.setAttribute("data-info", translation);
            });
    }

    // get translation for given element
    getTranslation(elementName) {
        let returnVal = false;
        for (let i = 0; i < this.translations.length; i++) {
            if (this.translations[i].name === elementName) {
                returnVal = this.translations[i].text;
                break;
            }
        }
        // return false if no translation has been found
        return returnVal;
    }

    // get translations 
    getTranslations() {
        return [
            { name: "inputtype", text: "kies een invoertype" },
            { name: "regex_validation", text: "Vul hier de regex validatie in" },
            { name: "language_code", text: "Vul een taalcode in, zoals nl-NL" },
            { name: "display_name", text: "Vul hier de naam in" },
            { name: "property_name", text: "Vul hier de eigenschap naam in" },
            { name: "group_name", text: "Hier een nieuwe groepnaam invoeren of een bestaande groepnaam selecteren uit de lijst"},
            { name: "tab_name", text: "Hier een nieuwe tabnaam invoeren of een bestaande tabnaam selecteren uit de lijst" },
            { name: "width", text: "Vul de breedte van het veld in" },
            { name: "height", text: "Vul de hoogte van het veld in" },
            { name: "overview_width", text: "Vul de breedte van het veld in het overzicht in" },
            { name: "overview_fieldtype", text: "Kies een invoertype voor het overzicht" },
            { name: "default_value", text: "Standaard waarde" },
            { name: "html editor layout", text: "Kies een layout voor de html editor" },
            { name: "datasource", text: "Kies waar de brondata vandaanmoet komen" },
            { name: "data_query", text: "Geef een query op. Let op! Geef id en name op." },
            { name: "qr_content_query", text: "Geef een query op die de waarde van de QR-code geeft. Dat kan een URL of gewoon een stuk tekst zijn." },
            { name: "automation", text: "" },
            { name: "depends_on", text: "Kies in de volgende 3 velden wanneer dit veld pas zichtbaar kan/mag zijn" },
            { name: "explanation", text: "Schrijf hier een duidelijke uitleg over wat het veld precies moet doen" },
            { name: "css", text: "Geef hier de custom-css in op" },
            { name: "custom_script", text: "Geef hier custom-scripting in op" },
            { name: "data-selector-tex", text: "Wanneer er geen tekst wordt opgegeven is de standaardtekst van deze knop is 'Bewerken'" },
            { name: "grid-select-options", text: "Indien de gebruiker items in het grid moet kunnen selecteren (voor bijvoorbeeld bepaalde acties in de toolbar), dan kun je dat hier aangeven" },
            { name: "allowed-extensions", text: "Je kunt ook validatie toevoegen als je wilt dat gebruikers alleen bestanden met bepaalde extensies kunnen uploaden. Eextensies komma gescheiden invoeren bijvoorbeeld: .jpg,.gif" }
        ];
    }
}

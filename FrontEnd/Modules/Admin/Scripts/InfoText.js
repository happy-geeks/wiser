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
            { name: "regexValidation", text: "Vul hier de regex validatie in" },
            { name: "languageCode", text: "Vul een taalcode in, zoals nl-NL" },
            { name: "displayName", text: "Vul hier de naam in" },
            { name: "propertyName", text: "Vul hier de eigenschap naam in" },
            { name: "groupName", text: "Hier een nieuwe groepnaam invoeren of een bestaande groepnaam selecteren uit de lijst"},
            { name: "tabName", text: "Hier een nieuwe tabnaam invoeren of een bestaande tabnaam selecteren uit de lijst" },
            { name: "width", text: "Vul de breedte van het veld in" },
            { name: "height", text: "Vul de hoogte van het veld in" },
            { name: "overviewWidth", text: "Vul de breedte van het veld in het overzicht in" },
            { name: "overviewFieldtype", text: "Kies een invoertype voor het overzicht" },
            { name: "defaultValue", text: "Standaard waarde" },
            { name: "html editor layout", text: "Kies een layout voor de html editor" },
            { name: "datasource", text: "Kies waar de brondata vandaanmoet komen" },
            { name: "dataQuery", text: "Geef een query op. Let op! Geef id en name op." },
            { name: "qrContentQuery", text: "Geef een query op die de waarde van de QR-code geeft. Dat kan een URL of gewoon een stuk tekst zijn." },
            { name: "automation", text: "" },
            { name: "dependsOn", text: "Kies in de volgende 3 velden wanneer dit veld pas zichtbaar kan/mag zijn" },
            { name: "explanation", text: "Schrijf hier een duidelijke uitleg over wat het veld precies moet doen" },
            { name: "css", text: "Geef hier de custom-css in op" },
            { name: "customScript", text: "Geef hier custom-scripting in op" },
            { name: "data-selector-tex", text: "Wanneer er geen tekst wordt opgegeven is de standaardtekst van deze knop is 'Bewerken'" },
            { name: "grid-select-options", text: "Indien de gebruiker items in het grid moet kunnen selecteren (voor bijvoorbeeld bepaalde acties in de toolbar), dan kun je dat hier aangeven" },
            { name: "allowed-extensions", text: "Je kunt ook validatie toevoegen als je wilt dat gebruikers alleen bestanden met bepaalde extensies kunnen uploaden. Eextensies komma gescheiden invoeren bijvoorbeeld: .jpg,.gif" },
            { name: "entityAcceptedChildtypes", text: "Een lijst van entiteiten, die aan items van deze entiteit gekoppeld mogen worden. Laat leeg om alles mogelijk te maken" },
            { name: "queryAfterInsert", text: "Hier kan een query ingevuld worden die uitgevoerd wordt nadat een nieuw item van dit type wordt aangemaakt.Je kunt hier de volgende variabelen gebruiken: { itemId }, { title }, { moduleId }, { userId }, { username }.Het is ook mogelijk om alle ingevulde velden als variabelen te gebruiken in de query."},
            { name: "queryAfterUpdate", text: "Hier kan een query ingevuld worden die wordt uitgevoerd nadat een item is gewijzigd. Je kunt hier de volgende variabelen gebruiken: { itemId }, { title }, { moduleId }, { userId }, { username }.Het is ook mogelijk om alle ingevulde velden als variabelen te gebruiken in de query." },
            { name: "queryBeforeUpdate", text: "Hier kun je een query invullen om te controleren of een item gewijzigd mag worden. Deze query moeten minimaal 1 kolom teruggeven met waarde 1 indien de verwijdering/update door mag gaan en waarde 0 als dit niet mag. Optioneel kun je nog een 2e kolom toevoegen aan je query met daarin de tekst die als foutmelding aan de gebruiker getoond wordt wanneer de waarde van de eerste kolom 0 is. Indien je hier geen tekst invult, wordt de standaardtekst Het is niet meer mogelijk om aanpassingen te maken in dit item. getoond.Indien er geen query is ingevuld, of de ingevulde query geeft geen resultaten terug, dan mag het verwijderen/ opslaan gewoon doorgaan.Je kunt hier { itemId } gebruiken, dat is het ID van het item dat men probeert te updaten.Het is ook mogelijk om alle ingevulde velden als variabelen te gebruiken in de query." },
            { name: "queryBeforeDelete", text: "Hier kun je een query invullen om te controleren of een item verwijderd mag worden. Deze query moeten minimaal 1 kolom teruggeven met waarde 1 indien de verwijdering/update door mag gaan en waarde 0 als dit niet mag. Optioneel kun je nog een 2e kolom toevoegen aan je query met daarin de tekst die als foutmelding aan de gebruiker getoond wordt wanneer de waarde van de eerste kolom 0 is. Indien je hier geen tekst invult, wordt de standaardtekst Het is niet meer mogelijk om dit item te verwijderen. getoond.Indien er geen query is ingevuld, of de ingevulde query geeft geen resultaten terug, dan mag het verwijderen/ opslaan gewoon doorgaan.Je kunt hier { itemId } gebruiken, dat is het ID van het item dat men probeert te updaten." },
            { name: "entityModule", text: "De module instellen waar deze entiteit voor bedoeld is. Waarschijnlijk gaat deze kolom nog weg, omdat entiteiten uniek moeten zijn over alle modules." },
            { name: "entityIcon", text: "Het icoon dat getoond moet worden voor items van dit type in de boom, wanneer die nog niet uitgeklapt is." },
            { name: "entityIconAdd", text: "Het icoon dat getoond moet worden voor items van dit type in de boom, wanneer die uitgeklapt is." },
            { name: "entityIconExpanded", text: "Het icoon dat getoond moet worden voor items van dit type in de boom, wanneer die nog wel uitgeklapt is." },
            { name: "defaultOrdering", text: "De manier waarop items gesorteerd moeten worden in de boom (treeview), of andere plekken waar de template GET_ITEMS wordt gebruikt." },
            { name: "friendlyName", text: "De naam van dit entiteitstype zoals die aan de gebruiker getoond moet worden." }
        ];
    }
}

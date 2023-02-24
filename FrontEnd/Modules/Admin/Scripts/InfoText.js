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
            { name: "regexValidation", text: "Vul hier de regex in die gebruikt moet worden om de waarde van het veld te valideren tijdens invullen/opslaan." },
            { name: "languageCode", text: "Vul een taalcode in, zoals nl-NL" },
            { name: "displayName", text: "Vul hier de naam in" },
            { name: "propertyName", text: "Vul hier de eigenschap naam in" },
            { name: "groupName", text: "Hier een nieuwe groepnaam invoeren of een bestaande groepnaam selecteren uit de lijst"},
            { name: "tabName", text: "Hier een nieuwe tabnaam invoeren of een bestaande tabnaam selecteren uit de lijst" },
            { name: "width", text: "Vul de breedte van het veld in" },
            { name: "height", text: "Vul de hoogte van het veld in" },
            { name: "overviewWidth", text: "Vul de breedte van het veld in het overzicht in" },
            { name: "defaultValue", text: "De waarde die standaard ingevuld wordt, indien er geen andere waarde is ingevuld." },
            { name: "html editor layout", text: "Kies een layout voor de html editor" },
            { name: "datasource", text: "Kies waar de brondata vandaanmoet komen" },
            { name: "dataQuery", text: "Geef een query op. Let op! Geef id en name op." },
            { name: "qrContentQuery", text: "Geef een query op die de waarde van de QR-code geeft. Dat kan een URL of gewoon een stuk tekst zijn." },
            { name: "dependsOn", text: "Velden kunnen afhankelijk van elkaar zijn. Hier kan ingesteld worden dat bijvoorbeeld veld X alleen getoond mag worden inden veld Y een bepaalde waarde heeft. Of dat veld X ververst moet worden nadat de waarde van veld Y wordt gewijzigd." },
            { name: "explanation", text: "Schrijf hier een duidelijke uitleg over wat het veld precies moet doen" },
            { name: "extendedExplanation", text: "Vink dit aan wanneer dit veld een lange (meer dan 1 zin) uitleg nodig heeft. De uitleg wordt dan niet meer onder het veld getoond, maar dit veld krijgt dan een info-icoon en als de gebruiker daarop klikt, wordt deze informatie aan de rechterkant van het scherm getoond." },
            { name: "css", text: "Geef hier de custom-css in op" },
            { name: "customScript", text: "Bij elk veld kun je een custom script uitvoeren die alleen voor dat specifieke veld gedaan wordt. Dit kan bijvoorbeeld gebruikt worden om op basis van een wijziging in veld X, de waarde van veld Y aan te passen (zoals het automatisch invullen van straatnaam na het invullen van postcode en huisnummer). Voor meer informatie, zie documentatie van Wiser." },
            { name: "data-selector-tex", text: "Wanneer er geen tekst wordt opgegeven is de standaardtekst van deze knop is 'Bewerken'" },
            { name: "grid-select-options", text: "Indien de gebruiker items in het grid moet kunnen selecteren (voor bijvoorbeeld bepaalde acties in de toolbar), dan kun je dat hier aangeven" },
            { name: "allowed-extensions", text: "Je kunt ook validatie toevoegen als je wilt dat gebruikers alleen bestanden met bepaalde extensies kunnen uploaden. Eextensies komma gescheiden invoeren bijvoorbeeld: .jpg,.gif" },
            { name: "entityAcceptedChildTypes", text: "Een lijst van entiteiten, die aan items van deze entiteit gekoppeld mogen worden. Laat leeg om alles mogelijk te maken" },
            { name: "queryAfterInsert", text: "Hier kan een query ingevuld worden die uitgevoerd wordt nadat een nieuw item van dit type wordt aangemaakt.Je kunt hier de volgende variabelen gebruiken: { itemId }, { title }, { moduleId }, { userId }, { username }.Het is ook mogelijk om alle ingevulde velden als variabelen te gebruiken in de query."},
            { name: "queryAfterUpdate", text: "Hier kan een query ingevuld worden die wordt uitgevoerd nadat een item is gewijzigd. Je kunt hier de volgende variabelen gebruiken: { itemId }, { title }, { moduleId }, { userId }, { username }.Het is ook mogelijk om alle ingevulde velden als variabelen te gebruiken in de query." },
            { name: "queryBeforeUpdate", text: "Hier kun je een query invullen om te controleren of een item gewijzigd mag worden. Deze query moet minimaal 1 kolom teruggeven met waarde 1 indien de verwijdering/update door mag gaan en waarde 0 als dit niet mag. Optioneel kun je nog een 2e kolom toevoegen aan je query met daarin de tekst die als foutmelding aan de gebruiker getoond wordt wanneer de waarde van de eerste kolom 0 is. Indien je hier geen tekst invult, wordt de standaardtekst Het is niet meer mogelijk om aanpassingen te maken in dit item. getoond.Indien er geen query is ingevuld, of de ingevulde query geeft geen resultaten terug, dan mag het verwijderen/ opslaan gewoon doorgaan.Je kunt hier { itemId } gebruiken, dat is het ID van het item dat men probeert te updaten.Het is ook mogelijk om alle ingevulde velden als variabelen te gebruiken in de query." },
            { name: "queryBeforeDelete", text: "Hier kun je een query invullen om te controleren of een item verwijderd mag worden. Deze query moet minimaal 1 kolom teruggeven met waarde 1 indien de verwijdering/update door mag gaan en waarde 0 als dit niet mag. Optioneel kun je nog een 2e kolom toevoegen aan je query met daarin de tekst die als foutmelding aan de gebruiker getoond wordt wanneer de waarde van de eerste kolom 0 is. Indien je hier geen tekst invult, wordt de standaardtekst Het is niet meer mogelijk om dit item te verwijderen. getoond.Indien er geen query is ingevuld, of de ingevulde query geeft geen resultaten terug, dan mag het verwijderen/ opslaan gewoon doorgaan.Je kunt hier { itemId } gebruiken, dat is het ID van het item dat men probeert te updaten." },
            { name: "entityModule", text: "De module instellen waar deze entiteit voor bedoeld is." },
            { name: "entityIcon", text: "Het icoon dat getoond moet worden voor items van dit type in de boom, wanneer die nog niet uitgeklapt is." },
            { name: "entityIconAdd", text: "Het icoon dat getoond moet worden bij de optie om een nieuw item van dit type toe te voegen aan de boom." },
            { name: "entityIconExpanded", text: "Het icoon dat getoond moet worden voor items van dit type in de boom, wanneer die wel uitgeklapt is." },
            { name: "defaultOrdering", text: "De manier waarop items gesorteerd moeten worden in de boom (tree view). Let op; indien dit ingesteld staat op 'naam', dan kan de volgorde van items niet meer handmatig aangepast worden in Wiser." },
            { name: "friendlyName", text: "De naam van dit entiteitstype zoals die aan de gebruiker getoond moet worden." },
            { name: "entityDedicatedTablePrefix", text: "Items van Wiser kunnen in verschillende tabellen worden opgeslagen. Standaard worden all items opgeslagen in wiser_item en wiser_itemdetail, behalve wanneer hier een voorvoegsel is ingevuld. Dan worden items van dit type opgeslagen in [voorvoegsel]_wiser_item en [voorvoegsel]_wiser_itemdetail." },
            { name: "searchQuery", text: "Dit wordt gebruikt bij velden van het type sub-entities-grid. Bij die velden kan een knop toegevoegd worden welke een popup opent waarin gezocht kan worden naar items om te koppelen. De query daarvoor kan hier gevuld worden. Zie Wiser documentatie voor meer informatie." },
            { name: "searchCountQuery", text: "Deze kolom gaat samen met search_query, hier moet dezelfde query in komen te staan, maar dan zonder limit en met een  count, zodat het grid weet hoeveel items er in totaal zijn." },
            { name: "labelStyle", text: "De manier waarop het label getoond wordt. Heeft deze mogelijke waardes: normal: Het label wordt boven het veld getoond, inline: Het label wordt links van het veld getoond, float: Het label wordt in het veld getoond wanneer die leeg en niet geselecteerd is en erboven wanner die een waarde heeft of de focus heeft." },
            { name: "accessKey", text: "De sneltoets waarmee de gebruiker de focus naar dit veld kan brengen." },
            { name: "aggregateOptions", text: "Extra aggregatie-instellingen. Zie Wiser documentatie voor meer informatie." },
            { name: "templateQuery", text: "De query die uitgevoerd moet worden voor het ophalen van de data wanneer een item van dit entiteitstype als entiteit-blok op de pagina wordt gezet. Alle waardes die uit deze query komen, kunnen gebruikt worden in de Template HTML. Gebruik \"{itemId}\" of \"?itemId\" in deze query om het ID dat is ingevuld te gebruiken." },
            { name: "templateHtml", text: "De HTML die gebruikt moet worden wanneer een item van dit entiteitstype als entiteit-blok op de pagina wordt gezet. Alle waardes die uit de Template Query komen, kunnen hierin gebruikt worden als variabelen/vervangingen." },
            { name: "deleteAction", text: "Wat er moet gebeuren wanneer een item van dit type wordt verwijderd. De volgende opties zijn mogelijk: Archiveren (staandaardoptie, items worden verplaatst naar archieftabellen), Permanent verwijderen (items worden direct en helemaal uit de database verwijderd), Verbergen (items worden gemarkeerd als verborgen) en Niet toestaan (items kunnen niet verwijderd worden)." },
            { name: "enableMultipleEnvironments", text: "Met deze optie aan, kunnen er meerdere versies van items met dit entiteitstype bestaan. Items van dit type krijgen dan een extra optie in Wiser om die op te slaan naar een bepaalde omgeving (ontwikkeling, test, acceptatie en live). Hier worden dan aparte regels voor aangemaakt in de database en die worden aan elkaar gekoppeld dmv de kolom 'original_item_id'." },
            { name: "visibilityPathRegex", text: "Hiermee kan een regex ingesteld worden die uitgevoerd wordt op het volledige pad van het item, wanneer die geopend wordt in Wiser via een boom (tree view). Dit pad is de naam van alle bovenliggende items achter elkaar geplakt met slashes ertussen, bijvoorbeeld '/Eten & Drinken/Frisdranken/Cola/'. Indien de regex succesvol valideert op dit pad, wordt dit veld getoond, anders verborgen." },
            { name: "checkBoxMode", text: "De mode waarin de checkbox gezet moet worden. Er is een standaard checkbox, waarbij je iets kunt aanvinken en er is de 'switch' mode. Met switch krijg je een schuifje om de optie aan of uit te zetten en daar kan dan een afbeelding bij getoond worden." },
            { name: "checkBoxImageId", text: "Het ID uit wiser_itemfile voor de afbeelding die bij deze switch getoond moet worden. Indien hier iets ingevuld is, zal 'Afbeelding URL' genegeerd worden." },
            { name: "checkBoxImageUrl", text: "De URL van de afbeelding die bij de switch getoond moet worden. Wordt alleen gebruikt indien 'Afbeelding ID' leeg of '0' is." },
            { name: "multiSelectMode", text: "De mode waarin de multiselect gezet moet worden. Er is een standaard multiselect en er is de 'checkboxgroep' mode. Met checkboxgroep krijg je een schuifje per item in de dataSource, om die opties aan of uit te zetten. Vervolgens daar kan dan een afbeelding per optie getoond worden en een globale afbeelding bovenaan alle opties. Voor meer informatie, zie de documentatie van Wiser over multiSelect." },
            { name: "multiSelectMainImageId", text: "Het ID uit wiser_itemfile voor de afbeelding die bovenaan de checkboxgroep getoond moet worden. Indien hier iets ingevuld is, zal 'Hoofdafbeelding URL' genegeerd worden." },
            { name: "multiSelectMainImageUrl", text: "De URL van de afbeelding die bovenaan de checkboxgroep getoond moet worden. Wordt alleen gebruikt indien 'Hoofdafbeelding ID' leeg of '0' is." },
            { name: "multiSelectImagePropertyName", text: "De waarde van de kolom property_name van wiser_itemfile van de afbeelding die per optie getoond moet worden. De dataSource moet dan een property 'id' bevatten van het item waar deze afbeelding opgeslagen staat. Deze optie hoeft niet ingevuld worden als de dataSource al een eigenschap 'imagePropertyName' heeft, of als afbeeldingen op een andere manier uit de dataSource komen. Voor meer informatie, zie de documentatie van Wiser over multiSelect." },
            { name: "optionsJson", text: "Dit is een JSON object met alle overige opties, waar geen losse velden voor staan. Vrijwel alle velden in Wiser zijn componenten van Kendo UI, alle opties die Kendo heeft, kunnen hier ingevuld worden. Zie de documentatie van Kendo UI voor alle beschikbare opties." },
            { name: "moduleInitialItemId", text: "Indien de module bij initieel laden direct een specifiek item moet openen, vul dan het ID van dat item hier in." },
            { name: "moduleInitialItemEntityType", text: "Indien de module bij initieel laden direct een specifiek item moet openen, vul dan het entiteitstype van dat item hier in." },
            { name: "moduleOptions", text: "Alle overige opties voor de module, zie documentatie voor meer informatie over alle beschikbare opties." },
            { name: "moduleQuery", text: "De query die de resultaten ophaalt die in de module getoond moeten worden. Zie documentatie voor meer informatie en voorbeelden." },
            { name: "moduleCountQuery", text: "De query die telt hoeveel resultaten de module in totaal heeft. Dit is nodig voor het gebruik van virtual en inifinite scrolling in de tabel. Voor meer informatie en voorbeelden, zie de documentatie." },
        ];
    }
}

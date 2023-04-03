/* Filter cell operator messages */

if (kendo.ui.FilterCell) {
kendo.ui.FilterCell.prototype.options.operators =
$.extend(true, kendo.ui.FilterCell.prototype.options.operators,{
  "date": {
	"eq": "Is gelijk aan",
	"gt": "Is na",
	"gte": "Is op of na",
	"lt": "Is voor",
	"lte": "Is op of voor",
	"neq": "Is ongelijk aan"
  },
  "enums": {
	"eq": "Is gelijk aan",
	"neq": "Is ongelijk aan"
  },
  "number": {
	"eq": "Is gelijk aan",
	"gt": "Is groter dan",
	"gte": "Is groter of gelijk aan",
	"lt": "Is kleiner dan",
	"lte": "Is kleiner of gelijk aan",
	"neq": "Is ongelijk aan"
  },
  "string": {
	"contains": "Bevat",
	"doesnotcontain": "Bevat niet",
	"endswith": "Eindigt op",
	"eq": "Is gelijk aan",
	"neq": "Is ongelijk aan",
	"startswith": "Begint met"
  }
});
}

/* Filter menu operator messages */

if (kendo.ui.FilterMenu) {
kendo.ui.FilterMenu.prototype.options.operators =
$.extend(true, kendo.ui.FilterMenu.prototype.options.operators,{
  "date": {
	"eq": "Is gelijk aan",
	"gt": "Is na",
	"gte": "Is op of na",
	"lt": "Is voor",
	"lte": "Is op of voor",
	"neq": "Is ongelijk aan",
	"isnull": "Is leeg",
	"isnotnull": "Is niet leeg"
  },
  "enums": {
	"eq": "Is gelijk aan",
	"neq": "Is ongelijk aan"
  },
  "number": {
	"eq": "Is gelijk aan",
	"gt": "Is groter dan",
	"gte": "Is groter of gelijk aan",
	"lt": "Is kleiner dan",
	"lte": "Is kleiner of gelijk aan",
	"neq": "Is ongelijk aan",
	"isnull": "Is leeg",
	"isnotnull": "Is niet leeg"
  },
  "string": {
	"contains": "Bevat",
	"doesnotcontain": "Bevat niet",
	"endswith": "Eindigt op",
	"eq": "Is gelijk aan",
	"neq": "Is ongelijk aan",
	"startswith": "Begint met",
	"isnull": "Is leeg",
	"isnotnull": "Is niet leeg"
  }
});
}

/* ColumnMenu messages */

if (kendo.ui.ColumnMenu) {
kendo.ui.ColumnMenu.prototype.options.messages =
$.extend(true, kendo.ui.ColumnMenu.prototype.options.messages,{
  "columns": "Kolommen",
  "settings": "Kolom instellingen",
  "done": "Gereed",
  "sortAscending": "Sorteer Oplopend",
  "sortDescending": "Sorteer Aflopend",
  "lock": "Slot",
  "unlock": "Ontsluiten"
});
}
/* Filter cell messages */

if (kendo.ui.FilterCell) {
kendo.ui.FilterCell.prototype.options.messages =
$.extend(true, kendo.ui.FilterCell.prototype.options.messages,{
  "clear": "Filter wissen",
  "filter": "Filter",
  "isFalse": "is niet waar",
  "isTrue": "is waar",
  "operator": "Operator"
});
}

/* FilterMenu messages */

if (kendo.ui.FilterMenu) {
kendo.ui.FilterMenu.prototype.options.messages =
$.extend(true, kendo.ui.FilterMenu.prototype.options.messages,{
  "and": "En",
  "cancel": "Annuleren",
  "clear": "Filter wissen",
  "filter": "Filter",
  "info": "Toon items met waarde:",
  "title": "Toon items met waarde",
  "isFalse": "is niet waar",
  "isTrue": "is waar",
  "operator": "Operator",
  "or": "Of",
  "selectValue": "-Selecteer waarde-",
  "value": "Waarde"
});
}

/* FilterMultiCheck messages */

if (kendo.ui.FilterMultiCheck) {
kendo.ui.FilterMultiCheck.prototype.options.messages =
$.extend(true, kendo.ui.FilterMultiCheck.prototype.options.messages,{
  "search": "Zoek"
});
}

/* Grid messages */

if (kendo.ui.Grid) {
kendo.ui.Grid.prototype.options.messages =
$.extend(true, kendo.ui.Grid.prototype.options.messages,{
  "commands": {
	"canceledit": "Annuleren",
	"cancel": "Wijzigingen annuleren",
	"create": "Item toevoegen",
	"destroy": "Verwijderen",
	"edit": "Bewerken",
	"excel": "Export naar Excel",
	"pdf": "Export naar PDF",
	"save": "Wijzigingen opslaan",
	"select": "Selecteren",
	"update": "Bijwerken"
  },
  "editable": {
	"cancelDelete": "Annuleren",
	"confirmation": "Weet u zeker dat u dit item wilt verwijderen?",
	"confirmDelete": "Verwijderen"
  }
});
}

/* Groupable messages */

if (kendo.ui.Groupable) {
kendo.ui.Groupable.prototype.options.messages =
$.extend(true, kendo.ui.Groupable.prototype.options.messages,{
  "empty": "Sleep een kolomtitel in dit vak om de kolom te groeperen."
});
}

/* Pager messages */

if (kendo.ui.Pager) {
kendo.ui.Pager.prototype.options.messages =
$.extend(true, kendo.ui.Pager.prototype.options.messages,{
  "allPages": "All",
  "display": "items {0} - {1} van {2}",
  "empty": "Geen items om te tonen",
  "first": "Ga naar eerste pagina",
  "itemsPerPage": "items per pagina",
  "last": "Ga naar laatste pagina",
  "next": "Ga naar volgende pagina",
  "of": "van {0}",
  "page": "Pagina",
  "previous": "Ga naar vorige pagina",
  "refresh": "Verversen",
  "morePages": "Meer pagina"
});
}
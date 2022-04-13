if (kendo.ui.Scheduler) {
        kendo.ui.Scheduler.prototype.options.messages =
            $.extend(true, kendo.ui.Scheduler.prototype.options.messages,{
                    "allDay": "Toon hele dag",
                    "cancel": "Annuleren",
                    "editable": {
                            "confirmation": "Weet u zeker dat u deze afspraak wilt verwijderen?"
                    },
                    "date": "Datum",
                    "deleteWindowTitle": "Afspraak verwijderen",
                    "destroy": "Verwijderen",
                    "editor": {
                            "allDayEvent": "Duurt hele dag",
                            "description": "Omschrijving",
                            "editorTitle": "Afspraak",
                            "end": "Eind",
                            "endTimezone": "Eindtijd",
                            "repeat": "Terugkeerpatroon",
                            "separateTimezones": "Gebruik verschillende begin- en eindtijd",
                            "start": "Start",
                            "startTimezone": "Begintijd",
                            "timezone": "Pas tijdschema aan",
                            "timezoneEditorButton": "Tijdschema",
                            "timezoneEditorTitle": "Tijdschema's",
                            "title": "Onderwerp",
                            "noTimezone": "No timezone"
                    },
                    "event": "Afspraak",
                    "recurrenceMessages": {
                            "deleteRecurring": "Wilt u alleen dit exemplaar uit de reeks verwijderen of wilt u de hele reeks verwijderen?",
                            "deleteWindowOccurrence": "Verwijder exemplaar",
                            "deleteWindowSeries": "Verwijder reeks",
                            "deleteWindowTitle": "Verwijder terugkeerpatroon",
                            "editRecurring": "Wilt u alleen dit exemplaar uit de reeks bewerken of wilt u de hele reeks bewerken?",
                            "editWindowOccurrence": "Bewerken exemplaar",
                            "editWindowSeries": "Bewerken reeks",
                            "editWindowTitle": "Bewerken terugkeerpatroon"
                    },
                    "save": "Bewaren",
                    "showFullDay": "Toon hele dag",
                    "showWorkDay": "Toon werktijden",
                    "time": "Tijd",
                    "today": "Vandaag",
                    "views": {
                            "agenda": "Agenda",
                            "day": "Dag",
                            "month": "Maand",
                            "week": "Week",
                            "workWeek": "Work Week",
                            "timeline": "Tijdlijn"
                    }
            });
}
(function() {
const options = $.extend({ change: window.dynamicItems.fields.onFieldValueChange.bind(window.dynamicItems.fields) }, {options});
const field = $("#field_{propertyIdWithSuffix}");
const savedValue = field.val();
const readonly = {readonly};

let kendoComponent;

switch(options.type) {
	case "time":
        if (savedValue) {
            var date = Dates.parseTime(savedValue);
            if ((typeof date.isValid === "function" && !date.isValid()) || !date.isValid) {
                date = Dates.parseDateTime(savedValue);
                if ((typeof date.isValid === "function" && !date.isValid()) || !date.isValid) {
                    if (savedValue !== "NOW()") {
                        console.warn("Unable to parse time for field {title}", savedValue, date);
                        kendo.alert("De tijd in het veld '{title}' staat opgeslagen in een onbekend formaat. Let op dat de tijd daarom mogelijk niet klopt en verloren kan gaan bij opslaan van dit item.");
                    }
                    if (savedValue === "NOW()" || options.value === "NOW()") {
                        options.value = new Date();
                    }
                } else {
                    options.value = date.toDate ? date.toDate() : date.toJSDate();
                }
            } else {
                options.value = date.toDate ? date.toDate() : date.toJSDate();
            }
        } else if(options.value === "NOW()") {
            options.value = new Date();
        } else {
			options.value = null;
		}
        
        field.data("kendoControl", "kendoTimePicker");
		kendoComponent = field.kendoTimePicker(options).data("kendoTimePicker");
		break;
	case "date":
        if (savedValue) {
            var date = Dates.parseDate(savedValue);
            if ((typeof date.isValid === "function" && !date.isValid()) || !date.isValid) {
                date = Dates.parseDateTime(savedValue);
                if ((typeof date.isValid === "function" && !date.isValid()) || !date.isValid) {
                    if (savedValue !== "NOW()") {
                        console.warn("Unable to parse time for field {title}", savedValue, date);
                        kendo.alert("De datum in het veld '{title}' staat opgeslagen in een onbekend formaat. Let op dat de datum daarom mogelijk niet klopt en verloren kan gaan bij opslaan van dit item.");
                    }
                    if (savedValue === "NOW()" || options.value === "NOW()") {
                        options.value = new Date();
                    }
                } else {
                    options.value = date.toDate ? date.toDate() : date.toJSDate();
                }
            } else {
                options.value = date.toDate ? date.toDate() : date.toJSDate();
            }
        } else if(options.value === "NOW()") {
            options.value = new Date();
        } else {
			options.value = null;
		}
        
        field.data("kendoControl", "kendoDatePicker");
		kendoComponent = field.kendoDatePicker(options).data("kendoDatePicker");
		break;
	default:
        if (savedValue) {
            var date = Dates.parseDateTime(savedValue);
            if ((typeof date.isValid === "function" && !date.isValid()) || !date.isValid) {
                if (savedValue !== "NOW()") {
                    console.warn("Unable to parse time for field {title}", savedValue, date);
                    kendo.alert("De datum & tijd in het veld '{title}' staat opgeslagen in een onbekend formaat. Let op dat de datum & tijd daarom mogelijk niet klopt en verloren kan gaan bij opslaan van dit item.");
                }
                if (savedValue === "NOW()" || options.value === "NOW()") {
                    options.value = new Date();
                }
            } else {
                options.value = date.toDate ? date.toDate() : date.toJSDate();
            }
        } else if(options.value === "NOW()") {
            options.value = new Date();
        } else {
			options.value = null;
		}
        
		kendoComponent = field.kendoDateTimePicker(options).data("kendoDateTimePicker");
		break;
}

kendoComponent.readonly(readonly);
{customScript}
})();
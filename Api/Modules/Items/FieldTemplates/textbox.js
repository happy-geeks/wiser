(() => {
let field = $("#field_{propertyIdWithSuffix}");
let options = {options};
options.type = options.type || "text";

let codeMirrorSettings = {
    lineNumbers: true,
    indentUnit: 4,
    lineWrapping: true,
    foldGutter: true,
    gutters: ["CodeMirror-linenumbers", "CodeMirror-foldgutter", "CodeMirror-lint-markers"],
    lint: true,
	extraKeys: {
		"Ctrl-Q": function (cm) {
			cm.foldCode(cm.getCursor());
		},
		"F11": function (cm) {
			cm.setOption("fullScreen", !cm.getOption("fullScreen"));
		},
		"Esc": function (cm) {
			if (cm.getOption("fullScreen")) cm.setOption("fullScreen", false);
		},
		"Ctrl-Space": "autocomplete"
	}
};

switch (options.type.toLowerCase()) {
    case "css":
        codeMirrorSettings.mode = "text/css";
        break;
    case "javascript":
        codeMirrorSettings.mode = "text/javascript";
        break;
    case "mysql":
        codeMirrorSettings.mode = "text/x-mysql";
        break;
    case "xml":
        codeMirrorSettings.mode = "application/xml";
        break;
    case "html":
        codeMirrorSettings.mode = "text/html";
        break;
    case "json":
        codeMirrorSettings.mode = "application/json";
        break;
}

if (codeMirrorSettings.mode) {
	// Only load code mirror when we actually need it.
	Misc.ensureCodeMirror().then(() => {
        field.parent().removeAttr("class");
		let codeMirrorInstance = CodeMirror.fromTextArea(field[0], codeMirrorSettings);
		field.data("CodeMirrorInstance", codeMirrorInstance);
	});
}

field.change(window.dynamicItems.fields.onFieldValueChange.bind(window.dynamicItems.fields)).keyup(window.dynamicItems.fields.onTextFieldKeyUp.bind(window.dynamicItems.fields));

{customScript}
})();
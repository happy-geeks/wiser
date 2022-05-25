/*
    Data Selector Plugin
*/

(function () {
    if(typeof _cb === 'undefined') return;
	
	var modalHtml = `<div class="is-modal data-selector-modal">
	<div style="max-width:300px;height:200px;">
		<div class="is-modal-bar is-draggable"><div class="is-modal-close">✕</div></div>
        <div class="form-row">
            <label for="DataSelectorDropDown">Dataselector</label>
            <select id="DataSelectorDropDown"></select>
        </div>
        <div class="form-row">
            <label for="TemplateDropDown">Template</label>
            <select id="TemplateDropDown"></select>
        </div>
        <div style="text-align:right">
            <button title="Cancel" class="input-cancel classic-secondary">Cancel</button>
            <button title="Ok" class="input-ok classic-primary">Ok</button>
        </div>
	</div>
</div>`;
	_cb.addHtml(modalHtml);
	
    var buttonHtml = `<button id="WiserDataSelectorBbutton" title="Data selector met template" style="text-transform:none"><svg class="is-icon-flex" style="width:16px;height:16px;"><use xlink:href="#ion-ios-gear"></use></svg></button>`;

    _cb.addButton('WiserDataSelector', buttonHtml, '#WiserDataSelectorBbutton', function () {
        showDataSelectorDialog();
    });
	
    _cb.addButton2('WiserDataSelector', buttonHtml, '#WiserDataSelectorBbutton', function () {
        showDataSelectorDialog();
    });

    let dialogInitialized = false;
	async function showDataSelectorDialog() {
        const modal = document.querySelector('.data-selector-modal');
        const dataSelectorDropDown = modal.querySelector("#DataSelectorDropDown");
        const templateDropDown = modal.querySelector("#TemplateDropDown");

        _cb.showModal(modal);
        
        if (dialogInitialized) {
            return;
        }

        const dataSelectors = await main.dataSelectorsService.getAll(true);
        for (let dataSelector of dataSelectors.data || []) {
            const option = document.createElement("option");
            option.value = dataSelector.id;
            option.text = dataSelector.name;
            dataSelectorDropDown.add(option);
        }

        const dataSelectorTemplates = await main.dataSelectorsService.getTemplates();
        for (let template of dataSelectorTemplates.data || []) {
            const option = document.createElement("option");
            option.value = template.id;
            option.text = template.title;
            templateDropDown.add(option);
        }

        let closeButton = modal.querySelector(".is-modal-close");
        closeButton.addEventListener("click", function (e) {
            _cb.hideModal(modal);
        });

        closeButton = modal.querySelector(".input-cancel");
        closeButton.addEventListener("click", function (e) {
            _cb.hideModal(modal);
        });

        const okButton = modal.querySelector(".input-ok");
        okButton.addEventListener("click", async function (e) {
            const dataSelectorDropDown = document.querySelector("#DataSelectorDropDown");
            const dataSelectorTemplateDropDown = document.querySelector("#TemplateDropDown");
            const selectedDataSelector = dataSelectorDropDown.value;
            const selectedTemplate = dataSelectorTemplateDropDown.value;
            if (!selectedDataSelector || !selectedTemplate) {
                alert("Kies a.u.b. een data selector en een template.")
                return false;
            }

            let html = `<div class="dynamic-content" data-selector-id="${selectedDataSelector}" template-id="${selectedTemplate}"><h2>Data selector '${dataSelectorDropDown.selectedOptions[0].text}' met template '${dataSelectorTemplateDropDown.selectedOptions[0].text}'</h2></div>`;
            const previewResult = await main.dataSelectorsService.generatePreview(html);
            if (previewResult.success && previewResult.data) {
                html = previewResult.data;
            }
            _cb.pasteHtmlAtCaret(html, false);
            if (previewResult.success) {
                _cb.hideModal(modal);
            }
        });
        
        dialogInitialized = true;
	}
})();
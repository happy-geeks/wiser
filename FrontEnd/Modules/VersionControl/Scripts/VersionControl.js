import { TrackJS } from "trackjs";
import { Modules, Dates, Wiser2 } from "../../Base/Scripts/Utils.js";
import "../../Base/Scripts/Processing.js";
import { DateTime } from "luxon";
//import { Fields } from "./Fields.js";
//import { Dialogs } from "./Dialogs.js";
import { Grids } from "./Grids.js";
import { Commit } from "./Commit.js";
import { Template } from "./Template.js";
window.JSZip = require("jszip");

require("@progress/kendo-ui/js/kendo.notification.js");
require("@progress/kendo-ui/js/kendo.tooltip.js");
require("@progress/kendo-ui/js/kendo.button.js");
require("@progress/kendo-ui/js/kendo.dialog.js");
require("@progress/kendo-ui/js/kendo.grid.js");
require("@progress/kendo-ui/js/kendo.tabstrip.js");
require("@progress/kendo-ui/js/kendo.sortable.js");
require("@progress/kendo-ui/js/kendo.validator.js");
require("@progress/kendo-ui/js/kendo.splitter.js");
require("@progress/kendo-ui/js/kendo.treeview.js");
require("@progress/kendo-ui/js/cultures/kendo.culture.nl-NL.js");
require("@progress/kendo-ui/js/messages/kendo.messages.nl-NL.js");

import "../Css/VersionControl.css";

// Any custom settings can be added here. They will overwrite most default settings inside the module.
const moduleSettings = {

};

((settings) => {
    /**
     * Main class.
     */
    class VersionControl {

        /**
         * Initializes a new instance of DynamicItems.
         * @param {any} settings An object containing the settings for this class.
         */
        constructor(settings) {
            this.base = this;

            // Sub classes.
            //this.dragAndDrop = null;
            //this.dialogs = null;
            this.grids = null;
            this.commit = null;
            this.template = null;

            this.newItemId = null;
            this.selectedItem = null;
            this.selectedItemTitle = null;

            // Kendo components.
            this.notification = null;
            this.mainSplitter = null;
            this.mainTreeView = null;
            this.mainTreeViewContextMenu = null;
            this.mainTabStrip = null;
            this.mainTabStripSortable = null;
            this.mainValidator = null;

            // Other.
            this.mainLoader = null;

            // Set the Kendo culture to Dutch. TODO: Base this on the language in Wiser.
            kendo.culture("nl-NL");

            // Flags to use in wiser_field_templates, so that we can add code there that depends on code in this file, without having to deploy this to live right away.
            this.fieldTemplateFlags = {
                enableSubEntitiesGridsOrdering: true
            };

            // Default settings
            this.settings = {
                moduleId: 0,
                encryptedModuleId: "",
                customerId: 0,
                initialItemId: null,
                iframeMode: false,
                gridViewMode: false,
                openGridItemsInBlock: false,
                username: "Onbekend",
                userEmailAddress: "",
                userType: ""
            };
            Object.assign(this.settings, settings);

            this.permissionsEnum = Object.freeze({
                read: 1 << 0,
                create: 1 << 1,
                update: 1 << 2,
                delete: 1 << 3
            });

            this.environmentsEnum = Object.freeze({
                hidden: 0,
                development: 1 << 0,
                test: 1 << 1,
                acceptance: 1 << 2,
                live: 1 << 3
            });

            this.comparisonOperatorsEnum = Object.freeze({
                equals: "eq",
                doesNotEqual: "neq",
                contains: "contains",
                doesNotContain: "doesnotcontain",
                startsWith: "startswith",
                doesNotStartWith: "doesnotstartwith",
                endsWith: "endswith",
                doesNotEndWith: "doesnotendwith",
                isEmpty: "isempty",
                isNotEmpty: "isnotempty",
                isGreaterThan: "gt",
                isGreaterThanOrEquals: "gte",
                isLessThan: "lt",
                isLessThanOrEquals: "lte"
            });

            this.dependencyActionsEnum = Object.freeze({
                toggleVisibility: "toggle-visibility",
                refresh: "refresh"
            });

            // Create instances of sub classes.
            //this.dragAndDrop = new DragAndDrop(this);
            //this.dialogs = new Dialogs(this);
            this.grids = new Grids(this);
            this.commit = new Commit(this);
            this.template = new Template(this);

            // Add logged in user access token to default authorization headers for all jQuery ajax requests.
            $.ajaxSetup({
                headers: { "Authorization": `Bearer ${localStorage.getItem("accessToken")}` }
            });

            // Fire event on page ready for direct actions
            $(document).ready(() => {
                this.onPageReady();
            });
        }

        /**
         * Event that will be fired when the page is ready.
         */
        async onPageReady() {
            this.mainLoader = $("#mainLoader");
            
            // Setup processing.
            document.addEventListener("processing.Busy", this.toggleMainLoader.bind(this, true));
            document.addEventListener("processing.Idle", this.toggleMainLoader.bind(this, false));

            
            
            // Fullscreen event for elements that can go fullscreen, such as HTML editors.
            const classHolder = $(document.documentElement);
            const fullscreenChange = "webkitfullscreenchange mozfullscreenchange fullscreenchange MSFullscreenChange";
            $(document).bind(fullscreenChange, $.proxy(classHolder.toggleClass, classHolder, "k-fullscreen"));

            // Setup any settings from the body element data. These settings are added via the Wiser backend and they take preference.
            Object.assign(this.settings, $("body").data());

            if (this.settings.trackJsToken) {
                TrackJS.install({
                    token: this.settings.trackJsToken
                });
            }

            const user = JSON.parse(localStorage.getItem("userData"));
            this.settings.oldStyleUserId = user.oldStyleUserId;
            this.settings.username = user.adminAccountName ? `Happy Horizon (${user.adminAccountName})` : user.name;
            this.settings.adminAccountLoggedIn = !!user.adminAccountName;

            const userData = await Wiser2.getLoggedInUserData(this.settings.wiserApiRoot);
            this.settings.userId = userData.encryptedId;
            this.settings.customerId = userData.encryptedCustomerId;
            this.settings.zeroEncrypted = userData.zeroEncrypted;
            this.settings.filesRootId = userData.filesRootId;
            this.settings.imagesRootId = userData.imagesRootId;
            this.settings.templatesRootId = userData.templatesRootId;
            this.settings.mainDomain = userData.mainDomain;

            if (!this.settings.wiserApiRoot.endsWith("/")) {
                this.settings.wiserApiRoot += "/";
            }
            console.log("aaaa");
            this.settings.serviceRoot = `${this.settings.wiserApiRoot}templates/get-and-execute-query`;
            this.settings.htmlEditorCssUrl = `${this.settings.wiserApiRoot}templates/css-for-html-editors?encryptedCustomerId=${encodeURIComponent(this.base.settings.customerId)}&isTest=${this.base.settings.isTestEnvironment}&encryptedUserId=${encodeURIComponent(this.base.settings.userId)}&username=${encodeURIComponent(this.base.settings.username)}&userType=${encodeURIComponent(this.base.settings.userType)}&subDomain=${encodeURIComponent(this.base.settings.subDomain)}`
            console.log("aaaa");
            const extraModuleSettings = await Modules.getModuleSettings(this.settings.wiserApiRoot, this.settings.moduleId);
            Object.assign(this.settings, extraModuleSettings.options);
            let permissions = Object.assign({}, extraModuleSettings);
            delete permissions.options;
            this.settings.permissions = permissions;
            this.settings.getItemsUrl = `${this.settings.wiserApiRoot}data-selectors`;
            $("body").toggleClass("gridViewMode", this.settings.gridViewMode);
            
            this.setupBindings();
            this.initializeKendoComponents();

            // Initialize sub classes.
            //this.dragAndDrop.initialize();
            //await this.dialogs.initialize();
            this.grids.initialize();
            this.commit.initialize();
            this.template.initialize();
            

            if (this.settings.iframeMode && this.settings.hideHeader) {
                $("#tabstrip > ul").addClass("hidden");
            }
            if (this.settings.iframeMode && this.settings.hideFooter) {
                $("#right-pane > footer").addClass("hidden");
            } 

            
        }
        
        /**
         * Setup all basis bindings for this module.
         * Specific bindings (for buttons in certain pop-ups for example) will be set when they are needed.
         */
        setupBindings() {
            // Do stuff when the module is being closed in Wiser 1.0.
            

            // Keyboard shortcuts
            $("body").on("keyup", (event) => {
                const target = $(event.target);

                if (target.prop("tagName") === "INPUT" || target.prop("tagName") === "TEXTAREA") {
                    return;
                }

                switch (event.key) {
                    case "N":
                        {
                            const addButton = $("#addButton");
                            if (event.shiftKey && addButton.is(":visible")) {
                                addButton.click();
                            }
                            break;
                        }
                }
            });


            // Close first alert.
            $(".alert-close").click((event) => {
                var target = $(event.target);

                target.closest(".alert-overlay").hide();
            });

            // Close panel
            $(".close-panel").click((event) => {
                var target = $(event.target);

                target.closest(".k-window").find(".entity-container").removeClass("info-active");
                target.closest(".k-window").find("#right-pane").removeClass("info-active");
            });

            $("body").on("click", ".imgZoom", function () {
                const image = $(this).parents(".product").find("img");
                const dialogElement = $("#imageDialog");
                let dialog = dialogElement.data("kendoDialog");
                if (!dialog) {
                    dialog = dialogElement.kendoDialog({
                        title: "Afbeelding",
                        closable: true,
                        modal: false,
                        resizable: true
                    }).data("kendoDialog");
                }

                dialog.content(image.clone());
                dialog.open();
            });

            $("body").on("click", ".imgEdit", function () {
                const image = $(this).parents(".product").find("img");
                kendo.alert("Deze functionaliteit is nog niet geïmplementeerd");
            });

            

            $("#mainScreenForm").submit(event => { event.preventDefault(); });

          

            $("#mainEditMenu .deleteItem").click(async (event) => {
                this.onDeleteItemClick(event, this.settings.iframeMode ? this.settings.initialItemId : this.selectedItem.id, this.settings.iframeMode ? this.settings.entityType : this.selectedItem.entityType);
            });

            $("#mainEditMenu .undeleteItem").click(async (event) => {
                this.onUndeleteItemClick(event, this.settings.iframeMode ? this.settings.initialItemId : this.selectedItem.id);
            });

            
            
        }
        

        /**
         * Initializes all kendo components for the base class.
         */
        

        initializeKendoComponents() {
            $("#dialog").kendoDialog({
                title: "Dialog",
                visible: false,
                content: "",
                closable: true,
                modal: true,
                actions: [
                    { text: 'Ja', action: this.onCommitSelectedItemsWithDynamicContentItems.bind(this) },
                    { text: 'Nee', action: this.onCommitOnlySelectedItems.bind(this) },
                    { text: 'Cancel', action: this.onCancel }
                ]
            }).data("kendoDialog");

          
            $("#commitTemplate").kendoButton({
                click: this.onCommit.bind(this),
                icon: "save"
            });

            $("#commitDynamicContent").kendoButton({
                click: this.onCommitDynamicContent.bind(this),
                icon: "save"
            });

            $(".evironment_deploy, .evironment_deploy, .evironment_deploy").kendoButton({
                click: this.Deploy.bind(this),
                icon: "save"
            });

            $(".evironment_deploy_template_history").kendoButton({
                click: this.HistoryTemplatesRevert.bind(this),
                icon: "save"
            });
            /*
            $(".environment_dynamic_content, .environment_dynamic_content, .environment_dynamic_content").kendoButton({
                click: this.DeployDynamicContent.bind(this),
                icon: "save"
            });*/

            $(".environment_dynamic_content_history").kendoButton({
                click: this.HistoryDynamicCOntent.bind(this),
                icon: "save"
            });

            /*
            $("#commitLowerVersions").kendoButton({
                click: this.onCommitLowerVersion.bind(this),
                icon: "save"
            });*/

          

            // Normal notifications.
            this.notification = $("#alert").kendoNotification({
                button: true,
                autoHideAfter: 5000,
                stacking: "down",
                position: {
                    top: 0,
                    left: 0,
                    right: 0,
                    bottom: null,
                    pinned: true
                },
                show: this.onShowNotification,
                templates: [{
                    type: "error",
                    template: $("#errorTemplate").html()
                }, {
                    type: "success",
                    template: $("#successTemplate").html()
                }]
            }).data("kendoNotification");

          

            this.mainTabStripSortable = $("#tabstrip ul.k-tabstrip-items").kendoSortable({
                filter: "li.k-item",
                axis: "x",
                container: "ul.k-tabstrip-items",
                hint: (element) => {
                    return $(`<div id='hint' class='k-widget k-header k-tabstrip'><ul class='k-tabstrip-items k-reset'><li class='k-item k-state-active k-tab-on-top'>${element.html()}</li></ul></div>`);
                    console.log("SORT");
                },
                start: (event) => {
                    this.mainTabStrip.activateTab(event.item);
                    console.log("SORT");
                },
                change: (event) => {
                    const reference = this.mainTabStrip.tabGroup.children().eq(event.newIndex);
                    console.log("SORT");
                    if (event.oldIndex < event.newIndex) {
                        this.mainTabStrip.insertAfter(event.item, reference);
                    } else {
                        this.mainTabStrip.insertBefore(event.item, reference);
                    }
                }
            }).data("kendoSortable");

            // Validator.
            this.mainValidator = $("#right-pane").kendoValidator({
                validate: this.onValidateForm.bind(this, this.mainTabStrip),
                validateOnBlur: false,
                messages: {
                    required: (input) => {
                        const fieldDisplayName = $(input).closest(".item").find("> h4 > label").text() || $(input).attr("name");
                        return `${fieldDisplayName} is verplicht`;
                    }
                }
            }).data("kendoValidator");

            // Some things should not be done if we're in iframe mode.
            if (this.settings.iframeMode || this.settings.gridViewMode) {
                return;
            }

            /***** NOTE: Only add code below this line that should NOT be executed if the module is loaded inside an iframe *****/
            // Splitter.
            /*this.mainSplitter = $("#horizontal").kendoSplitter({
                panes: [{
                    collapsible: true,
                    size: "20%"
                }, {
                    collapsible: false
                }]
            }).data("kendoSplitter");
            this.mainSplitter.resize(true);*/

            
            
            
        }




        async Deploy(event) {

            console.log(this.settings.username);
            console.log(this.settings.username);
            var envioronmentbuttonValue = event.event.target.value;
            //GET THE DYNAMIC CONTENT ID
            var commitGrid = document.querySelector("#deploygrid");
            var commits = commitGrid.querySelectorAll(".k-state-selected");


            console.log("DEPLOY");
          
            console.log(envioronmentbuttonValue);
            console.log(commits);


            for (const [key, value] of Object.entries(commits)) {


                var commitId = value.querySelector('[data-field="id"]').innerHTML;


                var templates = await this.template.GetTemplatesFromCommit(commitId);
                var dynamicContent = await this.GetDynamicContentFromCommit(commitId);

                console.log(templates);
                console.log(dynamicContent);

                for (const [key, value] of Object.entries(templates)) {

                    await this.template.PublishTemplate(value["templateId"], envioronmentbuttonValue, value["version"]);
                }

                for (const [key, value] of Object.entries(dynamicContent)) {

                    await this.PublishDynamicContent(value["dynamicContentId"], envioronmentbuttonValue, value["version"]);
                }


                console.log(templates);
            }

            //haal de hoogste versie template op van deze commit
            



            //haal de hoogste versie dynamic content op van deze commit


            //var templateData = await this.getSelectedTemplateWithIdAndVersion(templateVersionId, version);
            //var templateId =  templateData['templateId'];

            //console.log(templateId);

            //zet template naar de omgeving
      
            //await this.PublishTemplate(templateVersionId, envioronmentbuttonValue, version);


            //voeg aanpassing toe aan de log
            //await this.CreatePublishLog(templateVersionId, envioronmentbuttonValue, version);
                
           // console.log(event.event.target.value);
            //console.log(version);

            //document.location.reload();

        }

        async DeployDynamicContent(event) {
            var envioronmentbuttonValue = event.event.target.value;
            //GET THE DYNAMIC CONTENT ID
            var dynamicContentId = this.getSelectedDynamicContentId("#DynamicContentDeployGrid");
            var version = document.querySelector(".k-state-selected").innerHTML;

        


            console.log(envioronmentbuttonValue);
            console.log(dynamicContentId);
            console.log(version);



            //var templateData = await this.getSelectedTemplateWithIdAndVersion(templateVersionId, version);
            //var templateId =  templateData['templateId'];

            //console.log(templateId);

            //zet template naar de omgeving

            // PUBLISH THE DYNAMIC CONTENT NOT THE TEMPLATE
            //await this.PublishDynamicContent(dynamicContentId, envioronmentbuttonValue, version);


            //voeg aanpassing toe aan de log
            //await this.CreatePublishLog(templateVersionId, envioronmentbuttonValue, version);

            // console.log(event.event.target.value);
            //console.log(version);

           // document.location.reload();
        }

        async HistoryTemplatesRevert(event) {

        }

        async HistoryDynamicCOntent(event) {
            var envioronmentbuttonValue = event.event.target.value;
            //GET THE DYNAMIC CONTENT ID
            var dynamicContentId = this.getSelectedDynamicContentId("#historyDynamicContentGridId");
            var version = document.querySelector(".k-state-selected").querySelector('[data-field="version"]').innerHTML;




            console.log(envioronmentbuttonValue);
            console.log(dynamicContentId);
            console.log(version);

            //var templateData = await this.getSelectedTemplateWithIdAndVersion(templateVersionId, version);
            //var templateId =  templateData['templateId'];

            //console.log(templateId);

            //zet template naar de omgeving

            // PUBLISH THE DYNAMIC CONTENT NOT THE TEMPLATE
            await this.PublishDynamicContent(dynamicContentId, envioronmentbuttonValue, version);


 

            document.location.reload();
        }


        async onCommitDynamicContent(event) {
            var dynamicContentId = this.getSelectedDynamicContentId("#dynamicContentGrid");

            const selectedChoices = this.getSlectedDeployment();

            var commitMessage = this.getCommitMessage();

            var version = this.getDynamicContentVersion();


            var dynamicContent = await this.getSelectedDynamicContent(dynamicContentId, version);

            console.log(dynamicContent);

            var contentId = dynamicContent['componentModeId'];

           

            await this.commit.CreateNewCommit(contentId, commitMessage, "Admin");

            var publishEnviornment = "";
            var publishNumber;

            if (selectedChoices[0] != null) {
                //isTest = true;
                publishNumber = 2;
                publishEnviornment = "test";
            }

            if (selectedChoices[1] != null) {
                //isAcceptance = true;
                publishNumber = 4;
                publishEnviornment = "accept";
            }

            if (selectedChoices[2] != null) {
                //isLive = true;
                publishNumber = 8;
                publishEnviornment = "live";
            }

            var data1 = await this.commit.GetCommitWithId()
            var commitId = data1['id'];


            var lowerVersionTemplates = await this.GetDynamicContentWithLowerVersion(dynamicContentId, version);


            for (const [key, value] of Object.entries(lowerVersionTemplates)) {
                await this.PutDynamicContentCommit(commitId, value, key, publishEnviornment);
            }



            await this.PutDynamicContentCommit(commitId, dynamicContentId, version, publishEnviornment);

            await this.PublishDynamicContent(dynamicContentId, publishEnviornment, version);
       

           
            document.location.reload();
        }

        async onCommitSelectedOnly() {
            //return;
            //get alll selected items
            //selected templates
            const templateTable = document.querySelector("#gridView");
            const templateSelected = templateTable.querySelectorAll(".k-state-selected");


            console.log(templateTable);
            console.log(templateSelected);


            //selected dynamische content
            //NOG IMPLEMENTEREN
            const dynamicContentTable = document.querySelector("#dynamicContentGrid");
            const dynamicContentSelected = dynamicContentTable.querySelectorAll(".k-state-selected");





            //selected Environment choices
            const selectedChoices = this.getSlectedDeployment();

            var isTest = false;
            var isAcceptance = false;
            var isLive = false;
            var publishEnviornment = "";
            var publishNumber;

            if (selectedChoices[0] != null) {
                isTest = true;
                publishNumber = 2;
                publishEnviornment = "test";
            }

            if (selectedChoices[1] != null) {
                isAcceptance = true;
                publishNumber = 4;
                publishEnviornment = "accept";
            }

            if (selectedChoices[2] != null) {
                isLive = true;
                publishNumber = 8;
                publishEnviornment = "live";
            }



            //Commit message
            //return this commit message
            var commitMessage = this.getCommitMessage();
            if (commitMessage == "") {
                kendo.alert("Voer een commit message in");
                return;
            }


            /* if (templateVersionId == null) {
                 kendo.alert("Selecteer een template.");
                 return;
             }*/


            await this.commit.CreateNewCommit(commitMessage, "Admin");
            var createdCommit = await this.commit.GetCommitWithId()
            var commitId = createdCommit['id'];
            console.log(createdCommit);
            console.log(commitId);

            for (const [key, value] of Object.entries(templateSelected)) {



                var templateVersionId = value.querySelector('[data-field="template_id"]').innerHTML;
                var version = value.querySelector('[data-field="version"]').innerHTML;
                // var templateData = await this.getSelectedTemplateWithIdAndVersion(templateVersionId, version);
                //var templateId = templateData['templateId'];

                console.log(value);
                console.log(templateVersionId);
                console.log(version);


                //gets lower version of the template
                var lowerVersionTemplates = await this.template.GetTemplatesWithLowerVersion(templateVersionId, version);



                //console.log(Object.keys(lowerVersionTemplates).length);


                for (const [key, value] of Object.entries(lowerVersionTemplates)) {
                    await this.commit.PutTemplateCommit(commitId, value, key, publishEnviornment);
                }


                await this.commit.PutTemplateCommit(commitId, templateVersionId, version, publishEnviornment);

                await this.template.PublishTemplate(templateVersionId, publishEnviornment, version);

            }

            for (const [key, value] of Object.entries(dynamicContentSelected)) {

                var dynamicContentId = value.querySelector('[data-field="content_id"]').innerHTML;
                var version = value.querySelector('[data-field="version"]').innerHTML;
                //var templateData = await this.getSelectedTemplateWithIdAndVersion(dynamicContentId, version);
                //var templateId = templateData['templateId'];

                //gets lower version of the template
                var lowerVersionDynamicContent = await this.GetDynamicContentWithLowerVersion(dynamicContentId, version);



                console.log(Object.keys(lowerVersionDynamicContent).length);


                for (const [key, value] of Object.entries(lowerVersionDynamicContent)) {
                    await this.PutDynamicContentCommit(commitId, value, key, publishEnviornment);
                }


                await this.PutDynamicContentCommit(commitId, dynamicContentId, version, publishEnviornment);

                await this.PublishDynamicContent(dynamicContentId, publishEnviornment, version);
            }

        }

        async onCommitSelectedAndRelatedDynamicConent() {
            //foreach template selected
            //check for related content


            //get alll selected items
            //selected templates
            const templateTable = document.querySelector("#gridView");
            const templateSelected = templateTable.querySelectorAll(".k-state-selected");



            //selected dynamische content
            //NOG IMPLEMENTEREN
            const dynamicContentTable = document.querySelector("#dynamicContentGrid");
            const dynamicContentSelected = dynamicContentTable.querySelectorAll(".k-state-selected");


            var DynamicContentList = [];


            for (const [key, value] of Object.entries(templateSelected)) {

                var templateVersionId = value.querySelector('[data-field="template_id"]').innerHTML;

                var DynamicContentListResults = await this.DynamicContentInTemplates(templateVersionId);

                DynamicContentList.push(DynamicContentListResults);

            }


            
            //selected Environment choices
            const selectedChoices = this.getSlectedDeployment();

            var isTest = false;
            var isAcceptance = false;
            var isLive = false;
            var publishEnviornment = "";
            var publishNumber;

            if (selectedChoices[0] != null) {
                isTest = true;
                publishNumber = 2;
                publishEnviornment = "test";
            }

            if (selectedChoices[1] != null) {
                isAcceptance = true;
                publishNumber = 4;
                publishEnviornment = "accept";
            }

            if (selectedChoices[2] != null) {
                isLive = true;
                publishNumber = 8;
                publishEnviornment = "live";
            }



            //Commit message
            //return this commit message
            var commitMessage = this.getCommitMessage();
            if (commitMessage == "") {
                kendo.alert("Voer een commit message in");
                return;
            }


            /* if (templateVersionId == null) {
                 kendo.alert("Selecteer een template.");
                 return;
             }*/


            await this.commit.CreateNewCommit(commitMessage, "Admin");
            var createdCommit = await this.commit.GetCommitWithId()
            var commitId = createdCommit['id'];

            
            for (const [key, value] of Object.entries(templateSelected)) {



                var templateVersionId = value.querySelector('[data-field="template_id"]').innerHTML;
                var version = value.querySelector('[data-field="version"]').innerHTML;
                // var templateData = await this.getSelectedTemplateWithIdAndVersion(templateVersionId, version);
                //var templateId = templateData['templateId'];



                //gets lower version of the template
                var lowerVersionTemplates = await this.template.GetTemplatesWithLowerVersion(templateVersionId, version);



                //console.log(Object.keys(lowerVersionTemplates).length);


                for (const [key, value] of Object.entries(lowerVersionTemplates)) {
                    await this.commit.PutTemplateCommit(commitId, value, key, publishEnviornment);
                }


                await this.commit.PutTemplateCommit(commitId, templateVersionId, version, publishEnviornment);

                await this.template.PublishTemplate(templateVersionId, publishEnviornment, version);

            }
            
            for (const [key, value] of Object.entries(dynamicContentSelected)) {

                var dynamicContentId = value.querySelector('[data-field="content_id"]').innerHTML;
                var version = value.querySelector('[data-field="version"]').innerHTML;
                //var templateData = await this.getSelectedTemplateWithIdAndVersion(dynamicContentId, version);
                //var templateId = templateData['templateId'];

                //gets lower version of the template
                var lowerVersionDynamicContent = await this.GetDynamicContentWithLowerVersion(dynamicContentId, version);




                for (const [key, value] of Object.entries(lowerVersionDynamicContent)) {
                    await this.PutDynamicContentCommit(commitId, value, key, publishEnviornment);
                }


                await this.PutDynamicContentCommit(commitId, dynamicContentId, version, publishEnviornment);

                await this.PublishDynamicContent(dynamicContentId, publishEnviornment, version);
            }








            for (const [key, value] of Object.entries(DynamicContentList)) {

                var dynamicContentFromTemplate = value;
                for (const [key, value] of Object.entries(dynamicContentFromTemplate)) {
                    var dynamicContentInTemplateId = value["id"];
                    var dynamicContentInTemplateVersion = value["version"];

                    console.log(value);
                    console.log(dynamicContentInTemplateId);
                    console.log(dynamicContentInTemplateVersion);


                    var alreadySelected = false;




                    for (const [key, value2] of Object.entries(dynamicContentSelected)) {
                        var dynamicContentSelectedId = value2.querySelector('[data-field="content_id"]').innerHTML;
                        var dynamicContentSelectedVersion = value2.querySelector('[data-field="version"]').innerHTML;

                        console.log(dynamicContentSelectedId);
                        console.log(dynamicContentSelectedVersion);

                        if (dynamicContentInTemplateId == dynamicContentSelectedId && dynamicContentInTemplateVersion == dynamicContentSelectedVersion) {
                            //al geselecteerd
                            alreadySelected = true;

                            break;
                        }

                    }

                    value["selected"] = alreadySelected;


                }

            }







            //DynamicContentList
            for (const [key, value] of Object.entries(DynamicContentList)) {

                var dynamicConentItemFromTemplate = value;
                

   
                var dynamicContent = value
                     
                for (const [key, value] of Object.entries(dynamicContent)) {

                    console.log(value);
                    if (value["selected"]) {
                        //SKIP THIS FROM COMMITTIN BECOUSE YOU ALREADY SELECTED IT
                    } else {

                        var dynamicContentId = value["id"];
                        var version = value["version"];
                        console.log(value);
                        //var templateData = await this.getSelectedTemplateWithIdAndVersion(dynamicContentId, version);
                        //var templateId = templateData['templateId'];
    
                        //gets lower version of the template
                        var lowerVersionDynamicContent = await this.GetDynamicContentWithLowerVersion(dynamicContentId, version);
    
    
    
    
                        for (const [key, value] of Object.entries(lowerVersionDynamicContent)) {
                            await this.PutDynamicContentCommit(commitId, value, key, publishEnviornment);
                        }
    
    
                        await this.PutDynamicContentCommit(commitId, dynamicContentId, version, publishEnviornment);
    
                        await this.PublishDynamicContent(dynamicContentId, publishEnviornment, version);
                    }
                }
                
            }
        }


        
        //Dyalogue popup choices
        onCancel() {
            console.log("action :: cancel");
        }

        async onCommitSelectedItemsWithDynamicContentItems() {
            console.log("action :: Templates gecommit met dynamic content");
            this.onCommitSelectedAndRelatedDynamicConent();
            //document.location.reload();
        }


        async onCommitOnlySelectedItems() {
            console.log("action :: Templates committet zonder dynamic content");
            this.onCommitSelectedOnly();
           // document.location.reload();
        }


        

        async onCommit(event) {

            const templateTable1 = document.querySelector("#gridView");
            const templateSelected1 = templateTable1.querySelectorAll(".k-state-selected");

            var DynamicContentList = [];

            const dynamicContentTable = document.querySelector("#dynamicContentGrid");
            const dynamicContentSelected = dynamicContentTable.querySelectorAll(".k-state-selected");


            for (const [key, value] of Object.entries(templateSelected1)) {

                var templateVersionId = value.querySelector('[data-field="template_id"]').innerHTML;

                var DynamicContentListResults = await this.DynamicContentInTemplates(templateVersionId);

                DynamicContentList.push(DynamicContentListResults);

              //  await this.commit.PutTemplateCommit(commitId, value, key, publishEnviornment);
            }



            for (const [key, value] of Object.entries(DynamicContentListResults)) {
                var dynamicContentInTemplateId = value["id"];
                var dynamicContentInTemplateVersion = value["version"];

                console.log(value);
                console.log(dynamicContentInTemplateId);
                console.log(dynamicContentInTemplateVersion);


                var alreadySelected = false;

                    


                for (const [key, value2] of Object.entries(dynamicContentSelected)) {
                    var dynamicContentSelectedId = value2.querySelector('[data-field="content_id"]').innerHTML;
                    var dynamicContentSelectedVersion = value2.querySelector('[data-field="version"]').innerHTML;

                    console.log(dynamicContentSelectedId);
                    console.log(dynamicContentSelectedVersion);

                    if (dynamicContentInTemplateId == dynamicContentSelectedId && dynamicContentInTemplateVersion == dynamicContentSelectedVersion) {
                        //al geselecteerd
                        alreadySelected = true;
                        
                        break;
                    }

                }

                value["selected"] = alreadySelected;

               
            }
            

            var showDialog = false;
            for (const [key, value] of Object.entries(DynamicContentListResults)) {

                if (!value["selected"]) {
                    showDialog = true;
                }

            }

            

            if (showDialog) {
                var dialog = $('#dialog').data("kendoDialog");

                var table = "<table><tbody>";

                table += "<tr><td>Id</td></tr>";
                table += "<tr><td>Version</td></tr>";

                for (const [key, value] of Object.entries(DynamicContentList)) {

                   
                    var dynamicContentItem = value;

                    for (const [key, value] of Object.entries(dynamicContentItem)) {
                        table += "<tr>";

                        var id = value["id"];
                        var version = value["version"];
                        table += "<td>" + id + "</td>";
                        table += "<td>" + version + "</td>";

                        table += "</tr>";
                    }

               
                   
                }
                table += "</tbody></table>";


                dialog.content("<p>Er zijn dynamische content gegevens van deze template die niet geselecteerd zijn. Wilt u deze wel mee committen?</p>");
                dialog.open();
            } else {
                console.log("DONT SHOW DIALOG");
                this.onCommitSelectedOnly();
            }

           
        }





        //Deploy
        

        async DynamicContentInTemplates(templateId) {
            try {

                const createCommit = await Wiser2.api({
                    url: `${this.base.settings.wiserApiRoot}VersionControl/dynamic-content-in-template/${templateId}`,
                    method: "GET",
                    contentType: "application/json",
                });

                return createCommit;

            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan. Sluit a.u.b. deze module, open deze daarna opnieuw en probeer het vervolgens opnieuw. Of neem contact op als dat niet werkt.");
            }
        }

      

        async PublishDynamicContent(dynamicContentId, environment, version) {
            

            try {
                const createCommit = await Wiser2.api({
                    url: `${this.base.settings.wiserApiRoot}VersionControl/${dynamicContentId}/publishDyamicContent/${environment}/${version}`,
                    method: "POST",
                    contentType: "application/json",
                });

                return createCommit;

            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan. Sluit a.u.b. deze module, open deze daarna opnieuw en probeer het vervolgens opnieuw. Of neem contact op als dat niet werkt.");
            }
        } 

    

        async GetDynamicContentFromCommit(commitId) {

            try {
                const createCommit = await Wiser2.api({
                    url: `${this.base.settings.wiserApiRoot}VersionControl/dynamic_content-of-commit/${commitId}`,
                    method: "GET",
                    contentType: "application/json",
                });

                return createCommit;

            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan. Sluit a.u.b. deze module, open deze daarna opnieuw en probeer het vervolgens opnieuw. Of neem contact op als dat niet werkt.");
            }
        }

        //Commit change history
      

        async CreateNewCommitItem(commitId, templateId, version) {
            try {

            
                const commitItemData = {
                    CommitId: commitId,
                    TemplateId: templateId,
                    Version: version
                }


                const createCommit = await Wiser2.api({
                    url: `${this.base.settings.wiserApiRoot}VersionControl/${templateId}`,
                    method: "PUT",
                    contentType: "application/json",
                    data: JSON.stringify(commitItemData)
                });

            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan. Sluit a.u.b. deze module, open deze daarna opnieuw en probeer het vervolgens opnieuw. Of neem contact op als dat niet werkt.");
            }
        }

        

       

        async PutDynamicContentCommit(commitId, dynamicContentId, version) {
            try {

                const dynamicContentCommitData = {
                    CommitId: commitId,
                    DynamicContentId: dynamicContentId,
                    Version: version,

                }

                const createCommit = await Wiser2.api({
                    url: `${this.base.settings.wiserApiRoot}VersionControl/dynamic-content-commit`,
                    method: "PUT",
                    contentType: "application/json",
                    data: JSON.stringify(dynamicContentCommitData)
                });

                return createCommit;

            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan. Sluit a.u.b. deze module, open deze daarna opnieuw en probeer het vervolgens opnieuw. Of neem contact op als dat niet werkt.");
            }
        }

        

        async CreatePublishLog(templateId, environment, version) {
            try {
                
                
                const createCommit = await Wiser2.api({
                    url: `${this.base.settings.wiserApiRoot}VersionControl/${templateId}/publish/${environment}/${version}`,
                    method: "POST",
                    contentType: "application/json",
                });

  
            return createCommit;
            
            } catch(exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan. Sluit a.u.b. deze module, open deze daarna opnieuw en probeer het vervolgens opnieuw. Of neem contact op als dat niet werkt.");
            }
       }

        async getSelectedTemplateWithIdAndVersion(templateId, version) {
            try {

            
                const templateData = await Wiser2.api({
                    url: `${this.base.settings.wiserApiRoot}templates/${templateId}/${version}`,
                    method: "GET",
                    contentType: "application/json",
                });


                return templateData;
            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan. Sluit a.u.b. deze module, open deze daarna opnieuw en probeer het vervolgens opnieuw. Of neem contact op als dat niet werkt.");
            }
        }

        async getSelectedDynamicContent(dynamicContentId, version) {
            try {


                const templateData = await Wiser2.api({
                    url: `${this.base.settings.wiserApiRoot}VersionControl/dynamic-content/${dynamicContentId}/${version}`,
                    method: "GET",
                    contentType: "application/json",
                });


                return templateData;
            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan. Sluit a.u.b. deze module, open deze daarna opnieuw en probeer het vervolgens opnieuw. Of neem contact op als dat niet werkt.");
            }
        }


        


        async GetDynamicContentWithLowerVersion(templateId, version) {
            try {

                const templateCommitData = {
                    TemplateId: templateId,
                    Version: version
                }

                const templateData = await Wiser2.api({
                    url: `${this.base.settings.wiserApiRoot}VersionControl/dynamic-content/lower-versions/${templateId}/${version}`,
                    method: "GET",
                    contentType: "application/json",
                    data: JSON.stringify(templateCommitData)
                });


                return templateData;
            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan. Sluit a.u.b. deze module, open deze daarna opnieuw en probeer het vervolgens opnieuw. Of neem contact op als dat niet werkt.");
            }
        }


        getSelectedId() {
            var parent = document.querySelector(".k-state-selected");

            if (parent == null) {
                return null
            }

            var child = parent.firstChild.textContent;
            return child;
        }

        getSelectedDynamicContentId(gridIdName) {

            //get dynamic content grid en haal hiervan de selected op
            var dynamicContentTable = document.querySelector(gridIdName);

            var parent1 = dynamicContentTable.querySelector(".k-state-selected");
           
           
            historyDynamicContentGridId
           
            if (parent1 == null) {
                console.log("test");
                return null
            }

            var child = parent1.firstChild.textContent;
            return child;
        } 

        getDynamicContentVersion() {
            var dynamicContentTable = document.querySelector("#dynamicContentGrid");

            var parent = dynamicContentTable.querySelector(".k-state-selected").querySelector('[data-field="version"]').innerHTML;


            if (parent == null) {
                return null
            }

            return parent;
        }

        getSlectedDeployment() {

            //Gets data vrom checkboxes
            var commitToTestCheckbox = document.getElementById("commitTest");
            var commitToAcceptanceCheckbox = document.getElementById("commitAcceptatie");
            var commitToLiveCheckbox = document.getElementById("commitLive");

            const deploymentOptions = [];
            if (commitToTestCheckbox.checked) {
                deploymentOptions[0] = commitToTestCheckbox.value;
            }

            if (commitToAcceptanceCheckbox.checked) {
                deploymentOptions[1] = commitToAcceptanceCheckbox.value;
            }

            if (commitToLiveCheckbox.checked) {
                deploymentOptions[2] = commitToLiveCheckbox.value;
            }

            return deploymentOptions;
        }

        getCommitMessage() {
            var commitMessage = document.getElementById("description-textbox").value;
            return commitMessage;
        }


        /**
         * Refreshes the current view.
         * If it's a gridView, it refreshed the grid.
         * If it's the normal view, it refreshes the tree view and currently opened item.
         * @param {any} event The click event of the button.
         */
        onMainRefreshButtonClick(event) {
            event.preventDefault();
            if (this.settings.gridViewMode) {
                this.grids.mainGridForceRecount = true;
                this.grids.mainGrid.dataSource.read();

                if (this.grids.informationBlockIframe && this.grids.informationBlockIframe[0] && this.grids.informationBlockIframe[0].contentWindow) {
                    this.grids.informationBlockIframe[0].contentWindow.location.reload();
                }
            } else {
                if (!this.settings.iframeMode) {
                    this.mainTreeView.dataSource.read();
                }
                if (this.selectedItem || this.settings.initialItemId) {
                    const previouslySelectedTab = this.mainTabStrip.select().index();
                    this.loadItem(this.settings.iframeMode ? this.settings.initialItemId : this.selectedItem.id, previouslySelectedTab, this.settings.iframeMode ? this.settings.entityType : this.selectedItem.entityType);
                }
            }
        }


        /**
         * Load all Kendo scripts of components that are used in a script template that will be used in an eval() function.
         * @param {string} scriptTemplate The script template that is going to be loaded in an eval().
         */
        async loadKendoScripts(scriptTemplate) {
            if (scriptTemplate.indexOf("kendoDateTimePicker") > -1) {
                await require("@progress/kendo-ui/js/kendo.datetimepicker.js");
            }
            if (scriptTemplate.indexOf("kendoDatePicker") > -1) {
                await require("@progress/kendo-ui/js/kendo.datepicker.js");
            }
            if (scriptTemplate.indexOf("kendoTimePicker") > -1) {
                await require("@progress/kendo-ui/js/kendo.timepicker.js");
            }
            if (scriptTemplate.indexOf("kendoChart") > -1) {
                await require("@progress/kendo-ui/js/dataviz/chart/chart.js");
            }
            if (scriptTemplate.indexOf("kendoColorPicker") > -1) {
                await require("@progress/kendo-ui/js/kendo.colorpicker.js");
            }
            if (scriptTemplate.indexOf("kendoDropDownList") > -1) {
                await require("@progress/kendo-ui/js/kendo.dropdownlist.js");
            }
            if (scriptTemplate.indexOf("kendoComboBox") > -1) {
                await require("@progress/kendo-ui/js/kendo.combobox.js");
            }
            if (scriptTemplate.indexOf("kendoUpload") > -1) {
                await require("@progress/kendo-ui/js/kendo.upload.js");
            }
            if (scriptTemplate.indexOf("kendoEditor") > -1) {
                await require("@progress/kendo-ui/js/kendo.editor.js");
            }
            if (scriptTemplate.indexOf("kendoNumericTextBox") > -1) {
                await require("@progress/kendo-ui/js/kendo.numerictextbox.js");
            }
            if (scriptTemplate.indexOf("kendoMultiSelect") > -1) {
                await require("@progress/kendo-ui/js/kendo.multiselect.js");
            }
            if (scriptTemplate.indexOf("kendoScheduler") > -1) {
                await require("@progress/kendo-ui/js/kendo.scheduler.js");
            }
            if (scriptTemplate.indexOf("kendoTimeline") > -1) {
                await require("@progress/kendo-ui/js/kendo.timeline.js");
            }
        }

        /**
         * Shows or hides the main (full screen) loader.
         * @param {boolean} show True to show the loader, false to hide it.
         */
        toggleMainLoader(show) {
            this.mainLoader.toggleClass("loading", show);
        }

        /**
         * Parses a bitwise environments value to an array of text values.
         * @param {any} publishedEnvironment
         */
        parseEnvironments(publishedEnvironment) {
            const environmentLabel = [];
            if (publishedEnvironment === this.environmentsEnum.hidden) {
                environmentLabel.push("verborgen");
                return environmentLabel;
            }

            if ((publishedEnvironment & this.environmentsEnum.development) > 0) {
                environmentLabel.push("ontwikkeling");
            }
            if ((publishedEnvironment & this.environmentsEnum.test) > 0) {
                environmentLabel.push("test");
            }
            if ((publishedEnvironment & this.environmentsEnum.acceptance) > 0) {
                environmentLabel.push("acceptatie");
            }
            if ((publishedEnvironment & this.environmentsEnum.live) > 0) {
                environmentLabel.push("live");
            }
            return environmentLabel;
        }

        /**
         * Function for event of validator, it will open the tab sheet that contains the first field with an error.
         * @param {any} tabStrip The tab strip that we're validating.
         * @param {any} event The validate event.
         */
        onValidateForm(tabStrip, event) {
            if (event.valid) {
                return;
            }

            // Switch to the tab sheet that contains the first error.
            const fieldName = Object.keys(event.sender._errors)[0];
            const tabSheet = tabStrip.element.find(`[name=${fieldName}]`).closest("div[role=tabpanel]");
            tabStrip.select(tabSheet.index() - 1); // -1 because the first element is the <ul> with tab names, which we don't want to count.
        }

        /**
         * Event that gets fired when the users opens the context menu.
         * @param {any} event The context open event.
         */
        async onContextMenuOpen(event) {
            try {
                const nodeId = this.mainTreeView.dataItem(event.target).id;
                let contextMenu = await Wiser2.api({ url: `${this.base.settings.serviceRoot}/GET_CONTEXT_MENU?moduleId=${encodeURIComponent(this.base.settings.moduleId)}&itemId=${encodeURIComponent(nodeId)}` });
                //TODO: DIT MOET ANDERS MAAR KOMT ZO VERKEERD UIT WISER
                contextMenu = JSON.parse(JSON.stringify(contextMenu).replace(/"attr":\[/g, '"attr":').replace(/\}\]\},/g, "}},").replace("}]}]", "}}]"));
                this.mainTreeViewContextMenu.setOptions({
                    dataSource: contextMenu
                });
            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan tijdens het ophalen van het rechtermuismenu van dit item. Probeer het a.u.b. nogmaals of neem contact op met ons.");
            }
        }

        /**
         * This event gets fired when the user clicks an option in the context menu.
         * @param {any} event The click event.
         */
        
        /**
         * Refreshes the current view.
         * If it's a gridView, it refreshed the grid.
         * If it's the normal view, it refreshes the tree view and currently opened item.
         * @param {any} event The click event of the button.
         */
        

        

        /**
         * Event that gets called once a notification is being shown.
         */
        onShowNotification(event) {
            event.element.parent().css("width", "100%").css("height", "auto");
        }

        /**
         * Handles the main tree view data bound event.
         * @param {any} event The Kendo data bound event.
         */
        onTreeViewDataBound(event) {
            (event.node || event.sender.element).find("li").each((index, element) => {
                const dataItem = event.sender.dataItem(element);
                if (!dataItem.nodeCssClass) {
                    return;
                }

                $(element).addClass(dataItem.nodeCssClass);
            });

            this.base.toggleMainLoader(false);
        }

        

        /**
         * Event for when an item in a kendoTreeView gets collapsed.
         * @param {any} event The collapsed event of a kendoTreeView.
         */
        onTreeViewCollapseItem(event) {
            const dataItem = event.sender.dataItem(event.node);
            dataItem.spriteCssClass = dataItem.collapsedSpriteCssClass;

            // Changing the text causes kendo to actually update the icon. If we don't change the test, the icon will not change.
            event.sender.text(event.node, event.sender.text(event.node).trim());
        }

        /**
         * Event for when an item in a kendoTreeView gets expanded.
         * @param {any} event The expanded event of a kendoTreeView.
         */
        onTreeViewExpandItem(event) {
            const dataItem = event.sender.dataItem(event.node);
            dataItem.spriteCssClass = dataItem.expandedSpriteCssClass || dataItem.collapsedSpriteCssClass;

            // Changing the text causes kendo to actually update the icon. If we don't change the test, the icon will not change.
            event.sender.text(event.node, event.sender.text(event.node) + " ");
        }

        /**
         * Event for when an item in a kendoTreeView gets dragged.
         * @param {any} event The drop item event of a kendoTreeView.
         */
        onTreeViewDragItem(event) {
            // Scroll left pane up/down when moving items.
            const leftPane = $("#left-pane");
            const topOfTreeView = leftPane.offset().top;
            const bottomOfTreeView = topOfTreeView + leftPane.height();
            const dropTargetOffset = $(event.dropTarget).offset().top;
            if (dropTargetOffset > 0 && dropTargetOffset > bottomOfTreeView - 50) {
                leftPane.scrollTop(leftPane.scrollTop() + $(event.dropTarget).height() + 50);
            } else if (dropTargetOffset > 0 && dropTargetOffset - 10 < topOfTreeView) {
                leftPane.scrollTop(leftPane.scrollTop() - $(event.dropTarget).height() - 50);
            }

            if (event.statusClass === "i-cancel") {
                // Tree view already denies this operation
                return;
            }

            const dropTarget = $(event.dropTarget);
            let destinationNode = dropTarget.closest("li.k-item");
            if (dropTarget.hasClass("k-mid")) {
                // If the dropTarget is an element with class k-mid we need to go higher, because those elements are located inside an li.k-item instead of after/before them.
                destinationNode = destinationNode.parentsUntil("li.k-item");
            }
            if (event.statusClass === "i-insert-down" || (event.statusClass === "i-insert-middle" && (dropTarget.hasClass("k-bot") || dropTarget.hasClass("k-in")))) {
                // If the statusClass is i-insert-down, it means we are adding the item below the destination, so we need to check it's parent.
                destinationNode = destinationNode.parentsUntil("li.k-item");
            }

            const sourceDataItem = event.sender.dataItem(event.sourceNode) || {};
            const destinationDataItem = event.sender.dataItem(destinationNode) || {};

            if ((destinationDataItem.acceptedChildTypes || "").toLowerCase().split(",").indexOf(sourceDataItem.entityType.toLowerCase()) === -1) {
                // Tell the kendo tree view to deny the drag to this item, if the current item is of a type that is not allowed to be linked to the destination.
                event.setStatusClass("k-i-cancel");
            }
        }

        /**
         * Event for when an item in a kendoTreeView gets dropped.
         * @param {any} event The drop item event of a kendoTreeView.
         */
        async onTreeViewDropItem(event) {
            if (!event.valid) {
                return;
            }

            try {
                const sourceDataItem = event.sender.dataItem(event.sourceNode);
                const destinationDataItem = event.sender.dataItem(event.destinationNode);

                const moveItemResult = await Wiser2.api({
                    url: `${this.base.settings.wiserApiRoot}items/${encodeURIComponent(sourceDataItem.id)}/move/${encodeURIComponent(destinationDataItem.id)}`,
                    method: "PUT",
                    contentType: "application/json",
                    data: JSON.stringify({
                        position: event.dropPosition,
                        encryptedSourceParentId: sourceDataItem.destinationItemId,
                        encryptedDestinationParentId: destinationDataItem.destinationItemId,
                        sourceEntityType: sourceDataItem.entityType,
                        destinationEntityType: destinationDataItem.entityType,
                        moduleId: this.base.settings.moduleId
                    })
                });
            } catch (exception) {
                console.error(exception);
                kendo.alert(`Er is iets fout gegaan met het verplaatsen van dit item. De fout was:<br>${exception.responseText || exception.statusText}`);
                event.setValid(false);
            }
        }



        /**
         * Load the basic overview grid. This is a grid in a separate tab that only contains this grid.
         * This grid shows all items linked to the current item.
         * @param {any} itemId The ID of the current item.
         * @param {any} overviewGridElement The DOM element of the overview grid.
         */
        async loadOverviewGrid(itemId, overviewGridElement) {
            console.log("L");
            try {
                if (!itemId || overviewGridElement.length <= 0) {
                    return;
                }

                const customColumns = await Wiser2.api({ url: `${this.settings.serviceRoot}/GET_COLUMNS_FOR_TABLE?itemId=${encodeURIComponent(itemId)}` });
                const model = {
                    id: "id",
                    fields: {
                        id: {
                            type: "number"
                        },
                        publishedEnvironment: {
                            type: "string"
                        },
                        title: {
                            type: "string"
                        },
                        entityType: {
                            type: "string"
                        },
                        property_: {
                            type: "object"
                        }
                    }
                };

                const columns = [
                    {
                        field: "id",
                        title: "Id",
                        width: 55
                    },
                    {
                        field: "entityType",
                        title: "Type",
                        width: 100
                    },
                    {
                        template: "<ins title='#: publishedEnvironment #' class='icon-#: publishedEnvironment #'></ins>",
                        field: "publishedEnvironment",
                        title: "Gepubliceerde omgeving",
                        width: 50
                    },
                    {
                        field: "title",
                        title: "Naam"
                    }
                ];

                if (customColumns && customColumns.length > 0) {
                    for (let column of customColumns) {
                        if (column.field) {
                            column.field = column.field.toLowerCase();
                        }

                        columns.push(column);
                    }
                }

                let grid;
                columns.push({
                    title: "&nbsp;",
                    width: 80,
                    command: [{
                        name: "openDetails",
                        iconClass: "k-icon k-i-hyperlink-open",
                        text: "",
                        click: (event) => { this.base.grids.onShowDetailsClick(event, grid, {}); }
                    }]
                });

                if (overviewGridElement.data("kendoGrid")) {
                    overviewGridElement.data("kendoGrid").destroy();
                    overviewGridElement.empty();
                }

                grid = overviewGridElement.kendoGrid({
                    dataSource: {
                        transport: {
                            read: async (options) => {
                                try {
                                    const results = await Wiser2.api({ url: `${this.settings.serviceRoot}/GET_DATA_FOR_TABLE?itemId=${encodeURIComponent(itemId)}` });
                                    if (!results) {
                                        options.success(results);
                                        return;
                                    }

                                    for (let i = 0; i < results.length; i++) {
                                        const row = results[i];
                                        if (!row.property_) {
                                            row.property_ = {};
                                        }
                                    }

                                    options.success(results);
                                } catch (exception) {
                                    console.error(exception);
                                    options.error(exception);
                                }
                            }
                        },
                        pageSize: 100,
                        schema: {
                            model: model
                        }
                    },
                    toolbar: ["excel"],
                    excel: {
                        fileName: "Export overzicht " + this.selectedItem.name + ".xlsx",
                        //proxyURL: "https://demos.telerik.com/kendo-ui/service/export",
                        filterable: true
                    },
                    columns: columns,
                    pageable: true,
                    sortable: true,
                    resizable: true,
                    scrollable: true,
                    height: "600px",
                    filterable: {
                        extra: false,
                        messages: {
                            isTrue: "<span>Ja</span>",
                            isFalse: "<span>Nee</span>"
                        }
                    },
                    filterMenuInit: this.base.grids.onFilterMenuInit.bind(this),
                    filterMenuOpen: this.base.grids.onFilterMenuOpen.bind(this)
                }).data("kendoGrid");

                grid.thead.kendoTooltip({
                    filter: "th",
                    content: (event) => {
                        const target = event.target; // element for which the tooltip is shown
                        return $(target).text();
                    }
                });

                grid.element.on("dblclick", "tbody tr[data-uid] td", (event) => { this.base.grids.onShowDetailsClick(event, grid, {}); });
            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan tijdens het laden van het overzicht. Probeer het a.u.b. nogmaals door het item te sluiten en opnieuw te openen, of neem contact op met ons.");
            }
        }

        

        /**
         * The click event for the undelete button.
         * @param {any} event The click event.
         * @param {string} encryptedItemId The encrypted ID of the item to undelete.
         */
        async onUndeleteItemClick(event, encryptedItemId) {
            event.preventDefault();

            await Wiser2.showConfirmDialog(`Weet u zeker dat u het verwijderen ongedaan wilt maken voor '${this.base.selectedItem.title}'?`);

            const process = `undeleteItem_${Date.now()}`;
            window.processing.addProcess(process);

            const popupWindowContainer = $(event.currentTarget).closest(".popup-container");

            try {
                popupWindowContainer.find(".popup-loader").addClass("loading");
                popupWindowContainer.data("saving", true);

                let entityType = popupWindowContainer.data("entityTypeDetails");

                if (Wiser2.validateArray(entityType)) {
                    entityType = entityType[0];
                }

                await this.base.undeleteItem(encryptedItemId, entityType);

                const kendoWindow = popupWindowContainer.data("kendoWindow");
                if (kendoWindow) {
                    const data = kendoWindow.element.data();
                    if (data.senderGrid) {
                        data.senderGrid.dataSource.read();
                    }
                }

                $(event.currentTarget).closest(".entity-container").find(".k-i-verversen").click();
            } catch (exception) {
                console.error(exception);
                popupWindowContainer.find(".popup-loader").removeClass("loading");

                if (exception.status === 409) {
                    const message = exception.responseText || "Het is niet meer mogelijk om het verwijderen ongedaan te maken.";
                    kendo.alert(message);
                } else {
                    kendo.alert("Er is iets fout gegaan tijdens het verwijderen ongedaan maken van dit item. Probeer het a.u.b. nogmaals of neem contact op met ons.");
                }
            }

            window.processing.removeProcess(process);
            popupWindowContainer.find(".popup-loader").removeClass("loading");
            popupWindowContainer.data("saving", false);
        }



        /**
         * Links two items together.
         * @param {string} sourceId The ID of the item that you want to link.
         * @param {string} destinationId The ID of the item that you want to link to.
         * @param {number} linkTypeNumber The link type number.
         * @returns {any} A promise with the result of the AJAX call.
         */
        async addItemLink(sourceId, destinationId, linkTypeNumber) {
            return Wiser2.api({ url: `${this.settings.serviceRoot}/ADD_LINK?source=${encodeURIComponent(sourceId)}&destination=${encodeURIComponent(destinationId)}&linkTypeNumber=${encodeURIComponent(linkTypeNumber)}` });
        }

        /**
         * Update the link of an item, to link/move that item to another item.
         * @param {number} linkId The ID of the current link that you want to update.
         * @param {number} newDestinationId The ID of the item where you want to move the item to.
         * @returns {any} A promise with the result of the AJAX call.
         */
        async updateItemLink(linkId, newDestinationId) {
            return Wiser2.api({ url: `${this.settings.serviceRoot}/UPDATE_LINK?linkId=${encodeURIComponent(linkId)}&destinationId=${encodeURIComponent(newDestinationId)}` });
        }

        /**
         * Removes a link between two items.
         * @param {string} sourceId The ID of the item that you want to link.
         * @param {string} destinationId The ID of the item that you want to link to.
         * @param {number} linkTypeNumber The link type number.
         * @returns {Promise} A promise with the result of the AJAX call.
         */
        async removeItemLink(sourceId, destinationId, linkTypeNumber) {
            return Wiser2.api({ url: `${this.settings.serviceRoot}/REMOVE_LINK?source=${encodeURIComponent(sourceId)}&destination=${encodeURIComponent(destinationId)}&linkTypeNumber=${encodeURIComponent(linkTypeNumber)}` });
        }

        /**
         * Creates a new item in the database and executes any workflow for creating an item.
         * @param {string} entityType The type of item to create.
         * @param {string} parentId The (encrypted) ID of the parent to add the new item to.
         * @param {string} name Optional: The name of the new item.
         * @param {number} linkTypeNumber Optional: The type number of the link between the new item and it's parent.
         * @param {any} data Optional: The data to save with the new item.
         * @returns {Object<string, any>} An object with the properties 'itemId', 'icon' and 'workflowResult'.
         * @param {number} moduleId Optional: The id of the module in which the item should be created.
         */
        
        /**
         * Marks an item as deleted.
         * @param {string} encryptedItemId The encrypted item ID.
         * @param {string} entityType The entity type of the item to delete. This is required for workflows.
         * @returns {Promise} A promise with the result of the AJAX call.
         */
        async deleteItem(encryptedItemId, entityType) {
            console.warn("deleteItem in dynamicItems.js called");

            try {
                const apiActionId = await this.getApiAction("before_delete", entityType);
                if (apiActionId) {
                    await Wiser2.doApiCall(this.settings, apiActionId, { encryptedId: encryptedItemId });
                }
            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan tijdens het uitvoeren (of opzoeken) van de actie 'api_before_delete'. Hierdoor is het betreffende item ook niet uit Wiser verwijderd. Probeer het a.u.b. nogmaals of neem contact op met ons.");
                return new Promise((resolve, reject) => {
                    reject(exception);
                });
            }

            return Wiser2.api({
                url: `${this.base.settings.wiserApiRoot}items/${encodeURIComponent(encryptedItemId)}?entityType=${entityType || ""}`,
                method: "DELETE",
                contentType: "application/json",
                dataType: "JSON"
            });
        }

        /**
         * Marks an item as deleted.
         * @param {string} encryptedItemId The encrypted item ID.
         * @param {string} entityType The entity type of the item to undelete.
         * @returns {Promise} A promise with the result of the AJAX call.
         */
        async undeleteItem(encryptedItemId, entityType) {
            console.warn("undeleteItem in dynamicItems.js called");

            return Wiser2.api({
                url: `${this.base.settings.wiserApiRoot}items/${encodeURIComponent(encryptedItemId)}?undelete=true&entityType=${entityType || ""}`,
                method: "DELETE",
                contentType: "application/json",
                dataType: "JSON"
            });
        }

        /**
         * Duplicates an item (including values of fields, excluding linked items).
         * @param {string} itemId The (encrypted) ID of the item to get the HTML for.
         * @param {string} parentId The (encrypted) ID of the parent item, so that the duplicated item will be linked to the same parent.
         * @param {string} entityType Optional: The entity type of the item to duplicate, so that the API can use the correct table and settings.
         * @param {string} parentEntityType Optional: The entity type of the parent of item to duplicate, so that the API can use the correct table and settings.
         * @returns {Promise} The details about the newly created item.
         */
        async duplicateItem(itemId, parentId, entityType = null, parentEntityType = null) {
            try {
                const entityTypeQueryString = !entityType ? "" : `?entityType=${encodeURIComponent(entityType)}`;
                const parentEntityTypeQueryString = !parentEntityType ? "" : `${!entityType ? "?" : "&"}parentEntityType=${encodeURIComponent(parentEntityType)}`;
                const createItemResult = await Wiser2.api({
                    method: "POST",
                    url: `${this.base.settings.wiserApiRoot}items/${encodeURIComponent(itemId)}/duplicate/${encodeURIComponent(parentId)}${entityTypeQueryString}${parentEntityTypeQueryString}`,
                    contentType: "application/json",
                    dataType: "JSON"
                });
                const workflowResult = await Wiser2.api({
                    method: "POST",
                    url: `${this.settings.wiserApiRoot}items/${encodeURIComponent(createItemResult.newItemId)}/workflow?isNewItem=true`,
                    contentType: "application/json",
                    dataType: "JSON"
                });
                return {
                    itemId: createItemResult.newItemId,
                    itemIdPlain: createItemResult.newItemIdPlain,
                    linkId: createItemResult.newLinkId,
                    icon: createItemResult.icon,
                    workflowResult: workflowResult,
                    title: createItemResult.title
                };
            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan met het dupliceren van het item. Neem a.u.b. contact op met ons.");
                return {};
            }
        }

        /**
         * Marks an item as deleted.
         * @param {string} encryptedItemId The encrypted item ID.
         * @param {int} newEnvironments The environments to copy this item to.
         * @returns {Promise} A promise with the result of the AJAX call.
         */
        async copyToEnvironment(encryptedItemId, newEnvironments) {
            return Wiser2.api({
                url: `${this.base.settings.wiserApiRoot}items/${encodeURIComponent(encryptedItemId)}/copy-to-environment/${newEnvironments}`,
                method: "POST",
                contentType: "application/json",
                dataType: "JSON"
            });
        }

        /**
         * Gets the title of an item
         * @param {string} itemId The encrypted item ID.
         * @returns {Promise} A promise with the results.
         */
        async getTitle(itemId) {
            return Wiser2.api({ url: `${this.settings.serviceRoot}/GET_TITLE?itemId=${encodeURIComponent(itemId)}` });
        }

        /**
         * Gets the HTML for an item. This contains all fields and the javascript for those fields.
         * @param {string} itemId The (encrypted) ID of the item to get the HTML for.
         * @param {string} entityType The entity type of the item to get the HTML for.
         * @param {string} propertyIdSuffix Optional: The suffix for property IDs, this is required when opening items in a popup, so that the fields always have unique ID, even if they already exist in the main tab sheet.
         * @param {number} linkId Optional: The ID of the link between this item and another item. If you're opening this item via a specific link, you should enter the ID of that link, because it's possible to have fields/properties on a link instead of an item.
         * @returns {Promise} A promise with the results.
         */
        async getItemHtml(itemId, entityType, propertyIdSuffix, linkId) {
            let url = `${this.settings.wiserApiRoot}items/${encodeURIComponent(itemId)}?entityType=${encodeURIComponent(entityType)}&encryptedModuleId=${encodeURIComponent(this.base.settings.encryptedModuleId)}`;
            if (propertyIdSuffix) {
                url += `&propertyIdSuffix=${encodeURIComponent(propertyIdSuffix)}`;
            }
            if (linkId) {
                url += `&itemLinkId=${encodeURIComponent(linkId)}`;
            }
            return Wiser2.api({ url: url });
        }

        /**
         * Get all properties / fields from a single item.
         * @param {any} itemId The ID of the item to get the details of.
         * @returns {Promise} A promise, which will return an array with 1 item. That item will contain it's basic properties and a property called "property_" which contains an object with all fields and their values.
         */
        async getItemDetails(itemId) {
            return Wiser2.api({ url: `${this.settings.serviceRoot}/GET_ITEM_DETAILS?itemId=${encodeURIComponent(itemId)}` });
        }

        /**
         * Get all properties / fields from a single item.
         * @param {any} itemId The ID of the item to get the details of.
         * @returns {Promise} A promise, which will return an array with 1 item. That item will contain it's basic properties and a property called "property_" which contains an object with all fields and their values.
         */
        async getEntityBlock(itemId) {
            return Wiser2.api({ url: `${this.settings.wiserApiRoot}items/${encodeURIComponent(itemId)}/block/` });
        }

        /**
         * Get the value of a specific field from a specific item.
         * @param {number} encryptedItemId The ID of the item.
         * @param {string} propertyName The name of the property / field.
         * @returns {Promise} A promise, which will return an array with 1 item. That item will contain information about the property / field, including it's value.
         */
        async getItemValue(encryptedItemId, propertyName) {
            return Wiser2.api({ url: `${this.settings.serviceRoot}/GET_ITEM_VALUE?itemId=${encodeURIComponent(encryptedItemId)}&propertyName=${encodeURIComponent(propertyName)}` });
        }

        /**
         * Get all meta data from a single item.
         * @param {string} itemId The ID of the item to get the meta data of.
         * @param {string} itemId The entity type of the item to get the meta data of.
         * @returns {Promise} A promise, which will return an array with 1 item. That item will contain it's basic properties and a property called "property_" which contains an object with all fields and their values.
         */
        async getItemMetaData(itemId, entityType) {
            const entityTypeUrlPart = entityType ? `?entityType=${encodeURIComponent(entityType)}` : "";
            return Wiser2.api({ url: `${this.settings.wiserApiRoot}items/${encodeURIComponent(itemId)}/meta${entityTypeUrlPart}` });
        }

        /**
         * Get all versions of an item for different environments.
         * @param {any} mainItemId The ID of the main/original item.
         * @returns {Promise} A promise, which will return an array with the results.
         */
        async getItemEnvironments(mainItemId) {
            return Wiser2.api({ url: `${this.settings.serviceRoot}/GET_ITEM_ENVIRONMENTS?mainItemId=${encodeURIComponent(mainItemId)}` });
        }

        /**
         * Gets all available entity types that can be added as a child to the given parent.
         * @param {string} parentId The (encrypted) ID of the parent to get the available entity types of.
         * @return {any} An array with all the available entity types.
         */
        async getAvailableEntityTypes(parentId) {
            const names = await Wiser2.api({ url: `${this.base.settings.wiserApiRoot}entity-types/${encodeURIComponent(this.settings.moduleId)}?parentId=${encodeURIComponent(parentId)}` });
            return names.map(name => { return { name: name }; });
        }

        /**
         * Get the details for an entity type.
         * @param {string} name The name of the entity type.
         * @param {number} moduleId The ID of the module (different modules can have entity types with the same name).
         * @returns {Promise} A promise with the results.
         */
        async getEntityType(name, moduleId) {
            const sessionStorageKey = `wiserEntityTypeInfo${name}`;
            let result = sessionStorage.getItem(sessionStorageKey);
            if (result) {
                return JSON.parse(result);
            }

            result = await Wiser2.api({ url: `${this.settings.serviceRoot}/GET_ENTITY_TYPE?entityType=${encodeURIComponent(name)}&moduleId=${encodeURIComponent(moduleId || "")}` });
            sessionStorage.setItem(sessionStorageKey, JSON.stringify(result));
            return result;
        }

        /**
         * Gets a certain API action for a certain entity type. This will return the ID that can be used for executing an API action.
         * @param {string} actionType The type of action to get. Possible values: "after_insert", "after_update", "before_update" and "before_delete".
         * @param {string} entityType The name of the entity type to get the action for.
         * @returns {number} The ID of the API action, or 0 if there is no action set.
         */
        async getApiAction(actionType, entityType) {
            const result = await Wiser2.api({ url: `${this.settings.serviceRoot}/GET_API_ACTION?entityType=${encodeURIComponent(entityType)}&actionType=${encodeURIComponent(actionType)}` });
            return !result || !result.length ? 0 : result[0].apiConnectionId || 0;
        }


         
        async loadTemplate(id) {

            /*const gridDataResult = await Wiser2.api({
                url: `${this.base.settings.wiserApiRoot}items/${encodeURIComponent(parentId)}/entity-grids/${encodeURIComponent(entityType)}?moduleId=${this.base.settings.moduleId}&propertyId=${propertyId}${gridTypeQueryString}`,
                method: "POST",
                contentType: "application/json",
                data: JSON.stringify(options)
            });
            console.log(gridDataResult);

            const template = await Wiser2.api({
                url: `${this.settings.wiserApiRoot}6001/overview-grid`,
                dataType: "json",
                method: "GET"
            });
            var temp = JSON.stringify(template);*/
        }

    }

    // Initialize the DynamicItems class and make one instance of it globally available.
    window.dynamicItems = new VersionControl(settings);
})(moduleSettings);

import { TrackJS } from "trackjs";
import { Modules, Wiser2 } from "../../Base/Scripts/Utils.js";
import "../../Base/Scripts/Processing.js";
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
            this.settings.serviceRoot = `${this.settings.wiserApiRoot}templates/get-and-execute-query`;
            this.settings.htmlEditorCssUrl = `${this.settings.wiserApiRoot}templates/css-for-html-editors?encryptedCustomerId=${encodeURIComponent(this.base.settings.customerId)}&isTest=${this.base.settings.isTestEnvironment}&encryptedUserId=${encodeURIComponent(this.base.settings.userId)}&username=${encodeURIComponent(this.base.settings.username)}&userType=${encodeURIComponent(this.base.settings.userType)}&subDomain=${encodeURIComponent(this.base.settings.subDomain)}`
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

            $("#mainScreenForm").submit(event => { event.preventDefault(); });

            $(".commitCheckbox").on("click", function () {

                var liveElement = document.getElementById("commitLive");
                var acceptanceElement = document.getElementById("commitAcceptatie");
                var testElement = document.getElementById("commitTest");

                if (liveElement.checked) {
                    acceptanceElement.checked = true;
                    testElement.checked = true;
                } else if (acceptanceElement.checked) {
                    testElement.checked = true;
                } else if (testElement.checked) {
                    if (liveElement.checked || acceptanceElement.checked) {
                        testElement.checked = true;
                    }
                }
            });

            $(".tablinks").click(async (event) => {

                var target = $(event.target);
                var tabValue = target[0].value

                var evt = event;
                var tabName = tabValue;

                
                    var i, tabcontent, tablinks;
                    tabcontent = document.getElementsByClassName("tabcontent");
                    for (i = 0; i < tabcontent.length; i++) {
                        tabcontent[i].style.display = "none";
                    }
                    tablinks = document.getElementsByClassName("tablinks");
                    for (i = 0; i < tablinks.length; i++) {
                        tablinks[i].className = tablinks[i].className.replace(" active", "");
                    }
                    document.getElementById(tabName).style.display = "block";

                    evt.currentTarget.className += " active";
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


            $(".environment_dynamic_content_history").kendoButton({
                click: this.HistoryDynamicContent.bind(this),
                icon: "save"
            });


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

           

            // Some things should not be done if we're in iframe mode.
            if (this.settings.iframeMode || this.settings.gridViewMode) {
                return;
            }

        }


        async Deploy(event) {

            var envioronmentbuttonValue = event.event.target.value;
            var commitGrid = document.querySelector("#deploygrid");
            var commits = commitGrid.querySelectorAll(".k-state-selected");

            for (const [key, value] of Object.entries(commits)) {

                var commitId = value.querySelector('[data-field="id"]').innerHTML;
                var templates = await this.template.GetTemplatesFromCommit(commitId);
                var dynamicContent = await this.GetDynamicContentFromCommit(commitId);
                       
                for (const [key, value] of Object.entries(templates)) {

                    await this.template.PublishTemplate(value["templateId"], envioronmentbuttonValue, value["version"]);
                }

                for (const [key, value] of Object.entries(dynamicContent)) {

                    await this.PublishDynamicContent(value["dynamicContentId"], envioronmentbuttonValue, value["version"]);
                }
            }
            document.location.reload();
        }
        


        async HistoryDynamicContent(event) {
            const templateTable = document.querySelector("#historyGridId");
            const templateSelected = templateTable.querySelectorAll(".k-state-selected");

            var DynamicContentList = [];
            var DynamicContentListResults = [];

            const dynamicContentTable = document.querySelector("#historyDynamicContentGridId");
            const dynamicContentSelected = dynamicContentTable.querySelectorAll(".k-state-selected");

            var envioronmentbuttonValue = event.event.target.value;

            for (const [key, value] of Object.entries(templateSelected)) {

                var templateVersionId = value.querySelector('[data-field="template_id"]').innerHTML;
                var version = value.querySelector('[data-field="version"]').innerHTML;


                await this.template.PublishTemplate(templateVersionId, envioronmentbuttonValue, version);

            }

            for (const [key, value] of Object.entries(dynamicContentSelected)) {

                var dynamicContentId = value.querySelector('[data-field="content_id"]').innerHTML;
                var version = value.querySelector('[data-field="version"]').innerHTML;
               

                await this.PublishDynamicContent(dynamicContentId, envioronmentbuttonValue, version);
            }

            document.location.reload();

        }


        async onCommitDynamicContent(event) {
            var dynamicContentId = this.getSelectedDynamicContentId("#dynamicContentGrid");
            const publishEnviornment = this.getSlectedDeployment();
            var commitMessage = this.getCommitMessage();
            var version = this.getDynamicContentVersion();
            var dynamicContent = await this.getSelectedDynamicContent(dynamicContentId, version);
            var contentId = dynamicContent['componentModeId'];

           

            await this.commit.CreateNewCommit(commitMessage);

           

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
            
            const templateTable = document.querySelector("#gridView");
            const templateSelected = templateTable.querySelectorAll(".k-state-selected");

            const dynamicContentTable = document.querySelector("#dynamicContentGrid");
            const dynamicContentSelected = dynamicContentTable.querySelectorAll(".k-state-selected");

            const publishEnviornment = this.getSlectedDeployment();

           
            
            var commitMessage = this.getCommitMessage();
            await this.commit.CreateNewCommit(commitMessage);
            var createdCommit = await this.commit.GetCommitWithId()
            var commitId = createdCommit['id'];
        

            for (const [key, value] of Object.entries(templateSelected)) {

                var templateVersionId = value.querySelector('[data-field="template_id"]').innerHTML;
                var version = value.querySelector('[data-field="version"]').innerHTML;
                
                var lowerVersionTemplates = await this.template.GetTemplatesWithLowerVersion(templateVersionId, version);
              

                for (const [key, value] of Object.entries(lowerVersionTemplates)) {
                    await this.commit.PutTemplateCommit(commitId, value, key, publishEnviornment);
                }

                await this.commit.PutTemplateCommit(commitId, templateVersionId, version, publishEnviornment);
                await this.template.PublishTemplate(templateVersionId, publishEnviornment, version);

            }
          
            for (const [key, value] of Object.entries(dynamicContentSelected)) {

                var dynamicContentId = value.querySelector('[data-field="content_id"]').innerHTML;
                var version = value.querySelector('[data-field="version"]').innerHTML;

                var lowerVersionDynamicContent = await this.GetDynamicContentWithLowerVersion(dynamicContentId, version);

                for (const [key, value] of Object.entries(lowerVersionDynamicContent)) {
                    await this.PutDynamicContentCommit(commitId, value, key, publishEnviornment);
                }

                await this.PutDynamicContentCommit(commitId, dynamicContentId, version, publishEnviornment);
                await this.PublishDynamicContent(dynamicContentId, publishEnviornment, version);
            }
            document.location.reload();
        }

        async onCommitSelectedAndRelatedDynamicConent() {
            
            const templateTable = document.querySelector("#gridView");
            const templateSelected = templateTable.querySelectorAll(".k-state-selected");

            const dynamicContentTable = document.querySelector("#dynamicContentGrid");
            const dynamicContentSelected = dynamicContentTable.querySelectorAll(".k-state-selected");

            var DynamicContentList = [];

            for (const [key, value] of Object.entries(templateSelected)) {

                var templateVersionId = value.querySelector('[data-field="template_id"]').innerHTML;

                var DynamicContentListResults = await this.DynamicContentInTemplates(templateVersionId);

                DynamicContentList.push(DynamicContentListResults);

            }

            //selected Environment choices
            const publishEnviornment = this.getSlectedDeployment();

            //Commit message
            //return this commit message
            var commitMessage = this.getCommitMessage();
            if (commitMessage == "") {
                kendo.alert("Voer een commit message in");
                return;
            }

            await this.commit.CreateNewCommit(commitMessage);
            var createdCommit = await this.commit.GetCommitWithId()
            var commitId = createdCommit['id'];

            for (const [key, value] of Object.entries(templateSelected)) {
                var templateVersionId = value.querySelector('[data-field="template_id"]').innerHTML;
                var version = value.querySelector('[data-field="version"]').innerHTML;

                //gets lower version of the template
                var lowerVersionTemplates = await this.template.GetTemplatesWithLowerVersion(templateVersionId, version);

                for (const [key, value] of Object.entries(lowerVersionTemplates)) {
                    await this.commit.PutTemplateCommit(commitId, value, key, publishEnviornment);
                }


                await this.commit.PutTemplateCommit(commitId, templateVersionId, version, publishEnviornment);
                await this.template.PublishTemplate(templateVersionId, publishEnviornment, version);

            }
            
            for (const [key, value] of Object.entries(dynamicContentSelected)) {

                var dynamicContentId = value.querySelector('[data-field="content_id"]').innerHTML;
                var version = value.querySelector('[data-field="version"]').innerHTML;

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

                    var alreadySelected = false;

                    for (const [key, value2] of Object.entries(dynamicContentSelected)) {
                        var dynamicContentSelectedId = value2.querySelector('[data-field="content_id"]').innerHTML;
                        var dynamicContentSelectedVersion = value2.querySelector('[data-field="version"]').innerHTML;

                        if (dynamicContentInTemplateId == dynamicContentSelectedId && dynamicContentInTemplateVersion == dynamicContentSelectedVersion) {
                            alreadySelected = true;
                            break;
                        }
                    }
                    value["selected"] = alreadySelected;
                }
            }

            //DynamicContentList
            for (const [key, value] of Object.entries(DynamicContentList)) {

                var dynamicContent = value
                for (const [key, value] of Object.entries(dynamicContent)) {

                   
                    if (value["selected"]) {
                       
                    } else {

                        var dynamicContentId = value["id"];
                        var version = value["version"];
    
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
            document.location.reload();
        }


        
        //Dyalogue popup choices
        onCancel() {
        }

        async onCommitSelectedItemsWithDynamicContentItems() {
            this.onCommitSelectedAndRelatedDynamicConent();
         
        }


        async onCommitOnlySelectedItems() {
            this.onCommitSelectedOnly();
        }


        

        async onCommit(event) {

            const templateTable1 = document.querySelector("#gridView");
            const templateSelected1 = templateTable1.querySelectorAll(".k-state-selected");

            var DynamicContentList = [];
            var DynamicContentListResults = [];

            const dynamicContentTable = document.querySelector("#dynamicContentGrid");
            const dynamicContentSelected = dynamicContentTable.querySelectorAll(".k-state-selected");


            for (const [key, value] of Object.entries(templateSelected1)) {

                var templateVersionId = value.querySelector('[data-field="template_id"]').innerHTML;
                DynamicContentListResults = await this.DynamicContentInTemplates(templateVersionId);
                DynamicContentList.push(DynamicContentListResults);
            }

            const selectedChoices = this.getSlectedDeployment();

            if (selectedChoices == null || selectedChoices == "") {
                
                kendo.alert("Selecteer een environment");
                return;
            }
           
            var commitMessage = this.getCommitMessage();
            if (commitMessage == "") {
                kendo.alert("Voer een commit message in");
                return;
            }

            if (templateSelected1 == null && dynamicContentSelected == null) {
                kendo.alert("Selecteer een template.");
                return;
            }
            
            for (const [key, value] of Object.entries(DynamicContentListResults)) {
                var dynamicContentInTemplateId = value["id"];
                var dynamicContentInTemplateVersion = value["version"];

                var alreadySelected = false;

                for (const [key, value2] of Object.entries(dynamicContentSelected)) {
                    var dynamicContentSelectedId = value2.querySelector('[data-field="content_id"]').innerHTML;
                    var dynamicContentSelectedVersion = value2.querySelector('[data-field="version"]').innerHTML;

                    if (dynamicContentInTemplateId == dynamicContentSelectedId && dynamicContentInTemplateVersion == dynamicContentSelectedVersion) {
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
                this.onCommitSelectedOnly();
            } 
        }


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
                    url: `${this.base.settings.wiserApiRoot}VersionControl/${dynamicContentId}/publish-dynamic-content/${environment}/${version}`,
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

            var dynamicContentTable = document.querySelector(gridIdName);
            var parent1 = dynamicContentTable.querySelector(".k-state-selected");

            if (parent1 == null) {
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
            var commitToTestCheckbox = document.getElementById("commitTest");
            var commitToAcceptanceCheckbox = document.getElementById("commitAcceptatie");
            var commitToLiveCheckbox = document.getElementById("commitLive");

            var environment;

            const deploymentOptions = [];
            if (commitToTestCheckbox.checked) {
                deploymentOptions[0] = commitToTestCheckbox.value;
                environment = "test";
            }

            if (commitToAcceptanceCheckbox.checked) {
                deploymentOptions[1] = commitToAcceptanceCheckbox.value;
                environment = "acceptance";
            }

            if (commitToLiveCheckbox.checked) {
                deploymentOptions[2] = commitToLiveCheckbox.value;
                environment = "live";
            }

            if (!(commitToLiveCheckbox.checked) && (!commitToAcceptanceCheckbox.checked) && !(commitToTestCheckbox.checked)) {
                environment = "";
            }
            return environment;
        }

        getCommitMessage() {
            var commitMessage = document.getElementById("description-textbox").value;
            return commitMessage;
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
         * Event that gets called once a notification is being shown.
         */
        onShowNotification(event) {
            event.element.parent().css("width", "100%").css("height", "auto");
        }

    }

    // Initialize the DynamicItems class and make one instance of it globally available.
    window.dynamicItems = new VersionControl(settings);
})(moduleSettings);

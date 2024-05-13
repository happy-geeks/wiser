import { TrackJS } from "trackjs";
import { Wiser } from "../../Base/Scripts/Utils.js";
import { TaskUtils } from "./TaskUtils.js";
import "../../Base/Scripts/Processing.js";

require("@progress/kendo-ui/js/kendo.all.js");
require("@progress/kendo-ui/js/cultures/kendo.culture.nl-NL.js");
require("@progress/kendo-ui/js/messages/kendo.messages.nl-NL.js");
import Pusher from "pusher-js/with-encryption";

import "../Css/TaskAlerts.css";

// Any custom settings can be added here. They will overwrite most default settings inside the module.
const moduleSettings = {
    
};

((moduleSettings) => {
    class TaskAlerts {
        constructor(settings) {
            kendo.culture("nl-NL");

            // Base settings.
            this.settings = {};
            Object.assign(this.settings, settings);
            
            // Add logged in user access token to default authorization headers for all jQuery ajax requests.
            $.ajaxSetup({
                headers: { "Authorization": `Bearer ${localStorage.getItem("accessToken")}` }
            });

            this.tasks = [];
            this.taskInEdit = null;

            // Some elements for easy access.
            this.taskForm = null;
            this.editTaskForm = null;
            this.taskList = null;
            this.taskHistoryButton = null;

            // Buttons and inputs.
            this.addTaskButton = null;
            this.taskDateInput = null;
            this.taskDatePicker = null;
            this.taskUserSelect = null;
            this.saveTaskButton = null;
            this.cancelTaskButton = null;

            // Buttons and inputs for edit form.
            this.editTaskDateInput = null;
            this.editTaskDatePicker = null;
            this.editTaskUserSelect = null;
            this.editTaskStatusSelect = null;
            this.saveEditTaskButton = null;
            this.cancelEditTaskButton = null;

            // Other.
            this.mainLoader = null;
            this.idOfLastCompletedTask = 0;

            // Fire event on page ready for direct actions
            document.addEventListener("DOMContentLoaded", () => {
                this.onPageReady();
            });
        }

        async onPageReady() {
            this.mainLoader = $("#mainLoader");

            // Setup processing.
            document.addEventListener("processing.Busy", this.toggleMainLoader.bind(this, true));
            document.addEventListener("processing.Idle", this.toggleMainLoader.bind(this, false));

            Object.assign(this.settings, $("body").data());

            if (this.settings.trackJsToken) {
                TrackJS.install({
                    token: this.settings.trackJsToken
                });
            }
            
            // Show an error if the user is no longer logged in.
            const accessTokenExpires = localStorage.getItem("accessTokenExpiresOn");
            if (!accessTokenExpires || accessTokenExpires <= new Date()) {
                Wiser.alert({
                    title: "Niet ingelogd",
                    content: "U bent niet (meer) ingelogd. Ververs a.u.b. de pagina en probeer het opnieuw."
                });

                this.toggleMainLoader(false);
                return;
            }

            const user = JSON.parse(localStorage.getItem("userData"));
            this.settings.oldStyleUserId = user.oldStyleUserId;
            this.settings.username = user.adminAccountName ? `${user.adminAccountName} (Admin)` : user.name;
            this.settings.adminAccountLoggedIn = user.adminAccountName;
            
            const userData = await Wiser.getLoggedInUserData(this.settings.wiserApiRoot);
            this.settings.userId = userData.encryptedId;
            this.settings.tenantId = userData.encryptedTenantId;
            this.settings.zeroEncrypted = userData.zeroEncrypted;
            this.settings.wiserUserId = userData.id;
            
            if (!this.settings.wiserApiRoot.endsWith("/")) {
                this.settings.wiserApiRoot += "/";
            }
            
            this.settings.serviceRoot = `${this.settings.wiserApiRoot}templates/get-and-execute-query`;

            // Some elements for easy access.
            this.taskForm = document.querySelector(".taskForm");
            this.editTaskForm = document.querySelector(".editTaskForm");
            this.taskList = document.getElementById("taskList");

            // Buttons and inputs.
            this.addTaskButton = document.getElementById("addTask");
            this.taskDateInput = document.getElementById("taskDate");
            this.saveTaskButton = document.getElementById("saveTask");
            this.cancelTaskButton = document.getElementById("cancelTask");

            this.editTaskDateInput = document.getElementById("editTaskDate");
            this.saveEditTaskButton = document.getElementById("saveEditTask");
            this.cancelEditTaskButton = document.getElementById("cancelEditTask");

            this.taskHistoryButton = this.taskList.querySelector("#taskHistory a");

            this.initializeKendoElements();
            this.setBindings();
            this.loadTasks();
            this.registerPusherAndEventListeners();

            // Have to use jQuery to select because the Kendo widgets need to be retrieved.
            this.taskDatePicker = $("#taskDate").getKendoDatePicker();
            this.taskUserSelect = $("#taskUsers").getKendoMultiSelect();

            // Have to use jQuery to select because the Kendo widgets need to be retrieved.
            this.editTaskDatePicker = $("#editTaskDate").getKendoDatePicker();
            this.editTaskUserSelect = $("#editTaskUser").getKendoDropDownList();
            this.editTaskStatusSelect = $("#editTaskStatus").getKendoDropDownList();
            
            this.taskUserSelect.setDataSource({
                transport: {
                    read: {
                        url: `${this.settings.wiserApiRoot}users`,
                        dataType: "json"
                    }
                }
            });
            this.editTaskUserSelect.setDataSource({
                transport: {
                    read: {
                        url: `${this.settings.wiserApiRoot}users`,
                        dataType: "json"
                    }
                }
            });

            // Hide loader at the end.
            this.toggleMainLoader(false);
        }

        /**
         * Shows or hides the main (full screen) loader.
         * @param {boolean} show True to show the loader, false to hide it.
         */
        toggleMainLoader(show) {
            this.mainLoader.toggleClass("loading", show);
        }

        getTaskByEncryptedId(encryptedId) {
            if (this.tasks.length === 0 || typeof encryptedId !== "string" || encryptedId === "") {
                return null;
            }

            return this.tasks.find((task) => task.encryptedId === encryptedId);
        }

        async registerPusherAndEventListeners() {
            if (!this.settings.pusherAppKey) {
                console.warn("No pusher app key set. Task alerts will not receive new messages automatically.");
                return;
            }

            // Generate new pusher component
            const pusher = new Pusher(this.settings.pusherAppKey, {
                cluster: "eu",
                forceTLS: true
            });

            // Wiser update channel for pusher messages
            const channel = pusher.subscribe("agendering");

            // Generate pusher event for the current logged-in tenant
            const eventId = await Wiser.api({ url: `${this.settings.wiserApiRoot}pusher/event-id` });

            // User update channel for pusher messages
            channel.bind(`agendering_${eventId}`, (event) => {
                $("#taskList li:not(#taskHistory)").remove(); // First remove all loaded tasks

                parent.postMessage({
                    action: "NewMessageReceived"
                }, window.location.origin);

                taskAlerts.loadTasks();
            });
        }

        /**
         * @description Sets all bindings of the default elements found in the module.
         */
        setBindings() {
            this.taskHistoryButton.addEventListener("click", this.onOpenTaskHistoryClick.bind(this));

            //OPEN FORM PANEL
            this.addTaskButton.addEventListener("click", () => {
                this.openForm();
            });

            this.taskDateInput.addEventListener("click", () => {
                this.taskDatePicker.open();
            });

            this.cancelTaskButton.addEventListener("click", () => {
                this.closeForm();
            });

            this.saveTaskButton.addEventListener("click", async (e) => {
                const me = e.currentTarget;
                if (me.classList.contains("busy")) {
                    return;
                }

                window.processing.addProcess("createTaskAlerts");
                await this.createTaskAlerts();
                window.processing.removeProcess("createTaskAlerts");
            });

            // Edit buttons and inputs.
            this.editTaskDateInput.addEventListener("click", () => {
                this.editTaskDatePicker.open();
            });

            this.cancelEditTaskButton.addEventListener("click", () => {
                this.closeEditForm();
            });

            this.saveEditTaskButton.addEventListener("click", async (e) => {
                const me = e.currentTarget;
                if (me.classList.contains("busy")) {
                    return;
                }

                window.processing.addProcess("updateTaskAlert");
                await this.updateTaskAlert();
                window.processing.removeProcess("updateTaskAlert");
            });
        }

        openForm() {
            // Show the form.
            this.closeEditForm();
            this.taskForm.classList.add("active");
        }

        closeForm() {
            this.taskForm.classList.remove("active");
        }

        openEditForm(task) {
            this.editTaskDatePicker.value(task.createdOn);
            this.editTaskUserSelect.value(task.userId);
            this.editTaskStatusSelect.value(task.status);
            document.getElementById("editTaskDescription").value = task.content;

            this.taskInEdit = task;

            // Show the form.
            this.closeForm();
            this.editTaskForm.classList.add("active");
        }

        closeEditForm() {
            this.taskInEdit = null;
            this.editTaskForm.classList.remove("active");
        }

        async loadTasks() {
            const data = await Wiser.api({ url: `${this.settings.wiserApiRoot}task-alerts` });

            if (!data) {
                return;
            }

            data.forEach((task) => {
                const date = new Date(task.createdOn);
                task.agenderingDatePretty = kendo.toString(date, "dddd d MMMM yyyy");

                // Check if the task has a linked item.
                task.hasLinkedItem = task.linkedItemId && task.linkedItemModuleId > 0;
            });

            // Gotta re-order the data.
            data.sort((a, b) => {
                const date1 = new Date(a.createdOn).getTime();
                const date2 = new Date(b.createdOn).getTime();
                return date1 > date2 ? 1 : -1;
            });

            this.tasks = data;

            this.renderTasks();

            this.updateTaskCount();
        }

        renderTasks() {
            const template = document.getElementById("taskHtmlTemplate").innerHTML;
            const htmlList = [];
            this.tasks.forEach((task) => {
                const html = this.replaceVariables(template, task);
                htmlList.push(html);
            });

            // Remove the old "li" elements, except for the task history button.
            Array.from(this.taskList.children).forEach((li) => {
                if (li.id === "taskHistory") {
                    return;
                }
                li.parentElement.removeChild(li);
            });

            // And finally place the HTML.
            this.taskList.insertAdjacentHTML("afterbegin", htmlList.join(""));

            this.taskList.querySelectorAll("li").forEach((listItem) => {
                if (listItem.dataset.hasLinkedItem === "false") {
                    listItem.querySelector("a.open-task").style.display = "none";
                }
            });

            this.taskList.querySelectorAll("li input[type='checkbox']").forEach((input) => {
                const parent = input.closest("li");
                input.addEventListener("change", async () => {
                    if (parent.classList.contains("completed")) {
                        return;
                    }

                    await TaskUtils.completeTask(input.value, this.settings.username, this.settings.wiserApiRoot);

                    let completionWasUndone = false;
                    const notification = $("<div />").kendoNotification({
                        autoHideAfter: 3000,
                        position: {
                            left: 10
                        },
                        hide: () => {
                            if (completionWasUndone) {
                                return;
                            }
                            
                            parent.classList.add("completed");
                            this.updateTaskCount();
                        }
                    }).data("kendoNotification");
                    
                    notification.show(`<p>De taak is gemarkeerd als afgerond.</p><button type="button" class="k-primary" id="undoCompleteTask">Ongedaan maken</button>`);
                    $("#undoCompleteTask").kendoButton({
                        click: (event) => {
                            event.preventDefault();
                            completionWasUndone = true;
                            input.checked = false;
                            TaskUtils.returnTask(input.value, this.settings.username, this.settings.wiserApiRoot);
                        }
                    });
                });
            });

            this.taskList.querySelectorAll("a.open-task").forEach((link) => link.addEventListener("click", this.onOpenTaskClick.bind(this)));
            this.taskList.querySelectorAll("a.edit-task").forEach((link) => link.addEventListener("click", this.onEditTaskClick.bind(this)));
        }

        async createTaskAlerts() {
            let created = 0;

            try {
                const selectedUsers = this.taskUserSelect.dataItems();

                for (let i = 0; i < selectedUsers.length; i++) {
                    const userId = selectedUsers[i].id;
                    const username = selectedUsers[i].name;
                    const parentId = this.settings.zeroEncrypted;

                    // The input data is the entered information.
                    const inputData = [
                        {
                            key: "agendering_date",
                            value: kendo.toString(this.taskDatePicker.value() || new Date(), "yyyy-MM-dd")
                        },
                        {
                            key: "content",
                            value: document.getElementById("taskDescription").value
                        },
                        {
                            key: "userid",
                            value: userId
                        },
                        {
                            key: "username",
                            value: username
                        },
                        {
                            key: "placed_by",
                            value: this.settings.username
                        },
                        {
                            key: "placed_by_id",
                            value: this.settings.wiserUserId
                        }
                    ];

                    // Create the item.
                    const createResult = await this.createItem("agendering", parentId, null, null, inputData);

                    // Send a pusher to notify the receiving user.
                    await Wiser.api({
                        url: `${this.settings.wiserApiRoot}pusher/message`,
                        method: "POST",
                        contentType: "application/json",
                        data: JSON.stringify({
                            channel: "agendering",
                            userId: userId
                        })
                    });

                    created++;
                    if (created >= selectedUsers.length) {
                        // Refresh.
                        await this.loadTasks();
                    }
                }

                // After creating the new task, immediately close the form.
                this.closeForm();
            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan met het aanmaken van deze agendering. Probeer het opnieuw of neem contact op met ons");
            }
        }

        async updateTaskAlert() {
            if (!this.taskInEdit) {
                return;
            }

            try {
                // The input data is the entered information.
                const inputData = [
                    {
                        key: "agendering_date",
                        value: kendo.toString(this.editTaskDatePicker.value(), "yyyy-MM-dd")
                    },
                    {
                        key: "content",
                        value: document.getElementById("editTaskDescription").value
                    },
                    {
                        key: "userid",
                        value: this.editTaskUserSelect.dataItem().id
                    },
                    {
                        key: "username",
                        value: this.editTaskUserSelect.dataItem().title
                    },
                    {
                        key: "status",
                        value: this.editTaskStatusSelect.value()
                    },
                    {
                        key: "placed_by",
                        value: this.settings.wiserFullName
                    },
                    {
                        key: "placed_by_id",
                        value: this.settings.wiserUserId
                    }
                ];

                // And finally update the newly created item.
                await TaskUtils.updateItem(this.taskInEdit.encryptedId, inputData, this.settings.username, this.settings.wiserApiRoot);

                // Refresh to reflect changes.
                await this.loadTasks();

                // Send a pusher to notify the receiving user
                await Wiser.api({
                    url: `${this.settings.wiserApiRoot}pusher/message`,
                    method: "POST",
                    contentType: "application/json",
                    data: JSON.stringify({
                        channel: "agendering",
                        userId: this.editTaskUserSelect.value()
                    })
                });

                // After updating the task, immediately close the form.
                this.closeEditForm();
            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan met het updaten van deze agendering. Probeer het opnieuw of neem contact op met ons");
            }
        }

        replaceVariables(input, data) {
            let html = input;
            let startIndex = html.indexOf("{");
            let loopCount = 0;
            let rootIndex = 0;

            while (startIndex !== -1 && loopCount++ < 100) {
                const endIndex = html.indexOf("}", startIndex);
                if (endIndex === -1) {
                    break;
                }

                const variableName = html.slice(startIndex + 1, endIndex);
                if (data.hasOwnProperty(variableName)) {
                    let value = data[variableName];
                    if (variableName === "status" && value === "") {
                        value = "geen status";
                    }

                    const re = new RegExp(`\\{${variableName}\\}`, "g");
                    html = html.replace(re, value);
                } else {
                    rootIndex = endIndex + 1;
                }

                startIndex = html.indexOf("{", rootIndex);
            }
            return html;
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
        async createItem(entityType, parentId, name, linkTypeNumber, data = [], skipUpdate = false, moduleId = null) {
            try {
                const newItem = {
                    entityType: entityType,
                    title: name,
                    moduleId: moduleId || this.settings.moduleId
                };
                const parentIdUrlPart = parentId ? `&parentId=${encodeURIComponent(parentId)}` : "";
                const createItemResult = await Wiser.api({
                    url: `${this.settings.wiserApiRoot}items?linkType=${linkTypeNumber || 0}${parentIdUrlPart}&isNewItem=true`,
                    method: "POST",
                    contentType: "application/json",
                    data: JSON.stringify(newItem)
                });
                if (!skipUpdate) await TaskUtils.updateItem(createItemResult.newItemId, data || [], this.settings.username, this.settings.wiserApiRoot, true, entityType);

                const workflowResult = await Wiser.api({
                    url: `${this.settings.wiserApiRoot}items/${encodeURIComponent(createItemResult.newItemId)}/workflow?isNewItem=true`,
                    method: "POST",
                    contentType: "application/json",
                    data: JSON.stringify(newItem)
                });

                return {
                    itemId: createItemResult.newItemId,
                    itemIdPlain: createItemResult.newItemIdPlain,
                    linkId: createItemResult.newLinkId,
                    icon: createItemResult.icon,
                    workflowResult: workflowResult
                };
            } catch (exception) {
                console.error(exception);
                let error = exception;
                if (exception.responseText) {
                    error = exception.responseText;
                } else if (exception.statusText) {
                    error = exception.statusText;
                }
                kendo.alert(`Er is iets fout gegaan met het aanmaken van het item. Probeer het a.u.b. nogmaals of neem contact op met ons.<br><br>De fout was:<br><pre>${kendo.htmlEncode(error)}</pre>`);
                return null;
            }
        }

        updateTaskCount() {
            const taskCount = Array.from(document.getElementById("taskList").querySelectorAll("li")).filter((li) => {
                return !li.classList.contains("completed") && li.id !== "taskHistory";
            }).length;

            parent.postMessage({
                action: "UpdateTaskCount",
                taskCount: taskCount
            }, window.location.origin);
        }

        initializeKendoElements() {
            //DATE PICKER
            $(".datepicker").kendoDatePicker({
                format: "dd MMMM yyyy",
                culture: "nl-NL"
            });

            //MULTISELECT
            document.querySelectorAll(".multi-select").forEach((e) => {
                const element = $(e);
                const options = $.extend({
                    autoClose: false
                }, element.data());

                const widget = element.kendoMultiSelect(options).getKendoMultiSelect();
                Wiser.fixKendoDropDownScrolling(widget);
            });

            //DROPDOWNLIST
            document.querySelectorAll(".drop-down-list").forEach((e) => {
                const element = $(e);
                const data = element.data();

                const options = Object.assign({
                    filter: "contains",
                    height: 500
                }, data);

                const widget = element.kendoDropDownList(options).getKendoDropDownList();
                Wiser.fixKendoDropDownScrolling(widget);
            });

            //BUTTONS
            document.querySelectorAll(".module-button").forEach((e) => {
                const element = $(e);
                element.kendoButton({
                    icon: element.data("icon")
                });
            });
            
            // Button for undoing the completion of a task.
            $("#undoCompleteTask").kendoButton({
                click: (event) => {
                    event.preventDefault();
                    TaskUtils.returnTask(this.idOfLastCompletedTask, this.settings.username, this.settings.wiserApiRoot).then(() => {
                        this.loadTasks();
                    });
                }
            });
        }

        onOpenTaskClick(event) {
            event.preventDefault();
            const properties = event.currentTarget.dataset;

            if (window.parent) {
                window.parent.main.vueApp.openModule({
                    moduleId: `wiserItem_${properties.itemId}`,
                    name: `Wiser item via agendering`,
                    type: "dynamicItems",
                    iframe: true,
                    itemId: properties.itemId,
                    fileName: "DynamicItems.aspx",
                    queryString: `?moduleId=${encodeURIComponent(properties.moduleId || 0)}&iframe=true&itemId=${encodeURIComponent(properties.itemId)}&entityType=${encodeURIComponent(properties.entityType || "agendering")}`
                });
            }
        }

        async onEditTaskClick(event) {
            event.preventDefault();

            const properties = event.currentTarget.dataset;

            const task = this.getTaskByEncryptedId(properties.taskId);
            this.openEditForm(task);
        }

        onOpenTaskHistoryClick(event) {
            event.preventDefault();

            if (window.parent) {
                window.parent.main.vueApp.openModule({
                    moduleId: `taskHistory`,
                    name: `Agendering historie`,
                    type: "TaskAlerts",
                    iframe: true,
                    fileName: "/History"
                });
            }

        }
    }

    window.taskAlerts = new TaskAlerts(moduleSettings);
})(moduleSettings);

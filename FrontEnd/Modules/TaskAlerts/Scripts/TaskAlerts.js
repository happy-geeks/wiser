import { TrackJS } from "trackjs";
import { Wiser2 } from "../../Base/Scripts/Utils.js";
require("@progress/kendo-ui/js/kendo.all.js");
require("@progress/kendo-ui/js/cultures/kendo.culture.nl-NL.js");
require("@progress/kendo-ui/js/messages/kendo.messages.nl-NL.js");
import Pusher from "pusher-js/with-encryption";

import "../css/TaskAlerts.css";

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
                headers: { "Authorization": `Bearer ${localStorage.getItem("access_token")}` }
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

            // Fire event on page ready for direct actions
            document.addEventListener("DOMContentLoaded", () => {
                this.onPageReady();
            });
        }

        async onPageReady() {
            this.mainLoader = $("#mainLoader");

            // Setup JJL processing.
            document.addEventListener("processing.Busy", this.toggleMainLoader.bind(this, true));
            document.addEventListener("processing.Idle", this.toggleMainLoader.bind(this, false));

            Object.assign(this.settings, $("body").data());

            if (this.settings.trackJsToken) {
                TrackJS.install({
                    token: this.settings.trackJsToken
                });
            }
            
            // Show an error if the user is no longer logged in.
            const accessTokenExpires = localStorage.getItem("access_token_expires_on");
            if (!accessTokenExpires || accessTokenExpires <= new Date()) {
                Wiser2.alert({
                    title: "Niet ingelogd",
                    content: "U bent niet (meer) ingelogd. Ververs a.u.b. de pagina en probeer het opnieuw."
                });

                this.toggleMainLoader(false);
                return;
            }

            const user = JSON.parse(localStorage.getItem("userData"));
            this.settings.oldStyleUserId = user.oldStyleUserId;
            this.settings.username = user.adminAccountName ? `Happy Horizon (${user.adminAccountName})` : user.name;
            this.settings.adminAccountLoggedIn = user.adminAccountName;
            
            const userData = await Wiser2.getLoggedInUserData(this.settings.wiserApiRoot, this.settings.isTestEnvironment);
            this.settings.userId = userData.encrypted_id;
            this.settings.customerId = userData.encrypted_customer_id;
            this.settings.zeroEncrypted = userData.zero_encrypted;
            this.settings.wiser2UserId = userData.id;
            
           if (!this.settings.wiserApiRoot.endsWith("/")) {
                this.settings.wiserApiRoot += "/";
            }

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

        getTaskById(id) {
            if (this.tasks.length === 0 || typeof id !== "number") {
                return null;
            }

            return this.tasks.find((task) => task.id === id);
        }

        getTaskByEncryptedId(encryptedId) {
            if (this.tasks.length === 0 || typeof encryptedId !== "string" || encryptedId === "") {
                return null;
            }

            return this.tasks.find((task) => task.idencrypted === encryptedId);
        }

        async registerPusherAndEventListeners() {
            // Generate new pusher component
            const pusher = new Pusher("81c3d15c9d95132050cc", {
                cluster: "eu",
                forceTLS: true
            });

            // Wiser update channel for pusher messages
            const channel = pusher.subscribe("Wiser");
            channel.bind("update_event", (event) => {
                console.log("Received message for `update_event`", event);
            });

            // Generate pusher event for the current logged-in customer
            const eventId = await Wiser2.api({ url: `${this.settings.wiserApiRoot}pusher/event-id` });

            // User update channel for pusher messages
            channel.bind("agendering_" + eventId, (event) => {
                $("#taskList li:not(#taskHistory)").remove(); // First remove all loaded tasks

                console.log("Received message for pusher event `agendering`, for the logged-in user", event);

                parent.postMessage({
                    action: "NewMessageReceived"
                }, window.location.origin);

                taskAlerts.loadTasks();
            });

            console.log("New pusher element generated, and subscribed to the channel(s) `Wiser`");
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

                jjl.processing.addProcess("createTaskAlerts");
                await this.createTaskAlerts();
                jjl.processing.removeProcess("createTaskAlerts");
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

                jjl.processing.addProcess("updateTaskAlert");
                await this.updateTaskAlert();
                jjl.processing.removeProcess("updateTaskAlert");
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
            this.editTaskDatePicker.value(task.agendering_date);
            this.editTaskUserSelect.value(task.userid);
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
            const data = await Wiser2.api({ url: `${this.settings.wiserApiRoot}task-alerts` });

            if (!data) {
                return;
            }

            data.forEach((task) => {
                const date = new Date(task.agendering_date);
                task.agendering_date_pretty = kendo.toString(date, "dddd d MMMM yyyy");

                // Check if the task has a linked item.
                task.has_linked_item = task.linked_item_id && task.linked_item_module_id > 0;
            });

            // Gotta re-order the data.
            data.sort((a, b) => {
                const date1 = new Date(a.agendering_date).getTime();
                const date2 = new Date(b.agendering_date).getTime();
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

                    await this.completeTask(input.value);
                    parent.classList.add("completed");
                    this.updateTaskCount();
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
                            value: kendo.toString(this.taskDatePicker.value(), "yyyy-MM-dd")
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
                            value: this.settings.wiser2UserId
                        }
                    ];

                    // Create the item.
                    const createResult = await this.createItem("agendering", parentId, null, null, inputData);

                    // Send a pusher to notify the receiving user.
                    await Wiser2.api({
                        url: `${this.settings.wiserApiRoot}pusher/message`,
                        method: "POST",
                        contentType: "application/json",
                        data: JSON.stringify({ user_id: userId })
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
                await this.updateItem(this.taskInEdit.idencrypted, inputData);

                // Refresh to reflect changes.
                this.loadTasks();

                // Send a pusher to notify the receiving user
                await Wiser2.api({
                    url: `${this.settings.wiserApiRoot}pusher/message`,
                    method: "POST",
                    contentType: "application/json",
                    data: JSON.stringify({ user_id: this.editTaskUserSelect.value() })
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
                    entity_type: entityType,
                    title: name,
                    module_id: moduleId || this.settings.moduleId
                };
                const parentIdUrlPart = parentId ? `&parentId=${encodeURIComponent(parentId)}` : "";
                const createItemResult = await Wiser2.api({
                    url: `${this.settings.wiserApiRoot}items?linkType=${linkTypeNumber || 0}${parentIdUrlPart}&isNewItem=true`,
                    method: "POST",
                    contentType: "application/json",
                    data: JSON.stringify(newItem)
                });
                if (!skipUpdate) await this.updateItem(createItemResult.new_item_id, data || [], true, entityType);

                const workflowResult = await Wiser2.api({
                    url: `${this.settings.wiserApiRoot}items/${encodeURIComponent(createItemResult.new_item_id)}/workflow?isNewItem=true`,
                    method: "POST",
                    contentType: "application/json",
                    data: JSON.stringify(newItem)
                });

                return {
                    itemId: createItemResult.new_item_id,
                    itemIdPlain: createItemResult.new_item_id_plain,
                    linkId: createItemResult.new_link_id,
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

        /**
         * Updates an item in the database.
         * @param {string} encryptedItemId The encrypted item ID.
         * @param {Array<any>} inputData All values of all fields.
         * @param {boolean} isNewItem Whether or not this is a new item.
         * @returns {any} A promise with the result of the AJAX call.
         */
        async updateItem(encryptedItemId, inputData, isNewItem, entityType) {
            const updateItemData = {
                details: inputData,
                changed_by: this.settings.username,
                entity_type: entityType,
                published_environment: "Live"
            };

            return Wiser2.api({
                url: `${this.settings.wiserApiRoot}items/${encodeURIComponent(encryptedItemId)}?isNewItem=${!!isNewItem}`,
                method: "PUT",
                contentType: "application/json",
                dataType: "JSON",
                data: JSON.stringify(updateItemData)
            });
        }

        async completeTask(taskId) {
            const { DateTime } = await import("luxon");
            const checkedOnValue = DateTime.now().toISO();

            const inputData = [
                {
                    key: "checkedon",
                    value: checkedOnValue
                },
                {
                    key: "status",
                    value: "afgerond"
                }
            ];
            await this.updateItem(taskId, inputData);
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
                Wiser2.fixKendoDropDownScrolling(widget);
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
                Wiser2.fixKendoDropDownScrolling(widget);
            });

            //BUTTONS
            document.querySelectorAll(".module-button").forEach((e) => {
                const element = $(e);
                const options = element.data();
                element.kendoButton(options);
            });
        }

        onOpenTaskClick(event) {
            event.preventDefault();
            const properties = event.currentTarget.dataset;

            if (window.parent) {
                window.parent.main.vueApp.openModule({
                    module_id: `wiserItem_${properties.itemId}`,
                    name: `Wiser item via agendering`,
                    type: "dynamicItems",
                    iframe: true,
                    item_id: properties.itemId,
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
                    module_id: `taskHistory`,
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

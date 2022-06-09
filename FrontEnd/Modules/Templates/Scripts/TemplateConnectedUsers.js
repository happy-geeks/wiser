require("@microsoft/signalr/dist/browser/signalr.min.js");

export class TemplateConnectedUsers {
    #connection;

    constructor(base) {
        this.base = base;
        // The current template ID that is being tracked.
        this.currentTemplateId = 0;
    }

    async init() {
        this.#connection = new signalR.HubConnectionBuilder().withUrl("/templatesHub").build();
        this.#connection.on("UserOpenedTemplate", this.#userOpenedTemplate.bind(this));
        this.#connection.on("UserClosedTemplate", this.#userClosedTemplate.bind(this));
        await this.#connection.start();
    }

    add(templateId, username) {
        this.#connection.invoke("AddUserAsync", templateId, username).then(() => {
            this.#notifyUsersUpdate(templateId);
        }).catch(err => console.error(err));
    }

    remove(templateId, username) {
        this.#connection.invoke("RemoveUserAsync", templateId, username).then(() => {
            this.#notifyUsersUpdate(templateId);
        }).catch(err => console.error(err));
    }

    async #getCurrentUsers(templateId) {
        return await this.#connection.invoke("GetUsersInTemplate", templateId);
    }

    #userOpenedTemplate(templateId) {
        if (templateId !== -1 && templateId !== this.currentTemplateId) return;

        this.#notifyUsersUpdate(templateId);
    }

    #userClosedTemplate(templateId) {
        if (templateId !== -1 && templateId !== this.currentTemplateId) return;

        this.#notifyUsersUpdate(templateId);
    }

    #notifyUsersUpdate(templateId) {
        this.#getCurrentUsers(templateId !== -1 ? templateId : this.currentTemplateId).then((usersList) => {
            const users = [];
            usersList.forEach(user => {
                users.push(user.username);
            });
            const event = new CustomEvent("TemplateConnectedUsers:UsersUpdate", {
                detail: users
            })
            document.dispatchEvent(event);
        });
    }
}
require("@microsoft/signalr/dist/browser/signalr.min.js");

export class TemplateConnectedUsers {
    constructor(base) {
        this.base = base;
        this.connectedUsers = {
            templates: {}
        };

        this.connection = new signalR.HubConnectionBuilder().withUrl("/templatesHub").build();
        this.connection.on("UserOpenedTemplate", this.#userOpenedTemplate.bind(this));
        this.connection.on("UserClosedTemplate", this.#userClosedTemplate.bind(this));

        this.connection.start();
    }

    add(templateId, user) {
        this.connection.invoke("AddUser", templateId, user).then(() => {
            this.#notifyUsersUpdate(templateId);
        }).catch(err => console.error(err));
    }

    remove(templateId, user) {
        this.connection.invoke("RemoveUser", templateId, user).then(() => {
            this.#notifyUsersUpdate(templateId);
        }).catch(err => console.error(err));
    }

    async getCurrentUsers(templateId) {
        return await this.connection.invoke("GetUsersInTemplate", templateId);
    }

    #rebuildConnectedUsers(templateId) {
        // TODO
        
    }

    #userOpenedTemplate(templateId, user) {
        const key = `template_${templateId}`;
        if (!this.connectedUsers.templates.hasOwnProperty(key)) {
            this.connectedUsers.templates[key] = [];
        }

        if (!this.connectedUsers.templates[key].includes(user)) {
            this.connectedUsers.templates[key].push(user);
        }
    }

    #userClosedTemplate(templateId, user) {
        const key = `template_${templateId}`;

        // Check if template ID array exists.
        if (!this.connectedUsers.templates.hasOwnProperty(key)) {
            return;
        }

        // Find user in the array.
        const index = this.connectedUsers.templates[key].findIndex(u => u === user);
        if (index === -1) return;

        // Remove user from the array.
        this.connectedUsers.templates[key].splice(index, 1);
    }

    #notifyUsersUpdate(templateId) {
        const event = new CustomEvent("TemplateConnectedUsers:UsersUpdate", {
            detail: this.connectedUsers.templates[`template_${templateId}`]
        })
        document.dispatchEvent(event);
    }
}
import Pusher from "pusher-js/with-encryption";
import {Wiser} from "../../Base/Scripts/Utils";

export class TemplateConnectedUsers {
    constructor(base) {
        this.base = base;
        // The current template ID that is being tracked.
        this.currentTemplateId = 0;

        // The users that have the current template open.
        this.usersInTemplate = [];
    }

    async init() {
        if (!this.base.settings.pusherAppKey) {
            console.log("No pusher app key set. Wiser will not be able to show which users have opened this template.");
            return;
        }

        // Create user event ID to uniquely identify this user.
        this.userEventId = `${this.base.settings.username}-${new Date().toISOString()}`;

        // Generate new pusher component.
        const pusher = new Pusher(this.base.settings.pusherAppKey, {
            cluster: "eu",
            forceTLS: true
        });
        pusher.connection.bind("state_change", (states) => {
            if (["disconnected", "unavailable"].includes(states.current)) {
                this.removeUser();
            }
        });

        // Wiser update channel for pusher messages.
        const channel = pusher.subscribe("Wiser");
        channel.bind(`template_users`, this.#handlePusherEvent.bind(this));

        console.log("New pusher element generated, and subscribed to the channel(s) `Wiser`");
    }

    switchTemplate(templateId) {
        // If already in a template, notify other users that this user has closed this template.
        if (this.currentTemplateId > 0) {
            this.removeUser();
        }
        this.currentTemplateId = templateId;
        // Reset users in template list.
        this.usersInTemplate = [];

        // Ping all users (including self) who has connected with this template.
        this.ping();
    }

    #handlePusherEvent(event) {
        if (!event.hasOwnProperty("templateId") || event.templateId <= 0) {
            return;
        }

        if (!["subscribe", "unsubscribe", "ping"].includes(event.action)) {
            console.error(`Unknown action for TemplateConnectedUsers: '${event.action}'`);
            return;
        }

        if (event.templateId !== this.currentTemplateId) {
            return;
        }

        const index = this.usersInTemplate.findIndex(u => u.userEventId === event.userEventId);
        switch (event.action) {
            case "subscribe": {
                if (index === -1)
                {
                    this.usersInTemplate.push({
                        username: event.username,
                        userEventId: event.userEventId
                    });
                }
                this.#notifyUsersUpdate();
                break;
            }
            case "unsubscribe": {
                if (index >= 0) {
                    this.usersInTemplate.splice(index, 1);
                }
                this.#notifyUsersUpdate();
                break;
            }
            case "ping": {
                // Another user has asked who is in this template.
                this.addUser();
                break;
            }
        }
    }

    #notifyUsersUpdate() {
        const usernames = this.usersInTemplate.map(u => u.username);
        const event = new CustomEvent("TemplateConnectedUsers:UsersUpdate", {
            detail: usernames
        })
        document.dispatchEvent(event);
    }

    /**
     * Send a message using Pusher.
     */
    async #sendPusherMessage(eventData) {
        await Wiser.api({
            url: `${this.base.settings.wiserApiRoot}pusher/message`,
            method: "POST",
            contentType: "application/json",
            data: JSON.stringify({
                channel: "template_users",
                isGlobalMessage: true,
                eventData: eventData
            })
        });
    }

    /**
     * Add current user to the template's connected users.
     */
    async addUser() {
        await this.#sendPusherMessage({
            action: "subscribe",
            templateId: this.currentTemplateId,
            username: this.base.settings.username,
            userEventId: this.userEventId
        });
    }

    /**
     * Remove current user from the template's connected users.
     */
    async removeUser() {
        await this.#sendPusherMessage({
            action: "unsubscribe",
            templateId: this.currentTemplateId,
            userEventId: this.userEventId
        });
    }

    /**
     * Ping other users to check who else has opened this template.
     */
    async ping() {
        await this.#sendPusherMessage({
            action: "ping",
            templateId: this.currentTemplateId
        });
    }
}
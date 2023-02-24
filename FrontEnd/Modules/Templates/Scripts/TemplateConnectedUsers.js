import Pusher from "pusher-js/with-encryption";
import {Wiser} from "../../Base/Scripts/Utils";

export class TemplateConnectedUsers {
    constructor(base) {
        this.base = base;
        this.pusherChannel = null;
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

        // Generate unique channel name for the current subdomain.
        this.pusherChannel = `Wiser-Templates-${this.base.settings.subDomain}`;

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

        // Subscribe to the channel unique for the templates module and this subdomain.
        const channel = pusher.subscribe(this.pusherChannel);
        // Bind the various events.
        channel.bind("subscribe", this.#onSubscribe.bind(this));
        channel.bind("unsubscribe", this.#onUnsubscribe.bind(this));
        channel.bind("ping", this.#onPing.bind(this));

        console.log(`New pusher element generated, and subscribed to the channel '${this.pusherChannel}'`);
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

    /**
     * Checks if the event was meant for users that have the same template open as the current user.
     */
    #validatePusherEvent(event) {
        return event.hasOwnProperty("templateId") && event.templateId > 0 && event.templateId === this.currentTemplateId;
    }

    /**
     * Handles the "subscribe" Pusher event. This will add the name of the user in the event data to the list of
     * connected users if it wasn't already added.
     */
    #onSubscribe(event) {
        if (!this.#validatePusherEvent(event)) return;

        const index = this.usersInTemplate.findIndex(u => u.userEventId === event.userEventId);
        if (index === -1) {
            this.usersInTemplate.push({
                username: event.username,
                userEventId: event.userEventId
            });
        }
        this.#notifyUsersUpdate();
    }

    /**
     * Handles the "unsubscribe" Pusher event. This will remove the name of the user in the event data from the list of
     * connected users.
     */
    #onUnsubscribe(event) {
        if (!this.#validatePusherEvent(event)) return;

        const index = this.usersInTemplate.findIndex(u => u.userEventId === event.userEventId);
        if (index >= 0) {
            this.usersInTemplate.splice(index, 1);
        }
        this.#notifyUsersUpdate();
    }

    /**
     * Handles the "ping" Pusher event. This will notify all other users in the channel which template the
     * current user has opened.
     */
    #onPing(event) {
        if (!this.#validatePusherEvent(event)) return;

        // Another user has asked who is in this template.
        this.addUser();
    }

    /**
     * Dispatches an event with the new information about the connected users.
     * This will be picked up by the Templates script to update the list of connected users.
     */
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
    async #sendPusherMessage(eventName, eventData) {
        await Wiser.api({
            url: `${this.base.settings.wiserApiRoot}pusher/message`,
            method: "POST",
            contentType: "application/json",
            data: JSON.stringify({
                channel: this.pusherChannel,
                eventName: eventName || "template_users",
                isGlobalMessage: true,
                eventData: eventData
            })
        });
    }

    /**
     * Broadcast existence to other users in this channel. 
     */
    async addUser() {
        await this.#sendPusherMessage("subscribe", {
            templateId: this.currentTemplateId,
            username: this.base.settings.username,
            userEventId: this.userEventId
        });
    }

    /**
     * Remove current user from the template's connected users.
     */
    async removeUser() {
        await this.#sendPusherMessage("unsubscribe", {
            templateId: this.currentTemplateId,
            userEventId: this.userEventId
        });
    }

    /**
     * Ping other users to check who else has opened this template.
     */
    async ping() {
        await this.#sendPusherMessage("ping", {
            templateId: this.currentTemplateId
        });
    }
}
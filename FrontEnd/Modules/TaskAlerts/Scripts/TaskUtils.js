import {Wiser} from "../../Base/Scripts/Utils";

export class TaskUtils {
    /**
     * Updates an item in the database.
     * @param {string} encryptedItemId The encrypted item ID.
     * @param {Array<any>} inputData All values of all fields.
     * @param username {string} The username of the logged in user.
     * @param wiserApiRoot {string} The root URL of the Wiser API, including /api/vX/.
     * @param {boolean} isNewItem Whether or not this is a new item.
     * @param entityType {string} The entity type of the task item.
     * @returns {any} A promise with the result of the AJAX call.
     */
    static async updateItem(encryptedItemId, inputData, username, wiserApiRoot, isNewItem = false, entityType = "agendering") {
        const updateItemData = {
            details: inputData,
            changedBy: username,
            entityType: entityType,
            publishedEnvironment: "Live"
        };

        return Wiser.api({
            url: `${wiserApiRoot}items/${encodeURIComponent(encryptedItemId)}?isNewItem=${!!isNewItem}`,
            method: "PUT",
            contentType: "application/json",
            dataType: "JSON",
            data: JSON.stringify(updateItemData)
        });
    }

    /**
     * Mark a task as completed
     * @param taskId The ID of the task.
     * @param username The username of the user that is logged in.
     * @param wiserApiRoot {string} The root URL of the Wiser API, including /api/vX/.
     * @returns {Promise<void>}
     */
    static async completeTask(taskId, username, wiserApiRoot) {
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
        
        await this.updateItem(taskId, inputData, username, wiserApiRoot);
        
        
    }

    /**
     * Return a task to the todo list.
     * @param taskId The ID of the task.
     * @param username The username of the user that is logged in.
     * @param wiserApiRoot {string} The root URL of the Wiser API, including /api/vX/.
     * @returns {Promise<void>}
     */
    static async returnTask(taskId, username, wiserApiRoot) {
        const { DateTime } = await import("luxon");
        const checkedOnValue = DateTime.now().toISO();

        const inputData = [
            {
                key: "checkedon",
                value: checkedOnValue
            },
            {
                key: "status",
                value: ""
            }
        ];
        await this.updateItem(taskId, inputData, username, wiserApiRoot);
    }
}
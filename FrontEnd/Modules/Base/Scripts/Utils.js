import {DateTime} from "luxon";
import "./Processing.js";
import * as Diff2Html from "diff2html/lib/diff2html"
import "diff2html/bundles/css/diff2html.min.css"

window.$ = require("jquery");

/**
 * This function overrides the default ":contains" psuedo from jQuery, so that it's no longer case sensitive.
 */
$.expr[":"].contains = $.expr.createPseudo(function (arg) {
    return function (elem) {
        return $(elem).text().toUpperCase().indexOf(arg.toUpperCase()) >= 0;
    };
});

export class Utils {
    static sleep(sleepTimeInMs = 1000) {
        return new Promise((resolve) => {
            setTimeout(resolve, sleepTimeInMs);
        });
    }

    /**
         * Check whether element is an array
         * @param {element} element
         * @return boolean
         */
    static isArray(element) {
        if (Array.isArray) {
            if (Array.isArray(element)) {
                return true;
            } else {
                return false;
            }
        } else {
            if (element instanceof Array) {
                return true;
            } else {
                return false;
            }
        }
    }

    static toQueryString(obj, prependQuestionMarkOnData = false, arraySeparator = ",") {
        let returnString = "";
        let cnt = 0;
        for (const key in obj) {
            if (!obj.hasOwnProperty(key) || obj[key] === undefined) continue;

            if (cnt > 0) {
                returnString += "&";
            }
            cnt++;

            const val = obj[key];

            returnString += `${key}=${(Utils.isArray(val) ? val.join(arraySeparator) : val)}`;
        }
        return returnString.length === 0 ? "" : `${prependQuestionMarkOnData ? "?" : ""}${returnString}`;
    }
}

/**
 * Main class.
 */
export class Modules {
    /**
     * Get the settings of a module from the database.
     * @param {string} apiRoot The root URL of the Wiser API.
     * @param {number} moduleId The ID of the module.
     * @returns {any} The module settings as an object.
     */
    static async getModuleSettings(apiRoot, moduleId) {
        try {
            const result = await Wiser.api({ url: `${apiRoot}modules/${moduleId}/settings` });
            if (!result) {
                return {
                    id: moduleId,
                    options: {}
                };
            }

            result.options = result.options || {};

            return result;
        } catch (exception) {
            if (exception.status !== 404) {
                console.error("Error while getting module settings", exception);
                kendo.alert("Er is iets fout gegaan met het ophalen van de instellingen voor deze module. Neem a.u.b. contact op met ons.");
            }
            return {};
        }
    }

    /**
     * Checks if the response from a request is an array and, optionally, if it contains at least one item. This can be used to validate the response from HTTP requests.
     * @deprecated This function is deprecated in favor of Wiser.validateArray.
     * @param {any} response The response from a request.
     * @param {boolean} allowEmptyResponse Whether the response must contain at least one item.
     * @returns {boolean} Whether the result is an array and satisfies the condition of mustHaveItems.
     */
    static validateJsonResponse(response, allowEmptyResponse = false) {
        console.warn("Modules.validateJsonResponse is deprecated. User Wiser.validateArray instead.");
        return Wiser.validateArray(response, allowEmptyResponse);
    }
}

/**
 * Date utils.
 */
export class Dates {
    static get LongDateTimeFormat() { return { year: "numeric", month: "long", day: "2-digit", hour: "2-digit", minute: "2-digit", weekDay: "long" } }

    /**
     * Parses a string with a date and time into an actual date object. This function uses Luxon for parsing the date.
     * @param {string} value The date string.
     * @returns {any} A momentJs object. You can use result.isValid() to check whether the date has been parsed successfully, or result.toDate() to get a normal javascript date object. For more information, see https://momentjs.com/docs/.
     */
    static parseDateTime(value) {
        return this.parseDate(value);
    }

    /**
     * Parses a string with a date into an actual date object. This function uses Luxon for parsing the date.
     * @param {string} value The date string.
     * @returns {any} A momentJs object. You can use result.isValid() to check whether the date has been parsed successfully, or result.toDate() to get a normal javascript date object. For more information, see https://momentjs.com/docs/.
     */
    static parseDate(value) {
        value = value || "";

        return typeof(value) === "string" ? DateTime.fromSQL(value.trim(), { locale: "nl-NL" }) : DateTime.fromJSDate(value, { locale: "nl-NL" });
    }

    /**
     * Parses a string with a time into an actual date object. This function uses Luxon for parsing the date.
     * @param {string} value The date string.
     * @returns {any} A momentJs object. You can use result.isValid() to check whether the date has been parsed successfully, or result.toDate() to get a normal javascript date object. For more information, see https://momentjs.com/docs/.
     */
    static parseTime(value) {
        value = (value || "").trim();

        return DateTime.fromSQL(value);
    }

    static formatWiserDateString(dateString) {
        return this.parseDate(dateString).toLocaleString(this.LongDateTimeFormat);
    }

    static convertMomentFormatToLuxonFormat(momentFormattingString) {
        if (!momentFormattingString) return momentFormattingString;

        return momentFormattingString
            .replace(/M/g, "L")
            .replace("DDDD", "ooo")
            .replace("DDD", "o")
            .replace(/D/g, "d")
            .replace(/Y/g, "y");
    }
}

/**
 * String utils.
 */
export class Strings {
    /**
     * Converts the first letter of a string to upper case and returns the new string.
     * @param {string} input The input.
     * @returns {string} The new string where the first letter is now upper case.
     */
    static capitalizeFirst(input) {
        if (!input) {
            return input;
        }

        return input[0].toUpperCase() + input.slice(1);
    }

    /**
     * Checks if the input is a number and returns that parsed as a number.
     * If the input is not a number, it returns the original value.
     * @param {string} input The input string.
     * @returns {any} The input parsed as number, if it is a number, otherwise the original value.
     */
    static convertToNumberIfPossible(input) {
        const numberValue = Number(input);
        return isNaN(numberValue) ? input : numberValue;
    }

    /**
     * Returns a string to show a human readable file size.
     * @param {any} sizeInBytes The size in bytes.
     * @returns {string} The human readable value.
     */
    static getTotalFileSizeMessage(sizeInBytes) {
        if (!sizeInBytes) {
            return sizeInBytes;
        }

        if (typeof sizeInBytes === "string") {
            sizeInBytes = parseInt(sizeInBytes);
        } else if (typeof sizeInBytes !== "number") {
            return sizeInBytes;
        }

        if (sizeInBytes < 1024) {
            return sizeInBytes.toString() + " B";
        }

        sizeInBytes /= 1024;

        if (sizeInBytes < 1024) {
            return sizeInBytes.toFixed(2) + " KB";
        } else {
            return (sizeInBytes / 1024).toFixed(2) + " MB";
        }
    }

    /**
     * Function to clean up HTML in html editors after pasting.
     * @param {string} input The input HTML.
     * @returns {string} The clean HTML.
     */
    static cleanupHtml(input) {
        return input.replace(/<\/?[wo]:[^>]*>/gm, "");
    }

    /**
     * Checks whether the given string is undefined, null, or an empty string.
     * @param {string} input The input string.
     * @returns {boolean} True if the given string is undefined, null, or an empty string; otherwise, false.
     */
    static isNullOrEmpty(input) {
        return input === undefined || input === null || (typeof input === "string" && input === "");
    }

    /**
     * Checks whether the given string is undefined, null, an empty string, or consists exclusively of white-space characters.
     * @param {string} value The input string.
     * @returns {boolean} True if the given string is undefined, null, an empty string, or if the given consists exclusively of white-space characters; otherwise, false.
     */
    static isNullOrWhiteSpace(value) {
        return value === undefined || value === null || (typeof value === "string" && value.trim() === "");
    }

    /**
     * Makes sure that a string can be used as a JSON property by converting certain characters to placeholders.
     * @param input {string} The input string to convert.
     * @returns {string} A string that can safely be used as a JSON property.
     */
    static makeJsonPropertyName(input) {
        if (!input) return input;
        return input.replaceAll("-", "__h__").replaceAll(" ", "__s__").replaceAll(":", "__c__").replaceAll("(", "__bl__").replaceAll(")", "__br__").replaceAll(".", "__d__").replaceAll(",", "__co__");
    }

    /**
     * Convert a string that was created by makeJsonPropertyName back into it's original form.
     * @param input {string} The input string that was converted by makeJsonPropertyName.
     * @returns {string} The original string.
     */
    static unmakeJsonPropertyName(input) {
        if (!input) return input;
        return input.replaceAll("__h__", "-").replaceAll("__s__", " ").replaceAll("__c__", ":").replaceAll("__bl__", "(").replaceAll("__br__", ")").replaceAll("__d__", ".").replaceAll("__co__", ",");
    }
}

/**
 * Wiser utils.
 */
export class Wiser {
    static async api(settings) {
        // Find the Window that contains the main vue app of Wiser. We need this for saving the promise of refreshing the auth token.
        // We do this on that window, because some modules have multiple iframes that all do xhr calls, so we need to make sure they all wait for each other
        // and use the same refresh token.
        let wiserMainWindow = window;
        let previousWindow = null;
        while (wiserMainWindow && !wiserMainWindow.main && previousWindow !== wiserMainWindow) {
            previousWindow = wiserMainWindow;
            wiserMainWindow = wiserMainWindow.parent;
        }

        // If another process/request is already requesting a new access token, wait for that to finish first.
        // This is to prevent multiple access token requests at the same time and to prevent racing conditions.
        if (wiserMainWindow.wiserApiRefreshTokenPromise) {
            const timeoutPromise = new Promise((res) => setTimeout(() => res("TIMEOUT"), 1000));
            const newRefreshToken = await Promise.race([wiserMainWindow.wiserApiRefreshTokenPromise, timeoutPromise]);

            // Add logged in user access token to default authorization headers for all jQuery ajax requests.
            if (newRefreshToken !== "TIMEOUT") {
                $.ajaxSetup({
                    headers: {"Authorization": `Bearer ${newRefreshToken.access_token}`}
                });
            }
        }

        const accessTokenExpires = localStorage.getItem("accessTokenExpiresOn");
        let user = JSON.parse(localStorage.getItem("userData"));
        let currentDate = new Date();
        currentDate.setSeconds(currentDate.getSeconds() - 5);
        if (settings.url.indexOf("/connect/token") === -1 && (!accessTokenExpires || new Date(accessTokenExpires) <= currentDate)) {
            if (!user || !user.refresh_token) {
                console.error("No refresh token found!");

                // If we have no refresh token for some reason, logout the user.
                if (wiserMainWindow && wiserMainWindow.main && wiserMainWindow.main.vueApp) {
                    await wiserMainWindow.main.vueApp.logout();
                }

                return Promise.reject("No refresh token found!");
            }

            const wiserSettings = document.body.dataset;

            // Create a promise for the refresh token, so that other requests know we're already busy getting one. They will then wait for this to finish.
            wiserMainWindow.wiserApiRefreshTokenPromise = new Promise(async (resolve, reject) => {
                try {
                    const refreshTokenResult = await $.ajax({
                        url: wiserSettings.wiserApiAuthenticationUrl,
                        method: "POST",
                        data: {
                            "grant_type": "refresh_token",
                            "refresh_token": user.refresh_token,
                            "subDomain": wiserSettings.subDomain,
                            "client_id": wiserSettings.apiClientId,
                            "client_secret": wiserSettings.apiClientSecret,
                            "isTestEnvironment": wiserSettings.isTestEnvironment
                        }
                    });

                    refreshTokenResult.expiresOn = new Date(new Date().getTime() + ((refreshTokenResult.expires_in - (refreshTokenResult.expires_in > 60 ? 60 : 0)) * 1000));
                    refreshTokenResult.adminLogin = refreshTokenResult.adminLogin === "true" || refreshTokenResult.adminLogin === true || refreshTokenResult.adminAccountId > 0;

                    localStorage.setItem("accessToken", refreshTokenResult.access_token);
                    localStorage.setItem("accessTokenExpiresOn", refreshTokenResult.expiresOn);
                    user = Object.assign({}, user, refreshTokenResult);
                    localStorage.setItem("userData", JSON.stringify(user));

                    // Add logged in user access token to default authorization headers for all jQuery ajax requests.
                    $.ajaxSetup({
                        headers: {"Authorization": `Bearer ${refreshTokenResult.access_token}`}
                    });

                    resolve(refreshTokenResult);
                } catch (exception) {
                    console.error("Error occurred while trying to get a new token.", exception);
                    reject(exception);
                }
            });

            // Of course we also need to wait until we have the new auth token, otherwise the code below will be executed too early.
            const timeoutPromise = new Promise((res) => setTimeout(() => res("TIMEOUT"), 1000));
            await Promise.race([wiserMainWindow.wiserApiRefreshTokenPromise, timeoutPromise]);
        }

        // Double check if the current ajax setup has the correct token set.
        // It can happen that this still has an old token when someone is working in multiple browser tabs at the same time,
        // If the token gets refreshed in tab X, it will not update the ajax setup in tab Y, so we need to do that now.
        const currentAjaxSetup = $.ajaxSetup();
        if (!currentAjaxSetup.headers || currentAjaxSetup.headers.Authorization !== `Bearer ${user.access_token}`) {
            $.ajaxSetup({
                headers: {"Authorization": `Bearer ${user.access_token}`}
            });
        }

        return $.ajax(settings).fail((jqXhr, textStatus, errorThrown) => {
            if (jqXhr.status !== 401) {
                return;
            }

            if (settings.url.indexOf("/connect/token") > -1) {
                console.error("Refresh token failed!");

                // If we got a 401 while using the refresh token, it means the refresh token is no longer valid, so logout the user.
                if (window.parent && window.parent.main && window.parent.main.vueApp) {
                    window.parent.main.vueApp.logout();
                }
            }
        });
    }

    /**
     * Get the data of the logged in user.
     * @param {string} apiRoot The root URL of the Wiser API.
     * @param {boolean} forceRefresh If true, the user data will be retrieved from the API instead of the session storage.
     * @returns {any} The user data as an object.
     */
    static async getLoggedInUserData(apiRoot, forceRefresh = false) {
        try {
            let result = sessionStorage.getItem("userSettings");
            if (result) {
                const sessionData = JSON.parse(result);
                if (sessionData.dateTime && new Date() - new Date(sessionData.dateTime) < 3600000) {
                    // Only use the data from session if it's less than 1 hour old (1000 milliseconds * 60 seconds * 60 minutes).
                    result = sessionData.data;
                } else {
                    result = null;
                }
            }

            if (!result || forceRefresh) {
                result = await Wiser.api({ url: `${apiRoot}users/self` });
                if (result) {
                    sessionStorage.setItem("userSettings", JSON.stringify({ dateTime: new Date(), data: result }));
                }
            }

            if (!result) {
                return {};
            }

            return result;
        } catch (exception) {
            console.error("Error while getting logged in user data", exception);
            kendo.alert("Er is iets fout gegaan met het ophalen van instellingen (logged in user data) die nodig zijn voor bepaalde functionaliteit. Neem a.u.b. contact op met ons.");
            return {};
        }
    }

    /**
     * Shows a default alert window using Kendo UI.
     * @param {any} options The options for the alert window.
     * @returns {kendo.ui.Alert} The Kendo Alert widget that was created.
     */
    static alert(options) {
        return $("<div />").kendoAlert(options).getKendoAlert().open();
    }

    /**
     * Shows a default confirm dialog using Kendo UI.
     * @param {any} options The options for the confirm dialog.
     * @returns {kendo.ui.Confirm} The Kendo Confirm widget that was created.
     */
    static confirm(options) {
        return $("<div />").kendoConfirm(options).getKendoConfirm().open();
    }

    /**
     * Shows a simple popup with a message, and optionally a title.
     * @param {any} options The options for the message dialog.
     * @returns {kendo.ui.Dialog} The KendoDialog widget that was created, or null if Kendo UI was unavailable.
     */
    static showMessage(options) {
        options = Object.assign({
            actions: [{
                text: "OK",
                primary: true
            }]
        }, options);

        return $("<div />").kendoDialog(options).getKendoDialog().open();
    }

    /**
     * Shows a dialog that can be used as a confirmation for deleting something.
     * @param {string} text The text to show in the dialog.
     * @param {string} title Optional: The title of the dialog. Default value is "Verwijderen".
     * @param {string} cancelButtonText Optional: The text to show in the cancel button. Default value is "Annuleren".
     * @param {string} confirmButtonText TOptional: The text to show in the confirm button. Default value is "Verwijderen".
     */
    static showConfirmDialog(text, title = "Verwijderen", cancelButtonText = "Annuleren", confirmButtonText = "Verwijderen") {
        return new Promise((resolve, reject) => {
            const dialog = $("<div />").kendoDialog({
                title: title,
                closable: true,
                modal: true,
                content: text,
                actions: [
                    {
                        text: cancelButtonText,
                        cssClass: "cancel-button"
                    },
                    {
                        text: confirmButtonText,
                        primary: true,
                        cssClass: "delete-button",
                        action: (event) => {
                            resolve(event);
                        }
                    }
                ],
                close: (event) => {
                    reject(event);
                }
            }).data("kendoDialog");

            dialog.wrapper.addClass("delete-dialog");

            dialog.open();
        });
    }

    /**
     * Checks if a given object is an array and, optionally, if it contains at least one item. This can be used to validate the response from HTTP requests.
     * @param {any} obj The response from a request.
     * @param {boolean} allowEmpty Whether the response must contain at least one item.
     * @returns {boolean} Whether the given object is an array and satisfies the condition of {@param allowEmpty}.
     */
    static validateArray(obj, allowEmpty = false) {
        return Array.isArray(obj) && (allowEmpty || obj.length > 0);
    }

    /**
     * This function can be used the fix a problem with scrolling in Kendo components, such as DropDownList, ComboBox and MultiSelect.
     * The problem is when you scroll past the bottom of the list, the entire page will start scrolling, which closes the dropdown list.
     * @param {any} widget The kendo widget.
     */
    static fixKendoDropDownScrolling(widget) {
        if (!widget || !widget.ul) {
            console.warn("Wiser.fixKendoDropDownScrolling called with an undefined widget, or a widget that has no 'ul' property.", widget);
            return;
        }

        // The container must be a DOM element (so not a jQuery object).
        // The .get function gets the DOM element.
        const container = widget.ul.parent().get(0);
        container.addEventListener("wheel", (event) => {
            const triedToScrollPastTop = container.scrollTop === 0 && event.deltaY < 0;
            const triedToScrollPastBottom = container.scrollTop === container.scrollHeight - container.offsetHeight && event.deltaY > 0;

            if (triedToScrollPastTop || triedToScrollPastBottom) {
                event.preventDefault();
                event.stopPropagation();
            }
        });
    }

    /**
     * A method to replace variables with values from item details.
     * @param {string} input The input string to do the replacements on.
     * @param {any} itemDetails The details (fields/properties + values) of an item.
     * @param {boolean} uriEncodeValues Whether or not to encode all values to be safely used in an URL.
     * @param {boolean} removeUnknownVariables Whether or not to remove all variables that are not found in the data object.
     * @param {boolean} convertToNumberIfPossible Whether or not to convert the output to a number if possible. Default is true.
     * @returns {string} The input string with all variables replaced with values from fields.
     */
    static doWiserItemReplacements(input, itemDetails, uriEncodeValues = false, removeUnknownVariables = false, convertToNumberIfPossible = true) {
        if (!input || typeof input !== "string") {
            return input;
        }

        let output = input.replace(/{itemTitle}/gi, !uriEncodeValues ? itemDetails.title : encodeURIComponent(itemDetails.title));
        output = output.replace(/{itemId}/gi, !uriEncodeValues ? itemDetails.id : encodeURIComponent(itemDetails.id));
        output = output.replace(/{encryptedId}/gi, !uriEncodeValues ? (itemDetails.encryptedId || itemDetails.encrypted_id || itemDetails.encryptedid) : encodeURIComponent(itemDetails.encryptedId || itemDetails.encrypted_id || itemDetails.encryptedid));
        output = output.replace(/{environment}/gi, !uriEncodeValues ? (itemDetails.publishedEnvironment || itemDetails.published_environment) : (encodeURIComponent(itemDetails.publishedEnvironment) || encodeURIComponent(itemDetails.published_environment)));
        output = output.replace(/{entityType}/gi, !uriEncodeValues ? (itemDetails.entityType || itemDetails.entity_type) : (encodeURIComponent(itemDetails.entityType) || encodeURIComponent(itemDetails.entity_type)));

        if (itemDetails.details && !itemDetails.property_) {
            itemDetails.property_ = {};

            for (let field of itemDetails.details) {
                itemDetails.property_[field.key] = field.value;
            }
        }

        return Wiser.doObjectReplacements(output, itemDetails.property_, uriEncodeValues, removeUnknownVariables, convertToNumberIfPossible);
    }

    /**
     * A method to replace variables with values from item details.
     * @param {string} input The input string to do the replacements on.
     * @param {any} data An JSON object with keys and values to use for replacements.
     * @param {boolean} uriEncodeValues Whether or not to encode all values to be safely used in an URL.
     * @param {boolean} removeUnknownVariables Whether or not to remove all variables that are not found in the data object.
     * @param {boolean} convertToNumberIfPossible Whether or not to convert the output to a number if possible. Default is true.
     * @returns {any} The input string with all variables replaced with values from the object.
     */
    static doObjectReplacements(input, data, uriEncodeValues = false, removeUnknownVariables = false, convertToNumberIfPossible = true) {
        if (!data) {
            return input;
        }

        let output = input || "";
        for (let key in data) {
            if (!data.hasOwnProperty(key)) {
                continue;
            }

            const regExp = new RegExp(`{${key}([\?a-zA-Z0-9]*)?}`, "gi");
            let value = !uriEncodeValues ? data[key] : encodeURIComponent(data[key]);

            // If output is just one single variable and it ends with a question mark followed by a text or number,
            // it means that the default value should be the value after the question mark, instead of empty string.
            // If there is no value after the question mark, the default value becomes NULL.
            const regExpMatch = output.match(regExp);
            if (regExpMatch && regExpMatch.length === 1 && regExpMatch[0] === output && output.indexOf("?") > 0 && !value) {
                const split = output.split(/[\{\}\?]+/);

                return split.length <= 3 ? null : (!convertToNumberIfPossible ? split[2] : Strings.convertToNumberIfPossible(split[2]));
            }

            output = output.replace(regExp, value);
        }

        if (removeUnknownVariables) {
            output = Wiser.removeUnknownVariables(output);
        }

        return !convertToNumberIfPossible ? output : Strings.convertToNumberIfPossible(output);
    }

    /**
     * A method to remove all variables from a string.
     * @param input The string to remove the variables from.
     * @returns {*} The input string without any variables.
     */
    static removeUnknownVariables(input) {
        if (!input) {
            return input;
        }

        return input.replace(/{[a-zA-Z0-9]+[\?a-zA-Z0-9]*}/gi, "");
    }

    /**
     * This method will call an external API, based on the options that are set in the table "wiser_api_connection".
     * @param {string} settings The Wiser settings.
     * @param {number} apiConnectionId The ID of the API options in "wiser_api_connection".
     * @param {any} itemDetails Optional: If this is called via an action button, you can enter the details of the opened item here so that they can be used in the API call.
     * @param {any} extraData Optional: A JSON object with keys and values, with any other extra data that should be used for replacements in the data that will be sent to the API.
     * @param {any} newAuthenticationData Optional: New authentication data, such as authenticationCode. This will override any authenticationData from database.
     * @param {string} subDomain The current sub domain, if any.
     * @returns {any} A promise.
     */
    static doApiCall(settings, apiConnectionId, itemDetails = null, extraData = null, newAuthenticationData = null) {
        console.log("doApiCall", { settings: settings, apiConnectionId: apiConnectionId, itemDetails: itemDetails, extraData: extraData, newAuthenticationData: newAuthenticationData });

        return new Promise(async (success, reject) => {
            const process = `doApiCall_${apiConnectionId}_${Date.now()}`;

            try {
                // Initial checks.
                if (!apiConnectionId) {
                    reject("Er is geen 'apiConnectionId' ingesteld. Neem a.u.b. contact op met ons.");
                    return;
                }

                if (!settings || !settings.serviceRoot) {
                    reject("Er is geen 'serviceRoot' ingesteld. Neem a.u.b. contact op met ons.");
                    return;
                }

                window.processing.addProcess(process);

                // Get the settings.
                const apiConnectionData = await Wiser.api({ url: `${settings.wiserApiRoot}api-connections/${apiConnectionId}` });
                if (!apiConnectionData || !apiConnectionData.options) {
                    reject("Er werd geprobeerd om een API aan te roepen, echter zijn er niet genoeg gegevens bekend. Neem a.u.b. contact op met ons.");
                    window.processing.removeProcess(process);
                    return;
                }


                // Parse the settings.
                const apiOptions = apiConnectionData.options || {};
                let authenticationData = apiConnectionData.authenticationData || {};
                if (newAuthenticationData) {
                    authenticationData = $.extend(authenticationData, newAuthenticationData);
                }

                const extraHeaders = apiOptions.extraHeaders || {};

                if (!apiOptions.baseUrl) {
                    reject("Er werd geprobeerd om een API aan te roepen, echter zijn er niet genoeg gegevens bekend. Neem a.u.b. contact op met ons.");
                    window.processing.removeProcess(process);
                    return;
                }

                // If base URL ends with a slash, remove it.
                if (apiOptions.baseUrl[apiOptions.baseUrl.length - 1] === "/") {
                    apiOptions.baseUrl = apiOptions.baseUrl.substr(0, apiOptions.baseUrl.length - 2);
                }

                // Do authentication if required.
                if (apiOptions.authentication) {
                    switch ((apiOptions.authentication.type || "").toUpperCase()) {
                        case "OAUTH2":
                            await Wiser.doOauth2Authentication(settings, apiOptions, apiConnectionId, authenticationData, extraHeaders, itemDetails, extraData, success, reject);
                            break;
                        default:
                            reject("Geen of onbekend authenticatie-type opgegeven. Neem a.u.b. contact op met ons.");
                            window.processing.removeProcess(process);
                            return;
                    }
                }

                // Execute all the actions.
                const allActionResults = [];
                for (let action of apiOptions.actions) {
                    // Set default values to all properties.
                    action.method = action.method || "POST";
                    action.contentType = action.contentType || "application/json";
                    action.extraHeaders = action.extraHeaders || extraHeaders || {};

                    // If a query ID is set, execute that query first, so that the results can be used in the call to the API.
                    if (action.preRequestQueryId && itemDetails) {
                        const queryResult = await Wiser.api({
                            method: action.method,
                            url: `${settings.wiserApiRoot}items/${encodeURIComponent(itemDetails.encryptedId || itemDetails.encrypted_id || itemDetails.encryptedid)}/action-button/0?queryId=${encodeURIComponent(action.preRequestQueryId)}&itemLinkId=${encodeURIComponent(itemDetails.linkId || itemDetails.link_id || 0)}`,
                            data: !extraData ? null : JSON.stringify(extraData),
                            contentType: "application/json"
                        });

                        if (queryResult && queryResult.otherData && queryResult.otherData.length > 0) {
                            extraData = $.extend(extraData || {}, queryResult.otherData[0]);
                        }
                    }

                    // Do replacements on action function.
                    if (extraData) {
                        action.function = Wiser.doObjectReplacements(action.function, extraData);
                    }
                    if (itemDetails) {
                        action.function = Wiser.doWiserItemReplacements(action.function, itemDetails);
                    }

                    // If function does not start with a slash, add it.
                    if (action.function[0] !== "/") {
                        action.function = "/" + action.function;
                    }

                    // Setup the headers for the request.
                    const headers = {
                        "X-Api-Url": `${apiOptions.baseUrl}${action.function}`
                    };

                    if (action.extraHeaders) {
                        for (let headerName in action.extraHeaders) {
                            if (!action.extraHeaders.hasOwnProperty(headerName)) {
                                continue;
                            }

                            headers[`X-Extra-${headerName}`] = action.extraHeaders[headerName];
                        }
                    }

                    // Do replacements on the request data, if there is any.
                    if (action.data) {
                        const doAllReplacements = (data) => {
                            for (let key in data) {
                                if (!data.hasOwnProperty(key)) {
                                    continue;
                                }

                                switch (typeof data[key]) {
                                    case "string":
                                        if (extraData) {
                                            data[key] = Wiser.doObjectReplacements(data[key], extraData);
                                        }
                                        if (itemDetails) {
                                            data[key] = Wiser.doWiserItemReplacements(data[key], itemDetails);
                                        }
                                        break;
                                    case "object":
                                        doAllReplacements(data[key]);
                                        break;
                                }
                            }
                        };

                        doAllReplacements(action.data);
                    }

                    // Execute the request.
                    let apiResults = await $.ajax({
                        url: "/ExternalApis/Proxy",
                        headers: headers,
                        method: "POST",
                        contentType: action.contentType,
                        data: action.contentType.toLowerCase() === "application/json" ? JSON.stringify(action.data) : action.data
                    });

                    let resultsPropertyNames = [];

                    // A lot of APIs don't directly return their data, they will have a surrounding property (or more than one).
                    // For Example, Exact returns results like this: { d: { results: [] } }. So we added settings for handling this.
                    switch (action.contentType) {
                        case "text/csv":
                            apiResults = { "Content": apiResults };
                            break;

                        default:
                            if (action.resultsPropertyName) {
                                resultsPropertyNames = action.resultsPropertyName.split(".");
                            }
                            else if (apiOptions.resultsPropertyName) {
                                resultsPropertyNames = apiOptions.resultsPropertyName.split(".");
                            }
                            for (let resultsPropertyName of resultsPropertyNames) {
                                if (!apiResults) {
                                    break;
                                }
                                apiResults = apiResults[resultsPropertyName];
                            }
                            break;
                    }
                    
                    let postQueryData = apiResults;
                    
                    const appendDataWithApiResults = action.appendDataWithApiResults ?? false;
                    if(appendDataWithApiResults) {
                        postQueryData = {};
                        postQueryData['response'] = apiResults;
                        postQueryData['data'] = extraData;

                        postQueryData = Misc.flattenObject(postQueryData);
                    }

                    // If a postRequestQueryId is set, execute that query after the API call, so that the results of the API call can be used in the query.
                    if (action.postRequestQueryId && itemDetails) {
                        const postRequestQueryResult = await Wiser.api({
                            method: "POST",
                            url: `${settings.wiserApiRoot}items/${encodeURIComponent(itemDetails.encryptedId || itemDetails.encrypted_id || itemDetails.encryptedid)}/action-button/0?queryId=${encodeURIComponent(action.postRequestQueryId)}&itemLinkId=${encodeURIComponent(itemDetails.linkId || itemDetails.link_id || 0)}`,
                            data: !postQueryData ? null : JSON.stringify(postQueryData),
                            contentType: "application/json"
                        });
                    }

                    allActionResults.push(apiResults);
                }

                // We're done, handle the promise' success.
                window.processing.removeProcess(process);
                success(allActionResults);

                // Do the actual API call, after authentication.
            } catch (exception) {
                window.processing.removeProcess(process);
                reject(exception);
            }
        });
    }

    /**
     * Use standard full OAUTH2 authentication.
     * If a manual login is required, this will open a window where the user can login.
     * @param {any} settings The module settings.
     * @param {any} apiOptions The API options from wiser_api_connection.
     * @param {any} apiConnectionId The ID of the API connection/authentication data in wiser_api_connection.
     * @param {any} authenticationData The saved authentication data from wiser_api_connection.
     * @param {any} extraHeaders The extra headers that are going to be sent with the final API request. This method will add the authorization header to this object.
     * @param {any} itemDetails Optional: If this is called via an action button, you can enter the details of the opened item here so that they can be used in the API call.
     * @param {any} extraData The extra data to send with the authentication.
     * @param {any} success The success from the promise of the doApiCall function.
     * @param {any} reject The reject from the promise of the doApiCall function.
     */
    static async doOauth2Authentication(settings, apiOptions, apiConnectionId, authenticationData, extraHeaders, itemDetails, extraData, success, reject) {
        // Check if we still have a valid authentication token.
        if (!authenticationData.accessTokenExpire || new Date(authenticationData.accessTokenExpire) <= new Date()) {
            console.log(`[doApiCall] - AccessToken has expired on ${authenticationData.accessTokenExpire}`);

            // If we have either a refresh token, or an authentication token, then the user doesn't have to manually login anymore.
            if (authenticationData.refreshToken || authenticationData.authenticationToken) {
                const authenticationRequest = {
                    method: "POST",
                    url: "/ExternalApis/Proxy",
                    headers: { "X-Api-Url": `${apiOptions.baseUrl}${apiOptions.authentication.accessTokenUrl}` },
                    data: {}
                };

                if (authenticationData.refreshToken) {
                    console.log(`[doApiCall] - We have a refresh token, so using that to get a new access token and a new refresh token.`);
                    authenticationRequest.data.refresh_token = authenticationData.refreshToken;
                    authenticationRequest.data.grant_type = "refresh_token";
                } else {
                    console.log(`[doApiCall] - We have no refresh token, but we do have an authentication code, using that to get access token and refresh token.`);
                    authenticationRequest.data.redirect_uri = apiOptions.authentication.callBackUrl;
                    authenticationRequest.data.code = authenticationData.authenticationToken;
                    authenticationRequest.data.grant_type = "authorization_code";
                }

                authenticationRequest.data.client_id = apiOptions.authentication.clientId;
                authenticationRequest.data.client_secret = apiOptions.authentication.clientSecret;

                console.log("Do ajax request:", authenticationRequest);
                const authenticationResult = await $.ajax(authenticationRequest);
                console.log("authenticationResult", authenticationResult);
                authenticationData = $.extend(authenticationData, authenticationResult);
                authenticationData.accessTokenExpire = moment().add(parseInt(authenticationData.expiresIn), "seconds").toDate();

                await Wiser.api({
                    method: "POST",
                    url: `${settings.serviceRoot}/UPDATE_API_AUTHENTICATION_DATA?id=${encodeURIComponent(apiConnectionId)}`,
                    data: {
                        id: apiConnectionId,
                        authenticationData: JSON.stringify(authenticationData)
                    }
                });
            } else {
                // We have no refresh token and no authentication token, this means the user must manually login first (that is how OAUTH2 works).
                if (!apiOptions.authentication.authUrl || !apiOptions.authentication.clientId || !apiOptions.authentication.callBackUrl) {
                    reject("Er werd geprobeerd om een API aan te roepen, echter zijn er niet genoeg gegevens bekend voor de authenticatie. Neem a.u.b. contact op met ons.");
                    return;
                }

                // Open a window where the user can login.
                const loginUrl = `${apiOptions.baseUrl}${apiOptions.authentication.authUrl}?clientId=${encodeURIComponent(apiOptions.authentication.clientId)}&redirectUri=${encodeURIComponent(apiOptions.authentication.callBackUrl)}&responseType=code&forceLogin=0`;
                console.log(`[doApiCall] - We have no information for authentication, which means the tenant needs to login first. Opening window with url '${loginUrl}'...`);
                const loginWindow = window.open(loginUrl, "_blank", "height=550, width=550, status=yes, toolbar=no, menubar=no, location=no,addressbar=no");

                // Wait for the user to finish logging in
                let interval = setInterval(async () => {
                    try {
                        if (loginWindow.document.domain === document.domain && loginWindow.document.readyState === "complete") {
                            // we're here when the child window returned to our domain
                            const urlParams = new URLSearchParams(loginWindow.location.search);
                            const authenticationToken = urlParams.get("code");

                            if (!authenticationToken) {
                                console.log("No token/code found yet.");
                                return;
                            }

                            clearInterval(interval);
                            loginWindow.close();
                            Wiser.doApiCall(settings, apiConnectionId, itemDetails, extraData, { authenticationToken: authenticationToken }).then(success).catch(reject);
                        }
                    } catch (intervalException) {
                        // we're here when the child window has been navigated away or closed
                        if (loginWindow.closed) {
                            clearInterval(interval);
                            console.warn("Window was closed");
                            reject("Het loginscherm was vroegtijdig gesloten.");
                            return;
                        }
                    }
                }, 200);
            }
        }

        extraHeaders.Authorization = `${Strings.capitalizeFirst(authenticationData.tokenType)} ${authenticationData.accessToken}`;
    }

    static fileManagerModes = Object.freeze({
        images: "images",
        files: "files",
        templates: "templates"
    })

    /**
     * Event that gets called when the user executes the custom action for adding a link to a file from Wiser to the HTML editor.
     * This will open the fileHandler from Wiser 1.0 via the parent frame. Therefor this function only works while Wiser is being loaded in an iframe.
     * @param {any} event The event from the execute action.
     * @param {any} kendoEditor The Kendo HTML editor where the action is executed in.
     * @param {any} moduleName The name of the module where the action is executed in.
     * @param {any} filesRootId The (encrypted) ID of the file parent directory.
     */
    static async onHtmlEditorFileExec(event, kendoEditor, moduleName, filesRootId) {
        if (!filesRootId) {
            kendo.alert("Er is nog geen 'filesRootId' ingesteld in de database. Neem a.u.b. contact op met ons om dit te laten instellen.");
        } else {
            const fileManagerWindowSender = { kendoEditor: kendoEditor, codeMirror: null, contentbuilder: null };
            const fileManagerWindowMode = this.fileManagerModes.files;
            const fileManagerWindow = Wiser.initializeFileManager(fileManagerWindowSender, fileManagerWindowMode, null, null, moduleName);

            fileManagerWindow.center().open();
        }
    }

    /**
     * Event that gets called when the user executes the custom action for adding an image from Wiser to the HTML editor.
     * This will open the fileHandler from Wiser 1.0 via the parent frame. Therefor this function only works while Wiser is being loaded in an iframe.
     * @param {any} event The event from the execute action.
     * @param {any} kendoEditor The Kendo HTML editor where the action is executed in.
     * @param {any} moduleName The name of the module where the action is executed in.
     * @param {any} imagesRootId The (encrypted) ID of the image parent directory.
     */
    static async onHtmlEditorImageExec(event, kendoEditor, moduleName, imagesRootId) {
        if (!imagesRootId) {
            kendo.alert("Er is nog geen 'imagesRootId' ingesteld in de database. Neem a.u.b. contact op met ons om dit te laten instellen.");
        } else {
            const fileManagerWindowSender = { kendoEditor: kendoEditor, codeMirror: null, contentbuilder: null };
            const fileManagerWindowMode = this.fileManagerModes.images;

            const fileManagerWindow = Wiser.initializeFileManager(fileManagerWindowSender, fileManagerWindowMode, null, null, moduleName);
            fileManagerWindow.center().open();
        }
    }

    /**
     * Event that gets called when the user executes the custom action for entering a translation variable.
     * @param {any} event The event from the execute action.
     * @param {any} editor The HTML editor where the action is executed in.
     * @param {string} wiserApiRoot The root of the Wiser API.
     */
    static async onHtmlEditorTranslationExec(event, editor, wiserApiRoot) {
        try {
            const dialogElement = $("#translationsDialog");
            let translationsDialog = dialogElement.data("kendoDialog");

            if (translationsDialog) {
                translationsDialog.destroy();
            }

            const translationsDropDown = dialogElement.find("#translationsDropDown").kendoDropDownList({
                optionLabel: "Selecteer een vertaalwoord",
                dataTextField: "value",
                dataValueField: "key",
                dataSource: {
                    transport: {
                        read: async (options) => {
                            try {
                                const results = await Wiser.api({ url: `${wiserApiRoot}languages/translations` });
                                options.success(results);
                            } catch (exception) {
                                console.error(exception);
                                options.error(exception);
                            }
                        }
                    }
                }
            }).data("kendoDropDownList");

            translationsDialog = dialogElement.kendoDialog({
                width: "900px",
                title: "Vertaalwoord invoegen",
                closable: false,
                modal: true,
                actions: [
                    {
                        text: "Annuleren"
                    },
                    {
                        text: "Invoegen",
                        primary: true,
                        action: (event) => {
                            const selectedTranslation = translationsDropDown.value();
                            if (!selectedTranslation) {
                                kendo.alert("Kies a.u.b. een vertaalwoord.")
                                return false;
                            }

                            const originalOptions = editor.options.pasteCleanup;
                            editor.options.pasteCleanup.none = true;
                            editor.options.pasteCleanup.span = false;
                            editor.exec("inserthtml", { value: `[T{${selectedTranslation}}]` });
                            editor.options.pasteCleanup.none = originalOptions.none;
                            editor.options.pasteCleanup.span = originalOptions.span;
                        }
                    }
                ]
            }).data("kendoDialog");

            translationsDialog.open();
        } catch (exception) {
            console.error(exception);
            kendo.alert("Er is iets fout gegaan. Probeer het a.u.b. nogmaals of neem contact op met ons.");
        }
    }

    /**
     * Creates a new item in the database and executes any workflow for creating an item.
     * @param {any} moduleSettings The settings of the module that calls this method. This needs to contain at least the "wiserApiRoot" property.
     * @param {string} entityType The type of item to create.
     * @param {string} parentId The (encrypted) ID of the parent to add the new item to.
     * @param {string} name Optional: The name of the new item.
     * @param {number} linkTypeNumber Optional: The type number of the link between the new item and it's parent.
     * @param {any} data Optional: The data to save with the new item.
     * @param {boolean} skipUpdate Optional: By default the updateItem function will be called after creating the item, to save the data of the item. Set this parameter to true if you want to skip that step (if you have no other data to save).
     * @param {number} moduleId Optional: The id of the module in which the item should be created.
     * @returns {Object<string, any>} An object with the properties 'itemId', 'icon' and 'workflowResult'.
     */
    static async createItem(moduleSettings, entityType, parentId, name, linkTypeNumber, data = [], skipUpdate = false, moduleId = null) {
        try {
            const newItem = {
                entityType: entityType,
                title: name,
                moduleId: moduleId || moduleSettings.moduleId || 0
            };

            const parentIdUrlPart = parentId ? `&parentId=${encodeURIComponent(parentId)}` : "";
            const createItemResult = await Wiser.api({
                url: `${moduleSettings.wiserApiRoot}items?linkType=${linkTypeNumber || 0}${parentIdUrlPart}&isNewItem=true`,
                method: "POST",
                contentType: "application/json",
                dataType: "JSON",
                data: JSON.stringify(newItem)
            });

            // Call updateItem with only the title, to make sure the SEO value of the title gets saved if needed.
            let newItemDetails = [];
            if (!skipUpdate) newItemDetails = await Wiser.updateItem(moduleSettings, createItemResult.newItemId, data || [], false, name, false, entityType);

            const workflowResult = await Wiser.api({
                url: `${moduleSettings.wiserApiRoot}items/${encodeURIComponent(createItemResult.newItemId)}/workflow?isNewItem=true`,
                method: "POST",
                contentType: "application/json",
                dataType: "JSON",
                data: JSON.stringify(newItem)
            });
            let apiActionResult = null;

            // Check if we need to execute any API action and do that.
            try {
                const apiActionId = await Wiser.getApiAction(moduleSettings, "after_insert", entityType);
                if (apiActionId) {
                    apiActionResult = await Wiser.doApiCall(moduleSettings, apiActionId, newItemDetails);
                }
            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan tijdens het uitvoeren (of opzoeken) van de actie 'api_after_update'. Indien er een koppeling is opgezet met een extern systeem, dan zijn de wijzigingen nu niet gesynchroniseerd naar dat systeem. Probeer het a.u.b. nogmaals, of neem contact op met ons.");
            }

            return {
                itemId: createItemResult.newItemId,
                itemIdPlain: createItemResult.newItemIdPlain,
                linkId: createItemResult.newLinkId,
                icon: createItemResult.icon,
                workflowResult: workflowResult,
                apiActionResult: apiActionResult
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
     * @param {any} moduleSettings The settings of the module that calls this method. This needs to contain at least the "wiserApiRoot" property.
     * @param {string} encryptedItemId The encrypted item ID.
     * @param {Array<any>} inputData All values of all fields.
     * @param {boolean} isNewItem Whether or not this is a new item.
     * @param {string} title The title of the item.
     * @param {boolean} executeWorkFlow Whether or not to execute any workflow that might be set up, if/when the update has succeeded.
     * @param {string} entityType The entity type of the item.
     * @returns {any} A promise with the result of the AJAX call.
     */
    static async updateItem(moduleSettings, encryptedItemId, inputData, isNewItem, title = null, executeWorkFlow = true, entityType = null) {
        const updateItemData = {
            title: title,
            details: inputData,
            changedBy: moduleSettings.username,
            entityType: entityType
        };

        if (executeWorkFlow) {
            const apiActionId = await Wiser.getApiAction(moduleSettings, "before_update", entityType);
            if (apiActionId) {
                await Wiser.doApiCall(moduleSettings, apiActionId, updateItemData);
            }
        }

        try {
            const updateResult = await Wiser.api({
                url: `${moduleSettings.wiserApiRoot}items/${encodeURIComponent(encryptedItemId)}?isNewItem=${!!isNewItem}`,
                method: "PUT",
                contentType: "application/json",
                dataType: "JSON",
                data: JSON.stringify(updateItemData)
            });

            // Check if we need to execute any API action and do that.
            try {
                if (executeWorkFlow) {
                    const apiActionId = await Wiser.getApiAction(moduleSettings, "after_update", updateResult.entityType);
                    if (apiActionId) {
                        await Wiser.doApiCall(this.settings, apiActionId, updateResult);
                    }
                }
            } catch (exception) {
                console.error(exception);
                kendo.alert("Er is iets fout gegaan tijdens het uitvoeren (of opzoeken) van de actie 'api_after_update'. Indien er een koppeling is opgezet met een extern systeem, dan zijn de wijzigingen nu niet gesynchroniseerd naar dat systeem. Probeer het a.u.b. nogmaals, of neem contact op met ons.");
            }

            return updateResult;
        } catch (exception) {
            console.error(exception);
            kendo.alert("Er is iets fout gegaan tijdens opslaan van de wijzigingen. Probeer het a.u.b. nogmaals, of neem contact op met ons.");
            return false;
        }
    }

    /**
     * Marks an item as deleted.
     * @param {any} moduleSettings The settings of the module that calls this method. This needs to contain at least the "wiserApiRoot" property.
     * @param {string} encryptedItemId The encrypted item ID.
     * @param {string} entityType The entity type of the item to delete. This is required for workflows.
     * @returns {Promise} A promise with the result of the AJAX call.
     */
    static async deleteItem(moduleSettings, encryptedItemId, entityType) {
        try {
            const apiActionId = await Wiser.getApiAction(moduleSettings, "before_delete", entityType);
            if (apiActionId) {
                await Wiser.doApiCall(moduleSettings, apiActionId, { encryptedId: encryptedItemId });
            }
        } catch (exception) {
            console.error(exception);
            kendo.alert("Er is iets fout gegaan tijdens het uitvoeren (of opzoeken) van de actie 'api_before_delete'. Hierdoor is het betreffende item ook niet uit Wiser verwijderd. Probeer het a.u.b. nogmaals of neem contact op met ons.");
            return new Promise((resolve, reject) => {
                reject(exception);
            });
        }

        return Wiser.api({
            url: `${moduleSettings.wiserApiRoot}items/${encodeURIComponent(encryptedItemId)}?entityType=${entityType || ""}`,
            method: "DELETE",
            contentType: "application/json",
            dataType: "JSON"
        });
    }

    /**
     * Moves an item from archive back to the default tables again, so that it can be used again..
     * @param {any} moduleSettings The settings of the module that calls this method. This needs to contain at least the "wiserApiRoot" property.
     * @param {string} encryptedItemId The encrypted item ID.
     * @param {string} entityType The entity type of the item to undelete.
     * @returns {Promise} A promise with the result of the AJAX call.
     */
    static async undeleteItem(moduleSettings, encryptedItemId, entityType) {
        return Wiser.api({
            url: `${moduleSettings.wiserApiRoot}items/${encodeURIComponent(encryptedItemId)}?undelete=true&entityType=${entityType || ""}`,
            method: "DELETE",
            contentType: "application/json",
            dataType: "JSON"
        });
    }

    /**
     * Duplicates an item (including values of fields, excluding linked items).
     * @param {any} moduleSettings The settings of the module that calls this method. This needs to contain at least the "wiserApiRoot" property.
     * @param {string} itemId The (encrypted) ID of the item to get the HTML for.
     * @param {string} parentId The (encrypted) ID of the parent item, so that the duplicated item will be linked to the same parent.
     * @param {string} entityType Optional: The entity type of the item to duplicate, so that the API can use the correct table and settings.
     * @param {string} parentEntityType Optional: The entity type of the parent of item to duplicate, so that the API can use the correct table and settings.
     * @returns {Promise} The details about the newly created item.
     */
    static async duplicateItem(moduleSettings, itemId, parentId, entityType = null, parentEntityType = null) {
        try {
            const entityTypeQueryString = !entityType ? "" : `?entityType=${encodeURIComponent(entityType)}`;
            const parentEntityTypeQueryString = !parentEntityType ? "" : `${!entityType ? "?" : "&"}parentEntityType=${encodeURIComponent(parentEntityType)}`;
            const createItemResult = await Wiser.api({
                method: "POST",
                url: `${moduleSettings.wiserApiRoot}items/${encodeURIComponent(itemId)}/duplicate/${encodeURIComponent(parentId)}${entityTypeQueryString}${parentEntityTypeQueryString}`,
                contentType: "application/json",
                dataType: "JSON"
            });
            const workflowResult = await Wiser.api({
                method: "POST",
                url: `${moduleSettings.wiserApiRoot}items/${encodeURIComponent(createItemResult.newItemId)}/workflow?isNewItem=true`,
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
     * Gets a certain API action for a certain entity type. This will return the ID that can be used for executing an API action.
     * @param {any} moduleSettings The settings of the module that calls this method. This needs to contain at least the "wiserApiRoot" property.
     * @param {string} actionType The type of action to get. Possible values: "after_insert", "after_update", "before_update" and "before_delete".
     * @param {string} entityType The name of the entity type to get the action for.
     * @returns {number} The ID of the API action, or 0 if there is no action set.
     */
    static async getApiAction(moduleSettings, actionType, entityType) {
        const result = await Wiser.api({ url: `${moduleSettings.wiserApiRoot}entity-types/${encodeURIComponent(entityType)}/api-connection/${encodeURIComponent(actionType)}` });
        return result || 0;
    }

    /**
     * Builds a custom window for fileManagers inside of the HTML editor
     * @param {any} fileManagerWindowSender The fileManager window used to set all the settings.
     * @param {any} fileManagerWindowMode The fileManagerWindowMode execution type (ex. images, files or translations).
     * @param {any} iframeMode Check if its run in an IFrame.
     * @param {any} gridviewMode Check if its run in an GridView.
     * @param {any} moduleName The name of the module where the action is executed in.
     */
    static initializeFileManager(fileManagerWindowSender, fileManagerWindowMode, iframeMode, gridviewMode, moduleName) {
        // File manager
        const fileManagerIframe = document.querySelector("#fileManagerFrame");
        const fileManagerWindow = $("#fileManagerWindow").kendoWindow({
            width: "90%",
            height: "90%",
            title: "",
            visible: false,
            modal: true,
            actions: ["Close"],
            open: (event) => {
                let selectedText = "";
                if (fileManagerWindowSender && fileManagerWindowSender.kendoEditor) {
                    selectedText = fileManagerWindowSender.kendoEditor.getSelection().toString();
                }

                fileManagerIframe.src = `/Modules/FileManager?mode=${fileManagerWindowMode}&iframe=true&selectedText=${encodeURIComponent(selectedText)}`;

                switch (fileManagerWindowMode) {
                    case this.fileManagerModes.images:
                        event.sender.title("Afbeelding invoegen");
                        break;
                    case this.fileManagerModes.files:
                        event.sender.title("Link naar bestand invoegen");
                        break;
                    case this.fileManagerModes.templates:
                        event.sender.title("Template invoegen");
                        break;
                }
            }
        }).data("kendoWindow");

        const fileManagerWindowAddButton = $("#fileManagerWindow button[name=addFileToEditor]").kendoButton({
            icon: "save",
            click: async (event) => {
                if (!fileManagerWindowSender) {
                    kendo.alert("Er is geen HTML editor gevonden waar dit bestand toegevoegd kan worden. Sluit aub dit scherm en probeer het opnieuw, of neem contact op met ons.");
                    return;
                }

                let html = "";

                if (!fileManagerIframe || !fileManagerIframe.contentWindow || !fileManagerIframe.contentWindow.document) {
                    kendo.alert("Het iframe voor bestandsbeheer kon niet gevonden worden of is leeg. Ververs a.u.b. de tab waar Wiser in draait en probeer het opnieuw, of neem contact op met ons.");
                    return;
                }

                const fileManagerClassFromIframe = fileManagerIframe.contentWindow.fileManager;

                switch (fileManagerWindowMode) {
                    case this.fileManagerModes.images: {
                        const selectedItem = fileManagerClassFromIframe.imagesUploaderWindowTreeView.dataItem(fileManagerClassFromIframe.imagesUploaderWindowTreeView.select());
                        const extension = selectedItem.name.substring(selectedItem.name.lastIndexOf(".") + 1);

                        const imagePreviewUrl = fileManagerClassFromIframe.generateImagePreviewUrl(extension);
                        html = `<figure>
                                    <picture>
                                        <source media="(min-width: 0px)" srcset="${imagePreviewUrl.url}" type="image/${extension}" />
                                        <source media="(min-width: 0px)" srcset="${fileManagerClassFromIframe.generateImagePreviewUrl('webp').url}" type="image/webp" />
                                        <img width="100%" height="auto" loading="lazy" src="${imagePreviewUrl.url}" alt="${imagePreviewUrl.altText}" />
                                    </picture>
                                </figure>`;
                        break;
                    }
                    case this.fileManagerModes.files: {
                        const fileUrl = fileManagerClassFromIframe.generateFilePreviewUrl();
                        html = `<a href="${fileUrl}">${(fileManagerClassFromIframe.filesUploaderWindow.element.find("#fileLinkText").val() || fileUrl)}</a>`;

                        break;
                    }
                    case this.fileManagerModes.templates: {
                        const selectedItem = fileManagerClassFromIframe.templatesUploaderWindowTreeView.dataItem(fileManagerClassFromIframe.templatesUploaderWindowTreeView.select());
                        html = selectedItem.html;
                        break;
                    }
                    default: {
                        kendo.alert(`Onbekende mode ('${this.fileManagerModes}') voor bestandsbeheer. Sluit a.u.b. dit scherm en probeer het opnieuw, of neem contact op met ons.`)
                        return;
                    }
                }

                if (fileManagerWindowSender.kendoEditor) {
                    const originalOptions = fileManagerWindowSender.kendoEditor.options.pasteCleanup;
                    fileManagerWindowSender.kendoEditor.options.pasteCleanup.none = true;
                    fileManagerWindowSender.kendoEditor.options.pasteCleanup.span = false;
                    fileManagerWindowSender.kendoEditor.exec("inserthtml", { value: html });
                    fileManagerWindowSender.kendoEditor.options.pasteCleanup.none = originalOptions.none;
                    fileManagerWindowSender.kendoEditor.options.pasteCleanup.span = originalOptions.span;
                }

                if (fileManagerWindowSender.codeMirror) {
                    const doc = fileManagerWindowSender.codeMirror.getDoc();
                    const cursor = doc.getCursor();
                    doc.replaceRange(html, cursor);
                }

                if (fileManagerWindowSender.contentbuilder) {
                    $(fileManagerWindowSender.contentbuilder.activeElement).replaceWith(html);
                }

                fileManagerWindow.close();
            }
        });

        // Window for searching for items to link to another item.
        const historyGridWindow = $("#historyWindowGrid").kendoWindow({
            width: "90%",
            height: "90%",
            title: "History",
            visible: false,
            modal: true,
            actions: ["Close"]
        }).data("kendoWindow");

        // Window for searching for items to link to another item.
        const searchItemsWindow = $("#searchItemsWindow").kendoWindow({
            width: "90%",
            height: "90%",
            title: "Item zoeken",
            visible: false,
            modal: true,
            actions: ["Close"]
        }).data("kendoWindow");

        // Some things should not be done if we're in iframe mode.
        if (iframeMode || gridviewMode) {
            return;
        }

        /***** NOTE: Only add code below this line that should NOT be executed if the module is loaded inside an iframe *****/
        const mainWindow = $("#window").kendoWindow({
            title: moduleName || "Modulenaam",
            visible: true,
            actions: ["refresh"]
        }).data("kendoWindow").maximize().open();
        mainWindow.wrapper.addClass("main-window");

        return fileManagerWindow;
    }

    /**
     * Replaces all div.diffField elements with diff2html diff interfaces.
     * @param {Element} container The container that will be searched for div.diffField elements.
     */
    static createHistoryDiffFields(container) {
        const Diff = require("diff");
        const pretty = require("pretty");

        let fields = container.querySelectorAll("div.diffField");
        for (let i = 0; i < fields.length; i++) {
            let field = fields[i];
            let oldValue = field.querySelector("span.oldValue")?.getAttribute("value");
            let newValue = field.querySelector("span.newValue")?.getAttribute("value");
            const fieldName = field.getAttribute("field-name");
            const dataType = field.getAttribute("data-type");
            switch (dataType) { // TemplateTypes enum
                case "Html": // Html is saved without whitespace in the database, so we need to make it readable first
                    oldValue = !oldValue ? "" : pretty(oldValue, { ocd: false });
                    newValue = !newValue ? "" : pretty(newValue, { ocd: false });
                    break;
                default:
                    oldValue = !oldValue ? "" : oldValue;
                    newValue = !newValue ? "" : newValue;
            }

            const diff = Diff.createTwoFilesPatch(fieldName, fieldName, oldValue, newValue);
            field.innerHTML = Diff2Html.html(diff, {
                drawFileList: false,
                matching: "words",
                outputFormat: "side-by-side"
            });

            field.classList.remove("diffField");
        }
    }
}

/**
 * Miscellaneous utils.
 */
export class Misc {
    static loadExternalScript(url) {
        return new Promise((resolve) => {
            const scriptEl = document.createElement("script");
            scriptEl.src = url;
            if (scriptEl.readyState) {  //IE
                scriptEl.onreadystatechange = () => {
                    if (scriptEl.readyState === "loaded" ||
                        scriptEl.readyState === "complete") {
                        scriptEl.onreadystatechange = null;
                        resolve();
                    }
                };
            } else {  //Others
                scriptEl.onload = resolve;
            }

            document.body.insertAdjacentElement("beforeEnd", scriptEl);
        });
    }

    /**
     * Loads CodeMirror if it's not loaded yet.
     * @return {Promise} A promise.
     */
    static ensureCodeMirror() {
        return new Promise((resolve) => {
            if (window.CodeMirror) {
                resolve(window.CodeMirror);
                return;
            }

            const codeMirrorPromises = [
                import("codemirror"),
                import("codemirror/mode/css/css.js"),
                import("codemirror/mode/xml/xml.js"),
                import("codemirror/mode/htmlmixed/htmlmixed.js"),
                import("codemirror/mode/javascript/javascript.js"),
                import("codemirror/mode/sql/sql.js"),
                import("codemirror/addon/hint/show-hint.js"),
                import("codemirror/addon/hint/css-hint.js"),
                import("codemirror/addon/hint/xml-hint.js"),
                import("codemirror/addon/hint/html-hint.js"),
                import("codemirror/addon/hint/javascript-hint.js"),
                import("codemirror/addon/edit/matchbrackets.js"),
                import("codemirror/addon/display/fullscreen.js"),
                import("codemirror/addon/fold/foldcode.js"),
                import("codemirror/addon/fold/foldgutter.js"),
                import("codemirror/addon/fold/brace-fold.js"),
                import("codemirror/addon/fold/xml-fold.js"),
                import("codemirror/addon/fold/comment-fold.js"),
                import("codemirror/addon/lint/lint.js"),
                import("jshint"),
                import("codemirror/addon/lint/javascript-lint.js"),
                import("csslint"),
                import("codemirror/addon/lint/css-lint.js"),
                import("codemirror/addon/search/searchcursor.js"),
                import("codemirror/addon/search/search.js"),
                import("codemirror/addon/dialog/dialog.js"),
                import("./codemirror/scsslint.js"),
                import("./codemirror/scss-lint.js"),

                import("codemirror/lib/codemirror.css"),
                import("codemirror/addon/display/fullscreen.css"),
                import("codemirror/addon/fold/foldgutter.css"),
                import("codemirror/addon/lint/lint.css"),
                import("codemirror/addon/hint/show-hint.css"),
                import("codemirror/addon/dialog/dialog.css")
            ];

            Promise.all(codeMirrorPromises).then((modules) => {
                window.CodeMirror = modules[0];
                if (typeof window.CodeMirror.fromTextArea !== "function") {
                    window.CodeMirror = window.CodeMirror.default;
                }
                window.JSHINT = modules[19].JSHINT;
                window.CSSLint = modules[21].CSSLint;
                resolve(window.CodeMirror);
                return;
            });
        });
    }

    async printDymoLabel(stringToPrint, labelXml) {
        await loadExternalScript("/scripts/labelwriter/DYMO.Label.Framework.js");

        if (labelXml === "") {
            // Specify Label Layout to Print
            labelXml = '<DieCutLabel Version="8.0" Units="twips"> \
                        <PaperOrientation>Landscape</PaperOrientation> \
                        <Id>Address</Id> \
                        <PaperName>30252 Address</PaperName> \
                        <DrawCommands> \
                            <RoundRectangle X="0" Y="0" Width="2040" Height="5000" Rx="270" Ry="270" /> \
                        </DrawCommands> \
                        <ObjectInfo> \
                            <TextObject> \
                                <Name>stringToPrint</Name> \
                                <ForeColor Alpha="255" Red="0" Green="0" Blue="0" /> \
                                <BackColor Alpha="0" Red="255" Green="255" Blue="255" /> \
                                <LinkedObjectName></LinkedObjectName> \
                                <Rotation>Rotation0</Rotation> \
                                <IsMirrored>False</IsMirrored> \
                                <IsVariable>True</IsVariable> \
                                <HorizontalAlignment>Left</HorizontalAlignment> \
                                <VerticalAlignment>Top</VerticalAlignment> \
                                <TextFitMode>ShrinkToFit</TextFitMode> \
                                <UseFullFontHeight>True</UseFullFontHeight> \
                                <Verticalized>False</Verticalized> \
                                <StyledText> \
                                    <Element> \
                                        <String xml:space="preserve">[stringToPrint]</String> \
                                        <Attributes> \
                                            <Font Family="Arial" Size="14" Bold="False" Italic="False" Underline="False" Strikeout="False" /> \
                                            <ForeColor Alpha="255" Red="0" Green="0" Blue="0" HueScale="100" /> \
                                        </Attributes> \
                                    </Element> \
                                </StyledText> \
                            </TextObject> \
                            <Bounds X="400" Y="50" Width="4500" Height="2000" /> \
                        </ObjectInfo> \
                     </DieCutLabel>';
        }
        let label = dymo.label.framework.openLabelXml(labelXml);

        // Setting Data to Print
        label.setObjectText("stringToPrint", stringToPrint);

        // Selecting the Printer to Print on
        let printers = dymo.label.framework.getPrinters();
        if (printers.length === 0) {
            Wiser.showMessage({
                title: "Geen printer gevonden",
                content: "No DYMO printers are installed. Install DYMO printers."
            });
        }

        let printerName = "";
        for (let i = 0; i < printers.length; ++i) {
            let printer = printers[i];
            if (printer.printerType === "LabelWriterPrinter") {
                printerName = printer.name;
                break;
            }
        }

        // Actual Printing
        label.print(printerName);
    }

    static async downloadFile(fetchResult, fileName) {
        const pdfBlob = await fetchResult.blob();
        const pdfUrl = window.URL.createObjectURL(pdfBlob);

        const anchor = document.createElement("a");
        anchor.href = pdfUrl;
        anchor.download = fileName;
        document.body.appendChild(anchor);
        anchor.click();
        document.body.removeChild(anchor);
        window.URL.revokeObjectURL(pdfUrl);
    }

    static addEventToFixToolTipPositions(toolTipSelector = ".info") {
        const body = $(document.body);
        body.on("mouseenter", toolTipSelector, (event) => {
            const toolTip = event.currentTarget;
            const rectangle = toolTip.getBoundingClientRect();
            const isOutOfBoundsRight = body.width() - rectangle.right - 350 <= 0;
            if (isOutOfBoundsRight) {
                toolTip.classList.add("tooltip-left");
            } else {
                toolTip.classList.remove("tooltip-left");
            }
        });
    }

    /**
     * Flattens an object with a finite amount of nested objects into a one-dimensional object. The keys of all
     * properties will be transformed by prefixing them with the path leading to the original property separated by a
     * period.
     * @param originalRootObject The object to flatten.
     * @param writeRootObject (Internal) used during recursion to refer back to the object to flatten.
     * @param currentPath (Internal) used during recursion to keep track of the current path leading to the object that
     * is being flattened.
     * @returns {{Object}} A copy of the given object represented in a flattened state.
     */
    static flattenObject(originalRootObject, writeRootObject = {}, currentPath = null) {
        // Get the current path in the recursion as segments in an array.
        const pathSegments = currentPath !== null ? currentPath.split('.') : [];
        // Get the current object in the recursion given by the path.
        let currentObject = originalRootObject;
        for(let pathSegment of pathSegments)
            currentObject = currentObject[pathSegment];

        // List all property names.
        const keys = Object.keys(currentObject);
        // Loop through all properties in the current object of the recursion.
        for(let key of keys) {
            // Get the absolute path of the property.
            const propertyPath = (currentPath !== null ? currentPath + "." : '') + key;
            
            // Get the value of the property.
            const propertyValue = currentObject[key];
            
            // Check whether the property is an object. If so, perform a new recursion iteration over this object.
            // Otherwise, flatten the property by saving its value and path (as key) to the object that will be
            // returned.
            if(propertyValue instanceof Object && !Array.isArray(propertyValue)) {
                writeRootObject = this.flattenObject(originalRootObject, writeRootObject, propertyPath);
            } else {
                writeRootObject[propertyPath] = propertyValue;
            }
        }
        
        // Give back the object in it's current state.
        // Returning is necessary, since adding new properties to objects is not by reference, but by value.
        return writeRootObject;
    }
    
    static encodeHtml(input) {
        let encodedInput = input.replace(/[<>&]/g, function(match) {
            switch(match) {
                case '<': return '&lt;';
                case '>': return '&gt;';
                case '&': return '&amp;';
            }
        });

        encodedInput = encodedInput.replace(/[\u00A0-\u9999]/g, function(match) {
            return '&#' + match.charCodeAt(0) + ';';
        });
        
        return encodedInput;
    }
    
    static decodeHtml(input) {
        let decodedInput = input.replace(/&#\d+;/g, function(match) {
            return String.fromCharCode(match.match(/\d+/g)[0]);
        });
        
        decodedInput = decodedInput.replace(/(&lt;|&gt;|&ampt;)/g, function(match) {
            switch(match) {
                case '&lt;': return '<';
                case '&gt;': return '>';
                case '&amp;': return '&';
            }
        });
        
        return decodedInput;
    }
}

// Make the classes globally available, so that they also work in scripts that are not loaded via Webpack.
window.modules = Modules;
window.Dates = Dates;
window.Strings = Strings;
window.Wiser = Wiser;
window.Misc = Misc;
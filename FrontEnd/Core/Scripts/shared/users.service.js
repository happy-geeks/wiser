﻿import BaseService from "./base.service";

export default class UsersService extends BaseService {
    /**
     * Sends a login request to the API and returns the result.
     * If an error occurred, it will return a friendly user message.
     * @param {string} username The username.
     * @param {string} password The password.
     * @param {string} selectedUser If an admin account is logging in, add the username they selected here.
     * @returns {any} An object that looks like this: { success: true, message: "", data: {}}
     */
    async loginUser(username, password, selectedUser) {
        const result = {};

        try {
            const loginData = new URLSearchParams();
            loginData.append("grant_type", "password");
            loginData.append("username", username);
            loginData.append("password", password);
            loginData.append("subDomain", this.base.appSettings.subDomain);
            loginData.append("client_id", this.base.appSettings.apiClientId);
            loginData.append("client_secret", this.base.appSettings.apiClientSecret);
            loginData.append("isTestEnvironment", this.base.appSettings.isTestEnvironment);
            if (selectedUser) {
                loginData.append("selectedUser", selectedUser);
            }

            const loginResult = await this.base.api.post(`/connect/token`, loginData);
            result.success = true;
            result.data = loginResult.data;
            result.data.expiresOn = new Date(new Date().getTime() + (loginResult.data.expires_in * 1000));
            result.data.usersList = JSON.parse(result.data.users || "[]").map((user) => {
                if (!user.Details || !user.Details.length) {
                    return user;
                }

                for (let detail of user.Details) {
                    user[detail.Key] = detail.Value;
                }

                return user;
            });
            result.data.adminLogin = result.data.adminLogin === "true" || result.data.adminLogin === true;
        } catch (error) {
            result.success = false;
            console.error("Error during login", error);

            if (error.response) {
                console.warn(error.response);
                // The request was made and the server responded with a status code
                // that falls out of the range of 2xx
                if (error.response.status !== 400 || error.response.data.error === "server_error") {
                    result.message = "Er is een onbekende fout opgetreden tijdens het inloggen. Probeer het a.u.b. nogmaals of neem contact op met ons.";
                } else {
                    result.message = "U heeft ongeldige gegevens ingevuld. Probeer het a.u.b. opnieuw.";
                }
            } else if (error.request) {
                // The request was made but no response was received
                // `error.request` is an instance of XMLHttpRequest in the browser and an instance of
                // http.ClientRequest in node.js
                console.warn(error.request);
                result.message = "Er is een onbekende fout opgetreden tijdens het inloggen. Probeer het a.u.b. nogmaals of neem contact op met ons.";
            } else {
                // Something happened in setting up the request that triggered an Error
                console.warn(error.message);
                result.message = "Er is een onbekende fout opgetreden tijdens het inloggen. Probeer het a.u.b. nogmaals of neem contact op met ons.";
            }
        }

        return result;
    }
    /**
     * Refreshes the user login via a refresh token that was supplied by the API.
     * @param {string} refreshToken The refresh token.
     * @returns {any} An object that looks like this: { success: true, message: "", data: {}}
     */
    async refreshToken(refreshToken) {
        const result = {};

        try {
            const loginData = new URLSearchParams();
            loginData.append("grant_type", "refresh_token");
            loginData.append("refresh_token", refreshToken);
            loginData.append("subDomain", this.base.appSettings.subDomain);
            loginData.append("client_id", this.base.appSettings.apiClientId);
            loginData.append("client_secret", this.base.appSettings.apiClientSecret);
            loginData.append("isTestEnvironment", this.base.appSettings.isTestEnvironment);

            const loginResult = await this.base.api.post(`/connect/token`, loginData);
            result.success = true;
            result.data = loginResult.data;
            result.data.expiresOn = new Date(new Date().getTime() + (loginResult.data.expires_in * 1000));
            result.data.adminLogin = result.data.adminLogin === "true" || result.data.adminLogin === true;
        } catch (error) {
            result.success = false;
            console.error("Error during login", error);

            if (error.response) {
                console.warn(error.response);
                // The request was made and the server responded with a status code
                // that falls out of the range of 2xx
                if (error.response.status !== 400 || error.response.data.error === "server_error") {
                    result.message = "Er is een onbekende fout opgetreden tijdens het inloggen. Probeer het a.u.b. nogmaals of neem contact op met ons.";
                } else {
                    result.message = "U heeft ongeldige gegevens ingevuld. Probeer het a.u.b. opnieuw.";
                }
            } else if (error.request) {
                // The request was made but no response was received
                // `error.request` is an instance of XMLHttpRequest in the browser and an instance of
                // http.ClientRequest in node.js
                console.warn(error.request);
                result.message = "Er is een onbekende fout opgetreden tijdens het inloggen. Probeer het a.u.b. nogmaals of neem contact op met ons.";
            } else {
                // Something happened in setting up the request that triggered an Error
                console.warn(error.message);
                result.message = "Er is een onbekende fout opgetreden tijdens het inloggen. Probeer het a.u.b. nogmaals of neem contact op met ons.";
            }
        }

        return result;
    }
    
    /**
     * Get the data of the logged in user.
     * @returns {any} The user data as an object.
     */
    async getLoggedInUserData() {
        let result = {};

        try {
            result = sessionStorage.getItem("userSettings");
            if (result) {
                const sessionData = JSON.parse(result);
                if (sessionData.dateTime && new Date() - new Date(sessionData.dateTime) < 3600000) {
                    // Only use the data from session if it's less than 1 hour old (1000 milliseconds * 60 seconds * 60 minutes).
                    result = sessionData.data;
                } else {
                    result = null;
                }
            }

            if (!result) {
                const response = await this.base.api.get(`/api/v3/users/self`);
                result = response.data;
                if (result) {
                    sessionStorage.setItem("userSettings", JSON.stringify({ dateTime: new Date(), data: result }));
                }
            }

            result.success = true;
            result.data = result;
        } catch (error) {
            result.success = false;
            console.error("Error getLoggedInUserData", error);
            result.message = "Er is een onbekende fout opgetreden tijdens het ophalen van informatie over de ingelogde gebruiker. Probeer het a.u.b. nogmaals of neem contact op met ons.";

            if (error.response) {
                // The request was made and the server responded with a status code
                // that falls out of the range of 2xx
                console.warn(error.response);
            } else if (error.request) {
                // The request was made but no response was received
                // `error.request` is an instance of XMLHttpRequest in the browser and an instance of
                // http.ClientRequest in node.js
                console.warn(error.request);
            } else {
                // Something happened in setting up the request that triggered an Error
                console.warn(error.message);
            }
        }

        return result;
    }

    async forgotPassword(username, email) {
        try {
            await this.base.api.put(`/api/v3/users/reset-password`, { username: username, emailAddress: email, subDomain: this.base.appSettings.subDomain });
            return true;
        } catch (error) {
            console.error(error);
            return false;
        }
    }

    async changePassword(changePasswordModel) {
        const result = {};

        try {
            result.response = await this.base.api.put(`/api/v3/users/password`, { 
                oldPassword: changePasswordModel.oldPassword, 
                newPassword: changePasswordModel.newPassword, 
                newPasswordRepeat: changePasswordModel.newPasswordRepeat 
            });
        } catch (error) {
            if ((error.response.status !== 400 && error.response.status !== 401) || error.response.data.error === "server_error") {
                result.error = "Er is een onbekende fout opgetreden tijdens het wijzigen van uw wachtwoord. Probeer het a.u.b. nogmaals of neem contact op met ons.";
            } else {
                result.error = "U heeft ongeldige gegevens ingevuld. Probeer het a.u.b. opnieuw.";
            }
        }

        return result;
    }

    async savePinnedModules(moduleIds) {
        try {
            await this.base.api.post(`/api/v3/users/pinned-modules`, moduleIds);
            return true;
        } catch (error) {
            console.error(error);
            return false;
        }
    }

    async saveAutoLoadModules(moduleIds) {
        try {
            await this.base.api.post(`/api/v3/users/auto-load-modules`, moduleIds);
            return true;
        } catch (error) {
            console.error(error);
            return false;
        }
    }
}
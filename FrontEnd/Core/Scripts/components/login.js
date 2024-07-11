import {AUTH_LOGOUT, AUTH_REQUEST, CHANGE_PASSWORD_LOGIN, FORGOT_PASSWORD} from "../store/mutation-types";
import {ComboBox} from "@progress/kendo-vue-dropdowns";
import {UserManager, WebStorageStateStore} from 'oidc-client';

export default {
    name: "login",
    template: "#login-template",
    props: ["subDomain", "mainLoader"],
    data() {
        return {
            loginForm: {
                username: "",
                password: "",
                rememberMe: true,
                selectedUser: "",
                capslock: false,
                totpPin: "",
                totpBackupCode: ""
            },
            users: null,
            showForgotPasswordScreen: false,
            forgotPasswordForm: {
                username: "",
                email: ""
            },
            changePasswordForm: {
                newPassword: "",
                newPasswordRepeat: ""
            },
            showTotpBackupCodeScreen: false,
            config: {
                authority: 'https://localhost:44349',
                client_id: 'js_client',
                redirect_uri: 'https://localhost:44377/?loginCallback=true',
                response_type: 'id_token token',
                scope: 'openid profile api1',
                post_logout_redirect_uri: 'https://localhost:44377/',
                userStore: new WebStorageStateStore({ store: window.localStorage })
            },
            userManager: null
        };
    },
    async created() {
        await this.$store.dispatch(AUTH_REQUEST);
        this.userManager = new UserManager(this.config);

        function paramsToObject(entries) {
            const result = {}
            for(const [key, value] of entries) { // each 'entry' is a [key, value] tupple
                result[key] = value;
            }
            return result;
        }

        const params = new URLSearchParams(window.location.search);
        const isCallback = params.get("loginCallback") === "true";
        if (isCallback) {
            this.userManager.signinRedirectCallback().then(function () {
                const params2 = new URLSearchParams(window.location.hash.substring(1));
                console.log("isCallback", params, params2);
// https://localhost:44377/?loginCallback=true
// #id_token=eyJhbGciOiJSUzI1NiIsImtpZCI6IkExMThCNTVGQzc1QjEzQkU3QTQ0OTY3RUYxOTEzNTFEIiwidHlwIjoiSldUIiwiY3R5IjoiSldUIn0.eyJuYmYiOjE3MjA3MDQ2MzIsImV4cCI6MTcyMDcwNDkzMiwiaXNzIjoiaHR0cHM6Ly9sb2NhbGhvc3Q6NDQzNDkiLCJhdWQiOiJqc19jbGllbnQiLCJub25jZSI6Ijk1YzIxMmMwNTJkMjQ1NmE4OTJkODEzNGVjMDIyMTNjIiwiaWF0IjoxNzIwNzA0NjMyLCJhdF9oYXNoIjoiNnNYU0tZTVQxQUMxZmhOcU1YN3NfQSIsInNfaGFzaCI6InY4U2M2Z1NOdGdEVkVSWVM3UGJsX1EiLCJzaWQiOiI0OUYxREVERkY0RTQzMzI2MDBBNkU2QTkzNDNBRDc4MiIsInN1YiI6IjEwMDg4NDg3MjU0MjAxNjEwNzIzNSIsImF1dGhfdGltZSI6MTcyMDcwNDE5MSwiaWRwIjoiR29vZ2xlIiwiYW1yIjpbImV4dGVybmFsIl19.lbbvmLv4nH1vlZCRzSjus1jB5SYzq5Px11EU5S7KdzKldd9TXr7pIysbv3mDqVOz2fGMtfpEdRweWaeNjXHmpdrH45uqx5V3LcOXybRVTr83SgwyEY6yjhPDy2QEAa-mIkvo4wkcZQiIMflYBUURxK7efVDG65DXFXPrAlohFKeTrrP_5CXUklPVV6tiT3E1VXk871K-CMMJl0yG66RSHI7qn5jhx7mN3zfRNoBeGSGxGT6qh6oFLI4HvRppgLAnstBt1F2CYqmq6qDJMzfi7W-57YCAij3r25ALQVboVWNE-ddYTzWa7jjAkAvevLNz0YyxfQA82PbvZq9iOLsxVQ
// &access_token=eyJhbGciOiJSUzI1NiIsImtpZCI6IkExMThCNTVGQzc1QjEzQkU3QTQ0OTY3RUYxOTEzNTFEIiwidHlwIjoiYXQrand0IiwiY3R5IjoiSldUIn0.eyJuYmYiOjE3MjA3MDQ2MzIsImV4cCI6MTcyMDcwODIzMiwiaXNzIjoiaHR0cHM6Ly9sb2NhbGhvc3Q6NDQzNDkiLCJjbGllbnRfaWQiOiJqc19jbGllbnQiLCJzdWIiOiIxMDA4ODQ4NzI1NDIwMTYxMDcyMzUiLCJhdXRoX3RpbWUiOjE3MjA3MDQxOTEsImlkcCI6Ikdvb2dsZSIsIm5hbWUiOiJHaWxpYW4gS2V1bGVucyIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL25hbWVpZGVudGlmaWVyIjoiMTAwODg0ODcyNTQyMDE2MTA3MjM1IiwiaHR0cDovL3NjaGVtYXMueG1sc29hcC5vcmcvd3MvMjAwNS8wNS9pZGVudGl0eS9jbGFpbXMvbmFtZSI6IkdpbGlhbiBLZXVsZW5zIiwiaHR0cDovL3NjaGVtYXMueG1sc29hcC5vcmcvd3MvMjAwNS8wNS9pZGVudGl0eS9jbGFpbXMvZ2l2ZW5uYW1lIjoiR2lsaWFuIiwiaHR0cDovL3NjaGVtYXMueG1sc29hcC5vcmcvd3MvMjAwNS8wNS9pZGVudGl0eS9jbGFpbXMvc3VybmFtZSI6IktldWxlbnMiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9lbWFpbGFkZHJlc3MiOiJnaWxpYW5rZXVsZW5zQGhhcHB5aG9yaXpvbi5jb20iLCJqdGkiOiI1MTFFMUE2N0U4Q0Y0RkJCMDY1MTY2RjkxQkY0MTk2QSIsInNpZCI6IjQ5RjFERURGRjRFNDMzMjYwMEE2RTZBOTM0M0FENzgyIiwiaWF0IjoxNzIwNzA0NjMyLCJzY29wZSI6WyJvcGVuaWQiLCJwcm9maWxlIiwiYXBpMSJdLCJhbXIiOlsiZXh0ZXJuYWwiXX0.czO568TAukeGqoijQSn_bVKjF5G5fB5jE7Js-93y7Znvz82tFxcSa6hOar-PlDK_VP3eD8cOfTLw_C3HLxzrSyETrKpfJ_fzc32Ffk8YC6qDhgq2pOITUjQK7qsk0dhFG15GSDIIqgPOA7gIVpoLgWFxAyt4v5ZRjweEgz3Z4h2r22TCEMVNPAhXmXWyMYBH_ZFaM6VEGKHWvyi-Lp2q07UiiPh80pygBmAWK-fVb6lNF87reulPfPiQHndd1rwg19zSsQZEcB3viD_RryY90sS60EBTjbaEv9DuuHsGCwEfzkdslXr6Q0RGW65F5-m3i5W22BPXDldQqa5p7r4jXQ
// &token_type=Bearer
// &expires_in=3600
// &scope=openid%20profile%20api1
// &state=732a5b477570497ea19a7eb8eaf29a2f
// &session_state=TBZmY26-ThxLdeChMutUCBKWQC9nzcDnwblkVfvjPRQ.B73F071116EA74FD2B8FAC242CE5DC7F
                localStorage.setItem("accessToken", params2.get("access_token"));
                let date = new Date();
                date.setSeconds(date.getSeconds() + parseInt(params2.get("expires_in")));
                localStorage.setItem("accessTokenExpiresOn", date);
                localStorage.setItem("userData", JSON.stringify(paramsToObject(params2)));
                window.location = "/";
            }).catch(function (e) {
                console.error(e);
            });
        }
    },
    computed: {
        loginStatus() {
            return this.$store.state.login.loginStatus;
        },

        message() {
            return this.$store.state.login.loginMessage;
        },

        listOfUsers() {
            if (this.users === null) {
                this.users = this.$store.state.login.listOfUsers;
            }

            return this.users;
        },

        resetPassword() {
            return this.$store.state.login.resetPassword;
        },

        requirePasswordChange() {
            return this.$store.state.login.requirePasswordChange;
        },

        tenantTitle() {
            return this.$store.state.tenants.title;
        },

        validSubDomain() {
            return this.$store.state.tenants.validSubDomain;
        },

        totpQrImageUrl() {
            return this.$store.state.login.totpQrImageUrl;
        },

        user() {
            return this.$store.state.login.user;
        }
    },
    components: {
        "combobox": ComboBox
    },
    methods: {
        async login(event) {
            event.preventDefault();

            if (this.showForgotPasswordScreen) {
                if (this.forgotPasswordForm.username.length === 0 || this.forgotPasswordForm.email.length === 0) {
                    return;
                }

                await this.$store.dispatch(FORGOT_PASSWORD, {user: Object.assign({}, this.forgotPasswordForm)});
                return;
            } else if (this.requirePasswordChange) {
                if (event.submitter.id === "submitPasswordChange") {
                    await this.$store.dispatch(CHANGE_PASSWORD_LOGIN, { user: Object.assign({},
                            {
                                oldPassword: this.loginForm.password,
                                newPassword: this.changePasswordForm.newPassword,
                                newPasswordRepeat: this.changePasswordForm.newPasswordRepeat
                            }) });
                } else {
                    this.$store.dispatch(AUTH_LOGOUT);
                }

                return;
            }

            // Don't try a new login request if one is still running.
            if (this.loginStatus === "loading") {
                return;
            }

            await this.$store.dispatch(AUTH_REQUEST, {
                user: Object.assign({}, this.loginForm),
                loginStatus: this.loginStatus
            });

            if (this.loginStatus === "error") {
                this.loginForm.selectedUser = "";
                this.loginForm.password = "";
            }
            else if (this.users && this.users.length > 0) {
                this.loginForm.selectedUser = this.users[0];
            }
        },

        async logout() {
            await this.$store.dispatch(AUTH_LOGOUT);
            this.toggleTotpBackupCodeScreen(false);
        },

        userFilterChange(event) {
            this.users = [];
            this.$store.state.login.listOfUsers.forEach(user => {
                if (user.Title.toLowerCase().includes(event.filter.value.toLowerCase())) {
                    this.users.push(user);
                }
            });
        },

        togglePassword() {
            const password = document.querySelector("#password");

            // toggle the type attribute
            const type = password.getAttribute("type") === "password" ? "text" : "password";
            password.setAttribute("type", type);
        },

        togglePasswordForgottenScreen(show) {
            this.showForgotPasswordScreen = show;
        },

        toggleTotpBackupCodeScreen(show) {
            this.showTotpBackupCodeScreen = show;
        },

        async forgotPassword() {
            if (this.forgotPasswordForm.username.length === 0 || this.forgotPasswordForm.email.length === 0) {
                return;
            }

            await this.$store.dispatch(FORGOT_PASSWORD, { user: Object.assign({}, this.forgotPasswordForm) });
        },

        checkCapslock(e) {
            this.loginForm.capslock = e.getModifierState('CapsLock');
        },

        async googleSignInButtonClick() {
            //https://localhost:5001/connect/authorize?client_id=your-client-id&response_type=code&scope=openid profile api1&redirect_uri=https://localhost:5002/signin-oidc&state={state}&nonce={nonce}&code_challenge={codeChallenge}&code_challenge_method=S256&acr_values=idp:Google
            /*const authUrl = "https://localhost:44349/connect/authorize" +
                "?client_id=google-test" +
                "&response_type=code" +
                "&scope=openid profile wiser-api" +
                "&redirect_uri=" + encodeURIComponent("https://localhost:44377/signin-oidc") +
                "&state=" + encodeURIComponent(Math.random().toString(36).substring(7)) +
                "&nonce=" + encodeURIComponent(Math.random().toString(36).substring(7)) +
                "&code_challenge=" + encodeURIComponent(window.main.appSettings.codeChallenge) +
                "&code_challenge_method=S256" +
                "&acr_values=idp:Google";

            //window.location.href = authUrl;
            window.location.href = `https://localhost:44349/api/v3/users/external-login?provider=Google`;*/
            debugger;
            const loginResult = await this.login2();
            console.log("loginResult", loginResult);
        },

        login2() {
            return this.userManager.signinRedirect();
        },

        logout2() {
            return this.userManager.signoutRedirect();
        },

        getUser2() {
            return this.userManager.getUser();
        },

        renewToken2() {
            return this.userManager.signinSilent();
        }
    }
};
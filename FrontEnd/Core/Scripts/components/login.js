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


        const params = new URLSearchParams(window.location.search);
        const isCallback = params.get("loginCallback") === "true";
        if (isCallback) {
            console.log("isCallback", params);
            this.userManager.signinRedirectCallback().then(function () {
                window.location = "index.html";
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

        googleSignInButtonClick() {
            debugger;
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
            const loginResult = this.login2();
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
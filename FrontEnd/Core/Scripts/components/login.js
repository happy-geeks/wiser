import { AUTH_REQUEST, AUTH_LOGOUT, FORGOT_PASSWORD, CHANGE_PASSWORD } from "../store/mutation-types";
import { ComboBox } from "@progress/kendo-vue-dropdowns";

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
                capslock: false
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
            }
        };
    },
    async created() {
        await this.$store.dispatch(AUTH_REQUEST);

        if (this.$store.state.login.requirePasswordChange && this.loginForm.password === "") {
            //this.$store.dispatch(AUTH_LOGOUT);
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

        customerTitle() {
            return this.$store.state.customers.title;
        },

        validSubDomain() {
            return this.$store.state.customers.validSubDomain;
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

                await this.$store.dispatch(FORGOT_PASSWORD, { user: Object.assign({}, this.forgotPasswordForm) });
                return;
            } else if (this.requirePasswordChange) {
                if (event.submitter.id === "submitPasswordChange") {
                    await this.$store.dispatch(CHANGE_PASSWORD, { user: Object.assign({},
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

            await this.$store.dispatch(AUTH_REQUEST, { user: Object.assign({}, this.loginForm) });
            if (this.loginStatus === "error") {
                this.loginForm.selectedUser = "";
                this.loginForm.password = "";
            }
            else {
                this.loginForm.selectedUser = this.users[0].username;
                console.log('eerste waarde:' + this.users[0].username);
                console.log('selectedUser in loginForm:' + this.loginForm.selectedUser);
            }
        },

        logout() {
            this.$store.dispatch(AUTH_LOGOUT);
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

        async forgotPassword() {
            if (this.forgotPasswordForm.username.length === 0 || this.forgotPasswordForm.email.length === 0) {
                return;
            }

            await this.$store.dispatch(FORGOT_PASSWORD, { user: Object.assign({}, this.forgotPasswordForm) });
        },

        checkCapslock(e) {
            this.loginForm.capslock = e.getModifierState('CapsLock');
        }
    }
};
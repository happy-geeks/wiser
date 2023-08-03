import "../../Scss/customer-management.scss";

export default {
    name: "customerManagement",
    template: "#customer-management-template",
    props: ["subDomain"],
    data() {
        return {
            currentStep: 1,
            lastStep: 2,
            message: "",
            errors: [],
            loading: false,
            newCustomerData: {
                name: "",
                subDomain: "",
                isWebShop: false,
                isConfigurator: false,
                isMultiLanguage: false,
                createNewDatabase: true,
                digitalOceanApiAccessToken: "",
                databaseHost: "",
                databaseUsername: "",
                databasePassword: "",
                databaseSchema: "",
                databasePort: 25060,
                dbClusterChoice: "",
                dataUrlLive: "",
                dataUrlTest: "",
                hostLive: "",
                hostTest: "",
                hostDev: "",
                siteName: ""
            },
            createCustomerResult: {
                data: {}
            },
            availableDbClusters: []
        };
    },
    async created() {
    },
    computed: {
        nextButtonText() {
            return this.currentStep >= this.lastStep ? "Opslaan" : "Verder";
        }
    },
    methods: {
        previousStep(event) {
            event.preventDefault();
            this.currentStep = this.currentStep > 1 ? this.currentStep - 1 : 1;
        },

        async nextStep(event) {
            this.message = "";
            if (!event.target.closest("form").checkValidity()) {
                return;
            }

            event.preventDefault();

            switch (this.currentStep) {
            case 1:
                if (!await this.handleStep1()) {
                    return;
                }
                break;
            case 2:
                if (!await this.handleStep2()) {
                    return;
                }

                this.currentStep = "end";
                return;
            default:
                console.warn("Unknown/invalid step", this.currentStep);
                break;
            }

            this.currentStep++;
        },

        reset() {
            this.message = "";
            this.errors = [];
            this.newCustomerData = {
                name: "",
                subDomain: "",
                isWebShop: false,
                isConfigurator: false,
                isMultiLanguage: false,
                createNewDatabase: true,
                digitalOceanApiAccessToken: "",
                databaseHost: "",
                databaseUsername: "",
                databasePassword: "",
                databaseSchema: "",
                databasePort: 25060,
                dbClusterChoice: "",
                dataUrlLive: "",
                dataUrlTest: "",
                hostLive: "",
                hostTest: "",
                hostDev: "",
                siteName: ""
            };
            this.availableDbClusters = [];
            this.currentStep = 1;
        },

        async getDbClusters() {
            this.message = "";
            this.errors = [];
            this.loading = true;
            const result = await main.customersService.getDbClusters(this.newCustomerData.digitalOceanApiAccessToken);
            this.loading = false;
            if (!result.success) {
                this.message = result.message;
            } else {
                this.availableDbClusters = result.data.databases;
            }
        },

        async createDatabaseAndUser() {
            this.loading = true;
            const result = await main.customersService.createDatabaseAndUser(this.newCustomerData.dbClusterChoice, this.newCustomerData.databaseSchema, this.newCustomerData.databaseUsername, this.newCustomerData.digitalOceanApiAccessToken);
            this.loading = false;
            if (!result.success) {
                this.message = result.message;
                return result;
            }

            for (let user of result.data.users) {
                if (user.name.endsWith("_wiser")) {
                    this.newCustomerData.databasePassword = user.password;
                    this.newCustomerData.databaseUsername = user.name;
                    break;
                }
            }

            this.newCustomerData.databaseHost = result.data.cluster.database.connection.host;
            this.newCustomerData.databasePort = result.data.cluster.database.connection.port;
            return result;
        },

        async handleStep1() {
            this.message = "";
            this.errors = [];
            this.loading = true;
            const result = await main.customersService.exists(this.newCustomerData.name, this.newCustomerData.subDomain);
            this.loading = false;

            if (!result.success) {
                this.message = result.message;
                return false;
            }

            if (result.data === "Available") {
                return true;
            }

            this.errors = (result.data || "").split(", ");
            return false;
        },

        async handleStep2() {
            let databaseResult;
            if (this.newCustomerData.createNewDatabase) {
                if (!this.newCustomerData.dbClusterChoice) {
                    this.message = "Kies eerst een databasecluster";
                    return false;
                }

                databaseResult = await this.createDatabaseAndUser();
                if (!databaseResult.success) {
                    return false;
                }
            } else {
                databaseResult = {
                    data: {
                        users: [
                            {
                                name: this.newCustomerData.databaseUsername,
                                password: this.newCustomerData.databasePassword
                            }
                        ]
                    }
                };
            }

            const newCustomer = {
                name: this.newCustomerData.name,
                database: {
                    host: this.newCustomerData.databaseHost,
                    username: this.newCustomerData.databaseUsername,
                    password: this.newCustomerData.databasePassword,
                    databaseName: this.newCustomerData.databaseSchema,
                    portNumber: this.newCustomerData.databasePort
                },
                subDomain: this.newCustomerData.subDomain,
                wiserSettings: {
                    hostLive: this.newCustomerData.hostLive,
                    hostTest: this.newCustomerData.hostTest,
                    hostDev: this.newCustomerData.hostDev,
                    siteName: this.newCustomerData.siteName
                },
                properties: {
                    imagespath: "images;images",
                    flashpath: "flash;flash",
                    documentpath: "documents;documents",
                    templatespath: "templates;templates",
                    imagesRootID: "1",
                    filesRootID: "2",
                    templatesRootID: "3",
                    flashRootID: "4",
                    mediaRootID: "5",
                    linkreplace: "/{pagename}/",
                    XHTML: "false",
                    enableSEO: "false",
                    urlstolower: "true",
                    canredirectwebpage: "false",
                    loadatonce: "false",
                    canhandledynamiccontent: "false",
                    linksuffix: "/",
                    linkprefix: "/",
                    useRedirectImport: "false",
                    useWebservice: "0",
                    EnableContentBuilder: "true"
                }
            };

            this.loading = true;
            this.createCustomerResult = await main.customersService.create(newCustomer, this.newCustomerData.isWebShop, this.newCustomerData.isConfigurator, this.newCustomerData.isMultiLanguage);
            this.createCustomerResult.databaseUsers = databaseResult.data.users;
			// For testing CSS/HTML, comment the line above and uncomment the code below.
            /*this.createCustomerResult = {
                success: true,
                data: {
                    id: 123,
                    wiser2_encryption_key: "DIT_IS&EEN-TEST",
                    wiser2_encryption_key_test: "ER_is_geen_echte_klant_aangemaakt"
                }
            };*/
            this.loading = false;

            if (!this.createCustomerResult || !this.createCustomerResult.success) {
                this.message = this.createCustomerResult.message;
                return false;
            }

            return true;
        }
    }
};
import "../../Scss/tenant-management.scss";

export default {
    name: "tenantManagement",
    template: "#tenant-management-template",
    props: ["subDomain"],
    data() {
        return {
            currentStep: 1,
            lastStep: 2,
            message: "",
            errors: [],
            loading: false,
            newTenantData: {
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
            createTenantResult: {
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
            this.newTenantData = {
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
            const result = await main.tenantsService.getDbClusters(this.newTenantData.digitalOceanApiAccessToken);
            this.loading = false;
            if (!result.success) {
                this.message = result.message;
            } else {
                this.availableDbClusters = result.data.databases;
            }
        },

        async createDatabaseAndUser() {
            this.loading = true;
            const result = await main.tenantsService.createDatabaseAndUser(this.newTenantData.dbClusterChoice, this.newTenantData.databaseSchema, this.newTenantData.databaseUsername, this.newTenantData.digitalOceanApiAccessToken);
            this.loading = false;
            if (!result.success) {
                this.message = result.message;
                return result;
            }

            for (let user of result.data.users) {
                if (user.name.endsWith("_wiser")) {
                    this.newTenantData.databasePassword = user.password;
                    this.newTenantData.databaseUsername = user.name;
                    break;
                }
            }

            this.newTenantData.databaseHost = result.data.cluster.database.connection.host;
            this.newTenantData.databasePort = result.data.cluster.database.connection.port;
            return result;
        },

        async handleStep1() {
            this.message = "";
            this.errors = [];
            this.loading = true;
            const result = await main.tenantsService.exists(this.newTenantData.name, this.newTenantData.subDomain);
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
            if (this.newTenantData.createNewDatabase) {
                if (!this.newTenantData.dbClusterChoice) {
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
                                name: this.newTenantData.databaseUsername,
                                password: this.newTenantData.databasePassword
                            }
                        ]
                    }
                };
            }

            const newTenant = {
                name: this.newTenantData.name,
                database: {
                    host: this.newTenantData.databaseHost,
                    username: this.newTenantData.databaseUsername,
                    password: this.newTenantData.databasePassword,
                    databaseName: this.newTenantData.databaseSchema,
                    portNumber: this.newTenantData.databasePort
                },
                subDomain: this.newTenantData.subDomain,
                wiserSettings: {
                    hostLive: this.newTenantData.hostLive,
                    hostTest: this.newTenantData.hostTest,
                    hostDev: this.newTenantData.hostDev,
                    siteName: this.newTenantData.siteName
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
            this.createTenantResult = await main.tenantsService.create(newTenant, this.newTenantData.isWebShop, this.newTenantData.isConfigurator, this.newTenantData.isMultiLanguage);
            this.createTenantResult.databaseUsers = databaseResult.data.users;
			// For testing CSS/HTML, comment the line above and uncomment the code below.
            /*this.createTenantResult = {
                success: true,
                data: {
                    id: 123,
                    wiser2_encryption_key: "DIT_IS&EEN-TEST",
                    wiser2_encryption_key_test: "ER_is_geen_echte_klant_aangemaakt"
                }
            };*/
            this.loading = false;

            if (!this.createTenantResult || !this.createTenantResult.success) {
                this.message = this.createTenantResult.message;
                return false;
            }

            return true;
        }
    }
};
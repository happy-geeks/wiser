import {Utils} from "../../Base/Scripts/Utils";

export class RoleTab {
    constructor(base) {
        this.base = base;

        this.setupBindings();
        this.initializeKendoComponents();
    }

    /**
    * Setup all basis bindings for this module.
    * Specific bindings (for buttons in certain pop-ups for example) will be set when they are needed.
    */
    setupBindings() {
        // Add role button
        $(".addRoleBtn").kendoButton({
            click: () => {
                this.base.openDialog("Rol toevoegen", "Voer de naam in van de nieuwe rol").then((data) => {
                    this.addRemoveRoles(data);
                });
            },
            icon: "file"
        });

        // Delete role button
        $(".delRoleBtn").kendoButton({
            click: () => {
                const roleList = this.roleList;
                const index = roleList.select().index();
                const dataItem = roleList.dataSource.view()[index];

                this.base.openDialog("Item verwijderen", "Weet u zeker dat u dit item wil verwijderen?", this.base.kendoPromptType.CONFIRM).then(() => {
                    this.addRemoveRoles("", dataItem.id);
                });
            },
            icon: "delete"
        });
    }

    /**
     * Add or remove rights from the database based on the given parameters
     * @param {any} role The id of the role
     * @param {any} entity The name of the entity property
     * @param {any} permissionCode The code of the permission to add or delete
     */
    async updateEntityPropertyPermissions(role, entity, permissionCode) {
        try {
            await Wiser.api({
                url: `${this.base.settings.serviceRoot}/UPDATE_ENTITY_PROPERTY_PERMISSIONS?entityId=${encodeURIComponent(entity)}&roleId=${encodeURIComponent(role)}&permissionCode=${encodeURIComponent(permissionCode)}`,
                method: "GET"
            });

            this.base.showNotification("notification", `De wijzigingen zijn opgeslagen`, "success");
        }
        catch(exception) {
            console.error("Error while updating entity property permissions", exception);
            this.base.showNotification("notification", `Er is iets fout gegaan, probeer het opnieuw`, "error");
        }
    }

    /**
     * Add or remove module rights from the database based on the given parameters
     * @param {any} role The id of the role
     * @param {any} module The id of the module
     * @param {any} permissionCode The code of the permission to add or delete
     */
    async addRemoveModuleRightAssignment(role, module, permissionCode) {
        try {
            await Wiser.api({
                url: `${this.base.settings.serviceRoot}/UPDATE_MODULE_PERMISSION?moduleId=${encodeURIComponent(module)}&roleId=${encodeURIComponent(role)}&permissionCode=${encodeURIComponent(permissionCode)}`,
                method: "GET"
            });

            // Reload the modules list in the side bar of Wiser.
            await this.base.reloadModulesOnParentFrame();

            this.base.showNotification("notification", `De wijzigingen zijn opgeslagen.`, "success");
        }
        catch(exception) {
            console.error("Error while updating entity property permissions", exception);
            this.base.showNotification("notification", `Er is iets fout gegaan, probeer het opnieuw`, "error");
        }
    }

    /**
     * Add or remove roles from the database based on the given parameters
     * @param {string} name The specified name of the role that must be added
     * @param {any} id The id of the role that must be deleted
     */
    async addRemoveRoles(name = "", id = 0) {
        if (name === "" && id === 0) {
            return;
        }

        const data = {
            entityName: this.entitySelected
        };

        let template;
        let notification;
        if (id !== 0) {
            data.remove = true;
            template = "DELETE_ROLE";
            data.roleId = id;
            notification = "verwijderd";
        } else {
            data.add = true;
            template = "INSERT_ROLE";
            data.displayName = name;
            notification = "toegevoegd";
        }

        try {
            await Wiser.api({
                url: `${this.base.settings.serviceRoot}/${template}${Utils.toQueryString(data, true)}`,
                method: "GET"
            });

            this.base.showNotification("notification", `Item succesvol ${notification}`, "success");
            this.roleList.dataSource.read();
        } catch (exception) {
            this.base.showNotification("notification", `Item is niet succesvol ${notification}, probeer het opnieuw`, "error");
        }
    }

    /**
     * Init Kendo grid component
     * @param {any} item The item id of the selected role
     */
    initializeOrRefreshRolesEntityPropertiesGrid(item) {
        if (!this.entityPropertiesGrid) {
            this.entityPropertiesGrid = $("#EntityPropertiesGrid").kendoGrid({
                resizable: true,
                filterable: {
                    mode: "row"
                },
                columns: [
                    {
                        field: "entityName",
                        title: "Entiteit"
                    },
                    {
                        field: "tabName",
                        title: "Tab"
                    },
                    {
                        field: "groupName",
                        title: "Groep"
                    },
                    {
                        field: "displayName",
                        title: "Veld"
                    },
                    {
                        title: "Alle rechten",
                        width: "100px",
                        attributes: {
                            style: "text-align: center;"
                        },
                        headerTemplate: () => {
                            return `<div class="checkAll"><span>Alle rechten</span><input type="checkbox" id="role-check-all" class="k-checkbox role"><label class="k-checkbox-label" for="role-check-all"></label></div>`;
                        },
                        template: (dataItem) => {
                            return `<input type="checkbox" ${dataItem.permission === 15 ? "checked" : ""} id="role-entity-property-all-${dataItem.propertyId}" data-type="all" data-role-id="${dataItem.roleId}" data-entity="${dataItem.propertyId}" data-permission="15" class="k-checkbox role"><label class="k-checkbox-label" for="role-entity-property-all-${dataItem.propertyId}"></label>`;
                        }
                    },
                    {
                        title: "Geen rechten",
                        width: "100px",
                        attributes: {
                            style: "text-align: center;"
                        },
                        headerTemplate: () => {
                            return `<div class="checkAll"><span>Geen rechten</span><input type="checkbox" id="role-check-disable" class="k-checkbox role"><label class="k-checkbox-label" for="role-check-disable"></label></div>`;
                        },
                        template: (dataItem) => {
                            return `<input type="checkbox" id="role-entity-property-disable-${dataItem.propertyId}" data-role-id="${dataItem.roleId}" data-type="nothing" data-entity="${dataItem.propertyId}" data-permission="0" ${dataItem.permission === 0 ? "checked" : ""} class="k-checkbox role"><label class="k-checkbox-label" for="role-entity-property-disable-${dataItem.propertyId}"></label>`;
                        }
                    },
                    {
                        title: "Lezen",
                        width: "100px",
                        attributes: {
                            style: "text-align: center;"
                        },
                        headerTemplate: () => {
                            return `<div class="checkAll"><span>Lezen</span><input type="checkbox" id="role-check-read" class="k-checkbox role"><label class="k-checkbox-label" for="role-check-read"></label></div>`;
                        },
                        template: (dataItem) => {
                            return `<input type="checkbox" id="role-entity-property-read-${dataItem.propertyId}" data-role-id="${dataItem.roleId}" data-type="read" data-entity="${dataItem.propertyId}" data-permission="1" ${(1 << 0 & dataItem.permission) > 0 ? "checked" : ""} class="k-checkbox role"><label class="k-checkbox-label" for="role-entity-property-read-${dataItem.propertyId}"></label>`;
                        }
                    },
                    {
                        title: "Aanmaken",
                        width: "100px",
                        attributes: {
                            style: "text-align: center;"
                        },
                        headerTemplate: () => {
                            return `<div class="checkAll"><span>Aanmaken</span><input type="checkbox" id="role-check-edit" class="k-checkbox"><label class="k-checkbox-label" for="role-check-edit"></label></div>`;
                        },
                        template: (dataItem) => {
                            return `<input type="checkbox" id="role-entity-property-create-${dataItem.propertyId}" data-role-id="${dataItem.roleId}" data-type="create" data-entity="${dataItem.propertyId}" data-permission="2" ${(1 << 1 & dataItem.permission) > 0 ? "checked" : ""} class="k-checkbox role"><label class="k-checkbox-label" for="role-entity-property-create-${dataItem.propertyId}"></label>`;
                        }
                    },
                    {
                        title: "Wijzigen",
                        width: "100px",
                        attributes: {
                            style: "text-align: center;"
                        },
                        headerTemplate: () => {
                            return `<div class="checkAll"><span>Wijzigen</span><input type="checkbox" id="role-check-edit" class="k-checkbox"><label class="k-checkbox-label" for="role-check-edit"></label></div>`;
                        },
                        template: (dataItem) => {
                            return `<input type="checkbox" id="role-entity-property-edit-${dataItem.propertyId}" data-role-id="${dataItem.roleId}" data-type="edit" data-entity="${dataItem.propertyId}" data-permission="4" ${(1 << 2 & dataItem.permission) > 0 ? "checked" : ""} class="k-checkbox role"><label class="k-checkbox-label" for="role-entity-property-edit-${dataItem.propertyId}"></label>`;
                        }
                    },
                    {
                        title: "Verwijderen",
                        width: "100px",
                        attributes: {
                            style: "text-align: center;"
                        },
                        headerTemplate: () => {
                            return `<div class="checkAll"><span>Verwijderen</span><input type="checkbox" id="role-check-edit" class="k-checkbox"><label class="k-checkbox-label" for="role-check-edit"></label></div>`;
                        },
                        template: (dataItem) => {
                            return `<input type="checkbox" id="role-entity-property-delete-${dataItem.propertyId}" data-role-id="${dataItem.roleId}" data-type="remove" data-entity="${dataItem.propertyId}" data-permission="8" ${(1 << 3 & dataItem.permission) > 0 ? "checked" : ""} class="k-checkbox role"><label class="k-checkbox-label" for="role-entity-property-delete-${dataItem.propertyId}"></label>`;
                        }
                    }
                ],
                dataBound: (e) => {
                    // When a item in the header is selected
                    e.sender.thead.find(".checkAll > input").off("change").change((element) => {
                        this.base.openDialog("Meerdere rechten wijzigen", "U staat op het punt voor meer dan een regel de rechten te zetten, weet u zeker dat u wilt doorgaan?", this.base.kendoPromptType.CONFIRM).then(() => {
                            const clickedElement = element.currentTarget;
                            this.base.setCheckboxForHeaderItems(clickedElement);
                        });
                    });

                    // When a item in the grid is checked
                    e.sender.tbody.find(".k-checkbox").off("change").change((element) => {
                        const targetElement = element.currentTarget;
                        const tagetType = targetElement.dataset.type;
                        const roleId = parseInt(targetElement.dataset.roleId);
                        const entityId = parseInt(targetElement.dataset.entity);
                        const permissionValue = this.base.setCheckboxForItems(targetElement, tagetType, entityId, "entity-property");

                        this.updateEntityPropertyPermissions(roleId, entityId, permissionValue);
                    });
                }
            }).data("kendoGrid");
        }

        const queryStringForEntityPropertiesGrid = {
            roleId: item
        };
        this.entityPropertiesGrid.setDataSource({
            transport: {
                read: {
                    url: `${this.base.settings.serviceRoot}/GET_ROLE_RIGHTS${Utils.toQueryString(queryStringForEntityPropertiesGrid, true)}`
                }
            }
        });
    }

    /**
     * Init Kendo grid component
     * @param {any} item The item id of the selected role
     */
    initializeOrRefreshRolesModulesGrid(item) {
        if (!this.modulesGrid) {
            this.modulesGrid = $("#ModulesGrid").kendoGrid({
                resizable: true,
                filterable: {
                    mode: "row"
                },
                columns: [
                    {
                        title: "Module naam",
                        field: "moduleName"
                    },
                    {
                        title: "Alle rechten",
                        width: "100px",
                        attributes: {
                            style: "text-align: center;"
                        },
                        headerTemplate: () => {
                            return `<div class="checkAll"><span>Alle rechten</span><input type="checkbox" id="role-check-all" class="k-checkbox module"><label class="k-checkbox-label" for="role-check-all"></label></div>`;
                        },
                        template: (dataItem) => {
                            return `<input type="checkbox" id="role-module-all-${dataItem.moduleId}" data-type="all" data-role-id="${dataItem.roleId}" data-module="${dataItem.moduleId}" data-permission="0" ${dataItem.permission === 15 ? "checked" : ""} class="k-checkbox module"><label class="k-checkbox-label" for="role-module-all-${dataItem.moduleId}"></label>`;
                        }
                    },
                    {
                        title: "Geen rechten",
                        width: "100px",
                        attributes: {
                            style: "text-align: center;"
                        },
                        headerTemplate: () => {
                            return `<div class="checkAll"><span>Geen rechten</span><input type="checkbox" id="role-check-disable" class="k-checkbox module"><label class="k-checkbox-label" for="role-check-disable"></label></div>`;
                        },
                        template: (dataItem) => {
                            return `<input type="checkbox" id="role-module-disable-${dataItem.moduleId}" data-type="nothing" data-role-id="${dataItem.roleId}" data-module="${dataItem.moduleId}" data-permission="0" ${dataItem.permission === 0 ? "checked" : ""} class="k-checkbox module"><label class="k-checkbox-label" for="role-module-disable-${dataItem.moduleId}"></label>`;
                        }
                    },
                    {
                        title: "Lezen",
                        width: "100px",
                        attributes: {
                            style: "text-align: center;"
                        },
                        headerTemplate: () => {
                            return `<div class="checkAll"><span>Lezen</span><input type="checkbox" id="role-check-read" class="k-checkbox module"><label class="k-checkbox-label" for="role-check-read"></label></div>`;
                        },
                        template: (dataItem) => {
                            return `<input type="checkbox" id="role-module-read-${dataItem.moduleId}" data-type="read" data-role-id="${dataItem.roleId}" data-module="${dataItem.moduleId}" data-permission="1" ${(1 << 0 & dataItem.permission) > 0 ? "checked" : ""} class="k-checkbox"><label class="k-checkbox-label" for="role-module-read-${dataItem.moduleId}"></label>`;
                        }
                    },
                    {
                        title: "Aanmaken",
                        width: "100px",
                        attributes: {
                            style: "text-align: center;"
                        },
                        headerTemplate: () => {
                            return `<div class="checkAll"><span>Aanmaken</span><input type="checkbox" id="role-check-edit" class="k-checkbox module"><label class="k-checkbox-label" for="role-check-edit"></label></div>`;
                        },
                        template: (dataItem) => {
                            return `<input type="checkbox" id="role-module-create-${dataItem.moduleId}" data-type="create" data-role-id="${dataItem.roleId}" data-module="${dataItem.moduleId}" data-permission="2" ${(1 << 1 & dataItem.permission) > 0 ? "checked" : ""} class="k-checkbox"><label class="k-checkbox-label" for="role-module-create-${dataItem.moduleId}"></label>`;
                        }
                    },
                    {
                        title: "Wijzigen",
                        width: "100px",
                        attributes: {
                            style: "text-align: center;"
                        },
                        headerTemplate: () => {
                            return `<div class="checkAll"><span>Wijzigen</span><input type="checkbox" id="role-check-edit" class="k-checkbox module"><label class="k-checkbox-label" for="role-check-edit"></label></div>`;
                        },
                        template: (dataItem) => {
                            return `<input type="checkbox" id="role-module-edit-${dataItem.moduleId}" data-type="edit" data-role-id="${dataItem.roleId}" data-module="${dataItem.moduleId}" data-permission="4" ${(1 << 2 & dataItem.permission) > 0 ? "checked" : ""} class="k-checkbox"><label class="k-checkbox-label" for="role-module-edit-${dataItem.moduleId}"></label>`;
                        }
                    },
                    {
                        title: "Verwijderen",
                        width: "100px",
                        attributes: {
                            style: "text-align: center;"
                        },
                        headerTemplate: () => {
                            return `<div class="checkAll"><span>Verwijderen</span><input type="checkbox" id="role-check-edit" class="k-checkbox module"><label class="k-checkbox-label" for="role-check-edit"></label></div>`;
                        },
                        template: (dataItem) => {
                            return `<input type="checkbox" id="role-module-delete-${dataItem.moduleId}" data-type="remove"  data-role-id="${dataItem.roleId}" data-module="${dataItem.moduleId}" data-permission="8" ${(1 << 3 & dataItem.permission) > 0 ? "checked" : ""} class="k-checkbox"><label class="k-checkbox-label" for="role-module-delete-${dataItem.moduleId}"></label>`;
                        }
                    }
                ],
                dataBound: (e) => {
                    // When a item in the header is selected
                    e.sender.thead.find(".checkAll > input").off("change").change((element) => {
                        this.base.openDialog("Meerdere rechten wijzigen", "U staat op het punt voor meer dan een regel de rechten te zetten, weet u zeker dat u wilt doorgaan?", this.base.kendoPromptType.CONFIRM).then(() => {
                            const clickedElement = element.currentTarget;
                            this.base.setCheckboxForHeaderItems(clickedElement);
                        })
                    });

                    // When a item in the grid is checked
                    e.sender.tbody.find(".k-checkbox").off("change").change((element) => {
                        const targetElement = element.currentTarget;
                        const tagetType = targetElement.dataset.type;
                        const roleId = parseInt(targetElement.dataset.roleId);
                        const moduleId = parseInt(targetElement.dataset.module);

                        let permissionValue = this.base.setCheckboxForItems(targetElement, tagetType, moduleId, "module");

                        if (permissionValue !== -1) {
                            this.addRemoveModuleRightAssignment(roleId, moduleId, permissionValue);
                        }
                    });
                }
            }).data("kendoGrid");
        }

        const queryStringForModulesGrid = {
            roleId: item
        };

        this.modulesGrid.setDataSource({
            transport: {
                read: {
                    url: `${this.base.settings.serviceRoot}/GET_MODULE_PERMISSIONS${Utils.toQueryString(queryStringForModulesGrid, true)}`
                }
            }
        });
    }

    getSelectedTabName() {
        return this.rolesTabStrip.select().find(".k-link").text();
    }

    /** Init Kendo listview component */
    initializeKendoComponents() {
        this.rolesTabStrip = $("#RolesTabStrip").kendoTabStrip({
            animation: {
                open: {
                    effects: "expand:vertical",
                    duration: 0
                },
                close: {
                    effects: "expand:vertical",
                    duration: 0
                }
            },
            select: (event) => {
                const selectedTab = event.item.querySelector(".k-link").innerHTML.toLowerCase();
                console.log("rolesTabStrip select", selectedTab);
            },
            activate: (event) => {
                const selectedTab = event.item.querySelector(".k-link").innerHTML.toLowerCase();
                const dataItem = this.roleList.dataItem(this.roleList.select());
                console.log("rolesTabStrip activate", selectedTab, dataItem);
                if (typeof dataItem !== "undefined") {
                    if (selectedTab === "velden") {
                        this.initializeOrRefreshRolesEntityPropertiesGrid(dataItem.id);
                    }
                    if (selectedTab === "modules") {
                        this.initializeOrRefreshRolesModulesGrid(dataItem.id);
                    }
                }
            }
        }).data("kendoTabStrip");

        this.roleList = $("#roleList").kendoListView({
            contentElement: "ul",
            template: "<li class='sortable' data-item='${id}' >${roleName}</li>",
            dataSource: {
                transport: {
                    read: {
                        url: `${this.base.settings.serviceRoot}/GET_ROLES`
                    }
                }
            },
            dataTextField: "displayName",
            dataValueField: "id",
            selectable: true,
            change: () => {
                const dataItem = this.roleList.dataItem(this.roleList.select());

                const selectedTab = this.getSelectedTabName().toLowerCase();
                if (selectedTab === "velden") {
                    this.initializeOrRefreshRolesEntityPropertiesGrid(dataItem.id);
                }
                if (selectedTab === "modules") {
                    this.initializeOrRefreshRolesModulesGrid(dataItem.id);
                }
            }
        }).data("kendoListView");
    }

    hasChanges() {
        return false;
    }
}
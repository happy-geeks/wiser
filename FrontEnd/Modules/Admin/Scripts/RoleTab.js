import {Utils, Wiser} from "../../Base/Scripts/Utils";

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
     * Add or remove rights from the database based on the given parameters
     * @param {any} subject The subject of which the rights get updates, for example modules
     * @param {any} role The id of the role
     * @param {any} subjectId The id of the permission subject
     * @param {any} permission The code of the permission to add or delete
     */
    async addRemoveSubjectRightAssignment(subject, role, subjectId, permission) {
        try {
            await Wiser.api({
                url: `${this.base.settings.wiserApiRoot}permissions/`,
                contentType: "application/json",
                data: JSON.stringify({
                    subject: subject,
                    subjectId: subjectId,
                    roleId: role,
                    permission: permission
                }),
                method: "POST"
            });

            // Reload the modules list in the side bar of Wiser.
            await this.base.reloadModulesOnParentFrame();

            this.base.showNotification("notification", `De wijzigingen zijn opgeslagen.`, "success");
        }
        catch(exception) {
            console.error(`Error while updating ${subject} permissions`, exception);
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
                            return `<div class="checkAll"><span>Alle rechten</span><input type="checkbox" id="role-entity-property-check-all" class="k-checkbox role"><label class="k-checkbox-label" for="role-entity-property-check-all"></label></div>`;
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
                            return `<div class="checkAll"><span>Geen rechten</span><input type="checkbox" id="role-entity-property-check-disable" class="k-checkbox role"><label class="k-checkbox-label" for="role-entity-property-check-disable"></label></div>`;
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
                            return `<div class="checkAll"><span>Lezen</span><input type="checkbox" id="role-entity-property-check-read" class="k-checkbox role"><label class="k-checkbox-label" for="role-entity-property-check-read"></label></div>`;
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
                            return `<div class="checkAll"><span>Aanmaken</span><input type="checkbox" id="role-entity-property-check-create" class="k-checkbox"><label class="k-checkbox-label" for="role-entity-property-check-create"></label></div>`;
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
                            return `<div class="checkAll"><span>Wijzigen</span><input type="checkbox" id="role-entity-property-check-edit" class="k-checkbox"><label class="k-checkbox-label" for="role-entity-property-check-edit"></label></div>`;
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
                            return `<div class="checkAll"><span>Verwijderen</span><input type="checkbox" id="role-entity-property-check-delete" class="k-checkbox"><label class="k-checkbox-label" for="role-entity-property-check-delete"></label></div>`;
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
    
    initializeOrRefreshRolesModulesGrid(roleId) {
        const subject = 'Modules';
        if (!this.modulesGrid) {
            this.modulesGrid = this.initializeOrRefreshRolesOnGrid(roleId, '#ModulesGrid', subject);
        }
        const queryStringForModulesGrid = {
            roleId: roleId,
            subject: subject
        };

        this.modulesGrid.setDataSource({
            transport: {
                read: {
                    url: `${this.base.settings.wiserApiRoot}permissions/${Utils.toQueryString(queryStringForModulesGrid, true)}`
                }
            }
        });
    }

    initializeOrRefreshRolesQueriesGrid(roleId) {
        const subject = 'Queries';
        if (!this.queriesGrid) {
            this.queriesGrid = this.initializeOrRefreshRolesOnGrid(roleId, '#QueriesGrid', subject);
        }

        const queryStringForQueriesGrid = {
            roleId: roleId,
            subject: subject
        };

        this.queriesGrid.setDataSource({
            transport: {
                read: {
                    url: `${this.base.settings.wiserApiRoot}permissions/${Utils.toQueryString(queryStringForQueriesGrid, true)}`
                }
            }
        });
    }

    initializeOrRefreshEndpointsGrid(roleId) {
        const queryStringForEndpointsGrid = {
            roleId: roleId,
            subject: "Endpoints"
        };

        if (!this.endpointsGrid) {
            this.endpointsGrid = $("#EndpointsGrid").kendoGrid({
                editable: "inline",
                filterable: true,
                toolbar: ["create"],
                columns: [
                    {
                        title: "URL",
                        field: "endpointUrl"
                    },
                    {
                        title: "HTTP Method",
                        field: "endpointHttpMethod"
                    },
                    {
                        title: "Toegestaan",
                        field: "permission"
                    },
                    {
                        command: ["edit", "destroy"],
                        title: "&nbsp;",
                        width: "250px"
                    }
                ]
            }).data("kendoGrid");
        }

        this.endpointsGrid.setDataSource({
            transport: {
                read: {
                    url: `${this.base.settings.wiserApiRoot}permissions/${Utils.toQueryString(queryStringForEndpointsGrid, true)}`
                },
                create: {
                    url: `${this.base.settings.wiserApiRoot}permissions`,
                    contentType: "application/json",
                    method: "POST"
                },
                update: {
                    url: `${this.base.settings.wiserApiRoot}permissions`,
                    contentType: "application/json",
                    method: "POST"
                },
                destroy: {
                    url: `${this.base.settings.wiserApiRoot}permissions`,
                    contentType: "application/json",
                    method: "DELETE"
                },
                parameterMap: (data, operation) => {
                    console.log("parameterMap", data, operation);
                    if (operation !== "read") {
                        return kendo.stringify($.extend({"subject": "endpoints"}, data));
                    }
                }
            },
            schema: {
                model: {
                    id: "id",
                    fields: {
                        id: { type: "number" },
                        objectId: { type: "number" },
                        objectName: { type: "string" },
                        roleId: { type: "number" },
                        roleName: { type: "string" },
                        endpointUrl: { type: "string" },
                        endpointHttpMethod: { type: "string" },
                        permission: { type: "number" }
                    }
                }
            }
        });
    }

    /**
     * Init Kendo grid component
     * @param {any} item The item id of the selected role
     */
    initializeOrRefreshRolesOnGrid(item, gridSelector, subject) {
        return $(gridSelector).kendoGrid({
            resizable: true,
            filterable: {
                mode: "row"
            },
            columns: [
                {
                    title: "Naam",
                    field: "objectName"
                },
                {
                    title: "Alle rechten",
                    width: "100px",
                    attributes: {
                        style: "text-align: center;"
                    },
                    headerTemplate: () => {
                        return `<div class="checkAll"><span>Alle rechten</span><input type="checkbox" id="role-${subject}-check-all" class="k-checkbox ${subject}"><label class="k-checkbox-label" for="role-${subject}-check-all"></label></div>`;
                    },
                    template: (dataItem) => {
                        return `<input type="checkbox" id="role-${subject}-all-${dataItem.objectId}" data-type="all" data-role-id="${dataItem.roleId}" data-id="${dataItem.objectId}" data-permission="0" ${dataItem.permission === 15 ? "checked" : ""} class="k-checkbox ${subject}"><label class="k-checkbox-label" for="role-${subject}-all-${dataItem.objectId}"></label>`;
                    }
                },
                {
                    title: "Geen rechten",
                    width: "100px",
                    attributes: {
                        style: "text-align: center;"
                    },
                    headerTemplate: () => {
                        return `<div class="checkAll"><span>Geen rechten</span><input type="checkbox" id="role-${subject}-check-disable" class="k-checkbox ${subject}"><label class="k-checkbox-label" for="role-${subject}-check-disable"></label></div>`;
                    },
                    template: (dataItem) => {
                        return `<input type="checkbox" id="role-${subject}-disable-${dataItem.objectId}" data-type="nothing" data-role-id="${dataItem.roleId}" data-id="${dataItem.objectId}" data-permission="0" ${dataItem.permission === 0 ? "checked" : ""} class="k-checkbox ${subject}"><label class="k-checkbox-label" for="role-${subject}-disable-${dataItem.objectId}"></label>`;
                    }
                },
                {
                    title: "Lezen",
                    width: "100px",
                    attributes: {
                        style: "text-align: center;"
                    },
                    headerTemplate: () => {
                        return `<div class="checkAll"><span>Lezen</span><input type="checkbox" id="role-${subject}-check-read" class="k-checkbox ${subject}"><label class="k-checkbox-label" for="role-${subject}-check-read"></label></div>`;
                    },
                    template: (dataItem) => {
                        return `<input type="checkbox" id="role-${subject}-read-${dataItem.objectId}" data-type="read" data-role-id="${dataItem.roleId}" data-id="${dataItem.objectId}" data-permission="1" ${(1 << 0 & dataItem.permission) > 0 ? "checked" : ""} class="k-checkbox"><label class="k-checkbox-label" for="role-${subject}-read-${dataItem.objectId}"></label>`;
                    }
                },
                {
                    title: "Aanmaken",
                    width: "100px",
                    attributes: {
                        style: "text-align: center;"
                    },
                    headerTemplate: () => {
                        return `<div class="checkAll"><span>Aanmaken</span><input type="checkbox" id="role-${subject}-check-create" class="k-checkbox ${subject}"><label class="k-checkbox-label" for="role-${subject}-check-create"></label></div>`;
                    },
                    template: (dataItem) => {
                        return `<input type="checkbox" id="role-${subject}-create-${dataItem.objectId}" data-type="create" data-role-id="${dataItem.roleId}" data-id="${dataItem.objectId}" data-permission="2" ${(1 << 1 & dataItem.permission) > 0 ? "checked" : ""} class="k-checkbox"><label class="k-checkbox-label" for="role-${subject}-create-${dataItem.objectId}"></label>`;
                    }
                },
                {
                    title: "Wijzigen",
                    width: "100px",
                    attributes: {
                        style: "text-align: center;"
                    },
                    headerTemplate: () => {
                        return `<div class="checkAll"><span>Wijzigen</span><input type="checkbox" id="role-${subject}-check-edit" class="k-checkbox ${subject}"><label class="k-checkbox-label" for="role-${subject}-check-edit"></label></div>`;
                    },
                    template: (dataItem) => {
                        return `<input type="checkbox" id="role-${subject}-edit-${dataItem.objectId}" data-type="edit" data-role-id="${dataItem.roleId}" data-id="${dataItem.objectId}" data-permission="4" ${(1 << 2 & dataItem.permission) > 0 ? "checked" : ""} class="k-checkbox"><label class="k-checkbox-label" for="role-${subject}-edit-${dataItem.objectId}"></label>`;
                    }
                },
                {
                    title: "Verwijderen",
                    width: "100px",
                    attributes: {
                        style: "text-align: center;"
                    },
                    headerTemplate: () => {
                        return `<div class="checkAll"><span>Verwijderen</span><input type="checkbox" id="role-${subject}-check-delete" class="k-checkbox ${subject}"><label class="k-checkbox-label" for="role-${subject}-check-delete"></label></div>`;
                    },
                    template: (dataItem) => {
                        return `<input type="checkbox" id="role-${subject}-delete-${dataItem.objectId}" data-type="remove"  data-role-id="${dataItem.roleId}" data-id="${dataItem.objectId}" data-permission="8" ${(1 << 3 & dataItem.permission) > 0 ? "checked" : ""} class="k-checkbox"><label class="k-checkbox-label" for="role-${subject}-delete-${dataItem.objectId}"></label>`;
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
                    const moduleId = parseInt(targetElement.dataset.id);

                    let permissionValue = this.base.setCheckboxForItems(targetElement, tagetType, moduleId, subject);

                    if (permissionValue !== -1) {
                        this.addRemoveSubjectRightAssignment(subject, roleId, moduleId, permissionValue);
                    }
                });
            }
        }).data("kendoGrid");
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
                if (typeof dataItem === "undefined") {
                    return;
                }
                switch (selectedTab) {
                    case "velden":
                        this.initializeOrRefreshRolesEntityPropertiesGrid(dataItem.id);
                        break;
                    case "modules":
                        this.initializeOrRefreshRolesModulesGrid(dataItem.id);
                        break;
                    case "query's":
                        this.initializeOrRefreshRolesQueriesGrid(dataItem.id);
                        break;
                    case "endpoints":
                        this.initializeOrRefreshEndpointsGrid(dataItem.id);
                        break;
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
                switch (selectedTab) {
                    case "velden":
                        this.initializeOrRefreshRolesEntityPropertiesGrid(dataItem.id);
                        break;
                    case "modules":
                        this.initializeOrRefreshRolesModulesGrid(dataItem.id);
                        break;
                    case "query's":
                        this.initializeOrRefreshRolesQueriesGrid(dataItem.id);
                        break;
                    case "endpoints":
                        this.initializeOrRefreshEndpointsGrid(dataItem.id);
                        break;
                }
            }
        }).data("kendoListView");
    }

    hasChanges() {
        return false;
    }
}
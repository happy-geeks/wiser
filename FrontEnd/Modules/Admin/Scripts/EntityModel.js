export class EntityModel {
    constructor(id=-1, name="", module_id = 700, accepted_childtypes = "", icon = "", icon_add = "", show_in_tree_view = 1, query_after_insert = "", query_after_update = "",
        query_before_update = "", query_before_delete = "", color = "blue", show_in_search = 1, show_overview_tab = 1, save_title_as_seo = 1, api_after_insert = "",
        api_after_update = "", api_before_update = "", api_before_delete = "", show_title_field = 1, friendly_name = "", save_history = 1, default_ordering = "", icon_expanded = "") {
        this.id = id;
        this.name = name;
        this.module_id = module_id;
        this.accepted_childtypes = accepted_childtypes;
        this.icon = icon;
        this.icon_add = icon_add;
        this.show_in_tree_view = show_in_tree_view;
        this.query_after_insert = query_after_insert;
        this.query_after_update = query_after_update;
        this.query_before_update = query_before_update;
        this.query_before_delete = query_before_delete;
        this.color = color;
        this.show_in_search = show_in_search;
        this.show_overview_tab = show_overview_tab;
        this.save_title_as_seo = save_title_as_seo;
        this.api_after_insert = api_after_insert;
        this.api_after_update = api_after_update;
        this.api_before_update = api_before_update;
        this.api_before_delete = api_before_delete;
        this.show_title_field = show_title_field;
        this.friendly_name = friendly_name;
        this.save_history = save_history;
        this.default_ordering = default_ordering;
        this.icon_expanded = icon_expanded;
    }
}

SET NAMES utf8mb4 COLLATE utf8mb4_general_ci;

# Drop old triggers with old names, originally we had trigger names that weren't very consistent and sometimes not logical either, so we changed them.
DROP TRIGGER IF EXISTS `CreateItem`;
DROP TRIGGER IF EXISTS `UpdateItem`;
DROP TRIGGER IF EXISTS `DeleteItem`;
DROP TRIGGER IF EXISTS `toevoeging`;
DROP TRIGGER IF EXISTS `wijziging`;
DROP TRIGGER IF EXISTS `verwijdering`;
DROP TRIGGER IF EXISTS `On_insert`;
DROP TRIGGER IF EXISTS `On_change`;
DROP TRIGGER IF EXISTS `On_remove`;
DROP TRIGGER IF EXISTS `addhistory`;
DROP TRIGGER IF EXISTS `updatehistory`;
DROP TRIGGER IF EXISTS `removehistory`;
DROP TRIGGER IF EXISTS `On_insert_file`;
DROP TRIGGER IF EXISTS `On_change_file`;
DROP TRIGGER IF EXISTS `On_remove_file`;

-- ----------------------------
-- Triggers structure for table wiser_entityproperty
-- ----------------------------
DROP TRIGGER IF EXISTS `EntityPropertyInsert`;
CREATE TRIGGER `EntityPropertyInsert` AFTER INSERT ON `wiser_entityproperty` FOR EACH ROW BEGIN
    IF IFNULL(@saveHistory, TRUE) = TRUE THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('INSERT_ENTITYPROPERTY', 'wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), '', '', '');

        IF IFNULL(NEW.`module_id`, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY','wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'module_id', '', NEW.`module_id`);
        END IF;

        IF IFNULL(NEW.`entity_name`, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY','wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'entity_name', '', NEW.`entity_name`);
        END IF;

        IF IFNULL(NEW.`link_type`, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY','wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'link_type', '', NEW.`link_type`);
        END IF;

        IF IFNULL(NEW.`visible_in_overview`, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY','wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'visible_in_overview', '', NEW.`visible_in_overview`);
        END IF;

        IF IFNULL(NEW.`overview_width`, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY','wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'overview_width', '', NEW.`overview_width`);
        END IF;

        IF IFNULL(NEW.`tab_name`, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY','wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'tab_name', '', NEW.`tab_name`);
        END IF;

        IF IFNULL(NEW.`group_name`, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY','wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'group_name', '', NEW.`group_name`);
        END IF;

        IF IFNULL(NEW.`inputtype`, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY','wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'inputtype', '', NEW.`inputtype`);
        END IF;

        IF IFNULL(NEW.`display_name`, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY','wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'display_name', '', NEW.`display_name`);
        END IF;

        IF IFNULL(NEW.`property_name`, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY','wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'property_name', '', NEW.`property_name`);
        END IF;

        IF IFNULL(NEW.`explanation`, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY','wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'explanation', '', NEW.`explanation`);
        END IF;

        IF IFNULL(NEW.`ordering`, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY','wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'ordering', '', NEW.`ordering`);
        END IF;

        IF IFNULL(NEW.`regex_validation`, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY','wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'regex_validation', '', NEW.`regex_validation`);
        END IF;

        IF IFNULL(NEW.`mandatory`, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY','wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'mandatory', '', NEW.`mandatory`);
        END IF;

        IF IFNULL(NEW.`readonly`, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY','wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'readonly', '', NEW.`readonly`);
        END IF;

        IF IFNULL(NEW.`default_value`, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY','wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'default_value', '', NEW.`default_value`);
        END IF;

        IF IFNULL(NEW.`automation`, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY','wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'automation', '', NEW.`automation`);
        END IF;

        IF IFNULL(NEW.`css`, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY','wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'css', '', NEW.`css`);
        END IF;

        IF IFNULL(NEW.`width`, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY','wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'width', '', NEW.`width`);
        END IF;

        IF IFNULL(NEW.`height`, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY','wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'height', '', NEW.`height`);
        END IF;

        IF IFNULL(NEW.`options`, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY','wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'options', '', NEW.`options`);
        END IF;

        IF IFNULL(NEW.`data_query`, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY','wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'data_query', '', NEW.`data_query`);
        END IF;

        IF IFNULL(NEW.`action_query`, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY','wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'action_query', '', NEW.`action_query`);
        END IF;

        IF IFNULL(NEW.`search_query`, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY','wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'search_query', '', NEW.`search_query`);
        END IF;

        IF IFNULL(NEW.`search_count_query`, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY','wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'search_count_query', '', NEW.`search_count_query`);
        END IF;

        IF IFNULL(NEW.`grid_delete_query`, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY','wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'grid_delete_query', '', NEW.`grid_delete_query`);
        END IF;

        IF IFNULL(NEW.`grid_insert_query`, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY','wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'grid_insert_query', '', NEW.`grid_insert_query`);
        END IF;

        IF IFNULL(NEW.`grid_update_query`, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY','wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'grid_update_query', '', NEW.`grid_update_query`);
        END IF;

        IF IFNULL(NEW.`depends_on_field`, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY','wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'depends_on_field', '', NEW.`depends_on_field`);
        END IF;

        IF IFNULL(NEW.`depends_on_operator`, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY','wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'depends_on_operator', '', NEW.`depends_on_operator`);
        END IF;

        IF IFNULL(NEW.`depends_on_value`, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY','wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'depends_on_value', '', NEW.`depends_on_value`);
        END IF;

        IF IFNULL(NEW.`language_code`, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY','wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'language_code', '', NEW.`language_code`);
        END IF;

        IF IFNULL(NEW.`custom_script`, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY','wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'custom_script', '', NEW.`custom_script`);
        END IF;

        IF IFNULL(NEW.`also_save_seo_value`, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY','wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'also_save_seo_value', '', NEW.`also_save_seo_value`);
        END IF;

        IF IFNULL(NEW.`depends_on_action`, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY','wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'depends_on_action', '', NEW.`depends_on_action`);
        END IF;

        IF IFNULL(NEW.`save_on_change`, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY','wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'save_on_change', '', NEW.`save_on_change`);
        END IF;

        IF IFNULL(NEW.`extended_explanation`, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY','wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'extended_explanation', '', NEW.`extended_explanation`);
        END IF;

        IF IFNULL(NEW.`label_style`, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY','wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'label_style', '', NEW.`label_style`);
        END IF;

        IF IFNULL(NEW.`label_width`, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY','wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'label_width', '', NEW.`label_width`);
        END IF;

        IF IFNULL(NEW.`enable_aggregation`, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY','wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'enable_aggregation', '', NEW.`enable_aggregation`);
        END IF;

        IF IFNULL(NEW.`aggregate_options`, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY','wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'aggregate_options', '', NEW.`aggregate_options`);
        END IF;

        IF IFNULL(NEW.`access_key`, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY','wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'access_key', '', NEW.`access_key`);
        END IF;

        IF IFNULL(NEW.`visibility_path_regex`, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY','wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'visibility_path_regex', '', NEW.`visibility_path_regex`);
        END IF;
    END IF;
END;

DROP TRIGGER IF EXISTS `EntityPropertyUpdate`;
CREATE TRIGGER `EntityPropertyUpdate` AFTER UPDATE ON `wiser_entityproperty` FOR EACH ROW BEGIN
    IF IFNULL(@saveHistory, TRUE) = TRUE THEN
        IF IFNULL(NEW.`module_id`, '') <> IFNULL(OLD.`module_id`, '') THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY', 'wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'module_id', OLD.`module_id`, NEW.`module_id`);
        END IF;

        IF IFNULL(NEW.`entity_name`, '') <> IFNULL(OLD.`entity_name`, '') THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY', 'wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'entity_name', OLD.`entity_name`, NEW.`entity_name`);
        END IF;

        IF IFNULL(NEW.`link_type`, '') <> IFNULL(OLD.`link_type`, '') THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY', 'wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'link_type', OLD.`link_type`, NEW.`link_type`);
        END IF;

        IF IFNULL(NEW.`visible_in_overview`, '') <> IFNULL(OLD.`visible_in_overview`, '') THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY', 'wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'visible_in_overview', OLD.`visible_in_overview`, NEW.`visible_in_overview`);
        END IF;

        IF IFNULL(NEW.`overview_width`, '') <> IFNULL(OLD.`overview_width`, '') THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY', 'wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'overview_width', OLD.`overview_width`, NEW.`overview_width`);
        END IF;

        IF IFNULL(NEW.`tab_name`, '') <> IFNULL(OLD.`tab_name`, '') THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY', 'wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'tab_name', OLD.`tab_name`, NEW.`tab_name`);
        END IF;

        IF IFNULL(NEW.`group_name`, '') <> IFNULL(OLD.`group_name`, '') THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY', 'wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'group_name', OLD.`group_name`, NEW.`group_name`);
        END IF;

        IF IFNULL(NEW.`inputtype`, '') <> IFNULL(OLD.`inputtype`, '') THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY', 'wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'inputtype', OLD.`inputtype`, NEW.`inputtype`);
        END IF;

        IF IFNULL(NEW.`display_name`, '') <> IFNULL(OLD.`display_name`, '') THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY', 'wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'display_name', OLD.`display_name`, NEW.`display_name`);
        END IF;

        IF IFNULL(NEW.`property_name`, '') <> IFNULL(OLD.`property_name`, '') THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY', 'wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'property_name', OLD.`property_name`, NEW.`property_name`);
        END IF;

        IF IFNULL(NEW.`explanation`, '') <> IFNULL(OLD.`explanation`, '') THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY', 'wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'explanation', OLD.`explanation`, NEW.`explanation`);
        END IF;

        IF IFNULL(NEW.`ordering`, '') <> IFNULL(OLD.`ordering`, '') THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY', 'wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'ordering', OLD.`ordering`, NEW.`ordering`);
        END IF;

        IF IFNULL(NEW.`regex_validation`, '') <> IFNULL(OLD.`regex_validation`, '') THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY', 'wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'regex_validation', OLD.`regex_validation`, NEW.`regex_validation`);
        END IF;

        IF IFNULL(NEW.`mandatory`, '') <> IFNULL(OLD.`mandatory`, '') THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY', 'wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'mandatory', OLD.`mandatory`, NEW.`mandatory`);
        END IF;

        IF IFNULL(NEW.`readonly`, '') <> IFNULL(OLD.`readonly`, '') THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY', 'wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'readonly', OLD.`readonly`, NEW.`readonly`);
        END IF;

        IF IFNULL(NEW.`default_value`, '') <> IFNULL(OLD.`default_value`, '') THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY', 'wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'default_value', OLD.`default_value`, NEW.`default_value`);
        END IF;

        IF IFNULL(NEW.`automation`, '') <> IFNULL(OLD.`automation`, '') THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY', 'wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'automation', OLD.`automation`, NEW.`automation`);
        END IF;

        IF IFNULL(NEW.`css`, '') <> IFNULL(OLD.`css`, '') THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY', 'wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'css', OLD.`css`, NEW.`css`);
        END IF;

        IF IFNULL(NEW.`width`, '') <> IFNULL(OLD.`width`, '') THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY', 'wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'width', OLD.`width`, NEW.`width`);
        END IF;

        IF IFNULL(NEW.`height`, '') <> IFNULL(OLD.`height`, '') THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY', 'wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'height', OLD.`height`, NEW.`height`);
        END IF;

        IF IFNULL(NEW.`options`, '') <> IFNULL(OLD.`options`, '') THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY', 'wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'options', OLD.`options`, NEW.`options`);
        END IF;

        IF IFNULL(NEW.`data_query`, '') <> IFNULL(OLD.`data_query`, '') THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY', 'wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'data_query', OLD.`data_query`, NEW.`data_query`);
        END IF;

        IF IFNULL(NEW.`action_query`, '') <> IFNULL(OLD.`action_query`, '') THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY', 'wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'action_query', OLD.`action_query`, NEW.`action_query`);
        END IF;

        IF IFNULL(NEW.`search_query`, '') <> IFNULL(OLD.`search_query`, '') THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY', 'wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'search_query', OLD.`search_query`, NEW.`search_query`);
        END IF;

        IF IFNULL(NEW.`search_count_query`, '') <> IFNULL(OLD.`search_count_query`, '') THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY', 'wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'search_count_query', OLD.`search_count_query`, NEW.`search_count_query`);
        END IF;

        IF IFNULL(NEW.`grid_delete_query`, '') <> IFNULL(OLD.`grid_delete_query`, '') THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY', 'wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'grid_delete_query', OLD.`grid_delete_query`, NEW.`grid_delete_query`);
        END IF;

        IF IFNULL(NEW.`grid_insert_query`, '') <> IFNULL(OLD.`grid_insert_query`, '') THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY', 'wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'grid_insert_query', OLD.`grid_insert_query`, NEW.`grid_insert_query`);
        END IF;

        IF IFNULL(NEW.`grid_update_query`, '') <> IFNULL(OLD.`grid_update_query`, '') THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY', 'wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'grid_update_query', OLD.`grid_update_query`, NEW.`grid_update_query`);
        END IF;

        IF IFNULL(NEW.`depends_on_field`, '') <> IFNULL(OLD.`depends_on_field`, '') THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY', 'wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'depends_on_field', OLD.`depends_on_field`, NEW.`depends_on_field`);
        END IF;

        IF IFNULL(NEW.`depends_on_operator`, '') <> IFNULL(OLD.`depends_on_operator`, '') THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY', 'wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'depends_on_operator', OLD.`depends_on_operator`, NEW.`depends_on_operator`);
        END IF;

        IF IFNULL(NEW.`depends_on_value`, '') <> IFNULL(OLD.`depends_on_value`, '') THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY', 'wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'depends_on_value', OLD.`depends_on_value`, NEW.`depends_on_value`);
        END IF;

        IF IFNULL(NEW.`language_code`, '') <> IFNULL(OLD.`language_code`, '') THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY', 'wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'language_code', OLD.`language_code`, NEW.`language_code`);
        END IF;

        IF IFNULL(NEW.`custom_script`, '') <> IFNULL(OLD.`custom_script`, '') THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY', 'wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'custom_script', OLD.`custom_script`, NEW.`custom_script`);
        END IF;

        IF IFNULL(NEW.`also_save_seo_value`, '') <> IFNULL(OLD.`also_save_seo_value`, '') THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY', 'wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'also_save_seo_value', OLD.`also_save_seo_value`, NEW.`also_save_seo_value`);
        END IF;

        IF IFNULL(NEW.`depends_on_action`, '') <> IFNULL(OLD.`depends_on_action`, '') THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY', 'wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'depends_on_action', OLD.`depends_on_action`, NEW.`depends_on_action`);
        END IF;

        IF IFNULL(NEW.`save_on_change`, '') <> IFNULL(OLD.`save_on_change`, '') THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY', 'wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'save_on_change', OLD.`save_on_change`, NEW.`save_on_change`);
        END IF;

        IF IFNULL(NEW.`extended_explanation`, '') <> IFNULL(OLD.`extended_explanation`, '') THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY', 'wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'extended_explanation', OLD.`extended_explanation`, NEW.`extended_explanation`);
        END IF;

        IF IFNULL(NEW.`label_style`, '') <> IFNULL(OLD.`label_style`, '') THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY', 'wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'label_style', OLD.`label_style`, NEW.`label_style`);
        END IF;

        IF IFNULL(NEW.`label_width`, '') <> IFNULL(OLD.`label_width`, '') THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY', 'wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'label_width', OLD.`label_width`, NEW.`label_width`);
        END IF;

        IF IFNULL(NEW.`enable_aggregation`, '') <> IFNULL(OLD.`enable_aggregation`, '') THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY', 'wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'enable_aggregation', OLD.`enable_aggregation`, NEW.`enable_aggregation`);
        END IF;

        IF IFNULL(NEW.`aggregate_options`, '') <> IFNULL(OLD.`aggregate_options`, '') THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY', 'wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'aggregate_options', OLD.`aggregate_options`, NEW.`aggregate_options`);
        END IF;

        IF IFNULL(NEW.`access_key`, '') <> IFNULL(OLD.`access_key`, '') THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY', 'wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'access_key', OLD.`access_key`, NEW.`access_key`);
        END IF;

        IF IFNULL(NEW.`visibility_path_regex`, '') <> IFNULL(OLD.`visibility_path_regex`, '') THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_ENTITYPROPERTY', 'wiser_entityproperty', NEW.id, IFNULL(@_username, USER()), 'visibility_path_regex', OLD.`visibility_path_regex`, NEW.`visibility_path_regex`);
        END IF;
    END IF;
END;

DROP TRIGGER IF EXISTS `EntityPropertyDelete`;
CREATE TRIGGER `EntityPropertyDelete` AFTER DELETE ON `wiser_entityproperty` FOR EACH ROW BEGIN
    IF IFNULL(@saveHistory, TRUE) = TRUE THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('DELETE_ENTITYPROPERTY', 'wiser_entityproperty', OLD.id, IFNULL(@_username, USER()), '', '', '');
    END IF;
END;

-- ----------------------------
-- Triggers structure for table wiser_item
-- ----------------------------
DROP TRIGGER IF EXISTS `ItemInsert`;
CREATE TRIGGER `ItemInsert` AFTER INSERT ON `wiser_item` FOR EACH ROW
BEGIN
    IF IFNULL(@saveHistory, TRUE) = TRUE THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('CREATE_ITEM','wiser_item', NEW.id, IFNULL(@_username, USER()), '', '', '');

        IF IFNULL(NEW.`unique_uuid`, '') <> '' THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_ITEM','wiser_item',NEW.`id`,IFNULL(@_username, USER()),'unique_uuid',NULL,NEW.`unique_uuid`);
        END IF;

        IF IFNULL(NEW.`entity_type`, '') <> '' THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_ITEM','wiser_item',NEW.`id`,IFNULL(@_username, USER()),'entity_type',NULL,NEW.`entity_type`);
        END IF;

        IF IFNULL(NEW.`moduleid`, '') <> '' THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_ITEM','wiser_item',NEW.`id`,IFNULL(@_username, USER()),'moduleid',NULL,NEW.`moduleid`);
        END IF;

        IF IFNULL(NEW.`published_environment`, '') <> '' THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_ITEM','wiser_item',NEW.`id`,IFNULL(@_username, USER()),'published_environment',NULL,NEW.`published_environment`);
        END IF;

        IF IFNULL(NEW.`readonly`, '') <> '' THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_ITEM','wiser_item',NEW.`id`,IFNULL(@_username, USER()),'readonly',NULL,NEW.`readonly`);
        END IF;

        IF IFNULL(NEW.`title`, '') <> '' THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_ITEM','wiser_item',NEW.`id`,IFNULL(@_username, USER()),'title',NULL,NEW.`title`);
        END IF;

        IF IFNULL(NEW.`original_item_id`, '') <> '' THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_ITEM','wiser_item',NEW.`id`,IFNULL(@_username, USER()),'original_item_id',NULL,NEW.`original_item_id`);
        END IF;

        IF IFNULL(NEW.`parent_item_id`, '') <> '' THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_ITEM','wiser_item',NEW.`id`,IFNULL(@_username, USER()),'parent_item_id',NULL,NEW.`parent_item_id`);
        END IF;

        IF IFNULL(NEW.`ordering`, '') <> '' THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_ITEM','wiser_item',NEW.`id`,IFNULL(@_username, USER()),'ordering',NULL,NEW.`ordering`);
        END IF;

        IF NEW.`json` IS NOT NULL THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_ITEM','wiser_item',NEW.`id`,IFNULL(@_username, USER()),'json',NULL,NEW.`json`);
        END IF;

        IF NEW.`json_last_processed_date` IS NOT NULL THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_ITEM','wiser_item',NEW.`id`,IFNULL(@_username, USER()),'json_last_processed_date',NULL,NEW.`json_last_processed_date`);
        END IF;
    END IF;
END;

DROP TRIGGER IF EXISTS `ItemUpdate`;
CREATE TRIGGER `ItemUpdate` AFTER UPDATE ON `wiser_item` FOR EACH ROW
BEGIN
    IF IFNULL(@saveHistory, TRUE) = TRUE THEN
        IF IFNULL(NEW.`unique_uuid`, '') <> IFNULL(OLD.`unique_uuid`, '') THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_ITEM','wiser_item',NEW.`id`,IFNULL(@_username, USER()),'unique_uuid',OLD.`unique_uuid`,NEW.`unique_uuid`);
        END IF;

        IF IFNULL(NEW.`entity_type`, '') <> IFNULL(OLD.`entity_type`, '') THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_ITEM','wiser_item',NEW.`id`,IFNULL(@_username, USER()),'entity_type',OLD.`entity_type`,NEW.`entity_type`);
        END IF;

        IF IFNULL(NEW.`moduleid`, '') <> IFNULL(OLD.`moduleid`, '') THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_ITEM','wiser_item',NEW.`id`,IFNULL(@_username, USER()),'moduleid',OLD.`moduleid`,NEW.`moduleid`);
        END IF;

        IF IFNULL(NEW.`published_environment`, '') <> IFNULL(OLD.`published_environment`, '') THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_ITEM','wiser_item',NEW.`id`,IFNULL(@_username, USER()),'published_environment',OLD.`published_environment`,NEW.`published_environment`);
        END IF;

        IF IFNULL(NEW.`readonly`, '') <> IFNULL(OLD.`readonly`, '') THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_ITEM','wiser_item',NEW.`id`,IFNULL(@_username, USER()),'readonly',OLD.`readonly`,NEW.`readonly`);
        END IF;

        IF IFNULL(NEW.`title`, '') <> IFNULL(OLD.`title`, '') THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_ITEM','wiser_item',NEW.`id`,IFNULL(@_username, USER()),'title',OLD.`title`,NEW.`title`);
        END IF;

        IF IFNULL(NEW.`original_item_id`, '') <> IFNULL(OLD.`original_item_id`, '') THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_ITEM','wiser_item',NEW.`id`,IFNULL(@_username, USER()),'original_item_id',OLD.`original_item_id`,NEW.`original_item_id`);
        END IF;

        IF IFNULL(NEW.`parent_item_id`, '') <> IFNULL(OLD.`parent_item_id`, '') THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_ITEM','wiser_item',NEW.`id`,IFNULL(@_username, USER()),'parent_item_id',OLD.`parent_item_id`,NEW.`parent_item_id`);
        END IF;

        IF IFNULL(NEW.`ordering`, '') <> IFNULL(OLD.`ordering`, '') THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_ITEM','wiser_item',NEW.`id`,IFNULL(@_username, USER()),'ordering',OLD.`ordering`,NEW.`ordering`);
        END IF;

        IF NEW.`json` <> OLD.`json` THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_ITEM','wiser_item',NEW.`id`,IFNULL(@_username, USER()),'json',OLD.`json`,NEW.`json`);
        END IF;

        IF NEW.`json_last_processed_date` <> OLD.`json_last_processed_date` THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_ITEM','wiser_item',NEW.`id`,IFNULL(@_username, USER()),'json_last_processed_date',OLD.`json_last_processed_date`,NEW.`json_last_processed_date`);
        END IF;
    END IF;
END;

DROP TRIGGER IF EXISTS `ItemDelete`;
CREATE TRIGGER `ItemDelete` AFTER DELETE ON `wiser_item` FOR EACH ROW
BEGIN
    DELETE d.* FROM wiser_itemlink l JOIN wiser_itemlinkdetail AS d ON d.itemlink_id = l.id WHERE l.item_id = OLD.id OR l.destination_item_id = OLD.id;
    DELETE FROM wiser_itemlink WHERE (item_id = OLD.id OR destination_item_id = OLD.id) AND OLD.id > 0;
    DELETE FROM wiser_itemdetail WHERE item_id = OLD.id;
    DELETE FROM wiser_itemfile WHERE item_id = OLD.id;

    IF IFNULL(@saveHistory, TRUE) = TRUE THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('DELETE_ITEM','wiser_item', OLD.id, IFNULL(@_username, USER()), OLD.entity_type, '', '');
    END IF;
END;

-- ----------------------------
-- Triggers structure for table wiser_itemdetail
-- ----------------------------
DROP TRIGGER IF EXISTS `DetailInsert`;
CREATE TRIGGER `DetailInsert` AFTER INSERT ON `wiser_itemdetail` FOR EACH ROW BEGIN
    IF IFNULL(@saveHistory, TRUE) = TRUE THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue, language_code, groupname)
        VALUES ('UPDATE_ITEM','wiser_itemdetail', NEW.item_id, IFNULL(@_username, USER()), NEW.`key`, '', CONCAT_WS('', NEW.`value`, NEW.`long_value`), NEW.language_code, NEW.groupname);
    END IF;

    IF IFNULL(@performParentUpdate, TRUE) = TRUE THEN
        IF (NEW.`item_id` IS NOT NULL AND NEW.`item_id` <> IFNULL(@previousItemId, 0)) THEN
            INSERT `wiser_parent_updates`(`target_id`, `target_table`, `changed_on`, `changed_by`)
            VALUES (
                NEW.`item_id`,
                'wiser_item',
                NOW(),
                IFNULL(@_username, USER())
            );
        END IF;

        SET @previousItemId = NEW.`item_id`;
    END IF;
END;

DROP TRIGGER IF EXISTS `DetailUpdate`;
CREATE TRIGGER `DetailUpdate` AFTER UPDATE ON `wiser_itemdetail` FOR EACH ROW BEGIN
    DECLARE oldValue MEDIUMTEXT;
    DECLARE newValue MEDIUMTEXT;
    
    SET oldValue = CONCAT_WS('', OLD.`value`, OLD.`long_value`);
    SET newValue = CONCAT_WS('', NEW.`value`, NEW.`long_value`);

    IF oldValue <> newValue THEN
        IF IFNULL(@saveHistory, TRUE) = TRUE THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue, language_code, groupname)
            VALUES ('UPDATE_ITEM', 'wiser_itemdetail', NEW.item_id, IFNULL(@_username, USER()), NEW.`key`, oldValue, newValue, NEW.language_code, NEW.groupname);
        END IF;

        IF IFNULL(@performParentUpdate, TRUE) = TRUE THEN
            IF (NEW.`item_id` IS NOT NULL AND NEW.`item_id` <> IFNULL(@previousItemId, 0)) THEN
                INSERT `wiser_parent_updates`(`target_id`, `target_table`, `changed_on`, `changed_by`)
                VALUES (
                    NEW.`item_id`,
                    'wiser_item',
                    NOW(),
                    IFNULL(@_username, USER())
                );
            END IF;

            SET @previousItemId = NEW.`item_id`;
        END IF;
    END IF;
END;

DROP TRIGGER IF EXISTS `DetailDelete`;
CREATE TRIGGER `DetailDelete` AFTER DELETE ON `wiser_itemdetail` FOR EACH ROW BEGIN
    IF IFNULL(@saveHistory, TRUE) = TRUE THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue, language_code, groupname)
        VALUES ('UPDATE_ITEM', 'wiser_itemdetail', OLD.item_id, IFNULL(@_username, USER()), OLD.`key`, CONCAT_WS('', OLD.`value`, OLD.`long_value`), '', OLD.language_code, OLD.groupname);
    END IF;

    IF IFNULL(@performParentUpdate, TRUE) = TRUE THEN
        IF (OLD.`item_id` IS NOT NULL AND OLD.`item_id` <> IFNULL(@previousItemId, 0)) THEN
            INSERT `wiser_parent_updates`(`target_id`, `target_table`, `changed_on`, `changed_by`)
            VALUES (
                OLD.`item_id`,
                'wiser_item',
                NOW(),
                IFNULL(@_username, USER())
            );
        END IF;

        SET @previousItemId = OLD.`item_id`;
    END IF;
END;

-- ----------------------------
-- Triggers structure for table wiser_itemlink
-- ----------------------------
DROP TRIGGER IF EXISTS `LinkInsert`;
CREATE TRIGGER `LinkInsert` AFTER INSERT ON `wiser_itemlink` FOR EACH ROW BEGIN
    IF IFNULL(@saveHistory, TRUE) = TRUE THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('ADD_LINK', 'wiser_itemlink', NEW.destination_item_id, IFNULL(@_username, USER()), CONCAT(IFNULL(NEW.`type`, '1'), ',', IFNULL(NEW.`ordering`, '0')), NULL, NEW.item_id);
    END IF;

    IF IFNULL(@performParentUpdate, TRUE) = TRUE THEN
        IF (NEW.`item_id` IS NOT NULL AND NEW.`item_id` <> IFNULL(@previousItemId, 0)) THEN
            INSERT `wiser_parent_updates`(`target_id`, `target_table`, `changed_on`, `changed_by`)
            VALUES (
                NEW.`item_id`,
                'wiser_item',
                NOW(),
                IFNULL(@_username, USER())
            );
        END IF;

        SET @previousItemId = NEW.`item_id`;
    END IF;
END;

DROP TRIGGER IF EXISTS `LinkUpdate`;
CREATE TRIGGER `LinkUpdate` AFTER UPDATE ON `wiser_itemlink` FOR EACH ROW BEGIN
    DECLARE updateChangeDate BOOL;
    
    SET updateChangeDate = FALSE;
    

    IF IFNULL(@saveHistory, TRUE) = TRUE AND NEW.`destination_item_id` <> OLD.`destination_item_id` THEN
        SET updateChangeDate = TRUE;
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('CHANGE_LINK', 'wiser_itemlink', OLD.id, IFNULL(@_username, USER()), 'destination_item_id', OLD.destination_item_id, NEW.destination_item_id);
    END IF;

    IF IFNULL(@saveHistory, TRUE) = TRUE AND NEW.`item_id` <> OLD.`item_id` THEN
        SET updateChangeDate = TRUE;
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('CHANGE_LINK', 'wiser_itemlink', OLD.id, IFNULL(@_username, USER()), 'item_id', OLD.item_id, NEW.item_id);
    END IF;

    IF IFNULL(@saveHistory, TRUE) = TRUE AND NEW.`type` <> OLD.`type` THEN
        SET updateChangeDate = TRUE;
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('CHANGE_LINK', 'wiser_itemlink', OLD.id, IFNULL(@_username, USER()), 'type', OLD.type, NEW.type);
    END IF;

    IF IFNULL(@saveHistory, TRUE) = TRUE AND NEW.`ordering` <> OLD.`ordering` THEN
        SET updateChangeDate = TRUE;
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('CHANGE_LINK', 'wiser_itemlink', OLD.id, IFNULL(@_username, USER()), 'ordering', OLD.ordering, NEW.ordering);
    END IF;

    IF IFNULL(@performParentUpdate, TRUE) = TRUE AND updateChangeDate = TRUE THEN
        IF (NEW.`item_id` IS NOT NULL AND NEW.`item_id` <> IFNULL(@previousItemId, 0)) THEN
            INSERT `wiser_parent_updates`(`target_id`, `target_table`, `changed_on`, `changed_by`)
            VALUES (
                NEW.`item_id`,
                'wiser_item',
                NOW(),
                IFNULL(@_username, USER())
            );
        END IF;

        SET @previousItemId = NEW.`item_id`;
    END IF;
END;

DROP TRIGGER IF EXISTS `linkDelete`;
CREATE TRIGGER `linkDelete` AFTER DELETE ON `wiser_itemlink` FOR EACH ROW BEGIN
    DELETE FROM wiser_itemlinkdetail WHERE itemlink_id = OLD.id;
    DELETE FROM wiser_itemfile WHERE itemlink_id = OLD.id;

    IF IFNULL(@saveHistory, TRUE) = TRUE THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('REMOVE_LINK', 'wiser_itemlink', OLD.destination_item_id, IFNULL(@_username, USER()), OLD.`type`, OLD.item_id, NULL);
    END IF;

    IF IFNULL(@performParentUpdate, TRUE) = TRUE THEN
        IF (OLD.`item_id` IS NOT NULL AND OLD.`item_id` <> IFNULL(@previousItemId, 0)) THEN
            INSERT `wiser_parent_updates`(`target_id`, `target_table`, `changed_on`, `changed_by`)
            VALUES (
                OLD.`item_id`,
                'wiser_item',
                NOW(),
                IFNULL(@_username, USER())
            );
        END IF;

        SET @previousItemId = OLD.`item_id`;
    END IF;
END;
-- ----------------------------
-- Triggers structure for table wiser_itemlinkdetail
-- ----------------------------
DROP TRIGGER IF EXISTS `LinkDetailInsert`;
CREATE TRIGGER `LinkDetailInsert` AFTER INSERT ON `wiser_itemlinkdetail` FOR EACH ROW BEGIN
    IF IFNULL(@saveHistory, TRUE) = TRUE THEN
        INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue, language_code, groupname)
        VALUES ('UPDATE_ITEMLINKDETAIL','wiser_itemlinkdetail',NEW.itemlink_id,IFNULL(@_username, USER()),NEW.`key`,'',CONCAT_WS('',NEW.`value`,NEW.`long_value`), NEW.language_code, NEW.groupname);
    END IF;
END;

DROP TRIGGER IF EXISTS `LinkDetailUpdate`;
CREATE TRIGGER `LinkDetailUpdate` AFTER UPDATE ON `wiser_itemlinkdetail` FOR EACH ROW BEGIN
    DECLARE oldValue MEDIUMTEXT;
    DECLARE newValue MEDIUMTEXT;

    IF IFNULL(@saveHistory, TRUE) = TRUE THEN
        SET oldValue = CONCAT_WS('',OLD.`value`,OLD.`long_value`);
        SET newValue = CONCAT_WS('',NEW.`value`,NEW.`long_value`);
        IF newValue <> oldValue THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue, language_code, groupname)
            VALUES ('UPDATE_ITEMLINKDETAIL','wiser_itemlinkdetail',NEW.itemlink_id,IFNULL(@_username, USER()),NEW.`key`,oldValue,newValue, NEW.language_code, NEW.groupname);
        END IF;
    END IF;
END;

DROP TRIGGER IF EXISTS `LinkDetailDelete`;
CREATE TRIGGER `LinkDetailDelete` AFTER DELETE ON `wiser_itemlinkdetail` FOR EACH ROW BEGIN
    IF IFNULL(@saveHistory, TRUE) = TRUE THEN
        INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue, language_code, groupname)
        VALUES ('UPDATE_ITEMLINKDETAIL','wiser_itemlinkdetail',OLD.itemlink_id,IFNULL(@_username, USER()),OLD.`key`,CONCAT_WS('',OLD.`value`,OLD.`long_value`),'', OLD.language_code, OLD.groupname);
    END IF;
END;

-- ----------------------------
-- Triggers structure for table wiser_itemfile
-- ----------------------------
DROP TRIGGER IF EXISTS `FileInsert`;
CREATE TRIGGER `FileInsert` AFTER INSERT ON `wiser_itemfile` FOR EACH ROW BEGIN
    DECLARE prevLinkedItemId BIGINT;

    IF IFNULL(@saveHistory, TRUE) = TRUE THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('ADD_FILE', 'wiser_itemfile', NEW.id, IFNULL(@_username, USER()), IFNULL(NEW.property_name, ''), IF(IFNULL(NEW.item_id, 0) > 0, 'item_id', 'itemlink_id'), IF(IFNULL(NEW.item_id, 0) > 0, NEW.item_id, NEW.itemlink_id));

        IF IFNULL(NEW.content_type, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', 'wiser_itemfile', NEW.id, IFNULL(@_username, USER()), 'content_type', NULL, NEW.content_type);
        END IF;

        IF NEW.content IS NOT NULL THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', 'wiser_itemfile', NEW.id, IFNULL(@_username, USER()), 'content_length', '0 bytes', CONCAT(FORMAT(OCTET_LENGTH(NEW.content), 0, 'nl-NL'), ' bytes'));
        END IF;

        IF IFNULL(NEW.content_url, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', 'wiser_itemfile', NEW.id, IFNULL(@_username, USER()), 'content_url', NULL, NEW.content_url);
        END IF;

        IF IFNULL(NEW.width, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', 'wiser_itemfile', NEW.id, IFNULL(@_username, USER()), 'width', NULL, NEW.width);
        END IF;

        IF IFNULL(NEW.height, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', 'wiser_itemfile', NEW.id, IFNULL(@_username, USER()), 'height', NULL, NEW.height);
        END IF;

        IF IFNULL(NEW.file_name, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', 'wiser_itemfile', NEW.id, IFNULL(@_username, USER()), 'file_name', NULL, NEW.file_name);
        END IF;

        IF IFNULL(NEW.extension, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', 'wiser_itemfile', NEW.id, IFNULL(@_username, USER()), 'extension', NULL, NEW.extension);
        END IF;

        IF IFNULL(NEW.title, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', 'wiser_itemfile', NEW.id, IFNULL(@_username, USER()), 'title', NULL, NEW.title);
        END IF;

        IF IFNULL(NEW.property_name, '') <> '' THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', 'wiser_itemfile', NEW.id, IFNULL(@_username, USER()), 'property_name', NULL, NEW.property_name);
        END IF;

        IF IFNULL(NEW.ordering, 0) <> 0 THEN
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', 'wiser_itemfile', NEW.id, IFNULL(@_username, USER()), 'ordering', NULL, NEW.ordering);
        END IF;
    END IF;

    IF IFNULL(@performParentUpdate, TRUE) = TRUE THEN
        IF (NEW.`item_id` IS NOT NULL AND NEW.`item_id` <> IFNULL(@previousItemId, 0)) THEN
            INSERT `wiser_parent_updates`(`target_id`, `target_table`, `changed_on`, `changed_by`)
            VALUES (
                NEW.`item_id`,
                'wiser_item',
                NOW(),
                IFNULL(@_username, USER())
            );
        END IF;

        SET @previousItemId = NEW.`item_id`;
    END IF;
END;

DROP TRIGGER IF EXISTS `FileUpdate`;
CREATE TRIGGER `FileUpdate` AFTER UPDATE ON `wiser_itemfile` FOR EACH ROW BEGIN
    DECLARE updateChangeDate BOOL;

    SET updateChangeDate = FALSE;

    IF IFNULL(@saveHistory, TRUE) = TRUE THEN
        IF NEW.item_id <> OLD.item_id THEN
            SET updateChangeDate = TRUE;
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', 'wiser_itemfile', OLD.id, IFNULL(@_username, USER()), 'item_id', OLD.item_id, NEW.item_id);
        END IF;

        IF NEW.content_type <> OLD.content_type THEN
            SET updateChangeDate = TRUE;
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', 'wiser_itemfile', OLD.id, IFNULL(@_username, USER()), 'content_type', OLD.content_type, NEW.content_type);
        END IF;

        IF NEW.content <> OLD.content THEN
            SET updateChangeDate = TRUE;
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', 'wiser_itemfile', OLD.id, IFNULL(@_username, USER()), 'content_length', CONCAT(FORMAT(OCTET_LENGTH(OLD.content), 0, 'nl-NL'), ' bytes'), CONCAT(FORMAT(OCTET_LENGTH(NEW.content), 0, 'nl-NL'), ' bytes'));
        END IF;

        IF NEW.content_url <> OLD.content_url THEN
            SET updateChangeDate = TRUE;
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', 'wiser_itemfile', OLD.id, IFNULL(@_username, USER()), 'content_url', OLD.content_url, NEW.content_url);
        END IF;

        IF NEW.width <> OLD.width THEN
            SET updateChangeDate = TRUE;
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', 'wiser_itemfile', OLD.id, IFNULL(@_username, USER()), 'width', OLD.width, NEW.width);
        END IF;

        IF NEW.height <> OLD.height THEN
            SET updateChangeDate = TRUE;
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', 'wiser_itemfile', OLD.id, IFNULL(@_username, USER()), 'height', OLD.height, NEW.height);
        END IF;

        IF NEW.file_name <> OLD.file_name THEN
            SET updateChangeDate = TRUE;
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', 'wiser_itemfile', OLD.id, IFNULL(@_username, USER()), 'file_name', OLD.file_name, NEW.file_name);
        END IF;

        IF NEW.extension <> OLD.extension THEN
            SET updateChangeDate = TRUE;
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', 'wiser_itemfile', OLD.id, IFNULL(@_username, USER()), 'extension', OLD.extension, NEW.extension);
        END IF;

        IF NEW.title <> OLD.title THEN
            SET updateChangeDate = TRUE;
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', 'wiser_itemfile', OLD.id, IFNULL(@_username, USER()), 'title', OLD.title, NEW.title);
        END IF;

        IF NEW.property_name <> OLD.property_name THEN
            SET updateChangeDate = TRUE;
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', 'wiser_itemfile', OLD.id, IFNULL(@_username, USER()), 'property_name', OLD.property_name, NEW.property_name);
        END IF;

        IF NEW.itemlink_id <> OLD.itemlink_id THEN
            SET updateChangeDate = TRUE;
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', 'wiser_itemfile', OLD.id, IFNULL(@_username, USER()), 'itemlink_id', OLD.itemlink_id, NEW.itemlink_id);
        END IF;

        IF NEW.ordering <> OLD.ordering THEN
            SET updateChangeDate = TRUE;
            INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
            VALUES ('UPDATE_FILE', 'wiser_itemfile', OLD.id, IFNULL(@_username, USER()), 'ordering', OLD.ordering, NEW.ordering);
        END IF;
    END IF;

    IF IFNULL(@performParentUpdate, TRUE) = TRUE AND updateChangeDate = TRUE THEN
        IF (NEW.`item_id` IS NOT NULL AND NEW.`item_id` <> IFNULL(@previousItemId, 0)) THEN
            INSERT `wiser_parent_updates`(`target_id`, `target_table`, `changed_on`, `changed_by`)
            VALUES (
                NEW.`item_id`,
                'wiser_item',
                NOW(),
                IFNULL(@_username, USER())
            );
        END IF;

        SET @previousItemId = NEW.`item_id`;
    END IF;
END;

DROP TRIGGER IF EXISTS `FileDelete`;
CREATE TRIGGER `FileDelete` AFTER DELETE ON `wiser_itemfile` FOR EACH ROW BEGIN
    IF IFNULL(@saveHistory, TRUE) = TRUE THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('DELETE_FILE', 'wiser_itemfile', OLD.id, IFNULL(@_username, USER()), IFNULL(OLD.property_name, ''), IF(IFNULL(OLD.item_id, 0) > 0, 'item_id', 'itemlink_id'), IF(IFNULL(OLD.item_id, 0) > 0, OLD.item_id, OLD.itemlink_id));
    END IF;

    IF IFNULL(@performParentUpdate, TRUE) = TRUE THEN
        IF (OLD.`item_id` IS NOT NULL AND OLD.`item_id` <> IFNULL(@previousItemId, 0)) THEN
            INSERT `wiser_parent_updates`(`target_id`, `target_table`, `changed_on`, `changed_by`)
            VALUES (
                OLD.`item_id`,
                'wiser_item',
                NOW(),
                IFNULL(@_username, USER())
            );
        END IF;

        SET @previousItemId = OLD.`item_id`;
    END IF;
END;

-- ----------------------------
-- Triggers structure for table wiser_module
-- ----------------------------
DROP TRIGGER IF EXISTS `ModuleInsert`;
CREATE TRIGGER `ModuleInsert` AFTER INSERT ON `wiser_module` FOR EACH ROW BEGIN
    INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
    VALUES ('INSERT_MODULE', 'wiser_module', NEW.id, IFNULL(@_username, USER()), 'id', NULL, NEW.id);

    IF IFNULL(NEW.`custom_query`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_MODULE', 'wiser_module', NEW.id, IFNULL(@_username, USER()), 'custom_query', NULL, NEW.`custom_query`);
    END IF;

    IF IFNULL(NEW.`count_query`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_MODULE', 'wiser_module', NEW.id, IFNULL(@_username, USER()), 'count_query', NULL, NEW.`count_query`);
    END IF;

    IF IFNULL(NEW.`options`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_MODULE', 'wiser_module', NEW.id, IFNULL(@_username, USER()), 'options', NULL, NEW.`options`);
    END IF;

    IF IFNULL(NEW.`name`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_MODULE', 'wiser_module', NEW.id, IFNULL(@_username, USER()), 'name', NULL, NEW.`name`);
    END IF;

    IF IFNULL(NEW.`icon`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_MODULE', 'wiser_module', NEW.id, IFNULL(@_username, USER()), 'icon', NULL, NEW.`icon`);
    END IF;

    IF IFNULL(NEW.`color`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_MODULE', 'wiser_module', NEW.id, IFNULL(@_username, USER()), 'color', NULL, NEW.`color`);
    END IF;

    IF IFNULL(NEW.`type`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_MODULE', 'wiser_module', NEW.id, IFNULL(@_username, USER()), 'type', NULL, NEW.`type`);
    END IF;

    IF IFNULL(NEW.`group`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_MODULE', 'wiser_module', NEW.id, IFNULL(@_username, USER()), 'group', NULL, NEW.`group`);
    END IF;
END;

DROP TRIGGER IF EXISTS `ModuleUpdate`;
CREATE TRIGGER `ModuleUpdate` AFTER UPDATE ON `wiser_module` FOR EACH ROW BEGIN
    IF IFNULL(NEW.`custom_query`, '') <> IFNULL(OLD.`custom_query`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_MODULE', 'wiser_module', NEW.id, IFNULL(@_username, USER()), 'custom_query', OLD.`custom_query`, NEW.`custom_query`);
    END IF;

    IF IFNULL(NEW.`count_query`, '') <> IFNULL(OLD.`count_query`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_MODULE', 'wiser_module', NEW.id, IFNULL(@_username, USER()), 'count_query', OLD.`count_query`, NEW.`count_query`);
    END IF;

    IF IFNULL(NEW.`options`, '') <> IFNULL(OLD.`options`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_MODULE', 'wiser_module', NEW.id, IFNULL(@_username, USER()), 'options', OLD.`options`, NEW.`options`);
    END IF;

    IF IFNULL(NEW.`name`, '') <> IFNULL(OLD.`name`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_MODULE', 'wiser_module', NEW.id, IFNULL(@_username, USER()), 'name', OLD.`name`, NEW.`name`);
    END IF;

    IF IFNULL(NEW.`icon`, '') <> IFNULL(OLD.`icon`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_MODULE', 'wiser_module', NEW.id, IFNULL(@_username, USER()), 'icon', OLD.`icon`, NEW.`icon`);
    END IF;

    IF IFNULL(NEW.`color`, '') <> IFNULL(OLD.`color`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_MODULE', 'wiser_module', NEW.id, IFNULL(@_username, USER()), 'color', OLD.`color`, NEW.`color`);
    END IF;

    IF IFNULL(NEW.`type`, '') <> IFNULL(OLD.`type`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_MODULE', 'wiser_module', NEW.id, IFNULL(@_username, USER()), 'type', OLD.`type`, NEW.`type`);
    END IF;

    IF IFNULL(NEW.`group`, '') <> IFNULL(OLD.`group`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_MODULE', 'wiser_module', NEW.id, IFNULL(@_username, USER()), 'group', OLD.`group`, NEW.`group`);
    END IF;
END;

DROP TRIGGER IF EXISTS `ModuleDelete`;
CREATE TRIGGER `ModuleDelete` AFTER DELETE ON `wiser_module` FOR EACH ROW BEGIN
    INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
    VALUES ('DELETE_MODULE', 'wiser_module', OLD.id, IFNULL(@_username, USER()), 'name', OLD.name, '');
END;

-- ----------------------------
-- Triggers structure for table wiser_query
-- ----------------------------
DROP TRIGGER IF EXISTS `QueryInsert`;
CREATE TRIGGER `QueryInsert` AFTER INSERT ON `wiser_query` FOR EACH ROW BEGIN
    INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
    VALUES ('INSERT_QUERY', 'wiser_query', NEW.id, IFNULL(@_username, USER()), 'id', NULL, NEW.id);

    IF IFNULL(NEW.`description`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_QUERY', 'wiser_query', NEW.id, IFNULL(@_username, USER()), 'description', NULL, NEW.`description`);
    END IF;

    IF IFNULL(NEW.`query`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_QUERY', 'wiser_query', NEW.id, IFNULL(@_username, USER()), 'query', NULL, NEW.`query`);
    END IF;

    IF IFNULL(NEW.`show_in_export_module`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_QUERY', 'wiser_query', NEW.id, IFNULL(@_username, USER()), 'show_in_export_module', NULL, NEW.`show_in_export_module`);
    END IF;

    IF IFNULL(NEW.`show_in_communication_module`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_QUERY', 'wiser_query', NEW.id, IFNULL(@_username, USER()), 'show_in_communication_module', NULL, NEW.`show_in_communication_module`);
    END IF;
END;

DROP TRIGGER IF EXISTS `QueryUpdate`;
CREATE TRIGGER `QueryUpdate` AFTER UPDATE ON `wiser_query` FOR EACH ROW BEGIN
    IF IFNULL(NEW.`description`, '') <> IFNULL(OLD.`description`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_QUERY', 'wiser_query', NEW.id, IFNULL(@_username, USER()), 'description', OLD.`description`, NEW.`description`);
    END IF;

    IF IFNULL(NEW.`query`, '') <> IFNULL(OLD.`query`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_QUERY', 'wiser_query', NEW.id, IFNULL(@_username, USER()), 'query', OLD.`query`, NEW.`query`);
    END IF;

    IF IFNULL(NEW.`show_in_export_module`, '') <> IFNULL(OLD.`show_in_export_module`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_QUERY', 'wiser_query', NEW.id, IFNULL(@_username, USER()), 'show_in_export_module', OLD.`show_in_export_module`, NEW.`show_in_export_module`);
    END IF;

    IF IFNULL(NEW.`show_in_communication_module`, '') <> IFNULL(OLD.`show_in_communication_module`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_QUERY', 'wiser_query', NEW.id, IFNULL(@_username, USER()), 'show_in_communication_module', OLD.`show_in_communication_module`, NEW.`show_in_communication_module`);
    END IF;
END;

DROP TRIGGER IF EXISTS `QueryDelete`;
CREATE TRIGGER `QueryDelete` AFTER DELETE ON `wiser_query` FOR EACH ROW BEGIN
    INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
    VALUES ('DELETE_QUERY', 'wiser_query', OLD.id, IFNULL(@_username, USER()), 'description', OLD.description, '');
END;

-- ----------------------------
-- Triggers structure for table wiser_entity
-- ----------------------------
DROP TRIGGER IF EXISTS `EntityInsert`;
CREATE TRIGGER `EntityInsert` AFTER INSERT ON `wiser_entity` FOR EACH ROW BEGIN
    INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
    VALUES ('INSERT_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'id', NULL, NEW.id);

    IF IFNULL(NEW.`name`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'name', NULL, NEW.`name`);
    END IF;

    IF IFNULL(NEW.`module_id`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'module_id', NULL, NEW.`module_id`);
    END IF;

    IF IFNULL(NEW.`accepted_childtypes`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'accepted_childtypes', NULL, NEW.`accepted_childtypes`);
    END IF;

    IF IFNULL(NEW.`icon`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'icon', NULL, NEW.`icon`);
    END IF;

    IF IFNULL(NEW.`icon_add`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'icon_add', NULL, NEW.`icon_add`);
    END IF;

    IF IFNULL(NEW.`icon_expanded`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'icon_expanded', NULL, NEW.`icon_expanded`);
    END IF;

    IF IFNULL(NEW.`show_in_tree_view`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'show_in_tree_view', NULL, NEW.`show_in_tree_view`);
    END IF;

    IF IFNULL(NEW.`query_after_insert`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'query_after_insert', NULL, NEW.`query_after_insert`);
    END IF;

    IF IFNULL(NEW.`query_after_update`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'query_after_update', NULL, NEW.`query_after_update`);
    END IF;

    IF IFNULL(NEW.`query_before_update`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'query_before_update', NULL, NEW.`query_before_update`);
    END IF;

    IF IFNULL(NEW.`query_before_delete`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'query_before_delete', NULL, NEW.`query_before_delete`);
    END IF;

    IF IFNULL(NEW.`color`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'color', NULL, NEW.`color`);
    END IF;

    IF IFNULL(NEW.`show_in_search`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'show_in_search', NULL, NEW.`show_in_search`);
    END IF;

    IF IFNULL(NEW.`show_overview_tab`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'show_overview_tab', NULL, NEW.`show_overview_tab`);
    END IF;

    IF IFNULL(NEW.`save_title_as_seo`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'save_title_as_seo', NULL, NEW.`save_title_as_seo`);
    END IF;

    IF IFNULL(NEW.`api_after_insert`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'api_after_insert', NULL, NEW.`api_after_insert`);
    END IF;

    IF IFNULL(NEW.`api_after_update`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'api_after_update', NULL, NEW.`api_after_update`);
    END IF;

    IF IFNULL(NEW.`api_before_update`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'api_before_update', NULL, NEW.`api_before_update`);
    END IF;

    IF IFNULL(NEW.`api_before_delete`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'api_before_delete', NULL, NEW.`api_before_delete`);
    END IF;

    IF IFNULL(NEW.`show_title_field`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'show_title_field', NULL, NEW.`show_title_field`);
    END IF;

    IF IFNULL(NEW.`friendly_name`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'friendly_name', NULL, NEW.`friendly_name`);
    END IF;

    IF IFNULL(NEW.`default_ordering`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'default_ordering', NULL, NEW.`default_ordering`);
    END IF;

    IF IFNULL(NEW.`save_history`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'save_history', NULL, NEW.`save_history`);
    END IF;

    IF IFNULL(NEW.`enable_multiple_environments`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'enable_multiple_environments', NULL, NEW.`enable_multiple_environments`);
    END IF;

    IF IFNULL(NEW.`dedicated_table_prefix`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'dedicated_table_prefix', NULL, NEW.`dedicated_table_prefix`);
    END IF;

    IF IFNULL(NEW.`store_type`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'store_type', NULL, NEW.`store_type`);
    END IF;
END;

DROP TRIGGER IF EXISTS `EntityUpdate`;
CREATE TRIGGER `EntityUpdate` AFTER UPDATE ON `wiser_entity` FOR EACH ROW BEGIN
    IF IFNULL(NEW.`name`, '') <> IFNULL(OLD.`name`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'name', OLD.`name`, NEW.`name`);
    END IF;

    IF IFNULL(NEW.`module_id`, '') <> IFNULL(OLD.`module_id`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'module_id', OLD.`module_id`, NEW.`module_id`);
    END IF;

    IF IFNULL(NEW.`accepted_childtypes`, '') <> IFNULL(OLD.`accepted_childtypes`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'accepted_childtypes', OLD.`accepted_childtypes`, NEW.`accepted_childtypes`);
    END IF;

    IF IFNULL(NEW.`icon`, '') <> IFNULL(OLD.`icon`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'icon', OLD.`icon`, NEW.`icon`);
    END IF;

    IF IFNULL(NEW.`icon_expanded`, '') <> IFNULL(OLD.`icon_expanded`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'icon_expanded', OLD.`icon_expanded`, NEW.`icon_expanded`);
    END IF;

    IF IFNULL(NEW.`icon_add`, '') <> IFNULL(OLD.`icon_add`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'icon_add', OLD.`icon_add`, NEW.`icon_add`);
    END IF;

    IF IFNULL(NEW.`show_in_tree_view`, '') <> IFNULL(OLD.`show_in_tree_view`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'show_in_tree_view', OLD.`show_in_tree_view`, NEW.`show_in_tree_view`);
    END IF;

    IF IFNULL(NEW.`query_after_insert`, '') <> IFNULL(OLD.`query_after_insert`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'query_after_insert', OLD.`query_after_insert`, NEW.`query_after_insert`);
    END IF;

    IF IFNULL(NEW.`query_after_update`, '') <> IFNULL(OLD.`query_after_update`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'query_after_update', OLD.`query_after_update`, NEW.`query_after_update`);
    END IF;

    IF IFNULL(NEW.`query_before_update`, '') <> IFNULL(OLD.`query_before_update`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'query_before_update', OLD.`query_before_update`, NEW.`query_before_update`);
    END IF;

    IF IFNULL(NEW.`query_before_delete`, '') <> IFNULL(OLD.`query_before_delete`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'query_before_delete', OLD.`query_before_delete`, NEW.`query_before_delete`);
    END IF;

    IF IFNULL(NEW.`color`, '') <> IFNULL(OLD.`color`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'color', OLD.`color`, NEW.`color`);
    END IF;

    IF IFNULL(NEW.`show_in_search`, '') <> IFNULL(OLD.`show_in_search`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'show_in_search', OLD.`show_in_search`, NEW.`show_in_search`);
    END IF;

    IF IFNULL(NEW.`show_overview_tab`, '') <> IFNULL(OLD.`show_overview_tab`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'show_overview_tab', OLD.`show_overview_tab`, NEW.`show_overview_tab`);
    END IF;

    IF IFNULL(NEW.`save_title_as_seo`, '') <> IFNULL(OLD.`save_title_as_seo`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'save_title_as_seo', OLD.`save_title_as_seo`, NEW.`save_title_as_seo`);
    END IF;

    IF IFNULL(NEW.`api_after_insert`, '') <> IFNULL(OLD.`api_after_insert`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'api_after_insert', OLD.`api_after_insert`, NEW.`api_after_insert`);
    END IF;

    IF IFNULL(NEW.`api_after_update`, '') <> IFNULL(OLD.`api_after_update`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'api_after_update', OLD.`api_after_update`, NEW.`api_after_update`);
    END IF;

    IF IFNULL(NEW.`api_before_update`, '') <> IFNULL(OLD.`api_before_update`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'api_before_update', OLD.`api_before_update`, NEW.`api_before_update`);
    END IF;

    IF IFNULL(NEW.`api_before_delete`, '') <> IFNULL(OLD.`api_before_delete`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'api_before_delete', OLD.`api_before_delete`, NEW.`api_before_delete`);
    END IF;

    IF IFNULL(NEW.`show_title_field`, '') <> IFNULL(OLD.`show_title_field`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'show_title_field', OLD.`show_title_field`, NEW.`show_title_field`);
    END IF;

    IF IFNULL(NEW.`friendly_name`, '') <> IFNULL(OLD.`friendly_name`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'friendly_name', OLD.`friendly_name`, NEW.`friendly_name`);
    END IF;

    IF IFNULL(NEW.`default_ordering`, '') <> IFNULL(OLD.`default_ordering`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'default_ordering', OLD.`default_ordering`, NEW.`default_ordering`);
    END IF;

    IF IFNULL(NEW.`save_history`, '') <> IFNULL(OLD.`save_history`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'save_history', OLD.`save_history`, NEW.`save_history`);
    END IF;

    IF IFNULL(NEW.`enable_multiple_environments`, '') <> IFNULL(OLD.`enable_multiple_environments`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'enable_multiple_environments', OLD.`enable_multiple_environments`, NEW.`enable_multiple_environments`);
    END IF;

    IF IFNULL(NEW.`dedicated_table_prefix`, '') <> IFNULL(OLD.`dedicated_table_prefix`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'dedicated_table_prefix', OLD.`dedicated_table_prefix`, NEW.`dedicated_table_prefix`);
    END IF;

    IF IFNULL(NEW.`store_type`, '') <> IFNULL(OLD.`store_type`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ENTITY', 'wiser_entity', NEW.id, IFNULL(@_username, USER()), 'store_type', OLD.`store_type`, NEW.`store_type`);
    END IF;
END;

DROP TRIGGER IF EXISTS `EntityDelete`;
CREATE TRIGGER `EntityDelete` AFTER DELETE ON `wiser_entity` FOR EACH ROW BEGIN
    INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
    VALUES ('DELETE_ENTITY', 'wiser_entity', OLD.id, IFNULL(@_username, USER()), 'name', OLD.name, '');
END;

-- ----------------------------
-- Triggers structure for table wiser_field_templates
-- ----------------------------
DROP TRIGGER IF EXISTS `FieldTemplateInsert`;
CREATE TRIGGER `FieldTemplateInsert` AFTER INSERT ON `wiser_field_templates` FOR EACH ROW BEGIN
    INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
    VALUES ('INSERT_FIELD_TEMPLATE', 'wiser_field_templates', NEW.id, IFNULL(@_username, USER()), 'id', NULL, NEW.id);

    IF IFNULL(NEW.`field_type`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_FIELD_TEMPLATE', 'wiser_field_templates', NEW.id, IFNULL(@_username, USER()), 'field_type', NULL, NEW.`field_type`);
    END IF;

    IF IFNULL(NEW.`html_template`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_FIELD_TEMPLATE', 'wiser_field_templates', NEW.id, IFNULL(@_username, USER()), 'html_template', NULL, NEW.`html_template`);
    END IF;

    IF IFNULL(NEW.`script_template`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_FIELD_TEMPLATE', 'wiser_field_templates', NEW.id, IFNULL(@_username, USER()), 'script_template', NULL, NEW.`script_template`);
    END IF;
END;

DROP TRIGGER IF EXISTS `FieldTemplateUpdate`;
CREATE TRIGGER `FieldTemplateUpdate` AFTER UPDATE ON `wiser_field_templates` FOR EACH ROW BEGIN
    IF IFNULL(NEW.`field_type`, '') <> IFNULL(OLD.`field_type`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_FIELD_TEMPLATE', 'wiser_field_templates', NEW.id, IFNULL(@_username, USER()), 'field_type', OLD.`field_type`, NEW.`field_type`);
    END IF;

    IF IFNULL(NEW.`field_type`, '') <> IFNULL(OLD.`field_type`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_FIELD_TEMPLATE', 'wiser_field_templates', NEW.id, IFNULL(@_username, USER()), 'field_type', OLD.`field_type`, NEW.`field_type`);
    END IF;

    IF IFNULL(NEW.`script_template`, '') <> IFNULL(OLD.`script_template`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_FIELD_TEMPLATE', 'wiser_field_templates', NEW.id, IFNULL(@_username, USER()), 'script_template', OLD.`script_template`, NEW.`script_template`);
    END IF;
END;

DROP TRIGGER IF EXISTS `FieldTemplateDelete`;
CREATE TRIGGER `FieldTemplateDelete` AFTER DELETE ON `wiser_field_templates` FOR EACH ROW BEGIN
    INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
    VALUES ('DELETE_FIELD_TEMPLATE', 'wiser_field_templates', OLD.id, IFNULL(@_username, USER()), 'field_type', OLD.field_type, '');
END;

-- ----------------------------
-- Triggers structure for table wiser_link
-- ----------------------------
DROP TRIGGER IF EXISTS `LinkSettingInsert`;
CREATE TRIGGER `LinkSettingInsert` AFTER INSERT ON `wiser_link` FOR EACH ROW BEGIN
    INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
    VALUES ('INSERT_LINK_SETTING', 'wiser_link', NEW.id, IFNULL(@_username, USER()), 'id', NULL, NEW.id);

    IF IFNULL(NEW.`type`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_LINK_SETTING', 'wiser_link', NEW.id, IFNULL(@_username, USER()), 'type', NULL, NEW.`type`);
    END IF;

    IF IFNULL(NEW.`destination_entity_type`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_LINK_SETTING', 'wiser_link', NEW.id, IFNULL(@_username, USER()), 'destination_entity_type', NULL, NEW.`destination_entity_type`);
    END IF;

    IF IFNULL(NEW.`connected_entity_type`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_LINK_SETTING', 'wiser_link', NEW.id, IFNULL(@_username, USER()), 'connected_entity_type', NULL, NEW.`connected_entity_type`);
    END IF;

    IF IFNULL(NEW.`name`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('INSERT_LINK_SETTING', 'wiser_link', NEW.id, IFNULL(@_username, USER()), 'name', NULL, NEW.`name`);
    END IF;

    IF IFNULL(NEW.`show_in_tree_view`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_LINK_SETTING', 'wiser_link', NEW.id, IFNULL(@_username, USER()), 'show_in_tree_view', NULL, NEW.`show_in_tree_view`);
    END IF;

    IF IFNULL(NEW.`show_in_data_selector`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_LINK_SETTING', 'wiser_link', NEW.id, IFNULL(@_username, USER()), 'show_in_data_selector', NULL, NEW.`show_in_data_selector`);
    END IF;

    IF IFNULL(NEW.`relationship`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_LINK_SETTING', 'wiser_link', NEW.id, IFNULL(@_username, USER()), 'relationship', NULL, NEW.`relationship`);
    END IF;

    IF IFNULL(NEW.`duplication`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_LINK_SETTING', 'wiser_link', NEW.id, IFNULL(@_username, USER()), 'duplication', NULL, NEW.`duplication`);
    END IF;

    IF IFNULL(NEW.`show_in_tree_view`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_LINK_SETTING', 'wiser_link', NEW.id, IFNULL(@_username, USER()), 'show_in_tree_view', NULL, NEW.`show_in_tree_view`);
    END IF;

    IF IFNULL(NEW.`use_item_parent_id`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_LINK_SETTING', 'wiser_link', NEW.id, IFNULL(@_username, USER()), 'use_item_parent_id', NULL, NEW.`use_item_parent_id`);
    END IF;

    IF IFNULL(NEW.`use_dedicated_table`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_LINK_SETTING', 'wiser_link', NEW.id, IFNULL(@_username, USER()), 'use_dedicated_table', NULL, NEW.`use_dedicated_table`);
    END IF;
END;

DROP TRIGGER IF EXISTS `LinkSettingUpdate`;
CREATE TRIGGER `LinkSettingUpdate` AFTER UPDATE ON `wiser_link` FOR EACH ROW BEGIN
    IF IFNULL(NEW.`type`, '') <> IFNULL(OLD.`type`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_LINK_SETTING', 'wiser_link', NEW.id, IFNULL(@_username, USER()), 'type', OLD.`type`, NEW.`type`);
    END IF;

    IF IFNULL(NEW.`destination_entity_type`, '') <> IFNULL(OLD.`destination_entity_type`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_LINK_SETTING', 'wiser_link', NEW.id, IFNULL(@_username, USER()), 'destination_entity_type', OLD.`destination_entity_type`, NEW.`destination_entity_type`);
    END IF;

    IF IFNULL(NEW.`connected_entity_type`, '') <> IFNULL(OLD.`connected_entity_type`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_LINK_SETTING', 'wiser_link', NEW.id, IFNULL(@_username, USER()), 'connected_entity_type', OLD.`connected_entity_type`, NEW.`connected_entity_type`);
    END IF;

    IF IFNULL(NEW.`name`, '') <> IFNULL(OLD.`name`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_LINK_SETTING', 'wiser_link', NEW.id, IFNULL(@_username, USER()), 'name', OLD.`name`, NEW.`name`);
    END IF;

    IF IFNULL(NEW.`show_in_tree_view`, '') <> IFNULL(OLD.`show_in_tree_view`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_LINK_SETTING', 'wiser_link', NEW.id, IFNULL(@_username, USER()), 'show_in_tree_view', OLD.`show_in_tree_view`, NEW.`show_in_tree_view`);
    END IF;

    IF IFNULL(NEW.`show_in_data_selector`, '') <> IFNULL(OLD.`show_in_data_selector`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_LINK_SETTING', 'wiser_link', NEW.id, IFNULL(@_username, USER()), 'show_in_data_selector', OLD.`show_in_data_selector`, NEW.`show_in_data_selector`);
    END IF;

    IF IFNULL(NEW.`relationship`, '') <> IFNULL(OLD.`relationship`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_LINK_SETTING', 'wiser_link', NEW.id, IFNULL(@_username, USER()), 'relationship', OLD.`relationship`, NEW.`relationship`);
    END IF;

    IF IFNULL(NEW.`duplication`, '') <> IFNULL(OLD.`duplication`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_LINK_SETTING', 'wiser_link', NEW.id, IFNULL(@_username, USER()), 'duplication', OLD.`duplication`, NEW.`duplication`);
    END IF;

    IF IFNULL(NEW.`show_in_tree_view`, '') <> IFNULL(OLD.`show_in_tree_view`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_LINK_SETTING', 'wiser_link', NEW.id, IFNULL(@_username, USER()), 'show_in_tree_view', OLD.`show_in_tree_view`, NEW.`show_in_tree_view`);
    END IF;

    IF IFNULL(NEW.`use_item_parent_id`, '') <> IFNULL(OLD.`use_item_parent_id`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_LINK_SETTING', 'wiser_link', NEW.id, IFNULL(@_username, USER()), 'use_item_parent_id', OLD.`use_item_parent_id`, NEW.`use_item_parent_id`);
    END IF;

    IF IFNULL(NEW.`use_dedicated_table`, '') <> IFNULL(OLD.`use_dedicated_table`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_LINK_SETTING', 'wiser_link', NEW.id, IFNULL(@_username, USER()), 'use_dedicated_table', OLD.`use_dedicated_table`, NEW.`use_dedicated_table`);
    END IF;
END;

DROP TRIGGER IF EXISTS `LinkSettingDelete`;
CREATE TRIGGER `LinkSettingDelete` AFTER DELETE ON `wiser_link` FOR EACH ROW BEGIN
    INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
    VALUES ('DELETE_LINK_SETTING', 'wiser_link', OLD.id, IFNULL(@_username, USER()), 'type/destination_entity_type/connected_entity_type', CONCAT_WS('/', OLD.type, OLD.destination_entity_type, OLD.connected_entity_type), '');
END;

-- ----------------------------
-- Triggers structure for table wiser_permission
-- ----------------------------
DROP TRIGGER IF EXISTS `PermissionInsert`;
CREATE TRIGGER `PermissionInsert` AFTER INSERT ON `wiser_permission` FOR EACH ROW BEGIN
    INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
    VALUES ('INSERT_PERMISSION', 'wiser_permission', NEW.id, IFNULL(@_username, USER()), 'id', NULL, NEW.id);

    IF IFNULL(NEW.`role_id`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_PERMISSION', 'wiser_permission', NEW.id, IFNULL(@_username, USER()), 'role_id', NULL, NEW.`role_id`);
    END IF;

    IF IFNULL(NEW.`entity_name`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_PERMISSION', 'wiser_permission', NEW.id, IFNULL(@_username, USER()), 'entity_name', NULL, NEW.`entity_name`);
    END IF;

    IF IFNULL(NEW.`item_id`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_PERMISSION', 'wiser_permission', NEW.id, IFNULL(@_username, USER()), 'item_id', NULL, NEW.`item_id`);
    END IF;

    IF IFNULL(NEW.`entity_property_id`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_PERMISSION', 'wiser_permission', NEW.id, IFNULL(@_username, USER()), 'entity_property_id', NULL, NEW.`entity_property_id`);
    END IF;

    IF IFNULL(NEW.`permissions`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_PERMISSION', 'wiser_permission', NEW.id, IFNULL(@_username, USER()), 'permissions', NULL, NEW.`permissions`);
    END IF;

    IF IFNULL(NEW.`module_id`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_PERMISSION', 'wiser_permission', NEW.id, IFNULL(@_username, USER()), 'module_id', NULL, NEW.`module_id`);
    END IF;
END;

DROP TRIGGER IF EXISTS `PermissionUpdate`;
CREATE TRIGGER `PermissionUpdate` AFTER UPDATE ON `wiser_permission` FOR EACH ROW BEGIN
    IF IFNULL(NEW.`role_id`, '') <> IFNULL(OLD.`role_id`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_PERMISSION', 'wiser_permission', NEW.id, IFNULL(@_username, USER()), 'role_id', OLD.`role_id`, NEW.`role_id`);
    END IF;

    IF IFNULL(NEW.`entity_name`, '') <> IFNULL(OLD.`entity_name`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_PERMISSION', 'wiser_permission', NEW.id, IFNULL(@_username, USER()), 'entity_name', OLD.`entity_name`, NEW.`entity_name`);
    END IF;

    IF IFNULL(NEW.`item_id`, '') <> IFNULL(OLD.`item_id`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_PERMISSION', 'wiser_permission', NEW.id, IFNULL(@_username, USER()), 'item_id', OLD.`item_id`, NEW.`item_id`);
    END IF;

    IF IFNULL(NEW.`entity_property_id`, '') <> IFNULL(OLD.`entity_property_id`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_PERMISSION', 'wiser_permission', NEW.id, IFNULL(@_username, USER()), 'entity_property_id', OLD.`entity_property_id`, NEW.`entity_property_id`);
    END IF;

    IF IFNULL(NEW.`permissions`, '') <> IFNULL(OLD.`permissions`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_PERMISSION', 'wiser_permission', NEW.id, IFNULL(@_username, USER()), 'permissions', OLD.`permissions`, NEW.`permissions`);
    END IF;

    IF IFNULL(NEW.`module_id`, '') <> IFNULL(OLD.`module_id`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_PERMISSION', 'wiser_permission', NEW.id, IFNULL(@_username, USER()), 'module_id', OLD.`module_id`, NEW.`module_id`);
    END IF;
END;

DROP TRIGGER IF EXISTS `PermissionDelete`;
CREATE TRIGGER `PermissionDelete` AFTER DELETE ON `wiser_permission` FOR EACH ROW BEGIN
    INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
    VALUES ('DELETE_PERMISSION', 'wiser_permission', OLD.id, IFNULL(@_username, USER()), 'old_data', JSON_OBJECT('role_id', OLD.role_id, 'entity_name', OLD.entity_name, 'item_id', OLD.item_id, 'entity_property_id', OLD.entity_property_id, 'permissions', OLD.permissions, 'module_id', OLD.module_id), '');
END;

-- ----------------------------
-- Triggers structure for table wiser_roles
-- ----------------------------
DROP TRIGGER IF EXISTS `RoleInsert`;
CREATE TRIGGER `RoleInsert` AFTER INSERT ON `wiser_roles` FOR EACH ROW BEGIN
    INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
    VALUES ('INSERT_ROLE', 'wiser_roles', NEW.id, IFNULL(@_username, USER()), 'id', NULL, NEW.id);

    IF IFNULL(NEW.`role_name`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ROLE', 'wiser_roles', NEW.id, IFNULL(@_username, USER()), 'role_name', NULL, NEW.`role_name`);
    END IF;
END;

DROP TRIGGER IF EXISTS `RoleUpdate`;
CREATE TRIGGER `RoleUpdate` AFTER UPDATE ON `wiser_roles` FOR EACH ROW BEGIN
    IF IFNULL(NEW.`role_name`, '') <> IFNULL(OLD.`role_name`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_ROLE', 'wiser_roles', NEW.id, IFNULL(@_username, USER()), 'role_name', OLD.`role_name`, NEW.`role_name`);
    END IF;
END;

DROP TRIGGER IF EXISTS `RoleDelete`;
CREATE TRIGGER `RoleDelete` AFTER DELETE ON `wiser_roles` FOR EACH ROW BEGIN
    INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
    VALUES ('DELETE_ROLE', 'wiser_roles', OLD.id, IFNULL(@_username, USER()), 'role_name', OLD.`role_name`, '');
END;

-- ----------------------------
-- Triggers structure for table wiser_user_roles
-- ----------------------------
DROP TRIGGER IF EXISTS `UserRoleInsert`;
CREATE TRIGGER `UserRoleInsert` AFTER INSERT ON `wiser_user_roles` FOR EACH ROW BEGIN
    INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
    VALUES ('INSERT_USER_ROLE', 'wiser_user_roles', NEW.id, IFNULL(@_username, USER()), 'id', NULL, NEW.id);

    IF IFNULL(NEW.`user_id`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_USER_ROLE', 'wiser_user_roles', NEW.id, IFNULL(@_username, USER()), 'user_id', NULL, NEW.`user_id`);
    END IF;

    IF IFNULL(NEW.`role_id`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_USER_ROLE', 'wiser_user_roles', NEW.id, IFNULL(@_username, USER()), 'role_id', NULL, NEW.`role_id`);
    END IF;
END;

DROP TRIGGER IF EXISTS `UserRoleUpdate`;
CREATE TRIGGER `UserRoleUpdate` AFTER UPDATE ON `wiser_user_roles` FOR EACH ROW BEGIN
    IF IFNULL(NEW.`user_id`, '') <> IFNULL(OLD.`user_id`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_USER_ROLE', 'wiser_user_roles', NEW.id, IFNULL(@_username, USER()), 'user_id', OLD.`user_id`, NEW.`user_id`);
    END IF;

    IF IFNULL(NEW.`role_id`, '') <> IFNULL(OLD.`role_id`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_USER_ROLE', 'wiser_user_roles', NEW.id, IFNULL(@_username, USER()), 'role_id', OLD.`role_id`, NEW.`role_id`);
    END IF;
END;

DROP TRIGGER IF EXISTS `UserRoleDelete`;
CREATE TRIGGER `UserRoleDelete` AFTER DELETE ON `wiser_user_roles` FOR EACH ROW BEGIN
    INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
    VALUES ('DELETE_USER_ROLE', 'wiser_user_roles', OLD.id, IFNULL(@_username, USER()), 'old_data', JSON_OBJECT('user_id', OLD.user_id, 'role_id', OLD.role_id), '');
END;

-- ----------------------------
-- Triggers structure for table wiser_api_connection
-- ----------------------------
DROP TRIGGER IF EXISTS `ApiConnectionInsert`;
CREATE TRIGGER `ApiConnectionInsert` AFTER INSERT ON `wiser_api_connection` FOR EACH ROW BEGIN
    INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
    VALUES ('INSERT_API_CONNECTION', 'wiser_api_connection', NEW.id, IFNULL(@_username, USER()), 'id', NULL, NEW.id);

    IF IFNULL(NEW.`name`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_API_CONNECTION', 'wiser_api_connection', NEW.id, IFNULL(@_username, USER()), 'name', NULL, NEW.`name`);
    END IF;

    IF IFNULL(NEW.`options`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_API_CONNECTION', 'wiser_api_connection', NEW.id, IFNULL(@_username, USER()), 'options', NULL, NEW.`options`);
    END IF;

    IF IFNULL(NEW.`authentication_data`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_API_CONNECTION', 'wiser_api_connection', NEW.id, IFNULL(@_username, USER()), 'authentication_data', NULL, NEW.`authentication_data`);
    END IF;
END;

DROP TRIGGER IF EXISTS `ApiConnectionUpdate`;
CREATE TRIGGER `ApiConnectionUpdate` AFTER UPDATE ON `wiser_api_connection` FOR EACH ROW BEGIN
    IF IFNULL(NEW.`name`, '') <> IFNULL(OLD.`name`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_API_CONNECTION', 'wiser_api_connection', NEW.id, IFNULL(@_username, USER()), 'name', OLD.`name`, NEW.`name`);
    END IF;

    IF IFNULL(NEW.`options`, '') <> IFNULL(OLD.`options`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_API_CONNECTION', 'wiser_api_connection', NEW.id, IFNULL(@_username, USER()), 'options', OLD.`options`, NEW.`options`);
    END IF;

    IF IFNULL(NEW.`authentication_data`, '') <> IFNULL(OLD.`authentication_data`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_API_CONNECTION', 'wiser_api_connection', NEW.id, IFNULL(@_username, USER()), 'authentication_data', OLD.`authentication_data`, NEW.`authentication_data`);
    END IF;
END;

DROP TRIGGER IF EXISTS `ApiConnectionDelete`;
CREATE TRIGGER `ApiConnectionDelete` AFTER DELETE ON `wiser_api_connection` FOR EACH ROW BEGIN
    INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
    VALUES ('DELETE_API_CONNECTION', 'wiser_api_connection', OLD.id, IFNULL(@_username, USER()), 'name', OLD.name, '');
END;

-- ----------------------------
-- Triggers structure for table wiser_data_selector
-- ----------------------------
DROP TRIGGER IF EXISTS `DataSelectorInsert`;
CREATE TRIGGER `DataSelectorInsert` AFTER INSERT ON `wiser_data_selector` FOR EACH ROW BEGIN
    INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
    VALUES ('INSERT_DATA_SELECTOR', 'wiser_data_selector', NEW.id, IFNULL(@_username, USER()), 'id', NULL, NEW.id);

    IF IFNULL(NEW.`name`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_DATA_SELECTOR', 'wiser_data_selector', NEW.id, IFNULL(@_username, USER()), 'name', NULL, NEW.`name`);
    END IF;

    IF IFNULL(NEW.`removed`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_DATA_SELECTOR', 'wiser_data_selector', NEW.id, IFNULL(@_username, USER()), 'removed', NULL, NEW.`removed`);
    END IF;

    IF IFNULL(NEW.`module_selection`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_DATA_SELECTOR', 'wiser_data_selector', NEW.id, IFNULL(@_username, USER()), 'module_selection', NULL, NEW.`module_selection`);
    END IF;

    IF IFNULL(NEW.`request_json`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_DATA_SELECTOR', 'wiser_data_selector', NEW.id, IFNULL(@_username, USER()), 'request_json', NULL, NEW.`request_json`);
    END IF;

    IF IFNULL(NEW.`saved_json`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_DATA_SELECTOR', 'wiser_data_selector', NEW.id, IFNULL(@_username, USER()), 'saved_json', NULL, NEW.`saved_json`);
    END IF;

    IF IFNULL(NEW.`show_in_export_module`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_DATA_SELECTOR', 'wiser_data_selector', NEW.id, IFNULL(@_username, USER()), 'show_in_export_module', NULL, NEW.`show_in_export_module`);
    END IF;

    IF IFNULL(NEW.`show_in_dashboard`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_DATA_SELECTOR', 'wiser_data_selector', NEW.id, IFNULL(@_username, USER()), 'show_in_dashboard', NULL, NEW.`show_in_dashboard`);
    END IF;

    IF IFNULL(NEW.`available_for_branches`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_DATA_SELECTOR', 'wiser_data_selector', NEW.id, IFNULL(@_username, USER()), 'available_for_branches', NULL, NEW.`available_for_branches`);
    END IF;
END;

DROP TRIGGER IF EXISTS `DataSelectorUpdate`;
CREATE TRIGGER `DataSelectorUpdate` AFTER UPDATE ON `wiser_data_selector` FOR EACH ROW BEGIN
    IF IFNULL(NEW.`name`, '') <> IFNULL(OLD.`name`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_DATA_SELECTOR', 'wiser_data_selector', NEW.id, IFNULL(@_username, USER()), 'name', OLD.`name`, NEW.`name`);
    END IF;

    IF IFNULL(NEW.`removed`, '') <> IFNULL(OLD.`removed`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_DATA_SELECTOR', 'wiser_data_selector', NEW.id, IFNULL(@_username, USER()), 'removed', OLD.`removed`, NEW.`removed`);
    END IF;

    IF IFNULL(NEW.`module_selection`, '') <> IFNULL(OLD.`module_selection`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_DATA_SELECTOR', 'wiser_data_selector', NEW.id, IFNULL(@_username, USER()), 'module_selection', OLD.`module_selection`, NEW.`module_selection`);
    END IF;

    IF IFNULL(NEW.`request_json`, '') <> IFNULL(OLD.`request_json`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_DATA_SELECTOR', 'wiser_data_selector', NEW.id, IFNULL(@_username, USER()), 'request_json', OLD.`request_json`, NEW.`request_json`);
    END IF;

    IF IFNULL(NEW.`saved_json`, '') <> IFNULL(OLD.`saved_json`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_DATA_SELECTOR', 'wiser_data_selector', NEW.id, IFNULL(@_username, USER()), 'saved_json', OLD.`saved_json`, NEW.`saved_json`);
    END IF;

    IF IFNULL(NEW.`show_in_export_module`, '') <> IFNULL(OLD.`show_in_export_module`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_DATA_SELECTOR', 'wiser_data_selector', NEW.id, IFNULL(@_username, USER()), 'show_in_export_module', OLD.`show_in_export_module`, NEW.`show_in_export_module`);
    END IF;

    IF IFNULL(NEW.`show_in_dashboard`, '') <> IFNULL(OLD.`show_in_dashboard`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_DATA_SELECTOR', 'wiser_data_selector', NEW.id, IFNULL(@_username, USER()), 'show_in_dashboard', OLD.`show_in_dashboard`, NEW.`show_in_dashboard`);
    END IF;

    IF IFNULL(NEW.`available_for_branches`, '') <> IFNULL(OLD.`available_for_branches`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_DATA_SELECTOR', 'wiser_data_selector', NEW.id, IFNULL(@_username, USER()), 'available_for_branches', OLD.`available_for_branches`, NEW.`available_for_branches`);
    END IF;
END;

DROP TRIGGER IF EXISTS `DataSelectorDelete`;
CREATE TRIGGER `DataSelectorDelete` AFTER DELETE ON `wiser_data_selector` FOR EACH ROW BEGIN
    INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
    VALUES ('DELETE_DATA_SELECTOR', 'wiser_data_selector', OLD.id, IFNULL(@_username, USER()), 'name', OLD.name, '');
END;

-- ----------------------------
-- Triggers structure for table wiser_communication
-- ----------------------------
DROP TRIGGER IF EXISTS `CommunicationInsert`;
CREATE TRIGGER `CommunicationInsert` AFTER INSERT ON `wiser_communication` FOR EACH ROW BEGIN
    INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
    VALUES ('INSERT_COMMUNICATION', 'wiser_communication', NEW.id, IFNULL(@_username, USER()), 'id', NULL, NEW.id);

    IF IFNULL(NEW.`name`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_COMMUNICATION', 'wiser_communication', NEW.id, IFNULL(@_username, USER()), 'name', NULL, NEW.`name`);
    END IF;

    IF IFNULL(NEW.`receiver_list`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_COMMUNICATION', 'wiser_communication', NEW.id, IFNULL(@_username, USER()), 'receiver_list', NULL, NEW.`receiver_list`);
    END IF;

    IF IFNULL(NEW.`receivers_data_selector_id`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_COMMUNICATION', 'wiser_communication', NEW.id, IFNULL(@_username, USER()), 'receivers_data_selector_id', NULL, NEW.`receivers_data_selector_id`);
    END IF;

    IF IFNULL(NEW.`receivers_query_id`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_COMMUNICATION', 'wiser_communication', NEW.id, IFNULL(@_username, USER()), 'receivers_query_id', NULL, NEW.`receivers_query_id`);
    END IF;

    IF IFNULL(NEW.`content_data_selector_id`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_COMMUNICATION', 'wiser_communication', NEW.id, IFNULL(@_username, USER()), 'content_data_selector_id', NULL, NEW.`content_data_selector_id`);
    END IF;

    IF IFNULL(NEW.`content_query_id`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_COMMUNICATION', 'wiser_communication', NEW.id, IFNULL(@_username, USER()), 'content_query_id', NULL, NEW.`content_query_id`);
    END IF;

    IF IFNULL(NEW.`settings`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_COMMUNICATION', 'wiser_communication', NEW.id, IFNULL(@_username, USER()), 'settings', NULL, NEW.`settings`);
    END IF;

    IF IFNULL(NEW.`send_trigger_type`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_COMMUNICATION', 'wiser_communication', NEW.id, IFNULL(@_username, USER()), 'send_trigger_type', NULL, NEW.`send_trigger_type`);
    END IF;

    IF IFNULL(NEW.`trigger_start`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_COMMUNICATION', 'wiser_communication', NEW.id, IFNULL(@_username, USER()), 'trigger_start', NULL, NEW.`trigger_start`);
    END IF;

    IF IFNULL(NEW.`trigger_end`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_COMMUNICATION', 'wiser_communication', NEW.id, IFNULL(@_username, USER()), 'trigger_end', NULL, NEW.`trigger_end`);
    END IF;

    IF IFNULL(NEW.`trigger_time`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_COMMUNICATION', 'wiser_communication', NEW.id, IFNULL(@_username, USER()), 'trigger_time', NULL, NEW.`trigger_time`);
    END IF;

    IF IFNULL(NEW.`trigger_period_value`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_COMMUNICATION', 'wiser_communication', NEW.id, IFNULL(@_username, USER()), 'trigger_period_value', NULL, NEW.`trigger_period_value`);
    END IF;

    IF IFNULL(NEW.`trigger_period_type`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_COMMUNICATION', 'wiser_communication', NEW.id, IFNULL(@_username, USER()), 'trigger_period_type', NULL, NEW.`trigger_period_type`);
    END IF;

    IF IFNULL(NEW.`trigger_week_days`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_COMMUNICATION', 'wiser_communication', NEW.id, IFNULL(@_username, USER()), 'trigger_week_days', NULL, NEW.`trigger_week_days`);
    END IF;

    IF IFNULL(NEW.`trigger_day_of_month`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_COMMUNICATION', 'wiser_communication', NEW.id, IFNULL(@_username, USER()), 'trigger_day_of_month', NULL, NEW.`trigger_day_of_month`);
    END IF;

    IF IFNULL(NEW.`last_processed`, '') <> '' THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_COMMUNICATION', 'wiser_communication', NEW.id, IFNULL(@_username, USER()), 'last_processed', NULL, NEW.`last_processed`);
    END IF;
END;

DROP TRIGGER IF EXISTS `CommunicationUpdate`;
CREATE TRIGGER `CommunicationUpdate` AFTER UPDATE ON `wiser_communication` FOR EACH ROW BEGIN
    IF IFNULL(NEW.`name`, '') <> IFNULL(OLD.`name`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_COMMUNICATION', 'wiser_communication', NEW.id, IFNULL(@_username, USER()), 'name', OLD.`name`, NEW.`name`);
    END IF;
    IF IFNULL(NEW.`receiver_list`, '') <> IFNULL(OLD.`receiver_list`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_COMMUNICATION', 'wiser_communication', NEW.id, IFNULL(@_username, USER()), 'receiver_list', OLD.`receiver_list`, NEW.`receiver_list`);
    END IF;
    IF IFNULL(NEW.`receivers_data_selector_id`, '') <> IFNULL(OLD.`receivers_data_selector_id`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_COMMUNICATION', 'wiser_communication', NEW.id, IFNULL(@_username, USER()), 'receivers_data_selector_id', OLD.`receivers_data_selector_id`, NEW.`receivers_data_selector_id`);
    END IF;
    IF IFNULL(NEW.`receivers_query_id`, '') <> IFNULL(OLD.`receivers_query_id`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_COMMUNICATION', 'wiser_communication', NEW.id, IFNULL(@_username, USER()), 'receivers_query_id', OLD.`receivers_query_id`, NEW.`receivers_query_id`);
    END IF;
    IF IFNULL(NEW.`content_data_selector_id`, '') <> IFNULL(OLD.`content_data_selector_id`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_COMMUNICATION', 'wiser_communication', NEW.id, IFNULL(@_username, USER()), 'content_data_selector_id', OLD.`content_data_selector_id`, NEW.`content_data_selector_id`);
    END IF;
    IF IFNULL(NEW.`content_query_id`, '') <> IFNULL(OLD.`content_query_id`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_COMMUNICATION', 'wiser_communication', NEW.id, IFNULL(@_username, USER()), 'content_query_id', OLD.`content_query_id`, NEW.`content_query_id`);
    END IF;
    IF IFNULL(NEW.`settings`, '') <> IFNULL(OLD.`settings`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_COMMUNICATION', 'wiser_communication', NEW.id, IFNULL(@_username, USER()), 'settings', OLD.`settings`, NEW.`settings`);
    END IF;
    IF IFNULL(NEW.`send_trigger_type`, '') <> IFNULL(OLD.`send_trigger_type`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_COMMUNICATION', 'wiser_communication', NEW.id, IFNULL(@_username, USER()), 'send_trigger_type', OLD.`send_trigger_type`, NEW.`send_trigger_type`);
    END IF;
    IF IFNULL(NEW.`trigger_start`, '') <> IFNULL(OLD.`trigger_start`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_COMMUNICATION', 'wiser_communication', NEW.id, IFNULL(@_username, USER()), 'trigger_start', OLD.`trigger_start`, NEW.`trigger_start`);
    END IF;
    IF IFNULL(NEW.`trigger_end`, '') <> IFNULL(OLD.`trigger_end`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_COMMUNICATION', 'wiser_communication', NEW.id, IFNULL(@_username, USER()), 'trigger_end', OLD.`trigger_end`, NEW.`trigger_end`);
    END IF;
    IF IFNULL(NEW.`trigger_time`, '') <> IFNULL(OLD.`trigger_time`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_COMMUNICATION', 'wiser_communication', NEW.id, IFNULL(@_username, USER()), 'trigger_time', OLD.`trigger_time`, NEW.`trigger_time`);
    END IF;
    IF IFNULL(NEW.`trigger_period_value`, '') <> IFNULL(OLD.`trigger_period_value`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_COMMUNICATION', 'wiser_communication', NEW.id, IFNULL(@_username, USER()), 'trigger_period_value', OLD.`trigger_period_value`, NEW.`trigger_period_value`);
    END IF;
    IF IFNULL(NEW.`trigger_period_type`, '') <> IFNULL(OLD.`trigger_period_type`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_COMMUNICATION', 'wiser_communication', NEW.id, IFNULL(@_username, USER()), 'trigger_period_type', OLD.`trigger_period_type`, NEW.`trigger_period_type`);
    END IF;
    IF IFNULL(NEW.`trigger_week_days`, '') <> IFNULL(OLD.`trigger_week_days`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_COMMUNICATION', 'wiser_communication', NEW.id, IFNULL(@_username, USER()), 'trigger_week_days', OLD.`trigger_week_days`, NEW.`trigger_week_days`);
    END IF;
    IF IFNULL(NEW.`trigger_day_of_month`, '') <> IFNULL(OLD.`trigger_day_of_month`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_COMMUNICATION', 'wiser_communication', NEW.id, IFNULL(@_username, USER()), 'trigger_day_of_month', OLD.`trigger_day_of_month`, NEW.`trigger_day_of_month`);
    END IF;
    IF IFNULL(NEW.`last_processed`, '') <> IFNULL(OLD.`last_processed`, '') THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('UPDATE_COMMUNICATION', 'wiser_communication', NEW.id, IFNULL(@_username, USER()), 'name', OLD.`last_processed`, NEW.`last_processed`);
    END IF;
END;

DROP TRIGGER IF EXISTS `CommunicationDelete`;
CREATE TRIGGER `CommunicationDelete` AFTER DELETE ON `wiser_communication` FOR EACH ROW BEGIN
    INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
    VALUES ('DELETE_COMMUNICATION', 'wiser_communication', OLD.id, IFNULL(@_username, USER()), 'name', OLD.name, '');
END;

-- ----------------------------
-- Triggers structure for table wiser_styled_output
-- ----------------------------
DROP TRIGGER IF EXISTS `StyledOutputInsert`;
CREATE TRIGGER `StyledOutputInsert` AFTER INSERT ON `wiser_styled_output` FOR EACH ROW BEGIN
    IF IFNULL(@saveHistory, TRUE) = TRUE THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('CREATE_STYLED_OUTPUT','wiser_styled_output', NEW.id, IFNULL(@_username, USER()), '', '', '');

        IF IFNULL(NEW.`name`, '') <> '' THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_STYLED_OUTPUT','wiser_styled_output',NEW.`id`,IFNULL(@_username, USER()),'name',NULL,NEW.`name`);
        END IF;

        IF IFNULL(NEW.`format_begin`, '') <> '' THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_STYLED_OUTPUT','wiser_styled_output',NEW.`id`,IFNULL(@_username, USER()),'format_begin',NULL,NEW.`format_begin`);
        END IF;

        IF IFNULL(NEW.`format_item`, '') <> '' THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_STYLED_OUTPUT','wiser_styled_output',NEW.`id`,IFNULL(@_username, USER()),'format_item',NULL,NEW.`format_item`);
        END IF;

        IF IFNULL(NEW.`format_end`, '') <> '' THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_STYLED_OUTPUT','wiser_styled_output',NEW.`id`,IFNULL(@_username, USER()),'format_end',NULL,NEW.`format_end`);
        END IF;

        IF IFNULL(NEW.`format_empty`, '') <> '' THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_STYLED_OUTPUT','wiser_styled_output',NEW.`id`,IFNULL(@_username, USER()),'format_empty',NULL,NEW.`format_empty`);
        END IF;

        IF IFNULL(NEW.`query_id`, '') <> '' THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_STYLED_OUTPUT','wiser_styled_output',NEW.`id`,IFNULL(@_username, USER()),'query_id',NULL,NEW.`query_id`);
        END IF;

        IF IFNULL(NEW.`return_type`, '') <> '' THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_STYLED_OUTPUT','wiser_styled_output',NEW.`id`,IFNULL(@_username, USER()),'return_type',NULL,NEW.`return_type`);
        END IF;
    END IF;
END;

DROP TRIGGER IF EXISTS `StyledOutputUpdate`;
CREATE TRIGGER `StyledOutputUpdate` AFTER UPDATE ON `wiser_styled_output` FOR EACH ROW BEGIN
    IF IFNULL(@saveHistory, TRUE) = TRUE THEN
        IF IFNULL(NEW.`name`, '') <> IFNULL(OLD.`name`, '') THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_STYLED_OUTPUT','wiser_styled_output',NEW.`id`,IFNULL(@_username, USER()),'name',OLD.`name`,NEW.`name`);
        END IF;

        IF IFNULL(NEW.`format_begin`, '') <> IFNULL(OLD.`format_begin`, '') THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_STYLED_OUTPUT','wiser_styled_output',NEW.`id`,IFNULL(@_username, USER()),'format_begin',OLD.`format_begin`,NEW.`format_begin`);
        END IF;

        IF IFNULL(NEW.`format_item`, '') <> IFNULL(OLD.`format_item`, '') THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_STYLED_OUTPUT','wiser_styled_output',NEW.`id`,IFNULL(@_username, USER()),'format_item',OLD.`format_item`,NEW.`format_item`);
        END IF;

        IF IFNULL(NEW.`format_end`, '') <> IFNULL(OLD.`format_end`, '') THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_STYLED_OUTPUT','wiser_styled_output',NEW.`id`,IFNULL(@_username, USER()),'format_end',OLD.`format_end`,NEW.`format_end`);
        END IF;

        IF IFNULL(NEW.`format_empty`, '') <> IFNULL(OLD.`format_empty`, '') THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_STYLED_OUTPUT','wiser_styled_output',NEW.`id`,IFNULL(@_username, USER()),'format_empty',OLD.`format_empty`,NEW.`format_empty`);
        END IF;

        IF IFNULL(NEW.`query_id`, '') <> IFNULL(OLD.`query_id`, '') THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_STYLED_OUTPUT','wiser_styled_output',NEW.`id`,IFNULL(@_username, USER()),'query_id',OLD.`query_id`,NEW.`query_id`);
        END IF;

        IF IFNULL(NEW.`return_type`, '') <> IFNULL(OLD.`return_type`, '') THEN
            INSERT INTO wiser_history (action,tablename,item_id,changed_by,field,oldvalue,newvalue)
            VALUES ('UPDATE_STYLED_OUTPUT','wiser_styled_output',NEW.`id`,IFNULL(@_username, USER()),'return_type',OLD.`return_type`,NEW.`return_type`);
        END IF;
    END IF;
END;

DROP TRIGGER IF EXISTS `StyledOutputDelete`;
CREATE TRIGGER `StyledOutputDelete` AFTER DELETE ON `wiser_styled_output` FOR EACH ROW BEGIN
    IF IFNULL(@saveHistory, TRUE) = TRUE THEN
        INSERT INTO wiser_history (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
        VALUES ('DELETE_STYLED_OUTPUT','wiser_styled_output', OLD.id, IFNULL(@_username, USER()), OLD.`name`, '', '');
    END IF;
END;

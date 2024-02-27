-- ----------------------------
-- Record for WTS based subitem parent update
-- ----------------------------

SET @ServicesFolderTemplateId := (SELECT id FROM `wiser_template` WHERE template_name='SERVICES');
SET @subItemParentUpdateId := (SELECT MAX(`template_id`)+1 FROM wiser_template);

INSERT INTO `wiser_template` (`parent_id`, `template_name`, `template_data`, `template_data_minified`, `template_type`, `version`, `template_id`, `added_on`, `added_by`, `changed_on`, `changed_by`, `published_environment`, `cache_minutes`, `login_required`, `login_role`, `linked_templates`, `ordering`, `insert_mode`, `load_always`, `url_regex`, `external_files`, `grouping_create_object_instead_of_array`, `grouping_prefix`, `grouping_key`, `grouping_key_column_name`, `grouping_value_column_name`, `removed`, `is_scss_include_template`, `use_in_wiser_html_editors`, `pre_load_query`, `cache_location`, `return_not_found_when_pre_load_query_has_no_data`, `cache_regex`, `login_redirect_url`, `routine_type`, `routine_parameters`, `routine_return_type`, `disable_minifier`, `is_default_header`, `is_default_footer`, `default_header_footer_regex`) 
VALUES (@ServicesFolderTemplateId, 'subItem parent update', 
-- ----------------------------
'<Configuration>
    <ServiceName>[Wiser] WTS Sub item parent Update</ServiceName>
	<!-- !!! Please consider making a seperate WTS user for these kind of configs -->
    <ConnectionString>server=?setting_hostname;port=?setting_port;uid=?setting_username;pwd=?setting_password;database=?setting_database;pooling=true;Allow User Variables=True;CharSet=utf8</ConnectionString>
    <RunSchemes>
        <RunScheme>
            <Type>Continuous</Type>
            <TimeId>1</TimeId>
            <Delay>00:01:00</Delay>
        </RunScheme>
    </RunSchemes>

    <Query>
        <TimeId>1</TimeId>
        <Order>1</Order>
        <Query>
            <![CDATA[
				\'UPDATE wiser_item `item`
				INNER JOIN wiser_parent_updates `updates` ON `item`.id = `updates`.targetId AND `updates`.target_table = \'wiser_item\'
				SET `item`.changed_on = `updates`.changed_on, `item`.changed_by = `updates`.changed_by;
				TRUNCATE wiser_parent_updates;\'
            ]]>
        </Query>
    </Query>
</Configuration>'
-- ----------------------------
, NULL, 8, 1, @subItemParentUpdateId, NOW(), '', NOW(), '', 15, 0, 0, NULL, NULL, 6, 0, 0, NULL, NULL, 0, NULL, NULL, NULL, NULL, 0, 0, 0, NULL, 0, 0, NULL, NULL, 0, NULL, NULL, 0, 0, 0, NULL);

-- ----------------------------
-- 
-- ----------------------------


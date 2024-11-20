-- First add a module for the branch settings.
SET @newModuleId = (SELECT IFNULL(MAX(id), 0) + 1 FROM wiser_module);
INSERT INTO `wiser_module` (`id`, `custom_query`, `count_query`, `options`, `name`, `icon`, `color`, `type`, `group`) VALUES (@newModuleId, '', '', '', 'Branch beheer', 'git', NULL, 'DynamicItems', 'Systeem');

-- Add the entity for the branch settings and link it to the new module.
INSERT INTO `wiser_entity` (`name`, `module_id`, `accepted_childtypes`, `icon`, `icon_add`, `show_in_tree_view`, `show_in_search`, `show_overview_tab`, `save_title_as_seo`, `show_title_field`, `friendly_name`, `save_history`, `default_ordering`, `icon_expanded`, `dedicated_table_prefix`, `delete_action`) VALUES ('branch_settings', @newModuleId, '', 'icon-git', 'icon-add', 0, 1, 1, 0, 0, 'Branch beheer', 1, 'link_ordering', 'icon-git', '', 'archive');
INSERT INTO `wiser_entityproperty` (`module_id`, `entity_name`, `link_type`, `visible_in_overview`, `overview_fieldtype`, `overview_width`, `tab_name`, `group_name`, `inputtype`, `display_name`, `property_name`, `explanation`, `ordering`, `regex_validation`, `mandatory`, `readonly`, `default_value`, `width`, `height`, `options`, `data_query`, `action_query`, `search_query`, `search_count_query`, `grid_delete_query`, `grid_insert_query`, `grid_update_query`, `depends_on_field`, `depends_on_operator`, `depends_on_value`, `language_code`, `custom_script`, `also_save_seo_value`, `depends_on_action`, `save_on_change`, `extended_explanation`, `label_style`, `label_width`, `enable_aggregation`, `aggregate_options`, `access_key`, `visibility_path_regex`) VALUES (@newModuleId, 'branch_settings', 0, 0, '', 100, 'Automatische deploy', 'Algemeen', 'input', 'Naam branch', 'branch_name', 'Dit is de naam van de branch in Wiser,&nbsp;<strong>niet</strong> de Git branch.', 1, '', 0, 0, '', 50, 0, '', '', '', '', '', '', '', '', '', NULL, '', '', '', 0, NULL, 0, 0, 'normal', '0', 0, '', '', '');
INSERT INTO `wiser_entityproperty` (`module_id`, `entity_name`, `link_type`, `visible_in_overview`, `overview_fieldtype`, `overview_width`, `tab_name`, `group_name`, `inputtype`, `display_name`, `property_name`, `explanation`, `ordering`, `regex_validation`, `mandatory`, `readonly`, `default_value`, `width`, `height`, `options`, `data_query`, `action_query`, `search_query`, `search_count_query`, `grid_delete_query`, `grid_insert_query`, `grid_update_query`, `depends_on_field`, `depends_on_operator`, `depends_on_value`, `language_code`, `custom_script`, `also_save_seo_value`, `depends_on_action`, `save_on_change`, `extended_explanation`, `label_style`, `label_width`, `enable_aggregation`, `aggregate_options`, `access_key`, `visibility_path_regex`) VALUES (@newModuleId, 'branch_settings', 0, 0, '', 100, 'Automatische deploy', 'Algemeen', 'combobox', 'Template voor automatische merge', 'branch_merge_template', 'De template met instellingen die de WTS moet gebruiken voor het automatisch mergen van de branch naar productie. Deze templates kunnen toegevoegd worden via de branch functies van Wiser (via Wiser Configuratie).', 2, '', 0, 0, '', 50, 0, '{\"useDropDownList\":true,\"saveValueAsItemLink\":false}', 'SELECT id, name\nFROM wiser_branches_queue\nWHERE is_template = 1\nORDER BY name ASC', '', '', '', '', '', '', '', NULL, '', '', '', 0, NULL, 0, 0, 'normal', '0', 0, '', '', '');
INSERT INTO `wiser_entityproperty` (`module_id`, `entity_name`, `link_type`, `visible_in_overview`, `overview_fieldtype`, `overview_width`, `tab_name`, `group_name`, `inputtype`, `display_name`, `property_name`, `explanation`, `ordering`, `regex_validation`, `mandatory`, `readonly`, `default_value`, `width`, `height`, `options`, `data_query`, `action_query`, `search_query`, `search_count_query`, `grid_delete_query`, `grid_insert_query`, `grid_update_query`, `depends_on_field`, `depends_on_operator`, `depends_on_value`, `language_code`, `custom_script`, `also_save_seo_value`, `depends_on_action`, `save_on_change`, `extended_explanation`, `label_style`, `label_width`, `enable_aggregation`, `aggregate_options`, `access_key`, `visibility_path_regex`) VALUES (@newModuleId, 'branch_settings', 0, 0, '', 100, 'Automatische deploy', 'Algemeen', 'input', 'E-mailadressen voor statusupdates', 'email_for_status_updates', 'De e-mailadressen waar statusupdates van de automatische deploy naar gestuurd moeten worden. Je kunt meerdere e-mailadressen invullen door ze te scheiden met een puntkomma (;).', 3, '', 0, 0, '', 100, 0, '', '', '', '', '', '', '', '', '', NULL, '', '', '', 0, NULL, 0, 0, 'normal', '0', 0, '', '', '');
INSERT INTO `wiser_entityproperty` (`module_id`, `entity_name`, `link_type`, `visible_in_overview`, `overview_fieldtype`, `overview_width`, `tab_name`, `group_name`, `inputtype`, `display_name`, `property_name`, `explanation`, `ordering`, `regex_validation`, `mandatory`, `readonly`, `default_value`, `width`, `height`, `options`, `data_query`, `action_query`, `search_query`, `search_count_query`, `grid_delete_query`, `grid_insert_query`, `grid_update_query`, `depends_on_field`, `depends_on_operator`, `depends_on_value`, `language_code`, `custom_script`, `also_save_seo_value`, `depends_on_action`, `save_on_change`, `extended_explanation`, `label_style`, `label_width`, `enable_aggregation`, `aggregate_options`, `access_key`, `visibility_path_regex`) VALUES (@newModuleId, 'branch_settings', 0, 0, '', 100, 'Automatische deploy', 'GitHub', 'secure-input', 'Access token', 'github_access_token', 'De Fine-grained personal access token voor communicatie met de GitHub API. Zie <a href=\"https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/managing-your-personal-access-tokens\" target=\"_blank\">dit artikel</a> voor meer informatie. Deze token moet voldoende rechten hebben om de branch \"develop\" naar \"staging\" te mergen en om \"staging\" naar \"main\" te mergen.', 4, '', 0, 0, '', 70, 0, '{\"type\":\"password\",\"securityMethod\":\"AES\"}', '', '', '', '', '', '', '', '', NULL, '', '', '', 0, NULL, 0, 0, 'normal', '0', 0, '', '', '');
INSERT INTO `wiser_entityproperty` (`module_id`, `entity_name`, `link_type`, `visible_in_overview`, `overview_fieldtype`, `overview_width`, `tab_name`, `group_name`, `inputtype`, `display_name`, `property_name`, `explanation`, `ordering`, `regex_validation`, `mandatory`, `readonly`, `default_value`, `width`, `height`, `options`, `data_query`, `action_query`, `search_query`, `search_count_query`, `grid_delete_query`, `grid_insert_query`, `grid_update_query`, `depends_on_field`, `depends_on_operator`, `depends_on_value`, `language_code`, `custom_script`, `also_save_seo_value`, `depends_on_action`, `save_on_change`, `extended_explanation`, `label_style`, `label_width`, `enable_aggregation`, `aggregate_options`, `access_key`, `visibility_path_regex`) VALUES (@newModuleId, 'branch_settings', 0, 0, '', 100, 'Automatische deploy', 'GitHub', 'date-time picker', 'Verloopdatum access token', 'github_access_token_expires', 'De datum waarop de access token verloopt, zodat we makkelijk kunnen terugzien wanneer een nieuwe aangemaakt moet worden.', 5, '', 0, 0, '', 30, 0, '{\"type\":\"datetime\"}', '', '', '', '', '', '', '', '', NULL, '', '', '', 0, NULL, 0, 0, 'normal', '0', 0, '', '', '');
INSERT INTO `wiser_entityproperty` (`module_id`, `entity_name`, `link_type`, `visible_in_overview`, `overview_fieldtype`, `overview_width`, `tab_name`, `group_name`, `inputtype`, `display_name`, `property_name`, `explanation`, `ordering`, `regex_validation`, `mandatory`, `readonly`, `default_value`, `width`, `height`, `options`, `data_query`, `action_query`, `search_query`, `search_count_query`, `grid_delete_query`, `grid_insert_query`, `grid_update_query`, `depends_on_field`, `depends_on_operator`, `depends_on_value`, `language_code`, `custom_script`, `also_save_seo_value`, `depends_on_action`, `save_on_change`, `extended_explanation`, `label_style`, `label_width`, `enable_aggregation`, `aggregate_options`, `access_key`, `visibility_path_regex`) VALUES (@newModuleId, 'branch_settings', 0, 0, '', 100, 'Automatische deploy', 'GitHub', 'input', 'Organisatienaam', 'github_organization', 'De naam van de GitHub organisatie welke de eigenaar is van het project.', 6, '', 0, 0, '', 50, 0, '', '', '', '', '', '', '', '', '', NULL, '', '', '', 0, NULL, 0, 0, 'normal', '0', 0, '', '', '');
INSERT INTO `wiser_entityproperty` (`module_id`, `entity_name`, `link_type`, `visible_in_overview`, `overview_fieldtype`, `overview_width`, `tab_name`, `group_name`, `inputtype`, `display_name`, `property_name`, `explanation`, `ordering`, `regex_validation`, `mandatory`, `readonly`, `default_value`, `width`, `height`, `options`, `data_query`, `action_query`, `search_query`, `search_count_query`, `grid_delete_query`, `grid_insert_query`, `grid_update_query`, `depends_on_field`, `depends_on_operator`, `depends_on_value`, `language_code`, `custom_script`, `also_save_seo_value`, `depends_on_action`, `save_on_change`, `extended_explanation`, `label_style`, `label_width`, `enable_aggregation`, `aggregate_options`, `access_key`, `visibility_path_regex`) VALUES (@newModuleId, 'branch_settings', 0, 0, '', 100, 'Automatische deploy', 'GitHub', 'input', 'Projectnaam', 'github_respository', 'De naam van het GitHub project (Repository) waarop de livegang uitgevoerd moet worden.&nbsp;', 7, '', 0, 0, '', 50, 0, '', '', '', '', '', '', '', '', '', NULL, '', '', '', 0, NULL, 0, 0, 'normal', '0', 0, '', '', '');
INSERT INTO `wiser_entityproperty` (`module_id`, `entity_name`, `link_type`, `visible_in_overview`, `overview_fieldtype`, `overview_width`, `tab_name`, `group_name`, `inputtype`, `display_name`, `property_name`, `explanation`, `ordering`, `regex_validation`, `mandatory`, `readonly`, `default_value`, `width`, `height`, `options`, `data_query`, `action_query`, `search_query`, `search_count_query`, `grid_delete_query`, `grid_insert_query`, `grid_update_query`, `depends_on_field`, `depends_on_operator`, `depends_on_value`, `language_code`, `custom_script`, `also_save_seo_value`, `depends_on_action`, `save_on_change`, `extended_explanation`, `label_style`, `label_width`, `enable_aggregation`, `aggregate_options`, `access_key`, `visibility_path_regex`) VALUES (@newModuleId, 'branch_settings', 0, 0, '', 100, 'Automatische deploy', 'WTS', 'multiselect', 'Configuraties om te pauzeren', 'configurations_to_pause', 'Indien de WTS \'s nachts bepaalde acties uitvoert die de automatische deploy in de weg kunnen zitten, of om andere redenen gepauzeerd moeten worden tijdens of voor de deploy, dan kun je hier selecteren welke dat moeten zijn.&nbsp;', 8, '', 0, 0, '', 60, 0, '{\"saveValueAsItemLink\":false}', 'SELECT\n    id,\n    CONCAT(\'(\', id, \') \', IFNULL(configuration, \'Onbekende configuratie\'), \' -> \', IFNULL(action, \'Onbekende actie\'), \' (Time ID: \', time_id, \')\') AS name\nFROM wts_services\nWHERE state != \'stopped\'\nORDER BY configuration ASC, time_id ASC', '', '', '', '', '', '', '', NULL, '', '', '', 0, NULL, 0, 0, 'normal', '0', 0, '', '', '');
INSERT INTO `wiser_entityproperty` (`module_id`, `entity_name`, `link_type`, `visible_in_overview`, `overview_fieldtype`, `overview_width`, `tab_name`, `group_name`, `inputtype`, `display_name`, `property_name`, `explanation`, `ordering`, `regex_validation`, `mandatory`, `readonly`, `default_value`, `width`, `height`, `options`, `data_query`, `action_query`, `search_query`, `search_count_query`, `grid_delete_query`, `grid_insert_query`, `grid_update_query`, `depends_on_field`, `depends_on_operator`, `depends_on_value`, `language_code`, `custom_script`, `also_save_seo_value`, `depends_on_action`, `save_on_change`, `extended_explanation`, `label_style`, `label_width`, `enable_aggregation`, `aggregate_options`, `access_key`, `visibility_path_regex`) VALUES (@newModuleId, 'branch_settings', 0, 0, '', 100, 'Automatische deploy', 'WTS', 'date-time picker', 'Configuraties pauzeren om', 'configurations_pause_time', 'De tijd van de dag waarop de configuraties, welke in het vorige veld zijn geselecteerd, gepauzeerd moeten worden.', 9, '', 0, 0, '', 20, 0, '{\"type\":\"datetime\"}', '', '', '', '', '', '', '', '', NULL, '', '', '', 0, NULL, 0, 0, 'normal', '0', 0, '', '', '');
INSERT INTO `wiser_entityproperty` (`module_id`, `entity_name`, `link_type`, `visible_in_overview`, `overview_fieldtype`, `overview_width`, `tab_name`, `group_name`, `inputtype`, `display_name`, `property_name`, `explanation`, `ordering`, `regex_validation`, `mandatory`, `readonly`, `default_value`, `width`, `height`, `options`, `data_query`, `action_query`, `search_query`, `search_count_query`, `grid_delete_query`, `grid_insert_query`, `grid_update_query`, `depends_on_field`, `depends_on_operator`, `depends_on_value`, `language_code`, `custom_script`, `also_save_seo_value`, `depends_on_action`, `save_on_change`, `extended_explanation`, `label_style`, `label_width`, `enable_aggregation`, `aggregate_options`, `access_key`, `visibility_path_regex`) VALUES (@newModuleId, 'branch_settings', 0, 0, '', 100, 'Automatische deploy', 'WTS', 'date-time picker', 'Deploy starten om', 'deploy_start_time', 'De tijd van de dag waarop de automatische deploy uitgevoerd moet worden. Let op dat deze tijd later moet zijn dan \"Tijd voor pauze\", anders heeft \"Tijd voor pauze\" geen nut.', 10, '', 0, 0, '', 20, 0, '{\"type\":\"datetime\"}', '', '', '', '', '', '', '', '', NULL, '', '', '', 0, NULL, 0, 0, 'normal', '0', 0, '', '', '');

-- Add an item for the branch settings.
INSERT INTO `wiser_item` (entity_type, moduleid, title, added_by) VALUES ('branch_settings', @newModuleId, 'Standaard instellingen', 'Wiser');
SET @newItemId = LAST_INSERT_ID();

-- Set up the branch settings module to always open the newly created item.
UPDATE `wiser_module` SET `options` = CONCAT('{\n    \"gridViewMode\": true,\n    \"gridViewSettings\": {\n        \"informationBlock\": {\n            \"position\": \"left\",\n            \"width\": \"100%\",\n            \"openGridItemsInBlock\": true,\n            \"initialItem\": {\n                \"itemId\": ', @newItemId, ',\n                \"entityType\": \"branch_settings\",\n                \"readOnly\": false\n            }\n        }\n    },\n    \"onlyOneInstanceAllowed\": true\n}') WHERE `id` = @newModuleId;
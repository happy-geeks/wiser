-- Translations
INSERT INTO `wiser_item` (`entity_type`, `title`, `added_by`, `moduleid`) VALUES ('translations', 'Vertalingen', 'Systeem', 5507);
SET @translationsItemId = LAST_INSERT_ID();
INSERT INTO `easy_objects`(`typenr`, `level`, `key`, `value`, `order`, `active`, `parent_id`) VALUES (-1, 0, 'W2LANGUAGES_TranslationsItemId', @translationsItemId, 0, 1, 0);
INSERT INTO `wiser_module` (`id`, `custom_query`, `count_query`, `options`, `name`, `icon`, `color`, `type`, `group`) VALUES (5507, NULL, NULL, CONCAT('{\r\n	\"gridViewMode\": true,\r\n	\"gridViewSettings\": {\r\n        \"informationBlock\": {\r\n            \"position\": \"left\",\r\n            \"width\": \"100%\",\r\n            \"openGridItemsInBlock\": true,\r\n            \"initialItem\": {\r\n				\"itemId\": ', @translationsItemId, ',\r\n                \"entityType\": \"translations\",\r\n                \"readOnly\": false,\r\n				\"hideHeader\": true,\r\n				\"hideFooter\": true\r\n            }\r\n        }\r\n	}\r\n}'), 'Vertalingen', 'flag', NULL, 'DynamicItems', 'Contentbeheer');
INSERT INTO `wiser_entity`(`name`, `module_id`, `accepted_childtypes`, `show_title_field`) VALUES ('', 5507, 'translations', 1);
INSERT INTO `wiser_entity`(`name`, `module_id`, `accepted_childtypes`, `show_title_field`) VALUES ('translations', 5507, '', 0);
INSERT INTO `wiser_entityproperty`(`module_id`, `entity_name`, `inputtype`, `display_name`, `property_name`, `ordering`, `width`, `options`, `data_query`, `grid_delete_query`, `grid_insert_query`, `grid_update_query`) VALUES (5507, 'translations', 'sub-entities-grid', 'Vertalingen', 'translations', 1, 100, '{\r\n	\"customQuery\": true,\r\n	\"hasCustomInsertQuery\": true,\r\n	\"hasCustomUpdateQuery\": true,\r\n	\"hasCustomDeleteQuery\": true,\r\n	\"disableOpeningOfItems\": true,\r\n	\"pageSize\": 18\r\n}', 'SET @joins = \'\';\r\nSET @selects = NULL;\r\nSET @languageCounter = 0;\r\n\r\n# This is so that the results of this query won\'t get shown in the grid, because MySQL will always return the results of the first SELECT query.\r\nSET @dummy = (\r\n	SELECT NULL\r\n	FROM (\r\n		SELECT \r\n			lang.id, \r\n			languageCode.`value`,\r\n			@languageCounter := @languageCounter + 1,\r\n			@selects := CONCAT_WS(\',\\n\', @selects, CONCAT(\'CONCAT_WS(\\\'\\\', `\', languageCode.`value`, \'`.value, `\', languageCode.`value`, \'`.long_value) AS `\', lang.title, \'`\')),\r\n			@joins := CONCAT(@joins, \'\\n\', \'LEFT JOIN wiser_itemdetail AS `\', languageCode.`value`, \'` ON `\', languageCode.`value`, \'`.item_id = main.item_id AND `\', languageCode.`value`, \'`.`key` = main.`key` AND `\', languageCode.`value`, \'`.language_code = \\\'\', languageCode.`value`, \'\\\'\')\r\n		FROM wiser_item AS lang\r\n		JOIN wiser_itemdetail AS languageCode ON languageCode.item_id = lang.id AND languageCode.`key` = \'language_code\'\r\n		LEFT JOIN wiser_itemlink AS link ON link.item_id = lang.id AND link.type = 1\r\n		WHERE lang.entity_type = \'language\'\r\n		ORDER BY IFNULL(link.ordering, 999)\r\n	) AS x\r\n	LIMIT 1\r\n);\r\n\r\nSET @fullQuery = CONCAT(\'SELECT \r\n	main.id AS id_hide,\r\n	main.`key` AS `Naam`,\',\r\n	@selects, \'\r\nFROM (\r\n	SELECT \r\n		main.id,\r\n		main.item_id,\r\n		main.`key`\r\n	FROM wiser_itemdetail AS main\r\n	WHERE main.item_id = {itemId}\r\n	GROUP BY main.`key`\r\n) AS main\',\r\n@joins);\r\n\r\nPREPARE `stmt1` FROM @fullQuery;\r\nEXECUTE `stmt1`;\r\nDEALLOCATE PREPARE `stmt1`;', 'DELETE FROM wiser_itemdetail WHERE item_id = {itemId} AND `key` = ?naam;', '-- Empty --', 'INSERT INTO wiser_itemdetail (language_code, item_id, groupname, `key`, `value`, `long_value`)\r\nSELECT \r\n	languageCode.`value`, \r\n	\'{itemId}\', \r\n	\'translations\', \r\n	?naam,\r\n	\'\',\r\n	CASE \r\n		WHEN lang.title = \'{propertyKey2}\' THEN \'{propertyValue2}\'\r\n		WHEN lang.title = \'{propertyKey3}\' THEN \'{propertyValue3}\'\r\n		WHEN lang.title = \'{propertyKey4}\' THEN \'{propertyValue4}\'\r\n		WHEN lang.title = \'{propertyKey5}\' THEN \'{propertyValue5}\'\r\n		WHEN lang.title = \'{propertyKey6}\' THEN \'{propertyValue6}\'\r\n		WHEN lang.title = \'{propertyKey7}\' THEN \'{propertyValue7}\'\r\n		WHEN lang.title = \'{propertyKey8}\' THEN \'{propertyValue8}\'\r\n		WHEN lang.title = \'{propertyKey9}\' THEN \'{propertyValue9}\'\r\n		WHEN lang.title = \'{propertyKey10}\' THEN \'{propertyValue10}\'\r\n		WHEN lang.title = \'{propertyKey11}\' THEN \'{propertyValue11}\'\r\n	END\r\nFROM wiser_item AS lang\r\nJOIN wiser_itemdetail AS languageCode ON languageCode.item_id = lang.id AND languageCode.`key` = \'language_code\'\r\nWHERE lang.entity_type = \'language\'\r\nAND lang.title IN (\'{propertyKey2}\', \'{propertyKey3}\', \'{propertyKey4}\', \'{propertyKey5}\', \'{propertyKey6}\', \'{propertyKey7}\', \'{propertyKey8}\', \'{propertyKey9}\', \'{propertyKey10}\', \'{propertyKey11}\')\r\nON DUPLICATE KEY UPDATE `value` = VALUES(`value`), long_value = VALUES(long_value)');

-- Languages
INSERT INTO `wiser_entity`(`name`, `module_id`, `accepted_childtypes`, `icon`, `icon_add`) VALUES ('language', 700, '', '', '');
INSERT INTO `wiser_entityproperty` (`module_id`, `entity_name`, `link_type`, `visible_in_overview`, `overview_fieldtype`, `overview_width`, `tab_name`, `group_name`, `inputtype`, `display_name`, `property_name`, `explanation`, `ordering`, `regex_validation`, `mandatory`, `readonly`, `default_value`, `automation`, `css`, `width`, `height`, `options`, `data_query`, `action_query`, `search_query`, `search_count_query`, `grid_delete_query`, `grid_insert_query`, `grid_update_query`, `depends_on_field`, `depends_on_operator`, `depends_on_value`, `language_code`, `custom_script`, `also_save_seo_value`, `depends_on_action`, `save_on_change`, `extended_explanation`, `label_style`, `label_width`) VALUES (0, 'language', 0, 0, '', 100, '', '', 'input', 'Taalcode', 'language_code', NULL, 1, '', 0, 0, NULL, '', '', 0, 0, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, '', NULL, 0, NULL, 0, 0, NULL, NULL);
INSERT INTO `wiser_entityproperty` (`module_id`, `entity_name`, `link_type`, `visible_in_overview`, `overview_fieldtype`, `overview_width`, `tab_name`, `group_name`, `inputtype`, `display_name`, `property_name`, `explanation`, `ordering`, `regex_validation`, `mandatory`, `readonly`, `default_value`, `automation`, `css`, `width`, `height`, `options`, `data_query`, `action_query`, `search_query`, `search_count_query`, `grid_delete_query`, `grid_insert_query`, `grid_update_query`, `depends_on_field`, `depends_on_operator`, `depends_on_value`, `language_code`, `custom_script`, `also_save_seo_value`, `depends_on_action`, `save_on_change`, `extended_explanation`, `label_style`, `label_width`) VALUES (0, 'language', 0, 0, '', 100, '', '', 'input', 'Hreflang code', 'hreflang', NULL, 2, '', 0, 0, NULL, '', '', 0, 0, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, '', NULL, 0, NULL, 0, 0, NULL, NULL);
INSERT INTO `wiser_entityproperty` (`module_id`, `entity_name`, `link_type`, `visible_in_overview`, `overview_fieldtype`, `overview_width`, `tab_name`, `group_name`, `inputtype`, `display_name`, `property_name`, `explanation`, `ordering`, `regex_validation`, `mandatory`, `readonly`, `default_value`, `automation`, `css`, `width`, `height`, `options`, `data_query`, `action_query`, `search_query`, `search_count_query`, `grid_delete_query`, `grid_insert_query`, `grid_update_query`, `depends_on_field`, `depends_on_operator`, `depends_on_value`, `language_code`, `custom_script`, `also_save_seo_value`, `depends_on_action`, `save_on_change`, `extended_explanation`, `label_style`, `label_width`) VALUES (0, 'language', 0, 0, '', 100, '', '', 'input', 'Domein url', 'domain_url', NULL, 3, '', 0, 0, NULL, '', '', 0, 0, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, '', NULL, 0, NULL, 0, 0, NULL, NULL);
INSERT INTO `wiser_entityproperty` (`module_id`, `entity_name`, `link_type`, `visible_in_overview`, `overview_fieldtype`, `overview_width`, `tab_name`, `group_name`, `inputtype`, `display_name`, `property_name`, `explanation`, `ordering`, `regex_validation`, `mandatory`, `readonly`, `default_value`, `automation`, `css`, `width`, `height`, `options`, `data_query`, `action_query`, `search_query`, `search_count_query`, `grid_delete_query`, `grid_insert_query`, `grid_update_query`, `depends_on_field`, `depends_on_operator`, `depends_on_value`, `language_code`, `custom_script`, `also_save_seo_value`, `depends_on_action`, `save_on_change`, `extended_explanation`, `label_style`, `label_width`) VALUES (0, 'language', 0, 0, '', 100, '', '', 'checkbox', 'Is standaard taal', 'is_default_language', NULL, 4, '', 0, 0, NULL, '', '', 0, 0, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, '', NULL, 0, NULL, 0, 0, NULL, NULL);
INSERT INTO `wiser_itemlink` (item_id, destination_item_id, ordering, type) VALUES (LAST_INSERT_ID(), 0, 1, 1);
INSERT INTO `wiser_permission`(`role_id`, `entity_name`, `item_id`, `entity_property_id`, `permissions`, `module_id`) VALUES (1, '', 0, 0, 15, 5507);
INSERT INTO `wiser_link` (`type`, `destination_entity_type`, `connected_entity_type`, `name`, `show_in_tree_view`, `show_in_data_selector`, `relationship`, `duplication`, `use_item_parent_id`) VALUES (1, 'map', 'language', 'language', 1, 0, 'one-to-one', 'none', 1);
INSERT INTO `wiser_item` (`entity_type`, `title`, `added_by`, `moduleid`) VALUES ('map', 'Talen', 'Systeem', 700);
SET @languagesDirectoryItemId = LAST_INSERT_ID();
INSERT INTO `wiser_item` (`entity_type`, `title`, `parent_item_id`, `added_by`, `moduleid`) VALUES ('language', 'Nederlands', @languagesDirectoryItemId, 'Systeem', 700);
SET @dutchItemId = LAST_INSERT_ID();
INSERT INTO `wiser_itemdetail` (`item_id`, `key`, `value`) VALUES (@dutchItemId, 'hreflang', 'nl');
INSERT INTO `wiser_itemdetail` (`item_id`, `key`, `value`) VALUES (@dutchItemId, 'language_code', 'nl');
INSERT INTO `wiser_itemdetail` (`item_id`, `key`, `value`) VALUES (@dutchItemId, 'is_default_language', '1');
INSERT INTO `wiser_item` (`entity_type`, `title`, `parent_item_id`, `added_by`, `moduleid`) VALUES ('language', 'Engels', @languagesDirectoryItemId, 'Systeem', 700);
SET @englishItemId = LAST_INSERT_ID();
INSERT INTO `wiser_itemdetail` (`item_id`, `key`, `value`) VALUES (@englishItemId, 'hreflang', 'en');
INSERT INTO `wiser_itemdetail` (`item_id`, `key`, `value`) VALUES (@englishItemId, 'language_code', 'en');
INSERT INTO `wiser_itemdetail` (`item_id`, `key`, `value`) VALUES (@englishItemId, 'is_default_language', '0');
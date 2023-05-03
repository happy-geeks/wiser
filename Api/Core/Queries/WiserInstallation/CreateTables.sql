SET NAMES utf8mb4 COLLATE utf8mb4_general_ci;
SET FOREIGN_KEY_CHECKS = 0;

-- ----------------------------
-- Table structure for wiser_login_attempts
-- ----------------------------
CREATE TABLE IF NOT EXISTS `wiser_login_attempts`  (
  `id` int NOT NULL AUTO_INCREMENT,
  `ip_address` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `attempts` int NOT NULL DEFAULT 0,
  `blocked` tinyint(1) NOT NULL DEFAULT 0,
  `username` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '0',
  PRIMARY KEY (`id`) USING BTREE,
  UNIQUE INDEX `idx_username`(`username`) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for wiser_entity
-- ----------------------------
-- NOTE: When changing columns, make sure to also change the triggers for this table!
CREATE TABLE IF NOT EXISTS `wiser_entity`  (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `module_id` int NOT NULL DEFAULT 0,
  `accepted_childtypes` varchar(1000) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '' COMMENT 'comma separated list of accepted child nodes to be created',
  `icon` varchar(25) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `icon_add` varchar(25) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `icon_expanded` varchar(25) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `show_in_tree_view` tinyint(1) NOT NULL DEFAULT 1,
  `query_after_insert` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
  `query_after_update` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
  `query_before_update` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
  `query_before_delete` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
  `color` enum('blue','orange','yellow','green','red') CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT 'blue',
  `show_in_search` tinyint(1) NOT NULL DEFAULT 1,
  `show_overview_tab` tinyint(1) NOT NULL DEFAULT 1,
  `save_title_as_seo` tinyint(1) NOT NULL DEFAULT 0,
  `api_after_insert` int NULL DEFAULT NULL,
  `api_after_update` int NULL DEFAULT NULL,
  `api_before_update` int NULL DEFAULT NULL,
  `api_before_delete` int NULL DEFAULT NULL,
  `show_title_field` tinyint(1) NOT NULL DEFAULT 1,
  `friendly_name` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `default_ordering` enum('link_ordering','item_title') CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT 'link_ordering',
  `template_query` mediumtext,
  `template_html` mediumtext,
  `save_history` tinyint(1) NOT NULL DEFAULT 1,
  `enable_multiple_environments` tinyint(1) NOT NULL DEFAULT 0 COMMENT 'Whether or not to enable multiple environments for entities of this type. This means that the test can have a different version of an item than the live for example.',
  `dedicated_table_prefix` varchar(25) NOT NULL DEFAULT '',
  `delete_action` enum('archive','permanent','hide','disallow') CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT 'archive',
  `show_in_dashboard` tinyint(1) NOT NULL DEFAULT 0,
  `store_type` enum('normal','document_store','hybrid') CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT 'normal',
  PRIMARY KEY (`id`) USING BTREE,
  UNIQUE INDEX `name_module_id`(`name`, `module_id`) USING BTREE,
  INDEX `name`(`name`(100), `show_in_tree_view`) USING BTREE,
  INDEX `module_id`(`module_id`) USING BTREE,
  INDEX `show_in_dashboard`(`show_in_dashboard`) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for wiser_entityproperty
-- ----------------------------
-- NOTE: When changing columns, make sure to also change the triggers for this table!
CREATE TABLE IF NOT EXISTS `wiser_entityproperty`  (
  `id` int NOT NULL AUTO_INCREMENT,
  `module_id` smallint NOT NULL DEFAULT 0,
  `entity_name` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `link_type` int(11) NOT NULL DEFAULT 0,
  `visible_in_overview` tinyint NOT NULL DEFAULT 0,
  `overview_width` smallint NOT NULL DEFAULT 100,
  `tab_name` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `group_name` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `inputtype` enum('input','secure-input','textbox','radiobutton','checkbox','combobox','multiselect','numeric-input','file-upload','HTMLeditor','querybuilder','date-time picker','grid','imagecoords','button','image-upload','gpslocation','daterange','sub-entities-grid','item-linker','color-picker','auto-increment','linked-item','action-button','data-selector','chart','scheduler','timeline','empty','iframe') CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT 'input',
  `display_name` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `property_name` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `explanation` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
  `ordering` smallint NOT NULL DEFAULT 1,
  `regex_validation` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `mandatory` tinyint NOT NULL DEFAULT 0,
  `readonly` tinyint NOT NULL DEFAULT 0,
  `default_value` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
  `automation` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '' COMMENT 'E.g. upperCaseFirst, trim, replaces, etc.',
  `css` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL COMMENT 'Optional css',
  `width` smallint NOT NULL DEFAULT 0,
  `height` smallint NOT NULL DEFAULT 0,
  `options` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL COMMENT 'The options for this item (in case of dropdown etc.)',
  `data_query` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL COMMENT 'Additionally load data from a query to load the options',
  `action_query` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL COMMENT 'A query for certain fields that can execute actions, such as action-button',
  `search_query` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL COMMENT 'This query is used in sub-entities-grids with the option to link existing items enabled. The data from the search window will be retrieved via this query, if it contains a value.',
  `search_count_query` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL COMMENT 'This query is used in combination with the \"search_query\". This should be the same query except that it should return a COUNT with the total number of results.',
  `grid_delete_query` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL COMMENT 'The query to remove records if a node is removed',
  `grid_insert_query` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL COMMENT 'The query to save each record in the grid, always proceeded by the delete query',
  `grid_update_query` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL COMMENT 'The query for updating an existing record in a grid',
  `depends_on_field` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `depends_on_operator` enum('eq','neq','contains','doesnotcontain','startswith','doesnotstartwith','endswith','doesnotendwith','isempty','isnotempty','gte','gt','lte','lt') CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `depends_on_value` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `language_code` varchar(5) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `custom_script` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
  `also_save_seo_value` tinyint NOT NULL DEFAULT 0,
  `depends_on_action` enum('toggle-visibility','refresh') CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `save_on_change` tinyint NOT NULL DEFAULT 0,
  `extended_explanation` tinyint NOT NULL DEFAULT 0,
  `label_style` enum('normal','inline', 'float') CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `label_width` enum('0','10','20','30','40','50') CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `enable_aggregation` tinyint NOT NULL DEFAULT 0,
  `aggregate_options` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `access_key` varchar(1) NOT NULL DEFAULT '',
  `visibility_path_regex` varchar(255) NOT NULL DEFAULT '',
  PRIMARY KEY (`id`) USING BTREE,
  UNIQUE INDEX `idx_unique`(`entity_name`, `property_name`, `language_code`, `link_type`, `display_name`) USING BTREE,
  INDEX `idx_module_entity`(`module_id`, `entity_name`) USING BTREE,
  INDEX `idx_entity_overview`(`entity_name`, `visible_in_overview`) USING BTREE,
  INDEX `idx_link_overview`(`link_type`, `visible_in_overview`) USING BTREE,
  INDEX `idx_property`(`property_name`) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for wiser_field_templates
-- ----------------------------
-- NOTE: When changing columns, make sure to also change the triggers for this table!
CREATE TABLE IF NOT EXISTS `wiser_field_templates`  (
  `id` int NOT NULL AUTO_INCREMENT,
  `field_type` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `html_template` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
  `script_template` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
  `last_change` datetime(0) NULL DEFAULT CURRENT_TIMESTAMP(0) ON UPDATE CURRENT_TIMESTAMP(0),
  PRIMARY KEY (`id`) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for wiser_history
-- ----------------------------
CREATE TABLE IF NOT EXISTS `wiser_history`  (
  `id` bigint UNSIGNED NOT NULL AUTO_INCREMENT,
  `action` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL COMMENT 'added or changed',
  `tablename` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `item_id` bigint UNSIGNED NOT NULL DEFAULT 0,
  `changed_on` datetime(0) NOT NULL DEFAULT CURRENT_TIMESTAMP(0),
  `changed_by` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `field` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `oldvalue` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
  `newvalue` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
  `language_code` varchar(5) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `groupname` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  PRIMARY KEY (`id`) USING BTREE,
  INDEX `idx_item_id`(`item_id`, `field`) USING BTREE,
  INDEX `idx_changed_on`(`changed_on`) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for wiser_item
-- ----------------------------
-- NOTE: When changing columns, make sure to also change the triggers for this table!
CREATE TABLE IF NOT EXISTS `wiser_item`  (
  `id` bigint UNSIGNED NOT NULL AUTO_INCREMENT,
  `original_item_id` bigint UNSIGNED NOT NULL DEFAULT 0,
  `parent_item_id` bigint UNSIGNED NOT NULL DEFAULT 0,
  `ordering` mediumint NOT NULL DEFAULT 0,
  `unique_uuid` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `entity_type` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `moduleid` mediumint NOT NULL DEFAULT 0,
  `published_environment` mediumint NOT NULL DEFAULT 15 COMMENT 'Bitwise, 0 = hidden, 1 = development, 2 = test, 4 = acceptance, 8 = live',
  `readonly` tinyint NOT NULL DEFAULT 0,
  `title` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `added_on` datetime(0) NOT NULL DEFAULT CURRENT_TIMESTAMP(0),
  `added_by` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `changed_on` datetime(0) NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(0),
  `changed_by` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  PRIMARY KEY (`id`) USING BTREE,
  INDEX `moduleid`(`moduleid`, `published_environment`) USING BTREE,
  INDEX `entity_type`(`entity_type`, `unique_uuid`) USING BTREE,
  INDEX `unique_uuid`(`unique_uuid`) USING BTREE,
  INDEX `entity_environment`(`entity_type`, `published_environment`) USING BTREE,
  INDEX `original_item_id`(`original_item_id`) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for wiser_item_archive
-- ----------------------------
CREATE TABLE IF NOT EXISTS `wiser_item_archive`  (
  `id` bigint UNSIGNED NOT NULL AUTO_INCREMENT,
  `original_item_id` bigint UNSIGNED NOT NULL DEFAULT 0,
  `parent_item_id` bigint UNSIGNED NOT NULL DEFAULT 0,
  `ordering` mediumint NOT NULL DEFAULT 0,
  `unique_uuid` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `entity_type` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `moduleid` mediumint NOT NULL DEFAULT 0,
  `published_environment` mediumint NOT NULL DEFAULT 15 COMMENT 'Bitwise, 0 = hidden, 1 = development, 2 = test, 4 = acceptance, 8 = live',
  `readonly` tinyint NOT NULL DEFAULT 0,
  `title` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `added_on` datetime(0) NOT NULL DEFAULT CURRENT_TIMESTAMP(0),
  `added_by` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `changed_on` datetime(0) NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(0),
  `changed_by` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  PRIMARY KEY (`id`) USING BTREE,
  INDEX `idx_moduleid`(`moduleid`, `published_environment`) USING BTREE,
  INDEX `idx_entity_type`(`entity_type`, `unique_uuid`) USING BTREE,
  INDEX `idx_unique_uuid`(`unique_uuid`) USING BTREE,
  INDEX `idx_entity_environment`(`entity_type`, `published_environment`) USING BTREE,
  INDEX `idx_original_item_id`(`original_item_id`) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for wiser_itemdetail
-- ----------------------------
-- NOTE: When changing columns, make sure to also change the triggers for this table!
CREATE TABLE IF NOT EXISTS `wiser_itemdetail`  (
  `id` bigint UNSIGNED NOT NULL AUTO_INCREMENT,
  `language_code` varchar(5) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '' COMMENT 'leeg betekent beschikbaar voor alle talen',
  `item_id` bigint UNSIGNED NOT NULL DEFAULT 0,
  `groupname` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '' COMMENT 'optionele groepering van items, zoals een \'specs\' tabel',
  `key` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `value` varchar(1000) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `long_value` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL COMMENT 'Voor waardes die niet in \'value\' passen, zoals van HTMLeditors',
  PRIMARY KEY (`id`) USING BTREE,
  UNIQUE INDEX `item_key`(`item_id`, `key`, `language_code`) USING BTREE COMMENT 'voor opbouwen productoverzicht',
  INDEX `key_value`(`key`(50), `value`(100)) USING BTREE COMMENT 'filteren van items',
  INDEX `item_id_key_value`(`item_id`, `key`(40), `value`(40)) USING BTREE,
  INDEX `item_id_group`(`item_id`, `groupname`, `key`(40)) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for wiser_itemdetail_archive
-- ----------------------------
CREATE TABLE IF NOT EXISTS `wiser_itemdetail_archive`  (
  `id` bigint UNSIGNED NOT NULL AUTO_INCREMENT,
  `language_code` varchar(5) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '' COMMENT 'leeg betekent beschikbaar voor alle talen',
  `item_id` bigint UNSIGNED NOT NULL DEFAULT 0,
  `groupname` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '' COMMENT 'optionele groepering van items, zoals een \'specs\' tabel',
  `key` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `value` varchar(1000) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `long_value` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL COMMENT 'Voor waardes die niet in \'value\' passen, zoals van HTMLeditors',
  PRIMARY KEY (`id`) USING BTREE,
  UNIQUE INDEX `item_key`(`item_id`, `key`, `language_code`) USING BTREE COMMENT 'voor opbouwen productoverzicht',
  INDEX `key_value`(`key`(50), `value`(100)) USING BTREE COMMENT 'filteren van items',
  INDEX `item_id_key_value`(`item_id`, `key`(40), `value`(40)) USING BTREE,
  INDEX `item_id_group`(`item_id`, `groupname`, `key`(40)) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for wiser_itemfile
-- ----------------------------
CREATE TABLE IF NOT EXISTS `wiser_itemfile`  (
  `id` bigint NOT NULL AUTO_INCREMENT,
  `item_id` bigint UNSIGNED NOT NULL DEFAULT 0 COMMENT 'let op: dit is het item_id van de content',
  `content_type` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `content` longblob NULL,
  `content_url` varchar(1024) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `width` smallint NOT NULL DEFAULT 0,
  `height` smallint NOT NULL DEFAULT 0,
  `file_name` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `extension` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `added_on` datetime(0) NOT NULL DEFAULT CURRENT_TIMESTAMP(0),
  `added_by` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `title` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `property_name` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL COMMENT 'De naam van het veld waar deze afbeelding bijhoort',
  `itemlink_id` bigint UNSIGNED NOT NULL DEFAULT 0,
  `protected` tinyint NOT NULL DEFAULT 0 COMMENT 'Stel in op 1 om alleen toe te staan dat het bestand wordt opgehaald via een versleutelde id',
  `ordering` int NOT NULL DEFAULT 0,
  `extra_data` mediumtext,
  PRIMARY KEY (`id`) USING BTREE,
  INDEX `item_id`(`item_id`, `content_type`) USING BTREE,
  INDEX `idx_itemlinkid`(`itemlink_id`) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for wiser_itemfile_archive
-- ----------------------------
CREATE TABLE IF NOT EXISTS `wiser_itemfile_archive`  (
  `id` bigint NOT NULL AUTO_INCREMENT,
  `item_id` bigint NOT NULL DEFAULT 0 COMMENT 'let op: dit is het item_id van de content',
  `content_type` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `content` longblob NULL,
  `content_url` varchar(1024) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `width` smallint NOT NULL DEFAULT 0,
  `height` smallint NOT NULL DEFAULT 0,
  `file_name` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `extension` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `added_on` datetime(0) NOT NULL DEFAULT CURRENT_TIMESTAMP(0),
  `added_by` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `title` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `property_name` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL COMMENT 'De naam van het veld waar deze afbeelding bijhoort',
  `itemlink_id` bigint NOT NULL DEFAULT 0,
  `protected` tinyint NOT NULL DEFAULT 0 COMMENT 'Stel in op 1 om alleen toe te staan dat het bestand wordt opgehaald via een versleutelde id',
  `ordering` int NOT NULL DEFAULT 0,
  `extra_data` mediumtext,
  PRIMARY KEY (`id`) USING BTREE,
  INDEX `idx_item_id`(`item_id`, `property_name`) USING BTREE,
  INDEX `idx_item_link_id`(`itemlink_id`, `property_name`) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for wiser_itemlink
-- ----------------------------
-- NOTE: When changing columns, make sure to also change the triggers for this table!
CREATE TABLE IF NOT EXISTS `wiser_itemlink`  (
  `id` bigint NOT NULL AUTO_INCREMENT,
  `item_id` bigint NOT NULL DEFAULT 0,
  `destination_item_id` bigint NOT NULL DEFAULT 0 COMMENT 'where the item is connected to',
  `ordering` mediumint NOT NULL DEFAULT 1,
  `type` mediumint NOT NULL DEFAULT 1 COMMENT '1 = main connection, 2 = sub connection, 3 = generated (virtual), 4 = content(media), 100+  = custom',
  `added_on` timestamp(0) NOT NULL DEFAULT CURRENT_TIMESTAMP(0),
  PRIMARY KEY (`id`) USING BTREE,
  UNIQUE INDEX `uniquelink`(`item_id`, `destination_item_id`, `type`) USING BTREE,
  INDEX `type`(`type`, `destination_item_id`, `ordering`) USING BTREE,
  INDEX `item_id`(`item_id`) USING BTREE,
  INDEX `destination_item_id_2`(`destination_item_id`, `item_id`, `ordering`) USING BTREE,
  INDEX `destination_item_id`(`destination_item_id`, `type`, `ordering`) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for wiser_itemlink_archive
-- ----------------------------
CREATE TABLE IF NOT EXISTS `wiser_itemlink_archive`  (
  `id` bigint NOT NULL AUTO_INCREMENT,
  `item_id` bigint NOT NULL DEFAULT 0,
  `destination_item_id` bigint NOT NULL DEFAULT 0 COMMENT 'where the item is connected to',
  `ordering` mediumint NOT NULL DEFAULT 1,
  `type` mediumint NOT NULL DEFAULT 1 COMMENT '1 = main connection, 2 = sub connection, 3 = generated (virtual), 4 = content(media), 100+  = custom',
  `added_on` timestamp(0) NOT NULL DEFAULT CURRENT_TIMESTAMP(0),
  PRIMARY KEY (`id`) USING BTREE,
  UNIQUE INDEX `uniquelink`(`item_id`, `destination_item_id`, `type`) USING BTREE,
  INDEX `type`(`type`, `destination_item_id`, `ordering`) USING BTREE,
  INDEX `item_id`(`item_id`) USING BTREE,
  INDEX `destination_item_id_2`(`destination_item_id`, `item_id`, `ordering`) USING BTREE,
  INDEX `destination_item_id`(`destination_item_id`, `type`, `ordering`) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for wiser_itemlinkdetail
-- ----------------------------
-- NOTE: When changing columns, make sure to also change the triggers for this table!
CREATE TABLE IF NOT EXISTS `wiser_itemlinkdetail`  (
  `id` bigint UNSIGNED NOT NULL AUTO_INCREMENT,
  `language_code` varchar(5) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '' COMMENT 'leeg betekent beschikbaar voor alle talen',
  `itemlink_id` bigint UNSIGNED NOT NULL DEFAULT 0,
  `groupname` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '' COMMENT 'optionele groepering van items, zoals een \'specs\' tabel',
  `key` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `value` varchar(1000) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `long_value` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL COMMENT 'Voor waardes die niet in \'value\' passen, zoals van HTMLeditors',
  PRIMARY KEY (`id`) USING BTREE,
  UNIQUE INDEX `itemlink_key`(`itemlink_id`, `key`, `language_code`) USING BTREE COMMENT 'voor opbouwen productoverzicht',
  INDEX `itemlink_id`(`itemlink_id`) USING BTREE COMMENT 'voor zoeken waardes van 1 item',
  INDEX `language_id`(`key`(50), `value`(100)) USING BTREE COMMENT 'filteren van items'
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for wiser_itemlinkdetail_archive
-- ----------------------------
CREATE TABLE IF NOT EXISTS `wiser_itemlinkdetail_archive`  (
  `id` bigint UNSIGNED NOT NULL AUTO_INCREMENT,
  `language_code` varchar(5) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '' COMMENT 'leeg betekent beschikbaar voor alle talen',
  `itemlink_id` bigint UNSIGNED NOT NULL DEFAULT 0,
  `groupname` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '' COMMENT 'optionele groepering van items, zoals een \'specs\' tabel',
  `key` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `value` varchar(1000) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `long_value` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL COMMENT 'Voor waardes die niet in \'value\' passen, zoals van HTMLeditors',
  PRIMARY KEY (`id`) USING BTREE,
  UNIQUE INDEX `itemlink_key`(`itemlink_id`, `key`, `language_code`) USING BTREE COMMENT 'voor opbouwen productoverzicht',
  INDEX `itemlink_id`(`itemlink_id`) USING BTREE COMMENT 'voor zoeken waardes van 1 item',
  INDEX `language_id`(`key`(50), `value`(100)) USING BTREE COMMENT 'filteren van items'
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for wiser_link
-- ----------------------------
-- NOTE: When changing columns, make sure to also change the triggers for this table!
CREATE TABLE IF NOT EXISTS `wiser_link`  (
  `id` int NOT NULL AUTO_INCREMENT,
  `type` int NOT NULL,
  `destination_entity_type` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `connected_entity_type` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `name` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `show_in_tree_view` tinyint NOT NULL DEFAULT 1,
  `show_in_data_selector` tinyint NOT NULL DEFAULT 1,
  `relationship` enum('one-to-one','one-to-many','many-to-many') CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT 'one-to-many',
  `duplication` enum('none','copy-link','copy-item') CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT 'none' COMMENT 'What to do with this link, when an item is being duplicated. None means that links of this type will not be copied/duplicatied to the new item. Copy-link means that the linked item will also be linked to the new item. Copy-item means that the linked item will also be duplicated and then that duplicated item will be linked to the new item.',
  `use_item_parent_id` tinyint(1) NOT NULL DEFAULT 0 COMMENT 'Set this to 1 to use the column \"parent_item_id\" from wiser_item for these links. This will then no longer use or need the table wiser_itemlink for these links.',
  `use_dedicated_table` tinyint(1) NOT NULL DEFAULT 0 COMMENT 'Set this to 1 to use a dedicated table for links of this type. The GCL and Wiser expect there to be a table \"[linkType]_wiser_itemlink\" to store the links in. So if your link type is \"1\", we will use the table \"1_wiser_itemlink\" instead of \"wiser_itemlink\". This table will not be created automatically. To create this table, make a copy of wiser_itemlink (including triggers, but the the name of the table in the triggers too).',
  `cascade_delete` tinyint(1) NOT NULL DEFAULT 0 COMMENT 'Set this to 1 to also delete children when a parent is being deleted.',
  PRIMARY KEY (`id`) USING BTREE,
  UNIQUE INDEX `idx_link`(`type`, `destination_entity_type`, `connected_entity_type`) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for wiser_module
-- ----------------------------
-- NOTE: When changing columns, make sure to also change the triggers for this table!
CREATE TABLE IF NOT EXISTS `wiser_module`  (
  `id` int NOT NULL AUTO_INCREMENT,
  `custom_query` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL COMMENT 'Voor sommige modules kun je een query instellen voor wat die module initieel moet laden.',
  `count_query` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL COMMENT 'De query om te tellen hoeveel resultaten er in totaal zijn voor custom_query.',
  `options` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL COMMENT 'Eventuele opties voor deze module (als JSON object)',
  `name` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `icon` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `color` varchar(8) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `type` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT 'DynamicItems',
  `group` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  PRIMARY KEY (`id`) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for wiser_permission
-- ----------------------------
-- NOTE: When changing columns, make sure to also change the triggers for this table!
CREATE TABLE IF NOT EXISTS `wiser_permission`  (
  `id` int NOT NULL AUTO_INCREMENT,
  `role_id` int NOT NULL DEFAULT 0,
  `entity_name` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `item_id` int NOT NULL DEFAULT 0,
  `entity_property_id` int NOT NULL DEFAULT 0,
  `permissions` int NOT NULL DEFAULT 0 COMMENT '0 = Nothing\r\n1 = Read\r\n2 = Create\r\n4 = Update\r\n8 = Delete',
  `module_id` int NOT NULL DEFAULT 0,
  `query_id` int NOT NULL DEFAULT 0,
  `data_selector_id` int NOT NULL DEFAULT 0,
  PRIMARY KEY (`id`) USING BTREE,
  UNIQUE INDEX `role_id`(`role_id`, `entity_name`, `item_id`, `entity_property_id`, `module_id`, `query_id`, `data_selector_id`) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for wiser_query
-- ----------------------------
-- NOTE: When changing columns, make sure to also change the triggers for this table!
CREATE TABLE IF NOT EXISTS `wiser_query`  (
  `id` int NOT NULL AUTO_INCREMENT,
  `description` varchar(512) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `query` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
  `show_in_export_module` tinyint(1) NOT NULL DEFAULT 0,
  `changed_on` datetime(0) NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(0),
  PRIMARY KEY (`id`) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for wiser_roles
-- ----------------------------
CREATE TABLE IF NOT EXISTS `wiser_roles`  (
  `id` int NOT NULL AUTO_INCREMENT,
  `role_name` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  PRIMARY KEY (`id`) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for wiser_user_auth_token
-- ----------------------------
CREATE TABLE IF NOT EXISTS `wiser_user_auth_token`  (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `selector` varchar(32) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `hashed_validator` varchar(150) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `user_id` bigint(20) NULL DEFAULT NULL,
  `expires` datetime NULL DEFAULT NULL,
  `refresh_token` varchar(128) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `ticket` mediumtext CHARACTER SET utf8 COLLATE utf8_general_ci NULL,
  `refresh_token_expires` datetime NULL DEFAULT NULL,
  PRIMARY KEY (`id`) USING BTREE,
  UNIQUE INDEX `idx_selector`(`selector`) USING BTREE,
  UNIQUE INDEX `idx_token`(`refresh_token`) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8 COLLATE = utf8_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for wiser_user_roles
-- ----------------------------
-- NOTE: When changing columns, make sure to also change the triggers for this table!
CREATE TABLE IF NOT EXISTS `wiser_user_roles`  (
  `id` int NOT NULL AUTO_INCREMENT,
  `user_id` int NOT NULL,
  `role_id` int NOT NULL,
  PRIMARY KEY (`id`) USING BTREE,
  UNIQUE INDEX `idx_user_id`(`user_id`, `role_id`) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for wiser_api_connection
-- ----------------------------
-- NOTE: When changing columns, make sure to also change the triggers for this table!
CREATE TABLE IF NOT EXISTS `wiser_api_connection`  (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `options` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
  `authentication_data` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
  PRIMARY KEY (`id`) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for wiser_communication
-- ----------------------------
CREATE TABLE `wiser_communication`  (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `receivers_data_selector_id` int NOT NULL DEFAULT 0,
  `receivers_query_id` int NOT NULL DEFAULT 0,
  `receiver_list` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
  `settings` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
  `send_trigger_type` enum('direct','fixed','recurring') CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `trigger_start` date NULL DEFAULT NULL,
  `trigger_end` date NULL DEFAULT NULL,
  `trigger_time` time NULL DEFAULT NULL,
  `trigger_period_value` tinyint NOT NULL DEFAULT 1,
  `trigger_period_type` enum('minute','hour','day','week','month','year') CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `trigger_week_days` int NOT NULL DEFAULT 0,
  `last_processed` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
  `added_by` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `added_on` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `trigger_day_of_month` int NOT NULL DEFAULT 0,
  `changed_by` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `changed_on` datetime NULL DEFAULT NULL,
  `content_data_selector_id` int NOT NULL DEFAULT 0,
  `content_query_id` int NOT NULL DEFAULT 0,
  PRIMARY KEY (`id`) USING BTREE,
  UNIQUE INDEX `idx_name`(`name`) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for wiser_communication_generated
-- ----------------------------
CREATE TABLE IF NOT EXISTS `wiser_communication_generated`  (
  `id` int NOT NULL AUTO_INCREMENT,
  `item_id` bigint NOT NULL DEFAULT 0,
  `communication_id` int NOT NULL DEFAULT 0,
  `receiver` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `receiver_name` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
  `bcc` text CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL COMMENT 'Multiple addresses can be separated by a semicolon (;).',
  `subject` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `content` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
  `uploaded_file` longblob NULL,
  `uploaded_filename` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `attachment_urls` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL COMMENT 'A collection of URLs where attachments should be downloaded from and added to the email. This works only with email communication types. Multiple URLs should be separated by newline.',
  `communicationtype` enum('email','sms','whatsapp','pdf') CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT 'email',
  `creation_date` timestamp(0) NOT NULL DEFAULT CURRENT_TIMESTAMP(0),
  `send_date` datetime(0) NULL DEFAULT NULL,
  `processed_date` datetime(0) NULL DEFAULT NULL,
  `status_code` varchar(30) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `status_message` text CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
  `cc` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
  `reply_to` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
  `reply_to_name` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
  `sender` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
  `sender_name` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
  `wiser_item_files` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL COMMENT 'One or more IDs from wiser_itemfile that should be sent with the communication as attachments. Only works for e-mail communications.',
  `attempt_count` int NOT NULL DEFAULT 0,
  `last_attempt` datetime NULL,
  `is_internal_error_mail` tinyint(1) NOT NULL DEFAULT 0 COMMENT 'If the WTS was not able to send a communication after X tries, it will send an e-mail to notify us about the problem. For these e-mails, this column will be set to 1, so that we can use that to make sure we don\'t send too many errors.',
  PRIMARY KEY (`id`) USING BTREE,
  INDEX `idx_communication_id`(`communication_id`) USING BTREE,
  INDEX `idx_send_processed_date`(`send_date`, `processed_date`) USING BTREE,
  INDEX `idx_internal_error`(`send_date`, `is_internal_error_mail`) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for wiser_data_selector
-- ----------------------------
-- NOTE: When changing columns, make sure to also change the triggers for this table!
CREATE TABLE IF NOT EXISTS `wiser_data_selector`  (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '' COMMENT 'The name of the data selector item.',
  `removed` tinyint NOT NULL DEFAULT 0,
  `module_selection` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '' COMMENT 'Comma separated list of module IDs.',
  `request_json` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL COMMENT 'The JSON that was created to be used in the request.',
  `saved_json` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL COMMENT 'Saved value of the JSON, which is used during loading. This is slightly different from the request JSON as it contains more information.',
  `added_on` datetime(0) NOT NULL DEFAULT CURRENT_TIMESTAMP(0),
  `changed_on` datetime(0) NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(0),
  `show_in_export_module` tinyint(1) NOT NULL DEFAULT 1,
  `available_for_rendering` tinyint(1) NOT NULL DEFAULT 1,
  `default_template` bigint UNSIGNED NOT NULL DEFAULT 0,
  `show_in_dashboard` tinyint(1) NOT NULL DEFAULT 0,
  PRIMARY KEY (`id`) USING BTREE,
  INDEX `idx_name`(`name`) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for easy_objects
-- ----------------------------
CREATE TABLE IF NOT EXISTS `easy_objects`  (
  `id` int NOT NULL AUTO_INCREMENT,
  `customerid` int NOT NULL DEFAULT 0,
  `typenr` int NOT NULL DEFAULT 0,
  `level` int NOT NULL DEFAULT 0,
  `key` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `value` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
  `order` int NOT NULL DEFAULT 0,
  `created` timestamp(0) NOT NULL DEFAULT CURRENT_TIMESTAMP(0),
  `active` tinyint NOT NULL DEFAULT 1,
  `parent_id` int NOT NULL DEFAULT 0,
  `description` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
  `visibleForCustomer` tinyint NOT NULL DEFAULT 0,
  PRIMARY KEY (`id`) USING BTREE,
  UNIQUE INDEX `type_key`(`typenr`, `key`) USING BTREE,
  INDEX `typenr`(`typenr`) USING BTREE,
  INDEX `typenractive`(`active`, `typenr`) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for wiser_import
-- ----------------------------
CREATE TABLE IF NOT EXISTS `wiser_import` (
  `id` int NOT NULL AUTO_INCREMENT,
  `added_on` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'The date and time that the import task was created',
  `added_by` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL COMMENT 'The name of the Wiser user that created this import task',
  `started_on` datetime NULL DEFAULT NULL COMMENT 'The date and time that the import actually started',
  `finished_on` datetime NULL DEFAULT NULL COMMENT 'The date and time that the import finished',
  `start_on` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'The date and time that the import should start',
  `name` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `success` tinyint(1) NULL DEFAULT NULL,
  `errors` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
  `data` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
  `user_id` bigint NOT NULL COMMENT 'The ID of the user that created this import task',
  `customer_id` int NOT NULL,
  `server_name` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL COMMENT 'The name of the server on which this import task was created',
  `sub_domain` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '' COMMENT 'The wiser sub domain that the user was on when starting the import.',
  `rollback_on` datetime NULL DEFAULT NULL COMMENT 'If this import needs to me rolled back, enter a datetime to start the rollback here',
  `rollback_started_on` datetime NULL DEFAULT NULL COMMENT 'The date and time that the rollback started',
  `rollback_finished_on` datetime NULL DEFAULT NULL COMMENT 'The date and time that the rollback finished',
  `rollback_success` tinyint(1) NULL DEFAULT NULL COMMENT 'Whether or not the rollback was successfull',
  `rollback_errors` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
  PRIMARY KEY (`id`) USING BTREE,
  INDEX `server_name`(`server_name`, `started_on`) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for wiser_import_log
-- ----------------------------
CREATE TABLE IF NOT EXISTS `wiser_import_log`  (
  `id` int NOT NULL AUTO_INCREMENT,
  `items_total` int NOT NULL DEFAULT 0,
  `items_created` int NOT NULL DEFAULT 0,
  `items_updated` int NOT NULL DEFAULT 0,
  `items_successful` int NOT NULL DEFAULT 0,
  `items_failed` int NOT NULL DEFAULT 0,
  `errors` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
  `added_on` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `added_by` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  PRIMARY KEY (`id`) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for wiser_grant_store
-- ----------------------------
CREATE TABLE IF NOT EXISTS `wiser_grant_store`  (
  `id` int NOT NULL AUTO_INCREMENT,
  `key` varchar(512) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `type` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `client_id` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `data` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
  `subject_id` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `description` varchar(1000) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `creation_time` datetime NOT NULL,
  `expiration` datetime NOT NULL,
  `session_id` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  PRIMARY KEY (`id`) USING BTREE,
  UNIQUE INDEX `idx_key`(`key`) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;


-- ----------------------------
-- Table structure for wiser_template
-- ----------------------------
CREATE TABLE IF NOT EXISTS `wiser_template`  (
   `id` int NOT NULL AUTO_INCREMENT,
   `parent_id` int NULL DEFAULT NULL,
   `template_name` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
   `template_data` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
   `template_data_minified` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
   `template_type` int NOT NULL,
   `version` mediumint NOT NULL,
   `template_id` int NOT NULL,
   `changed_on` datetime NULL DEFAULT NULL,
   `changed_by` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
   `published_environment` tinyint NOT NULL DEFAULT 0,
   `use_cache` int NOT NULL DEFAULT 0,
   `cache_minutes` int NOT NULL DEFAULT 0,
   `login_required` tinyint(1) NOT NULL DEFAULT 0,
   `login_role` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
   `login_redirect_url` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
   `linked_templates` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
   `ordering` int NOT NULL DEFAULT 0,
   `insert_mode` int NOT NULL DEFAULT 0,
   `load_always` tinyint(1) NOT NULL DEFAULT 0,
   `disable_minifier` tinyint(1) NOT NULL DEFAULT 0,
   `url_regex` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
   `external_files` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
   `grouping_create_object_instead_of_array` tinyint(1) NOT NULL DEFAULT 0,
   `grouping_prefix` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
   `grouping_key` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
   `grouping_key_column_name` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
   `grouping_value_column_name` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
   `removed` tinyint(1) NOT NULL DEFAULT 0,
   `is_scss_include_template` tinyint(1) NOT NULL DEFAULT 0,
   `use_in_wiser_html_editors` tinyint(1) NOT NULL DEFAULT 0,
   `pre_load_query` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
   `cache_location` int NOT NULL DEFAULT 0,
   `return_not_found_when_pre_load_query_has_no_data` tinyint(1) NOT NULL DEFAULT 0,
   `cache_regex` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
   `routine_type` int NOT NULL DEFAULT 0 COMMENT 'For routine templates only',
   `routine_parameters` text CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL COMMENT 'For routine templates only',
   `routine_return_type` varchar(25) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL COMMENT 'For routine templates only',
   `trigger_timing` int NOT NULL DEFAULT 0 COMMENT 'For trigger templates only',
   `trigger_event` int NOT NULL DEFAULT 0 COMMENT 'For trigger templates only',
   `trigger_table_name` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL COMMENT 'For trigger templates only',
   `is_default_header` tinyint(1) NOT NULL DEFAULT 0,
   `is_default_footer` tinyint(1) NOT NULL DEFAULT 0,
   `default_header_footer_regex` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
   `is_partial` tinyint(1) NOT NULL DEFAULT 0,
   `widget_content` mediumtext,
   `widget_location` tinyint(4) NOT NULL DEFAULT 1,
   PRIMARY KEY (`id`) USING BTREE,
   UNIQUE INDEX `idx_unique`(`template_id` ASC, `version` ASC) USING BTREE,
   INDEX `idx_removed`(`removed` ASC) USING BTREE,
   INDEX `template_id`(`template_id` ASC) USING BTREE,
   INDEX `idx_template_id`(`template_id` ASC, `removed` ASC) USING BTREE,
   INDEX `idx_parent_id`(`parent_id` ASC, `removed` ASC) USING BTREE,
   INDEX `idx_type`(`template_type` ASC, `removed` ASC) USING BTREE,
   INDEX `idx_environment`(`published_environment` ASC, `removed` ASC) USING BTREE,
   FULLTEXT INDEX `idx_fulltext`(`template_name`, `template_data`)
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for wiser_commit
-- ----------------------------
CREATE TABLE IF NOT EXISTS `wiser_commit`  (
    `id` int NOT NULL AUTO_INCREMENT,
    `description` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
    `external_id` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
    `added_on` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `added_by` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
    `completed` tinyint NOT NULL DEFAULT 0,
    PRIMARY KEY (`id`) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for wiser_commit_dynamic_content
-- ----------------------------
CREATE TABLE IF NOT EXISTS `wiser_commit_dynamic_content`  (
    `id` int NOT NULL AUTO_INCREMENT,
    `dynamic_content_id` int NOT NULL,
    `version` int NOT NULL,
    `commit_id` int NOT NULL,
    `added_on` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `added_by` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
    PRIMARY KEY (`id`) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for wiser_commit_template
-- ----------------------------
CREATE TABLE IF NOT EXISTS `wiser_commit_template`  (
    `id` int NOT NULL AUTO_INCREMENT,
    `template_id` int NOT NULL,
    `version` int NOT NULL,
    `commit_id` int NOT NULL,
    `added_on` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `added_by` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
    PRIMARY KEY (`id`) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;


-- ----------------------------
-- Table structure for wiser_dynamic_content
-- ----------------------------
CREATE TABLE IF NOT EXISTS `wiser_dynamic_content`  (
    `id` int NOT NULL AUTO_INCREMENT,
    `content_id` int NULL DEFAULT NULL,
    `settings` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
    `component` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
    `component_mode` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
    `version` mediumint NOT NULL DEFAULT 0,
    `title` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
    `changed_on` datetime NOT NULL ON UPDATE CURRENT_TIMESTAMP,
    `changed_by` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
    `published_environment` tinyint NOT NULL DEFAULT 0,
    `removed` tinyint NOT NULL DEFAULT 0,
    PRIMARY KEY (`id`) USING BTREE,
    UNIQUE INDEX `idx_unique`(`content_id` ASC, `version` ASC) USING BTREE,
    INDEX `content_id`(`content_id` ASC) USING BTREE,
    FULLTEXT INDEX `idx_fulltext`(`title`, `settings`, `component_mode`)
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for wiser_dynamic_content_publish_log
-- ----------------------------
CREATE TABLE IF NOT EXISTS `wiser_dynamic_content_publish_log`  (
    `id` int NOT NULL AUTO_INCREMENT,
    `content_id` int NOT NULL,
    `old_live` int NOT NULL,
    `old_accept` int NOT NULL,
    `old_test` int NOT NULL,
    `new_live` int NOT NULL,
    `new_accept` int NOT NULL,
    `new_test` int NOT NULL,
    `changed_on` datetime NOT NULL,
    `changed_by` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
    PRIMARY KEY (`id`) USING BTREE,
    INDEX `idx_content_id`(`content_id` ASC) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for wiser_preview_profiles
-- ----------------------------
CREATE TABLE IF NOT EXISTS `wiser_preview_profiles`  (
    `id` int NOT NULL AUTO_INCREMENT,
    `name` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
    `template_id` int NOT NULL,
    `url` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
    `variables` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
    PRIMARY KEY (`id`) USING BTREE,
    INDEX `idx_template_id`(`template_id` ASC) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for wiser_template_dynamic_content
-- ----------------------------
CREATE TABLE IF NOT EXISTS `wiser_template_dynamic_content`  (
   `id` int NOT NULL AUTO_INCREMENT,
   `content_id` int NOT NULL,
   `destination_template_id` int NOT NULL,
   `added_on` datetime NOT NULL,
   `added_by` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
   PRIMARY KEY (`id`) USING BTREE,
   UNIQUE INDEX `idx_unique`(`content_id` ASC, `destination_template_id` ASC) USING BTREE,
   INDEX `idx_destination`(`destination_template_id` ASC) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for wiser_template_publish_log
-- ----------------------------
CREATE TABLE IF NOT EXISTS `wiser_template_publish_log`  (
    `id` int NOT NULL AUTO_INCREMENT,
    `template_id` int NOT NULL,
    `old_live` int NOT NULL,
    `old_accept` int NOT NULL,
    `old_test` int NOT NULL,
    `new_live` int NOT NULL,
    `new_accept` int NOT NULL,
    `new_test` int NOT NULL,
    `changed_on` datetime NOT NULL,
    `changed_by` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
    PRIMARY KEY (`id`) USING BTREE,
    INDEX `idx_template_id`(`template_id` ASC) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for wiser_dashboard
-- ----------------------------
CREATE TABLE IF NOT EXISTS `wiser_dashboard`  (
  `id` int NOT NULL AUTO_INCREMENT,
  `last_update` datetime NOT NULL,
  `items_data` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
  `entities_data` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
  `user_login_count_top10` int NOT NULL DEFAULT 0,
  `user_login_count_other` int NOT NULL DEFAULT 0,
  `user_login_active_top10` bigint NOT NULL DEFAULT 0,
  `user_login_active_other` bigint NOT NULL DEFAULT 0,
  PRIMARY KEY (`id`) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for wiser_table_changes
-- ----------------------------
CREATE TABLE IF NOT EXISTS `wiser_table_changes`  (
    `name` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
    `last_update` datetime NOT NULL,
    PRIMARY KEY (`name`) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

SET FOREIGN_KEY_CHECKS = 1;
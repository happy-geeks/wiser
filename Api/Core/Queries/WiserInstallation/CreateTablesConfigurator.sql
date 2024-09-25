SET NAMES utf8;
SET FOREIGN_KEY_CHECKS = 0;

-- ----------------------------
-- Table structure for configurations_wiser_item
-- ----------------------------
CREATE TABLE IF NOT EXISTS `configurations_wiser_item`  (
  `id` bigint UNSIGNED NOT NULL AUTO_INCREMENT,
  `original_item_id` bigint UNSIGNED NOT NULL DEFAULT 0,
  `parent_item_id` bigint UNSIGNED NOT NULL DEFAULT 0,
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
-- Table structure for configurations_wiser_item_archive
-- ----------------------------
CREATE TABLE IF NOT EXISTS `configurations_wiser_item_archive`  (
  `id` bigint UNSIGNED NOT NULL AUTO_INCREMENT,
  `original_item_id` bigint UNSIGNED NOT NULL DEFAULT 0,
  `parent_item_id` bigint UNSIGNED NOT NULL DEFAULT 0,
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
-- Table structure for configurations_wiser_itemdetail
-- ----------------------------
CREATE TABLE IF NOT EXISTS `configurations_wiser_itemdetail`  (
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
-- Table structure for configurations_wiser_itemdetail_archive
-- ----------------------------
CREATE TABLE IF NOT EXISTS `configurations_wiser_itemdetail_archive`  (
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
-- Table structure for configurations_wiser_itemlinkdetail
-- ----------------------------
CREATE TABLE IF NOT EXISTS `configurations_wiser_itemlinkdetail`  (
  `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT,
  `language_code` varchar(5) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '' COMMENT 'leeg betekent beschikbaar voor alle talen',
  `itemlink_id` bigint(20) UNSIGNED NOT NULL DEFAULT 0,
  `groupname` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '' COMMENT 'optionele groepering van items, zoals een \'specs\' tabel',
  `key` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `value` varchar(1000) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL DEFAULT '',
  `long_value` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL COMMENT 'Voor waardes die niet in \'value\' passen, zoals van HTMLeditors',
  PRIMARY KEY (`id`) USING BTREE,
  UNIQUE INDEX `itemlink_key`(`itemlink_id`, `key`, `language_code`) USING BTREE COMMENT 'voor opbouwen productoverzicht',
  INDEX `itemlink_id`(`itemlink_id`) USING BTREE COMMENT 'voor zoeken waardes van 1 item',
  INDEX `key_value`(`key`(50), `value`(100)) USING BTREE COMMENT 'filteren van items'
) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = DYNAMIC;

-- ----------------------------
-- Table structure for configurations_wiser_itemlinkdetail_archive
-- ----------------------------
CREATE TABLE IF NOT EXISTS `configurations_wiser_itemlinkdetail_archive`  (
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
  INDEX `key_value`(`key`(50), `value`(100)) USING BTREE COMMENT 'filteren van items'
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for configurations_wiser_itemfile
-- ----------------------------
CREATE TABLE IF NOT EXISTS `configurations_wiser_itemfile` (
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
-- Table structure for configurations_wiser_itemfile_archive`
-- ----------------------------
CREATE TABLE IF NOT EXISTS `configurations_wiser_itemfile_archive`  (
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